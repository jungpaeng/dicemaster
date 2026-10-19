using System;
using System.Linq;
using System.Text.RegularExpressions;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using UnityEngine;

internal static partial class Program
{
    private static void RichResultUiRegressions()
    {
        RichPreviewAndLiteralBoundaries();
        IneligibleRowsStaySelectable();
        RichSummaryTruncationAndLayout();
    }

    private static void RichPreviewAndLiteralBoundaries()
    {
        var popup = Start(); EventCandidate candidate = EventSelectionRuntime.Candidates[0];
        candidate.Title = "<color=#BCA136>정상 후보</color> <sprite=42>";
        candidate.State = "현재 상태 <size=999>문자</size>";
        candidate.Summary = "발동 조건 <color=red>문자 그대로</color>";
        candidate.ResultSummary = "동전 +100 · 심상 -2";
        candidate.ResultSummaryRich = "<color=#A3B86C>동전 +100</color> · <color=#CD594A>심상 -2</color>";
        candidate.ResultDetail = "동전 +100\n심상 -2\n경우 1: 호감도 +3";
        candidate.ResultDetailRich = "<color=#A3B86C>동전 +100</color>\n<color=#CD594A>심상 -2</color>\n<color=cyan>경우 1</color>: 호감도 +3";
        Draw(popup);
        GUILayout.ControlRecord row = GUILayout.Controls.Single(control => control.Button && RowCaption(control.Text, "eligible"));
        Check(row.RichText && row.Text.Contains("#A3B86C") && row.Text.Contains("#CD594A") && !row.Text.Contains("#BCA136"),
            "List results retain the existing positive/negative effect colors without importing title markup");
        Check(row.Text.Contains("＜sprite=42＞") && row.Text.Contains("＜size=999＞") && !row.Text.Contains("<size="),
            "Names and status remain escaped literals inside the rich result row");
        Check(GUILayout.Buttons.All(value => !value.Contains("클릭·터치") && !value.Contains("다시 눌러"))
            && GUILayout.LiteralLabels.All(value => !value.Contains("클릭·터치") && !value.Contains("다시 눌러")),
            "Repeated pointer selection/progress instructions are absent from rows and the popup footer");
        popup.Handle(LogPadCommand.Down); Draw(popup);
        row = GUILayout.Controls.Single(control => control.Button && RowCaption(control.Text, "eligible"));
        Check(row.Text.StartsWith("▶ ", StringComparison.Ordinal) && row.Text.Contains("#A3B86C") && row.Text.Contains("#CD594A")
            && row.Text.Contains("\n<color=#FFFFFF>") && row.TextColor.r == 1 && row.TextColor.g == 1 && row.TextColor.b == 0,
            "Selected-row highlighting keeps its arrow while preserving the result's own colors");
        GUILayout.ControlRecord result = GUILayout.Controls.Single(control => !control.Button && control.Text == candidate.ResultDetailRich);
        GUILayout.ControlRecord conditions = GUILayout.Controls.Single(control => !control.Button && control.Text == candidate.Summary);
        Check(result.RichText && !conditions.RichText
            && GUILayout.Controls.Where(control => !control.Button && (control.Text == "예상 결과" || control.Text == "발동 조건")).All(control => !control.RichText),
            "Only the prepared result body interprets rich colors; conditions and headings retain literal styles");
        Check(GUILayout.Buttons.Any(value => value.StartsWith("이 이벤트로 진행 [A / Enter]", StringComparison.Ordinal)),
            "The confirm button retains its action meaning and keyboard/gamepad shortcut");
        Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
            "Color and hint presentation cannot execute an event");
    }

    private static void IneligibleRowsStaySelectable()
    {
        var pairs = new[] {
            (EventType.MouseDown, EventType.MouseUp, EventType.MouseDown, EventType.MouseUp),
            (EventType.MouseDown, EventType.MouseUp, EventType.TouchDown, EventType.TouchUp),
            (EventType.TouchDown, EventType.TouchUp, EventType.TouchDown, EventType.TouchUp)
        };
        foreach (string state in new[] { "조건 미충족", "최종 가중치 0", "판정 미확인", "판정 미확인 · 조회 실패" })
        foreach (var pair in pairs)
        {
            var popup = Start(); EventCandidate candidate = EventSelectionRuntime.Candidates[1];
            candidate.State = state; candidate.CanForce = state != "판정 미확인 · 조회 실패";
            candidate.ResultSummaryRich = "<color=#A3B86C>동전 +100</color>";
            Draw(popup);
            GUILayout.ControlRecord before = GUILayout.Controls.Single(control => control.Button && RowCaption(control.Text, "blocked"));
            string[] lines = before.Text.Split('\n');
            Check(before.Enabled && lines.Length == 3 && lines[0].Contains("<color=#B3B3B3>") && lines[1].Contains("<color=#B3B3B3>")
                && !lines[2].Contains("#B3B3B3") && lines[2].Contains("#A3B86C"),
                "Ineligible title/status are visibly gray while the selectable row keeps effect colors: " + state + " " + pair);
            Vector2 point = Point(popup, text => RowCaption(text, "blocked"));
            Pointer(popup, pair.Item1, pair.Item3, point); Pointer(popup, pair.Item2, pair.Item4, point);
            Draw(popup);
            GUILayout.ControlRecord selected = GUILayout.Controls.Single(control => control.Button && RowCaption(control.Text, "blocked"));
            Check(selected.Text.StartsWith("▶ ", StringComparison.Ordinal) && selected.Text.Contains("#B3B3B3")
                && selected.TextColor.r == 1 && selected.TextColor.g == 1 && selected.TextColor.b == 0
                && selected.Rect.height == before.Rect.height && EventSelectionRuntime.Commands == 0,
                "The first pointer gesture selects a gray row without executing or changing its height: " + state + " " + pair);
            Pointer(popup, pair.Item1, pair.Item3, point); Pointer(popup, pair.Item2, pair.Item4, point);
            Check(candidate.CanForce ? EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Forced
                    && EventSelectionRuntime.Selected == "blocked" : EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                "The second same-point gesture uses explicit force only for a readable ineligible result: " + state + " " + pair);
        }

        var padPopup = Start(); padPopup.Handle(LogPadCommand.Down); padPopup.Handle(LogPadCommand.Down); Draw(padPopup);
        Check(GUILayout.Buttons.Any(value => value.StartsWith("조건 무시하고 이번만 진행 [A / Enter]", StringComparison.Ordinal)),
            "The selected gray row exposes its explicit condition-bypass action");
        padPopup.Handle(LogPadCommand.Activate);
        Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Forced,
            "Gray styling preserves the gamepad A force action");
    }

    private static void RichSummaryTruncationAndLayout()
    {
        foreach (var dimensions in new[] { (640, 480), (1280, 720), (1920, 1080), (3840, 2160) })
        foreach (int font in new[] { 12, 20, 64 })
        {
            var popup = Start(); Screen.width = dimensions.Item1; Screen.height = dimensions.Item2;
            DiceMaster.Config.ConfigManager.LogFontSize.Value = font;
            DiceMaster.Config.ConfigManager.LogWindowX.Value = 37;
            EventCandidate candidate = EventSelectionRuntime.Candidates[0];
            candidate.ResultSummaryRich = "<color=#A3B86C>" + new string('효', 140) + "😀</color><color=#CD594A> -2</color>";
            candidate.ResultSummary = EventResultPreview.PlainText(candidate.ResultSummaryRich);
            candidate.ResultDetailRich = "<color=#A3B86C>" + new string('상', 6000) + "</color>";
            candidate.ResultDetail = EventResultPreview.PlainText(candidate.ResultDetailRich);
            Draw(popup);
            GUILayout.ControlRecord before = GUILayout.Controls.Where(control => control.Button && control.Text.Split('\n').Length == 3).ElementAt(1);
            string summary = before.Text.Split('\n')[2];
            Check(summary.Contains("…") && summary.Contains("#A3B86C")
                && Regex.Matches(summary, "<color=").Count == Regex.Matches(summary, "</color>").Count,
                "Summary ellipsis retains and closes effect-color spans at " + dimensions + " font=" + font);
            Check(!EventResultPreview.PlainText(summary).Any(char.IsSurrogate)
                && before.Rect.height <= (font + 4) * 3 + 1,
                "Color tag length cannot add wrapped row lines or leave a split emoji at " + dimensions + " font=" + font);
            popup.Handle(LogPadCommand.Down); Draw(popup); Draw(popup);
            GUILayout.ControlRecord selected = GUILayout.Controls.Where(control => control.Button && control.Text.Split('\n').Length == 3).ElementAt(1);
            Check(selected.Rect.height == before.Rect.height && selected.Rect.y == before.Rect.y,
                "Three-line colored rows keep stable geometry on selection at " + dimensions + " font=" + font);
            foreach (GUILayout.ControlRecord control in GUILayout.Controls.Where(control => control.Button &&
                (control.Text.StartsWith("이 이벤트로 진행 [", StringComparison.Ordinal) || control.Text.StartsWith("무작위로 진행 [", StringComparison.Ordinal))))
                Check(control.VisibleRect.height >= control.Rect.height - .1f,
                    "Removing repeated instructions leaves confirm/recovery buttons accessible at " + dimensions + " font=" + font);
            Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                "Rich summary/detail rendering remains a display-only operation");
        }
    }
}
