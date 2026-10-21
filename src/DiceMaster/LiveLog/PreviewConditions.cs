using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DiceMaster.LiveLog
{
    public enum PreviewConditionState { Unknown, Met, NotMet }

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

        private sealed class EffectSnapshot
        {
            internal readonly HashSet<string> Flags;
            internal readonly bool Opaque, TalkReset;
            internal EffectSnapshot(HashSet<string> flags, bool opaque, bool talkReset)
            { Flags = flags; Opaque = opaque; TalkReset = talkReset; }
        }

        private static WeakReference observedEnvironment, observedTalkFlags;

        // Observe only an environment the game already loaded. Read the
        // interpreter field and MoonSharp's exact raw table lookup, never a Lua
        // expression, index metamethod, lazy singleton or environment initializer.
        public static void ObserveLuaEnvironment(object environment)
        {
            observedEnvironment = observedTalkFlags = null;
            try
            {
                object collection = TalkCollection(environment);
                if (collection == null) return;
                observedEnvironment = new WeakReference(environment);
                observedTalkFlags = new WeakReference(collection);
            }
            catch (Exception) { }
        }
        private static object TalkCollection(object environment)
        {
            if (environment == null) return null;
            Type owner = environment.GetType();
            while (owner != null && owner.FullName != "Fungus.LuaEnvironment") owner = owner.BaseType;
            if (owner == null) return null;
            FieldInfo interpreter = owner.GetField("interpreter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (interpreter == null) return null;
            object script = interpreter.GetValue(environment);
            Snapshot.Exact(script, "MoonSharp.Interpreter.Script");
            object globals = Snapshot.Field(script, "m_GlobalTable");
            Snapshot.Exact(globals, "MoonSharp.Interpreter.Table");
            MethodInfo raw = globals.GetType().GetMethod("RawGet", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
                null, new[] { typeof(string) }, null);
            if (raw == null || raw.IsVirtual || raw.ReturnType.FullName != "MoonSharp.Interpreter.DynValue") return null;
            object value = raw.Invoke(globals, new object[] { "statmodifymanager" });
            Snapshot.Exact(value, "MoonSharp.Interpreter.DynValue");
            object data = Snapshot.Field(value, "m_Object");
            Snapshot.Exact(data, "MoonSharp.Interpreter.UserData");
            object manager = Snapshot.Field(data, "<Object>k__BackingField");
            Snapshot.Exact(manager, "Mortal.Story.StatModifyManager");
            object collection = Snapshot.Field(manager, "_talkOptionFlag");
            Snapshot.Exact(collection, "Mortal.Core.FlagCollectionData");
            return collection;
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
            if (node.Operation == "Condition" || node.Operation == "Switch") return new PreviewCondition("captured", effects, left: node);
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
            private readonly Snapshot snapshot = new Snapshot();
            public bool AreFlagWritesDisjoint(PreviewCondition condition, IEnumerable<string> flagWrites)
            { return AreFlagWritesDisjoint(condition, flagWrites, null); }
            public bool AreFlagWritesDisjoint(PreviewCondition condition, IEnumerable<string> flagWrites, IDictionary<string, int> sameValueSetCandidates)
            { return AreFlagWritesDisjoint(condition, flagWrites, sameValueSetCandidates, false); }
            public bool AreFlagWritesDisjoint(PreviewCondition condition, IEnumerable<string> flagWrites, IDictionary<string, int> sameValueSetCandidates, bool afterTalkReset)
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
                            if (++count > 32 || pair.Key == null || pair.Key.Length > 256) return false;
                            if (flags.Contains(pair.Key)) sets.Add(pair.Key, pair.Value);
                        }
                    }
                    return snapshot.FlagWritesDisjoint(condition, flags, sets, afterTalkReset);
                }
                catch (Exception) { return false; }
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

        // Explicit log detail only. The sample is bounded and discarded after
        // formatting; no current values enter cached syntax or automatic logs.
        public static string DescribeCurrent(PreviewCondition condition)
        {
            var sample = new Snapshot(true); bool? result = null;
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

        private sealed class Snapshot
        {
            private readonly List<string> diagnostics;
            private bool diagnosticsLimited;
            internal Snapshot(bool explain = false) { if (explain) diagnostics = new List<string>(); }
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
            private HashSet<string> maskedFlags;
            private IDictionary<string, int> sameValueSets;
            private bool setsAfterTalkReset;
            private HashSet<object> maskedTalkFlags, talkFlags;
            private bool maskAllFlagInputs, talkFootprintRead, talkFootprintKnown;
            private sealed class AffectedInput : Exception { }
            private void Step() { if (++work > 16384) throw new InvalidOperationException("Preview condition read limit."); }
            internal bool FlagWritesDisjoint(PreviewCondition node, HashSet<string> flags, IDictionary<string, int> sets, bool afterTalkReset)
            {
                HashSet<string> previous = maskedFlags;
                IDictionary<string, int> previousSets = sameValueSets;
                bool previousReset = setsAfterTalkReset;
                maskedFlags = flags; sameValueSets = sets; setsAfterTalkReset = afterTalkReset;
                try { return ValidateDependencies(node, 0); }
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
                if (node.Operation == "Condition" || node.Operation == "Switch")
                {
                    if (!managerRead) { manager = ExistingManager(); managerRead = true; }
                    if (manager == null) return false;
                    if (node.Operation == "Condition")
                    {
                        object item = Find(Field(manager, "_condition"), "Mortal.Story.ConditionResultConfig", "Mortal.Core.ConditionResultData", node.Key);
                        if (item == null) return false;
                        ValidateConditionData(item, 0); return true;
                    }
                    Array configs = Field(manager, "_switchList") as Array;
                    if (configs == null || configs.Rank != 1 || configs.Length > 256) return false;
                    foreach (object config in configs)
                    {
                        Step();
                        object item = Find(config, "Mortal.Story.SwitchResultConfig", "Mortal.Story.SwitchResultData", node.Key);
                        if (item == null) continue;
                        if (!(Field(item, "_useCount") is bool) || (bool)Field(item, "_useCount")) return false;
                        foreach (object condition in List(item, "_items", 256)) ValidateCondition(condition, 0);
                        return true;
                    }
                    return false;
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
                if (node.Operation == "Switch" || node.Operation == "Condition")
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
                if (target == null) throw new InvalidOperationException("Missing preview condition data.");
                Type type = target as Type ?? target.GetType();
                for (Type at = type; at != null; at = at.BaseType)
                {
                    FieldInfo field = at.GetField(name, BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    if (field != null) return field.GetValue(target is Type ? null : target);
                }
                throw new MissingFieldException(type.FullName, name);
            }
            internal static void Exact(object value, string type)
            { if (value == null || value.GetType().FullName != type) throw new InvalidOperationException("Unsupported preview condition type."); }
            private IList List(object value, string name, int limit = 8192)
            {
                object result = Field(value, name);
                if (result == null || !result.GetType().IsGenericType || result.GetType().GetGenericTypeDefinition() != typeof(List<>))
                    throw new InvalidOperationException("Unsupported preview condition collection.");
                IList list = (IList)result;
                if (list.Count > limit) throw new InvalidOperationException("Preview condition collection limit.");
                return list;
            }
            private static object ExistingManager()
            {
                const string name = "Mortal.Story.CheckPointManager";
                Type type = typeof(PreviewConditions).Assembly.GetType(name, false);
                if (type == null)
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    { type = assembly.GetType(name, false); if (type != null) break; }
                if (type == null) return null;
                object manager = Field(type, "<Instance>k__BackingField");
                Exact(manager, name);
                return manager;
            }
            private static string Name(object value)
            {
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
                foreach (object item in List(config, "_list"))
                {
                    Step(); Exact(item, itemType);
                    if (string.Equals(Name(item), key, StringComparison.Ordinal)) return item;
                }
                return null;
            }
            private object ReadCheckpoint(string operation, string key)
            {
                if (!managerRead) { manager = ExistingManager(); managerRead = true; }
                if (manager == null) return null;
                if (operation == "Condition")
                {
                    if (diagnostics != null) Trace("Condition " + key);
                    object condition = Find(Field(manager, "_condition"), "Mortal.Story.ConditionResultConfig", "Mortal.Core.ConditionResultData", key);
                    if (condition == null) return null;
                    return ReadConditionData(condition, 0);
                }
                Array configs = Field(manager, "_switchList") as Array;
                if (configs == null || configs.Rank != 1 || configs.Length > 256) return null;
                foreach (object config in configs)
                {
                    Step();
                    object data = Find(config, "Mortal.Story.SwitchResultConfig", "Mortal.Story.SwitchResultData", key);
                    if (data == null) continue;
                    if (diagnostics != null)
                    {
                        string count;
                        try { count = Convert.ToString(Field(data, "_count"), CultureInfo.InvariantCulture); } catch (Exception) { count = "?"; }
                        Trace("Switch " + key + " · useCount=" + Convert.ToString(Field(data, "_useCount"), CultureInfo.InvariantCulture) + " · count=" + count);
                    }
                    if (!(Field(data, "_useCount") is bool) || (bool)Field(data, "_useCount")) return null;
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
                return null;
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
                catch (Exception) { return null; }
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
                // A random, affected or malformed comparison is one unknown
                // operand. It must not hide a separate false AND / true OR.
                catch (Exception) { return null; }
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
                catch (Exception) { return null; }
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
                    ValidateReference(value, depth);
                    int number = SafeDisplayEvaluation.ReadReference(value);
                    if (diagnostics != null && kind == 3)
                    {
                        string id; try { id = Name(Field(value, "_flag")); } catch (Exception) { id = "?"; }
                        Trace("플래그 " + id + " = " + number.ToString(CultureInfo.InvariantCulture) + " (비교 부호 반영)");
                    }
                    return number;
                }
                catch (AffectedInput) { Trace("선행 쓰기/대화 초기화 영향 · 이전 값으로 확정하지 않음"); return null; }
                catch (Exception) { Trace("미지원/읽기 제한 값 · 미확정"); return null; }
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
                                case "Mortal.Core.FlagDataRateConvertData": ValidateFlag(Field(conversion, "_flagData")); break;
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
                        if (!talkFootprintKnown || talkFlags.Contains(value)) throw new AffectedInput();
                    }
                    if (FlagState(value) == constant) return;
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
            private void ReadTalkFootprint()
            {
                if (talkFootprintRead) return;
                talkFootprintRead = true;
                try
                {
                    object environment = observedEnvironment == null ? null : observedEnvironment.Target;
                    if (environment == null) { observedEnvironment = observedTalkFlags = null; return; }
                    // The same live environment can replace its registered
                    // manager/collection. Validate that current raw binding once
                    // per reader rather than retaining any flag or State value.
                    object collection = TalkCollection(environment);
                    if (collection == null) { observedTalkFlags = null; return; }
                    if (observedTalkFlags == null || !ReferenceEquals(observedTalkFlags.Target, collection)) observedTalkFlags = new WeakReference(collection);
                    IList list = List(collection, "_list", 1024);
                    var flags = new HashSet<object>(new Identity());
                    foreach (object flag in list) { Step(); Exact(flag, "Mortal.Core.FlagData"); flags.Add(flag); }
                    talkFlags = flags; talkFootprintKnown = true;
                }
                catch (Exception) { observedTalkFlags = null; }
            }
        }
    }
}
