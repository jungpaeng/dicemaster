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
            internal readonly bool Opaque;
            internal EffectSnapshot(HashSet<string> flags, bool opaque) { Flags = flags; Opaque = opaque; }
        }

        // Capture definition-time effects, never sampled game values. A bound
        // checkpoint keeps the effects preceding its own read, so a later flag
        // write cannot retroactively change that already captured Lua value.
        public static PreviewCondition CaptureCurrent(PreviewCondition condition, IEnumerable<string> flagWrites, bool opaque)
        {
            var flags = new HashSet<string>(StringComparer.Ordinal);
            if (flagWrites != null) foreach (string flag in flagWrites)
            { if (flags.Count >= 32 || flag == null || flag.Length > 256) { opaque = true; break; } flags.Add(flag); }
            int work = 0;
            return Capture(condition, new EffectSnapshot(flags, opaque), 0, ref work);
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

        private sealed class Identity : IEqualityComparer<object>
        {
            public new bool Equals(object left, object right) { return ReferenceEquals(left, right); }
            public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
        }

        private sealed class Snapshot
        {
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
            private sealed class AffectedInput : Exception { }
            private void Step() { if (++work > 16384) throw new InvalidOperationException("Preview condition read limit."); }
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
                    else if (effects.Flags.Count == 0) result = Value(node.Left, depth + 1);
                    else
                    {
                        var previous = maskedFlags; maskedFlags = effects.Flags;
                        try { result = ReadCheckpoint(node.Left.Operation, node.Left.Key); }
                        catch (AffectedInput) { result = null; }
                        finally { maskedFlags = previous; }
                    }
                    captured.Add(node, result); return result;
                }
                if (node.Operation == "Switch" || node.Operation == "Condition")
                {
                    string key = node.Operation + ":" + node.Key;
                    object result;
                    if (!checkpoints.TryGetValue(key, out result))
                    { result = ReadCheckpoint(node.Operation, node.Key); checkpoints.Add(key, result); }
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

            private static object Field(object target, string name)
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
            private static void Exact(object value, string type)
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
                    object condition = Find(Field(manager, "_condition"), "Mortal.Story.ConditionResultConfig", "Mortal.Core.ConditionResultData", key);
                    if (condition == null) return null;
                    ValidateConditionData(condition, 0);
                    return SafeDisplayEvaluation.CheckConditionData(condition);
                }
                Array configs = Field(manager, "_switchList") as Array;
                if (configs == null || configs.Rank != 1 || configs.Length > 256) return null;
                foreach (object config in configs)
                {
                    Step();
                    object data = Find(config, "Mortal.Story.SwitchResultConfig", "Mortal.Story.SwitchResultData", key);
                    if (data == null) continue;
                    if (!(Field(data, "_useCount") is bool) || (bool)Field(data, "_useCount")) return null;
                    IList items = List(data, "_items", 256);
                    // Validate every alternative first. A random comparison in
                    // any alternative keeps the whole branch group unknown.
                    foreach (object item in items) ValidateCondition(item, 0);
                    for (int i = 0; i < items.Count; i++)
                        if (SafeDisplayEvaluation.CheckCondition(items[i])) return i + 1;
                    return items.Count + 1;
                }
                return null;
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
                    foreach (object comparison in List(value, "_items", 256))
                    {
                        Step(); Exact(comparison, "Mortal.Core.StatCompareItem");
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
                    foreach (object reference in List(value, "_value", 256))
                    {
                        Step(); Exact(reference, "Mortal.Core.StatValueReference");
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
                }
                finally { active.Remove(value); }
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
                if (maskedFlags != null && maskedFlags.Contains(Name(value))) throw new AffectedInput();
            }
        }
    }
}
