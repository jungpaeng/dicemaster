using System;
using System.Linq;
using DiceMaster.Config;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using DiceMaster.LiveLog;
using DiceMaster.LogUI;
using UnityEngine;

internal static partial class Program
{
    private static void RendererJudgmentPresentation()
    {
        Rect saved = ConfiguredRect(); int font = ConfigManager.LogFontSize.Value;
        int attempts = LogBridge.Reservations;
        object eventReservation = LogBridge.CurrentReservation("이벤트"), conditionReservation = LogBridge.CurrentReservation("조건");
        try
        {
            ConfigureLayout(new Rect(20, 20, 1200, 1000), 20);
            EventJudgmentStates();
            ConditionJudgmentStates();
            JudgmentReasonPrefixes();
            JudgmentFreshness();
            JudgmentPointerReservations();
            ConditionJudgmentRefreshGeometry();
        }
        finally
        {
            ConfigureLayout(saved, font);
            LogBridge.SetReservation("이벤트", eventReservation); LogBridge.SetReservation("조건", conditionReservation);
            LogBridge.Reservations = attempts;
            Input.Held.Clear(); Input.touchCount = 0; GUILayout.ResetPointerCapture();
            GamepadBridge.PointerActivity = null;
        }
    }

    private static EventEligibilitySnapshot Judgment(EventEligibilityState state, EventEligibilityReason reason, bool usable = true)
    {
        string summary = !usable ? "! 판정 자료 확인 필요" : state == EventEligibilityState.Unknown ?
            reason == EventEligibilityReason.Random ? "? 난수" : "? 미지원 계산" : state == EventEligibilityState.Ineligible ?
            reason == EventEligibilityReason.ZeroWeight ? "가중치 0" : "미충족" : "이름이 조건 미충족/난수라고 해도 판정에 사용하지 않음";
        return new EventEligibilitySnapshot(state, reason, state == EventEligibilityState.Eligible ? 20 : (int?)null,
            summary + " · 문자열로 판정하지 않음", usable,
            "전체 조건: <color=#FF0000>명성(3) >= 5</color> AND 호감도(20) >= 10\n기본 가중치: 10\n최종 가중치: 20");
    }

    private static LogItem JudgmentEvent(EventEligibilitySnapshot judgment)
    {
        var row = Item("judgment-event", new object());
        row.Title = "<color=#FF0000>색상 이벤트</color>";
        row.Description = "<color=#A3B86C>동전 +100</color> · <color=#CD594A>심상 -2</color>";
        row.EventEligibility = judgment;
        return row;
    }

    private static LogRenderer JudgmentEventRenderer(LogModel model, LogItem row)
    {
        model.ResetContext(920); Publish(model, "이벤트", row);
        var renderer = new LogRenderer(model, null, null);
        renderer.Handle(LogPadCommand.None); renderer.Handle(LogPadCommand.NextTab); renderer.Handle(LogPadCommand.NextTab);
        return renderer;
    }

    private static LogItem JudgmentCheckpoint(EventEligibilitySnapshot judgment)
    {
        var row = Checkpoint("judgment-condition", new object(), new object());
        LogConditionBranch original = row.ConditionDetails.Branches[0];
        row.ConditionDetails = new LogConditionDetails("처음 만족하는 분기로 진행", new[] {
            new LogConditionBranch("<color=#FF0000>색상 분기</color>", "앞 분기가 모두 미충족일 때 검사", judgment.Detail,
                original.Id, original.Target, judgment)
        });
        return row;
    }

    private static bool Gray(GUILayout.ControlRecord control) => control.TextColor.r == .7f && control.TextColor.g == .7f && control.TextColor.b == .7f;

    private static void EventJudgmentStates()
    {
        foreach (var state in new[] { EventEligibilityState.Eligible, EventEligibilityState.Ineligible, EventEligibilityState.Unknown })
        foreach (var reason in state == EventEligibilityState.Unknown ? new[] { EventEligibilityReason.Random, EventEligibilityReason.Unsupported, EventEligibilityReason.ReadFailure }
            : state == EventEligibilityState.Ineligible ? new[] { EventEligibilityReason.ConditionsNotMet, EventEligibilityReason.ZeroWeight } : new[] { EventEligibilityReason.None })
        {
            bool usable = reason != EventEligibilityReason.ReadFailure;
            var row = JudgmentEvent(Judgment(state, reason, usable));
            var model = new LogModel(); var renderer = JudgmentEventRenderer(model, row);
            Repaint(renderer);
            var before = GUILayout.ScrollViews[0].Controls.Single(control => control.Button);
            bool muted = state == EventEligibilityState.Ineligible;
            Check(before.Enabled && before.Text.Contains("#B3B3B3") == muted
                && before.Text.Contains("#FF0000") != muted,
                "Event title gray overrides its original color only for a current known unmet decision: " + state + "/" + reason);
            string marker = !usable ? "! 판정 자료 확인 필요" : state == EventEligibilityState.Unknown
                ? reason == EventEligibilityReason.Random ? "? 난수" : "? 미지원 계산" : reason == EventEligibilityReason.ZeroWeight ? "가중치 0" : state == EventEligibilityState.Ineligible ? "미충족" : "정상 발동 가능";
            Check(before.Text.Contains(marker) && !before.Text.Contains("문자열로 판정하지 않음"),
                "Event list preserves a short reason while deriving judgment state from structured data: " + state + "/" + reason);
            renderer.Handle(LogPadCommand.Activate); Repaint(renderer);
            var selected = GUILayout.ScrollViews[0].Controls.Single(control => control.Button);
            var condition = GUILayout.ScrollViews[1].Controls.Single(control => !control.Button && control.Text == row.EventEligibility.Detail);
            var result = GUILayout.ScrollViews[1].Controls.Single(control => !control.Button && control.Text == row.Description);
            Check(selected.Text.StartsWith("▶ ", StringComparison.Ordinal) && selected.Text.Contains("#B3B3B3") == muted
                && selected.TextColor.b < .5f && selected.Rect == before.Rect,
                "Selection highlighting stays visible beside event gray without moving its touch target: " + state + "/" + reason);
            Check(Gray(condition) == muted && !condition.RichText && result.RichText && result.Text.Contains("#A3B86C") && result.Text.Contains("#CD594A")
                && !Gray(result),
                "Event full conditions use literal judgment color while static results keep their own rich effect colors: " + state + "/" + reason);
            Check(GUILayout.LiteralLabels.IndexOf("조건 상세") < GUILayout.LiteralLabels.IndexOf(row.EventEligibility.Detail)
                && GUILayout.LiteralLabels.IndexOf(row.EventEligibility.Detail) < GUILayout.LiteralLabels.IndexOf("예상 결과")
                && GUILayout.LiteralLabels.Count(text => text == row.EventEligibility.Detail) == 1,
                "Event detail exposes full conditions once before the existing static result body: " + state + "/" + reason);
        }
    }

    private static void ConditionJudgmentStates()
    {
        foreach (var state in new[] { EventEligibilityState.Eligible, EventEligibilityState.Ineligible, EventEligibilityState.Unknown })
        foreach (var reason in state == EventEligibilityState.Unknown ? new[] { EventEligibilityReason.Random, EventEligibilityReason.Unsupported, EventEligibilityReason.ReadFailure }
            : new[] { state == EventEligibilityState.Ineligible ? EventEligibilityReason.ConditionsNotMet : EventEligibilityReason.None })
        {
            var row = JudgmentCheckpoint(Judgment(state, reason, reason != EventEligibilityReason.ReadFailure));
            var model = new LogModel(); var renderer = ConditionRenderer(model, row);
            Repaint(renderer);
            var checkpoint = GUILayout.ScrollViews[0].Controls.Single(control => control.Button);
            var branch = GUILayout.ScrollViews[1].Controls.Single(control => control.Button);
            bool muted = state == EventEligibilityState.Ineligible;
            Check(!checkpoint.Text.Contains("#B3B3B3") && branch.Text.Contains("#B3B3B3") == muted && branch.Text.Contains("#FF0000") != muted,
                "Condition gray belongs to the individual result, never the entire checkpoint: " + state + "/" + reason);
            var condition = GUILayout.ScrollViews[1].Controls.Single(control => !control.Button && control.Text == row.ConditionDetails.Branches[0].Description);
            Check(Gray(condition) == muted && !condition.RichText,
                "Each branch's whole condition body uses its structured judgment color, immune to embedded color tags: " + state + "/" + reason);
            var context = GUILayout.ScrollViews[1].Controls.Single(control => !control.Button && control.Text == row.ConditionDetails.Branches[0].Context);
            Check(Gray(context) == muted && !context.RichText,
                "Each branch context shares its title and condition's literal judgment color: " + state + "/" + reason);
            if (state == EventEligibilityState.Unknown)
                Check(GUILayout.ScrollViews[1].Controls.Any(control => !control.Button && control.Text.StartsWith(reason == EventEligibilityReason.ReadFailure ? "! " : "? ", StringComparison.Ordinal)),
                    "Unknown and unreadable branches expose distinct reason markers: " + reason);
            renderer.Handle(LogPadCommand.Right); Repaint(renderer);
            var selected = GUILayout.ScrollViews[1].Controls.Single(control => control.Button);
            Check(selected.Rect == branch.Rect && selected.Text == branch.Text && selected.TextColor.b < .5f
                && Area(renderer) == "ConditionDetails" && model.Tab("조건").SelectedBranchId == "1",
                "Entering a branch preserves judgment text, touch geometry and selected emphasis: " + state + "/" + reason);
        }
    }

    private static void JudgmentReasonPrefixes()
    {
        foreach (string tab in new[] { "이벤트", "조건" })
        foreach (string summary in new[] { "앞 분기 우선 · 이 조건 자체는 충족", "? 앞 분기 미확정 · 이 조건 자체는 충족" })
        {
            bool unknown = summary.StartsWith("?", StringComparison.Ordinal);
            var eligibility = new EventEligibilitySnapshot(unknown ? EventEligibilityState.Unknown : EventEligibilityState.Ineligible,
                unknown ? EventEligibilityReason.Random : EventEligibilityReason.ConditionsNotMet, null, summary, detail: "이 조건 자체는 충족");
            LogItem row = tab == "이벤트" ? JudgmentEvent(eligibility) : JudgmentCheckpoint(eligibility);
            var model = new LogModel(); var renderer = tab == "이벤트" ? JudgmentEventRenderer(model, row) : ConditionRenderer(model, row);
            Repaint(renderer);
            var pane = GUILayout.ScrollViews[tab == "이벤트" ? 0 : 1];
            string expected = unknown ? "? 앞 분기 미확정" : "앞 분기 우선";
            Check(pane.Controls.Any(control => control.Text.Contains(expected))
                && !pane.Controls.Any(control => control.Text.Contains("? ?") || control.Text.Contains("조건 미충족")),
                "Short reasons preserve earlier-branch precedence and uncertainty without inventing own-condition failure: " + tab + "/" + summary);
        }
    }

    private static void JudgmentFreshness()
    {
        foreach (string tab in new[] { "이벤트", "조건" })
        foreach (string failure in new[] { "waiting", "read", "retained", "publication" })
        {
            var eligibility = Judgment(EventEligibilityState.Ineligible, EventEligibilityReason.ConditionsNotMet);
            LogItem row = tab == "이벤트" ? JudgmentEvent(eligibility) : JudgmentCheckpoint(eligibility);
            var model = new LogModel(); var renderer = tab == "이벤트" ? JudgmentEventRenderer(model, row) : ConditionRenderer(model, row);
            if (tab == "이벤트") renderer.Handle(LogPadCommand.Activate);
            Repaint(renderer);
            if (failure == "publication") model.MarkPublicationFailure(tab);
            else
            {
                row.RefreshState = failure == "waiting" ? LogRefreshState.Waiting : failure == "read" ? LogRefreshState.ReadFailure : LogRefreshState.RetainedFailure;
                row.RefreshStatus = "보존한 이전 조회 설명"; row.HasLastGood = true;
                Publish(model, tab, row);
            }
            Repaint(renderer);
            var list = GUILayout.ScrollViews[0]; var detail = GUILayout.ScrollViews[1];
            Check(!list.Controls.Any(control => control.Text.Contains("#B3B3B3"))
                && !detail.Controls.Any(control => control.Button && control.Text.Contains("#B3B3B3"))
                && !detail.Controls.Any(Gray),
                "A failed or stale publication cannot retain an old gray decision: " + tab + "/" + failure);
            Check((tab == "이벤트" ? list : detail).Controls.Any(control => control.Text.Contains("? 판정 미확인"))
                && !model.Tab(tab).CanReserveSelected,
                "Stale rows expose uncertain judgment while keeping current reservation changes blocked: " + tab + "/" + failure);
            Check(row.EventEligibility == eligibility || row.ConditionDetails.Branches[0].Eligibility == eligibility,
                "Freshness presentation does not mutate the last-good eligibility snapshot: " + tab + "/" + failure);
        }
    }

    private static void JudgmentPointerReservations()
    {
        foreach (string tab in new[] { "이벤트", "조건" })
        foreach (int kind in new[] { 0, 1, 2, 3 })
        {
            var eligibility = Judgment(EventEligibilityState.Ineligible, EventEligibilityReason.ConditionsNotMet);
            LogItem row = tab == "이벤트" ? JudgmentEvent(eligibility) : JudgmentCheckpoint(eligibility);
            var model = new LogModel(); var renderer = tab == "이벤트" ? JudgmentEventRenderer(model, row) : ConditionRenderer(model, row);
            LogBridge.SetReservation(tab, null); int attempts = LogBridge.Reservations;
            Input.touchCount = kind == 1 || kind == 3 ? 1 : 0;
            EventType down = kind == 3 ? EventType.TouchDown : EventType.MouseDown, up = kind == 3 ? EventType.TouchUp : EventType.MouseUp;
            EventType rawDown = kind >= 2 ? EventType.TouchDown : EventType.MouseDown, rawUp = kind >= 2 ? EventType.TouchUp : EventType.MouseUp;
            Repaint(renderer);
            Func<GUILayout.ControlRecord, bool> match = control => control.Button && control.Text.Contains(tab == "이벤트" ? "색상 이벤트" : "색상 분기");
            Vector2 point = PointerTarget(renderer, match);
            Rect before = GUILayout.Controls.Find(control => match(control)).WindowLocalRect;
            DrawPointer(renderer, down, rawDown, point); SoftResetForPointer(renderer);
            DrawPointer(renderer, up, rawUp, point); SoftResetForPointer(renderer); Repaint(renderer);
            Check(LogBridge.Reservations == attempts && GUILayout.Controls.Find(control => match(control)).WindowLocalRect == before,
                "First pointer gesture selects a gray result without reserving or moving its heading: " + tab + "/" + kind);
            DrawPointer(renderer, down, rawDown, point); SoftResetForPointer(renderer);
            DrawPointer(renderer, up, rawUp, point); SoftResetForPointer(renderer);
            object target = tab == "이벤트" ? row.Target : row.ConditionDetails.Branches[0].Target;
            Check(LogBridge.Reservations == attempts + 1 && LogBridge.IsReserved(tab, target),
                "The immediate next same-point gesture reserves a gray result exactly once: " + tab + "/" + kind);
            renderer.Handle(LogPadCommand.Tools); renderer.Handle(LogPadCommand.Back);
            Check(Area(renderer) == (tab == "조건" ? "ConditionDetails" : "List"),
                "Tools return keeps the gray result's active navigation area: " + tab + "/" + kind);
            renderer.Handle(LogPadCommand.Activate);
            Check(LogBridge.Reservations == attempts + 2 && !LogBridge.IsReserved(tab, target),
                "Fresh pad confirmation still cancels the selected gray result reservation: " + tab + "/" + kind);
        }
    }

    private static void ConditionJudgmentRefreshGeometry()
    {
        Rect saved = ConfiguredRect(); int font = ConfigManager.LogFontSize.Value;
        try
        {
            foreach (int width in new[] { 540, 872 })
            foreach (int kind in new[] { 0, 1, 2, 3 })
            foreach (EventEligibilityState next in new[] { EventEligibilityState.Eligible, EventEligibilityState.Ineligible, EventEligibilityState.Unknown })
            {
                ConfigureLayout(new Rect(20, 20, width, 900), 32);
                object checkpoint = new object(), first = new object(), second = new object();
                LogItem Build(EventEligibilityState predecessor)
                {
                    var current = new EventEligibilitySnapshot(predecessor, predecessor == EventEligibilityState.Unknown ? EventEligibilityReason.Random :
                        predecessor == EventEligibilityState.Ineligible ? EventEligibilityReason.ConditionsNotMet : EventEligibilityReason.None, null,
                        predecessor == EventEligibilityState.Unknown ? "? 앞 분기 미확정 · 원래 조건" : predecessor == EventEligibilityState.Ineligible ? "앞 분기 우선 · 원래 조건" : "원래 조건",
                        detail: "모든 갱신에서 같은 조건 본문");
                    return new LogItem { Id = "same-checkpoint", RawId = "same-checkpoint", Title = "같은 체크포인트", Target = checkpoint,
                        ConditionDetails = new LogConditionDetails("", new[] {
                            new LogConditionBranch("첫 분기", "동일 맥락", "모든 갱신에서 같은 조건 본문", "1", first, current),
                            new LogConditionBranch("다음 분기", "동일 맥락", "모든 갱신에서 같은 조건 본문", "2", second,
                                Judgment(EventEligibilityState.Ineligible, EventEligibilityReason.ConditionsNotMet))
                        }) };
                }
                var model = new LogModel(); var renderer = ConditionRenderer(model, Build(EventEligibilityState.Eligible));
                LogBridge.SetReservation("조건", null); int attempts = LogBridge.Reservations;
                Input.touchCount = kind == 1 || kind == 3 ? 1 : 0;
                EventType down = kind == 3 ? EventType.TouchDown : EventType.MouseDown, up = kind == 3 ? EventType.TouchUp : EventType.MouseUp;
                EventType rawDown = kind >= 2 ? EventType.TouchDown : EventType.MouseDown, rawUp = kind >= 2 ? EventType.TouchUp : EventType.MouseUp;
                Repaint(renderer);
                model.Tab("조건").DetailScroll = Math.Max(0, GUILayout.ScrollViews[1].Controls.Single(control => control.Button && control.Text.Contains("다음 분기")).Rect.y - 20);
                Repaint(renderer);
                Vector2 point = PointerTarget(renderer, control => control.Button && control.Text.Contains("다음 분기"));
                DrawPointer(renderer, down, rawDown, point); DrawPointer(renderer, up, rawUp, point); Repaint(renderer);
                var before = GUILayout.ScrollViews[1].Controls.Single(control => control.Button && control.Text.Contains("다음 분기"));
                Check(model.Tab("조건").SelectedBranchId == "2" && LogBridge.Reservations == attempts,
                    "The first gesture selects only the following branch before its predecessor changes: " + width + "/" + kind + "/" + next);
                Publish(model, "조건", Build(next)); Repaint(renderer);
                var after = GUILayout.ScrollViews[1].Controls.Single(control => control.Button && control.Text.Contains("다음 분기"));
                Check(before.WindowLocalRect == after.WindowLocalRect && model.Tab("조건").SelectedBranchId == "2",
                    "Fresh predecessor judgment changes preserve the exact following-branch touch rectangle: " + width + "/" + kind + "/" + next);
                DrawPointer(renderer, down, rawDown, point); DrawPointer(renderer, up, rawUp, point);
                Check(LogBridge.Reservations == attempts + 1 && LogBridge.IsReserved("조건", second),
                    "The immediate next same-point touch still reserves the selected branch after a fresh judgment change: " + width + "/" + kind + "/" + next);
                LogBridge.SetReservation("조건", null);
            }
        }
        finally { ConfigureLayout(saved, font); }
    }
}
