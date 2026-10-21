using System;
using System.Collections.Generic;
using DiceMaster.LiveLog;
using Mortal.Free;

namespace DiceMaster.Information
{
    public static partial class MapGuideSource
    {
        // Only the selected place prepares/samples these paths. Global markers
        // never call this method or start downstream discovery. The one shared
        // source graph retains the existing work/time/resource limits.
        private static void CollectSelectedRoute(Place place, PositionController controller, Source source, List<Candidate> candidates)
        {
            if (!source.Routes.RootMenuComplete || source.Routes.HasReadFailure || source.Routes.StoryEntries.Count == 0) return;
            var dispatchProbe = new Candidate { Rank = 0, Status = "", Detail = "" };
            ApplyPlace(controller, dispatchProbe);
            if (dispatchProbe.Rank != 0) return;
            var read = MapRouteRead.ForSelectedPlace(Read(controller, "_positionData"));
            if (read == null) return;
            int paths = 32, gates = 32, kinds = 32;
            foreach (MapStoryEntry story in source.Routes.StoryEntries)
                foreach (MapRoutePath path in story.Paths)
                {
                    if (paths-- <= 0) return;
                    if (path.Steps.Count < 2 || string.IsNullOrEmpty(path.DialogueCharacter) ||
                        path.ReadStoryKind(read, ref kinds) == MapStoryKind.ActivityResponse ||
                        path.StoryKind == MapStoryKind.ActivityResponse && !path.ReadActivityIndependence(read, ref kinds).HasValue) continue;
                    // Entry-written inputs are masked and reset-owned inputs
                    // use their concrete serialized default only at that read.
                    // Every future native option must be proven unlocked.
                    if (path.ReadChoiceAvailability(read, ref gates) != MapRouteState.Met ||
                        !read.BudgetedCurrent(path.CurrentPrerequisite, ref gates, out PreviewConditionState state) ||
                        state == PreviewConditionState.NotMet || read.UnresolvedEntrySwitch(path.CurrentPrerequisite, ref gates)) continue;
                    string route = ChoiceStoryLabels.Path(path.Steps);
                    if (route.Length > 96) route = route.Substring(0, 95) + "…";
                    string target = ChoiceStoryLabels.DialogueTarget(path.DialogueCharacter);
                    candidates.Add(new Candidate { Id = "selected-route/" + story.Identity, Name = target, Kind = "연결 경로", ChoiceStory = true,
                        Rank = 2, Status = "진행 가능성 있음", RoutePreview = "경로 후보: " + route + " · " + target + " · 진행 가능성 있음",
                        Detail = "선택 경로: " + ChoiceStoryLabels.Path(path.Steps) + "\n각 선택지의 해금을 확인한 연결 경로입니다. 확인된 대화 초기화는 기본값으로 가상 조회하고, 장소 진입 시 쓸 수 있는 다른 플래그는 미확정으로 처리합니다. 실제 후속 대화 발생이나 1회성·기회 마커를 보장하지 않습니다." });
                    return;
                }
        }
    }
}
