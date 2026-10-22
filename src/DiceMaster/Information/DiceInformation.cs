using System;
using System.Collections;

namespace DiceMaster.Information
{
    // Event-driven observation. No polling, Lua analysis, RNG or UI mutation.
    public static class DiceInformation
    {
        public sealed class View
        { public bool Visible; public string Name; public string[] Labels, Raw; }
        internal sealed class Choice
        { internal long Source; internal string Label; internal bool Confirmed, Failed; }
        private sealed class Session
        {
            internal WeakReference Owner;
            internal string Context, Key, Name;
            internal Choice Choice;
            internal string[] Labels = new string[0], Raw = new string[0];
            internal Func<View> Reader;
            internal bool Seen, Closed;
            internal InformationRow Row;
        }
        internal sealed class Closing
        { internal object Session; internal DiceRollRecord Record; internal string Context; }
        private static Session active;
        [ThreadStatic] private static Session advancing;
        private static Choice pending;
        private static long source;
        public static Func<object, bool> Visible;
        public static Func<float> Clock;
        public static Action<Exception> Failure;
        public static void Clear() { active = null; pending = null; unchecked { source++; } }
        public static void SourceChanged() { pending = null; unchecked { source++; } }
        public static void MenuBuilding() { pending = null; }
        internal static Choice BeginChoice(string label)
        {
            if (!BranchHistory.Enabled) return null;
            string value = DiceRollRecord.Clean(label, 160);
            return pending = value.Length == 0 ? null : new Choice { Source = source, Label = value };
        }
        internal static void EndChoice(Choice choice, bool success)
        {
            if (choice == null) return;
            choice.Confirmed = success; choice.Failed = !success;
            if (!success && ReferenceEquals(pending, choice)) pending = null;
            if (active != null && ReferenceEquals(active.Choice, choice)) active.Row = null;
        }
        private static bool Current(Session session)
        { return session != null && ReferenceEquals(active, session) && BranchHistory.Enabled && session.Context == BranchHistory.ContextKey && !session.Closed; }
        private static void Report(Exception error) { try { Failure?.Invoke(error); } catch { } }
        private static void Safe(Action action)
        { try { action(); } catch (Exception error) { Report(error); } }
        private static Session Begin(object owner, string key, Func<View> reader)
        {
            if (!BranchHistory.Enabled) return null;
            var choice = pending; pending = null;
            return active = new Session { Owner = new WeakReference(owner), Context = BranchHistory.ContextKey,
                Key = DiceRollRecord.Clean(key, 256), Reader = reader,
                Choice = choice != null && !choice.Failed && choice.Source == source ? choice : null };
        }
        private static void Sample(Session session)
        {
            if (!Current(session)) return;
            View view;
            try { view = session.Reader == null ? null : session.Reader(); }
            catch
            {
                // A failed later read cannot publish an older result name as
                // fresh. Keep only independently observed visibility/identity.
                session.Name = null; session.Labels = session.Raw = new string[0]; session.Row = null;
                try { session.Seen |= Visible != null && Visible(session.Owner.Target); } catch { }
                throw;
            }
            if (view == null || !view.Visible) return;
            session.Seen = true;
            session.Name = DiceRollRecord.Clean(view.Name, 160);
            session.Labels = Copy(view.Labels); session.Raw = Copy(view.Raw, true); session.Row = null;
        }
        private static string[] Copy(string[] values, bool literal = false)
        {
            if (values == null) return new string[0];
            var copy = new string[Math.Min(64, values.Length)];
            for (int at = 0; at < copy.Length; at++) copy[at] = DiceRollRecord.Clean(literal ? (values[at] ?? "").Replace("<", "＜").Replace(">", "＞") : values[at], 512);
            return copy;
        }
        public static InformationRow CurrentRow()
        {
            try
            {
                Session session = active;
                if (!Current(session) || !session.Seen || Visible == null || !Visible(session.Owner.Target)) return null;
                if (session.Row != null) return session.Row;
                string name = string.IsNullOrEmpty(session.Name) ? "주사위 판정" : session.Name;
                string choice = session.Choice != null && session.Choice.Confirmed && !session.Choice.Failed ? session.Choice.Label : "";
                string text = "주사위 판정: " + DiceRollRecord.Short(name) + (choice.Length == 0 ? "" : "\n진입 선택: " + DiceRollRecord.Short(choice)) + "\n진행 중";
                return session.Row = new InformationRow("dice/current", name, "주사위", "진행 중", text, text);
            }
            catch (Exception error) { Report(error); return null; }
        }
        // Called by the native MenuDialog.Clear prefix, only while advancing
        // the observed native dice coroutine. External clears/cancel/save do
        // not count. Its finalizer must confirm success before recording.
        internal static Closing BeginClosing(object owner, int index)
        {
            Session session = active;
            if (!Current(session) || !ReferenceEquals(advancing, session) || !ReferenceEquals(session.Owner.Target, owner) || !session.Seen || index < 1) return null;
            int at = index - 1;
            string choice = session.Choice != null && session.Choice.Confirmed && !session.Choice.Failed ? session.Choice.Label : "";
            return new Closing { Session = session, Context = session.Context,
                Record = new DiceRollRecord(session.Name, session.Key, choice, at < session.Labels.Length ? session.Labels[at] : "",
                    at < session.Raw.Length ? session.Raw[at] : "", index) };
        }
        internal static void EndClosing(Closing closing, Exception error)
        {
            if (closing == null || error != null) return;
            var session = closing.Session as Session;
            if (!Current(session) || !ReferenceEquals(advancing, session)) return;
            session.Closed = true; active = null; session.Reader = null;
            BranchHistory.RecordDice(closing.Record, Clock == null ? float.NaN : Clock(), closing.Context);
        }
        public static IEnumerator Observe(IEnumerator original, object owner, string key, Func<View> reader)
        { return original == null || !BranchHistory.Enabled ? original : new Observer(original, owner, key, reader); }
        private sealed class Observer : IEnumerator, IDisposable
        {
            private readonly IEnumerator original;
            private object owner; private string key; private Func<View> reader;
            private Session session;
            private bool started, running, disposed;
            internal Observer(IEnumerator original, object owner, string key, Func<View> reader)
            { this.original = original; this.owner = owner; this.key = key; this.reader = reader; }
            private void Abort() { if (ReferenceEquals(active, session)) active = null; if (session != null) session.Reader = null; owner = null; reader = null; }
            public object Current { get { try { return original.Current; } catch { Abort(); throw; } } }
            public bool MoveNext()
            {
                if (!started) { started = true; Safe(() => session = Begin(owner, key, reader)); owner = null; reader = null; }
                Session previous = advancing; advancing = session; running = true;
                try
                {
                    bool next = original.MoveNext();
                    if (next) Safe(() => Sample(session)); else Abort();
                    return next;
                }
                catch { Abort(); throw; }
                finally { advancing = previous; running = false; if (disposed) Abort(); }
            }
            public void Reset() { original.Reset(); }
            public void Dispose()
            {
                if (disposed) return; disposed = true;
                try { (original as IDisposable)?.Dispose(); }
                finally { if (!running) Abort(); }
            }
        }
    }
}
