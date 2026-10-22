using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using DiceMaster.EventSelection;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Story;

namespace DiceMaster.Information
{
    public static partial class ChoiceStorySource
    {
        internal sealed class PendingChoice { internal string Name, Kind; }
        private static readonly FieldInfo waitingInstance = typeof(MissionManagerData).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly Dictionary<MapRouteEntry, string> eventNames = new Dictionary<MapRouteEntry, string>();
        private static int opportunityWork = -1;
        private static int opportunityKind, opportunityEntry, opportunityPath, opportunityStep;
        private static bool opportunityIndexed;
        private static bool Feature(Func<bool> value)
        { try { return value == null || value(); } catch (Exception) { return false; } }
        private static bool Interactable(object control)
        { try { return Interactive == null || Interactive(control); } catch (Exception) { return false; } }
        private static string ChoiceIdentity(MapChoiceStep step)
        { return step.Script + "/" + step.SourceOffset + "/" + step.Index; }
        private static bool PathMet(IList<MapRoutePath> paths, Option option, Binding binding, MapRouteRead current, ref int choiceBudget)
        {
            if (binding.Nodes.Count != 1 || paths.Count > 32) return false;
            foreach (MapRoutePath path in paths)
            {
                if (path.Uncertain) continue;
                bool belongs = false;
                foreach (MapChoiceStep step in path.Steps)
                    if (step.Script == script && ChoiceStoryLabels.Raw(step.RawLabel) == option.Raw && binding.Nodes.ContainsKey(ChoiceIdentity(step))) { belongs = true; break; }
                int selected = CurrentStep(path, option.Raw);
                if (belongs && selected >= 0 && current.Current(path.Gate) == PreviewConditionState.Met &&
                    path.ReadChoiceAvailability(current, ref choiceBudget, selected) == MapRouteState.Met) return true;
            }
            return false;
        }
        private static bool IndexOpportunities()
        {
            if (routes.IsPreparing || routes.HasReadFailure || routes.HasIncompleteOpportunityPaths) return false;
            if (opportunityWork != routes.WorkCount)
            {
                foreach (Binding binding in bindings.Values) { binding.Events.Clear(); binding.Scripts.Clear(); }
                opportunityWork = routes.WorkCount; opportunityIndexed = false;
                opportunityKind = opportunityEntry = opportunityPath = opportunityStep = 0;
            }
            if (opportunityIndexed) return true;
            int budget = 64;
            // Definition-only links yield after 64 cells, then resume. No
            // completed path index is rebuilt on healthy refreshes.
            while (opportunityKind < 2 && budget-- > 0)
            {
                int count = opportunityKind == 0 ? routes.Entries.Count : routes.ScriptEntries.Count;
                if (opportunityEntry >= count) { opportunityKind++; opportunityEntry = opportunityPath = opportunityStep = 0; continue; }
                IList<MapRoutePath> paths = opportunityKind == 0 ? routes.Entries[opportunityEntry].Paths : routes.ScriptEntries[opportunityEntry].Paths;
                if (opportunityPath >= paths.Count) { opportunityEntry++; opportunityPath = opportunityStep = 0; continue; }
                var steps = paths[opportunityPath].Steps;
                if (opportunityStep >= steps.Count) { opportunityPath++; opportunityStep = 0; continue; }
                var step = steps[opportunityStep++];
                if (step.Script != script) continue;
                var binding = Bind(step);
                if (opportunityKind == 0) { var entry = routes.Entries[opportunityEntry]; if (!binding.Events.Contains(entry)) binding.Events.Add(entry); }
                else { var entry = routes.ScriptEntries[opportunityEntry]; if (!binding.Scripts.Contains(entry)) binding.Scripts.Add(entry); }
            }
            return opportunityIndexed = opportunityKind == 2;
        }
        internal static Dictionary<string, PendingChoice> WaitingScripts()
        {
            var result = new Dictionary<string, PendingChoice>(StringComparer.Ordinal);
            object manager = waitingInstance == null ? null : waitingInstance.GetValue(null);
            if (manager == null) return result; // Never invoke a singleton getter/scene scan.
            if ((bool)Field(manager, "MainMissionStart")) AddWaiting(result, Field(manager, "MainCheckData"), "주 임무");
            var active = Field(manager, "ActiveSubMissions") as IDictionary;
            if (active == null) return result;
            int remaining = 256;
            foreach (DictionaryEntry pair in active)
            { if (remaining-- <= 0) break; AddWaiting(result, pair.Value, "보조 임무"); }
            return result;
        }
        private static void AddWaiting(Dictionary<string, PendingChoice> result, object stage, string kind)
        {
            if (stage == null) return;
            string target = Field(stage, "_startScript") as string;
            if (string.IsNullOrEmpty(target) || result.ContainsKey(target)) return;
            var named = stage as UnityEngine.Object;
            string raw = named == null ? target : named.name;
            result.Add(target, new PendingChoice { Name = DisplayNames.LookupName("Mission/CheckName/" + raw, raw), Kind = kind });
        }
        private static void AppendOpportunities(ChoiceStorySnapshot snapshot, MapRouteRead current, bool indexing, bool details, MapRouteDefinitions.PreparationSlice slice, ref int choiceBudget)
        {
            if (indexing || !IndexOpportunities())
            {
                if (details) snapshot.Entries.Add(new InformationRow("choice/opportunity/status/" + snapshot.ContextKey, "기회 표식 판정", "자료 상태", "표식 미확정",
                    "현재 메뉴의 선택 위치·연결·선행 변경을 전부 확인하지 못해 붉은 점을 붙이지 않습니다.\n" + routes.Diagnostic, ""));
                return;
            }
            bool guide = Feature(GuideEnabled), once = Feature(OneShotEnabled);
            Dictionary<string, PendingChoice> waiting = guide && routes.ScriptEntries.Count != 0 ? WaitingScripts() : null;
            int candidates = 64, events = 16, coldProofs = 1;
            foreach (Option option in options)
            {
                if (slice.ShouldYield() || candidates <= 0) break;
                Binding binding;
                if (option.Raw == null || option.Context != Context || !ReferenceEquals(option.Menu, snapshot.Menu) || !Live(option.Control.Target, snapshot.Menu) || !Interactable(option.Control.Target) ||
                    !bindings.TryGetValue(option.Raw, out binding) || binding.Nodes.Count != 1) continue;
                var notices = new List<string>(); var proofDetails = new List<string>();
                if (waiting != null) foreach (MapScriptEntry target in binding.Scripts)
                {
                    if (candidates-- <= 0) break;
                    PendingChoice pending;
                    if (!waiting.TryGetValue(target.Script, out pending) || !PathMet(target.Paths, option, binding, current, ref choiceBudget))
                    { if (details) proofDetails.Add("대기 임무의 현재 등록·이 선택의 실행 경로를 함께 확정하지 못했습니다: " + target.Script); continue; }
                    notices.Add(ChoiceStoryLabels.ShortName(pending.Name) + " · " + pending.Kind + " · 진행 가능");
                    if (details) proofDetails.Add("실제 현재 대기 목록의 시작 스크립트와 이 선택의 확정 연결이 일치합니다: " + target.Script + "\n" + Paths(target.Paths, option, current, ref choiceBudget));
                }
                if (once) foreach (MapRouteEntry entry in binding.Events)
                {
                    if (candidates-- <= 0 || events <= 0 || slice.ShouldYield()) break;
                    if (!PathMet(entry.Paths, option, binding, current, ref choiceBudget) || CanReadEvent == null || !CanReadEvent(entry.Event))
                    { if (details) proofDetails.Add("이 선택의 현재 도달 경로·조회 범위를 확정하지 못했습니다.\n" + Paths(entry.Paths, option, current, ref choiceBudget)); continue; }
                    events--;
                    OneShotResult proof;
                    if (!OneShotProof.TryGetEvent(entry.Event, entry.ResultScript, out proof))
                    {
                        if (coldProofs <= 0 || !routes.CanBuildBoundedProof(entry.ResultScript))
                        { if (details) proofDetails.Add("1회성 증명이 준비되지 않았거나 제한된 자동 결과 분석 범위 밖입니다: " + entry.ResultScript); continue; }
                        coldProofs--; proof = OneShotProof.ForEvent(entry.Event, entry.ResultScript);
                    }
                    if (proof.Failed) throw new InvalidOperationException("Choice one-shot proof read failed.");
                    if (!proof.Proven) { if (details) proofDetails.Add(proof.DetailText); continue; }
                    var eligibility = EventEligibility.EvaluateCurrent(entry.Event);
                    if (eligibility.Reason == EventEligibilityReason.ReadFailure) throw new InvalidOperationException("Choice event state read failed.");
                    if (!eligibility.IsEligible) { if (details) proofDetails.Add("현재 이벤트 조건·가중치: " + eligibility.Summary + "\n" + Paths(entry.Paths, option, current, ref choiceBudget)); continue; }
                    string name;
                    if (!eventNames.TryGetValue(entry, out name)) eventNames.Add(entry, name = DataHelper.GetEventName((PositionEventData)entry.Event));
                    notices.Add(ChoiceStoryLabels.ShortName(name) + " · 1회성 이야기 · 선택 후 진행 가능");
                    if (details) proofDetails.Add(proof.DetailText + "\n현재 조건·경로 충족. 실제 추첨 결과를 예측하거나 바꾸지 않습니다.\n" + Paths(entry.Paths, option, current, ref choiceBudget));
                }
                if (notices.Count == 0)
                {
                    if (details && proofDetails.Count != 0) snapshot.Entries.Add(new InformationRow("choice/research/" + snapshot.ContextKey + "/" + ((UnityEngine.Object)option.Control.Target).GetInstanceID(),
                        option.Title, "기회 표식 검토", "확정 표식 없음", string.Join("\n", proofDetails), ""));
                    continue;
                }
                int existing = snapshot.Entries.FindIndex(row => ReferenceEquals(row.Anchor, option.Control.Target));
                InformationRow old = existing < 0 ? null : snapshot.Entries[existing];
                string automatic = string.Join("\n• ", notices.GetRange(0, Math.Min(3, notices.Count)));
                if (notices.Count > 3) automatic += "\n외 " + (notices.Count - 3) + "개 · 상세에서 확인";
                if (old != null && !string.IsNullOrEmpty(old.AutoText)) automatic += "\n• 연결 안내: " + old.AutoText;
                var row = new InformationRow(old == null ? "choice/" + snapshot.ContextKey + "/" + ((UnityEngine.Object)option.Control.Target).GetInstanceID() : old.Id, option.Title,
                    "현재 기회", "진행 가능", (old == null ? "" : old.Detail + "\n") + string.Join("\n", proofDetails) +
                    "\n붉은 점은 실제 등록된 대기 임무의 확정 연결 또는 증명된 1회성과 현재 도달성에만 표시합니다. 일반 대화 연결만으로 표시하지 않습니다.",
                    automatic, option.Control.Target, true, true);
                if (existing < 0) snapshot.Entries.Add(row); else snapshot.Entries[existing] = row;
            }
        }
        private static string Paths(IList<MapRoutePath> paths, Option option, MapRouteRead current, ref int choiceBudget)
        {
            var lines = new List<string>(); int remaining = 32;
            foreach (var path in paths)
            {
                if (remaining-- <= 0) break;
                int selected = CurrentStep(path, option.Raw);
                if (selected >= 0 && path.ReadChoiceAvailability(current, ref choiceBudget, selected) == MapRouteState.Met)
                    lines.Add("연결 경로: " + ChoiceStoryLabels.Path(path.Steps));
            }
            return lines.Count == 0 ? "현재 안내할 해금 확인 경로가 없습니다." : string.Join("\n", lines);
        }
    }
}
