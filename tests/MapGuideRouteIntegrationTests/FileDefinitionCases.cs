using System;
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
    private static void FileDefinitionCases()
    {
        string data = Environment.GetEnvironmentVariable("DICEMASTER_MAP_GAME_DATA");
        if (string.IsNullOrEmpty(data)) { Console.WriteLine("Actual installed definition files not supplied; file-backed cases skipped."); return; }
        string corpus = Environment.GetEnvironmentVariable("DICEMASTER_CORPUS_INDEX");
        if (string.IsNullOrEmpty(corpus)) throw new Exception("File definition cases require the verified Lua index.");
        MapGuideSource.Clear(); MapDefinitionAssets.Clear(); PreviewConditions.ClearTalkContext(); Resources.Loaded.Clear();
        MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = _ => true;
        CheckPointManager.Instance = null; LuaManager.Instance = null; MapDefinitionAssets.Install();
        var flags = new[] { new FlagData { name = "Talk_Brother4" }, new FlagData { name = "Talk_Brother4_00" }, new FlagData { name = "E10005" } };
        var collection = new FlagCollectionData { _list = flags.ToList() };
        var control = new PositionController { _positionData = new FreePositionData { name = "ActualRoom", _defaultScript = "room_001",
            _conditionFlags = new[] { Assign(FlagEquals(flags[2], 0), flags[0], 1, 0) } } };
        control._missionsManager._flagsCollection = new object[] { collection };
        control._gameStatManager._gameTimeData.name = "PlayerGameTime";
        control._gameStatManager._gameTimeData.GameTime.Month = 4;
        DataHelper.Scripts.Clear(); DataHelper.ExternalReader = null;
        using (var index = JsonDocument.Parse(File.ReadAllText(corpus))) foreach (var row in index.RootElement.EnumerateArray())
            DataHelper.Scripts[row.GetProperty("name").GetString()] = File.ReadAllText(row.GetProperty("path").GetString());
        var oldDirectory = MapFileDefinitions.DirectoryReader;
        MapFileDefinitions.DirectoryReader = () => data;
        try
        {
            MapGuideSource.Register(control); MapMarkerSnapshot snapshot = null; int first = 0;
            for (int at = 0; at < 1500; at++)
            {
                snapshot = GuidanceMarkers();
                if (snapshot.Entries.Any(row => row.Guidance)) { first = at + 1; break; }
                Thread.Sleep(1); // allow only the file worker to progress, not a game scene
            }
            Check(first > 0 && CheckPointManager.Instance == null && LuaManager.Instance == null,
                "actual installed file definitions recover a cold room marker with no Story owners or loaded config/pool assets");
            Check(MapFileDefinitions.Describe().StartsWith("확인", StringComparison.Ordinal), "diagnostic distinguishes verified installation files from loaded native assets");
            Check(flags[0].State == 0 && flags.All(flag => flag.StateWrites == 0) && Counters.GameCalls == 0,
                "file-backed recovery uses private entry projection and reads no serialized State as a current input");
            int resources = DataHelper.Reads; string diagnostic = MapFileDefinitions.Describe();
            for (int at = 0; at < 80; at++) snapshot = GuidanceMarkers();
            Check(snapshot.Entries.Any(row => row.Guidance) && DataHelper.Reads == resources && MapFileDefinitions.Describe() == diagnostic,
                "warm file-backed markers reuse parsed definitions and Lua without restarting file verification");
            flags[1].State = 1; snapshot = GuidanceMarkers();
            Check(!snapshot.Entries.Any(row => row.AutoText.Contains("talk+talk+brother4 ·", StringComparison.Ordinal)), "actual first-greeting completion flag immediately removes that file-backed marker");
            flags[1].State = 0; flags[2].State = 1;
            for (int at = 0; at < 40; at++) snapshot = GuidanceMarkers();
            Check(snapshot.Entries.Count == 0, "actual false persistent prerequisite closes a file-backed route");
            flags[2].State = 0;
            for (int at = 0; at < 40; at++) snapshot = GuidanceMarkers();
            Check(snapshot.Entries.Any(row => row.Guidance) && DataHelper.Reads == resources, "restored current inputs recover without rereading files/Lua or restarting the game");
            var gate = PreviewConditions.Parse("checkpointmanager.Condition('Talk_Brother4')");
            Check(PreviewConditions.CreateReader().Current(gate) == PreviewConditionState.Unknown,
                "strict one-shot/opportunity readers never borrow file-backed route definitions");
            Check(PreviewConditions.CreateRouteReader().Current(PreviewConditions.Parse("checkpointmanager.Switch('S0003_01_02_001')==1")) == PreviewConditionState.Unknown,
                "serialized execution counts cannot establish a cold counted Switch result");
            Check(PreviewConditions.CreateRouteReader().Current(PreviewConditions.CaptureAfterTalkReset(gate, null, false)) == PreviewConditionState.Unknown,
                "missing current members of the actual reset footprint remain unknown instead of borrowing serialized State/default flags");
            using (Stream index = typeof(MapFileDefinitions).Assembly.GetManifestResourceStream("DiceMaster.MapDefinitions.index"))
            {
                byte[] broken = new byte[index.Length]; index.Read(broken, 0, broken.Length); broken[0] ^= 1;
                bool rejected = false;
                try { MapDefinitionFileReader.Read(data, new MemoryStream(broken), MapFileDefinitions.Bindings(control._missionsManager, control._gameStatManager), CancellationToken.None, out _); }
                catch (InvalidDataException) { rejected = true; }
                Check(rejected, "a malformed offset index fails closed before reading installed definitions");
            }
            using (Stream index = typeof(MapFileDefinitions).Assembly.GetManifestResourceStream("DiceMaster.MapDefinitions.index"))
            {
                byte[] broken = new byte[index.Length]; index.Read(broken, 0, broken.Length);
                using (var r = new BinaryReader(new MemoryStream(broken))) { r.ReadBytes(8); r.ReadString(); r.ReadInt64(); broken[r.BaseStream.Position] ^= 1; }
                bool rejected = false;
                try { MapDefinitionFileReader.Read(data, new MemoryStream(broken), MapFileDefinitions.Bindings(control._missionsManager, control._gameStatManager), CancellationToken.None, out _); }
                catch (InvalidDataException) { rejected = true; }
                Check(rejected, "a changed installed-file fingerprint cannot publish partially decoded definitions");
            }
            using (Stream index = typeof(MapFileDefinitions).Assembly.GetManifestResourceStream("DiceMaster.MapDefinitions.index"))
            {
                bool canceled = false;
                try { MapDefinitionFileReader.Read(data, index, new Dictionary<string, object>(), new CancellationToken(true), out _); }
                catch (OperationCanceledException) { canceled = true; }
                Check(canceled, "an invalidated file preparation stops before opening/hash-reading installed assets");
            }
            MapFileDefinitions.Clear();
            MapFileDefinitions.Observe(control._missionsManager, control._gameStatManager);
            MapFileDefinitions.Read(PreviewDefinitionKind.Conditions);
            MapFileDefinitions.Suspend(); Thread.Sleep(5);
            Check(MapFileDefinitions.Describe() == "미조회", "hidden-window suspension cannot publish a canceled worker's late result");
            MapFileDefinitions.Clear();
            Check(MapFileDefinitions.Describe() == "미조회", "load/OFF invalidation discards previous file results and current native reference bindings");
            Check(Counters.GameCalls == 0 && flags[0].StateWrites == 0, "all file-backed boundary cases retain read-only gameplay behavior");
            GC.KeepAlive(control);
        }
        finally
        {
            MapFileDefinitions.DirectoryReader = oldDirectory; MapDefinitionAssets.Clear(); MapGuideSource.Clear(); Resources.Loaded.Clear();
            MapGuideSource.MarkerVisible = null; PreviewConditions.LoadedDefinitionReader = null; PreviewConditions.ExistingOwnerAlive = null;
            CheckPointManager.Instance = new CheckPointManager(); LuaManager.Instance = null;
        }
    }
}

namespace Mortal.Core
{
    // An unrelated owner must not be initialized merely to inspect a static
    // cached reference, even on an unsupported future game schema.
    public sealed class ItemDatabase : UnityEngine.Object
    {
        public static ItemDatabase _instance;
        static ItemDatabase() { Counters.GameCalls++; }
    }
    public sealed class LibrarySystem : UnityEngine.Object
    {
        public static LibrarySystem _instance;
        static LibrarySystem() { Counters.GameCalls++; }
    }
}
