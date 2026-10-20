using System;
using System.Collections.Generic;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Story;

internal static class PartialConditionCases
{
    private static StatGroupVariable Group(StatValueReference value) => new StatGroupVariable { _value = new List<StatValueReference> { value } };
    private static StatCompareItem Constant(bool value) => new StatCompareItem {
        _value1 = Group(new StatValueReference { _constant = value ? 1 : 0 }),
        _value2 = Group(new StatValueReference { _constant = 1 }) };
    private static StatCompareItem RandomComparison() => new StatCompareItem {
        _value1 = Group(new StatValueReference { _checkType = 7 }), _value2 = Group(new StatValueReference { _constant = 50 }) };
    private static ConditionResultItem Item(int logic, params StatCompareItem[] comparisons) => new ConditionResultItem { _logicOp = logic, _items = new List<object>(comparisons) };
    internal static void Run(Action<bool, string> check)
    {
        var manager = CheckPointManager.Instance;
        var conditions = manager._condition;
        var switches = manager._switchList;
        var data = new ConditionResultData { name = "partial_fixture", _items = new List<ConditionResultItem>() };
        var sw = new SwitchResultData { name = "partial_switch", _items = new List<ConditionResultItem>() };
        manager._condition = new ConditionResultConfig { _list = new List<ConditionResultData> { data } };
        manager._switchList = new object[] { new SwitchResultConfig { _list = new List<SwitchResultData> { sw } } };
        void State(string expected, string expression, string reason)
        { check(PreviewConditions.Current(PreviewConditions.Parse(expression)).ToString() == expected, reason); }
        const string Read = "checkpointmanager.Condition('partial_fixture')";
        const string Switch = "checkpointmanager.Switch('partial_switch')";
        try
        {
            foreach (bool randomFirst in new[] { true, false })
            {
                var random = RandomComparison(); var no = Constant(false); var yes = Constant(true);
                data._items = new List<ConditionResultItem> { Item(0, randomFirst ? random : no, yes, randomFirst ? no : random) };
                State("NotMet", Read, "random AND known false is false independent of comparison order");
                data._items = new List<ConditionResultItem> { Item(1, randomFirst ? random : yes, no, randomFirst ? yes : random) };
                State("Met", Read, "random OR known true is true independent of comparison order");
                data._items = randomFirst ? new List<ConditionResultItem> { Item(0, random), Item(0, yes) } : new List<ConditionResultItem> { Item(0, yes), Item(0, random) };
                State("Met", Read, "condition-result OR alternatives preserve known true independently of random order");
            }
            data._items = new List<ConditionResultItem> { Item(0, RandomComparison(), Constant(true)) };
            State("Unknown", Read, "random AND true remains unknown without sampling");
            data._items = new List<ConditionResultItem> { Item(1, RandomComparison(), Constant(false)) };
            State("Unknown", Read, "random OR false remains unknown without sampling");
            data._items = new List<ConditionResultItem> { Item(0, RandomComparison()) };
            foreach (string expression in new[] { Read + " and false", "false and " + Read, "checkpointmanager.Condition('missing') and false", "false and checkpointmanager.Condition('missing')" })
                State("NotMet", expression, "unknown checkpoint operands cannot hide a false AST sibling");
            foreach (string expression in new[] { Read + " or true", "true or " + Read, "checkpointmanager.Condition('missing') or true", "true or checkpointmanager.Condition('missing')" })
                State("Met", expression, "unknown checkpoint operands cannot hide a true AST sibling");
            data._items = null;
            State("NotMet", Read + " and false", "checkpoint graph exception is isolated to its AST leaf");
            State("Met", Read + " or true", "malformed checkpoint graph does not abort a separate true OR operand");
            var flag = new FlagData { name = "changed", State = 1 };
            var affected = new StatCompareItem { _value1 = Group(new StatValueReference { _checkType = 3, _flag = flag }), _value2 = Group(new StatValueReference { _constant = 1 }) };
            data._items = new List<ConditionResultItem> { Item(0, affected, Constant(false)) };
            var masked = PreviewConditions.CaptureCurrent(PreviewConditions.Parse(Read), new[] { "changed" }, false);
            check(PreviewConditions.Current(masked) == PreviewConditionState.NotMet, "a masked flag comparison cannot hide a separate stable false comparison");
            data._items = new List<ConditionResultItem> { Item(0, affected, Constant(true)) };
            check(PreviewConditions.Current(masked) == PreviewConditionState.Unknown, "a masked flag comparison is never evaluated at its old value");

            sw._items = new List<ConditionResultItem> { Item(0, Constant(false)), Item(0, RandomComparison()) };
            State("NotMet", Switch + "==1", "a proven false first switch item excludes result one despite later randomness");
            State("Met", Switch + "~=1", "excluded switch branch has a known negative equality result");
            State("Met", Switch + ">1", "all possible later switch indices satisfy a lower bound");
            State("Unknown", Switch + "==2", "a random later branch cannot be selected by preview");
            State("Unknown", Switch + "==3", "random fallthrough is possible rather than certain");
            sw._items = new List<ConditionResultItem> { Item(0, affected, RandomComparison()) };
            var maskedSwitch = PreviewConditions.CaptureCurrent(PreviewConditions.Parse(Switch + "==1"), new[] { "changed" }, false);
            check(PreviewConditions.Current(maskedSwitch) == PreviewConditionState.Unknown, "affected switch inputs cannot exclude a branch using their old flag values");
            sw._items = new List<ConditionResultItem> { Item(0, affected, Constant(false)) };
            check(PreviewConditions.Current(maskedSwitch) == PreviewConditionState.NotMet, "a stable false comparison excludes a switch item even beside another affected input");
            var opaqueSwitch = PreviewConditions.CaptureCurrent(PreviewConditions.Parse(Switch + "==1"), null, true);
            check(PreviewConditions.Current(opaqueSwitch) == PreviewConditionState.Unknown, "an opaque preceding effect still blocks current switch proofs");
            sw._items = new List<ConditionResultItem> { Item(0, RandomComparison()), Item(0, Constant(true)), Item(0, RandomComparison()) };
            State("NotMet", Switch + "==3", "a known-true earlier item excludes later indices even when an earlier random item may win");
            State("Met", Switch + "<=2", "possible switch choices preserve priority bounds");
            State("Unknown", Switch + "==1", "an earlier unknown item keeps its own result possible");
            sw._items = new List<ConditionResultItem> { Item(0, Constant(true)), Item(0, RandomComparison()) };
            State("Met", Switch + "==1", "known-true first switch item dominates later random items");
            sw._items = new List<ConditionResultItem> { Item(0, RandomComparison()), Item(0, Constant(false)), Item(0, Constant(true)) };
            State("NotMet", Switch + "==2", "noncontiguous possible switch indices do not invent an excluded middle branch");
            sw._useCount = true;
            State("Unknown", Switch + "==2", "count-dependent switches remain protected even when an item appears false"); sw._useCount = false;
            sw._items.Clear();
            State("Met", Switch + "==1", "empty switch still falls through to one");
            for (int i = 0; i < 256; i++) sw._items.Add(Item(0, RandomComparison()));
            State("NotMet", Switch + "==258", "bounded 256-item switch has no result outside one through 257");
            State("Met", Switch + ">0", "every supported possible switch result is Lua-truthy and positive");
            sw._items.Add(Item(0, Constant(true)));
            State("Unknown", Switch + "==1", "oversized switch graph remains unknown instead of bypassing its item cap");
            check(data._count == 29 && manager.GameplayCalls == 0 && manager.RngCalls == 0 && manager.Counter == 0 && UnityEngine.Random.Calls == 0,
                "partial condition evaluation never executes gameplay, RNG or checkpoint counters");
        }
        finally { manager._condition = conditions; manager._switchList = switches; }
    }
}
