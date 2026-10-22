using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.EventSelection;
using DiceMaster.LiveLog;
using DiceMaster.LuaParse;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;
using UnityEngine;
using LuaAction = DiceMaster.LuaParse.Action;

// The production provider, route engine, syntax, current reader and one-shot
// proof execute together. Resource/Unity/game objects and the effect formatter
// are the same explicit boundaries used by MapInformationTests; Lua is not run.
internal static partial class Program
{
    private static int checks;
    private static object observedTalkEnvironment;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static StatGroupVariable Constant(int value)
    { return new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = value } } }; }
    private static ConditionResultItem IsZero(FlagData flag)
    { return new ConditionResultItem { _items = new List<StatCompareItem> { new StatCompareItem {
        _value1 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 3, _flag = flag } } }, _value2 = Constant(0) } } }; }
    private static InformationRow Find(MapGuideSnapshot value, string title)
    { foreach (var row in value.Entries) if (row.Title == title) return row; return null; }
    private static bool ResearchStory(MapGuideSnapshot value, string title)
    { return value.Entries.Exists(row => row.Title.Contains(title, StringComparison.Ordinal) && row.Kind.Contains("인물 대화", StringComparison.Ordinal)) && !value.Summary.Opportunity; }
    private static bool HasStatus(MapGuideSnapshot value, string text)
    { foreach (var row in value.Entries) if (row.Status.Contains(text, StringComparison.Ordinal)) return true; return false; }
    private static MapGuideSnapshot Read(bool details = true)
    {
        var queries = MapGuideSource.Discover(true, true, details);
        Check(queries.Count == 1, "One provider query owns summary and every detail entry");
        return (MapGuideSnapshot)queries[0].Read();
    }
    private static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--map-guidance-benchmark") { SelectedStoryBenchmark(); return; }
        MapGuideSource.Clear(); OneShotProof.Clear(); MapGuideSource.Enabled = () => true; Time.unscaledTime = 0;
        DataHelper.Scripts.Clear(); PositionEventCache.Parser.Actions.Clear();
        var gateFlag = new FlagData { name = "entry-gate", State = 0 };
        var onceFlag = new FlagData { name = "once-flag", State = 0 };
        var manager = CheckPointManager.Instance = new CheckPointManager();
        manager._condition._list.Add(new ConditionResultData { name = "entry_gate", _items = new List<ConditionResultItem> { IsZero(gateFlag) } });
        var once = new PositionEventData { name = "한 번의 기회", ResultType = "once", _activeCondition = IsZero(onceFlag) };
        var ordinary = new PositionEventData { name = "일반 활동", ResultType = "ordinary", _toggleCondition = false };
        manager._position._list.Add(new PositionResultData { name = "Room_Work", _scriptPrefix = "room_result", _list = new List<PositionEventData> { once, ordinary } });
        DataHelper.Scripts["room_root"] = new string(' ', 10000) +
            "local choices={'수면','취소'}; local result=choose(choices); " +
            "if result==1 then if checkpointmanager.Condition('entry_gate') then luamanager.SetNextScript('room_break') end end";
        DataHelper.Scripts["room_break"] = "local result=checkpointmanager.Position('Room_Work'); luamanager.SetNextScript(result)";
        DataHelper.Scripts["room_result_once"] = "";
        DataHelper.Scripts["room_result_ordinary"] = "";
        PositionEventCache.Parser.Actions["room_result_once"] = new List<LuaAction> { new LuaAction { Analysis = new PreviewAnalysis { Effects = new List<PreviewEffect> {
            new PreviewEffect { Name = "SetFlag", Constants = new List<object> { "once-flag", 1 } } } } } };
        var controller = new PositionController { _positionData = new FreePositionData { name = "Room", _defaultScript = "room_root" }, _scriptName = "old-click-cache" };
        MapGuideSource.Register(controller); MapGuideSource.Select(controller);
        var first = Read();
        Check(first.HasSelection && first.Summary != null && HasStatus(first, "경로 준비 중"), "Cold lexical preparation yields before the full room menu is analyzed");
        Check(Find(first, "한 번의 기회") == null, "An incomplete definition prefix does not invent a terminal event");
        var value = first;
        int slices = 0;
        while ((HasStatus(value, "준비 중") || Find(value, "한 번의 기회") == null) && slices++ < 30)
        { Time.unscaledTime += .25f; value = Read(); }
        Check(slices < 30, "Room choice/followup/pool preparation completes over scheduled reads");
        var row = Find(value, "한 번의 기회");
        Check(row != null && row.Kind.Contains("선택 장소", StringComparison.Ordinal), "Actual route terminal reaches an individual provider detail row");
        Check(row.Status.Contains("선택 후 기회", StringComparison.Ordinal) && row.Status.Contains("1회성", StringComparison.Ordinal), "Choice reachability and strict one-shot proof are combined without becoming immediate execution");
        Check(row.Detail.Contains("room_root", StringComparison.Ordinal) && row.Detail.Contains("room_break", StringComparison.Ordinal) && row.Detail.Contains("Room_Work", StringComparison.Ordinal), "Original room choice and followup pool path survive into the detail text");
        Check(value.Summary.Status.Contains("확인된 1회성 1", StringComparison.Ordinal) && value.Summary.AutoText.Contains("한 번의 기회", StringComparison.Ordinal), "The same snapshot provides the one-shot count and selected summary name");
        Check(!value.Summary.AutoText.Contains("fixture effects", StringComparison.Ordinal) && !value.Summary.AutoText.Contains("fixture condition", StringComparison.Ordinal), "Automatic surface exposes names/kinds/status only");
        Check(Find(value, "일반 활동").Kind.Contains("기타 연결 정보", StringComparison.Ordinal) && !value.Summary.AutoText.Contains("일반 활동", StringComparison.Ordinal), "Ordinary terminal remains a separate detail row without inflating the default summary");
        Check(row.Id != Find(value, "일반 활동").Id, "Independent terminal rows have distinct identities");
        int reads = DataHelper.Reads, parses = LuaParser.Parses, descriptions = LuaParser.Descriptions;
        for (int i = 0; i < 40; i++) { Time.unscaledTime += .25f; value = Read(); }
        Check(DataHelper.Reads == reads && LuaParser.Parses == parses && LuaParser.Descriptions == descriptions, "Warm actual route/provider refresh never reloads or reparses scripts or formats unchanged effects: reads " + reads + "->" + DataHelper.Reads + ", parses " + parses + "->" + LuaParser.Parses + ", descriptions " + descriptions + "->" + LuaParser.Descriptions);
        string rowId = Find(value, "한 번의 기회").Id;
        controller._positionData._conditionFlags = new[] { new PositionConditionFlagItem { _condition = IsZero(gateFlag) } };
        value = Read(); row = Find(value, "한 번의 기회");
        Check(!row.Available && row.Status.Contains("진입 시 상태 변경 가능", StringComparison.Ordinal) && value.Summary.Status.Contains("확인된 1회성 0", StringComparison.Ordinal), "Actual route/provider integration rejects current opportunity certainty across native activity entry flag writes");
        Check(row.Detail.Contains("현재 이벤트 판정", StringComparison.Ordinal) && row.Detail.Contains("확정 상수 플래그", StringComparison.Ordinal), "Entry barrier keeps the current event snapshot and strict property proof as distinct details");
        gateFlag.State = 1; onceFlag.State = 1; value = Read(); row = Find(value, "한 번의 기회");
        Check(!row.Available && row.Status.Contains("경로 확인 필요", StringComparison.Ordinal) && row.Detail.Contains("조건·가중치 미충족", StringComparison.Ordinal), "Pre-entry writes prevent current false route/event snapshots from becoming definitive future rejection");
        gateFlag.State = 0; onceFlag.State = 0; controller._positionData._conditionFlags = null;
        Check(Find(Read(), "한 번의 기회").Status.Contains("진입 전 변경 여부 확인 불가", StringComparison.Ordinal), "Null native entry boundary is not treated as an empty safe array");
        controller._positionData._conditionFlags = Array.Empty<PositionConditionFlagItem>();
        value = Read();
        Check(Find(value, "한 번의 기회").Available && DataHelper.Reads == reads && LuaParser.Parses == parses, "Replacing the same asset flag array with empty restores availability without resource reload or Lua reanalysis");
        gateFlag.State = 1; Time.unscaledTime += .25f; value = Read();
        row = Find(value, "한 번의 기회");
        Check(row.Id == rowId && row.Status.Contains("경로 조건 미충족", StringComparison.Ordinal) && !row.Available, "Fresh field-only checkpoint snapshot changes reachability without rebuilding the route identity");
        Check(value.Summary.Status.Contains("확인된 1회성 0", StringComparison.Ordinal), "Known one-shot on a currently false route is excluded from current opportunity count");
        Check(!value.Summary.AutoText.Contains("한 번의 기회", StringComparison.Ordinal) && row.Detail.Contains("확정 상수 플래그", StringComparison.Ordinal),
            "A blocked proven one-shot is omitted from the actual route's automatic map summary while its proof remains in detail");
        Check(DataHelper.Reads == reads && LuaParser.Parses == parses, "Current condition change does not invalidate definition caches");
        gateFlag.State = 0; Time.unscaledTime += .25f; value = Read();
        Check(Find(value, "한 번의 기회").Available, "Current route safely recovers without executing a game condition");
        int beforeHidden = DataHelper.Reads; controller.gameObject.activeInHierarchy = false;
        Check(MapGuideSource.Discover(true, true).Count == 0 && DataHelper.Reads == beforeHidden, "Hidden actual route/provider session performs no reads");
        controller.gameObject.activeInHierarchy = true; MapGuideSource.Enabled = () => false;
        Check(MapGuideSource.Discover(true, true).Count == 0 && DataHelper.Reads == beforeHidden, "OFF stops the integrated route/provider query");
        Check(Counters.GameCalls == 0 && onceFlag.State == 0 && controller._gameStatManager._stats._list[0]._value == 3 && controller._scriptName == "old-click-cache", "Integration never calls game Position/Condition/Check, applies effects, consumes action points or initializes the click cache");
        CachedMarkerRoutes(controller, gateFlag, onceFlag);
        Limits();
        PassiveCosts();
        PassiveRouteReads();
        ChoiceStoryMap();
        MeaningfulDialogueMap();
        FullStoryGateMap();
        CurrentRequiredGuardMap();
        IndividualTopicsAndActivitiesMap();
        SelectedFollowupProjection();
        SelectedStoryScanCases();
        CurrentTalkContextCases();
        PositionEntryProjectionCases();
        GuidanceMarkerCases();
        ColdMapDefinitionCases();
        MapLogDiagnosticCases();
        FileDefinitionCases();
        string corpus = Environment.GetEnvironmentVariable("DICEMASTER_CORPUS_INDEX");
        if (!string.IsNullOrEmpty(corpus)) { NativeCurrentGuardMap(corpus); NativeCompletedTopicsMap(corpus); NativeSelectedStoryScan(corpus); }
        Console.WriteLine("PASS: MapGuideRouteIntegrationTests " + checks + " production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed.");
    }
    private static void CachedMarkerRoutes(PositionController controller, FlagData gate, FlagData once)
    {
        MapGuideSource.Enabled = () => true;
        var cold = new PositionController { _positionData = new FreePositionData { name = "Kitchen", _defaultScript = "marker_never_load" }, _position = PositionType.Kitchen,
            _missionsManager = controller._missionsManager, _gameStatManager = controller._gameStatManager, _freeManager = controller._freeManager };
        MapGuideSource.Register(cold); MapGuideSource.Deselect(controller);
        int resources = DataHelper.Reads, parses = LuaParser.Parses, descriptions = LuaParser.Descriptions;
        MapMarkerSnapshot Markers() { Time.unscaledTime += .25f; return (MapMarkerSnapshot)MapGuideSource.DiscoverMarkers(false, true)[0].Read(); }
        MapMarkerSnapshot value = null; for (int at = 0; at < 10; at++) value = Markers();
        Check(value.Entries.Count == 1 && ReferenceEquals(value.Entries[0].Anchor, controller) && value.Entries[0].AutoText.Contains("한 번의 기회"), "unselected prepared one-shot can publish a place marker while an unprepared place remains silent");
        once.State = 1; Check(Markers().Entries.Count == 0, "completed one-shot disappears from all-place markers using fresh event eligibility");
        once.State = 0; gate.State = 1; Check(Markers().Entries.Count == 0, "known one-shot property cannot bypass a currently false route condition");
        gate.State = 0; controller._positionData._conditionFlags = new[] { new PositionConditionFlagItem { _condition = IsZero(gate) } };
        Check(Markers().Entries.Count == 0, "native entry flag writes prevent a guaranteed one-shot dot before arrival"); controller._positionData._conditionFlags = Array.Empty<PositionConditionFlagItem>();
        for (int at = 0; at < 100; at++) value = Markers();
        Check(value.Entries.Count == 1 && DataHelper.Reads == resources && LuaParser.Parses == parses && LuaParser.Descriptions == descriptions, "one hundred all-place marker ticks reuse cached proof and syntax without loading cold place Lua or effect text");
        controller.gameObject.activeInHierarchy = false;
        Check(MapGuideSource.MarkerSessionVisible && Markers().Entries.Count == 0, "another live registered place preserves marker session while a hidden prepared place cannot retain dots");
        controller.gameObject.activeInHierarchy = true;
        Check(Counters.GameCalls == 0 && once.State == 0 && gate.State == 0, "fresh marker reachability leaves original flags and game judgments unchanged");
    }
    private static void ChoiceStoryMap()
    {
        MapGuideSource.Clear(); MapGuideSource.Enabled = () => true;
        var firstFlag = new FlagData { name = "talk-entry", State = 0 };
        var laterFlag = new FlagData { name = "talk-later", State = 1 };
        var manager = CheckPointManager.Instance = new CheckPointManager();
        manager._condition._list.Add(new ConditionResultData { name = "talk_entry", _items = new List<ConditionResultItem> { IsZero(firstFlag) } });
        manager._condition._list.Add(new ConditionResultData { name = "talk_later", _items = new List<ConditionResultItem> { IsZero(laterFlag) } });
        DataHelper.Scripts["choice_map_root"] = "runwait(function() end); local gate=checkpointmanager.Condition('talk_entry'); local room={}; room[1]='휴식'; room[2]=gate and '사사형' or '~x'; local r=choose(room); if r==2 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('talk_hello')); statmodifymanager.SetFlag('talk-later',0); luamanager.SetNextScript('choice_map_talk') end";
        DataHelper.Scripts["choice_map_talk"] = "local gate=checkpointmanager.Condition('talk_later'); local opts={}; opts[1]=gate and '대화 항목' or '~x'; local r=choose(opts); if r==1 then luamanager.SetNextScript('choice_map_story') end";
        DataHelper.Scripts["choice_map_story"] = "setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('story_line'))";
        var controller = new PositionController { _positionData = new FreePositionData { name = "Room", _defaultScript = "choice_map_root" } };
        MapGuideSource.Select(controller); MapGuideSnapshot snapshot = null;
        int formatter = LuaParser.Descriptions, parses = LuaParser.Parses;
        for (int at = 0; at < 30; at++) { Time.unscaledTime += .25f; snapshot = Read(true); }
        var story = Find(snapshot, "사사형");
        Check(story != null && story.Kind.Contains("인물 대화"), "Literal and inline selected dialogue becomes a map story without fabricating an event pool");
        Check(story.Status == "진행 가능성 있음" && !story.Available, "An opaque predecessor keeps the whole dialogue condition uncertain instead of borrowing the first option's true guard");
        Check(ResearchStory(snapshot, "사사형"), "Uncertain future conversation is retained for explicit research and omitted from automatic map advice");
        Check(!snapshot.Summary.AutoText.Contains("story_line") && !snapshot.Summary.AutoText.Contains("talk-later"), "Automatic story information excludes dialogue text and condition internals");
        Check(!story.Status.Contains("1회성"), "Choice route discovery never grants a one-shot property");
        Check(LuaParser.Descriptions == formatter && LuaParser.Parses == parses, "Passive choice story map performs zero full effect descriptions or analyzers");
        snapshot = Read(true); story = Find(snapshot, "사사형");
        Check(!story.Detail.Contains("사사형 → 대화 항목") && story.Detail.Contains("해금 여부를 확인하지 못한") && story.Detail.Contains("내부 분기 조건"), "Map detail excludes future topic names affected by a preceding write while retaining its condition scope");
        int firstReads = firstFlag.StateReads, laterReads = laterFlag.StateReads;
        for (int at = 0; at < 40; at++)
        {
            Time.unscaledTime += .25f; snapshot = Read(false);
            Check(Find(snapshot, "사사형") == null && !snapshot.Summary.AutoText.Contains("사사형"),
                "A prior research publication cannot leak a downstream NPC story into passive map rows or notices");
        }
        Check(firstFlag.StateReads == firstReads && laterFlag.StateReads == laterReads,
            "Closing actual-route research stops downstream NPC current-condition reads on every passive refresh");
        int resourceReads = DataHelper.Reads;
        for (int at = 0; at < 40; at++) { Time.unscaledTime += .25f; snapshot = Read(true); }
        Check(DataHelper.Reads == resourceReads && LuaParser.Parses == parses, "Warm story refresh reuses route definitions without resource reads or full Lua analysis");
        firstFlag.State = 1; snapshot = Read(true); story = Find(snapshot, "사사형");
        Check(story.Status == "진행 가능성 있음" && !story.Available, "A false observed entry condition on an incomplete route does not deny potentially omitted alternative paths");
        Check(laterFlag.State == 1 && firstFlag.State == 1 && Counters.GameCalls == 0, "Neither discovery nor path preview applies the intervening flag write or calls game judgments");
        firstFlag.State = 0; controller._gameStatManager._stats._list[0]._value = 0; snapshot = Read(true);
        Check(!Find(snapshot, "사사형").Available && Find(snapshot, "사사형").Status.Contains("행동 부족"), "Current first guard does not override native map action availability");
        controller._gameStatManager._stats._list[0]._value = 3;
        controller._positionData._defaultScript = "choice_labels";
        DataHelper.Scripts["choice_labels"] = "local opts={}; if checkpointmanager.Condition('talk_entry') then opts[1]='선택 A' else opts[1]='선택 B' end; local r=choose(opts); setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('line'))";
        firstFlag.State = 1;
        for (int at = 0; at < 8; at++) { Time.unscaledTime += .25f; snapshot = Read(true); }
        Check(Find(snapshot, "선택 A") != null && !Find(snapshot, "선택 A").Available && Find(snapshot, "선택 B").Available, "Same original choose index with alternative raw labels stays in separate groups and cannot mark the wrong title ready");
        Check(Find(snapshot, "선택 A").Status == "지금 진행 불가", "A complete supported false full story path is blocked even when its display-only speaker call was structurally partial");
        snapshot = Read(true);
        Check(!snapshot.Summary.AutoText.Contains("선택 A", StringComparison.Ordinal) && ResearchStory(snapshot, "선택 B") && Find(snapshot, "선택 A").Detail.Contains("선택 경로", StringComparison.Ordinal),
            "The actual choice-route map hides its false dialogue notice while retaining the positive alternative and the false route's detail");
    }
    private static void MeaningfulDialogueMap()
    {
        MapGuideSource.Clear(); MapGuideSource.Enabled = () => true;
        CheckPointManager.Instance = new CheckPointManager();
        DataHelper.Scripts["activity_filter"] = "local a={'휴식','훈련','사사형'};local r=choose(a);if r==1 then setcharacter(narrative);say(luamanager.GetStoryText('N_rest'));setcharacter(characters.Get('player'));say(luamanager.GetStoryText('D_rest')) elseif r==2 then say(luamanager.GetStoryText('N_train')) elseif r==3 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_brother')) end";
        var controller = new PositionController { _positionData = new FreePositionData { name = "Room", _defaultScript = "activity_filter" } };
        MapGuideSource.Select(controller); MapGuideSnapshot value = null;
        for (int at = 0; at < 15; at++) { Time.unscaledTime += .25f; value = Read(true); }
        Check(Find(value, "사사형")?.Kind.Contains("인물 대화", StringComparison.Ordinal) == true, "Map keeps a selected literal NPC-speaker conversation alongside repeated utility choices");
        Check(Find(value, "휴식") == null && Find(value, "훈련") == null && !value.Summary.AutoText.Contains("휴식", StringComparison.Ordinal) && !value.Summary.AutoText.Contains("훈련", StringComparison.Ordinal), "Basic rest/player D-lines and training narration do not become automatic map story names");

        DataHelper.Scripts["rest_followup"] = "local a={'휴식'};local r=choose(a);if r==1 then setcharacter(narrative);say(luamanager.GetStoryText('N_intro'));luamanager.SetNextScript('late_conversation') end";
        DataHelper.Scripts["late_conversation"] = "setcharacter(characters.Get('player'));say(luamanager.GetStoryText('D_thought'));setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_npc'))";
        controller._positionData._defaultScript = "rest_followup";
        for (int at = 0; at < 15; at++) { Time.unscaledTime += .25f; value = Read(true); }
        var rest = Find(value, "휴식");
        Check(rest?.Kind.Contains("인물 대화", StringComparison.Ordinal) == true && rest.Detail.Contains("선택 경로: 휴식", StringComparison.Ordinal), "Rest that reaches a real NPC line remains a conditional conversation path rather than being pruned with repeated rest");
        Check(rest.Status == "진행 가능" && !rest.Status.Contains("1회성", StringComparison.Ordinal), "Static NPC evidence preserves current first-guard semantics and grants no one-shot property");
        int reads = DataHelper.Reads, parses = LuaParser.Parses, descriptions = LuaParser.Descriptions;
        for (int at = 0; at < 20; at++) { Time.unscaledTime += .25f; value = Read(true); }
        Check(DataHelper.Reads == reads && LuaParser.Parses == parses && LuaParser.Descriptions == descriptions && Counters.GameCalls == 0,
            "Classification reuses static route definitions without warm Lua reload, full effects or game judgment calls");

        DataHelper.Scripts["player_talk_only"] = "local a={'talk+O_thought+0+0'};local r=choose(a);if r==1 then setcharacter(characters.Get('player'));say(luamanager.GetStoryText('D_player_talk')) end";
        controller._positionData._defaultScript = "player_talk_only";
        for (int at = 0; at < 12; at++) { Time.unscaledTime += .25f; value = Read(true); }
        bool npcRow = false; foreach (var row in value.Entries) npcRow |= row.Kind.Contains("인물 대화", StringComparison.Ordinal);
        Check(!npcRow && !value.Summary.AutoText.Contains("O_thought", StringComparison.Ordinal), "A talk-tagged player-only menu cannot reenter the map's automatic conversation summary");
    }
    private static void FullStoryGateMap()
    {
        MapGuideSource.Clear(); MapGuideSource.Enabled = () => true;
        var manager = CheckPointManager.Instance = new CheckPointManager();
        var gate = new FlagData { name = "training-npc-gate", State = 1 };
        manager._condition._list.Add(new ConditionResultData { name = "training_npc", _items = new List<ConditionResultItem> { IsZero(gate) } });
        const string npc = "setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_training_npc'))";
        DataHelper.Scripts["training_gate_root"] = "local a={'수련'};local r=choose(a);if r==1 then if checkpointmanager.Condition('training_npc') then " + npc + " end end";
        var controller = new PositionController { _positionData = new FreePositionData { name = "Center", _defaultScript = "training_gate_root" } };
        controller._missionsManager._subMissions._list.Add(new MissionData { name = "숨김_지도임무", _name = "hidden-map-mission", _currentKey = "start",
            _missions = new[] { new MissionCheckData { name = "hidden-mission-stage", _conditions = new[] { IsZero(gate) } } } });
        MapGuideSource.Select(controller); MapGuideSnapshot value = null;
        for (int at = 0; at < 16; at++) { Time.unscaledTime += .25f; value = Read(true); }
        var row = Find(value, "수련");
        Check(row.Status == "지금 진행 불가" && !row.Available && !value.Summary.Available && !value.Summary.AutoText.Contains("조건 충족", StringComparison.Ordinal),
            "An unguarded selected training action stays blocked when its full NPC-conversation condition is false");
        Check(!value.Summary.AutoText.Contains("수련", StringComparison.Ordinal) && !value.Summary.AutoText.Contains("숨김_지도임무", StringComparison.Ordinal) && value.Entries.Exists(item => item.Title.StartsWith("숨김_지도임무", StringComparison.Ordinal)) &&
            Array.FindAll(value.Summary.AutoText.Split('\n'), line => line.StartsWith("• ", StringComparison.Ordinal)).Length == 0 &&
            !value.Summary.AutoText.Contains("지금 진행 불가", StringComparison.Ordinal) && !value.Summary.AutoText.Contains("외 ", StringComparison.Ordinal),
            "The real parser/provider retains blocked dialogue and mission detail while an all-blocked place publishes no automatic item rows");
        int resources = DataHelper.Reads, parses = LuaParser.Parses, descriptions = LuaParser.Descriptions;
        gate.State = 0; value = Read(true); row = Find(value, "수련");
        Check(row.Status == "진행 가능" && row.Available && !value.Summary.Available, "A safely observed NPC condition colors its detail row, never an otherwise empty passive panel");
        Check(ResearchStory(value, "수련"), "Even a full positive NPC story stays in explicit research rather than passive map advice");
        Check(DataHelper.Reads == resources && LuaParser.Parses == parses && LuaParser.Descriptions == descriptions,
            "Changing the live full-story condition reads the cached route without loading or fully analyzing Lua");
        controller._gameStatManager._stats._list[0]._value = 0; value = Read(true); row = Find(value, "수련");
        Check(row.Status.StartsWith("지금 진행 불가", StringComparison.Ordinal) && row.Status.Contains("행동 부족", StringComparison.Ordinal) && !row.Available,
            "A positive full-story gate still uses a blocked status after the native action-cost gate fails");
        Check(!value.Summary.AutoText.Contains("수련", StringComparison.Ordinal), "A native action-cost blocker removes even a currently positive dialogue's automatic notice");
        controller._gameStatManager._stats._list[0]._value = 3; controller._active = false; value = Read(true); row = Find(value, "수련");
        Check(row.Status.StartsWith("지금 진행 불가", StringComparison.Ordinal) && !row.Available, "Disabled places never retain a positive full-story status prefix");
        controller._active = true; controller._missionsManager.MainMissionStart = true; controller._missionsManager.MainMissionPosition = controller._position;
        controller._missionsManager.MainCheckData = new MissionCheckData { name = "priority-main", _position = controller._position };
        value = Read(true); row = Find(value, "수련");
        Check(row.Status.StartsWith("지금 진행 불가", StringComparison.Ordinal) && row.Status.Contains("주 임무 우선", StringComparison.Ordinal) && !row.Available,
            "A staged same-place main mission replaces the positive story prefix with the actual dispatch blocker");
        controller._missionsManager.MainMissionStart = false; controller._missionsManager.MainCheckData = null;

        gate.State = 1;
        DataHelper.Scripts["training_own_write"] = "local a={'수련'};local r=choose(a);if r==1 then statmodifymanager.SetFlag('training-npc-gate',0);if checkpointmanager.Condition('training_npc') then " + npc + " end end";
        controller._positionData._defaultScript = "training_own_write";
        for (int at = 0; at < 16; at++) { Time.unscaledTime += .25f; value = Read(true); }
        Check(Find(value, "수련").Status == "진행 가능성 있음" && !Find(value, "수련").Available && gate.State == 1,
            "A write before the same condition is a possible future story, never a current false or green status");
        Check(ResearchStory(value, "수련"), "A conversation with genuinely unknown future conditions remains in explicit research only");

        DataHelper.Scripts["training_foreign_write"] = "local a={'수련'};local r=choose(a);if r==1 then statmodifymanager.SetFlag('unrelated-flag',0);if checkpointmanager.Condition('training_npc') then " + npc + " end end";
        controller._positionData._defaultScript = "training_foreign_write";
        for (int at = 0; at < 16; at++) { Time.unscaledTime += .25f; value = Read(true); }
        Check(Find(value, "수련").Status == "지금 진행 불가" && !Find(value, "수련").Available,
            "An exactly known unrelated flag write does not hide a safely false NPC predicate");
        value = Read(true);
        Check(!value.Summary.AutoText.Contains("수련", StringComparison.Ordinal) && Find(value, "수련").Detail.Contains("선택 경로", StringComparison.Ordinal),
            "A proven false dialogue is filtered from automatic notices without losing the real route detail");
        Check(Counters.GameCalls == 0 && gate.State == 1, "Integrated full-story classification never executes native judgments, writes, RNG or item operations");
    }
    private static void CurrentRequiredGuardMap()
    {
        MapGuideSource.Clear(); MapGuideSource.Enabled = () => true;
        var manager = CheckPointManager.Instance = new CheckPointManager();
        var completed = new FlagData { name = "completed-dialogue", State = 0 };
        var active = new FlagData { name = "active-dialogue", State = 0 };
        manager._condition._list.Add(new ConditionResultData { name = "completed_dialogue", _items = new List<ConditionResultItem> { IsZero(completed) } });
        manager._condition._list.Add(new ConditionResultData { name = "active_dialogue", _items = new List<ConditionResultItem> { IsZero(active) } });
        DataHelper.Scripts["current_required_root"] = "runwait(flowcharts.LoadView('room'));getvar(flowcharts.view,'ViewName').value='room';runblock(flowcharts.view,'view');mainui.DisplayTitle('P_Room_01');setmenudialog(menudialogs.Room);local a={};a[1]=checkpointmanager.Condition('completed_dialogue') and '완료 대화' or '~x';a[2]=checkpointmanager.Condition('active_dialogue') and '활성 대화' or '~x';a[3]=checkpointmanager.Condition('unknown_dialogue') and '불명 대화' or '~x';local r=choose(a);if r==1 or r==2 or r==3 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_current_dialogue')) end";
        var controller = new PositionController { _positionData = new FreePositionData { name = "Hall", _defaultScript = "current_required_root" } };
        MapGuideSource.Select(controller); MapGuideSnapshot value = null;
        for (int at = 0; at < 24; at++) { Time.unscaledTime += .25f; value = Read(true); }
        Check(Find(value, "완료 대화").Status == "진행 가능성 있음" && !Find(value, "완료 대화").Available,
            "A true first-option guard does not upgrade an opaque complete-story path into a current positive");
        string identity = Find(value, "완료 대화").Id; int resources = DataHelper.Reads;
        completed.State = 1; value = Read(true);
        Check(Find(value, "완료 대화").Id == identity && Find(value, "완료 대화").Status == "지금 진행 불가" && Find(value, "완료 대화").Detail.Contains("미충족", StringComparison.Ordinal) && !value.Summary.AutoText.Contains("완료 대화", StringComparison.Ordinal),
            "A freshly false current required option gate hides a completed conversation despite opaque/global partial state, while detail explains the current negative");
        Check(ResearchStory(value, "활성 대화") && ResearchStory(value, "불명 대화") && Find(value, "불명 대화").Status == "진행 가능성 있음",
            "The negative-only filter preserves another currently active conversation and an unsupported/unknown alternative");
        for (int at = 0; at < 30; at++) { Time.unscaledTime += .25f; value = Read(true); }
        Check(DataHelper.Reads == resources && !value.Summary.AutoText.Contains("완료 대화", StringComparison.Ordinal) && completed.State == 1 && active.State == 0 && Counters.GameCalls == 0,
            "Current required-gate refresh reuses the static definition cache without stale reappearance, flag effects, RNG or native judgment calls");

        var beforeWrite = new FlagData { name = "guard-before-write", State = 1 };
        manager._condition._list.Add(new ConditionResultData { name = "guard_before_write", _items = new List<ConditionResultItem> { IsZero(beforeWrite) } });
        DataHelper.Scripts["saved_guard_before_write"] = "local allowed=checkpointmanager.Condition('guard_before_write');statmodifymanager.SetFlag('guard-before-write',1);local a={};a[1]=allowed and '쓰기 전 활성 조건' or '~x';local r=choose(a);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_saved_guard')) end";
        controller._positionData._defaultScript = "saved_guard_before_write";
        for (int at = 0; at < 20; at++) { Time.unscaledTime += .25f; value = Read(true); }
        Check(Find(value, "쓰기 전 활성 조건").Status == "지금 진행 불가" && !value.Summary.AutoText.Contains("쓰기 전 활성 조건", StringComparison.Ordinal),
            "The map previews a fresh script entry, so a currently false guard read before a later write remains a negative fresh-entry path rather than an already displayed menu's historical value");
    }
    private static void NativeCurrentGuardMap(string indexPath)
    {
        var paths = new Dictionary<string, (string Path, string Sha)>(StringComparer.OrdinalIgnoreCase);
        using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(indexPath)))
            foreach (JsonElement item in document.RootElement.EnumerateArray()) paths[item.GetProperty("name").GetString()] = (item.GetProperty("path").GetString(), item.GetProperty("sha256").GetString());
        var failures = new List<string>();
        foreach (var sample in new[] {
            (Script: "girlroom_001", Raw: "talk+talk+sister1", Guard: "Talk_Sister1_00", Center: false, Nested: false),
            (Script: "room_001", Raw: "talk+talk+brother4", Guard: "Talk_Brother4", Center: false, Nested: false),
            (Script: "center_001", Raw: "practice+free+practice", Guard: "Center_001_001", Center: true, Nested: false),
            (Script: "center_001", Raw: "practice+free+practice", Guard: "Center_001_001", Center: true, Nested: true)
        })
        {
            MapGuideSource.Clear(); OneShotProof.Clear(); MapGuideSource.Enabled = () => true;
            var manager = CheckPointManager.Instance = new CheckPointManager();
            var flag = new FlagData { name = "synthetic-" + sample.Guard, State = 0 };
            var condition = IsZero(flag);
            condition._items.Insert(0, new StatCompareItem { _value1 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 7 } } }, _value2 = Constant(1) });
            if (sample.Center)
            {
                var time = new GameTimeData { GameTime = new GameTime { Year = 1, Month = 4, Stage = 1, _roundsPerMonth = 3 } };
                condition._items.Add(new StatCompareItem { _compareOp = 5, _value1 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 9, _gameTime = time } } }, _value2 = Constant(10) });
                if (sample.Nested)
                {
                    var nested = new ConditionResultData { name = "explicit-nested-fixture", _items = new List<ConditionResultItem> { condition } };
                    condition = new ConditionResultItem { _items = new List<StatCompareItem> { new StatCompareItem {
                        _value1 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 16, _condition = nested } } },
                        _value2 = Constant(1) } } };
                }
                manager._switchList = new object[] { new SwitchResultConfig { _list = new List<SwitchResultData> {
                    new SwitchResultData { name = sample.Guard, _items = new List<ConditionResultItem> { condition } }
                } } };
                ChoiceStoryLabels.DialogueNameReader = (raw, character) => "연무장 " + character;
            }
            else manager._condition._list.Add(new ConditionResultData { name = sample.Guard, _items = new List<ConditionResultItem> { condition } });
            // The Lua bytes are original; this flag/RNG condition and empty
            // native pools are explicit test data, not the user's game config.
            var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ScriptReadResult Exact(string key)
            {
                if (cache.TryGetValue(key, out string body)) return new ScriptReadResult { State = ScriptReadState.Success, Text = body };
                if (!paths.TryGetValue(key, out var record)) return new ScriptReadResult { State = ScriptReadState.Missing };
                byte[] bytes = File.ReadAllBytes(record.Path);
                if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(record.Sha, StringComparison.OrdinalIgnoreCase))
                { failures.Add(key); return new ScriptReadResult { State = ScriptReadState.Failed }; }
                cache[key] = body = new UTF8Encoding(false, true).GetString(bytes);
                foreach (Match match in Regex.Matches(body, "checkpointmanager\\.Position\\(\"([^\"]+)\"\\)"))
                {
                    string pool = match.Groups[1].Value;
                    if (!manager._position._list.Exists(item => item.name == pool)) manager._position._list.Add(new PositionResultData { name = pool });
                }
                return new ScriptReadResult { State = ScriptReadState.Success, Text = body };
            }
            Check(Exact(sample.Script).State == ScriptReadState.Success, "Current required-gate map test uses SHA-pinned original " + sample.Script + " bytes");
            DataHelper.ExternalReader = Exact;
            var controller = new PositionController { _positionData = new FreePositionData { name = "NativeRoom", _defaultScript = sample.Script } };
            controller._gameStatManager._gameTimeData.GameTime.Month = 4; controller._gameStatManager._gameTimeData.GameTime.Stage = 1;
            MapGuideSource.Select(controller); MapGuideSnapshot value = null;
            for (int at = 0; at < 180; at++) { Time.unscaledTime += .25f; value = Read(true); }
            string title = sample.Center ? "연무장 girl5" : sample.Raw;
            var before = Find(value, title);
            Check(before != null && before.Status == "진행 가능성 있음" && ResearchStory(value, title), "The actual " + sample.Script + " NPC route remains possible with a true synthetic flag and an unknown random operand");
            int resources = DataHelper.Reads, parses = LuaParser.Parses; flag.State = 1;
            value = Read(true); var after = Find(value, title);
            Check(after != null && after.Id == before.Id && after.Status == "지금 진행 불가" && after.Detail.Contains("미충족", StringComparison.Ordinal) && !value.Summary.AutoText.Contains(title, StringComparison.Ordinal),
                "A false current required guard removes the original " + sample.Script + " NPC conversation from the map while preserving its current-negative detail; actual=" + (after == null ? "missing" : after.Status));
            if (sample.Nested)
                Check(after.Detail.Contains("Switch Center_001_001", StringComparison.Ordinal) && after.Detail.Contains("synthetic-Center_001_001 = 1", StringComparison.Ordinal) && after.Detail.Contains("난수 · 추첨하지 않음", StringComparison.Ordinal),
                    "Explicit map detail samples the exact nested checkpoint and current flag while passive text contains no diagnostics");
            for (int at = 0; at < 40; at++)
            { Time.unscaledTime += .25f; value = Read(true); Check(!value.Summary.AutoText.Contains(title, StringComparison.Ordinal), "The consumed actual NPC entry stays absent across cached scheduled map refreshes"); }
            Check(DataHelper.Reads == resources && LuaParser.Parses == parses && flag.State == 1 && Counters.GameCalls == 0, "The SHA-original map transition performs no definition reload, full analyzer work, game condition/RNG calls or flag execution");
            DataHelper.ExternalReader = null; ChoiceStoryLabels.DialogueNameReader = null;
        }
        Check(failures.Count == 0, "All root and followup scripts used for original NPC map transitions match their pinned SHA-256");
    }
    private static void IndividualTopicsAndActivitiesMap()
    {
        foreach (string npc in new[] { "fixture_mentor", "renamed_traveller" })
        {
            MapGuideSource.Clear(); MapGuideSource.Enabled = () => true;
            var manager = CheckPointManager.Instance = new CheckPointManager();
            var topic = new FlagData { name = npc + "_topic_input", State = 1 };
            var second = new FlagData { name = npc + "_second_input", State = 1 };
            manager._condition._list.Add(new ConditionResultData { name = npc + "_topic_gate", _items = new List<ConditionResultItem> { IsZero(topic) } });
            manager._condition._list.Add(new ConditionResultData { name = npc + "_second_gate", _items = new List<ConditionResultItem> { IsZero(second) } });
            manager._switchList = new object[] { new SwitchResultConfig { _list = new List<SwitchResultData> {
                new SwitchResultData { name = npc + "_eligibility", _items = new List<ConditionResultItem> { new ConditionResultItem { _items = new List<StatCompareItem> {
                    new StatCompareItem { _value1 = Constant(1), _value2 = Constant(1) } } } } }
            } } };
            string root = npc + "_root", topics = npc + "_topics", activity = npc + "_service";
            string raw = "talk+talk+" + npc;
            string speaker = "setcharacter(characters.Get('" + npc + "'));say(luamanager.GetStoryText('";
            DataHelper.Scripts[root] = "local a={'" + raw + "'};local r=choose(a);if r==1 then luamanager.SetNextScript('" + topics + "') end";
            DataHelper.Scripts[topics] = "local a={};a[1]=checkpointmanager.Condition('" + npc + "_topic_gate') and 'talk+first_topic+0+0' or '~x';a[2]=checkpointmanager.Condition('" + npc + "_second_gate') and 'talk+second_topic+0+0' or '~x';a[3]='talk+O_43+0+0';a[4]='default+O_87+0+0';local r=choose(a);if r==1 then " + speaker + "D_first')) elseif r==2 then " + speaker + "D_second')) elseif r==3 then luamanager.SetNextScript('" + activity + "') elseif r==4 then unknown_utility_callback() end";
            DataHelper.Scripts[activity] = speaker + "D_fixed_start'));local s=checkpointmanager.Switch('" + npc + "_eligibility');if s==1 then statmodifymanager.Player('money',-100,'',1);" + speaker + "D_fixed_confirm'));luamanager.SetCurrentStoryScript('" + npc + "_after');luamanager.ChangeScene('Combat','fixture_combat','Story') elseif s==2 then luamanager.ChangeScene('Free','','') end";
            DataHelper.Scripts[npc + "_after"] = "";
            var controller = new PositionController { _positionData = new FreePositionData { name = "GenericTopics", _defaultScript = root } };
            MapGuideSource.Select(controller); MapGuideSnapshot value = null;
            for (int at = 0; at < 80; at++) { Time.unscaledTime += .25f; value = Read(true); }
            Check(!value.Summary.AutoText.Contains(raw, StringComparison.Ordinal),
                "An active outer NPC menu and repeat service cannot preserve an automatic story notice after every individual topic is false; renamed NPC=" + npc + "; actual=" + value.Summary.AutoText + "; detail=" + string.Join(" || ", value.Entries.ConvertAll(item => item.Title + ":" + item.Status + ":" + item.Detail)));
            int resources = DataHelper.Reads, parses = LuaParser.Parses, descriptions = LuaParser.Descriptions;
            for (int at = 0; at < 80; at++) { Time.unscaledTime += .25f; value = Read(true); }
            Check(!value.Summary.AutoText.Contains(raw, StringComparison.Ordinal) && DataHelper.Reads == resources && LuaParser.Parses == parses && LuaParser.Descriptions == descriptions,
                "The completed-topic/service-only state remains absent across warm scheduled refreshes without resource loads or Lua reanalysis");
            topic.State = 0; value = Read(true);
            Check(ResearchStory(value, raw), "A currently open individual topic restores the NPC research story independently of the repeat service");
            Check(topic.State == 0 && second.State == 1 && Counters.GameCalls == 0, "Generic classification and topic checks never execute service, combat, native conditions, RNG or flag effects");

            var consumed = new FlagData { name = npc + "_consumed_input", State = 0 };
            manager._condition._list.Add(new ConditionResultData { name = npc + "_consumed_gate", _items = new List<ConditionResultItem> { IsZero(consumed) } });
            DataHelper.Scripts[npc + "_consumed_root"] = "local a={};a[1]=checkpointmanager.Condition('" + npc + "_consumed_gate') and '조건을 소모하는 이야기' or '~x';local r=choose(a);if r==1 then statmodifymanager.SetFlag('" + npc + "_consumed_input',1);luamanager.SetNextScript('" + activity + "') end";
            controller._positionData._defaultScript = npc + "_consumed_root";
            for (int at = 0; at < 80; at++) { Time.unscaledTime += .25f; value = Read(true); }
            Check(ResearchStory(value, "조건을 소모하는 이야기"), "A parent topic that consumes its own eligibility flag remains a story despite an otherwise mechanical paid-activity followup");
            Check(consumed.State == 0 && Counters.GameCalls == 0, "Checking the consumed-guard footprint never executes that prospective one-shot flag write");

            DataHelper.Scripts[npc + "_opaque_parent"] = "local a={'선행 동작이 불명인 이야기'};local r=choose(a);if r==1 then unknown_callback();luamanager.SetNextScript('" + activity + "') end";
            controller._positionData._defaultScript = npc + "_opaque_parent";
            for (int at = 0; at < 80; at++) { Time.unscaledTime += .25f; value = Read(true); }
            Check(ResearchStory(value, "선행 동작이 불명인 이야기"), "An opaque parent callback prevents suppressing the entire selected topic as a repeat response based only on its child's paid-activity shape");

            var special = new FlagData { name = npc + "_special_input", State = 0 };
            manager._condition._list.Add(new ConditionResultData { name = npc + "_special_gate", _items = new List<ConditionResultItem> { IsZero(special) } });
            DataHelper.Scripts[npc + "_special_root"] = "local a={'별도 이야기'};local r=choose(a);if r==1 then " + speaker + "D_fixed_intro'));if checkpointmanager.Condition('" + npc + "_special_gate') then " + speaker + "D_special'));statmodifymanager.SetFlag('" + npc + "_special_seen',1) end;luamanager.ChangeScene('Combat','fixture_story_combat','Story') end";
            controller._positionData._defaultScript = npc + "_special_root";
            for (int at = 0; at < 40; at++) { Time.unscaledTime += .25f; value = Read(true); }
            Check(ResearchStory(value, "별도 이야기"), "A separately conditioned story inside a combat activity is retained instead of blanket filtering combat");
            DataHelper.Scripts[npc + "_callback_root"] = "local a={'후속 특별 이야기'};local r=choose(a);if r==1 then luamanager.SetNextScript('" + npc + "_callback_service') end";
            DataHelper.Scripts[npc + "_callback_service"] = DataHelper.Scripts[activity].Replace(npc + "_after", npc + "_callback_special", StringComparison.Ordinal);
            DataHelper.Scripts[npc + "_callback_special"] = "if checkpointmanager.Condition('" + npc + "_special_gate') then " + speaker + "D_special_result'));statmodifymanager.SetFlag('" + npc + "_callback_seen',1) end";
            controller._positionData._defaultScript = npc + "_callback_root";
            for (int at = 0; at < 80; at++) { Time.unscaledTime += .25f; value = Read(true); }
            Check(ResearchStory(value, "후속 특별 이야기"), "An activity parent's fixed response classification cannot hide a distinct conditional story in its combat callback");
            var receipt = new FlagData { name = npc + "_receipt_input", State = 0 };
            manager._condition._list.Add(new ConditionResultData { name = npc + "_receipt_gate", _items = new List<ConditionResultItem> { IsZero(receipt) } });
            DataHelper.Scripts[npc + "_receipt_root"] = "local a={};a[1]=checkpointmanager.Condition('" + npc + "_receipt_gate') and '결과 대사 뒤 조건을 소모하는 이야기' or '~x';local r=choose(a);if r==1 then luamanager.SetNextScript('" + npc + "_receipt_service') end";
            DataHelper.Scripts[npc + "_receipt_service"] = DataHelper.Scripts[activity].Replace(npc + "_after", npc + "_receipt_result", StringComparison.Ordinal);
            DataHelper.Scripts[npc + "_receipt_result"] = "local s=checkpointmanager.Switch('" + npc + "_outcome');if s==1 then statmodifymanager.Player('martial-point',2,'',1);" + speaker + "D_result_loss')) elseif s==2 then statmodifymanager.Player('martial-point',4,'',1);" + speaker + "D_result_win'));statmodifymanager.SetFlag('" + npc + "_receipt_input',1) end;luamanager.ChangeScene('Free','','')";
            controller._positionData._defaultScript = npc + "_receipt_root";
            for (int at = 0; at < 80; at++) { Time.unscaledTime += .25f; value = Read(true); }
            Check(ResearchStory(value, "결과 대사 뒤 조건을 소모하는 이야기"), "A mechanical-looking activity receipt that consumes the selected topic's own guard after its NPC line remains a distinct story");
            Check(receipt.State == 0 && Counters.GameCalls == 0, "The future receipt write is classified structurally and never applied during the read-only preview");
            DataHelper.Scripts[npc + "_missing_root"] = "local a={'후속 자료 대기 이야기'};local r=choose(a);if r==1 then luamanager.SetNextScript('" + npc + "_missing_service') end";
            DataHelper.Scripts[npc + "_missing_service"] = DataHelper.Scripts[activity].Replace(npc + "_after", npc + "_missing_result", StringComparison.Ordinal);
            controller._positionData._defaultScript = npc + "_missing_root";
            for (int at = 0; at < 40; at++) { Time.unscaledTime += .25f; value = Read(true); }
            Check(ResearchStory(value, "후속 자료 대기 이야기") && HasStatus(value, "읽기 실패"), "A proven fixed response cannot hide a selected topic while its registered callback is missing");
            DataHelper.Scripts[npc + "_missing_result"] = "if checkpointmanager.Condition('" + npc + "_special_gate') then " + speaker + "D_recovered_special'));statmodifymanager.SetFlag('" + npc + "_recovered_seen',1) end";
            Time.unscaledTime += 5;
            for (int at = 0; at < 40; at++) { Time.unscaledTime += .25f; value = Read(true); }
            Check(ResearchStory(value, "후속 자료 대기 이야기") && !HasStatus(value, "읽기 실패"), "The missing callback recovers on the existing retry schedule and its separate special story remains visible");
            DataHelper.Scripts[npc + "_opaque_root"] = "local a={'불명 이야기'};local r=choose(a);if r==1 then unknown_callback();" + speaker + "D_unknown'));luamanager.ChangeScene('Combat','fixture_unknown','Story') end";
            controller._positionData._defaultScript = npc + "_opaque_root";
            for (int at = 0; at < 40; at++) { Time.unscaledTime += .25f; value = Read(true); }
            Check(ResearchStory(value, "불명 이야기"), "An arbitrary callback prevents service-only proof and retains the unclassified NPC story candidate");
        }
    }
    private static void NativeCompletedTopicsMap(string indexPath)
    {
        var paths = new Dictionary<string, (string Path, string Sha)>(StringComparer.OrdinalIgnoreCase);
        using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(indexPath)))
            foreach (JsonElement item in document.RootElement.EnumerateArray()) paths[item.GetProperty("name").GetString()] = (item.GetProperty("path").GetString(), item.GetProperty("sha256").GetString());
        MapGuideSource.Clear(); OneShotProof.Clear(); MapGuideSource.Enabled = () => true;
        // The existing interpreter is an explicit fixture. Its known empty
        // reset collection proves ResetTalkFlag cannot touch topic inputs;
        // this does not initialize or execute a Lua interpreter.
        var resetFlags = new FlagCollectionData();
        var interpreter = new MoonSharp.Interpreter.Script();
        interpreter.m_GlobalTable.Values["checkpointmanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(CheckPointManager.Instance) };
        interpreter.m_GlobalTable.Values["statmodifymanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(new StatModifyManager { _talkOptionFlag = resetFlags }) };
        observedTalkEnvironment = new Fungus.LuaEnvironment(interpreter);
        PreviewConditions.ObserveLuaEnvironment(observedTalkEnvironment);
        var manager = CheckPointManager.Instance = new CheckPointManager();
        var switches = new SwitchResultConfig(); manager._switchList = new object[] { switches };
        var flags = new Dictionary<string, FlagData>(StringComparer.Ordinal);
        var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int checkedSources = 0;
        ScriptReadResult Exact(string key)
        {
            if (cache.TryGetValue(key, out string body)) return new ScriptReadResult { State = ScriptReadState.Success, Text = body };
            if (!paths.TryGetValue(key, out var record)) return new ScriptReadResult { State = ScriptReadState.Missing };
            byte[] bytes = File.ReadAllBytes(record.Path);
            Check(Convert.ToHexString(SHA256.HashData(bytes)).Equals(record.Sha, StringComparison.OrdinalIgnoreCase), "Completed-topic fixture verifies original " + key + " SHA-256");
            checkedSources++; cache[key] = body = new UTF8Encoding(false, true).GetString(bytes);
            // These conditions are synthetic, not the user's save/configuration:
            // access and repeat sparring remain open; every individual topic and
            // the first introduction are completed. All native pools are empty.
            // Only the intro Switch input deliberately shares its literal
            // SetFlag key/current value (1), testing an idempotent write. Other
            // dependencies are explicit disjoint synthetic objects; checkpoint
            // keys are not assumed to be native FlagData names.
            foreach (Match match in Regex.Matches(body, "checkpointmanager\\.Condition\\(\"([^\"]+)\"\\)"))
            {
                string name = match.Groups[1].Value;
                if (flags.ContainsKey(name)) continue;
                var flag = new FlagData { name = "synthetic-topic-" + name, State = name == "Talk_Brother4" || name == "Talk_Brother4_02_04" ? 0 : 1 };
                flags.Add(name, flag); manager._condition._list.Add(new ConditionResultData { name = name, _items = new List<ConditionResultItem> { IsZero(flag) } });
            }
            foreach (Match match in Regex.Matches(body, "checkpointmanager\\.Switch\\(\"([^\"]+)\"\\)"))
            {
                string name = match.Groups[1].Value;
                if (switches._list.Exists(item => item.name == name)) continue;
                var flag = new FlagData { name = name == "Talk_Brother4_00" ? name : "synthetic-switch-" + name, State = name == "Talk_Brother4_00" ? 1 : 0 };
                switches._list.Add(new SwitchResultData { name = name, _items = new List<ConditionResultItem> { IsZero(flag) } });
            }
            foreach (Match match in Regex.Matches(body, "checkpointmanager\\.Position\\(\"([^\"]+)\"\\)"))
            { string name = match.Groups[1].Value; if (!manager._position._list.Exists(item => item.name == name)) manager._position._list.Add(new PositionResultData { name = name }); }
            return new ScriptReadResult { State = ScriptReadState.Success, Text = body };
        }
        DataHelper.ExternalReader = Exact;
        var controller = new PositionController { _positionData = new FreePositionData { name = "OriginalRoomTopics", _defaultScript = "room_001" } };
        MapGuideSource.Select(controller); MapGuideSnapshot value = null;
        for (int at = 0; at < 180; at++) { Time.unscaledTime += .25f; value = Read(true); }
        const string raw = "talk+talk+brother4";
        Check(checkedSources > 2 && flags["Talk_Brother4"].State == 0 && flags["Talk_Brother4_02_04"].State == 0,
            "The original room/talk/sparring scripts are analyzed with explicit active access and repeat challenge fixtures");
        Check(!value.Summary.AutoText.Contains(raw, StringComparison.Ordinal), "SHA-original brother4 fixed sparring dialogue does not keep a map notice after every individual story topic is completed; actual=" + value.Summary.AutoText + "; rows=" + string.Join(" || ", value.Entries.ConvertAll(item => item.Title + ":" + item.Status + ":" + item.Detail)));
        int resources = DataHelper.Reads, parses = LuaParser.Parses, descriptions = LuaParser.Descriptions;
        for (int at = 0; at < 80; at++) { Time.unscaledTime += .25f; value = Read(true); Check(!value.Summary.AutoText.Contains(raw, StringComparison.Ordinal), "Completed original brother4 topics remain absent with repeat sparring still active"); }
        Check(DataHelper.Reads == resources && LuaParser.Parses == parses && LuaParser.Descriptions == descriptions, "Original-topic warm reads retain definition caches and never fully reparse Lua");
        var introConfig = switches._list.Find(item => item.name == "Talk_Brother4_00");
        var introFlag = (FlagData)introConfig._items[0]._items[0]._value1._value[0]._flag;
        resetFlags._list.Add(introFlag); value = Read(true);
        Check(ResearchStory(value, raw), "An apparently same-value SetFlag cannot prove an activity repeatable when the preceding ResetTalkFlag owns that input and may change it before the assignment");
        Check(introFlag.State == 1 && Counters.GameCalls == 0, "The no-op/reset-footprint interaction is read-only and never performs either the reset or the later SetFlag");
        resetFlags._list.Clear(); value = Read(true);
        Check(!value.Summary.AutoText.Contains(raw, StringComparison.Ordinal), "Removing the reset-owned input restores the exact current-value no-op proof without rebuilding Lua definitions");
        resetFlags._list.Add(flags["Talk_Brother4_00_01"]); value = Read(true);
        Check(ResearchStory(value, raw), "A topic flag actually owned by ResetTalkFlag remains uncertain instead of being incorrectly hidden from its current false value");
        resetFlags._list.Clear(); value = Read(true);
        Check(!value.Summary.AutoText.Contains(raw, StringComparison.Ordinal), "A newly observed disjoint reset footprint safely restores completed-topic suppression without reloading definitions");
        flags["Talk_Brother4_00_01"].State = 0; value = Read(true);
        Check(ResearchStory(value, raw), "A remaining ordinary brother4 topic remains visible even though the repeat sparring response is excluded");
        Check(Counters.GameCalls == 0 && flags["Talk_Brother4_02_04"].State == 0, "Original-topic preview calls no native game conditions/RNG, combat dispatch or effects");
        DataHelper.ExternalReader = null;
    }
    private static void Limits()
    {
        MapGuideSource.Clear(); MapGuideSource.Enabled = () => true;
        var manager = CheckPointManager.Instance = new CheckPointManager();
        var pool = new PositionResultData { name = "large_pool", _scriptPrefix = "large_result" };
        for (int i = 0; i < 5000; i++) pool._list.Add(new PositionEventData { name = "large-" + i, ResultType = "entry" + i, _toggleCondition = false });
        manager._position._list.Add(pool);
        DataHelper.Scripts["large_root"] = "local r=checkpointmanager.Position('large_pool'); luamanager.SetNextScript(r)";
        var controller = new PositionController { _positionData = new FreePositionData { name = "Large", _defaultScript = "large_root" } };
        MapGuideSource.Select(controller); var value = Read();
        int count = 0, nativeResultReads = 0;
        foreach (var row in value.Entries) if (row.Title.StartsWith("large-", StringComparison.Ordinal)) count++;
        foreach (var entry in pool._list) nativeResultReads += entry.ResultReads;
        Check(count == 256 && nativeResultReads == 256 && pool._list[256].ResultReads == 0 && pool._list[4999].ResultReads == 0, "Native pool boundary materializes only 256 entries before the engine's overflow sentinel");
        Check(HasStatus(value, "경로 일부 확인 불가") || HasStatus(value, "경로 읽기 실패") || HasStatus(value, "경로 준비 중"), "Oversized native pool retains explicit incomplete/preparing status instead of reporting a complete zero");
        bool limitDetail = false; foreach (var row in value.Entries) if (row.Detail.Contains("풀 항목 상한", StringComparison.Ordinal)) limitDetail = true;
        Check(limitDetail, "The 257th sentinel activates the production route item-limit diagnostic");
        manager._position._list.Add(new PositionResultData { name = "large_pool", _scriptPrefix = "wrong_second_match" });
        var choicePool = MapGuideSource.ReadChoicePool("large_pool");
        Check(choicePool.Count == 257 && choicePool[256] == null && ReferenceEquals(choicePool[0].Target, pool._list[0]) && choicePool[0].ResultScript == "large_result_entry0", "Choice pool shares native first-exact matching and 256-entry plus overflow-sentinel bound");
        manager._position._list.Clear();
        for (int i = 0; i < 1024; i++) manager._position._list.Add(new PositionResultData { name = "unrelated-" + i });
        var beyond = new PositionEventData { name = "beyond-limit", ResultType = "beyond", _toggleCondition = false };
        manager._position._list.Add(new PositionResultData { name = "beyond_pool", _list = new List<PositionEventData> { beyond } });
        DataHelper.Scripts["beyond_root"] = "local r=checkpointmanager.Position('beyond_pool'); luamanager.SetNextScript(r)";
        controller._positionData._defaultScript = "beyond_root"; value = Read();
        Check(Find(value, "beyond-limit") == null && beyond.ResultReads == 0 && HasStatus(value, "풀 검색 상한"), "Native config lookup stops before a 1025th pool and reports the exact lookup boundary");
        var diagnostic = Find(value, "풀 자료");
        Check(diagnostic != null && diagnostic.Detail.Contains("1,024", StringComparison.Ordinal), "Pool search budget is distinct from missing data and successful emptiness");
        var activity = Find(value, "beyond_root");
        Check(activity != null && !activity.Available && activity.Status.Contains("내부 경로 확인 필요", StringComparison.Ordinal), "Incomplete or failed route discovery preserves the default activity as unknown rather than a healthy possible fixed story");
        bool rejected = false; try { MapGuideSource.ReadChoicePool("beyond_pool"); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected && beyond.ResultReads == 0, "Choice pool adapter also rejects lookup beyond 1024 definitions without reading the hidden entry");
        Check(Counters.GameCalls == 0, "Bounded pool materialization never evaluates game entry or event methods");
    }
    private static void PassiveRouteReads()
    {
        foreach (bool once in new[] { false, true })
        {
            MapGuideSource.Clear(); OneShotProof.Clear(); MapGuideSource.Enabled = () => true;
            var manager = CheckPointManager.Instance = new CheckPointManager();
            var flag = new FlagData { name = "ordinary-route", State = 0 };
            manager._condition._list.Add(new ConditionResultData { name = "ordinary_gate", _items = new List<ConditionResultItem> { IsZero(flag) } });
            manager._position._list.Add(new PositionResultData { name = "OrdinaryPool", _scriptPrefix = "ordinary_result", _list = new List<PositionEventData> { new PositionEventData { name = "ordinary", ResultType = "ordinary", _toggleCondition = false } } });
            DataHelper.Scripts["ordinary_root"] = "if checkpointmanager.Condition('ordinary_gate') then local p=checkpointmanager.Position('OrdinaryPool');luamanager.SetNextScript(p) end";
            DataHelper.Scripts["ordinary_result_ordinary"] = "";
            var waiting = new TrackingMissions();
            var controller = new PositionController { _positionData = new FreePositionData { name = "Ordinary", _defaultScript = "ordinary_root" } };
            controller._missionsManager.ActiveSubMissions = waiting;
            MapGuideSource.Select(controller); int resources = DataHelper.Reads;
            DataHelper.ExternalReader = key => {
                Check(waiting.Enumerations > 0, "live waiting membership is read before optional Lua preparation");
                return DataHelper.Scripts.TryGetValue(key, out string body) ? new ScriptReadResult { State = ScriptReadState.Success, Text = body } : new ScriptReadResult { State = ScriptReadState.Missing };
            };
            try
            {
                MapGuideSnapshot value = null;
                for (int at = 0; at < 40; at++) { Time.unscaledTime += .25f; value = (MapGuideSnapshot)MapGuideSource.Discover(true, once, false)[0].Read(); }
                Check(DataHelper.Reads > resources, "a selected-place guide may prepare one bounded shared route graph with one-shot either ON or OFF");
                int warmResources = DataHelper.Reads, warmFlags = flag.StateReads;
                for (int at = 0; at < 100; at++) { Time.unscaledTime += .25f; value = (MapGuideSnapshot)MapGuideSource.Discover(true, once, false)[0].Read(); }
                Check(DataHelper.Reads == warmResources && flag.StateReads == warmFlags && flag.StateReads == 0, "ordinary route has zero warm Lua loads and zero hidden guard reads with either setting");
                Check(value.Summary.AutoText == "" && Find(value, "ordinary") == null, "ordinary automatic rows and allocations are omitted");
                value = (MapGuideSnapshot)MapGuideSource.Discover(true, true, true)[0].Read();
                for (int at = 0; at < 40; at++) value = (MapGuideSnapshot)MapGuideSource.Discover(true, true, true)[0].Read();
                Check(Find(value, "ordinary") != null && flag.StateReads > 0, "explicit detail still samples ordinary route guards");
                Check(value.Entries.Exists(row => row.Title == "지도 조회 성능" && row.Detail.Contains("p95", StringComparison.Ordinal)), "performance statistics are available in explicit detail");
            }
            finally { DataHelper.ExternalReader = null; }
        }
    }
    private static void PassiveCosts()
    {
        MapGuideSource.Clear(); OneShotProof.Clear(); MapGuideSource.Enabled = () => true;
        var manager = CheckPointManager.Instance = new CheckPointManager();
        var flag = new FlagData { name = "passive-flag", State = 0 };
        var pool = new PositionResultData { name = "PassivePool", _scriptPrefix = "passive_result" };
        for (int i = 0; i < 256; i++) pool._list.Add(new PositionEventData { name = "passive-" + i, ResultType = "shared", _activeCondition = IsZero(flag) });
        manager._position._list.Add(pool);
        DataHelper.Scripts["passive_root"] = "local r=checkpointmanager.Position('PassivePool');luamanager.SetNextScript(r)";
        DataHelper.Scripts["passive_result_shared"] = new string(' ', 25000) + "unknown_callback()";
        var controller = new PositionController { _positionData = new FreePositionData { name = "Passive", _defaultScript = "passive_root" } };
        MapGuideSource.Select(controller);
        int parses = LuaParser.Parses, descriptions = LuaParser.Descriptions, eventText = PopupConditionText.EventDescriptions, conditionText = PopupConditionText.ConditionDescriptions;
        MapGuideSnapshot value = null;
        for (int i = 0; i < 20; i++)
        {
            int before = DataHelper.Reads;
            Time.unscaledTime += .25f; value = Read(false);
            Check(DataHelper.Reads - before <= 1, "Passive preparation admits at most one resource load per scheduled tick");
        }
        int count = 0; foreach (var row in value.Entries) if (row.Title.StartsWith("passive-", StringComparison.Ordinal)) count++;
        Check(count == 0, "Passive refresh builds no ordinary rows; explicit detail owns their allocation");
        Check(LuaParser.Parses == parses && LuaParser.Descriptions == descriptions && PopupConditionText.EventDescriptions == eventText && PopupConditionText.ConditionDescriptions == conditionText,
            "Passive large-result display never enters the synchronous full analyzer, effect formatter or popup condition formatter");
        Check(flag.StateReads == 0, "Ordinary/unproved passive rows never recursively reread 256 event conditions");
        int resources = DataHelper.Reads;
        for (int i = 0; i < 40; i++) { Time.unscaledTime += .25f; value = Read(false); }
        Check(DataHelper.Reads == resources && LuaParser.Parses == parses && flag.StateReads == 0, "Warm passive event refresh has no resource, parser or hidden-condition work");
        PositionEventCache.Parser.Actions["passive_result_shared"] = new List<LuaAction> { new LuaAction { Analysis = new PreviewAnalysis { Effects = new List<PreviewEffect> {
            new PreviewEffect { Name = "SetFlag", Constants = new List<object> { "passive-flag", 1 } } } } } };
        value = Read(true);
        Check(pool._list.TrueForAll(data => Find(value, data.name) != null), "Explicit detail retains every ordinary event row");
        Check(PopupConditionText.EventDescriptions > eventText && LuaParser.Descriptions > descriptions && flag.StateReads > 0,
            "Opening explicit detail still reads full conditions/effects and builds the unchanged strict proof");
        eventText = PopupConditionText.EventDescriptions; descriptions = LuaParser.Descriptions;
        int priorReads = flag.StateReads;
        value = Read(false);
        Check(flag.StateReads - priorReads > 0 && flag.StateReads - priorReads <= 32, "Cached positive proofs reevaluate at most sixteen representative event snapshots per passive tick");
        Check(PopupConditionText.EventDescriptions == eventText && LuaParser.Descriptions == descriptions && value.Summary.AutoText.Contains("passive-0", StringComparison.Ordinal),
            "Positive passive opportunity names remain visible without detailed formatters");
        foreach (var data in pool._list)
        {
            data._eventRateItem = new RateItem[200];
            for (int i = 0; i < 200; i++) data._eventRateItem[i] = new RateItem { Condition = data._activeCondition, Rate = 1 };
        }
        priorReads = flag.StateReads; value = Read(false);
        Check(flag.StateReads == priorReads && (Find(value, "passive-0") == null || !Find(value, "passive-0").Available), "Passive rate graph preflight rejects wide recursive work before any current flag getter runs");
        var shared = new FlagData { name = "shared-mission", State = 0 }; var condition = IsZero(shared);
        var overflow = new FlagData { name = "overflow-mission", State = 0 };
        for (int i = 0; i < 17; i++)
        {
            var stage = new MissionCheckData { name = "perf-stage-" + i, _startScript = "expensive_mission_result", _conditions = new[] { i == 16 ? IsZero(overflow) : condition } };
            controller._missionsManager._subMissions._list.Add(new MissionData { name = "perf-mission-" + i, _name = "perf-logical-" + i, _currentKey = "start", _missions = new[] { stage } });
        }
        for (int i = 0; i < 3; i++) Read(false);
        int sharedBefore = shared.StateReads, overflowBefore = overflow.StateReads;
        parses = LuaParser.Parses; descriptions = LuaParser.Descriptions; conditionText = PopupConditionText.ConditionDescriptions;
        value = Read(false);
        Check(shared.StateReads == sharedBefore && overflow.StateReads == overflowBefore && !HasStatus(value, "임무 정의 준비 중"), "Unregistered mission conditions are never sampled by passive map refresh");
        Check(LuaParser.Parses == parses && LuaParser.Descriptions == descriptions && PopupConditionText.ConditionDescriptions == conditionText,
            "Mission passive status never analyzes start scripts or formats full conditions");
        Check(Counters.GameCalls == 0 && flag.State == 0 && shared.State == 0, "Passive optimization does not execute conditions/RNG or mutate flags");
    }
}

// Exact type/field boundaries for observing an already existing interpreter.
// The production reader only uses RawGet; these fixtures provide no Lua run,
// ResetTalkFlag or environment-initialization implementation.
namespace Mortal.Core { public sealed class FlagCollectionData : UnityEngine.Object { public List<FlagData> _list = new List<FlagData>(); } }
namespace Mortal.Story { public sealed class StatModifyManager : UnityEngine.Object { public Mortal.Core.FlagCollectionData _talkOptionFlag; } }
namespace MoonSharp.Interpreter
{
    public sealed class Script { public Table m_GlobalTable = new Table(); }
    public sealed class Table
    {
        public readonly Dictionary<string, DynValue> Values = new Dictionary<string, DynValue>();
        public DynValue RawGet(string key) { DynValue value; return Values.TryGetValue(key, out value) ? value : null; }
    }
    public sealed class DynValue { public object m_Object; }
    public sealed class UserData { public object Object { get; private set; } public UserData(object value) { Object = value; } }
}
namespace Fungus
{
    public class LuaEnvironment
    {
        protected MoonSharp.Interpreter.Script interpreter;
        public LuaEnvironment(MoonSharp.Interpreter.Script value) { interpreter = value; }
    }
}
