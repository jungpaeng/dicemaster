using System;
using System.Collections.Generic;
using System.Linq;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;

internal static partial class Program
{
    private static ConditionResultItem FlagEquals(FlagData flag, int value)
    { var result = IsZero(flag); result._items[0]._value2 = Constant(value); return result; }
    private static PositionConditionFlagItem Assign(ConditionResultItem condition, FlagData flag, int yes, int no)
    { return new PositionConditionFlagItem { _condition = condition, _flagItem = new[] { new PositionFlagItem {
        _flagData = flag, _trueState = yes, _falseState = no } } }; }
    private static ConditionResultItem RandomEntryCondition()
    { var condition = IsZero(new FlagData()); condition._items[0]._value1._value[0]._checkType = 7; return condition; }
    private static bool HasEntryRoute() => ScanRead().Summary.AutoText.Contains("O_scan_1", StringComparison.Ordinal);

    private static void PositionEntryProjectionCases()
    {
        var fixture = ScanFixture(1, -1); var controller = fixture.Controller; var target = fixture.Flags[0];
        target.State = 0;
        var condition = CheckPointManager.Instance._condition._list[0]; condition._items[0] = FlagEquals(target, 1);
        var driver = new FlagData { name = "renamed_entry_driver", State = 0 };
        var assignment = Assign(IsZero(driver), target, 1, 0);
        controller._positionData._conditionFlags = new[] { assignment };
        int writes = target.StateWrites, driverWrites = driver.StateWrites;
        for (int at = 0; at < 40; at++) ScanRead();
        var snapshot = ScanRead();
        Check(snapshot.Summary.AutoText.Contains("O_scan_1", StringComparison.Ordinal) &&
            !snapshot.Summary.Opportunity && !snapshot.Summary.Available,
            "a renamed first-choice flag currently 0 but assigned 1 on entry yields a map tooltip, never a marker");
        var predicate = PreviewConditions.Parse("checkpointmanager.Condition('scan_1')");
        Check(PreviewConditions.Current(predicate) == PreviewConditionState.NotMet &&
            MapRouteRead.ForSelectedPlace(controller._positionData).Current(predicate) == PreviewConditionState.Met,
            "strict current state stays false while the selected-place private entry forecast is true");
        var details = (MapGuideSnapshot)MapGuideSource.Discover(true, false, true)[0].Read();
        Check(details.Entries.Any(row => row.Detail.Contains("현재 조건 진단: 미충족", StringComparison.Ordinal) &&
            row.Detail.Contains("진입 후 가상 조건 진단: 충족", StringComparison.Ordinal) &&
            row.Detail.Contains("진입 가상 플래그 input_1 = 1", StringComparison.Ordinal)),
            "the real map detail distinguishes today's false condition from its projected entry value");
        int resources = DataHelper.Reads, parses = DiceMaster.LuaParse.LuaParser.Parses;
        driver.State = 1;
        Check(!HasEntryRoute(), "the freshly false entry condition assigns its false state and removes the preferred tooltip");
        driver.State = 0;
        Check(HasEntryRoute(), "a changed entry condition reopens guidance without reloading its definition");
        Check(target.State == 0 && target.StateWrites == writes && driver.StateWrites == driverWrites + 2,
            "even temporary flag changes/restoration are absent; only the fixture's explicit input changes write State");

        assignment._condition = RandomEntryCondition();
        Check(!HasEntryRoute(), "unknown random entry output is not an unlocked first choice");
        assignment._flagItem[0]._falseState = 1;
        Check(HasEntryRoute(), "equal true/false assignment targets are proven without drawing the unresolved random value");
        assignment._flagItem[0]._falseState = 0;
        var second = new FlagData { name = "renamed_second", State = 0 };
        controller._positionData._conditionFlags = new[] { assignment, Assign(IsZero(driver), target, 1, 0) };
        Check(HasEntryRoute(), "a known later assignment overwrites an earlier unknown private value in native order");
        Array.Reverse(controller._positionData._conditionFlags);
        Check(!HasEntryRoute(), "a later unknown write overrides an earlier known assignment; no earlier success is reused");

        // Every group checks once before any of its flag assignments. The next
        // group observes all of those assignments, not the real unchanged State.
        assignment = Assign(IsZero(target), target, 1, 0);
        assignment._flagItem = new[] { assignment._flagItem[0], new PositionFlagItem { _flagData = second, _trueState = 9, _falseState = 8 } };
        condition._items[0]._items.Add(FlagEquals(second, 9)._items[0]);
        controller._positionData._conditionFlags = new[] { assignment };
        Check(HasEntryRoute(), "one native group evaluates its condition once for all assignments");
        controller._positionData._conditionFlags = new[] { Assign(IsZero(second), target, 1, 0), Assign(FlagEquals(target, 1), second, 9, 8) };
        Check(HasEntryRoute(), "later groups read earlier virtual values while not-yet-written flags retain their current value");
        Check(target.State == 0 && second.State == 0 && target.StateWrites == writes && second.StateWrites == 1,
            "ordered projection never changes either real flag, including around duplicate/shared dependencies");

        // Preserve partial AND/OR truth without ever sampling a random operand.
        condition._items[0]._items.RemoveAt(1);
        var randomAndFalse = RandomEntryCondition(); randomAndFalse._items.Add(FlagEquals(driver, 1)._items[0]);
        controller._positionData._conditionFlags = new[] { Assign(randomAndFalse, target, 1, 0) };
        Check(!HasEntryRoute(), "random AND a definite false still produces the false assignment");
        randomAndFalse._logicOp = 1; randomAndFalse._items[1] = IsZero(driver)._items[0];
        Check(HasEntryRoute(), "random OR a definite true produces the true assignment without RNG");

        // Position flags run before action consumption; downstream guards may
        // still depend on the later spend and remain unknown.
        var action = controller._gameStatManager._stats._list[0]; action._value = 4;
        var actionCheck = IsZero(driver); actionCheck._items[0]._compareOp = 5;
        actionCheck._items[0]._value1._value[0] = new StatValueReference { _checkType = 1, _stat = action };
        actionCheck._items[0]._value2 = Constant(4);
        controller._positionData._conditionFlags = new[] { Assign(actionCheck, target, 1, 0) };
        Check(HasEntryRoute() && action._value == 4, "entry predicate reads pre-spend action without consuming or projecting an action stat");
        var original = condition._items[0]; condition._items[0] = actionCheck;
        Check(!HasEntryRoute(), "a downstream choice depending on post-spend action remains unknown");
        condition._items[0] = original;

        // A native derived-stat getter cannot see private flag overrides. Do
        // not advertise its real result as if the virtual assignment applied.
        var derived = new GameStat { _type = (StatKind)0, _value = 1, _statAdditionData = new[] {
            new StatConvertCollectionData { _list = new List<object> { new FlagDataRateConvertData { _flagData = target } } } } };
        var derivedCheck = IsZero(driver); derivedCheck._items[0]._value1._value[0] = new StatValueReference { _checkType = 1, _stat = derived };
        derivedCheck._items[0]._value2 = Constant(1);
        controller._positionData._conditionFlags = new[] { Assign(IsZero(driver), target, 1, 0), Assign(derivedCheck, target, 1, 0) };
        Check(!HasEntryRoute(), "an indirect getter affected by a private flag assignment is not evaluated as an unaffected real stat");

        controller._positionData._conditionFlags = new[] { Assign(IsZero(driver), target, 1, 0), new PositionConditionFlagItem() };
        Check(!HasEntryRoute(), "a malformed suffix rejects the full entry forecast, never its successful prefix");
        var unreadable = Assign(new ConditionResultItem { _items = null }, target, 1, 1);
        controller._positionData._conditionFlags = new[] { unreadable };
        Check(!HasEntryRoute(), "broken condition data cannot borrow the equal-target exemption for a valid unknown/random predicate");
        controller._positionData._conditionFlags = Enumerable.Repeat(Assign(IsZero(driver), target, 1, 0), 33).ToArray();
        Check(!HasEntryRoute(), "entry projection retains the 32-group bound");
        var excessive = Assign(IsZero(driver), target, 1, 0);
        excessive._flagItem = Enumerable.Repeat(excessive._flagItem[0], 33).ToArray();
        controller._positionData._conditionFlags = new[] { excessive };
        Check(!HasEntryRoute(), "entry projection retains the 32-assignment bound");
        var costly = new ConditionResultItem();
        for (int at = 0; at < 128; at++) costly._items.Add(new StatCompareItem {
            _value1 = new StatGroupVariable { _value = Enumerable.Range(0, 16).Select(_ => new StatValueReference()).ToList() },
            _value2 = Constant(0) });
        controller._positionData._conditionFlags = new[] { Assign(IsZero(driver), target, 1, 0), Assign(costly, second, 1, 0) };
        Check(MapRouteRead.ForSelectedPlace(controller._positionData) == null && !HasEntryRoute(),
            "entry work exhaustion rejects the whole forecast under its 2048-step sublimit");
        controller._positionData._conditionFlags = new[] { Assign(IsZero(driver), target, 1, 0) };
        Check(HasEntryRoute(), "valid input recovers on the next fresh read after malformed/limited data");
        Check(DataHelper.Reads == resources && DiceMaster.LuaParse.LuaParser.Parses == parses &&
            target.State == 0 && target.StateWrites == writes && second.StateWrites == 1 && Counters.GameCalls == 0,
            "entry forecasts retain warm caches and perform zero native evaluator, flag setter, RNG or action writes");
    }
}

namespace Mortal.Core
{
    public class StatConvertCollectionData { public List<object> _list = new List<object>(); }
    public class FlagDataRateConvertData { public object _flagData; }
}
