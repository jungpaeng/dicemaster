using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DiceMaster.LiveLog
{
    public sealed class PreviewValue
    {
        public bool IsKnown;
        public int Value;
        public bool HasRandom;
        public string Text;
        public bool IsSupported;
        public int Minimum;
        public int Maximum;
        public int RandomDraws;
        internal bool IsLiteralConstant;
        internal HashSet<object> RandomSources;
    }

    public sealed class PreviewComparison
    {
        public PreviewValue Left;
        public PreviewValue Right;
        public bool? Verdict;
        public string Operator;
        public string ProbabilityText;
        public bool HasRandom;
        public bool IsSupported;
        public string Text;
        public long ProbabilityNumerator;
        public long ProbabilityDenominator;
        internal HashSet<object> RandomSources;
    }

    // Display snapshots sample their own RNG. No game GetValue/GetResult/Check/
    // Execute method, Unity RNG entry, or RNG-state getter/setter is called.
    public static class RandomPreview
    {
        private static readonly object randomGate = new object();
        private static System.Random random = new System.Random();

        private sealed class Identity : IEqualityComparer<object>
        {
            public new bool Equals(object left, object right) { return ReferenceEquals(left, right); }
            public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
        }

        private sealed class ConditionPreview
        {
            internal bool Verdict;
            internal HashSet<object> RandomSources;
        }

        // Each standalone condition item or complete event description has one
        // session. Values, colors and the event header reuse the same results.
        // Shared reference instances draw once per session; separate instances
        // draw independently, even with identical bounds.
        internal sealed class Evaluation
        {
            private readonly HashSet<object> active = new HashSet<object>(new Identity());
            private readonly Dictionary<object, PreviewValue> groups = new Dictionary<object, PreviewValue>(new Identity());
            private readonly Dictionary<object, PreviewValue> references = new Dictionary<object, PreviewValue>(new Identity());
            private readonly Dictionary<object, PreviewComparison> comparisons = new Dictionary<object, PreviewComparison>(new Identity());
            private readonly Dictionary<object, ConditionPreview> conditions = new Dictionary<object, ConditionPreview>(new Identity());
            private readonly Dictionary<object, ConditionPreview> conditionData = new Dictionary<object, ConditionPreview>(new Identity());
            private readonly Dictionary<object, PreviewValue> rates = new Dictionary<object, PreviewValue>(new Identity());

            private static object Read(object value, string name) { return LogBridge.ReadFieldStrict(value, name); }
            private static int Number(object value, string name) { return Convert.ToInt32(Read(value, name)); }
            private static IList List(object value, string name)
            {
                IList items = Read(value, name) as IList;
                if (items == null) throw new InvalidOperationException("Invalid preview collection.");
                return items;
            }

            private void Enter(object value)
            {
                if (value == null || active.Count >= 128 || !active.Add(value))
                    throw new InvalidOperationException("Invalid or cyclic preview data.");
            }

            public PreviewValue Group(object value)
            {
                PreviewValue cached;
                if (value != null && groups.TryGetValue(value, out cached)) return cached;
                Enter(value);
                try
                {
                    IList items = List(value, "_value");
                    int type = Number(value, "_groupType");
                    if (type < 1 || type > 4) throw new InvalidOperationException("Unknown preview group type.");
                    if ((type == 2 || type == 3) && items.Count == 0)
                        throw new InvalidOperationException("Empty preview extrema.");
                    List<PreviewValue> values = new List<PreviewValue>();
                    foreach (object reference in items) values.Add(Reference(reference));
                    int result = type == 4 ? 1 : 0;
                    HashSet<object> sources = null;
                    for (int index = 0; index < values.Count; index++)
                    {
                        PreviewValue item = values[index];
                        sources = Merge(sources, item.RandomSources);
                        switch (type)
                        {
                            case 1: result = checked(result + item.Value); break;
                            case 2: result = index == 0 ? item.Value : Math.Max(result, item.Value); break;
                            case 3: result = index == 0 ? item.Value : Math.Min(result, item.Value); break;
                            case 4: result = unchecked(result * item.Value); break;
                        }
                    }
                    PreviewValue known = Known(result, sources);
                    known.IsLiteralConstant = type == 1 && items.Count == 1 && Number(items[0], "_checkType") == 0;
                    groups.Add(value, known);
                    return known;
                }
                finally { active.Remove(value); }
            }

            private PreviewValue Reference(object value)
            {
                PreviewValue cached;
                if (value != null && references.TryGetValue(value, out cached)) return cached;
                Enter(value);
                try
                {
                    int type = Number(value, "_checkType");
                    if (type < 0 || type > 16) throw new InvalidOperationException("Unknown preview reference type.");
                    PreviewValue result;
                    if (type == 7)
                    {
                        int first = Number(value, "_randomMin"), second = Number(value, "_randomMax");
                        int minimum = Math.Min(first, second), maximum = Math.Max(first, second);
                        // These are mod inclusive bounds, including Int32.MaxValue.
                        // Reversed endpoints are normalized; the original game's
                        // unchecked(maximum + 1) overflow/reversal is not reproduced.
                        // Like the audited random branch, _negative is ignored.
                        HashSet<object> sources = new HashSet<object>(new Identity());
                        sources.Add(value);
                        result = Known(Sample(minimum, maximum), sources);
                        result.Minimum = minimum;
                        result.Maximum = maximum;
                    }
                    else if (type == 8)
                    {
                        PreviewValue nested = Group(Read(value, "_statGroup"));
                        result = Known(nested.Value, nested.RandomSources);
                    }
                    else if (type == 16)
                    {
                        ConditionPreview condition = ConditionData(Read(value, "_condition"));
                        result = Known(condition.Verdict ? 1 : 0, condition.RandomSources);
                    }
                    else result = Known(SafeDisplayEvaluation.ReadReference(value), null);
                    references.Add(value, result);
                    return result;
                }
                finally { active.Remove(value); }
            }

            public PreviewComparison Compare(object value)
            {
                PreviewComparison cached;
                if (value != null && comparisons.TryGetValue(value, out cached)) return cached;
                Enter(value);
                try
                {
                    PreviewValue left = Group(Read(value, "_value1"));
                    PreviewValue right = Group(Read(value, "_value2"));
                    int operation = Number(value, "_compareOp");
                    HashSet<object> sources = Merge(left.RandomSources, right.RandomSources);
                    PreviewComparison result = new PreviewComparison {
                        Left = left, Right = right, Operator = Operator(operation),
                        Verdict = KnownComparison(left.Value, right.Value, operation),
                        HasRandom = sources != null && sources.Count != 0,
                        IsSupported = true, ProbabilityText = string.Empty, Text = string.Empty,
                        RandomSources = sources
                    };
                    comparisons.Add(value, result);
                    return result;
                }
                finally { active.Remove(value); }
            }

            private ConditionPreview Condition(object value)
            {
                ConditionPreview cached;
                if (value != null && conditions.TryGetValue(value, out cached)) return cached;
                Enter(value);
                try
                {
                    IList items = List(value, "_items");
                    List<PreviewComparison> results = new List<PreviewComparison>();
                    HashSet<object> sources = null;
                    // The game's item evaluates every comparison before its AND/OR.
                    foreach (object comparison in items)
                    {
                        PreviewComparison result = Compare(comparison);
                        results.Add(result);
                        sources = Merge(sources, result.RandomSources);
                    }
                    bool verdict = false;
                    if (results.Count != 0)
                    {
                        int logic = Number(value, "_logicOp");
                        if (logic != 0 && logic != 1) throw new InvalidOperationException("Unknown preview logic.");
                        verdict = results[0].Verdict.Value;
                        for (int index = 1; index < results.Count; index++)
                            verdict = logic == 0 ? verdict & results[index].Verdict.Value : verdict | results[index].Verdict.Value;
                    }
                    ConditionPreview known = new ConditionPreview { Verdict = verdict, RandomSources = sources };
                    conditions.Add(value, known);
                    return known;
                }
                finally { active.Remove(value); }
            }

            private ConditionPreview ConditionData(object value)
            {
                ConditionPreview cached;
                if (value != null && conditionData.TryGetValue(value, out cached)) return cached;
                Enter(value);
                try
                {
                    ConditionPreview result = new ConditionPreview();
                    // Preserve Execute's short circuit without its _count write.
                    foreach (object item in List(value, "_items"))
                    {
                        ConditionPreview condition = Condition(item);
                        result.RandomSources = Merge(result.RandomSources, condition.RandomSources);
                        if (condition.Verdict) { result.Verdict = true; break; }
                    }
                    conditionData.Add(value, result);
                    return result;
                }
                finally { active.Remove(value); }
            }

            public bool ContainsRandomCondition(object value)
            {
                HashSet<object> sources = Condition(value).RandomSources;
                return sources != null && sources.Count != 0;
            }
            public bool? ConditionVerdict(object value) { return Condition(value).Verdict; }

            public PreviewValue Rate(object value)
            {
                PreviewValue cached;
                if (value != null && rates.TryGetValue(value, out cached)) return cached;
                Enter(value);
                try
                {
                    int rate = Number(value, "_defaultRate");
                    HashSet<object> sources = null;
                    if ((bool)Read(value, "_toggleCondition"))
                    {
                        ConditionPreview condition = Condition(Read(value, "_activeCondition"));
                        sources = Merge(sources, condition.RandomSources);
                        if (!condition.Verdict)
                        {
                            PreviewValue inactive = Known(0, sources);
                            rates.Add(value, inactive);
                            return inactive;
                        }
                    }
                    foreach (object item in List(value, "_eventRateItem"))
                    {
                        Enter(item);
                        try
                        {
                            ConditionPreview condition = Condition(Read(item, "Condition"));
                            sources = Merge(sources, condition.RandomSources);
                            if (condition.Verdict) rate = unchecked(rate + Number(item, "Rate"));
                        }
                        finally { active.Remove(item); }
                    }
                    PreviewValue result = Known(Math.Max(rate, 0), sources);
                    rates.Add(value, result);
                    return result;
                }
                finally { active.Remove(value); }
            }
        }

        private static int Sample(int minimum, int maximum)
        {
            ulong span = (ulong)((long)maximum - minimum + 1L);
            const ulong universe = 1UL << 32;
            ulong limit = universe - universe % span;
            byte[] bytes = new byte[4];
            lock (randomGate)
            {
                ulong sample;
                do
                {
                    random.NextBytes(bytes);
                    sample = (uint)bytes[0] | ((uint)bytes[1] << 8) | ((uint)bytes[2] << 16) | ((uint)bytes[3] << 24);
                } while (sample >= limit);
                return (int)((long)minimum + (long)(sample % span));
            }
        }

        private static HashSet<object> Merge(HashSet<object> first, HashSet<object> second)
        {
            if ((first == null || first.Count == 0) && (second == null || second.Count == 0)) return null;
            HashSet<object> result = first == null ? new HashSet<object>(new Identity()) : new HashSet<object>(first, new Identity());
            if (second != null) result.UnionWith(second);
            return result;
        }

        private static PreviewValue Known(int value, HashSet<object> sources)
        {
            return new PreviewValue { IsKnown = true, Value = value, IsSupported = true,
                HasRandom = sources != null && sources.Count != 0, RandomSources = sources,
                RandomDraws = sources == null ? 0 : sources.Count,
                Minimum = value, Maximum = value, Text = value.ToString(CultureInfo.InvariantCulture) };
        }

        private static string Operator(int operation)
        {
            switch (operation)
            {
                case 0: return "==";
                case 1: return "!=";
                case 2: return "<";
                case 3: return "<=";
                case 4: return ">";
                case 5: return ">=";
                default: throw new InvalidOperationException("Unknown preview comparison.");
            }
        }

        private static bool KnownComparison(int left, int right, int operation)
        {
            switch (operation)
            {
                case 0: return left == right;
                case 1: return left != right;
                case 2: return left < right;
                case 3: return left <= right;
                case 4: return left > right;
                case 5: return left >= right;
                default: throw new InvalidOperationException("Unknown preview comparison.");
            }
        }

        public static PreviewValue Group(object value) { return new Evaluation().Group(value); }
        public static PreviewComparison Compare(object value) { return new Evaluation().Compare(value); }
        public static PreviewValue EventRate(object value) { return new Evaluation().Rate(value); }
        public static bool ContainsRandomCondition(object value) { return new Evaluation().ContainsRandomCondition(value); }
        public static bool? ConditionVerdict(object value) { return new Evaluation().ConditionVerdict(value); }
    }
}
