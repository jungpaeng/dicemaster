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
        events.ListScroll = 140; events.DetailScroll = 230; events.ToggleExpanded("a");
        var ticket = Arm(events);
        Check(ticket != null && events.CanActivate(ticket), "Fresh selected target could not be armed");
        Publish(model, "이벤트", Row("b", other, "바뀐 제목"), Row("a", target, "번역 변경"));
        Check(events.SelectedId == "a" && events.Selected.Title == "번역 변경", "Reorder/title must not change selected identity");
        Check(events.ListScroll == 140 && events.DetailScroll == 230 && events.Expanded.Contains("a"), "Warm publication reset navigation");
        Check(events.CanActivate(ticket), "Healthy same-target refresh wrongly invalidated ticket");
        model.MarkPublicationFailure("이벤트");
        Check(events.PublicationFailed && !events.CanActivate(ticket) && events.BeginReservation() == null, "Publication failure retained executable stale view");
        Publish(model, "이벤트", Row("b", other), Row("a", target));
        Check(!events.PublicationFailed && events.SelectedId == "a" && !events.CanActivate(ticket), "Publication recovery failed or revived old confirmation");
        Publish(model, "미션", Row("m", new object()));
        Check(model.Tab("이벤트").SelectedId == "a" && model.Tab("미션").Selected == null, "Tab state mixed");
        events.SetSearch("raw/a");
        Check(events.VisibleRows().Count == 1 && events.SelectedId == "a", "Raw identifier search lost matching selected item");
        Check(!events.CanActivate(ticket), "Search edit failed to cancel pending execution");
        events.SetSearch("긴 한국어"); Check(events.VisibleRows().Count == 2, "Korean description search failed");
        Publish(model, "조건", Row("rich", null, "<color=#fff>한국어</color> 검색"));
        model.Tab("조건").SetSearch("한국어 검색");
        Check(model.Tab("조건").VisibleRows().Count == 1, "Rich-text markup interrupted Korean phrase search");
        events.SetSearch("선택 문장"); Check(events.VisibleRows().Count == 2, "Actual option text search failed");
        events.SetSearch("RAW/A"); Check(events.VisibleRows().Count == 1, "Identifier search is not case-insensitive");
        events.SetSearch("없는 결과");
        Check(events.VisibleRows().Count == 0 && events.Selected == null && !events.ExecutionFocus, "Search disappearance retained a target");
        events.SetSearch(""); Check(events.Selected == null, "Clear search auto-selected a reappearing target");
        events.Select("a"); ticket = Arm(events);
        Publish(model, "이벤트", Row("b", other));
        Check(events.Selected == null && !events.CanActivate(ticket) && !events.Expanded.Contains("a"), "Target disappearance retained action/navigation identity");
        Publish(model, "이벤트", Row("a", target), Row("b", other));
        Check(events.Selected == null && !events.CanActivate(ticket), "Held activation attached itself to reappearing target");
        events.Select("a"); ticket = Arm(events);
        var failed = Row("a", target); failed.RefreshState = LogRefreshState.RetainedFailure; failed.HasLastGood = true;
        Publish(model, "이벤트", failed, Row("b", other));
        Check(events.SelectedId == "a" && !events.Selected.CanReserve && !events.CanActivate(ticket), "Failed last-good row permitted reservation");
        events.SetSearch("raw/b"); Check(events.HiddenErrorCount == 1 && events.VisibleRows().Count == 1, "Hidden error accounting polluted matching results");
        events.SetSearch(""); events.Select("a");
        Check(Arm(events) == null, "Failed row produced reservation ticket");
        Publish(model, "이벤트", Row("a", target));
        events.Select("a"); ticket = Arm(events);
        var stale = Row("a", target); stale.ReservationAllowed = false;
        Publish(model, "이벤트", stale);
        Check(!events.CanActivate(ticket) && events.BeginReservation() == null, "Discovery failure allowed retained target");
        Publish(model, "이벤트", Row("a", target)); events.Select("a"); ticket = Arm(events);
        Publish(model, "이벤트", Row("a", new object()));
        Check(events.Selected == null && !events.CanActivate(ticket), "Object replacement behind same ID retained execution");
        Publish(model, "선택지", Row("o1"), Row("o2"));
        var cards = model.Tab("선택지"); cards.Select("o1"); cards.ToggleExpanded("o1"); cards.ToggleExpanded("o2");
        Check(cards.Expanded.Count == 2, "Cards must permit simultaneous expansion");
        Check(Arm(cards) == null, "Non-reservable card created reservation ticket");
        cards.SetSearch("선택"); cards.ListScroll = 17;
        events.Select("a"); ticket = Arm(events);
        model.ResetContext(2);
        Check(!events.CanActivate(ticket) && events.Selected == null && !events.HasPublished, "Retained prior-tab reference survived context invalidation");
        Check(model.Tab("선택지").Search == "" && model.Tab("선택지").Selected == null && model.Tab("선택지").Expanded.Count == 0 && model.Tab("선택지").ListScroll == 0, "New context retained runtime navigation");
        model.Publish(new LogSnapshot(1, "이벤트", new[] { Row("stale", target) }));
        Check(model.Tab("이벤트").Snapshot.Rows.Count == 0, "Late prior-context publication was admitted");
        Publish(model, "이벤트", Row("a", target)); events = model.Tab("이벤트"); events.Select("a"); ticket = Arm(events);
        model.CancelExecution(); Check(!events.CanActivate(ticket), "Mode/focus loss did not cancel pending ticket");
        for (int i = 0; i < 300; i++)
        {
            var rows = new List<LogItem>();
            for (int j = 0; j < 40; j++) rows.Add(Row(((j + i) % 40).ToString(), target));
            Publish(model, "이벤트", rows.ToArray());
            events.Select("17"); events.DetailScroll = 125;
            Publish(model, "이벤트", rows.ToArray());
            Check(events.SelectedId == "17" && events.DetailScroll == 125, "Stable identity stress regression");
        }
        Console.WriteLine("PASS: " + checks + " log model assertions (runtime navigation, identity, search, last-good/errors, context and activation tickets).");
        Measure();
        return 0;
    }

    static void Measure()
    {
        const int count = 1000, iterations = 100;
        var measured = new LogModel(); measured.ResetContext(1);
        var target = new object();
        string[] ids = new string[count]; for (int i = 0; i < count; i++) ids[i] = i.ToString();
        void Cycle(int iteration)
        {
            var rows = new List<LogItem>(count);
            foreach (string id in ids) rows.Add(Row(id, target));
            measured.Publish(new LogSnapshot(1, "이벤트", rows));
            var state = measured.Tab("이벤트");
            state.SetSearch(iteration % 2 == 0 ? "한국어 설명" : "긴 한국어");
            state.Select("517"); state.VisibleRows();
        }
        for (int i = 0; i < 10; i++) Cycle(i);
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++) Cycle(i);
        watch.Stop(); allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Console.WriteLine("MEASURE managed-model-only os=" + RuntimeInformation.OSDescription + " runtime=" + RuntimeInformation.FrameworkDescription +
            " rows=" + count + " iterations=" + iterations + " elapsed_ms=" + watch.Elapsed.TotalMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
            " thread_allocated_bytes=" + allocated + " (publish+selection+Korean-search; excludes Unity/native/game frame performance)");
    }
}
