using System;
using System.Collections;
using System.Linq;
using System.Reflection;

static partial class RealGameApiTests
{
    static string PopupCondition(object condition) => (string)T("DiceMaster.EventSelection.PopupConditionText")
        .GetMethod("Condition").Invoke(null, new[] { condition });
    static string PopupEvent(object data, int? rate) => (string)T("DiceMaster.EventSelection.PopupConditionText")
        .GetMethod("Event", new[] { typeof(object), typeof(int?) }).Invoke(null, new object[] { data, rate });

    static void TestPopupConditionText()
    {
        ResetRandom();
        CountingRandom sampledPreview = SeedPreview(123);
        int expandedNames = (int)StaticGet("DiceMaster.DataHelper", "ExpandedNameReads");
        object six = Condition("AND", Enumerable.Range(101, 6).Select(number => Comparison(Constant(number), Constant(number + 100))).ToArray());
        string text = PopupCondition(six);
        foreach (int number in Enumerable.Range(101, 6))
            Check(text.Contains("현재 " + number) && text.Contains("현재 " + (number + 100)), "popup retains every comparison beyond previous three-row summary limit");
        Eq(5, text.Split(" AND ").Length - 1, "popup preserves all AND operators");
        object namedStat = Constant(7); Set(namedStat, "name", "數值_學問");
        Check(PopupCondition(Condition("AND", Comparison(namedStat, Constant(8)))).Contains("학문"), "popup preserves friendly stat names through finite literal translation");
        object namedTime = Constant(9); Set(namedTime, "name", "目前時間");
        Check(PopupCondition(Condition("AND", Comparison(namedTime, Constant(10)))).Contains("현재 시간"), "popup preserves friendly time names without expression expansion");
        object namedFlag = Constant(11); Set(namedFlag, "name", "旗標_PopupFlag001");
        Check(PopupCondition(Condition("AND", Comparison(namedFlag, Constant(12)))).Contains("자기 참조 GetFlag('PopupFlag001')(PopupFlag001)"),
            "translated flag label containing a self reference remains literal and keeps flag ID");
        Set(namedFlag, "name", "GetFlag('PopupFlag001')");
        Check(PopupCondition(Condition("AND", Comparison(namedFlag, Constant(12)))).Contains("GetFlag('PopupFlag001'):"),
            "raw GetFlag expression is retained without recursive expansion");
        Set(namedFlag, "name", "FlagNameUnavailable");
        Check(PopupCondition(Condition("AND", Comparison(namedFlag, Constant(12)))).Contains("FlagNameUnavailable:"), "unavailable literal name translation preserves original identifier");
        Check(PopupCondition(Condition("OR", Comparison(Constant(7), Constant(8)), Comparison(Constant(9), Constant(10))))
            .Contains(" OR "), "popup distinguishes OR from AND");
        Check(PopupCondition(Condition("AND")).Contains("조건 없음 (불충족)"), "existing empty condition is explicitly false");

        object random = Condition("AND", Comparison(Group("Sum", RandomReference(0, 30)), Constant(10), "LessEqual"));
        text = PopupCondition(random);
        Check(text.Contains("랜덤(0~30)") && text.Contains("미확정") && text.Contains("<="), "popup uses original random bounds, operator and unknown verdict");
        object nested = Value("數值群組"); Set(nested, "_statGroup", Group("Multiply", RandomReference(-2, 3), Value("常數", 4)));
        text = PopupCondition(Condition("AND", Comparison(Group("Sum", nested, Value("常數", int.MaxValue)), Constant(0))));
        Check(text.Contains("랜덤(-2~3)") && text.Contains(" × ") && text.Contains(" + ") && text.Contains("2147483647"),
            "popup preserves nested arithmetic rather than inventing transformed random bounds");
        Check(PopupCondition(Condition("AND", Comparison(Group("Sum", RandomReference(5, 2)), Constant(0))))
            .Contains("잘못된 범위"), "reversed configured random bounds are explicit unsupported data");
        Check(PopupCondition(Condition("AND", Comparison(Group("Sum", RandomReference(0, int.MaxValue)), Constant(0))))
            .Contains("상한 계산 확인 필요"), "inclusive-upper-bound overflow cannot become a fabricated safe range");

        object counted = New("Mortal.Core.ConditionResultData");
        Set(counted, "_items", List("ConditionResultItem", False(), random)); Set(counted, "_count", 52);
        object conditional = Value("條件設定"); Set(conditional, "_condition", counted);
        text = PopupCondition(Condition("AND", Comparison(Group("Sum", conditional), Constant(1))));
        Check(text.Contains("조건값") && text.Contains(" OR ") && text.Contains("랜덤(0~30)"), "nested condition-backed values preserve full condition graph");
        Eq(52, Get(counted, "_count"), "popup condition formatting never executes nested condition counters");

        text = PopupEvent(Rate(7, true, six, RateItem(12, True()), RateItem(0, random), RateItem(-9, False()), RateItem(4, True())), 14);
        Check(text.Contains("활성 조건:") && text.Contains("현재 106"), "event popup retains full activation condition");
        Check(text.Contains("기본 가중치: 7") && text.Contains("1.") && text.Contains("4.") && text.Contains("→ +12") && text.Contains("→ 0") && text.Contains("→ -9"),
            "event popup includes every positive, zero and negative conditional-weight entry");
        Check(text.Contains("최종 가중치: 14") && !text.Contains("%"), "event weight is never mislabeled as probability");
        Check(PopupEvent(Rate(0, false, null), null).Contains("추가 활성 조건 없음") && PopupEvent(Rate(0, false, null), null).Contains("최종 가중치: 미확인"),
            "disabled activation condition and unavailable final rate are explicit");
        object[] arguments = { Rate(7, true, six), (int?)7, null };
        string detail = (string)T("DiceMaster.EventSelection.PopupConditionText").GetMethod("Event", new[] { typeof(object), typeof(int?), typeof(string).MakeByRefType() })
            .Invoke(null, arguments);
        Check(((string)arguments[2]).Contains("현재 106") && ((string)arguments[2]).Contains("가중치 7") && detail.Contains("최종 가중치: 7"),
            "event description and row condition summary share one formatted activation expression");

        object invalid = Value("常數"); Set(invalid, "_checkType", Enum.ToObject(T("Mortal.Core.StatCheckType"), 987));
        text = PopupCondition(Condition("AND", Comparison(Group("Sum", invalid), Constant(13))));
        Check(text.Contains("지원하지 않는 값 유형 987") && text.Contains("현재 13"), "unsupported leaf preserves readable opposite comparison operand");
        object stat = New("Mortal.Core.GameStat"); PropertySet(stat, "FinalValue", 42);
        object collection = New("Mortal.Core.StatConvertCollectionData");
        PropertySet(collection, "List", new ArrayList { New("Mortal.Core.UnknownStatConvertData") });
        Set(stat, "_statAdditionData", new[] { collection });
        object statReference = Value("角色數值"); Set(statReference, "_stat", stat);
        text = PopupCondition(Condition("AND", Comparison(Group("Sum", statReference), Constant(42))));
        Check(text.Contains("지원하지 않는 계산"), "unapproved stat conversion has an explicit unknown explanation");
        Eq(0, Get(stat, "FinalReads"), "popup never invokes a stat getter before strict validation");
        Check(PopupCondition(Condition("AND", Comparison(null, Constant(19)), null)).Contains("현재 19"), "malformed entry retains readable partial comparison");
        Check(PopupCondition(null).Contains("미확인"), "missing condition returns visible unknown text instead of throwing");
        Check(PopupEvent(null, null).Contains("미확인"), "missing event data returns visible unknown text");
        object malformedEvent = Rate(9, false, null); Set(malformedEvent, "_eventRateItem", null);
        text = PopupEvent(malformedEvent, 9);
        Check(text.Contains("기본 가중치: 9") && text.Contains("읽기 실패") && text.Contains("최종 가중치: 9"), "formatting failure preserves already-known weight and other readable event information");
        object blockedEvent = Rate(9, true, False()); Set(blockedEvent, "_eventRateItem", null);
        object blockedSnapshot = T("DiceMaster.EventSelection.EventEligibility").GetMethod("Evaluate").Invoke(null, new[] { blockedEvent });
        Eq("Ineligible", Get(blockedSnapshot, "State").ToString(), "authoritative inactive event result remains known despite unavailable unused weight descriptions");
        Eq(true, Get(blockedSnapshot, "CanUseResult"), "descriptive formatting failure never blocks an otherwise readable manual choice");
        Check(PopupEvent(blockedEvent, 0).Contains("읽기 실패"), "unused malformed weight descriptions remain visible as partial failure");
        object cycleReference = Value("數值群組"); object cycleGroup = Group("Sum", cycleReference); Set(cycleReference, "_statGroup", cycleGroup);
        Check(PopupCondition(Condition("AND", Comparison(cycleGroup, Constant(23)))).Contains("순환"), "cyclic group formatting terminates with a visible reason");
        Set(counted, "_items", List("ConditionResultItem", Condition("AND", Comparison(Group("Sum", conditional), Constant(1)))));
        Check(PopupCondition(Condition("AND", Comparison(Group("Sum", conditional), Constant(1)))).Contains("순환"), "cyclic condition graph formatting terminates");
        Eq(52, Get(counted, "_count"), "cyclic formatting never mutates condition counter");
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "all popup formatting scenarios preserve gameplay RNG state");
        Eq(0, sampledPreview.ByteDraws, "popup formatting never samples the mod-owned narrative RNG");
        Eq(expandedNames, StaticGet("DiceMaster.DataHelper", "ExpandedNameReads"), "popup never calls recursive condition-name expansion helper");
    }
}
