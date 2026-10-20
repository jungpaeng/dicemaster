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
            internal Exception Failure;
            internal float RetryAt;
            internal bool Ready;
        }
        private sealed class EventDefinition
        {
            internal PositionEventData Data;
            internal string Script;
        }
        private sealed class ScriptDefinition
        {
            internal List<LuaAction> Actions;
            internal string Description;
            internal float RetryAt;
        }
        private sealed class Candidate
        {
            internal string Name, Kind, Status, Detail;
            internal int Rank; // possible, impossible, unknown; stable within each
            internal bool OneShot, StatusOnly;
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
        }
        private sealed class MissionIndex
        {
            internal object Collection, Entries, Main, MainStage;
            internal int Count;
            internal bool Ready;
            internal Exception Failure;
            internal float RetryAt;
            internal Dictionary<int, List<MissionLink>> Places = new Dictionary<int, List<MissionLink>>();
        }
        private static readonly Dictionary<object, MissionIndex> missionIndices = new Dictionary<object, MissionIndex>();
        private static readonly List<Place> places = new List<Place>();
        private static readonly Dictionary<string, ScriptDefinition> scripts = new Dictionary<string, ScriptDefinition>(StringComparer.Ordinal);
        private static readonly Regex tags = new Regex("<[^>]*>");
        private static int sequence, generation;
        private static bool hasClock;
        private static float lastClock;
        private static double elapsed;
        private static PositionController selected;
        public static Func<bool> Enabled;
        private static bool IsEnabled { get { try { return Enabled == null || Enabled(); } catch (Exception) { return false; } } }
        public static string ContextKey { get { return generation.ToString(CultureInfo.InvariantCulture) + "/" + (selected == null ? "" : selected.GetInstanceID().ToString(CultureInfo.InvariantCulture)); } }
        public static bool Visible
        {
            get
            {
                if (!IsEnabled) return false;
                foreach (Place place in places) if (Live(place) != null) return true;
                return false;
            }
        }
        private static PositionController Live(Place place)
        {
            var controller = place.Controller.Target as PositionController;
            return controller != null && controller.isActiveAndEnabled && controller.gameObject.activeInHierarchy ? controller : null;
        }
        public static void Register(PositionController controller)
        {
            if (controller == null) return;
            foreach (Place place in places) if (ReferenceEquals(place.Controller.Target, controller)) return;
            places.Add(new Place { Controller = new WeakReference(controller), Id = checked(++sequence) });
            generation++;
        }
        public static void Select(PositionController controller)
        {
            if (!IsEnabled) return;
            Register(controller);
            selected = controller;
        }
        public static void Deselect(PositionController controller)
        { if (ReferenceEquals(selected, controller)) selected = null; }
        public static void Clear()
        { places.Clear(); scripts.Clear(); missionIndices.Clear(); selected = null; hasClock = false; elapsed = 0; generation++; }
        public static void ClearSnapshots()
        {
            // Used for OFF: retain weak lifecycle handles, never parsed data.
            bool changed = scripts.Count != 0 || missionIndices.Count != 0 || selected != null;
            foreach (Place place in places)
            {
                changed |= place.Definition != null || place.Data != null || place.PositionConfig != null || place.Reads.Count != 0;
                place.Definition = null; place.Reads.Clear(); place.Data = place.PositionConfig = null;
            }
            scripts.Clear(); missionIndices.Clear(); selected = null; if (changed) generation++;
        }
        public static List<DisplayQuery> Discover(bool guide, bool oneShot)
        {
            var result = new List<DisplayQuery>();
            if (!IsEnabled || !guide && !oneShot) return result;
            for (int index = places.Count - 1; index >= 0; index--)
                if (places[index].Controller.Target == null || places[index].Controller.Target as PositionController == null) places.RemoveAt(index);
            foreach (Place place in places)
            {
                PositionController controller = Live(place);
                if (controller == null) continue;
                Place captured = place;
                result.Add(new DisplayQuery("map/" + place.Id, () => ReadCurrentPlace(captured, controller, guide, oneShot)));
            }
            return result;
        }
        private static object ReadCurrentPlace(Place place, PositionController controller, bool guide, bool oneShot)
        {
            if (Live(place) != controller) throw new DataNotReadyException();
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
        private static Source GetSource(Place place)
        {
            Source source = place.Definition;
            if (source == null) place.Definition = source = new Source { Script = place.Script };
            if (source.Ready || source.Failure != null && Now < source.RetryAt) return source;
            try
            {
                source.Events.Clear();
                var keys = new List<string>();
                if (!string.IsNullOrEmpty(source.Script))
                {
                    ScriptReadResult script = DataHelper.ReadPreviewScript(source.Script);
                    if (script.State != ScriptReadState.Success) throw script.State == ScriptReadState.Waiting ? new DataNotReadyException() : new InvalidOperationException("Map script unavailable.");
                    keys.AddRange(PositionEventCache.GetDefinitionParser().ExtractPositionWorkFromLua(source.Script, script.Text));
                }
                // The legacy place-key association is retained if the live
                // pool really has that exact name; it never guesses a prefix.
                if (place.PositionConfig != null)
                {
                    IList pools = List(place.PositionConfig, "_list");
                    foreach (object pool in pools)
                        if (pool != null && Name(pool) == place.Key && !keys.Contains(place.Key)) keys.Add(place.Key);
                    foreach (string key in keys)
                    {
                        bool found = false;
                        foreach (object pool in pools)
                            if (pool != null && Name(pool) == key)
                            {
                                found = true;
                                string prefix = (string)Read(pool, "_scriptPrefix");
                                foreach (object entry in List(pool, "_list"))
                                {
                                    var data = entry as PositionEventData;
                                    if (data == null) throw new InvalidOperationException("Unsupported map event definition.");
                                    source.Events.Add(new EventDefinition { Data = data, Script = prefix + "_" + data.ResultType });
                                }
                            }
                        if (!found) throw new DataNotReadyException();
                    }
                }
                else if (keys.Count > 0) throw new DataNotReadyException();
                if (source.Failure != null) DicePlugin.LogWarning("Information map source=" + place.Key + " recovered");
                source.Failure = null; source.Ready = true;
            }
            catch (Exception error)
            {
                if (source.Failure == null || source.Failure.GetType() != error.GetType())
                    DicePlugin.LogWarning("Information map source=" + place.Key + " failure type=" + error.GetType().FullName);
                source.Failure = error; source.RetryAt = Now + 5;
            }
            return source;
        }
        private static ScriptDefinition Effects(string script)
        {
            if (string.IsNullOrEmpty(script)) return new ScriptDefinition { Description = "연결된 결과 스크립트 없음" };
            ScriptDefinition definition;
            if (!scripts.TryGetValue(script, out definition)) { definition = new ScriptDefinition(); scripts.Add(script, definition); }
            if (definition.Description != null && (definition.RetryAt <= 0 || Now < definition.RetryAt)) return definition;
            if (definition.Actions == null) definition.Actions = PositionEventCache.GetDefinitionParser().ExtractActionsFromLua(script);
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
            return new Candidate { Name = state.Last == null ? "후보 자료" : state.Last.Name, Kind = state.Last == null ? "확인 불가" : state.Last.Kind,
                Rank = 2, Status = waiting ? "게임 데이터 대기 중" : state.Last == null ? "읽기 실패" : "갱신 실패 · 이전 값",
                Detail = state.Last == null ? "후보의 조건·결과를 읽지 못했습니다." : state.Last.Detail };
        }
        private static object ReadPlace(Place place, PositionController controller, bool guide, bool oneShot)
        {
            if (!IsEnabled || Live(place) != controller || !ReferenceEquals(Read(controller, "_positionData"), place.Data)) throw new DataNotReadyException();
            Source source = GetSource(place);
            var candidates = new List<Candidate>();
            foreach (EventDefinition entry in source.Events)
            {
                EventDefinition captured = entry;
                candidates.Add(TryCandidate(place, entry.Data, () => Event(controller, captured, guide, oneShot)));
            }
            // Direct fixed stories are candidates even when there is no
            // Position pool in that place's own script. Never scan other maps.
            if (guide && source.Ready && source.Events.Count == 0 && !string.IsNullOrEmpty(source.Script))
                candidates.Add(TryCandidate(place, source, () => Story(controller, source.Script)));
            object manager = guide ? Read(controller, "_missionsManager") : null;
            if (guide && manager != null)
            {
                MissionIndex index = GetMissionIndex(manager, place.Key);
                if (index.Failure != null)
                    candidates.Add(new Candidate { Name = "임무 자료", Kind = "임무", StatusOnly = true, Rank = 2,
                        Status = index.Failure is DataNotReadyException ? "게임 데이터 대기 중" : "읽기 실패",
                        Detail = "현재 장소에 연결된 임무 정의를 준비하지 못했습니다." });
                else
                {
                    List<MissionLink> related;
                    if (index.Places.TryGetValue(Convert.ToInt32(Read(controller, "_position")), out related))
                        foreach (MissionLink link in related) AddMission(place, controller, manager, link.Mission, link.Main, candidates);
                }
            }
            else if (guide) candidates.Add(new Candidate { Name = "임무 자료", Kind = "임무", StatusOnly = true, Rank = 2, Status = "게임 데이터 대기 중", Detail = "게임이 준비한 임무 관리자를 기다립니다." });
            if (source.Failure != null)
                candidates.Add(new Candidate { Name = place.Key, Kind = "이야기", StatusOnly = true, Rank = 2, Status = source.Failure is DataNotReadyException ? "게임 데이터 대기 중" : "읽기 실패", Detail = "이 장소의 연결된 이야기 자료를 읽지 못했습니다." });
            var ordered = new List<Candidate>();
            for (int rank = 0; rank < 3; rank++) foreach (Candidate candidate in candidates) if (candidate.Rank == rank) ordered.Add(candidate);
            int possible = 0, actualCount = 0; var names = new List<string>(); var hints = new List<string>(); var diagnostics = new List<string>(); var details = new StringBuilder();
            foreach (Candidate candidate in ordered)
            {
                if (!candidate.StatusOnly) actualCount++;
                else
                {
                    string diagnostic = candidate.Kind + " · " + candidate.Status;
                    if (!diagnostics.Contains(diagnostic)) diagnostics.Add(diagnostic);
                }
                if (!candidate.StatusOnly && candidate.Rank == 0) { possible++; names.Add(candidate.Name); }
                if (candidate.OneShot) hints.Add(candidate.Name + " · 1회성");
                details.AppendLine(candidate.Name + " · " + candidate.Kind + " · " + candidate.Status + (candidate.OneShot ? " · 1회성" : ""));
                details.AppendLine(candidate.Detail); details.AppendLine();
            }
            string title = LocationTranslations.TranslateLocation((place.Key ?? "").Replace("養成_", ""));
            string status = "후보 " + actualCount + " · 진행 가능 " + possible + (diagnostics.Count == 0 ? "" : " · " + string.Join(" · ", diagnostics));
            string auto = guide ? status + (names.Count == 0 ? "" : "\n" + string.Join(" · ", names)) : "";
            if (oneShot && hints.Count > 0) auto += (auto.Length == 0 ? "" : "\n") + string.Join(" · ", hints);
            string detail = "후보 수는 현재 인식한 관련 항목 수이며 실제 발생을 보장하지 않습니다.\n\n" + (ordered.Count == 0 ? "연결된 이야기·임무 후보 없음" : details.ToString());
            return new InformationRow("map/" + place.Id, Plain(title), "지도", status, detail, auto, controller, possible > 0);
        }
        private static Candidate Event(PositionController controller, EventDefinition entry, bool guide, bool oneShot)
        {
            if (!guide)
            {
                OneShotResult only = OneShotProof.ForEvent(entry.Data, entry.Script);
                if (only.Failed) throw new InvalidOperationException("Map one-shot read failed.");
                return new Candidate { Name = Plain(DataHelper.GetEventName(entry.Data)), Kind = "이야기", Rank = 2, Status = only.Status, Detail = only.Detail, OneShot = only.Proven };
            }
            EventEligibilitySnapshot state = EventEligibility.Evaluate(entry.Data);
            if (state.Reason == EventEligibilityReason.ReadFailure) throw new InvalidOperationException("Map event read failed.");
            var result = new Candidate { Name = Plain(DataHelper.GetEventName(entry.Data)), Kind = "이야기", Rank = state.IsEligible ? 0 : state.State == EventEligibilityState.Ineligible ? 1 : 2,
                Status = state.IsEligible ? "진행 가능" : state.State == EventEligibilityState.Ineligible ? "진행 불가" : "확인 불가", Detail = state.Detail };
            ApplyPlace(controller, false, result);
            result.Detail += "\n결과: " + Effects(entry.Script).Description;
            if (oneShot)
            {
                OneShotResult proof = OneShotProof.ForEvent(entry.Data, entry.Script);
                if (proof.Failed) throw new InvalidOperationException("Map one-shot read failed.");
                result.OneShot = proof.Proven;
                result.Detail += "\n" + proof.Status + ": " + proof.Detail;
            }
            return result;
        }
        private static Candidate Story(PositionController controller, string script)
        {
            var result = new Candidate { Name = script, Kind = "이야기", Rank = 0, Status = "진행 가능", Detail = "현재 장소의 고정 실행 스크립트입니다. 내부의 미지원·동적 경로는 발생을 보장하지 않습니다.\n결과: " + Effects(script).Description };
            ApplyPlace(controller, false, result); return result;
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
                if (index.Ready && ReferenceEquals(index.Collection, collection) && ReferenceEquals(index.Entries, entries) &&
                    index.Count == entries.Count && ReferenceEquals(index.Main, main) && ReferenceEquals(index.MainStage, mainStage)) return index;
                // Build immutable location membership from stage definitions
                // only. No unrelated mission's current key, live condition or
                // period is evaluated during index preparation or normal ticks.
                var byPlace = new Dictionary<int, List<MissionLink>>();
                if (main != null) IndexMission(byPlace, main, true, mainStage);
                foreach (object mission in entries) if (mission != null) IndexMission(byPlace, mission, false, null);
                index.Collection = collection; index.Entries = entries; index.Count = entries.Count;
                index.Main = main; index.MainStage = mainStage; index.Places = byPlace; index.Ready = true;
                if (index.Failure != null) DicePlugin.LogWarning("Information map mission-index target=" + location + " recovered");
                index.Failure = null;
            }
            catch (Exception error)
            {
                if (index.Failure == null || index.Failure.GetType() != error.GetType())
                    DicePlugin.LogWarning("Information map mission-index target=" + location + " failure type=" + error.GetType().FullName);
                index.Ready = false; index.Places.Clear(); index.Failure = error; index.RetryAt = Now + 5;
            }
            return index;
        }
        private static void IndexMission(Dictionary<int, List<MissionLink>> index, object mission, bool main, object currentMainStage)
        {
            var locations = new HashSet<int>();
            foreach (object stage in List(mission, "_missions"))
                if (stage != null) locations.Add(Convert.ToInt32(Read(stage, "_position")));
            if (currentMainStage != null) locations.Add(Convert.ToInt32(Read(currentMainStage, "_position")));
            foreach (int location in locations)
            {
                List<MissionLink> related;
                if (!index.TryGetValue(location, out related)) { related = new List<MissionLink>(); index.Add(location, related); }
                related.Add(new MissionLink { Mission = mission, Main = main });
            }
        }
        private static void AddMission(Place place, PositionController controller, object manager, object mission, bool main, List<Candidate> target)
        {
            // Isolate malformed stages without hiding other missions/events.
            string revision;
            try
            {
                object mainStage = main ? Read(manager, "MainCheckData") : null;
                revision = "mission/" + RuntimeHelpers.GetHashCode(mission) + "/" + Read(mission, "_currentKey") + "/" + (mainStage == null ? 0 : RuntimeHelpers.GetHashCode(mainStage));
            }
            catch (Exception) { revision = "mission/" + RuntimeHelpers.GetHashCode(mission) + "/unreadable"; }
            Candidate candidate = TryCandidate(place, revision, () => Mission(controller, manager, mission, main));
            if (candidate != null) target.Add(candidate);
        }
        private static Candidate Mission(PositionController controller, object manager, object mission, bool main)
        {
            string key = (string)Read(mission, "_currentKey");
            object stage = main && (bool)Read(manager, "MainMissionStart") ? Read(manager, "MainCheckData") : null;
            if (stage == null)
            {
                if (key == "clear") return null;
                foreach (object value in List(mission, "_missions")) if (value != null && (string)Read(value, "_key") == key) { stage = value; break; }
            }
            if (stage == null) throw new DataNotReadyException();
            if (!Equals(Read(stage, "_position"), Read(controller, "_position")) || string.IsNullOrEmpty((string)Read(stage, "_startScript"))) return null;
            string raw = Name(mission), stageName = Name(stage);
            var candidate = new Candidate { Name = DisplayNames.LookupName("Mission/Name/" + raw, raw) + " · " + DisplayNames.LookupName("Mission/CheckName/" + stageName, stageName), Kind = main ? "주 임무" : "임무", Rank = 0, Status = "진행 가능" };
            var detail = new StringBuilder();
            if (!(bool)Read(stage, "_active")) { candidate.Rank = 1; candidate.Status = "진행 불가 · 비활성"; }
            bool unknown = false;
            foreach (object condition in List(stage, "_conditions"))
            {
                detail.AppendLine(PopupConditionText.Condition(condition));
                try { if (!SafeDisplayEvaluation.CheckCondition(condition)) { candidate.Rank = 1; candidate.Status = "진행 불가 · 조건 미충족"; } }
                catch (RandomDisplayValueException) { unknown = true; }
                catch (UnsupportedDisplayStatException) { unknown = true; }
                catch (InvalidOperationException error)
                { if (error.Message.StartsWith("Unknown display", StringComparison.Ordinal)) unknown = true; else throw; }
            }
            if ((bool)Read(stage, "_dayCheck"))
            {
                object player = Read(controller, "_gameStatManager");
                object current = Read(Read(player, "_gameTimeData"), "GameTime");
                int type = Convert.ToInt32(Read(stage, "_timeCheckType"), CultureInfo.InvariantCulture);
                bool period = type == 1 ? CompareTime(current, Read(stage, "_specificTime")) == 0 : type == 2 ? CompareTime(current, Read(stage, "_minTime")) >= 0 && CompareTime(current, Read(stage, "_maxTime")) <= 0 : true;
                detail.AppendLine("기간: " + (type == 1 ? TimeText(Read(stage, "_specificTime")) : type == 2 ? TimeText(Read(stage, "_minTime")) + " ~ " + TimeText(Read(stage, "_maxTime")) : "게임의 주기 판정 설정"));
                if (!period) { candidate.Rank = 1; candidate.Status = "진행 불가 · 기간 밖"; }
            }
            if (unknown && candidate.Rank == 0) { candidate.Rank = 2; candidate.Status = "확인 불가 · 난수/미지원 조건"; }
            candidate.Detail = detail.ToString();
            ApplyPlace(controller, main, candidate);
            candidate.Detail += "\n결과: " + Effects((string)Read(stage, "_startScript")).Description;
            return candidate;
        }
        private static int CompareTime(object left, object right)
        {
            foreach (string field in new[] { "Year", "Month", "Stage" })
            { int compare = Convert.ToInt32(Read(left, field)).CompareTo(Convert.ToInt32(Read(right, field))); if (compare != 0) return compare; }
            return 0;
        }
        private static string TimeText(object time)
        { return Read(time, "Year") + "년 " + Read(time, "Month") + "월 " + DisplayNames.Text(Convert.ToString(Read(time, "Stage"))); }
        private static void ApplyPlace(PositionController controller, bool main, Candidate result)
        {
            if (!(bool)Read(controller, "_active")) { result.Rank = 1; result.Status = "진행 불가 · 장소 비활성"; return; }
            object free = Read(controller, "_freeManager");
            if (free == null) throw new DataNotReadyException();
            if ((bool)Read(free, "IsProcessing")) { result.Rank = 1; result.Status = "진행 불가 · 행동 진행 중"; return; }
            object missions = Read(controller, "_missionsManager");
            bool mainStarted = (bool)Read(missions, "MainMissionStart");
            object mainPosition = Read(missions, "MainMissionPosition");
            if (mainStarted && Convert.ToInt32(mainPosition) != 0)
            {
                if (!main || !Equals(mainPosition, Read(controller, "_position"))) { result.Rank = 1; result.Status = "진행 불가 · 주 임무 우선"; }
                else { result.Rank = 0; result.Status = "진행 가능 · 주 임무 대기"; }
                return;
            }
            object player = Read(controller, "_gameStatManager");
            object action = null;
            foreach (object stat in List(Read(player, "_stats"), "_list"))
                if (stat != null && Convert.ToString(Read(stat, "_type")) == "行動次數") { action = stat; break; }
            if (action == null) throw new DataNotReadyException();
            int value = SafeDisplayEvaluation.ReadStatFinalValue(action);
            int cost = Convert.ToInt32(Read(controller, "_costAction"));
            result.Detail += "\n행동: " + value + " / 필요 " + cost;
            if (value < cost) { result.Rank = 1; result.Status = "진행 불가 · 행동 부족"; return; }
            if (result.Kind == "이야기")
            {
                var activeMissions = Read(missions, "ActiveSubMissions") as IDictionary;
                if (activeMissions != null)
                    foreach (DictionaryEntry item in activeMissions)
                        if (item.Value != null && Equals(Read(item.Value, "_position"), Read(controller, "_position")) && !string.IsNullOrEmpty((string)Read(item.Value, "_startScript")))
                        { result.Rank = 1; result.Status = "진행 불가 · 임무 우선"; break; }
            }
        }
    }
}
