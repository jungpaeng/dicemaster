using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using Fungus;

internal static class Program
{
    private static int checks;
    private static long context;
    private static bool enabled;
    // Unity's live scene/menu owns its components independently of the
    // observer's weak handles. The fixture must model that native ownership.
    private static readonly List<object> liveSceneControls = new List<object>();
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static void Reset()
    {
        ChoiceStorySource.Clear(); liveSceneControls.Clear(); enabled = true; context++;
        ChoiceStorySource.Enabled = () => enabled; ChoiceStorySource.ContextReader = () => context;
        ChoiceStorySource.PoolReader = key => new List<MapRouteEvent>();
        DataHelper.Scripts.Clear(); DataHelper.ExternalReader = null; DataHelper.Reads = 0; Time.unscaledTime = 0;
        MenuDialog.ActiveMenuDialog = new MenuDialog(); EventSystem.current = new EventSystem();
        DicePlugin.Logs.Clear(); DisplayNames.Labels.Clear(); DisplayNames.LabelReads.Clear();
        DisplayNames.Labels["Position/title/brother4"] = "사사형";
        DisplayNames.Labels["Story/O_topic"] = "어린 시절 이야기";
        DisplayNames.Labels["Story/O_other"] = "다른 이야기";
    }
    private static PositionOptionButton Position(string raw, string title)
    {
        var value = new PositionOptionButton(); value.transform.Parent = MenuDialog.ActiveMenuDialog.transform; value.gameObject.transform = value.transform;
        value._source.text = raw; value._titleText[0].text = title; liveSceneControls.Add(value); MenuDialog.ActiveMenuDialog.nextOptionIndex++; ChoiceStorySource.Observe(value); return value;
    }
    private static BreakOptionButton Break(string raw, string title)
    {
        var value = new BreakOptionButton(); value.transform.Parent = MenuDialog.ActiveMenuDialog.transform; value.gameObject.transform = value.transform;
        value._source.text = raw; value._titleText[0].text = title; liveSceneControls.Add(value); MenuDialog.ActiveMenuDialog.nextOptionIndex++; ChoiceStorySource.Observe(value); return value;
    }
    private static NormalOptionButton Normal(string raw, string title)
    {
        var value = new NormalOptionButton(); value.transform.Parent = MenuDialog.ActiveMenuDialog.transform; value.gameObject.transform = value.transform;
        value._source.text = raw; value._text[0].text = title; liveSceneControls.Add(value); MenuDialog.ActiveMenuDialog.nextOptionIndex++; ChoiceStorySource.Observe(value); return value;
    }
    private static ChoiceStorySnapshot Read(bool detail = false)
    {
        var queries = ChoiceStorySource.Discover(detail); Check(queries.Count == 1, "One live menu has one scheduled query");
        Time.unscaledTime += .25f; return (ChoiceStorySnapshot)queries[0].Read();
    }
    private static ChoiceStorySnapshot Ready(bool detail = false)
    {
        ChoiceStorySnapshot value = null;
        for (int index = 0; index < 30; index++)
        {
            value = Read(detail);
            if (!value.Entries.Any(row => row.Status.Contains("준비 중", StringComparison.Ordinal))) return value;
        }
        throw new Exception("Preparation did not complete");
    }
    private static InformationRow Linked(ChoiceStorySnapshot value, string title)
    { return value.Entries.First(row => row.Title == title); }
    private static void Labels()
    {
        Reset();
        Check(ChoiceStoryLabels.Resolve("talk+talk+brother4") == "사사형", "Position labels use the native Position/title key");
        Check(DisplayNames.LabelReads.Last() == "Position/title/brother4", "Character IDs are not substituted for native Position titles");
        Check(ChoiceStoryLabels.Resolve("talk+O_topic+0+0") == "어린 시절 이야기", "Break labels use the story title key, without cost reads");
        Check(ChoiceStoryLabels.Resolve("O_topic") == "어린 시절 이야기", "Normal labels use the same underlying story table");
        Check(ChoiceStoryLabels.Resolve("O_topic_extra").Contains("이름 미확인", StringComparison.Ordinal), "Unknown IDs are not resolved by substring or invented names");
        Check(ChoiceStoryLabels.Group("talk+talk+brother4") == "사사형 관련 이야기", "Compact map group uses an actually resolved native label");
        Check(ChoiceStoryLabels.DialogueName("talk+talk+brother4", "brother4") == "사사형", "An explicit native conversation entry for that same NPC keeps the known character title");
        DisplayNames.Labels["Position/title/sleep"] = "휴식";
        Check(ChoiceStoryLabels.DialogueName("sleep+free+sleep", "brother4") == "휴식 → 사사형 대화", "A future conversation reached through another localized action names its actual supported NPC target");
        Check(ChoiceStoryLabels.DialogueTarget("missing_npc") == "인물 대화", "An unresolved NPC title never exposes its internal ID as a character name");
        Check(ChoiceStoryLabels.Rendered("<color=white>실제 항목</color> <size=18>(긴 효과·분석 설명)</size>") == "실제 항목", "Only the trailing existing addon effect wrapper is excluded from the title");
        Check(ChoiceStoryLabels.Rendered("실제 <size=18>이름</size>") == "실제 이름", "A native formatted title without the addon's parenthesized wrapper is preserved");
    }
    private static void LiveMenus()
    {
        Reset();
        string root = "local room={'talk+talk+brother4','sleep+free+sleep'}; local r=choose(room); if r==1 then luamanager.SetNextScript('talk_brother4') end";
        DataHelper.Scripts["talk_brother4"] = "local talk={'talk+O_topic+0+0','talk+O_other+0+0'}; local r=choose(talk); if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_topic')) elseif r==2 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_other')) end";
        ChoiceStorySource.ObserveScript(root, "room_001.LuaScript");
        var brother = Position("talk+talk+brother4", "사사형 <size=18>(효과를 길게 붙인 기존 안내)</size>");
        var sleep = Position("sleep+free+sleep", "휴식");
        ChoiceStorySource.ContextChanged();
        Check(ChoiceStorySource.Visible && ChoiceStorySource.MenuVisible, "The first Tick preserves a just-observed same-context menu");
        var value = Ready(true); var row = Linked(value, "사사형");
        Check(!row.Available && row.Detail.Contains("현재 선택 조건값: 충족", StringComparison.Ordinal), "The actual Position button retains its current option guard without promising execution across a speaker-call barrier");
        Check(row.AutoText.Contains("사사형 → 어린 시절 이야기", StringComparison.Ordinal) && row.AutoText.Contains("사사형 대화", StringComparison.Ordinal), "Compact automatic row contains a supported future choice path and the explicitly observed NPC");
        Check(!row.AutoText.Contains("외 ", StringComparison.Ordinal), "Incomplete speaker-call paths do not advertise an exact total story count");
        Check(!row.AutoText.Contains("긴", StringComparison.Ordinal) && !row.AutoText.Contains("D_topic", StringComparison.Ordinal), "Native title is clean and dialogue content is not invented as a title");
        Check(row.Detail.Contains("talk+talk+brother4", StringComparison.Ordinal) && row.Detail.Contains("연결 경로", StringComparison.Ordinal), "Explicit detail retains exact source identity and full supported paths");
        Check(!value.Entries.Any(item => item.Title == "휴식"), "A menu-only utility is not presented as a discovered story");
        EventSystem.current.currentSelectedGameObject = brother.gameObject;
        Check(ReferenceEquals(row, ChoiceStorySource.CurrentRow(value)), "Pad selection reads the existing selected GameObject without hover");
        int reads = DataHelper.Reads, sourceReads = brother._source.Reads, textReads = brother._titleText[0].Reads, labels = DisplayNames.LabelReads.Count;
        for (int frame = 0; frame < 1000; frame++) ChoiceStorySource.CurrentRow(value);
        Check(DataHelper.Reads == reads && brother._source.Reads == sourceReads && brother._titleText[0].Reads == textReads && DisplayNames.LabelReads.Count == labels, "Warm rendering performs no script, title, condition or localization queries");
        var child = new GameObject(); child.transform.Parent = brother.transform;
        EventSystem.current.currentSelectedGameObject = child;
        Check(ReferenceEquals(row, ChoiceStorySource.CurrentRow(value)), "A native child button selection stays attached to its one owned option");
        EventSystem.current.currentSelectedGameObject = sleep.gameObject;
        Check(ChoiceStorySource.CurrentRow(value) == null, "Unlinked focus has no invented selected story");
        for (int index = 0; index < 8; index++) Read();
        Check(DataHelper.Reads == reads, "Healthy warm refreshes retain the static route cache");
        MenuDialog old = MenuDialog.ActiveMenuDialog; MenuDialog.ActiveMenuDialog = new MenuDialog();
        Check(!ChoiceStorySource.Visible && !ChoiceStorySource.IsCurrent(value), "A different active menu never inherits another menu's publication");
        MenuDialog.ActiveMenuDialog = old; ChoiceStorySource.MenuCleared(old);
        old.nextOptionIndex = 0;
        Check(!ChoiceStorySource.MenuVisible && !ChoiceStorySource.Visible && !ChoiceStorySource.IsCurrent(value), "Native Clear invalidates captured menu ownership immediately");

        ChoiceStorySource.ObserveScript(DataHelper.Scripts["talk_brother4"], "talk_brother4.LuaScript");
        var topic = Break("talk+O_topic+0+0", "원래 대화 항목");
        Break("talk+O_other+0+0", "두 번째 항목");
        value = Ready(); row = Linked(value, "원래 대화 항목");
        Check(row.AutoText.Contains("원래 대화 항목", StringComparison.Ordinal) && !row.AutoText.Contains("어린 시절 이야기", StringComparison.Ordinal), "Already rendered Break title takes priority over resource fallback even in a qualified candidate");
        EventSystem.current.currentSelectedGameObject = topic.gameObject;
        Check(ReferenceEquals(row, ChoiceStorySource.CurrentRow(value)), "Break selection is available to the same passive pad selector");
        ChoiceStorySource.ObserveScript("local o={'O_topic'};local r=choose(o);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_topic')) end", "normal.LuaScript");
        var normal = Normal("O_topic", "일반 항목"); Normal("O_topic_extra", "일반 항목");
        value = Ready();
        Check(value.Entries.Count(row => row.Kind == "인물 대화") == 1, "Matching is by exact raw ID, never equal translated labels or ID prefixes");
        EventSystem.current.currentSelectedGameObject = normal.gameObject;
        Check(ChoiceStorySource.CurrentRow(value)?.Title == "일반 항목", "Normal choice is observed without changing its original text");
        int writes = normal._text[0].Writes;
        Check(normal._text[0].text == "일반 항목" && normal._text[0].Writes == writes, "This source appends no UI annotation and takes no input ownership");
        var previous = value; normal._text = null;
        try { ChoiceStorySource.Observe(normal); } catch (DataNotReadyException) { }
        Check(!ChoiceStorySource.IsCurrent(previous), "An observer read failure invalidates the old publication before attempting a replacement capture");
        normal._text = new[] { new UnityEngine.UI.Text { text = new string('가', 1000) } }; ChoiceStorySource.Observe(normal);
        row = Linked(Ready(), new string('가', 1000));
        Check(row.AutoText.Length <= 220 && row.AutoText.EndsWith("연결 후보 · 이후 진행 확인", StringComparison.Ordinal) && row.AutoText.Contains("…", StringComparison.Ordinal), "Automatic names are bounded without cutting the complete status; full native title stays in the log");
        Check(ChoiceStoryLabels.ShortName(ChoiceStoryLabels.Resolve("O_" + new string('x', 2000))).Length <= 28, "An unresolved long raw key cannot expand the automatic path without a bound");
    }
    private static void StateAndLifetimes()
    {
        Reset();
        ChoiceStorySource.ObserveScript("local a={'O_topic'};local r=choose(a);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_one')) end;local b={'O_topic'};local s=choose(b);if s==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_two')) end", "duplicates.LuaScript");
        Normal("O_topic", "중복 항목");
        var value = Ready(true); var row = Linked(value, "중복 항목");
        Check(!row.Available && row.Status.Contains("연결 선택지 확인 필요", StringComparison.Ordinal), "Duplicate raw label at two choose offsets is explicitly ambiguous");
        Check(row.Detail.Contains("여러 번", StringComparison.Ordinal), "Detail explains why the menu offset cannot be guessed");
        Check(!row.AutoText.Contains("→", StringComparison.Ordinal) && !row.AutoText.Contains("외 ", StringComparison.Ordinal), "An ambiguous actual menu receives no invented story path or count");
        ChoiceStorySource.ObserveScript("local a={'O_topic'};local r=choose(a);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_one')) end;local b={'O_topic'};local s=choose(b);if s==1 then local unused=1 end", "utility_duplicate.LuaScript");
        Normal("O_topic", "이야기 없는 두 번째 메뉴");
        value = Ready(true); row = Linked(value, "이야기 없는 두 번째 메뉴");
        Check(!row.Available && row.Status.Contains("연결 선택지 확인 필요", StringComparison.Ordinal) && !row.AutoText.Contains("→", StringComparison.Ordinal), "The all-choice registry detects the duplicate utility-only node even though it has no story entry");
        ChoiceStorySource.ObserveScript("local a={'O_topic'};local r=choose(a);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_one')) end;" + new string(';', 20000) + "local b={'O_topic'};local s=choose(b);if s==1 then local unused=1 end", "late_duplicate.LuaScript");
        Normal("O_topic", "준비 중 원문 후보"); bool sawCandidate = false;
        for (int at = 0; at < 80; at++)
        {
            value = Read(); row = value.Entries.FirstOrDefault(item => item.Title == "준비 중 원문 후보");
            if (row != null && row.AutoText.StartsWith("경로 후보:", StringComparison.Ordinal)) { sawCandidate = true; Check(!row.Available && row.Status.Contains("이후 진행 확인", StringComparison.Ordinal), "An incomplete unique prefix is a qualified future candidate, never a confirmed currently executable connection"); }
            if (row != null && row.Status.Contains("연결 선택지 확인 필요", StringComparison.Ordinal)) break;
        }
        Check(sawCandidate, "Cold preparation exposes a qualified candidate before the later duplicate is discovered");
        Check(row != null && !row.Available && !row.AutoText.Contains("→", StringComparison.Ordinal), "A duplicate discovered after cold preparation replaces provisional connection text with an ambiguity state");
        var yes = PreviewConditions.Constant(true); var no = PreviewConditions.Constant(false);
        var first = new MapChoiceStep("root", 1, 1, "O_topic", no, no, false);
        var second = new MapChoiceStep("root", 1, 1, "O_topic", yes, yes, false);
        Check(ChoiceStorySource.CurrentConditions(new[] { first, second }, new MapRouteRead()) == MapRouteState.Met, "Same-slot alternative definitions aggregate their current guards rather than keep only the first false definition");
        second = new MapChoiceStep("root", 1, 1, "O_topic", no, yes, false);
        Check(ChoiceStorySource.CurrentConditions(new[] { first, second }, new MapRouteRead()) == MapRouteState.NotMet, "A true own guard in an inactive enclosing branch does not create false confidence");
        var many = new List<MapChoiceStep>(); for (int i = 0; i < 33; i++) many.Add(first);
        Check(ChoiceStorySource.CurrentConditions(many, new MapRouteRead()) == MapRouteState.Unknown, "Unexamined alternative guards retain unknown at the 32-gate limit");
        context++; ChoiceStorySource.ContextChanged();
        Check(!ChoiceStorySource.Visible && !ChoiceStorySource.IsCurrent(value), "An unobserved newer Lua context discards old menus and source definitions");

        enabled = false;
        string script = "local a={'O_topic'};local r=choose(a);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_one')) end";
        ChoiceStorySource.ObserveScript(script, "off_observation.LuaScript");
        var normal = new NormalOptionButton(); normal.transform.Parent = MenuDialog.ActiveMenuDialog.transform; normal.gameObject.transform = normal.transform;
        liveSceneControls.Add(normal);
        normal._source.text = "O_topic"; normal._text[0].text = "나중에 ON";
        int raw = normal._source.Reads, reads = DataHelper.Reads;
        MenuDialog.ActiveMenuDialog.nextOptionIndex++; ChoiceStorySource.Observe(normal);
        Check(normal._source.Reads == raw && ChoiceStorySource.Discover().Count == 0 && DataHelper.Reads == reads, "OFF hooks retain weak identity only and start no data query");
        Check(ChoiceStorySource.MenuVisible && !ChoiceStorySource.Visible, "Physical menu visibility still occludes the underlying map when the feature is OFF");
        enabled = true; value = Ready();
        Check(Linked(value, "나중에 ON").Kind == "인물 대화", "ON recaptures an already visible menu without replaying any game method");
        ChoiceStorySource.ClearSnapshots(); value = Ready();
        Check(Linked(value, "나중에 ON").Kind == "인물 대화", "Display-cache cleanup retains enough weak metadata for visible ON recovery");
        MenuDialog.ActiveMenuDialog.gameObject.activeInHierarchy = false; reads = DataHelper.Reads;
        Check(!ChoiceStorySource.MenuVisible && ChoiceStorySource.Discover().Count == 0 && DataHelper.Reads == reads, "Hidden menus stop all discovery and resource work");
    }
    private static void RetryCadence()
    {
        Reset();
        ChoiceStorySource.ObserveScript("local a={'O_topic'};local r=choose(a);if r==1 then luamanager.SetNextScript('late_story') end", "retry.LuaScript");
        Normal("O_topic", "나중 자료");
        var engine = new RefreshEngine();
        int publications = 0;
        Action<float> tick = now => { Time.unscaledTime = now; engine.Tick("choice", ChoiceStorySource.Visible, true, now, false, false,
            () => ChoiceStorySource.Discover(), value => publications++, message => { }); };
        tick(0); tick(.1f); Check(publications == 1, "The shared engine enforces the healthy quarter-second interval");
        tick(.25f); int reads = DataHelper.Reads;
        Check(reads == 1 && DicePlugin.Logs.Count(message => message.Contains("failure", StringComparison.Ordinal)) == 1, "A missing followup records one transition diagnostic with no script body");
        for (float time = .5f; time < 5; time += .25f) tick(time);
        Check(DataHelper.Reads == reads && DicePlugin.Logs.Count == 1, "Healthy UI ticks do not bypass the route reader's failed-resource five-second backoff or spam logs");
        DataHelper.Scripts["late_story"] = "setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_late'))";
        tick(5.25f); tick(5.5f);
        Check(DataHelper.Reads == reads + 1 && DicePlugin.Logs.Count(message => message.Contains("recovered", StringComparison.Ordinal)) == 1, "A prepared resource recovers at the five-second retry and records one recovery diagnostic");
        Check(Counters.GameCalls == 0, "All source/route/condition reads preserve forbidden game execution counters");
    }
    private static void MeaningfulDialogue()
    {
        Reset();
        ChoiceStorySource.ObserveScript("local a={'sleep+free+sleep'};local r=choose(a);if r==1 then setcharacter(narrative);say(luamanager.GetStoryText('N_rest'));setcharacter(characters.Get('player'));say(luamanager.GetStoryText('D_player_only'));characters.Focus(characters.Get('brother4')) end", "rest_narration.LuaScript");
        Position("sleep+free+sleep", "휴식");
        var value = Ready(true);
        Check(!value.Entries.Any(row => row.Kind == "인물 대화"), "Rest narration, a D-prefixed player line and NPC focus without an NPC speaker produce no automatic NPC-conversation row");
        Check(value.Hints.Count == 0, "Excluded repeated activity does not leak through fallback automatic hints");

        Reset();
        ChoiceStorySource.ObserveScript("local a={'talk+O_topic+0+0'};local r=choose(a);if r==1 then setcharacter(characters.Get('player'));say(luamanager.GetStoryText('D_player_talk')) end", "player_talk.LuaScript");
        Break("talk+O_topic+0+0", "생각하기"); value = Ready(true);
        Check(!value.Entries.Any(row => row.Kind == "인물 대화") && value.Hints.Count == 0, "A native talk-tagged Break option with only the player as speaker is not upgraded into an NPC conversation");

        Reset();
        DataHelper.Scripts["late_npc"] = "setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_late_npc'))";
        ChoiceStorySource.ObserveScript("local a={'sleep+free+sleep'};local r=choose(a);if r==1 then setcharacter(narrative);say(luamanager.GetStoryText('N_rest'));setcharacter(characters.Get('player'));say(luamanager.GetStoryText('T_player'));luamanager.SetNextScript('late_npc') end", "rest_then_npc.LuaScript");
        var rest = Position("sleep+free+sleep", "휴식"); value = Ready(true);
        var linked = Linked(value, "휴식");
        Check(linked.Kind == "인물 대화" && linked.AutoText.Contains("휴식", StringComparison.Ordinal) && linked.AutoText.Contains("사사형 대화", StringComparison.Ordinal), "Rest followed by a literal NPC-speaker followup remains a qualified path to that NPC");
        Check(!linked.Available && linked.Status.Contains("후보", StringComparison.Ordinal) && !linked.Status.Contains("1회성", StringComparison.Ordinal), "A future NPC observation grants neither immediate execution nor a one-shot property");
        EventSystem.current.currentSelectedGameObject = rest.gameObject;
        Check(ReferenceEquals(linked, ChoiceStorySource.CurrentRow(value)), "The retained conditional rest-to-NPC path is available through passive pad focus");
        int reads = DataHelper.Reads;
        for (int at = 0; at < 40; at++) Read();
        Check(DataHelper.Reads == reads && Counters.GameCalls == 0, "Meaningful-dialogue filtering keeps the warm resource cache and never invokes game condition or execution APIs");

        Reset();
        ChoiceStorySource.ObserveScript("local a={'O_topic'};local r=choose(a);if r==1 then setcharacter(narrative);say(luamanager.GetStoryText('N_intro'));setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_real_npc'));setcharacter(characters.Get('player'));say(luamanager.GetStoryText('D_player_reply')) end", "late_inline_npc.LuaScript");
        Normal("O_topic", "동일 선택의 대화"); value = Ready(true);
        Check(Linked(value, "동일 선택의 대화").AutoText.Contains("사사형 대화", StringComparison.Ordinal), "An earlier narrative segment does not consume the selected path before its later explicit NPC line");
    }
    private static void Metadata(string root)
    {
        string managed = System.IO.Path.Combine(root, "Mortal_Data", "Managed");
        using (var stream = File.OpenRead(System.IO.Path.Combine(managed, "Mortal.Core.dll")))
        using (var pe = new PEReader(stream))
        {
            MetadataReader reader = pe.GetMetadataReader();
            foreach (string name in new[] { "NormalOptionButton", "PositionOptionButton", "BreakOptionButton" })
            {
                TypeDefinition type = reader.GetTypeDefinition(reader.TypeDefinitions.Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == name));
                var fields = type.GetFields().Select(handle => reader.GetString(reader.GetFieldDefinition(handle).Name)).ToList();
                Check(fields.Contains("_source") && fields.Contains(name == "NormalOptionButton" ? "_text" : "_titleText") && fields.Contains("_button"), "Pinned native " + name + " exposes the exact captured source/label/button fields");
                Check(type.GetMethods().Any(handle => reader.GetString(reader.GetMethodDefinition(handle).Name) == "UpdateContent"), "Pinned native " + name + " has the observed UpdateContent boundary");
            }
        }
        using (var stream = File.OpenRead(System.IO.Path.Combine(managed, "Fungus.dll")))
        using (var pe = new PEReader(stream))
        {
            MetadataReader reader = pe.GetMetadataReader();
            TypeDefinition type = reader.GetTypeDefinition(reader.TypeDefinitions.Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "MenuDialog"));
            Check(type.GetFields().Any(handle => reader.GetString(reader.GetFieldDefinition(handle).Name) == "nextOptionIndex"), "Pinned native physical visibility uses the real menu count field");
            MethodDefinition getter = reader.GetMethodDefinition(type.GetMethods().Single(handle => reader.GetString(reader.GetMethodDefinition(handle).Name) == "get_ActiveMenuDialog"));
            byte[] il = pe.GetMethodBody(getter.RelativeVirtualAddress).GetILBytes();
            Check(il.Length == 6 && il[0] == 0x7e && il[5] == 0x2a, "Native active-menu lookup is a direct static field read, without lazy menu creation");
        }
        using (var stream = File.OpenRead(System.IO.Path.Combine(managed, "UnityEngine.UI.dll")))
        using (var pe = new PEReader(stream))
        {
            MetadataReader reader = pe.GetMetadataReader();
            TypeDefinition type = reader.GetTypeDefinition(reader.TypeDefinitions.Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "EventSystem"));
            MethodDefinition getter = reader.GetMethodDefinition(type.GetMethods().Single(handle => reader.GetString(reader.GetMethodDefinition(handle).Name) == "get_currentSelectedGameObject"));
            byte[] il = pe.GetMethodBody(getter.RelativeVirtualAddress).GetILBytes();
            Check(il.Length == 7 && il[0] == 0x02 && il[1] == 0x7b && il[6] == 0x2a, "Native pad-focus lookup only reads the existing selected-object field");
        }
    }
    private static void NativeCorpus(string indexPath)
    {
        var paths = new Dictionary<string, (string Path, string Sha)>(StringComparer.OrdinalIgnoreCase);
        var failures = new List<string>();
        using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(indexPath)))
            foreach (JsonElement item in document.RootElement.EnumerateArray()) paths[item.GetProperty("name").GetString()] = (item.GetProperty("path").GetString(), item.GetProperty("sha256").GetString());
        var cases = new[] {
            (Name: "room_001", Raw: "talk+talk+brother4", Title: "사사형", Position: true, Expected: true),
            (Name: "talk_brother4", Raw: "talk+O_Talk_Brother4_003+0+0", Title: "사사형 대화 항목", Position: false, Expected: true),
            (Name: "room_001", Raw: "sleep+free+sleep", Title: "휴식", Position: true, Expected: false),
            (Name: "ch1_1_break_option", Raw: "talk+O_1_1_Break_002+0+0", Title: "실제 player-only talk 항목", Position: false, Expected: false),
            (Name: "ch2_4_6_2_break_start_01", Raw: "talk+O_2_4_6_2_Break_01_001+0+0", Title: "실제 D_ 독백 후속 항목", Position: false, Expected: false)
        };
        foreach (var sample in cases)
        {
            string name = sample.Name, title = sample.Title;
            Reset();
            var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ScriptReadResult Exact(string key)
            {
                string body;
                if (cache.TryGetValue(key, out body)) return ScriptReadResult.Success(body);
                if (!paths.TryGetValue(key, out var record)) return new ScriptReadResult { State = ScriptReadState.Missing };
                byte[] bytes = File.ReadAllBytes(record.Path);
                if (!string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), record.Sha, StringComparison.OrdinalIgnoreCase))
                { failures.Add(key); return new ScriptReadResult { State = ScriptReadState.Failed }; }
                cache.Add(key, body = new UTF8Encoding(false, true).GetString(bytes)); return ScriptReadResult.Success(body);
            }
            DataHelper.ExternalReader = Exact;
            ScriptReadResult root = Exact(name);
            Check(root.State == ScriptReadState.Success, "Actual root script bytes match the pinned corpus SHA-256 before observation");
            ChoiceStorySource.ObserveScript(root.Text, name + ".LuaScript");
            if (sample.Position) Position(sample.Raw, title); else Break(sample.Raw, title);
            ForceCollection();
            Check(ChoiceStorySource.Discover().Count == 1, "The fixture scene owns the actual corpus menu even after forced GC/finalization before its first row exists");
            ChoiceStorySnapshot value = null; InformationRow row = null;
            for (int at = 0; at < 160; at++)
            {
                value = Read(); row = value.Entries.FirstOrDefault(item => item.Title == title);
                if (sample.Expected && row != null && (name != "room_001" || row.AutoText.Contains("→", StringComparison.Ordinal))) break;
            }
            if (!sample.Expected)
            {
                Check(row == null && value.Hints.Count == 0, "Actual " + name + " " + sample.Raw + " has no automatic NPC-conversation row: narration, player-only dialogue and a talk tag are insufficient proof");
                Check(!value.Entries.Any(item => item.Kind == "인물 대화"), "Excluded actual corpus activity cannot be presented through another automatic entry");
                continue;
            }
            Check(row != null && (name != "room_001" || row.AutoText.Contains("→", StringComparison.Ordinal)), "Actual " + name + " corpus preserves its known choice labels despite visual/runwait uncertainty");
            Check(row.AutoText.StartsWith("경로 후보:", StringComparison.Ordinal) && !row.Available, "Actual " + name + " partial route is a visible candidate rather than a claimed exact current connection");
            Check(row.AutoText.Length <= 220 && !row.AutoText.Contains("1회성 확인 불가", StringComparison.Ordinal), "Actual corpus automatic path is bounded and carries no unknown one-shot spam");
        }
        Check(failures.Count == 0, "Every followup script read by this independent corpus run matches its index SHA-256");
    }
    private static void ForceCollection()
    { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
    private static void FixtureOwnership()
    {
        Reset(); ChoiceStorySource.ObserveScript("", "scene_ownership.LuaScript");
        Position("talk+talk+brother4", "장소"); Break("talk+O_topic+0+0", "대화"); Normal("O_topic", "일반");
        ForceCollection();
        Check(liveSceneControls.Count == 3 && ChoiceStorySource.Visible && ChoiceStorySource.Discover().Count == 1,
            "The scene keeps all three native button kinds alive while the production observer uses weak references");
        Reset(); Check(liveSceneControls.Count == 0, "Fixture reset releases the previous scene's component ownership");
    }
    private static void Main(string[] args)
    {
        Labels(); LiveMenus(); StateAndLifetimes(); RetryCadence(); MeaningfulDialogue(); FixtureOwnership();
        if (args.Length > 0) Metadata(args[0]);
        if (args.Length > 1) NativeCorpus(args[1]);
        Console.WriteLine("PASS: ChoiceStoryTests " + checks + " production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution.");
    }
}
