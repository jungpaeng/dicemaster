using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;
using UnityEngine;

internal static partial class Program
{
    private static List<DisplayDefinition> CalendarDefinitions(object[][] roots)
    {
        var result = new List<DisplayDefinition>(); var visited = new HashSet<object>();
        void Visit(object value)
        {
            if (value == null || !visited.Add(value)) return;
            if (value is DisplayDefinition definition)
            { result.Add(definition); foreach (object child in definition.Fields.Values) Visit(child); }
            else if (value is IList list) foreach (object child in list) Visit(child);
        }
        Visit(roots); return result;
    }
    private static int CalendarSection(byte[] index)
    {
        using (var r = new BinaryReader(new MemoryStream(index)))
        {
            r.ReadBytes(4); int files = r.ReadInt32();
            for (int i = 0; i < files; i++) { r.ReadString(); r.ReadBytes(40); int n = r.ReadInt32(); r.ReadBytes(n * 4); }
            int types = r.ReadInt32(); for (int i = 0; i < types; i++) r.ReadString();
            int entries = r.ReadInt32(); r.ReadBytes(entries * 22);
            int roots = r.ReadInt32(); r.ReadBytes(roots * 9); return (int)r.BaseStream.Position;
        }
    }
    private static void FileCalendarCases()
    {
        string data = Environment.GetEnvironmentVariable("DICEMASTER_MAP_GAME_DATA");
        if (string.IsNullOrEmpty(data)) return;
        MapGuideSource.Clear(); MapDefinitionAssets.Clear(); PreviewConditions.ClearTalkContext(); Resources.Loaded.Clear();
        MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = _ => true;
        CheckPointManager.Instance = null; LuaManager.Instance = null;
        var firstTalk = new FlagData { name = "Talk_Brother2_00" };
        var flags = new[] { firstTalk, new FlagData { name = "Talk_Brother4" }, new FlagData { name = "Talk_Brother4_00" }, new FlagData { name = "E10005" } };
        var alchemy = new PositionController { _positionData = new FreePositionData { name = "ActualAlchemy", _defaultScript = "alchemy_001" } };
        alchemy._missionsManager._flagsCollection = new object[] { new FlagCollectionData { _list = flags.ToList() } };
        var now = alchemy._gameStatManager._gameTimeData; now.name = "PlayerGameTime"; now.GameTime.Month = 4;
        var room = new PositionController { _positionData = new FreePositionData { name = "ActualRoom", _defaultScript = "room_001",
            _conditionFlags = new[] { Assign(FlagEquals(flags[3], 0), flags[1], 1, 0) } },
            _missionsManager = alchemy._missionsManager, _gameStatManager = alchemy._gameStatManager, _freeManager = alchemy._freeManager };
        DataHelper.Scripts.Clear(); DataHelper.ExternalReader = null;
        using (var corpus = JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("DICEMASTER_CORPUS_INDEX"))))
            foreach (var row in corpus.RootElement.EnumerateArray()) DataHelper.Scripts[row.GetProperty("name").GetString()] = File.ReadAllText(row.GetProperty("path").GetString());
        byte[] index;
        using (Stream stream = typeof(MapFileDefinitions).Assembly.GetManifestResourceStream("DiceMaster.MapDefinitions.index"))
        using (var copy = new MemoryStream()) { stream.CopyTo(copy); index = copy.ToArray(); }
        var bindings = MapFileDefinitions.Bindings(alchemy._missionsManager, alchemy._gameStatManager);
        object[][] Load(byte[] profile) => MapDefinitionFileReader.Read(data, new MemoryStream(profile), bindings, CancellationToken.None, out _);
        object[][] roots = null;
        var gate = PreviewConditions.Parse("checkpointmanager.Condition('Talk_Brother2_00')");
        PreviewConditionState State() => PreviewConditions.CreateRouteReader().Current(gate);
        MapMarkerSnapshot Poll(int count) { MapMarkerSnapshot value = null; for (int i = 0; i < count; i++) value = GuidanceMarkers(); return value; }
        bool Has(MapMarkerSnapshot value, PositionController anchor) => value.Entries.Any(row => ReferenceEquals(row.Anchor, anchor) && row.Guidance);
        try
        {
            int section = CalendarSection(index); byte[] noCalendars = new byte[section + 4]; Array.Copy(index, noCalendars, section);
            roots = Load(noCalendars);
            PreviewConditions.LoadedDefinitionReader = kind => roots[(int)kind];
            PreviewConditions.ExistingOwnerAlive = value => value is UnityEngine.Object native && native != null;
            MapGuideSource.Register(alchemy); MapGuideSource.Register(room);
            var snapshot = Poll(300);
            Check(State() == PreviewConditionState.Unknown && !Has(snapshot, alchemy) && Has(snapshot, room),
                "actual alchemy Lua/gate reproduce alpha.33 missing cutoff: cold map loses only alchemy while room guidance works");
            roots = Load(index); MapGuideSource.ClearSnapshots(); snapshot = Poll(300);
            Check(State() == PreviewConditionState.Met && Has(snapshot, alchemy) && Has(snapshot, room) && MapGuideSource.SelectedAnchor == null,
                "verified fixed calendar recovers actual alchemy first-talk guidance before selecting/entering any place");
            Check(CheckPointManager.Instance == null && LuaManager.Instance == null && Resources.Loaded.Count == 0 && firstTalk.State == 0,
                "recovery needs no Story scene/config assets, work action, native first-talk state mutation or place visit");
            var definitions = CalendarDefinitions(roots);
            object TimeReference(string group) => ((DisplayDefinition)((IList)definitions.Single(d => d.TypeName == "Mortal.Core.StatGroupVariable" && d.Name == group).Read("_value"))[0]).Read("_gameTime");
            var cutoff = (DisplayDefinition)TimeReference("時間_第一年五月下旬"); var cutoffTime = (DisplayDefinition)cutoff.Read("GameTime");
            Check(cutoff.Name == "第一年五月下旬" && (int)cutoffTime.Read("Year") == 1 && (int)cutoffTime.Read("Month") == 5 && (int)cutoffTime.Read("Stage") == 3,
                "private fixed date reads exact installation payload; it is absent from current player/mission bindings");
            var fixedKeys = new HashSet<string>();
            using (var r = new BinaryReader(new MemoryStream(index)))
            { r.BaseStream.Position = section; int count = r.ReadInt32(); for (int i = 0; i < count; i++) fixedKeys.Add(r.ReadByte() + ":" + r.ReadInt64()); }
            Check(!bindings.ContainsKey("Mortal.Core.GameTimeData/第一年五月下旬") && ReferenceEquals(TimeReference("時間_目前時間"), now) &&
                !fixedKeys.Contains("0:6686") && Enumerable.Range(6622, 18).All(id => !fixedKeys.Contains("0:" + id)),
                "current clock keeps its native identity and all 18 serialized mission times are excluded from calendar fallback");
            int reads = DataHelper.Reads, parses = DiceMaster.LuaParse.LuaParser.Parses;
            for (int i = 0; i < 80; i++) Check(Has(GuidanceMarkers(), alchemy), "unchanged current inputs retain the calendar-backed guidance marker");
            Check(DataHelper.Reads == reads && DiceMaster.LuaParse.LuaParser.Parses == parses,
                "warm calendar-backed guidance does not reload or reparse Lua");
            now.GameTime.Month = 5; now.GameTime.Stage = 3; snapshot = GuidanceMarkers();
            Check(State() == PreviewConditionState.Met && Has(snapshot, alchemy), "actual <= gate includes the final allowed round");
            now.GameTime.Month = 6; now.GameTime.Stage = 1; snapshot = GuidanceMarkers();
            Check(State() == PreviewConditionState.NotMet && !Has(snapshot, alchemy), "deadline passage invalidates a positive marker on the next read without stale cached truth");
            now.GameTime.Month = 4; snapshot = Poll(40);
            Check(Has(snapshot, alchemy), "restoring an earlier current time rediscovers the calendar-backed route");
            firstTalk.State = 1; snapshot = GuidanceMarkers();
            Check(State() == PreviewConditionState.NotMet && !Has(snapshot, alchemy), "first-talk completion removes actual alchemy guidance without keeping a repeat-activity marker");
            firstTalk.State = 0; snapshot = Poll(40);
            Check(Has(snapshot, alchemy) && DiceMaster.LuaParse.LuaParser.Parses == parses,
                "date/completion changes restore the marker without invoking the full Lua parser; other path preparation retains its existing budget/retry contract");
            Check(PreviewConditions.CreateReader().Current(gate) == PreviewConditionState.Unknown,
                "fixed calendar file recovery does not relax strict one-shot/opportunity proof");
            bindings.Remove("Mortal.Core.GameTimeData/PlayerGameTime"); roots = Load(index);
            Check(State() == PreviewConditionState.Unknown, "missing current clock stays unknown even when its disk default would pass the deadline");
            bindings["Mortal.Core.GameTimeData/PlayerGameTime"] = now;
            bindings["Mortal.Core.GameTimeData/第一年五月下旬"] = null; roots = Load(index);
            Check(State() == PreviewConditionState.Unknown, "ambiguous native calendar binding cannot silently fall back to a serialized constant");
            var nativeCutoff = new GameTimeData { name = "第一年五月下旬", GameTime = new GameTime { Year = 1, Month = 3, Stage = 1 } };
            bindings["Mortal.Core.GameTimeData/第一年五月下旬"] = nativeCutoff; roots = Load(index);
            Check(State() == PreviewConditionState.NotMet, "existing native calendar reference takes priority over file metadata");
            nativeCutoff.GameTime.Month = 5;
            Check(State() == PreviewConditionState.Met, "a native calendar is reread as current input without modifying that game object");
            bindings.Remove("Mortal.Core.GameTimeData/第一年五月下旬");
            var missionTime = new GameTimeData { name = "Back_Work_E_01", GameTime = new GameTime { Year = 9, Month = 12, Stage = 3 } };
            alchemy._gameStatManager._timeCollection._list.Add(missionTime);
            bindings = MapFileDefinitions.Bindings(alchemy._missionsManager, alchemy._gameStatManager); roots = Load(index); definitions = CalendarDefinitions(roots);
            Check(ReferenceEquals(bindings["Mortal.Core.GameTimeData/Back_Work_E_01"], missionTime) && missionTime.GameTime.Year == 9 &&
                !definitions.Any(d => d.TypeName == "Mortal.Core.GameTimeData" && d.Name == "Back_Work_E_01"),
                "mutable mission clock binds its live collection identity, preserves saved values and has no private disk calendar");
            byte[] wrongType = (byte[])index.Clone(); wrongType[section + 4] = 1; BitConverter.GetBytes(6077L).CopyTo(wrongType, section + 5);
            bool rejected = false; try { Load(wrongType); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "fixed-calendar index cannot name a condition config or another input type");
            byte[] duplicate = (byte[])index.Clone(); Array.Copy(index, section + 4, duplicate, section + 13, 9);
            rejected = false; try { Load(duplicate); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "duplicate fixed-calendar identities fail closed before installation decoding");
            Check(Counters.GameCalls == 0 && now.GameTime.Month == 4 && firstTalk.State == 0 && flags.Skip(1).All(f => f.StateWrites == 0),
                "all calendar cases execute no native Condition/Switch/effect/RNG/save and introduce no production gameplay writes");
        }
        finally
        {
            MapDefinitionAssets.Clear(); MapGuideSource.Clear(); Resources.Loaded.Clear(); MapGuideSource.MarkerVisible = null;
            DataHelper.ExternalReader = null;
            PreviewConditions.LoadedDefinitionReader = null; PreviewConditions.ExistingOwnerAlive = null;
            CheckPointManager.Instance = new CheckPointManager(); LuaManager.Instance = null;
        }
    }
}
