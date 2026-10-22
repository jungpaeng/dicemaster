using System;
using System.Collections.Generic;
using Mortal.Free;

namespace DiceMaster.Information
{
    public static partial class MapGuideSource
    {
        private static int backgroundPrepareCursor, backgroundReadCursor;
        // One screen-wide preparation slot (shared with the selected place),
        // never one full preparation budget per visible place. Positive
        // witnesses validate their observed inputs before optional preparation.
        private static void CollectGuidanceMarkers(List<Place> visible, MarkerBatch batch, MapMarkerSnapshot snapshot, MapRouteDefinitions.PreparationSlice slice)
        {
            if (visible.Count == 0) return;
            guidanceBudget = new GuidanceBudget { Paths = 64, Gates = 128, Kinds = 128, Index = 128 };
            guidancePreferredOnly = true;
            int start = (backgroundReadCursor++ & int.MaxValue) % visible.Count;
            // Mandatory bounded input checks must precede optional cold work.
            // Expiring its deadline must not erase an unchanged visible badge.
            for (int offset = 0; offset < visible.Count; offset++)
                AddGuidanceMarker(visible[(start + offset) % visible.Count], batch, snapshot, false);

            // Round-robin cold work cannot repeatedly restart from place zero.
            // Selected preparation already consumed this slot when it needed it.
            if (!routeAdvanced && !slice.ShouldYield())
                for (int offset = 0; offset < visible.Count; offset++)
                {
                    int at = (backgroundPrepareCursor++ & int.MaxValue) % visible.Count;
                    Place place = visible[at];
                    if (HasMarker(snapshot, place) || !CanReadGuidance(place, batch, out _)) continue;
                    Source source = place.Definition;
                    if (source != null && source.Routes != null && !source.Routes.HasPendingWork && source.Failure == null) continue;
                    AddGuidanceMarker(place, batch, snapshot, true);
                    break;
                }
            guidancePreferredOnly = false;
            for (int offset = 0; offset < visible.Count && !slice.ShouldYield() && guidanceBudget.Paths > 0; offset++)
                AddGuidanceMarker(visible[(start + offset) % visible.Count], batch, snapshot, false);
            // This is the SAME publication as the badges. Absence alone never
            // means pending. Completed/unsupported/failed queries stop loading.
            foreach (Place place in visible)
                if (!HasMarker(snapshot, place) && GuidancePreparing(place)) snapshot.Preparing.Add(Live(place));
        }
        private static bool GuidancePreparing(Place place)
        {
            if (!place.GuidanceReadable || string.IsNullOrEmpty(place.Script)) return false;
            Source source = place.Definition;
            if (source == null) return true; // queued under the shared budget
            if (source.Failure != null) return false;
            var routes = source.Routes;
            if (routes == null || routes.HasReadFailure) return false;
            if (routes.IsPreparing) return true;
            var scan = source.SelectedStories;
            return scan.Indexed < routes.StoryPaths.Count || scan.Checked < scan.Candidates.Count ||
                scan.Candidates.Count > 0 && MapDefinitionAssets.IsPreparing;
        }
        private static bool HasMarker(MapMarkerSnapshot snapshot, Place place)
        {
            var controller = Live(place);
            foreach (var row in snapshot.Entries) if (ReferenceEquals(row.Anchor, controller)) return true;
            return false;
        }
        private static bool CanReadGuidance(Place place, MarkerBatch batch, out PositionController controller)
        {
            controller = Live(place);
            try
            {
                // Visibility was sampled when building this query's visible list.
                // Rendering independently validates live geometry before drawing.
                if (controller == null ||
                    !ReferenceEquals(Read(controller, "_missionsManager"), batch.Manager) ||
                    !ReferenceEquals(Read(controller, "_gameStatManager"), batch.Player) ||
                    !ReferenceEquals(Read(controller, "_freeManager"), batch.Free)) return false;
                return true;
            }
            catch (Exception) { return false; }
        }
        private static void AddGuidanceMarker(Place place, MarkerBatch batch, MapMarkerSnapshot snapshot, bool prepare)
        {
            if (HasMarker(snapshot, place)) return;
            place.GuidanceReadable = false;
            InformationRow row = null;
            TryCandidate(place, "marker/guidance", () => {
                PositionController controller;
                if (!CanReadGuidance(place, batch, out controller)) return null;
                UpdatePlaceMetadata(place, controller, false);
                dispatch = MarkerDispatch(controller, batch); entryBarrier = null;
                var probe = new Candidate { Rank = 0, Status = "", Detail = "" };
                ApplyPlace(controller, probe);
                if (probe.Rank != 0) return null;
                place.GuidanceReadable = true;
                Source source = prepare ? GetSource(place) : place.Definition;
                if (source == null || source.Failure != null || source.Routes == null) return null;
                var candidates = new List<Candidate>(1);
                CollectSelectedRoute(place, controller, source, candidates);
                if (candidates.Count == 0) return null;
                Candidate route = candidates[0];
                var cached = source.SelectedStories;
                if (cached.Marker != null && ReferenceEquals(cached.MarkerRoute, route))
                { row = cached.Marker; return route; }
                string title = Plain(LocationTranslations.TranslateLocation((place.Key ?? "").Replace("養成_", "")));
                row = new InformationRow("map/guidance/" + place.Id, title, "연결 안내", "진행 가능성 있음", route.Detail,
                    MapGuidePresentation.Summary(new[] { route.RoutePreview }, false, false), controller, false, false, true);
                cached.Marker = row; cached.MarkerRoute = route;
                return route;
            });
            if (row != null) snapshot.Entries.Add(row);
        }
    }
}
