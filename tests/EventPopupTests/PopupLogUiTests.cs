using System;
using System.Linq;
using System.Reflection;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using DiceMaster.LogUI;
using UnityEngine;

internal static partial class Program
{
    private static void PopupShortcutReturnRegressions()
    {
        foreach (bool branch in new[] { false, true })
        foreach (string candidate in new[] { "eligible", "blocked" })
        foreach (var pair in new[] {
            (EventType.MouseDown, EventType.MouseUp, EventType.MouseDown, EventType.MouseUp),
            (EventType.MouseDown, EventType.MouseUp, EventType.TouchDown, EventType.TouchUp),
            (EventType.TouchDown, EventType.TouchUp, EventType.TouchDown, EventType.TouchUp) })
        {
            var popup = Start(); EventSelectionRuntime.IsSwitch = branch;
            popup.Handle(LogPadCommand.Down);
            if (candidate == "blocked") popup.Handle(LogPadCommand.Down);
            EventSelectionRuntime.Candidates[0].ResultDetail = new string('결', 6000);
            EventSelectionRuntime.Candidates[1].ResultDetail = new string('결', 6000);
            Draw(popup); popup.Scroll(1); Draw(popup);
            float priorScroll = DetailScroll(popup);
            string priorSelected = (string)typeof(EventPopupRenderer).GetField("selected", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup);
            Check(GUILayout.Buttons.Count == EventSelectionRuntime.Candidates.Count
                && !GUILayout.Controls.Any(control => control.Text.Contains("로그 열기"))
                && !GUILayout.LiteralLabels.Any(text => text.Contains("Tab 로그") || text.Contains("F8 로그")),
                "Both popups contain only candidate buttons and no replacement log control or shortcut hint: Switch=" + branch);
            Vector2 point = Point(popup, text => RowCaption(text, candidate));
            Pointer(popup, pair.Item1, pair.Item3, point);
            // The real plugin calls this hard boundary when an existing key
            // or pad shortcut opens/closes the log. Its foreground routing is
            // inspected against the delivered DLL in LogUiWiringTests.
            popup.ResetInput();
            Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                "Shortcut log entry cancels an unfinished row confirmation without executing: Switch=" + branch + " " + pair);
            Check(DetailScroll(popup) == priorScroll
                && (string)typeof(EventPopupRenderer).GetField("selected", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup) == priorSelected,
                "The shortcut log boundary retains the popup selection and reading position");
            Input.Held.Add(KeyCode.Return); popup.ResetInput(); popup.UpdateInput();
            Pointer(popup, pair.Item2, pair.Item4, point);
            Draw(popup, EventType.KeyDown, KeyCode.Return);
            Draw(popup, EventType.KeyUp, KeyCode.Return);
            Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                "Log return cannot reuse the old touch release or a held Enter to progress the preserved selection: Switch=" + branch + " " + pair);
            Check(DetailScroll(popup) == priorScroll
                && (string)typeof(EventPopupRenderer).GetField("selected", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup) == priorSelected,
                "Returning from the log keeps the same valid candidate and detail scroll");
            Pointer(popup, pair.Item2, pair.Item4, point);
            Check(EventSelectionRuntime.Commands == 0, "Duplicate abandoned release cannot execute after log return");
            Input.Held.Clear();
            popup.UpdateInput();
            Click(popup, SelectedRowCaption, pair.Item1, pair.Item2, pair.Item3, pair.Item4);
            Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Selected == candidate && EventSelectionRuntime.Forced == (candidate == "blocked"),
                "A fresh touch after the shortcut log-close neutral barrier executes the preserved normal/explicit-override selection once");
        }

        foreach (bool branch in new[] { false, true })
        {
            var popup = Start(); EventSelectionRuntime.IsSwitch = branch; Draw(popup);
            var heading = GUILayout.Controls.Single(control => control.Text == EventSelectionRuntime.PopupTitle);
            var point = new Vector2(heading.VisibleRect.x + heading.VisibleRect.width / 2,
                heading.VisibleRect.y + heading.VisibleRect.height / 2);
            for (int tap = 0; tap < 2; tap++)
            {
                Pointer(popup, EventType.TouchDown, EventType.TouchDown, point);
                Pointer(popup, EventType.TouchUp, EventType.TouchUp, point);
            }
            Check(!heading.Button && EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0 && Selected(popup) == null,
                "The popup heading remains literal text with no replacement action after removing log access: Switch=" + branch);
        }
        Start();
    }
}
