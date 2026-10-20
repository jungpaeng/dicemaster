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
        Console.WriteLine("PASS: MapGuideRouteIntegrationTests " + checks + " production provider/route/syntax/current-reader/one-shot assertions; shared explicit Unity/game/resource/effect-formatter fixtures; Lua/Windows/native execution NOT performed.");
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
