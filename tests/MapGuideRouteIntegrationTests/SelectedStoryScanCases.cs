using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;
using UnityEngine;

internal static partial class Program
{
    private static object Private(object value, string field) => value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(value);
    private static object SelectedDefinition() => Private(typeof(MapGuideSource).GetField("selectedPlace", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null), "Definition");
    private static object SelectedScan() => Private(SelectedDefinition(), "SelectedStories");
    private static MapRouteDefinitions SelectedRoutes() => (MapRouteDefinitions)Private(SelectedDefinition(), "Routes");
    private static MapGuideSnapshot ScanRead()
    {
        Time.unscaledTime += .25f;
        return (MapGuideSnapshot)MapGuideSource.Discover(true, false)[0].Read();
    }
    private static (PositionController Controller, List<FlagData> Flags) ScanFixture(int count, int unlocked)
    {
        MapGuideSource.Clear(); OneShotProof.Clear(); DataHelper.Scripts.Clear(); DataHelper.ExternalReader = null;
        MapGuideSource.Enabled = () => true; PreviewConditions.ObserveLuaEnvironment(null);
        var manager = CheckPointManager.Instance = new CheckPointManager();
        var flags = new List<FlagData>();
        for (int at = 1; at <= count; at++)
        {
            var flag = new FlagData { name = "input_" + at, State = at == unlocked ? 0 : 1 }; flags.Add(flag);
            manager._condition._list.Add(new ConditionResultData { name = "scan_" + at, _items = new List<ConditionResultItem> { IsZero(flag) } });
        }
        string Menu(int start, int length)
        {
            var body = new StringBuilder("local a={};");
            for (int local = 1; local <= length; local++)
            {
                int at = start + local - 1;
                body.Append("a[").Append(local).Append("]=checkpointmanager.Condition('scan_").Append(at).Append("') and 'O_scan_").Append(at).Append("' or '~x';");
            }
            body.Append("local r=choose(a);");
            for (int local = 1; local <= length; local++)
            {
                int at = start + local - 1;
                body.Append(local == 1 ? "if" : "elseif").Append(" r==").Append(local).Append(" then setcharacter(characters.Get('guide_npc'));say(luamanager.GetStoryText('D_scan_").Append(at).Append("')) ");
            }
            return body.Append("end").ToString();
        }
        if (count <= 16) DataHelper.Scripts["scan_root"] = Menu(1, count);
        else
        {
            // Several legal small menus, not one invalid >16-arm choice.
            int groups = (count + 11) / 12; var body = new StringBuilder("local a={");
            for (int group = 0; group < groups; group++) body.Append(group == 0 ? "" : ",").Append("'O_group_").Append(group).Append("'");
            body.Append("};local r=choose(a);");
            for (int group = 0; group < groups; group++)
            {
                DataHelper.Scripts["scan_group_" + group] = Menu(group * 12 + 1, Math.Min(12, count - group * 12));
                body.Append(group == 0 ? "if" : "elseif").Append(" r==").Append(group + 1).Append(" then luamanager.SetNextScript('scan_group_").Append(group).Append("');luamanager.Init() ");
            }
            DataHelper.Scripts["scan_root"] = body.Append("end").ToString();
        }
        var controller = new PositionController { _positionData = new FreePositionData { name = "ScanRoom", _defaultScript = "scan_root" } };
        MapGuideSource.Select(controller); return (controller, flags);
    }
    private static void SelectedStoryScanCases()
    {
        var fixture = ScanFixture(48, 46); MapGuideSnapshot value = null; int work = 0, reads = DataHelper.Reads, indexed = 0;
        bool shown = false;
        for (int at = 0; at < 160; at++)
        {
            value = ScanRead(); var graph = SelectedRoutes();
            int nextIndex = (int)Private(SelectedScan(), "Indexed");
            Check(nextIndex - indexed <= 64 && graph.WorkCount - work <= 8192 && DataHelper.Reads - reads <= 1,
                "map story indexing/preparation retain 64 metadata cells, 8192 work and one resource per query");
            indexed = nextIndex; work = graph.WorkCount; reads = DataHelper.Reads;
            shown |= value.Summary.AutoText.Contains("O_scan_46", StringComparison.Ordinal);
        }
        Check(shown && !value.Summary.Opportunity && !value.Summary.Available,
            "an unlocked story beyond the first 32 witnesses eventually appears as a tooltip, without a marker: " + value.Summary.AutoText + " / " + SelectedRoutes().Diagnostic);
        int before = fixture.Flags.Sum(flag => flag.StateReads), resources = DataHelper.Reads;
        value = ScanRead();
        int fresh = fixture.Flags.Sum(flag => flag.StateReads) - before;
        Check(value.Summary.AutoText.Contains("O_scan_46", StringComparison.Ordinal) && fresh > 0 && fresh < 8,
            "the previous witness is revalidated first, without rescanning the locked prefix or caching its truth: reads=" + fresh);
        fixture.Flags[45].State = 1;
        Check(!ScanRead().Summary.AutoText.Contains("O_scan_46", StringComparison.Ordinal), "a completed/locked preferred story disappears in the very next query");
        fixture.Flags[1].State = 0; shown = false;
        for (int at = 0; at < 80; at++) shown |= ScanRead().Summary.AutoText.Contains("O_scan_2", StringComparison.Ordinal);
        Check(shown, "a previously false candidate is reconsidered after the cursor wraps; false results are not retained");
        Check(DataHelper.Reads == resources && Counters.GameCalls == 0, "continued/warm scans reload no Lua and run no game judgment");
        fixture.Controller._positionData._conditionFlags = null;
        Check(!ScanRead().Summary.AutoText.Contains("경로 후보", StringComparison.Ordinal), "preferred definition cannot bypass unreadable native entry writes");
        fixture.Controller._positionData._conditionFlags = Array.Empty<PositionConditionFlagItem>();
        Check(ScanRead().Summary.AutoText.Contains("O_scan_2", StringComparison.Ordinal), "restored native entry safety revalidates the same definition hint");
        fixture.Controller._positionData._defaultScript = "replacement_root"; DataHelper.Scripts["replacement_root"] = "local a={'leave'};choose(a)";
        Check(!ScanRead().Summary.AutoText.Contains("O_scan_2", StringComparison.Ordinal), "source replacement discards the old preferred witness and cursor");
        SelectedDirectIntroduction();
    }
    private static void SelectedDirectIntroduction()
    {
        ScanFixture(1, 1);
        var table = new SwitchResultConfig();
        var intro = new SwitchResultData { name = "renamed_intro", _useCount = true, _count = 0,
            _countItems = new List<ValueCompareItem> { new ValueCompareItem { _compareOp = 0, _value = 1 } } };
        table._list.Add(intro); CheckPointManager.Instance._switchList = new object[] { table };
        DataHelper.Scripts["scan_root"] = "local a={'O_intro'};local r=choose(a);if r==1 then local s=checkpointmanager.Switch('renamed_intro');if s==1 then setcharacter(characters.Get('guide_npc'));say(luamanager.GetStoryText('D_intro')) end end";
        MapGuideSnapshot value = null; for (int at = 0; at < 40; at++) value = ScanRead();
        Check(value.Summary.AutoText.Contains("경로 후보: O_intro", StringComparison.Ordinal) && !value.Summary.Opportunity,
            "a first introduction with no further topic choice is also map tooltip guidance, never an opportunity marker");
        var path = SelectedRoutes().StoryPaths[0].Path;
        int gates = 32, kinds = 32;
        Check(path.ReadGuidance(new MapRouteRead(true), 0, true, true, ref gates, ref kinds, out _) == MapGuidanceRejection.None,
            "the active option and the map use the same introduction eligibility function");
        intro._count = 1;
        Check(!ScanRead().Summary.AutoText.Contains("경로 후보", StringComparison.Ordinal), "completed introduction removes the preferred map tooltip immediately");
        gates = kinds = 32;
        Check(path.ReadGuidance(new MapRouteRead(true), 0, true, true, ref gates, ref kinds, out _) != MapGuidanceRejection.None,
            "the active option's common filter also excludes that completed introduction");
        intro._count = 0;
        Check(ScanRead().Summary.AutoText.Contains("O_intro", StringComparison.Ordinal) && intro._count == 0 && Counters.GameCalls == 0,
            "a changed count is freshly reconsidered without executing or incrementing the native counter");
        var comparison = intro._countItems; intro._countItems = null;
        Check(!ScanRead().Summary.AutoText.Contains("경로 후보", StringComparison.Ordinal), "missing completion evidence cannot reuse the previous successful introduction");
        intro._countItems = comparison;
        Check(ScanRead().Summary.AutoText.Contains("O_intro", StringComparison.Ordinal), "restored completion evidence is freshly reconsidered");
    }
    private static void NativeSelectedStoryScan(string indexPath)
    {
        ScanFixture(1, -1); DataHelper.Scripts.Clear();
        var files = new Dictionary<string, (string Path, string Sha)>();
        using (var document = JsonDocument.Parse(File.ReadAllText(indexPath))) foreach (var item in document.RootElement.EnumerateArray())
            files[item.GetProperty("name").GetString()] = (item.GetProperty("path").GetString(), item.GetProperty("sha256").GetString());
        var manager = CheckPointManager.Instance; var switches = new SwitchResultConfig(); manager._switchList = new object[] { switches };
        var flags = new Dictionary<string, FlagData>(); var reset = new FlagCollectionData();
        var lua = new MoonSharp.Interpreter.Script();
        lua.m_GlobalTable.Values["statmodifymanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(new StatModifyManager { _talkOptionFlag = reset }) };
        observedTalkEnvironment = new Fungus.LuaEnvironment(lua); PreviewConditions.ObserveLuaEnvironment(observedTalkEnvironment);
        DataHelper.ExternalReader = key =>
        {
            if (!files.TryGetValue(key, out var file)) return new ScriptReadResult { State = ScriptReadState.Missing };
            byte[] bytes = File.ReadAllBytes(file.Path);
            Check(Convert.ToHexString(SHA256.HashData(bytes)).Equals(file.Sha, StringComparison.OrdinalIgnoreCase), "map scan verifies original Lua SHA: " + key);
            string body = Encoding.UTF8.GetString(bytes);
            foreach (Match match in Regex.Matches(body, "checkpointmanager\\.Condition\\(\"([^\"]+)\"\\)"))
            {
                string name = match.Groups[1].Value; if (flags.ContainsKey(name)) continue;
                var flag = new FlagData { name = name, State = name == "Talk_Brother4" ? 0 : 1 }; flags.Add(name, flag);
                manager._condition._list.Add(new ConditionResultData { name = name, _items = new List<ConditionResultItem> { IsZero(flag) } });
                if (name == "Talk_Brother4_02_06") { flag._default = 0; reset._list.Add(flag); }
            }
            foreach (Match match in Regex.Matches(body, "checkpointmanager\\.Switch\\(\"([^\"]+)\"\\)"))
            {
                string name = match.Groups[1].Value; if (switches._list.Exists(item => item.name == name)) continue;
                switches._list.Add(new SwitchResultData { name = name, _useCount = name == "Talk_Brother4_00", _count = 0,
                    _countItems = new List<ValueCompareItem> { new ValueCompareItem { _compareOp = 0, _value = 1 } },
                    _items = new List<ConditionResultItem> { IsZero(new FlagData { State = 1 }) } });
            }
            foreach (Match match in Regex.Matches(body, "checkpointmanager\\.Position\\(\"([^\"]+)\"\\)"))
            { string name = match.Groups[1].Value; if (!manager._position._list.Exists(item => item.name == name)) manager._position._list.Add(new PositionResultData { name = name }); }
            return new ScriptReadResult { State = ScriptReadState.Success, Text = body };
        };
        var control = new PositionController { _positionData = new FreePositionData { name = "OriginalRoom", _defaultScript = "room_001" } };
        MapGuideSource.Select(control); MapGuideSnapshot value = null; bool introduction = false;
        for (int at = 0; at < 200; at++) { value = ScanRead(); introduction |= value.Summary.AutoText.Contains("경로 후보: talk+talk+brother4 ·", StringComparison.Ordinal); }
        Check(introduction && !value.Summary.Opportunity, "SHA-original room introduction appears on the selected map without a new marker");
        switches._list.Find(item => item.name == "Talk_Brother4_00")._count = 1;
        bool later = false;
        for (int at = 0; at < 80; at++) { value = ScanRead(); later |= value.Summary.AutoText.Contains("O_Talk_Brother4_014", StringComparison.Ordinal); }
        Check(later, "original completed-introduction -> later unlocked descendant reaches the map under unchanged path/read caps");
        flags["Talk_Brother4_02_06"]._default = 1;
        Check(!ScanRead().Summary.AutoText.Contains("O_Talk_Brother4_014", StringComparison.Ordinal), "locking the original descendant removes the preferred tooltip on the next query");
        Check(Counters.GameCalls == 0 && switches._list.Find(item => item.name == "Talk_Brother4_00")._count == 1 && flags["Talk_Brother4_02_06"].State == 1,
            "original Lua with explicit synthetic predicates executes no Lua/native game and mutates no condition, reset or count");
        DataHelper.ExternalReader = null; PreviewConditions.ObserveLuaEnvironment(null); observedTalkEnvironment = null;
    }
    // Opt-in measurement, never a timing threshold or a Windows/FPS claim.
    private static void SelectedStoryBenchmark()
    {
        var rounds = new List<object>();
        for (int round = 0; round < 6; round++)
        {
            bool reuse = round % 4 == 1 || round % 4 == 2; var fixture = ScanFixture(28, 26);
            for (int at = 0; at < 100; at++) ScanRead();
            var scan = SelectedScan(); var type = scan.GetType();
            var preferred = type.GetField("Preferred", BindingFlags.Instance | BindingFlags.NonPublic);
            var cursor = type.GetField("Cursor", BindingFlags.Instance | BindingFlags.NonPublic);
            object savedWitness = preferred.GetValue(scan); int savedCursor = (int)cursor.GetValue(scan);
            for (int at = 0; at < 30; at++)
            { preferred.SetValue(scan, reuse ? savedWitness : null); cursor.SetValue(scan, reuse ? savedCursor : 0); ScanRead(); }
            int resources = DataHelper.Reads, states = fixture.Flags.Sum(flag => flag.StateReads);
            long bytes = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
            for (int at = 0; at < 300; at++)
            {
                // Comparison keeps identical cached definitions/current results
                // but forgets only the last witness and scan position.
                // Identical reflection overhead on both sides of the test.
                preferred.SetValue(scan, reuse ? savedWitness : null); cursor.SetValue(scan, reuse ? savedCursor : 0);
                if (!ScanRead().Summary.AutoText.Contains("O_scan_26", StringComparison.Ordinal)) throw new Exception("Benchmark lost its identical valid result");
            }
            double ms = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            rounds.Add(new { reuse, queries = 300, queryCpuMs = ms, bytesPerQuery = (GC.GetAllocatedBytesForCurrentThread() - bytes) / 300,
                stateReads = fixture.Flags.Sum(flag => flag.StateReads) - states, resourceReads = DataHelper.Reads - resources });
        }
        Console.WriteLine(JsonSerializer.Serialize(new { boundary = "host .NET8 production map provider and explicit synthetic state/resource/UI boundaries; no Lua/Windows/Unity/FPS", rounds }));
    }
}
