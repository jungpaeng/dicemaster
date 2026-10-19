using System;
using System.Reflection;
using DiceMaster.Config;
using DiceMaster.Gamepad;
using DiceMaster.LogUI;
using DiceMaster.LiveLog;
using UnityEngine;

internal static partial class Program
{
    private static LogItem Checkpoint(string id, object checkpoint, params object[] targets)
    {
        var branches = new LogConditionBranch[targets.Length];
        for (int i = 0; i < targets.Length; i++)
            branches[i] = new LogConditionBranch("분기 " + (i + 1), i == 0 ? "이 조건을 만족하면" : "앞 분기가 모두 불충족일 때 검사",
                "조건 본문 " + (i + 1) + " | 조건의 다음 줄\n결과 " + (i + 1), (i + 1).ToString(), targets[i]);
        return new LogItem { Id = id, RawId = id, Title = "체크포인트 " + id, Target = checkpoint,
            Description = "호환 설명은 반복하지 않음", ConditionDetails = new LogConditionDetails("처음 만족하는 분기로 진행", branches) };
    }

    private static LogRenderer ConditionRenderer(LogModel model, LogItem first, LogItem second = null, Action close = null)
    {
        model.ResetContext(811);
        Publish(model, "조건", second == null ? new[] { first } : new[] { first, second });
        var renderer = new LogRenderer(model, null, close);
        renderer.Handle(LogPadCommand.None); renderer.Handle(LogPadCommand.NextTab);
        renderer.Handle(LogPadCommand.Activate);
        return renderer;
    }

    private static void RendererReservableConditionDetails()
    {
        Rect saved = ConfiguredRect(); int font = ConfigManager.LogFontSize.Value;
        int attempts = LogBridge.Reservations; object last = LogBridge.LastTarget;
        object previous = LogBridge.CurrentReservation("조건");
        try
        {
            ConfigureLayout(new Rect(20, 20, 1200, 1000), 20);
            LogBridge.SetReservation("조건", null);
            ConditionDetailPadNavigation();
            ConditionDetailKeyboardNavigation();
            ConditionDetailPublicationBoundaries();
            ConditionDetailNativePointers();
            ConditionDetailPointerInvalidation();
            ConditionDetailMeasuredScroll();
            ConditionDetailStableTouchGeometry();
            ConditionDetailEntryViewportGeometry();
        }
        finally
        {
            ConfigureLayout(saved, font); LogBridge.SetReservation("조건", previous);
            LogBridge.Reservations = attempts; LogBridge.LastTarget = last;
            LogBridge.DuringReservation = null; LogBridge.ReservationReadable = true;
            Input.Held.Clear(); GUILayout.ButtonInput = null; GUILayout.ResetPointerCapture();
            GamepadBridge.PointerActivity = null; GamepadBridge.KeyboardActivity = null; GamepadBridge.Cancelled = null;
        }
    }

    private static void ConditionDetailPadNavigation()
    {
        int attempts = LogBridge.Reservations, closes = 0;
        object checkpoint = new object(), first = new object(), second = new object();
        object otherCheckpoint = new object();
        LogItem parent = Checkpoint("parent", checkpoint, first, second);
        LogItem other = Checkpoint("other", otherCheckpoint, new object());
        var model = new LogModel(); var renderer = ConditionRenderer(model, parent, other, () => closes++);
        var state = model.Tab("조건"); Repaint(renderer);
        Check(state.Snapshot.Rows.Count == 2 && GUILayout.ScrollViews[0].Controls.FindAll(c => c.Button).Count == 2,
            "condition list has one row per checkpoint rather than one row per result");
        Check(state.SelectedId == parent.Id && state.SelectedBranch == null && Area(renderer) == "List" && LogBridge.Reservations == attempts,
            "first A selects only a checkpoint without selecting or reserving a result");
        var pane = GUILayout.ScrollViews[1];
        Check(pane.Controls.Exists(c => c.Button && c.Text == "분기 1") && pane.Controls.Exists(c => c.Button && c.Text == "분기 2")
            && pane.Controls.Exists(c => !c.Button && c.Text == parent.ConditionDetails.Branches[1].Description)
            && !pane.Controls.Exists(c => c.Text == parent.Description),
            "selected checkpoint exposes every full structured branch in its detail pane without duplicate flattened text");
        Check(!GUILayout.ScrollViews[0].Controls.Exists(c => c.Text.Contains("분기 1", StringComparison.Ordinal)),
            "checkpoint captions do not duplicate branch result rows");
        renderer.Handle(LogPadCommand.Right);
        Check(Area(renderer) == "ConditionDetails" && state.SelectedBranchId == "1" && LogBridge.Reservations == attempts && !state.ExecutionFocus,
            "Right enters details and selects the first branch without reserving it");
        renderer.Handle(LogPadCommand.Up);
        Check(Area(renderer) == "ConditionDetails" && state.SelectedBranchId == "1", "Up at the first branch does not jump into popup settings");
        renderer.Handle(LogPadCommand.Down);
        Check(state.SelectedId == parent.Id && state.SelectedBranchId == "2" && !state.ExecutionFocus,
            "detail Down changes only the selected result, preserving the checkpoint");
        Repaint(renderer);
        Check(GUILayout.LiteralLabels.Contains("▶ 조건 상세") && GUILayout.ScrollViews[1].Controls.Exists(c => c.Button && c.Text == "분기 2" && c.TextColor.b < .5f),
            "detail heading and exact selected result have visible focus markers");
        object eventBefore = LogBridge.CurrentReservation("이벤트"), missionBefore = LogBridge.CurrentReservation("미션");
        renderer.Handle(LogPadCommand.Activate);
        Check(LogBridge.Reservations == attempts + 1 && LogBridge.IsReserved("조건", second) && Area(renderer) == "ConditionDetails"
            && !state.ExecutionFocus && state.BeginReservation() == null,
            "detail A reserves the exact selected result once and retains detail navigation without authority");
        Check(ReferenceEquals(eventBefore, LogBridge.CurrentReservation("이벤트")) && ReferenceEquals(missionBefore, LogBridge.CurrentReservation("미션")),
            "condition detail reservation cannot change event or mission slots");
        Repaint(renderer);
        Check(GUILayout.ScrollViews[0].Controls.Exists(c => c.Button && c.Text.Contains("체크포인트 parent", StringComparison.Ordinal) && c.Text.Contains("예약 중", StringComparison.Ordinal))
            && GUILayout.ScrollViews[1].Controls.Exists(c => !c.Button && c.Text.Contains("활성화 (예약 중)", StringComparison.Ordinal)),
            "parent row and the exact reserved branch both expose reservation state");
        renderer.Handle(LogPadCommand.Activate);
        Check(LogBridge.Reservations == attempts + 2 && !LogBridge.IsReserved("조건", second), "fresh detail A releases that result exactly once");
        renderer.Handle(LogPadCommand.Tools); renderer.Handle(LogPadCommand.Back);
        Check(Area(renderer) == "ConditionDetails" && state.SelectedBranchId == "2", "B from tools restores the prior condition detail area");
        renderer.Handle(LogPadCommand.Left);
        Check(Area(renderer) == "List" && state.SelectedBranchId == "2" && closes == 0, "Left returns to checkpoints while remembering the result");
        renderer.Handle(LogPadCommand.Activate);
        Check(Area(renderer) == "ConditionDetails" && state.SelectedBranchId == "2" && LogBridge.Reservations == attempts + 2,
            "A reenters the remembered branch without changing a reservation");
        renderer.Handle(LogPadCommand.Back);
        Check(Area(renderer) == "List" && closes == 0, "first B returns from details without closing");
        renderer.Handle(LogPadCommand.Down);
        Check(state.SelectedId == other.Id && state.SelectedBranchId == null && state.DetailScroll == 0,
            "changing checkpoint clears the previous result and detail scroll");
        renderer.Handle(LogPadCommand.Back);
        Check(closes == 1 && LogBridge.Reservations == attempts + 2, "B from checkpoint list closes with no reservation side effect");
    }

    private static void ConditionDetailKeyboardNavigation()
    {
        int attempts = LogBridge.Reservations; object target = new object();
        var model = new LogModel(); var renderer = ConditionRenderer(model, Checkpoint("keyboard", new object(), target, new object()));
        PressKey(renderer, KeyCode.Return); PressKey(renderer, KeyCode.Return);
        Check(Area(renderer) == "ConditionDetails" && model.Tab("조건").SelectedBranchId == "1" && LogBridge.Reservations == attempts,
            "Enter enters detail once and its held repeat cannot reserve");
        ReleaseKey(renderer, KeyCode.Return); PressKey(renderer, KeyCode.Return); PressKey(renderer, KeyCode.Return);
        Check(LogBridge.Reservations == attempts + 1 && LogBridge.IsReserved("조건", target), "a fresh Enter reserves once and held Enter cannot cancel it");
        ReleaseKey(renderer, KeyCode.Return); TapKey(renderer, KeyCode.Space);
        Check(LogBridge.Reservations == attempts + 2 && !LogBridge.IsReserved("조건", target), "fresh Space cancels the chosen branch once");
        TapKey(renderer, KeyCode.DownArrow);
        Check(model.Tab("조건").SelectedBranchId == "2" && model.Tab("조건").SelectedId == "keyboard", "keyboard Down moves branches inside the selected checkpoint");
        TapKey(renderer, KeyCode.Escape);
        Check(Area(renderer) == "List", "Escape leaves branch details for the checkpoint list");
    }

    private static void ConditionDetailPublicationBoundaries()
    {
        int attempts = LogBridge.Reservations;
        object checkpoint = new object(), first = new object(), second = new object();
        var model = new LogModel(); var renderer = ConditionRenderer(model, Checkpoint("refresh", checkpoint, first, second));
        renderer.Handle(LogPadCommand.Right); renderer.Handle(LogPadCommand.Down); Repaint(renderer);
        var state = model.Tab("조건"); state.DetailScroll = 73;
        Publish(model, "조건", Checkpoint("refresh", checkpoint, first, second));
        Repaint(renderer);
        Check(state.SelectedId == "refresh" && state.SelectedBranchId == "2" && state.DetailScroll == 73 && Area(renderer) == "ConditionDetails",
            "healthy publication preserves checkpoint, branch identity, detail scroll and focus");
        LogItem reordered = Checkpoint("refresh", checkpoint, first, second);
        reordered.ConditionDetails = new LogConditionDetails("reordered", new[] { reordered.ConditionDetails.Branches[1], reordered.ConditionDetails.Branches[0] });
        Publish(model, "조건", reordered); Repaint(renderer);
        Check(state.SelectedBranchId == "2" && ReferenceEquals(state.SelectedBranch.Target, second) && state.DetailScroll == 73,
            "reordering structured branches preserves their stable selected result instead of its old list position");
        state.ExecutionFocus = true; ReservationTicket old = state.BeginReservation();
        Publish(model, "조건", Checkpoint("refresh", checkpoint, first, new object()));
        renderer.Handle(LogPadCommand.Activate);
        Check(state.SelectedId == "refresh" && state.SelectedBranchId == null && Area(renderer) == "List" && !state.CanActivate(old)
            && LogBridge.Reservations == attempts,
            "branch target replacement preserves parent but clears result and consumes the pending confirmation");
        renderer.Handle(LogPadCommand.Activate);
        Check(state.SelectedBranchId == "1" && Area(renderer) == "ConditionDetails" && LogBridge.Reservations == attempts,
            "replacement requires a fresh detail entry before any reservation");
        foreach (LogRefreshState failure in new[] { LogRefreshState.Waiting, LogRefreshState.ReadFailure, LogRefreshState.RetainedFailure })
        {
            LogItem row = Checkpoint("refresh", checkpoint, first, second); row.RefreshState = failure;
            Publish(model, "조건", row); renderer.Handle(LogPadCommand.Activate); Repaint(renderer);
            Check(LogBridge.Reservations == attempts && !state.CanReserveSelected && !state.ExecutionFocus,
                "unhealthy checkpoint keeps details readable but refuses branch reservation: " + failure);
        }
        Publish(model, "조건", Checkpoint("refresh", checkpoint, first, second)); renderer.Handle(LogPadCommand.Right);
        model.MarkPublicationFailure("조건"); renderer.Handle(LogPadCommand.Activate);
        Check(LogBridge.Reservations == attempts && !state.ExecutionFocus, "publication failure cannot reserve a still-visible selected branch");
        Publish(model, "조건", Checkpoint("refresh", checkpoint, first, second)); renderer.Handle(LogPadCommand.Right);
        renderer.Handle(LogPadCommand.Activate);
        Check(LogBridge.Reservations == attempts + 1 && LogBridge.IsReserved("조건", first), "fresh confirmation after healthy recovery can reserve the explicitly selected result");
        LogBridge.SetReservation("조건", null);
    }

    private static void ConditionDetailNativePointers()
    {
        int oldTouch = Input.touchCount;
        try
        {
            foreach (int kind in new[] { 0, 1, 2, 3 })
            {
                Input.touchCount = kind == 1 || kind == 3 ? 1 : 0;
                EventType down = kind == 3 ? EventType.TouchDown : EventType.MouseDown, up = kind == 3 ? EventType.TouchUp : EventType.MouseUp;
                EventType rawDown = kind >= 2 ? EventType.TouchDown : EventType.MouseDown, rawUp = kind >= 2 ? EventType.TouchUp : EventType.MouseUp;
                int attempts = LogBridge.Reservations; object first = new object(), second = new object();
                var model = new LogModel(); var renderer = ConditionRenderer(model, Checkpoint("pointer", new object(), first, second));
                LogBridge.SetReservation("조건", null); GUILayout.ResetPointerCapture();
                Action<Func<GUILayout.ControlRecord, bool>> click = match =>
                {
                    Vector2 point = PointerTarget(renderer, match);
                    DrawPointer(renderer, down, rawDown, point); SoftResetForPointer(renderer);
                    DrawPointer(renderer, up, rawUp, point); SoftResetForPointer(renderer);
                };
                click(c => c.Button && c.Text.Contains("체크포인트 pointer", StringComparison.Ordinal));
                click(c => c.Button && c.Text.Contains("체크포인트 pointer", StringComparison.Ordinal));
                Check(LogBridge.Reservations == attempts && Area(renderer) == "List" && model.Tab("조건").SelectedBranchId == null,
                    "repeated native checkpoint touch stays selection-only, kind " + kind);
                click(c => c.Button && c.Text == "분기 2");
                Check(Area(renderer) == "ConditionDetails" && model.Tab("조건").SelectedBranchId == "2" && LogBridge.Reservations == attempts,
                    "first native branch touch selects that result only, kind " + kind);
                click(c => c.Button && c.Text == "분기 2");
                Check(LogBridge.Reservations == attempts + 1 && LogBridge.IsReserved("조건", second) && Area(renderer) == "ConditionDetails",
                    "second native touch reserves exactly that selected result once, kind " + kind);
                click(c => c.Button && c.Text == "분기 2");
                Check(LogBridge.Reservations == attempts + 2 && !LogBridge.IsReserved("조건", second),
                    "next fresh native touch cancels the same result without extra touches, kind " + kind);
                renderer.ResetInputFocus(preserveNavigationArea: true); renderer.Handle(LogPadCommand.Up);
                Check(Area(renderer) == "ConditionDetails" && model.Tab("조건").SelectedBranchId == "1" && model.Tab("조건").SelectedId == "pointer",
                    "physical pad takeover preserves touched detail area and Down/Up changes a result rather than checkpoint, kind " + kind);
                Vector2 samePoint = PointerTarget(renderer, c => c.Button && c.Text == "분기 2");
                Rect before = GUILayout.Controls.Find(c => c.Button && c.Text == "분기 2").WindowLocalRect;
                DrawPointer(renderer, down, rawDown, samePoint); DrawPointer(renderer, up, rawUp, samePoint);
                Repaint(renderer);
                Check(model.Tab("조건").SelectedBranchId == "2" && LogBridge.Reservations == attempts + 2
                    && GUILayout.Controls.Find(c => c.Button && c.Text == "분기 2").WindowLocalRect == before,
                    "switching selection between detail branches preserves the exact touched header geometry, kind " + kind);
                DrawPointer(renderer, down, rawDown, samePoint); DrawPointer(renderer, up, rawUp, samePoint);
                Check(LogBridge.Reservations == attempts + 3 && LogBridge.IsReserved("조건", second),
                    "the immediate next touch at identical coordinates reserves the newly selected branch once, kind " + kind);
                LogBridge.SetReservation("조건", null);
            }
        }
        finally { Input.touchCount = oldTouch; GUILayout.ResetPointerCapture(); }
    }

    private static void ConditionDetailPointerInvalidation()
    {
        foreach (string invalidation in new[] { "branch", "parent", "publication", "slot", "pad", "context" })
        {
            int attempts = LogBridge.Reservations; object checkpoint = new object(), first = new object(), second = new object();
            var model = new LogModel(); var parent = Checkpoint("press", checkpoint, first, second);
            var renderer = ConditionRenderer(model, parent, Checkpoint("other-press", new object(), new object()));
            renderer.Handle(LogPadCommand.Right); GUILayout.ResetPointerCapture();
            Vector2 point = PointerTarget(renderer, c => c.Button && c.Text == "분기 1");
            DrawPointer(renderer, EventType.TouchDown, EventType.TouchDown, point);
            ReservationTicket ticket = (ReservationTicket)Field(renderer, "pointerReservation");
            Check(ticket != null && model.Tab("조건").CanActivate(ticket), "selected branch touch captures one original target ticket before " + invalidation);
            if (invalidation == "branch") Publish(model, "조건", Checkpoint("press", checkpoint, new object(), second));
            else if (invalidation == "parent") { renderer.Handle(LogPadCommand.Left); renderer.Handle(LogPadCommand.Down); }
            else if (invalidation == "publication") model.MarkPublicationFailure("조건");
            else if (invalidation == "slot") LogBridge.SetReservation("조건", second);
            else if (invalidation == "pad") renderer.Handle(LogPadCommand.Down);
            else { model.ResetContext(812); Publish(model, "조건", Checkpoint("press", checkpoint, first, second)); }
            DrawPointer(renderer, EventType.TouchUp, EventType.TouchUp, point);
            Check(!model.Tab("조건").CanActivate(ticket) && !model.Tab("조건").ExecutionFocus,
                "changed condition context consumes original pointer ticket: " + invalidation);
            Check(LogBridge.Reservations == attempts + (invalidation == "slot" ? 1 : 0),
                "stale branch release cannot create a new reservation request: " + invalidation);
            if (invalidation == "slot") Check(LogBridge.IsReserved("조건", second), "captured empty slot cannot cancel or overwrite a later reservation");
            DrawPointer(renderer, EventType.TouchUp, EventType.TouchUp, point);
            Check(LogBridge.Reservations == attempts + (invalidation == "slot" ? 1 : 0), "duplicate stale release cannot retry: " + invalidation);
            LogBridge.SetReservation("조건", null);
        }
    }

    private static void ConditionDetailMeasuredScroll()
    {
        Rect saved = ConfiguredRect(); int font = ConfigManager.LogFontSize.Value;
        try
        {
            ConfigureLayout(new Rect(20, 20, 650, 600), 28);
            object checkpoint = new object(), one = new object(), two = new object();
            LogItem row = Checkpoint("long", checkpoint, one, two);
            row.ConditionDetails = new LogConditionDetails("위에서부터 검사", new[] {
                new LogConditionBranch("분기 1", "조건", new string('긴', 1000), "1", one),
                new LogConditionBranch("분기 2", "앞 분기 불충족", "RAW_" + new string('A', 300), "2", two)
            });
            var model = new LogModel(); var renderer = ConditionRenderer(model, row);
            renderer.Handle(LogPadCommand.Right); Repaint(renderer); Repaint(renderer);
            renderer.Handle(LogPadCommand.Down); Repaint(renderer); Repaint(renderer);
            var pane = GUILayout.ScrollViews[1]; var branch = pane.Controls.Find(c => c.Button && c.Text == "분기 2");
            Check(branch != null && branch.Rect.y >= model.Tab("조건").DetailScroll - .01f && branch.Rect.yMax <= model.Tab("조건").DetailScroll + pane.Viewport.height + .01f,
                "pad branch navigation brings the exact branch heading into its detail viewport even after a long preceding condition");
            float offset = model.Tab("조건").DetailScroll;
            renderer.Scroll(-1); float manual = model.Tab("조건").DetailScroll; Repaint(renderer); Repaint(renderer);
            Check(manual < offset && model.Tab("조건").DetailScroll == manual, "manual detail reading is not repeatedly pulled back to the selected branch");
            Publish(model, "조건", row); Repaint(renderer);
            Check(model.Tab("조건").DetailScroll == manual && model.Tab("조건").SelectedBranchId == "2", "healthy refresh preserves the manual reading offset and branch selection");
            Check(pane.Controls.Find(c => c.Text == row.ConditionDetails.Branches[1].Description).Rect.height > 28 * 3,
                "long unbroken condition identifiers wrap inside their original branch block");
        }
        finally { ConfigureLayout(saved, font); }
    }
}

internal static partial class Program
{
    private static void ConditionDetailStableTouchGeometry()
    {
        Rect saved = ConfiguredRect(); int font = ConfigManager.LogFontSize.Value;
        try
        {
            foreach (int width in new[] { 540, 872 })
            {
                ConfigureLayout(new Rect(20, 20, width, 900), 32);
                object first = new object(), second = new object();
                string firstTitle = "첫 번째 긴 분기 이름 " + new string('제', 38);
                string secondTitle = "두 번째 긴 분기 이름 " + new string('이', 38);
                LogItem parent = Checkpoint("geometry", new object(), first, second);
                parent.ConditionDetails = new LogConditionDetails("", new[] {
                    new LogConditionBranch(firstTitle, "첫 조건", "원래 조건 설명", "1", first),
                    new LogConditionBranch(secondTitle, "나머지 조건", "원래 결과 설명", "2", second)
                });
                var model = new LogModel(); var renderer = ConditionRenderer(model, parent);
                renderer.Handle(LogPadCommand.Right); Repaint(renderer); Repaint(renderer);
                LogBridge.SetReservation("조건", first);
                model.Tab("조건").DetailScroll = GUILayout.ScrollViews[1].Controls.Find(c => c.Button && c.Text == secondTitle).Rect.y - 10;
                Repaint(renderer);
                var heading = GUILayout.Controls.Find(c => c.Button && c.Text == secondTitle);
                Check(heading.VisibleRect.height > 0 && heading.Rect.height > 32 * 2,
                    "long branch heading wraps while remaining touchable in a narrow detail pane " + width);
                Vector2 point = new Vector2(heading.WindowLocalRect.x + 5, heading.WindowLocalRect.y + 5);
                Rect before = heading.WindowLocalRect;
                int attempts = LogBridge.Reservations;
                for (int tap = 0; tap < 3; tap++)
                {
                    DrawPointer(renderer, EventType.TouchDown, EventType.TouchDown, point);
                    DrawPointer(renderer, EventType.TouchUp, EventType.TouchUp, point);
                    Repaint(renderer);
                    Check(GUILayout.Controls.Find(c => c.Button && c.Text == secondTitle).WindowLocalRect == before,
                        "selection, replacement reservation and cancellation keep a wrapped branch heading at identical coordinates " + width + "/" + tap);
                    Check(LogBridge.Reservations == attempts + tap && model.Tab("조건").SelectedBranchId == "2",
                        "same-coordinate touch first selects, then reserves, then cancels exactly once " + width + "/" + tap);
                }
                Check(LogBridge.CurrentReservation("조건") == null, "final same-coordinate touch releases the replacement result " + width);
            }
        }
        finally { ConfigureLayout(saved, font); LogBridge.SetReservation("조건", null); GUILayout.ResetPointerCapture(); }
    }
}

internal static partial class Program
{
    private static void ConditionDetailEntryViewportGeometry()
    {
        Rect saved = ConfiguredRect(); int font = ConfigManager.LogFontSize.Value;
        int oldWidth = Screen.width, oldHeight = Screen.height;
        try
        {
            Screen.width = 1920; Screen.height = 1200;
            foreach (int height in new[] { 900, 440 })
            {
                ConfigureLayout(new Rect(20, 20, 728, height), 32);
                object first = new object(), second = new object();
                var model = new LogModel();
                var renderer = ConditionRenderer(model, Checkpoint("footer-geometry", new object(), first, second));
                Repaint(renderer); Repaint(renderer);
                Check(Area(renderer) == "List" && model.Tab("조건").SelectedBranch == null,
                    "footer geometry regression starts with checkpoint-list focus and no selected result " + height);
                var pane = GUILayout.ScrollViews[1];
                var heading = pane.Controls.Find(c => c.Button && c.Text == "분기 2");
                model.Tab("조건").DetailScroll = height == 900 ? Math.Max(0, pane.ContentHeight - pane.Viewport.height) : Math.Max(0, heading.Rect.y - 5);
                Repaint(renderer);
                pane = GUILayout.ScrollViews[1];
                heading = pane.Controls.Find(c => c.Button && c.Text == "분기 2");
                Check(heading.VisibleRect.height > 0,
                    "the detail result is touchable after bottom/manual scrolling before entering details " + height);
                Rect before = heading.WindowLocalRect, viewport = pane.Viewport;
                float scroll = model.Tab("조건").DetailScroll;
                LogLayout beforeLayout = (LogLayout)Field(renderer, "layout");
                GUIStyle statusStyle = (GUIStyle)Field(renderer, "statusStyle");
                MethodInfo hint = typeof(LogRenderer).GetMethod("Hint", BindingFlags.Instance | BindingFlags.NonPublic);
                float naturalBefore = statusStyle.CalcHeight(new GUIContent((string)hint.Invoke(renderer, null)), beforeLayout.Footer.width);
                Vector2 point = new Vector2(heading.WindowLocalRect.x + 5, Math.Max(heading.WindowLocalRect.y, heading.VisibleRect.y) + 5);
                int attempts = LogBridge.Reservations;
                DrawPointer(renderer, EventType.TouchDown, EventType.TouchDown, point);
                DrawPointer(renderer, EventType.TouchUp, EventType.TouchUp, point);
                Repaint(renderer);
                LogLayout afterLayout = (LogLayout)Field(renderer, "layout");
                float naturalAfter = statusStyle.CalcHeight(new GUIContent((string)hint.Invoke(renderer, null)), afterLayout.Footer.width);
                Check(height != 900 || naturalBefore != naturalAfter,
                    "negative control: active list/detail hint text naturally measures to different heights at this viewport");
                pane = GUILayout.ScrollViews[1];
                Check(Area(renderer) == "ConditionDetails" && model.Tab("조건").SelectedBranchId == "2" && LogBridge.Reservations == attempts,
                    "first touch enters the exact branch without reservation " + height);
                Check(beforeLayout.Footer.height == afterLayout.Footer.height && beforeLayout.Content.height == afterLayout.Content.height
                    && pane.Viewport == viewport && model.Tab("조건").DetailScroll == scroll,
                    "list-to-detail touch keeps footer height, content/scroll viewport and reading offset unchanged " + height);
                Check(pane.Controls.Find(c => c.Button && c.Text == "분기 2").WindowLocalRect == before,
                    "list-to-detail touch preserves the original branch heading coordinates including compact inline guidance " + height);
                DrawPointer(renderer, EventType.TouchDown, EventType.TouchDown, point);
                DrawPointer(renderer, EventType.TouchUp, EventType.TouchUp, point);
                Check(LogBridge.Reservations == attempts + 1 && LogBridge.IsReserved("조건", second),
                    "the immediate second touch at the same coordinates reserves exactly once after entering details " + height);
                LogBridge.SetReservation("조건", null); GUILayout.ResetPointerCapture();
            }
        }
        finally { ConfigureLayout(saved, font); Screen.width = oldWidth; Screen.height = oldHeight;
            LogBridge.SetReservation("조건", null); GUILayout.ResetPointerCapture(); }
    }
}
