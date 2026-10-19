using System;
using System.Linq;
using DiceMaster;
using DiceMaster.Config;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using DiceMaster.LogUI;
using MoonSharp.Interpreter;
using Mortal.Story;
using UnityEngine;

// Executes the real popup, runtime and MoonSharp continuation together. Native
// GUI capture, game data/evaluation and device polling remain explicit fixtures.
internal static class PopupExecutionRegression
{
    private static Action<bool, string> check;
    private sealed class Session
    {
        internal DicePlugin Plugin;
        internal EventPopupRenderer Popup => Plugin.eventPopup;
        internal readonly LogPadController Controller = new LogPadController();
        internal readonly PadState[] States = new PadState[4];
        internal readonly bool[] Connected = { true, false, false, false };
        internal LogInputMode Mode = LogInputMode.Gamepad;
        internal DynValue Coroutine;
        internal PositionEventData Target;
        internal CheckPointManager Manager;
        internal double Clock;

        internal void Update()
        {
            Plugin.RunModeChange(Mode);
            LogPadFrame frame = Controller.Tick(true, Application.isFocused, true, Mode, ++Clock, States, Connected);
            Plugin.RunPadReset(frame);
            Popup.UpdateInput();
            Popup.Handle(frame.Command);
        }
    }

    private static Session Start(bool force)
    {
        EventSelectionRuntime.Invalidate("popup integration setup");
        EventSelectionRuntime.Enabled = () => true;
        EventSelectionRuntime.WaitingChanged = null;
        EventResultPreview.Reads.Clear(); EventResultPreview.Reader = null;
        Input.Held.Clear(); Input.PointerSampleActive = false; Input.touchCount = 0;
        Application.isFocused = true; Screen.width = 1920; Screen.height = 1080;
        Time.unscaledTime = 0;
        ConfigManager.LogFontSize.Value = 20; ConfigManager.LogWindowX.Value = 37;
        GUILayout.ResetPointerCapture(); GUILayout.ButtonInput = null;
        var session = new Session { Plugin = new DicePlugin(),
            Target = new PositionEventData { name = "touch_target", ResultType = "touch_target", Eligible = !force } };
        var pool = new PositionResultData { name = "action", _scriptPrefix = "story" };
        pool.List.Add(session.Target);
        pool.List.Add(new PositionEventData { name = "other_target", ResultType = "other_target" });
        session.Manager = CheckPointManager.Instance = new CheckPointManager { _position = new PositionResultConfig() };
        session.Manager._position.List.Add(pool);
        UserData.RegisterType<CheckPointManager>();
        var engine = new Script(CoreModules.Preset_Complete);
        engine.Globals["checkpointmanager"] = session.Manager;
        long token = EventSelectionRuntime.NewScript();
        var context = new EventExecutionPatches.ScriptContext { Token = token, Epoch = EventSelectionRuntime.Epoch, Finalized = true };
        check(EventExecutionPatches.BindInterpreter(engine, context), "popup integration binds actual MoonSharp");
        string source = EventLuaGate.Instrument("return checkpointmanager.Position(\"action\")", token, out bool changed);
        check(changed, "popup integration instruments real Position expression");
        session.Coroutine = engine.CreateCoroutine(engine.LoadString(source).Function);
        session.Coroutine.Coroutine.Resume();
        check(EventSelectionRuntime.Waiting && session.Manager.PositionCalls == 0, "popup waits before original game call");
        GamepadBridge.PointerActivity = session.Controller.PointerActivity;
        GamepadBridge.KeyboardActivity = session.Controller.KeyboardActivity;
        session.Update(); session.Update();
        Draw(session.Popup);
        return session;
    }

    private static void Draw(EventPopupRenderer popup, EventType type = EventType.Repaint, Vector2 point = default, EventType? raw = null)
    {
        GUILayout.ResetLayout(Screen.width, Screen.height);
        Event.current = new Event { type = type, rawType = raw ?? type, button = 0, mousePosition = point };
        popup.Draw();
    }

    private static Vector2 Point(Session session)
    {
        Draw(session.Popup);
        var row = GUILayout.Controls.Single(control => control.Button && control.Text.Contains("touch_target"));
        return new Vector2(row.VisibleRect.x + row.VisibleRect.width / 2, row.VisibleRect.y + row.VisibleRect.height / 2);
    }

    private static void Press(Session session, (EventType Down, EventType Up, EventType RawDown, EventType RawUp) pair, Vector2 point, Action during = null)
    {
        Input.PointerSampleActive = true; Input.touchCount = pair.RawDown == EventType.TouchDown ? 1 : 0;
        Draw(session.Popup, pair.Down, point, pair.RawDown);
        Draw(session.Popup, EventType.Layout); Draw(session.Popup);
        during?.Invoke();
        Input.PointerSampleActive = false; Input.touchCount = 0;
        Draw(session.Popup, pair.Up, point, pair.RawUp);
    }

    private static void Select(Session session, (EventType Down, EventType Up, EventType RawDown, EventType RawUp) pair)
    {
        Press(session, pair, Point(session));
        session.Update();
        session.Coroutine.Coroutine.Resume();
        check(EventSelectionRuntime.Waiting && session.Manager.PositionCalls == 0,
            "first real pointer gesture only selects; it never resumes an event");
    }

    internal static int Run(Action<bool, string> assertion)
    {
        check = assertion;
        int scenarios = 0;
        var pairs = new[] {
            (EventType.MouseDown, EventType.MouseUp, EventType.MouseDown, EventType.MouseUp),
            (EventType.MouseDown, EventType.MouseUp, EventType.TouchDown, EventType.TouchUp),
            (EventType.TouchDown, EventType.TouchUp, EventType.TouchDown, EventType.TouchUp)
        };
        var activePressBoundaries = new[] {
            (ModalInputResetReason.AutomaticInputModeObserved, true),
            (ModalInputResetReason.PointerTakeover, true),
            (ModalInputResetReason.PhysicalPadTakeover, false),
            (ModalInputResetReason.ControllerBoundary, false),
            (ModalInputResetReason.LogVisibilityChanged, false),
            (ModalInputResetReason.ApplicationFocusChanged, false),
            (ModalInputResetReason.PopupWaitingChanged, false)
        };
        foreach (var pair in pairs)
        foreach (var boundary in activePressBoundaries)
        {
            scenarios++;
            Session session = Start(false); Select(session, pair);
            Press(session, pair, Point(session), () => session.Plugin.RunReset(boundary.Item1));
            DynValue result = session.Coroutine.Coroutine.Resume();
            if (!boundary.Item2)
            {
                check(EventSelectionRuntime.Waiting && session.Manager.PositionCalls == 0,
                    "hard pointer reset discards the unfinished down/up transaction: " + pair + " " + boundary.Item1);
                session.Update(); session.Update();
                Press(session, pair, Point(session));
                result = session.Coroutine.Coroutine.Resume();
            }
            check(result.String == "story_touch_target" && session.Coroutine.Coroutine.State == CoroutineState.Dead &&
                    session.Manager.PositionCalls == 1 && session.Manager.OriginalCalls == 0,
                "candidate reset policy preserves only deliberate active pointer transactions: " + pair + " " + boundary.Item1);
        }

        foreach (var pair in pairs)
        foreach (bool force in new[] { false, true })
        foreach (LogInputMode mode in new[] { LogInputMode.KeyboardMouse, LogInputMode.Unknown })
        foreach (string timing in new[] { "before-down", "between-down-up", "after-up" })
        {
            scenarios++;
            Session session = Start(force);
            Select(session, pair);
            Vector2 point = Point(session);
            Action modeChange = () => { session.Mode = mode; session.Update(); };
            if (timing == "before-down") modeChange();
            Press(session, pair, point, timing == "between-down-up" ? modeChange : null);
            if (timing == "after-up") modeChange();
            check(EventSelectionRuntime.Waiting && session.Manager.PositionCalls == 0,
                "accepted gesture is queued until Lua resumes: " + pair + " " + force + " " + timing);
            DynValue result = session.Coroutine.Coroutine.Resume();
            check(session.Coroutine.Coroutine.State == CoroutineState.Dead && result.String == "story_touch_target"
                && session.Manager.PositionCalls == 1 && session.Manager.OriginalCalls == 0,
                "one second gesture executes once across automatic scheme observation: " + pair + " force=" + force + " " + mode + " " + timing);
            check(!EventSelectionRuntime.Waiting && session.Target.RealCalls == 0,
                "actual result consumed without extra original condition/RNG calls");
            Press(session, pair, point);
            check(session.Manager.PositionCalls == 1, "late duplicate release/gesture cannot execute the completed action again");
        }

        foreach (var pair in pairs)
        {
            scenarios++;
            Session session = Start(false); Select(session, pair);
            Press(session, pair, Point(session));
            session.Plugin.RunReset(ModalInputResetReason.ApplicationFocusChanged);
            session.Coroutine.Coroutine.Resume();
            check(EventSelectionRuntime.Waiting && session.Manager.PositionCalls == 0,
                "a real hard cancellation still discards a completed pointer confirmation");
            check(!EventSelectionRuntime.Status.Contains("확인하고 있습니다"), "hard cancellation clears stale processing feedback");
            session.Update(); session.Update();
            Press(session, pair, Point(session));
            DynValue result = session.Coroutine.Coroutine.Resume();
            check(result.String == "story_touch_target" && session.Manager.PositionCalls == 1,
                "fresh confirmation after hard cancellation and neutral remains usable");
        }
        foreach (var pair in pairs)
        foreach (bool changeResult in new[] { false, true })
        foreach (bool refreshBeforeLua in new[] { false, true })
        {
            scenarios++;
            Session session = Start(false); Select(session, pair);
            Press(session, pair, Point(session));
            session.Mode = LogInputMode.KeyboardMouse; session.Update();
            if (changeResult) session.Target.ResultType = "changed_result";
            else session.Target.Eligible = false;
            if (refreshBeforeLua) EventSelectionRuntime.Tick(true, 1);
            session.Coroutine.Coroutine.Resume();
            check(EventSelectionRuntime.Waiting && session.Coroutine.Coroutine.State == CoroutineState.Suspended
                && session.Manager.PositionCalls == 0 && session.Manager.OriginalCalls == 0,
                "preserving a completed gesture never bypasses changed result/eligibility at Lua resume: " + pair + " result=" + changeResult + " refreshed=" + refreshBeforeLua);
            session.Target.ResultType = "touch_target"; session.Target.Eligible = true;
            EventSelectionRuntime.Tick(true, 10); session.Update();
            Draw(session.Popup);
            Press(session, pair, Point(session));
            DynValue result = session.Coroutine.Coroutine.Resume();
            check(result.String == "story_touch_target" && session.Manager.PositionCalls == 1,
                "changed result/eligibility needs a new valid gesture before one execution");
        }
        GamepadBridge.PointerActivity = GamepadBridge.KeyboardActivity = null;
        Input.PointerSampleActive = false; Input.touchCount = 0;
        return scenarios;
    }
}

namespace DiceMaster.LogUI
{
    // Only the full log window constraint is outside this popup execution test;
    // layout geometry is modeled by the separate managed popup fixtures;
    // native Unity rendering is not run here.
    internal static class LogRenderer
    {
        internal static Rect ConstrainWindow(Rect value, int width, int height) => value;
    }
}
