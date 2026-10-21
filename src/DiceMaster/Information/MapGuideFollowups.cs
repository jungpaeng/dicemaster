using System;
using System.Collections.Generic;
using Mortal.Free;

namespace DiceMaster.Information
{
    public static partial class MapGuideSource
    {
        // Only the selected place prepares/samples these paths. Global markers
        // never call this method or start downstream discovery.
        private static void CollectSelectedRoute(Place place, PositionController controller, Source source, List<Candidate> candidates)
        {
            if (!source.Routes.RootMenuComplete || source.Routes.HasReadFailure || source.Routes.StoryEntries.Count == 0) return;
            var dispatchProbe = new Candidate { Rank = 0, Status = "", Detail = "" };
            ApplyPlace(controller, dispatchProbe);
            if (dispatchProbe.Rank != 0) return;
            var scan = source.SelectedStories;
            // Index only new immutable witnesses, including later paths added
            // to an earlier story. Non-dialogue definitions use no read slots.
            var definitions = source.Routes.StoryPaths;
            int indexBudget = 64;
            while (scan.Indexed < definitions.Count && indexBudget-- > 0)
            {
                var item = definitions[scan.Indexed++];
                if (item.Path.Steps.Count != 0 && !string.IsNullOrEmpty(item.Path.DialogueCharacter))
                    scan.Candidates.Add(new StoryLink { Story = item.Story, Path = item.Path });
            }
            if (scan.Candidates.Count == 0) return;
            var read = MapRouteRead.ForSelectedPlace(Read(controller, "_positionData"));
            if (read == null) return;
            int paths = 32, gates = 32, kinds = 32;
            StoryLink previous = scan.Preferred;
            if (previous != null)
            {
                paths--;
                if (previous.Path.ReadGuidance(read, -1, true, true, ref gates, ref kinds, out _) == MapGuidanceRejection.None)
                { AddSelectedRoute(previous, candidates); return; }
                scan.Preferred = null;
            }
            int remaining = Math.Min(paths, scan.Candidates.Count);
            while (remaining-- > 0 && gates > 0 && kinds > 0)
            {
                int at = scan.Cursor % scan.Candidates.Count;
                StoryLink link = scan.Candidates[at];
                scan.Cursor = (at + 1) % scan.Candidates.Count;
                if (ReferenceEquals(link, previous)) continue;
                int priorGates = gates, priorKinds = kinds;
                if (link.Path.ReadGuidance(read, -1, true, true, ref gates, ref kinds, out _) == MapGuidanceRejection.None)
                {
                    scan.Preferred = link;
                    AddSelectedRoute(link, candidates);
                    return;
                }
                // Do not strand a valid tail at the end of each gate budget.
                // Retry with a fresh budget; a path exceeding a whole budget
                // is skipped for this sweep so it cannot starve later paths.
                if (gates < 0 || kinds < 0)
                {
                    if (priorGates < 32 || priorKinds < 32) scan.Cursor = at;
                    break;
                }
                if (preparationSlice.ShouldYield()) break;
            }
        }
        private static void AddSelectedRoute(StoryLink link, List<Candidate> candidates)
        {
            string route = ChoiceStoryLabels.Path(link.Path.Steps);
            if (route.Length > 96) route = route.Substring(0, 95) + "…";
            string target = ChoiceStoryLabels.DialogueTarget(link.Path.DialogueCharacter);
            candidates.Add(new Candidate { Id = "selected-route/" + link.Story.Identity, Name = target, Kind = "연결 경로", ChoiceStory = true,
                Rank = 2, Status = "진행 가능성 있음", RoutePreview = "경로 후보: " + route + " · " + target + " · 진행 가능성 있음",
                Detail = "선택 경로: " + ChoiceStoryLabels.Path(link.Path.Steps) + "\n선택 화면과 같은 해금·완료·반복 활동 기준으로 확인한 이야기 경로입니다. 초회 인사도 포함하며 확인한 경로 하나를 안내합니다. 확인된 대화 초기화는 기본값으로 가상 조회하고, 장소 진입 시 쓸 수 있는 다른 플래그는 미확정으로 처리합니다. 실제 후속 대화 발생이나 1회성·기회 마커를 보장하지 않습니다." });
        }
    }
}
