using System;
using System.Linq;
using System.Reflection;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using DiceMaster.LogUI;
using UnityEngine;

internal static partial class Program
{
    private static float DetailScroll(EventPopupRenderer popup) => (float)typeof(EventPopupRenderer)
        .GetField("detailScroll", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup);

    private static void ResultPreviewUiRegressions()
    {
        ResultPreviewPresentation();
        ResultPreviewLayoutAndScrolling();
        PopupModeTransitionRegressions();
    }

    private static void PopupModeTransitionRegressions()
    {
        var pairs = new[] {
            (EventType.MouseDown, EventType.MouseUp, EventType.MouseDown, EventType.MouseUp),
            (EventType.MouseDown, EventType.MouseUp, EventType.TouchDown, EventType.TouchUp),
            (EventType.TouchDown, EventType.TouchUp, EventType.TouchDown, EventType.TouchUp)
        };
        foreach (var pair in pairs)
        foreach (bool padConnected in new[] { false, true })
        {
            var popup = Start(); var controller = new LogPadController(); var states = new PadState[4];
            bool[] connected = { padConnected, false, false, false }; double time = 0;
            LogPadFrame Tick(LogInputMode mode, PadState state)
            {
                states[0] = state;
                LogPadFrame frame = controller.Tick(true, true, true, mode, time += .1, states, connected);
                if (frame.ResetFocus) popup.ResetInput(frame.PreservePointerTransaction);
                popup.UpdateInput(); popup.Handle(frame.Command); popup.Scroll(frame.Scroll);
                return frame;
            }
            Tick(LogInputMode.Gamepad, default); Tick(LogInputMode.Gamepad, default);
            GamepadBridge.PointerActivity = controller.PointerActivity;
            Vector2 point = Point(popup, text => RowCaption(text, "eligible"));
            Input.touchCount = pair.Item3 == EventType.TouchDown ? 1 : 0;
            Input.PointerSampleActive = pair.Item3 == EventType.MouseDown;
            PadState held = new PadState { Buttons = PadButtons.A };
            // Update observes the new device scheme before OnGUI receives
            // that same physical down. This is distinct from post-up takeover.
            LogPadFrame modeChange = Tick(LogInputMode.KeyboardMouse, held);
            Check(modeChange.ResetFocus && modeChange.PreservePointerTransaction
                && !modeChange.PreserveNavigationArea && modeChange.Command == LogPadCommand.None,
                "An early mode-only transition preserves the popup's first pointer gesture: " + pair + " pad=" + padConnected);
            Pointer(popup, pair.Item1, pair.Item3, point); Pointer(popup, pair.Item2, pair.Item4, point);
            Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0
                && (string)typeof(EventPopupRenderer).GetField("selected", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup) == "eligible",
                "The first tap selects after Update saw its scheme change before down: " + pair);
            Tick(LogInputMode.KeyboardMouse, held);
            Pointer(popup, pair.Item1, pair.Item3, point); Pointer(popup, pair.Item2, pair.Item4, point);
            Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Selected == "eligible" && !EventSelectionRuntime.Forced,
                "The second same-point tap executes after early mode change and late post-up takeover: " + pair);
        }

        foreach (var pair in pairs)
        {
            var popup = Start(); var controller = new LogPadController(); var states = new PadState[4];
            bool[] connected = { true, false, false, false }; double time = 0;
            LogPadFrame Tick(PadState state)
            {
                states[0] = state;
                LogPadFrame frame = controller.Tick(true, true, true, LogInputMode.KeyboardMouse, time += .1, states, connected);
                if (frame.ResetFocus) popup.ResetInput(frame.PreservePointerTransaction);
                popup.UpdateInput(); popup.Handle(frame.Command); popup.Scroll(frame.Scroll);
                return frame;
            }
            Tick(default); Tick(default); popup.Handle(LogPadCommand.Down);
            GamepadBridge.PointerActivity = controller.PointerActivity;
            Vector2 point = Point(popup, text => RowCaption(text, "eligible"));
            Input.touchCount = pair.Item3 == EventType.TouchDown ? 1 : 0;
            Input.PointerSampleActive = pair.Item3 == EventType.MouseDown;
            Pointer(popup, pair.Item1, pair.Item3, point);
            Tick(default); // Consume pointer takeover and rearm a neutral pad.
            LogPadFrame reacquire = Tick(new PadState { Buttons = PadButtons.A });
            Check(reacquire.ResetFocus && reacquire.PreserveNavigationArea && !reacquire.PreservePointerTransaction
                && reacquire.Command == LogPadCommand.None,
                "Physical pad reacquisition is a hard popup input boundary even when log tools are retained: " + pair);
            Check((bool)typeof(EventPopupRenderer).GetField("neutral", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup)
                && typeof(EventPopupRenderer).GetField("pointerControl", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup) == null,
                "Physical pad reacquisition cancels the old popup press and keeps the held-input barrier: " + pair);
            Pointer(popup, pair.Item2, pair.Item4, point);
            Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                "A touch release abandoned by physical pad reacquisition cannot execute: " + pair);
        }
        Input.touchCount = 0; Input.PointerSampleActive = false;
        GamepadBridge.PointerActivity = null;
    }

    private static void ResultPreviewPresentation()
    {
        var popup = Start();
        EventCandidate candidate = EventSelectionRuntime.Candidates[0];
        candidate.ResultSummary = "동전 +100 · 심상 -2";
        candidate.ResultDetail = "동전 +100\n심상 -2\n가능한 변화\n  호감도 +3";
        candidate.Summary = "표시 조건 설명 · 가중치 20";
        Draw(popup);
        Check(GUILayout.Buttons.Any(text => RowCaption(text, "eligible") && text.Contains(candidate.ResultSummary)),
            "Every candidate row exposes its prepared result summary before selection");
        Check(GUILayout.LiteralLabels.Contains("이벤트를 선택하면 예상 결과와 발동 조건을 확인할 수 있습니다.")
            && !GUILayout.LiteralLabels.Any(text => text.Contains(candidate.ResultDetail)),
            "No selection presents a reading hint without implying any chosen result");
        popup.Handle(LogPadCommand.Down); Draw(popup);
        Check(GUILayout.LiteralLabels.IndexOf("예상 결과") < GUILayout.LiteralLabels.IndexOf(candidate.ResultDetail)
            && GUILayout.LiteralLabels.IndexOf(candidate.ResultDetail) < GUILayout.LiteralLabels.IndexOf("발동 조건")
            && GUILayout.LiteralLabels.IndexOf("발동 조건") < GUILayout.LiteralLabels.IndexOf(candidate.Summary),
            "Selected detail presents expected results first and the separate eligibility explanation afterward");
        Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
            "Presenting result summaries/details never requests gameplay execution");

        foreach (string explanation in new[] { "결과는 진행 후 확인", "결과 설명 읽기 실패", "확인된 효과 없음", "추가 영향 가능\n동전 +100" })
        {
            popup = Start(); candidate = EventSelectionRuntime.Candidates[0];
            candidate.ResultSummary = candidate.ResultDetail = explanation;
            popup.Handle(LogPadCommand.Down); Draw(popup); Draw(popup);
            Check(GUILayout.LiteralLabels.Any(text => text.Contains(explanation)),
                "Prepared preview status is preserved as supplied: " + explanation);
            popup.Handle(LogPadCommand.Activate);
            Check(EventSelectionRuntime.Commands == 1 && !EventSelectionRuntime.Forced,
                "Result-preview availability never changes normal eligibility: " + explanation);
        }

        popup = Start(); candidate = EventSelectionRuntime.Candidates[1];
        candidate.ResultSummary = candidate.ResultDetail = "결과 설명 읽기 실패 · 실행 여부와 별도";
        popup.Handle(LogPadCommand.Down); popup.Handle(LogPadCommand.Down); Draw(popup);
        Check(GUILayout.LiteralLabels.Contains(candidate.ResultDetail) && GUILayout.LiteralLabels.Contains(candidate.Summary),
            "Readable blocked candidates retain separate result-preview failure and eligibility explanations");
        popup.Handle(LogPadCommand.Activate);
        Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Forced,
            "A failed result preview does not remove an otherwise valid explicit force action");

        popup = Start(); popup.Handle(LogPadCommand.Down); Draw(popup);
        candidate = EventSelectionRuntime.Candidates[0];
        EventSelectionRuntime.Candidates[0] = new EventCandidate { Id = candidate.Id, Title = candidate.Title,
            Target = new object(), Result = "replacement-result", CanSelect = true,
            ResultSummary = "교체 결과 요약", ResultDetail = "교체 결과 상세" };
        Draw(popup);
        Check(GUILayout.LiteralLabels.Any(text => text.Contains("교체 결과 상세"))
            && GUILayout.LiteralLabels.All(text => !text.Contains("예상 효과 상세")),
            "A replaced target/result publishes its new prepared description without retaining old detail text");
    }

    private static void ResultPreviewLayoutAndScrolling()
    {
        foreach (var dimensions in new[] { (640, 480), (1280, 720), (1920, 1080), (3840, 2160) })
        foreach (int font in new[] { 12, 20, 64 })
        {
            var popup = Start(); Screen.width = dimensions.Item1; Screen.height = dimensions.Item2;
            DiceMaster.Config.ConfigManager.LogFontSize.Value = font;
            DiceMaster.Config.ConfigManager.LogWindowX.Value = 37; // Keep this explicit font rather than a startup/default seed.
            EventCandidate candidate = EventSelectionRuntime.Candidates[0];
            candidate.Title = "정상 후보 " + new string('한', 150);
            candidate.ResultSummary = "동전 +100\n심상 -2 " + new string('글', 500);
            candidate.ResultDetail = string.Join("\n", Enumerable.Range(0, 180).Select(i => "결과 " + i + ": 동전 +100 · 호감도 +3 · 분기 이후 가능한 변화"));
            candidate.Summary = new string('조', 400);
            Draw(popup);
            GUILayout.ControlRecord before = GUILayout.Controls.Where(control => control.Button && control.Text.Split('\n').Length == 2).First();
            Check(before.Text.Split('\n').Length == 2 && before.Text.Contains("…"),
                "Long titles and result summaries remain two bounded lines at " + dimensions + " font=" + font);
            popup.Handle(LogPadCommand.Down); Draw(popup); Draw(popup);
            GUILayout.ControlRecord after = GUILayout.Controls.Where(control => control.Button && control.Text.Split('\n').Length == 2).First();
            Check(Math.Abs(before.Rect.height - after.Rect.height) < .1f && Math.Abs(before.Rect.y - after.Rect.y) < .1f,
                "Selecting a candidate retains its row height/position for the next tap at " + dimensions + " font=" + font);
            Check(GUILayout.ScrollViews.Count == 2 && GUILayout.ScrollViews.All(view => view.Viewport.height > 0),
                "Results and candidates have independent usable scroll viewports at " + dimensions + " font=" + font);
            foreach (GUILayout.ControlRecord control in GUILayout.Controls.Where(control => InstructionCaption(control.Text)))
                Check(control.VisibleRect.height >= control.Rect.height - .1f && control.VisibleRect.width > 0,
                    "Confirm/recovery text instructions remain visible beneath long result detail at " + dimensions + " font=" + font);
            float originalList = GUILayout.ScrollViews[0].InputScroll.y;
            popup.Scroll(1); Draw(popup);
            Check(DetailScroll(popup) > 0 && GUILayout.ScrollViews[0].InputScroll.y == originalList,
                "The pad right-stick scroll value moves only the result detail at " + dimensions + " font=" + font);
            for (int index = 0; index < 2000; index++) popup.Scroll(-1);
            Check(DetailScroll(popup) == 0, "Result detail cannot scroll above its beginning");
            popup.Scroll(float.NaN); popup.Scroll(float.PositiveInfinity);
            Check(DetailScroll(popup) == 0, "Invalid scroll samples cannot poison result geometry");
            popup.ResetInput(); popup.Scroll(1);
            Check(DetailScroll(popup) == 0, "Held entry input cannot scroll through a hard neutral barrier");
            popup.UpdateInput(); popup.Scroll(1); popup.Handle(LogPadCommand.Down); Draw(popup);
            Check(DetailScroll(popup) == 0, "Selecting a different candidate resets its result detail to the beginning");
            Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                "Result reading and scrolling never confirm an event");
        }

        var wheelPopup = Start(); wheelPopup.Handle(LogPadCommand.Down);
        EventSelectionRuntime.Candidates[0].ResultDetail = new string('결', 6000);
        Draw(wheelPopup);
        GUILayout.ControlRecord body = GUILayout.Controls.Single(control => !control.Button && control.Text.Contains("예상 결과"));
        var point = new Vector2(body.VisibleRect.x + body.VisibleRect.width / 2, body.VisibleRect.y + body.VisibleRect.height / 2);
        float previous = DetailScroll(wheelPopup);
        // Explicit boundary for the native scroll view's wheel result. The
        // renderer consumes its returned position, not a gameplay action.
        GUILayout.ScrollInput = (index, position) => index == 1 && Event.current.type == EventType.ScrollWheel
            ? new Vector2(position.x, position.y + 60) : position;
        try { Pointer(wheelPopup, EventType.ScrollWheel, EventType.ScrollWheel, point); }
        finally { GUILayout.ScrollInput = null; }
        Check(DetailScroll(wheelPopup) == previous + 60 && EventSelectionRuntime.Commands == 0,
            "Mouse-wheel scrolling is retained through the native result scroll-view boundary");
    }
}
