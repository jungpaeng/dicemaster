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
using Mortal.Story;
using UnityEngine;
using UnityEngine.EventSystems;
using Fungus;

internal static partial class Program
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
        ChoiceStorySource.GuideEnabled = ChoiceStorySource.OneShotEnabled = () => true;
        ChoiceStorySource.Interactive = _ => true; ChoiceStorySource.CanReadEvent = _ => true;
        OneShotProof.Clear(); MissionManagerData._instance = null;
        ChoiceStorySource.PoolReader = key => new List<MapRouteEvent>();
        DataHelper.Scripts.Clear(); DataHelper.ExternalReader = null; DataHelper.Reads = 0; Time.unscaledTime = 0;
        MenuDialog.ActiveMenuDialog = new MenuDialog(); EventSystem.current = new EventSystem();
        DicePlugin.Logs.Clear(); DisplayNames.Labels.Clear(); DisplayNames.LabelReads.Clear();
        CheckPointManager.Instance = new CheckPointManager();
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
    private static ConditionResultItem IsZero(FlagData flag)
    {
        return new ConditionResultItem { _items = new List<StatCompareItem> { new StatCompareItem {
            _value1 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 3, _flag = flag } } },
            _value2 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = 0 } } }
        } } };
    }
    private static FlagData Condition(string key, int value)
    {
        var flag = new FlagData { name = key, State = value };
        CheckPointManager.Instance._condition._list.Add(new ConditionResultData { name = key, _items = new List<ConditionResultItem> { IsZero(flag) } });
        return flag;
    }
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
        Check(row.Available && row.Status == "진행 가능", "The complete supported NPC-story path is positive without invoking a native menu or judgment");
        Check(row.AutoText.Contains("사사형 → 어린 시절 이야기", StringComparison.Ordinal) && row.AutoText.Contains("사사형 대화", StringComparison.Ordinal), "Compact automatic row contains a supported future choice path and the explicitly observed NPC");
        Check(row.AutoText.Contains("외 1개", StringComparison.Ordinal), "Fully observed safe alternatives retain their known compact remaining-story count");
        Check(!row.AutoText.Contains("긴", StringComparison.Ordinal) && !row.AutoText.Contains("D_topic", StringComparison.Ordinal), "Native title is clean and dialogue content is not invented as a title");
        Check(row.Detail.Contains("talk+talk+brother4", StringComparison.Ordinal) && row.Detail.Contains("연결 경로", StringComparison.Ordinal), "Explicit detail retains exact source identity and full supported paths");
        Check(!value.Entries.Any(item => item.Title == "휴식"), "A menu-only utility is not presented as a discovered story");
        brother.isActiveAndEnabled = false; value = Read(true);
        Check(!value.Entries.Any(item => item.Title == "사사형") && !value.Hints.Any(item => item.Title == "사사형"),
            "A source-defined future choice cannot produce a story row or hint while its actual menu control is disabled");
        brother.isActiveAndEnabled = true; brother.gameObject.activeInHierarchy = false; value = Read(true);
        Check(!value.Entries.Any(item => item.Title == "사사형"), "A hidden actual option is also excluded while another live option keeps the menu visible");
        brother.gameObject.activeInHierarchy = true; value = Read(true); row = Linked(value, "사사형");
        Check(row.Detail.Contains("실제 이야기 발생을 보장하지 않습니다", StringComparison.Ordinal),
            "The current actual option anchors the route guide without promising downstream story execution");
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
        Check(row.AutoText.Length <= 220 && row.AutoText.EndsWith("진행 가능", StringComparison.Ordinal) && row.AutoText.Contains("…", StringComparison.Ordinal), "Automatic names are bounded without cutting the complete status; full native title stays in the log");
        Check(ChoiceStoryLabels.ShortName(ChoiceStoryLabels.Resolve("O_" + new string('x', 2000))).Length <= 28, "An unresolved long raw key cannot expand the automatic path without a bound");
    }
    private static void StateAndLifetimes()
    {
        Reset();
        ChoiceStorySource.ObserveScript("local a={'O_topic'};local r=choose(a);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_one')) end;local b={'O_topic'};local s=choose(b);if s==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_two')) end", "duplicates.LuaScript");
        Normal("O_topic", "중복 항목");
        var value = Ready(true); var row = Linked(value, "중복 항목");
        Check(!row.Available && row.Status == "진행 가능성 있음", "Duplicate raw label at two choose offsets keeps the current story condition explicitly uncertain");
        Check(row.Detail.Contains("여러 번", StringComparison.Ordinal), "Detail explains why the menu offset cannot be guessed");
        Check(!row.AutoText.Contains("→", StringComparison.Ordinal) && !row.AutoText.Contains("외 ", StringComparison.Ordinal), "An ambiguous actual menu receives no invented story path or count");
        ChoiceStorySource.ObserveScript("local a={'O_topic'};local r=choose(a);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_one')) end;local b={'O_topic'};local s=choose(b);if s==1 then local unused=1 end", "utility_duplicate.LuaScript");
        Normal("O_topic", "이야기 없는 두 번째 메뉴");
        value = Ready(true); row = Linked(value, "이야기 없는 두 번째 메뉴");
        Check(!row.Available && row.Status == "진행 가능성 있음" && row.Detail.Contains("여러 번", StringComparison.Ordinal) && !row.AutoText.Contains("→", StringComparison.Ordinal), "The all-choice registry detects the duplicate utility-only node even though it has no story entry");
        ChoiceStorySource.ObserveScript("local a={'O_topic'};local r=choose(a);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_one')) end;" + new string(';', 20000) + "local b={'O_topic'};local s=choose(b);if s==1 then local unused=1 end", "late_duplicate.LuaScript");
        Normal("O_topic", "준비 중 원문 후보"); bool sawCandidate = false;
        for (int at = 0; at < 80; at++)
        {
            value = Read(); row = value.Entries.FirstOrDefault(item => item.Title == "준비 중 원문 후보");
            if (row != null && row.AutoText.StartsWith("경로 후보:", StringComparison.Ordinal)) { sawCandidate = true; Check(!row.Available && row.Status.Contains("진행 가능성 있음", StringComparison.Ordinal), "An incomplete unique prefix is a qualified future candidate, never a confirmed currently executable connection"); }
            if (row != null && row.Detail.Contains("여러 번", StringComparison.Ordinal)) break;
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
        Check(linked.Available && linked.Status == "진행 가능" && !linked.Status.Contains("1회성", StringComparison.Ordinal), "A fully supported NPC path has current full-condition evidence without granting a one-shot property");
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
    private static void ActivityResponses()
    {
        foreach (string npc in new[] { "fixture_teacher", "renamed_guest" })
        {
            Reset();
            string raw = "talk+O_94+0+0";
            string speaker = "setcharacter(characters.Get('" + npc + "'));say(luamanager.GetStoryText('";
            CheckPointManager.Instance._switchList = new object[] { new SwitchResultConfig { _list = new List<SwitchResultData> {
                new SwitchResultData { name = npc + "_service_guard", _items = new List<ConditionResultItem> { new ConditionResultItem { _items = new List<StatCompareItem> {
                    new StatCompareItem { _value1 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = 1 } } },
                        _value2 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = 1 } } } } } } } }
            } } };
            DataHelper.Scripts["generic_service_after"] = "";
            ChoiceStorySource.ObserveScript("local a={'" + raw + "'};local r=choose(a);if r==1 then " + speaker + "D_fixed_start'));local s=checkpointmanager.Switch('" + npc + "_service_guard');if s==1 then statmodifymanager.Player('money',-100,'',1);" + speaker + "D_fixed_reply'));luamanager.SetCurrentStoryScript('generic_service_after');luamanager.ChangeScene('Combat','generic_service_combat','Story') elseif s==2 then luamanager.ChangeScene('Free','','') end end", npc + "_service.LuaScript");
            var option = Break(raw, "계속 사용할 수 있는 활동");
            var value = Ready(true);
            Check(!value.Entries.Any(item => item.Title == "계속 사용할 수 있는 활동" && item.Kind == "인물 대화"),
                "NPC fixed lines wrapping a proven repeat service do not fabricate an individual story; arbitrary NPC=" + npc);
            Check(!value.Hints.Any(item => item.AutoText.Contains("계속 사용할 수 있는 활동", StringComparison.Ordinal)), "Excluded service responses cannot return through fallback automatic hints");
            EventSystem.current.currentSelectedGameObject = option.gameObject;
            Check(ChoiceStorySource.CurrentRow(value) == null, "Passive pad focus on the remaining native service has no invented story row and takes no input ownership");
            int reads = DataHelper.Reads, textReads = option._titleText[0].Reads, sourceReads = option._source.Reads;
            for (int at = 0; at < 1000; at++) ChoiceStorySource.CurrentRow(value);
            Check(DataHelper.Reads == reads && option._titleText[0].Reads == textReads && option._source.Reads == sourceReads, "Service filtering keeps render-time pad lookup free of script/title/current-condition reads");

            Reset(); var consumed = Condition(npc + "_own_once", 0);
            DataHelper.Scripts["consumed_topic_wrapper"] = speaker + "D_fixed_start'));local s=checkpointmanager.Switch('consumed_service_guard');if s==1 then statmodifymanager.Player('money',-100,'',1);luamanager.ChangeScene('Combat','consumed_combat','Story') elseif s==2 then luamanager.ChangeScene('Free','','') end";
            ChoiceStorySource.ObserveScript("local a={};a[1]=checkpointmanager.Condition('" + npc + "_own_once') and 'O_topic' or '~x';local r=choose(a);if r==1 then statmodifymanager.SetFlag('" + npc + "_own_once',1);luamanager.SetNextScript('consumed_topic_wrapper') end", npc + "_consumed_root.LuaScript");
            option = Break("O_topic", "조건을 소모하는 이야기"); value = Ready(true);
            Check(value.Entries.Any(item => item.Title == "조건을 소모하는 이야기" && item.Kind == "인물 대화"), "A selected parent that consumes its own guard cannot be hidden merely because the child dialogue wraps a paid service");
            EventSystem.current.currentSelectedGameObject = option.gameObject;
            Check(ChoiceStorySource.CurrentRow(value)?.Title == "조건을 소모하는 이야기" && consumed.State == 0 && Counters.GameCalls == 0, "The protected consumed-guard topic retains native pad focus without applying its flag write");

            Reset();
            DataHelper.Scripts["opaque_parent_wrapper"] = speaker + "D_fixed_start'));local s=checkpointmanager.Switch('opaque_parent_guard');if s==1 then statmodifymanager.Player('money',-100,'',1);luamanager.ChangeScene('Combat','opaque_parent_combat','Story') elseif s==2 then luamanager.ChangeScene('Free','','') end";
            ChoiceStorySource.ObserveScript("local a={'O_topic'};local r=choose(a);if r==1 then unknown_callback();luamanager.SetNextScript('opaque_parent_wrapper') end", npc + "_opaque_parent.LuaScript");
            Break("O_topic", "선행 동작이 불명인 이야기"); value = Ready(true);
            Check(value.Entries.Any(item => item.Title == "선행 동작이 불명인 이야기" && item.Status == "진행 가능성 있음"), "An opaque parent action keeps a child service-shaped dialogue unclassified instead of hiding a potentially special selected topic");

            Reset(); var special = Condition(npc + "_special", 0);
            ChoiceStorySource.ObserveScript("local a={'O_topic'};local r=choose(a);if r==1 then " + speaker + "D_fixed_intro'));if checkpointmanager.Condition('" + npc + "_special') then " + speaker + "D_special'));statmodifymanager.SetFlag('generic_seen',1) end;luamanager.ChangeScene('Combat','special_combat','Story') end", npc + "_combat_story.LuaScript");
            option = Break("O_topic", "활동 중 특별 이야기"); value = Ready(true);
            Check(value.Entries.Any(item => item.Title == "활동 중 특별 이야기" && item.Kind == "인물 대화"), "A separately conditioned NPC story remains eligible even when its script also dispatches combat");
            EventSystem.current.currentSelectedGameObject = option.gameObject;
            Check(ChoiceStorySource.CurrentRow(value)?.Title == "활동 중 특별 이야기", "The preserved combat story continues to follow native passive pad selection");
            Check(special.State == 0 && Counters.GameCalls == 0, "Activity/story analysis never applies a story flag, invokes native condition/RNG or starts combat");

            Reset(); special = Condition(npc + "_special_callback", 0);
            DataHelper.Scripts["parent_service"] = speaker + "D_fixed_start'));local s=checkpointmanager.Switch('callback_service_guard');if s==1 then statmodifymanager.Player('money',-100,'',1);luamanager.SetCurrentStoryScript('separate_callback_story');luamanager.ChangeScene('Combat','callback_combat','Story') elseif s==2 then luamanager.ChangeScene('Free','','') end";
            DataHelper.Scripts["separate_callback_story"] = "if checkpointmanager.Condition('" + npc + "_special_callback') then " + speaker + "D_callback_special'));statmodifymanager.SetFlag('callback_seen',1) end";
            ChoiceStorySource.ObserveScript("local a={'O_topic'};local r=choose(a);if r==1 then luamanager.SetNextScript('parent_service') end", npc + "_callback_root.LuaScript");
            option = Break("O_topic", "활동 뒤 특별 이야기"); value = Ready(true);
            Check(value.Entries.Any(item => item.Title == "활동 뒤 특별 이야기" && item.Kind == "인물 대화"), "A fixed activity prelude cannot hide a separate conditional story in the registered combat callback");
            EventSystem.current.currentSelectedGameObject = option.gameObject;
            Check(ChoiceStorySource.CurrentRow(value)?.Title == "활동 뒤 특별 이야기" && special.State == 0 && Counters.GameCalls == 0, "The preserved callback story retains native pad focus without executing future flags or combat");

            Reset(); var receipt = Condition(npc + "_receipt_once", 0);
            DataHelper.Scripts["receipt_parent_service"] = speaker + "D_fixed_start'));local s=checkpointmanager.Switch('receipt_service_guard');if s==1 then statmodifymanager.Player('money',-100,'',1);luamanager.SetCurrentStoryScript('receipt_result');luamanager.ChangeScene('Combat','receipt_combat','Story') elseif s==2 then luamanager.ChangeScene('Free','','') end";
            DataHelper.Scripts["receipt_result"] = "local s=checkpointmanager.Switch('receipt_outcome');if s==1 then statmodifymanager.Player('martial-point',2,'',1);" + speaker + "D_receipt_loss')) elseif s==2 then statmodifymanager.Player('martial-point',4,'',1);" + speaker + "D_receipt_win'));statmodifymanager.SetFlag('" + npc + "_receipt_once',1) end;luamanager.ChangeScene('Free','','')";
            ChoiceStorySource.ObserveScript("local a={};a[1]=checkpointmanager.Condition('" + npc + "_receipt_once') and 'O_topic' or '~x';local r=choose(a);if r==1 then luamanager.SetNextScript('receipt_parent_service') end", npc + "_receipt_root.LuaScript");
            option = Break("O_topic", "결과 대사 뒤 조건 소모"); value = Ready(true);
            Check(value.Entries.Any(item => item.Title == "결과 대사 뒤 조건 소모" && item.Kind == "인물 대화"), "An activity receipt that consumes the selected topic guard after its NPC response cannot be hidden as a generic mechanical result");
            EventSystem.current.currentSelectedGameObject = option.gameObject;
            Check(ChoiceStorySource.CurrentRow(value)?.Title == "결과 대사 뒤 조건 소모" && receipt.State == 0 && Counters.GameCalls == 0, "The protected outcome story remains available to passive pad focus without executing its future flag write");

            Reset(); special = Condition(npc + "_late_callback", 0);
            DataHelper.Scripts["waiting_parent_service"] = speaker + "D_fixed_start'));local s=checkpointmanager.Switch('waiting_service_guard');if s==1 then statmodifymanager.Player('money',-100,'',1);luamanager.SetCurrentStoryScript('late_callback_story');luamanager.ChangeScene('Combat','waiting_combat','Story') elseif s==2 then luamanager.ChangeScene('Free','','') end";
            ChoiceStorySource.ObserveScript("local a={'O_topic'};local r=choose(a);if r==1 then luamanager.SetNextScript('waiting_parent_service') end", npc + "_missing_callback.LuaScript");
            Break("O_topic", "후속 자료 대기 이야기"); value = Ready(true);
            Check(value.Entries.Any(item => item.Title == "후속 자료 대기 이야기" && item.Status == "진행 가능성 있음") && value.Entries.Any(item => item.Status.Contains("읽기 대기/실패", StringComparison.Ordinal)), "A fixed activity response cannot hide a topic while its registered callback remains unreadable");
            DataHelper.Scripts["late_callback_story"] = "if checkpointmanager.Condition('" + npc + "_late_callback') then " + speaker + "D_late_special'));statmodifymanager.SetFlag('late_callback_seen',1) end";
            Time.unscaledTime += 5; value = Ready(true);
            Check(value.Entries.Any(item => item.Title == "후속 자료 대기 이야기" && item.Kind == "인물 대화") && !value.Entries.Any(item => item.Status.Contains("읽기 대기/실패", StringComparison.Ordinal)), "A callback recovered after the normal five-second retry contributes its distinct special story without losing the selected topic");

            Reset();
            ChoiceStorySource.ObserveScript("local a={'O_topic'};local r=choose(a);if r==1 then unknown_callback();" + speaker + "D_unknown'));luamanager.ChangeScene('Combat','unknown_combat','Story') end", npc + "_unclassified.LuaScript");
            Break("O_topic", "구분 근거 부족"); value = Ready(true);
            Check(value.Entries.Any(item => item.Title == "구분 근거 부족" && item.Status == "진행 가능성 있음"), "An arbitrary predecessor callback prevents a repeat-service proof and preserves the unclassified NPC path");
        }
    }
    private static void FullStoryConditions()
    {
        const string npc = "setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_training_npc'))";
        const string start = "local a={'practice+free+practice'};local r=choose(a);if r==1 then ";
        Reset(); var gate = Condition("training_npc", 1);
        ChoiceStorySource.ObserveScript(start + "if checkpointmanager.Condition('training_npc') then " + npc + " end end", "training_condition.LuaScript");
        var training = Position("practice+free+practice", "수련");
        var value = Ready(true); var row = Linked(value, "수련");
        Check(row.Status == "지금 진행 불가" && !row.Available, "An unguarded training option cannot turn its false internal NPC condition into a positive story condition");
        Check(!row.AutoText.Contains("조건 충족", StringComparison.Ordinal), "The automatic row does not advertise the utility option's true first guard as story readiness");
        int reads = DataHelper.Reads;
        gate.State = 0; value = Read(true); row = Linked(value, "수련");
        Check(row.Status == "진행 가능" && row.Available, "A safely observed full NPC-story gate can become positive on a fresh current snapshot");
        EventSystem.current.currentSelectedGameObject = training.gameObject;
        Check(ReferenceEquals(row, ChoiceStorySource.CurrentRow(value)), "Full-story condition publications retain passive pad selection");
        for (int at = 0; at < 30; at++) Read();
        Check(DataHelper.Reads == reads && gate.State == 0 && Counters.GameCalls == 0, "Current full-story conditions update without Lua reload, game judgment, RNG or applying a result");

        Reset(); gate = Condition("training_npc", 1);
        ChoiceStorySource.ObserveScript(start + "statmodifymanager.SetFlag('training_npc',0);if checkpointmanager.Condition('training_npc') then " + npc + " end end", "training_write_before_condition.LuaScript");
        Position("practice+free+practice", "수련"); row = Linked(Ready(true), "수련");
        Check(row.Status == "진행 가능성 있음" && !row.Available && gate.State == 1, "A preceding write to the condition dependency keeps a currently false NPC gate possible and never executes that write");

        Reset(); gate = Condition("training_npc", 1);
        ChoiceStorySource.ObserveScript(start + "unknown_callback();if checkpointmanager.Condition('training_npc') then " + npc + " end end", "training_opaque_before_condition.LuaScript");
        Position("practice+free+practice", "수련"); row = Linked(Ready(true), "수련");
        Check(row.Status == "진행 가능성 있음" && !row.Available, "An opaque predecessor cannot certify either a current positive or a definitive false future NPC gate");

        Reset(); var first = Condition("first_npc", 1); var second = Condition("second_npc", 0);
        DataHelper.Scripts["training_npc_a"] = npc; DataHelper.Scripts["training_npc_b"] = npc;
        ChoiceStorySource.ObserveScript(start + "if checkpointmanager.Condition('first_npc') then luamanager.SetNextScript('training_npc_a') elseif checkpointmanager.Condition('second_npc') then luamanager.SetNextScript('training_npc_b') end end", "training_alternatives.LuaScript");
        Position("practice+free+practice", "수련"); row = Linked(Ready(true), "수련");
        Check(row.Status == "진행 가능" && row.Available, "One safely positive full conversation path is sufficient among same-option alternatives");
        second.State = 1; row = Linked(Read(true), "수련");
        Check(row.Status == "지금 진행 불가" && !row.Available, "All completely observed conversation paths must be false before denying the current opportunity");
        CheckPointManager.Instance._condition._list.RemoveAt(1); row = Linked(Read(true), "수련");
        Check(row.Status == "진행 가능성 있음" && !row.Available, "A missing alternative checkpoint keeps the aggregate possible instead of borrowing a true utility guard");
        Check(first.State == 1 && second.State == 1 && Counters.GameCalls == 0, "Alternative aggregation neither modifies flags nor invokes the native condition chain");

        Reset(); gate = Condition("training_npc", 1);
        DataHelper.Scripts["bounded_npc"] = npc + ";luamanager.SetNextScript('bounded_tail_0')";
        for (int at = 0; at < 40; at++) DataHelper.Scripts["bounded_tail_" + at] = "luamanager.SetNextScript('bounded_tail_" + (at + 1) + "')";
        ChoiceStorySource.ObserveScript(start + "if checkpointmanager.Condition('training_npc') then luamanager.SetNextScript('bounded_npc') end end", "training_late_cap.LuaScript");
        Position("practice+free+practice", "수련");
        bool found = false;
        for (int at = 0; at < 100; at++)
        {
            value = Read(true); row = value.Entries.FirstOrDefault(item => item.Title == "수련");
            if (row != null)
            {
                found = true;
                Check(!row.Available && row.Status != "진행 가능", "A false full-story prefix never becomes green while later literal descendants reach the source-script limit");
            }
        }
        Check(found && gate.State == 1 && value.Entries.Any(item => item.Status.Contains("확인", StringComparison.Ordinal) || item.Status == "진행 가능성 있음"),
            "A later source limit keeps recognized conversation evidence explicitly incomplete without changing its predicate");

        var negative = new MapRoutePath("negative", "negative", true, false, PreviewConditions.Constant(false));
        var positive = new MapRoutePath("positive", "positive", true, false, PreviewConditions.Constant(true));
        var paths = new List<MapRoutePath>(); for (int at = 0; at < 32; at++) paths.Add(negative); paths.Add(positive);
        Check(ChoiceStorySource.CurrentStoryConditions(paths, new MapRouteRead()) == MapRouteState.Unknown, "A thirty-third unread conversation alternative cannot be used to deny or certify the current option");
        paths[0] = positive;
        Check(ChoiceStorySource.CurrentStoryConditions(paths, new MapRouteRead()) == MapRouteState.Met, "A safely proven retained full path is sufficient even when later alternatives exceed the per-query gate budget");
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
            (Name: "center_001", Raw: "practice+free+practice", Title: "연무장 수련", Position: true, Expected: true),
            (Name: "room_001", Raw: "sleep+free+sleep", Title: "휴식", Position: true, Expected: false),
            (Name: "ch1_1_break_option", Raw: "talk+O_1_1_Break_002+0+0", Title: "실제 player-only talk 항목", Position: false, Expected: false),
            (Name: "ch2_4_6_2_break_start_01", Raw: "talk+O_2_4_6_2_Break_01_001+0+0", Title: "실제 D_ 독백 후속 항목", Position: false, Expected: false),
            (Name: "s0121_01", Raw: "O_S0121_01_001", Title: "실제 전투 연결 이야기 A", Position: false, Expected: true),
            (Name: "s0604_01", Raw: "O_S0604_01_002", Title: "실제 전투 연결 이야기 B", Position: false, Expected: true)
        };
        foreach (var sample in cases)
        {
            string name = sample.Name, title = sample.Title;
            Reset();
            if (name == "center_001")
            {
                // Synthetic runtime data only: the actual game's checkpoint
                // configuration and the user's current save are not supplied.
                CheckPointManager.Instance._switchList = new object[] { new SwitchResultConfig { _list = new List<SwitchResultData> {
                    new SwitchResultData { name = "Center_001_001", _items = new List<ConditionResultItem> {
                        new ConditionResultItem { _items = new List<StatCompareItem> { new StatCompareItem {
                            _value1 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = 0 } } },
                            _value2 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = 1 } } }
                        } } }
                    } }
                } } };
            }
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
                value = Read(name == "center_001"); row = value.Entries.FirstOrDefault(item => item.Title == title);
                if (name == "center_001") Check(row == null || !row.Available && !row.Status.Contains("조건 충족", StringComparison.Ordinal),
                    "Actual center_001 plus an explicitly never-met synthetic runtime switch cannot become green while descendants are preparing or capped");
                if (sample.Expected && name != "center_001" && row != null && (name != "room_001" || row.AutoText.Contains("→", StringComparison.Ordinal))) break;
            }
            if (!sample.Expected)
            {
                Check(row == null && value.Hints.Count == 0, "Actual " + name + " " + sample.Raw + " has no automatic NPC-conversation row: narration, player-only dialogue and a talk tag are insufficient proof");
                Check(!value.Entries.Any(item => item.Kind == "인물 대화"), "Excluded actual corpus activity cannot be presented through another automatic entry");
                continue;
            }
            Check(row != null && (name != "room_001" || row.AutoText.Contains("→", StringComparison.Ordinal)), "Actual " + name + " corpus preserves its known choice labels despite visual/runwait uncertainty");
            if (name == "center_001")
                Check(row.Status == "지금 진행 불가" && row.AutoText.Length == 0 && !value.Hints.Any(item => item.Title == title) && !Read().Entries.Any(item => item.Title == title),
                    "the original training script's proven false NPC prerequisite is detail-only, including visual prelude and disjoint action bookkeeping");
            Check(!row.Available && (row.Status == "진행 가능성 있음" || row.Status == "지금 진행 불가"), "Actual " + name + " incomplete full-story evidence cannot be advertised as currently met");
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
    private static void NativeCurrentGuardChanges(string indexPath)
    {
        var paths = new Dictionary<string, (string Path, string Sha)>(StringComparer.OrdinalIgnoreCase);
        using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(indexPath)))
            foreach (JsonElement item in document.RootElement.EnumerateArray()) paths[item.GetProperty("name").GetString()] = (item.GetProperty("path").GetString(), item.GetProperty("sha256").GetString());
        var failures = new List<string>();
        foreach (var sample in new[] {
            (Script: "girlroom_001", Raw: "talk+talk+sister1", Guard: "Talk_Sister1_00", Position: true),
            (Script: "room_001", Raw: "talk+talk+brother4", Guard: "Talk_Brother4", Position: true),
            (Script: "talk_brother4", Raw: "talk+O_Talk_Brother4_003+0+0", Guard: "Talk_Brother4_00_01", Position: false)
        })
        {
            Reset();
            // Only script bytes are actual game evidence. This explicit test
            // configuration models a current required flag AND an unknown RNG
            // comparison; the user's save and native config are not supplied.
            var flag = Condition(sample.Guard, 0);
            var guard = CheckPointManager.Instance._condition._list.Find(item => item.name == sample.Guard)._items[0];
            guard._items.Insert(0, new StatCompareItem {
                _value1 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 7 } } },
                _value2 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = 1 } } }
            });
            Condition("Talk_Brother4_00_02", 0);
            var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ScriptReadResult Exact(string key)
            {
                if (cache.TryGetValue(key, out string body)) return ScriptReadResult.Success(body);
                if (!paths.TryGetValue(key, out var record)) return new ScriptReadResult { State = ScriptReadState.Missing };
                byte[] bytes = File.ReadAllBytes(record.Path);
                if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(record.Sha, StringComparison.OrdinalIgnoreCase))
                { failures.Add(key); return new ScriptReadResult { State = ScriptReadState.Failed }; }
                cache[key] = body = new UTF8Encoding(false, true).GetString(bytes); return ScriptReadResult.Success(body);
            }
            DataHelper.ExternalReader = Exact; var root = Exact(sample.Script);
            Check(root.State == ScriptReadState.Success, "The current-guard transition observes the SHA-pinned actual " + sample.Script + " bytes");
            ChoiceStorySource.ObserveScript(root.Text, sample.Script + ".LuaScript");
            if (sample.Position) Position(sample.Raw, "완료 대상"); else Break(sample.Raw, "완료 대상");
            BreakOptionButton other = null;
            if (!sample.Position)
            { other = Break("talk+O_Talk_Brother4_004+0+0", "다른 활성 대화"); Break("talk+O_Talk_Brother4_005+0+0", "다른 불명 대화"); }
            ForceCollection(); ChoiceStorySnapshot value = null;
            for (int at = 0; at < 160; at++) value = Read(true);
            var before = Linked(value, "완료 대상");
            Check(before.Status == "진행 가능성 있음" && !before.Available, "A true flag paired with an unknown random input never promotes the first menu guard into full-story readiness");
            int resources = DataHelper.Reads; flag.State = 1;
            value = Read(true); var after = Linked(value, "완료 대상");
            string expected = sample.Position ? "지금 진행 불가" : "진행 가능성 있음";
            Check(after.Id == before.Id && after.Status == expected && !after.Available && (!sample.Position || after.Detail.Contains("미충족", StringComparison.Ordinal)),
                "The fresh actual " + sample.Script + " guard uses only the closed root profile for negative suppression; a nonprofile talk menu remains unknown rather than reinterpreting a previously captured guard; actual=" + after.Status);
            for (int at = 0; at < 40; at++) { value = Read(true); Check(Linked(value, "완료 대상").Status == expected, "Warm scheduled refreshes preserve the current profile's safe negative/unknown distinction"); }
            Check(DataHelper.Reads == resources && flag.State == 1 && Counters.GameCalls == 0, "The transition reuses definitions without reload, native judgments, RNG or flag execution");
            if (other != null)
            {
                Check(Linked(value, "다른 활성 대화").Status != "지금 진행 불가" && Linked(value, "다른 불명 대화").Status == "진행 가능성 있음", "Completing one real brother4 topic preserves another active topic and an unknown topic");
                EventSystem.current.currentSelectedGameObject = other.gameObject;
                Check(ChoiceStorySource.CurrentRow(value)?.Title == "다른 활성 대화", "The retained alternative still follows passive native pad focus without hover");
            }
        }
        Check(failures.Count == 0, "Every original and followup source used in the current-guard transitions matches its corpus SHA-256");
    }
    private static void CurrentRequiredChoice()
    {
        Reset(); var flag = Condition("completed_option", 0);
        ChoiceStorySource.ObserveScript("runwait(flowcharts.LoadView('room'));getvar(flowcharts.view,'ViewName').value='room';runblock(flowcharts.view,'view');mainui.DisplayTitle('P_Room_01');setmenudialog(menudialogs.Room);local a={};a[1]=checkpointmanager.Condition('completed_option') and 'O_topic' or '~x';a[2]='O_other';local r=choose(a);if r==1 or r==2 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_current_option')) end", "current_required_choice.LuaScript");
        Normal("O_topic", "완료 대상"); var other = Normal("O_other", "다른 항목");
        var value = Ready(true); var before = Linked(value, "완료 대상");
        Check(before.Status == "진행 가능성 있음" && !before.Available, "A current true option guard cannot promote an opaque full path into a confirmed positive");
        int resources = DataHelper.Reads; flag.State = 1; value = Read(true); var after = Linked(value, "완료 대상");
        Check(after.Id == before.Id && after.Status == "지금 진행 불가" && after.Detail.Contains("미충족", StringComparison.Ordinal), "The current required option's negative survives the global partial/opaque uncertainty boundary");
        Check(Linked(value, "다른 항목").Status == "진행 가능성 있음", "An independent unknown path is retained when another option is completed");
        EventSystem.current.currentSelectedGameObject = other.gameObject;
        Check(ChoiceStorySource.CurrentRow(value)?.Title == "다른 항목" && DataHelper.Reads == resources && flag.State == 1 && Counters.GameCalls == 0, "Current negative filtering preserves passive pad focus and performs no script read or game execution");

        Reset(); flag = Condition("guard_before_write", 1);
        ChoiceStorySource.ObserveScript("local allowed=checkpointmanager.Condition('guard_before_write');statmodifymanager.SetFlag('guard_before_write',1);local a={};a[1]=allowed and 'O_topic' or '~x';local r=choose(a);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_saved_guard')) end", "guard_saved_before_write.LuaScript");
        Normal("O_topic", "쓰기 전 저장한 활성 조건"); value = Ready(true);
        Check(Linked(value, "쓰기 전 저장한 활성 조건").Status == "진행 가능성 있음" && !Linked(value, "쓰기 전 저장한 활성 조건").Available,
            "A menu guard captured before a gameplay flag write cannot be reinterpreted from the post-write current value as a disabled option");
    }
    private static void Main(string[] args)
    {
        Labels(); LiveMenus(); StateAndLifetimes(); RetryCadence(); MeaningfulDialogue(); ActivityResponses(); FullStoryConditions(); CurrentRequiredChoice(); FixtureOwnership(); OpportunityMenus(); TooltipConditions();
        if (args.Length > 0) Metadata(args[0]);
        if (args.Length > 1) { NativeCorpus(args[1]); NativeCurrentGuardChanges(args[1]); NativeTravelOpportunityPolicy(args[1]); }
        Console.WriteLine("PASS: ChoiceStoryTests " + checks + " production UI source/labels/route/syntax/current-reader/refresh assertions; explicit Unity/game/resource fixtures, no Lua/Windows/native UI execution.");
    }
}
namespace Mortal.Story
{
    // Exact reflection boundaries used by PreviewConditions. Neither native
    // Switch nor Condition is called, and count-based switches stay unknown.
    public sealed class SwitchResultConfig { public List<SwitchResultData> _list = new List<SwitchResultData>(); }
    public sealed class SwitchResultData : UnityEngine.Object
    { public bool _useCount; public int _count; public List<Mortal.Core.ConditionResultItem> _items = new List<Mortal.Core.ConditionResultItem>(); }
}
