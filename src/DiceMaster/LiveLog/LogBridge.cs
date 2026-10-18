using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;

namespace DiceMaster.LiveLog
{
    // Reflection binds only to names already present in the input mod. No game
    // implementation or guessed load/new-game hook is supplied by this payload.
    public static class LogBridge
    {
        private static readonly RefreshEngine engine = new RefreshEngine();
        private static readonly LifecycleState lifecycle = new LifecycleState();
        private static readonly BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        private static string lua, friendly;
        private static IDictionary options, optionStories, switchStories;
        private static IList switches, positions;
        private static Exception analysisFailure;
        private static bool parsed, refreshing;
        private static object owner;
        private static object eventParser;
        private static long pendingLuaToken;
        private static string pendingLua, pendingFriendly;
        private static bool manualTick;
        private static double tickTime;
        private static float lastSourceClock;
        private static bool hasSourceClock;
        private static readonly Dictionary<string, EventSource> eventSources = new Dictionary<string, EventSource>();
        private static readonly Dictionary<string, string> boundaryFailures = new Dictionary<string, string>();
        private static readonly Dictionary<string, Value> partialOptions = new Dictionary<string, Value>();
        private static readonly Dictionary<string, Value> partialEvents = new Dictionary<string, Value>();
        public static int AnalysisCount;

        private sealed class EventSource
        {
            public IList Entries;
            public Exception Error;
            public double RetryAt;
        }

        private sealed class Value
        {
            public string Key, Description, Text;
            public object Data, StoryState;
        }

        private static Type TypeFor(string name)
        {
            Type type = typeof(LogBridge).Assembly.GetType(name);
            if (type != null) return type;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(name);
                if (type != null) return type;
            }
            throw new TypeLoadException(name);
        }

        private static object Read(object value, string name)
        {
            if (value == null) throw new DataNotReadyException();
            Type type = value as Type ?? value.GetType();
            object target = value is Type ? null : value;
            for (Type at = type; at != null; at = at.BaseType)
            {
                FieldInfo field = at.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target);
                PropertyInfo property = at.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property != null) return property.GetValue(target, null);
            }
            throw new MissingMemberException(type.FullName, name);
        }

        public static object ReadFieldStrict(object value, string name)
        {
            object result = Read(value, name);
            // The real ConditionResultItem.Check dereferences _items. A null
            // list is invalid data, whereas an existing empty list is valid.
            if (result == null && name == "_items" && value != null &&
                value.GetType().FullName == "Mortal.Core.ConditionResultItem")
                throw new InvalidOperationException("Null condition collection.");
            return result;
        }

        private static void Write(object value, string name, object data)
        {
            Type type = value as Type ?? value.GetType();
            FieldInfo field = type.GetField(name, flags);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            field.SetValue(value is Type ? null : value, data);
        }

        private static object Call(object value, string name, params object[] args)
        {
            Type type = value as Type ?? value.GetType();
            MethodInfo selected = null;
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.Name != name || method.IsGenericMethodDefinition) continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != args.Length) continue;
                bool match = true;
                for (int i = 0; i < parameters.Length; i++)
                {
                    Type parameter = parameters[i].ParameterType;
                    if (parameter.IsByRef) parameter = parameter.GetElementType();
                    if (args[i] != null && !parameter.IsInstanceOfType(args[i])) match = false;
                }
                if (match) { selected = method; break; }
            }
            if (selected == null) throw new MissingMethodException(type.FullName, name);
            try { return selected.Invoke(value is Type ? null : value, args); }
            catch (TargetInvocationException error)
            {
                ExceptionDispatchInfo.Capture(error.InnerException ?? error).Throw();
                throw;
            }
        }

        private static object Parser() { return Read(TypeFor("DiceMaster.LuaParse.LuaParser"), "Instance"); }
        public static object CallDisplayHelper(string owner, string name, params object[] arguments)
        {
            return Call(TypeFor(owner), name, arguments);
        }
        public static bool DisplayIsProtected() { return (int)Read(Patch(), "DisplayRefreshDepth") != 0; }
        public static string DescribeEvent(object cache, object data)
        {
            int previousDepth = (int)Read(Patch(), "DisplayRefreshDepth");
            Write(Patch(), "DisplayRefreshDepth", previousDepth + 1);
            try
            {
                string result = (string)Call(cache, "GenerateDynamicDesc", data);
                if (previousDepth == 0) RecordDescriptionResult(data, null);
                return result;
            }
            catch (Exception error)
            {
                if (previousDepth != 0) throw;
                RecordDescriptionResult(data, error);
                return RefreshEngine.ReadFailureStatus;
            }
            finally { Write(Patch(), "DisplayRefreshDepth", previousDepth); }
        }
        public static void RecordDescriptionResult(object source, Exception error)
        {
            // Narrative Harmony callbacks must not prevent normal gameplay if
            // a description fails. Deduplicate their diagnostics independently
            // from the engine's per-row error/backoff state.
            try
            {
                string key = "description/" + (source == null ? 0 : RuntimeHelpers.GetHashCode(source));
                if (error == null)
                {
                    if (boundaryFailures.Remove(key)) Log("LiveLog item=" + key + " recovered");
                    return;
                }
                string cause = error.GetType().FullName + "\n" + error.StackTrace;
                string previous;
                if (!boundaryFailures.TryGetValue(key, out previous) || previous != cause)
                {
                    boundaryFailures[key] = cause;
                    Log("LiveLog item=" + key + " failure type=" + cause);
                }
            }
            catch (Exception) { }
        }
        public static object GetEventParser()
        {
            if (eventParser == null) eventParser = Activator.CreateInstance(TypeFor("DiceMaster.LuaParse.LuaParser"), true);
            return eventParser;
        }
        private static object Cache() { return Read(TypeFor("DiceMaster.PositionEventCache"), "Instance"); }
        private static object Patch() { return TypeFor("DiceMaster.OptionInfoPatch"); }
        private static void Protect() { Write(Patch(), "DisplayRefreshDepth", (int)Read(Patch(), "DisplayRefreshDepth") + 1); }
        private static void Unprotect() { Write(Patch(), "DisplayRefreshDepth", (int)Read(Patch(), "DisplayRefreshDepth") - 1); }
        private static bool Enabled()
        {
            object config = Read(TypeFor("DiceMaster.Config.ConfigManager"), "EnableOptionInfo");
            return config != null && (bool)Read(config, "Value");
        }
        private static void Log(string message) { Call(TypeFor("DiceMaster.DicePlugin"), "LogWarning", message); }

        private static void RunBoundary(string name, Action work)
        {
            int previousDepth = 0;
            bool captured = false;
            try
            {
                previousDepth = (int)Read(Patch(), "DisplayRefreshDepth");
                captured = true;
                work();
                if (boundaryFailures.Remove(name))
                    try { Log("LiveLog boundary=" + name + " recovered"); } catch (Exception) { }
            }
            catch (Exception error)
            {
                string cause = error.GetType().FullName + "\n" + error.StackTrace;
                string previous;
                if (!boundaryFailures.TryGetValue(name, out previous) || previous != cause)
                {
                    boundaryFailures[name] = cause;
                    try { Log("LiveLog boundary=" + name + " failure type=" + cause); } catch (Exception) { }
                }
            }
            finally
            {
                if (captured)
                    try { Write(Patch(), "DisplayRefreshDepth", previousDepth); } catch (Exception) { }
            }
        }

        public static void ResetContext()
        {
            long token = EnterTransition();
            ExitTransition(token);
        }

        public static long EnterTransition()
        {
            long token = 0;
            RunBoundary("transition", () => {
                token = lifecycle.EnterTransition();
                pendingLuaToken = 0; pendingLua = pendingFriendly = null;
                owner = Read(TypeFor("DiceMaster.DicePlugin"), "Instance");
                ResetCore();
                Write(Patch(), "_lastLuaString", null); Write(Patch(), "_lastFriendlyName", null);
                Write(Patch(), "TargetEvent", null); Write(Patch(), "TargetMission", null);
            });
            return token;
        }

        public static void ExitTransition(long token) { RunBoundary("transition-exit", () => lifecycle.ExitTransition(token)); }

        public static long StageLua(string text, string name)
        {
            long token = 0;
            RunBoundary("lua-stage", () => {
                if ((int)Read(Patch(), "DisplayRefreshDepth") != 0) return;
                token = lifecycle.StageLua();
                owner = Read(TypeFor("DiceMaster.DicePlugin"), "Instance");
                ResetCore();
                Write(Patch(), "_lastLuaString", null); Write(Patch(), "_lastFriendlyName", null);
                pendingLuaToken = token; pendingLua = text; pendingFriendly = name;
            });
            return token;
        }

        public static void CompleteLua(long token, object result)
        {
            RunBoundary("lua-complete", () => {
                if (!lifecycle.CompleteLua(token, result != null))
                {
                    if (pendingLuaToken == token) { pendingLuaToken = 0; pendingLua = pendingFriendly = null; }
                    return;
                }
                if (pendingLuaToken != token) { lifecycle.AbortLua(token); ResetCore(); return; }
                string text = pendingLua, name = pendingFriendly;
                pendingLuaToken = 0; pendingLua = pendingFriendly = null;
                NewLuaCore(text, name);
            });
        }

        public static void AbortLua(long token)
        {
            RunBoundary("lua-abort", () => {
                bool wasReady = lifecycle.IsReady;
                lifecycle.AbortLua(token);
                if (!lifecycle.IsReady && (wasReady || pendingLuaToken == token))
                {
                    pendingLuaToken = 0; pendingLua = pendingFriendly = null;
                    ResetCore();
                }
            });
        }

        private static void ResetCore()
        {
            engine.ResetContext();
            parsed = false;
            options = optionStories = switchStories = null;
            switches = positions = null;
            analysisFailure = null;
            eventSources.Clear();
            eventParser = null;
            boundaryFailures.Clear();
            partialOptions.Clear();
            partialEvents.Clear();
            PreviewDiagnostics.ResetContext();
            tickTime = 0;
            hasSourceClock = false;
            lua = friendly = null;
            if (owner != null) Call(owner, "ClearLogEntries");
            Call(Parser(), "ClearCaches");
            ((IDictionary)Read(Parser(), "OptionResult")).Clear();
            Call(Cache(), "ClearDisplayCaches");
        }

        // Compatibility entry for already-successful source in the portable
        // harness. Production Harmony hooks use StageLua + real compiler result.
        public static void NewLua(string text, string name)
        {
            long token = StageLua(text, name);
            CompleteLua(token, new object());
        }

        private static void NewLuaCore(string text, string name)
        {
            if ((int)Read(Patch(), "DisplayRefreshDepth") != 0) return;
            owner = Read(TypeFor("DiceMaster.DicePlugin"), "Instance");
            ResetCore();
            lua = text;
            friendly = name;
            Write(Patch(), "_lastLuaString", text);
            Write(Patch(), "_lastFriendlyName", name);
            // Preserve the original real-Lua boundary's event request semantics.
            Write(Patch(), "TargetEvent", null);
            AdvanceSourceClock((float)Read(TypeFor("UnityEngine.Time"), "unscaledTime"));
            if (Enabled()) Analyze();
        }

        private static void Analyze()
        {
            if (lua == null || string.IsNullOrEmpty(friendly)) return;
            Protect();
            try
            {
                object parser = Parser();
                Call(parser, "ClearCaches");
                ((IDictionary)Read(parser, "OptionResult")).Clear();
                AnalysisCount++;
                options = (IDictionary)Call(parser, "ExtractOptionActionsFromLua", friendly, lua);
                optionStories = (IDictionary)Call(parser, "ExtractOptionFirstStoryTextsFromLua", friendly, lua);
                switchStories = (IDictionary)Call(parser, "ExtractSwitchBranchFirstStoryTextsFromLua", friendly, lua);
                switches = (IList)Call(parser, "ExtractSwitchCheckFromLua", friendly, lua);
                positions = (IList)Call(parser, "ExtractPositionWorkFromLua", friendly, lua);
                parsed = true;
                analysisFailure = null;
                // Prepare script-only event descriptions at the fixed-analysis
                // boundary. No GetRate/GetResult/Check is used here. Late game
                // definitions are prepared once when their source becomes ready.
                PrepareEventDefinitions();
            }
            catch (Exception error)
            {
                parsed = false;
                options = null;
                analysisFailure = error;
                // Completed-parser markers and shared results are invalidated together.
                Call(Parser(), "ClearCaches");
                ((IDictionary)Read(Parser(), "OptionResult")).Clear();
            }
            finally { Unprotect(); }
        }

        private static void PrepareEventDefinitions()
        {
            Discover("이벤트");
            foreach (EventSource source in eventSources.Values)
                if (source.Entries != null)
                    foreach (object data in source.Entries)
                        if (data != null)
                        {
                            try { PrepareEventDefinition(data); }
                            catch (Exception) { /* The corresponding row owns its failure and retry. */ }
                        }
        }

        private static void PrepareEventDefinition(object data)
        {
            IDictionary fixedDescriptions = (IDictionary)Read(Cache(), "eventDescCache");
            if (!fixedDescriptions.Contains(data))
                fixedDescriptions[data] = Call(Cache(), "GenerateStaticDesc", data);
        }

        public static void Tick(object plugin, bool manual) { RunBoundary("tick", () => TickCore(plugin, manual)); }

        private static void TickCore(object plugin, bool manual)
        {
            if (refreshing || (int)Read(Patch(), "DisplayRefreshDepth") != 0) return;
            refreshing = true;
            owner = plugin;
            try
            {
                bool enabled = Enabled();
                bool visible = (bool)Read(plugin, "showLogWindow");
                string tab = (string)Read(plugin, "currentTab");
                if (!lifecycle.IsReady)
                {
                    if (enabled && visible) Publish(tab, new List<DisplayRow> {
                        new DisplayRow(RefreshEngine.DiscoveryRowId, null, RefreshEngine.WaitingStatus)
                    });
                    return;
                }
                float now = (float)Read(TypeFor("UnityEngine.Time"), "unscaledTime");
                AdvanceSourceClock(now);
                manualTick = manual;
                if (manual && enabled && visible)
                {
                    Call(Cache(), "ClearDisplayCaches");
                    eventSources.Clear();
                    eventParser = null;
                    parsed = false;
                    Analyze();
                }
                Protect();
                try { engine.Tick(tab, visible, enabled, now, false, manual, () => Discover(tab), rows => Publish(tab, rows), Log); }
                finally { Unprotect(); }
            }
            finally { refreshing = false; manualTick = false; }
        }

        private static List<DisplayQuery> Discover(string tab)
        {
            if (!parsed)
            {
                Analyze();
                if (analysisFailure != null) ExceptionDispatchInfo.Capture(analysisFailure).Throw();
                if (!parsed) throw new DataNotReadyException();
            }
            List<DisplayQuery> queries = new List<DisplayQuery>();
            if (tab == "선택지")
            {
                foreach (DictionaryEntry item in options)
                {
                    string key = (string)item.Key;
                    object actions = item.Value;
                    queries.Add(new DisplayQuery(key, () => {
                        object state = Call(Patch(), "EvaluateStoryReadState", optionStories, key);
                        Value value = new Value {
                            Key = (string)Call(owner, "ColorizeKeyByStoryReadState", key, state),
                            Text = (string)Call(Patch(), "ResolveOptionStoryText", key), StoryState = state
                        };
                        Call(TypeFor("DiceMaster.LuaParse.LuaParser"), "RefreshPreviewReads", actions);
                        try
                        {
                            value.Description = Translate((string)Call(TypeFor("DiceMaster.LuaParse.LuaParser"), "Description", actions));
                            partialOptions.Remove(key);
                            return value;
                        }
                        catch (PreviewReadException failure)
                        {
                            value.Description = Translate(failure.PartialDescription);
                            partialOptions[key] = value;
                            if (failure.IsWaiting) throw new DataNotReadyException();
                            throw new InvalidOperationException("Preview read boundary failed.");
                        }
                    }));
                }
            }
            else if (tab == "조건")
            {
                foreach (string entry in switches)
                {
                    string key = entry;
                    queries.Add(new DisplayQuery(key, () => {
                        if (Read(RequireManager("Mortal.Story.CheckPointManager"), "_switchList") == null) throw new DataNotReadyException();
                        string label = (string)Call(owner, "NormalizeCheckpointKey", key);
                        if (string.IsNullOrEmpty(label)) label = key;
                        string branches = (string)Call(Patch(), "BuildSwitchBranchStateText", switchStories, key);
                        object state = Call(Patch(), "EvaluateSwitchStoryReadState", switchStories, key);
                        string title = string.IsNullOrWhiteSpace(branches) ? (string)Call(owner, "ColorizeKeyByStoryReadState", label, state)
                            : (string)Call(TypeFor("DiceMaster.RichText"), "Golden", label) + " - " + branches;
                        return new Value { Key = title,
                            Description = Translate((string)Call(TypeFor("DiceMaster.DataHelper"), "ParseSwitchCheckpoint", key)),
                            Text = branches, StoryState = state };
                    }));
                }
            }
            else if (tab == "이벤트")
            {
                int sourceIndex = 0;
                foreach (string position in positions)
                {
                    EventSource source;
                    if (!eventSources.TryGetValue(position, out source))
                    {
                        source = new EventSource();
                        eventSources.Add(position, source);
                    }
                    if ((source.Entries == null && source.Error == null) || manualTick ||
                        (source.Error != null && tickTime >= source.RetryAt))
                    {
                        try
                        {
                            object manager = RequireManager("Mortal.Story.CheckPointManager");
                            object config = Read(manager, "_position");
                            IList definitions = (IList)Read(config, "List");
                            if (definitions == null) throw new DataNotReadyException();
                            foreach (object definition in definitions)
                                if (definition != null && (string)Read(definition, "name") == position && Read(definition, "List") == null)
                                    throw new DataNotReadyException();
                            source.Entries = (IList)Call(Cache(), "GetPositionEvents", position);
                            if (source.Entries == null) throw new DataNotReadyException();
                            source.Error = null;
                        }
                        catch (Exception error) { source.Error = error; source.RetryAt = tickTime + 5; }
                    }
                    // Each static source fails independently; a missing source
                    // cannot prevent other positions' event rows from updating.
                    Exception failure = source.Error;
                    // Keep the successful sentinel as a null value so a failed
                    // source has an observable recovery before its status clears.
                    queries.Add(new DisplayQuery("source/" + sourceIndex, () => {
                        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
                        return null;
                    }));
                    sourceIndex++;
                    if (source.Entries == null) continue;
                    foreach (object entry in source.Entries)
                    {
                        object data = entry;
                        if (data == null) continue;
                        string id = position + "/" + RuntimeHelpers.GetHashCode(data);
                        queries.Add(new DisplayQuery(id, () => {
                            Value value = new Value { Data = data,
                                Key = (string)Call(Cache(), "GetPositionEventName", data) };
                            Call(Cache(), "RetryDefinitionReads", data);
                            try
                            {
                                PrepareEventDefinition(data);
                                value.Description = Translate((string)Call(Cache(), "GetPositionEventDesc", data));
                                partialEvents.Remove(id);
                                return value;
                            }
                            catch (PreviewReadException failure)
                            {
                                value.Description = Translate(failure.PartialDescription);
                                partialEvents[id] = value;
                                if (failure.IsWaiting) throw new DataNotReadyException();
                                throw new InvalidOperationException("Preview event read boundary failed.");
                            }
                        }));
                    }
                }
            }
            else if (tab == "미션")
            {
                object manager = RequireManager("Mortal.Core.MissionManagerData");
                object subMissions = Read(manager, "_subMissions");
                IList missions = (IList)Read(subMissions, "List");
                if (missions == null) throw new DataNotReadyException();
                foreach (object entry in missions)
                {
                    object mission = entry;
                    if (mission == null) continue;
                    queries.Add(new DisplayQuery("mission/" + RuntimeHelpers.GetHashCode(mission), () => {
                        ValidateMissionStage(mission);
                        Type helper = TypeFor("DiceMaster.DataHelper");
                        Write(helper, "LiveMissionFilter", mission);
                        try
                        {
                            IDictionary result = (IDictionary)Call(helper, "GetMissions");
                            foreach (DictionaryEntry item in result)
                                return new Value { Data = item.Key, Key = (string)Read(item.Value, "Item1"), Description = Translate((string)Read(item.Value, "Item2")) };
                            return null; // A completed/out-of-period mission is a successful absence.
                        }
                        finally { Write(helper, "LiveMissionFilter", null); }
                    }));
                }
            }
            return queries;
        }

        private static void ValidateMissionStage(object mission)
        {
            string key = (string)Read(mission, "CurrentKey");
            if (key == "clear") return;
            if (string.IsNullOrWhiteSpace(key)) throw new DataNotReadyException();
            IList stages = Read(mission, "_missions") as IList;
            if (stages == null) throw new DataNotReadyException();
            foreach (object stage in stages)
                if (stage != null && (string)Read(stage, "Key") == key)
                {
                    if ((bool)Read(stage, "_active") && Read(stage, "_conditions") == null)
                        throw new InvalidOperationException("Null active mission conditions.");
                    return;
                }
            // The original GetByKey logs and returns null on an undefined key.
            // Catch that before the helper can mistake it for a normal absence.
            throw new InvalidOperationException("Unknown current mission stage.");
        }

        private static object RequireManager(string name)
        {
            object manager = Read(TypeFor(name), "Instance");
            if (manager == null) throw new DataNotReadyException();
            return manager;
        }

        private static void AdvanceSourceClock(float now)
        {
            if (float.IsNaN(now) || float.IsInfinity(now)) return;
            if (hasSourceClock && now > lastSourceClock) tickTime += (double)now - lastSourceClock;
            hasSourceClock = true;
            lastSourceClock = now;
        }

        private static string Translate(string text) { return (string)Call(TypeFor("DiceMaster.UiTextMapper"), "ApplyTranslations", text); }

        private static void Publish(string tab, List<DisplayRow> rows)
        {
            string field = tab == "선택지" ? "optionEntries" : tab == "조건" ? "switchEntries" : tab == "이벤트" ? "eventEntries" : "missionEntries";
            IList entries = (IList)Read(owner, field);
            IList staged = (IList)Activator.CreateInstance(entries.GetType());
            Type tuple = entries.GetType().GetGenericArguments()[0];
            foreach (DisplayRow row in rows)
            {
                Value value = row.Value as Value;
                if (value == null && tab == "선택지" && !string.IsNullOrEmpty(row.Status))
                    partialOptions.TryGetValue(row.Id, out value);
                if (value == null && tab == "이벤트" && !string.IsNullOrEmpty(row.Status))
                    partialEvents.TryGetValue(row.Id, out value);
                if (value == null && string.IsNullOrEmpty(row.Status)) continue;
                string key = value == null ? row.Id : value.Key;
                if (value == null && (row.Id == RefreshEngine.DiscoveryRowId || tab == "이벤트" || tab == "미션"))
                    key = tab + " 정보";
                string description = value == null ? "" : value.Description;
                if (!string.IsNullOrEmpty(row.Status)) description += "\n" + row.Status;
                if (tab == "선택지" || tab == "조건")
                {
                    object[] parts = tab == "선택지" ? new object[] { key, value == null ? "" : value.Text, description }
                        : new object[] { key, description };
                    staged.Add(Activator.CreateInstance(tuple, parts));
                }
                else staged.Add(Activator.CreateInstance(tuple, new object[] { value == null ? null : value.Data, key, description }));
            }
            // A publication failure leaves the last complete UI list intact.
            Write(owner, field, staged);
        }
    }
}
