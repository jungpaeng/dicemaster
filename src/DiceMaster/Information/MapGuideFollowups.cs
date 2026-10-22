using System;
using System.Collections.Generic;
using DiceMaster.LiveLog;
using Mortal.Free;

namespace DiceMaster.Information
{
    public static partial class MapGuideSource
    {
        // Selected-place and visible-place guidance use identical predicates.
        // The background caller supplies a shared, bounded screen-wide budget.
        private static void CollectSelectedRoute(Place place, PositionController controller, Source source, List<Candidate> candidates)
        {
            // Each witness still proves its own closed choices, completion,
            // entry guards and activity independence. A failed unrelated pool
            // is not evidence against this already observed dialogue path.
            if (!source.Routes.RootMenuComplete || source.Routes.StoryEntries.Count == 0) return;
            var dispatchProbe = new Candidate { Rank = 0, Status = "", Detail = "" };
            ApplyPlace(controller, dispatchProbe);
            if (dispatchProbe.Rank != 0) return;
            var scan = source.SelectedStories;
            if (guidancePreferredOnly && scan.Preferred == null) return;
            // Verify only the successful witness's observed inputs. An
            // unchanged witness needs no graph walk or shared search slots.
            if (scan.Preferred != null && scan.Stamp != null)
            {
                if (scan.Stamp.IsCurrent()) { AddSelectedRoute(scan.Preferred, candidates); return; }
                scan.Checked = 0;
            }
            scan.Stamp = null;
            using (var stamp = new PreviewConditions.ReadStamp())
                CollectUncachedRoute(controller, source, candidates, scan, stamp);
        }
        private static void CollectUncachedRoute(PositionController controller, Source source, List<Candidate> candidates,
            SelectedStoryScan scan, PreviewConditions.ReadStamp stamp)
        {
            var budget = guidanceBudget ?? new GuidanceBudget();
            if (budget.Paths <= 0 || budget.Gates <= 0 || budget.Kinds <= 0) return;
            // Index only new immutable witnesses, including later paths added
            // to an earlier story. Non-dialogue definitions use no read slots.
            var definitions = source.Routes.StoryPaths;
            int indexBudget = Math.Min(64, budget.Index);
            while (!guidancePreferredOnly && scan.Indexed < definitions.Count && indexBudget-- > 0 && budget.Index-- > 0)
            {
                var item = definitions[scan.Indexed++];
                if (item.Path.Steps.Count != 0 && !string.IsNullOrEmpty(item.Path.DialogueCharacter))
                    scan.Candidates.Add(new StoryLink { Story = item.Story, Path = item.Path });
            }
            if (scan.Candidates.Count == 0) return;
            var read = MapRouteRead.ForSelectedPlace(Read(controller, "_positionData"), guidanceDefinitions);
            if (read == null) return;
            int paths = Math.Min(32, budget.Paths);
            StoryLink previous = scan.Preferred;
            if (previous != null)
            {
                paths--; budget.Paths--;
                if (previous.Path.ReadGuidance(read, -1, true, true, ref budget.Gates, ref budget.Kinds, out _) == MapGuidanceRejection.None)
                { scan.Stamp = stamp; AddSelectedRoute(previous, candidates); return; }
                if (budget.Gates < 0 || budget.Kinds < 0) return;
                scan.Preferred = null;
            }
            if (guidancePreferredOnly) return;
            int remaining = Math.Min(paths, scan.Candidates.Count);
            bool isolated = previous == null;
            while (remaining-- > 0 && budget.Paths > 0 && budget.Gates > 0 && budget.Kinds > 0)
            {
                int at = scan.Cursor % scan.Candidates.Count;
                StoryLink link = scan.Candidates[at];
                scan.Cursor = (at + 1) % scan.Candidates.Count;
                if (ReferenceEquals(link, previous)) continue;
                budget.Paths--;
                int priorGates = budget.Gates, priorKinds = budget.Kinds;
                if (link.Path.ReadGuidance(read, -1, true, true, ref budget.Gates, ref budget.Kinds, out _) == MapGuidanceRejection.None)
                {
                    scan.Preferred = link;
                    // A search prefix also observed unrelated locked paths.
                    // Capture just this witness on its next preferred read.
                    scan.Stamp = isolated ? stamp : null;
                    AddSelectedRoute(link, candidates);
                    return;
                }
                isolated = false;
                // Do not strand a valid tail at the end of each gate budget.
                // Retry with a fresh budget; a path exceeding a whole budget
                // is skipped for this sweep so it cannot starve later paths.
                if (budget.Gates < 0 || budget.Kinds < 0)
                {
                    if (priorGates < 32 || priorKinds < 32) scan.Cursor = at;
                    else if (at == scan.Checked) scan.Checked++;
                    break;
                }
                if (at == scan.Checked) scan.Checked++;
                if (preparationSlice.ShouldYield()) break;
            }
        }
        private static void AddSelectedRoute(StoryLink link, List<Candidate> candidates)
        {
            if (link.Notice != null) { candidates.Add(link.Notice); return; }
            string route = ChoiceStoryLabels.Path(link.Path.Steps);
            if (route.Length > 96) route = route.Substring(0, 95) + "…";
            string target = ChoiceStoryLabels.DialogueTarget(link.Path.DialogueCharacter);
            candidates.Add(link.Notice = new Candidate { Id = "selected-route/" + link.Story.Identity, Name = target, Kind = "연결 경로", ChoiceStory = true,
                Rank = 2, Status = "진행 가능성 있음", RoutePreview = "경로 후보: " + route + " · " + target + " · 진행 가능성 있음",
                Detail = "선택 경로: " + ChoiceStoryLabels.Path(link.Path.Steps) + "\n선택 화면과 같은 해금·완료·반복 활동 기준으로 확인한 이야기 경로입니다. 초회 인사도 포함하며 확인한 경로 하나를 안내합니다. 현재 게임 Lua 연결에서 확인한 대화 초기화 기본값과 안전하게 판독한 진입 플래그를 가상 조회합니다. 미확인 자료는 보류하며 실제 후속 대화 발생이나 1회성·기회 마커를 보장하지 않습니다." });
        }
    }
}
