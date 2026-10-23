using System;
using System.Collections;
using System.Reflection;
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
        if (args.Contains("scheduling")) { Scheduling(); return; }
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
    // Same provider calls for the Git baseline and the working tree. Virtual
    // .25-second ticks measure scheduling opportunities, not Windows latency.
    static void Scheduling()
    {
        Run(1, false); // JIT warm-up outside reported cases
        var rows = new List<object>();
        foreach (int count in new[] { 1, 8, 16 }) foreach (bool selected in new[] { false, true })
            rows.Add(Run(count, selected));
        foreach (int count in new[] { 1, 4, 8 }) rows.Add(RunBlocked(count));
        rows.Add(Lifecycle());
        Console.WriteLine(JsonSerializer.Serialize(rows));
    }
    static object Lifecycle()
    {
        MapGuideSource.Clear(); MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = _ => true;
        var keep = new List<PositionController>();
        for (int visit = 0; visit < 100; visit++)
        {
            foreach (var controller in keep) controller.Destroyed = true;
            keep.Clear(); MapGuideSource.ClearSnapshots();
            var owner = new PositionController();
            for (int at = 0; at < 11; at++)
            {
                var controller = new PositionController { _positionData = new FreePositionData { name = "Place" + at, _defaultScript = "" },
                    _missionsManager = owner._missionsManager, _gameStatManager = owner._gameStatManager, _freeManager = owner._freeManager };
                keep.Add(controller); MapGuideSource.Register(controller);
            }
        }
        var all = (IList)typeof(MapGuideSource).GetField("places", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        int registered = all.Count;
        for (int at = 0; at < 20; at++) MapGuideSource.DiscoverMarkers(true, false)[0].Read();
        var times = new double[1000]; long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int at = 0; at < times.Length; at++)
        {
            long begin = Stopwatch.GetTimestamp(); MapGuideSource.DiscoverMarkers(true, false)[0].Read();
            times[at] = (Stopwatch.GetTimestamp() - begin) * 1000.0 / Stopwatch.Frequency;
        }
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated; Array.Sort(times);
        GC.KeepAlive(keep);
        return new { test = "lifecycle-100-visits", registered, live = keep.Count, p50Ms = times[499], p95Ms = times[949], bytesPerQuery = allocated / times.Length };
    }

    static object Field(object value, string key) => value.GetType()
        .GetField(key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(value);
    static MapRouteDefinitions Routes(PositionController controller)
    {
        var places = (IEnumerable)typeof(MapGuideSource).GetField("places", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        foreach (var place in places)
            if (ReferenceEquals(((WeakReference)Field(place, "Controller")).Target, controller))
            { var source = Field(place, "Definition"); return source == null ? null : (MapRouteDefinitions)Field(source, "Routes"); }
        return null;
    }
    static void ResetScheduling()
    {
        MapGuideSource.Clear(); MapDefinitionAssets.Clear(); DiceMaster.LiveLog.PreviewConditions.ClearTalkContext();
        MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = _ => true;
        DataHelper.Scripts.Clear(); DataHelper.ExternalReader = null; UnityEngine.Time.unscaledTime = 0;
        CheckPointManager.Instance = new CheckPointManager();
    }
    static void SchedulingGate(string key) => CheckPointManager.Instance._condition._list.Add(
        new ConditionResultData { name = key, _items = new List<ConditionResultItem> { IsZero(new FlagData { name = key }) } });
    static PositionController SchedulingPlace(PositionController owner, string script)
    {
        var controller = new PositionController { _positionData = new FreePositionData { name = script, _defaultScript = script },
            _missionsManager = owner._missionsManager, _gameStatManager = owner._gameStatManager, _freeManager = owner._freeManager };
        MapGuideSource.Register(controller); return controller;
    }
    static MapMarkerSnapshot SchedulingRead(bool selected)
    {
        UnityEngine.Time.unscaledTime += .25f; MapGuideSource.BeginScheduledReads();
        try
        {
            if (selected) MapGuideSource.Discover(true, true)[0].Read();
            return (MapMarkerSnapshot)MapGuideSource.DiscoverMarkers(true, true)[0].Read();
        }
        finally { MapGuideSource.EndScheduledReads(); }
    }
    static string SchedulingHash(MapMarkerSnapshot value)
    {
        string canonical = string.Join("\n", value.Entries.Select(row => row.Title + "|" + row.Kind + "|" + row.Status + "|" +
            row.AutoText + "|" + row.Available + "|" + row.Opportunity + "|" + row.Guidance).OrderBy(row => row, StringComparer.Ordinal));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
    const string SchedulingTalk = "setcharacter(characters.Get('person'));say(luamanager.GetStoryText('D_probe'))";
    static string SchedulingChoice(string key, string body) => "local a={};a[1]=checkpointmanager.Condition('" + key +
        "') and 'O_" + key + "' or '~x';local r=choose(a);if r==1 then " + body + " end";
    static object Run(int branches, bool selected)
    {
        ResetScheduling(); var owner = new PositionController();
        var root = new StringBuilder("local a={};");
        for (int at = 1; at <= branches; at++)
        {
            string key = "focus_gate_" + at; SchedulingGate(key);
            root.Append("a[" + at + "]=checkpointmanager.Condition('" + key + "') and 'O_" + key + "' or '~x';");
        }
        root.Append("local r=choose(a);");
        for (int at = 1; at <= branches; at++)
        {
            root.Append((at == 1 ? "if " : "elseif ") + "r==" + at + " then ");
            root.Append(at == 1 ? SchedulingTalk : "luamanager.SetNextScript('tail_" + at + "');luamanager.Init()");
            root.Append(' '); DataHelper.Scripts["tail_" + at] = SchedulingTalk;
        }
        DataHelper.Scripts["focus"] = root.Append("end").ToString();
        SchedulingGate("other_gate"); DataHelper.Scripts["other"] = SchedulingChoice("other_gate", SchedulingTalk);
        var focus = SchedulingPlace(owner, "focus"); var other = SchedulingPlace(owner, "other");
        if (selected) MapGuideSource.Select(focus);
        int focusAt = 0, otherAt = 0, completeAt = 0, maxReads = 0;
        var trace = new List<object>(); var cost = new double[70]; MapMarkerSnapshot value = null;
        for (int tick = 1; tick <= cost.Length; tick++)
        {
            int reads = DataHelper.Reads; long begin = Stopwatch.GetTimestamp(); value = SchedulingRead(selected);
            cost[tick - 1] = (Stopwatch.GetTimestamp() - begin) * 1000.0 / Stopwatch.Frequency;
            maxReads = Math.Max(maxReads, DataHelper.Reads - reads);
            if (focusAt == 0 && value.Entries.Any(row => ReferenceEquals(row.Anchor, focus) && row.Guidance)) focusAt = tick;
            if (otherAt == 0 && value.Entries.Any(row => ReferenceEquals(row.Anchor, other) && row.Guidance)) otherAt = tick;
            var a = Routes(focus); var b = Routes(other);
            if (completeAt == 0 && a?.HasPendingWork == false) completeAt = tick;
            if (tick <= 25) trace.Add(new { tick, reads = DataHelper.Reads - reads, focusMarker = focusAt != 0, otherMarker = otherAt != 0,
                focusPending = a?.HasPendingWork, focusReads = a?.ScriptReadCount, focusRoot = a?.RootMenuComplete, otherReads = b?.ScriptReadCount });
        }
        Array.Sort(cost); string displayHash = SchedulingHash(value);
        GC.KeepAlive(focus); GC.KeepAlive(other);
        return new { branches, selected, focusAt, otherAt, completeAt, maxReads, p95Ms = cost[66], maximumMs = cost[69], displayHash, trace };
    }
    static object RunBlocked(int count)
    {
        ResetScheduling(); var owner = new PositionController(); var keep = new List<PositionController>();
        for (int at = 0; at < count; at++) keep.Add(SchedulingPlace(owner, "missing_" + at));
        for (int depth = 0; depth < 3; depth++)
        {
            string key = "good_" + depth; SchedulingGate(key);
            DataHelper.Scripts[key] = SchedulingChoice(key, depth < 2 ? "luamanager.SetNextScript('good_" + (depth + 1) + "');luamanager.Init()" : SchedulingTalk);
        }
        var good = SchedulingPlace(owner, "good_0"); keep.Add(good);
        int first = 0, maxReads = 0, idle = 0; var trace = new List<object>(); MapMarkerSnapshot value = null;
        for (int tick = 1; tick <= 30; tick++)
        {
            int reads = DataHelper.Reads; value = SchedulingRead(false); reads = DataHelper.Reads - reads;
            maxReads = Math.Max(maxReads, reads);
            if (first == 0 && reads == 0) idle++;
            if (first == 0 && value.Entries.Any(row => ReferenceEquals(row.Anchor, good) && row.Guidance)) first = tick;
            if (tick <= 25) trace.Add(new { tick, reads, goodReads = Routes(good)?.ScriptReadCount, found = first > 0 });
        }
        string displayHash = SchedulingHash(value); GC.KeepAlive(keep);
        return new { test = "failed-prerequisite-queued", count, first, idleBeforeFirst = idle, maxReads, displayHash, trace };
    }
}

#if GUIDANCE_NATIVE_DEFINITIONS
namespace Mortal.Core { public sealed class FlagCollectionData : UnityEngine.Object { public System.Collections.Generic.List<FlagData> _list = new System.Collections.Generic.List<FlagData>(); } }
namespace Mortal.Story { public sealed class StatModifyManager : UnityEngine.Object { public Mortal.Core.FlagCollectionData _talkOptionFlag; } }
namespace UnityEngine { public static class Resources { public static T[] FindObjectsOfTypeAll<T>() where T : Object { return System.Array.Empty<T>(); } } }
#endif
