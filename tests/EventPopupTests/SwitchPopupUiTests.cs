using System;
using System.Linq;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using UnityEngine;

internal static partial class Program
{
    private static void SwitchPopupRegressions()
    {
        foreach (var pair in new[] {
            (EventType.MouseDown, EventType.MouseUp, EventType.MouseDown, EventType.MouseUp),
            (EventType.MouseDown, EventType.MouseUp, EventType.TouchDown, EventType.TouchUp),
            (EventType.TouchDown, EventType.TouchUp, EventType.TouchDown, EventType.TouchUp) })
        foreach (bool forced in new[] { false, true })
        {
            var popup = Start();
            EventSelectionRuntime.IsSwitch = true;
            string id = forced ? "blocked" : "eligible";
            Draw(popup);
            Check(GUILayout.LiteralLabels.Contains("이번 대화의 조건 분기 선택") &&
                GUILayout.LiteralLabels.Contains("[A / Enter] 분기를 선택하세요") &&
                GUILayout.LiteralLabels.Contains("[B / Esc] 원래 판정으로 진행") &&
                !GUILayout.LiteralLabels.Contains("[B / Esc] 무작위로 진행"),
                "Switch popup distinguishes the decision and the original-game fallback");
            Click(popup, text => RowCaption(text, id), pair.Item1, pair.Item2, pair.Item3, pair.Item4);
            Check(EventSelectionRuntime.Commands == 0, "First switch row press only selects");
            Draw(popup);
            Check(GUILayout.LiteralLabels.Contains("조건 상세") && !GUILayout.LiteralLabels.Contains("발동 조건") && !GUILayout.LiteralLabels.Contains("분기 조건"),
                "Switch detail describes branch conditions");
            Check(GUILayout.LiteralLabels.Contains(forced ? "[A / Enter] 조건 무시하고 이번만 진행" : "[A / Enter] 이 분기로 진행"),
                "Switch confirmation preserves the displayed normal or forced intent");
            Check(GUILayout.Buttons.All(text => !InstructionCaption(text)), "Switch footer stays literal guidance");
            Click(popup, SelectedRowCaption, pair.Item1, pair.Item2, pair.Item3, pair.Item4);
            Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Selected == id && EventSelectionRuntime.Forced == forced,
                "The next fresh switch row press confirms once with its captured mode");
        }
        var original = Start(); EventSelectionRuntime.IsSwitch = true;
        original.Handle(LogPadCommand.Back);
        Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Random, "Switch B requests the original game decision");
        SwitchConditionsPresentation();
        Start(); // Leave the shared runtime boundary in its default event mode.
    }

    private static void SwitchConditionsPresentation()
    {
        foreach (EventEligibilityState state in new[] { EventEligibilityState.Eligible, EventEligibilityState.Ineligible, EventEligibilityState.Unknown })
        foreach (EventEligibilityReason reason in state == EventEligibilityState.Unknown
            ? new[] { EventEligibilityReason.Random, EventEligibilityReason.Unsupported, EventEligibilityReason.ReadFailure }
            : new[] { state == EventEligibilityState.Eligible ? EventEligibilityReason.None : EventEligibilityReason.ConditionsNotMet })
        {
            var popup = Start(); EventSelectionRuntime.IsSwitch = true;
            EventCandidate candidate = EventSelectionRuntime.Candidates[0];
            candidate.Summary = (state != EventEligibilityState.Unknown ? "" : reason == EventEligibilityReason.ReadFailure ? "! " : "? ")
                + "A10005(A10005)(0) = 0(0) | 랜덤값0_30(??) <= 10(10)";
            candidate.Eligibility = new EventEligibilitySnapshot(state, reason, null, candidate.Summary,
                reason != EventEligibilityReason.ReadFailure);
            candidate.CanSelect = state == EventEligibilityState.Eligible;
            candidate.CanForce = state != EventEligibilityState.Eligible && reason != EventEligibilityReason.ReadFailure;
            candidate.ResultSummaryRich = "<color=#A3B86C>동전 +100</color>";
            candidate.ResultDetailRich = "<color=#CD594A>심상 -2</color>\n" + new string('결', 3000);
            Draw(popup);
            var before = GUILayout.Controls.Single(control => control.Button && RowCaption(control.Text, "eligible"));
            string[] lines = before.Text.Split('\n');
            Check(lines.Length == 3 && lines[2].Contains("조건:") && lines[2].Contains("A10005"),
                "Switch rows advertise an actual condition excerpt before selection: " + state + "/" + reason);
            Check(lines[0].Contains("#B3B3B3") == (state == EventEligibilityState.Ineligible)
                && lines[2].Contains("#B3B3B3") == (state == EventEligibilityState.Ineligible)
                && lines[1].Contains("#A3B86C") && !lines[1].Contains("#B3B3B3") && before.Enabled,
                "Only known unmet Switch titles/conditions dim, while unknown and rich results retain their meaning: " + state + "/" + reason);
            Vector2 point = Point(popup, text => RowCaption(text, "eligible"));
            Pointer(popup, EventType.TouchDown, EventType.TouchDown, point);
            Pointer(popup, EventType.TouchUp, EventType.TouchUp, point);
            Draw(popup);
            var after = GUILayout.Controls.Single(control => control.Button && RowCaption(control.Text, "eligible"));
            var condition = GUILayout.Controls.Single(control => !control.Button && control.Text == candidate.Summary);
            Check(before.Rect == after.Rect && after.Text.StartsWith("▶ ") && EventSelectionRuntime.Commands == 0,
                "Selecting a Switch branch preserves the next same-point touch target: " + state + "/" + reason);
            Check(GUILayout.LiteralLabels.IndexOf("조건 상세") < GUILayout.LiteralLabels.IndexOf(candidate.Summary)
                && GUILayout.LiteralLabels.IndexOf(candidate.Summary) < GUILayout.LiteralLabels.IndexOf("예상 결과")
                && !condition.RichText && condition.VisibleRect.height > 0,
                "The full literal Switch condition is visible before even very long result detail: " + state + "/" + reason);
            Check((condition.TextColor.r == .7f && condition.TextColor.g == .7f && condition.TextColor.b == .7f)
                == (state == EventEligibilityState.Ineligible),
                "Selected full conditions share the known-unmet gray distinction: " + state + "/" + reason);
            Pointer(popup, EventType.TouchDown, EventType.TouchDown, point);
            Pointer(popup, EventType.TouchUp, EventType.TouchUp, point);
            Check(reason == EventEligibilityReason.ReadFailure ? EventSelectionRuntime.Commands == 0
                : EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Forced == !candidate.CanSelect,
                "Switch condition presentation preserves the next fresh touch's exact normal/force/read-failure authority: " + state + "/" + reason);
        }

        foreach (var size in new[] { (640, 480), (1280, 720), (1920, 1080), (3840, 2160) })
        foreach (int font in new[] { 12, 20, 64 })
        {
            var popup = Start(); EventSelectionRuntime.IsSwitch = true;
            Screen.width = size.Item1; Screen.height = size.Item2;
            DiceMaster.Config.ConfigManager.LogFontSize.Value = font;
            DiceMaster.Config.ConfigManager.LogWindowX.Value = 37;
            EventCandidate candidate = EventSelectionRuntime.Candidates[0];
            candidate.Summary = "직전 분기 미충족 시 기본 분기\n" + new string('조', 2000) + " <color=red>문자</color>";
            candidate.ResultDetail = new string('결', 4000);
            popup.OpenLog = () => { };
            popup.LogHint = "F8 로그";
            Draw(popup);
            var before = GUILayout.ScrollViews[0].Controls.First(control => control.Button);
            Check(before.Text.Split('\n').Length == 3 && before.Text.Contains("…") && !before.Text.Contains("<color=red>"),
                "Long Switch row conditions remain bounded escaped excerpts at " + size + " font=" + font);
            popup.Handle(LogPadCommand.Down); Draw(popup); Draw(popup);
            var after = GUILayout.ScrollViews[0].Controls.First(control => control.Button);
            Check(before.Rect == after.Rect && GUILayout.LiteralLabels.Contains(candidate.Summary),
                "Long full conditions are retained without changing the selected row geometry at " + size + " font=" + font);
            Check(GUILayout.ScrollViews.All(view => view.Viewport.width > 0 && view.Viewport.height > 0)
                && GUILayout.ScrollViews[1].Controls.First(control => control.Text == "조건 상세").VisibleRect.height > 0,
                "Switch conditions lead an independently scrollable readable detail viewport at " + size + " font=" + font);
            foreach (var control in GUILayout.Controls.Where(control => InstructionCaption(control.Text) || control.Text.StartsWith("로그 열기")))
                Check(control.VisibleRect.height >= control.Rect.height - .1f,
                    "Switch log access and progress hints remain visible at " + size + " font=" + font);
            Check(EventSelectionRuntime.Commands == 0, "Reading long Switch conditions never executes a result");
        }
    }
}
