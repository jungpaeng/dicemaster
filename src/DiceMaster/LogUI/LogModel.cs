using System;
using System.Collections.Generic;
using DiceMaster.EventSelection;

namespace DiceMaster.LogUI
{
    public enum LogReadState { Unknown, Unread, Read }
    public enum LogRefreshState { Healthy, Waiting, ReadFailure, RetainedFailure }

    // A checkpoint's branches come from separate ConditionResultItem objects.
    // Preserve those boundaries before formatting; visual line wrapping and
    // localized operator text cannot establish the branch structure later.
    public sealed class LogConditionBranch
    {
        public readonly string Title, Context, Description, Id;
        public readonly object Target;
        public readonly EventEligibilitySnapshot Eligibility;
        public bool CanReserve { get { return Target != null; } }
        public LogConditionBranch(string title, string context, string description)
            : this(title, context, description, "", null) { }
        public LogConditionBranch(string title, string context, string description, string id, object target)
            : this(title, context, description, id, target, null) { }
        public LogConditionBranch(string title, string context, string description, string id, object target, EventEligibilitySnapshot eligibility)
        { Title = title ?? ""; Context = context ?? ""; Description = description ?? ""; Id = id ?? ""; Target = target; Eligibility = eligibility; }
    }

    public sealed class LogConditionDetails
    {
        public readonly string Hint, DescriptionFallback;
        public readonly IList<LogConditionBranch> Branches;
        public LogConditionDetails(string hint, IEnumerable<LogConditionBranch> branches, string descriptionFallback = "")
        {
            Hint = hint ?? "";
            DescriptionFallback = descriptionFallback ?? "";
            // The publication owns a stable, immutable list. A builder or
            // later refresh cannot mutate an already published last-good view.
            var copy = new List<LogConditionBranch>();
            if (branches != null)
                foreach (LogConditionBranch branch in branches)
                    if (branch != null) copy.Add(branch);
            Branches = copy.AsReadOnly();
        }
    }

    // A publication carries identities and original structured states.
    // The view never recovers them from localized titles, colors or row positions.
    public sealed class LogItem
    {
        public string Id = "", RawId = "", Title = "", Text = "", Description = "";
        public string SearchDisplayTitle = "";
        public LogConditionDetails ConditionDetails;
        public EventEligibilitySnapshot EventEligibility;
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
        public bool IsAvailableEvent { get { return !IsStatusOnly && RefreshState == LogRefreshState.Healthy &&
            EventEligibility != null && EventEligibility.IsEligible && EventEligibility.CanUseResult; } }
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
            List<LogItem> otherEvents = null;
            List<LogItem> statusRows = null;
            if (rows != null)
                foreach (LogItem row in rows)
                    if (row.IsStatusOnly)
                    {
                        if (statusRows == null) statusRows = new List<LogItem>();
                        statusRows.Add(row);
                    }
                    else if (Tab == "이벤트" && !row.IsAvailableEvent)
                    {
                        if (otherEvents == null) otherEvents = new List<LogItem>();
                        otherEvents.Add(row);
                    }
                    else ordered.Add(row);
            if (otherEvents != null) ordered.AddRange(otherEvents);
            if (statusRows != null) ordered.AddRange(statusRows);
            Rows = ordered.AsReadOnly();
        }
    }

    public sealed class ReservationTicket
    {
        public readonly long ContextId;
        public readonly string Tab, Id, BranchId;
        public readonly object Target;
        // Opaque game-boundary state captured on the original confirmation.
        // The model carries it without reading game state or deciding whether
        // the action is a reservation or its cancellation.
        public readonly object ReservationState;
        internal readonly long SelectionVersion;
        internal ReservationTicket(long contextId, string tab, string id, object target, long version, object reservationState, string branchId = null)
        { ContextId = contextId; Tab = tab; Id = id; Target = target; SelectionVersion = version; ReservationState = reservationState; BranchId = branchId; }
    }

    // Runtime-only navigation. No game API, config or save access belongs here.
    public sealed class LogTabState
    {
        public LogSnapshot Snapshot { get; private set; }
        public bool HasPublished { get; private set; }
        public bool PublicationFailed { get; private set; }
        public string SelectedId { get; private set; }
        public string SelectedBranchId { get; private set; }
        public float ListScroll, DetailScroll;
        public bool ExecutionFocus;
        public bool SupportsReservation { get { return Snapshot.Tab == "조건" || Snapshot.Tab == "이벤트" || Snapshot.Tab == "미션"; } }
        private long selectionVersion;
        private string searchQuery = "";
        private bool availableOnly;
        private IList<LogItem> visibleRows;

        public string SearchQuery
        {
            get { return searchQuery; }
            set
            {
                string next = value ?? "";
                if (Snapshot.Tab != "이벤트" || next == searchQuery) return;
                searchQuery = next; ApplyViewChange();
            }
        }

        public bool AvailableOnly
        {
            get { return availableOnly; }
            set
            {
                if (Snapshot.Tab != "이벤트" || value == availableOnly) return;
                availableOnly = value; ApplyViewChange();
            }
        }

        // Pure projection of the last publication; typing/searching/filtering
        // cannot invoke a game evaluator or extend the visible refresh scope.
        public IList<LogItem> VisibleRows
        {
            get
            {
                if (visibleRows != null) return visibleRows;
                if (Snapshot.Tab != "이벤트" || !availableOnly && searchQuery.Length == 0)
                    return visibleRows = Snapshot.Rows;
                var result = new List<LogItem>();
                foreach (LogItem row in Snapshot.Rows)
                    if ((!availableOnly || !PublicationFailed && row.IsAvailableEvent) && Matches(row, searchQuery)) result.Add(row);
                return visibleRows = result.AsReadOnly();
            }
        }

        private static bool Matches(LogItem row, string query)
        { return query.Length == 0 || (row.Title ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
            (row.SearchDisplayTitle ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
            (row.RawId ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
            (row.Description ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
            (row.Text ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0; }

        private void ApplyViewChange()
        {
            visibleRows = null; ListScroll = 0; CancelExecution();
            if (SelectedId != null && Selected == null) ClearSelection();
        }

        public LogTabState(long contextId, string tab)
        { Snapshot = new LogSnapshot(contextId, tab, null); }

        public LogItem Selected
        {
            get
            {
                foreach (LogItem row in VisibleRows)
                    if (row.Id == SelectedId && row.CanSelect) return row;
                return null;
            }
        }

        public LogConditionBranch SelectedBranch
        {
            get
            {
                LogItem row = Selected;
                if (Snapshot.Tab != "조건" || SelectedBranchId == null || row == null || row.ConditionDetails == null) return null;
                foreach (LogConditionBranch branch in row.ConditionDetails.Branches)
                    if (branch.Id == SelectedBranchId) return branch;
                return null;
            }
        }

        public object SelectedReservationTarget
        {
            get
            {
                if (Snapshot.Tab == "조건") return SelectedBranch == null ? null : SelectedBranch.Target;
                return Selected == null ? null : Selected.Target;
            }
        }

        public bool CanReserveSelected
        { get { return Selected != null && Selected.CanReserve && SelectedReservationTarget != null && !PublicationFailed; } }

        public bool SelectBranch(string id)
        {
            LogItem row = Selected;
            if (Snapshot.Tab == "조건" && !string.IsNullOrEmpty(id) && row != null && row.ConditionDetails != null)
                foreach (LogConditionBranch branch in row.ConditionDetails.Branches)
                    if (branch.Id == id)
                    {
                        if (SelectedBranchId != id) CancelExecution();
                        SelectedBranchId = id;
                        return true;
                    }
            ClearBranchSelection();
            return false;
        }

        public void ClearBranchSelection()
        { SelectedBranchId = null; CancelExecution(); }

        public bool Select(string id)
        {
            foreach (LogItem row in VisibleRows)
                if (row.Id == id)
                {
                    if (!row.CanSelect) return false;
                    if (SelectedId != id) { ClearBranchSelection(); DetailScroll = 0; }
                    SelectedId = id;
                    return true;
                }
            ClearSelection();
            return false;
        }

        public void ClearSelection()
        { SelectedId = null; DetailScroll = 0; ClearBranchSelection(); }

        public void CancelExecution()
        { ExecutionFocus = false; selectionVersion++; }

        internal void Publish(LogSnapshot snapshot)
        {
            LogItem previous = Selected;
            LogConditionBranch previousBranch = SelectedBranch;
            Snapshot = snapshot;
            visibleRows = null;
            HasPublished = true;
            PublicationFailed = false;
            LogItem next = Selected;
            if (SelectedId != null && (next == null || previous != null && !ReferenceEquals(previous.Target, next.Target)))
                ClearSelection();
            else
            {
                LogConditionBranch nextBranch = SelectedBranch;
                if (SelectedBranchId != null && (nextBranch == null || previousBranch != null &&
                    !ReferenceEquals(previousBranch.Target, nextBranch.Target))) ClearBranchSelection();
                if (next != null && !CanReserveSelected) CancelExecution();
            }
        }

        internal void Reset(long contextId)
        {
            Snapshot = new LogSnapshot(contextId, Snapshot.Tab, null);
            searchQuery = ""; availableOnly = false; visibleRows = null;
            HasPublished = false; PublicationFailed = false;
            ClearSelection(); ListScroll = DetailScroll = 0;
        }

        public ReservationTicket BeginReservation(object reservationState = null)
        {
            LogItem row = Selected;
            if (!SupportsReservation || !ExecutionFocus || !CanReserveSelected) return null;
            return new ReservationTicket(Snapshot.ContextId, Snapshot.Tab, row.Id, SelectedReservationTarget, selectionVersion,
                reservationState, Snapshot.Tab == "조건" ? SelectedBranchId : null);
        }

        public bool CanActivate(ReservationTicket ticket)
        {
            LogItem row = Selected;
            return SupportsReservation && !PublicationFailed && ticket != null && ExecutionFocus && ticket.ContextId == Snapshot.ContextId && ticket.Tab == Snapshot.Tab &&
                ticket.SelectionVersion == selectionVersion && CanReserveSelected && row.Id == ticket.Id &&
                (Snapshot.Tab != "조건" || SelectedBranchId == ticket.BranchId) && ReferenceEquals(SelectedReservationTarget, ticket.Target);
        }

        internal void MarkPublicationFailure()
        { PublicationFailed = true; visibleRows = null; CancelExecution(); }
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
