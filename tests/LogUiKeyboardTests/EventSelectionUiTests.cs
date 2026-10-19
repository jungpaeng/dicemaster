using System;
using DiceMaster.Config;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using DiceMaster.LiveLog;
using DiceMaster.LogUI;
using UnityEngine;

internal static partial class Program
{
    private static void RendererPopupSettings()
    {
        Rect saved = ConfiguredRect(); int font = ConfigManager.LogFontSize.Value;
        bool events = ConfigManager.EnableEventSelectionPopup.Value, switches = ConfigManager.EnableSwitchSelectionPopup.Value;
        int attempts = LogBridge.Reservations;
        try
        {
            ConfigureLayout(new Rect(80, 80, 1200, 900), 24);
            foreach (int tab in new[] { 1, 2 })
                foreach (bool empty in new[] { false, true })
                {
                    var model = new LogModel(); model.ResetContext(800);
                    var eligible = Item("eligible", new object());
                    eligible.EventEligibility = new EventEligibilitySnapshot(EventEligibilityState.Eligible, EventEligibilityReason.None, 10, "조건 충족");
                    var blocked = Item("blocked", new object());
                    blocked.EventEligibility = new EventEligibilitySnapshot(EventEligibilityState.Ineligible, EventEligibilityReason.ConditionsNotMet, 0, "조건 부족");
                    string name = tab == 1 ? "조건" : "이벤트";
                    Publish(model, name, empty ? Array.Empty<LogItem>() : new[] { eligible, blocked });
                    // A retained old projection must not survive the removal
                    // of its controls, including when the list is empty.
                    model.Tab("이벤트").SearchQuery = "hidden-query";
                    model.Tab("이벤트").AvailableOnly = true;
                    int refreshes = 0, closes = 0;
                    var renderer = new LogRenderer(model, () => refreshes++, () => closes++);
                    renderer.Handle(LogPadCommand.None);
                    for (int index = 0; index < tab; index++) renderer.Handle(LogPadCommand.NextTab);
                    Repaint(renderer);
                    var option = tab == 1 ? ConfigManager.EnableSwitchSelectionPopup : ConfigManager.EnableEventSelectionPopup;
                    var other = tab == 1 ? ConfigManager.EnableEventSelectionPopup : ConfigManager.EnableSwitchSelectionPopup;
                    string label = tab == 1 ? "조건 분기 선택 팝업:" : "이벤트 선택 팝업:";
                    Check(model.Tab("이벤트").SearchQuery == "" && !model.Tab("이벤트").AvailableOnly,
                        "removed event controls cannot leave an inherited hidden projection");
                    Check(GUILayout.Controls.Exists(control => control.Button && control.Text.StartsWith(label, StringComparison.Ordinal))
                        && !GUILayout.Controls.Exists(control => control.Text.Contains("검색", StringComparison.Ordinal)
                            || control.Text.Contains("발동 가능한 항목만 보기", StringComparison.Ordinal)),
                        name + ": direct popup setting remains visible even with empty rows; search/filter controls are absent");
                    if (!empty)
                        Check(GUILayout.ScrollViews[0].Controls.Exists(control => control.Text.Contains(blocked.Id, StringComparison.Ordinal)),
                            "ineligible published rows remain readable without a filter");

                    foreach (int kind in new[] { 0, 1, 2 })
                    {
                        bool before = option.Value, otherBefore = other.Value; int writes = option.Writes;
                        var controller = new LogPadController(); var states = new PadState[4]; bool[] connected = { true, false, false, false };
                        controller.Tick(true, true, true, LogInputMode.Gamepad, 0, states, connected);
                        controller.Tick(true, true, true, LogInputMode.Gamepad, .1, states, connected);
                        GamepadBridge.PointerActivity = controller.PointerActivity;
                        Input.touchCount = kind == 0 ? 0 : 1;
                        EventType down = kind == 2 ? EventType.TouchDown : EventType.MouseDown;
                        EventType up = kind == 2 ? EventType.TouchUp : EventType.MouseUp;
                        EventType rawDown = kind == 0 ? EventType.MouseDown : EventType.TouchDown;
                        EventType rawUp = kind == 0 ? EventType.MouseUp : EventType.TouchUp;
                        Vector2 point = PointerTarget(renderer, control => control.Button && control.Text.StartsWith(label, StringComparison.Ordinal));
                        DrawPointer(renderer, down, rawDown, point);
                        DrawPointer(renderer, EventType.Layout, rawUp, point);
                        DrawPointer(renderer, EventType.Repaint, rawUp, point);
                        SoftResetForPointer(renderer);
                        DrawPointer(renderer, up, rawUp, point);
                        Check(option.Value != before && option.Writes == writes + 1 && other.Value == otherBefore,
                            name + " pointer form " + kind + ": a single native gesture persists only its own popup setting");
                        DrawPointer(renderer, up, rawUp, point);
                        Check(option.Writes == writes + 1 && Area(renderer) == "PopupSetting",
                            "duplicate release cannot retoggle or move focus into the list");
                        var soft = controller.Tick(true, true, true, LogInputMode.KeyboardMouse, .2, states, connected);
                        if (soft.ResetFocus) renderer.ResetInputFocus(soft.PreservePointerTransaction, soft.PreserveNavigationArea);
                        states[0] = new PadState { Buttons = PadButtons.A };
                        var takeover = controller.Tick(true, true, true, LogInputMode.KeyboardMouse, .3, states, connected);
                        if (takeover.ResetFocus) renderer.ResetInputFocus(takeover.PreservePointerTransaction, takeover.PreserveNavigationArea);
                        renderer.Handle(takeover.Command);
                        Check(takeover.ResetFocus && takeover.PreserveNavigationArea && takeover.Command == LogPadCommand.None
                            && Area(renderer) == "PopupSetting" && option.Writes == writes + 1,
                            "actual pad takeover after a completed touch keeps setting focus behind its neutral barrier");
                        states[0] = default;
                        controller.Tick(true, true, true, LogInputMode.KeyboardMouse, .4, states, connected);
                        states[0] = new PadState { Buttons = PadButtons.A };
                        renderer.Handle(controller.Tick(true, true, true, LogInputMode.KeyboardMouse, .5, states, connected).Command);
                        Check(option.Value == before && option.Writes == writes + 2 && Area(renderer) == "PopupSetting",
                            "fresh pad A after neutral activates the touched setting rather than a list row");
                        GamepadBridge.PointerActivity = null;
                        Repaint(renderer);
                        Check(!GUILayout.ScrollViews[0].Controls.Exists(control => control.Text.StartsWith("▶ ", StringComparison.Ordinal))
                            && GUILayout.Controls.FindAll(control => control.Text.StartsWith("● ", StringComparison.Ordinal)).Count == 1,
                            "setting focus never marks a list row or tools as a second active selection");
                        renderer.Handle(LogPadCommand.Down);
                        Check(Area(renderer) == "List" && (empty || model.Tab(name).SelectedId == eligible.Id),
                            "Down explicitly leaves the setting for its own list");
                        renderer.Handle(LogPadCommand.Up);
                        Check(Area(renderer) == "PopupSetting", "Up from the first/empty row reaches the tab setting");
                    }
                    if (!empty)
                    {
                        Publish(model, name, Array.Empty<LogItem>()); Repaint(renderer);
                        Check(Area(renderer) == "PopupSetting" && model.Tab(name).SelectedId == null,
                            "automatic removal of the selected row cannot move setting focus to a different action");
                    }
                    int keyboardWrites = option.Writes;
                    TapKey(renderer, KeyCode.Return);
                    Check(option.Writes == keyboardWrites + 1 && Area(renderer) == "PopupSetting",
                        "keyboard Enter toggles only the focused setting");
                    TapKey(renderer, KeyCode.X);
                    Check(refreshes == 1 && Area(renderer) == "PopupSetting", "X refresh keeps tab-setting navigation");
                    TapKey(renderer, KeyCode.Y); Repaint(renderer);
                    Check(Area(renderer) == "Tools" && !GUILayout.Controls.Exists(control => control.Text.Contains("검색", StringComparison.Ordinal)
                        || control.Text.Contains("발동 가능한 항목만 보기", StringComparison.Ordinal)),
                        "tools retain popup options while removing search/filter actions");
                    TapKey(renderer, KeyCode.Escape);
                    Check(Area(renderer) == "List" && closes == 0, "B/Escape exits tools to the content area");
                    TapKey(renderer, KeyCode.Escape);
                    Check(closes == 1 && LogBridge.Reservations == attempts, "B/Escape closes without reserving or executing events");
                }
        }
        finally
        {
            ConfigManager.EnableEventSelectionPopup.Value = events; ConfigManager.EnableSwitchSelectionPopup.Value = switches;
            GamepadBridge.PointerActivity = null;
            Input.touchCount = 0; Input.Held.Clear(); GUILayout.ButtonInput = null; GUI.FocusControl(null);
            ConfigureLayout(saved, font);
        }
    }
}
