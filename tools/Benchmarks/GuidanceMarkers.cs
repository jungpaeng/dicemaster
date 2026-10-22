using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
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
        if (args.Contains("definitions")) { Definitions(); return; }
        if (args.Contains("metadata")) { Metadata(); return; }
        bool selected = args.Contains("selected");
        bool deep = args.Contains("deep");
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
                int depth = deep ? 3 : 1;
                for (int step = 0; step < depth; step++)
                {
                    string key = "probe_" + i + "_" + step;
                    var flag = new FlagData { name = key };
                    manager._condition._list.Add(new ConditionResultData { name = key, _items = new List<ConditionResultItem> { IsZero(flag) } });
                    string next = step + 1 < depth ? "luamanager.SetNextScript('probe_tail_" + i + "_" + (step + 1) + "');luamanager.Init()" :
                        "setcharacter(characters.Get('person'));say(luamanager.GetStoryText('D_probe'))";
                    DataHelper.Scripts[step == 0 ? "probe_root_" + i : "probe_tail_" + i + "_" + step] =
                        "local a={};a[1]=checkpointmanager.Condition('" + key + "') and 'O_" + key + "' or '~x';local r=choose(a);if r==1 then " + next + " end";
                }
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
            MapMarkerSnapshot last = null;
            for (int i = 0; i < times.Length; i++)
            {
                long begin = Stopwatch.GetTimestamp(); var value = Read();
                last = value;
                times[i] = (Stopwatch.GetTimestamp() - begin) * 1000.0 / Stopwatch.Frequency;
                min = Math.Min(min, value.Entries.Count); max = Math.Max(max, value.Entries.Count);
            }
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Array.Sort(times); Array.Sort(cold);
            string canonical = string.Join("\n", last.Entries.Select(row => row.Title + "|" + row.Kind + "|" + row.Status + "|" + row.AutoText + "|" + row.Available + "|" + row.Opportunity + "|" + row.Guidance).OrderBy(text => text, StringComparer.Ordinal));
            string displayHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
            results.Add(new { count, padding, selected, deep, firstMarkerTick = firstTick, allMarkersTick = allTick,
                coldMaxReadsPerTick = maxReads, coldTotalReads = coldReads, coldP95Ms = cold[284], coldMaxMs = cold[299],
                minShown = min, maxShown = max, p95Ms = times[189], maxMs = times[199], bytesPerQuery = allocated / times.Length,
                resourceReads = DataHelper.Reads - resources, displayHash });
            GC.KeepAlive(kept);
        }
        Console.WriteLine(JsonSerializer.Serialize(results));
    }
    static void Definitions()
    {
        string directory = Environment.GetEnvironmentVariable("DICEMASTER_MAP_GAME_DATA");
        if (string.IsNullOrEmpty(directory)) throw new Exception("Supply read-only actual game data for definition measurements.");
        var rows = new List<object>();
        for (int i = 0; i < 6; i++)
        {
            GC.Collect(); GC.WaitForPendingFinalizers();
            long before = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
            using var index = typeof(MapFileDefinitions).Assembly.GetManifestResourceStream("DiceMaster.MapDefinitions.index");
            var roots = MapDefinitionFileReader.Read(directory, index, new Dictionary<string, object>(), System.Threading.CancellationToken.None, out long bytes);
            double ms = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            // Include the worst case too: laziness must not hide the cost of
            // asking for EVERY definition after setup. Normal map queries ask
            // for a small subset, but this exposes the deferred work separately.
            int nodes = 0; var visited = new HashSet<object>();
            var templateField = typeof(DiceMaster.LiveLog.DisplayDefinition).GetField("template", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Action<object> walk = null;
            walk = value => {
                if (value == null || !visited.Add(value)) return;
                if (value is DiceMaster.LiveLog.DisplayDefinition definition)
                {
                    nodes++;
                    var source = templateField?.GetValue(definition) as DiceMaster.LiveLog.DisplayDefinition ?? definition;
                    foreach (string key in source.Fields.Keys) walk(definition.Read(key));
                }
                else if (value is System.Collections.IEnumerable list && !(value is string)) foreach (object item in list) walk(item);
            };
            walk(roots);
            double fullMs = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            long fullAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
            rows.Add(new { load = i, ms, allocated, fullMs, fullAllocated, materializedNodes = nodes, hashedBytes = bytes, conditions = roots[0].Length, switches = roots[1].Length });
        }
        Console.WriteLine(JsonSerializer.Serialize(rows));
    }
    static void Metadata()
    {
        var type = typeof(DiceMaster.LiveLog.PreviewConditions).GetNestedType("Snapshot", System.Reflection.BindingFlags.NonPublic);
        var method = type.GetMethod("Field", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var read = (Func<object, string, object>)method.CreateDelegate(typeof(Func<object, string, object>));
        var value = new StatValueReference { _constant = 7 };
        var rows = new List<object>();
        for (int round = 0; round < 6; round++)
        {
            for (int i = 0; i < 10000; i++) read(value, "_constant");
            const int count = 600000; long sum = 0, start = Stopwatch.GetTimestamp(), bytes = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < count; i++) sum += (int)read(value, "_constant");
            double ms = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
            value._constant++; bool fresh = (int)read(value, "_constant") == value._constant;
            rows.Add(new { round, count, ms, bytes, sum, fresh });
        }
        Console.WriteLine(JsonSerializer.Serialize(rows));
    }
}

#if GUIDANCE_NATIVE_DEFINITIONS
namespace Mortal.Core { public sealed class FlagCollectionData : UnityEngine.Object { public System.Collections.Generic.List<FlagData> _list = new System.Collections.Generic.List<FlagData>(); } }
namespace Mortal.Story { public sealed class StatModifyManager : UnityEngine.Object { public Mortal.Core.FlagCollectionData _talkOptionFlag; } }
namespace UnityEngine { public static class Resources { public static T[] FindObjectsOfTypeAll<T>() where T : Object { return System.Array.Empty<T>(); } } }
#endif
