using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DiceMaster.LiveLog
{
    public sealed class DisplayQuery
    {
        public string Id;
        public Func<object> Read;

        public DisplayQuery() { }

        public DisplayQuery(string id, Func<object> read)
        {
            Id = id;
            Read = read;
        }
    }

    public sealed class DisplayRow
    {
        public string Id;
        public object Value;
        public string Status;

        public DisplayRow(string id, object value, string status)
        {
            Id = id;
            Value = value;
            Status = status;
        }
    }

    // An adapter must distinguish unavailable game data from an available, empty list.
    public sealed class DataNotReadyException : Exception
    {
        public DataNotReadyException() { }
    }

    // This file deliberately depends only on the .NET Framework 4.8 base library.
    // It is also linked into the boundary tests and copied into the game assembly.
    public sealed class RefreshEngine
    {
        public const string WaitingStatus = "게임 데이터 대기 중";
        public const string ReadFailureStatus = "읽기 실패";
        public const string RetainedFailureStatus = "갱신 실패 · 이전 값";
        public const string DiscoveryRowId = "__discovery__";

        private sealed class ItemState
        {
            internal DisplayQuery Query;
            internal object Value;
            internal bool HasValue;
            internal bool Attempted;
            internal bool Failed;
            internal double RetryAt;
            internal string Status = string.Empty;
            internal string Cause;
        }

        private sealed class TabState
        {
            internal List<ItemState> Items = new List<ItemState>();
            internal readonly ItemState Discovery = new ItemState();
            internal readonly ItemState Publication = new ItemState();
            internal double HealthyAt;
        }

        private readonly Dictionary<string, TabState> tabs = new Dictionary<string, TabState>(StringComparer.Ordinal);
        private bool busy;
        private bool wasVisible;
        private bool wasEnabled;
        private string activeTab;
        private int context;
        private bool hasClock;
        private float lastClock;
        private double elapsed;

        public void ResetContext()
        {
            context++;
            tabs.Clear();
            wasVisible = false;
            wasEnabled = false;
            activeTab = null;
            hasClock = false;
            elapsed = 0;
        }

        public void Tick(string tab, bool visible, bool enabled, float now, bool forceHealthy, bool manual,
            Func<List<DisplayQuery>> discover, Action<List<DisplayRow>> publish, Action<string> log)
        {
            if (busy) return;
            busy = true;
            try
            {
                double clock = AdvanceClock(now);
                bool opened = !wasVisible || !wasEnabled || !string.Equals(activeTab, tab, StringComparison.Ordinal);
                wasVisible = visible;
                wasEnabled = enabled;
                if (!visible || !enabled) return;
                activeTab = tab;
                string key = tab ?? string.Empty;
                TabState state;
                if (!tabs.TryGetValue(key, out state))
                {
                    state = new TabState();
                    tabs.Add(key, state);
                }

                bool healthyDue = manual || forceHealthy || opened || clock >= state.HealthyAt;
                if (!healthyDue && !HasRetryDue(state, clock)) return;
                if (healthyDue) state.HealthyAt = clock + 0.25;
                int currentContext = context;

                if (manual || (state.Discovery.Failed ? clock >= state.Discovery.RetryAt : healthyDue))
                {
                    try
                    {
                        List<DisplayQuery> queries = discover == null ? null : discover();
                        if (context != currentContext) return;
                        ValidateQueries(queries);
                        state.Items = Reconcile(state.Items, queries);
                        Recover(state.Discovery, key, DiscoveryRowId, log);
                    }
                    catch (Exception exception)
                    {
                        if (context != currentContext) return;
                        Fail(state.Discovery, exception, clock, key, DiscoveryRowId, log);
                    }
                }

                // A failed discovery still leaves the previous independent queries usable.
                for (int index = 0; index < state.Items.Count; index++)
                {
                    if (context != currentContext) return;
                    ItemState item = state.Items[index];
                    bool due = manual || (!item.Attempted) || (item.Failed ? clock >= item.RetryAt : healthyDue);
                    if (!due) continue;
                    try
                    {
                        if (item.Query.Read == null) throw new InvalidOperationException();
                        object value = item.Query.Read();
                        if (context != currentContext) return;
                        item.Value = value;
                        item.HasValue = true;
                        item.Attempted = true;
                        Recover(item, key, item.Query.Id, log);
                    }
                    catch (Exception exception)
                    {
                        if (context != currentContext) return;
                        Fail(item, exception, clock, key, item.Query.Id, log);
                    }
                }

                if (context != currentContext) return;
                List<DisplayRow> rows = new List<DisplayRow>();
                for (int index = 0; index < state.Items.Count; index++)
                {
                    ItemState item = state.Items[index];
                    rows.Add(new DisplayRow(item.Query.Id, item.Value, item.Status));
                }
                if (state.Discovery.Failed)
                    rows.Add(new DisplayRow(DiscoveryRowId, null, state.Discovery.Status));
                try
                {
                    if (publish != null) publish(rows);
                    if (context != currentContext) return;
                    Recover(state.Publication, key, "__publish__", log);
                }
                catch (Exception exception)
                {
                    if (context != currentContext) return;
                    Fail(state.Publication, exception, clock, key, "__publish__", log);
                }
            }
            finally
            {
                busy = false;
            }
        }

        private double AdvanceClock(float now)
        {
            if (float.IsNaN(now) || float.IsInfinity(now)) return elapsed;
            if (hasClock)
            {
                double difference = (double)now - lastClock;
                // A Unity time reset/wrap starts a new clock segment; existing waits
                // continue against elapsed time instead of an unreachable old deadline.
                if (difference > 0) elapsed += difference;
            }
            hasClock = true;
            lastClock = now;
            return elapsed;
        }

        private static bool HasRetryDue(TabState state, double now)
        {
            if (state.Discovery.Failed && now >= state.Discovery.RetryAt) return true;
            for (int index = 0; index < state.Items.Count; index++)
            {
                ItemState item = state.Items[index];
                if (item.Failed && now >= item.RetryAt) return true;
            }
            return false;
        }

        private static void ValidateQueries(List<DisplayQuery> queries)
        {
            if (queries == null) throw new DataNotReadyException();
            Dictionary<string, bool> seen = new Dictionary<string, bool>(StringComparer.Ordinal);
            for (int index = 0; index < queries.Count; index++)
            {
                DisplayQuery query = queries[index];
                if (query == null || string.IsNullOrEmpty(query.Id) || query.Id == DiscoveryRowId || seen.ContainsKey(query.Id))
                    throw new InvalidOperationException();
                seen.Add(query.Id, true);
            }
        }

        private static List<ItemState> Reconcile(List<ItemState> oldItems, List<DisplayQuery> queries)
        {
            Dictionary<string, ItemState> previous = new Dictionary<string, ItemState>(StringComparer.Ordinal);
            for (int index = 0; index < oldItems.Count; index++)
                previous.Add(oldItems[index].Query.Id, oldItems[index]);
            List<ItemState> items = new List<ItemState>();
            for (int index = 0; index < queries.Count; index++)
            {
                DisplayQuery query = queries[index];
                ItemState item;
                if (!previous.TryGetValue(query.Id, out item)) item = new ItemState();
                item.Query = query;
                items.Add(item);
            }
            return items;
        }

        private void Fail(ItemState item, Exception exception, double now, string tab, string id, Action<string> log)
        {
            string type = exception.GetType().FullName;
            string stack = exception.StackTrace ?? string.Empty;
            // Repeated ExceptionDispatchInfo propagation of a cached source
            // exception appends transport frames to StackTrace. Identify the
            // original throw site so those wrapper frames do not create new
            // causes on every retry; retain the full stack in the diagnostic.
            StackFrame source = new StackTrace(exception, false).GetFrame(0);
            System.Reflection.MethodBase method = source == null ? null : source.GetMethod();
            string cause = type + "\n" + (method == null ? string.Empty :
                (method.DeclaringType == null ? string.Empty : method.DeclaringType.FullName) + "::" + method + "@IL" + source.GetILOffset());
            bool changed = !item.Failed || !string.Equals(item.Cause, cause, StringComparison.Ordinal);
            item.Attempted = true;
            item.Failed = true;
            item.RetryAt = now + 5;
            item.Status = exception is DataNotReadyException ? WaitingStatus : item.HasValue ? RetainedFailureStatus : ReadFailureStatus;
            item.Cause = cause;
            if (changed) SafeLog(log, DiagnosticPrefix(tab, id) + " failure type=" + type + "\n" + stack);
        }

        private void Recover(ItemState item, string tab, string id, Action<string> log)
        {
            bool recovered = item.Failed;
            item.Failed = false;
            item.Cause = null;
            item.Status = string.Empty;
            if (recovered) SafeLog(log, DiagnosticPrefix(tab, id) + " recovered");
        }

        private string DiagnosticPrefix(string tab, string id)
        {
            return "LiveLog context=" + context + " tab=" + SafeIdentity(tab) + " item=" + SafeIdentity(id);
        }

        private static string SafeIdentity(string identity)
        {
            if (identity == null) return string.Empty;
            // Keep diagnostics single-line and bounded even if an adapter passes an
            // unexpected identifier. Values and exception messages never reach logs.
            int length = Math.Min(identity.Length, 96);
            char[] characters = new char[length];
            for (int index = 0; index < length; index++)
            {
                char character = identity[index];
                characters[index] = char.IsControl(character) ? '_' : character;
            }
            return new string(characters);
        }

        private static void SafeLog(Action<string> log, string text)
        {
            if (log == null) return;
            try { log(text); }
            catch (Exception) { }
        }
    }
}
