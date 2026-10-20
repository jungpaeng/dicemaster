using System;
using System.Collections.Generic;
using DiceMaster.EventSelection;
using MoonSharp.Interpreter;
using Mortal.Core;
using Mortal.Story;

internal static class SwitchExecutionRegression
{
    private static Action<bool, string> check;
    private static SwitchResultData pool;
    private static ConditionResultItem condition;
    private static long script;
    private const string Source = "local r=checkpointmanager.Switch('branch'); if r==1 then return 'first' elseif r==2 then return 'second' end";
    private static void Begin(bool value = false, bool random = false)
    {
        EventSelectionRuntime.Invalidate("switch test");
        EventSelectionRuntime.Enabled = () => false;
        EventSelectionRuntime.SwitchEnabled = () => true;
        EventSelectionRuntime.WaitingChanged = null;
        condition = new ConditionResultItem { Value = value, Random = random };
        pool = new SwitchResultData { name = "branch" };
        pool._items.Add(condition);
        var config = new SwitchResultConfig(); config.Items.Add("branch", pool);
        CheckPointManager.Instance = new CheckPointManager { _switchList = new[] { config }, _position = new PositionResultConfig() };
        script = EventSelectionRuntime.NewScript();
    }
    private static DynValue Engine(string code = Source, bool main = false)
    {
        UserData.RegisterType<CheckPointManager>();
        var engine = new Script(CoreModules.Preset_Complete);
        engine.Globals["checkpointmanager"] = CheckPointManager.Instance;
        bool changed;
        Dictionary<int, EventSwitchSite> sites;
        string transformed = EventLuaGate.Instrument(code, script, out changed, out sites);
        var context = new EventExecutionPatches.ScriptContext { Token = script, Epoch = EventSelectionRuntime.Epoch, Finalized = true, SwitchSites = sites };
        check(EventExecutionPatches.BindInterpreter(engine, context), "Switch typed callback bound");
        var closure = engine.LoadString(transformed);
        return main ? engine.Call(closure) : engine.CreateCoroutine(closure.Function);
    }
    private static EventCandidate Branch(int value)
    {
        foreach (var item in EventSelectionRuntime.Candidates) if (item.Result == value.ToString()) return item;
        throw new Exception("missing Switch branch " + value);
    }
    private static string Finish(DynValue coroutine, int branch, bool force)
    {
        check(EventSelectionRuntime.Select(Branch(branch).Id, force), "Switch request accepted");
        check(!EventSelectionRuntime.Select(Branch(branch).Id, force), "duplicate Switch request rejected");
        DynValue result = coroutine.Coroutine.Resume();
        check(coroutine.Coroutine.State == CoroutineState.Dead && EventSelectionRuntime.Token == 0, "Switch coroutine completed and owner released");
        check(CheckPointManager.Instance.SwitchCalls == 1 && pool._count == 1, "exactly one Switch invocation and counter increment");
        return result.String;
    }
    private static int ConditionDescriptions()
    {
        const string threeBranches = "local r=checkpointmanager.Switch('branch'); if r==1 then return 'first' elseif r==2 then return 'second' elseif r==3 then return 'fallback' end";
        Begin(true);
        condition.Description = "<color=#00ff00>A10005(0) = 0(0) | 명성(20) >= 10(10)</color>";
        var co = Engine(); co.Coroutine.Resume();
        check(Branch(1).ConditionDetail.Contains("A10005(0) = 0(0) | 명성(20) ＞= 10(10)") &&
            !Branch(1).ConditionDetail.Contains("<color") && Branch(1).Eligibility.IsEligible,
            "eligible Switch displays the complete plain condition expression alongside its verdict");
        check(condition.DescriptionReads == 1 && condition.GameplayReads == 0 && pool._count == 0,
            "condition text is read once without gameplay evaluation or counter mutation");
        check(Branch(1).Summary == Branch(1).ConditionDetail && !Branch(1).Summary.Contains("정상 발동 가능") &&
                Branch(2).Summary.StartsWith("앞 분기 우선", StringComparison.Ordinal) &&
                Branch(2).Summary != Branch(2).ConditionDetail,
            "normal branch summaries omit redundant availability badges while priority reasons remain distinct from full details");
        check(Branch(2).ConditionDetail.Contains("앞 분기가 먼저") && Branch(2).ConditionDetail.Contains("모두 미충족") &&
            Branch(2).Eligibility.State == EventEligibilityState.Ineligible,
            "fallback preserves its all-prior-false condition when an earlier branch blocks it");

        Begin(false); condition.Description = "A10005(1) = 0(0)";
        co = Engine(); co.Coroutine.Resume();
        check(Branch(1).ConditionDetail.Contains("A10005(1) = 0(0)") &&
            Branch(1).Eligibility.State == EventEligibilityState.Ineligible && Branch(1).CanForce,
            "definitely unmet condition remains visible with explicit override available");
        check(Branch(1).Summary.StartsWith("미충족", StringComparison.Ordinal) &&
                !Branch(1).ConditionDetail.StartsWith("미충족", StringComparison.Ordinal),
            "definitely unmet summary adds a short reason without replacing the complete condition");
        check(Branch(2).Eligibility.IsEligible && Branch(2).ConditionDetail.Contains("모두 미충족"),
            "eligible fallback explains why it applies instead of inventing its own condition");

        Begin(true); condition.Description = "첫 조건";
        var later = new ConditionResultItem { Value = false, Description = "두 번째 조건: 무공(2) ≥ 5(5)" };
        pool._items.Add(later);
        co = Engine(threeBranches); co.Coroutine.Resume();
        check(Branch(2).ConditionDetail.Contains(later.Description) && Branch(2).ConditionDetail.Contains("앞 분기가 먼저") &&
            Branch(2).Eligibility.State == EventEligibilityState.Ineligible,
            "priority-blocked branch retains its own condition rather than only an earlier-match explanation");
        check(Branch(3).ConditionDetail.Contains("1~2번") && Branch(3).Eligibility.State == EventEligibilityState.Ineligible,
            "fallback identifies the complete set of preceding conditions");

        Begin(false, true); condition.Description = "A10005(0) = 0(0) | 랜덤값0_30(??) ≤ 10(10)";
        co = Engine(); co.Coroutine.Resume();
        check(Branch(1).ConditionDetail.Contains(condition.Description) && Branch(1).Eligibility.State == EventEligibilityState.Unknown &&
            Branch(1).Eligibility.Reason == EventEligibilityReason.Random && Branch(1).CanForce,
            "random expression stays visible and unknown rather than being labeled unmet");
        check(Branch(1).Summary.StartsWith("? 난수", StringComparison.Ordinal) &&
                Branch(2).Summary.StartsWith("? 앞 분기 미확정", StringComparison.Ordinal) &&
                !Branch(1).Summary.Contains("미충족"),
            "random and dependent branches have explicit unknown summaries instead of being reported as failed");
        check(Branch(2).ConditionDetail.Contains("모두 미충족") && Branch(2).Eligibility.State == EventEligibilityState.Unknown &&
            condition.GameplayReads == 0 && pool._count == 0,
            "random-dependent fallback remains unknown without consuming gameplay RNG or count");

        Begin(); condition.Unsupported = true; condition.Description = "외부 능력치(??) ≥ 3(3)";
        co = Engine(); co.Coroutine.Resume();
        check(Branch(1).ConditionDetail.Contains(condition.Description) &&
            Branch(1).Eligibility.Reason == EventEligibilityReason.Unsupported && Branch(1).CanForce,
            "unsupported evaluation retains readable conditions without claiming a failed condition");
        check(Branch(1).Summary.StartsWith("? 미지원 계산", StringComparison.Ordinal),
            "unsupported condition shares the popup's unknown reason convention");
        check(Branch(2).Eligibility.Reason == EventEligibilityReason.Unsupported && Branch(2).ConditionDetail.Contains("모두 미충족"),
            "fallback depending on unsupported conditions is not mislabeled as a random condition");

        Begin(); condition.Broken = true; condition.Description = "앞 조건: 호감(??) ≥ 10(10)";
        later = new ConditionResultItem { Value = true, Description = "다음 조건: 명성(20) ≥ 10(10)" };
        pool._items.Add(later);
        co = Engine(threeBranches); co.Coroutine.Resume();
        check(Branch(1).ConditionDetail.Contains(condition.Description) && !Branch(1).CanForce,
            "unreadable evaluation retains available expression but cannot authorize an override");
        check(Branch(2).ConditionDetail.Contains(later.Description) && !Branch(2).CanForce &&
            Branch(3).ConditionDetail.Contains("모두 미충족") && !Branch(3).CanForce,
            "preceding read failure does not erase the later branch or fallback conditions");

        Begin(true); condition.DescriptionBroken = true;
        co = Engine(); co.Coroutine.Resume();
        check(Branch(1).ConditionDetail.Contains("조건 설명을 읽지 못했습니다") && Branch(1).CanSelect && pool._count == 0,
            "condition-description failure is explicit and does not replace safely evaluated eligibility");
        Begin(true); condition.Description = "";
        co = Engine(); co.Coroutine.Resume();
        check(Branch(1).ConditionDetail.Contains("조건 설명이 비어") && Branch(1).CanSelect,
            "empty formatted text never invents a failed condition");

        foreach (int operation in new[] { 0, 1, 2, 3, 4, 5 })
        {
            Begin(); pool._useCount = true; pool._count = 2;
            pool._countItems.Add(new ValueCompareItem { _compareOp = operation, _value = 3 });
            co = Engine(); co.Coroutine.Resume();
            string[] operators = { "=", "≠", "<", "≤", ">", "≥" };
            bool pass = operation == 0 || operation == 3 || operation == 5;
            check(Branch(1).ConditionDetail.Contains("이번 실행 횟수 3 " + operators[operation] + " 3") &&
                Branch(1).CanSelect == pass && Branch(2).CanSelect != pass && pool._count == 2 && condition.GameplayReads == 0,
                "count condition uses the next count and exact comparison without executing the switch: " + operation);
        }
        Begin(); pool._useCount = true;
        pool._countItems.Add(new ValueCompareItem { _compareOp = 99, _value = 3 });
        co = Engine(); co.Coroutine.Resume();
        check(!Branch(1).CanSelect && !Branch(1).CanForce && !Branch(2).CanForce &&
            Branch(1).ConditionDetail.Contains("조건 설명을 읽지 못했습니다"),
            "unknown count comparison remains unreadable and cannot be chosen from misleading text");
        return 15;
    }
    internal static int Run(Action<bool, string> assertion)
    {
        check = assertion;
        Begin(false, true);
        condition.Actual = () => throw new Exception("game RNG must not run for direct choice");
        var co = Engine(); co.Coroutine.Resume();
        check(EventSelectionRuntime.Waiting && EventSelectionRuntime.IsSwitch && EventSelectionRuntime.Candidates.Count == 2, "hidden random condition admits both handled branches");
        check(pool._count == 0 && condition.GameplayReads == 0 && !Branch(1).CanSelect && Branch(1).CanForce && Branch(2).CanForce, "display does not evaluate gameplay or counter; random requires explicit force");
        EventSelectionRuntime.Tick(true, 0);
        check(Branch(1).ResultDetail.Contains("first"), "exact branch body reaches display preview boundary");
        check(Finish(co, 1, true) == "first" && pool.OriginalCalls == 0 && condition.GameplayReads == 0, "force enters exact branch without original condition/RNG/nested side effects");

        Begin(true); co = Engine(); co.Coroutine.Resume();
        check(Branch(1).CanSelect && !Branch(2).CanSelect && Branch(2).CanForce, "first matching branch priority applies to else");
        check(Finish(co, 1, false) == "first" && condition.GameplayReads == 0, "deterministic normal selection uses safe reads only");

        Begin(false); co = Engine(); co.Coroutine.Resume();
        check(Branch(2).CanSelect && Branch(1).CanForce, "false condition makes else normal");
        check(Finish(co, 2, false) == "second", "normal else return is exact");

        Begin(false, true); condition.Actual = () => true; co = Engine(); co.Coroutine.Resume();
        EventSelectionRuntime.ContinueRandom(); EventSelectionRuntime.CancelConfirmation();
        co.Coroutine.Resume();
        check(EventSelectionRuntime.Waiting && pool._count == 0 && condition.GameplayReads == 0, "cancelled original request stays suspended without side effects");
        EventSelectionRuntime.ContinueRandom(); var original = co.Coroutine.Resume();
        check(original.String == "first" && pool._count == 1 && pool.OriginalCalls == 1 && condition.GameplayReads == 1, "original decision runs original condition once");

        Begin(false, true); condition.Actual = () => false; co = Engine(); co.Coroutine.Resume();
        EventSelectionRuntime.Select(Branch(1).Id, true);
        EventSelectionRuntime.SwitchEnabled = () => false;
        EventSelectionRuntime.Tick(false, 1); original = co.Coroutine.Resume();
        check(original.String == "second" && pool._count == 1 && pool.OriginalCalls == 1, "Switch OFF releases original independently of Event setting");

        Begin(true); co = Engine(); co.Coroutine.Resume();
        EventSelectionRuntime.Select(Branch(1).Id, false); condition.Value = false; co.Coroutine.Resume();
        check(EventSelectionRuntime.Waiting && pool._count == 0 && CheckPointManager.Instance.SwitchCalls == 0, "changed normal condition requires fresh confirmation");
        check(EventSelectionRuntime.Select(Branch(2).Id, false), "fresh changed condition confirmation accepted"); co.Coroutine.Resume();
        check(pool._count == 1, "retry executes only once");

        Begin(false); co = Engine(); co.Coroutine.Resume();
        EventSelectionRuntime.Select(Branch(1).Id, true); condition.Value = true; co.Coroutine.Resume();
        check(EventSelectionRuntime.Waiting && pool._count == 0 && Branch(1).CanSelect, "force becoming normal cannot silently replace captured mode");

        Begin(true); co = Engine(); co.Coroutine.Resume();
        EventSelectionRuntime.Select(Branch(1).Id, false); pool._count = 5; co.Coroutine.Resume();
        check(EventSelectionRuntime.Waiting && pool._count == 5 && CheckPointManager.Instance.SwitchCalls == 0, "changed execution count rejects stale request");
        check(EventSelectionRuntime.Select(Branch(1).Id, false), "fresh count revision can be confirmed"); co.Coroutine.Resume();
        check(pool._count == 6, "fresh count advances exactly once");

        Begin(true); co = Engine(); co.Coroutine.Resume();
        EventSelectionRuntime.Select(Branch(1).Id, false);
        pool._items[0] = new ConditionResultItem { Value = true }; co.Coroutine.Resume();
        check(EventSelectionRuntime.Waiting && CheckPointManager.Instance.SwitchCalls == 0, "condition object replacement rejects stale target");

        Begin(); pool._useCount = true; pool._countItems.Add(new ValueCompareItem { _compareOp = 5, _value = 3 });
        co = Engine(); co.Coroutine.Resume();
        check(Branch(2).CanSelect && Branch(1).CanForce && pool._count == 0, "count branch eligibility uses next increment without mutating count");
        check(Finish(co, 1, true) == "first" && pool.OriginalCalls == 0, "count route forced branch increments once without original execute");

        Begin(); condition.Broken = true; co = Engine(); co.Coroutine.Resume();
        check(!Branch(1).CanSelect && !Branch(1).CanForce && !Branch(2).CanForce, "read failure cannot be forced, including dependent else");

        Begin(); pool._items.Add(new ConditionResultItem { Value = true });
        co = Engine(); co.Coroutine.Resume();
        check(EventSelectionRuntime.Candidates.Count == 2, "Lua-unhandled third return is not offered");
        check(Branch(2).CanSelect, "second branch normal after first fails");

        Begin(); co = Engine("local r=checkpointmanager.Switch('branch'); if r==1 then return 'first' else return 'fallback' end"); co.Coroutine.Resume();
        check(Finish(co, 2, false) == "fallback", "exact else body applies only to unmatched branch values");

        Begin(true); var result = Engine(Source, true);
        check(result.String == "first" && !EventSelectionRuntime.Waiting && pool.OriginalCalls == 1, "main thread Switch cannot yield or create modal");

        Begin(true); EventSelectionRuntime.SwitchEnabled = () => false; co = Engine(); result = co.Coroutine.Resume();
        check(result.String == "first" && pool.OriginalCalls == 1 && !EventSelectionRuntime.Waiting, "disabled Switch directly follows original path");

        Begin(); bool changed;
        foreach (string unsupported in new[] {
            "return checkpointmanager.Switch('branch')",
            "local r=checkpointmanager.Switch('branch'); print('later'); if r==1 then end",
            "local r=checkpointmanager.Switch('branch'); if r==1 and true then end",
            "local r=checkpointmanager.Switch('branch'); if r==1 then elseif r==1 then end",
            "local r=checkpointmanager:Switch('branch'); if r==1 then end" })
        { check(EventLuaGate.Instrument(unsupported, script, out changed) == unsupported && !changed, "unsupported Switch shape safely passes through"); }

        Begin(false, true);
        pool.name = "Ch_3_6_4_002";
        CheckPointManager.Instance._switchList[0].Items.Clear();
        CheckPointManager.Instance._switchList[0].Items.Add(pool.name, pool);
        // Synthetic executable boundary matching the reported rest scene:
        // flag A10005=0 with hidden RNG<=10, then story versus meeting dice.
        condition.Actual = () => throw new Exception("hidden game RNG must not run before confirmation");
        co = Engine("local switch_002_result=checkpointmanager.Switch('Ch_3_6_4_002'); if switch_002_result==1 then return 'ch3_6_4_3' elseif switch_002_result==2 then return 'meeting dice' end");
        co.Coroutine.Resume();
        check(EventSelectionRuntime.Waiting && Branch(1).ConditionDetail.Contains("A10005") && condition.GameplayReads == 0, "reported Ch_3_6_4_002 hidden random condition is displayed without gameplay RNG");
        check(EventSelectionRuntime.Select(Branch(2).Id, true), "reported meeting dice branch explicitly confirmed");
        result = co.Coroutine.Resume();
        check(result.String == "meeting dice" && pool._count == 1 && condition.GameplayReads == 0, "reported exact branch continues after dialogue gate with one counter increment");

        Begin(true);
        co = Engine("local r=checkpointmanager.Switch('branch'); if r==1 then local r=checkpointmanager.Switch('branch'); if r==1 then return 'nested' elseif r==2 then return 'innerelse' end elseif r==2 then return 'outerelse' end");
        co.Coroutine.Resume(); long outer = EventSelectionRuntime.Token;
        check(EventSelectionRuntime.Select(Branch(1).Id, false), "outer same-key selected"); co.Coroutine.Resume();
        check(EventSelectionRuntime.Waiting && EventSelectionRuntime.Token != outer && pool._count == 1 && Branch(1).ResultDetail != null, "nested same-key call owns fresh exact site and counter");
        check(EventSelectionRuntime.Select(Branch(1).Id, false), "nested selected"); result = co.Coroutine.Resume();
        check(result.String == "nested" && pool._count == 2 && pool.OriginalCalls == 0, "nested same-key sites execute once each");
        return 18 + ConditionDescriptions();
    }
}
