using System;
using System.Linq;
using System.Reflection;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using DiceMaster.LogUI;
using UnityEngine;

internal static partial class Program
{
    private static string Selected(EventPopupRenderer popup) => (string)typeof(EventPopupRenderer)
        .GetField("selected", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup);

    private static void CandidateOnlyRegressions()
    {
        foreach (LogPadCommand direction in new[] { LogPadCommand.Up, LogPadCommand.Down })
        {
            var popup = Start(); Draw(popup);
            Check(Selected(popup) == null && !GUILayout.Buttons.Any(text => text.StartsWith("▶ ")),
                "Opening a decision leaves every real event unselected");
            Check(GUILayout.ScrollViews[0].Controls.Count(control => control.Button) == EventSelectionRuntime.Candidates.Count
                && !GUILayout.ScrollViews[0].Controls.Any(control => control.Text.Contains("무작위")),
                "The candidate scroll view contains only real events");
            Check(!GUILayout.Buttons.Contains("무작위로 진행 [A / Enter]")
                && GUILayout.Controls.Single(control => control.Button && control.Text.StartsWith("이벤트를 선택하세요")).Enabled == false,
                "An unselected popup has no default A/Enter random action");
            popup.Handle(LogPadCommand.Activate); Key(popup, KeyCode.Return); Key(popup, KeyCode.KeypadEnter);
            Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                "Initial A and both Enter keys leave the decision waiting");
            popup.Handle(direction);
            Check(Selected(popup) == "eligible", "The first " + direction + " enters the first real event");
            popup.Handle(LogPadCommand.Up);
            Check(Selected(popup) == "eligible", "Up from the first event cannot navigate to a virtual random row");
            for (int index = 0; index < 10; index++) popup.Handle(LogPadCommand.Down);
            Check(Selected(popup) == "broken", "Down clamps to the last real event");
        }

        foreach (bool empty in new[] { false, true })
        {
            var popup = Start(); popup.Handle(LogPadCommand.Down);
            EventSelectionRuntime.Candidates.RemoveAt(0);
            if (empty) EventSelectionRuntime.Candidates.Clear();
            Draw(popup); popup.UpdateInput();
            popup.Handle(LogPadCommand.Activate); Key(popup, KeyCode.Return);
            Check(Selected(popup) == null && EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                "Removing the selected event cancels confirmation without assigning random or another event; empty=" + empty);
            popup.Handle(LogPadCommand.Up); popup.Handle(LogPadCommand.Down);
            Check(empty ? Selected(popup) == null : Selected(popup) != null,
                "Candidate-only navigation remains safe after removal; empty=" + empty);
        }

        foreach (var pair in new[] {
            (EventType.MouseDown, EventType.MouseUp, EventType.MouseDown, EventType.MouseUp),
            (EventType.MouseDown, EventType.MouseUp, EventType.TouchDown, EventType.TouchUp),
            (EventType.TouchDown, EventType.TouchUp, EventType.TouchDown, EventType.TouchUp) })
        foreach (bool empty in new[] { false, true })
        {
            var popup = Start(); if (empty) EventSelectionRuntime.Candidates.Clear();
            Draw(popup);
            Check(GUILayout.Buttons.Count(text => text.StartsWith("무작위로 진행")) == 1
                && GUILayout.LiteralLabels.All(text => !text.Contains("B/Esc 무작위")),
                "Exactly one clickable footer action also supplies the B/Esc instruction; empty=" + empty);
            Click(popup, text => text == "무작위로 진행 [B / Esc]", pair.Item1, pair.Item2, pair.Item3, pair.Item4);
            Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Random,
                "The footer preserves the original random game path for " + pair + "; empty=" + empty);
        }
        foreach (bool keyboard in new[] { false, true })
        {
            var popup = Start(); EventSelectionRuntime.Candidates.Clear();
            popup.Handle(LogPadCommand.Up); popup.Handle(LogPadCommand.Down); popup.Handle(LogPadCommand.Activate);
            if (keyboard) Key(popup, KeyCode.Escape); else popup.Handle(LogPadCommand.Back);
            Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Random,
                "An empty candidate list retains explicit " + (keyboard ? "Esc" : "B") + " random recovery");
        }

        BoldTitleRegressions();
    }

    private static void BoldTitleRegressions()
    {
        foreach (var dimensions in new[] { (640, 480), (1280, 720), (1920, 1080), (3840, 2160) })
        foreach (int font in new[] { 12, 20, 64 })
        {
            var popup = Start(); Screen.width = dimensions.Item1; Screen.height = dimensions.Item2;
            DiceMaster.Config.ConfigManager.LogFontSize.Value = font;
            DiceMaster.Config.ConfigManager.LogWindowX.Value = 37;
            EventSelectionRuntime.Candidates[0].Title = "Section_123_Result_9:이벤트_이름 " + new string('한', 180) + "😀";
            EventSelectionRuntime.Candidates[0].State = "정상 발동 가능";
            EventSelectionRuntime.Candidates[1].State = "발동 불가 · 조건 미충족";
            Draw(popup);
            var row = GUILayout.ScrollViews[0].Controls.First(control => control.Button);
            string title = row.Text.Split('\n')[0];
            Check(title.StartsWith("  <b>") && title.EndsWith("</b>") && title.Contains("…")
                && row.Text.Split('\n').Length == 2 && !GUILayout.Buttons.Any(text => text.Contains("정상 발동 가능") || text.Contains("발동 불가")),
                "The full event heading is bold and eligibility labels are absent at " + dimensions + " font=" + font);
            var measure = new GUIStyle { fontSize = font, fontStyle = FontStyle.Bold };
            var normal = new GUIStyle { fontSize = font };
            string literal = EventResultPreview.PlainText(title).Substring(2);
            Check(measure.CalcSize(new GUIContent(literal)).x > normal.CalcSize(new GUIContent(literal)).x
                && measure.CalcSize(new GUIContent(literal)).x + normal.CalcSize(new GUIContent("  ")).x <= row.Rect.width + .1f
                && row.Rect.height <= (font + 4) * 2 + .1f,
                "Bold glyph width, rather than normal width, bounds the title to one line at " + dimensions + " font=" + font);
            popup.Handle(LogPadCommand.Down); Draw(popup); Draw(popup);
            var selected = GUILayout.ScrollViews[0].Controls.First(control => control.Button);
            Check(selected.Text.StartsWith("▶ <b>") && selected.Text.Split('\n')[0].Substring(2) == title.Substring(2)
                && selected.Rect.height == row.Rect.height && selected.Rect.y == row.Rect.y,
                "Selection preserves the exact truncated bold heading and two-line geometry at " + dimensions + " font=" + font);
        }

        var literalPopup = Start();
        EventSelectionRuntime.Candidates[0].Title = "Section_1:이름 <sprite=42> <b>문자</b>";
        Draw(literalPopup);
        string caption = GUILayout.ScrollViews[0].Controls.First(control => control.Button).Text;
        Check(caption.Contains("＜sprite=42＞") && System.Text.RegularExpressions.Regex.Matches(caption, "<b>").Count == 1
            && System.Text.RegularExpressions.Regex.Matches(caption, "</b>").Count == 1,
            "Only the renderer supplies balanced title bold markup; literal unknown tags stay visible text");
    }
}
