using System;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LuaParse;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;
using UnityEngine;
static partial class Program
{
    private static void MarkerCases()
    {
        MapGuideSource.Clear(); OneShotProof.Clear(); MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = null;
        var first = new PositionController { _positionData = new FreePositionData { name = "Hall", _defaultScript = "never_load_hall" } };
        var second = new PositionController { _position = PositionType.Kitchen, _positionData = new FreePositionData { name = "Kitchen", _defaultScript = "never_load_kitchen" },
            _missionsManager = first._missionsManager, _gameStatManager = first._gameStatManager, _freeManager = first._freeManager };
        var pending = new TrackingMissions(); first._missionsManager.ActiveSubMissions = pending;
        var hall = new MissionCheckData { name = "first-hall", _position = PositionType.Hall };
        pending.Add("first", hall); pending.Add("later", new MissionCheckData { name = "later-hall", _position = PositionType.Hall });
        pending.Add("kitchen", new MissionCheckData { name = "first-kitchen", _position = PositionType.Kitchen });
        MapGuideSource.Register(first); MapGuideSource.Register(second);
        MapMarkerSnapshot ReadMarkers(bool once = false)
        { Time.unscaledTime += .25f; return (MapMarkerSnapshot)MapGuideSource.DiscoverMarkers(true, once)[0].Read(); }
        int resources = DataHelper.Reads, parses = LuaParser.Parses, catalogue = first._missionsManager._subMissions.ListReads;
        MapMarkerSnapshot value = null; for (int at = 0; at < 8; at++) value = ReadMarkers();
        Check(!MapGuideSource.Visible && value.Entries.Count == 2, "unselected visible places receive independent waiting markers");
        Check(value.Entries.Exists(row => ReferenceEquals(row.Anchor, first) && row.AutoText.Contains("first-hall") && !row.AutoText.Contains("later-hall")), "only native first waiting mission appears for a place");
        int enumerations = pending.Enumerations; ReadMarkers();
        Check(pending.Enumerations == enumerations + 1, "entire map enumerates live waiting membership once per snapshot");
        for (int at = 0; at < 100; at++) ReadMarkers();
        Check(DataHelper.Reads == resources && LuaParser.Parses == parses && first._missionsManager._subMissions.ListReads == catalogue, "all-place waiting markers perform zero Lua loads, parses and mission catalogue reads across one hundred ticks");
        first._gameStatManager._stats._list[0]._value = 0; value = ReadMarkers();
        Check(value.Entries.Count == 0, "action shortage hides side mission dots everywhere");
        first._missionsManager.MainMissionStart = true; first._missionsManager.MainMissionPosition = PositionType.Hall; first._missionsManager.MainCheckData = hall;
        value = ReadMarkers(); Check(value.Entries.Count == 1 && ReferenceEquals(value.Entries[0].Anchor, first), "main mission priority blocks other places and keeps native main-action exemption");
        first._missionsManager.MainMissionStart = false; first._gameStatManager._stats._list[0]._value = 3;
        first._freeManager.IsProcessing = true; Check(ReadMarkers().Entries.Count == 0, "in-progress activity cannot retain ready dots"); first._freeManager.IsProcessing = false;
        first._active = false; value = ReadMarkers(); Check(value.Entries.Count == 1 && ReferenceEquals(value.Entries[0].Anchor, second), "inactive place is omitted without erasing another place"); first._active = true;
        MapGuideSource.MarkerVisible = place => ReferenceEquals(place, first); value = ReadMarkers();
        Check(value.Entries.Count == 1 && ReferenceEquals(value.Entries[0].Anchor, first), "visibility filter excludes offscreen or occluded place geometry"); MapGuideSource.MarkerVisible = null;
        CheckPointManager.Instance = null; value = ReadMarkers(true);
        Check(value.Entries.Count == 2 && DataHelper.Reads == resources && LuaParser.Parses == parses, "one-shot ON cannot start unprepared place analysis or block waiting markers on missing checkpoint data");
        first._positionData = null; value = ReadMarkers();
        Check(value.Entries.Count == 1 && ReferenceEquals(value.Entries[0].Anchor, second), "one broken place is isolated and never retains a previous ready dot");
        Check(Counters.GameCalls == 0 && first._gameStatManager._stats._list[0]._value == 3, "marker queries do not execute game checks or spend action points");
        value = ReadMarkers(); MapGuideSource.ClearSnapshots(); Check(!MapGuideSource.IsCurrent(value), "OFF/context cleanup invalidates old map marker publications");
        MapGuideSource.Enabled = () => false; int before = pending.Enumerations;
        Check(MapGuideSource.DiscoverMarkers(true, true).Count == 0 && pending.Enumerations == before, "OFF performs no marker discovery or waiting reads");
        MapGuideSource.Clear(); MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = null;
    }
}
