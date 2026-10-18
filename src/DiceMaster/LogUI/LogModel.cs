using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DiceMaster.LogUI
{
    public enum LogReadState { Unknown, Unread, Read }
    public enum LogRefreshState { Healthy, Waiting, ReadFailure, RetainedFailure }

    // A publication carries identities and original structured states.
    // The view never recovers them from localized titles, colors or row positions.
    public sealed class LogItem
    {
        public string Id = "", RawId = "", Title = "", Text = "", Description = "";
        public LogReadState ReadState;
        public readonly List<string> AnalysisStates = new List<string>();
        public string RefreshStatus = "";
        public LogRefreshState RefreshState;
        public bool HasLastGood;
        public object Target;
        public bool ReservationAllowed = true;
        public bool CanReserve { get { return Target != null && ReservationAllowed && RefreshState == LogRefreshState.Healthy; } }
        private string searchText;
        private static readonly Regex richTags = new Regex(@"</?(?:color|size|b|i|material|quad)(?:[=\s][^>]*)?>", RegexOptions.IgnoreCase);
        internal string SearchText
        {
            get
            {
                if (searchText == null) searchText = RawId + "\n" + richTags.Replace((Title ?? "") + "\n" + (Text ?? "") + "\n" + (Description ?? ""), "");
                return searchText;
            }
        }
    }

    public sealed class LogSnapshot
    {
        public readonly long ContextId;
        public readonly string Tab;
        public readonly IList<LogItem> Rows;
        public LogSnapshot(long contextId, string tab, IList<LogItem> rows)
        {
            ContextId = contextId; Tab = tab ?? "";
            Rows = new List<LogItem>(rows ?? new List<LogItem>()).AsReadOnly();
        }
    }

    public sealed class ReservationTicket
    {
        public readonly long ContextId;
        public readonly string Tab, Id;
        public readonly object Target;
        internal readonly long SelectionVersion;
        internal ReservationTicket(long contextId, string tab, string id, object target, long version)
        { ContextId = contextId; Tab = tab; Id = id; Target = target; SelectionVersion = version; }
    }

    // Runtime-only navigation. No game API, config or save access belongs here.
    public sealed class LogTabState
    {
        public LogSnapshot Snapshot { get; private set; }
        public bool HasPublished { get; private set; }
        public bool PublicationFailed { get; private set; }
        public string Search { get; private set; } = "";
        public string SelectedId { get; private set; }
        public readonly HashSet<string> Expanded = new HashSet<string>(StringComparer.Ordinal);
        public float ListScroll, DetailScroll;
        public bool ExecutionFocus;
        private long selectionVersion;

        public LogTabState(long contextId, string tab)
        { Snapshot = new LogSnapshot(contextId, tab, null); }

        public LogItem Selected
        {
            get
            {
                foreach (LogItem row in Snapshot.Rows)
                    if (row.Id == SelectedId && Matches(row)) return row;
                return null;
            }
        }

        public IList<LogItem> VisibleRows()
        {
            var result = new List<LogItem>();
            foreach (LogItem row in Snapshot.Rows) if (Matches(row)) result.Add(row);
            return result;
        }

        public int HiddenErrorCount
        {
            get
            {
                int count = 0;
                foreach (LogItem row in Snapshot.Rows)
                    if (row.RefreshState != LogRefreshState.Healthy && !Matches(row)) count++;
                return count;
            }
        }

        private bool Matches(LogItem row)
        {
            return Search.Length == 0 || Contains(row.SearchText, Search);
        }

        private static bool Contains(string text, string query)
        { return text != null && text.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0; }

        public void SetSearch(string search)
        {
            search = search ?? "";
            if (Search == search) return;
            Search = search;
            CancelExecution();
            if (Selected == null) ClearSelection();
        }

        public bool Select(string id)
        {
            foreach (LogItem row in Snapshot.Rows)
                if (row.Id == id && Matches(row))
                {
                    if (SelectedId != id) { CancelExecution(); DetailScroll = 0; }
                    SelectedId = id;
                    return true;
                }
            ClearSelection();
            return false;
        }

        public void ClearSelection()
        { SelectedId = null; DetailScroll = 0; CancelExecution(); }

        public void CancelExecution()
        { ExecutionFocus = false; selectionVersion++; }

        public bool ToggleExpanded(string id)
        {
            foreach (LogItem row in Snapshot.Rows)
                if (row.Id == id && Matches(row))
                { if (!Expanded.Remove(id)) Expanded.Add(id); return Expanded.Contains(id); }
            return false;
        }

        internal void Publish(LogSnapshot snapshot)
        {
            LogItem previous = Selected;
            Snapshot = snapshot;
            HasPublished = true;
            PublicationFailed = false;
            Expanded.RemoveWhere(id => !HasId(id));
            LogItem next = Selected;
            if (SelectedId != null && (next == null || previous != null && !ReferenceEquals(previous.Target, next.Target)))
                ClearSelection();
            else if (next != null && !next.CanReserve) CancelExecution();
        }

        internal void Reset(long contextId)
        {
            Snapshot = new LogSnapshot(contextId, Snapshot.Tab, null);
            HasPublished = false; PublicationFailed = false; Search = "";
            ClearSelection(); Expanded.Clear(); ListScroll = DetailScroll = 0;
        }

        private bool HasId(string id)
        { foreach (LogItem row in Snapshot.Rows) if (row.Id == id) return true; return false; }

        public ReservationTicket BeginReservation()
        {
            LogItem row = Selected;
            if (PublicationFailed || !ExecutionFocus || row == null || !row.CanReserve) return null;
            return new ReservationTicket(Snapshot.ContextId, Snapshot.Tab, row.Id, row.Target, selectionVersion);
        }

        public bool CanActivate(ReservationTicket ticket)
        {
            LogItem row = Selected;
            return !PublicationFailed && ticket != null && ExecutionFocus && ticket.ContextId == Snapshot.ContextId && ticket.Tab == Snapshot.Tab &&
                ticket.SelectionVersion == selectionVersion && row != null && row.CanReserve && row.Id == ticket.Id &&
                ReferenceEquals(row.Target, ticket.Target);
        }

        internal void MarkPublicationFailure()
        { PublicationFailed = true; CancelExecution(); }
    }

    public sealed class LogModel
    {
        public long ContextId { get; private set; }
        private readonly Dictionary<string, LogTabState> tabs = new Dictionary<string, LogTabState>(StringComparer.Ordinal);
        public LogTabState Tab(string tab)
        {
            tab = tab ?? "";
            LogTabState state;
            if (!tabs.TryGetValue(tab, out state)) { state = new LogTabState(ContextId, tab); tabs.Add(tab, state); }
            return state;
        }
        public void ResetContext(long contextId)
        {
            ContextId = contextId;
            foreach (LogTabState tab in tabs.Values) tab.Reset(contextId);
            tabs.Clear();
        }
        public void CancelExecution()
        { foreach (LogTabState tab in tabs.Values) tab.CancelExecution(); }
        public void MarkPublicationFailure(string tab)
        { Tab(tab).MarkPublicationFailure(); }
        public void Publish(LogSnapshot snapshot)
        {
            if (snapshot == null || snapshot.ContextId != ContextId) return;
            Tab(snapshot.Tab).Publish(snapshot);
        }
    }
}
