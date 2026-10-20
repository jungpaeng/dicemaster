using System;
using System.Collections.Generic;
using DiceMaster.EventSelection;
using MoonSharp.Interpreter;
using Mortal.Core;
using Mortal.Story;
using UnityEngine;

// Real instrumented Lua continuations exercise the log's runtime ownership,
// including deferred automatic releases and one-use reservations. Rendering
// and the plugin's focus handoff have their own UI/opener boundary tests.
internal static class PopupLogFocusRegression
{
    private static Action<bool, string> check;
    private static int scenarios;
    private static CheckPointManager manager;
    private static PositionResultData position;
    private static PositionEventData target, other;
    private static SwitchResultData branch;
    private static ConditionResultItem condition;
    private const string PositionSource = "return checkpointmanager.Position('action')";
    private const string SwitchSource = "local r=checkpointmanager.Switch('branch'); if r==1 then return 'first' elseif r==2 then return 'second' end";

    private static void Begin(string request)
    {
        scenarios++;
        EventSelectionRuntime.SetLogVisible(false);
        EventSelectionRuntime.Invalidate("popup/log focus test");
        CheckpointReservationStore.Clear();
        EventSelectionRuntime.Enabled = EventSelectionRuntime.SwitchEnabled = EventSelectionRuntime.ReservationsEnabled = () => true;
        EventSelectionRuntime.WaitingChanged = null;
        Time.unscaledTime = 0;
        bool unknown = request != "normal";
        target = new PositionEventData { name = "target", ResultType = "target", Eligible = !unknown, Unknown = unknown,
            ActualRate = () => throw new Exception("direct choice must not evaluate gameplay weight/RNG") };
        other = new PositionEventData { name = "other", ResultType = "other" };
        position = new PositionResultData { name = "action", _scriptPrefix = "story" };
        position.List.Add(target); position.List.Add(other);
        condition = new ConditionResultItem { Value = !unknown, Random = unknown, Actual = () => false };
        branch = new SwitchResultData { name = "branch" };
        branch._items.Add(condition);
        var switchConfig = new SwitchResultConfig(); switchConfig.Items.Add("branch", branch);
        manager = new CheckPointManager { _position = new PositionResultConfig(), _switchList = new[] { switchConfig } };
        manager._position.List.Add(position);
        CheckPointManager.Instance = manager;
    }

    private static DynValue Engine(bool isSwitch)
    {
        UserData.RegisterType<CheckPointManager>();
        var engine = new Script(CoreModules.Preset_Complete);
        engine.Globals["checkpointmanager"] = manager;
        long script = EventSelectionRuntime.NewScript();
        bool changed;
        Dictionary<int, EventSwitchSite> sites;
        string transformed = EventLuaGate.Instrument(isSwitch ? SwitchSource : PositionSource, script, out changed, out sites);
        var context = new EventExecutionPatches.ScriptContext { Token = script, Epoch = EventSelectionRuntime.Epoch,
            Finalized = true, SwitchSites = sites };
        check(changed && EventExecutionPatches.BindInterpreter(engine, context), "log-focus test uses the instrumented typed Lua callback");
        return engine.CreateCoroutine(engine.LoadString(transformed).Function);
    }

    private static EventCandidate Choice(bool isSwitch)
    {
        foreach (EventCandidate candidate in EventSelectionRuntime.Candidates)
            if (isSwitch ? candidate.Result == "1" : ReferenceEquals(candidate.Target, target)) return candidate;
        throw new Exception("missing log-focus candidate");
    }

    private static void Queue(EventCandidate candidate, string request)
    {
        if (request == "random")
        {
            EventSelectionRuntime.ContinueRandom();
            check(EventSelectionRuntime.Status.Contains("원래 판정"), "original decision was queued before the log takes focus");
        }
        else check(EventSelectionRuntime.Select(candidate.Id, request == "force"), request + " choice is accepted before the log takes focus");
    }

    private static void SuspendedWithoutEffects(DynValue co, long token, string reason)
    {
        co.Coroutine.Resume();
        check(co.Coroutine.State == CoroutineState.Suspended && EventSelectionRuntime.Token == token &&
            manager.PositionCalls == 0 && manager.SwitchCalls == 0 && manager.OriginalCalls == 0 &&
            branch.OriginalCalls == 0 && branch._count == 0 && condition.GameplayReads == 0 &&
            target.RealCalls == 0 && other.RealCalls == 0, reason);
    }

    private static void RequestsNeedFreshConfirmation()
    {
        foreach (bool isSwitch in new[] { false, true })
        foreach (string request in new[] { "normal", "force", "random" })
        {
            Begin(request);
            var co = Engine(isSwitch); co.Coroutine.Resume();
            EventCandidate candidate = Choice(isSwitch);
            long token = EventSelectionRuntime.Token;
            Queue(candidate, request);
            EventSelectionRuntime.SetLogVisible(true);
            check(EventSelectionRuntime.Waiting && EventSelectionRuntime.Status.Contains("다시 확인"),
                "opening the full log cancels the pending popup request and retains its owner");
            string status = EventSelectionRuntime.Status;
            check(!EventSelectionRuntime.Select(candidate.Id, request == "force"), "hidden popup cannot accept a selection while the log owns focus");
            EventSelectionRuntime.ContinueRandom();
            check(EventSelectionRuntime.Status == status, "hidden original-decision input cannot queue a request through the log");
            EventSelectionRuntime.SetLogVisible(true);
            EventSelectionRuntime.Tick(false, 1);
            SuspendedWithoutEffects(co, token, "the real Lua continuation stays suspended with the log open");
            SuspendedWithoutEffects(co, token, "repeated frames under log ownership cannot execute conditions or game calls");
            check(Choice(isSwitch).Id == candidate.Id, "log ownership preserves the current popup candidate identity");

            EventSelectionRuntime.SetLogVisible(false);
            SuspendedWithoutEffects(co, token, "closing the log does not restore or execute the cancelled request");
            Queue(Choice(isSwitch), request);
            EventSelectionRuntime.SetLogVisible(false);
            var value = co.Coroutine.Resume();
            string expected = isSwitch ? request == "random" ? "second" : "first" : request == "random" ? "original" : "story_target";
            check(co.Coroutine.State == CoroutineState.Dead && value.String == expected && EventSelectionRuntime.Token == 0 && !EventSelectionRuntime.Waiting,
                "fresh confirmation after log closure executes the intended result and releases ownership");
            check(isSwitch ? manager.SwitchCalls == 1 && branch._count == 1 && branch.OriginalCalls == (request == "random" ? 1 : 0) &&
                    condition.GameplayReads == (request == "random" ? 1 : 0) :
                    manager.PositionCalls == 1 && manager.OriginalCalls == (request == "random" ? 1 : 0),
                "fresh normal/force/original confirmation produces exactly one real invocation");
        }
    }

    private static void ReservationsWaitForLogClosure()
    {
        foreach (bool isSwitch in new[] { false, true })
        {
            Begin("force");
            var co = Engine(isSwitch); co.Coroutine.Resume();
            EventCandidate candidate = Choice(isSwitch);
            long token = EventSelectionRuntime.Token;
            Queue(candidate, "force");
            EventSelectionRuntime.SetLogVisible(true);
            var reservation = isSwitch ?
                new CheckpointReservation(true, manager, manager._switchList[0], branch, branch, "branch", "2", count: branch._count) :
                new CheckpointReservation(false, manager, manager._position, position, other, "action", "story_other", "story");
            CheckpointReservationStore.Set(reservation);
            CheckpointReservation stored = CheckpointReservationStore.Current(isSwitch);
            int reads = condition.DisplayReads + target.DisplayCalls + other.DisplayCalls;
            SuspendedWithoutEffects(co, token, "a reservation made in the log cannot bypass the current popup until the log closes");
            check(ReferenceEquals(CheckpointReservationStore.Current(isSwitch), stored) && EventSelectionRuntime.Waiting,
                "log ownership leaves the exact reservation unconsumed while retaining popup ownership");
            EventSelectionRuntime.SetLogVisible(false);
            var value = co.Coroutine.Resume();
            check(co.Coroutine.State == CoroutineState.Dead && value.String == (isSwitch ? "second" : "story_other") &&
                CheckpointReservationStore.Current(isSwitch) == null && EventSelectionRuntime.Token == 0,
                "the log's reservation takes priority over the cancelled popup choice after closure");
            check(reads == condition.DisplayReads + target.DisplayCalls + other.DisplayCalls && condition.GameplayReads == 0 &&
                target.RealCalls == 0 && (isSwitch ? branch._count == 1 && branch.OriginalCalls == 0 : manager.OriginalCalls == 0),
                "deferred reservation executes without evaluating gameplay or display eligibility");
            if (isSwitch) manager.Switch("branch"); else manager.Position("action");
            check(isSwitch ? branch._count == 2 && branch.OriginalCalls == 1 && condition.GameplayReads == 1 : manager.OriginalCalls == 1,
                "the next actual call follows the original path because the deferred reservation was consumed once");
        }
    }

    private static void DisabledPopupWaitsForLogClosure()
    {
        foreach (bool isSwitch in new[] { false, true })
        foreach (string request in new[] { "normal", "force", "random" })
        {
            Begin(request);
            var co = Engine(isSwitch); co.Coroutine.Resume();
            long token = EventSelectionRuntime.Token;
            Queue(Choice(isSwitch), request);
            EventSelectionRuntime.SetLogVisible(true);
            EventSelectionRuntime.Enabled = EventSelectionRuntime.SwitchEnabled = () => false;
            EventSelectionRuntime.Tick(false, 1);
            check(!EventSelectionRuntime.Waiting && EventSelectionRuntime.Token == token,
                "disabling the popup may release its display while the log continues owning the pending Lua call");
            SuspendedWithoutEffects(co, token, "a released popup cannot advance its original game call behind the log");
            SuspendedWithoutEffects(co, token, "repeated released frames cannot run condition RNG or counters while the log is open");
            EventSelectionRuntime.SetLogVisible(false);
            var value = co.Coroutine.Resume();
            check(co.Coroutine.State == CoroutineState.Dead && value.String == (isSwitch ? "second" : "original") && EventSelectionRuntime.Token == 0,
                "disabled popup resumes the original decision once after log closure");
            check(isSwitch ? manager.SwitchCalls == 1 && branch._count == 1 && branch.OriginalCalls == 1 && condition.GameplayReads == 1 :
                    manager.PositionCalls == 1 && manager.OriginalCalls == 1,
                "automatic release after log closure preserves exactly one original invocation");
        }
    }

    private static void InvalidatedContextCannotResumeOldChoice()
    {
        foreach (bool isSwitch in new[] { false, true })
        foreach (string request in new[] { "normal", "force", "random" })
        {
            Begin(request);
            var old = Engine(isSwitch); old.Coroutine.Resume();
            EventCandidate candidate = Choice(isSwitch);
            Queue(candidate, request);
            EventSelectionRuntime.SetLogVisible(true);
            EventSelectionRuntime.Invalidate("load while log owns focus");
            check(!EventSelectionRuntime.Waiting && EventSelectionRuntime.Token == 0 && !EventSelectionRuntime.Select(candidate.Id, request == "force"),
                "context invalidation removes the old popup and its confirmation authority while the log remains open");
            SuspendedWithoutEffects(old, 0, "an old real Lua continuation cannot execute after a context invalidation behind the log");
            EventSelectionRuntime.SetLogVisible(false);
            SuspendedWithoutEffects(old, 0, "closing the log cannot resurrect a prior-context Lua request");
            var fresh = Engine(isSwitch); fresh.Coroutine.Resume();
            EventCandidate next = Choice(isSwitch);
            check(next.Id != candidate.Id && !EventSelectionRuntime.Select(candidate.Id, request == "force"),
                "new-context popup has fresh candidate authority and rejects the previous row identity");
            Queue(next, request);
            var value = fresh.Coroutine.Resume();
            check(fresh.Coroutine.State == CoroutineState.Dead && value.String ==
                (isSwitch ? request == "random" ? "second" : "first" : request == "random" ? "original" : "story_target") &&
                (isSwitch ? manager.SwitchCalls == 1 && branch._count == 1 : manager.PositionCalls == 1),
                "a new context still requires fresh confirmation and executes exactly once");
        }
    }

    internal static int Run(Action<bool, string> assertion)
    {
        check = assertion; scenarios = 0;
        RequestsNeedFreshConfirmation(); ReservationsWaitForLogClosure(); DisabledPopupWaitsForLogClosure(); InvalidatedContextCannotResumeOldChoice();
        EventSelectionRuntime.SetLogVisible(false);
        CheckpointReservationStore.Clear();
        EventSelectionRuntime.ReservationsEnabled = null;
        return scenarios;
    }
}
