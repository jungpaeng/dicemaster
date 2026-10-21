using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Story;
using UnityEngine.EventSystems;

internal static partial class Program
{
    private const string EntryNpc = "setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_entry'))";
    private static SwitchResultData EntryCounter(string key, int count)
    {
        var data = new SwitchResultData { name = key, _useCount = true, _count = count,
            _countItems = new List<ValueCompareItem> { new ValueCompareItem { _compareOp = 0, _value = 1 } } };
        CheckPointManager.Instance._switchList = new object[] { new SwitchResultConfig { _list = new List<SwitchResultData> { data } } };
        return data;
    }
    private static void EntryDialogues()
    {
        Reset(); var count = EntryCounter("renamed_entry", 0);
        string body = "local a={'O_entry'};local r=choose(a);if r==1 then local first=checkpointmanager.Switch('renamed_entry');if first==1 then " + EntryNpc + " end end";
        ChoiceStorySource.ObserveScript(body, "entry_counter.LuaScript"); var button = Normal("O_entry", "진입 대화");
        EventSystem.current.currentSelectedGameObject = button.gameObject;
        Check(Linked(Ready(), "진입 대화").AutoText.Length != 0 && count._count == 0,
            "a genuinely remaining entry dialogue is retained by a scoped readonly next-count comparison");
        count._count = 1;
        Check(!Read().Entries.Any(x => x.Title == "진입 대화") && ChoiceStorySource.CurrentRow(Read()) == null,
            "a completed entry disappears from automatic rows and native pad tooltip without hiding the native option");
        Check(Linked(Read(true), "진입 대화").AutoText == "" && count._count == 1,
            "explicit completed-entry diagnostics cannot revive its automatic tooltip or increment its counter");
        count._count = 0; count._countItems = null;
        var row = Linked(Read(true), "진입 대화");
        Check(row.AutoText == "" && row.Detail.Contains("횟수·완료", StringComparison.Ordinal) && !Read().Entries.Any(x => x.Title == "진입 대화"),
            "unreadable entry completion evidence remains detail-only instead of fabricating a possible new conversation");
        count._countItems = new List<ValueCompareItem> { new ValueCompareItem { _compareOp = 0, _value = 1 } };
        Check(Linked(Read(), "진입 대화").AutoText.Length != 0, "entry count definition recovery restores the actual remaining dialogue without reloading Lua");
        int resources = DataHelper.Reads, parses = DiceMaster.LuaParse.LuaParser.Parses;
        for (int at = 0; at < 60; at++) Read();
        Check(count._count == 0 && Counters.GameCalls == 0 && DataHelper.Reads == resources && DiceMaster.LuaParse.LuaParser.Parses == parses,
            "warm next-count snapshots change no counter, RNG, native comparison, source cache or input ownership");

        foreach (string prefix in new[] { "local prior=checkpointmanager.Switch('renamed_entry');", "unknown_callback();" })
        {
            Reset(); EntryCounter("renamed_entry", 0);
            ChoiceStorySource.ObserveScript(prefix + body, "unsafe_entry_count.LuaScript"); Normal("O_entry", "경계 뒤 진입");
            Check(!Ready().Entries.Any(x => x.Title == "경계 뒤 진입"), "prior switch calls or opaque effects cannot borrow an unchanged next-count snapshot");
        }
        Reset(); EntryCounter("renamed_entry", 0);
        ChoiceStorySource.ObserveScript("local a={'O_entry'};local r=choose(a);if r==1 then if checkpointmanager.Switch('renamed_entry')==1 and checkpointmanager.Switch('renamed_entry')==1 then " + EntryNpc + " end end", "two_entry_calls.LuaScript");
        Normal("O_entry", "연속 횟수 판정");
        Check(!Ready().Entries.Any(x => x.Title == "연속 횟수 판정"), "two count calls in one predicate never reuse the same first invocation value");

        foreach (int op in Enumerable.Range(0, 6))
        {
            Reset(); count = EntryCounter("renamed_entry", 0);
            count._countItems[0]._compareOp = op;
            bool expected = op == 0 || op == 3 || op == 5;
            ChoiceStorySource.ObserveScript(body, "entry_comparison.LuaScript"); Normal("O_entry", "횟수 비교");
            Check(Ready().Entries.Any(x => x.Title == "횟수 비교") == expected && count._count == 0,
                "all six serialized native integer comparisons match the next invocation without calling Check or Execute: " + op);
        }
        foreach (int invalid in new[] { -1, int.MaxValue })
        {
            Reset(); EntryCounter("renamed_entry", invalid);
            ChoiceStorySource.ObserveScript(body, "invalid_entry_counter.LuaScript"); Normal("O_entry", "손상된 횟수");
            Check(!Ready().Entries.Any(x => x.Title == "손상된 횟수"), "invalid or overflowing counters cannot create a confirmed next-count story");
        }
        Reset(); var present = TooltipSwitch("entry_random", true); present.State = 1;
        ChoiceStorySource.ObserveScript("local a={'O_entry'};local r=choose(a);if r==1 then local n=checkpointmanager.Switch('entry_random');if n==1 then " + EntryNpc + " end end", "ordinary_entry_random.LuaScript");
        Normal("O_entry", "난수 이야기");
        Check(Linked(Ready(), "난수 이야기").Status == "진행 가능성 있음", "known ordinary random uncertainty is not confused with unreadable completion/count evidence");
    }

    private static void NativeEntryServices(string indexPath)
    {
        var paths = new Dictionary<string, (string Path, string Sha)>(StringComparer.OrdinalIgnoreCase);
        using (var doc = JsonDocument.Parse(File.ReadAllText(indexPath))) foreach (var item in doc.RootElement.EnumerateArray())
            paths[item.GetProperty("name").GetString()] = (item.GetProperty("path").GetString(), item.GetProperty("sha256").GetString());
        foreach (var sample in new[] { (Root: "room_001", Count: 1), (Root: "room_001", Count: 0), (Root: "talk_brother4", Count: 1) })
        {
            Reset(); var manager = CheckPointManager.Instance; var switches = new SwitchResultConfig(); manager._switchList = new object[] { switches };
            var flags = new Dictionary<string, FlagData>(); var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ScriptReadResult Exact(string key)
            {
                if (cache.TryGetValue(key, out string text)) return ScriptReadResult.Success(text);
                if (!paths.TryGetValue(key, out var data)) return new ScriptReadResult { State = ScriptReadState.Missing };
                byte[] bytes = File.ReadAllBytes(data.Path);
                Check(Convert.ToHexString(SHA256.HashData(bytes)).Equals(data.Sha, StringComparison.OrdinalIgnoreCase), "native entry/service source has the pinned SHA-256: " + key);
                cache[key] = text = new UTF8Encoding(false, true).GetString(bytes);
                foreach (Match match in Regex.Matches(text, "checkpointmanager\\.Condition\\(\"([^\"]+)\"\\)"))
                {
                    string name = match.Groups[1].Value; if (flags.ContainsKey(name)) continue;
                    var flag = new FlagData { name = "synthetic-" + name, State = name == "Talk_Brother4" || name == "Talk_Brother4_02_04" ? 0 : 1 };
                    flags.Add(name, flag); manager._condition._list.Add(new ConditionResultData { name = name, _items = new List<ConditionResultItem> { IsZero(flag) } });
                }
                foreach (Match match in Regex.Matches(text, "checkpointmanager\\.Switch\\(\"([^\"]+)\"\\)"))
                {
                    string name = match.Groups[1].Value; if (switches._list.Any(x => x.name == name)) continue;
                    switches._list.Add(new SwitchResultData { name = name, _useCount = name == "Talk_Brother4_00", _count = sample.Count,
                        _countItems = new List<ValueCompareItem> { new ValueCompareItem { _compareOp = 0, _value = 1 } },
                        _items = new List<ConditionResultItem> { IsZero(new FlagData { name = "guard-" + name, State = 0 }) } });
                }
                return ScriptReadResult.Success(text);
            }
            DataHelper.ExternalReader = Exact;
            DisplayNames.Labels["Story/O_Talk_Brother4_001"] = "떠나다"; DisplayNames.Labels["Story/O_Talk_Brother4_002"] = "물품 구매";
            DisplayNames.Labels["Story/O_Talk_Brother4_012"] = "대련: 사사형에게 도전한다";
            ChoiceStorySource.ObserveScript(Exact(sample.Root).Text, sample.Root + ".LuaScript");
            if (sample.Root == "room_001") Position("talk+talk+brother4", "사사형");
            else { Break("exit+O_Talk_Brother4_001+0+0", "떠나다"); Break("shop+O_Talk_Brother4_002+0+0", "물품 구매"); Break("talk+O_Talk_Brother4_012+0+0", "대련: 사사형에게 도전한다"); }
            ChoiceStorySnapshot value = null; for (int at = 0; at < 240; at++) value = Read();
            if (sample.Root == "room_001" && sample.Count == 0)
                Check(value.Entries.Any(x => x.Title == "사사형" && x.AutoText.Length != 0), "SHA-original remaining first introduction still has a conversation tooltip");
            else Check(!value.Entries.Any(x => x.Title == "사사형" || x.Title.StartsWith("대련", StringComparison.Ordinal)) && !value.Entries.Any(x => x.Opportunity),
                "SHA-original completed introduction plus exit/shop/repeat challenge has no invented conversation tooltip or marker: " + sample.Root);
            int reads = DataHelper.Reads;
            for (int at = 0; at < 30; at++) Read();
            Check(DataHelper.Reads == reads && Counters.GameCalls == 0 && switches._list.All(x => x._count == sample.Count),
                "native entry/service fixture never executes script, counter increment, comparison or combat and retains warm definitions");
        }
    }
}
