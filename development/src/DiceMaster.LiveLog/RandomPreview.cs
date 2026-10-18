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
    }

    // A separate display model is essential: a random value has no stable int
    // or bool result before gameplay performs its draw. This evaluator never
    // calls GetValue/GetResult/Check/Execute or saves/restores RNG state.
    public static class RandomPreview
    {
        public const string PendingText = "실행 시 판정";
        public const string UnsupportedText = "난수 조건 · 미리보기 미지원";
        public const string ProbabilityLabel = "균등 정수 기준 이론 확률 ";

        private sealed class Identity : IEqualityComparer<object>
        {
            public new bool Equals(object left, object right) { return ReferenceEquals(left, right); }
            public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
        }

        private sealed class ConditionPreview
        {
            internal bool? Verdict;
            internal bool HasRandom;
            internal bool IsSupported = true;
            internal int RandomDraws;
        }

        private sealed class Evaluation
        {
            private readonly HashSet<object> active = new HashSet<object>(new Identity());

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
                Enter(value);
                try
                {
                    IList references = List(value, "_value");
                    int type = Number(value, "_groupType");
                    if (type < 1 || type > 4) throw new InvalidOperationException("Unknown preview group type.");
                    if ((type == 2 || type == 3) && references.Count == 0)
                        throw new InvalidOperationException("Empty preview extrema.");
                    List<PreviewValue> values = new List<PreviewValue>();
                    int draws = 0;
                    // Do not stop at a random leaf: malformed later siblings
                    // are real errors, rather than a random-pending state.
                    foreach (object reference in references)
                    {
                        PreviewValue item = Reference(reference);
                        values.Add(item);
                        draws = checked(draws + item.RandomDraws);
                    }
                    if (draws != 0)
                    {
                        if (type == 1 && values.Count == 1 && values[0].HasRandom && values[0].IsSupported &&
                            Number(references[0], "_checkType") == 7)
                            return values[0];
                        return Unsupported(draws);
                    }
                    int result = type == 4 ? 1 : 0;
                    for (int index = 0; index < values.Count; index++)
                    {
                        int number = values[index].Value;
                        switch (type)
                        {
                            case 1: result = checked(result + number); break;
                            case 2: result = index == 0 ? number : Math.Max(result, number); break;
                            case 3: result = index == 0 ? number : Math.Min(result, number); break;
                            case 4: result = unchecked(result * number); break;
                        }
                    }
                    PreviewValue known = Known(result);
                    known.IsLiteralConstant = type == 1 && references.Count == 1 && Number(references[0], "_checkType") == 0;
                    return known;
                }
                finally { active.Remove(value); }
            }

            private PreviewValue Reference(object value)
            {
                Enter(value);
                try
                {
                    int type = Number(value, "_checkType");
                    if (type < 0 || type > 16) throw new InvalidOperationException("Unknown preview reference type.");
                    if (type == 7)
                    {
                        int minimum = Number(value, "_randomMin");
                        int maximum = Number(value, "_randomMax");
                        // The audited random branch ignores _negative. The
                        // native upper bound is unchecked(maximum + 1), so an
                        // overflow/reversed interval cannot be repaired here.
                        if (minimum > maximum || maximum == int.MaxValue) return Unsupported(1);
                        return new PreviewValue {
                            IsKnown = false, HasRandom = true, IsSupported = true,
                            Minimum = minimum, Maximum = maximum, RandomDraws = 1,
                            Text = "난수 " + minimum.ToString(CultureInfo.InvariantCulture) + "~" +
                                maximum.ToString(CultureInfo.InvariantCulture) + " · " + PendingText
                        };
                    }
                    if (type == 8)
                    {
                        PreviewValue nested = Group(Read(value, "_statGroup"));
                        return nested.HasRandom ? Unsupported(nested.RandomDraws) : nested;
                    }
                    if (type == 16)
                    {
                        ConditionPreview condition = ConditionData(Read(value, "_condition"));
                        return condition.HasRandom ? Unsupported(condition.RandomDraws) : Known(condition.Verdict.Value ? 1 : 0);
                    }
                    return Known(SafeDisplayEvaluation.ReadReference(value));
                }
                finally { active.Remove(value); }
            }

            public PreviewComparison Compare(object value)
            {
                Enter(value);
                try
                {
                    PreviewValue left = Group(Read(value, "_value1"));
                    PreviewValue right = Group(Read(value, "_value2"));
                    int operation = Number(value, "_compareOp");
                    string symbol = Operator(operation);
                    PreviewComparison result = new PreviewComparison {
                        Left = left, Right = right, Operator = symbol,
                        HasRandom = left.HasRandom || right.HasRandom,
                        IsSupported = true, ProbabilityText = string.Empty, Text = string.Empty
                    };
                    if (!result.HasRandom)
                    {
                        result.Verdict = KnownComparison(left.Value, right.Value, operation);
                        return result;
                    }
                    result.IsSupported = left.IsSupported && right.IsSupported && left.HasRandom != right.HasRandom &&
                        checked(left.RandomDraws + right.RandomDraws) == 1;
                    result.Text = result.IsSupported ? PendingText : UnsupportedText;
                    // Even a singleton interval, or a comparison whose integer
                    // count is 0/D or D/D, remains an unexecuted random verdict.
                    result.Verdict = null;
                    if (result.IsSupported)
                    {
                        PreviewValue range = left.HasRandom ? left : right;
                        PreviewValue constant = left.HasRandom ? right : left;
                        if (constant.IsLiteralConstant)
                        {
                            int oriented = left.HasRandom ? operation : Reverse(operation);
                            long denominator = (long)range.Maximum - range.Minimum + 1;
                            long numerator = Favorable(range.Minimum, range.Maximum, constant.Value, oriented);
                            result.ProbabilityNumerator = numerator;
                            result.ProbabilityDenominator = denominator;
                            result.ProbabilityText = ProbabilityLabel + numerator.ToString(CultureInfo.InvariantCulture) + "/" +
                                denominator.ToString(CultureInfo.InvariantCulture);
                        }
                    }
                    return result;
                }
                finally { active.Remove(value); }
            }

            private ConditionPreview Condition(object value)
            {
                Enter(value);
                try
                {
                    IList comparisons = List(value, "_items");
                    List<PreviewComparison> results = new List<PreviewComparison>();
                    ConditionPreview result = new ConditionPreview();
                    foreach (object comparison in comparisons)
                    {
                        PreviewComparison item = Compare(comparison);
                        results.Add(item);
                        result.HasRandom |= item.HasRandom;
                        result.IsSupported &= item.IsSupported;
                        result.RandomDraws = checked(result.RandomDraws + item.Left.RandomDraws + item.Right.RandomDraws);
                    }
                    if (results.Count == 0) { result.Verdict = false; return result; }
                    int logic = Number(value, "_logicOp");
                    if (logic != 0 && logic != 1) throw new InvalidOperationException("Unknown preview logic.");
                    if (result.HasRandom)
                    {
                        result.IsSupported &= results.Count == 1 && result.RandomDraws == 1;
                        result.Verdict = null;
                    }
                    else
                    {
                        bool verdict = results[0].Verdict.Value;
                        for (int index = 1; index < results.Count; index++)
                            verdict = logic == 0 ? verdict & results[index].Verdict.Value : verdict | results[index].Verdict.Value;
                        result.Verdict = verdict;
                    }
                    return result;
                }
                finally { active.Remove(value); }
            }

            private ConditionPreview ConditionData(object value)
            {
                Enter(value);
                try
                {
                    ConditionPreview result = new ConditionPreview { Verdict = false };
                    int count = 0;
                    foreach (object item in List(value, "_items"))
                    {
                        ConditionPreview condition = Condition(item);
                        count++;
                        if (!result.HasRandom && !condition.HasRandom && condition.Verdict.Value) return condition;
                        result.HasRandom |= condition.HasRandom;
                        result.IsSupported &= condition.IsSupported;
                        result.RandomDraws = checked(result.RandomDraws + condition.RandomDraws);
                    }
                    if (result.HasRandom)
                    {
                        result.IsSupported &= count == 1 && result.RandomDraws == 1;
                        result.Verdict = null;
                    }
                    return result;
                }
                finally { active.Remove(value); }
            }

            public bool ContainsRandomCondition(object value) { return Condition(value).HasRandom; }
            public bool? ConditionVerdict(object value) { return Condition(value).Verdict; }

            public PreviewValue Rate(object value)
            {
                Enter(value);
                try
                {
                    int rate = Number(value, "_defaultRate");
                    bool hasRandom = false, supported = true;
                    int draws = 0;
                    if ((bool)Read(value, "_toggleCondition"))
                    {
                        ConditionPreview condition = Condition(Read(value, "_activeCondition"));
                        if (!condition.HasRandom && !condition.Verdict.Value) return Known(0);
                        hasRandom = condition.HasRandom;
                        supported = condition.IsSupported;
                        draws = condition.RandomDraws;
                    }
                    foreach (object item in List(value, "_eventRateItem"))
                    {
                        Enter(item);
                        try
                        {
                            ConditionPreview condition = Condition(Read(item, "Condition"));
                            hasRandom |= condition.HasRandom;
                            supported &= condition.IsSupported;
                            draws = checked(draws + condition.RandomDraws);
                            // A known-false condition never reads its rate in
                            // the original pure path. An unknown condition can
                            // use it later, so validate that field without using
                            // it to fabricate a determined event rate.
                            if (condition.HasRandom) Number(item, "Rate");
                            else if (condition.Verdict.Value) rate = unchecked(rate + Number(item, "Rate"));
                        }
                        finally { active.Remove(item); }
                    }
                    if (!hasRandom) return Known(Math.Max(rate, 0));
                    if (!supported || draws != 1) return Unsupported(draws);
                    return new PreviewValue { IsKnown = false, HasRandom = true, IsSupported = true,
                        RandomDraws = draws, Text = PendingText };
                }
                finally { active.Remove(value); }
            }
        }

        private static PreviewValue Known(int value)
        {
            return new PreviewValue { IsKnown = true, Value = value, IsSupported = true,
                Minimum = value, Maximum = value, Text = value.ToString(CultureInfo.InvariantCulture) };
        }

        private static PreviewValue Unsupported(int draws)
        {
            return new PreviewValue { IsKnown = false, HasRandom = true, IsSupported = false,
                RandomDraws = draws, Text = UnsupportedText };
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

        private static int Reverse(int operation)
        {
            switch (operation)
            {
                case 2: return 4;
                case 3: return 5;
                case 4: return 2;
                case 5: return 3;
                default: return operation;
            }
        }

        private static long Clamp(long count, long total) { return Math.Max(0, Math.Min(count, total)); }

        private static long Favorable(int minimum, int maximum, int threshold, int operation)
        {
            long total = (long)maximum - minimum + 1;
            long below = Clamp((long)threshold - minimum, total);
            long through = Clamp((long)threshold - minimum + 1, total);
            long equal = threshold >= minimum && threshold <= maximum ? 1 : 0;
            switch (operation)
            {
                case 0: return equal;
                case 1: return total - equal;
                case 2: return below;
                case 3: return through;
                case 4: return total - through;
                case 5: return total - below;
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
