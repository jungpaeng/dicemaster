using System;
using System.Linq;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using UnityEngine;

internal static partial class Program
{
    private static void ConditionParityRegressions()
    {
        foreach (bool isSwitch in new[] { false, true })
        foreach (EventEligibilityState state in new[] { EventEligibilityState.Eligible, EventEligibilityState.Ineligible, EventEligibilityState.Unknown })
        foreach (EventEligibilityReason reason in state == EventEligibilityState.Unknown
            ? new[] { EventEligibilityReason.Random, EventEligibilityReason.Unsupported, EventEligibilityReason.ReadFailure }
            : new[] { state == EventEligibilityState.Eligible ? EventEligibilityReason.None : EventEligibilityReason.ConditionsNotMet })
        {
            var popup = Start(); EventSelectionRuntime.IsSwitch = isSwitch;
            EventCandidate candidate = EventSelectionRuntime.Candidates[0];
            bool unreadable = reason == EventEligibilityReason.ReadFailure;
            string currentReason = state == EventEligibilityState.Ineligible ? "명성 부족"
                : reason == EventEligibilityReason.Random ? "? 난수"
                : unreadable ? "! 대상/결과 확인 필요" : "? 미지원 계산";
            candidate.Summary = state == EventEligibilityState.Eligible ? "최종 가중치 20"
                : currentReason + " · 명성 조건";
            candidate.ConditionDetail = "전체 조건\n명성(3) >= 5\n호감도(20) >= 10\n실행 횟수(0) < 1\n"
                + "랜덤값 0~30 <= 10 · 미확정\n가중치 +10 · 최종 가중치 20\n문자 <color=red>그대로</color>";
            candidate.Eligibility = new EventEligibilitySnapshot(state, reason, null, candidate.Summary, !unreadable);
            candidate.CanSelect = state == EventEligibilityState.Eligible;
            candidate.CanForce = !candidate.CanSelect && !unreadable;
            candidate.ResultSummaryRich = "<color=#A3B86C>동전 +100</color>";
            candidate.ResultDetailRich = "<color=#CD594A>심상 -2</color>";
            Draw(popup);
            var before = GUILayout.Controls.Single(control => control.Button && RowCaption(control.Text, "eligible"));
            string[] lines = before.Text.Split('\n');
            Check(lines.Length == 3 && lines[2].Contains(candidate.Summary)
                && lines[1].Contains("#A3B86C"),
                "Both decisions expose title, rich result, and current reason in exactly three lines: " + isSwitch + "/" + state + "/" + reason);
            Check(lines[0].Contains("#B3B3B3") == (state == EventEligibilityState.Ineligible)
                && lines[2].Contains("#B3B3B3") == (state == EventEligibilityState.Ineligible),
                "Only a known unmet condition is gray in either decision: " + isSwitch + "/" + state + "/" + reason);
            Vector2 point = Point(popup, caption => RowCaption(caption, "eligible"));
            Pointer(popup, EventType.TouchDown, EventType.TouchDown, point);
            Pointer(popup, EventType.TouchUp, EventType.TouchUp, point);
            Draw(popup);
            var after = GUILayout.Controls.Single(control => control.Button && RowCaption(control.Text, "eligible"));
            Check(before.Rect == after.Rect && EventSelectionRuntime.Commands == 0,
                "Unified condition presentation retains the exact next-touch target: " + isSwitch + "/" + state + "/" + reason);
            int current = GUILayout.LiteralLabels.IndexOf(currentReason);
            int heading = GUILayout.LiteralLabels.IndexOf("조건 상세");
            int conditions = GUILayout.LiteralLabels.IndexOf(candidate.ConditionDetail);
            int resultHeading = GUILayout.LiteralLabels.IndexOf("예상 결과");
            int result = GUILayout.LiteralLabels.IndexOf(candidate.ResultDetailRich);
            Check((state == EventEligibilityState.Eligible ? current < 0 : current >= 0 && current < heading)
                && heading < conditions && conditions < resultHeading && resultHeading < result
                && !GUILayout.LiteralLabels.Contains(candidate.Summary),
                "The selected detail shows only a short non-normal reason, then full conditions and rich results without repeating the summary: " + isSwitch + "/" + state + "/" + reason);
            var detail = GUILayout.Controls.Single(control => !control.Button && control.Text == candidate.ConditionDetail);
            Check(!detail.RichText && detail.Text.Contains("실행 횟수") && detail.Text.Contains("0~30")
                && detail.Text.Contains("최종 가중치 20") && !detail.Text.Contains("%")
                && ((detail.TextColor.r == .7f && detail.TextColor.g == .7f && detail.TextColor.b == .7f) == (state == EventEligibilityState.Ineligible)),
                "Full literal conditions preserve all comparisons, weight and unknown range without changing judgment: " + isSwitch + "/" + state + "/" + reason);
            Pointer(popup, EventType.TouchDown, EventType.TouchDown, point);
            Pointer(popup, EventType.TouchUp, EventType.TouchUp, point);
            Check(unreadable ? EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0
                : EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Forced == !candidate.CanSelect,
                "The next touch keeps normal, explicit override, or blocked target authority: " + isSwitch + "/" + state + "/" + reason);
        }

        foreach (bool isSwitch in new[] { false, true })
        {
            var popup = Start(); EventSelectionRuntime.IsSwitch = isSwitch;
            EventCandidate candidate = EventSelectionRuntime.Candidates[0];
            candidate.Summary = candidate.ConditionDetail = "조건 설명 읽기 실패 · 실행 여부와 별도";
            popup.Handle(LogPadCommand.Down); Draw(popup);
            Check(GUILayout.LiteralLabels.Count(value => value == candidate.ConditionDetail) == 1,
                "A shared fallback explanation is shown once rather than duplicated: " + isSwitch);
            popup.Handle(LogPadCommand.Activate);
            Check(EventSelectionRuntime.Commands == 1 && !EventSelectionRuntime.Forced,
                "Formatting failure cannot remove an already verified normal execution authority: " + isSwitch);
        }
        UnreadableReasonMarkers();
        ConditionParityLayout();
        Start();
    }

    private static void UnreadableReasonMarkers()
    {
        foreach (bool isSwitch in new[] { false, true })
        foreach (string failure in new[] { "read", "target", "result" })
        {
            var popup = Start(); EventSelectionRuntime.IsSwitch = isSwitch;
            EventCandidate candidate = EventSelectionRuntime.Candidates[0];
            candidate.Summary = "자료 확인 필요";
            candidate.ConditionDetail = "미리 읽은 전체 조건";
            candidate.CanSelect = candidate.CanForce = false;
            if (failure == "read") candidate.Eligibility = EventEligibilitySnapshot.Unreadable("자료 확인 필요");
            if (failure == "target") candidate.Target = null;
            if (failure == "result") candidate.Result = "";
            Draw(popup);
            var row = GUILayout.ScrollViews[0].Controls.First(control => control.Button);
            Check(row.Text.Split('\n')[2].Contains("! 자료 확인 필요") && !row.Text.Contains("#B3B3B3"),
                "Unreadable snapshots, targets, and result identifiers get a distinct failure marker independently of previous eligibility: " + isSwitch + "/" + failure);
            popup.Handle(LogPadCommand.Down); Draw(popup);
            Check(GUILayout.LiteralLabels.Contains("! 자료 확인 필요")
                && GUILayout.LiteralLabels.Contains("[A / Enter] 진행 불가 · 대상/결과 확인 필요"),
                "Failure reasoning remains explicit in selected detail and execution guidance: " + isSwitch + "/" + failure);
            popup.Handle(LogPadCommand.Activate);
            Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
                "A failure marker does not invent any execution authority: " + isSwitch + "/" + failure);
        }
        foreach (bool isSwitch in new[] { false, true })
        {
            var popup = Start(); EventSelectionRuntime.IsSwitch = isSwitch;
            EventCandidate candidate = EventSelectionRuntime.Candidates[0];
            candidate.Summary = "계산 미지원";
            candidate.Eligibility = new EventEligibilitySnapshot(EventEligibilityState.Unknown, EventEligibilityReason.Unsupported, null, candidate.Summary);
            candidate.CanSelect = false; candidate.CanForce = true;
            popup.Handle(LogPadCommand.Down); Draw(popup);
            Check(GUILayout.ScrollViews[0].Controls.First(control => control.Button).Text.Contains("? 계산 미지원")
                && GUILayout.LiteralLabels.Count(value => value == "? 계산 미지원") == 1
                && !GUILayout.LiteralLabels.Contains(candidate.Summary),
                "An unmarked unknown reason gets one literal question marker without duplicate fallback detail: " + isSwitch);
        }
    }

    private static void ConditionParityLayout()
    {
        foreach (bool isSwitch in new[] { false, true })
        foreach (var dimensions in new[] { (640, 480), (1280, 720), (1920, 1080), (3840, 2160) })
        foreach (int font in new[] { 12, 20, 64 })
        {
            var popup = Start(); EventSelectionRuntime.IsSwitch = isSwitch;
            Screen.width = dimensions.Item1; Screen.height = dimensions.Item2;
            DiceMaster.Config.ConfigManager.LogFontSize.Value = font;
            DiceMaster.Config.ConfigManager.LogWindowX.Value = 37;
            EventCandidate candidate = EventSelectionRuntime.Candidates[0];
            candidate.Summary = "명성 부족 · " + new string('조', 300) + "😀";
            candidate.Eligibility = new EventEligibilitySnapshot(EventEligibilityState.Ineligible,
                EventEligibilityReason.ConditionsNotMet, null, candidate.Summary);
            candidate.ConditionDetail = string.Join("\n", Enumerable.Range(0, 100).Select(index => "조건 " + index + ": 명성 >= 5 & 호감도 >= 10"));
            candidate.ResultDetailRich = "<color=#A3B86C>" + new string('결', 4000) + "</color>";
            Draw(popup);
            var before = GUILayout.ScrollViews[0].Controls.First(control => control.Button);
            Check(before.Text.Split('\n').Length == 3 && before.Text.Contains("…") && before.Rect.height <= (font + 4) * 3 + .1f,
                "Both popup kinds bound long reasons to three row lines at " + isSwitch + "/" + dimensions + "/" + font);
            popup.Handle(LogPadCommand.Down); Draw(popup); Draw(popup);
            var after = GUILayout.ScrollViews[0].Controls.First(control => control.Button);
            Check(before.Rect == after.Rect && GUILayout.LiteralLabels.Contains(candidate.ConditionDetail)
                && GUILayout.LiteralLabels.IndexOf("명성 부족") >= 0
                && GUILayout.LiteralLabels.IndexOf("명성 부족") < GUILayout.LiteralLabels.IndexOf(candidate.ConditionDetail)
                && !GUILayout.LiteralLabels.Contains(candidate.Summary),
                "All full conditions remain readable after one reason without moving the next tap target at " + isSwitch + "/" + dimensions + "/" + font);
            Check(GUILayout.ScrollViews.All(view => view.Viewport.width > 0 && view.Viewport.height > 0),
                "Long complete condition and result bodies retain separate usable list/detail viewports at " + isSwitch + "/" + dimensions + "/" + font);
            Check(GUILayout.Buttons.Count == EventSelectionRuntime.Candidates.Count
                && !GUILayout.Controls.Any(control => control.Text.Contains("로그 열기")),
                "Both popups omit the log control at " + isSwitch + "/" + dimensions + "/" + font);
            foreach (var instruction in GUILayout.Controls.Where(control => InstructionCaption(control.Text)))
                Check(instruction.VisibleRect.height >= instruction.Rect.height - .1f,
                    "Confirm/recovery instructions remain completely visible without the log control at " + isSwitch + "/" + dimensions + "/" + font);
            popup.Scroll(1); Draw(popup);
            Check(DetailScroll(popup) > 0 && EventSelectionRuntime.Commands == 0,
                "Reading either popup's long complete conditions scrolls detail without executing at " + isSwitch + "/" + dimensions + "/" + font);
        }
    }
}
