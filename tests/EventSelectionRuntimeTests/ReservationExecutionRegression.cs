using System;
using System.Collections.Generic;
using DiceMaster.EventSelection;
using MoonSharp.Interpreter;
using Mortal.Core;
using Mortal.Story;

internal static class ReservationExecutionRegression
{
    private static Action<bool, string> check;
    private static int scenarios;
    private static CheckPointManager manager;
    private static PositionResultData position;
    private static PositionEventData target;
    private static SwitchResultData branch;
    private static ConditionResultItem condition;
    private const string SwitchSource = "local r=checkpointmanager.Switch('branch'); if r==1 then return 'first' elseif r==2 then return 'second' end";
    private const string PositionSource = "return checkpointmanager.Position('action')";

    private static void Begin(bool popups = false)
    {
        scenarios++;
        EventSelectionRuntime.Invalidate("reservation test");
        CheckpointReservationStore.Clear();
        EventSelectionRuntime.Enabled = EventSelectionRuntime.SwitchEnabled = () => popups;
        EventSelectionRuntime.ReservationsEnabled = () => true;
        EventSelectionRuntime.WaitingChanged = null;
        target = new PositionEventData { name = "reserved", ResultType = "reserved", Eligible = false, Unknown = true,
            ActualRate = () => throw new Exception("reservation must bypass gameplay weight/RNG") };
        position = new PositionResultData { name = "action", _scriptPrefix = "story" };
        position.List.Add(target);
        position.List.Add(new PositionEventData { name = "other", ResultType = "other" });
        condition = new ConditionResultItem { Value = false, Random = true,
            Actual = () => throw new Exception("reservation must bypass gameplay condition/RNG") };
        branch = new SwitchResultData { name = "branch" };
        branch._items.Add(condition);
        var switchConfig = new SwitchResultConfig(); switchConfig.Items.Add("branch", branch);
        manager = new CheckPointManager { _position = new PositionResultConfig(), _switchList = new[] { switchConfig } };
        manager._position.List.Add(position);
        CheckPointManager.Instance = manager;
    }

    private static void ReservePosition()
        => CheckpointReservationStore.Set(new CheckpointReservation(false, manager, manager._position, position, target,
            "action", "story_reserved", "story"));

    private static void ReserveSwitch(int result = 1)
    {
        object selected = result == 1 ? branch._useCount ? (object)branch._countItems[0] : condition : branch;
        CheckpointReservationStore.Set(new CheckpointReservation(true, manager, manager._switchList[0], branch, selected,
            "branch", result.ToString(), useCount: branch._useCount, count: branch._count));
    }

    private static DynValue Engine(string source, bool main = false)
    {
        UserData.RegisterType<CheckPointManager>();
        var engine = new Script(CoreModules.Preset_Complete);
        engine.Globals["checkpointmanager"] = manager;
        long script = EventSelectionRuntime.NewScript();
        bool changed;
        Dictionary<int, EventSwitchSite> sites;
        string transformed = EventLuaGate.Instrument(source, script, out changed, out sites);
        var context = new EventExecutionPatches.ScriptContext { Token = script, Epoch = EventSelectionRuntime.Epoch,
            Finalized = true, SwitchSites = sites };
        check(EventExecutionPatches.BindInterpreter(engine, context), "reservation Lua callback bound");
        var closure = engine.LoadString(transformed);
        return main ? engine.Call(closure) : engine.CreateCoroutine(closure.Function);
    }

    private static void PopupSettingsAndPriority()
    {
        foreach (bool isSwitch in new[] { false, true })
        foreach (bool popup in new[] { false, true })
        {
            Begin(popup);
            if (isSwitch) ReserveSwitch(); else ReservePosition();
            var co = Engine(isSwitch ? SwitchSource : PositionSource);
            DynValue value = co.Coroutine.Resume();
            check(co.Coroutine.State == CoroutineState.Dead && !EventSelectionRuntime.Waiting && EventSelectionRuntime.Token == 0,
                "reservation completes without popup regardless of its ON/OFF setting");
            check(value.String == (isSwitch ? "first" : "story_reserved") && CheckpointReservationStore.Current(isSwitch) == null,
                "exact reserved result is consumed once");
            check(target.RealCalls == 0 && condition.GameplayReads == 0 && target.DisplayCalls == 0 && condition.DisplayReads == 0,
                "reservation neither evaluates eligibility nor calls original condition/RNG");
            check(isSwitch ? branch._count == 1 && branch.OriginalCalls == 0 : manager.OriginalCalls == 0,
                "reservation executes one original checkpoint and keeps only the Switch counter increment");
        }

        foreach (bool isSwitch in new[] { false, true })
        {
            Begin(true);
            var co = Engine(isSwitch ? SwitchSource : PositionSource);
            co.Coroutine.Resume();
            check(EventSelectionRuntime.Waiting, "popup is waiting before a reservation is created in the full log");
            long old = EventSelectionRuntime.Token;
            if (isSwitch) ReserveSwitch(); else ReservePosition();
            var value = co.Coroutine.Resume();
            check(co.Coroutine.State == CoroutineState.Dead && value.String == (isSwitch ? "first" : "story_reserved"),
                "matching reservation replaces the pending popup at its own continuation");
            check(old != 0 && EventSelectionRuntime.Token == 0 && !EventSelectionRuntime.Waiting && condition.GameplayReads == 0,
                "popup ownership is released without invalidating the executing Lua epoch");
        }
    }

    private static void DirectAndUnsupportedCalls()
    {
        foreach (string source in new[] { "return checkpointmanager.Switch('branch')", SwitchSource })
        foreach (bool main in new[] { false, true })
        {
            Begin(true); ReserveSwitch(2);
            DynValue value = Engine(source, main);
            if (!main) value = value.Coroutine.Resume();
            check((source == SwitchSource ? value.String == "second" : value.Number == 2) && branch._count == 1 &&
                branch.OriginalCalls == 0 && !EventSelectionRuntime.Waiting,
                "non-yielding reservation works for unsupported Switch shapes and main Lua");
        }
        Begin(); ReservePosition(); ReserveSwitch();
        check(manager.Position("action") == "story_reserved" && manager.Switch("branch") == 1,
            "direct manager calls consume independent event and condition reservations");
        condition.Actual = () => false;
        check(manager.Position("action") == "original" && manager.Switch("branch") == 2 && branch._count == 2 && branch.OriginalCalls == 1,
            "second matching call returns to original game behavior");

        Begin(); ReservePosition(); ReserveSwitch();
        check(manager.Position("different") == "original" && manager.Switch("different") == 0,
            "unrelated checkpoints preserve original behavior");
        check(CheckpointReservationStore.Current(false) != null && CheckpointReservationStore.Current(true) != null,
            "unrelated checkpoints cannot consume either reservation");
        EventSelectionRuntime.Invalidate("ordinary next Lua");
        check(manager.Position("action") == "story_reserved" && manager.Switch("branch") == 1,
            "reservations survive ordinary Lua context replacement with fresh execution authority");

        Begin(); branch._useCount = true; branch._count = 4;
        branch._countItems.Add(new ValueCompareItem { _compareOp = 0, _value = 99 });
        ReserveSwitch();
        check(manager.Switch("branch") == 1 && branch._count == 5 && branch.OriginalCalls == 0 && condition.GameplayReads == 0,
            "count-mode reservation advances its own count once without changing comparison data");
        check(branch._countItems[0]._value == 99, "reservation never rewrites count conditions");
    }

    private static void StaleReservations()
    {
        foreach (Action mutate in new Action[] {
            () => position.List.Remove(target),
            () => target.ResultType = "replacement",
            () => position._scriptPrefix = "replacement",
            () => position.List[0] = new PositionEventData { name = target.name, ResultType = target.ResultType },
            () => manager._position = new PositionResultConfig(),
            () => manager._position.List[0] = new PositionResultData { name = "action", _scriptPrefix = "story" } })
        {
            Begin(); ReservePosition(); mutate();
            check(manager.Position("action") == "original" && CheckpointReservationStore.Current(false) == null && !EventSelectionRuntime.Waiting,
                "stale event identity, membership, prefix or result discards the override without adopting replacements");
        }
        foreach (Action mutate in new Action[] {
            () => branch._count++,
            () => branch._useCount = true,
            () => branch._items[0] = new ConditionResultItem { Value = true },
            () => manager._switchList[0] = new SwitchResultConfig(),
            () => manager._switchList[0].Items["branch"] = new SwitchResultData { name = "branch" } })
        {
            Begin(); ReserveSwitch(); condition.Actual = () => false; mutate();
            manager.Switch("branch");
            check(CheckpointReservationStore.Current(true) == null && !EventSelectionRuntime.Waiting && EventSelectionRuntime.Token == 0,
                "stale Switch identity, count or mode cannot produce a direct reserved result");
        }
        Begin(); ReserveSwitch(2); condition.Actual = () => false;
        branch._items.Add(new ConditionResultItem { Value = true });
        check(manager.Switch("branch") == 2 && branch.OriginalCalls == 1 && CheckpointReservationStore.Current(true) == null,
            "new branch membership cannot silently repurpose a reserved fallback result");

        Begin();
        CheckpointReservationStore.Set(new CheckpointReservation(true, manager, manager._switchList[0], branch, null, "branch", "999"));
        condition.Actual = () => false;
        check(manager.Switch("branch") == 2 && branch.OriginalCalls == 1 && CheckpointReservationStore.Current(true) == null,
            "malformed branch result and null target fail closed to original behavior");

        Begin(true); ReservePosition(); target.ResultType = "replacement";
        var co = Engine(PositionSource); co.Coroutine.Resume();
        check(EventSelectionRuntime.Waiting && CheckpointReservationStore.Current(false) == null && manager.PositionCalls == 0,
            "stale reservation is discarded before ordinary enabled popup admission");
    }

    private static void SettingsConcurrencyAndStoreAuthority()
    {
        Begin(); ReservePosition(); ReserveSwitch();
        EventSelectionRuntime.ReservationsEnabled = () => false;
        condition.Actual = () => false;
        check(manager.Position("action") == "original" && manager.Switch("branch") == 2 &&
            CheckpointReservationStore.Current(false) != null && CheckpointReservationStore.Current(true) != null,
            "master feature OFF cannot consume or execute reservations");
        EventSelectionRuntime.ReservationsEnabled = () => throw new Exception("settings boundary");
        check(manager.Position("action") == "original" && CheckpointReservationStore.Current(false) != null,
            "configuration read failure preserves pending intent and original behavior");

        Begin(true);
        var co = Engine(PositionSource); co.Coroutine.Resume();
        long token = EventSelectionRuntime.Token;
        ReserveSwitch(); condition.Actual = () => false;
        check(manager.Switch("branch") == 2 && CheckpointReservationStore.Current(true) != null && EventSelectionRuntime.Token == token && EventSelectionRuntime.Waiting,
            "unrelated direct checkpoint cannot steal a waiting modal owner or its reservation");
        EventSelectionRuntime.ContinueRandom(); co.Coroutine.Resume();
        check(manager.Switch("branch") == 2 && CheckpointReservationStore.Current(true) == null && branch.OriginalCalls == 2,
            "changed Switch count invalidates the deferred reservation after unrelated popup completion");

        Begin(); ReservePosition();
        CheckpointReservation old = CheckpointReservationStore.Current(false);
        CheckpointReservationStore.Set(old);
        check(!CheckpointReservationStore.Take(old) && CheckpointReservationStore.Current(false) != null,
            "consume and reinsert of the same choice never restores old reference authority");
        CheckpointReservationStore.Clear();
        check(manager.Position("action") == "original" && CheckpointReservationStore.Current(false) == null,
            "true game transition clears pending reservation authority");
    }

    internal static int Run(Action<bool, string> assertion)
    {
        check = assertion;
        PopupSettingsAndPriority(); DirectAndUnsupportedCalls(); StaleReservations(); SettingsConcurrencyAndStoreAuthority();
        CheckpointReservationStore.Clear();
        EventSelectionRuntime.ReservationsEnabled = null;
        return scenarios;
    }
}
