using System;
using DiceMaster.Config;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using DiceMaster.LiveLog;
using DiceMaster.LogUI;
using UnityEngine;

internal static partial class Program
{
    private static void RendererEventSelectionQueryAndConfig()
    {
        Rect saved = ConfiguredRect(); int savedFont = ConfigManager.LogFontSize.Value;
        bool enabled = ConfigManager.EnableEventSelectionPopup.Value;
        int attempts = LogBridge.Reservations;
        try
        {
            ConfigureLayout(new Rect(80, 80, 1200, 900), 24);
            var model = new LogModel(); model.ResetContext(800);
            LogItem blocked = Item("blocked", new object());
            blocked.Title = "조건 부족 이벤트"; blocked.Description = "특별한 설명 검색어";
            blocked.EventEligibility = new EventEligibilitySnapshot(EventEligibilityState.Ineligible,
                EventEligibilityReason.ConditionsNotMet, 0, "능력치 조건이 부족합니다.");
            LogItem eligible = Item("EVENT_RAW_42", new object());
            eligible.Title = "가능한 이름";
            eligible.EventEligibility = new EventEligibilitySnapshot(EventEligibilityState.Eligible,
                EventEligibilityReason.None, 10, "현재 조건을 충족합니다.");
            LogItem unknown = Item("unknown", new object());
            unknown.EventEligibility = new EventEligibilitySnapshot(EventEligibilityState.Unknown,
                EventEligibilityReason.Random, null, "난수 조건은 표시만으로 확정하지 않습니다.");
            Publish(model, "이벤트", blocked, eligible, unknown);
            int closes = 0, refreshes = 0;
            var renderer = new LogRenderer(model, () => refreshes++, () => closes++);
            renderer.Handle(LogPadCommand.None); renderer.Handle(LogPadCommand.NextTab); renderer.Handle(LogPadCommand.NextTab);
            Repaint(renderer);
            Check(model.Tab("이벤트").VisibleRows[0].Id == eligible.Id,
                "event list starts with verified available candidates while retaining every candidate");
            Check(GUILayout.Controls.Exists(control => control.Text.Contains("이벤트 검색", StringComparison.Ordinal))
                && GUILayout.Controls.Exists(control => control.Text.Contains("전체 보기", StringComparison.Ordinal)),
                "only the event pane exposes search and the default all-items filter");
            renderer.Handle(LogPadCommand.Activate); renderer.Handle(LogPadCommand.Activate);
            ClickRenderedButton(renderer, eligible.Title); ClickRenderedButton(renderer, eligible.Title);
            Check(LogBridge.Reservations == attempts && !model.Tab("이벤트").ExecutionFocus,
                "pad confirmation and repeated pointer confirmation on event rows never create reservations");
            Repaint(renderer);
            Check(GUILayout.ScrollViews[1].Controls.Exists(control => control.Text.Contains("실제 행동의 판정", StringComparison.Ordinal))
                && !GUILayout.Controls.Exists(control => control.Text.Contains("이벤트 우선 예약", StringComparison.Ordinal)),
                "event details explain the actual popup boundary and contain no legacy event reservation UI");

            var controller = new LogPadController();
            var padStates = new PadState[4]; bool[] connected = { true, false, false, false };
            controller.Tick(true, true, true, LogInputMode.Gamepad, 0, padStates, connected);
            controller.Tick(true, true, true, LogInputMode.Gamepad, .1, padStates, connected);
            GamepadBridge.PointerActivity = controller.PointerActivity;
            GamepadBridge.KeyboardActivity = controller.KeyboardActivity;
            GamepadBridge.Cancelled = controller.Cancel;
            Vector2 searchPoint = PointerTarget(renderer, control => !control.Button && control.Text == "");
            DrawPointer(renderer, EventType.MouseDown, EventType.MouseDown, searchPoint);
            Check(Event.current.type == EventType.Used && (string)Field(renderer, "pointerIdentity") == "DiceMaster.EventSearch",
                "native TextField consumption retains the search identity from its pre-consumption rectangle");
            LogPadFrame takeover = controller.Tick(true, true, true, LogInputMode.Gamepad, .2, padStates, connected);
            if (takeover.ResetFocus) renderer.ResetInputFocus(takeover.PreservePointerTransaction);
            DrawPointer(renderer, EventType.MouseUp, EventType.MouseUp, searchPoint); Repaint(renderer);
            Check(takeover.ResetFocus && takeover.PreservePointerTransaction && GUI.GetNameOfFocusedControl() == "DiceMaster.EventSearch",
                "actual controller mouse takeover preserves the event text caret across the next Update");
            GUI.FocusControl(null);
            DrawPointer(renderer, EventType.TouchDown, EventType.TouchDown, searchPoint);
            Check(Event.current.type == EventType.Used && (string)Field(renderer, "pointerIdentity") == "DiceMaster.EventSearch"
                && GUI.GetNameOfFocusedControl() == "DiceMaster.EventSearch",
                "direct touch is adapted to the actual native TextField mouse boundary and focuses the search field");
            foreach (EventType pass in new[] { EventType.Layout, EventType.Repaint, EventType.Ignore })
            {
                DrawPointer(renderer, pass, EventType.TouchUp, searchPoint);
                Check((string)Field(renderer, "pointerIdentity") == "DiceMaster.EventSearch",
                    pass + " with raw touch-up cannot cancel the search field's active pointer transaction");
            }
            LogPadFrame touchTakeover = controller.Tick(true, true, true, LogInputMode.Gamepad, .25, padStates, connected);
            if (touchTakeover.ResetFocus) renderer.ResetInputFocus(touchTakeover.PreservePointerTransaction);
            renderer.Handle(touchTakeover.Command); renderer.Scroll(touchTakeover.Scroll);
            DrawPointer(renderer, EventType.TouchUp, EventType.TouchUp, searchPoint); Repaint(renderer);
            Check(touchTakeover.ResetFocus && touchTakeover.PreservePointerTransaction && GUI.GetNameOfFocusedControl() == "DiceMaster.EventSearch",
                "actual controller Update between direct touch down/up retains the native search caret");
            GUILayout.TextFieldInput = previous => "특별한 설명";
            Repaint(renderer); GUILayout.TextFieldInput = null;
            LogPadFrame typing = controller.Tick(true, true, true, LogInputMode.Gamepad, .3, padStates, connected);
            if (typing.ResetFocus) renderer.ResetInputFocus(typing.PreservePointerTransaction);
            Repaint(renderer);
            Check(!typing.ResetFocus && model.Tab("이벤트").SelectedId == null && GUI.GetNameOfFocusedControl() == "DiceMaster.EventSearch",
                "typing that filters out the selected row keeps the caret and never creates a hard pad reset");
            GamepadBridge.PointerActivity = null; GamepadBridge.KeyboardActivity = null; GamepadBridge.Cancelled = null;

            foreach (string query in new[] { "가능한 이름", "event_raw_42", "특별한 설명" })
            {
                GUILayout.TextFieldInput = previous => query;
                Repaint(renderer); GUILayout.TextFieldInput = null; Repaint(renderer);
                Check(model.Tab("이벤트").SearchQuery == query && model.Tab("이벤트").VisibleRows.Count == 1,
                    "event search matches the published name, case-insensitive raw identifier and full description: " + query);
            }
            Check(model.Tab("이벤트").VisibleRows[0].Id == blocked.Id,
                "description search retains an ineligible item for inspection");
            renderer.Handle(LogPadCommand.Activate); Repaint(renderer);
            Check(GUILayout.ScrollViews[1].Controls.Exists(control => control.Text == blocked.Description)
                && GUILayout.ScrollViews[1].Controls.Exists(control => control.Text.Contains("조건 미충족", StringComparison.Ordinal)),
                "event inspection keeps the complete description and structured ineligibility reason");
            renderer.Handle(LogPadCommand.NextTab); Repaint(renderer);
            Check(!GUILayout.Controls.Exists(control => control.Text.Contains("이벤트 검색", StringComparison.Ordinal)),
                "mission pane does not restore removed search controls");
            renderer.Handle(LogPadCommand.PreviousTab);
            Check(model.Tab("이벤트").SearchQuery == "특별한 설명", "tab changes retain the current event query in memory");
            ClickRenderedButton(renderer, "발동 가능한 항목만 보기:");
            Repaint(renderer);
            Check(model.Tab("이벤트").AvailableOnly && model.Tab("이벤트").VisibleRows.Count == 0
                && model.Tab("이벤트").SelectedId == null,
                "mouse available-only filter removes hidden selection and does not treat unmet conditions as eligible");
            Check(GUILayout.Controls.Exists(control => control.Text.Contains("검색·필터에 맞는 이벤트가 없습니다", StringComparison.Ordinal)),
                "an empty filtered event list keeps recovery guidance visible");

            renderer.Handle(LogPadCommand.Tools);
            for (int index = 0; index < 19; index++) renderer.Handle(LogPadCommand.Down);
            renderer.Handle(LogPadCommand.Activate); Repaint(renderer);
            Check(model.Tab("이벤트").SearchQuery == "" && model.Tab("이벤트").AvailableOnly,
                "pad can clear the event query without changing the availability filter");
            renderer.Handle(LogPadCommand.Back); Repaint(renderer);
            Check(model.Tab("이벤트").VisibleRows.Count == 1 && model.Tab("이벤트").VisibleRows[0].Id == eligible.Id,
                "available-only navigation contains only fresh verified eligible events");

            eligible.RefreshState = LogRefreshState.RetainedFailure;
            eligible.RefreshStatus = "갱신 실패 · 이전 값";
            Publish(model, "이벤트", blocked, eligible, unknown);
            Repaint(renderer);
            Check(model.Tab("이벤트").VisibleRows.Count == 0,
                "a retained eligible explanation cannot keep a stale event in available-only results");
            ClickRenderedButton(renderer, "발동 가능한 항목만 보기:");
            model.Tab("이벤트").Select(eligible.Id); Repaint(renderer);
            Check(GUILayout.ScrollViews[0].Controls.Exists(control => control.Text.Contains(eligible.Title, StringComparison.Ordinal)
                && control.Text.Contains("판정 미확인", StringComparison.Ordinal) && !control.Text.Contains("정상 발동 가능", StringComparison.Ordinal))
                && GUILayout.ScrollViews[1].Controls.Exists(control => control.Text.StartsWith("이전 조회 설명:", StringComparison.Ordinal)),
                "retained event rows distinguish current unknown eligibility from their previous readable explanation");
            eligible.RefreshState = LogRefreshState.Healthy; eligible.RefreshStatus = "";
            Publish(model, "이벤트", blocked, eligible, unknown);
            ClickRenderedButton(renderer, "발동 가능한 항목만 보기:");
            model.MarkPublicationFailure("이벤트"); Repaint(renderer);
            Check(model.Tab("이벤트").VisibleRows.Count == 0,
                "publication failure removes cached availability claims while preserving all-view information");
            Publish(model, "이벤트", blocked, eligible, unknown); Repaint(renderer);

            GUI.FocusControl("DiceMaster.EventSearch");
            TapKey(renderer, KeyCode.X); TapKey(renderer, KeyCode.Y); TapKey(renderer, KeyCode.Space);
            Check(refreshes == 0 && Area(renderer) == "List" && LogBridge.Reservations == attempts,
                "typing X, Y or space in event search cannot refresh, open tools or reserve");
            TapKey(renderer, KeyCode.Escape);
            Check(closes == 0 && GUI.GetNameOfFocusedControl() == null,
                "Escape first exits event text entry without closing the log or advancing an event");

            renderer.Handle(LogPadCommand.Tools);
            for (int index = 0; index < 17; index++) renderer.Handle(LogPadCommand.Down);
            int writes = ConfigManager.EnableEventSelectionPopup.Writes;
            renderer.Handle(LogPadCommand.Activate); Repaint(renderer);
            Check(ConfigManager.EnableEventSelectionPopup.Value != enabled && ConfigManager.EnableEventSelectionPopup.Writes == writes + 1,
                "pad popup toggle writes the persistent ConfigElement exactly once");
            ClickRenderedButton(renderer, "이벤트 선택 팝업 사용:");
            Check(ConfigManager.EnableEventSelectionPopup.Value == enabled,
                "mouse popup toggle uses the same persisted option as pad");
            Check(LogBridge.Reservations == attempts, "event query, filter and option tools never invoke reservation execution");
        }
        finally
        {
            ConfigManager.EnableEventSelectionPopup.Value = enabled;
            GamepadBridge.PointerActivity = null; GamepadBridge.KeyboardActivity = null; GamepadBridge.Cancelled = null;
            GUILayout.TextFieldInput = null; GUILayout.ButtonInput = null; GUI.FocusControl(null);
            Input.Held.Clear(); ConfigureLayout(saved, savedFont);
        }
    }
}
