using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DiceMaster.LogUI;

static class Program
{
    static int checks;
    static void Check(bool condition, string message)
    { checks++; if (!condition) throw new InvalidOperationException(message); }
    static LogItem Row(string id, object target = null, string title = "같은 제목") => new LogItem {
        Id = id, RawId = "raw/" + id, Title = title, Text = "선택 문장", Description = "긴 한국어 설명", Target = target
    };
    static void Publish(LogModel model, string tab, params LogItem[] rows) => model.Publish(new LogSnapshot(model.ContextId, tab, rows));
    static ReservationTicket Arm(LogTabState state)
    { state.ExecutionFocus = true; return state.BeginReservation(); }

    static int Main()
    {
        var model = new LogModel(); model.ResetContext(1);
        Check(!model.Tab("이벤트").HasPublished, "Unpublished state was confused with a successful empty list");
        Publish(model, "이벤트");
        Check(model.Tab("이벤트").HasPublished && model.Tab("이벤트").Snapshot.Rows.Count == 0, "Successful empty publication remained waiting");
        var target = new object(); var other = new object();
        Publish(model, "이벤트", Row("a", target), Row("b", other));
        var events = model.Tab("이벤트");
        Check(events.Selected == null, "Publication must not auto-select a reservation target");
        Check(events.Select("a"), "Explicit target selection failed");
        events.ListScroll = 140; events.DetailScroll = 230;
        var ticket = Arm(events);
        Check(ticket != null && events.CanActivate(ticket), "Fresh selected target could not be armed");
        Check(ticket.ReservationState == null, "Legacy arming manufactured game reservation state");
        object reservationState = new object();
        ticket = events.BeginReservation(reservationState);
        Check(ReferenceEquals(ticket.ReservationState, reservationState) && events.CanActivate(ticket),
            "Action ticket did not carry the original opaque reservation state");
        Publish(model, "이벤트", Row("b", other, "바뀐 제목"), Row("a", target, "번역 변경"));
        Check(events.SelectedId == "a" && events.Selected.Title == "번역 변경", "Reorder/title must not change selected identity");
        Check(events.ListScroll == 140 && events.DetailScroll == 230, "Warm publication reset list/detail scroll");
        Check(events.CanActivate(ticket), "Healthy same-target refresh wrongly invalidated ticket");
        Check(ReferenceEquals(ticket.ReservationState, reservationState), "Healthy publication replaced the original game-boundary snapshot");
        model.MarkPublicationFailure("이벤트");
        Check(events.PublicationFailed && !events.CanActivate(ticket) && events.BeginReservation() == null, "Publication failure retained executable stale view");
        Publish(model, "이벤트", Row("b", other), Row("a", target));
        Check(!events.PublicationFailed && events.SelectedId == "a" && !events.CanActivate(ticket), "Publication recovery failed or revived old confirmation");
        Publish(model, "미션", Row("m", new object()));
        Check(model.Tab("이벤트").SelectedId == "a" && model.Tab("미션").Selected == null, "Tab state mixed");
        Check(events.Snapshot.Rows.Count == 2 && events.Snapshot.Rows[0].Id == "b" && events.Snapshot.Rows[1].Id == "a",
            "Publication omitted or reordered an unselected row");
        events.Select("a"); ticket = Arm(events); events.Select("b");
        Check(!events.CanActivate(ticket) && events.SelectedId == "b" && events.DetailScroll == 0 && events.ListScroll == 140,
            "Changing selection retained prior execution authority or reset list scroll");
        var rich = Row("rich", null, "<color=#fff>한국어</color> 제목");
        rich.Description = "<b>전체 설명</b>을 그대로 유지";
        Publish(model, "조건", rich);
        Check(model.Tab("조건").Snapshot.Rows.Count == 1 && model.Tab("조건").Snapshot.Rows[0].Title == rich.Title &&
            model.Tab("조건").Snapshot.Rows[0].Description == rich.Description, "Publication changed rich-text title or full description");
        events.Select("a"); ticket = Arm(events);
        Publish(model, "이벤트", Row("b", other));
        Check(events.Selected == null && !events.CanActivate(ticket) && events.DetailScroll == 0, "Target disappearance retained action/detail navigation identity");
        Publish(model, "이벤트", Row("a", target), Row("b", other));
        Check(events.Selected == null && !events.CanActivate(ticket), "Held activation attached itself to reappearing target");
        events.Select("a"); ticket = Arm(events);
        var failed = Row("a", target); failed.RefreshState = LogRefreshState.RetainedFailure; failed.HasLastGood = true;
        Publish(model, "이벤트", failed, Row("b", other));
        Check(events.SelectedId == "a" && !events.Selected.CanReserve && !events.CanActivate(ticket), "Failed last-good row permitted reservation");
        var waiting = Row("waiting"); waiting.RefreshState = LogRefreshState.Waiting; waiting.RefreshStatus = "게임 데이터 대기 중";
        var unreadable = Row("unreadable"); unreadable.RefreshState = LogRefreshState.ReadFailure; unreadable.RefreshStatus = "읽기 실패";
        var staged = new List<LogItem> { failed, waiting, unreadable, Row("b", other) };
        var fullSnapshot = new LogSnapshot(model.ContextId, "이벤트", staged); staged.Clear(); model.Publish(fullSnapshot);
        Check(events.Snapshot.Rows.Count == 4 && ReferenceEquals(events.Snapshot.Rows[0], failed) &&
            ReferenceEquals(events.Snapshot.Rows[1], waiting) && ReferenceEquals(events.Snapshot.Rows[2], unreadable) &&
            events.Snapshot.Rows[3].Id == "b", "Full publication lost an error/waiting row or shared the caller's mutable list");
        Check(events.Snapshot.Rows[0].HasLastGood && events.Snapshot.Rows[1].RefreshState == LogRefreshState.Waiting &&
            events.Snapshot.Rows[2].RefreshState == LogRefreshState.ReadFailure, "Full publication conflated independent refresh states");
        Check(events.Select("waiting") && Arm(events) == null && events.Select("unreadable") && Arm(events) == null,
            "Waiting/read-failure descriptions were inaccessible or produced a reservation ticket");
        events.Select("a");
        Check(Arm(events) == null, "Failed row produced reservation ticket");
        Publish(model, "이벤트", Row("a", target));
        events.Select("a"); ticket = Arm(events);
        var stale = Row("a", target); stale.ReservationAllowed = false;
        Publish(model, "이벤트", stale);
        Check(!events.CanActivate(ticket) && events.BeginReservation() == null, "Discovery failure allowed retained target");
        Publish(model, "이벤트", Row("a", target)); events.Select("a"); ticket = Arm(events);
        Publish(model, "이벤트", Row("a", new object()));
        Check(events.Selected == null && !events.CanActivate(ticket), "Object replacement behind same ID retained execution");
        var firstOption = Row("o1"); firstOption.Description = "조건에 따른 가능한 변화 전체";
        firstOption.ReadState = LogReadState.Unread; firstOption.AnalysisStates.Add("Partial");
        var secondOption = Row("o2"); secondOption.Description = "다른 선택지의 전체 효과";
        secondOption.ReadState = LogReadState.Read; secondOption.AnalysisStates.Add("Supported");
        Publish(model, "선택지", firstOption, secondOption);
        var options = model.Tab("선택지"); options.Select("o1");
        options.ListScroll = 91; options.DetailScroll = 53;
        Publish(model, "선택지", secondOption, firstOption);
        Check(options.Snapshot.Rows.Count == 2 && options.SelectedId == "o1" &&
            options.ListScroll == 91 && options.DetailScroll == 53, "Option refresh/reorder lost selection or scroll");
        Check(options.Snapshot.Rows[0].Description == secondOption.Description && options.Snapshot.Rows[1].Description == firstOption.Description,
            "Unselected option full description disappeared from the published comparison");
        Check(options.Selected.ReadState == LogReadState.Unread && options.Selected.AnalysisStates.Contains("Partial") &&
            options.Snapshot.Rows[0].ReadState == LogReadState.Read && options.Snapshot.Rows[0].AnalysisStates.Contains("Supported"),
            "Option comparison lost explicit read/analysis states");
        Check(Arm(options) == null, "Read-only option created reservation ticket");

        var firstCondition = Row("c1", null, "가문 체크포인트");
        firstCondition.Text = "1 2 그 외"; firstCondition.Description = "돈 >= 10\n평판 >= 5\n그 외 분기";
        firstCondition.ReadState = LogReadState.Unread;
        var secondCondition = Row("c2", null, "수련 체크포인트");
        secondCondition.Description = "체력 >= 20\n기력 >= 15"; secondCondition.ReadState = LogReadState.Read;
        Publish(model, "조건", firstCondition, secondCondition);
        var conditions = model.Tab("조건");
        Check(conditions.Selected == null, "Condition publication auto-selected a detail target");
        Check(conditions.Select("c1"), "Explicit checkpoint detail selection failed");
        conditions.ListScroll = 87; conditions.DetailScroll = 165;
        Publish(model, "조건", secondCondition, firstCondition);
        Check(conditions.SelectedId == "c1" && conditions.ListScroll == 87 && conditions.DetailScroll == 165,
            "Condition refresh/reorder reset independent list/detail navigation");
        Check(conditions.Selected.Description == "돈 >= 10\n평판 >= 5\n그 외 분기" && conditions.Selected.Text == "1 2 그 외" &&
            conditions.Selected.ReadState == LogReadState.Unread, "Condition detail lost full clauses, branch labels or read state");
        Check(Arm(conditions) == null, "Read-only checkpoint detail produced reservation ticket");
        Check(conditions.Select("c2") && conditions.Selected.ReadState == LogReadState.Read && conditions.DetailScroll == 0 &&
            conditions.ListScroll == 87 && !conditions.ExecutionFocus && conditions.Snapshot.Rows.Count == 2,
            "Checkpoint selection lost full rows/read state or retained previous detail scroll/authority");
        conditions.ClearSelection(); Publish(model, "조건", firstCondition, secondCondition);
        Check(conditions.Selected == null && conditions.Snapshot.Rows.Count == 2,
            "Full condition publication implicitly selected a detail after explicit selection cancellation");
        Check(!conditions.Select("missing") && conditions.Selected == null, "Checkpoint selection admitted an ID absent from the snapshot");
        conditions.Select("c2"); conditions.ListScroll = 35; conditions.DetailScroll = 46;
        Publish(model, "조건", firstCondition);
        Check(conditions.Selected == null && conditions.DetailScroll == 0 && !conditions.ExecutionFocus,
            "Removed checkpoint left stale detail or execution focus");
        options.ListScroll = 17; options.DetailScroll = 33;
        events.Select("a"); ticket = Arm(events);
        model.ResetContext(2);
        Check(!events.CanActivate(ticket) && events.Selected == null && !events.HasPublished, "Retained prior-tab reference survived context invalidation");
        Check(model.Tab("선택지").Selected == null && model.Tab("선택지").ListScroll == 0 &&
            model.Tab("선택지").DetailScroll == 0 && options.Selected == null && options.Snapshot.Rows.Count == 0,
            "New context retained option navigation or a prior option tab reference");
        Check(conditions.Selected == null && conditions.ListScroll == 0 && conditions.DetailScroll == 0 &&
            !conditions.HasPublished && conditions.Snapshot.Rows.Count == 0, "New context retained checkpoint detail navigation");
        model.Publish(new LogSnapshot(1, "이벤트", new[] { Row("stale", target) }));
        Check(model.Tab("이벤트").Snapshot.Rows.Count == 0, "Late prior-context publication was admitted");
        Publish(model, "이벤트", Row("a", target)); events = model.Tab("이벤트"); events.Select("a"); ticket = Arm(events);
        model.CancelExecution(); Check(!events.CanActivate(ticket), "Mode/focus loss did not cancel pending ticket");
        for (int i = 0; i < 300; i++)
        {
            var rows = new List<LogItem>();
            for (int j = 0; j < 40; j++) rows.Add(Row(((j + i) % 40).ToString(), target));
            Publish(model, "이벤트", rows.ToArray());
            events.Select("17"); events.ListScroll = 80; events.DetailScroll = 125;
            Publish(model, "이벤트", rows.ToArray());
            Check(events.Snapshot.Rows.Count == 40 && events.SelectedId == "17" && events.ListScroll == 80 && events.DetailScroll == 125,
                "Full publication, stable identity or list/detail scroll stress regression");
        }
        StatusOnlyRows();
        StructuredConditionDetails();
        Console.WriteLine("PASS: " + checks + " log model assertions (full publications, status-only rows, navigation, identity, last-good/errors, context and activation tickets).");
        Measure();
        return 0;
    }

    static void StatusOnlyRows()
    {
        var model = new LogModel(); model.ResetContext(30);
        var target = new object();
        var first = Row("first", target, "실제 미션");
        var waitingMission = Row("waiting-mission", new object(), "이전 설명이 있는 미션");
        waitingMission.RefreshState = LogRefreshState.Waiting; waitingMission.HasLastGood = true;
        var status1 = Row("status-1", null, "미션 정보"); status1.IsStatusOnly = true;
        status1.RefreshState = LogRefreshState.Waiting; status1.RefreshStatus = "게임 데이터 대기 중";
        var status2 = Row("status-2", new object(), "다른 상태 안내"); status2.IsStatusOnly = true;
        var status3 = Row("status-3", null, "조회 실패 안내"); status3.IsStatusOnly = true;
        status3.RefreshState = LogRefreshState.ReadFailure;
        Publish(model, "미션", status1, first, status2, waitingMission, status3);
        var state = model.Tab("미션");
        Check(state.Snapshot.Rows.Count == 5 && ReferenceEquals(state.Snapshot.Rows[0], first) &&
            ReferenceEquals(state.Snapshot.Rows[1], waitingMission), "Status-only rows were not placed after normal mission rows");
        Check(ReferenceEquals(state.Snapshot.Rows[2], status1) && ReferenceEquals(state.Snapshot.Rows[3], status2) &&
            ReferenceEquals(state.Snapshot.Rows[4], status3), "Status-only grouping changed diagnostic order or dropped a row");
        Check(status1.RefreshState == LogRefreshState.Waiting && status1.RefreshStatus == "게임 데이터 대기 중" &&
            status3.RefreshState == LogRefreshState.ReadFailure, "Status-only grouping rewrote diagnostic states");
        Check(!status1.CanSelect && !status2.CanSelect && !status2.CanReserve && !status3.CanReserve,
            "Status-only row remained selectable/reservable, including with an accidental healthy target");
        Check(!state.Select(status1.Id) && state.Selected == null && state.SelectedId == null,
            "Status-only mission was selected before any normal target");
        Check(state.Select(first.Id), "Normal mission became inaccessible beside status-only rows");
        state.ListScroll = 77; state.DetailScroll = 88;
        var ticket = Arm(state);
        Check(ticket != null && state.CanActivate(ticket), "Normal mission could not be armed beside diagnostic rows");
        Check(!state.Select(status1.Id) && !state.Select(status2.Id) && state.SelectedId == first.Id &&
            state.ListScroll == 77 && state.DetailScroll == 88 && state.ExecutionFocus && state.CanActivate(ticket),
            "Attempting to select a disabled diagnostic polluted the current target, scroll or ticket");
        Publish(model, "미션", status3, waitingMission, status2, first, status1);
        Check(state.Snapshot.Rows[0].Id == waitingMission.Id && state.Snapshot.Rows[1].Id == first.Id &&
            state.Snapshot.Rows[2].Id == status3.Id && state.Snapshot.Rows[3].Id == status2.Id && state.Snapshot.Rows[4].Id == status1.Id,
            "Refresh failed to preserve each group's new source order");
        Check(state.SelectedId == first.Id && state.ListScroll == 77 && state.DetailScroll == 88 && state.CanActivate(ticket),
            "Status row reordering invalidated a healthy selected mission");
        Check(!state.Select("absent") && state.Selected == null && state.DetailScroll == 0 && !state.CanActivate(ticket),
            "Missing ID no longer cancels prior selection/authority");
        Check(waitingMission.CanSelect && !waitingMission.CanReserve && state.Select(waitingMission.Id),
            "A real mission with retained waiting details was confused with a status-only placeholder");
        var readable = Row("readable", null, "미션 정보"); readable.Description = "설명 조회 대기";
        readable.RefreshState = LogRefreshState.ReadFailure;
        Publish(model, "조건", readable);
        Check(readable.CanSelect && !readable.CanReserve && model.Tab("조건").Select(readable.Id),
            "Target-null/read-failure detail or matching display text was misclassified as status-only");
        Publish(model, "선택지", Row("choice"));
        Check(model.Tab("선택지").Select("choice") && model.Tab("선택지").Selected.CanSelect &&
            !model.Tab("선택지").Selected.CanReserve, "Read-only choices became inaccessible");

        state.Select(first.Id); state.DetailScroll = 180; ticket = Arm(state);
        var becameStatus = Row(first.Id, target, "미션 정보"); becameStatus.IsStatusOnly = true;
        Publish(model, "미션", becameStatus, waitingMission);
        Check(state.Selected == null && state.SelectedId == null && state.DetailScroll == 0 && !state.ExecutionFocus &&
            !state.CanActivate(ticket), "Selected mission becoming status-only retained detail or execution authority");
        Check(state.Snapshot.Rows.Count == 2 && state.Snapshot.Rows[1].Id == first.Id,
            "Converted placeholder was lost instead of moved to the diagnostic group");
        Publish(model, "미션", status1, Row(first.Id, target));
        Check(state.Selected == null && !state.CanActivate(ticket), "Recovery auto-selected a former placeholder or revived its held input");
        Check(state.Select(first.Id) && Arm(state) != null, "Recovered normal mission could not be explicitly selected and armed");
        ticket = state.BeginReservation();
        Publish(model, "미션", status3, status1, status2);
        Check(state.Snapshot.Rows.Count == 3 && state.Snapshot.Rows[0].Id == status3.Id &&
            state.Snapshot.Rows[1].Id == status1.Id && state.Snapshot.Rows[2].Id == status2.Id,
            "An all-diagnostic publication was hidden or reordered");
        Check(state.HasPublished && state.Selected == null && !state.Select(status1.Id) && !state.CanActivate(ticket),
            "All-diagnostic publication auto-selected a target or preserved prior activation");
    }

    static void StructuredConditionDetails()
    {
        var inputs = new List<LogConditionBranch> {
            new LogConditionBranch("분기 1", "이 조건을 만족하면", "돈 >= 10 | 평판 >= 5\n같은 분기의 추가 줄"),
            new LogConditionBranch("분기 2", "앞 분기가 모두 불충족일 때 검사", "체력 >= 20"),
            new LogConditionBranch("그 외 (분기 3)", "위 분기가 모두 불충족일 때", "")
        };
        const string hint = "위에서부터 검사하며, 처음 만족한 분기로 진행합니다.";
        var details = new LogConditionDetails(hint, inputs, "호환용 평문");
        inputs.Clear();
        Check(details.Branches.Count == 3 && details.Branches[0].Title == "분기 1" && details.Branches[1].Title == "분기 2"
            && details.Branches[2].Title == "그 외 (분기 3)", "Published condition branches shared a mutable input list or lost source order");
        Check(details.Branches[0].Description == "돈 >= 10 | 평판 >= 5\n같은 분기의 추가 줄" && details.Hint == hint
            && details.Branches[2].Context == "위 분기가 모두 불충족일 때" && details.Branches[2].Description == "",
            "Condition publication changed clause operators, multiline branch boundaries, or fallback meaning");
        bool mutationBlocked = false;
        try { details.Branches.Add(new LogConditionBranch("임의 분기", "", "")); }
        catch (NotSupportedException) { mutationBlocked = true; }
        Check(mutationBlocked && details.Branches.IsReadOnly && details.Branches.Count == 3,
            "Condition details allowed later mutation of an existing publication or last-good snapshot");
        var model = new LogModel(); model.ResetContext(40);
        LogItem row = Row("checkpoint"); row.ConditionDetails = details;
        row.ReadState = LogReadState.Unread; row.Text = "1 2 그 외";
        Publish(model, "조건", row);
        var state = model.Tab("조건");
        Check(state.Selected == null && state.Select(row.Id) && ReferenceEquals(state.Selected.ConditionDetails, details),
            "Structured branch publication auto-selected a checkpoint or disconnected selected details");
        state.ListScroll = 71; state.DetailScroll = 172;
        var updated = Row(row.Id); updated.ConditionDetails = new LogConditionDetails("새 검사 안내", new[] {
            new LogConditionBranch("분기 1", "새 조건 맥락", "갱신된 본문 | 기존 연산자")
        });
        Publish(model, "조건", updated);
        Check(state.SelectedId == row.Id && state.ListScroll == 71 && state.DetailScroll == 172
            && ReferenceEquals(state.Selected.ConditionDetails, updated.ConditionDetails),
            "Same-context branch refresh lost selected checkpoint/scroll or retained stale detail data");
        Check(details.Branches.Count == 3 && details.Branches[0].Description.Contains("|"),
            "New branch publication mutated the earlier last-good description");
        Check(Arm(state) == null && state.Selected.CanSelect && !state.Selected.CanReserve,
            "Read-only structured condition publication produced reservation authority");
        var retained = Row(row.Id); retained.ConditionDetails = details;
        retained.RefreshState = LogRefreshState.RetainedFailure; retained.HasLastGood = true;
        Publish(model, "조건", retained);
        Check(state.SelectedId == row.Id && state.Selected.ConditionDetails.Branches.Count == 3
            && state.Selected.RefreshState == LogRefreshState.RetainedFailure && Arm(state) == null,
            "Condition last-good failure erased structured branches or made them actionable");
        var countOnly = new LogConditionDetails("", Array.Empty<LogConditionBranch>(), "실행 횟수 기준 분기");
        Check(countOnly.Branches.Count == 0 && countOnly.DescriptionFallback == "실행 횟수 기준 분기"
            && countOnly.Hint == "", "Count-based checkpoint invented normal branch structure or lost neutral guidance");
        Publish(model, "조건");
        Check(state.Selected == null && state.DetailScroll == 0 && !state.ExecutionFocus,
            "Removed structured checkpoint retained stale detail focus");
        Publish(model, "조건", row); state.Select(row.Id); state.ListScroll = 44; state.DetailScroll = 55;
        model.ResetContext(41);
        model.Publish(new LogSnapshot(40, "조건", new[] { row }));
        Check(model.Tab("조건").Selected == null && model.Tab("조건").Snapshot.Rows.Count == 0 && state.Snapshot.Rows.Count == 0
            && state.ListScroll == 0 && state.DetailScroll == 0,
            "Context replacement or late structured publication restored the old checkpoint and branches");
    }

    static void Measure()
    {
        const int count = 1000, iterations = 100;
        var measured = new LogModel(); measured.ResetContext(1);
        var target = new object();
        string[] ids = new string[count]; for (int i = 0; i < count; i++) ids[i] = i.ToString();
        long observedRows = 0;
        void Cycle()
        {
            var rows = new List<LogItem>(count);
            foreach (string id in ids) rows.Add(Row(id, target));
            measured.Publish(new LogSnapshot(1, "이벤트", rows));
            var state = measured.Tab("이벤트");
            state.Select("517"); observedRows += state.Snapshot.Rows.Count;
        }
        for (int i = 0; i < 10; i++) Cycle();
        observedRows = 0;
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++) Cycle();
        watch.Stop(); allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        if (observedRows != count * iterations) throw new InvalidOperationException("Measured publication omitted rows.");
        Console.WriteLine("MEASURE managed-model-only os=" + RuntimeInformation.OSDescription + " runtime=" + RuntimeInformation.FrameworkDescription +
            " rows=" + count + " iterations=" + iterations + " elapsed_ms=" + watch.Elapsed.TotalMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
            " thread_allocated_bytes=" + allocated + " observed_rows=" + observedRows + " (publish+selection+full-rows; excludes Unity/native/game frame performance)");
    }
}
