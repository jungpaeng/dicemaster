using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Story;

internal static partial class Program
{
    // Explicit host-side experiment, not a timing assertion or Windows/FPS
    // test. Run the test executable with --projection-benchmark. Each reader
    // comparison has identical immutable definitions and current outcomes.
    private static void ProjectionBenchmark()
    {
        Reset();
        var predicates = new List<PreviewCondition>();
        for (int at = 0; at < 256; at++) Condition("definition_" + at, 0);
        for (int at = 224; at < 256; at++) predicates.Add(PreviewConditions.Parse("checkpointmanager.Condition('definition_" + at + "')"));
        object Measure(bool optimized)
        {
            var times = new List<double>(); long bytes = 0;
            for (int at = 0; at < 180; at++)
            {
                long allocated = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
                var reader = optimized ? PreviewConditions.CreateRouteReader() : PreviewConditions.CreateReader();
                foreach (var predicate in predicates)
                    if (reader.Current(predicate) != PreviewConditionState.Met) throw new Exception("Benchmark changed the condition result.");
                double ms = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                if (at >= 30) { times.Add(ms); bytes += GC.GetAllocatedBytesForCurrentThread() - allocated; }
            }
            times.Sort();
            return new { optimized, queries = times.Count, meanMs = times.Average(), p95Ms = times[(int)Math.Ceiling(times.Count * .95) - 1], bytesPerQuery = bytes / times.Count };
        }
        // Alternate order to expose gross warmup/order effects in the report.
        var readerRounds = new List<object>();
        for (int at = 0; at < 3; at++)
        { readerRounds.Add(Measure(at % 2 != 0)); readerRounds.Add(Measure(at % 2 == 0)); }
        var cadence = new List<object>();
        ProjectionCadence(.25); // Warm the provider/JIT, not retained in report.
        for (int round = 0; round < 3; round++)
            foreach (double interval in new[] { .25, .5, 1.0 }) cadence.Add(ProjectionCadence(interval));
        Console.WriteLine(JsonSerializer.Serialize(new { boundary = "host .NET8, synthetic serialized definitions and production providers; no Lua/Windows/Unity rendering/FPS", readerRounds, cadence }));
    }
    private static object ProjectionCadence(double interval)
    {
        Reset(); MapRouteDefinitions.ClearSharedSyntaxCache();
        var reset = Condition("cadence_topic", 1); reset._default = 0;
        var persistent = Condition("cadence_persistent", 0);
        CheckPointManager.Instance._condition._list[0]._items[0]._items.Add(IsZero(persistent)._items[0]);
        var environment = ResetEnvironment(reset);
        DataHelper.Scripts["cadence_topics"] = "local a={};a[1]=checkpointmanager.Condition('cadence_topic') and 'O_topic' or '~x';local r=choose(a);if r==1 then " + UnlockNpc + " end";
        ChoiceStorySource.ObserveScript(UnlockRoot + "statmodifymanager.ResetTalkFlag();luamanager.SetNextScript('cadence_topics');luamanager.Init() end", "cadence_room.LuaScript");
        Normal("O_entry", "사사형");
        var engine = new RefreshEngine(); var queryMs = new List<double>();
        double first = -1, flipped = -1, removed = -1; long bytes = 0; int ticks = 0;
        for (int frame = 0; frame <= 5400; frame++)
        {
            double actual = frame / 60.0; UnityEngine.Time.unscaledTime = (float)actual;
            if (flipped < 0 && actual >= 45.123) { persistent.State = 1; flipped = actual; }
            // RefreshEngine retains its actual .25 s rule. Scaling its clock
            // simulates a different healthy interval without changing source;
            // the provider itself still sees real elapsed time (5 s retries).
            engine.Tick("choice", true, true, (float)(actual * .25 / interval), false, false, () =>
            {
                var requests = ChoiceStorySource.Discover();
                return requests.Select(request => new DisplayQuery(request.Id, () =>
                {
                    long allocated = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
                    var value = (ChoiceStorySnapshot)request.Read(); ticks++;
                    queryMs.Add((Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency);
                    bytes += GC.GetAllocatedBytesForCurrentThread() - allocated;
                    bool shown = value.Entries.Any(row => row.Title == "사사형" && row.AutoText.Length != 0);
                    if (shown && first < 0) first = actual;
                    if (!shown && flipped >= 0 && removed < 0) removed = actual;
                    return value;
                })).ToList();
            }, _ => { }, _ => { });
        }
        if (first < 0 || removed < flipped || Counters.GameCalls != 0 || DataHelper.Reads != 1) throw new Exception("Cadence experiment lost its route or read-only/cache boundary.");
        queryMs.Sort(); GC.KeepAlive(environment);
        return new { intervalSeconds = interval, simulatedSeconds = 90, queries = ticks, firstTooltipSeconds = first, unlockRemovalLatencySeconds = removed - flipped,
            queryCpuMs = queryMs.Sum(), p95QueryMs = queryMs[(int)Math.Ceiling(queryMs.Count * .95) - 1], allocatedBytes = bytes, resourceReads = DataHelper.Reads };
    }
}
