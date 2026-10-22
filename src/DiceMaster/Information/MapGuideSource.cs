using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
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
    public static partial class MapGuideSource
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
            internal readonly List<StoryGroup> Stories = new List<StoryGroup>();
            internal int StoryWork = -1;
            internal int EventWork = -1;
            internal readonly SelectedStoryScan SelectedStories = new SelectedStoryScan();
        }
        private sealed class SelectedStoryScan
        {
            internal readonly List<StoryLink> Candidates = new List<StoryLink>();
            internal int Indexed, Cursor;
            // A successful witness reuses only an unchanged observed read set.
            internal StoryLink Preferred;
            internal PreviewConditions.ReadStamp Stamp;
            internal InformationRow Marker;
            internal Candidate MarkerRoute;
        }
        private sealed class StoryGroup
        {
            internal string Id, RawLabel, DialogueCharacter;
            internal PreviewCondition CurrentPrerequisite;
            internal MapChoiceStep First;
            internal bool SameFirst = true;
            internal readonly List<StoryLink> Links = new List<StoryLink>();
            internal bool Omitted;
            internal readonly List<StoryTopic> Topics = new List<StoryTopic>();
            internal readonly List<MapChoiceStep> ObservedTopics = new List<MapChoiceStep>();
        }
        private sealed class StoryTopic
        {
            internal string Id;
            internal readonly List<StoryLink> Links = new List<StoryLink>();
        }
        private sealed class StoryLink
        { internal MapStoryEntry Story; internal MapRoutePath Path; internal Candidate Notice; }
        private sealed class EventDefinition
        {
            internal PositionEventData Data;
            internal string Script, Id, Name;
            internal MapRouteEntry Route;
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
            internal string Id, Name, Kind, Status, Detail, RoutePreview;
            internal int Rank, Priority; // conditions/route met, denied, unknown
            // Only unfinished scheduled work owns Preparing. Permanent limits,
            // missing game data and retry failures remain diagnostic detail.
            internal bool OneShot, StatusOnly, Preparing, Waiting, Mission, Global, Other, Main, ChoiceStory;
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
            internal object Collection, Entries, Main;
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
        private static MapRouteRead currentRoutes;
        private static MapRouteDefinitions.PreparationSlice preparationSlice;
        private static bool scheduledReads, routeAdvanced, guidancePreferredOnly;
        private static MapRouteDefinitions.PreparationSlice scheduledSlice;
        private sealed class GuidanceBudget
        { internal int Paths = 32, Gates = 32, Kinds = 32, Index = 64; }
        private static GuidanceBudget guidanceBudget;
        private static PreviewConditions.DefinitionScope guidanceDefinitions;
        private static readonly PreviewConditions.DefinitionScope definitionBuffers = new PreviewConditions.DefinitionScope();
        public static void BeginScheduledReads()
        { scheduledReads = true; routeAdvanced = false; scheduledSlice = null; definitionBuffers.Reset(); guidanceDefinitions = definitionBuffers; }
        public static void EndScheduledReads()
        { scheduledReads = false; scheduledSlice = null; guidanceBudget = null; guidancePreferredOnly = false; guidanceDefinitions = null; definitionBuffers.Reset(); }
        private static MapRouteDefinitions.PreparationSlice PreparationBudget()
        { return scheduledReads ? scheduledSlice ?? (scheduledSlice = new MapRouteDefinitions.PreparationSlice()) : new MapRouteDefinitions.PreparationSlice(); }
        private static int routeReads, effectStarts;
        private sealed class Timing
        {
            internal readonly double[] Total = new double[64], Missions = new double[64], Preparation = new double[64], Events = new double[64], Formatting = new double[64];
            internal readonly long[] Allocations = new long[64];
            internal int Count, Next;
            internal void Add(double total, double missions, double preparation, double events, double formatting, long bytes)
            { int at = Next; Total[at] = total; Missions[at] = missions; Preparation[at] = preparation; Events[at] = events; Formatting[at] = formatting; Allocations[at] = bytes; Next = (at + 1) % 64; Count = Math.Min(64, Count + 1); }
            internal string Describe(string name)
            {
                if (Count == 0) return name + ": 표본 없음";
                var sorted = new double[Count]; Array.Copy(Total, sorted, Count); Array.Sort(sorted);
                double missions = 0, preparation = 0, events = 0, formatting = 0; long bytes = 0;
                for (int at = 0; at < Count; at++) { missions += Missions[at]; preparation += Preparation[at]; events += Events[at]; formatting += Formatting[at]; bytes += Allocations[at]; }
                return string.Format(CultureInfo.InvariantCulture, "{0}: 최근 {1}회 · p95 {2:F2} ms · 최대 {3:F2} ms\n평균 임무 {4:F2} / 준비 {5:F2} / 후보 {6:F2} / 구성 {7:F2} ms · 할당 {8}", name, Count, sorted[(int)Math.Ceiling(Count * .95) - 1], sorted[Count - 1], missions / Count, preparation / Count, events / Count, formatting / Count, !allocationAvailable ? "런타임 미지원" : (bytes / Count).ToString(CultureInfo.InvariantCulture) + " B/회");
            }
        }
        private static readonly Timing[] timings = { new Timing(), new Timing(), new Timing() };
        private static readonly Func<long> allocationReader = AllocationReader();
        private static bool allocationAvailable = allocationReader != null;
        private static Func<long> AllocationReader()
        {
            try
            {
                var method = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread", BindingFlags.Static | BindingFlags.Public, null, Type.EmptyTypes, null);
                return method == null || method.ReturnType != typeof(long) ? null : (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
            }
            catch (Exception) { return null; }
        }
        private static long Allocated() { try { return !allocationAvailable ? 0 : allocationReader(); } catch (Exception) { allocationAvailable = false; return 0; } }
        private static double Milliseconds(long start, long end) { return (end - start) * 1000.0 / Stopwatch.Frequency; }
        private static void ClearTimings() { foreach (var timing in timings) { timing.Count = timing.Next = 0; } routeReads = effectStarts = 0; }
        private static WeakReference selection, session;
        private static object observedManager, observedPlayer;
        private static Place selectedPlace, detailPlace;
        private static string detailFailure;
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
        public static bool IsCurrent(InformationRow row) { return row != null && Visible && selectedPlace != null && ReferenceEquals(row.Anchor, Selected) && row.Id == SelectionKey(selectedPlace); }
        private static string SelectionKey(Place place)
        { return ReferenceEquals(place, selectedPlace) ? selectionId : "map/detail/" + place.Id + "/" + selectionVersion; }
        public static bool IsCurrent(string id) { return Visible && id == selectionId; }
        public static Func<bool> Enabled;
        private static bool IsEnabled { get { try { return Enabled == null || Enabled(); } catch (Exception) { return false; } } }
        public static string ContextKey { get { return generation.ToString(CultureInfo.InvariantCulture) + "/" + (Session == null ? "" : Session.GetInstanceID().ToString(CultureInfo.InvariantCulture)) + "/" + selectionId; } }
        private static bool OnMap(PositionController controller) { return controller != null && (MarkerVisible == null || MarkerVisible(controller)); }
        public static bool Visible { get { return IsEnabled && OnMap(Selected); } }
        public static object SelectedAnchor { get { return Selected; } }
        private static PositionController Session
        {
            get
            {
                var value = session == null ? null : session.Target as PositionController;
                return value != null && value.isActiveAndEnabled && value.gameObject.activeInHierarchy ? value : null;
            }
        }
        public static bool SessionVisible { get { return IsEnabled && OnMap(Session); } }
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
            places.Add(new Place { Controller = new WeakReference(controller), Id = checked(++sequence) }); markerGeneration++;
        }
        public static void Select(PositionController controller)
        {
            if (controller == null) return;
            Register(controller);
            if (selection != null && ReferenceEquals(selection.Target, controller)) { detailPlace = selectedPlace; return; }
            selection = new WeakReference(controller); session = new WeakReference(controller);
            foreach (Place place in places) if (ReferenceEquals(place.Controller.Target, controller)) { selectedPlace = place; break; }
            detailPlace = selectedPlace; detailFailure = null;
            selectionVersion++; selectionId = "map/" + selectedPlace.Id + "/" + selectionVersion; generation++;
        }
        public static void Deselect(PositionController controller)
        {
            if (selection == null || !ReferenceEquals(selection.Target, controller)) return;
            selection = null; selectedPlace = null; selectionId = ""; selectionVersion++; generation++;
        }
        public static void Clear()
        { backgroundPrepareCursor = backgroundReadCursor = 0; places.Clear(); globalPlace.Reads.Clear(); scripts.Clear(); missionIndices.Clear(); MapRouteDefinitions.ClearSharedSyntaxCache(); ClearTimings(); selection = session = null; observedManager = observedPlayer = null; selectedPlace = detailPlace = null; detailFailure = null; selectionId = ""; selectionVersion++; hasClock = false; elapsed = 0; generation++; markerGeneration++; }
        public static void ClearSnapshots() { ClearSnapshotsCore(true); }
        public static void ClearSnapshotsPreservingSyntax() { ClearSnapshotsCore(false); }
        private static void ClearSnapshotsCore(bool clearSyntax)
        {
            // Used for OFF: retain weak lifecycle handles, never parsed data.
            bool changed = scripts.Count != 0 || missionIndices.Count != 0 || detailPlace != null || detailFailure != null;
            detailPlace = null; detailFailure = null;
            foreach (Place place in places)
            {
                changed |= place.Definition != null || place.Data != null || place.PositionConfig != null || place.Reads.Count != 0;
                place.Definition = null; place.Reads.Clear(); place.Data = place.PositionConfig = null;
            }
            globalPlace.Reads.Clear(); scripts.Clear(); missionIndices.Clear(); observedManager = observedPlayer = null; if (changed) generation++;
            if (changed) markerGeneration++;
            if (clearSyntax) { MapRouteDefinitions.ClearSharedSyntaxCache(); ClearTimings(); }
        }
        public static List<DisplayQuery> Discover(bool guide, bool oneShot, bool details = false)
        {
            var result = new List<DisplayQuery>();
            if (!IsEnabled || !guide && !oneShot) return result;
            Place captured = details ? DetailPlace() : selectedPlace;
            PositionController controller = captured == null ? Selected ?? Session : Live(captured);
            if (controller == null || captured == null && !guide) return result;
            int version = selectionVersion;
            string context = ContextKey;
            result.Add(new DisplayQuery(captured == null ? "map/global/" + context : SelectionKey(captured), () => {
                if (version != selectionVersion || context != ContextKey || !ReferenceEquals(captured == null ? Selected ?? Session : Live(captured), controller) ||
                    details && captured != null && !OnMap(controller)) throw new DataNotReadyException();
                detailed = details;
                try
                {
                    object snapshot = captured == null ? ReadGlobal(controller, guide) : ReadCurrentPlace(captured, controller, guide, oneShot);
                    if (details) detailFailure = null;
                    return snapshot;
                }
                catch (Exception error) { if (details) detailFailure = error.GetType().FullName; throw; }
                finally { currentRoutes = null; preparationSlice = null; currentConditions.Clear(); dispatch = null; entryBarrier = null; }
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
            UpdatePlaceMetadata(place, controller, true);
            return ReadPlace(place, controller, guide, oneShot);
        }
        private static void UpdatePlaceMetadata(Place place, PositionController controller, bool announce)
        {
            try
            {
                object data = Read(controller, "_positionData");
                if (data == null) throw new DataNotReadyException();
                object checkpoints = ExistingCheckpoints();
                object config = checkpoints == null ? PreviewConditions.LoadedDefinitions(PreviewDefinitionKind.Positions) : Read(checkpoints, "_position");
                string script = (string)Read(data, "_defaultScript");
                string key = (string)Read(data, "name");
                if (!ReferenceEquals(data, place.Data) || !ReferenceEquals(config, place.PositionConfig) || script != place.Script || key != place.Key)
                {
                    place.Data = data; place.PositionConfig = config; place.Script = script; place.Key = key;
                    place.Definition = null; place.Reads.Clear(); if (announce) generation++;
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
                    place.Reads.Clear(); if (announce) generation++;
                }
                throw;
            }
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
                    if (field != null) { object value = field.GetValue(null); return PreviewConditions.OwnerAlive(value) ? value : null; }
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
            return ReadPoolDefinition(place.PositionConfig, key, message => {
                if (message == null) place.Definition.PoolDiagnostics.Remove(key);
                else place.Definition.PoolDiagnostics[key] = message;
            });
        }
        // The choice observer uses the same existing singleton/serialized pool
        // boundary as the map. Never call Position/GetResult to fill a route.
        public static IList<MapRouteEvent> ReadChoicePool(string key)
        {
            object manager = ExistingCheckpoints();
            object config = manager == null ? PreviewConditions.LoadedDefinitions(PreviewDefinitionKind.Positions) : Read(manager, "_position");
            return ReadPoolDefinition(config, key, null);
        }
        private static IList<MapRouteEvent> ReadPoolDefinition(object config, string key, Action<string> diagnostic)
        {
            if (config == null) throw new DataNotReadyException();
            object match = null;
            var configs = config as object[] ?? new[] { config };
            if (configs.Length > 64) throw new DataNotReadyException();
            int budget = 1024;
            foreach (object definition in configs)
            {
                IList pools = List(definition, "_list");
                foreach (object pool in pools)
                {
                    if (--budget < 0) { diagnostic?.Invoke("풀 검색 상한 · 1,024개 안에서 일치 항목을 찾지 못했습니다. 이후 자료는 확인하지 않았습니다."); throw new InvalidOperationException("Map pool search limit reached."); }
                    if (pool == null || Name(pool) != key) continue;
                    if (match != null && !ReferenceEquals(match, pool)) throw new DataNotReadyException();
                    match = pool;
                    // The native owner has a defined first-match ordering.
                    if (!(config is object[])) break;
                }
            }
            if (match != null)
            {
                object pool = match;
                diagnostic?.Invoke(null);
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
                    source.Routes = new MapRouteDefinitions(source.Script, DataHelper.ReadPreviewScript, key => ReadPool(place, key)) { UseSharedSyntax = true, PrioritizeRootPreparation = true };
                if (source.Routes != null)
                {
                    if (!scheduledReads || !routeAdvanced)
                    {
                        int work = source.Routes.WorkCount, resources = source.Routes.ScriptReadCount + source.Routes.PoolReadCount;
                        source.Routes.AdvanceBudgeted(Now, 8192, preparationSlice.ShouldYield);
                        if (scheduledReads && (source.Routes.WorkCount != work || source.Routes.ScriptReadCount + source.Routes.PoolReadCount != resources)) routeAdvanced = true;
                    }
                    if (source.EventWork != source.Routes.WorkCount)
                    {
                      source.EventWork = source.Routes.WorkCount;
                      foreach (MapRouteEntry route in source.Routes.Entries)
                      {
                        EventDefinition entry;
                        if (!source.RouteEvents.TryGetValue(route, out entry))
                        {
                            var data = route.Event as PositionEventData;
                            if (data == null) throw new InvalidOperationException("Unsupported map route event.");
                            entry = new EventDefinition { Data = data, Script = route.ResultScript, Id = route.Id, Route = route };
                            source.RouteEvents.Add(route, entry); source.Events.Add(entry);
                        }
                      }
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
                if (coldEffects <= 0 || !detailed && preparationSlice != null && preparationSlice.ShouldYield())
                {
                    definition.Preparing = true; effectsPreparing = true;
                    definition.Description = "효과·1회성 준비 중"; return definition;
                }
                coldEffects--; effectStarts++; definition.Actions = PositionEventCache.GetDefinitionParser().ExtractActionsFromLua(script);
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
            MapDefinitionAssets.Observe(manager, player);
            if (ReferenceEquals(manager, observedManager) && ReferenceEquals(player, observedPlayer)) return;
            globalPlace.Reads.Clear(); scripts.Clear(); missionIndices.Clear();
            foreach (Place value in places) { value.Reads.Clear(); value.Definition = null; }
            OneShotProof.Clear(); observedManager = manager; observedPlayer = player; generation++; markerGeneration++;
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
            var snapshot = Snapshot(null, controller, candidates, guide, false);
            if (detailed) snapshot.Entries.Add(DiagnosticRow());
            return snapshot;
        }
        private static object ReadPlace(Place place, PositionController controller, bool guide, bool oneShot)
        {
            if (!IsEnabled || Live(place) != controller || !ReferenceEquals(Read(controller, "_positionData"), place.Data)) throw new DataNotReadyException();
            long start = Stopwatch.GetTimestamp(), allocated = Allocated();
            routeReads = effectStarts = 0;
            var candidates = new List<Candidate>();
            if (guide) CollectMissions(place, controller, candidates, false);
            long missionsEnd = Stopwatch.GetTimestamp();
            // A waiting mission owns this click; do not prepare a default
            // activity that dispatch will bypass (one-shot OFF fast path).
            if (!detailed && !oneShot && candidates.Exists(value => value.Waiting && !value.Global))
            {
                var waiting = Snapshot(place, controller, candidates, guide, oneShot);
                long end = Stopwatch.GetTimestamp(); timings[0].Add(Milliseconds(start, end), Milliseconds(start, missionsEnd), 0, 0, Milliseconds(missionsEnd, end), Math.Max(0, Allocated() - allocated));
                return waiting;
            }
            preparationSlice = PreparationBudget();
            currentRoutes = new MapRouteRead();
            Source source = GetSource(place);
            long prepared = Stopwatch.GetTimestamp();
            foreach (EventDefinition entry in source.Events)
            {
                EventDefinition captured = entry;
                Candidate candidate = TryCandidate(place, "event/" + RuntimeHelpers.GetHashCode(entry.Data) + "/" + entry.Script, () => Event(controller, source, captured, guide, oneShot));
                if (candidate != null) candidates.Add(candidate);
            }
            // Future NPC stories are research/detail, not passive predictions.
            // Only an explicit log query samples their downstream conditions.
            if (guide && detailed && source.Routes != null) CollectStories(place, controller, source, candidates);
            if (guide && source.Routes != null) CollectSelectedRoute(place, controller, source, candidates);
            if (guide && detailed && source.Ready && source.Events.Count == 0 && source.Stories.Count == 0 && !string.IsNullOrEmpty(source.Script))
                candidates.Add(TryCandidate(place, source, () => Story(controller, source.Script,
                    source.Failure != null || source.Routes != null && (source.Routes.IsPartial || source.Routes.HasReadFailure))));
            if (source.Failure != null)
                candidates.Add(Diagnostic("이야기 자료", source.Failure is DataNotReadyException ? "게임 데이터 대기 중" : "읽기 실패", "이 장소의 연결된 이야기 자료를 읽지 못했습니다."));
            else if (source.Routes != null && (source.Routes.IsPreparing || source.Routes.IsPartial || source.Routes.HasReadFailure))
                candidates.Add(Diagnostic("경로 자료", source.Routes.IsPreparing ? "경로 준비 중" : source.Routes.HasReadFailure ? "경로 읽기 실패" : "경로 일부 확인 불가", source.Routes.Diagnostic, source.Routes.IsPreparing));
            foreach (string diagnostic in source.PoolDiagnostics.Values)
                candidates.Add(Diagnostic("풀 자료", "풀 검색 상한 · 일부 확인 불가", diagnostic));
            long eventsEnd = Stopwatch.GetTimestamp();
            var snapshot = Snapshot(place, controller, candidates, guide, oneShot);
            long finished = Stopwatch.GetTimestamp();
            timings[detailed ? 2 : 1].Add(Milliseconds(start, finished), Milliseconds(start, missionsEnd), Milliseconds(missionsEnd, prepared), Milliseconds(prepared, eventsEnd), Milliseconds(eventsEnd, finished), Math.Max(0, Allocated() - allocated));
            if (detailed) snapshot.Entries.Add(DiagnosticRow());
            return snapshot;
        }
        private static void CollectStories(Place place, PositionController controller, Source source, List<Candidate> candidates)
        {
            // Rebuild this small definition index only when incremental parsing
            // made progress. Warm .25 s reads only sample cached story gates.
            if (source.StoryWork != source.Routes.WorkCount)
            {
                source.StoryWork = source.Routes.WorkCount; source.Stories.Clear();
                var groups = new Dictionary<string, StoryGroup>(StringComparer.Ordinal);
                foreach (MapStoryEntry story in source.Routes.StoryEntries)
                    foreach (MapRoutePath path in story.Paths)
                    {
                        if (path.Steps.Count == 0 || string.IsNullOrEmpty(path.DialogueCharacter)) continue;
                        MapChoiceStep first = path.Steps[0];
                        string id = first.Script + "/" + first.SourceOffset + "/" + first.Index + "/" + first.RawLabel + "/npc/" + path.DialogueCharacter;
                        StoryGroup group;
                        if (!groups.TryGetValue(id, out group))
                        {
                            if (groups.Count >= 256) break;
                            groups.Add(id, group = new StoryGroup { Id = id, RawLabel = first.RawLabel, DialogueCharacter = path.DialogueCharacter, First = first,
                                CurrentPrerequisite = path.CurrentPrerequisite }); source.Stories.Add(group);
                        }
                        else
                        {
                            group.SameFirst &= ReferenceEquals(group.First, first);
                            group.CurrentPrerequisite = MapRoutePath.CommonPrerequisite(group.CurrentPrerequisite, path.CurrentPrerequisite);
                        }
                        foreach (MapChoiceStep step in path.Steps)
                            if (!group.ObservedTopics.Contains(step)) group.ObservedTopics.Add(step);
                        // Include every observed path in the common prefix,
                        // including paths omitted from the bounded detail list.
                        if (group.Links.Count < 32)
                        {
                            var link = new StoryLink { Story = story, Path = path }; group.Links.Add(link);
                            MapChoiceStep last = path.TopicStep;
                            string topicId = last.Script + "/" + last.SourceOffset + "/" + last.Index + "/" + last.RawLabel;
                            StoryTopic topic = group.Topics.Find(value => value.Id == topicId);
                            if (topic == null) group.Topics.Add(topic = new StoryTopic { Id = topicId });
                            topic.Links.Add(link);
                        }
                        else group.Omitted = true;
                    }
            }
            var read = new MapRouteRead(); int budget = 16, gates = 32, diagnosticBudget = 4, choiceBudget = 32;
            string talkSourceDiagnostic = null;
            foreach (StoryGroup group in source.Stories)
            {
                StoryGroup captured = group; bool sample = detailed || budget-- > 0;
                candidates.Add(TryCandidate(place, "story/" + group.Id, () => {
                    bool met = false, unknown = captured.Omitted || source.Routes.IsPreparing || source.Routes.HasIncompleteStoryPaths || source.Routes.HasReadFailure;
                    MapChoiceStep first = captured.Links.Count == 0 || captured.Links[0].Path.Steps.Count == 0 ? null : captured.Links[0].Path.Steps[0];
                    // Definition-time identity covers retained AND omitted
                    // paths. A larger discovered tail cannot revive a false
                    // shared root guard merely by exceeding the detail cap.
                    bool sharedFirst = sample && first != null && captured.SameFirst && !source.Routes.IsPreparing && !source.Routes.HasReadFailure;
                    // This complete root-menu prerequisite is a negative-only
                    // current snapshot, never a shortcut to positive eligibility.
                    bool currentBlocked = false;
                    if (sharedFirst && gates > 0)
                    { gates--; currentBlocked = first.ReadCurrentBlocker(read); }
                    if (sample && !currentBlocked && first != null && !source.Routes.IsPreparing && !source.Routes.HasReadFailure &&
                        captured.CurrentPrerequisite != null && !ReferenceEquals(captured.CurrentPrerequisite, PreviewConditions.Unknown) && gates > 0)
                    {
                        gates--;
                        if (read.Current(captured.CurrentPrerequisite) == PreviewConditionState.NotMet)
                            currentBlocked = source.Routes.CurrentOmissionsBlocked(first, captured.ObservedTopics, read, ref gates);
                    }
                    bool activityOnly = sample && !captured.Omitted;
                    bool allPathsBlocked = captured.Topics.Count > 0 && !captured.Omitted && !source.Routes.IsPreparing && !source.Routes.HasReadFailure;
                    if (sample && !currentBlocked) foreach (StoryTopic topic in captured.Topics)
                    {
                        // Resolve each selected topic before combining NPCs.
                        // A repeat service's uncertain fixed response must not
                        // keep an otherwise completed conversation alive.
                        var storyLinks = new List<StoryLink>();
                        foreach (StoryLink link in topic.Links)
                            if (link.Path.ReadStoryKind(read, ref gates) != MapStoryKind.ActivityResponse) storyLinks.Add(link);
                        if (storyLinks.Count == 0) continue;
                        activityOnly = false;
                        bool topicMet = false, topicUnknown = false, pathsBlocked = true;
                        foreach (StoryLink link in storyLinks)
                        {
                            // The captured necessary prefix often excludes a
                            // completed topic immediately, including an intro
                            // alternative. Spend no full-story read on it.
                            if (gates <= 0) { topicUnknown = true; pathsBlocked = false; break; }
                            gates--;
                            if (link.Path.ReadCurrentBlocker(read)) continue;
                            if (gates <= 0) { topicUnknown = true; pathsBlocked = false; break; }
                            gates--;
                            if (link.Path.TopicStep != null && link.Path.TopicStep.ReadTopicBlocker(read)) continue;
                            if (gates <= 0) { topicUnknown = true; pathsBlocked = false; break; }
                            gates--;
                            MapRouteState state = link.Path.ReadStoryConditions(read);
                            topicMet |= state == MapRouteState.Met; topicUnknown |= state == MapRouteState.Unknown;
                            pathsBlocked &= state == MapRouteState.NotMet;
                            if (topicMet) break;
                        }
                        met |= topicMet;
                        unknown |= !pathsBlocked && topicUnknown;
                        allPathsBlocked &= pathsBlocked;
                        if (met) break;
                    }
                    if (sample && !currentBlocked && !met && allPathsBlocked && first != null)
                    {
                        bool covered = source.Routes.CurrentOmissionsBlocked(first, captured.ObservedTopics, read, ref gates);
                        if (activityOnly) activityOnly = covered;
                        else currentBlocked = covered;
                    }
                    else if (source.Routes.IsPreparing || source.Routes.HasReadFailure) activityOnly = false;
                    if (currentBlocked) activityOnly = false;
                    int rank = !sample ? 2 : currentBlocked ? 1 : met ? 0 : unknown ? 2 : 1;
                    var result = new Candidate { Id = "story/" + captured.Id, Name = ChoiceStoryLabels.DialogueName(captured.RawLabel, captured.DialogueCharacter),
                        Kind = activityOnly ? "일반 활동" : "인물 대화", Other = activityOnly, ChoiceStory = !activityOnly, Rank = rank,
                        Status = StoryStatus(rank),
                        Detail = "원본 경로에서 플레이어 이외의 명시된 화자와 대사를 확인한 인물 대화입니다. 휴식 안내문·독백과 구조적으로 확인한 활동의 고정 반응은 자동 안내에서 제외합니다. 1회성을 뜻하지 않습니다.\n첫 선택뿐 아니라 해당 대화에 이르는 내부 분기 조건을 현재 값으로 읽습니다. 선행 상태 변경·지원하지 않는 조건·경로 누락이 있으면 진행 가능성 있음으로 표시합니다. 이는 확률이나 이야기 발생 보증이 아닙니다." };
                    if (activityOnly) result.Detail = "별도의 이야기 분기 없이 비용 확인·활동 실행·결과 응답으로 이어지는 고정 반응입니다. 자동 이야기 안내에서 제외하며 활동의 선택·실행 가능 여부를 뜻하지 않습니다.";
                    if (currentBlocked)
                        result.Detail += "\n현재 값의 필수 조건은 미충족입니다. 지도 경로 툴팁은 진입 후 가상 값을 별도로 확인하므로 아래 진입 후 진단과 구분합니다. 미래 실행 불가를 증명하지 않습니다.";
                    if (detailed)
                    {
                        var lines = new List<string>();
                        foreach (StoryLink link in captured.Links)
                        {
                            if (link.Path.ReadChoiceAvailability(read, ref choiceBudget) != MapRouteState.Met) continue;
                            string text = "선택 경로: " + ChoiceStoryLabels.Path(link.Path.Steps) + (link.Path.ReadStoryKind(read, ref gates) == MapStoryKind.ActivityResponse ? " · 활동 고정 반응 (자동 안내 제외)" : "");
                            if (!lines.Contains(text)) lines.Add(text);
                        }
                        result.Detail += "\n" + string.Join("\n", lines);
                        result.Detail += "\n잠겼거나 해금 여부를 확인하지 못한 선택 경로는 목록에서 제외합니다.";
                        if (diagnosticBudget > 0 && captured.CurrentPrerequisite != null)
                        {
                            diagnosticBudget--;
                            result.Detail += "\n현재 필수 조건 표본 (후속 상태/최종 실행 보증 아님):\n" +
                                PreviewConditions.DescribeCurrent(captured.CurrentPrerequisite) + "\n" +
                                PreviewConditions.DescribePositionEntry(captured.CurrentPrerequisite, Read(controller, "_positionData")) + "\n" +
                                (talkSourceDiagnostic ?? (talkSourceDiagnostic = PreviewConditions.DescribeTalkResetSource()));
                        }
                        if (captured.Omitted) result.Detail += "\n경로 상한으로 일부 연결은 생략했습니다.";
                    }
                    string conditionStatus = result.Status;
                    ApplyPlace(controller, result);
                    // A known dispatch blocker takes precedence over story
                    // conditions; never leave a positive prefix beside it.
                    result.Status = StoryStatus(result.Rank) + result.Status.Substring(conditionStatus.Length);
                    return result;
                }));
            }
        }
        private static string StoryStatus(int rank)
        { return rank == 0 ? "진행 가능" : rank == 1 ? "지금 진행 불가" : "진행 가능성 있음"; }
        private static Candidate Diagnostic(string name, string status, string detail, bool preparing = false)
        { return new Candidate { Id = "diagnostic/" + name, Name = name, Kind = "상태", StatusOnly = true, Preparing = preparing, Rank = 2, Status = status, Detail = detail }; }
        private static MapGuideSnapshot Snapshot(Place place, PositionController controller, List<Candidate> candidates, bool guide, bool once)
        {
            var snapshot = new MapGuideSnapshot { HasSelection = place != null, ContextKey = ContextKey };
            if (effectsPreparing) candidates.Add(Diagnostic("효과·1회성", "효과·1회성 준비 중", "새 결과 스크립트는 한 번의 예정된 조회에 하나씩 준비합니다. 아직 분석하지 않은 결과를 효과 없음이나 반복 가능으로 판단하지 않습니다.", true));
            var ordered = new List<Candidate>();
            // Detail keeps every recognized item; default counts omit ordinary activity.
            for (int group = 0; group < 6; group++)
                for (int rank = 0; rank < 3; rank++)
                    foreach (Candidate value in candidates)
                    {
                        int actual = value.StatusOnly ? 5 : value.Other ? 4 : value.Global ? 1 + value.Priority : 0;
                        if (actual == group && value.Rank == rank) ordered.Add(value);
                    }
            int waiting = 0, stages = 0, oneShots = 0; bool available = false, preparing = false; var automatic = new List<string>(); var diagnostics = new List<string>();
            int ordinal = 0;
            foreach (Candidate value in ordered)
            {
                string group = value.StatusOnly ? "자료 상태" : value.Other ? "기타 연결 정보" : value.Global ? "장소 무관" : "선택 장소";
                string title = Plain(value.Name);
                string status = value.Status + (value.OneShot ? " · 1회성" : "");
                snapshot.Entries.Add(new InformationRow((place == null ? "map/global/" + ContextKey : SelectionKey(place)) + "/" + (value.Id ?? "entry/" + ordinal++),
                    title, group + " · " + value.Kind, status, value.Detail ?? "", title + " · " + value.Kind + " · " + status, controller, value.Rank == 0));
                if (value.StatusOnly) { preparing |= value.Preparing; if (!diagnostics.Contains(value.Status)) diagnostics.Add(value.Status); continue; }
                if (value.Global || value.Other) continue;
                if (value.Waiting) waiting++;
                else if (value.Mission && value.Rank == 0) stages++;
                else if (value.OneShot && value.Rank == 0) oneShots++;
                // Keep research candidates and independent one-shot proofs in
                // detail. The passive surface requires current reachability
                // AND either real waiting membership or a proven one-shot.
                // Apply the policy before row limits, counts and panel color.
                if (AutomaticCandidate(value, guide, once))
                {
                    available = true;
                    automatic.Add(MapGuidePresentation.Notice(title, value.Mission ? (value.Main ? "주 임무" : "보조 임무") : "1회성 이야기", value.Rank, value.Waiting, value.Mission, value.Status));
                }
            }
            // A selected-place route is guidance only. It never changes the
            // opportunity count or Available bit. Its light badge is separate.
            bool hasGuidance = false;
            if (guide) foreach (Candidate value in ordered)
                if (!string.IsNullOrEmpty(value.RoutePreview)) { automatic.Add(value.RoutePreview); hasGuidance = true; break; }
            if (snapshot.Entries.Count == 0)
                snapshot.Entries.Add(new InformationRow("map/empty/" + ContextKey, place == null ? "장소 무관 임무 없음" : detailed ? "현재 연결 항목 없음" : "현재 자동 표시 대상 없음", "지도", "현재 확인된 항목 없음",
                    "현재 관찰한 자료 범위의 결과입니다. 전체 게임의 기회 없음이나 반복 가능을 뜻하지 않습니다."));
            if (place != null)
            {
                string title = LocationTranslations.TranslateLocation((place.Key ?? "").Replace("養成_", ""));
                string status = "대기 임무 " + waiting + " · 단계 후보 " + stages + " · 확인된 1회성 " + oneShots;
                if (diagnostics.Count != 0) status += " · " + string.Join(" · ", diagnostics);
                string text = MapGuidePresentation.Summary(automatic, guide, preparing);
                snapshot.Summary = new InformationRow(SelectionKey(place), Plain(title), "지도", status,
                    "대기 상태·현재 조건·선택 우선순위는 서로 다른 정보입니다. 경로·조건·효과와 장소 무관 항목은 지도 로그에서 읽습니다.", text, controller, available, available, hasGuidance);
            }
            return snapshot;
        }
        private static bool AutomaticCandidate(Candidate value, bool guide, bool oneShot)
        {
            return !value.StatusOnly && !value.Other && !value.Global && !value.ChoiceStory && value.Rank == 0 &&
                (guide && value.Waiting || oneShot && value.OneShot);
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
            if (!detailed && !proof.Proven) return null;
            bool sample = detailed || automaticEvents > 0 && SmallEventGraph(entry.Data);
            MapRouteState routeState = MapRouteState.Unknown;
            if (sample) { if (!detailed) automaticEvents--; routeReads++; routeState = entry.Route.ReadCurrent(currentRoutes); }
            bool readState = detailed || sample && routeState == MapRouteState.Met;
            EventEligibilitySnapshot state = readState ? (detailed ? EventEligibility.Evaluate(entry.Data) : EventEligibility.EvaluateCurrent(entry.Data)) : null;
            if (state != null && state.Reason == EventEligibilityReason.ReadFailure) throw new InvalidOperationException("Map event read failed.");
            if (entry.Name == null) entry.Name = Plain(DataHelper.GetEventName(entry.Data));
            var result = new Candidate { Id = "event/" + RuntimeHelpers.GetHashCode(entry.Data) + "/" + entry.Script, Name = entry.Name, Kind = "이야기", Other = true,
                Rank = state == null ? 2 : state.IsEligible ? 0 : state.State == EventEligibilityState.Ineligible ? 1 : 2,
                Status = state == null ? "현재 상태는 상세 조회에서 확인" : state.IsEligible ? "진행 가능" : state.State == EventEligibilityState.Ineligible ? "진행 불가" : "확인 불가",
                Detail = state == null ? "현재 조건·효과는 지도 로그 상세를 열면 조회합니다." : state.Detail };
            if (routeState == MapRouteState.NotMet) { result.Rank = 1; result.Status = "진행 불가 · 경로 조건 미충족"; }
            else if (routeState == MapRouteState.Unknown) { result.Rank = 2; result.Status = "경로 확인 필요"; }
            else if (entry.Route.HasChoice && result.Rank == 0) result.Status = "선택 후 기회";
            if (detailed)
            {
                var paths = new List<string>(); int choiceBudget = 32, remaining = 32;
                foreach (MapRoutePath path in entry.Route.Paths)
                {
                    if (remaining-- <= 0) break;
                    if (path.ReadChoiceAvailability(currentRoutes, ref choiceBudget) == MapRouteState.Met && !paths.Contains(path.Description)) paths.Add(path.Description);
                }
                result.Detail = (paths.Count == 0 ? "현재 안내할 해금 확인 경로가 없습니다." : "경로: " + string.Join("\n", paths)) +
                    "\n잠겼거나 해금 여부를 확인하지 못한 선택 경로는 목록에서 제외합니다.\n현재 이벤트 판정: " +
                    (state == null ? "상세 조회 대기" : state.IsEligible ? "조건·가중치 충족" : state.State == EventEligibilityState.Ineligible ? "조건·가중치 미충족" : "확인 불가") + "\n" + result.Detail;
            }
            if (detailed && routeState == MapRouteState.Unknown)
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
                object main = Read(manager, "_main");
                bool changed = !ReferenceEquals(index.Collection, collection) || !ReferenceEquals(index.Entries, entries) ||
                    index.Count != entries.Count || !ReferenceEquals(index.Main, main);
                if (changed || index.Failure != null)
                {
                    index.Collection = collection; index.Entries = entries; index.Count = entries.Count;
                    index.Main = main; index.Places.Clear(); index.LogicalIds.Clear();
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
            // The game's small live waiting set does not depend on the full
            // mission/stage definition index. Read it first, even if that
            // independent index is missing, failed or still being prepared.
            MissionIndex cachedIndex; missionIndices.TryGetValue(manager, out cachedIndex);
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
                    MissionLink owner = null;
                    if (cachedIndex != null) cachedIndex.LogicalIds.TryGetValue(id, out owner);
                    AddMission(cache, controller, manager, owner == null ? null : owner.Mission, stage, id, false, true, target);
                }
            }
            // Unregistered mission stages are detail-only research. Passive
            // refresh neither builds their global index nor reads current keys.
            if (!detailed) return;
            MissionIndex index = GetMissionIndex(manager, selected == null ? "global" : selected.Key);
            if (index.Failure != null)
                target.Add(Diagnostic("임무 정의", index.Failure is DataNotReadyException ? "게임 데이터 대기 중" : "읽기 실패", "임무 정의를 준비하지 못했습니다. 기존 대기 목록은 별도로 읽습니다."));
            else if (!index.Ready)
                // This is unfinished global work, not preparation of the
                // selected place. Keep it in log detail without keeping an
                // otherwise empty passive overlay alive.
                target.Add(Diagnostic("임무 정의", "임무 정의 준비 중", "전체 단계·장소 색인을 나누어 준비 중입니다. 현재 대기 임무와 선택 장소의 준비 상태는 별도이며, 아직 읽지 않은 임무가 있을 수 있습니다."));
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
        internal static bool SmallEventGraph(object data)
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
