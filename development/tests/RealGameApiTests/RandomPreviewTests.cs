using System;
using System.Collections;
using System.Reflection;

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
    static string PreviewProbability(object value) => (string)Member(value, "ProbabilityText");
    static void ProbabilityIs(object value, long numerator, long denominator, string reason)
    {
        Eq(numerator, Member(value, "ProbabilityNumerator"), reason + " numerator");
        Eq(denominator, Member(value, "ProbabilityDenominator"), reason + " denominator");
    }

    static void TestRandomPreview()
    {
        // Observe only the arguments emitted by actual GetValue IL into the
        // explicit RNG fixture. This does not execute Unity's native overload.
        foreach (var bounds in new[] { (1, int.MaxValue, int.MinValue), (9, 3, 4), (4, 4, 5) })
        {
            ResetRandom();
            Call(RandomReference(bounds.Item1, bounds.Item2), "GetValue");
            Eq(bounds.Item1, StaticGet("UnityEngine.Random", "LastMin"), "actual IL preserves configured random lower argument");
            Eq(bounds.Item3, StaticGet("UnityEngine.Random", "LastMax"), "actual IL uses unchecked upper + 1 even for boundary/reversed interval");
        }
        ResetRandom();
        object known = Preview("Group", Group("Sum", Value("常數", 3), Value("常數", 4)));
        Eq(true, PreviewKnown(known), "deterministic preview remains known");
        Eq(7, Member(known, "Value"), "deterministic preview preserves value");
        Eq(false, PreviewRandom(known), "deterministic preview has no random marker");
        object random = RandomReference(1, 100); Set(random, "_negative", true);
        object ranged = Preview("Group", Group("Sum", random));
        Eq(false, PreviewKnown(ranged), "random preview is not an invented current value");
        Eq(true, PreviewRandom(ranged), "random preview keeps a random marker");
        Check(PreviewText(ranged).Contains("1") && PreviewText(ranged).Contains("100"), "random preview retains configured range");
        Check(!PreviewText(ranged).Contains("-100"), "random negative flag is ignored as in supplied IL");
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "range preview never samples RNG");

        object comparison = Preview("Compare", Comparison(Group("Sum", RandomReference(1, 100)), Constant(50), "GreaterEqual"));
        Eq(null, Member(comparison, "Verdict"), "random comparison verdict remains undecided");
        Check(PreviewProbability(comparison).Contains("51/100"), "inclusive 1..100 >= 50 has 51 theoretical successful integers");
        Check(PreviewProbability(comparison).Contains("이론"), "probability is labeled theoretical");
        ProbabilityIs(comparison, 51, 100, "1..100 >= 50 exact theoretical count");
        foreach (object pending in new[] {
            Comparison(Group("Sum", RandomReference(3, 3)), Constant(3)),
            Comparison(Group("Sum", RandomReference(1, 6)), Constant(0), "Greater"),
            Comparison(Group("Sum", RandomReference(1, 6)), Constant(7), "Greater")
        })
        {
            object result = Preview("Compare", pending);
            Eq(null, Member(result, "Verdict"), "singleton and 0/D or D/D models remain unexecuted random verdicts");
            Eq(true, PreviewRandom(result), "trivial theoretical counts retain random provenance");
        }
        object certain = Preview("Compare", Comparison(Constant(2), Constant(2)));
        Eq(true, Member(certain, "Verdict"), "deterministic comparison remains true");
        Eq(false, PreviewRandom(certain), "deterministic comparison has no random marker");

        foreach (var bounds in new[] { (9, 3), (5, 4), (1, int.MaxValue), (int.MaxValue, int.MaxValue) })
        {
            object unsupported = Preview("Group", Group("Sum", RandomReference(bounds.Item1, bounds.Item2)));
            Eq(false, PreviewKnown(unsupported), "invalid/overflow random range is not a number");
            Eq(true, PreviewRandom(unsupported), "invalid random shape remains distinct from a missing data error");
            Check(PreviewText(unsupported).Contains("미지원"), "invalid/overflow range uses explicit unsupported text");
            object unsupportedComparison = Preview("Compare", Comparison(Group("Sum", RandomReference(bounds.Item1, bounds.Item2)), Constant(5)));
            Check(string.IsNullOrEmpty(PreviewProbability(unsupportedComparison)), "invalid/overflow range has no guessed probability");
            ProbabilityIs(unsupportedComparison, 0, 0, "invalid bounds do not invent probability counts");
        }
        object repeated = RandomReference(1, 6);
        object repeatedGroup = Group("Sum", repeated);
        foreach (object unsupported in new[] {
            Comparison(repeatedGroup, repeatedGroup),
            Comparison(Group("Sum", repeated, repeated), Constant(7)),
            Comparison(Group("Multiply", RandomReference(1, 6), Value("常數", 2)), Constant(7))
        })
        {
            object result = Preview("Compare", unsupported);
            Eq(true, PreviewRandom(result), "multiple/repeated/transformed random values remain random");
            Eq(null, Member(result, "Verdict"), "multiple random results do not become a fabricated verdict");
            Check(string.IsNullOrEmpty(PreviewProbability(result)), "multiple/repeated/transformed random structure has no guessed probability");
            ProbabilityIs(result, 0, 0, "multiple random structure does not invent probability counts");
        }
        object nested = Value("數值群組"); Set(nested, "_statGroup", Group("Sum", RandomReference(1, 6)));
        object nestedComparison = Preview("Compare", Comparison(Group("Sum", nested), Constant(3)));
        Check(string.IsNullOrEmpty(PreviewProbability(nestedComparison)), "nested random group has no unproven probability");
        foreach (string operation in new[] { "Max", "Min", "Multiply" })
        {
            object transformed = Preview("Group", Group(operation, RandomReference(1, 6)));
            Eq(false, Member(transformed, "IsSupported"), "non-sum random group requires explicit support " + operation);
            Check(PreviewText(transformed).Contains("미지원"), "non-sum random group has dedicated unsupported state " + operation);
        }
        object stat = New("Mortal.Core.GameStat"); PropertySet(stat, "FinalValue", 3);
        object statValue = Value("角色數值"); Set(statValue, "_stat", stat);
        foreach (object opposite in new[] { Group("Sum", statValue), Group("Sum", Value("常數", 1), Value("常數", 2)), Group("Sum") })
        {
            object nonliteral = Preview("Compare", Comparison(Group("Sum", RandomReference(1, 6)), opposite));
            Eq(null, Member(nonliteral, "Verdict"), "random comparison against current/expression value remains undecided");
            Check(string.IsNullOrEmpty(PreviewProbability(nonliteral)), "nonliteral current/expression value does not qualify as probability constant");
            ProbabilityIs(nonliteral, 0, 0, "nonliteral operand has no invented probability counts");
        }
        object negativeConstant = Value("常數", 2); Set(negativeConstant, "_negative", true);
        ProbabilityIs(Preview("Compare", Comparison(Group("Sum", RandomReference(-3, 3)), Group("Sum", negativeConstant), "GreaterEqual")),
            6, 7, "literal constant negative flag affects threshold");
        object overflowingNegative = Value("常數", int.MinValue); Set(overflowingNegative, "_negative", true);
        ProbabilityIs(Preview("Compare", Comparison(Group("Sum", RandomReference(1, 3)), Group("Sum", overflowingNegative), "GreaterEqual")),
            3, 3, "literal negative sign preserves original unchecked int overflow");

        object conditionData = New("Mortal.Core.ConditionResultData");
        Set(conditionData, "_count", 23);
        Set(conditionData, "_items", List("ConditionResultItem", Condition("AND", Comparison(Group("Sum", RandomReference(1, 100)), Constant(50), "GreaterEqual"))));
        object conditionValue = Value("條件設定"); Set(conditionValue, "_condition", conditionData);
        object conditionPreview = Preview("Group", Group("Sum", conditionValue));
        Eq(false, PreviewKnown(conditionPreview), "condition-backed random value remains unresolved");
        Eq(true, PreviewRandom(conditionPreview), "condition-backed random value preserves marker");
        Eq(23, Get(conditionData, "_count"), "random preview preserves execution counter");
        object conditionalComparison = Preview("Compare", Comparison(Group("Sum", conditionValue), Constant(1)));
        Check(string.IsNullOrEmpty(PreviewProbability(conditionalComparison)), "condition-backed dependent random value has no guessed probability");
        Set(conditionData, "_items", List("ConditionResultItem", True(), null));
        object shortCircuited = Preview("Group", Group("Sum", conditionValue));
        Eq(true, PreviewKnown(shortCircuited), "condition data keeps original first-true short circuit");
        Eq(1, Member(shortCircuited, "Value"), "condition data first true produces one without evaluating later bad item");
        Eq(23, Get(conditionData, "_count"), "short-circuited preview still preserves condition counter");

        object eventPreview = Preview("EventRate", Rate(40, true, Condition("AND", Comparison(Group("Sum", RandomReference(1, 100)), Constant(50), "GreaterEqual"))));
        Eq(false, PreviewKnown(eventPreview), "random event activation does not become a fake rate");
        Eq(true, PreviewRandom(eventPreview), "event preview marks random activation");
        object deterministicRate = Preview("EventRate", Rate(140, true, True()));
        Eq(true, PreviewKnown(deterministicRate), "deterministic event rate remains known");
        Eq(140, Member(deterministicRate, "Value"), "event rate is not clamped to 100");

        Throws<Exception>(() => Preview("Group", Group("Sum", RandomReference(), null)), "malformed group entry after random is still a read error");
        Throws<Exception>(() => Preview("Compare", Comparison(Group("Sum", RandomReference()), null)), "missing right operand is not hidden by random left operand");
        object malformedActive = Condition("AND", Comparison(Group("Sum", RandomReference()), Constant(5)), null);
        Throws<Exception>(() => Preview("EventRate", Rate(40, true, malformedActive)), "eager malformed condition after random remains a read error");
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
        Throws<InvalidOperationException>(() => Safe("ReadGroup", Group("Sum", RandomReference())), "primitive safe numeric API still refuses random value");
        Eq(23, Get(conditionData, "_count"), "all random preview scenarios preserve condition count");
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "all random preview scenarios avoid RNG calls");
        TestRandomProbabilityEnumeration();
        TestPreviewFormatting();
    }

    static bool Satisfies(int left, int right, string operation) => operation switch {
        "Equal" => left == right, "NotEqual" => left != right,
        "Less" => left < right, "LessEqual" => left <= right,
        "Greater" => left > right, "GreaterEqual" => left >= right,
        _ => throw new ArgumentException(operation)
    };

    static void TestRandomProbabilityEnumeration()
    {
        // Brute-force outcome counting is independent of the closed-form
        // production formula and includes singleton and negative intervals.
        foreach (string operation in new[] { "Equal", "NotEqual", "Less", "LessEqual", "Greater", "GreaterEqual" })
            for (int lower = -2; lower <= 2; lower++)
                for (int upper = lower; upper <= 2; upper++)
                    for (int threshold = -4; threshold <= 4; threshold++)
                        foreach (bool reversed in new[] { false, true })
                        {
                            int successful = 0;
                            for (int outcome = lower; outcome <= upper; outcome++)
                                if (reversed ? Satisfies(threshold, outcome, operation) : Satisfies(outcome, threshold, operation)) successful++;
                            object random = Group("Sum", RandomReference(lower, upper));
                            object constant = Constant(threshold);
                            object result = Preview("Compare", reversed ? Comparison(constant, random, operation) : Comparison(random, constant, operation));
                            ProbabilityIs(result, successful, upper - lower + 1,
                                $"brute-force probability {lower}..{upper} {operation} {threshold}, reversed={reversed}");
                        }
        foreach (var test in new[] {
            (int.MinValue, int.MaxValue - 1, int.MinValue, "GreaterEqual", 4294967295L),
            (int.MinValue, int.MaxValue - 1, int.MaxValue, "GreaterEqual", 0L),
            (int.MinValue, int.MaxValue - 1, 0, "Less", 2147483648L),
            (int.MinValue, int.MaxValue - 1, 0, "GreaterEqual", 2147483647L)
        })
        {
            object result = Preview("Compare", Comparison(Group("Sum", RandomReference(test.Item1, test.Item2)), Constant(test.Item3), test.Item4));
            ProbabilityIs(result, test.Item5, 4294967295L, "extreme random interval probability uses 64-bit arithmetic");
        }
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "probability enumeration never calls RNG boundary");
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
    static void TestPreviewFormatting()
    {
        StaticSet("DiceMaster.OptionInfoPatch", "DisplayRefreshDepth", 1);
        StaticSet("DiceMaster.RichText", "Colors", 0);
        ResetRandom();
        object comparison = Comparison(Group("Sum", RandomReference(1, 100)), Constant(50), "GreaterEqual");
        string text = FormatCondition(Condition("AND", comparison), out bool inPeriod);
        Check(text.Contains("난수 1~100") && text.Contains("실행 시 판정"), "formatter shows range and undecided verdict");
        Check(text.Contains("51/100") && text.Contains("이론"), "formatter scopes probability to a supported single comparison");
        Eq(true, inPeriod, "undecided random condition remains visible");
        Eq(0, StaticGet("DiceMaster.RichText", "Colors"), "undecided random comparison has no success/failure color");
        text = FormatCondition(Condition("AND", comparison, Comparison(Group("Sum", RandomReference(1, 100)), Constant(30))), out inPeriod);
        Check(!text.Contains("이론") && !text.Contains("51/100"), "compound condition formatter does not imply combined probability");
        Check(text.Contains("난수 조건 · 미리보기 미지원"), "compound random condition shows explicit unsupported preview state");
        Eq(0, StaticGet("DiceMaster.RichText", "Colors"), "compound random comparisons remain uncolored");
        text = FormatCondition(Condition("AND", Comparison(Group("Sum", RandomReference(5, 2)), Constant(3))), out inPeriod);
        Check(text.Contains("미지원") && !text.Contains("이론"), "unsupported range has dedicated text without probability");
        Eq(0, StaticGet("DiceMaster.RichText", "Colors"), "unsupported range is not colored as a false verdict");
        text = FormatCondition(True(), out inPeriod);
        Check(text.Contains("color=green"), "known true comparison retains success color");
        Eq(1, StaticGet("DiceMaster.RichText", "Colors"), "only known comparison uses verdict color");

        string eventText = FormatEvent(Rate(40, true, Condition("AND", comparison)));
        string firstLine = eventText.Split('\n')[0];
        Check(firstLine.Contains("판정") || firstLine.Contains("미지원"), "random event rate header remains unknown");
        Check(!firstLine.Contains("이론") && !firstLine.Contains("51/100"), "event header does not present comparison probability as trigger rate");
        Check(eventText.Contains("난수 1~100"), "event retains condition range detail");
        object inactiveNullItems = Rate(40, true, False()); Set(inactiveNullItems, "_eventRateItem", null);
        Check(FormatEvent(inactiveNullItems).Split('\n')[0].Contains("0(40)"), "known inactive event preserves zero despite null unused rate array");
        Check(FormatEvent(Rate(40, true, False(), new object[] { null })).Split('\n')[0].Contains("0(40)"), "known inactive event skips unused null rate entry");
        Check(FormatEvent(Rate(40, true, False(), RateItem(5, null))).Split('\n')[0].Contains("0(40)"), "known inactive event skips unused null rate condition");
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "formatter never samples RNG");
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
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "narrative error and recovery never sample RNG");
    }
}
