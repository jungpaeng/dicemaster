using System;
using System.Linq;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;

internal static partial class Program
{
    private static void LoadingMarkerCases()
    {
        MapGuideSource.Clear(); MapDefinitionAssets.Clear(); DataHelper.Scripts.Clear(); DataHelper.ExternalReader = null;
        MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = _ => true;
        CheckPointManager.Instance = new CheckPointManager();
        DataHelper.Scripts["loading_good"] = new string(';', 16000) + "local a={'O_person'};local r=choose(a);if r==1 then setcharacter(characters.Get('guide_npc'));say(luamanager.GetStoryText('D_greeting')) end";
        DataHelper.Scripts["loading_empty"] = "local a={'leave'};choose(a)";
        var good = new PositionController { _positionData = new FreePositionData { name = "Good", _defaultScript = "loading_good" } };
        PositionController Place(string name, string script) => new PositionController { _positionData = new FreePositionData { name = name, _defaultScript = script },
            _missionsManager = good._missionsManager, _gameStatManager = good._gameStatManager, _freeManager = good._freeManager };
        var empty = Place("Empty", "loading_empty"); var failed = Place("Failed", "not_installed");
        MapGuideSource.Register(good); MapGuideSource.Register(empty); MapGuideSource.Register(failed);
        var snapshot = GuidanceMarkers();
        Check(snapshot.Preparing.Count > 0 && MapGuideSource.SelectedAnchor == null, "unfocused map publishes queued per-place loading from the marker query itself");
        bool complete = false;
        for (int at = 0; at < 180; at++)
        {
            int reads = DataHelper.Reads, work = AllRouteWork(); snapshot = GuidanceMarkers();
            Check(DataHelper.Reads - reads <= 1 && AllRouteWork() - work <= 8192, "loading state adds no source work or private per-place preparation budget");
            Check(snapshot.Preparing.All(anchor => !snapshot.Entries.Any(row => ReferenceEquals(row.Anchor, anchor) && row.HasBadge)), "a positive witness replaces loading in the same snapshot");
            if (snapshot.Entries.Any(row => ReferenceEquals(row.Anchor, good)) && snapshot.Preparing.Count == 0) { complete = true; break; }
        }
        Check(complete && !snapshot.Entries.Any(row => ReferenceEquals(row.Anchor, empty) || ReferenceEquals(row.Anchor, failed)), "settled empty and failed places stop loading without inventing badges");
        int warmed = DataHelper.Reads;
        for (int at = 0; at < 8; at++)
        {
            snapshot = GuidanceMarkers();
            Check(snapshot.Preparing.Count == 0 && snapshot.Entries.Any(row => ReferenceEquals(row.Anchor, good)), "healthy refresh never turns an existing badge back into a spinner");
        }
        Check(DataHelper.Reads == warmed, "spinner publication preserves warm cache and failure backoff");
        good._gameStatManager._stats._list[0]._value = 0; snapshot = GuidanceMarkers();
        Check(snapshot.Entries.Count == 0 && snapshot.Preparing.Count == 0, "action-blocked places have neither pending spinners nor badges");
        good._gameStatManager._stats._list[0]._value = 3;
        MapGuideSource.MarkerVisible = place => !ReferenceEquals(place, good); snapshot = GuidanceMarkers();
        Check(!snapshot.Preparing.Contains(good) && !snapshot.Entries.Any(row => ReferenceEquals(row.Anchor, good)), "offscreen or covered geometry cannot retain a loading anchor");
        MapGuideSource.Clear(); MapGuideSource.MarkerVisible = _ => true;
        var blocked = ScanFixture(40, 0); MapGuideSource.Deselect(blocked.Controller);
        complete = false;
        for (int at = 0; at < 220; at++)
        {
            snapshot = GuidanceMarkers();
            if (at > 20 && snapshot.Preparing.Count == 0) { complete = true; break; }
        }
        Check(complete && snapshot.Entries.Count == 0, "a multi-budget negative scan eventually settles instead of spinning on every circular recheck");
        blocked.Flags[30].State = 0;
        for (int at = 0; at < 80 && snapshot.Entries.Count == 0; at++) snapshot = GuidanceMarkers();
        Check(snapshot.Entries.Count == 1 && snapshot.Preparing.Count == 0, "settled negative loading state is not an availability cache: a newly unlocked path still appears");
        Check(Counters.GameCalls == 0, "all loading transitions remain display-only");
        MapGuideSource.Clear(); MapGuideSource.MarkerVisible = null; MapDefinitionAssets.Clear();
    }
}
