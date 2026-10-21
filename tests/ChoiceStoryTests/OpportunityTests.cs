using System;
using System.Collections.Generic;
using System.Linq;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Story;
using UnityEngine;

internal static partial class Program
{
    private static ChoiceStorySnapshot WarmOpportunities()
    { ChoiceStorySnapshot value = null; for (int at = 0; at < 12; at++) value = Read(); return value; }
    private static PositionEventData OncePool(string flagName, FlagData flag)
    {
        var data = new PositionEventData { name = "안전한 1회성", _activeCondition = IsZero(flag), ResultType = "once" };
        DataHelper.Scripts["opportunity_once_result"] = "statmodifymanager.SetFlag('" + flagName + "',1)";
        PositionEventCache.Parser.Actions["opportunity_once_result"] = new List<DiceMaster.LuaParse.Action> {
            new DiceMaster.LuaParse.Action { Analysis = new PreviewAnalysis { Effects = new List<PreviewEffect> {
                new PreviewEffect { Name = "SetFlag", Constants = new List<object> { flagName, 1 } } } } } };
        ChoiceStorySource.PoolReader = _ => new List<MapRouteEvent> { new MapRouteEvent(data, "opportunity_once_result") };
        return data;
    }
    private static void OpportunityMenus()
    {
        Reset(); ChoiceStorySource.ObserveScript("local a={'O_topic'};local r=choose(a);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_repeat')) end", "ordinary_conversation.LuaScript");
        Normal("O_topic", "일반 인물 대화"); var value = Ready();
        Check(Linked(value, "일반 인물 대화").Available && !Linked(value, "일반 인물 대화").Opportunity, "confirmed ordinary NPC path keeps its green description but cannot grant a red opportunity dot");

        Reset(); var once = new FlagData { name = "opportunity-once", State = 0 }; var target = OncePool(once.name, once);
        string root = "local a={'move+O_walk+1+0'};local r=choose(a);if r==1 then local p=checkpointmanager.Position('TravelPool');luamanager.SetNextScript(p) end";
        ChoiceStorySource.ObserveScript(root, "generic_travel.LuaScript"); var walk = Break("move+O_walk+1+0", "산책"); value = WarmOpportunities();
        var row = Linked(value, "산책");
        Check(row.Opportunity && row.Available && row.AutoText.Contains("안전한 1회성 · 1회성 이야기 · 선택 후 진행 가능"), "a live action option gets a red dot only after strict one-shot proof, current eligibility and its own terminal path all pass");
        int resources = DataHelper.Reads, parses = DiceMaster.LuaParse.LuaParser.Parses, descriptions = DiceMaster.LuaParse.LuaParser.Descriptions;
        for (int at = 0; at < 100; at++) value = Read();
        Check(DataHelper.Reads == resources && DiceMaster.LuaParse.LuaParser.Parses == parses && DiceMaster.LuaParse.LuaParser.Descriptions == descriptions, "one hundred warm travel menu ticks never reload or reparse Lua and never format full effects");
        once.State = 1; value = Read(); Check(!value.Entries.Any(x => x.Opportunity), "completed one-shot is removed immediately from the next current snapshot");
        once.State = 0; ChoiceStorySource.Interactive = _ => false; Check(!Read().Entries.Any(x => x.Opportunity), "native action/contribution-disabled control cannot retain a red dot");
        ChoiceStorySource.Interactive = _ => true; Check(Read().Entries.Any(x => x.Opportunity), "native action availability recovery restores the currently proven opportunity");
        ChoiceStorySource.OneShotEnabled = () => false; int reads = once.StateReads;
        Check(!Read().Entries.Any(x => x.Opportunity) && once.StateReads == reads, "one-shot OFF removes the menu dot and stops eligibility reads");
        ChoiceStorySource.OneShotEnabled = () => true; ChoiceStorySource.GuideEnabled = () => false;
        Check(Read().Entries.Any(x => x.Opportunity), "one-shot menu markers remain independently available with general map guidance OFF");
        var current = Read(true); Check(Linked(current, "산책").Detail.Contains("연결 경로") && Linked(current, "산책").Detail.Contains("실제 추첨"), "explicit detail retains proof and real source path without pretending to select an RNG result");
        target._eventRateItem = null;
        try { Read(); Check(false, "unreadable current event must enter source failure handling"); } catch (InvalidOperationException) { checks++; }
        target._eventRateItem = Array.Empty<RateItem>();

        Reset(); once = new FlagData { name = "own-branch-once", State = 0 }; OncePool(once.name, once); var gate = Condition("own-branch", 1);
        root = "local a={'O_blocked','O_allowed'};local r=choose(a);if r==1 then if checkpointmanager.Condition('own-branch') then local p=checkpointmanager.Position('Shared');luamanager.SetNextScript(p) end elseif r==2 then local p=checkpointmanager.Position('Shared');luamanager.SetNextScript(p) end";
        ChoiceStorySource.ObserveScript(root, "own_branch.LuaScript"); Normal("O_blocked", "막힌 행동"); Normal("O_allowed", "열린 행동"); value = WarmOpportunities();
        Check(!value.Entries.Any(x => x.Title == "막힌 행동" && x.Opportunity) && value.Entries.Any(x => x.Title == "열린 행동" && x.Opportunity), "a shared pool's other true path cannot grant a dot to this option's false branch");
        gate.State = 0; Check(Read().Entries.Count(x => x.Opportunity) == 2, "fresh condition values affect each bound action without rebuilding definitions");

        Reset(); once = new FlagData { name = "mixed-once", State = 0 }; OncePool(once.name, once); gate = Condition("mixed-path", 0);
        ChoiceStorySource.ObserveScript("local a={'O_mixed'};local r=choose(a);if r==1 then if checkpointmanager.Condition('mixed-path') then local p=checkpointmanager.Position('Mixed');luamanager.SetNextScript(p) else setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_repeat')) end end", "mixed_opportunity.LuaScript");
        Normal("O_mixed", "공통 행동"); row = Linked(WarmOpportunities(), "공통 행동"); string identity = row.Id;
        Check(row.Opportunity, "a currently proven event may coexist with a different ordinary conversation route of the same action");
        gate.State = 1; row = Linked(Read(), "공통 행동");
        Check(row.Available && !row.Opportunity && row.Id == identity, "changing from event opportunity to ordinary dialogue preserves the existing log row identity while removing its red dot");

        Reset(); var missions = MissionManagerData._instance = new MissionManagerData(); var active = new TrackingMissions(); missions.ActiveSubMissions = active;
        var stage = new MissionCheckData { name = "이미 등록된 이야기", _startScript = "registered_choice_story" }; active.Add("registered", stage);
        DataHelper.Scripts["registered_choice_story"] = "";
        ChoiceStorySource.ObserveScript("local a={'O_go','O_other'};local r=choose(a);if r==1 then luamanager.SetNextScript('registered_choice_story');luamanager.Init() end", "direct_waiting.LuaScript"); Normal("O_go", "이동"); Normal("O_other", "다른 행동"); value = WarmOpportunities();
        Check(value.Entries.Count(x => x.Opportunity) == 1 && Linked(value, "이동").AutoText.Contains("보조 임무"), "a literal action route intersected with actual waiting membership grants a mission opportunity");
        int enumerations = active.Enumerations; Read(); Check(active.Enumerations == enumerations + 1 && missions._subMissions.ListReads == 0, "travel menu enumerates the live waiting dictionary once and never opens the mission catalogue");
        active.Clear(); Check(!Read().Entries.Any(x => x.Opportunity), "same-count/context caches cannot retain a removed live waiting mission");
        missions.MainCheckData = stage; missions.MainMissionStart = false; Check(!Read().Entries.Any(x => x.Opportunity), "an unregistered main stage does not count merely because its start script matches");
        missions.MainMissionStart = true; Check(Read().Entries.Any(x => x.Opportunity && x.AutoText.Contains("주 임무")), "actually waiting main stage uses the same action-route membership criterion");
        ChoiceStorySource.GuideEnabled = () => false; Check(!Read().Entries.Any(x => x.Opportunity), "map guide OFF removes waiting mission markers independently of one-shot ON");

        Reset(); missions = MissionManagerData._instance = new MissionManagerData(); missions.ActiveSubMissions.Add("registered", stage);
        DataHelper.Scripts["registered_choice_story"] = "";
        ChoiceStorySource.ObserveScript("local a={'O_go'};local r=choose(a);if r==1 then luamanager.SetNextScript('registered_choice_story') end", "registration_only.LuaScript"); Normal("O_go", "등록만");
        Check(!WarmOpportunities().Entries.Any(x => x.Opportunity), "setting a future script without its actual Init handoff cannot grant a waiting mission execution dot");

        foreach (string effect in new[] { "unknown_callback();", "statmodifymanager.SetFlag('changed',1);", "statmodifymanager.Player('action',-1,'',1);" })
        {
            Reset(); once = new FlagData { name = "opaque-once", State = 0 }; OncePool(once.name, once);
            ChoiceStorySource.ObserveScript("local a={'O_opaque'};local r=choose(a);if r==1 then " + effect + "local p=checkpointmanager.Position('Opaque');luamanager.SetNextScript(p) end", "opaque_action.LuaScript"); Normal("O_opaque", "변경 뒤 행동");
            Check(!WarmOpportunities().Entries.Any(x => x.Opportunity), "unknown callback or preceding state write cannot be upgraded to a current reachable opportunity");
        }
        Check(Counters.GameCalls == 0 && once.State == 0, "uniform marker policy never executes judgments, RNG, save writes, stat changes or event results");
    }
    private static void NativeTravelOpportunityPolicy(string indexPath)
    {
        using var document = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(indexPath));
        var scripts = new Dictionary<string, (string Path, string Hash)>();
        foreach (var item in document.RootElement.EnumerateArray()) scripts[item.GetProperty("name").GetString()] = (item.GetProperty("path").GetString(), item.GetProperty("sha256").GetString());
        foreach (string name in new[] { "section_01_free_start_02", "section_02_free_start_02" })
        {
            Reset(); string body = System.IO.File.ReadAllText(scripts[name].Path);
            string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
            Check(hash == scripts[name].Hash, "original travel root is verified against the independently indexed source SHA-256: " + name);
            DataHelper.ExternalReader = key => {
                if (!scripts.TryGetValue(key, out var record)) return new ScriptReadResult { State = ScriptReadState.Missing };
                string text = System.IO.File.ReadAllText(record.Path);
                Check(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant() == record.Hash, "travel followup source hash is pinned");
                return ScriptReadResult.Success(text);
            };
            ChoiceStorySource.ObserveScript(body, name + ".LuaScript");
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(body, "\"((?:move|default)\\+O_Section_[^\"]+)\"")) Break(match.Groups[1].Value, "원본 유학 행동 " + match.Index);
            int parses = DiceMaster.LuaParse.LuaParser.Parses;
            ChoiceStorySnapshot value = null; for (int at = 0; at < 40; at++) value = Read(true);
            Check(!value.Entries.Any(row => row.Opportunity), "actual travel prelude/state-changing routes stay unconfirmed rather than producing speculative opportunity dots: " + name);
            Check(value.Entries.Any(row => row.Title == "기회 표식 판정") && DiceMaster.LuaParse.LuaParser.Parses == parses && Counters.GameCalls == 0,
                "original travel limits are explained only in detail without result parsing or native game execution");
        }
    }
}
