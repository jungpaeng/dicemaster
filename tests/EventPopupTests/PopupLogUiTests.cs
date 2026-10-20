using System;
using System.Linq;
using System.Reflection;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using DiceMaster.LogUI;
using UnityEngine;

internal static partial class Program
{
    private static void PopupLogControlRegressions()
    {
        foreach (bool branch in new[] { false, true })
        foreach (var pair in new[] {
            (EventType.MouseDown, EventType.MouseUp, EventType.MouseDown, EventType.MouseUp),
            (EventType.MouseDown, EventType.MouseUp, EventType.TouchDown, EventType.TouchUp),
            (EventType.TouchDown, EventType.TouchUp, EventType.TouchDown, EventType.TouchUp) })
        {
            var popup = Start(); EventSelectionRuntime.IsSwitch = branch;
            popup.Handle(LogPadCommand.Down);
            EventSelectionRuntime.Candidates[0].ResultDetail = new string('결', 6000);
            Draw(popup); popup.Scroll(1); Draw(popup);
            float priorScroll = DetailScroll(popup);
            string priorSelected = (string)typeof(EventPopupRenderer).GetField("selected", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup);
            int opened = 0;
            bool afterLayout = false;
            popup.LogHint = "F8 로그";
            popup.OpenLog = () => {
                opened++;
                afterLayout = GUILayout.LiteralLabels.Any(text => text.StartsWith("↑↓ 선택"));
                popup.ResetInput();
            };
            Draw(popup);
            Check(GUILayout.Buttons.Count(text => text == "로그 열기 · F8 로그") == 1
                && !GUILayout.LiteralLabels.Any(text => text.Contains("Tab 로그")),
                "A visible popup log control uses the actual configured hint rather than a hardcoded Tab: Switch=" + branch);
            Vector2 point = Point(popup, text => text == "로그 열기 · F8 로그");
            Pointer(popup, pair.Item1, pair.Item3, point);
            Check(opened == 0 && EventSelectionRuntime.Commands == 0, "Log-control down cannot open or execute before its matching release");
            Pointer(popup, pair.Item2, pair.Item4, point);
            Check(opened == 1 && afterLayout && EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                "A fresh popup log touch is deferred beyond layout and never requests a result: Switch=" + branch + " " + pair);
            Check(DetailScroll(popup) == priorScroll
                && (string)typeof(EventPopupRenderer).GetField("selected", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup) == priorSelected,
                "Opening the log retains the popup selection and reading position");
            Pointer(popup, pair.Item2, pair.Item4, point);
            Check(opened == 1 && EventSelectionRuntime.Commands == 0, "Duplicate log-control release neither reopens nor executes");
            popup.UpdateInput();
            Click(popup, SelectedRowCaption, pair.Item1, pair.Item2, pair.Item3, pair.Item4);
            Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Selected == "eligible" && !EventSelectionRuntime.Forced,
                "A fresh touch after the log-close neutral barrier can execute the preserved popup selection");
        }

        foreach (string boundary in new[] { "token", "reset", "waiting", "outside-down", "row-down" })
        {
            var popup = Start(); popup.Handle(LogPadCommand.Down);
            int opened = 0; popup.OpenLog = () => opened++;
            Vector2 log = Point(popup, text => text == "로그 열기");
            Vector2 row = Point(popup, SelectedRowCaption);
            Pointer(popup, EventType.TouchDown, EventType.TouchDown,
                boundary == "outside-down" ? new Vector2(-30, -30) : boundary == "row-down" ? row : log);
            if (boundary == "token") EventSelectionRuntime.Token++;
            else if (boundary == "reset") popup.ResetInput();
            else if (boundary == "waiting") EventSelectionRuntime.Waiting = false;
            Pointer(popup, EventType.TouchUp, EventType.TouchUp, log);
            Check(opened == 0 && EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                "Popup log opening rejects stale or mismatched pointer transactions: " + boundary);
        }
        Start();
    }
}
