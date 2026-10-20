using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceMaster.Information
{
    // Bounded fixed-corner placement for at most two passive panels. Never
    // search the map's obstacle-edge grid or move a map label between places.
    public static class InformationLayout
    {
        public static Rect Place(Rect preferred, Rect viewport, IList<Rect> occupied)
        {
            float left = Finite(viewport.x) ? viewport.x : 0, top = Finite(viewport.y) ? viewport.y : 0;
            float extentX = Size(viewport.width, (double)float.MaxValue - left);
            float extentY = Size(viewport.height, (double)float.MaxValue - top);
            float width = Size(preferred.width, extentX), height = Size(preferred.height, extentY);
            float right = (float)((double)left + extentX - width), bottom = (float)((double)top + extentY - height);
            Rect best = new Rect(Clamp(preferred.x, left, right), Clamp(preferred.y, top, bottom), width, height);
            if (occupied == null || occupied.Count == 0) return best;
            double score = Overlap(best, occupied);
            if (score == 0) return best;
            // Log avoidance uses only four fixed corners; work is linear in
            // the supplied panels, independent of map-place/candidate count.
            foreach (float y in new[] { top, bottom })
                foreach (float x in new[] { right, left })
                {
                    Rect candidate = new Rect(x, y, width, height);
                    double next = Overlap(candidate, occupied);
                    if (next < score) { best = candidate; score = next; }
                    if (score == 0) return best;
                }
            return best;
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static float Size(float value, double available)
        { return (float)Math.Min(Math.Max(0, available), float.IsPositiveInfinity(value) ? Math.Max(0, available) : Finite(value) ? Math.Max(0, value) : 0); }
        private static float Clamp(float value, float min, float max)
        { return (float)Math.Max(min, Math.Min(max, Finite(value) ? value : min)); }
        private static double Overlap(Rect value, IList<Rect> occupied)
        {
            double result = 0;
            for (int index = 0; index < occupied.Count; index++)
            {
                Rect other = occupied[index];
                if (!Finite(other.x) || !Finite(other.y) || !Finite(other.width) || !Finite(other.height) || other.width <= 0 || other.height <= 0) continue;
                result += Math.Max(0, Math.Min((double)value.x + value.width, (double)other.x + other.width) - Math.Max(value.x, other.x)) *
                    Math.Max(0, Math.Min((double)value.y + value.height, (double)other.y + other.height) - Math.Max(value.y, other.y));
            }
            return result;
        }
    }
}
