using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using DiceMaster.LogUI;
using UnityEngine;

namespace UnityEngine { public static class Application { public static bool isFocused = true; } }

// Only the runtime command receiver is substituted. These tests run the actual
// popup view against the existing managed Unity layout/input boundary.
namespace DiceMaster.EventSelection
{
    public sealed class EventCandidate
    {
        public string Id, Title, Result = "mock-result", State = "정상 발동 가능", Summary = "현재 조건 충족";
        public bool CanSelect, CanForce;
        public object Target = new object();
    }
    public static class EventSelectionRuntime
    {
        public static bool Waiting = true;
        public static long Token = 1;
        public static IList<EventCandidate> Candidates = new List<EventCandidate>();
        public static string Status = "현재 판정";
        public static int Commands, Attempts;
        public static string Selected;
        public static bool Forced, Random;
        public static bool Select(string id, bool force)
        {
            Attempts++;
            EventCandidate row = Candidates.FirstOrDefault(candidate => candidate.Id == id);
            // Match the runtime command boundary: a formerly eligible normal
            // request is revalidated and cannot become a force request.
            if (row == null || (force ? !row.CanForce : !row.CanSelect)) return false;
            Commands++; Selected = id; Forced = force; Random = false; return true;
        }
        public static void ContinueRandom() { Commands++; Random = true; }
    }
}

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new InvalidOperationException(message); }
    private static EventPopupRenderer Start()
    {
        EventSelectionRuntime.Token++;
        EventSelectionRuntime.Waiting = true;
        EventSelectionRuntime.Commands = EventSelectionRuntime.Attempts = 0;
        EventSelectionRuntime.Selected = null;
        EventSelectionRuntime.Random = EventSelectionRuntime.Forced = false;
        EventSelectionRuntime.Candidates = new List<EventCandidate> {
            new EventCandidate { Id = "eligible", Title = "정상 후보", CanSelect = true },
            new EventCandidate { Id = "blocked", Title = "미충족 후보", State = "발동 불가 · 조건 미충족", CanForce = true },
            new EventCandidate { Id = "broken", Title = "읽기 실패 후보" }
        };
        Input.Held.Clear(); Application.isFocused = true; Input.touchCount = 0; Input.PointerSampleActive = false;
        Screen.width = 1920; Screen.height = 1080;
        DiceMaster.Config.ConfigManager.LogFontSize.Value = 20;
        GUILayout.ResetPointerCapture(); GUILayout.ButtonInput = null;
        GamepadBridge.PointerActivity = null; GamepadBridge.KeyboardActivity = null; GamepadBridge.Cancelled = null;
        var popup = new EventPopupRenderer(); popup.UpdateInput(); return popup;
    }
    private static void Draw(EventPopupRenderer popup, EventType type = EventType.Repaint, KeyCode key = KeyCode.None, bool shift = false)
    {
        GUILayout.ResetLayout(Screen.width, Screen.height);
        Event.current = new Event { type = type, rawType = type, keyCode = key, shift = shift };
        popup.Draw();
    }
    private static void Key(EventPopupRenderer popup, KeyCode key, bool shift = false)
    { Draw(popup, EventType.KeyDown, key, shift); Draw(popup, EventType.KeyUp, key); }
    private static void Main()
    {
        var popup = Start();
        popup.Handle(LogPadCommand.Activate);
        Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Random, "Initial focus must use original random decision");

        popup = Start(); popup.Handle(LogPadCommand.Down); popup.Handle(LogPadCommand.Activate);
        Check(EventSelectionRuntime.Selected == "eligible" && !EventSelectionRuntime.Forced, "A must submit normal current candidate");
        popup = Start(); popup.Handle(LogPadCommand.Down); popup.Handle(LogPadCommand.Down);
        Draw(popup);
        Check(GUILayout.Buttons.Count(text => text.StartsWith("조건 무시하고 이번만 진행 [A / Enter]")) == 1
            && !GUILayout.Buttons.Any(text => text.StartsWith("이 이벤트로 진행 [A / Enter]"))
            && !GUILayout.LiteralLabels.Any(text => text.Contains("X/Shift+Enter")),
            "Blocked candidate must expose one contextual A/Enter action and no separate force shortcut");
        popup.Handle(LogPadCommand.Refresh);
        Check(EventSelectionRuntime.Commands == 0, "X must no longer request a forced event");
        popup.Handle(LogPadCommand.Activate);
        Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Forced && EventSelectionRuntime.Selected == "blocked", "A must explicitly force current readable blocked candidate");
        popup.Handle(LogPadCommand.Down); popup.Handle(LogPadCommand.Activate);
        Check(EventSelectionRuntime.Commands == 1, "Unreadable result must not accept A");
        popup.Handle(LogPadCommand.Refresh);
        Check(EventSelectionRuntime.Commands == 1, "Unreadable result must not accept X");
        popup.Handle(LogPadCommand.Back);
        Check(EventSelectionRuntime.Commands == 2 && EventSelectionRuntime.Random, "B must discard choice and request original decision");

        popup = Start(); Input.Held.Add(KeyCode.Return); popup.ResetInput(); popup.UpdateInput();
        Draw(popup, EventType.KeyDown, KeyCode.Return);
        Check(EventSelectionRuntime.Commands == 0, "Entry held Enter must not confirm popup");
        Input.Held.Clear(); Draw(popup, EventType.KeyUp, KeyCode.Return); popup.UpdateInput();
        Key(popup, KeyCode.DownArrow); Key(popup, KeyCode.DownArrow); Key(popup, KeyCode.Return, true);
        Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Forced, "Shift+Enter uses the same contextual A/Enter confirmation");
        Draw(popup, EventType.KeyDown, KeyCode.Return, true);
        Check(EventSelectionRuntime.Commands == 1, "Queued confirmation must require neutral before new key");

        popup = Start(); Key(popup, KeyCode.DownArrow);
        EventSelectionRuntime.Candidates = new List<EventCandidate> { new EventCandidate { Id = "replacement", Title = "교체", CanSelect = true } };
        Draw(popup, EventType.KeyDown, KeyCode.Return);
        Check(EventSelectionRuntime.Commands == 0, "Target removal must cancel keyboard authority");
        Draw(popup, EventType.KeyUp, KeyCode.Return);
        popup.UpdateInput(); Key(popup, KeyCode.Return);
        Check(EventSelectionRuntime.Random, "Removed target must return focus to random without auto execution");
        popup = Start(); Key(popup, KeyCode.DownArrow); EventSelectionRuntime.Token++;
        Draw(popup, EventType.KeyDown, KeyCode.Return);
        Check(EventSelectionRuntime.Commands == 0, "Old token input must never execute new decision");

        popup = Start(); Input.Held.Add(KeyCode.Escape); popup.ResetInput(); popup.UpdateInput();
        Key(popup, KeyCode.Escape);
        Check(EventSelectionRuntime.Commands == 0, "Log-close B/Esc equivalent must wait for release on return");
        Input.Held.Clear(); popup.UpdateInput(); Key(popup, KeyCode.Escape);
        Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Random, "New Esc after neutral must use random");

        popup = Start();
        EventSelectionRuntime.Candidates[0].Title = "<color=#BCA136>긴 한국어 제목</color>";
        Draw(popup);
        Check(GUILayout.Buttons.Any(b => b.Contains("긴 한국어 제목")) && GUILayout.Buttons.All(b => !b.Contains("<color=")),
            "Popup must show translated game names without raw rich text markup");

        PointerRegressions();
        RowReclickRegressions();
        PostReleaseTakeoverRegressions();
        SoftResetBarrierRegressions();
        KeyboardContextualConfirmation();

        foreach (var size in new[] { (640, 480), (1280, 720), (1920, 1200), (2560, 1440) })
        foreach (int font in new[] { 12, 20, 64 })
        {
            popup = Start();
            Screen.width = size.Item1; Screen.height = size.Item2;
            DiceMaster.Config.ConfigManager.LogFontSize.Value = font;
            EventSelectionRuntime.Candidates = Enumerable.Range(0, 50).Select(i => new EventCandidate {
                Id = i.ToString(), Title = new string('한', 120), Summary = new string('글', 300), CanSelect = true }).ToList();
            Draw(popup);
            Check(GUILayout.ScrollViews.Count == 2 && GUILayout.ScrollViews.All(s => s.Viewport.width > 0 && s.Viewport.height > 0), "List and detail must have usable scroll viewports");
            Check(GUILayout.Buttons.Any(b => b.StartsWith("무작위로 진행")), "Random recovery must remain present at every size");
            foreach (var control in GUILayout.Controls.Where(c => c.Button &&
                (c.Text.StartsWith("이 이벤트로 진행") || c.Text.StartsWith("조건 무시하고") || c.Text.StartsWith("무작위로 진행 ["))))
                Check(control.VisibleRect.height >= control.Rect.height - .1f,
                    "All explicit progress/recovery buttons must fit at " + size + " font=" + font);
            Check(EventSelectionRuntime.Commands == 0, "Rendering must never queue event execution");
        }
        Console.WriteLine("PASS: EventPopupTests: " + checks + " actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering.");
    }

    private static void KeyboardContextualConfirmation()
    {
        var changing = Start(); changing.Handle(LogPadCommand.Down); Draw(changing);
        EventCandidate original = EventSelectionRuntime.Candidates[0];
        EventSelectionRuntime.Candidates[0] = new EventCandidate { Id = original.Id, Title = original.Title,
            Target = original.Target, CanForce = true };
        changing.Handle(LogPadCommand.Activate);
        Check(EventSelectionRuntime.Commands == 0, "A cannot silently upgrade the last presented normal intent after eligibility refresh");
        Draw(changing); changing.Handle(LogPadCommand.Activate);
        Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Forced,
            "After the changed force action is presented, a fresh A explicitly bypasses the selected result conditions");

        foreach (bool shift in new[] { false, true })
        foreach (KeyCode key in new[] { KeyCode.Return, KeyCode.KeypadEnter })
        foreach (string candidate in new[] { "eligible", "blocked", "broken" })
        {
            var popup = Start();
            Key(popup, KeyCode.DownArrow);
            if (candidate != "eligible") Key(popup, KeyCode.DownArrow);
            if (candidate == "broken") Key(popup, KeyCode.DownArrow);
            Key(popup, key, shift);
            Check(candidate == "broken" ? EventSelectionRuntime.Commands == 0
                : EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Selected == candidate
                    && EventSelectionRuntime.Forced == (candidate == "blocked"),
                key + " shift=" + shift + " must use the contextual A semantics for " + candidate);
        }
    }

    private static void PostReleaseTakeoverRegressions()
    {
        var identity = typeof(EventPopupRenderer).GetField("pointerControl", BindingFlags.Instance | BindingFlags.NonPublic);
        var eventPairs = new[] {
            (EventType.MouseDown, EventType.MouseUp, EventType.MouseDown, EventType.MouseUp),
            (EventType.MouseDown, EventType.MouseUp, EventType.TouchDown, EventType.TouchUp),
            (EventType.TouchDown, EventType.TouchUp, EventType.TouchDown, EventType.TouchUp)
        };
        foreach (var pair in eventPairs)
        foreach (string candidate in new[] { "eligible", "blocked" })
        foreach (bool padConnected in new[] { false, true })
        {
            var popup = Start();
            var controller = new LogPadController(); var states = new PadState[4];
            bool[] connected = { padConnected, false, false, false };
            LogInputMode mode = padConnected ? LogInputMode.Gamepad : LogInputMode.KeyboardMouse;
            double time = 0;
            LogPadFrame Tick(PadState state)
            {
                states[0] = state;
                LogPadFrame frame = controller.Tick(true, true, true, mode, time += .1, states, connected);
                if (frame.ResetFocus) popup.ResetInput(frame.PreservePointerTransaction);
                popup.UpdateInput(); popup.Handle(frame.Command);
                return frame;
            }
            Tick(default); Tick(default);
            GamepadBridge.PointerActivity = controller.PointerActivity;
            Vector2 point = Point(popup, text => RowCaption(text, candidate));
            // A touch may still be sampled in its Ended frame; a mouse's next
            // down may already be sampled before its queued OnGUI callback.
            // Neither requires an empty-input Update between two fresh taps.
            bool touch = pair.Item3 == EventType.TouchDown;
            Input.touchCount = touch ? 1 : 0;
            Input.PointerSampleActive = !touch;
            Pointer(popup, pair.Item1, pair.Item3, point);
            Pointer(popup, pair.Item2, pair.Item4, point);
            Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                pair + " first tap only selects before delayed pointer takeover; pad=" + padConnected);

            // The first down's activity is processed only after its up has
            // cleared pointerControl. This ordering was absent from Click().
            PadState held = new PadState { Buttons = PadButtons.A };
            LogPadFrame takeover = Tick(held);
            Check(takeover.ResetFocus && takeover.PreservePointerTransaction
                && takeover.Command == LogPadCommand.None && takeover.Scroll == 0,
                "Post-up takeover consumes held pad input even without a connected pad: " + pair + " pad=" + padConnected);

            // Reuse the original screen point. Do not repaint/recenter or
            // provide a zero-input Update to accidentally unlock the popup.
            Pointer(popup, pair.Item1, pair.Item3, point);
            Check((string)identity.GetValue(popup) == "row:" + candidate,
                pair + " the next fresh tap captures the selected row without a neutral gap");
            Pointer(popup, pair.Item2, pair.Item4, point);
            Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Selected == candidate
                && EventSelectionRuntime.Forced == (candidate == "blocked"),
                pair + " exactly the second tap executes the presented action; pad=" + padConnected);
            Pointer(popup, pair.Item2, pair.Item4, point);
            Check(EventSelectionRuntime.Commands == 1,
                "Duplicate release after the second tap cannot execute again: " + pair);
            LogPadFrame submitted = Tick(held);
            Check(submitted.Command == LogPadCommand.None && submitted.Scroll == 0 && EventSelectionRuntime.Commands == 1,
                "Delayed pointer takeover after Submit cannot replay held A: " + pair);
            Key(popup, KeyCode.Return);
            Check(EventSelectionRuntime.Commands == 1,
                "A soft takeover after Submit retains the existing neutral barrier: " + pair);
        }
        Input.PointerSampleActive = false; Input.touchCount = 0;
        GamepadBridge.PointerActivity = null;
    }

    private static void SoftResetBarrierRegressions()
    {
        var neutral = typeof(EventPopupRenderer).GetField("neutral", BindingFlags.Instance | BindingFlags.NonPublic);
        var identity = typeof(EventPopupRenderer).GetField("pointerControl", BindingFlags.Instance | BindingFlags.NonPublic);
        var popup = Start();
        popup.ResetInput();
        Check((bool)neutral.GetValue(popup), "Hard reset starts the entry neutral barrier");
        popup.ResetInput(true);
        Check((bool)neutral.GetValue(popup), "Soft takeover cannot clear an existing hard entry barrier");
        Input.Held.Add(KeyCode.Return); popup.UpdateInput();
        Check((bool)neutral.GetValue(popup), "Held Enter keeps the entry barrier after a soft takeover");
        Input.Held.Clear(); popup.UpdateInput();
        Check(!(bool)neutral.GetValue(popup), "An actual neutral input sample releases the entry barrier");
        popup.Handle(LogPadCommand.Down); popup.Handle(LogPadCommand.Activate);
        Check((bool)neutral.GetValue(popup), "Submit starts its own neutral barrier");
        popup.ResetInput(true);
        Check((bool)neutral.GetValue(popup), "Soft takeover cannot clear the Submit barrier");

        foreach (string boundary in new[] { "focus", "token", "target", "result" })
        {
            popup = Start();
            Click(popup, text => RowCaption(text, "eligible"));
            Vector2 point = Point(popup, text => RowCaption(text, "eligible"));
            Input.touchCount = 1;
            Pointer(popup, EventType.TouchDown, EventType.TouchDown, point);
            Check(identity.GetValue(popup) != null, "Touch is captured before hard invalidation: " + boundary);
            if (boundary == "focus") popup.ResetInput();
            else if (boundary == "token") { EventSelectionRuntime.Token++; popup.UpdateInput(); }
            else if (boundary == "target") { EventSelectionRuntime.Candidates[0].Target = new object(); popup.ResetInput(true); }
            else { EventSelectionRuntime.Candidates[0].Result = "changed-result"; popup.ResetInput(true); }
            Check(identity.GetValue(popup) == null && (bool)neutral.GetValue(popup),
                "Hard boundary or invalid active target cancels touch and starts a neutral barrier: " + boundary);
            Pointer(popup, EventType.TouchUp, EventType.TouchUp, point);
            Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                "The stale touch release after invalidation cannot execute: " + boundary);
        }
        Input.touchCount = 0;
    }

    private static bool RowCaption(string text, string id) => text.Contains(id == "eligible" ? "정상 후보"
        : id == "blocked" ? "미충족 후보" : id == "broken" ? "읽기 실패 후보" : "원래 게임 판정");
    private static bool ActionCaption(string text) => text.StartsWith("이 이벤트로 진행 [") || text.StartsWith("조건 무시하고 이번만 진행 [") || text.StartsWith("진행 불가 · 대상/결과 확인 필요");
    private static Vector2 Point(EventPopupRenderer popup, Func<string, bool> match)
    {
        Draw(popup);
        GUILayout.ControlRecord control = GUILayout.Controls.Single(item => item.Button && match(item.Text));
        Check(control.VisibleRect.width > 0 && control.VisibleRect.height > 0, "Pointer test requires a visible control: " + control.Text);
        return new Vector2(control.VisibleRect.x + control.VisibleRect.width / 2, control.VisibleRect.y + control.VisibleRect.height / 2);
    }
    private static void Pointer(EventPopupRenderer popup, EventType type, EventType raw, Vector2 point)
    {
        GUILayout.ResetLayout(Screen.width, Screen.height);
        Event.current = new Event { type = type, rawType = raw, button = 0, mousePosition = point };
        popup.Draw();
    }
    private static void Click(EventPopupRenderer popup, Func<string, bool> match,
        EventType down = EventType.MouseDown, EventType up = EventType.MouseUp,
        EventType rawDown = EventType.MouseDown, EventType rawUp = EventType.MouseUp)
    {
        Vector2 point = Point(popup, match);
        Pointer(popup, down, rawDown, point);
        Pointer(popup, up, rawUp, point);
    }

    private static void ConsumedDownDoesNotRetarget()
    {
        var button = typeof(EventPopupRenderer).GetMethod("SafeButton", BindingFlags.Instance | BindingFlags.NonPublic);
        var identity = typeof(EventPopupRenderer).GetField("pointerControl", BindingFlags.Instance | BindingFlags.NonPublic);
        var deferred = typeof(EventPopupRenderer).GetField("deferred", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (bool translatedTouch in new[] { false, true })
        {
            var popup = Start();
            var style = new GUIStyle(GUI.skin.button) { fontSize = 20 };
            int originalActions = 0, laterActions = 0;
            void Buttons()
            {
                button.Invoke(popup, new object[] { "original", "original:", (Action)(() => originalActions++), style, 400f });
                button.Invoke(popup, new object[] { "later", "random", (Action)(() => laterActions++), style, 400f });
            }
            // Native DoControl consumes the first button's down. Unity's
            // subsequent Used-event GetRect returns dummy (0,0,1,1), which
            // also contains this legal first-control corner point.
            GUILayout.ResetLayout(Screen.width, Screen.height);
            Event.current = new Event { type = EventType.MouseDown,
                rawType = translatedTouch ? EventType.TouchDown : EventType.MouseDown,
                button = 0, mousePosition = new Vector2(.5f, .5f) };
            Buttons();
            Check(Event.current.type == EventType.Used && GUILayoutUtility.GetLastRect() == new Rect(0, 0, 1, 1),
                "Regression requires native button consumption and Unity's Used-event dummy rectangle");
            Check((string)identity.GetValue(popup) == "original:",
                "Later Used/raw-down controls must not overwrite the first consumed popup press identity");
            GUILayout.ResetLayout(Screen.width, Screen.height);
            Event.current = new Event { type = EventType.MouseUp,
                rawType = translatedTouch ? EventType.TouchUp : EventType.MouseUp,
                button = 0, mousePosition = new Vector2(.5f, .5f) };
            Buttons();
            Action accepted = (Action)deferred.GetValue(popup);
            Check(accepted != null, "Matching native release must still queue the original popup action");
            accepted();
            Check(originalActions == 1 && laterActions == 0,
                "Consumed-event propagation must retain exactly the original popup button transaction");
        }
    }

    private static void PointerRegressions()
    {
        ConsumedDownDoesNotRetarget();
        var eventPairs = new[] {
            (EventType.MouseDown, EventType.MouseUp, EventType.MouseDown, EventType.MouseUp),
            (EventType.MouseDown, EventType.MouseUp, EventType.TouchDown, EventType.TouchUp),
            (EventType.TouchDown, EventType.TouchUp, EventType.TouchDown, EventType.TouchUp)
        };
        foreach (var pair in eventPairs)
        foreach (string candidate in new[] { "eligible", "blocked", "broken" })
        {
            var popup = Start();
            Click(popup, text => RowCaption(text, candidate), pair.Item1, pair.Item2, pair.Item3, pair.Item4);
            Check(EventSelectionRuntime.Commands == 0, "Pointer row selection must not itself execute a result");
            Draw(popup);
            Check(GUILayout.Buttons.Any(text => text.StartsWith("▶ ") && RowCaption(text, candidate)),
                pair + " selects the touched popup row after Unity consumes the pointer event");
            Click(popup, ActionCaption, pair.Item1, pair.Item2, pair.Item3, pair.Item4);
            Check(candidate == "broken" ? EventSelectionRuntime.Commands == 0
                : EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Selected == candidate
                    && EventSelectionRuntime.Forced == (candidate == "blocked"),
                pair + " confirms the contextual action exactly once for " + candidate);
        }

        foreach (var pair in eventPairs)
        foreach (string modeChange in new[] { "gamepad-stays", "gamepad-to-mouse", "unknown" })
        foreach (PadState held in new[] { new PadState { Buttons = PadButtons.A }, new PadState { Buttons = PadButtons.B },
            new PadState { Buttons = PadButtons.Up, RightY = 32767 }, new PadState { LeftX = 32767, RightTrigger = 255 } })
        {
            var popup = Start();
            var controller = new LogPadController(); var states = new PadState[4]; bool[] connected = { true, false, false, false };
            LogInputMode mode = modeChange == "unknown" ? LogInputMode.Unknown : LogInputMode.Gamepad;
            double time = 0;
            LogPadFrame Tick(PadState state)
            {
                states[0] = state;
                LogPadFrame frame = controller.Tick(true, true, true, mode, time += .1, states, connected);
                if (frame.ResetFocus) popup.ResetInput(frame.PreservePointerTransaction);
                popup.UpdateInput(); popup.Handle(frame.Command);
                return frame;
            }
            Tick(default); Tick(default);
            GamepadBridge.PointerActivity = controller.PointerActivity;
            GamepadBridge.KeyboardActivity = controller.KeyboardActivity;
            foreach (string action in new[] { "row", "confirm" })
            {
                Func<string, bool> caption = action == "row" ? text => RowCaption(text, "blocked") : ActionCaption;
                Vector2 point = Point(popup, caption);
                Input.touchCount = 1;
                Pointer(popup, pair.Item1, pair.Item3, point);
                Check(Event.current.type == EventType.Used, "Unity button consumes actual " + pair.Item1 + " before renderer finishes");
                if (modeChange == "gamepad-to-mouse") mode = LogInputMode.KeyboardMouse;
                LogPadFrame takeover = Tick(held);
                Check(takeover.ResetFocus && takeover.PreservePointerTransaction && takeover.Command == LogPadCommand.None && takeover.Scroll == 0,
                    "Actual pointer takeover Tick preserves transaction while consuming held pad input: " + modeChange);
                Input.touchCount = 0;
                Pointer(popup, pair.Item2, pair.Item4, point);
                Draw(popup);
                Check(action == "row" ? EventSelectionRuntime.Commands == 0
                        && GUILayout.Buttons.Any(text => text.StartsWith("▶ ") && RowCaption(text, "blocked"))
                    : EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Selected == "blocked" && EventSelectionRuntime.Forced,
                    "Down, actual Update reset, Up must complete the same popup " + action + " transaction");
                for (int frame = 0; frame < 3; frame++)
                {
                    LogPadFrame repeat = Tick(held);
                    Check(repeat.Command == LogPadCommand.None && repeat.Scroll == 0
                        && EventSelectionRuntime.Commands == (action == "row" ? 0 : 1),
                        "Held pad inputs cannot become a second popup action after pointer takeover");
                }
                Tick(default);
            }
        }

        foreach (string boundary in new[] { "focus", "ready", "disconnect", "close", "explicit-cancel", "context", "target", "waiting", "direct-reset" })
        {
            var popup = Start(); popup.Handle(LogPadCommand.Down);
            var controller = new LogPadController(); var states = new PadState[4]; bool[] connected = { true, false, false, false };
            controller.Tick(true, true, true, LogInputMode.Gamepad, 0, states, connected);
            controller.Tick(true, true, true, LogInputMode.Gamepad, .1, states, connected);
            GamepadBridge.PointerActivity = controller.PointerActivity;
            Vector2 point = Point(popup, ActionCaption);
            Input.touchCount = 1; Pointer(popup, EventType.MouseDown, EventType.TouchDown, point);
            if (boundary == "context") EventSelectionRuntime.Token++;
            else if (boundary == "target") EventSelectionRuntime.Candidates[0] = new EventCandidate { Id = "eligible", Title = "정상 후보", CanSelect = true };
            else if (boundary == "waiting") { EventSelectionRuntime.Waiting = false; popup.UpdateInput(); EventSelectionRuntime.Waiting = true; }
            else if (boundary == "direct-reset") popup.ResetInput();
            else
            {
                if (boundary == "explicit-cancel") controller.Cancel();
                if (boundary == "disconnect") connected[0] = false;
                LogPadFrame hard = controller.Tick(boundary != "close", boundary != "focus", boundary != "ready",
                    LogInputMode.KeyboardMouse, .2, states, connected);
                Check(hard.ResetFocus && !hard.PreservePointerTransaction, "Actual hard " + boundary + " must override simultaneous pointer/mode soft reset");
                popup.ResetInput(hard.PreservePointerTransaction);
            }
            popup.UpdateInput(); Input.touchCount = 0;
            Pointer(popup, EventType.MouseUp, EventType.TouchUp, point);
            Check(EventSelectionRuntime.Commands == 0, "Hard " + boundary + " must cancel prior popup press");
        }

        // Same-target publications may refresh eligibility between down and up.
        // The rendered normal/force intent is bound on down, and never silently
        // upgraded to bypassing conditions when the release sees changed data.
        foreach (string change in new[] { "same-target-snapshot", "normal-to-blocked", "force-to-normal" })
        {
            var popup = Start(); popup.Handle(LogPadCommand.Down);
            if (change == "force-to-normal") popup.Handle(LogPadCommand.Down);
            Vector2 point = Point(popup, ActionCaption);
            Input.touchCount = 1; Pointer(popup, EventType.MouseDown, EventType.MouseDown, point);
            int index = change == "force-to-normal" ? 1 : 0;
            EventCandidate original = EventSelectionRuntime.Candidates[index];
            bool eligible = change != "normal-to-blocked";
            EventSelectionRuntime.Candidates[index] = new EventCandidate { Id = original.Id, Title = original.Title,
                Target = original.Target, CanSelect = eligible, CanForce = !eligible };
            popup.ResetInput(true); popup.UpdateInput(); Input.touchCount = 0;
            Pointer(popup, EventType.MouseUp, EventType.MouseUp, point);
            Check(change == "same-target-snapshot" ? EventSelectionRuntime.Commands == 1 && !EventSelectionRuntime.Forced
                : EventSelectionRuntime.Commands == 0,
                "Eligibility refresh preserves only the exact captured action intent: " + change);
        }

        var rawTouchPopup = Start(); rawTouchPopup.Handle(LogPadCommand.Down);
        Vector2 rawTouch = Point(rawTouchPopup, ActionCaption);
        Pointer(rawTouchPopup, EventType.TouchDown, EventType.TouchDown, rawTouch);
        rawTouchPopup.ResetInput(true);
        Pointer(rawTouchPopup, EventType.TouchUp, EventType.TouchUp, rawTouch);
        Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Selected == "eligible" && !EventSelectionRuntime.Forced,
            "Direct touch adapter preserves Unity button capture through pointer takeover and confirms exactly once");
        Pointer(rawTouchPopup, EventType.TouchUp, EventType.TouchUp, rawTouch);
        Check(EventSelectionRuntime.Commands == 1, "Direct touch adapter cannot confirm a duplicate release");

        foreach (string shortcut in new[] { "A / Enter", "B / Esc" })
        {
            var randomPopup = Start();
            Click(randomPopup, text => text == "무작위로 진행 [" + shortcut + "]");
            Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Random,
                "Pointer random " + shortcut + " action preserves original game result path");
        }
        var outsidePopup = Start(); outsidePopup.Handle(LogPadCommand.Down);
        Vector2 inside = Point(outsidePopup, ActionCaption);
        Pointer(outsidePopup, EventType.MouseUp, EventType.MouseUp, inside);
        Check(EventSelectionRuntime.Commands == 0, "Release without corresponding popup press never confirms");
        Pointer(outsidePopup, EventType.MouseDown, EventType.MouseDown, new Vector2(-50, -50));
        Pointer(outsidePopup, EventType.MouseUp, EventType.MouseUp, inside);
        Check(EventSelectionRuntime.Commands == 0, "Outside press cannot manufacture a popup confirmation on release inside");
        GamepadBridge.PointerActivity = null; GamepadBridge.KeyboardActivity = null; GamepadBridge.Cancelled = null;
        Input.Held.Clear(); Input.touchCount = 0;
    }

    private static void RowReclickRegressions()
    {
        var eventPairs = new[] {
            (EventType.MouseDown, EventType.MouseUp, EventType.MouseDown, EventType.MouseUp),
            (EventType.MouseDown, EventType.MouseUp, EventType.TouchDown, EventType.TouchUp),
            (EventType.TouchDown, EventType.TouchUp, EventType.TouchDown, EventType.TouchUp)
        };
        foreach (var pair in eventPairs)
        {
            foreach (string candidate in new[] { "eligible", "blocked", "broken" })
            {
                var popup = Start();
                Click(popup, text => RowCaption(text, candidate), pair.Item1, pair.Item2, pair.Item3, pair.Item4);
                Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                    pair + " first row click selects without requesting an event: " + candidate);
                Draw(popup);
                Check(GUILayout.Buttons.Any(text => text.StartsWith("▶ ") && RowCaption(text, candidate)
                    && text.Contains("다시 클릭·터치:")),
                    "Selected row advertises its explicit repeat-click action: " + candidate);
                Click(popup, text => RowCaption(text, candidate), pair.Item1, pair.Item2, pair.Item3, pair.Item4);
                Check(candidate == "broken" ? EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0
                    : EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Selected == candidate
                        && EventSelectionRuntime.Forced == (candidate == "blocked"),
                    pair + " a fresh second row click uses the presented contextual action: " + candidate);
            }

            var randomPopup = Start();
            Click(randomPopup, text => RowCaption(text, "random"), pair.Item1, pair.Item2, pair.Item3, pair.Item4);
            Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Random,
                pair + " selected random row click requests the original game decision");

            foreach (string change in new[] { "same-target-snapshot", "normal-to-blocked", "force-to-normal", "token", "target", "result", "removed" })
            {
                var popup = Start();
                string candidate = change == "force-to-normal" ? "blocked" : "eligible";
                Click(popup, text => RowCaption(text, candidate), pair.Item1, pair.Item2, pair.Item3, pair.Item4);
                Vector2 point = Point(popup, text => RowCaption(text, candidate));
                Pointer(popup, pair.Item1, pair.Item3, point);
                int index = candidate == "blocked" ? 1 : 0;
                EventCandidate original = EventSelectionRuntime.Candidates[index];
                if (change == "token") EventSelectionRuntime.Token++;
                else if (change == "removed") EventSelectionRuntime.Candidates.RemoveAt(index);
                else
                {
                    bool eligible = change != "normal-to-blocked";
                    EventSelectionRuntime.Candidates[index] = new EventCandidate {
                        Id = original.Id, Title = original.Title, Target = change == "target" ? new object() : original.Target,
                        Result = change == "result" ? "changed-result" : original.Result,
                        CanSelect = eligible, CanForce = !eligible
                    };
                }
                // A repaint must not upgrade the press into the newly shown
                // force/normal action, even when the row ID stays unchanged.
                Draw(popup);
                popup.ResetInput(true); popup.UpdateInput();
                Pointer(popup, pair.Item2, pair.Item4, point);
                Check(change == "same-target-snapshot" ? EventSelectionRuntime.Commands == 1 && !EventSelectionRuntime.Forced
                    : EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                    pair + " selected-row press retains target/result/action intent across " + change);
                if (change == "normal-to-blocked" || change == "force-to-normal")
                {
                    popup.UpdateInput();
                    Click(popup, text => RowCaption(text, candidate), pair.Item1, pair.Item2, pair.Item3, pair.Item4);
                    Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Selected == candidate
                        && EventSelectionRuntime.Forced == (change == "normal-to-blocked"),
                        "A fresh selected-row click after the changed action is shown accepts that action: " + change);
                }
            }

            foreach (string invalid in new[] { "duplicate-up", "outside-down", "release-other-row", "unselected-cross-row" })
            {
                var popup = Start();
                if (invalid != "unselected-cross-row")
                    Click(popup, text => RowCaption(text, "eligible"), pair.Item1, pair.Item2, pair.Item3, pair.Item4);
                Vector2 eligiblePoint = Point(popup, text => RowCaption(text, "eligible"));
                Vector2 blockedPoint = Point(popup, text => RowCaption(text, "blocked"));
                Pointer(popup, pair.Item1, pair.Item3, invalid == "outside-down" ? new Vector2(-50, -50) : eligiblePoint);
                Pointer(popup, pair.Item2, pair.Item4, invalid.Contains("cross-row") || invalid == "release-other-row" ? blockedPoint : eligiblePoint);
                if (invalid == "duplicate-up")
                {
                    Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Selected == "eligible",
                        pair + " matching selected-row release submits exactly once");
                    Pointer(popup, pair.Item2, pair.Item4, eligiblePoint);
                    Check(EventSelectionRuntime.Commands == 1, pair + " duplicate row release cannot submit again");
                }
                else Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                    pair + " invalid selected-row gesture cannot submit: " + invalid);
            }

            var takeoverPopup = Start();
            var controller = new LogPadController(); var states = new PadState[4]; bool[] connected = { true, false, false, false };
            controller.Tick(true, true, true, LogInputMode.Gamepad, 0, states, connected);
            controller.Tick(true, true, true, LogInputMode.Gamepad, .1, states, connected);
            GamepadBridge.PointerActivity = controller.PointerActivity;
            Vector2 firstRow = Point(takeoverPopup, text => RowCaption(text, "eligible"));
            Pointer(takeoverPopup, pair.Item1, pair.Item3, firstRow);
            states[0] = new PadState { Buttons = PadButtons.A };
            LogPadFrame frame = controller.Tick(true, true, true, LogInputMode.Gamepad, .2, states, connected);
            Check(frame.ResetFocus && frame.PreservePointerTransaction && frame.Command == LogPadCommand.None,
                "Pointer takeover in gamepad mode consumes held A before the first row selection");
            takeoverPopup.ResetInput(frame.PreservePointerTransaction); takeoverPopup.UpdateInput();
            Pointer(takeoverPopup, pair.Item2, pair.Item4, firstRow);
            Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                pair + " first pointer row selection never executes in gamepad mode with held A");
            Draw(takeoverPopup);
            Check(GUILayout.Buttons.Any(text => text.StartsWith("▶ ") && RowCaption(text, "eligible")),
                "Pointer takeover completes selection in gamepad mode");
            GamepadBridge.PointerActivity = null;
        }

        NonInputPhaseRegressions(eventPairs);
    }

    private static void NonInputPhaseRegressions((EventType Down, EventType Up, EventType RawDown, EventType RawUp)[] eventPairs)
    {
        var identity = typeof(EventPopupRenderer).GetField("pointerControl", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var pair in eventPairs)
        foreach (EventType phase in new[] { EventType.Layout, EventType.Repaint, EventType.Ignore })
        {
            var popup = Start();
            Click(popup, text => RowCaption(text, "eligible"), pair.Down, pair.Up, pair.RawDown, pair.RawUp);
            Vector2 eligiblePoint = Point(popup, text => RowCaption(text, "eligible"));
            Pointer(popup, phase, pair.RawDown, eligiblePoint);
            Check(identity.GetValue(popup) == null,
                phase + " carrying raw " + pair.RawDown + " cannot capture a popup row transaction");
            Pointer(popup, pair.Up, pair.RawUp, eligiblePoint);
            Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                "A release after a noninput raw-down phase cannot submit: " + phase + " " + pair);

            popup = Start();
            Click(popup, text => RowCaption(text, "eligible"), pair.Down, pair.Up, pair.RawDown, pair.RawUp);
            eligiblePoint = Point(popup, text => RowCaption(text, "eligible"));
            Vector2 otherPoint = Point(popup, text => RowCaption(text, "blocked"));
            Pointer(popup, pair.Down, pair.RawDown, eligiblePoint);
            Check((string)identity.GetValue(popup) == "row:eligible", "Real pointer down captures selected row before noninput passes");
            Pointer(popup, phase, pair.RawDown, otherPoint);
            Check((string)identity.GetValue(popup) == "row:eligible",
                phase + " carrying raw " + pair.RawDown + " cannot retarget a valid active press");
            Pointer(popup, phase, pair.RawUp, eligiblePoint);
            Check((string)identity.GetValue(popup) == "row:eligible" && EventSelectionRuntime.Commands == 0,
                phase + " carrying raw " + pair.RawUp + " cannot clear or submit a valid active press");
            Pointer(popup, pair.Up, pair.RawUp, eligiblePoint);
            Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Selected == "eligible" && !EventSelectionRuntime.Forced,
                "The real release after noninput phases completes the original press exactly once: " + phase + " " + pair);
            Pointer(popup, pair.Up, pair.RawUp, eligiblePoint);
            Check(EventSelectionRuntime.Commands == 1, "Duplicate release after noninput phases cannot repeat the action");
        }
    }

}
