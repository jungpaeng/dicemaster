using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;
using UnityEngine;

internal static partial class Program
{
    private static object GuidanceStamp(PositionController controller)
    {
        var values = (IEnumerable)typeof(MapGuideSource).GetField("places", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        foreach (object place in values)
        {
            var weak = (WeakReference)LogBridge.ReadFieldStrict(place, "Controller");
            if (!ReferenceEquals(weak.Target, controller)) continue;
            object source = LogBridge.ReadFieldStrict(place, "Definition");
            return LogBridge.ReadFieldStrict(LogBridge.ReadFieldStrict(source, "SelectedStories"), "Stamp");
        }
        return null;
    }
    private static MapMarkerSnapshot ExpiredGuidanceMarkers()
    {
        MapGuideSource.BeginScheduledReads();
        // A deterministic expired optional-work deadline, without wall-clock
        // sleeps or reliance on the speed of the machine running the suite.
        var expired = (MapRouteDefinitions.PreparationSlice)RuntimeHelpers.GetUninitializedObject(typeof(MapRouteDefinitions.PreparationSlice));
        typeof(MapGuideSource).GetField("scheduledSlice", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, expired);
        try { return GuidanceMarkers(); }
        finally { MapGuideSource.EndScheduledReads(); }
    }
    private static void StableGuidanceMarkerCases()
    {
        foreach (int count in new[] { 11, 64 })
        {
            MapGuideSource.Clear(); PreviewConditions.ClearTalkContext(); MapDefinitionAssets.Clear();
            MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = _ => true;
            DataHelper.Scripts.Clear(); DataHelper.ExternalReader = null;
            var manager = CheckPointManager.Instance = new CheckPointManager(); LuaManager.Instance = null;
            var owner = new PositionController(); var places = new List<PositionController>(); var flags = new List<FlagData>();
            for (int at = 0; at < count; at++)
            {
                for (int depth = 0; depth < 3; depth++)
                {
                    string key = "stable_" + at + "_" + depth;
                    var flag = new FlagData { name = key }; flags.Add(flag);
                    manager._condition._list.Add(new ConditionResultData { name = key, _items = new List<ConditionResultItem> { IsZero(flag) } });
                    DataHelper.Scripts[key] = "local a={};a[1]=checkpointmanager.Condition('" + key + "') and 'O_" + key + "' or '~x';local r=choose(a);if r==1 then " +
                        (depth < 2 ? "luamanager.SetNextScript('stable_" + at + "_" + (depth + 1) + "');luamanager.Init()" :
                        "setcharacter(characters.Get('person'));say(luamanager.GetStoryText('D_stable'))") + " end";
                }
                var place = new PositionController { _positionData = new FreePositionData { name = "Stable" + at, _defaultScript = "stable_" + at + "_0" },
                    _missionsManager = owner._missionsManager, _gameStatManager = owner._gameStatManager, _freeManager = owner._freeManager };
                places.Add(place); MapGuideSource.Register(place);
            }
            MapMarkerSnapshot snapshot = null; int ready = 0;
            for (int at = 0; at < 800; at++)
            {
                int reads = DataHelper.Reads, work = AllRouteWork(); snapshot = GuidanceMarkers();
                Check(DataHelper.Reads - reads <= 1 && AllRouteWork() - work <= 8192, "stable markers preserve the shared cold resource/work limits");
                if (snapshot.Entries.Count == count) { ready = at + 1; break; }
            }
            Check(ready > 0, "cached successes cannot exhaust the search budget and starve the remaining three-menu places: " + count);
            var rows = snapshot.Entries.ToDictionary(row => row.Anchor);
            var stamps = places.ToDictionary(place => place, GuidanceStamp);
            int resources = DataHelper.Reads, parses = DiceMaster.LuaParse.LuaParser.Parses;
            for (int at = 0; at < 12; at++)
            {
                snapshot = ExpiredGuidanceMarkers();
                Check(snapshot.Entries.Count == count && snapshot.Entries.All(row => ReferenceEquals(rows[row.Anchor], row)),
                    "an expired optional preparation deadline cannot remove unchanged bright markers or rebuild their rows");
                Check(places.All(place => ReferenceEquals(stamps[place], GuidanceStamp(place))),
                    "unchanged observed inputs reuse the successful read stamp instead of traversing choice/story conditions again");
            }
            Check(resources == DataHelper.Reads && parses == DiceMaster.LuaParse.LuaParser.Parses, "retained input-verified markers perform zero warm Lua reads/parses");
            flags[1].State = 1; snapshot = ExpiredGuidanceMarkers();
            Check(snapshot.Entries.Count == count - 1 && !snapshot.Entries.Any(row => ReferenceEquals(row.Anchor, places[0])),
                "a changed descendant unlock input removes its marker even with an expired preparation deadline");
            flags[1].State = 0;
            for (int at = 0; at < 8; at++) snapshot = GuidanceMarkers();
            Check(snapshot.Entries.Count == count, "restoring an unlock rediscovers the route after invalidating its old decision");
            var gate = manager._condition._list[3];
            gate._items[0]._items[0]._compareOp = 1; snapshot = ExpiredGuidanceMarkers();
            Check(!snapshot.Entries.Any(row => ReferenceEquals(row.Anchor, places[1])), "an in-place predicate change invalidates the cached decision");
            gate._items[0]._items[0]._compareOp = 0;
            for (int at = 0; at < 8; at++) snapshot = GuidanceMarkers();
            var old = manager._condition._list;
            manager._condition._list = new List<ConditionResultData>(old);
            manager._condition._list[6] = new ConditionResultData { name = "stable_2_0", _items = new List<ConditionResultItem> { IsZero(new FlagData { State = 1 }) } };
            snapshot = ExpiredGuidanceMarkers();
            Check(!snapshot.Entries.Any(row => ReferenceEquals(row.Anchor, places[2])), "same-sized definition replacement cannot reuse a different owner's predicate");
            manager._condition._list = old;
            for (int at = 0; at < 8; at++) snapshot = GuidanceMarkers();
            owner._gameStatManager._stats._list[0]._value = 0;
            Check(ExpiredGuidanceMarkers().Entries.Count == 0, "cached route decisions cannot bypass fresh action/dispatch checks");
            owner._gameStatManager._stats._list[0]._value = 3;
            MapGuideSource.MarkerVisible = place => !ReferenceEquals(place, places[0]);
            Check(!ExpiredGuidanceMarkers().Entries.Any(row => ReferenceEquals(row.Anchor, places[0])), "visibility suppresses a cached marker immediately");
            Check(Counters.GameCalls == 0, "marker read plans execute no game predicate, RNG, Lua or setters");
            GC.KeepAlive(places);
        }
        MapGuideSource.Clear(); MapGuideSource.MarkerVisible = null;
        int input = 1; bool failed = false;
        PreviewConditions.ReadStamp stamp;
        using (stamp = new PreviewConditions.ReadStamp())
            PreviewConditions.ReadStamp.Observe("test", () => { if (failed) throw new Exception(); return input; }, input);
        Check(stamp.IsCurrent(), "a read plan validates unchanged primitives"); input = 2;
        Check(!stamp.IsCurrent(), "a changed primitive invalidates the plan"); input = 1; failed = true;
        Check(!stamp.IsCurrent(), "a failed current input read never retains a cached positive decision");
        var list = new List<int> { 1 };
        using (stamp = new PreviewConditions.ReadStamp()) PreviewConditions.ReadStamp.Observe("list", () => list, list);
        list[0] = 2;
        Check(!stamp.IsCurrent(), "same-list same-count element replacement invalidates its collection revision");
        var array = new object[] { new object() };
        using (stamp = new PreviewConditions.ReadStamp()) PreviewConditions.ReadStamp.Observe("array", () => array, array);
        array[0] = new object();
        Check(!stamp.IsCurrent(), "in-place array replacement invalidates the observed reference footprint");
        using (stamp = new PreviewConditions.ReadStamp()) for (int at = 0; at < 1025; at++)
            PreviewConditions.ReadStamp.Observe("large/" + at, () => 1, 1);
        Check(!stamp.IsCurrent(), "an oversized dependency plan falls back to fresh validation instead of retaining a truncated positive");
        var time = new GameTimeData();
        var reference = new StatValueReference { _checkType = 9, _gameTime = time };
        using (stamp = new PreviewConditions.ReadStamp()) SafeDisplayEvaluation.ReadReference(reference);
        time.GameTime.Month++;
        Check(!stamp.IsCurrent(), "time changes invalidate audited dynamic values inside a reused witness");
        var stat = new GameStat(); reference = new StatValueReference { _checkType = 1, _stat = stat };
        using (stamp = new PreviewConditions.ReadStamp()) SafeDisplayEvaluation.ReadReference(reference);
        stat._value++;
        Check(!stamp.IsCurrent(), "a changed audited stat getter invalidates the decision without native condition execution");
    }
}
