using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;
using UnityEngine;

internal static partial class Program
{
    private static MapMarkerSnapshot GuidanceMarkers(bool guide = true, bool once = false)
    { Time.unscaledTime += .25f; return (MapMarkerSnapshot)MapGuideSource.DiscoverMarkers(guide, once)[0].Read(); }
    private static int AllRouteWork()
    {
        int total = 0;
        foreach (var place in (IEnumerable)typeof(MapGuideSource).GetField("places", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null))
        { var source = Private(place, "Definition"); if (source != null && Private(source, "Routes") is MapRouteDefinitions graph) total += graph.WorkCount; }
        return total;
    }
    private static void GuidanceMarkerCases()
    {
        MapGuideSource.Clear(); DataHelper.Scripts.Clear(); DataHelper.ExternalReader = null; PreviewConditions.ClearTalkContext();
        MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = _ => true;
        var manager = CheckPointManager.Instance = new CheckPointManager();
        var first = new PositionController(); var places = new List<PositionController>(); var flags = new List<FlagData>();
        for (int at = 0; at < 8; at++)
        {
            var flag = new FlagData { name = "guide_flag_" + at }; flags.Add(flag);
            manager._condition._list.Add(new ConditionResultData { name = "guide_gate_" + at, _items = new List<ConditionResultItem> { IsZero(flag) } });
            DataHelper.Scripts["guide_root_" + at] = "local a={'O_parent_" + at + "'};local r=choose(a);if r==1 then luamanager.SetNextScript('guide_tail_" + at + "');luamanager.Init() end";
            DataHelper.Scripts["guide_tail_" + at] = "local a={};a[1]=checkpointmanager.Condition('guide_gate_" + at + "') and 'O_topic_" + at + "' or '~x';local r=choose(a);if r==1 then setcharacter(characters.Get('guide_person'));say(luamanager.GetStoryText('D_topic_" + at + "')) end";
            var controller = new PositionController { _position = (PositionType)(at + 10), _positionData = new FreePositionData { name = "GuidePlace" + at, _defaultScript = "guide_root_" + at },
                _missionsManager = first._missionsManager, _gameStatManager = first._gameStatManager, _freeManager = first._freeManager };
            places.Add(controller); MapGuideSource.Register(controller);
        }
        MapMarkerSnapshot snapshot = null; int readyAt = 0;
        for (int at = 0; at < 160; at++)
        {
            int reads = DataHelper.Reads, work = AllRouteWork(); snapshot = GuidanceMarkers();
            Check(DataHelper.Reads - reads <= 1 && AllRouteWork() - work <= 8192,
                "all unselected places share one cold Lua read and one 8192-work preparation slot per marker query");
            Check(snapshot.Entries.All(row => row.Guidance && row.HasBadge && !row.Opportunity && !row.Available),
                "unfocused route markers have a distinct guidance bit, never opportunity or available evidence");
            if (snapshot.Entries.Count == places.Count) { readyAt = at + 1; break; }
        }
        Check(readyAt > 1 && readyAt < 160 && MapGuideSource.SelectedAnchor == null,
            "every registered visible place eventually gains an eligible route marker without selecting or entering any place");
        Check(snapshot.Entries.All(row => row.AutoText.Contains("O_topic_", StringComparison.Ordinal)), "background marker rows retain the corresponding verified route for native-focus or mouse tooltip display");
        int resources = DataHelper.Reads, parses = DiceMaster.LuaParse.LuaParser.Parses;
        for (int at = 0; at < 20; at++) snapshot = GuidanceMarkers();
        Check(snapshot.Entries.Count == 8 && resources == DataHelper.Reads && parses == DiceMaster.LuaParse.LuaParser.Parses,
            "warm all-place guidance revalidates current conditions without Lua resource reload or reparse");
        flags[2].State = 1; snapshot = GuidanceMarkers();
        Check(!snapshot.Entries.Any(row => ReferenceEquals(row.Anchor, places[2])) && snapshot.Entries.Count == 7,
            "a newly locked descendant removes its unselected-place guide marker on the next publication");
        flags[2].State = 0;
        for (int at = 0; at < 20; at++) snapshot = GuidanceMarkers();
        Check(snapshot.Entries.Count == 8, "a new unlock is rediscovered without retaining false results");
        MapGuideSource.MarkerVisible = controller => !ReferenceEquals(controller, places[1]);
        snapshot = GuidanceMarkers();
        Check(snapshot.Entries.Count == 7 && !snapshot.Entries.Any(row => ReferenceEquals(row.Anchor, places[1])),
            "an off-screen or covered native place cannot retain its previous bright marker");
        MapGuideSource.MarkerVisible = _ => true;
        DataHelper.Scripts["empty_guide"] = "local a={'leave'};choose(a)";
        var empty = new PositionController { _positionData = new FreePositionData { name = "Empty", _defaultScript = "empty_guide" },
            _missionsManager = first._missionsManager, _gameStatManager = first._gameStatManager, _freeManager = first._freeManager };
        MapGuideSource.Register(empty);
        for (int at = 0; at < 20; at++) snapshot = GuidanceMarkers();
        Check(snapshot.Entries.Count == 8 && !snapshot.Entries.Any(row => ReferenceEquals(row.Anchor, empty)),
            "a prepared place with no dialogue publishes no marker or pending placeholder");
        first._gameStatManager._stats._list[0]._value = 0;
        Check(GuidanceMarkers().Entries.Count == 0, "insufficient action suppresses every unreachable route marker");
        first._gameStatManager._stats._list[0]._value = 3;
        var waiting = new MissionCheckData { _position = places[0]._position, _startScript = "registered_waiting" };
        first._missionsManager.ActiveSubMissions.Add("first", waiting);
        snapshot = GuidanceMarkers();
        var gold = snapshot.Entries.Single(row => ReferenceEquals(row.Anchor, places[0]));
        Check(gold.Opportunity && !gold.Guidance && snapshot.Entries.Count(row => ReferenceEquals(row.Anchor, places[0])) == 1,
            "a real registered opportunity takes precedence and never stacks a second light guide marker on the same place");
        first._missionsManager.ActiveSubMissions.Clear();
        places[3]._positionData = null; snapshot = GuidanceMarkers();
        Check(!snapshot.Entries.Any(row => ReferenceEquals(row.Anchor, places[3])) && snapshot.Entries.Count >= 6,
            "one broken place cannot retain an old light marker or fail all other place publications");
        resources = DataHelper.Reads;
        for (int at = 0; at < 10; at++) snapshot = GuidanceMarkers(false, true);
        Check(snapshot.Entries.All(row => !row.Guidance) && resources == DataHelper.Reads,
            "map guide OFF preserves one-shot behavior and starts no background story preparation");
        MapGuideSource.Enabled = () => false;
        Check(MapGuideSource.DiscoverMarkers(true, false).Count == 0 && resources == DataHelper.Reads, "both information features OFF admit no new background query");
        Check(Counters.GameCalls == 0 && flags.All(flag => flag.State == 0) && first._gameStatManager._stats._list[0]._value == 3,
            "background discovery never executes Lua/game checks, changes persistent inputs or consumes action points");
        GC.KeepAlive(empty); GC.KeepAlive(places);
        MapGuideSource.Clear(); MapGuideSource.MarkerVisible = null; MapGuideSource.Enabled = () => true;
        // The same explicit scheduling scope used by InformationRuntime owns
        // both selected and background preparation, including their cold reads.
        DataHelper.Scripts.Clear(); DataHelper.Scripts["shared_slow"] = new string(';', 100000) + "local a={'leave'};choose(a)";
        DataHelper.Scripts["shared_other"] = "local a={'leave'};choose(a)";
        var selected = new PositionController { _positionData = new FreePositionData { name = "Selected", _defaultScript = "shared_slow" } };
        var other = new PositionController { _positionData = new FreePositionData { name = "Other", _defaultScript = "shared_other" },
            _missionsManager = selected._missionsManager, _gameStatManager = selected._gameStatManager, _freeManager = selected._freeManager };
        MapGuideSource.Select(selected); MapGuideSource.Register(other);
        for (int at = 0; at < 24; at++)
        {
            Time.unscaledTime += .25f; resources = DataHelper.Reads; int work = AllRouteWork();
            MapGuideSource.BeginScheduledReads();
            try { MapGuideSource.Discover(true, false)[0].Read(); MapGuideSource.DiscoverMarkers(true, false)[0].Read(); }
            finally { MapGuideSource.EndScheduledReads(); }
            Check(DataHelper.Reads - resources <= 1 && AllRouteWork() - work <= 8192,
                "a selected place plus all background places cannot multiply the cold preparation/resource budget in a single scheduled tick");
        }
        MapGuideSource.Clear();
        SharedGuidanceDefinitions();
        PreparationFairness();
    }
    private static void PreparationFairness()
    {
        MapGuideSource.Clear(); DataHelper.Scripts.Clear(); DataHelper.ExternalReader = null;
        MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = _ => true;
        CheckPointManager.Instance = new CheckPointManager();
        DataHelper.Scripts["long_peer"] = new string(';', 120000) + "local a={'leave'};choose(a)";
        DataHelper.Scripts["short_peer"] = "local a={'talk'};local r=choose(a);if r==1 then setcharacter(characters.Get('peer'));say(luamanager.GetStoryText('D_peer')) end";
        var slow = new PositionController { _positionData = new FreePositionData { name = "LongPeer", _defaultScript = "long_peer" } };
        var fast = new PositionController { _positionData = new FreePositionData { name = "ShortPeer", _defaultScript = "short_peer" },
            _missionsManager = slow._missionsManager, _gameStatManager = slow._gameStatManager, _freeManager = slow._freeManager };
        MapGuideSource.Register(slow); MapGuideSource.Register(fast); bool shown = false;
        for (int at = 0; at < 12 && !shown; at++)
        {
            int reads = DataHelper.Reads, work = AllRouteWork();
            MapGuideSource.BeginScheduledReads();
            try { shown = GuidanceMarkers().Entries.Any(row => ReferenceEquals(row.Anchor, fast)); }
            finally { MapGuideSource.EndScheduledReads(); }
            Check(DataHelper.Reads - reads <= 1 && AllRouteWork() - work <= 8192, "short continuation bursts preserve the shared read/work ceiling");
        }
        Check(shown, "a long lexical preparation yields to an independent short story instead of monopolizing background work");
        GC.KeepAlive(slow); GC.KeepAlive(fast); MapGuideSource.Clear(); MapGuideSource.MarkerVisible = null;
    }
    private static void SharedGuidanceDefinitions()
    {
        var manager = CheckPointManager.Instance = new CheckPointManager();
        var flag = new FlagData { name = "shared_definition_input", State = 0 };
        manager._condition._list.Add(new ConditionResultData { name = "shared_condition", _items = new List<ConditionResultItem> { IsZero(flag) } });
        var gate = PreviewConditions.Parse("checkpointmanager.Condition('shared_condition')");
        var scope = new PreviewConditions.DefinitionScope();
        var zero = new FreePositionData { _conditionFlags = new[] { Assign(IsZero(flag), flag, 0, 0) } };
        var one = new FreePositionData { _conditionFlags = new[] { Assign(IsZero(flag), flag, 1, 1) } };
        Check(MapRouteRead.ForSelectedPlace(zero, scope).Current(gate) == PreviewConditionState.Met &&
            MapRouteRead.ForSelectedPlace(one, scope).Current(gate) == PreviewConditionState.NotMet,
            "shared definition names never share per-place virtual entry values or truth between two places");
        flag.State = 1;
        Check(MapRouteRead.ForSelectedPlace(new FreePositionData(), scope).Current(gate) == PreviewConditionState.NotMet,
            "another reader sharing definitions still reads today's actual flag instead of another reader's cached truth");
        manager._condition._list = new List<ConditionResultData> { new ConditionResultData { name = "shared_condition", _items = new List<ConditionResultItem> { FlagEquals(flag, 1) } } };
        Check(MapRouteRead.ForSelectedPlace(new FreePositionData(), scope).Current(gate) == PreviewConditionState.Met,
            "replacement of a same-sized serialized definition list invalidates the shared name index");
        Check(flag.StateWrites == 2 && Counters.GameCalls == 0,
            "sharing metadata changes no game state beyond the two explicit fixture assignments");
        scope.Reset();
        Check(scope.Indices.Count == 0, "the end of a publication drops all configuration/name references and retains only empty reusable buffers");
    }
}
