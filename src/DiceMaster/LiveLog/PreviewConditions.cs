using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DiceMaster.LiveLog
{
    public enum PreviewConditionState { Unknown, Met, NotMet }
    public enum PreviewDefinitionKind { Conditions, Switches, Positions, TalkFlags }

    // Syntax only. No current game value is retained in a cached analysis.
    public sealed class PreviewCondition
    {
        internal readonly string Operation, Key;
        internal readonly object Literal;
        internal readonly PreviewCondition Left, Right;
        private readonly bool knownSyntax;
        internal PreviewCondition(string operation, object literal = null, string key = null,
            PreviewCondition left = null, PreviewCondition right = null)
        {
            Operation = operation; Literal = literal; Key = key; Left = left; Right = right;
            knownSyntax = operation != "unknown" && (left == null || left.IsKnownSyntax) && (right == null || right.IsKnownSyntax);
        }
        public bool IsKnownSyntax { get { return knownSyntax; } }
    }

    // Reads an existing checkpoint configuration and its current values. The
    // original Switch/Condition/Execute chain increments counters and may draw
    // Unity RNG; none of those methods is part of this display boundary.
    public static class PreviewConditions
    {
        private static readonly PreviewCondition unknown = new PreviewCondition("unknown");
        public static PreviewCondition Unknown { get { return unknown; } }
        public static PreviewCondition Constant(bool value) { return new PreviewCondition("literal", value); }
        public static PreviewCondition Not(PreviewCondition value)
        { return new PreviewCondition("not", left: value ?? Unknown); }
        public static PreviewCondition All(PreviewCondition left, PreviewCondition right)
        { return new PreviewCondition("and", left: left ?? Unknown, right: right ?? Unknown); }
        // Opt-in for a single next invocation in a closed entry segment. Plain
        // Switch previews retain their count protection; saved bindings are not
        // rewritten, and two calls cannot borrow the same current counter.
        internal static PreviewCondition ScopeNextSwitch(PreviewCondition condition, bool allowed, out bool readsSwitch)
        {
            int work = 0, count = SwitchCalls(condition, 0, ref work);
            readsSwitch = count != 0;
            return allowed && count == 1 ? ReplaceNextSwitch(condition) : condition;
        }
        private static int SwitchCalls(PreviewCondition node, int depth, ref int work)
        {
            if (node == null) return 0;
            if (depth > 64 || ++work > 512) return 2;
            if (node.Operation == "captured") return 0;
            if (node.Operation == "Switch") return 1;
            return Math.Min(2, SwitchCalls(node.Left, depth + 1, ref work) + SwitchCalls(node.Right, depth + 1, ref work));
        }
        private static PreviewCondition ReplaceNextSwitch(PreviewCondition node)
        {
            if (node == null || node.Operation == "captured") return node;
            if (node.Operation == "Switch") return new PreviewCondition("NextSwitch", key: node.Key);
            if (node.Left == null && node.Right == null) return node;
            return new PreviewCondition(node.Operation, node.Literal, node.Key, ReplaceNextSwitch(node.Left), ReplaceNextSwitch(node.Right));
        }

        private sealed class EffectSnapshot
        {
            internal readonly HashSet<string> Flags;
            internal readonly bool Opaque, TalkReset;
            internal EffectSnapshot(HashSet<string> flags, bool opaque, bool talkReset)
            { Flags = flags; Opaque = opaque; TalkReset = talkReset; }
        }

        private static WeakReference observedEnvironment, observedCheckpoints;
        // Installed by the visible information owner. Only route readers may
        // use loaded definition assets; strict judgments/proofs stay unchanged.
        internal static Func<PreviewDefinitionKind, object[]> LoadedDefinitionReader = null;
        internal static Func<object, bool> ExistingOwnerAlive = null;
        internal static object[] LoadedDefinitions(PreviewDefinitionKind kind)
        {
            var values = LoadedDefinitionReader == null ? null : LoadedDefinitionReader(kind);
            if (ReadStamp.IsRecording) ReadStamp.ObserveLoaded(kind, values);
            if (values == null || values.Length > 64) return null;
            return values;
        }
        internal static bool OwnerAlive(object value)
        {
            bool alive = value != null && (ExistingOwnerAlive == null || ExistingOwnerAlive(value));
            if (ReadStamp.IsRecording) ReadStamp.ObserveAlive(value, alive);
            return alive;
        }
        private static object LoadedTalkCollection(out string source)
        {
            source = "로드된 대화 초기화 설정";
            var values = LoadedDefinitions(PreviewDefinitionKind.TalkFlags);
            if (values == null || values.Length == 0) return null;
            object result = values[0]; Snapshot.Exact(result, "Mortal.Core.FlagCollectionData");
            if (result is DisplayDefinition) source = "설치 파일 정의와 현재 게임 플래그 연결";
            foreach (object value in values) if (!ReferenceEquals(value, result)) return null;
            return result;
        }

        public static void ClearTalkContext()
        { observedEnvironment = observedCheckpoints = null; }

        // Observe only an environment the game already loaded. Read the
        // interpreter field and MoonSharp's exact raw table lookup, never a Lua
        // expression, index metamethod, lazy singleton or environment initializer.
        public static void ObserveLuaEnvironment(object environment)
        {
            ClearTalkContext();
            try
            {
                object checkpoints = Snapshot.ExistingManager();
                if (checkpoints == null) return;
                object collection = TalkCollection(environment, checkpoints);
                if (collection == null) return;
                observedEnvironment = new WeakReference(environment);
                observedCheckpoints = new WeakReference(checkpoints);
            }
            catch (Exception) { }
        }
        private static object TalkCollection(object environment, object checkpoints = null)
        {
            if (environment == null) return null;
            Type owner = environment.GetType();
            while (owner != null && owner.FullName != "Fungus.LuaEnvironment") owner = owner.BaseType;
            if (owner == null) return null;
            FieldInfo interpreter = owner.GetField("interpreter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (interpreter == null) return null;
            object script = interpreter.GetValue(environment);
            if (ReadStamp.IsRecording) ReadStamp.ObserveField(environment, interpreter, script);
            Snapshot.Exact(script, "MoonSharp.Interpreter.Script");
            object globals = Snapshot.Field(script, "m_GlobalTable");
            Snapshot.Exact(globals, "MoonSharp.Interpreter.Table");
            MethodInfo raw = globals.GetType().GetMethod("RawGet", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
                null, new[] { typeof(string) }, null);
            if (raw == null || raw.IsVirtual || raw.ReturnType.FullName != "MoonSharp.Interpreter.DynValue") return null;
            if (checkpoints != null)
            {
                object bound = RawObject(globals, raw, "checkpointmanager");
                Snapshot.Exact(bound, "Mortal.Story.CheckPointManager");
                if (!ReferenceEquals(bound, checkpoints)) throw new InvalidOperationException("Different Lua checkpoint context.");
            }
            object manager = RawObject(globals, raw, "statmodifymanager");
            Snapshot.Exact(manager, "Mortal.Story.StatModifyManager");
            object collection = Snapshot.Field(manager, "_talkOptionFlag");
            Snapshot.Exact(collection, "Mortal.Core.FlagCollectionData");
            return collection;
        }
        private static object RawObject(object globals, MethodInfo raw, string key)
        {
            object value = raw.Invoke(globals, new object[] { key });
            if (ReadStamp.IsRecording) ReadStamp.ObserveRaw(globals, raw, key, value);
            Snapshot.Exact(value, "MoonSharp.Interpreter.DynValue");
            object data = Snapshot.Field(value, "m_Object");
            Snapshot.Exact(data, "MoonSharp.Interpreter.UserData");
            return Snapshot.Field(data, "<Object>k__BackingField");
        }

        // Prefer the game's CURRENT serialized owner, not whichever Lua file
        // happened to compile last. No Instance getter, initialization or Lua
        // execution is used. Absent owners may use bounded loaded metadata;
        // broken current owners never borrow an older observed environment.
        private static object CurrentTalkCollection(object checkpoints, out string source)
        {
            source = "현재 게임 Lua 연결";
            if (checkpoints == null) return LoadedTalkCollection(out source);
            Snapshot.Exact(checkpoints, "Mortal.Story.CheckPointManager");
            Type type = checkpoints.GetType().Assembly.GetType("Mortal.Story.LuaManager", false);
            if (type != null)
            {
                object owner = Snapshot.Field(type, "<Instance>k__BackingField");
                if (OwnerAlive(owner))
                {
                    Snapshot.Exact(owner, "Mortal.Story.LuaManager");
                    return TalkCollection(Snapshot.Field(owner, "_luaEnvironment"), checkpoints);
                }
            }
            source = "동일 문맥에서 관찰한 Lua 연결";
            if (ReadStamp.IsRecording) ReadStamp.ObserveTalkContext();
            if (observedCheckpoints == null) return LoadedTalkCollection(out source);
            if (!ReferenceEquals(observedCheckpoints.Target, checkpoints)) return null;
            return TalkCollection(observedEnvironment == null ? null : observedEnvironment.Target, checkpoints);
        }

        // Capture definition-time effects, never sampled game values. A bound
        // checkpoint keeps the effects preceding its own read, so a later flag
        // write cannot retroactively change that already captured Lua value.
        public static PreviewCondition CaptureCurrent(PreviewCondition condition, IEnumerable<string> flagWrites, bool opaque)
        { return CaptureEffects(condition, flagWrites, opaque, false); }
        public static PreviewCondition CaptureAfterTalkReset(PreviewCondition condition, IEnumerable<string> flagWrites, bool opaque)
        { return CaptureEffects(condition, flagWrites, opaque, true); }
        // Dependency proof: writes must not consume the selected topic's own
        // gate. The two-argument form reads definitions only. The typed literal
        // SetFlag overload may read the matching concrete FlagData.State to
        // prove an assignment leaves it unchanged; no FinalValue/judgment runs.
        public static bool AreFlagWritesDisjoint(PreviewCondition condition, IEnumerable<string> flagWrites)
        { return CreateReader().AreFlagWritesDisjoint(condition, flagWrites); }
        public static bool AreFlagWritesDisjoint(PreviewCondition condition, IEnumerable<string> flagWrites, IDictionary<string, int> sameValueSetCandidates)
        { return CreateReader().AreFlagWritesDisjoint(condition, flagWrites, sameValueSetCandidates); }
        public static bool AreFlagWritesDisjoint(PreviewCondition condition, IEnumerable<string> flagWrites, IDictionary<string, int> sameValueSetCandidates, bool afterTalkReset)
        { return CreateReader().AreFlagWritesDisjoint(condition, flagWrites, sameValueSetCandidates, afterTalkReset); }
        private static HashSet<string> FlagWriteSet(IEnumerable<string> flagWrites)
        {
            var flags = new HashSet<string>(StringComparer.Ordinal);
            if (flagWrites != null) foreach (string flag in flagWrites)
            { if (flags.Count >= 32 || flag == null || flag.Length > 256) throw new InvalidOperationException("Preview flag write limit."); flags.Add(flag); }
            return flags;
        }
        private static PreviewCondition CaptureEffects(PreviewCondition condition, IEnumerable<string> flagWrites, bool opaque, bool talkReset)
        {
            var flags = new HashSet<string>(StringComparer.Ordinal);
            if (flagWrites != null) foreach (string flag in flagWrites)
            { if (flags.Count >= 32 || flag == null || flag.Length > 256) { opaque = true; break; } flags.Add(flag); }
            int work = 0;
            return Capture(condition, new EffectSnapshot(flags, opaque, talkReset), 0, ref work);
        }
        private static PreviewCondition Capture(PreviewCondition node, EffectSnapshot effects, int depth, ref int work)
        {
            if (node == null || depth > 64 || ++work > 512) return Unknown;
            if (node.Operation == "captured") return node;
            if (node.Operation == "Condition" || node.Operation == "Switch" || node.Operation == "NextSwitch") return new PreviewCondition("captured", effects, left: node);
            if (node.Left == null && node.Right == null) return node;
            PreviewCondition left = node.Left == null ? null : Capture(node.Left, effects, depth + 1, ref work);
            PreviewCondition right = node.Right == null ? null : Capture(node.Right, effects, depth + 1, ref work);
            return new PreviewCondition(node.Operation, node.Literal, node.Key, left, right);
        }

        public static PreviewCondition Parse(string expression, IDictionary<string, PreviewCondition> bindings = null)
        {
            if (expression == null || expression.Length > 4096) return Unknown;
            List<PreviewToken> tokens = PreviewSyntax.Tokenize(expression);
            return ParseTokens(tokens, 0, tokens.Count, bindings);
        }

        public static PreviewCondition ParseTokens(List<PreviewToken> tokens, int start, int end,
            IDictionary<string, PreviewCondition> bindings = null)
        {
            if (tokens == null || start < 0 || end > tokens.Count || start >= end || end - start > 256) return Unknown;
            if (tokens[end - 1].End - tokens[start].Start > 4096) return Unknown;
            var parser = new Parser(tokens, start, end, bindings);
            PreviewCondition value = parser.Or(0);
            return parser.Position == end && !parser.Failed ? value : Unknown;
        }

        private sealed class Parser
        {
            private readonly List<PreviewToken> tokens;
            private readonly int end;
            private readonly IDictionary<string, PreviewCondition> bindings;
            internal int Position;
            internal bool Failed;
            internal Parser(List<PreviewToken> tokens, int start, int end, IDictionary<string, PreviewCondition> bindings)
            { this.tokens = tokens; Position = start; this.end = end; this.bindings = bindings; }
            private bool At(string value) { return Position < end && !tokens[Position].IsString && tokens[Position].Text == value; }
            private PreviewCondition Binary(string operation, PreviewCondition left, PreviewCondition right)
            { return new PreviewCondition(operation, left: left, right: right); }
            internal PreviewCondition Or(int depth)
            {
                PreviewCondition value = And(depth + 1);
                while (At("or")) { Position++; value = Binary("or", value, And(depth + 1)); }
                return value;
            }
            private PreviewCondition And(int depth)
            {
                PreviewCondition value = Compare(depth + 1);
                while (At("and")) { Position++; value = Binary("and", value, Compare(depth + 1)); }
                return value;
            }
            private PreviewCondition Compare(int depth)
            {
                PreviewCondition value = Unary(depth + 1);
                foreach (string operation in new[] { "==", "~=", "<", "<=", ">", ">=" })
                    if (At(operation)) { Position++; return Binary(operation, value, Unary(depth + 1)); }
                return value;
            }
            private PreviewCondition Unary(int depth)
            {
                if (depth > 64 || Position >= end) { Failed = true; return Unknown; }
                if (At("not")) { Position++; return Not(Unary(depth + 1)); }
                if (At("-"))
                {
                    Position++;
                    if (Position < end && !tokens[Position].IsString && int.TryParse("-" + tokens[Position].Text,
                        NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int negative))
                    { Position++; return new PreviewCondition("literal", negative); }
                    Failed = true; return Unknown;
                }
                if (At("("))
                {
                    Position++; PreviewCondition value = Or(depth + 1);
                    if (!At(")")) { Failed = true; return Unknown; }
                    Position++; return value;
                }
                PreviewToken token = tokens[Position++];
                if (token.IsString) { Failed = true; return Unknown; }
                if (token.Text == "true" || token.Text == "false") return Constant(token.Text == "true");
                if (int.TryParse(token.Text, NumberStyles.None, CultureInfo.InvariantCulture, out int number))
                    return new PreviewCondition("literal", number);
                if (token.Text == "checkpointmanager" && (At(".") || At(":")))
                {
                    Position++;
                    if (Position + 3 >= end || tokens[Position].IsString) { Failed = true; return Unknown; }
                    string method = tokens[Position++].Text;
                    if ((method != "Switch" && method != "Condition") || !At("(")) { Failed = true; return Unknown; }
                    Position++;
                    if (!tokens[Position].IsString || !PreviewSyntax.TryString(tokens[Position++].Text, out string key) ||
                        key.Length == 0 || key.Length > 256 || !At(")")) { Failed = true; return Unknown; }
                    Position++;
                    return new PreviewCondition(method, key: key);
                }
                PreviewCondition bound;
                return token.IsIdentifier && bindings != null && bindings.TryGetValue(token.Text, out bound) && bound != null ? bound : Unknown;
            }
        }

        public static PreviewConditionState Current(PreviewCondition condition)
        { return CreateReader().Current(condition); }

        // One rendering gets one bounded snapshot session. It reuses each
        // checkpoint read across alternatives and is never stored in a cache.
        public sealed class Reader
        {
            private readonly Snapshot snapshot;
            internal Reader(bool projectTalkResets = false, DefinitionScope definitions = null) { snapshot = new Snapshot(false, projectTalkResets, definitions); }
            // Selected-place guidance only. Interpret serialized entry checks
            // into this read's private values; never execute a game evaluator.
            internal bool ProjectPositionEntry(object data) { snapshot.ProjectPositionEntry(data); return snapshot.EntryBoundaryKnown; }
            internal bool HasUnresolvedEntrySwitch(PreviewCondition condition)
            { try { return snapshot.UnresolvedEntrySwitch(condition, 0); } catch (Exception) { return true; } }
            public bool AreFlagWritesDisjoint(PreviewCondition condition, IEnumerable<string> flagWrites)
            { return AreFlagWritesDisjoint(condition, flagWrites, null); }
            public bool AreFlagWritesDisjoint(PreviewCondition condition, IEnumerable<string> flagWrites, IDictionary<string, int> sameValueSetCandidates)
            { return AreFlagWritesDisjoint(condition, flagWrites, sameValueSetCandidates, false); }
            public bool AreFlagWritesDisjoint(PreviewCondition condition, IEnumerable<string> flagWrites, IDictionary<string, int> sameValueSetCandidates, bool afterTalkReset)
            { return ReadFlagWriteIndependence(condition, flagWrites, sameValueSetCandidates, afterTalkReset) == true; }
            // true: proven independent; false: a known affected input; null:
            // missing/unsupported evidence. A failed proof is not a new story.
            internal bool? ReadFlagWriteIndependence(PreviewCondition condition, IEnumerable<string> flagWrites, IDictionary<string, int> sameValueSetCandidates, bool afterTalkReset)
            {
                try
                {
                    HashSet<string> flags = FlagWriteSet(flagWrites);
                    if (flags.Count == 0) return true;
                    var sets = new Dictionary<string, int>(StringComparer.Ordinal);
                    if (sameValueSetCandidates != null)
                    {
                        int count = 0;
                        foreach (var pair in sameValueSetCandidates)
                        {
                            if (++count > 32 || pair.Key == null || pair.Key.Length > 256) return null;
                            if (flags.Contains(pair.Key)) sets.Add(pair.Key, pair.Value);
                        }
                    }
                    return snapshot.FlagWritesDisjoint(condition, flags, sets, afterTalkReset);
                }
                catch (Exception) { return null; }
            }
            public PreviewConditionState Current(PreviewCondition condition)
            {
                try
                {
                    object value = snapshot.Value(condition, 0);
                    bool? truth = Snapshot.Truth(value);
                    return !truth.HasValue ? PreviewConditionState.Unknown : truth.Value ? PreviewConditionState.Met : PreviewConditionState.NotMet;
                }
                catch (Exception) { return PreviewConditionState.Unknown; }
            }
        }
        public static Reader CreateReader() { return new Reader(); }
        public static Reader CreateRouteReader() { return new Reader(true); }

        // Explicit log detail only. The sample is bounded and discarded after
        // formatting; no current values enter cached syntax or automatic logs.
        public static string DescribeCurrent(PreviewCondition condition)
        { return DescribeSample(condition, false); }
        public static string DescribeRouteCurrent(PreviewCondition condition)
        { return "후속 경로 가상 조회: 확인된 대화 초기화 기본값만 반영 · 게임 상태 유지\n" + DescribeSample(condition, true); }
        public static string DescribeTalkResetSource()
        {
            var sample = new Snapshot(true, true);
            sample.ReadTalkFootprint();
            return sample.TalkSourceDiagnostic;
        }
        public static string DescribePositionEntry(PreviewCondition condition, object data)
        {
            var sample = new Snapshot(true, true); bool? result = null;
            sample.ProjectPositionEntry(data);
            try { if (sample.EntryBoundaryKnown) result = Snapshot.Truth(sample.Value(condition, 0)); }
            catch (Exception) { sample.Trace("읽기 제한/미지원 자료 · 판정 미확정"); }
            return "진입 후 가상 조건 진단: " + (!result.HasValue ? "미확정" : result.Value ? "충족" : "미충족") +
                " · 게임 상태 유지\n" + sample.Diagnostics;
        }
        private static string DescribeSample(PreviewCondition condition, bool projectTalkResets)
        {
            var sample = new Snapshot(true, projectTalkResets); bool? result = null;
            try { result = Snapshot.Truth(sample.Value(condition, 0)); }
            catch (Exception) { sample.Trace("읽기 제한/미지원 자료 · 판정 미확정"); }
            return "현재 조건 진단: " + (!result.HasValue ? "미확정" : result.Value ? "충족" : "미충족") +
                "\n" + sample.Diagnostics;
        }

        private sealed class Identity : IEqualityComparer<object>
        {
            public new bool Equals(object left, object right) { return ReferenceEquals(left, right); }
            public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
        }

        // Read-only alternatives, not a sampled switch result. Earlier unknown
        // items may win; an earlier known-true item excludes all later indices.
        private sealed class SwitchChoices
        {
            internal readonly HashSet<int> Values;
            internal readonly int Minimum, Maximum;
            internal SwitchChoices(List<int> values)
            { Values = new HashSet<int>(values); Minimum = values[0]; Maximum = values[values.Count - 1]; }
        }

        // Owned by one scheduled map publication only. This shares names and
        // configuration object references, NEVER flags, truth or virtual entry
        // values between places. The next scheduled read starts a new scope.
        internal sealed class DefinitionScope
        {
            internal readonly Dictionary<object, DefinitionIndex> Indices = new Dictionary<object, DefinitionIndex>(new Identity());
            private readonly Stack<DefinitionIndex> empty = new Stack<DefinitionIndex>(4);
            private void Recycle(DefinitionIndex index)
            {
                index.List = null; index.Count = index.Cursor = 0; index.Values.Clear();
                if (empty.Count < 4) empty.Push(index);
            }
            internal void Reset()
            { foreach (var index in Indices.Values) Recycle(index); Indices.Clear(); }
            internal DefinitionIndex Index(object config, IList list)
            {
                DefinitionIndex index;
                if (Indices.TryGetValue(config, out index))
                {
                    if (ReferenceEquals(index.List, list) && index.Count == list.Count) return index;
                    Recycle(index);
                }
                index = empty.Count == 0 ? new DefinitionIndex() : empty.Pop();
                index.List = list; index.Count = list.Count; Indices[config] = index;
                return index;
            }
        }
        internal sealed class DefinitionIndex
        {
            internal object List;
            internal int Count, Cursor;
            internal readonly Dictionary<string, object> Values = new Dictionary<string, object>(StringComparer.Ordinal);
        }
        // A route witness may reuse its decision only while every observed
        // input/reference and collection revision still matches. No game
        // evaluator, global flag scan or setter is used by this read plan.
        internal sealed class ReadStamp : IDisposable
        {
            [ThreadStatic] private static ReadStamp recording;
            private readonly ReadStamp previous;
            private readonly Dictionary<object, Dictionary<string, Observation>> reads =
                new Dictionary<object, Dictionary<string, Observation>>(new Identity());
            private int count;
            private bool reusable = true;
            internal static bool IsRecording { get { return recording != null; } }
            internal ReadStamp() { previous = recording; recording = this; }
            public void Dispose() { recording = previous; }
            private sealed class Observation
            {
                internal Func<object> Read;
                internal object Value;
                internal FieldInfo VersionField;
                internal object Version;
                internal object[] Cells;
                internal bool Matches(object value)
                {
                    if (!Same(Value, value)) return false;
                    if (VersionField != null && !Equals(Version, VersionField.GetValue(value))) return false;
                    if (Cells != null)
                    {
                        var list = value as IList;
                        if (list == null || list.Count != Cells.Length) return false;
                        for (int at = 0; at < Cells.Length; at++) if (!Same(Cells[at], list[at])) return false;
                    }
                    return true;
                }
            }
            private static bool Same(object left, object right)
            {
                if (ReferenceEquals(left, right)) return true;
                if (left == null || right == null || left.GetType() != right.GetType()) return false;
                return left is string || left.GetType().IsPrimitive || left.GetType().IsEnum ? left.Equals(right) : false;
            }
            internal static void Observe(string key, Func<object> read, object value)
            { Observe(typeof(ReadStamp), key, read, value); }
            internal static void ObserveLoaded(PreviewDefinitionKind kind, object[] values)
            { Observe("loaded/" + kind, () => LoadedDefinitionReader == null ? null : LoadedDefinitionReader(kind), values); }
            internal static void ObserveAlive(object value, bool alive)
            { Observe(value, "alive", () => value != null && (ExistingOwnerAlive == null || ExistingOwnerAlive(value)), alive); }
            internal static void ObserveRaw(object owner, MethodInfo raw, string key, object value)
            { Observe(owner, key, () => raw.Invoke(owner, new object[] { key }), value); }
            internal static void ObserveField(object target, FieldInfo field, object result)
            { Observe(target, field.Name, () => field.GetValue(target is Type ? null : target), result); }
            internal static void ObserveTalkContext()
            {
                Observe("observed/environment", () => observedEnvironment, observedEnvironment);
                Observe("observed/checkpoints", () => observedCheckpoints, observedCheckpoints);
            }
            internal static void Observe(object owner, string key, Func<object> read, object value)
            {
                var stamp = recording;
                if (stamp == null || !stamp.reusable) return;
                Dictionary<string, Observation> fields;
                owner = owner ?? typeof(ReadStamp);
                if (!stamp.reads.TryGetValue(owner, out fields)) stamp.reads.Add(owner, fields = new Dictionary<string, Observation>(StringComparer.Ordinal));
                Observation old;
                if (fields.TryGetValue(key, out old))
                { if (!old.Matches(value)) stamp.reusable = false; return; }
                if (++stamp.count > 1024) { stamp.reusable = false; return; }
                var item = new Observation { Read = read, Value = value };
                var list = value as IList;
                if (list != null)
                {
                    Type type = value.GetType();
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        item.VersionField = type.GetField("_version", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (item.VersionField == null) { stamp.reusable = false; return; }
                        item.Version = item.VersionField.GetValue(value);
                    }
                    else if (type.IsArray && type.GetArrayRank() == 1 && list.Count <= 256)
                    { item.Cells = new object[list.Count]; list.CopyTo(item.Cells, 0); }
                    else { stamp.reusable = false; return; }
                }
                fields.Add(key, item);
            }
            internal bool IsCurrent()
            {
                if (!reusable) return false;
                ReadStamp outer = recording; recording = null;
                try
                {
                    foreach (var fields in reads.Values) foreach (var item in fields.Values)
                        if (!item.Matches(item.Read())) return false;
                    return true;
                }
                catch (Exception) { return false; }
                finally { recording = outer; }
            }
        }

        private sealed class Snapshot
        {
            private readonly List<string> diagnostics;
            private bool diagnosticsLimited;
            private readonly bool projectTalkResets;
            internal Snapshot(bool explain = false, bool projectTalkResets = false, DefinitionScope scope = null)
            {
                this.projectTalkResets = projectTalkResets; if (explain) diagnostics = new List<string>();
                if (projectTalkResets)
                {
                    routeValues = new Dictionary<PreviewCondition, object>();
                    resetDefaults = new Dictionary<object, int>(new Identity());
                    definitions = scope ?? new DefinitionScope();
                }
            }
            internal void Trace(string text)
            {
                if (diagnostics == null) return;
                if (diagnostics.Count >= 64) { diagnosticsLimited = true; return; }
                text = text ?? "";
                if (text.Length > 160) text = text.Substring(0, 160) + "…";
                diagnostics.Add(text.Replace('\r', ' ').Replace('\n', ' '));
            }
            internal string Diagnostics { get { return string.Join("\n", diagnostics ?? new List<string>()) + (diagnosticsLimited ? "\n진단 표시 상한 · 일부 생략" : ""); } }
            // Known truth does not imply a known operand value: Lua's and/or
            // return operands. Keep these distinct for surrounding comparisons.
            private static readonly object truthy = new object(), falsey = new object();
            private int work;
            private object manager;
            private bool managerRead;
            private readonly Dictionary<string, object> checkpoints = new Dictionary<string, object>(StringComparer.Ordinal);
            private readonly HashSet<object> active = new HashSet<object>(new Identity());
            private readonly Dictionary<PreviewCondition, object> captured = new Dictionary<PreviewCondition, object>();
            private readonly Dictionary<PreviewCondition, object> routeValues;
            private HashSet<string> maskedFlags;
            private IDictionary<string, int> sameValueSets;
            private bool setsAfterTalkReset;
            private HashSet<object> maskedTalkFlags, talkFlags;
            private bool maskAllFlagInputs, talkFootprintRead, talkFootprintKnown;
            private Dictionary<object, int?> entryValues;
            private bool entryUnknown, enteringPlace, projectingEntry, entryReadFailed;
            private int entryReadLimit;
            internal bool EntryBoundaryKnown { get { return !entryUnknown; } }
            private readonly Dictionary<object, int> resetDefaults;
            private readonly DefinitionScope definitions;
            internal void ProjectPositionEntry(object data)
            {
                entryUnknown = true; enteringPlace = false; entryValues = null; entryReadFailed = false;
                entryReadLimit = Math.Min(16384, work + 2048);
                try
                {
                    Exact(data, "Mortal.Free.FreePositionData");
                    Array groups = Field(data, "_conditionFlags") as Array;
                    if (groups == null || groups.Rank != 1 || groups.Length > 32 ||
                        groups.GetType().GetElementType().FullName != "Mortal.Free.PositionConditionFlagItem") return;
                    var flags = new HashSet<object>(new Identity()); int count = 0;
                    foreach (object group in groups)
                    {
                        Step(); Exact(group, "Mortal.Free.PositionConditionFlagItem");
                        Exact(Field(group, "_condition"), "Mortal.Core.ConditionResultItem");
                        Array items = Field(group, "_flagItem") as Array;
                        if (items == null || items.Rank != 1 || items.Length > 32 ||
                            items.GetType().GetElementType().FullName != "Mortal.Free.PositionFlagItem") return;
                        foreach (object item in items)
                        {
                            if (++count > 32) return;
                            Step(); Exact(item, "Mortal.Free.PositionFlagItem");
                            object flag = Field(item, "_flagData"); Exact(flag, "Mortal.Core.FlagData"); flags.Add(flag);
                            if (!(Field(item, "_trueState") is int) || !(Field(item, "_falseState") is int)) return;
                        }
                    }
                    // Validate the complete write footprint before reading a
                    // condition. A malformed suffix cannot leave a trusted prefix.
                    if (flags.Count == 0) { entryUnknown = false; return; }
                    entryValues = new Dictionary<object, int?>(new Identity());
                    projectingEntry = true;
                    int ordinal = 0;
                    foreach (object group in groups)
                    {
                        Step();
                        // Native Setup checks once, then assigns all members.
                        // Each later group observes earlier private assignments.
                        bool? state = ReadCondition(Field(group, "_condition"), 0);
                        if (entryReadFailed) throw new InvalidOperationException("Unreadable position entry predicate.");
                        if (work > entryReadLimit) throw new InvalidOperationException("Position entry read limit.");
                        if (diagnostics != null) Trace("진입 조건 " + (++ordinal) + ": " +
                            (!state.HasValue ? "미확정" : state.Value ? "충족" : "미충족"));
                        foreach (object item in (Array)Field(group, "_flagItem"))
                        {
                            Step(); object flag = Field(item, "_flagData");
                            int yes = (int)Field(item, "_trueState"), no = (int)Field(item, "_falseState");
                            int? value = state.HasValue ? (state.Value ? yes : no) : yes == no ? (int?)yes : null;
                            entryValues[flag] = value;
                            if (diagnostics != null) Trace("진입 가상 플래그 " + Name(flag) + " = " +
                                (value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "?") + " · 실제 상태 유지");
                        }
                    }
                    entryUnknown = false;
                }
                catch (Exception) { Trace("진입 설정/읽기 한도 확인 실패 · 안내 보류"); }
                finally
                {
                    projectingEntry = false; enteringPlace = true; entryReadLimit = 0;
                    if (entryUnknown) { entryValues = null; Trace("진입 처리 전체를 확인하지 못함 · 가상 값 사용 중단"); }
                }
            }
            private sealed class AffectedInput : Exception { }
            private sealed class EntryReadLimit : Exception { }
            private void Step()
            {
                ++work;
                if (entryReadLimit != 0 && work > entryReadLimit) throw new EntryReadLimit();
                if (work > 16384) throw new InvalidOperationException("Preview condition read limit.");
            }
            internal bool? FlagWritesDisjoint(PreviewCondition node, HashSet<string> flags, IDictionary<string, int> sets, bool afterTalkReset)
            {
                HashSet<string> previous = maskedFlags;
                IDictionary<string, int> previousSets = sameValueSets;
                bool previousReset = setsAfterTalkReset;
                maskedFlags = flags; sameValueSets = sets; setsAfterTalkReset = afterTalkReset;
                try { return ValidateDependencies(node, 0) ? (bool?)true : null; }
                catch (AffectedInput) { return false; }
                finally { maskedFlags = previous; sameValueSets = previousSets; setsAfterTalkReset = previousReset; }
            }
            private bool ValidateDependencies(PreviewCondition node, int depth)
            {
                Step();
                if (node == null || depth > 64 || node.Operation == "unknown") return false;
                if (node.Operation == "literal") return node.Literal is bool || node.Literal is int;
                if (node.Operation == "captured")
                {
                    var effects = node.Literal as EffectSnapshot;
                    return effects != null && !effects.Opaque && ValidateDependencies(node.Left, depth + 1);
                }
                if (node.Operation == "Condition" || node.Operation == "Switch" || node.Operation == "NextSwitch")
                {
                    object item = FindCheckpoint(node.Operation == "Condition", node.Key);
                    if (item == null) return false;
                    if (node.Operation == "Condition") { ValidateConditionData(item, 0); return true; }
                    object countMode = Field(item, "_useCount");
                    if (!(countMode is bool)) return false;
                    if ((bool)countMode)
                    {
                        if (!(Field(item, "_count") is int)) return false;
                        foreach (object entry in List(item, "_countItems", 256))
                        {
                            Step(); Exact(entry, "Mortal.Core.ValueCompareItem");
                            int op = Convert.ToInt32(Field(entry, "_compareOp"));
                            if (!(Field(entry, "_value") is int) || op < 0 || op > 5) return false;
                        }
                        return true;
                    }
                    foreach (object condition in List(item, "_items", 256)) ValidateCondition(condition, 0);
                    return true;
                }
                if (node.Operation == "not") return ValidateDependencies(node.Left, depth + 1);
                if (node.Operation != "and" && node.Operation != "or" && node.Operation != "==" && node.Operation != "~=" &&
                    node.Operation != "<" && node.Operation != "<=" && node.Operation != ">" && node.Operation != ">=") return false;
                return ValidateDependencies(node.Left, depth + 1) && ValidateDependencies(node.Right, depth + 1);
            }
            internal static bool? Truth(object value)
            {
                if (value == null) return null;
                if (ReferenceEquals(value, falsey)) return false;
                return !(value is bool) || (bool)value;
            }
            internal object Value(PreviewCondition node, int depth)
            {
                Step();
                if (node == null || depth > 64 || node.Operation == "unknown") return null;
                if (!projectTalkResets) return ReadValue(node, depth);
                object value;
                if (!routeValues.TryGetValue(node, out value))
                { value = ReadValue(node, depth); routeValues.Add(node, value); }
                return value;
            }
            private object ReadValue(PreviewCondition node, int depth)
            {
                if (node.Operation == "literal") return node.Literal;
                if (node.Operation == "captured")
                {
                    object result;
                    if (captured.TryGetValue(node, out result)) return result;
                    var effects = node.Literal as EffectSnapshot;
                    if (effects == null || effects.Opaque) result = null;
                    else if (effects.Flags.Count == 0 && !effects.TalkReset) result = Value(node.Left, depth + 1);
                    else
                    {
                        var previous = maskedFlags; var previousTalk = maskedTalkFlags; bool previousAll = maskAllFlagInputs;
                        maskedFlags = effects.Flags;
                        if (effects.TalkReset)
                        {
                            ReadTalkFootprint(); maskedTalkFlags = talkFlags; maskAllFlagInputs = !talkFootprintKnown;
                        }
                        else { maskedTalkFlags = null; maskAllFlagInputs = false; }
                        try { result = ReadCheckpoint(node.Left.Operation, node.Left.Key); }
                        catch (Exception) { result = null; }
                        finally { maskedFlags = previous; maskedTalkFlags = previousTalk; maskAllFlagInputs = previousAll; }
                    }
                    captured.Add(node, result); return result;
                }
                if (node.Operation == "Switch" || node.Operation == "NextSwitch" || node.Operation == "Condition")
                {
                    string key = node.Operation + ":" + node.Key;
                    object result;
                    if (!checkpoints.TryGetValue(key, out result))
                    {
                        try { result = ReadCheckpoint(node.Operation, node.Key); }
                        catch (Exception) { result = null; }
                        checkpoints.Add(key, result);
                    }
                    return result;
                }
                object left = Value(node.Left, depth + 1);
                bool? leftTruth = Truth(left);
                if (node.Operation == "not") return leftTruth.HasValue ? (object)!leftTruth.Value : null;
                if (node.Operation == "and" && leftTruth == false || node.Operation == "or" && leftTruth == true) return left;
                object right = Value(node.Right, depth + 1);
                bool? rightTruth = Truth(right);
                if (node.Operation == "and") return leftTruth == true ? right : rightTruth == false ? falsey : null;
                if (node.Operation == "or") return leftTruth == false ? right : rightTruth == true ? truthy : null;
                if (left == null || right == null || ReferenceEquals(left, truthy) || ReferenceEquals(left, falsey) ||
                    ReferenceEquals(right, truthy) || ReferenceEquals(right, falsey)) return null;
                if (left is SwitchChoices || right is SwitchChoices) return CompareChoices(node.Operation, left, right);
                if (node.Operation == "==") return left.GetType() == right.GetType() && left.Equals(right);
                if (node.Operation == "~=") return left.GetType() != right.GetType() || !left.Equals(right);
                if (!(left is int) || !(right is int)) return null;
                switch (node.Operation)
                {
                    case "<": return (int)left < (int)right;
                    case "<=": return (int)left <= (int)right;
                    case ">": return (int)left > (int)right;
                    case ">=": return (int)left >= (int)right;
                    default: return null;
                }
            }
            private object CompareChoices(string operation, object left, object right)
            {
                var a = left as SwitchChoices; var b = right as SwitchChoices;
                if (a == null && !(left is int) || b == null && !(right is int))
                    return operation == "==" ? (object)false : operation == "~=" ? true : (object)null;
                int minA = a == null ? (int)left : a.Minimum, maxA = a == null ? (int)left : a.Maximum;
                int minB = b == null ? (int)right : b.Minimum, maxB = b == null ? (int)right : b.Maximum;
                if (operation == "==" || operation == "~=")
                {
                    bool? equal;
                    if (minA == maxA && minB == maxB) equal = minA == minB;
                    else
                    {
                        bool overlap = false;
                        if (a == null) overlap = b.Values.Contains((int)left);
                        else if (b == null) overlap = a.Values.Contains((int)right);
                        else foreach (int value in a.Values) { Step(); if (b.Values.Contains(value)) { overlap = true; break; } }
                        equal = overlap ? (bool?)null : false;
                    }
                    return !equal.HasValue ? null : (object)(operation == "==" ? equal.Value : !equal.Value);
                }
                switch (operation)
                {
                    case "<": return maxA < minB ? (object)true : minA >= maxB ? false : (object)null;
                    case "<=": return maxA <= minB ? (object)true : minA > maxB ? false : (object)null;
                    case ">": return minA > maxB ? (object)true : maxA <= minB ? false : (object)null;
                    case ">=": return minA >= maxB ? (object)true : maxA < minB ? false : (object)null;
                    default: return null;
                }
            }

            internal static object Field(object target, string name)
            {
                if (target is DisplayDefinition definition) return definition.Read(name); // immutable installation definitions
                if (target == null) throw new InvalidOperationException("Missing preview condition data.");
                Type type = target as Type ?? target.GetType();
                for (Type at = type; at != null; at = at.BaseType)
                {
                    FieldInfo field = at.GetField(name, BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    if (field != null)
                    {
                        object owner = target is Type ? null : target;
                        object result = field.GetValue(owner);
                        if (ReadStamp.IsRecording) ReadStamp.ObserveField(target, field, result);
                        return result;
                    }
                }
                throw new MissingFieldException(type.FullName, name);
            }
            internal static void Exact(object value, string type)
            { if (value == null || (value is DisplayDefinition definition ? definition.TypeName : value.GetType().FullName) != type) throw new InvalidOperationException("Unsupported preview condition type."); }
            private IList List(object value, string name, int limit = 8192)
            {
                object result = Field(value, name);
                if (result == null || !result.GetType().IsGenericType || result.GetType().GetGenericTypeDefinition() != typeof(List<>))
                    throw new InvalidOperationException("Unsupported preview condition collection.");
                IList list = (IList)result;
                if (list.Count > limit) throw new InvalidOperationException("Preview condition collection limit.");
                return list;
            }
            private static Type checkpointType;
            internal static object ExistingManager()
            {
                const string name = "Mortal.Story.CheckPointManager";
                Type type = checkpointType;
                if (type == null)
                {
                    type = typeof(PreviewConditions).Assembly.GetType(name, false);
                    if (type == null)
                        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                        { type = assembly.GetType(name, false); if (type != null) break; }
                    if (type == null) return null;
                    checkpointType = type; // Assembly metadata only, never an instance or current value.
                }
                object manager = Field(type, "<Instance>k__BackingField");
                if (!OwnerAlive(manager)) return null;
                Exact(manager, name);
                return manager;
            }
            private static string Name(object value)
            {
                if (value is DisplayDefinition definition) return definition.Name;
                // Read the audited nonvirtual Unity Object.name getter only;
                // no arbitrary configuration property or custom getter runs.
                for (Type at = value.GetType(); at != null; at = at.BaseType)
                {
                    if (at.FullName != "UnityEngine.Object") continue;
                    PropertyInfo property = at.GetProperty("name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    MethodInfo getter = property == null ? null : property.GetGetMethod();
                    if (getter == null || getter.IsVirtual) break;
                    return property.GetValue(value, null) as string;
                }
                throw new InvalidOperationException("Unsupported preview checkpoint name.");
            }
            private object Find(object config, string configType, string itemType, string key)
            {
                Exact(config, configType);
                IList list = List(config, "_list");
                DefinitionIndex index = null; object found;
                if (projectTalkResets)
                {
                    index = definitions.Index(config, list);
                    if (index.Values.TryGetValue(key, out found)) return found;
                }
                for (int at = index == null ? 0 : index.Cursor; at < list.Count; at++)
                {
                    object item = list[at];
                    Step(); Exact(item, itemType);
                    string name = Name(item);
                    if (index != null)
                    {
                        if (name != null && !index.Values.ContainsKey(name)) index.Values.Add(name, item);
                        index.Cursor = at + 1;
                    }
                    if (string.Equals(name, key, StringComparison.Ordinal)) return item;
                }
                return null;
            }
            private object FindCheckpoint(bool condition, string key)
            {
                if (!managerRead) { manager = ExistingManager(); managerRead = true; }
                string configType = condition ? "Mortal.Story.ConditionResultConfig" : "Mortal.Story.SwitchResultConfig";
                string itemType = condition ? "Mortal.Core.ConditionResultData" : "Mortal.Story.SwitchResultData";
                Array configs;
                bool loaded = manager == null;
                if (!loaded) configs = condition ? new object[] { Field(manager, "_condition") } : Field(manager, "_switchList") as Array;
                else configs = projectTalkResets ? LoadedDefinitions(condition ? PreviewDefinitionKind.Conditions : PreviewDefinitionKind.Switches) : null;
                if (configs == null || configs.Rank != 1 || configs.Length > (loaded ? 64 : 256)) return null;
                object result = null;
                foreach (object config in configs)
                {
                    Step(); object item = Find(config, configType, itemType, key);
                    if (item == null) continue;
                    // Native ownership keeps its first-match semantics. Asset
                    // fallback has no authoritative order: conflicting names
                    // stay unknown instead of borrowing an unrelated config.
                    if (!loaded) return item;
                    if (result != null && !ReferenceEquals(result, item)) throw new InvalidOperationException("Ambiguous loaded checkpoint definition.");
                    result = item;
                }
                return result;
            }
            private object ReadCheckpoint(string operation, string key)
            {
                object data = FindCheckpoint(operation == "Condition", key);
                if (data == null) return null;
                if (operation == "Condition")
                { if (diagnostics != null) Trace("Condition " + key); return ReadConditionData(data, 0); }
                if (diagnostics != null)
                {
                    string count;
                    try { count = Convert.ToString(Field(data, "_count"), CultureInfo.InvariantCulture); } catch (Exception) { count = "?"; }
                    Trace("Switch " + key + " · useCount=" + Convert.ToString(Field(data, "_useCount"), CultureInfo.InvariantCulture) + " · count=" + count);
                }
                if (!(Field(data, "_useCount") is bool)) return null;
                // A missing story owner has not established its execution
                // counters. Serialized asset counts cannot stand in for them.
                if ((bool)Field(data, "_useCount")) return operation == "NextSwitch" && manager != null ? ReadNextCount(data) : null;
                IList items = List(data, "_items", 256);
                var possible = new List<int>(); bool stopped = false;
                for (int i = 0; i < items.Count; i++)
                {
                    bool? value = ReadCondition(items[i], 0);
                    if (value != false) possible.Add(i + 1);
                    if (value == true) { stopped = true; break; }
                }
                if (!stopped) possible.Add(items.Count + 1);
                return possible.Count == 1 ? (object)possible[0] : new SwitchChoices(possible);
            }
            private object ReadNextCount(object data)
            {
                object raw = Field(data, "_count");
                if (!(raw is int) || (int)raw < 0 || (int)raw == int.MaxValue) return null;
                int next = (int)raw + 1;
                IList items = List(data, "_countItems", 256);
                for (int at = 0; at < items.Count; at++)
                {
                    Step(); Exact(items[at], "Mortal.Core.ValueCompareItem");
                    object target = Field(items[at], "_value"); int op = Convert.ToInt32(Field(items[at], "_compareOp"));
                    if (!(target is int) || op < 0 || op > 5) return null;
                    int value = (int)target;
                    bool match = op == 0 ? next == value : op == 1 ? next != value : op == 2 ? next < value :
                        op == 3 ? next <= value : op == 4 ? next > value : next >= value;
                    if (match) return at + 1;
                }
                return items.Count + 1;
            }
            internal bool UnresolvedEntrySwitch(PreviewCondition node, int depth)
            {
                Step(); if (node == null) return false;
                if (depth > 64) return true;
                PreviewCondition leaf = node.Operation == "captured" ? node.Left : node;
                if (leaf != null && (leaf.Operation == "Switch" || leaf.Operation == "NextSwitch"))
                {
                    object data = FindCheckpoint(false, leaf.Key);
                    if (data == null) return leaf.Operation == "NextSwitch";
                    object mode = Field(data, "_useCount");
                    if (mode is bool && !(bool)mode) return false;
                    return !(mode is bool) || Value(node, depth + 1) == null;
                }
                return UnresolvedEntrySwitch(node.Left, depth + 1) || UnresolvedEntrySwitch(node.Right, depth + 1);
            }
            private bool? ReadConditionData(object value, int depth)
            {
                Enter(value, depth);
                try
                {
                    Exact(value, "Mortal.Core.ConditionResultData"); bool unknown = false;
                    foreach (object item in List(value, "_items", 256))
                    {
                        bool? state = ReadCondition(item, depth + 1);
                        if (state == true) return true;
                        unknown |= !state.HasValue;
                    }
                    return unknown ? (bool?)null : false;
                }
                finally { active.Remove(value); }
            }
            private bool? ReadCondition(object value, int depth)
            {
                bool entered = false;
                try
                {
                    Enter(value, depth); entered = true; Exact(value, "Mortal.Core.ConditionResultItem");
                    int logic = Convert.ToInt32(Field(value, "_logicOp"));
                    if (logic != 0 && logic != 1) return null;
                    if (diagnostics != null) Trace("조건 " + (logic == 0 ? "AND" : "OR"));
                    IList items = List(value, "_items", 256);
                    if (items.Count == 0) return false;
                    bool unknown = false;
                    foreach (object item in items)
                    {
                        bool? state = ReadComparison(item, depth + 1);
                        if (logic == 0 && state == false || logic == 1 && state == true) return logic == 1;
                        unknown |= !state.HasValue;
                    }
                    return unknown ? (bool?)null : logic == 0;
                }
                catch (EntryReadLimit) { throw; }
                catch (Exception) { if (projectingEntry) entryReadFailed = true; return null; }
                finally { if (entered) active.Remove(value); }
            }
            private bool? ReadComparison(object value, int depth)
            {
                try
                {
                    Step(); if (depth > 32) return null; Exact(value, "Mortal.Core.StatCompareItem");
                    int? left = ReadNumberGroup(Field(value, "_value1"), depth + 1);
                    int? right = ReadNumberGroup(Field(value, "_value2"), depth + 1);
                    int op = Convert.ToInt32(Field(value, "_compareOp"));
                    if (diagnostics != null) Trace("비교 " + (left.HasValue ? left.Value.ToString(CultureInfo.InvariantCulture) : "?") + " " +
                        (op >= 0 && op < 6 ? new[] { "=", "!=", "<", "<=", ">", ">=" }[op] : "미지원") + " " +
                        (right.HasValue ? right.Value.ToString(CultureInfo.InvariantCulture) : "?"));
                    if (!left.HasValue || !right.HasValue) return null;
                    switch (op)
                    {
                        case 0: return left.Value == right.Value;
                        case 1: return left.Value != right.Value;
                        case 2: return left.Value < right.Value;
                        case 3: return left.Value <= right.Value;
                        case 4: return left.Value > right.Value;
                        case 5: return left.Value >= right.Value;
                        default: return null;
                    }
                }
                // Entry work exhaustion aborts the entire forecast instead of
                // spending another caught exception on every remaining value.
                catch (EntryReadLimit) { throw; }
                // A random, affected or malformed comparison is one unknown
                // operand. It must not hide a separate false AND / true OR.
                catch (Exception) { if (projectingEntry) entryReadFailed = true; return null; }
            }
            private int? ReadNumberGroup(object value, int depth)
            {
                bool entered = false;
                try
                {
                    Enter(value, depth); entered = true; Exact(value, "Mortal.Core.StatGroupVariable");
                    int type = Convert.ToInt32(Field(value, "_groupType"));
                    if (type < 1 || type > 4) return null;
                    IList items = List(value, "_value", 256);
                    if ((type == 2 || type == 3) && items.Count == 0) return null;
                    int total = type == 4 ? 1 : 0; bool unknown = false; int at = 0;
                    foreach (object item in items)
                    {
                        int? number = ReadNumberReference(item, depth + 1);
                        if (!number.HasValue) { unknown = true; continue; }
                        switch (type)
                        {
                            case 1: total = checked(total + number.Value); break;
                            case 2: total = at == 0 ? number.Value : Math.Max(total, number.Value); break;
                            case 3: total = at == 0 ? number.Value : Math.Min(total, number.Value); break;
                            case 4: total = unchecked(total * number.Value); break;
                        }
                        at++;
                    }
                    return unknown ? (int?)null : total;
                }
                catch (EntryReadLimit) { throw; }
                catch (Exception) { if (projectingEntry) entryReadFailed = true; return null; }
                finally { if (entered) active.Remove(value); }
            }
            private int? ReadNumberReference(object value, int depth)
            {
                bool entered = false;
                try
                {
                    Enter(value, depth); entered = true; Exact(value, "Mortal.Core.StatValueReference");
                    int kind = Convert.ToInt32(Field(value, "_checkType"));
                    if (kind == 7) { Trace("난수 · 추첨하지 않음"); return null; }
                    if (kind == 8) return ReadNumberGroup(Field(value, "_statGroup"), depth + 1);
                    if (kind == 16)
                    {
                        // Preserve partial truth at every nested condition, not
                        // just at the outer checkpoint. Native GetValue would
                        // Execute() this condition and mutate its counter.
                        bool? truth = ReadConditionData(Field(value, "_condition"), depth + 1);
                        if (diagnostics != null) Trace("중첩 조건 = " + (!truth.HasValue ? "?" : truth.Value ? "1" : "0"));
                        return truth.HasValue ? (int?)(truth.Value ? 1 : 0) : null;
                    }
                    if (projectTalkResets && kind == 3)
                    {
                        object flag = Field(value, "_flag"); Exact(flag, "Mortal.Core.FlagData");
                        // Explicit writes retain their conservative mask, even
                        // if they occur before a later reset. Never guess order.
                        if (maskedFlags != null && maskedFlags.Contains(Name(flag))) throw new AffectedInput();
                        if (maskAllFlagInputs) throw new AffectedInput();
                        if (maskedTalkFlags != null && maskedTalkFlags.Contains(flag))
                        {
                            int resetValue;
                            if (!resetDefaults.TryGetValue(flag, out resetValue))
                            {
                                object raw = Field(flag, "_default");
                                if (!(raw is int)) throw new InvalidOperationException("Missing serialized reset default.");
                                resetDefaults.Add(flag, resetValue = (int)raw);
                            }
                            if (diagnostics != null) Trace("대화 초기화 기본값 " + Name(flag) + " = " + resetValue.ToString(CultureInfo.InvariantCulture) + " · 가상 조회 (게임 상태 유지)");
                            return unchecked(resetValue * ((bool)Field(value, "_negative") ? -1 : 1));
                        }
                        int? projected;
                        if ((!entryUnknown || projectingEntry) && entryValues != null && entryValues.TryGetValue(flag, out projected))
                        {
                            if (!projected.HasValue) throw new AffectedInput();
                            return unchecked(projected.Value * ((bool)Field(value, "_negative") ? -1 : 1));
                        }
                    }
                    ValidateReference(value, depth);
                    int number = SafeDisplayEvaluation.ReadReference(value);
                    if (diagnostics != null && kind == 3)
                    {
                        string id; try { id = Name(Field(value, "_flag")); } catch (Exception) { id = "?"; }
                        Trace("플래그 " + id + " = " + number.ToString(CultureInfo.InvariantCulture) + " (비교 부호 반영)");
                    }
                    return number;
                }
                catch (EntryReadLimit) { throw; }
                catch (AffectedInput) { Trace("선행 쓰기/대화 초기화 영향 · 이전 값으로 확정하지 않음"); return null; }
                catch (Exception) { if (projectingEntry) entryReadFailed = true; Trace("미지원/읽기 제한 값 · 미확정"); return null; }
                finally { if (entered) active.Remove(value); }
            }
            private void Enter(object value, int depth)
            {
                Step();
                if (value == null || depth > 32 || !active.Add(value)) throw new InvalidOperationException("Invalid preview condition graph.");
            }
            private void ValidateConditionData(object value, int depth)
            {
                Enter(value, depth);
                try
                {
                    Exact(value, "Mortal.Core.ConditionResultData");
                    foreach (object condition in List(value, "_items", 256)) ValidateCondition(condition, depth + 1);
                }
                finally { active.Remove(value); }
            }
            private void ValidateCondition(object value, int depth)
            {
                Enter(value, depth);
                try
                {
                    Exact(value, "Mortal.Core.ConditionResultItem");
                    int logic = Convert.ToInt32(Field(value, "_logicOp"));
                    if (logic != 0 && logic != 1) throw new InvalidOperationException("Unsupported preview condition logic.");
                    foreach (object comparison in List(value, "_items", 256))
                    {
                        Step(); Exact(comparison, "Mortal.Core.StatCompareItem");
                        int operation = Convert.ToInt32(Field(comparison, "_compareOp"));
                        if (operation < 0 || operation > 5) throw new InvalidOperationException("Unsupported preview comparison.");
                        ValidateGroup(Field(comparison, "_value1"), depth + 1);
                        ValidateGroup(Field(comparison, "_value2"), depth + 1);
                    }
                }
                finally { active.Remove(value); }
            }
            private void ValidateGroup(object value, int depth)
            {
                Enter(value, depth);
                try
                {
                    Exact(value, "Mortal.Core.StatGroupVariable");
                    int groupType = Convert.ToInt32(Field(value, "_groupType"));
                    if (groupType < 1 || groupType > 4) throw new InvalidOperationException("Unsupported preview value group.");
                    foreach (object reference in List(value, "_value", 256))
                    {
                        Step(); ValidateReference(reference, depth);
                    }
                }
                finally { active.Remove(value); }
            }
            private void ValidateReference(object reference, int depth)
            {
                Exact(reference, "Mortal.Core.StatValueReference");
                int kind = Convert.ToInt32(Field(reference, "_checkType"));
                if (kind < 0 || kind > 16 || kind == 7) throw new InvalidOperationException("Unsupported current preview condition value.");
                // Only the same concrete reference targets whose getters
                // were audited by SafeDisplayEvaluation are admissible.
                // An unexpected subclass cannot supply a new getter.
                switch (kind)
                {
                    case 1: ValidateStat(Field(reference, "_stat"), depth + 1); break;
                    case 2: Exact(Field(reference, "_relationship"), "Mortal.Core.RelationshipStat"); break;
                    case 3: ValidateFlag(Field(reference, "_flag")); break;
                    case 4: case 11: Exact(Field(reference, "_book"), "Mortal.Core.Book"); break;
                    case 5: Exact(Field(reference, "_misc"), "Mortal.Core.Misc"); break;
                    case 6: Exact(Field(reference, "_specialItem"), "Mortal.Core.SpecialItem"); break;
                    case 9:
                        object time = Field(reference, "_gameTime");
                        Exact(time, "Mortal.Core.GameTimeData"); Exact(Field(time, "GameTime"), "Mortal.Core.GameTime");
                        break;
                    case 10: Exact(Field(reference, "_talentData"), "Mortal.Core.PlayerTalentData"); break;
                    case 12: case 13: Exact(Field(reference, "_upgradeItemData"), "Mortal.Core.UpgradeItemData"); break;
                    case 14:
                        object books = Field(reference, "_bookCollection");
                        Exact(books, "Mortal.Core.BookDataCollection");
                        foreach (object book in List(books, "_list", 256)) { Step(); Exact(book, "Mortal.Core.Book"); }
                        break;
                    case 15: Exact(Field(reference, "_libraryAchieve"), "Mortal.Core.LibraryItemData"); break;
                }
                if (kind == 8) ValidateGroup(Field(reference, "_statGroup"), depth + 1);
                if (kind == 16) ValidateConditionData(Field(reference, "_condition"), depth + 1);
            }
            private void ValidateStat(object value, int depth)
            {
                Enter(value, depth);
                try
                {
                    Exact(value, "Mortal.Core.GameStat");
                    // Native activity entry spends action before loading Lua.
                    // Projecting arbitrary stat arithmetic/listeners is outside
                    // this route preview; action-dependent guards stay unknown.
                    if (enteringPlace && Convert.ToInt32(Field(value, "_type")) == 19) throw new AffectedInput();
                    object raw = Field(value, "_statAdditionData");
                    if (raw == null) return;
                    Array collections = raw as Array;
                    if (collections == null || collections.Rank != 1 || collections.Length > 256)
                        throw new InvalidOperationException("Unsupported preview stat conversions.");
                    foreach (object collection in collections)
                    {
                        Step(); Exact(collection, "Mortal.Core.StatConvertCollectionData");
                        foreach (object conversion in List(collection, "_list", 256))
                        {
                            Step();
                            if (conversion == null) throw new InvalidOperationException("Missing preview stat conversion.");
                            switch (conversion.GetType().FullName)
                            {
                                case "Mortal.Core.GameStatConvertData": case "Mortal.Core.GameStatRateConvertData":
                                    ValidateStat(Field(conversion, "_statData"), depth + 1); break;
                                case "Mortal.Core.FlagDataRateConvertData":
                                    object input = Field(conversion, "_flagData"); ValidateFlag(input);
                                    // Its native getter still reads real State;
                                    // direct virtual flag reads above are the
                                    // only supported substitution boundary.
                                    if (entryValues != null && entryValues.ContainsKey(input)) throw new AffectedInput();
                                    break;
                                case "Mortal.Core.BookLevelStatConvertData": Exact(Field(conversion, "_book"), "Mortal.Core.Book"); break;
                                case "Mortal.Core.UpgradeItemStatConvertData": Exact(Field(conversion, "_statData"), "Mortal.Core.UpgradeItemData"); break;
                                default: throw new UnsupportedDisplayStatException();
                            }
                        }
                    }
                }
                finally { active.Remove(value); }
            }
            private void ValidateFlag(object value)
            {
                Exact(value, "Mortal.Core.FlagData");
                if (entryUnknown && !projectingEntry) throw new AffectedInput();
                int? projected;
                if (entryValues != null && entryValues.TryGetValue(value, out projected) && !projected.HasValue) throw new AffectedInput();
                if (maskAllFlagInputs || maskedTalkFlags != null && maskedTalkFlags.Contains(value)) throw new AffectedInput();
                if (maskedFlags == null || maskedFlags.Count == 0) return;
                string name = Name(value);
                if (!maskedFlags.Contains(name)) return;
                int constant;
                if (sameValueSets != null && sameValueSets.TryGetValue(name, out constant))
                {
                    // A reset before this future assignment can change today's
                    // equal value. Only a known disjoint reset footprint permits
                    // this no-op exemption; unrelated writes remain disjoint.
                    if (setsAfterTalkReset)
                    {
                        ReadTalkFootprint();
                        if (!talkFootprintKnown) throw new InvalidOperationException("Talk reset dependency is unavailable.");
                        if (talkFlags.Contains(value)) throw new AffectedInput();
                    }
                    if (entryValues != null && entryValues.TryGetValue(value, out projected)
                        ? projected.HasValue && projected.Value == constant : FlagState(value) == constant) return;
                }
                throw new AffectedInput();
            }
            private int FlagState(object value)
            {
                Step();
                // The supplied game's concrete FlagData.State is a public int
                // field. Managed fixtures may expose the equivalent audited,
                // nonvirtual int getter; no subclass/custom target is accepted.
                Type type = value.GetType();
                FieldInfo field = type.GetField("State", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (field != null && field.FieldType == typeof(int)) return (int)field.GetValue(value);
                PropertyInfo property = type.GetProperty("State", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                MethodInfo getter = property == null ? null : property.GetGetMethod();
                if (getter == null || getter.IsVirtual || getter.ReturnType != typeof(int) || getter.GetParameters().Length != 0)
                    throw new InvalidOperationException("Unsupported preview flag state.");
                return (int)getter.Invoke(value, null);
            }
            internal string TalkSourceDiagnostic { get; private set; } = "대화 초기화 자료: 미확인";
            internal void ReadTalkFootprint()
            {
                if (talkFootprintRead) return;
                talkFootprintRead = true;
                try
                {
                    if (!managerRead) { manager = ExistingManager(); managerRead = true; }
                    string source;
                    object collection = CurrentTalkCollection(manager, out source);
                    if (collection == null) return;
                    IList list = List(collection, "_list", 1024);
                    var flags = new HashSet<object>(new Identity());
                    foreach (object flag in list) { Step(); Exact(flag, "Mortal.Core.FlagData"); flags.Add(flag); }
                    talkFlags = flags; talkFootprintKnown = true;
                    TalkSourceDiagnostic = "대화 초기화 자료: 확인 · " + source + " · " + flags.Count.ToString(CultureInfo.InvariantCulture) + "개";
                }
                catch (Exception) { }
                finally { Trace(TalkSourceDiagnostic); }
            }
        }
    }
}
