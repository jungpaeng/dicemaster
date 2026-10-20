using System;
using System.Collections;
using System.Reflection;

static partial class RealGameApiTests
{
    // Only the Switch container/count comparisons are field boundaries. All
    // ConditionResultItem, comparison, group and reference types retain the
    // game's transplanted managed IL used by the other actual-API tests.
    sealed class SwitchReadPool
    {
        public int _count;
        public IList _items, _countItems;
    }
    sealed class SwitchReadCount
    {
        public int _value, _compareOp;
    }

    static IList SwitchStates(SwitchReadPool pool, bool useCount, out int count)
    {
        object[] arguments = { pool, useCount, 0 };
        try
        {
            IList states = (IList)T("DiceMaster.EventSelection.SwitchEligibility").GetMethod("Evaluate").Invoke(null, arguments);
            count = (int)arguments[2];
            return states;
        }
        catch (TargetInvocationException error) { throw error.InnerException ?? error; }
    }

    static void SwitchState(IList states, int index, string state, string reason, bool usable = true)
    {
        Eq(state, Get(states[index], "State").ToString(), "shared Switch state at branch " + (index + 1));
        Eq(reason, Get(states[index], "Reason").ToString(), "shared Switch reason at branch " + (index + 1));
        Eq(usable, Get(states[index], "CanUseResult"), "shared Switch readable-result boundary at branch " + (index + 1));
    }

    static void TestSwitchEligibility()
    {
        ResetRandom();
        CountingRandom sampledPreview = SeedPreview(321);
        var pool = new SwitchReadPool { _count = 11, _items = List("ConditionResultItem", False(), True(), True()) };
        IList states = SwitchStates(pool, false, out int count);
        Eq(11, count, "shared Switch exposes the captured current execution count");
        Eq(4, states.Count, "shared Switch contains every condition and one fallback");
        SwitchState(states, 0, "Ineligible", "ConditionsNotMet");
        SwitchState(states, 1, "Eligible", "None");
        SwitchState(states, 2, "Ineligible", "ConditionsNotMet");
        SwitchState(states, 3, "Ineligible", "ConditionsNotMet");
        Check(((string)Get(states[2], "Summary")).StartsWith("앞 분기 우선"), "later true Switch branch is blocked by the earlier matching branch");
        Check(((string)Get(states[3], "Detail")).Contains("앞 분기가 먼저"), "fallback explains first-match priority");
        Eq(11, pool._count, "ordered Switch display evaluation never advances its execution count");

        pool._items = List("ConditionResultItem", False(), Condition("AND"));
        states = SwitchStates(pool, false, out count);
        SwitchState(states, 0, "Ineligible", "ConditionsNotMet");
        SwitchState(states, 1, "Ineligible", "ConditionsNotMet");
        SwitchState(states, 2, "Eligible", "None");
        Check(((string)Get(states[1], "Detail")).Contains("조건 없음 (불충족)"), "empty condition item remains explicitly false");
        pool._items = List("ConditionResultItem");
        states = SwitchStates(pool, false, out count);
        Eq(1, states.Count, "empty Switch list retains only fallback");
        SwitchState(states, 0, "Eligible", "None");

        object random = Condition("AND", Comparison(Group("Sum", RandomReference(0, 30)), Constant(10), "LessEqual"));
        pool._items = List("ConditionResultItem", random, False(), True());
        states = SwitchStates(pool, false, out count);
        SwitchState(states, 0, "Unknown", "Random");
        SwitchState(states, 1, "Ineligible", "ConditionsNotMet");
        SwitchState(states, 2, "Unknown", "Random");
        SwitchState(states, 3, "Ineligible", "ConditionsNotMet");
        Check(((string)Get(states[0], "Detail")).Contains("랜덤(0~30)") && ((string)Get(states[0], "Detail")).Contains("미확정"),
            "shared Switch random conditions preserve original bounds without a sampled verdict");
        Check(((string)Get(states[2], "Summary")).StartsWith("? 앞 분기 미확정"), "readable true branch remains unknown behind an unknown earlier branch");
        pool._items = List("ConditionResultItem", random, False());
        states = SwitchStates(pool, false, out count);
        SwitchState(states, 2, "Unknown", "Random");

        object invalidReference = Value("常數");
        Set(invalidReference, "_checkType", Enum.ToObject(T("Mortal.Core.StatCheckType"), 987));
        object unsupported = Condition("AND", Comparison(Group("Sum", invalidReference), Constant(1)));
        pool._items = List("ConditionResultItem", unsupported, True());
        states = SwitchStates(pool, false, out count);
        SwitchState(states, 0, "Unknown", "Unsupported");
        SwitchState(states, 1, "Unknown", "Unsupported");
        SwitchState(states, 2, "Ineligible", "ConditionsNotMet");
        pool._items = List("ConditionResultItem", unsupported, random);
        states = SwitchStates(pool, false, out count);
        SwitchState(states, 1, "Unknown", "Unsupported");
        SwitchState(states, 2, "Unknown", "Unsupported");

        pool._items = List("ConditionResultItem", null, True(), False());
        states = SwitchStates(pool, false, out count);
        for (int index = 0; index < states.Count; index++) SwitchState(states, index, "Unknown", "ReadFailure", false);
        pool._items = List("ConditionResultItem", True(), null);
        states = SwitchStates(pool, false, out count);
        SwitchState(states, 0, "Eligible", "None");
        SwitchState(states, 1, "Unknown", "ReadFailure", false);
        SwitchState(states, 2, "Unknown", "ReadFailure", false);

        object nested = New("Mortal.Core.ConditionResultData");
        Set(nested, "_count", 27); Set(nested, "_items", List("ConditionResultItem", True()));
        object conditionValue = Value("條件設定"); Set(conditionValue, "_condition", nested);
        pool._items = List("ConditionResultItem", Condition("AND", Comparison(Group("Sum", conditionValue), Constant(1))));
        states = SwitchStates(pool, false, out count);
        SwitchState(states, 0, "Eligible", "None");
        Eq(27, Get(nested, "_count"), "shared Switch evaluation never executes nested condition-data counters");

        pool._items = new ArrayList { null }; // The inactive ordinary route must never be read in count mode.
        pool._count = 2;
        string[] operators = { "=", "≠", "<", "≤", ">", "≥" };
        for (int operation = 0; operation < 6; operation++)
            for (int right = 2; right <= 4; right++)
            {
                pool._countItems = new ArrayList { new SwitchReadCount { _value = right, _compareOp = operation } };
                states = SwitchStates(pool, true, out count);
                bool eligible = operation switch { 0 => 3 == right, 1 => 3 != right, 2 => 3 < right, 3 => 3 <= right, 4 => 3 > right, _ => 3 >= right };
                SwitchState(states, 0, eligible ? "Eligible" : "Ineligible", eligible ? "None" : "ConditionsNotMet");
                SwitchState(states, 1, eligible ? "Ineligible" : "Eligible", eligible ? "ConditionsNotMet" : "None");
                Check(((string)Get(states[0], "Detail")).Contains("이번 실행 횟수 3 " + operators[operation] + " " + right), "count-based Switch description and verdict use the next execution count");
                Eq(2, count, "count-based Switch returns the unmodified current count");
                Eq(2, pool._count, "count-based Switch display evaluation does not increment count");
            }
        pool._countItems = new ArrayList {
            new SwitchReadCount { _compareOp = 3, _value = 4 }, new SwitchReadCount { _compareOp = 4, _value = 1 } };
        states = SwitchStates(pool, true, out count);
        SwitchState(states, 0, "Eligible", "None");
        SwitchState(states, 1, "Ineligible", "ConditionsNotMet");
        Check(((string)Get(states[1], "Summary")).StartsWith("앞 분기 우선"), "count route preserves first-match priority");
        pool._count = int.MaxValue;
        pool._countItems = new ArrayList { new SwitchReadCount { _compareOp = 0, _value = int.MinValue } };
        states = SwitchStates(pool, true, out count);
        SwitchState(states, 0, "Eligible", "None");
        Eq(int.MaxValue, count, "count overflow preview still captures original count");
        Check(((string)Get(states[0], "Detail")).Contains("-2147483648"), "next-count calculation preserves original unchecked overflow semantics");
        pool._countItems = new ArrayList { new SwitchReadCount { _compareOp = 99, _value = 1 } };
        states = SwitchStates(pool, true, out count);
        SwitchState(states, 0, "Unknown", "ReadFailure", false);
        SwitchState(states, 1, "Unknown", "ReadFailure", false);
        pool._countItems = new ArrayList();
        states = SwitchStates(pool, true, out count);
        SwitchState(states, 0, "Eligible", "None");

        pool._items = null;
        Throws<InvalidOperationException>(() => SwitchStates(pool, false, out _), "missing active Switch collection fails without inventing fallback availability");
        pool._countItems = null;
        Throws<InvalidOperationException>(() => SwitchStates(pool, true, out _), "missing count route fails without evaluating ordinary conditions");
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "all shared Switch display scenarios preserve gameplay RNG state");
        Eq(0, sampledPreview.ByteDraws, "shared Switch display evaluation never samples the mod-owned narrative RNG");
        Eq(27, Get(nested, "_count"), "all Switch evaluation scenarios leave nested execution counters unchanged");
    }
}
