using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using DiceMaster.LogUI;
using DiceMaster.EventSelection;

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
        public static readonly LogModel Model = new LogModel();
        private static long contextId;
        private static long nextIdentity;
        private static readonly Dictionary<object, long> identities = new Dictionary<object, long>(new ReferenceIdentity());
        private static readonly Dictionary<string, Dictionary<string, Value>> publishedValues = new Dictionary<string, Dictionary<string, Value>>();
        private static readonly HashSet<string> failedPublications = new HashSet<string>(StringComparer.Ordinal);
        public static int AnalysisCount;

        private sealed class ReservationSlot
        {
            public object Target;
            public long Revision;
            public bool Observed;
        }

        // Opaque authority captured when a physical confirmation begins. The
        // original intent is derived from this slot, never from release-time UI.
        private sealed class ReservationSnapshot
        {
            public readonly long ContextId, Revision;
            public readonly string Tab;
            public readonly object Target;
            public readonly CheckpointReservation Choice;
            public ReservationSnapshot(long contextId, string tab, object target, long revision, CheckpointReservation choice = null)
            { ContextId = contextId; Tab = tab; Target = target; Revision = revision; Choice = choice; }
        }

        private sealed class MissionEntryMutation
        {
            public readonly object Manager, Target, Previous;
            public readonly IDictionary Entries;
            public readonly string Key;
            public readonly bool HadPrevious;
            public MissionEntryMutation(object manager, IDictionary entries, string key, object target, bool hadPrevious, object previous)
            { Manager = manager; Entries = entries; Key = key; Target = target; HadPrevious = hadPrevious; Previous = previous; }
        }

        private static readonly ReservationSlot missionReservation = new ReservationSlot();
        private static readonly ReservationSlot conditionReservation = new ReservationSlot();
        private static readonly ReservationSlot eventReservation = new ReservationSlot();
        private static readonly Dictionary<object, Dictionary<int, CheckpointReservation>> conditionChoices =
            new Dictionary<object, Dictionary<int, CheckpointReservation>>(new ReferenceIdentity());
        private static MissionEntryMutation missionEntryMutation;
        private static object canceledMissionTarget;

        private sealed class EventSource
        {
            public IList Entries;
            public Exception Error;
            public double RetryAt;
        }

        private sealed class Value
        {
            public string Key, Description, Text, RawId, Position;
            public LogConditionDetails ConditionDetails;
            public List<CheckpointReservation> SwitchChoices;
            public EventEligibilitySnapshot EventEligibility;
            public CheckpointReservation EventChoice;
            public object Data, Source, StoryState;
            public readonly List<string> AnalysisStates = new List<string>();
        }

        private sealed class ReferenceIdentity : IEqualityComparer<object>
        {
            public new bool Equals(object left, object right) { return ReferenceEquals(left, right); }
            public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
        }

        private static long Identity(object value)
        {
            long id;
            if (!identities.TryGetValue(value, out id)) { id = checked(++nextIdentity); identities.Add(value, id); }
            return id;
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
                CheckpointReservationStore.Clear();
                conditionChoices.Clear();
                ResetCore();
                Write(Patch(), "_lastLuaString", null); Write(Patch(), "_lastFriendlyName", null);
                Write(Patch(), "TargetMission", null);
                NotifyReservationChanged("미션");
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
            // Optional only for portable historical fixtures; the full-source
            // production assembly always owns this runtime. Reset actual
            // execution authority on every game/Lua context replacement.
            Type selectionRuntime = typeof(LogBridge).Assembly.GetType("DiceMaster.EventSelection.EventSelectionRuntime", false);
            if (selectionRuntime != null) Call(selectionRuntime, "Invalidate", "게임 문맥 변경");
            Model.ResetContext(checked(++contextId));
            // A new game context owns its own active mission state. Never roll
            // a prior context's dictionary back while invalidating UI requests.
            missionEntryMutation = null;
            canceledMissionTarget = null;
            publishedValues.Clear();
            failedPublications.Clear();
            identities.Clear(); nextIdentity = 0;
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
                            RawId = key,
                            Key = (string)Call(owner, "ColorizeKeyByStoryReadState", key, state),
                            Text = (string)Call(Patch(), "ResolveOptionStoryText", key), StoryState = state
                        };
                        Call(TypeFor("DiceMaster.LuaParse.LuaParser"), "RefreshPreviewReads", actions);
                        try
                        {
                            value.Description = Translate((string)Call(TypeFor("DiceMaster.LuaParse.LuaParser"), "Description", actions));
                            CaptureAnalysis(value, actions);
                            partialOptions.Remove(key);
                            return value;
                        }
                        catch (PreviewReadException failure)
                        {
                            value.Description = Translate(failure.PartialDescription);
                            CaptureAnalysis(value, actions);
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
                        object manager = RequireManager("Mortal.Story.CheckPointManager");
                        if (Read(manager, "_switchList") == null) throw new DataNotReadyException();
                        string label = (string)Call(owner, "NormalizeCheckpointKey", key);
                        if (string.IsNullOrEmpty(label)) label = key;
                        string branches = (string)Call(Patch(), "BuildSwitchBranchStateText", switchStories, key);
                        object state = Call(Patch(), "EvaluateSwitchStoryReadState", switchStories, key);
                        string title = string.IsNullOrWhiteSpace(branches) ? (string)Call(owner, "ColorizeKeyByStoryReadState", label, state)
                            : (string)Call(TypeFor("DiceMaster.RichText"), "Golden", label) + " - " + branches;
                        // Capture each real branch once at the normal refresh
                        // boundary. Never infer groups from formatted lines or
                        // evaluate descriptions a second time for the model.
                        LogConditionDetails details = MapConditionDetails(
                            (LogConditionDetails)Call(TypeFor("DiceMaster.DataHelper"), "DescribeSwitchCheckpoint", key), Translate);
                        object config;
                        object pool = ResolveSwitch(manager, key, out config);
                        if (pool != null && (bool)Read(pool, "_useCount")) details = DescribeCountBranches(pool);
                        return new Value { Key = title, RawId = key, Data = pool,
                            Description = details == null ? "" : details.DescriptionFallback,
                            ConditionDetails = details,
                            SwitchChoices = pool == null ? null : BuildConditionChoices(manager, config, pool, key),
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
                        string id = position + "/" + Identity(data);
                        queries.Add(new DisplayQuery(id, () => {
                            Value value = new Value { Data = data, Position = position, RawId = (string)Read(data, "name"),
                                Key = (string)Call(Cache(), "GetPositionEventName", data),
                                EventEligibility = EventEligibility.Evaluate(data) };
                            value.EventChoice = EventReservation(value);
                            Call(Cache(), "RetryDefinitionReads", data);
                            try
                            {
                                PrepareEventDefinition(data);
                                value.Description = Translate((string)Call(Cache(), "GetPositionEventDesc", data));
                                CaptureEventAnalysis(value, data);
                                partialEvents.Remove(id);
                                return value;
                            }
                            catch (PreviewReadException failure)
                            {
                                value.Description = Translate(failure.PartialDescription);
                                CaptureEventAnalysis(value, data);
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
                    queries.Add(new DisplayQuery("mission/" + Identity(mission), () => {
                        ValidateMissionStage(mission);
                        Type helper = TypeFor("DiceMaster.DataHelper");
                        Write(helper, "LiveMissionFilter", mission);
                        try
                        {
                            IDictionary result = (IDictionary)Call(helper, "GetMissions");
                            foreach (DictionaryEntry item in result)
                                return new Value { Data = item.Key, Source = mission, RawId = (string)Read(mission, "name") + "/" + (string)Read(item.Key, "Key"), Key = (string)Read(item.Value, "Item1"), Description = Translate((string)Read(item.Value, "Item2")) };
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

        private static object ResolveSwitch(object manager, string name, out object config)
        {
            config = null;
            IEnumerable list = Read(manager, "_switchList") as IEnumerable;
            if (list == null) throw new DataNotReadyException();
            foreach (object entry in list)
            {
                if (entry == null) continue;
                object pool = Call(entry, "GetData", name);
                if (pool != null) { config = entry; return pool; }
            }
            return null;
        }

        private static LogConditionDetails DescribeCountBranches(object pool)
        {
            IList items = Read(pool, "_countItems") as IList;
            if (items == null) throw new DataNotReadyException();
            int next = unchecked((int)Read(pool, "_count") + 1);
            string[] operators = { "=", "≠", "<", "≤", ">", "≥" };
            var branches = new List<LogConditionBranch>();
            for (int index = 0; index < items.Count; index++)
            {
                int op = Convert.ToInt32(Read(items[index], "_compareOp"));
                if (op < 0 || op >= operators.Length) throw new InvalidOperationException("Unreadable Switch comparison.");
                string description = "이번 실행 횟수 " + next + " " + operators[op] + " " + Convert.ToInt32(Read(items[index], "_value"));
                branches.Add(new LogConditionBranch("분기 " + (index + 1),
                    index == 0 ? "이 조건을 만족하면" : "앞 분기가 모두 불충족일 때 검사", description));
            }
            branches.Add(new LogConditionBranch("그 외 (분기 " + (items.Count + 1) + ")", "위 분기가 모두 불충족일 때", ""));
            return new LogConditionDetails("실행 횟수를 위에서부터 검사하며, 처음 만족한 분기로 진행합니다.", branches);
        }

        private static List<CheckpointReservation> BuildConditionChoices(object manager, object config, object pool, string name)
        {
            bool useCount = (bool)Read(pool, "_useCount");
            int count = (int)Read(pool, "_count");
            IList items = Read(pool, useCount ? "_countItems" : "_items") as IList;
            if (items == null) return null;
            Dictionary<int, CheckpointReservation> cached;
            if (!conditionChoices.TryGetValue(pool, out cached))
                conditionChoices.Add(pool, cached = new Dictionary<int, CheckpointReservation>());
            var choices = new List<CheckpointReservation>();
            for (int result = 1; result <= items.Count + 1; result++)
            {
                object target = result <= items.Count ? items[result - 1] : pool;
                if (target == null) throw new InvalidOperationException("Missing Switch branch.");
                CheckpointReservation choice;
                if (!cached.TryGetValue(result, out choice) || !ReferenceEquals(choice.Manager, manager) ||
                    !ReferenceEquals(choice.Config, config) || !ReferenceEquals(choice.Target, target) ||
                    choice.Name != name || choice.UseCount != useCount || choice.Count != count)
                {
                    choice = new CheckpointReservation(true, manager, config, pool, target, name,
                        result.ToString(System.Globalization.CultureInfo.InvariantCulture), useCount: useCount, count: count);
                    cached[result] = choice;
                }
                choices.Add(choice);
            }
            return choices;
        }

        private static CheckpointReservation EventReservation(Value value)
        {
            object manager = RequireManager("Mortal.Story.CheckPointManager");
            object config = Read(manager, "_position");
            IList pools = Read(config, "List") as IList;
            if (pools == null) throw new DataNotReadyException();
            foreach (object pool in pools)
            {
                if (pool == null || (string)Read(pool, "name") != value.Position) continue;
                IList items = Read(pool, "List") as IList;
                if (items == null) return null;
                bool present = false;
                foreach (object item in items) if (ReferenceEquals(item, value.Data)) present = true;
                string prefix = Read(pool, "_scriptPrefix") as string;
                string result = Read(value.Data, "ResultType") as string;
                if (!present || prefix == null || string.IsNullOrEmpty(result)) return null;
                return new CheckpointReservation(false, manager, config, pool, value.Data, value.Position,
                    prefix + "_" + result, prefix, choice: value.Data);
            }
            return null;
        }

        private static bool ValidConditionChoice(CheckpointReservation choice)
        {
            if (choice == null || !choice.IsSwitch || !ReferenceEquals(RequireManager("Mortal.Story.CheckPointManager"), choice.Manager)) return false;
            object config;
            object pool = ResolveSwitch(choice.Manager, choice.Name, out config);
            if (!ReferenceEquals(pool, choice.Pool) || !ReferenceEquals(config, choice.Config) ||
                (bool)Read(pool, "_useCount") != choice.UseCount || (int)Read(pool, "_count") != choice.Count) return false;
            IList items = Read(pool, choice.UseCount ? "_countItems" : "_items") as IList;
            int result;
            if (items == null || !int.TryParse(choice.Result, out result) || result < 1 || result > items.Count + 1) return false;
            return ReferenceEquals(result <= items.Count ? items[result - 1] : pool, choice.Target);
        }

        private static bool SameChoice(CheckpointReservation left, CheckpointReservation right)
        {
            return left != null && right != null && left.IsSwitch == right.IsSwitch &&
                ReferenceEquals(left.Manager, right.Manager) && ReferenceEquals(left.Config, right.Config) &&
                ReferenceEquals(left.Pool, right.Pool) && ReferenceEquals(left.Target, right.Target) &&
                left.Name == right.Name && left.Result == right.Result && left.Prefix == right.Prefix &&
                left.UseCount == right.UseCount && left.Count == right.Count;
        }

        private static void AdvanceSourceClock(float now)
        {
            if (float.IsNaN(now) || float.IsInfinity(now)) return;
            if (hasSourceClock && now > lastSourceClock) tickTime += (double)now - lastSourceClock;
            hasSourceClock = true;
            lastSourceClock = now;
        }

        private static string Translate(string text) { return (string)Call(TypeFor("DiceMaster.UiTextMapper"), "ApplyTranslations", text); }
        private static string Localize(string text) { return (string)Call(TypeFor("DiceMaster.EmbeddedTranslations.Display"), "Translate", text); }

        private static LogConditionDetails MapConditionDetails(LogConditionDetails details, Func<string, string> transform)
        {
            if (details == null) return null;
            var branches = new List<LogConditionBranch>();
            foreach (LogConditionBranch branch in details.Branches)
                branches.Add(new LogConditionBranch(transform(branch.Title), transform(branch.Context), transform(branch.Description), branch.Id, branch.Target));
            return new LogConditionDetails(transform(details.Hint), branches, transform(details.DescriptionFallback));
        }

        private static void CaptureAnalysis(Value value, object actions)
        {
            value.AnalysisStates.Clear();
            foreach (object action in (IEnumerable)actions)
            {
                PreviewAnalysis analysis = Read(action, "Analysis") as PreviewAnalysis;
                if (analysis == null) continue;
                string state = analysis.State.ToString();
                if (!value.AnalysisStates.Contains(state)) value.AnalysisStates.Add(state);
            }
        }

        private static void CaptureEventAnalysis(Value value, object data)
        {
            // Read the fixed-analysis result prepared by GenerateStaticDesc.
            // Publishing/searching never extracts a script or evaluates a game value.
            if (eventParser == null) return;
            IDictionary prefixes = (IDictionary)Read(Cache(), "scriptPrefixCache");
            string prefix = prefixes.Contains(data) ? (string)prefixes[data] : (string)Read(data, "name");
            string key = prefix + "_" + Read(data, "ResultType");
            IDictionary descriptions = (IDictionary)Read(eventParser, "luaModification");
            if (descriptions.Contains(key)) CaptureAnalysis(value, descriptions[key]);
        }

        private static void Publish(string tab, List<DisplayRow> rows)
        {
            // If any conversion/publication boundary fails, the previous view
            // remains visible but its formerly healthy action is no longer current.
            failedPublications.Add(tab);
            try
            {
                string field = tab == "선택지" ? "optionEntries" : tab == "조건" ? "switchEntries" : tab == "이벤트" ? "eventEntries" : "missionEntries";
                IList entries = (IList)Read(owner, field);
                IList staged = (IList)Activator.CreateInstance(entries.GetType());
                Type tuple = entries.GetType().GetGenericArguments()[0];
                var modelRows = new List<LogItem>();
                var values = new Dictionary<string, Value>(StringComparer.Ordinal);
                bool discoveryFailed = rows.Exists(row => row.Id == RefreshEngine.DiscoveryRowId && !string.IsNullOrEmpty(row.Status));
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
                    var modelRow = new LogItem {
                        Id = row.Id, RawId = value == null ? row.Id : value.RawId ?? row.Id,
                        Title = Localize(Translate(key ?? "")), Text = value == null ? "" : Localize(Translate(value.Text ?? "")),
                        Description = Localize(description ?? ""), Target = value == null ? null : value.Data,
                        ConditionDetails = value == null ? null : MapConditionDetails(value.ConditionDetails, Localize),
                        EventEligibility = value == null ? null : value.EventEligibility,
                        RefreshStatus = row.Status ?? "", RefreshState = row.Status == RefreshEngine.WaitingStatus ? LogRefreshState.Waiting :
                            row.Status == RefreshEngine.RetainedFailureStatus ? LogRefreshState.RetainedFailure :
                            string.IsNullOrEmpty(row.Status) ? LogRefreshState.Healthy : LogRefreshState.ReadFailure,
                        HasLastGood = row.Value != null, ReservationAllowed =
                            (tab == "미션" || tab == "조건" && value != null && value.SwitchChoices != null || tab == "이벤트" && value != null && value.EventChoice != null &&
                                value.EventEligibility != null && value.EventEligibility.CanUseResult) && !discoveryFailed && lifecycle.IsReady,
                        // No mission value means a diagnostic row, not a
                        // mission whose description can be inspected/acted on.
                        IsStatusOnly = tab == "미션" && value == null
                    };
                    if (tab == "이벤트") modelRow.SearchDisplayTitle =
                        (string)Call(TypeFor("DiceMaster.EmbeddedTranslations.Display"), "TranslateLabel", modelRow.Title);
                    if (value != null)
                    {
                        string state = value.StoryState == null ? "" : value.StoryState.ToString();
                        modelRow.ReadState = state == "Read" ? LogReadState.Read : state == "Unread" ? LogReadState.Unread : LogReadState.Unknown;
                        modelRow.AnalysisStates.AddRange(value.AnalysisStates);
                    }
                    if (tab == "조건" && value != null && value.SwitchChoices != null && value.ConditionDetails != null &&
                        value.SwitchChoices.Count == value.ConditionDetails.Branches.Count)
                    {
                        var branches = new List<LogConditionBranch>();
                        for (int index = 0; index < value.SwitchChoices.Count; index++)
                        {
                            CheckpointReservation choice = value.SwitchChoices[index];
                            LogConditionBranch branch = modelRow.ConditionDetails.Branches[index];
                            branches.Add(new LogConditionBranch(branch.Title, branch.Context, branch.Description, choice.Result, choice));
                        }
                        modelRow.ConditionDetails = new LogConditionDetails(modelRow.ConditionDetails.Hint, branches,
                            modelRow.ConditionDetails.DescriptionFallback);
                    }
                    modelRows.Add(modelRow);
                    if (value != null) values[row.Id] = value;
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
                publishedValues[tab] = values;
                Model.Publish(new LogSnapshot(contextId, tab, modelRows));
                failedPublications.Remove(tab);
            }
            catch
            {
                Model.MarkPublicationFailure(tab);
                throw;
            }
        }

        // All activations, including mouse/keyboard, return through this boundary.
        // Publication identity and live membership are both required; no Lua,
        // display evaluation or execution method is called to validate a target.
        public static bool TryReserve(long expectedContext, string tab, string id, object target)
        { return ChangeReservation(expectedContext, tab, id, target, null, false); }

        private static ReservationSlot Slot(string tab)
        { return tab == "미션" ? missionReservation : tab == "조건" ? conditionReservation : tab == "이벤트" ? eventReservation : null; }

        private static string ReservationField(string tab)
        { return "TargetMission"; }

        private static object ReservationChoice(object reservation)
        { return reservation is CheckpointReservation checkpoint ? checkpoint.Choice : reservation; }

        private static object ReadReservation(string tab)
        { return tab == "미션" ? Read(Patch(), ReservationField(tab)) : CheckpointReservationStore.Current(tab == "조건"); }

        private static object ObserveReservation(string tab)
        {
            ReservationSlot slot = Slot(tab);
            object raw;
            try { raw = ReadReservation(tab); }
            catch
            {
                slot.Revision = checked(slot.Revision + 1);
                slot.Observed = false;
                if (tab == "미션") missionEntryMutation = null;
                throw;
            }
            if (!slot.Observed || !ReferenceEquals(slot.Target, raw))
            {
                slot.Revision = checked(slot.Revision + 1);
                slot.Target = raw; slot.Observed = true;
                if (tab == "미션" && missionEntryMutation != null && !ReferenceEquals(missionEntryMutation.Target, raw))
                    missionEntryMutation = null;
            }
            return raw;
        }

        // The actual one-use game postfixes notify even when a target is later
        // reinserted before UI polling. That ABA cycle must invalidate a press.
        // Notification cannot replace or escape an original game's result.
        public static void NotifyReservationChanged(string tab)
        {
            ReservationSlot slot = Slot(tab);
            if (slot == null) return;
            try
            {
                slot.Revision = checked(slot.Revision + 1);
                slot.Target = ReadReservation(tab);
                slot.Observed = true;
                if (tab == "미션" && missionEntryMutation != null && !ReferenceEquals(missionEntryMutation.Target, slot.Target))
                    missionEntryMutation = null;
            }
            catch
            {
                slot.Observed = false;
                if (tab == "미션") missionEntryMutation = null;
            }
        }

        private static CheckpointReservation PublishedConditionChoice(LogTabState state, Value value)
        {
            LogConditionBranch branch = state.SelectedBranch;
            CheckpointReservation choice = branch == null ? null : branch.Target as CheckpointReservation;
            if (choice == null || branch.Id != choice.Result || value.SwitchChoices == null ||
                !ReferenceEquals(value.Data, choice.Pool)) return null;
            foreach (CheckpointReservation published in value.SwitchChoices)
                if (ReferenceEquals(published, choice)) return choice;
            return null;
        }

        public static bool TryCaptureReservation(string tab, out object snapshot)
        {
            object captured = null;
            RunBoundary("reservation-capture", () => {
                ReservationSlot slot = Slot(tab);
                if (slot == null || !lifecycle.IsReady) return;
                object raw = ObserveReservation(tab);
                CheckpointReservation choice = null;
                if (tab != "미션")
                {
                    LogTabState state = Model.Tab(tab);
                    LogItem selected = state.Selected;
                    Dictionary<string, Value> values; Value value;
                    if (!state.CanReserveSelected || !publishedValues.TryGetValue(tab, out values) ||
                        !values.TryGetValue(selected.Id, out value)) return;
                    choice = tab == "조건" ? PublishedConditionChoice(state, value) : value.EventChoice;
                    if (choice == null) return;
                }
                captured = new ReservationSnapshot(contextId, tab, raw, slot.Revision, choice);
            });
            snapshot = captured;
            return captured != null;
        }

        public static bool TryToggleReservation(long expectedContext, string tab, string id, object target, object snapshot)
        {
            ReservationSnapshot captured = snapshot as ReservationSnapshot;
            if (captured == null) return false;
            return ChangeReservation(expectedContext, tab, id, target, captured, true);
        }

        // Position clicks retain their existing auto-selection except for the
        // exact mission explicitly canceled in this context. Describing that
        // candidate must not silently recreate the one-use override.
        public static bool ShouldAutoReserveMission(object target)
        { return target != null && !ReferenceEquals(canceledMissionTarget, target); }

        private static void RestoreOwnedMissionEntry(object manager)
        {
            MissionEntryMutation mutation = missionEntryMutation;
            missionEntryMutation = null;
            if (mutation == null || !ReferenceEquals(mutation.Manager, manager)) return;
            IDictionary current = Read(manager, "ActiveSubMissions") as IDictionary;
            // Undo only an untouched write made by this pending reservation.
            // A rebuilt game dictionary, replacement value or consumed check
            // belongs to the game and must never be removed or resurrected.
            if (!ReferenceEquals(current, mutation.Entries) || !current.Contains(mutation.Key) ||
                !ReferenceEquals(current[mutation.Key], mutation.Target)) return;
            if (mutation.HadPrevious) current[mutation.Key] = mutation.Previous;
            else current.Remove(mutation.Key);
        }

        private static void SetMissionEntry(object manager, string key, object target)
        {
            IDictionary active = Read(manager, "ActiveSubMissions") as IDictionary;
            if (active == null) return;
            if (missionEntryMutation != null && ReferenceEquals(missionEntryMutation.Manager, manager) &&
                ReferenceEquals(missionEntryMutation.Entries, active) && missionEntryMutation.Key == key &&
                ReferenceEquals(missionEntryMutation.Target, target) && active.Contains(key) && ReferenceEquals(active[key], target))
                return;
            RestoreOwnedMissionEntry(manager);
            bool existed = active.Contains(key);
            object previous = existed ? active[key] : null;
            if (existed && ReferenceEquals(previous, target)) return;
            active[key] = target;
            missionEntryMutation = new MissionEntryMutation(manager, active, key, target, existed, previous);
        }

        private static bool ChangeReservation(long expectedContext, string tab, string id, object target, ReservationSnapshot snapshot, bool toggle)
        {
            bool reserved = false;
            RunBoundary("reserve", () => {
                if (expectedContext != contextId || !lifecycle.IsReady || !Enabled() || owner == null || target == null ||
                    Slot(tab) == null || failedPublications.Contains(tab) || !(bool)Read(owner, "showLogWindow") || (string)Read(owner, "currentTab") != tab) return;
                Type unityObject = TypeFor("UnityEngine.Object");
                if (unityObject.IsInstanceOfType(target) && (bool)Call(unityObject, "op_Equality", target, null)) return;
                LogTabState state = Model.Tab(tab);
                LogItem selected = state.Selected;
                if (!state.CanReserveSelected || selected.Id != id || !ReferenceEquals(state.SelectedReservationTarget, target)) return;
                Dictionary<string, Value> values; Value value;
                if (!publishedValues.TryGetValue(tab, out values) || !values.TryGetValue(id, out value)) return;
                CheckpointReservation conditionChoice = tab == "조건" ? PublishedConditionChoice(state, value) : null;
                if (tab == "조건" ? conditionChoice == null : !ReferenceEquals(value.Data, target)) return;
                object currentReservation = ObserveReservation(tab);
                if (toggle && (snapshot.ContextId != expectedContext || snapshot.Tab != tab || snapshot.Revision != Slot(tab).Revision ||
                    !ReferenceEquals(snapshot.Target, currentReservation))) return;
                if (tab != "미션" && toggle && !SameChoice(snapshot.Choice,
                    tab == "조건" ? conditionChoice : value.EventChoice)) return;
                bool cancel = toggle && ReferenceEquals(ReservationChoice(snapshot.Target), target);
                if (tab == "미션")
                {
                    object manager = RequireManager("Mortal.Core.MissionManagerData");
                    bool present = false;
                    foreach (object mission in (IList)Read(Read(manager, "_subMissions"), "List"))
                        if (ReferenceEquals(mission, value.Source)) present = true;
                    if (!present || (string)Read(value.Source, "CurrentKey") != (string)Read(target, "Key")) return;
                    present = false;
                    foreach (object candidate in (IList)Read(value.Source, "_missions"))
                        if (ReferenceEquals(candidate, target)) present = true;
                    if (!present) return;
                    if (cancel) RestoreOwnedMissionEntry(manager);
                    else SetMissionEntry(manager, value.Key, target);
                    Write(Patch(), "TargetMission", cancel ? null : target);
                    canceledMissionTarget = cancel ? target : null;
                    NotifyReservationChanged(tab);
                    reserved = true;
                }
                else
                {
                    if (cancel)
                    {
                        if (!CheckpointReservationStore.Take(currentReservation as CheckpointReservation)) return;
                    }
                    else
                    {
                        CheckpointReservation choice;
                        if (tab == "조건")
                        {
                            choice = conditionChoice;
                            if (!ValidConditionChoice(choice)) return;
                        }
                        else
                        {
                            choice = value.EventChoice;
                            if (!SameChoice(choice, EventReservation(value))) return;
                        }
                        CheckpointReservationStore.Set(choice);
                    }
                    NotifyReservationChanged(tab);
                    reserved = true;
                }
            });
            return reserved;
        }

        public static bool IsReserved(string tab, object target)
        {
            if (target == null || Slot(tab) == null) return false;
            try { return ReferenceEquals(ReservationChoice(ObserveReservation(tab)), target); }
            catch (Exception) { return false; }
        }
    }
}
