using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using DiceMaster.LiveLog;

internal static class Program
{
    private static int assertions;

    private sealed class Harness
    {
        internal readonly RefreshEngine Engine = new RefreshEngine();
        internal readonly List<string> Logs = new List<string>();
        internal List<DisplayQuery> Queries = new List<DisplayQuery>();
        internal Func<List<DisplayQuery>> Discover;
        internal Action<List<DisplayRow>> Publish;
        internal Action<string> Log;
        internal List<DisplayRow> Rows = new List<DisplayRow>();
        internal int Discoveries;
        internal int Publications;
        internal string Tab = "events";
        internal bool Visible = true;
        internal bool Enabled = true;

        internal Harness()
        {
            Discover = delegate { return Queries; };
            Publish = delegate(List<DisplayRow> rows) { Rows = rows; };
            Log = delegate(string text) { Logs.Add(text); };
        }

        internal void Tick(float now, bool force = false, bool manual = false)
        {
            Engine.Tick(Tab, Visible, Enabled, now, force, manual,
                delegate { Discoveries++; return Discover(); },
                delegate(List<DisplayRow> rows) { Publications++; Publish(rows); },
                delegate(string text) { Log(text); });
        }

        internal DisplayRow Row(string id)
        {
            for (int index = 0; index < Rows.Count; index++)
                if (Rows[index].Id == id) return Rows[index];
            throw new InvalidOperationException("Missing row: " + id);
        }
    }

    private sealed class Probe
    {
        internal object Value;
        internal Exception Failure;
        internal int Reads;

        internal Probe(object value) { Value = value; }

        internal object Read()
        {
            Reads++;
            if (Failure != null) throw Failure;
            return Value;
        }

        internal DisplayQuery Query(string id) { return new DisplayQuery(id, Read); }
    }

    private static void Check(bool result, string message)
    {
        assertions++;
        if (!result) throw new InvalidOperationException(message);
    }

    private static void FirstAndMiddleFailuresAreIndependent()
    {
        Harness harness = new Harness();
        Probe first = new Probe("first") { Failure = new InvalidOperationException("secret-first") };
        Probe second = new Probe("second");
        Probe middle = new Probe("middle") { Failure = new ArgumentException("secret-middle") };
        Probe last = new Probe("last");
        harness.Queries.Add(first.Query("a"));
        harness.Queries.Add(second.Query("b"));
        harness.Queries.Add(middle.Query("c"));
        harness.Queries.Add(last.Query("d"));
        harness.Tick(0);
        Check(harness.Rows.Count == 4 && harness.Rows[0].Id == "a" && harness.Rows[3].Id == "d", "A failed row changed row ordering/count.");
        Check(harness.Row("a").Status == RefreshEngine.ReadFailureStatus && harness.Row("c").Status == RefreshEngine.ReadFailureStatus, "Initial failures lack their own status.");
        Check((string)harness.Row("b").Value == "second" && (string)harness.Row("d").Value == "last", "A first/middle failure suppressed a later row.");
        second.Value = "second-v2";
        last.Value = "last-v2";
        harness.Tick(0.25f);
        Check(first.Reads == 1 && middle.Reads == 1 && second.Reads == 2 && last.Reads == 2, "Failed and healthy rows share a retry timer.");
        Check((string)harness.Row("b").Value == "second-v2" && (string)harness.Row("d").Value == "last-v2", "Unrelated successful updates rolled back.");
        Check(harness.Logs.Count == 2, "Healthy reads or skipped retries generated diagnostics.");
    }

    private static void RetainedFailureAndRecovery()
    {
        Harness harness = new Harness();
        object oldValue = new object();
        Probe probe = new Probe(oldValue);
        harness.Queries.Add(probe.Query("row"));
        harness.Tick(20);
        probe.Failure = new InvalidOperationException("do-not-log-save-data");
        harness.Tick(20.25f);
        Check(ReferenceEquals(harness.Row("row").Value, oldValue), "A failed read discarded its last successful value.");
        Check(harness.Row("row").Status == RefreshEngine.RetainedFailureStatus, "Retained value is not marked stale.");
        harness.Tick(25.25f);
        Check(harness.Logs.Count == 1, "An unchanged consecutive cause was logged twice.");
        object recoveredValue = new object();
        probe.Failure = null;
        probe.Value = recoveredValue;
        harness.Tick(30.25f);
        Check(ReferenceEquals(harness.Row("row").Value, recoveredValue) && harness.Row("row").Status == string.Empty, "Recovery did not replace/clear stale content.");
        Check(harness.Logs.Count == 2 && harness.Logs[1].Contains("recovered"), "Recovery must be logged once.");
        harness.Tick(30.5f);
        Check(harness.Logs.Count == 2, "An ordinary success was logged as another recovery.");
        Check(!string.Join("\n", harness.Logs).Contains("do-not-log-save-data"), "Exception messages leaked into diagnostics.");
    }

    private static void RetryDeadlinesArePerRow()
    {
        Harness harness = new Harness();
        Probe a = new Probe(1) { Failure = new InvalidOperationException() };
        Probe b = new Probe(2);
        harness.Queries.Add(a.Query("a")); harness.Queries.Add(b.Query("b"));
        harness.Tick(0);
        b.Failure = new InvalidOperationException();
        harness.Tick(0.25f);
        harness.Tick(4.9f);
        harness.Tick(5);
        Check(a.Reads == 2 && b.Reads == 2, "A failed-row deadline was delayed by the healthy cadence or another failure.");
        harness.Tick(5.25f);
        Check(a.Reads == 2 && b.Reads == 3, "Two failed rows did not keep independent deadlines.");
    }

    private static void ForceTabReopenAndEnablePreserveRetry()
    {
        Harness harness = new Harness();
        Probe healthy = new Probe(1);
        Probe failed = new Probe(null) { Failure = new InvalidOperationException() };
        harness.Queries.Add(healthy.Query("healthy")); harness.Queries.Add(failed.Query("failed"));
        harness.Tick(0);
        harness.Tick(0.01f, true);
        Check(healthy.Reads == 2 && failed.Reads == 1, "Force bypassed a failed row's deadline.");
        harness.Visible = false; harness.Tick(0.02f, true, true);
        Check(healthy.Reads == 2 && failed.Reads == 1, "A hidden window performed reads.");
        harness.Visible = true; harness.Tick(0.03f);
        Check(healthy.Reads == 3 && failed.Reads == 1, "Reopening did not refresh only healthy rows.");
        harness.Enabled = false; harness.Tick(0.04f, true, true);
        harness.Enabled = true; harness.Tick(0.05f);
        Check(healthy.Reads == 4 && failed.Reads == 1, "Re-enabling reset the failed-row deadline.");
        harness.Tab = "missions";
        harness.Tick(0.06f);
        Check(healthy.Reads == 5 && failed.Reads == 2, "A separate tab lacks its own row state.");
        harness.Tab = "events";
        harness.Tick(0.07f);
        Check(healthy.Reads == 6 && failed.Reads == 2, "Returning to a tab lost its failure deadline.");
        Check(harness.Logs.Count == 2, "Diagnostics were not isolated by tab.");
    }

    private static void ManualRefreshBypassesFailureBackoff()
    {
        Harness harness = new Harness();
        Probe probe = new Probe(1) { Failure = new InvalidOperationException() };
        harness.Queries.Add(probe.Query("row"));
        harness.Tick(0);
        harness.Tick(0.01f, false, true);
        Check(probe.Reads == 2 && harness.Logs.Count == 1, "Manual refresh did not bypass the failed row or repeated the same diagnostic.");
        probe.Failure = null;
        harness.Tick(0.02f, false, true);
        Check(probe.Reads == 3 && harness.Row("row").Status == string.Empty, "Manual recovery did not happen immediately.");
        Check(harness.Logs.Count == 2, "Manual recovery diagnostic missing.");
    }

    private static void DiscoveryFailurePreservesAndRefreshesKnownRows()
    {
        Harness harness = new Harness();
        Probe first = new Probe("first");
        Probe middle = new Probe("middle");
        Probe last = new Probe("last");
        harness.Queries.Add(first.Query("first")); harness.Queries.Add(middle.Query("middle")); harness.Queries.Add(last.Query("last"));
        harness.Tick(0);
        harness.Discover = ThrowDiscovery;
        first.Value = "updated-first";
        middle.Failure = new InvalidOperationException();
        last.Value = "updated-last";
        harness.Tick(0.25f);
        Check(harness.Rows.Count == 4 && harness.Row(RefreshEngine.DiscoveryRowId).Status == RefreshEngine.ReadFailureStatus, "A discovery failure lacks a synthetic status row.");
        Check((string)harness.Row("first").Value == "updated-first" && (string)harness.Row("last").Value == "updated-last", "Discovery failure rolled back successful rows.");
        Check((string)harness.Row("middle").Value == "middle" && harness.Row("middle").Status == RefreshEngine.RetainedFailureStatus, "A discovery failure concealed an independent item failure.");
        harness.Tick(0.5f);
        Check(harness.Discoveries == 2 && first.Reads == 3 && last.Reads == 3, "Discovery retry backoff blocked healthy known rows.");
        harness.Tick(5.25f);
        Check(harness.Discoveries == 3 && harness.Logs.Count == 2, "Repeated discovery/item failures were not deduplicated.");
        harness.Discover = delegate { return harness.Queries; };
        middle.Failure = null;
        harness.Tick(5.26f, false, true);
        Check(harness.Rows.Count == 3 && harness.Row("middle").Status == string.Empty, "Discovery recovery did not remove the synthetic row.");
        Check(harness.Logs.Count == 4, "Independent discovery/item recoveries were not logged once each.");
    }

    private static List<DisplayQuery> ThrowDiscovery() { throw new InvalidOperationException("private-lua-source"); }

    private static void EmptyAndUnavailableAreDifferent()
    {
        Harness empty = new Harness();
        empty.Tick(0);
        Check(empty.Rows.Count == 0 && empty.Logs.Count == 0, "An available empty list became an error.");
        Harness waiting = new Harness();
        waiting.Discover = delegate { throw new DataNotReadyException(); };
        waiting.Tick(0);
        Check(waiting.Rows.Count == 1 && waiting.Rows[0].Status == RefreshEngine.WaitingStatus, "Unavailable discovery was shown as an empty successful list.");
        waiting.Tick(0.25f);
        Check(waiting.Discoveries == 1, "Unavailable discovery retries every healthy frame.");
        waiting.Discover = delegate { return new List<DisplayQuery>(); };
        waiting.Tick(0.5f, false, true);
        Check(waiting.Rows.Count == 0 && waiting.Logs.Count == 2, "A recovered empty list retained a waiting row.");
        Harness nullDiscovery = new Harness();
        nullDiscovery.Discover = delegate { return null; };
        nullDiscovery.Tick(0);
        Check(nullDiscovery.Rows[0].Status == RefreshEngine.WaitingStatus, "Null discovery was not classified as unavailable.");
        Harness nullValue = new Harness();
        Probe probe = new Probe(null);
        nullValue.Queries.Add(probe.Query("row")); nullValue.Tick(0);
        Check(nullValue.Row("row").Value == null && nullValue.Row("row").Status == string.Empty, "A legitimate null model was classified as unavailable.");
        probe.Failure = new DataNotReadyException(); nullValue.Tick(0.25f);
        Check(nullValue.Row("row").Status == RefreshEngine.WaitingStatus, "Unavailable item data lacks a waiting status.");
    }

    private static void ContextResetClearsValuesBackoffAndDiagnostics()
    {
        Harness harness = new Harness();
        Probe probe = new Probe("old-save-value");
        harness.Queries.Add(probe.Query("row")); harness.Tick(100);
        probe.Failure = new InvalidOperationException(); harness.Tick(100.25f);
        harness.Engine.ResetContext(); harness.Tick(0);
        Check(probe.Reads == 3 && harness.Row("row").Value == null, "A new context retained old game values or backoff.");
        Check(harness.Row("row").Status == RefreshEngine.ReadFailureStatus, "New-context failure was marked as a retained value.");
        Check(harness.Logs.Count == 2 && harness.Logs[0].Contains("context=0") && harness.Logs[1].Contains("context=1"), "Context diagnostics shared deduplication state.");
        probe.Failure = null; probe.Value = "new-save-value";
        harness.Tick(0.01f, false, true);
        Check((string)harness.Row("row").Value == "new-save-value", "New context failed to recover.");
    }

    private static void ClockResetAndInvalidTimesCannotStrandRetries()
    {
        Harness harness = new Harness();
        Probe probe = new Probe(1) { Failure = new InvalidOperationException() };
        harness.Queries.Add(probe.Query("row")); harness.Tick(100);
        harness.Tick(0);
        harness.Tick(float.NaN);
        harness.Tick(float.PositiveInfinity);
        harness.Tick(4.99f);
        Check(probe.Reads == 1, "Clock reset or an invalid clock bypassed the retry deadline.");
        harness.Tick(5);
        Check(probe.Reads == 2, "Clock reset stranded a failed row at its old absolute deadline.");
        Harness wrap = new Harness();
        Probe wrapProbe = new Probe(1) { Failure = new InvalidOperationException() };
        wrap.Queries.Add(wrapProbe.Query("row")); wrap.Tick(10);
        wrap.Tick(-10); wrap.Tick(-5);
        Check(wrapProbe.Reads == 2, "A wrapping clock stranded the failed row.");
    }

    private static void CauseChangesDeduplicateConsecutiveFailuresOnly()
    {
        Harness harness = new Harness();
        Probe probe = new Probe("secret-model") { Failure = new InvalidOperationException("secret-message-one") };
        harness.Queries.Add(probe.Query("row")); harness.Tick(0);
        probe.Failure = new InvalidOperationException("secret-message-two");
        harness.Tick(0.01f, false, true);
        Check(harness.Logs.Count == 1, "Different messages bypassed type/stack deduplication.");
        probe.Failure = new ArgumentException("secret-message-three"); harness.Tick(0.02f, false, true);
        probe.Failure = new InvalidOperationException("secret-message-four"); harness.Tick(0.03f, false, true);
        Check(harness.Logs.Count == 3, "A changed cause or a return to a former cause was suppressed.");
        probe.Failure = null; harness.Tick(0.04f, false, true);
        probe.Failure = new InvalidOperationException("secret-message-five"); harness.Tick(0.05f, false, true);
        Check(harness.Logs.Count == 5, "A new failure after recovery was suppressed.");
        string diagnostics = string.Join("\n", harness.Logs);
        Check(!diagnostics.Contains("secret-message") && !diagnostics.Contains("secret-model"), "Raw values or exception messages were logged.");
        Check(diagnostics.Contains("System.InvalidOperationException") && diagnostics.Contains("Probe.Read"), "Diagnostics omit the cause type or stack.");
    }

    private static void LoggerFailuresDoNotPoisonState()
    {
        Harness harness = new Harness();
        Probe probe = new Probe(1) { Failure = new InvalidOperationException() };
        harness.Queries.Add(probe.Query("row"));
        int logAttempts = 0;
        harness.Log = delegate { logAttempts++; throw new Exception("broken logger"); };
        harness.Tick(0);
        Check(harness.Publications == 1 && harness.Row("row").Status == RefreshEngine.ReadFailureStatus, "A logger failure prevented publication.");
        harness.Tick(5);
        Check(probe.Reads == 2 && logAttempts == 1, "A logger failure poisoned retry/dedup state.");
        probe.Failure = null; harness.Tick(10);
        Check(harness.Row("row").Status == string.Empty && logAttempts == 2, "A recovery logger failure poisoned the recovered row.");
        harness.Tick(10.25f);
        Check(probe.Reads == 4 && logAttempts == 2, "A logger failure left the engine reentry guard active.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void OriginalFailureA() { throw new InvalidOperationException("private-original-source-a"); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void OriginalFailureB() { throw new InvalidOperationException("private-original-source-b"); }

    private static Exception StoredFailure(bool alternate)
    {
        try
        {
            if (alternate) OriginalFailureB();
            else OriginalFailureA();
        }
        catch (Exception error) { return error; }
        throw new InvalidOperationException("Expected a source exception.");
    }

    private static void StoredFailuresRethrownViaEdiStayDeduplicated()
    {
        Harness harness = new Harness();
        Exception stored = StoredFailure(false);
        harness.Queries.Add(new DisplayQuery("row", delegate {
            if (stored != null) ExceptionDispatchInfo.Capture(stored).Throw();
            return "recovered";
        }));
        harness.Tick(0);
        harness.Tick(5);
        harness.Tick(5.01f, false, true);
        Check(harness.Logs.Count == 1, "Repeated EDI propagation of the same cached source failure bypassed cause deduplication.");
        stored = StoredFailure(true);
        harness.Tick(5.02f, false, true);
        Check(harness.Logs.Count == 2, "A different original throwing stack frame was suppressed.");
        stored = null;
        harness.Tick(5.03f, false, true);
        harness.Tick(5.5f);
        Check(harness.Logs.Count == 3 && harness.Logs[2].Contains("recovered"), "EDI source recovery did not produce exactly one recovery.");
        Check(!string.Join("\n", harness.Logs).Contains("private-original-source"), "EDI diagnostics leaked the source exception message.");
    }

    private static void ReentryGuardAlwaysRecovers()
    {
        Harness harness = new Harness();
        Probe probe = new Probe(1);
        harness.Queries.Add(new DisplayQuery("row", delegate { harness.Tick(0, true, true); return probe.Read(); }));
        harness.Publish = delegate { harness.Tick(0, true, true); throw new Exception("publish failure"); };
        harness.Tick(0);
        Check(probe.Reads == 1 && harness.Publications == 1, "A callback recursively entered the refresh engine.");
        harness.Publish = delegate(List<DisplayRow> rows) { harness.Rows = rows; };
        harness.Tick(0.25f);
        Check(probe.Reads == 2 && harness.Publications == 2 && harness.Rows.Count == 1, "Publication failure left the reentry guard active.");
        Check(harness.Logs.Count == 2 && harness.Logs[1].Contains("recovered"), "Publication failure/recovery diagnostics missing.");
    }

    private static void ContextResetInsideCallbackDropsStaleResult()
    {
        Harness harness = new Harness();
        int secondReads = 0;
        harness.Queries.Add(new DisplayQuery("a", delegate { harness.Engine.ResetContext(); return "old-context"; }));
        harness.Queries.Add(new DisplayQuery("b", delegate { secondReads++; return "old-context"; }));
        harness.Tick(0);
        Check(harness.Publications == 0 && secondReads == 0, "A context reset inside a callback published/read stale context data.");
        harness.Queries.Clear(); harness.Queries.Add(new DisplayQuery("fresh", delegate { return "new-context"; }));
        harness.Tick(0.01f);
        Check(harness.Rows.Count == 1 && harness.Rows[0].Id == "fresh", "Context reset inside a callback poisoned the next tick.");
    }

    private static void InvalidDiscoveryDoesNotPartiallyReconcile()
    {
        Harness harness = new Harness();
        Probe probe = new Probe("prior");
        harness.Queries.Add(probe.Query("row")); harness.Tick(0);
        harness.Queries = new List<DisplayQuery> { probe.Query("replacement"), probe.Query("replacement") };
        probe.Value = "current"; harness.Tick(0.25f);
        Check(harness.Rows.Count == 2 && (string)harness.Row("row").Value == "current", "Invalid discovery partially replaced the prior successful list.");
        Check(harness.Row(RefreshEngine.DiscoveryRowId).Status == RefreshEngine.ReadFailureStatus, "Invalid discovery lacks an isolated status row.");
        harness.Queries = new List<DisplayQuery> { new DisplayQuery("fresh", null) };
        harness.Tick(0.5f, false, true);
        Check(harness.Rows.Count == 1 && harness.Row("fresh").Status == RefreshEngine.ReadFailureStatus, "A missing reader crashed the whole refresh.");
    }

    private static void UnchangedValuesStayQuietAndCadenceIsBounded()
    {
        Harness harness = new Harness();
        object value = new object(); Probe probe = new Probe(value);
        harness.Queries.Add(probe.Query("row")); harness.Tick(0);
        harness.Tick(0.1f); harness.Tick(0.249f);
        Check(probe.Reads == 1 && harness.Publications == 1 && harness.Discoveries == 1, "Healthy cadence exceeded four reads/second.");
        harness.Tick(0.25f);
        Check(probe.Reads == 2 && ReferenceEquals(harness.Row("row").Value, value) && harness.Logs.Count == 0, "Unchanged successful values created error/recovery noise.");
        harness.Visible = false; harness.Tick(10);
        Check(probe.Reads == 2 && harness.Discoveries == 2, "Hidden polling still read/discovered data.");
    }

    private static int Main()
    {
        Action[] tests = {
            FirstAndMiddleFailuresAreIndependent,
            RetainedFailureAndRecovery,
            RetryDeadlinesArePerRow,
            ForceTabReopenAndEnablePreserveRetry,
            ManualRefreshBypassesFailureBackoff,
            DiscoveryFailurePreservesAndRefreshesKnownRows,
            EmptyAndUnavailableAreDifferent,
            ContextResetClearsValuesBackoffAndDiagnostics,
            ClockResetAndInvalidTimesCannotStrandRetries,
            CauseChangesDeduplicateConsecutiveFailuresOnly,
            LoggerFailuresDoNotPoisonState,
            StoredFailuresRethrownViaEdiStayDeduplicated,
            ReentryGuardAlwaysRecovers,
            ContextResetInsideCallbackDropsStaleResult,
            InvalidDiscoveryDoesNotPartiallyReconcile,
            UnchangedValuesStayQuietAndCadenceIsBounded
        };
        for (int index = 0; index < tests.Length; index++)
        {
            tests[index]();
            Console.WriteLine("PASS " + tests[index].Method.Name);
        }
        Console.WriteLine("PASS: " + tests.Length + " refresh engine scenarios, " + assertions + " assertions.");
        return 0;
    }
}
