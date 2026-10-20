using System;
using System.Collections.Generic;
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
internal static class Program
{
    private static int checks;
    private static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    private static StatGroupVariable Constant(int value)
    { return new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = value } } }; }
    private static ConditionResultItem IsZero(FlagData flag)
    { return new ConditionResultItem { _items = new List<StatCompareItem> { new StatCompareItem {
        _value1 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 3, _flag = flag } } }, _value2 = Constant(0) } } }; }
    private static InformationRow Find(MapGuideSnapshot value, string title)
    { foreach (var row in value.Entries) if (row.Title == title) return row; return null; }
    private static bool HasStatus(MapGuideSnapshot value, string text)
    { foreach (var row in value.Entries) if (row.Status.Contains(text, StringComparison.Ordinal)) return true; return false; }
    private static MapGuideSnapshot Read(bool details = true)
    {
        var queries = MapGuideSource.Discover(true, true, details);
        Check(queries.Count == 1, "One provider query owns summary and every detail entry");
        return (MapGuideSnapshot)queries[0].Read();
    }
    private static void Main()
    {
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
        Limits();
        PassiveCosts();
        ChoiceStoryMap();
        MeaningfulDialogueMap();
        FullStoryGateMap();
        Console.WriteLine("PASS: MapGuideRouteIntegrationTests " + checks + " production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed.");
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
        for (int at = 0; at < 30; at++) { Time.unscaledTime += .25f; snapshot = Read(false); }
        var story = Find(snapshot, "사사형");
        Check(story != null && story.Kind.Contains("인물 대화"), "Literal and inline selected dialogue becomes a map story without fabricating an event pool");
        Check(story.Status == "진행 가능성 있음" && !story.Available, "An opaque predecessor keeps the whole dialogue condition uncertain instead of borrowing the first option's true guard");
        Check(snapshot.Summary.AutoText.Contains("사사형") && snapshot.Summary.AutoText.Contains("진행 가능성 있음"), "Compact map retains the known conversation as possible without claiming a currently met full path");
        Check(!snapshot.Summary.AutoText.Contains("story_line") && !snapshot.Summary.AutoText.Contains("talk-later"), "Automatic story information excludes dialogue text and condition internals");
        Check(!story.Status.Contains("1회성"), "Choice route discovery never grants a one-shot property");
        Check(LuaParser.Descriptions == formatter && LuaParser.Parses == parses, "Passive choice story map performs zero full effect descriptions or analyzers");
        snapshot = Read(true); story = Find(snapshot, "사사형");
        Check(story.Detail.Contains("사사형 → 대화 항목") && story.Detail.Contains("내부 분기 조건"), "Map detail provides the required choice route and explains its whole-path condition scope");
        int resourceReads = DataHelper.Reads;
        for (int at = 0; at < 40; at++) { Time.unscaledTime += .25f; snapshot = Read(false); }
        Check(DataHelper.Reads == resourceReads && LuaParser.Parses == parses, "Warm story refresh reuses route definitions without resource reads or full Lua analysis");
        firstFlag.State = 1; snapshot = Read(false); story = Find(snapshot, "사사형");
        Check(story.Status == "진행 가능성 있음" && !story.Available, "A false observed entry condition on an incomplete route does not deny potentially omitted alternative paths");
        Check(laterFlag.State == 1 && firstFlag.State == 1 && Counters.GameCalls == 0, "Neither discovery nor path preview applies the intervening flag write or calls game judgments");
        firstFlag.State = 0; controller._gameStatManager._stats._list[0]._value = 0; snapshot = Read(false);
        Check(!Find(snapshot, "사사형").Available && Find(snapshot, "사사형").Status.Contains("행동 부족"), "Current first guard does not override native map action availability");
        controller._gameStatManager._stats._list[0]._value = 3;
        controller._positionData._defaultScript = "choice_labels";
        DataHelper.Scripts["choice_labels"] = "local opts={}; if checkpointmanager.Condition('talk_entry') then opts[1]='선택 A' else opts[1]='선택 B' end; local r=choose(opts); setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('line'))";
        firstFlag.State = 1;
        for (int at = 0; at < 8; at++) { Time.unscaledTime += .25f; snapshot = Read(false); }
        Check(Find(snapshot, "선택 A") != null && !Find(snapshot, "선택 A").Available && Find(snapshot, "선택 B").Available, "Same original choose index with alternative raw labels stays in separate groups and cannot mark the wrong title ready");
        Check(Find(snapshot, "선택 A").Status == "지금 진행 불가", "A complete supported false full story path is blocked even when its display-only speaker call was structurally partial");
        snapshot = Read(true);
        Check(!snapshot.Summary.AutoText.Contains("선택 A", StringComparison.Ordinal) && snapshot.Summary.AutoText.Contains("선택 B", StringComparison.Ordinal) && Find(snapshot, "선택 A").Detail.Contains("선택 경로", StringComparison.Ordinal),
            "The actual choice-route map hides its false dialogue notice while retaining the positive alternative and the false route's detail");
    }
    private static void MeaningfulDialogueMap()
    {
        MapGuideSource.Clear(); MapGuideSource.Enabled = () => true;
        CheckPointManager.Instance = new CheckPointManager();
        DataHelper.Scripts["activity_filter"] = "local a={'휴식','훈련','사사형'};local r=choose(a);if r==1 then setcharacter(narrative);say(luamanager.GetStoryText('N_rest'));setcharacter(characters.Get('player'));say(luamanager.GetStoryText('D_rest')) elseif r==2 then say(luamanager.GetStoryText('N_train')) elseif r==3 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_brother')) end";
        var controller = new PositionController { _positionData = new FreePositionData { name = "Room", _defaultScript = "activity_filter" } };
        MapGuideSource.Select(controller); MapGuideSnapshot value = null;
        for (int at = 0; at < 15; at++) { Time.unscaledTime += .25f; value = Read(false); }
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
        for (int at = 0; at < 20; at++) { Time.unscaledTime += .25f; value = Read(false); }
        Check(DataHelper.Reads == reads && LuaParser.Parses == parses && LuaParser.Descriptions == descriptions && Counters.GameCalls == 0,
            "Classification reuses static route definitions without warm Lua reload, full effects or game judgment calls");

        DataHelper.Scripts["player_talk_only"] = "local a={'talk+O_thought+0+0'};local r=choose(a);if r==1 then setcharacter(characters.Get('player'));say(luamanager.GetStoryText('D_player_talk')) end";
        controller._positionData._defaultScript = "player_talk_only";
        for (int at = 0; at < 12; at++) { Time.unscaledTime += .25f; value = Read(false); }
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
        for (int at = 0; at < 16; at++) { Time.unscaledTime += .25f; value = Read(false); }
        var row = Find(value, "수련");
        Check(row.Status == "지금 진행 불가" && !row.Available && !value.Summary.Available && !value.Summary.AutoText.Contains("조건 충족", StringComparison.Ordinal),
            "An unguarded selected training action stays blocked when its full NPC-conversation condition is false");
        Check(!value.Summary.AutoText.Contains("수련", StringComparison.Ordinal) && !value.Summary.AutoText.Contains("숨김_지도임무", StringComparison.Ordinal) && value.Entries.Exists(item => item.Title.StartsWith("숨김_지도임무", StringComparison.Ordinal)) &&
            Array.FindAll(value.Summary.AutoText.Split('\n'), line => line.StartsWith("• ", StringComparison.Ordinal)).Length == 0 &&
            !value.Summary.AutoText.Contains("지금 진행 불가", StringComparison.Ordinal) && !value.Summary.AutoText.Contains("외 ", StringComparison.Ordinal),
            "The real parser/provider retains blocked dialogue and mission detail while an all-blocked place publishes no automatic item rows");
        int resources = DataHelper.Reads, parses = LuaParser.Parses, descriptions = LuaParser.Descriptions;
        gate.State = 0; value = Read(false); row = Find(value, "수련");
        Check(row.Status == "진행 가능" && row.Available && value.Summary.Available, "A full safely observed NPC condition, rather than only the first option, controls positive map color");
        Check(value.Summary.AutoText.Contains("수련", StringComparison.Ordinal), "A full positive story is still included after applying the blocked-notice filter");
        Check(DataHelper.Reads == resources && LuaParser.Parses == parses && LuaParser.Descriptions == descriptions,
            "Changing the live full-story condition reads the cached route without loading or fully analyzing Lua");
        controller._gameStatManager._stats._list[0]._value = 0; value = Read(false); row = Find(value, "수련");
        Check(row.Status.StartsWith("지금 진행 불가", StringComparison.Ordinal) && row.Status.Contains("행동 부족", StringComparison.Ordinal) && !row.Available,
            "A positive full-story gate still uses a blocked status after the native action-cost gate fails");
        Check(!value.Summary.AutoText.Contains("수련", StringComparison.Ordinal), "A native action-cost blocker removes even a currently positive dialogue's automatic notice");
        controller._gameStatManager._stats._list[0]._value = 3; controller._active = false; value = Read(false); row = Find(value, "수련");
        Check(row.Status.StartsWith("지금 진행 불가", StringComparison.Ordinal) && !row.Available, "Disabled places never retain a positive full-story status prefix");
        controller._active = true; controller._missionsManager.MainMissionStart = true; controller._missionsManager.MainMissionPosition = controller._position;
        controller._missionsManager.MainCheckData = new MissionCheckData { name = "priority-main", _position = controller._position };
        value = Read(false); row = Find(value, "수련");
        Check(row.Status.StartsWith("지금 진행 불가", StringComparison.Ordinal) && row.Status.Contains("주 임무 우선", StringComparison.Ordinal) && !row.Available,
            "A staged same-place main mission replaces the positive story prefix with the actual dispatch blocker");
        controller._missionsManager.MainMissionStart = false; controller._missionsManager.MainCheckData = null;

        gate.State = 1;
        DataHelper.Scripts["training_own_write"] = "local a={'수련'};local r=choose(a);if r==1 then statmodifymanager.SetFlag('training-npc-gate',0);if checkpointmanager.Condition('training_npc') then " + npc + " end end";
        controller._positionData._defaultScript = "training_own_write";
        for (int at = 0; at < 16; at++) { Time.unscaledTime += .25f; value = Read(false); }
        Check(Find(value, "수련").Status == "진행 가능성 있음" && !Find(value, "수련").Available && gate.State == 1,
            "A write before the same condition is a possible future story, never a current false or green status");
        Check(value.Summary.AutoText.Contains("수련", StringComparison.Ordinal), "A conversation with genuinely unknown future conditions remains in the automatic map notices");

        DataHelper.Scripts["training_foreign_write"] = "local a={'수련'};local r=choose(a);if r==1 then statmodifymanager.SetFlag('unrelated-flag',0);if checkpointmanager.Condition('training_npc') then " + npc + " end end";
        controller._positionData._defaultScript = "training_foreign_write";
        for (int at = 0; at < 16; at++) { Time.unscaledTime += .25f; value = Read(false); }
        Check(Find(value, "수련").Status == "지금 진행 불가" && !Find(value, "수련").Available,
            "An exactly known unrelated flag write does not hide a safely false NPC predicate");
        value = Read(true);
        Check(!value.Summary.AutoText.Contains("수련", StringComparison.Ordinal) && Find(value, "수련").Detail.Contains("선택 경로", StringComparison.Ordinal),
            "A proven false dialogue is filtered from automatic notices without losing the real route detail");
        Check(Counters.GameCalls == 0 && gate.State == 1, "Integrated full-story classification never executes native judgments, writes, RNG or item operations");
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
        Check(count == 256, "Passive work limits preserve every original event row for explicit detail");
        Check(LuaParser.Parses == parses && LuaParser.Descriptions == descriptions && PopupConditionText.EventDescriptions == eventText && PopupConditionText.ConditionDescriptions == conditionText,
            "Passive large-result display never enters the synchronous full analyzer, effect formatter or popup condition formatter");
        Check(flag.StateReads == 0, "Ordinary/unproved passive rows never recursively reread 256 event conditions");
        int resources = DataHelper.Reads;
        for (int i = 0; i < 40; i++) { Time.unscaledTime += .25f; value = Read(false); }
        Check(DataHelper.Reads == resources && LuaParser.Parses == parses && flag.StateReads == 0, "Warm passive event refresh has no resource, parser or hidden-condition work");
        PositionEventCache.Parser.Actions["passive_result_shared"] = new List<LuaAction> { new LuaAction { Analysis = new PreviewAnalysis { Effects = new List<PreviewEffect> {
            new PreviewEffect { Name = "SetFlag", Constants = new List<object> { "passive-flag", 1 } } } } } };
        value = Read(true);
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
        Check(flag.StateReads == priorReads && !Find(value, "passive-0").Available, "Passive rate graph preflight rejects wide recursive work before any current flag getter runs");
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
        Check(shared.StateReads - sharedBefore == 1 && overflow.StateReads == overflowBefore, "Shared mission condition is read once and seventeenth candidate is deferred within the same passive snapshot");
        Check(LuaParser.Parses == parses && LuaParser.Descriptions == descriptions && PopupConditionText.ConditionDescriptions == conditionText,
            "Mission passive status never analyzes start scripts or formats full conditions");
        Check(Counters.GameCalls == 0 && flag.State == 0 && shared.State == 0, "Passive optimization does not execute conditions/RNG or mutate flags");
    }
}
