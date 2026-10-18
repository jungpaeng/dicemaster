using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

static partial class RealGameApiTests
{
    static object Preview(string method, object value)
    {
        try { return T("DiceMaster.LiveLog.RandomPreview").GetMethod(method).Invoke(null, new[] { value }); }
        catch (TargetInvocationException error) { throw error.InnerException ?? error; }
    }
    static object Member(object value, string name)
    {
        var type = value.GetType();
        var field = type.GetField(name);
        return field != null ? field.GetValue(value) : type.GetProperty(name).GetValue(value);
    }
    static bool PreviewKnown(object value) => (bool)Member(value, "IsKnown");
    static bool PreviewRandom(object value) => (bool)Member(value, "HasRandom");
    static string PreviewText(object value) => (string)Member(value, "Text");
    static int PreviewNumber(object value) => (int)Member(value, "Value");

    sealed class CountingRandom : Random
    {
        internal int ByteDraws;
        internal CountingRandom(int seed) : base(seed) { }
        public override void NextBytes(byte[] buffer) { ByteDraws++; base.NextBytes(buffer); }
    }
    sealed class ScriptedRandom : Random
    {
        readonly byte[] patterns;
        internal int Draws;
        internal ScriptedRandom(params byte[] patterns) { this.patterns = patterns; }
        public override void NextBytes(byte[] buffer)
        {
            if (Draws >= patterns.Length) throw new InvalidOperationException("Unexpected display RNG redraw.");
            Array.Fill(buffer, patterns[Draws++]);
        }
    }
    static void InstallPreviewRandom(Random random)
    {
        var field = T("DiceMaster.LiveLog.RandomPreview").GetField("random", BindingFlags.NonPublic | BindingFlags.Static);
        Check(field != null && field.FieldType == typeof(Random) && !field.IsInitOnly,
            "mod owns a private independently seedable System.Random instance");
        field.SetValue(null, random);
    }
    static CountingRandom SeedPreview(int seed)
    {
        var random = new CountingRandom(seed);
        InstallPreviewRandom(random);
        return random;
    }
    static void NumericPreview(object result, int expected, bool random, string reason)
    {
        Eq(true, PreviewKnown(result), reason + " is numeric");
        Eq(true, Member(result, "IsSupported"), reason + " is supported");
        Eq(expected, PreviewNumber(result), reason + " value");
        Eq(random, PreviewRandom(result), reason + " random provenance");
        Eq(expected.ToString(System.Globalization.CultureInfo.InvariantCulture), PreviewText(result), reason + " has bare numeric text");
    }
    static void NoRandomAnnotations(string text, string reason)
    {
        foreach (string forbidden in new[] { "표시용", "실행 시 판정", "미지원", "이론 확률", "난수 조건" })
            Check(!text.Contains(forbidden), reason + " excludes " + forbidden);
    }

    static void TestRandomPreview()
    {
        // Establish what actual managed GetValue sends to the native boundary;
        // the display sampler intentionally normalizes serialized inclusive
        // endpoints rather than reproducing Unity overflow/reversal behavior.
        foreach (var bounds in new[] { (1, int.MaxValue, int.MinValue), (9, 3, 4), (4, 4, 5) })
        {
            ResetRandom();
            Call(RandomReference(bounds.Item1, bounds.Item2), "GetValue");
            Eq(bounds.Item1, StaticGet("UnityEngine.Random", "LastMin"), "actual IL preserves configured random lower argument");
            Eq(bounds.Item3, StaticGet("UnityEngine.Random", "LastMax"), "actual IL uses unchecked upper + 1 even for boundary/reversed interval");
        }
        ResetRandom();
        foreach (var endpoint in new[] { ((byte)0, int.MinValue), ((byte)255, int.MaxValue) })
        {
            var controlled = new ScriptedRandom(endpoint.Item1); InstallPreviewRandom(controlled);
            NumericPreview(Preview("Group", Group("Sum", RandomReference(int.MinValue, int.MaxValue))), endpoint.Item2, true,
                "full integer interval reaches endpoint with controlled independent bytes");
            Eq(1, controlled.Draws, "full integer interval endpoint needs one managed draw");
        }
        var rejection = new ScriptedRandom(255, 0); InstallPreviewRandom(rejection);
        NumericPreview(Preview("Group", Group("Sum", RandomReference(0, 2))), 0, true,
            "three-outcome range rejects incomplete modulo bucket then accepts zero");
        Eq(2, rejection.Draws, "sampler rejects the biased uint tail rather than folding it into an outcome");
        SeedPreview(104729);
        NumericPreview(Preview("Group", Group("Sum", Value("常數", 3), Value("常數", 4))), 7, false, "deterministic preview");
        object positive = RandomReference(7, 7); Set(positive, "_negative", true);
        NumericPreview(Preview("Group", Group("Sum", positive)), 7, true, "random ignores negative flag as actual IL does");
        var distinct = new HashSet<int>();
        foreach (var bounds in new[] { (-9, -3), (1, 100), (9, 3), (5, 4), (int.MinValue, int.MaxValue), (int.MaxValue, int.MaxValue), (int.MinValue, int.MinValue) })
            for (int iteration = 0; iteration < 32; iteration++)
            {
                object result = Preview("Group", Group("Sum", RandomReference(bounds.Item1, bounds.Item2)));
                Eq(true, PreviewKnown(result), "supported random endpoints yield a number");
                Eq(true, PreviewRandom(result), "random endpoints preserve provenance without annotation");
                int number = PreviewNumber(result);
                Check(number >= Math.Min(bounds.Item1, bounds.Item2) && number <= Math.Max(bounds.Item1, bounds.Item2),
                    "sample is inside normalized inclusive integer endpoints");
                NoRandomAnnotations(PreviewText(result), "supported random value");
                if (bounds == (1, 100)) distinct.Add(number);
            }
        Check(distinct.Count > 1, "seeded independent display RNG varies across fresh snapshots");
        CountingRandom replay = SeedPreview(97);
        int first = PreviewNumber(Preview("Group", Group("Sum", RandomReference(int.MinValue, int.MaxValue))));
        Check(replay.ByteDraws > 0, "sampling uses the mod-owned RNG instance");
        SeedPreview(97);
        Eq(first, PreviewNumber(Preview("Group", Group("Sum", RandomReference(int.MinValue, int.MaxValue)))),
            "a seeded display generator replays without using Unity state");

        foreach (var operation in new[] { ("Sum", 3), ("Max", 4), ("Min", -3), ("Multiply", -24) })
            NumericPreview(Preview("Group", Group(operation.Item1, RandomReference(-3, -3), Value("常數", 4), RandomReference(2, 2))),
                operation.Item2, true, "compound random " + operation.Item1);
        NumericPreview(Preview("Group", Group("Sum")), 0, false, "empty sum");
        NumericPreview(Preview("Group", Group("Multiply")), 1, false, "empty product");
        NumericPreview(Preview("Group", Group("Multiply", RandomReference(int.MaxValue, int.MaxValue), Value("常數", 2))),
            -2, true, "sampled multiplication preserves unchecked overflow");
        Throws<OverflowException>(() => Preview("Group", Group("Sum", RandomReference(int.MaxValue, int.MaxValue), Value("常數", 1))),
            "sampled sum preserves checked overflow failure");
        object nested = Value("數值群組"); Set(nested, "_statGroup", Group("Multiply", RandomReference(3, 3), Value("常數", 4))); Set(nested, "_negative", true);
        NumericPreview(Preview("Group", Group("Sum", nested, Value("常數", 1))), 13, true, "nested random arithmetic ignores outer negative as actual IL does");
        foreach (string operation in new[] { "Equal", "NotEqual", "Less", "LessEqual", "Greater", "GreaterEqual" })
            for (int left = -3; left <= 3; left++)
                for (int right = -3; right <= 3; right++)
                {
                    object result = Preview("Compare", Comparison(Group("Sum", RandomReference(left, left)), Constant(right), operation));
                    Eq(Satisfies(left, right, operation), Member(result, "Verdict"), "numeric random comparison " + operation);
                    Eq(left, Member(Member(result, "Left"), "Value"), "comparison stores the sampled left number");
                    Eq(right, Member(Member(result, "Right"), "Value"), "comparison stores its right number");
                    Eq("", Member(result, "ProbabilityText"), "sampled comparison has no theoretical probability suffix");
                }
        object repeated = RandomReference(1, 100), repeatedGroup = Group("Sum", repeated);
        for (int iteration = 0; iteration < 20; iteration++)
        {
            object equality = Preview("Compare", Comparison(repeatedGroup, Group("Sum", repeated)));
            Eq(true, Member(equality, "Verdict"), "same random reference reuses one sample within a comparison snapshot");
            Eq(Member(Member(equality, "Left"), "Value"), Member(Member(equality, "Right"), "Value"), "snapshot aliases show one consistent number");
        }
        object stat = New("Mortal.Core.GameStat"); PropertySet(stat, "FinalValue", 3);
        object statReference = Value("角色數值"); Set(statReference, "_stat", stat);
        object statGroup = Group("Sum", statReference, RandomReference(2, 2));
        object statComparison = Comparison(statGroup, statGroup);
        Eq(true, Member(Preview("Compare", statComparison), "Verdict"), "actual game stat getter participates in consistent sampled comparison");
        Eq(1, Get(stat, "FinalReads"), "shared source getter is read once per snapshot");
        PropertySet(stat, "FinalValue", 10);
        Eq(12, Member(Member(Preview("Compare", statComparison), "Left"), "Value"), "next snapshot reads fresh deterministic game state");
        Eq(2, Get(stat, "FinalReads"), "getter cache does not survive the next refresh");

        TestRandomConditionCounters();
        object eventPreview = Preview("EventRate", Rate(40, true, Condition("AND", Comparison(Group("Sum", RandomReference(1, 1)), Constant(1)))));
        NumericPreview(eventPreview, 40, true, "sampled active event rate");
        NumericPreview(Preview("EventRate", Rate(140, true, True())), 140, false, "deterministic rate remains unclamped at 100");
        NumericPreview(Preview("EventRate", Rate(40, true, Condition("AND", Comparison(Group("Sum", RandomReference(1, 1)), Constant(2))))),
            0, true, "sampled false activation makes zero rate");
        NumericPreview(Preview("EventRate", Rate(40, false, null, RateItem(7, Condition("AND", Comparison(Group("Sum", RandomReference(2, 2)), Constant(2)))))),
            47, true, "sampled rate item affects numeric rate");

        Throws<Exception>(() => Preview("Group", Group("Sum", RandomReference(), null)), "malformed group entry after random remains a read error");
        Throws<Exception>(() => Preview("Compare", Comparison(Group("Sum", RandomReference()), null)), "missing right operand is not hidden by random left operand");
        Throws<Exception>(() => Preview("EventRate", Rate(40, true, Condition("AND", Comparison(Group("Sum", RandomReference()), Constant(5)), null))),
            "eager malformed condition after random remains a read error");
        object eagerTrue = Condition("OR", Comparison(Constant(1), Constant(1)), Comparison(Group("Sum", RandomReference()), Constant(5)), null);
        Throws<Exception>(() => Preview("EventRate", Rate(40, true, eagerTrue)), "logical OR true does not hide eager later random/error comparison");
        object cycleReference = Value("數值群組");
        object cycleGroup = Group("Sum", RandomReference(), cycleReference); Set(cycleReference, "_statGroup", cycleGroup);
        Throws<Exception>(() => Preview("Group", cycleGroup), "random value does not hide nested group cycle");
        object invalidOperation = Comparison(Group("Sum", RandomReference()), Constant(5));
        Set(invalidOperation, "_compareOp", Enum.ToObject(T("Mortal.Core.StatCompareType"), 999));
        Throws<InvalidOperationException>(() => Preview("Compare", invalidOperation), "random operand does not hide invalid comparison operator");
        object invalidGroup = Group("Sum", RandomReference()); Set(invalidGroup, "_groupType", Enum.ToObject(T("Mortal.Core.StatGroupType"), 999));
        Throws<InvalidOperationException>(() => Preview("Group", invalidGroup), "random operand does not hide invalid group operator");
        Throws<InvalidOperationException>(() => Safe("ReadGroup", Group("Sum", RandomReference())), "strict primitive API still refuses random value");
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "all sampled preview scenarios avoid game Unity RNG calls");
        TestPreviewFormatting();
    }

    static bool Satisfies(int left, int right, string operation) => operation switch {
        "Equal" => left == right, "NotEqual" => left != right,
        "Less" => left < right, "LessEqual" => left <= right,
        "Greater" => left > right, "GreaterEqual" => left >= right,
        _ => throw new ArgumentException(operation)
    };

    static void TestRandomConditionCounters()
    {
        object inner = New("Mortal.Core.ConditionResultData"); Set(inner, "_count", 23);
        Set(inner, "_items", List("ConditionResultItem", Condition("AND", Comparison(Group("Sum", RandomReference(1, 1)), Constant(1)))));
        object innerValue = Value("條件設定"); Set(innerValue, "_condition", inner);
        object outer = New("Mortal.Core.ConditionResultData"); Set(outer, "_count", 41);
        Set(outer, "_items", List("ConditionResultItem", Condition("OR", Comparison(Group("Sum", innerValue), Constant(1)),
            Comparison(Group("Multiply", RandomReference(-2, -2), Value("常數", 3)), Constant(-6)))));
        object outerValue = Value("條件設定"); Set(outerValue, "_condition", outer);
        for (int iteration = 0; iteration < 8; iteration++)
        {
            NumericPreview(Preview("Group", Group("Sum", outerValue)), 1, true, "nested random condition-backed value");
            Eq(23, Get(inner, "_count"), "nested inner execution count unchanged");
            Eq(41, Get(outer, "_count"), "nested outer execution count unchanged");
        }
        Set(inner, "_items", List("ConditionResultItem", True(), null));
        NumericPreview(Preview("Group", Group("Sum", innerValue)), 1, false, "condition data preserves first-true short circuit");
        Eq(23, Get(inner, "_count"), "short-circuited preview does not increment count");
    }

    static string FormatCondition(object condition, out bool inPeriod)
    {
        object[] arguments = { condition, false };
        object result;
        try { result = T("DiceMaster.LiveLog.PreviewFormatting").GetMethod("ExtractConditions").Invoke(null, arguments); }
        catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        inPeriod = (bool)arguments[1];
        return (string)result;
    }
    static string FormatEvent(object data)
    {
        try { return (string)T("DiceMaster.LiveLog.PreviewFormatting").GetMethod("EventDescription").Invoke(null, new[] { data }); }
        catch (TargetInvocationException error) { throw error.InnerException ?? error; }
    }
    static int[] FormattedNumbers(string text)
    {
        var matches = Regex.Matches(text, @"fixture\((-?\d+)\)");
        var numbers = new int[matches.Count];
        for (int index = 0; index < numbers.Length; index++) numbers[index] = int.Parse(matches[index].Groups[1].Value);
        return numbers;
    }
    static void TestPreviewFormatting()
    {
        StaticSet("DiceMaster.OptionInfoPatch", "DisplayRefreshDepth", 1);
        StaticSet("DiceMaster.RichText", "Colors", 0);
        ResetRandom();
        object comparison = Comparison(Group("Sum", RandomReference(1, 100)), Constant(50), "GreaterEqual");
        for (int seed = 100; seed < 120; seed++)
        {
            CountingRandom rng = SeedPreview(seed);
            string rendered = FormatCondition(Condition("AND", comparison), out bool visible);
            int[] values = FormattedNumbers(rendered);
            Eq(2, values.Length, "single comparison renders numeric operands");
            Check(rendered.Contains(values[0] >= 50 ? "color=green" : "color=red"), "color matches the displayed sampled number");
            Eq(true, visible, "non-time sampled comparison stays visible");
            NoRandomAnnotations(rendered, "sampled comparison text");
            Check(rng.ByteDraws > 0, "formatted comparison uses independent display RNG");
        }
        object timeGroup = Group("Sum", RandomReference(1, 100)); Set(timeGroup, "name", "時間_fixture");
        object timeComparison = Comparison(timeGroup, Constant(50), "GreaterEqual");
        SeedPreview(702);
        Preview("Compare", timeComparison);
        // Measure one seeded sample separately, then require visibility and color
        // formatting to consume precisely the same sample, without another draw.
        CountingRandom baseline = SeedPreview(702); Preview("Compare", timeComparison); int draws = baseline.ByteDraws;
        CountingRandom formatted = SeedPreview(702);
        string text = FormatCondition(Condition("AND", timeComparison), out bool inPeriod);
        int[] displayed = FormattedNumbers(text);
        Eq(displayed[0] >= 50, inPeriod, "time visibility uses the displayed comparison verdict");
        Eq(draws, formatted.ByteDraws, "time visibility does not redraw the comparison");

        object sharedReference = RandomReference(1, 100);
        object shared = Comparison(Group("Sum", sharedReference), Constant(50), "GreaterEqual");
        object sharedCondition = Condition("AND", shared);
        for (int seed = 400; seed < 420; seed++)
        {
            CountingRandom single = SeedPreview(seed); Preview("Compare", shared); int singleDraws = single.ByteDraws;
            CountingRandom eventRng = SeedPreview(seed);
            string description = FormatEvent(Rate(40, true, sharedCondition, RateItem(5, sharedCondition)));
            int[] values = FormattedNumbers(description);
            Eq(4, values.Length, "event renders active and rate-item numeric operands");
            Eq(values[0], values[2], "event header/active/rate detail share one condition snapshot");
            string header = description.Split('\n')[0];
            Check(header.Contains((values[0] >= 50 ? "45" : "0") + "(40)"), "header rate agrees with printed active and rate-item verdicts");
            Eq(singleDraws, eventRng.ByteDraws, "same shared reference is sampled only once for an entire event description");
            Eq(2, Regex.Matches(description, values[0] >= 50 ? "color=green" : "color=red").Count,
                "active and rate-item colors both match their printed sample");
            NoRandomAnnotations(description, "event description");
        }
        object independentA = Condition("AND", Comparison(Group("Sum", RandomReference(1, 100)), Constant(50), "GreaterEqual"));
        object independentB = Condition("AND", Comparison(Group("Sum", RandomReference(1, 100)), Constant(25), "LessEqual"));
        bool sawDistinct = false;
        for (int seed = 800; seed < 820; seed++)
        {
            SeedPreview(seed);
            string description = FormatEvent(Rate(40, true, independentA, RateItem(5, independentB)));
            int[] values = FormattedNumbers(description);
            Eq(4, values.Length, "event renders distinct random conditions");
            sawDistinct |= values[0] != values[2];
            int expectedRate = values[0] >= 50 ? 40 + (values[2] <= 25 ? 5 : 0) : 0;
            Check(description.Split('\n')[0].Contains(expectedRate + "(40)"), "event header agrees with two independently displayed conditions");
            Check(description.Contains((values[0] >= 50 ? "color=green>" : "color=red>") + "fixture(" + values[0] + ")"), "active color agrees with active sample");
            Check(description.Contains((values[2] <= 25 ? "color=green>" : "color=red>") + "fixture(" + values[2] + ")"), "rate-item color agrees with item sample");
        }
        Check(sawDistinct, "distinct reference objects sample independently within one event snapshot");
        text = FormatCondition(Condition("AND", comparison, Comparison(Group("Multiply", RandomReference(2, 2), Value("常數", 3)), Constant(6))), out inPeriod);
        Check(text.Contains("fixture(6)") && text.Contains("color=green"), "compound random conditions produce numeric colored rows");
        NoRandomAnnotations(text, "compound random text");
        text = FormatCondition(Condition("AND", Comparison(Group("Sum", RandomReference(5, 2)), Constant(3))), out inPeriod);
        NoRandomAnnotations(text, "normalized reversed range text");
        object inactiveNullItems = Rate(40, true, False()); Set(inactiveNullItems, "_eventRateItem", null);
        Check(FormatEvent(inactiveNullItems).Split('\n')[0].Contains("0(40)"), "inactive event preserves zero despite null unused rate array");
        Check(FormatEvent(Rate(40, true, False(), new object[] { null })).Split('\n')[0].Contains("0(40)"), "inactive event skips unused null rate entry");
        Check(FormatEvent(Rate(40, true, False(), RateItem(5, null))).Split('\n')[0].Contains("0(40)"), "inactive event skips unused null rate condition");
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "numeric formatter never calls Unity RNG");
        object malformed = Condition("AND"); Set(malformed, "_items", null);
        Throws<InvalidOperationException>(() => FormatCondition(malformed, out _), "formatter preserves malformed condition as actual error");
        Throws<Exception>(() => FormatCondition(Condition("AND", comparison, null), out _), "formatter does not hide later malformed entry behind random text");
        StaticSet("DiceMaster.OptionInfoPatch", "DisplayRefreshDepth", 0);
        var warnings = (IList)StaticGet("DiceMaster.DicePlugin", "Warnings"); warnings.Clear();
        text = FormatCondition(malformed, out inPeriod);
        Eq("읽기 실패", text, "unprotected narrative formatter returns explicit read failure instead of throwing into gameplay");
        Eq(true, inPeriod, "unprotected description failure does not hide a mission via its period out parameter");
        Eq(1, warnings.Count, "first narrative description failure logs once");
        Eq("읽기 실패", FormatCondition(malformed, out _), "repeated narrative error remains explicit failure text");
        Eq(1, warnings.Count, "repeated identical narrative error is deduplicated");
        Set(malformed, "_items", List("StatCompareItem", Comparison(Constant(1), Constant(1))));
        Check(FormatCondition(malformed, out _).Contains("color=green"), "same narrative source can recover to a known description");
        Eq(2, warnings.Count, "narrative recovery logs once");
        Check(((string)warnings[0]).Contains("failure") && ((string)warnings[1]).Contains("recovered"), "narrative diagnostics distinguish failure and recovery");
        FormatCondition(malformed, out _);
        Eq(2, warnings.Count, "repeated narrative success does not repeat recovery diagnostic");
        Set(malformed, "_items", null);
        StaticSet("DiceMaster.OptionInfoPatch", "DisplayRefreshDepth", 1);
        Throws<InvalidOperationException>(() => FormatCondition(malformed, out _), "protected row still propagates actual error after narrative recovery");
        Eq(2, warnings.Count, "protected row error is owned by engine and does not duplicate narrative diagnostics");
        StaticSet("DiceMaster.OptionInfoPatch", "DisplayRefreshDepth", 0);
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "narrative error and recovery never call Unity RNG");
    }
}
