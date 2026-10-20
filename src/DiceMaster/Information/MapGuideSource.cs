using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using DiceMaster.EventSelection;
using DiceMaster.LiveLog;
using DiceMaster.LuaParse;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;
using UnityEngine;
using LuaAction = DiceMaster.LuaParse.Action;

namespace DiceMaster.Information
{
    // Only live controllers registered by the game's own lifecycle are used.
    // No Unity object search, hidden-map discovery, mission activation, or
    // gameplay evaluation occurs here. The owner supplies visibility cadence.
    public static class MapGuideSource
    {
        private sealed class Place
        {
            internal WeakReference Controller;
            internal int Id;
            internal object Data, PositionConfig;
            internal string Script, Key;
            internal Source Definition;
            internal readonly Dictionary<object, CandidateRead> Reads = new Dictionary<object, CandidateRead>();
        }
        private sealed class Source
        {
            internal readonly List<EventDefinition> Events = new List<EventDefinition>();
            internal string Script;
            internal MapRouteDefinitions Routes;
            internal Exception Failure;
            internal float RetryAt;
            internal bool Ready;
            internal readonly Dictionary<string, string> PoolDiagnostics = new Dictionary<string, string>(StringComparer.Ordinal);
            internal readonly Dictionary<MapRouteEntry, EventDefinition> RouteEvents = new Dictionary<MapRouteEntry, EventDefinition>();
        }
        private sealed class EventDefinition
        {
            internal PositionEventData Data;
            internal string Script, Path, Id, Name;
            internal bool HasChoice;
            internal MapRouteState RouteState;
        }
        private sealed class ScriptDefinition
        {
            internal List<LuaAction> Actions;
            internal string Description;
            internal float RetryAt;
            internal bool Preparing;
        }
        private sealed class Candidate
        {
            internal string Id, Name, Kind, Status, Detail;
            internal int Rank, Priority; // conditions/route met, denied, unknown
            internal bool OneShot, StatusOnly, Waiting, Mission, Global, Other, Main;
            internal object Stage;
        }
        private sealed class CandidateRead
        {
            internal Candidate Last;
            internal Exception Failure;
            internal float RetryAt;
        }
        private sealed class MissionLink
        {
            internal object Mission;
            internal bool Main;
            internal string LogicalId;
            internal readonly Dictionary<string, object> Stages = new Dictionary<string, object>(StringComparer.Ordinal);
        }
        private sealed class DispatchSnapshot
        {
            internal bool Active, Processing, MainStarted, SubOrderUnknown, ActionUnknown;
            internal object MainStage, MainPosition, FirstSub;
            internal int Action, Cost;
            internal Exception Failure;
        }
        private sealed class MissionIndex
        {
            internal object Collection, Entries, Main, MainStage;
            internal int Count;
            internal bool Ready;
            internal Exception Failure;
            internal float RetryAt;
            internal Dictionary<int, List<MissionLink>> Places = new Dictionary<int, List<MissionLink>>();
            internal Dictionary<string, MissionLink> LogicalIds = new Dictionary<string, MissionLink>(StringComparer.Ordinal);
            internal int MissionCursor, StageCursor;
            internal MissionLink Building;
            internal IList BuildingStages;
        }
        private static readonly Dictionary<object, MissionIndex> missionIndices = new Dictionary<object, MissionIndex>();
        private static readonly List<Place> places = new List<Place>();
        private static readonly Place globalPlace = new Place { Key = "global" };
        private static readonly Dictionary<string, ScriptDefinition> scripts = new Dictionary<string, ScriptDefinition>(StringComparer.Ordinal);
        private static readonly Regex tags = new Regex("<[^>]*>");
        private static int sequence, generation;
        private static bool hasClock;
        private static float lastClock;
        private static double elapsed;
        private static int coldEffects;
        private static bool effectsPreparing;
        private static bool detailed;
        private static int automaticEvents, automaticMissions, automaticProofs;
        private const int AutomaticEventLimit = 16, AutomaticMissionLimit = 16;
        private static readonly Dictionary<object, bool?> currentConditions = new Dictionary<object, bool?>();
        private static DispatchSnapshot dispatch;
        private static string entryBarrier;
        private static WeakReference selection, session;
        private static object observedManager, observedPlayer;
        private static Place selectedPlace;
        private static int selectionVersion;
        private static string selectionId = "";
        private static PositionController Selected
        {
            get
            {
                var value = selection == null ? null : selection.Target as PositionController;
                return value != null && value.isActiveAndEnabled && value.gameObject.activeInHierarchy ? value : null;
            }
        }
        public static bool IsCurrent(InformationRow row) { return row != null && IsEnabled && Selected != null && selectedPlace != null && ReferenceEquals(row.Anchor, Selected) && row.Id == SelectionKey(selectedPlace); }
        private static string SelectionKey(Place place) { return selectionId; }
        public static bool IsCurrent(string id) { return IsEnabled && Selected != null && id == selectionId; }
        public static Func<bool> Enabled;
        private static bool IsEnabled { get { try { return Enabled == null || Enabled(); } catch (Exception) { return false; } } }
        public static string ContextKey { get { return generation.ToString(CultureInfo.InvariantCulture) + "/" + (Session == null ? "" : Session.GetInstanceID().ToString(CultureInfo.InvariantCulture)) + "/" + selectionId; } }
        public static bool Visible { get { return IsEnabled && Selected != null; } }
        private static PositionController Session
        {
            get
            {
                var value = session == null ? null : session.Target as PositionController;
                return value != null && value.isActiveAndEnabled && value.gameObject.activeInHierarchy ? value : null;
            }
        }
        public static bool SessionVisible { get { return IsEnabled && Session != null; } }
        private static PositionController Live(Place place)
        {
            var controller = place.Controller.Target as PositionController;
            return controller != null && controller.isActiveAndEnabled && controller.gameObject.activeInHierarchy ? controller : null;
        }
        public static void Register(PositionController controller)
        {
            if (controller == null) return;
            if (Session == null) { session = new WeakReference(controller); generation++; }
            foreach (Place place in places) if (ReferenceEquals(place.Controller.Target, controller)) return;
            places.Add(new Place { Controller = new WeakReference(controller), Id = checked(++sequence) });
        }
        public static void Select(PositionController controller)
        {
            if (controller == null) return;
            Register(controller);
            if (selection != null && ReferenceEquals(selection.Target, controller)) return;
            selection = new WeakReference(controller); session = new WeakReference(controller);
            foreach (Place place in places) if (ReferenceEquals(place.Controller.Target, controller)) { selectedPlace = place; break; }
            selectionVersion++; selectionId = "map/" + selectedPlace.Id + "/" + selectionVersion; generation++;
        }
        public static void Deselect(PositionController controller)
        {
            if (selection == null || !ReferenceEquals(selection.Target, controller)) return;
            selection = null; selectedPlace = null; selectionId = ""; selectionVersion++; generation++;
        }
        public static void Clear()
        { places.Clear(); globalPlace.Reads.Clear(); scripts.Clear(); missionIndices.Clear(); selection = session = null; observedManager = observedPlayer = null; selectedPlace = null; selectionId = ""; selectionVersion++; hasClock = false; elapsed = 0; generation++; }
        public static void ClearSnapshots()
        {
            // Used for OFF: retain weak lifecycle handles, never parsed data.
            bool changed = scripts.Count != 0 || missionIndices.Count != 0;
            foreach (Place place in places)
            {
                changed |= place.Definition != null || place.Data != null || place.PositionConfig != null || place.Reads.Count != 0;
                place.Definition = null; place.Reads.Clear(); place.Data = place.PositionConfig = null;
            }
            globalPlace.Reads.Clear(); scripts.Clear(); missionIndices.Clear(); observedManager = observedPlayer = null; if (changed) generation++;
        }
        public static List<DisplayQuery> Discover(bool guide, bool oneShot, bool details = false)
        {
            var result = new List<DisplayQuery>();
            if (!IsEnabled || !guide && !oneShot) return result;
            PositionController controller = Selected ?? Session;
            Place captured = selectedPlace;
            if (controller == null || Selected == null && !guide) return result;
            int version = selectionVersion;
            string context = ContextKey;
            result.Add(new DisplayQuery(Selected == null ? "map/global/" + context : SelectionKey(captured), () => {
                if (version != selectionVersion || !ReferenceEquals(Selected ?? Session, controller)) throw new DataNotReadyException();
                detailed = details;
                return Selected == null ? ReadGlobal(controller, guide) : ReadCurrentPlace(captured, controller, guide, oneShot);
            }));
            return result;
        }
        private static object ReadCurrentPlace(Place place, PositionController controller, bool guide, bool oneShot)
        {
            if (Live(place) != controller) throw new DataNotReadyException();
            coldEffects = 1; effectsPreparing = false; dispatch = null; entryBarrier = null;
            automaticEvents = AutomaticEventLimit; automaticMissions = AutomaticMissionLimit;
            automaticProofs = 4;
            currentConditions.Clear();
            ObserveSessionData(controller);
            try
            {
                object data = Read(controller, "_positionData");
                if (data == null) throw new DataNotReadyException();
                object checkpoints = ExistingCheckpoints();
                object config = checkpoints == null ? null : Read(checkpoints, "_position");
                string script = (string)Read(data, "_defaultScript");
                string key = (string)Read(data, "name");
                if (!ReferenceEquals(data, place.Data) || !ReferenceEquals(config, place.PositionConfig) || script != place.Script || key != place.Key)
                {
                    place.Data = data; place.PositionConfig = config; place.Script = script; place.Key = key;
                    place.Definition = null; place.Reads.Clear(); generation++;
                }
            }
            catch (Exception)
            {
                // Metadata belongs to this place's independently scheduled
                // query. A broken/null source cannot fail map discovery or
                // retain another target's last-good snapshot as current.
                if (place.Data != null || place.Definition != null)
                {
                    place.Data = place.PositionConfig = null; place.Definition = null;
                    place.Reads.Clear(); generation++;
                }
                throw;
            }
            return ReadPlace(place, controller, guide, oneShot);
        }
        private static object ExistingCheckpoints()
        {
            // The supplied CheckPointManager has an auto-property assigned by
            // Awake. Read only its existing backing field, never a getter or
            // any lazy singleton/object discovery path.
            for (Type type = typeof(CheckPointManager); type != null; type = type.BaseType)
                foreach (string name in new[] { "<Instance>k__BackingField", "_instance", "Instance" })
                {
                    FieldInfo field = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (field != null) return field.GetValue(null);
                }
            throw new MissingFieldException(typeof(CheckPointManager).FullName, "existing instance field");
        }
        private static object Read(object target, string name) { return LogBridge.ReadFieldStrict(target, name); }
        private static string Plain(string value) { return tags.Replace(DisplayNames.Text(value ?? ""), "").Trim(); }
        private static IList List(object target, string field)
        { IList value = Read(target, field) as IList; if (value == null) throw new DataNotReadyException(); return value; }
        private static string Name(object value) { return (string)Read(value, "name"); }
        private static float Now
        {
            get
            {
                float raw = Time.unscaledTime;
                if (!float.IsNaN(raw) && !float.IsInfinity(raw))
                {
                    if (hasClock && raw > lastClock) elapsed += (double)raw - lastClock;
                    lastClock = raw; hasClock = true;
                }
                return (float)elapsed;
            }
        }
        private static IList<MapRouteEvent> ReadPool(Place place, string key)
        {
            if (place.PositionConfig == null) return null;
            IList pools = List(place.PositionConfig, "_list");
            // Native PositionResultConfig.GetResult uses the first exact name.
            // Locate that same pool without materializing unrelated definitions.
            for (int at = 0; at < pools.Count && at < 1024; at++)
            {
                object pool = pools[at];
                if (pool == null || Name(pool) != key) continue;
                place.Definition.PoolDiagnostics.Remove(key);
                string prefix = (string)Read(pool, "_scriptPrefix");
                IList entries = List(pool, "_list");
                var result = new List<MapRouteEvent>();
                int count = Math.Min(entries.Count, 256);
                for (int ordinal = 0; ordinal < count; ordinal++)
                {
                    var data = entries[ordinal] as PositionEventData;
                    if (data == null) throw new InvalidOperationException("Unsupported map event definition.");
                    result.Add(new MapRouteEvent(data, prefix + "_" + data.ResultType, key + "/" + ordinal));
                }
                // The route engine sees a 257th slot, reaches its own item
                // limit before dereferencing it, and publishes partial status.
                if (entries.Count > count) result.Add(null);
                return result;
            }
            if (pools.Count > 1024)
            {
                place.Definition.PoolDiagnostics[key] = "풀 검색 상한 · 1,024개 안에서 일치 항목을 찾지 못했습니다. 이후 자료는 확인하지 않았습니다.";
                throw new InvalidOperationException("Map pool search limit reached.");
            }
            throw new DataNotReadyException();
        }
        private static Source GetSource(Place place)
        {
            Source source = place.Definition;
            if (source == null) place.Definition = source = new Source { Script = place.Script };
            if (source.Failure != null && Now < source.RetryAt) return source;
            try
            {
                if (source.Routes == null && !string.IsNullOrEmpty(source.Script))
                    source.Routes = new MapRouteDefinitions(source.Script, DataHelper.ReadPreviewScript, key => ReadPool(place, key));
                if (source.Routes != null)
                {
                    source.Routes.Advance(Now);
                    var current = new MapRouteRead();
                    foreach (MapRouteEntry route in source.Routes.Entries)
                    {
                        EventDefinition entry;
                        if (!source.RouteEvents.TryGetValue(route, out entry))
                        {
                            var data = route.Event as PositionEventData;
                            if (data == null) throw new InvalidOperationException("Unsupported map route event.");
                            entry = new EventDefinition { Data = data, Script = route.ResultScript, Id = route.Id, HasChoice = route.HasChoice };
                            source.RouteEvents.Add(route, entry); source.Events.Add(entry);
                        }
                        entry.Path = route.Path; entry.HasChoice = route.HasChoice; entry.RouteState = route.ReadCurrent(current);
                    }
                }
                source.Ready = source.Routes == null || !source.Routes.IsPreparing;
                if (source.Failure != null) DicePlugin.LogWarning("Information map source=" + place.Key + " recovered");
                source.Failure = null;
            }
            catch (Exception error)
            {
                if (source.Failure == null || source.Failure.GetType() != error.GetType())
                    DicePlugin.LogWarning("Information map source=" + place.Key + " failure type=" + error.GetType().FullName);
                source.Failure = error; source.RetryAt = Now + 5;
            }
            return source;
        }
        private static ScriptDefinition Effects(string script, bool describe = true)
        {
            if (string.IsNullOrEmpty(script)) return new ScriptDefinition { Description = "연결된 결과 스크립트 없음" };
            ScriptDefinition definition;
            if (!scripts.TryGetValue(script, out definition)) { definition = new ScriptDefinition(); scripts.Add(script, definition); }
            if (definition.Actions == null)
            {
                if (coldEffects <= 0)
                {
                    definition.Preparing = true; effectsPreparing = true;
                    definition.Description = "효과·1회성 준비 중"; return definition;
                }
                coldEffects--; definition.Actions = PositionEventCache.GetDefinitionParser().ExtractActionsFromLua(script);
                definition.Preparing = false; definition.Description = null;
            }
            if (!describe) return definition;
            if (definition.Description != null && (definition.RetryAt <= 0 || Now < definition.RetryAt)) return definition;
            LuaParser.RefreshPreviewReads(definition.Actions);
            int pending = DisplayNames.PendingNames;
            try { definition.Description = LuaParser.Description(definition.Actions); definition.RetryAt = DisplayNames.PendingNames != pending ? Now + 5 : 0; }
            catch (PreviewReadException error)
            {
                definition.Description = (error.IsWaiting ? "게임 데이터 대기 중" : "읽기 실패") + "\n" + error.PartialDescription;
                definition.RetryAt = Now + 5;
            }
            return definition;
        }
        private static Candidate TryCandidate(Place place, object key, Func<Candidate> read)
        {
            CandidateRead state;
            if (!place.Reads.TryGetValue(key, out state)) { state = new CandidateRead(); place.Reads.Add(key, state); }
            if (state.Failure != null && Now < state.RetryAt) return FailedCandidate(state);
            try
            {
                state.Last = read();
                if (state.Failure != null) DicePlugin.LogWarning("Information map target=" + place.Key + " recovered");
                state.Failure = null; return state.Last;
            }
            catch (Exception error)
            {
                if (state.Failure == null || state.Failure.GetType() != error.GetType())
                    DicePlugin.LogWarning("Information map target=" + place.Key + " failure type=" + error.GetType().FullName);
                state.Failure = error; state.RetryAt = Now + 5; return FailedCandidate(state);
            }
        }
        private static Candidate FailedCandidate(CandidateRead state)
        {
            bool waiting = state.Failure is DataNotReadyException;
            Candidate prior = state.Last;
            return new Candidate { Id = prior == null ? null : prior.Id, Name = prior == null ? "후보 자료" : prior.Name, Kind = prior == null ? "확인 불가" : prior.Kind,
                Rank = 2, StatusOnly = true, Global = prior != null && prior.Global,
                Status = waiting ? "게임 데이터 대기 중" : prior == null ? "읽기 실패" : "갱신 실패 · 이전 값",
                Detail = prior == null ? "후보의 조건·결과를 읽지 못했습니다." : prior.Detail };
        }
        private static void ObserveSessionData(PositionController controller)
        {
            object manager = Read(controller, "_missionsManager"), player = Read(controller, "_gameStatManager");
            if (ReferenceEquals(manager, observedManager) && ReferenceEquals(player, observedPlayer)) return;
            globalPlace.Reads.Clear(); scripts.Clear(); missionIndices.Clear();
            foreach (Place value in places) { value.Reads.Clear(); value.Definition = null; }
            OneShotProof.Clear(); observedManager = manager; observedPlayer = player; generation++;
        }
        private static MapGuideSnapshot ReadGlobal(PositionController controller, bool guide)
        {
            coldEffects = 1; effectsPreparing = false; dispatch = null; entryBarrier = null;
            automaticEvents = AutomaticEventLimit; automaticMissions = AutomaticMissionLimit;
            automaticProofs = 4;
            currentConditions.Clear();
            ObserveSessionData(controller);
            var candidates = new List<Candidate>();
            if (guide) CollectMissions(null, controller, candidates, true);
            return Snapshot(null, controller, candidates, guide, false);
        }
        private static object ReadPlace(Place place, PositionController controller, bool guide, bool oneShot)
        {
            if (!IsEnabled || Live(place) != controller || !ReferenceEquals(Read(controller, "_positionData"), place.Data)) throw new DataNotReadyException();
            Source source = GetSource(place);
            var candidates = new List<Candidate>();
            foreach (EventDefinition entry in source.Events)
            {
                EventDefinition captured = entry;
                candidates.Add(TryCandidate(place, "event/" + RuntimeHelpers.GetHashCode(entry.Data) + "/" + entry.Script, () => Event(controller, source, captured, guide, oneShot)));
            }
            if (guide && source.Ready && source.Events.Count == 0 && !string.IsNullOrEmpty(source.Script))
                candidates.Add(TryCandidate(place, source, () => Story(controller, source.Script,
                    source.Failure != null || source.Routes != null && (source.Routes.IsPartial || source.Routes.HasReadFailure))));
            if (guide) CollectMissions(place, controller, candidates, false);
            if (source.Failure != null)
                candidates.Add(Diagnostic("이야기 자료", source.Failure is DataNotReadyException ? "게임 데이터 대기 중" : "읽기 실패", "이 장소의 연결된 이야기 자료를 읽지 못했습니다."));
            else if (source.Routes != null && (source.Routes.IsPreparing || source.Routes.IsPartial || source.Routes.HasReadFailure))
                candidates.Add(Diagnostic("경로 자료", source.Routes.IsPreparing ? "경로 준비 중" : source.Routes.HasReadFailure ? "경로 읽기 실패" : "경로 일부 확인 불가", source.Routes.Diagnostic));
            foreach (string diagnostic in source.PoolDiagnostics.Values)
                candidates.Add(Diagnostic("풀 자료", "풀 검색 상한 · 일부 확인 불가", diagnostic));
            return Snapshot(place, controller, candidates, guide, oneShot);
        }
        private static Candidate Diagnostic(string name, string status, string detail)
        { return new Candidate { Id = "diagnostic/" + name, Name = name, Kind = "상태", StatusOnly = true, Rank = 2, Status = status, Detail = detail }; }
        private static MapGuideSnapshot Snapshot(Place place, PositionController controller, List<Candidate> candidates, bool guide, bool once)
        {
            var snapshot = new MapGuideSnapshot { HasSelection = place != null, ContextKey = ContextKey };
            if (effectsPreparing) candidates.Add(Diagnostic("효과·1회성", "효과·1회성 준비 중", "새 결과 스크립트는 한 번의 예정된 조회에 하나씩 준비합니다. 아직 분석하지 않은 결과를 효과 없음이나 반복 가능으로 판단하지 않습니다."));
            var ordered = new List<Candidate>();
            // Detail keeps every recognized item; default counts omit ordinary activity.
            for (int group = 0; group < 6; group++)
                for (int rank = 0; rank < 3; rank++)
                    foreach (Candidate value in candidates)
                    {
                        int actual = value.StatusOnly ? 5 : value.Other ? 4 : value.Global ? 1 + value.Priority : 0;
                        if (actual == group && value.Rank == rank) ordered.Add(value);
                    }
            int waiting = 0, stages = 0, oneShots = 0; bool available = false; var automatic = new List<string>(); var diagnostics = new List<string>();
            int ordinal = 0;
            foreach (Candidate value in ordered)
            {
                string group = value.StatusOnly ? "자료 상태" : value.Other ? "기타 연결 정보" : value.Global ? "장소 무관" : "선택 장소";
                string title = Plain(value.Name);
                string status = value.Status + (value.OneShot ? " · 1회성" : "");
                snapshot.Entries.Add(new InformationRow((place == null ? "map/global/" + ContextKey : SelectionKey(place)) + "/" + (value.Id ?? "entry/" + ordinal++),
                    title, group + " · " + value.Kind, status, value.Detail ?? "", title + " · " + value.Kind + " · " + status, controller, value.Rank == 0));
                if (value.StatusOnly) { if (!diagnostics.Contains(value.Status)) diagnostics.Add(value.Status); continue; }
                if (value.Global || value.Other) continue;
                if (value.Rank == 0 && (value.Waiting || value.Mission || value.OneShot)) available = true;
                if (value.Waiting) waiting++;
                else if (value.Mission && value.Rank == 0) stages++;
                else if (value.OneShot && value.Rank == 0) oneShots++;
                if (guide && (value.Waiting || value.Mission) || value.OneShot)
                    automatic.Add(MapGuidePresentation.Notice(title, value.Mission ? (value.Main ? "주 임무" : "보조 임무") : "1회성 이야기", value.Rank, value.Waiting, value.Mission, value.Status));
            }
            if (snapshot.Entries.Count == 0)
                snapshot.Entries.Add(new InformationRow("map/empty/" + ContextKey, place == null ? "장소 무관 임무 없음" : "현재 연결 항목 없음", "지도", "현재 확인된 항목 없음",
                    "현재 관찰한 자료 범위의 결과입니다. 전체 게임의 기회 없음이나 반복 가능을 뜻하지 않습니다."));
            if (place != null)
            {
                string title = LocationTranslations.TranslateLocation((place.Key ?? "").Replace("養成_", ""));
                string status = "대기 임무 " + waiting + " · 단계 후보 " + stages + " · 확인된 1회성 " + oneShots;
                if (diagnostics.Count != 0) status += " · " + string.Join(" · ", diagnostics);
                string text = MapGuidePresentation.Summary(automatic, guide, diagnostics.Count > 0);
                snapshot.Summary = new InformationRow(SelectionKey(place), Plain(title), "지도", status,
                    "대기 상태·현재 조건·선택 우선순위는 서로 다른 정보입니다. 경로·조건·효과와 장소 무관 항목은 지도 로그에서 읽습니다.", text, controller, available);
            }
            return snapshot;
        }
        private static Candidate Event(PositionController controller, Source source, EventDefinition entry, bool guide, bool oneShot)
        {
            OneShotResult proof = OneShotProof.Unknown("전체 결과와 조건은 지도 로그 상세에서 조회합니다.");
            if (oneShot)
            {
                if (!OneShotProof.TryGetEvent(entry.Data, entry.Script, out proof))
                {
                    bool small = automaticProofs > 0 && source.Routes != null && source.Routes.CanBuildBoundedProof(entry.Script) &&
                        (!(bool)Read(entry.Data, "_toggleCondition") || SmallCurrentGraph(Read(entry.Data, "_activeCondition")));
                    if (detailed || small)
                    {
                        if (!detailed) automaticProofs--;
                        ScriptDefinition prepared = Effects(entry.Script, false);
                        proof = prepared.Preparing ? OneShotProof.Unknown("결과 분석 준비 중입니다.") : OneShotProof.ForEvent(entry.Data, entry.Script);
                    }
                    else proof = OneShotProof.Unknown("자동 표시에서 긴 결과/후속 분석을 실행하지 않습니다. 지도 로그 상세에서 전체 증명을 조회합니다.");
                }
                if (proof.Failed) throw new InvalidOperationException("Map one-shot read failed.");
            }
            // Ordinary/unproved rows are retained for the log, which requests
            // a fresh detailed snapshot on entry. Passive rendering needs no
            // recursive condition tree or full formatter for those hidden rows.
            bool readState = detailed || proof.Proven && entry.RouteState == MapRouteState.Met && automaticEvents-- > 0 && SmallEventGraph(entry.Data);
            EventEligibilitySnapshot state = readState ? (detailed ? EventEligibility.Evaluate(entry.Data) : EventEligibility.EvaluateCurrent(entry.Data)) : null;
            if (state != null && state.Reason == EventEligibilityReason.ReadFailure) throw new InvalidOperationException("Map event read failed.");
            if (entry.Name == null) entry.Name = Plain(DataHelper.GetEventName(entry.Data));
            var result = new Candidate { Id = "event/" + RuntimeHelpers.GetHashCode(entry.Data) + "/" + entry.Script, Name = entry.Name, Kind = "이야기", Other = true,
                Rank = state == null ? 2 : state.IsEligible ? 0 : state.State == EventEligibilityState.Ineligible ? 1 : 2,
                Status = state == null ? "현재 상태는 상세 조회에서 확인" : state.IsEligible ? "진행 가능" : state.State == EventEligibilityState.Ineligible ? "진행 불가" : "확인 불가",
                Detail = state == null ? "현재 조건·효과는 지도 로그 상세를 열면 조회합니다." : state.Detail };
            if (entry.RouteState == MapRouteState.NotMet) { result.Rank = 1; result.Status = "진행 불가 · 경로 조건 미충족"; }
            else if (entry.RouteState == MapRouteState.Unknown) { result.Rank = 2; result.Status = "경로 확인 필요"; }
            else if (entry.HasChoice && result.Rank == 0) result.Status = "선택 후 기회";
            if (detailed) result.Detail = "경로: " + entry.Path + "\n현재 이벤트 판정: " + (state == null ? "상세 조회 대기" : state.IsEligible ? "조건·가중치 충족" : state.State == EventEligibilityState.Ineligible ? "조건·가중치 미충족" : "확인 불가") + "\n" + result.Detail;
            if (detailed && entry.RouteState == MapRouteState.Unknown)
                result.Detail += "\n선행 호출·상태 변경 뒤의 경로가 확정되지 않아 현재 이벤트 조건만으로 도달 시점의 진행 불가를 단정하지 않습니다.";
            ApplyActivityEntry(controller, result);
            ApplyPlace(controller, result);
            if (detailed && guide) result.Detail += "\n결과: " + Effects(entry.Script).Description;
            if (oneShot)
            {
                result.OneShot = proof.Proven; result.Other = !proof.Proven;
                if (detailed) result.Detail += "\n" + proof.Status + ": " + proof.Detail;
            }
            return result;
        }
        private static Candidate Story(PositionController controller, string script, bool pathUnknown)
        {
            var result = new Candidate { Id = "activity/" + script, Name = script, Kind = "일반 활동", Other = true,
                Rank = pathUnknown ? 2 : 0, Status = pathUnknown ? "기본 활동 · 내부 경로 확인 필요" : "진행 가능", Detail = "현재 장소의 고정 실행 스크립트입니다. 내부의 미지원·동적 경로는 발생을 보장하지 않습니다." + (detailed ? "\n결과: " + Effects(script).Description : "\n전체 효과는 지도 로그 상세에서 조회합니다.") };
            ApplyActivityEntry(controller, result); ApplyPlace(controller, result); return result;
        }
        private static void ApplyActivityEntry(PositionController controller, Candidate result)
        {
            if (entryBarrier == null)
            {
                // Native normal activity writes these condition flags before
                // entering the default Lua script, including false Setup arms.
                // Inspect only the array boundary; never inspect/execute its
                // condition, counter, RNG, Setup or Execute chain.
                entryBarrier = "";
                try
                {
                    object data = Read(controller, "_positionData");
                    Array flags = Read(data, "_conditionFlags") as Array;
                    if (flags == null || flags.Rank != 1 || flags.GetType().GetElementType().FullName != "Mortal.Free.PositionConditionFlagItem")
                        entryBarrier = "진입 전 변경 여부 확인 불가";
                    else if (flags.Length != 0) entryBarrier = "진입 시 상태 변경 가능";
                }
                catch (Exception) { entryBarrier = "진입 전 변경 여부 확인 불가"; }
            }
            if (entryBarrier.Length == 0) return;
            string current = result.Status;
            result.Rank = 2; result.Status = "경로 확인 필요 · " + entryBarrier;
            if (detailed) result.Detail += "\n현재 시점 판정: " + current + "\n" + entryBarrier + ": 일반 활동 진입 전에 조건 플래그 처리가 있어 현재 조건을실행 시점 조건으로 단정하지 않습니다. 읽기만 수행했고 해당 처리나 조건·난수·카운터를 실행하지 않았습니다.";
        }
        private static MissionIndex GetMissionIndex(object manager, string location)
        {
            MissionIndex index;
            if (!missionIndices.TryGetValue(manager, out index))
            { index = new MissionIndex(); missionIndices.Add(manager, index); }
            if (index.Failure != null && Now < index.RetryAt) return index;
            try
            {
                object collection = Read(manager, "_subMissions");
                IList entries = List(collection, "_list");
                object main = Read(manager, "_main"), mainStage = Read(manager, "MainCheckData");
                bool changed = !ReferenceEquals(index.Collection, collection) || !ReferenceEquals(index.Entries, entries) ||
                    index.Count != entries.Count || !ReferenceEquals(index.Main, main) || !ReferenceEquals(index.MainStage, mainStage);
                if (changed || index.Failure != null)
                {
                    index.Collection = collection; index.Entries = entries; index.Count = entries.Count;
                    index.Main = main; index.MainStage = mainStage; index.Places.Clear(); index.LogicalIds.Clear();
                    index.MissionCursor = index.StageCursor = 0; index.Building = null; index.BuildingStages = null; index.Ready = false;
                }
                // Preparation is resumable, including mission/stage indexing.
                // Ordinary refresh never rescans unrelated definitions.
                int budget = 64;
                while (!index.Ready && budget-- > 0)
                {
                    if (index.Building == null)
                    {
                        if (index.MissionCursor > entries.Count) { index.Ready = true; break; }
                        bool isMain = index.MissionCursor == 0;
                        object mission = isMain ? main : entries[index.MissionCursor - 1];
                        index.MissionCursor++;
                        if (mission == null) continue;
                        string logical = (string)Read(mission, "_name");
                        if (string.IsNullOrEmpty(logical)) logical = Name(mission) ?? (isMain ? "main" : "mission/" + RuntimeHelpers.GetHashCode(mission));
                        index.Building = new MissionLink { Mission = mission, Main = isMain, LogicalId = logical };
                        index.BuildingStages = List(mission, "_missions"); index.StageCursor = 0;
                        if (!index.LogicalIds.ContainsKey(logical)) index.LogicalIds.Add(logical, index.Building);
                        if (isMain && mainStage != null) IndexStage(index, index.Building, mainStage, false);
                    }
                    if (index.StageCursor < index.BuildingStages.Count)
                    {
                        object stage = index.BuildingStages[index.StageCursor++];
                        if (stage != null) IndexStage(index, index.Building, stage, true);
                    }
                    else { index.Building = null; index.BuildingStages = null; }
                }
                if (index.Failure != null) DicePlugin.LogWarning("Information map mission-index target=" + location + " recovered");
                index.Failure = null;
            }
            catch (Exception error)
            {
                if (index.Failure == null || index.Failure.GetType() != error.GetType())
                    DicePlugin.LogWarning("Information map mission-index target=" + location + " failure type=" + error.GetType().FullName);
                index.Ready = false; index.Places.Clear(); index.LogicalIds.Clear(); index.Failure = error; index.RetryAt = Now + 5;
            }
            return index;
        }
        private static void IndexStage(MissionIndex index, MissionLink link, object stage, bool keyed)
        {
            if (keyed)
            {
                string key = (string)Read(stage, "_key");
                if (!string.IsNullOrEmpty(key) && !link.Stages.ContainsKey(key)) link.Stages.Add(key, stage);
            }
            int location = Convert.ToInt32(Read(stage, "_position"));
            List<MissionLink> related;
            if (!index.Places.TryGetValue(location, out related)) { related = new List<MissionLink>(); index.Places.Add(location, related); }
            if (!related.Contains(link)) related.Add(link);
        }
        private static void CollectMissions(Place selected, PositionController controller, List<Candidate> target, bool globalOnly)
        {
            Place cache = selected ?? globalPlace;
            object manager = Read(controller, "_missionsManager");
            if (manager == null) { target.Add(Diagnostic("임무 자료", "게임 데이터 대기 중", "현재 지도에서 관찰한 임무 관리자를 기다립니다.")); return; }
            MissionIndex index = GetMissionIndex(manager, selected == null ? "global" : selected.Key);
            if (index.Failure != null)
                target.Add(Diagnostic("임무 정의", index.Failure is DataNotReadyException ? "게임 데이터 대기 중" : "읽기 실패", "임무 정의를 준비하지 못했습니다. 기존 대기 목록은 별도로 읽습니다."));
            else if (!index.Ready) target.Add(Diagnostic("임무 정의", "임무 정의 준비 중", "단계·장소 색인을 나누어 준비 중입니다. 아직 읽지 않은 임무가 있을 수 있습니다."));
            int selectedPosition = globalOnly ? -1 : Convert.ToInt32(Read(controller, "_position"));
            var staged = new HashSet<object>();
            object main = Read(manager, "_main"), mainStage = Read(manager, "MainCheckData");
            bool mainWaiting = (bool)Read(manager, "MainMissionStart");
            if (mainWaiting && mainStage != null && TryRelated(cache, mainStage, selectedPosition, target))
            {
                staged.Add(mainStage);
                string mainId = main == null ? "main" : (string)Read(main, "_name");
                if (string.IsNullOrEmpty(mainId)) mainId = main == null ? "main" : Name(main) ?? "main";
                AddMission(cache, controller, manager, main, mainStage, mainId, true, true, target);
            }
            var active = Read(manager, "ActiveSubMissions") as IDictionary;
            if (active == null) target.Add(Diagnostic("대기 임무", "게임 데이터 대기 중", "현재 대기 사전이 아직 준비되지 않았습니다. 조건 충족과 게임의 대기 등록은 구분합니다."));
            else
            {
                int budget = 256;
                foreach (DictionaryEntry entry in active)
                {
                    if (budget-- == 0) { target.Add(Diagnostic("대기 임무", "대기 목록 일부 확인 불가", "한 번의 조회에서 읽는 기존 대기 항목 한도에 도달했습니다.")); break; }
                    object stage = entry.Value;
                    if (stage == null || !TryRelated(cache, stage, selectedPosition, target) || !staged.Add(stage)) continue;
                    string id = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
                    MissionLink owner; index.LogicalIds.TryGetValue(id, out owner);
                    AddMission(cache, controller, manager, owner == null ? null : owner.Mission, stage, id, false, true, target);
                }
            }
            if (index.Failure != null) return;
            var links = new List<MissionLink>();
            var membership = new HashSet<MissionLink>();
            foreach (int location in globalOnly ? new[] { 0 } : new[] { selectedPosition, 0 })
            {
                List<MissionLink> values;
                if (index.Places.TryGetValue(location, out values)) foreach (MissionLink link in values) if (membership.Add(link)) links.Add(link);
            }
            foreach (MissionLink link in links)
            {
                string key;
                try { key = (string)Read(link.Mission, "_currentKey"); }
                catch (Exception) { target.Add(Diagnostic("현재 임무 단계", "읽기 실패", "관련 임무의 현재 키를 읽지 못했습니다.")); continue; }
                if (string.IsNullOrWhiteSpace(key) || key == "clear") continue;
                object stage;
                if (!link.Stages.TryGetValue(key, out stage))
                {
                    if (index.Ready) target.Add(Diagnostic("현재 임무 단계", "게임 데이터 대기 중", "현재 키에 맞는 임무 단계를 찾지 못했습니다."));
                    continue;
                }
                if (!TryRelated(cache, stage, selectedPosition, target) || staged.Contains(stage)) continue;
                AddMission(cache, controller, manager, link.Mission, stage, link.LogicalId, link.Main, false, target);
            }
        }
        private static bool TryRelated(Place cache, object stage, int selectedPosition, List<Candidate> target)
        {
            Candidate read = TryCandidate(cache, "location/" + RuntimeHelpers.GetHashCode(stage), () => {
                int location = Convert.ToInt32(Read(stage, "_position"));
                return new Candidate { Stage = location == 0 || location == selectedPosition ? stage : null };
            });
            if (read.StatusOnly) { target.Add(read); return false; }
            return read.Stage != null;
        }
        private static void AddMission(Place place, PositionController controller, object manager, object mission, object stage,
            string logicalId, bool main, bool waiting, List<Candidate> target)
        {
            string revision = "mission/" + RuntimeHelpers.GetHashCode(manager) + "/" + logicalId + "/" + RuntimeHelpers.GetHashCode(stage) + "/" + waiting;
            Candidate candidate = TryCandidate(place, revision, () => Mission(controller, manager, mission, stage, logicalId, main, waiting));
            if (candidate != null) target.Add(candidate);
        }
        private static Candidate Mission(PositionController controller, object manager, object mission, object stage, string logicalId, bool main, bool waiting)
        {
            string script = (string)Read(stage, "_startScript");
            if (string.IsNullOrEmpty(script)) return null;
            string raw = mission == null ? logicalId : Name(mission), stageName = Name(stage);
            string key = (string)Read(stage, "_key");
            bool global = Convert.ToInt32(Read(stage, "_position")) == 0;
            int priority = global ? main ? 1 : (bool)Read(stage, "_beforeMainMission") ? 0 : 2 : 0;
            var candidate = new Candidate { Id = "mission/" + logicalId + "/" + key + "/" + RuntimeHelpers.GetHashCode(stage),
                Name = DisplayNames.LookupName("Mission/Name/" + raw, raw) + " · " + DisplayNames.LookupName("Mission/CheckName/" + stageName, stageName),
                Kind = main ? "주 임무" : "임무", Mission = true, Main = main, Waiting = waiting, Stage = stage, Global = global, Priority = priority,
                Rank = 0, Status = waiting ? "기존 대기" : "조건 충족 · 대기 미확인" };
            var detail = new StringBuilder();
            detail.AppendLine(waiting ? "게임의 현재 대기 자료에 있는 단계입니다. 명시적 예약도 이 자료를 바꿀 수 있으며 실제 실행 완료를 뜻하지 않습니다." : "현재 키의 단계입니다. 아래 조건이 충족되어도 게임의 대기 등록은 확인되지 않았습니다.");
            bool inspect = detailed || !waiting && automaticMissions-- > 0;
            bool denied = inspect && !(bool)Read(stage, "_active"), unknown = !inspect && !waiting;
            if (!inspect) detail.AppendLine("현재 조건은 지도 로그 상세에서 조회합니다. 대기 사실과 현재 조건 충족은 별개입니다.");
            if (denied) detail.AppendLine("현재 조건: 비활성");
            if (inspect) foreach (object condition in List(stage, "_conditions"))
            {
                if (detailed) detail.AppendLine(PopupConditionText.Condition(condition));
                bool? result = CurrentCondition(condition);
                if (result == false) denied = true;
                if (!result.HasValue) unknown = true;
            }
            if (inspect && (bool)Read(stage, "_dayCheck"))
            {
                object player = Read(controller, "_gameStatManager");
                object current = Read(Read(player, "_gameTimeData"), "GameTime");
                int type = Convert.ToInt32(Read(stage, "_timeCheckType"), CultureInfo.InvariantCulture);
                bool period = type == 1 ? CompareTime(current, Read(stage, "_specificTime")) == 0 : type == 2 ? CompareTime(current, Read(stage, "_minTime")) >= 0 && CompareTime(current, Read(stage, "_maxTime")) <= 0 : true;
                detail.AppendLine("기간: " + (type == 1 ? TimeText(Read(stage, "_specificTime")) : type == 2 ? TimeText(Read(stage, "_minTime")) + " ~ " + TimeText(Read(stage, "_maxTime")) : type == 3 ? "주기형 · 제공 게임 판정은 월 단계를 검사하지 않음" : "확인 불가 · 미지원 기간 설정"));
                if (type < 1 || type > 3) unknown = true;
                if (!period) { denied = true; detail.AppendLine("현재 조건: 기간 밖"); }
            }
            detail.AppendLine("현재 조건: " + (denied ? "미충족" : unknown ? "확인 불가 · 난수/미지원" : "충족"));
            if (!waiting)
            {
                if (denied) { candidate.Rank = 1; candidate.Status = "조건 미충족 · 대기 미확인"; }
                else if (unknown) { candidate.Rank = 2; candidate.Status = "조건 확인 불가 · 대기 미확인"; }
            }
            else if (denied || unknown) candidate.Status += " · 현재 조건 " + (denied ? "미충족" : "확인 불가");
            if (global) detail.AppendLine("장소 무관 경로: " + (priority == 1 ? "주 임무" : priority == 0 ? "주 임무 전" : "주 임무 후") + " · 선택 장소의 활성·행동력 제한과 별개이며 호출 시점·다른 대기 항목의 우선순위에 따라 실행됩니다.");
            candidate.Detail = detail.ToString();
            if (!global) ApplyPlace(controller, candidate);
            if (detailed) candidate.Detail += "\n결과: " + Effects(script).Description;
            return candidate;
        }
        private static bool? CurrentCondition(object condition)
        {
            bool? result;
            if (currentConditions.TryGetValue(condition, out result)) return result;
            if (!detailed && !SmallCurrentGraph(condition)) { currentConditions[condition] = null; return null; }
            try { result = SafeDisplayEvaluation.CheckCondition(condition); }
            catch (RandomDisplayValueException) { result = null; }
            catch (UnsupportedDisplayStatException) { result = null; }
            catch (InvalidOperationException error)
            { if (error.Message.StartsWith("Unknown display", StringComparison.Ordinal)) result = null; else throw; }
            currentConditions[condition] = result; return result;
        }
        private static bool SmallEventGraph(object data)
        {
            var roots = new List<object>();
            if ((bool)Read(data, "_toggleCondition")) roots.Add(Read(data, "_activeCondition"));
            IList rates = List(data, "_eventRateItem");
            if (rates.Count > 16) return false;
            foreach (object rate in rates) roots.Add(Read(rate, "Condition"));
            return SmallCurrentGraph(roots);
        }
        private static bool SmallCurrentGraph(object root)
        { return SmallCurrentGraph(new List<object> { root }); }
        private static bool SmallCurrentGraph(List<object> pending)
        {
            // Count occurrences, not just distinct objects: a shared DAG can
            // expand exponentially in the original recursive evaluator. This
            // cheap field-only preflight bounds a passive current-state read;
            // explicit detail retains the original complete interpretation.
            int at = 0;
            while (at < pending.Count)
            {
                if (at >= 128 || pending.Count > 128) return false;
                object value = pending[at++];
                if (value == null) throw new DataNotReadyException();
                string type = value.GetType().FullName;
                if (type == "Mortal.Core.ConditionResultItem" || type == "Mortal.Core.ConditionResultData")
                { if (!AppendSmall(pending, List(value, "_items"))) return false; }
                else if (type == "Mortal.Core.StatCompareItem")
                { pending.Add(Read(value, "_value1")); pending.Add(Read(value, "_value2")); }
                else if (type == "Mortal.Core.StatGroupVariable")
                { if (!AppendSmall(pending, List(value, "_value"))) return false; }
                else if (type == "Mortal.Core.StatValueReference")
                {
                    int kind = Convert.ToInt32(Read(value, "_checkType"));
                    if (kind == 1) pending.Add(Read(value, "_stat"));
                    else if (kind == 8) pending.Add(Read(value, "_statGroup"));
                    else if (kind == 16) pending.Add(Read(value, "_condition"));
                    else if (kind == 14 && List(Read(value, "_bookCollection"), "List").Count > 32) return false;
                }
                else if (type == "Mortal.Core.GameStat")
                {
                    object conversions = Read(value, "_statAdditionData");
                    if (conversions != null)
                    {
                        IList collections = conversions as IList;
                        if (collections == null || !AppendSmall(pending, collections)) return false;
                    }
                }
                else if (type == "Mortal.Core.StatConvertCollectionData")
                { if (!AppendSmall(pending, List(value, "List"))) return false; }
                else if (type == "Mortal.Core.GameStatConvertData" || type == "Mortal.Core.GameStatRateConvertData") pending.Add(Read(value, "_statData"));
                else if (type != "Mortal.Core.FlagDataRateConvertData" && type != "Mortal.Core.BookLevelStatConvertData" && type != "Mortal.Core.UpgradeItemStatConvertData") return false;
            }
            return true;
        }
        private static bool AppendSmall(List<object> target, IList source)
        {
            if (source.Count > 32 || target.Count + source.Count > 128) return false;
            foreach (object item in source) target.Add(item);
            return true;
        }
        private static int CompareTime(object left, object right)
        {
            foreach (string field in new[] { "Year", "Month", "Stage" })
            { int compare = Convert.ToInt32(Read(left, field)).CompareTo(Convert.ToInt32(Read(right, field))); if (compare != 0) return compare; }
            return 0;
        }
        private static string TimeText(object time)
        { return Read(time, "Year") + "년 " + Read(time, "Month") + "월 " + DisplayNames.Text(Convert.ToString(Read(time, "Stage"))); }
        private static DispatchSnapshot ReadDispatch(PositionController controller)
        {
            if (dispatch != null)
            {
                if (dispatch.Failure != null) throw dispatch.Failure;
                return dispatch;
            }
            // Native click gates are shared by all candidates in this one
            // scheduled query. They are never retained for the next refresh.
            dispatch = new DispatchSnapshot();
            try
            {
                dispatch.Active = (bool)Read(controller, "_active");
                if (!dispatch.Active) return dispatch;
                object free = Read(controller, "_freeManager");
                if (free == null) throw new DataNotReadyException();
                dispatch.Processing = (bool)Read(free, "IsProcessing");
                if (dispatch.Processing) return dispatch;
                object missions = Read(controller, "_missionsManager");
                dispatch.MainStarted = (bool)Read(missions, "MainMissionStart");
                dispatch.MainPosition = Read(missions, "MainMissionPosition");
                dispatch.MainStage = Read(missions, "MainCheckData");
                if (dispatch.MainStarted && Convert.ToInt32(dispatch.MainPosition) != 0) return dispatch;
                object player = Read(controller, "_gameStatManager"), action = null;
                foreach (object stat in List(Read(player, "_stats"), "_list"))
                    if (stat != null && Convert.ToString(Read(stat, "_type")) == "行動次數") { action = stat; break; }
                if (action == null) throw new DataNotReadyException();
                dispatch.Cost = Convert.ToInt32(Read(controller, "_costAction"));
                if (!detailed && !SmallCurrentGraph(action)) { dispatch.ActionUnknown = true; return dispatch; }
                dispatch.Action = SafeDisplayEvaluation.ReadStatFinalValue(action);
                if (dispatch.Action < dispatch.Cost) return dispatch;
                var active = Read(missions, "ActiveSubMissions") as IDictionary;
                if (active == null) { dispatch.SubOrderUnknown = true; return dispatch; }
                int budget = 256;
                foreach (DictionaryEntry entry in active)
                {
                    if (budget-- == 0) { dispatch.SubOrderUnknown = true; break; }
                    if (entry.Value != null && Equals(Read(entry.Value, "_position"), Read(controller, "_position")) && !string.IsNullOrEmpty((string)Read(entry.Value, "_startScript")))
                    { dispatch.FirstSub = entry.Value; break; }
                }
                return dispatch;
            }
            catch (Exception error) { dispatch.Failure = error; throw; }
        }
        private static void ApplyPlace(PositionController controller, Candidate result)
        {
            DispatchSnapshot state = ReadDispatch(controller);
            if (!state.Active) { result.Rank = 1; result.Status += " · 장소 비활성"; return; }
            if (state.Processing) { result.Rank = 1; result.Status += " · 행동 진행 중"; return; }
            if (state.MainStarted && Convert.ToInt32(state.MainPosition) != 0)
            {
                if (result.Main && result.Waiting && ReferenceEquals(result.Stage, state.MainStage) && Equals(state.MainPosition, Read(controller, "_position")))
                { result.Rank = 0; result.Status += " · 이번 클릭의 주 임무"; }
                else { result.Rank = 1; result.Status += " · 주 임무 우선"; }
                return;
            }
            if (state.ActionUnknown) { result.Rank = 2; result.Status += " · 행동 상세 확인 필요"; return; }
            if (detailed) result.Detail += "\n행동: " + state.Action + " / 필요 " + state.Cost;
            if (state.Action < state.Cost) { result.Rank = 1; result.Status += " · 행동 부족"; return; }
            if (state.SubOrderUnknown)
            {
                if (result.Rank == 0) { result.Rank = 2; result.Status += " · 대기 순서 확인 불가"; }
                return;
            }
            if (state.FirstSub != null)
            {
                if (result.Waiting && ReferenceEquals(state.FirstSub, result.Stage)) { result.Rank = 0; result.Status += " · 이번 클릭의 첫 대기 임무"; }
                else { result.Rank = 1; result.Status += " · 앞선 대기 임무 우선"; }
            }
        }

    }
}
