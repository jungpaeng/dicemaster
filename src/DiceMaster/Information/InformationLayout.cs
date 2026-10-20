using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceMaster.Information
{
    // Pure passive-label placement. No GUI/input/configuration is read here.
    // The candidate grid contains every intersection-area breakpoint: between
    // two breakpoints total overlap is bilinear, so its minimum occurs on a
    // grid corner. This also finds a free fitting region when one exists.
    public static class InformationLayout
    {
        private struct Bounds
        {
            internal double Left, Top, Right, Bottom;
        }

        public static Rect Place(Rect preferred, Rect viewport, IList<Rect> occupied)
        {
            double left = Coordinate(viewport.x, 0), top = Coordinate(viewport.y, 0);
            double extentX = Extent(viewport.width), extentY = Extent(viewport.height);
            // Keep even extreme finite inputs representable as a Unity Rect.
            extentX = Math.Min(extentX, (double)float.MaxValue - left);
            extentY = Math.Min(extentY, (double)float.MaxValue - top);
            double right = left + extentX, bottom = top + extentY;
            float width = (float)Size(preferred.width, extentX);
            float height = (float)Size(preferred.height, extentY);
            double maxX = Math.Max(left, right - width), maxY = Math.Max(top, bottom - height);
            float preferredX = Position(Coordinate(preferred.x, left), left, maxX);
            float preferredY = Position(Coordinate(preferred.y, top), top, maxY);
            Rect best = new Rect(preferredX, preferredY, width, height);
            if (width <= 0 || height <= 0 || occupied == null || occupied.Count == 0) return best;

            var obstacles = new List<Bounds>();
            for (int index = 0; index < occupied.Count; index++)
            {
                Rect value = occupied[index];
                if (!Finite(value.x) || !Finite(value.y) || !Finite(value.width) || !Finite(value.height) ||
                    value.width <= 0 || value.height <= 0) continue;
                var clipped = new Bounds {
                    Left = Math.Max(left, value.x), Top = Math.Max(top, value.y),
                    Right = Math.Min(right, (double)value.x + value.width),
                    Bottom = Math.Min(bottom, (double)value.y + value.height)
                };
                if (clipped.Right > clipped.Left && clipped.Bottom > clipped.Top) obstacles.Add(clipped);
            }
            double bestOverlap = Overlap(best, obstacles), bestDistance = 0;
            if (bestOverlap == 0) return best;

            var xs = new List<float>(); var ys = new List<float>();
            Add(xs, preferredX, left, maxX); Add(xs, left, left, maxX); Add(xs, maxX, left, maxX);
            Add(ys, preferredY, top, maxY); Add(ys, top, top, maxY); Add(ys, maxY, top, maxY);
            foreach (Bounds obstacle in obstacles)
            {
                // Both outside edges find non-overlapping left/right/above/
                // below placements. Both inside edges are also needed for the
                // minimum-overlap fallback when the screen has no free fit.
                Add(xs, obstacle.Left - width, left, maxX); Add(xs, obstacle.Left, left, maxX);
                Add(xs, obstacle.Right - width, left, maxX); Add(xs, obstacle.Right, left, maxX);
                Add(ys, obstacle.Top - height, top, maxY); Add(ys, obstacle.Top, top, maxY);
                Add(ys, obstacle.Bottom - height, top, maxY); Add(ys, obstacle.Bottom, top, maxY);
            }
            // Nearby coordinates are tried first so ordinary free placements
            // finish quickly. Tie ordering does not depend on obstacle order.
            Sort(xs, preferredX); Sort(ys, preferredY);
            foreach (float y in ys)
                foreach (float x in xs)
                {
                    Rect candidate = new Rect(x, y, width, height);
                    double overlap = Overlap(candidate, obstacles);
                    if (overlap == 0) return candidate;
                    double dx = (double)x - preferredX, dy = (double)y - preferredY;
                    double distance = dx * dx + dy * dy;
                    if (overlap < bestOverlap || overlap == bestOverlap &&
                        (distance < bestDistance || distance == bestDistance &&
                        (candidate.y < best.y || candidate.y == best.y && candidate.x < best.x)))
                    { best = candidate; bestOverlap = overlap; bestDistance = distance; }
                }
            return best;
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static double Coordinate(float value, double fallback) { return Finite(value) ? value : fallback; }
        private static double Extent(float value) { return Finite(value) && value > 0 ? value : 0; }
        private static double Size(float requested, double available)
        { return float.IsPositiveInfinity(requested) ? available : Math.Min(Extent(requested), available); }
        private static float Position(double value, double minimum, double maximum)
        { return (float)Math.Max(minimum, Math.Min(maximum, value)); }
        private static void Add(List<float> values, double value, double minimum, double maximum)
        {
            float coordinate = Position(value, minimum, maximum);
            if (!values.Contains(coordinate)) values.Add(coordinate);
        }
        private static void Sort(List<float> values, float preferred)
        {
            values.Sort((first, second) => {
                int distance = Math.Abs((double)first - preferred).CompareTo(Math.Abs((double)second - preferred));
                return distance != 0 ? distance : first.CompareTo(second);
            });
        }
        private static double Overlap(Rect candidate, List<Bounds> obstacles)
        {
            double result = 0, right = (double)candidate.x + candidate.width, bottom = (double)candidate.y + candidate.height;
            foreach (Bounds obstacle in obstacles)
                result += Math.Max(0, Math.Min(right, obstacle.Right) - Math.Max(candidate.x, obstacle.Left)) *
                    Math.Max(0, Math.Min(bottom, obstacle.Bottom) - Math.Max(candidate.y, obstacle.Top));
            return result;
        }
    }
}
