using System;
using System.Collections.Generic;

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
        public bool IsStatusOnly;
        public bool ReservationAllowed = true;
        public bool CanSelect { get { return !IsStatusOnly; } }
        public bool CanReserve { get { return CanSelect && Target != null && ReservationAllowed && RefreshState == LogRefreshState.Healthy; } }
    }

    public sealed class LogSnapshot
    {
        public readonly long ContextId;
        public readonly string Tab;
        public readonly IList<LogItem> Rows;
        public LogSnapshot(long contextId, string tab, IList<LogItem> rows)
        {
            ContextId = contextId; Tab = tab ?? "";
            // Keep diagnostic rows visible after all actionable/readable rows.
            // Classification is supplied by the bridge, never inferred here
            // from a translated title, missing Target or failed reservation.
            var ordered = new List<LogItem>(rows == null ? 0 : rows.Count);
            List<LogItem> statusRows = null;
            if (rows != null)
                foreach (LogItem row in rows)
                    if (row.IsStatusOnly)
                    {
                        if (statusRows == null) statusRows = new List<LogItem>();
                        statusRows.Add(row);
                    }
                    else ordered.Add(row);
            if (statusRows != null) ordered.AddRange(statusRows);
            Rows = ordered.AsReadOnly();
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
        public string SelectedId { get; private set; }
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
                    if (row.Id == SelectedId && row.CanSelect) return row;
                return null;
            }
        }

        public bool Select(string id)
        {
            foreach (LogItem row in Snapshot.Rows)
                if (row.Id == id)
                {
                    if (!row.CanSelect) return false;
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

        internal void Publish(LogSnapshot snapshot)
        {
            LogItem previous = Selected;
            Snapshot = snapshot;
            HasPublished = true;
            PublicationFailed = false;
            LogItem next = Selected;
            if (SelectedId != null && (next == null || previous != null && !ReferenceEquals(previous.Target, next.Target)))
                ClearSelection();
            else if (next != null && !next.CanReserve) CancelExecution();
        }

        internal void Reset(long contextId)
        {
            Snapshot = new LogSnapshot(contextId, Snapshot.Tab, null);
            HasPublished = false; PublicationFailed = false;
            ClearSelection(); ListScroll = DetailScroll = 0;
        }

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
