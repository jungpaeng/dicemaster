using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DiceMaster.LiveLog;
using Mortal.Free;

namespace DiceMaster.Information
{
    public static partial class MapGuideSource
    {
        private static int markerGeneration;
        private static int markerCandidates;
        public static Func<PositionController, bool> MarkerVisible;
        private static PositionController MarkerSession
        {
            get
            {
                var current = Session;
                if (current != null && (MarkerVisible == null || MarkerVisible(current))) return current;
                for (int at = 0; at < Math.Min(64, places.Count); at++)
                { current = Live(places[at]); if (current != null && (MarkerVisible == null || MarkerVisible(current))) return current; }
                return null;
            }
        }
        public static bool MarkerSessionVisible { get { return IsEnabled && MarkerSession != null; } }
        public static string MarkerContextKey { get { return markerGeneration.ToString(CultureInfo.InvariantCulture); } }
        public static bool IsCurrent(MapMarkerSnapshot snapshot)
        { return snapshot != null && MarkerSessionVisible && snapshot.ContextKey == MarkerContextKey; }
        private sealed class MarkerPending
        { internal object Stage; internal string Id; internal bool Main; }
        private sealed class MarkerBatch
        {
            internal object Manager, Player, Free, MainStage, MainPosition;
            internal bool MainStarted, Processing, SubUnknown, ActionRead, ActionUnknown;
            internal int Action;
            internal readonly Dictionary<int, MarkerPending> First = new Dictionary<int, MarkerPending>();
        }
        public static List<DisplayQuery> DiscoverMarkers(bool guide, bool oneShot)
        {
            var queries = new List<DisplayQuery>();
            if (!IsEnabled || !guide && !oneShot || !MarkerSessionVisible) return queries;
            PositionController controller = MarkerSession;
            string key = MarkerContextKey;
            queries.Add(new DisplayQuery("map/markers/" + key, () => {
                if (!MarkerSessionVisible || key != MarkerContextKey) throw new DataNotReadyException();
                try { return ReadMarkers(controller, guide, oneShot); }
                finally { currentRoutes = null; preparationSlice = null; currentConditions.Clear(); dispatch = null; entryBarrier = null; }
            }));
            return queries;
        }
        private static MarkerBatch ReadMarkerBatch(PositionController controller)
        {
            var batch = new MarkerBatch { Manager = Read(controller, "_missionsManager"), Player = Read(controller, "_gameStatManager"), Free = Read(controller, "_freeManager") };
            if (batch.Manager == null || batch.Player == null || batch.Free == null) throw new DataNotReadyException();
            batch.Processing = (bool)Read(batch.Free, "IsProcessing");
            batch.MainStarted = (bool)Read(batch.Manager, "MainMissionStart");
            batch.MainStage = Read(batch.Manager, "MainCheckData"); batch.MainPosition = Read(batch.Manager, "MainMissionPosition");
            if (batch.MainStarted && Convert.ToInt32(batch.MainPosition) != 0) return batch;
            var active = Read(batch.Manager, "ActiveSubMissions") as IDictionary;
            if (active == null) { batch.SubUnknown = true; return batch; }
            int budget = 256;
            // Enumerate the small live waiting set once, never the mission catalogue.
            foreach (DictionaryEntry pair in active)
            {
                if (budget-- <= 0) { batch.SubUnknown = true; break; }
                object stage = pair.Value;
                if (stage == null || string.IsNullOrEmpty((string)Read(stage, "_startScript"))) continue;
                int position = Convert.ToInt32(Read(stage, "_position"));
                if (position != 0 && !batch.First.ContainsKey(position)) batch.First.Add(position, new MarkerPending { Stage = stage, Id = Convert.ToString(pair.Key, CultureInfo.InvariantCulture) });
            }
            return batch;
        }
        private static DispatchSnapshot MarkerDispatch(PositionController controller, MarkerBatch batch)
        {
            var state = new DispatchSnapshot { Active = (bool)Read(controller, "_active"), Processing = batch.Processing,
                MainStarted = batch.MainStarted, MainStage = batch.MainStage, MainPosition = batch.MainPosition, Cost = Convert.ToInt32(Read(controller, "_costAction")) };
            if (!state.Active || state.Processing || state.MainStarted && Convert.ToInt32(state.MainPosition) != 0) return state;
            if (!batch.ActionRead)
            {
                batch.ActionRead = true; object action = null; int remaining = 256;
                foreach (object stat in List(Read(batch.Player, "_stats"), "_list"))
                {
                    if (remaining-- <= 0) break;
                    if (stat != null && Convert.ToString(Read(stat, "_type")) == "行動次數") { action = stat; break; }
                }
                if (action == null) throw new DataNotReadyException();
                batch.ActionUnknown = !SmallCurrentGraph(action);
                if (!batch.ActionUnknown) batch.Action = SafeDisplayEvaluation.ReadStatFinalValue(action);
            }
            state.Action = batch.Action; state.ActionUnknown = batch.ActionUnknown;
            MarkerPending first;
            if (batch.First.TryGetValue(Convert.ToInt32(Read(controller, "_position")), out first)) state.FirstSub = first.Stage;
            // An omitted suffix cannot precede an already observed first item.
            state.SubOrderUnknown = batch.SubUnknown && state.FirstSub == null;
            return state;
        }
        private static MapMarkerSnapshot ReadMarkers(PositionController sessionController, bool guide, bool oneShot)
        {
            ObserveSessionData(sessionController);
            var snapshot = new MapMarkerSnapshot { ContextKey = MarkerContextKey };
            var batch = ReadMarkerBatch(sessionController);
            detailed = false; automaticEvents = AutomaticEventLimit; automaticProofs = coldEffects = 0;
            markerCandidates = 64;
            currentRoutes = new MapRouteRead(); currentConditions.Clear();
            var slice = new MapRouteDefinitions.PreparationSlice();
            var visible = new List<Place>(64);
            var preparedNotices = new Dictionary<Place, List<string>>();
            if (selectedPlace != null) visible.Add(selectedPlace);
            foreach (Place place in places)
            {
                if (ReferenceEquals(place, selectedPlace)) continue;
                if (visible.Count >= 64) break;
                var live = Live(place);
                if (live != null && (MarkerVisible == null || MarkerVisible(live))) visible.Add(place);
            }
            // All shallow waiting notices precede optional cached one-shot reads.
            if (guide) foreach (Place place in visible)
            { if (slice.ShouldYield()) break; AddMarker(place, batch, snapshot, preparedNotices, true, false, null); }
            if (!oneShot) return snapshot;
            object config;
            try { object checkpoints = ExistingCheckpoints(); config = checkpoints == null ? null : Read(checkpoints, "_position"); }
            catch (Exception) { return snapshot; } // Missing research data never erases waiting membership.
            // Selected place first. No source preparation or result parsing is admitted here.
            foreach (Place place in visible)
            { if (slice.ShouldYield() || markerCandidates <= 0) break; AddMarker(place, batch, snapshot, preparedNotices, false, true, config); }
            return snapshot;
        }
        private static void AddMarker(Place place, MarkerBatch batch, MapMarkerSnapshot snapshot, Dictionary<Place, List<string>> preparedNotices, bool guide, bool oneShot, object config)
        {
            var controller = Live(place);
            if (controller == null || MarkerVisible != null && !MarkerVisible(controller)) return;
            List<string> added = null;
            Candidate result = TryCandidate(place, oneShot ? "marker/once" : "marker/waiting", () => {
                if (!ReferenceEquals(Read(controller, "_missionsManager"), batch.Manager) || !ReferenceEquals(Read(controller, "_gameStatManager"), batch.Player) || !ReferenceEquals(Read(controller, "_freeManager"), batch.Free)) return null;
                dispatch = MarkerDispatch(controller, batch); entryBarrier = null;
                var notices = new List<string>();
                MarkerPending pending = null;
                int position = Convert.ToInt32(Read(controller, "_position"));
                if (batch.MainStarted && Convert.ToInt32(batch.MainPosition) != 0)
                {
                    if (batch.MainStage != null && Convert.ToInt32(batch.MainPosition) == position && Convert.ToInt32(Read(batch.MainStage, "_position")) == position)
                        pending = new MarkerPending { Stage = batch.MainStage, Id = Name(Read(batch.Manager, "_main")), Main = true };
                }
                else batch.First.TryGetValue(position, out pending);
                if (guide && pending != null)
                {
                    Candidate mission = Mission(controller, batch.Manager, null, pending.Stage, pending.Id, pending.Main, true);
                    if (mission != null && AutomaticCandidate(mission, true, false)) notices.Add(MapGuidePresentation.Notice(mission.Name, pending.Main ? "주 임무" : "보조 임무", 0, true, true, mission.Status));
                }
                Source source = place.Definition;
                object data = Read(controller, "_positionData");
                if (oneShot && source != null && source.Failure == null && source.Routes != null && ReferenceEquals(data, place.Data) && ReferenceEquals(config, place.PositionConfig) && (string)Read(data, "_defaultScript") == place.Script)
                    foreach (EventDefinition entry in source.Events)
                    {
                        OneShotResult proof;
                        if (automaticEvents <= 0 || markerCandidates-- <= 0) break;
                        if (!OneShotProof.TryGetEvent(entry.Data, entry.Script, out proof) || !proof.Proven) continue;
                        Candidate candidate = Event(controller, source, entry, false, true);
                        if (candidate != null && AutomaticCandidate(candidate, false, true)) notices.Add(MapGuidePresentation.Notice(candidate.Name, "1회성 이야기", 0, false, false, candidate.Status));
                    }
                if (notices.Count == 0) return null;
                added = notices;
                return new Candidate { Name = Plain(LocationTranslations.TranslateLocation(((string)Read(data, "name") ?? "").Replace("養成_", ""))), Rank = 0,
                    Status = "확인된 기회 " + notices.Count };
            });
            if (result == null || result.StatusOnly || result.Rank != 0) return;
            List<string> combined;
            if (!preparedNotices.TryGetValue(place, out combined)) preparedNotices.Add(place, combined = new List<string>());
            combined.AddRange(added);
            var row = new InformationRow("map/marker/" + place.Id, result.Name, "지도", "확인된 기회 " + combined.Count,
                "현재 확인한 대기 임무와 준비된 1회성의 현재 도달성만 표시합니다. 미분석 장소의 전체 기회 없음을 뜻하지 않습니다.", MapGuidePresentation.Summary(combined, false, false), controller, true, true);
            for (int at = 0; at < snapshot.Entries.Count; at++)
                if (ReferenceEquals(snapshot.Entries[at].Anchor, controller)) { snapshot.Entries[at] = row; return; }
            snapshot.Entries.Add(row);
        }
    }
}
