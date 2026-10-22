using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using DiceMaster;
using DiceMaster.Information;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;

static class GuideProbe
{
    static ConditionResultItem IsZero(FlagData flag) => new ConditionResultItem { _items = new List<StatCompareItem> { new StatCompareItem {
        _value1 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 3, _flag = flag } } },
        _value2 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = 0 } } } } } };
    static void Main(string[] args)
    {
        bool selected = args.Contains("selected");
        var results = new List<object>();
        foreach (int count in new[] { 8, 32, 64 }) foreach (int padding in new[] { 0, 4096 })
        {
            MapGuideSource.Clear(); MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = _ => true;
            DataHelper.Scripts.Clear(); DataHelper.ExternalReader = null;
            var manager = CheckPointManager.Instance = new CheckPointManager();
            for (int i = 0; i < padding; i++) manager._condition._list.Add(new ConditionResultData { name = "unused_" + i });
            var first = new PositionController(); var kept = new List<PositionController>();
            for (int i = 0; i < count; i++)
            {
                var flag = new FlagData { name = "probe_" + i };
                manager._condition._list.Add(new ConditionResultData { name = flag.name, _items = new List<ConditionResultItem> { IsZero(flag) } });
                DataHelper.Scripts["probe_root_" + i] = "local a={};a[1]=checkpointmanager.Condition('probe_" + i + "') and 'O_probe_" + i + "' or '~x';local r=choose(a);if r==1 then setcharacter(characters.Get('person'));say(luamanager.GetStoryText('D_probe')) end";
                var place = new PositionController { _positionData = new FreePositionData { name = "Place" + i, _defaultScript = "probe_root_" + i },
                    _missionsManager = first._missionsManager, _gameStatManager = first._gameStatManager, _freeManager = first._freeManager };
                kept.Add(place); MapGuideSource.Register(place);
            }
            // The fake native surface must strongly own its controllers, as Unity does.
            // Weak registration alone is deliberately not the owner.
            if (selected) MapGuideSource.Select(kept[0]);
            MapMarkerSnapshot Read()
            {
                UnityEngine.Time.unscaledTime += .25f;
#if SCHEDULED_SCOPE
                MapGuideSource.BeginScheduledReads();
                try {
#endif
                    if (selected) MapGuideSource.Discover(true, false)[0].Read();
                    return (MapMarkerSnapshot)MapGuideSource.DiscoverMarkers(true, false)[0].Read();
#if SCHEDULED_SCOPE
                } finally { MapGuideSource.EndScheduledReads(); }
#endif
            }
            int firstTick = 0, allTick = 0, coldReads = DataHelper.Reads, maxReads = 0;
            var cold = new double[300];
            for (int i = 0; i < cold.Length; i++)
            {
                int reads = DataHelper.Reads; long begin = Stopwatch.GetTimestamp(); var value = Read();
                cold[i] = (Stopwatch.GetTimestamp() - begin) * 1000.0 / Stopwatch.Frequency;
                maxReads = Math.Max(maxReads, DataHelper.Reads - reads);
                if (value.Entries.Count != 0 && firstTick == 0) firstTick = i + 1;
                if (value.Entries.Count == count && allTick == 0) allTick = i + 1;
            }
            coldReads = DataHelper.Reads - coldReads;
            int resources = DataHelper.Reads, min = count, max = 0; var times = new double[200];
            GC.Collect(); GC.WaitForPendingFinalizers();
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < times.Length; i++)
            {
                long begin = Stopwatch.GetTimestamp(); var value = Read();
                times[i] = (Stopwatch.GetTimestamp() - begin) * 1000.0 / Stopwatch.Frequency;
                min = Math.Min(min, value.Entries.Count); max = Math.Max(max, value.Entries.Count);
            }
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Array.Sort(times); Array.Sort(cold);
            results.Add(new { count, padding, selected, firstMarkerTick = firstTick, allMarkersTick = allTick,
                coldMaxReadsPerTick = maxReads, coldTotalReads = coldReads, coldP95Ms = cold[284], coldMaxMs = cold[299],
                minShown = min, maxShown = max, p95Ms = times[189], maxMs = times[199], bytesPerQuery = allocated / times.Length,
                resourceReads = DataHelper.Reads - resources });
            GC.KeepAlive(kept);
        }
        Console.WriteLine(JsonSerializer.Serialize(results));
    }
}
