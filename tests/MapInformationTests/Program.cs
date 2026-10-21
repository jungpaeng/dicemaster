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
        Check(!OneShotProof.ForOption("option", Result(Set("F1", 1))).Proven && OneShotProof.CachedOptionTag("option") == "", "No activation evidence adds no noisy automatic badge");
        Check(OneShotProof.CachedOptionDetail("option").Contains("1회성 확인 불가") && OneShotProof.CachedOptionDetail("option").Contains("활성 조건"), "Unknown option proof remains available in explicit detail");
        Check(OneShotProof.ForOption("option", condition, Result(Set("F1", 1))).Proven && OneShotProof.CachedOptionTag("option") == " · 1회성", "Option with explicit exact gate uses same proof");
        OneShotProof.Clear(); Check(OneShotProof.CachedOptionTag("option") == "", "OFF/context clear removes cached hints");
        var target = Event("proof-cache", condition);
        DiceMaster.PositionEventCache.Parser.Actions["proof_result"] = Result(Set("F1", 1));
        OneShotResult first = OneShotProof.ForEvent(target, "proof_result");
        Check(first.Proven && OneShotProof.CachedEventTag(target) == " · 1회성", "Exact event proof supplies the positive-only badge");
        OneShotResult cached;
        Check(OneShotProof.TryGetEvent(target, "proof_result", out cached) && ReferenceEquals(first, cached) && ReferenceEquals(first, OneShotProof.ForEvent(target, "proof_result")), "Warm proof reuses definition evidence rather than re-evaluating results");
        flag.State = 19;
        Check(OneShotProof.TryGetEvent(target, "proof_result", out cached) && ReferenceEquals(first, cached), "Definition proof does not depend on live pre-result flag values");
        target._activeCondition = Compare(Flag(flag), Constant(1));
        Check(!OneShotProof.TryGetEvent(target, "proof_result", out cached) && OneShotProof.CachedEventTag(target) == "", "Replacing active condition discards the old badge and proof");
        Check(!OneShotProof.ForEvent(target, "proof_result").Proven && OneShotProof.CachedEventTag(target) == "" && OneShotProof.CachedEventDetail(target).Contains("확인 불가"), "Unknown event proof is visible only as detail");
        Check(!OneShotProof.TryGetEvent(target, "other_result", out cached), "Changing the result script invalidates cached definition evidence");
        target._toggleCondition = false;
        Check(!OneShotProof.ForEvent(target, "proof_result").Proven && OneShotProof.CachedEventTag(target) == "", "Missing active condition adds no automatic failure or unknown label");
        OneShotProof.Clear();
        Check(!OneShotProof.TryGetEvent(target, "proof_result", out cached) && OneShotProof.CachedEventDetail(target) == "", "Context cleanup removes both detail and definition cache");
    }
    private static void Presentation()
    {
        var notices = new List<string> {
            MapGuidePresentation.Notice("약초 찾기", "보조 임무", 0, true, true, "대기 등록"),
            MapGuidePresentation.Notice("사형과 대화", "주 임무", 0, false, true, "조건 충족 · 대기 미확인"),
            MapGuidePresentation.Notice("문파 방문", "1회성 이야기", 1, false, false, "진행 불가"),
            MapGuidePresentation.Notice("나머지 이야기", "1회성 이야기", 2, false, false, "경로 확인 필요") };
        string text = MapGuidePresentation.Summary(notices, true, false);
        Check(text.Contains("약초 찾기 · 보조 임무 · 진행 가능") && text.Contains("사형과 대화 · 주 임무 · 발생 조건 충족"), "Notice distinguishes ready waiting missions from currently met stage candidates in plain language");
        Check(text.Contains("문파 방문 · 1회성 이야기 · 지금 진행 불가") && !text.Contains("나머지 이야기") && text.Contains("외 1개"), "Automatic notice bounds complete entries rather than cutting statuses midway");
        Check(text.EndsWith("자세히: 로그 → 도구 → 지도 안내") && !text.Contains("단계 후보") && !text.Contains("확인 불가"), "Compact notice keeps the detail path and omits analysis jargon");
        Check(MapGuidePresentation.Summary(new List<string>(), true, true).StartsWith("이 장소의 안내를 확인하는 중"), "Preparing is distinct from confirmed empty");
        Check(MapGuidePresentation.Summary(new List<string>(), true, false) == "", "Settled empty notices leave no passive panel or detail-link-only noise");
        Check(MapGuidePresentation.Summary(null, true, false) == "", "A settled absent notice list also hides the passive panel");
        Check(MapGuidePresentation.Summary(new List<string>(), false, true) == "", "One-shot-only mode does not create an empty or unknown noise panel");
    }
    private static MapGuideSnapshot Snapshot(bool guide = true, bool once = true, bool details = true)
    { var queries = MapGuideSource.Discover(guide, once, details); Check(queries.Count == 1, "One observed session has one scheduled query"); return (MapGuideSnapshot)queries[0].Read(); }
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
        Check(row.AutoText.StartsWith("이 장소의 안내를 확인하는 중", StringComparison.Ordinal), "Unfinished result analysis contributes real preparation independently of a simultaneous failed candidate");
        int preparing = 0;
        while (Details(snapshot).Contains("효과·1회성 준비 중", StringComparison.Ordinal) && preparing++ < 16)
        {
            int coldBefore = LuaParser.Parses;
            snapshot = Snapshot();
            Check(LuaParser.Parses - coldBefore <= 1, "A scheduled read starts at most one new result analysis");
        }
        Check(preparing < 16, "Result preparation completes over bounded scheduled reads");
        Check(snapshot.Summary.AutoText == "" && Details(snapshot).Contains("읽기 실패"), "Once result analysis finishes, a remaining failed candidate cannot retain a checking-only empty panel");
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
    private static void StoryConditionAggregation()
    {
        MapGuideSource.Clear(); MapGuideSource.Enabled = () => true;
        MapRouteDefinitions.StoryFixtures.Clear(); MapRouteDefinitions.StoryIncomplete = false;
        var story = new MapStoryEntry();
        MapRoutePath Path(MapRouteState state) => new MapRoutePath { DialogueCharacter = "brother4", StoryState = state,
            Steps = new List<MapChoiceStep> { new MapChoiceStep { Script = "story_gate", SourceOffset = 7, Index = 1, RawLabel = "수련" } } };
        var first = Path(MapRouteState.NotMet); var second = Path(MapRouteState.NotMet);
        story.Paths.Add(first); story.Paths.Add(second); MapRouteDefinitions.StoryFixtures.Add(story);
        DataHelper.Scripts["story_gate"] = "";
        var controller = EmptyPlace(); controller._positionData._defaultScript = "story_gate"; MapGuideSource.Select(controller);
        var value = Snapshot(); var row = Entry(value, "수련");
        Check(row.Status == "지금 진행 불가" && !row.Available && !value.Summary.Available,
            "True first-option fixture guards cannot override two false full NPC-story paths");
        second.StoryState = MapRouteState.Met; value = Snapshot(); row = Entry(value, "수련");
        Check(row.Status == "진행 가능" && row.Available && !value.Summary.Available && value.Summary.AutoText == "",
            "A positive research story retains its detailed state without coloring or opening the passive map");
        second.StoryState = MapRouteState.Unknown; value = Snapshot();
        Check(Entry(value, "수련").Status == "진행 가능성 있음" && !Entry(value, "수련").Available,
            "A false path plus an unknown alternative remains possible rather than green or definitively blocked");
        second.StoryState = MapRouteState.NotMet; MapRouteDefinitions.StoryIncomplete = true; value = Snapshot();
        Check(Entry(value, "수련").Status == "진행 가능성 있음" && !Entry(value, "수련").Available,
            "Structural omissions prevent an all-false sampled prefix from rejecting unexamined story alternatives");
        MapRouteDefinitions.StoryIncomplete = false;

        story = new MapStoryEntry();
        for (int at = 0; at < 33; at++) story.Paths.Add(Path(at == 32 ? MapRouteState.Met : MapRouteState.NotMet));
        MapRouteDefinitions.StoryFixtures.Clear(); MapRouteDefinitions.StoryFixtures.Add(story);
        controller._positionData._defaultScript = "story_gate_omitted"; DataHelper.Scripts["story_gate_omitted"] = "";
        value = Snapshot(); row = Entry(value, "수련");
        Check(row.Status == "진행 가능성 있음" && !row.Available && story.Paths[32].StoryReads == 0, "The provider's 32-path group bound cannot infer all-false or read a positive thirty-third path");
        story.Paths[0].StoryState = MapRouteState.Met; value = Snapshot();
        Check(Entry(value, "수련").Available && Entry(value, "수련").Status == "진행 가능",
            "A proven retained positive path is sufficient even when later alternatives were omitted");
        Check(Counters.GameCalls == 0, "Full-story aggregation is display-only and executes no game condition or result");
        MapRouteDefinitions.StoryFixtures.Clear(); MapRouteDefinitions.StoryIncomplete = false; MapGuideSource.Clear();
    }
    private static void AutomaticNoticeFiltering()
    {
        var controller = EmptyPlace(); MapGuideSource.Select(controller);
        MapRouteDefinitions.StoryFixtures.Clear(); MapRouteDefinitions.StoryIncomplete = false;
        var paths = new List<MapRoutePath>();
        void Story(string name, MapRouteState state)
        {
            var path = new MapRoutePath { DialogueCharacter = "brother4", StoryState = state,
                Steps = new List<MapChoiceStep> { new MapChoiceStep { Script = "filtered_notices", SourceOffset = 7, Index = paths.Count + 1, RawLabel = name } } };
            paths.Add(path); MapRouteDefinitions.StoryFixtures.Add(new MapStoryEntry { Paths = new List<MapRoutePath> { path } });
        }
        Story("숨김_대화_A", MapRouteState.NotMet); Story("열린_대화", MapRouteState.Met);
        Story("제목에 지금 진행 불가", MapRouteState.Unknown); Story("불명_대화_B", MapRouteState.Unknown);
        Story("숨김_대화_B", MapRouteState.NotMet); Story("불명_대화_C", MapRouteState.Unknown); Story("불명_대화_D", MapRouteState.Unknown);
        var zero = Compare(Constant(0), Constant(1));
        controller._missionsManager._subMissions._list.Add(Mission("숨김_임무", new MissionCheckData { name = "hidden-stage", _conditions = new[] { zero } }));
        var flag = new FlagData { name = "hidden-once", State = 1 };
        var once = Event("숨김_1회성", Compare(Flag(flag), Constant(0)));
        CheckPointManager.Instance._position._list.Add(new PositionResultData { name = "FilteredPool", _scriptPrefix = "filtered_result", _list = new List<PositionEventData> { once } });
        PositionEventCache.Parser.Actions["filtered_result_숨김_1회성"] = Result(Set("hidden-once", 1));
        DataHelper.Scripts["filtered_notices"] = "FilteredPool"; controller._positionData._defaultScript = "filtered_notices";
        var value = Snapshot();
        for (int i = 0; i < 4 && Details(value).Contains("효과·1회성 준비 중"); i++) value = Snapshot();
        Check(!Entry(value, "숨김_대화_A").Available && !Entry(value, "숨김_임무").Available && !Entry(value, "숨김_1회성").Available && Entry(value, "숨김_1회성").Status.Contains("1회성"),
            "Blocked dialogue, mission and proven one-shot rows retain their individual status and detail publication");
        Check(!value.Summary.AutoText.Contains("숨김_대화_A") && !value.Summary.AutoText.Contains("숨김_대화_B") && !value.Summary.AutoText.Contains("숨김_임무") && !value.Summary.AutoText.Contains("숨김_1회성"),
            "Typed blocked candidates of every supported kind are omitted only from the automatic map notices");
        Check(value.Summary.AutoText == "" && !value.Summary.Available && Entry(value, "열린_대화").Available && Entry(value, "제목에 지금 진행 불가").Status == "진행 가능성 있음",
            "Positive and unknown research stories remain in detail only; literal status words in their names do not affect the typed policy");
        foreach (var path in paths) path.StoryState = MapRouteState.NotMet;
        value = Snapshot();
        Check(value.Summary.AutoText == "",
            "An all-blocked map hides the passive panel without hidden names, phantom rows, an overflow count or an empty message");
        Check(Entry(value, "열린_대화").Status == "지금 진행 불가" && Entry(value, "숨김_임무").Detail.Length > 0 && Entry(value, "숨김_1회성").Detail.Length > 0 && !value.Summary.Available,
            "Filtering all automatic rows leaves their condition/effect details intact and does not invent an available summary");
        Check(Counters.GameCalls == 0 && flag.State == 1, "Automatic filtering executes no game judgment, result, RNG or consumed one-shot flag write");
        MapRouteDefinitions.StoryFixtures.Clear(); MapRouteDefinitions.StoryIncomplete = false; MapGuideSource.Clear();
    }
    private static void SettledEmptyOverlay()
    {
        var controller = EmptyPlace(); MapGuideSource.Select(controller);
        var value = Snapshot();
        Check(value.Summary.AutoText == "" && Details(value).Contains("현재 연결 항목 없음"),
            "A genuinely empty selected place hides its passive panel and retains explicit empty detail");
        controller._positionData._defaultScript = "empty_overlay"; DataHelper.Scripts["empty_overlay"] = "";
        MapRouteDefinitions.FixturePreparing = true;
        value = Snapshot();
        Check(value.Summary.AutoText.StartsWith("이 장소의 안내를 확인하는 중", StringComparison.Ordinal) && Details(value).Contains("경로 준비 중"),
            "Actual unfinished route work exposes a checking notice even when no automatic candidates have been discovered yet");
        MapRouteDefinitions.FixturePreparing = false; MapRouteDefinitions.FixturePartial = true;
        value = Snapshot();
        Check(value.Summary.AutoText == "" && Details(value).Contains("경로 일부 확인 불가") && value.Summary.Status.Contains("경로 일부 확인 불가"),
            "Settled route-analysis limits keep their distinct diagnostic detail without masquerading as ongoing work");
        int reads = DataHelper.Reads, parses = LuaParser.Parses;
        bool stayedHidden = true;
        for (int at = 0; at < 200; at++) { Time.unscaledTime += .25f; stayedHidden &= Snapshot().Summary.AutoText == ""; }
        Check(stayedHidden && DataHelper.Reads == reads && LuaParser.Parses == parses,
            "Two hundred warm settled-empty refreshes stay hidden without reloading or reparsing Lua");
        MapRouteDefinitions.FixtureReadFailure = true;
        value = Snapshot();
        Check(value.Summary.AutoText == "" && Details(value).Contains("경로 읽기 실패") && !Details(value).Contains("경로 준비 중"),
            "Route read failure remains a failure in the log and cannot keep a checking-only panel alive");
        MapRouteDefinitions.FixturePartial = false; MapRouteDefinitions.FixtureReadFailure = false;
        value = Snapshot();
        Check(value.Summary.AutoText == "" && !Details(value).Contains("경로 읽기 실패"),
            "Recovery to a settled empty route removes the failure state while leaving the panel hidden");

        controller = EmptyPlace(); MapGuideSource.Select(controller);
        var zero = Compare(Constant(0), Constant(1));
        for (int at = 0; at < 100; at++) controller._missionsManager._subMissions._list.Add(Mission("empty-index-" + at,
            new MissionCheckData { name = "empty-stage-" + at, _conditions = new[] { zero } }));
        value = Snapshot();
        Check(value.Summary.AutoText == "" && Details(value).Contains("임무 정의 준비 중"),
            "Unfinished global mission indexing stays in detail without keeping an empty selected-place overlay alive");
        int pending = 0;
        while (Details(value).Contains("임무 정의 준비 중") && pending++ < 20) { Time.unscaledTime += .25f; value = Snapshot(); }
        Check(pending < 20 && value.Summary.AutoText == "" && Entry(value, "empty-index-0").Status.Contains("조건 미충족"),
            "Finishing an all-blocked mission index clears the checking panel without dropping the blocked details");
        controller._missionsManager._subMissions._list = null;
        value = Snapshot();
        Check(value.Summary.AutoText == "" && Details(value).Contains("게임 데이터 대기 중") && !Details(value).Contains("임무 정의 준비 중"),
            "Missing game data is retained as a distinct waiting detail rather than reported as active index preparation or confirmed none");
        Check(Counters.GameCalls == 0, "Empty-overlay preparation and diagnostics never execute game judgments or results");
        MapGuideSource.Clear();
    }

    private static void IndependentPreparation()
    {
        var controller = EmptyPlace(); MapGuideSource.Select(controller);
        var no = Compare(Constant(0), Constant(1));
        for (int i = 0; i < 100; i++)
        {
            var stages = new MissionCheckData[100];
            for (int j = 0; j < stages.Length; j++) stages[j] = new MissionCheckData {
                name = "unrelated-" + i + "-" + j, _position = PositionType.Kitchen, _conditions = new[] { no }, _key = "stage-" + j };
            var mission = Mission("bulk-" + i, stages[0]); mission._missions = stages;
            controller._missionsManager._subMissions._list.Add(mission);
        }
        var value = Snapshot();
        Check(value.Summary.AutoText == "" && Details(value).Contains("임무 정의 준비 중"),
            "Ten thousand unrelated stages cannot hold an empty selected-place overlay open on the first query");
        var waiting = new MissionCheckData { name = "live-waiting", _startScript = "live_story", _conditions = new[] { Compare(Constant(1), Constant(1)) } };
        controller._missionsManager.ActiveSubMissions["live-id"] = waiting;
        value = Snapshot();
        Check(value.Summary.AutoText.Contains("진행 가능") && Details(value).Contains("임무 정의 준비 중"),
            "The live waiting mission is published while the independent global definition index is still incomplete");
        controller._missionsManager.ActiveSubMissions.Clear();
        bool quiet = true; int ticks = 0;
        do
        {
            // Live MainCheckData is a dispatch value, not an immutable index
            // revision. Replacing it must not restart all unrelated stages.
            controller._missionsManager.MainCheckData = new MissionCheckData { name = "live-main-" + ticks };
            Time.unscaledTime += .25f; value = Snapshot();
            quiet &= value.Summary.AutoText == "";
        } while (Details(value).Contains("임무 정의 준비 중") && ++ticks < 180);
        Check(quiet && ticks < 180, "Global indexing completes across changing live dispatch values without showing an empty-place loader");
        int definitionReads = 0; foreach (var mission in controller._missionsManager._subMissions._list) definitionReads += mission.DefinitionReads;
        Snapshot(); Snapshot();
        int after = 0; foreach (var mission in controller._missionsManager._subMissions._list) after += mission.DefinitionReads;
        Check(after == definitionReads && definitionReads == 100, "Warm immutable definitions are read once despite live MainCheckData replacements");
        controller._missionsManager._subMissions._list = null;
        controller._missionsManager.ActiveSubMissions["live-id"] = waiting;
        value = Snapshot();
        Check(value.Summary.AutoText.Contains("진행 가능") && Details(value).Contains("게임 데이터 대기 중"),
            "A global definition-source failure cannot erase a valid live waiting mission");
        Check(Counters.GameCalls == 0, "Independent preparation and live waiting reads execute no gameplay judgment");
        MapGuideSource.Clear();
    }
    private static void AutomaticScope()
    {
        var controller = EmptyPlace(); MapGuideSource.Select(controller);
        var yes = Compare(Constant(1), Constant(1));
        var stage = new MissionCheckData { name = "unregistered-stage", _conditions = new[] { yes } };
        var mission = Mission("unregistered-definition", stage);
        controller._missionsManager._subMissions._list.Add(mission);
        var path = new MapRoutePath { DialogueCharacter = "renamed_npc", StoryState = MapRouteState.Met,
            Steps = new List<MapChoiceStep> { new MapChoiceStep { Script = "scope_root", RawLabel = "positive-research", Index = 1 } } };
        MapRouteDefinitions.StoryFixtures.Add(new MapStoryEntry { Paths = new List<MapRoutePath> { path } });
        var flag = new FlagData { name = "scope-once", State = 0 };
        var once = Event("proven-once", Compare(Flag(flag), Constant(0)));
        CheckPointManager.Instance._position._list.Add(new PositionResultData { name = "ScopePool", _scriptPrefix = "scope_result", _list = new List<PositionEventData> { once } });
        PositionEventCache.Parser.Actions["scope_result_proven-once"] = Result(Set("scope-once", 1));
        DataHelper.Scripts["scope_root"] = "ScopePool"; controller._positionData._defaultScript = "scope_root";
        for (int i = 0; i < 4; i++) Snapshot(details: false);
        Check(mission.DefinitionReads == 0 && mission.CurrentReads == 0 && path.StoryReads == 0,
            "Passive map neither builds unregistered-stage definitions nor samples downstream NPC story conditions");
        var value = Snapshot();
        Check(Entry(value, "unregistered-stage").Available && Entry(value, "positive-research").Available && value.Summary.AutoText.Contains("proven-once") &&
            !value.Summary.AutoText.Contains("unregistered-stage") && !value.Summary.AutoText.Contains("positive-research"),
            "Detailed positive research does not become passive advice; a reachable proven one-shot does");
        value = Snapshot(once: false);
        Check(value.Summary.AutoText == "" && !value.Summary.Available && Entry(value, "unregistered-stage").Available,
            "Guide alone cannot publish or color the map from unregistered mission or positive NPC research");
        MapRouteDefinitions.State = MapRouteState.Unknown; value = Snapshot();
        Check(Entry(value, "proven-once").Status.Contains("1회성") && !Entry(value, "proven-once").Available && value.Summary.AutoText == "",
            "A proven one-shot with unknown reachability keeps its independent proof in detail and gets no automatic notice");
        MapRouteDefinitions.State = MapRouteState.Met;
        controller._missionsManager.ActiveSubMissions["waiting"] = stage;
        value = Snapshot();
        Check(value.Summary.AutoText.Contains("진행 가능") && value.Summary.AutoText.Contains("unregistered-stage") && !value.Summary.AutoText.Contains("proven-once"),
            "Actual waiting membership permits the first dispatchable mission and blocks its competing activity");
        controller._gameStatManager._stats._list[0]._value = 0; value = Snapshot();
        Check(value.Summary.AutoText == "" && !value.Summary.Available && Entry(value, "unregistered-stage").Status.Contains("행동 부족"),
            "A registered waiting mission still needs sufficient action before becoming automatic advice");
        controller._missionsManager.ActiveSubMissions.Clear(); controller._gameStatManager._stats._list[0]._value = 3;
        var pool = CheckPointManager.Instance._position._list[0];
        for (int i = 0; i < 4; i++)
        {
            string id = "extra-once-" + i; var input = new FlagData { name = id, State = 0 };
            pool._list.Add(Event(id, Compare(Flag(input), Constant(0))));
            PositionEventCache.Parser.Actions["scope_result_" + id] = Result(Set(id, 1));
        }
        controller._positionData._defaultScript = "scope_more"; DataHelper.Scripts["scope_more"] = "ScopePool";
        for (int i = 0; i < 8; i++) value = Snapshot();
        Check(Array.FindAll(value.Summary.AutoText.Split('\n'), line => line.StartsWith("• ", StringComparison.Ordinal)).Length == 3 && value.Summary.AutoText.Contains("외 2개"),
            "Row limit and overflow count include only five reachable proven one-shots, excluding all research candidates");
        int storyReads = path.StoryReads, definitions = mission.DefinitionReads, keys = mission.CurrentReads;
        for (int i = 0; i < 30; i++) { Time.unscaledTime += .25f; value = Snapshot(details: false); }
        Check(path.StoryReads == storyReads && mission.DefinitionReads == definitions && mission.CurrentReads == keys &&
            !Details(value).Contains("positive-research") && !Details(value).Contains("unregistered-stage"),
            "Closing research returns to passive-only work without inherited research rows or current-key sampling");
        Check(Counters.GameCalls == 0 && flag.State == 0, "Automatic scope restriction preserves native counters, RNG, and one-shot flags");
        MapRouteDefinitions.StoryFixtures.Clear(); MapGuideSource.Clear();
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
        Check(button._text[0].text == "original option", "Unproven options keep their original game text");
        int writes = button._text[0].Writes, parses = LuaParser.Parses;
        int labelReads = button._text[0].Reads;
        for (int frame = 0; frame < 1000; frame++) OptionHintSource.RenderHints();
        Check(button._text[0].Reads == labelReads, "1000 warm renders do not reread unchanged option label text");
        Check(button._text[0].Writes == writes && LuaParser.Parses == parses, "Rendering warm hints neither rewrites same UI nor analyzes Lua");
        var condition = Compare(Flag(new FlagData { name = "F1" }), Constant(0));
        OneShotProof.ForOption("O_first", condition, Result(Set("F1", 1)));
        OptionHintSource.RenderHints(); Check(button._text[0].text == "original option · 1회성", "Exact gate proof replaces unknown cached text without action execution");
        button.isActiveAndEnabled = false; labelReads = button._text[0].Reads;
        OneShotProof.ForOption("O_first", Result(Set("F1", 1)));
        OptionHintSource.RenderHints();
        Check(!OptionHintSource.Visible && button._text[0].Reads == labelReads, "Inactive controls are neither visible requests nor render-time label reads");
        button.isActiveAndEnabled = true; OptionHintSource.RenderHints();
        Check(button._text[0].text == "original option", "An expired proof removes only its owned badge when the control becomes visible");
        Fungus.MenuDialog.ActiveMenuDialog = new Fungus.MenuDialog(); labelReads = button._text[0].Reads;
        OptionHintSource.RenderHints();
        Check(!OptionHintSource.Visible && button._text[0].Reads == labelReads, "Controls belonging to a different active menu perform no hint work");
        Fungus.MenuDialog.ActiveMenuDialog = menu;
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
        Check(button._text[0].text == "second option" && !button._text[0].text.Contains("original"), "A reused unknown button has no noisy annotation or stale name");
        OptionHintSource.MenuCleared(menu);
        Check(button._text[0].text == "second option" && OneShotProof.CachedOptionTag("O_second") == "", "Game menu Clear discards session proofs and restores UI");
        menu.gameObject.activeInHierarchy = false; writes = button._text[0].Writes;
        Check(!OptionHintSource.Visible && OptionHintSource.Discover().Count == 0, "Hidden menu performs no read retries");
        OptionHintSource.RenderHints(); Check(button._text[0].Writes == writes, "Hidden menu does not decorate stale text");
    }
    public static int Main() { Proofs(); Presentation(); Map(); StoryConditionAggregation(); AutomaticNoticeFiltering(); SettledEmptyOverlay(); IndependentPreparation(); AutomaticScope(); Options(); Console.WriteLine("PASS: " + assertions + " map/one-shot managed boundary assertions; Unity/Windows runtime not executed."); return 0; }
}
