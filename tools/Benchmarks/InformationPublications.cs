using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using DiceMaster.Information;
using DiceMaster.LiveLog;

// Real scheduler/publication code, fixture game boundaries; no Unity/Windows FPS.
static class PublicationProbe
{
    static void Main()
    {
        var reports = new List<object>();
        foreach (bool map in new[] { false, true })
        {
            InformationRuntime.Clear();
            InformationRuntime.Install(new HarmonyLib.Harmony());
            MapGuideSource.Visible = map;
            ShopEffectSource.Visible = !map;
            ChoiceStorySource.Visible = false;
            string title = map ? "지도 안내" : "상점 효과";
            for (int i = 0; i < 200; i++) InformationRuntime.Tick(null, true, i * .5f);
            int before = map ? MapGuideSource.Reads : ShopEffectSource.Reads, publications = 0;
            var previous = LogBridge.Model.Tab(title).Snapshot;
            const int count = 2000;
            var times = new double[count];
            GC.Collect(); GC.WaitForPendingFinalizers();
            long bytes = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < count; i++)
            {
                long start = Stopwatch.GetTimestamp();
                InformationRuntime.Tick(null, true, 100 + i * .5f);
                times[i] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                var current = LogBridge.Model.Tab(title).Snapshot;
                if (!ReferenceEquals(previous, current)) publications++;
                previous = current;
            }
            bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
            Array.Sort(times);
            reports.Add(new { surface = title, queries = (map ? MapGuideSource.Reads : ShopEffectSource.Reads) - before,
                publications, bytesPerTick = bytes / count, p50Ms = times[count / 2], p95Ms = times[count * 95 / 100] });
        }
        Console.WriteLine(JsonSerializer.Serialize(reports));
    }
}
