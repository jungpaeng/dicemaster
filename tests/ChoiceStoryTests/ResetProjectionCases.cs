using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Story;

internal static partial class Program
{
    private static Fungus.LuaEnvironment ResetEnvironment(params FlagData[] flags)
    {
        var manager = new StatModifyManager { _talkOptionFlag = new FlagCollectionData { _list = flags.ToList() } };
        var lua = new MoonSharp.Interpreter.Script();
        lua.m_GlobalTable.Values["checkpointmanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(CheckPointManager.Instance) };
        lua.m_GlobalTable.Values["statmodifymanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(manager) };
        var environment = new Fungus.LuaEnvironment(lua); PreviewConditions.ObserveLuaEnvironment(environment); return environment;
    }
    private static void ResetProjectionCases()
    {
        Reset(); var reset = Condition("renamed_topic_checkpoint", 7); reset._default = 0;
        var persistent = Condition("persistent_lock", 0);
        var environment = ResetEnvironment(reset);
        var predicate = PreviewConditions.Parse("checkpointmanager.Condition('renamed_topic_checkpoint')");
        var captured = PreviewConditions.CaptureAfterTalkReset(predicate, null, false);
        Check(PreviewConditions.Current(captured) == PreviewConditionState.Unknown && PreviewConditions.CreateRouteReader().Current(captured) == PreviewConditionState.Met,
            "only the route reader projects the concrete serialized reset default; strict evidence readers keep the original mask");
        Check(reset.StateReads == 0, "projected reset never samples or writes today's affected State");
        reset._default = 9;
        Check(PreviewConditions.CreateRouteReader().Current(captured) == PreviewConditionState.NotMet,
            "reset projection reads the actual nonzero default afresh, never assumes zero or persists a sampled value");
        reset._default = 0;
        var before = PreviewConditions.CaptureCurrent(predicate, null, false);
        Check(PreviewConditions.CreateRouteReader().Current(PreviewConditions.CaptureAfterTalkReset(before, null, false)) == PreviewConditionState.NotMet,
            "a later reset cannot rewrite a checkpoint captured earlier");
        Check(PreviewConditions.CreateRouteReader().Current(PreviewConditions.CaptureAfterTalkReset(predicate, new[] { reset.name }, false)) == PreviewConditionState.Unknown &&
            PreviewConditions.CreateRouteReader().Current(PreviewConditions.CaptureAfterTalkReset(predicate, null, true)) == PreviewConditionState.Unknown,
            "literal writes in either order and opaque effects stay unknown; no unmodeled ordering is guessed");

        CheckPointManager.Instance._condition._list[0]._items[0]._items.Add(IsZero(persistent)._items[0]);
        DataHelper.Scripts["projection_topics"] = "local a={};a[1]=checkpointmanager.Condition('renamed_topic_checkpoint') and 'talk+O_topic+0+0' or '~x';local r=choose(a);if r==1 then " + UnlockNpc + " end";
        ChoiceStorySource.ObserveScript(UnlockRoot + "statmodifymanager.ResetTalkFlag();luamanager.SetNextScript('projection_topics');luamanager.Init() end", "projection_room.LuaScript");
        var button = Normal("O_entry", "사사형");
        for (int at = 0; at < 60; at++) Read();
        var value = Read(true); var row = Linked(value, "사사형");
        Check(row.Guidance && row.HasBadge && !row.Opportunity,
            "an eligible route-only tooltip explicitly grants a light guidance badge without opportunity evidence");
        Check(row.AutoText.Contains("어린 시절 이야기", StringComparison.Ordinal) && !row.Opportunity,
            "a parent native choice previews the unlocked followup after a virtual reset without creating an opportunity marker");
        persistent.State = 1;
        Check(!Read().Entries.Any(x => x.Title == "사사형"), "a false persistent unlock still removes the parent tooltip after reset projection");
        persistent.State = 0; reset._default = 1;
        Check(!Read().Entries.Any(x => x.Title == "사사형"), "an actually locked reset default does not advertise a future topic");
        reset._default = 0; int resources = DataHelper.Reads, parses = DiceMaster.LuaParse.LuaParser.Parses;
        for (int at = 0; at < 50; at++) Read();
        Check(DataHelper.Reads == resources && DiceMaster.LuaParse.LuaParser.Parses == parses && reset.State == 7 && Counters.GameCalls == 0 && button.isActiveAndEnabled,
            "warm projected routes reuse definitions, preserve controls/state and invoke no game evaluator, reset or Lua");
        PreviewConditions.ObserveLuaEnvironment(null);
        Check(!Read().Entries.Any(x => x.Title == "사사형"), "losing the observed reset collection removes the route on the next refresh");
        GC.KeepAlive(environment);

        Reset(); var early = Condition("early_followup", 0); environment = ResetEnvironment(early);
        DataHelper.Scripts["early_topics"] = "local a={};a[1]=checkpointmanager.Condition('early_followup') and 'O_topic' or '~x';a[2]='O_other';local r=choose(a);if r==1 then " + UnlockNpc + " elseif r==2 then luamanager.SetNextScript('slow_unrelated') end";
        DataHelper.Scripts["slow_unrelated"] = new string(';', 90000);
        ChoiceStorySource.ObserveScript(UnlockRoot + "statmodifymanager.ResetTalkFlag();luamanager.SetNextScript('early_topics');luamanager.Init() end", "early_parent.LuaScript"); Normal("O_entry", "상위 선택");
        bool sawEarly = false;
        for (int at = 0; at < 90; at++)
        {
            value = Read();
            if (value.Entries.Any(x => x.Title == "상위 선택" && x.AutoText.Contains("어린 시절 이야기", StringComparison.Ordinal)))
            { sawEarly = ChoiceRoutes().HasPendingWork; break; }
        }
        Check(sawEarly, "an unlocked discovered followup publishes after root identity closes, before an unrelated slow tail finishes");
        early._default = 1;
        Check(!Read().Entries.Any(x => x.Title == "상위 선택"), "a newly locked projected topic disappears even during unrelated pending preparation");
        PreviewConditions.ObserveLuaEnvironment(null); GC.KeepAlive(environment);
        CurrentOwnerProjection();
    }

    private static void CurrentOwnerProjection()
    {
        Reset(); var topic = Condition("canonical_topic", 8); topic._default = 0;
        var environment = ResetEnvironment(topic);
        var script = (MoonSharp.Interpreter.Script)typeof(Fungus.LuaEnvironment).GetField("interpreter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(environment);
        script.m_GlobalTable.Values["checkpointmanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(CheckPointManager.Instance) };
        LuaManager.Instance = new LuaManager { _luaEnvironment = environment };
        DataHelper.Scripts["canonical_topics"] = "local a={};a[1]=checkpointmanager.Condition('canonical_topic') and 'O_topic' or '~x';local r=choose(a);if r==1 then " + UnlockNpc + " end";
        ChoiceStorySource.ObserveScript(UnlockRoot + "statmodifymanager.ResetTalkFlag();luamanager.SetNextScript('canonical_topics');luamanager.Init() end", "canonical_room.LuaScript");
        Normal("O_entry", "사사형");
        PreviewConditions.ObserveLuaEnvironment(new Fungus.LuaEnvironment(new MoonSharp.Interpreter.Script()));
        for (int at = 0; at < 60; at++) Read();
        Check(Read().Entries.Any(x => x.Title == "사사형" && x.AutoText.Contains("어린 시절 이야기", StringComparison.Ordinal)),
            "active parent choice and map share current-owner reset acquisition after unrelated Lua observation loss");
        LuaManager.Instance._luaEnvironment = null;
        Check(!Read().Entries.Any(x => x.Title == "사사형"), "a missing canonical environment still hides the active parent route");
        LuaManager.Instance._luaEnvironment = environment;
        Check(Read().Entries.Any(x => x.Title == "사사형"), "active parent route recovers with late canonical initialization without observing another script");
        topic._default = 1;
        Check(!Read().Entries.Any(x => x.Title == "사사형") && topic.State == 8 && topic.StateWrites == 1 && Counters.GameCalls == 0,
            "canonical acquisition preserves locked-topic filters and read-only game state on active choices");
        LuaManager.Instance = null; PreviewConditions.ClearTalkContext();
    }

    private static void NativeResetProjection(string index)
    {
        Reset(); MapRouteDefinitions.ClearSharedSyntaxCache();
        var files = new Dictionary<string, (string Path, string Sha)>();
        using (var doc = JsonDocument.Parse(File.ReadAllText(index))) foreach (var item in doc.RootElement.EnumerateArray())
            files[item.GetProperty("name").GetString()] = (item.GetProperty("path").GetString(), item.GetProperty("sha256").GetString());
        var flags = new Dictionary<string, FlagData>(); var switches = new SwitchResultConfig();
        CheckPointManager.Instance._switchList = new object[] { switches };
        var collection = new FlagCollectionData(); var lua = new MoonSharp.Interpreter.Script();
        lua.m_GlobalTable.Values["checkpointmanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(CheckPointManager.Instance) };
        lua.m_GlobalTable.Values["statmodifymanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(new StatModifyManager { _talkOptionFlag = collection }) };
        var environment = new Fungus.LuaEnvironment(lua); PreviewConditions.ObserveLuaEnvironment(environment);
        ScriptReadResult Exact(string key)
        {
            if (!files.TryGetValue(key, out var file)) return new ScriptReadResult { State = ScriptReadState.Missing };
            byte[] bytes = File.ReadAllBytes(file.Path);
            Check(Convert.ToHexString(SHA256.HashData(bytes)).Equals(file.Sha, StringComparison.OrdinalIgnoreCase), "reset projection uses pinned original Lua: " + key);
            string body = System.Text.Encoding.UTF8.GetString(bytes);
            foreach (Match match in Regex.Matches(body, "checkpointmanager\\.Condition\\(\"([^\"]+)\"\\)"))
            {
                string name = match.Groups[1].Value; if (flags.ContainsKey(name)) continue;
                var flag = Condition(name, name == "Talk_Brother4" ? 0 : 1); flags.Add(name, flag);
                if (name == "Talk_Brother4_00_01") { flag._default = 0; collection._list.Add(flag); }
            }
            foreach (Match match in Regex.Matches(body, "checkpointmanager\\.Switch\\(\"([^\"]+)\"\\)"))
            {
                string name = match.Groups[1].Value; if (switches._list.Any(x => x.name == name)) continue;
                switches._list.Add(new SwitchResultData { name = name, _useCount = name == "Talk_Brother4_00", _count = 1,
                    _countItems = new List<ValueCompareItem> { new ValueCompareItem { _compareOp = 0, _value = 1 } },
                    _items = new List<ConditionResultItem> { IsZero(new FlagData { State = 1 }) } });
            }
            return ScriptReadResult.Success(body);
        }
        DataHelper.ExternalReader = Exact; DisplayNames.Labels["Story/O_Talk_Brother4_003"] = "검사 주제";
        ObserveCompiled(Exact("room_001").Text, "room_001.LuaScript");
        // The generic compiler fixture owns an empty raw environment. Supply
        // this scenario's actual registered manager boundary after that hook.
        PreviewConditions.ObserveLuaEnvironment(environment); Position("talk+talk+brother4", "사사형");
        bool shown = false, early = false; int work = 0, reads = 0;
        for (int at = 0; at < 160; at++)
        {
            var value = Read(); var graph = ChoiceRoutes();
            Check(graph.WorkCount - work <= 8192 && graph.ScriptReadCount - reads <= 1, "original descendant preparation retains the work/resource budget");
            work = graph.WorkCount; reads = graph.ScriptReadCount;
            if (value.Entries.Any(x => x.Title == "사사형" && x.AutoText.Contains("검사 주제", StringComparison.Ordinal))) { shown = true; early |= graph.HasPendingWork; }
        }
        Check(shown && early, "SHA-original room -> brother4 -> guarded topic is found under unchanged caps and published before unrelated preparation finishes");
        flags["Talk_Brother4_00_01"]._default = 1;
        Check(!Read().Entries.Any(x => x.Title == "사사형" && x.AutoText.Length > 0), "locking the projected original topic removes its parent route rather than reviving the completed greeting or sparring");
        Check(flags["Talk_Brother4_00_01"].State == 1 && switches._list.All(x => x._count == 1) && Counters.GameCalls == 0,
            "original source plus synthetic serialized predicates executes no Lua, game judgment, reset, RNG or count/flag mutation");
        PreviewConditions.ObserveLuaEnvironment(null); GC.KeepAlive(environment);
    }
}

namespace Mortal.Story
{
    public sealed class LuaManager
    {
        public static LuaManager Instance { get; set; }
        public Fungus.LuaEnvironment _luaEnvironment;
    }
}
