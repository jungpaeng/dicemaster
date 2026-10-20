using System;
using System.Collections.Generic;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using DiceMaster.LuaParse;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;
using UnityEngine;
using LuaAction = DiceMaster.LuaParse.Action;
static class Program
{
    private static int assertions;
    private static void Check(bool condition, string message) { assertions++; if (!condition) throw new Exception(message); }
    private static StatGroupVariable Constant(int value) => new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = value } } };
    private static StatGroupVariable Flag(FlagData flag) => new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 3, _flag = flag } } };
    private static ConditionResultItem Compare(StatGroupVariable left, StatGroupVariable right, int op = 0) => new ConditionResultItem { _items = new List<StatCompareItem> { new StatCompareItem { _value1 = left, _value2 = right, _compareOp = op } } };
    private static List<LuaAction> Result(params PreviewEffect[] effects) => new List<LuaAction> { new LuaAction { Analysis = new PreviewAnalysis { Effects = new List<PreviewEffect>(effects) } } };
    private static PreviewEffect Set(string id, int value) => new PreviewEffect { Name = "SetFlag", Constants = new List<object> { id, value } };
    private static PositionEventData Event(string name, ConditionResultItem condition) => new PositionEventData { name = name, _activeCondition = condition, ResultType = name };
    private static void Proofs()
    {
        var flag = new FlagData { name = "F1", State = 0 };
        var condition = Compare(Flag(flag), Constant(0));
        Check(OneShotProof.Evaluate(condition, Result(Set("F1", 1))).Proven, "A definite SetFlag breaks the exact active flag condition");
        Check(flag.State == 0 && Counters.GameCalls == 0, "Proof never applies effects or evaluates game conditions");
        Check(!OneShotProof.Evaluate(condition, Result(Set("other", 1))).Proven, "An unrelated flag cannot prove once");
        Check(!OneShotProof.Evaluate(Compare(Constant(0), Constant(1)), Result(Set("F1", 1))).Proven, "A condition already statically false is not a change proof");
        var conjunction = Compare(Flag(flag), Constant(1));
        conjunction._items.Add(new StatCompareItem { _value1 = Constant(0), _value2 = Constant(1) });
        Check(!OneShotProof.Evaluate(conjunction, Result(Set("F1", 1))).Proven, "Changed true atom plus unrelated false atom cannot prove once");
        var action = Set("F1", 1); action.Conditional = true;
        Check(!OneShotProof.Evaluate(condition, Result(action)).Proven, "Conditional SetFlag cannot prove execution");
        action.Conditional = false; action.Followup = true;
        Check(!OneShotProof.Evaluate(condition, Result(action)).Proven, "Followup not guaranteed");
        action.Followup = false;
        Check(!OneShotProof.Evaluate(condition, Result(action, Set("F1", 0))).Proven, "Later restore cancels proof");
        var add = new PreviewEffect { Name = "AddFlag", Constants = new List<object> { "F1", 1 } };
        Check(!OneShotProof.Evaluate(condition, Result(add)).Proven, "State-dependent AddFlag cannot create constant proof");
        Check(OneShotProof.Evaluate(condition, Result(Set("F1", 0), add)).Proven, "Constant set then add is safe arithmetic");
        var nested = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 8, _negative = true, _statGroup = Flag(flag) } } };
        Check(!OneShotProof.Evaluate(Compare(nested, Constant(1)), Result(Set("F1", 1))).Proven, "Audited nested-group reference ignores its negative flag");
        var cycle = new StatGroupVariable(); cycle._value.Add(new StatValueReference { _checkType = 8, _statGroup = cycle });
        Check(!OneShotProof.Evaluate(Compare(cycle, Constant(0)), Result(Set("F1", 1))).Proven, "Cyclic condition cannot prove once");
        var or = Compare(Flag(flag), Constant(0)); or._logicOp = 1;
        or._items.Add(new StatCompareItem { _value1 = Constant(1), _value2 = Constant(1) });
        Check(!OneShotProof.Evaluate(or, Result(Set("F1", 1))).Proven, "OR with another true branch remains active");
        var random = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 7 } } };
        var unknownOr = Compare(Flag(flag), Constant(0)); unknownOr._logicOp = 1;
        unknownOr._items.Add(new StatCompareItem { _value1 = random, _value2 = Constant(1) });
        Check(!OneShotProof.Evaluate(unknownOr, Result(Set("F1", 1))).Proven, "Unknown OR alternative never becomes repeat or proven once");
        var and = Compare(Flag(flag), Constant(0)); and._items.Add(unknownOr._items[1]);
        Check(OneShotProof.Evaluate(and, Result(Set("F1", 1))).Proven, "Known changed false gate dominates AND without evaluating RNG");
        Check(!OneShotProof.ForOption("option", Result(Set("F1", 1))).Proven && OneShotProof.CachedOptionTag("option").Contains("확인 불가"), "No activation evidence remains visibly unknown");
        Check(OneShotProof.ForOption("option", condition, Result(Set("F1", 1))).Proven && OneShotProof.CachedOptionTag("option") == " · 1회성", "Option with explicit exact gate uses same proof");
        OneShotProof.Clear(); Check(OneShotProof.CachedOptionTag("option") == "", "OFF/context clear removes cached hints");
    }
    private static MapGuideSnapshot Snapshot(bool guide = true, bool once = true)
    { var queries = MapGuideSource.Discover(guide, once); Check(queries.Count == 1, "One observed session has one scheduled query"); return (MapGuideSnapshot)queries[0].Read(); }
    private static string Details(MapGuideSnapshot value)
    { var text = new System.Text.StringBuilder(); foreach (var row in value.Entries) text.AppendLine(row.Title + " · " + row.Kind + " · " + row.Status + "\n" + row.Detail); return text.ToString(); }
    private static InformationRow Entry(MapGuideSnapshot value, string name)
    { foreach (var row in value.Entries) if (row.Title.Contains(name, StringComparison.Ordinal)) return row; throw new Exception("Missing entry " + name + "\n" + Details(value)); }
    private static PositionController EmptyPlace()
    {
        MapGuideSource.Clear(); MapGuideSource.Enabled = () => true; Time.unscaledTime = 0;
        MapRouteDefinitions.State = MapRouteState.Met; MapRouteDefinitions.Choice = false;
        CheckPointManager.Instance = new CheckPointManager();
        var value = new PositionController { _positionData = new FreePositionData { name = "Hall", _defaultScript = "" } };
        MapGuideSource.Register(value); return value;
    }
    private static MissionData Mission(string id, MissionCheckData stage)
    { return new MissionData { name = "asset-" + id, _name = id, _currentKey = "start", _missions = new[] { stage } }; }
    private static void Map()
    {
        var controller = EmptyPlace();
        Check(!MapGuideSource.Visible && MapGuideSource.SessionVisible, "Observed session remains available without a place selection");
        var noSelection = Snapshot();
        Check(!noSelection.HasSelection && noSelection.Summary == null && Details(noSelection).Contains("장소 무관 임무 없음"), "No-selection query has global detail and no overlay");
        MapGuideSource.Select(controller);
        var met = Compare(Constant(1), Constant(1)); var zero = Compare(Constant(0), Constant(1));
        var unknown = Compare(new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 7 } } }, Constant(1));
        var denied = Event("denied", zero); var allowed = Event("allowed", met); var maybe = Event("maybe", unknown);
        var broken = Event("broken", met); broken._eventRateItem = null;
        var pool = new PositionResultData { name = "Hall", _list = new List<PositionEventData> { denied, allowed, maybe, broken } };
        CheckPointManager.Instance._position._list.Add(pool);
        controller._positionData._defaultScript = "hall"; DataHelper.Scripts["hall"] = "Hall";
        var snapshot = Snapshot(); var row = snapshot.Summary;
        Check(row.Status.StartsWith("대기 임무 0 · 단계 후보 0 · 확인된 1회성 0"), "Ordinary activities do not inflate the default opportunity counts");
        Check(!row.AutoText.Contains("allowed", StringComparison.Ordinal) && !row.AutoText.Contains("fixture effects", StringComparison.Ordinal), "Automatic summary omits ordinary names and detailed effects");
        Check(Entry(snapshot, "allowed").Kind.Contains("기타 연결 정보") && Details(snapshot).Contains("fixture effects"), "Ordinary and unknown stories remain readable as individual detail rows");
        Check(Details(snapshot).Contains("읽기 실패") && Counters.GameCalls == 0, "One broken event is isolated without invoking game checks");
        int preparing = 0;
        while (Details(snapshot).Contains("효과·1회성 준비 중", StringComparison.Ordinal) && preparing++ < 16)
        {
            int coldBefore = LuaParser.Parses;
            snapshot = Snapshot();
            Check(LuaParser.Parses - coldBefore <= 1, "A scheduled read starts at most one new result analysis");
        }
        Check(preparing < 16, "Result preparation completes over bounded scheduled reads");
        int parses = LuaParser.Parses, reads = DataHelper.Reads, descriptions = LuaParser.Descriptions;
        Snapshot(); Time.unscaledTime = .25f; Snapshot();
        Check(LuaParser.Parses == parses && DataHelper.Reads == reads && LuaParser.Descriptions == descriptions, "Warm refresh reuses route/source/effect caches");
        broken._eventRateItem = Array.Empty<RateItem>(); Time.unscaledTime = 4.99f;
        Check(Details(Snapshot()).Contains("읽기 실패"), "Failed candidate keeps its five-second retry");
        Time.unscaledTime = 5; Check(Entry(Snapshot(), "broken").Status.Contains("진행 가능"), "Candidate recovers independently at five seconds");
        MapRouteDefinitions.State = MapRouteState.NotMet;
        Check(Entry(Snapshot(), "allowed").Status.Contains("경로 조건 미충족"), "Known event does not erase an unreachable intermediate route");
        MapRouteDefinitions.State = MapRouteState.Unknown;
        Check(Entry(Snapshot(), "allowed").Status.Contains("경로 확인 필요"), "Unknown route stays unknown rather than becoming available");
        MapRouteDefinitions.State = MapRouteState.Met; MapRouteDefinitions.Choice = true;
        Check(Entry(Snapshot(), "allowed").Status.Contains("선택 후 기회"), "Required choice is distinguished from an immediate event");
        MapRouteDefinitions.Choice = false;
        var proofFlag = new FlagData { name = "map-once", State = 0 };
        allowed._activeCondition = Compare(Flag(proofFlag), Constant(0));
        PositionEventCache.Parser.Actions["result_allowed"] = Result(Set("map-once", 1));
        snapshot = Snapshot(true, true);
        Check(snapshot.Summary.Status.Contains("확인된 1회성 1") && snapshot.Summary.AutoText.Contains("allowed"), "Proven current one-shot is counted and named separately from ordinary activity");
        int beforeEntryParses = LuaParser.Parses, beforeEntryReads = DataHelper.Reads;
        controller._positionData._conditionFlags = new[] { new PositionConditionFlagItem { _condition = zero } };
        snapshot = Snapshot(true, true);
        var entryBarrier = Entry(snapshot, "allowed");
        Check(!entryBarrier.Available && entryBarrier.Status.Contains("진입 시 상태 변경 가능") && entryBarrier.Status.Contains("경로 확인 필요") && snapshot.Summary.Status.Contains("확인된 1회성 0") && !snapshot.Summary.Available, "Even false condition-flag arms make activity entry uncertain and remove it from current one-shot counts");
        Check(entryBarrier.Detail.Contains("현재 이벤트 판정: 조건·가중치 충족") && entryBarrier.Detail.Contains("1회성") && entryBarrier.Detail.Contains("확정 상수 플래그"), "Entry uncertainty preserves the current condition snapshot and independent strict one-shot proof detail");
        proofFlag.State = 1;
        entryBarrier = Entry(Snapshot(false, true), "allowed");
        Check(!entryBarrier.Available && entryBarrier.Status.Contains("경로 확인 필요") && entryBarrier.Detail.Contains("현재 이벤트 판정: 조건·가중치 미충족"), "Entry changes keep a currently inactive event unknown at future entry rather than definitively unavailable");
        proofFlag.State = 0; controller._positionData._conditionFlags = null;
        Check(Entry(Snapshot(), "allowed").Status.Contains("진입 전 변경 여부 확인 불가"), "Null entry flag array is unknown, not the safe empty case");
        controller._positionData._conditionFlags = Array.Empty<PositionConditionFlagItem>();
        snapshot = Snapshot();
        Check(Entry(snapshot, "allowed").Available && snapshot.Summary.Status.Contains("확인된 1회성 1") && LuaParser.Parses == beforeEntryParses && DataHelper.Reads == beforeEntryReads, "Replacing the same asset entry array with empty restores current availability without reloading or reparsing Lua");
        MapRouteDefinitions.State = MapRouteState.Unknown; proofFlag.State = 1;
        entryBarrier = Entry(Snapshot(), "allowed");
        Check(entryBarrier.Status.Contains("경로 확인 필요") && entryBarrier.Detail.Contains("현재 이벤트 판정: 조건·가중치 미충족"), "Unknown intermediate route dominates current event ineligibility while preserving the current condition detail");
        MapRouteDefinitions.State = MapRouteState.Met; proofFlag.State = 0;
        snapshot = Snapshot(true, false);
        Check(snapshot.Summary.Status.Contains("확인된 1회성 0") && !snapshot.Summary.AutoText.Contains("allowed"), "Turning one-shot off cannot retain its cached badge or default opportunity count");
        snapshot = Snapshot(false, true);
        Check(snapshot.Summary.AutoText.Contains("allowed") && !snapshot.Summary.AutoText.Contains("fixture effects"), "One-shot-only mode retains the current proven mark without effect text");
        MapRouteDefinitions.State = MapRouteState.Unknown;
        Check(Entry(Snapshot(false, true), "allowed").Status.Contains("경로 확인 필요"), "One-shot-only badge retains unknown reachability rather than promising availability");
        MapRouteDefinitions.State = MapRouteState.Met;
        proofFlag.State = 1;
        snapshot = Snapshot(false, true);
        Check(!Entry(snapshot, "allowed").Available && Entry(snapshot, "allowed").Status.Contains("진행 불가") && snapshot.Summary.Status.Contains("확인된 1회성 0"), "One-shot-only mode never turns an already inactive one-shot into a current opportunity");
        proofFlag.State = 0; allowed._defaultRate = 0;
        Check(!Entry(Snapshot(false, true), "allowed").Available, "One-shot-only mode respects zero event weight");
        allowed._defaultRate = 10; controller._active = false;
        Check(!Entry(Snapshot(false, true), "allowed").Available, "One-shot-only mode respects disabled selected place");
        controller._active = true; controller._gameStatManager._stats._list[0]._value = 0;
        Check(!Entry(Snapshot(false, true), "allowed").Available && Entry(Snapshot(false, true), "allowed").Status.Contains("행동 부족"), "One-shot-only mode respects selected-place action cost");
        controller._gameStatManager._stats._list[0]._value = 3;
        controller._missionsManager.MainMissionStart = true; controller._missionsManager.MainMissionPosition = PositionType.Hall;
        Check(!Entry(Snapshot(false, true), "allowed").Available && Entry(Snapshot(false, true), "allowed").Status.Contains("주 임무 우선"), "One-shot-only mode respects placed-main priority");
        controller._missionsManager.MainMissionStart = false;
        controller._missionsManager.ActiveSubMissions["blocker"] = new MissionCheckData();
        Check(!Entry(Snapshot(false, true), "allowed").Available && Entry(Snapshot(false, true), "allowed").Status.Contains("대기 임무 우선"), "One-shot-only mode respects existing placed-submission priority");
        controller._missionsManager.ActiveSubMissions.Clear(); allowed._activeCondition = met;
        controller._gameStatManager._stats._list[0]._value = 0;
        Check(Entry(Snapshot(), "allowed").Status.Contains("행동 부족"), "Placed story follows the live action gate");
        controller._gameStatManager._stats._list[0]._value = 3;

        var stage = new MissionCheckData { name = "stage", _conditions = new[] { met }, _dayCheck = true, _specificTime = new GameTime { Year = 1, Month = 5, Stage = 1 } };
        var mission = Mission("logical-id", stage); controller._missionsManager._subMissions._list.Add(mission);
        snapshot = Snapshot();
        Check(snapshot.Summary.Status.Contains("단계 후보 1") && Entry(snapshot, "asset-logical-id").Status.Contains("대기 미확인"), "Satisfied current-key stage is a candidate, not a confirmed waiting entry");
        controller._missionsManager.ActiveSubMissions["logical-id"] = stage;
        snapshot = Snapshot();
        Check(snapshot.Summary.Status.Contains("대기 임무 1 · 단계 후보 0") && Entry(snapshot, "asset-logical-id").Status.Contains("첫 대기 임무"), "Logical ID associates existing dictionary state while deduplicating current stage");
        controller._positionData._conditionFlags = new[] { new PositionConditionFlagItem { _condition = zero } };
        Check(Entry(Snapshot(), "asset-logical-id").Available, "Staged placed submission dispatch precedes activity entry flags and remains unchanged");
        controller._positionData._conditionFlags = Array.Empty<PositionConditionFlagItem>();
        Check(Entry(snapshot, "allowed").Status.Contains("대기 임무 우선"), "Existing placed waiting entry blocks ordinary activities");
        var trackedQueue = new TrackingMissions { ["logical-id"] = stage };
        controller._missionsManager.ActiveSubMissions = trackedQueue;
        snapshot = Snapshot();
        Check(trackedQueue.Enumerations == 2, "Many candidate dispatch gates share one bounded queue snapshot plus the independent detail collection");
        string stagedId = Entry(snapshot, "asset-logical-id").Id;
        controller._missionsManager.ActiveSubMissions.Clear();
        Check(Entry(Snapshot(), "asset-logical-id").Id == stagedId, "Waiting/current-state changes preserve the logical stage row identity");
        controller._missionsManager.ActiveSubMissions["logical-id"] = stage;
        var laterWaiting = new MissionCheckData { name = "later-waiting", _conditions = new[] { met } };
        controller._missionsManager.ActiveSubMissions["later"] = laterWaiting;
        snapshot = Snapshot();
        Check(Entry(snapshot, "stage").Available && !Entry(snapshot, "later-waiting").Available && Entry(snapshot, "later-waiting").Status.Contains("앞선 대기 임무 우선"), "Only the first same-place waiting entry is dispatchable on this click");
        controller._missionsManager.ActiveSubMissions.Remove("later");
        stage._conditions = new[] { zero }; stage._active = false;
        var waiting = Entry(Snapshot(), "asset-logical-id");
        Check(waiting.Available && waiting.Status.Contains("현재 조건 미충족") && waiting.Detail.Contains("현재 조건: 미충족"), "Game-staged entry remains dispatchable while its independent current conditions differ");
        stage._active = true; stage._conditions = new[] { met };
        var replacement = new MissionCheckData { name = "replacement", _conditions = new[] { met } };
        controller._missionsManager.ActiveSubMissions["logical-id"] = replacement;
        snapshot = Snapshot();
        Check(Details(snapshot).Contains("replacement") && !Entry(snapshot, "replacement").Status.Contains("대기 미확인"), "Same dictionary/key/count replacement is read immediately even outside declared stage definitions");
        Check(Entry(snapshot, "stage").Status.Contains("대기 미확인"), "Old stage becomes an independent candidate after replacement");
        controller._missionsManager.ActiveSubMissions.Clear(); stage._specificTime.Month = 4;
        snapshot = Snapshot();
        Check(snapshot.Summary.Status.Contains("단계 후보 0") && Entry(snapshot, "stage").Detail.Contains("기간 밖"), "Period-outside candidate stays in detail and out of current opportunity counts");
        stage._timeCheckType = 2; stage._minTime = new GameTime { Year = 1, Month = 5, Stage = 1 }; stage._maxTime = new GameTime { Year = 1, Month = 5, Stage = 1 };
        Check(Entry(Snapshot(), "stage").Status.Contains("조건 충족"), "Interval boundaries are inclusive");
        stage._timeCheckType = 3; stage._specificTime.Month = 1;
        Check(Entry(Snapshot(), "stage").Detail.Contains("월 단계를 검사하지 않음") && Entry(Snapshot(), "stage").Status.Contains("조건 충족"), "Periodic metadata does not invent a check absent from the supplied game IL");
        stage._dayCheck = false;

        var unrelated = Mission("other-map", new MissionCheckData { name = "hidden", _position = PositionType.Kitchen, _conditions = null }); unrelated.BrokenCurrent = true;
        controller._missionsManager._subMissions._list.Add(unrelated);
        Snapshot(); int definitions = unrelated.DefinitionReads;
        Time.unscaledTime += .25f; Snapshot(); Snapshot();
        Check(unrelated.CurrentReads == 0 && unrelated.DefinitionReads == definitions, "Warm query never reads unrelated current keys or rescans unrelated stage definitions");
        var actualMain = new MissionCheckData { name = "actual-main", _conditions = new[] { zero } };
        controller._missionsManager.MainCheckData = actualMain; controller._missionsManager.MainMissionStart = true; controller._missionsManager.MainMissionPosition = PositionType.Hall;
        controller._gameStatManager._stats._list[0]._value = 0;
        snapshot = Snapshot();
        Check(Entry(snapshot, "actual-main").Available && Entry(snapshot, "actual-main").Status.Contains("이번 클릭의 주 임무"), "Staged main survives clear current key and bypasses action cost");
        controller._positionData._conditionFlags = new[] { new PositionConditionFlagItem { _condition = zero } };
        Check(Entry(Snapshot(), "actual-main").Available, "Staged placed main bypasses normal activity entry flags");
        controller._positionData._conditionFlags = Array.Empty<PositionConditionFlagItem>();
        controller._active = false; snapshot = Snapshot();
        Check(!snapshot.Summary.Available && snapshot.Summary.Status.Contains("대기 임무 1"), "A blocked waiting entry stays counted but does not color the panel as currently available");
        controller._active = true;
        controller._missionsManager.MainMissionStart = false;
        Check(!Details(Snapshot()).Contains("actual-main"), "MainMissionStart change invalidates waiting state with unchanged stage/key/count");
        controller._missionsManager.MainCheckData = null; controller._gameStatManager._stats._list[0]._value = 3;

        var global = new MissionCheckData { name = "global", _position = PositionType.無, _beforeMainMission = true, _conditions = new[] { met } };
        controller._missionsManager._subMissions._list.Add(Mission("global-id", global));
        controller._missionsManager.ActiveSubMissions["global-id"] = global;
        controller._active = false; controller._freeManager.IsProcessing = true; controller._gameStatManager._stats._list[0]._value = 0;
        snapshot = Snapshot();
        Check(Entry(snapshot, "global").Available && Entry(snapshot, "global").Kind.Contains("장소 무관") && Entry(snapshot, "global").Detail.Contains("주 임무 전"), "Global waiting route ignores selected-place activation/processing/action gates");
        controller._positionData._conditionFlags = null;
        Check(Entry(Snapshot(), "global").Available, "No-place mission route never inherits an unknown selected-place activity entry boundary");
        controller._positionData._conditionFlags = Array.Empty<PositionConditionFlagItem>();
        Check(snapshot.Summary.Status.Contains("대기 임무 0"), "Global entries do not inflate selected-place counts");
        var globalMain = new MissionCheckData { name = "global-main", _position = PositionType.無, _conditions = new[] { met } };
        var globalAfter = new MissionCheckData { name = "global-after", _position = PositionType.無, _conditions = new[] { met } };
        controller._missionsManager.MainCheckData = globalMain; controller._missionsManager.MainMissionStart = true; controller._missionsManager.MainMissionPosition = PositionType.無;
        controller._missionsManager.ActiveSubMissions["after"] = globalAfter;
        string globalOrder = Details(Snapshot());
        Check(globalOrder.IndexOf("asset-global-id", StringComparison.Ordinal) < globalOrder.IndexOf("global-main", StringComparison.Ordinal) && globalOrder.IndexOf("global-main", StringComparison.Ordinal) < globalOrder.IndexOf("global-after", StringComparison.Ordinal), "No-place detail preserves before-main/main/after-main route grouping");
        controller._missionsManager.MainMissionStart = false; controller._missionsManager.MainCheckData = null; controller._missionsManager.ActiveSubMissions.Remove("after");
        MapGuideSource.Deselect(controller); snapshot = Snapshot();
        Check(!snapshot.HasSelection && snapshot.Summary == null && Entry(snapshot, "global").Available, "Global detail remains available after deselection without a hidden place search");
        var savedManager = controller._missionsManager;
        controller._missionsManager = new MissionManagerData();
        Check(!Details(Snapshot()).Contains("asset-global-id"), "Changing the session manager clears old global source/failure snapshots immediately");
        controller._missionsManager = savedManager;
        Check(Entry(Snapshot(), "global").Available, "Restoring the observed manager produces a fresh global snapshot");
        controller._active = true; controller._freeManager.IsProcessing = false; controller._gameStatManager._stats._list[0]._value = 3;
        MapGuideSource.Select(controller);
        var pending = MapGuideSource.Discover(true, true)[0]; var published = Snapshot().Summary;
        var other = new PositionController { _positionData = new FreePositionData { name = "Other", _defaultScript = "" } };
        MapGuideSource.Select(other); MapGuideSource.Select(controller);
        Check(!MapGuideSource.IsCurrent(published), "A-B-A rejects an old selected overlay publication");
        try { pending.Read(); Check(false, "Old selection request must fail"); } catch (DataNotReadyException) { assertions++; }
        MapGuideSource.Deselect(other); Check(MapGuideSource.Visible, "Late deselect from another controller does not clear current selection");
        int before = DataHelper.Reads; controller.gameObject.activeInHierarchy = false;
        Check(!MapGuideSource.SessionVisible && MapGuideSource.Discover(true, true).Count == 0 && DataHelper.Reads == before, "Hidden session performs zero source reads");
        controller.gameObject.activeInHierarchy = true; MapGuideSource.Enabled = () => false;
        Check(!MapGuideSource.SessionVisible && MapGuideSource.Discover(true, true).Count == 0, "OFF does not discover display data");
        MapGuideSource.ClearSnapshots(); MapGuideSource.Enabled = () => true;
        Check(MapGuideSource.Visible, "OFF cleanup preserves the observed current selection handle");
        string context = MapGuideSource.ContextKey; MapGuideSource.ClearSnapshots(); Check(MapGuideSource.ContextKey == context, "Already-clean OFF cleanup does not churn context");
        Snapshot(true, false); snapshot = Snapshot(false, true);
        Check(!Details(snapshot).Contains("임무"), "Guide OFF with one-shot ON cannot retain previous mission rows");
        snapshot = Snapshot(true, false);
        Check(!snapshot.Summary.AutoText.Contains("fixture condition") && !snapshot.Summary.AutoText.Contains("fixture effects"), "Mixed settings preserve name/kind/status-only automatic text");

        controller = EmptyPlace(); MapGuideSource.Select(controller);
        for (int index = 0; index < 100; index++) controller._missionsManager._subMissions._list.Add(Mission("many-" + index, new MissionCheckData { name = "stage-" + index, _conditions = new[] { met } }));
        snapshot = Snapshot();
        Check(Details(snapshot).Contains("임무 정의 준비 중"), "Cold mission indexing yields a preparing status instead of scanning the entire graph in one read");
        int prepared = 0; while (Details(snapshot).Contains("임무 정의 준비 중") && prepared++ < 20) { Time.unscaledTime += .25f; snapshot = Snapshot(); }
        Check(prepared < 20 && snapshot.Summary.Status.Contains("단계 후보 100"), "Resumable index completes and exposes all related current stages");
        int definitionReads = 0; foreach (var value in controller._missionsManager._subMissions._list) definitionReads += value.DefinitionReads;
        Snapshot(); Snapshot(); int afterDefinitions = 0; foreach (var value in controller._missionsManager._subMissions._list) afterDefinitions += value.DefinitionReads;
        Check(afterDefinitions == definitionReads, "Completed definition index stays warm without full rescans");
        controller._missionsManager._subMissions._list = null; float failedAt = Time.unscaledTime;
        snapshot = Snapshot(); int indexReads = controller._missionsManager._subMissions.ListReads;
        Check(Details(snapshot).Contains("게임 데이터 대기 중"), "Malformed index is an explicit diagnostic, not successful zero");
        Time.unscaledTime = failedAt + 1; Snapshot(); Check(controller._missionsManager._subMissions.ListReads == indexReads, "Index source failure has five-second retry backoff");
        controller._missionsManager._subMissions._list = new List<MissionData>(); Time.unscaledTime = failedAt + 5;
        Check(!Details(Snapshot()).Contains("게임 데이터 대기 중"), "Definition-index failure recovers from a replacement collection at five seconds");
        controller = EmptyPlace(); MapGuideSource.Select(controller);
        controller._positionData = new UnreadableEntryPositionData { name = "unreadable-entry", _defaultScript = "fixed_entry" };
        DataHelper.Scripts["fixed_entry"] = "";
        snapshot = Snapshot();
        Check(Entry(snapshot, "fixed_entry").Status.Contains("진입 전 변경 여부 확인 불가") && !Entry(snapshot, "fixed_entry").Available, "Unreadable entry flags make the fixed activity unknown without executing the entry chain");
        Check(Counters.GameCalls == 0, "All map/one-shot/mission reads preserve game evaluator counters");
    }
    private static void Options()
    {
        OptionHintSource.Clear(); OneShotProof.Clear(); OptionHintSource.Enabled = () => true;
        var menu = new Fungus.MenuDialog(); Fungus.MenuDialog.ActiveMenuDialog = menu;
        var button = new NormalOptionButton(); button.transform.Parent = menu.transform;
        button._source.text = "O_first"; button._text[0].text = "original option";
        LuaParser.Instance.OptionResult["O_first"] = Result(Set("F1", 1));
        OptionHintSource.Observe(button);
        Check(OptionHintSource.Visible, "Visible live menu/button requires no mouse hover");
        var queries = OptionHintSource.Discover(); Check(queries.Count == 1, "Only currently visible option is discovered");
        var proof = (OneShotResult)queries[0].Read();
        Check(!proof.Proven && proof.Status.Contains("확인 불가"), "A rendered button boolean does not invent its activation condition");
        OptionHintSource.RenderHints();
        Check(button._text[0].text == "original option · 1회성 확인 불가", "Delayed cached tag reaches already-updated game option text");
        int writes = button._text[0].Writes, parses = LuaParser.Parses;
        OptionHintSource.RenderHints(); OptionHintSource.RenderHints();
        Check(button._text[0].Writes == writes && LuaParser.Parses == parses, "Rendering warm hints neither rewrites same UI nor analyzes Lua");
        var condition = Compare(Flag(new FlagData { name = "F1" }), Constant(0));
        OneShotProof.ForOption("O_first", condition, Result(Set("F1", 1)));
        OptionHintSource.RenderHints(); Check(button._text[0].text == "original option · 1회성", "Exact gate proof replaces unknown cached text without action execution");
        OptionHintSource.Clear(); Check(button._text[0].text == "original option", "OFF/context clear restores only the appended annotation");
        string context = OptionHintSource.ContextKey; OptionHintSource.Clear();
        Check(OptionHintSource.ContextKey == context, "Already-empty clear does not reset scheduling each frame");
        OptionHintSource.Enabled = () => false;
        Check(!OptionHintSource.Visible && OptionHintSource.Discover().Count == 0, "OFF performs no option proof query");
        OptionHintSource.Enabled = () => true; queries = OptionHintSource.Discover();
        Check(queries.Count == 1, "Weak handle supports OFF to ON without game method replay");
        queries[0].Read(); OptionHintSource.RenderHints();
        button._text[0].text = "replacement by game";
        OptionHintSource.Clear(); Check(button._text[0].text == "replacement by game", "Cleanup does not overwrite a newer external content update");
        button._source.text = "O_second"; button._text[0].text = "second option";
        LuaParser.Instance.OptionResult["O_second"] = Result(); OptionHintSource.Observe(button);
        queries = OptionHintSource.Discover(); queries[0].Read(); OptionHintSource.RenderHints();
        Check(button._text[0].text == "second option · 1회성 확인 불가" && !button._text[0].text.Contains("original"), "A reused button receives only its current key snapshot");
        OptionHintSource.MenuCleared(menu);
        Check(button._text[0].text == "second option" && OneShotProof.CachedOptionTag("O_second") == "", "Game menu Clear discards session proofs and restores UI");
        menu.gameObject.activeInHierarchy = false; writes = button._text[0].Writes;
        Check(!OptionHintSource.Visible && OptionHintSource.Discover().Count == 0, "Hidden menu performs no read retries");
        OptionHintSource.RenderHints(); Check(button._text[0].Writes == writes, "Hidden menu does not decorate stale text");
    }
    public static int Main() { Proofs(); Map(); Options(); Console.WriteLine("PASS: " + assertions + " map/one-shot managed boundary assertions; Unity/Windows runtime not executed."); return 0; }
}
