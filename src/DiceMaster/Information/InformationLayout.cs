using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceMaster.Information
{
    // At most four adjacent candidates for the focused item. Branch notices
    // retain fixed corners; neither path searches the map's obstacle grid.
    public static class InformationLayout
    {
        public static Rect Near(Rect anchor, Rect viewport, float width, float height)
        {
            width = Size(width, viewport.width); height = Size(height, viewport.height);
            if (!Finite(anchor.x) || !Finite(anchor.y) || !Finite(anchor.width) || !Finite(anchor.height) || anchor.width <= 0 || anchor.height <= 0)
                return Place(new Rect(viewport.x, viewport.y, width, height), viewport, null);
            const float gap = 8;
            Rect best = default(Rect); double score = double.MaxValue;
            for (int at = 0; at < 4; at++)
            {
                float x = at == 0 ? anchor.x + anchor.width + gap : at == 1 ? anchor.x - width - gap : anchor.x;
                float y = at < 2 ? anchor.y : at == 2 ? anchor.y + anchor.height + gap : anchor.y - height - gap;
                Rect value = Place(new Rect(x, y, width, height), viewport, null);
                double overlap = Intersection(value, anchor);
                double clamped = Math.Abs(value.x - x) + Math.Abs(value.y - y);
                double next = overlap * 1000000 + clamped;
                if (next < score) { best = value; score = next; }
                if (next == 0) break;
            }
            return best;
        }
        public static Rect Badge(Rect anchor, Rect viewport, float size, bool map = false)
        { return Place(new Rect(map ? anchor.x + (anchor.width - size) * .5f : anchor.x + anchor.width - size * .25f,
            map ? anchor.y - size - 7 : anchor.y - size * .75f, size, size), viewport, null); }
        public static Rect Stem(Rect badge, Rect anchor)
        {
            float start = badge.y + badge.height, end = Math.Min(start + 7, anchor.y);
            return end > start ? new Rect(badge.x + badge.width * .5f - .5f, start, 1, end - start) : default(Rect);
        }
        public static Rect IncludingBadge(Rect anchor, Rect badge)
        {
            float left = Math.Min(anchor.x, badge.x), top = Math.Min(anchor.y, badge.y);
            return new Rect(left, top, Math.Max(anchor.x + anchor.width, badge.x + badge.width) - left,
                Math.Max(anchor.y + anchor.height, badge.y + badge.height) - top);
        }
        private static double Intersection(Rect a, Rect b)
        { return Math.Max(0, Math.Min((double)a.x + a.width, (double)b.x + b.width) - Math.Max(a.x, b.x)) * Math.Max(0, Math.Min((double)a.y + a.height, (double)b.y + b.height) - Math.Max(a.y, b.y)); }
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
    // A parked mouse must not steal the description from pad/keyboard focus.
    public sealed class InformationFocus
    {
        private object native;
        private Vector2 pointer;
        private bool known, preferPointer;
        public void Clear() { native = null; known = preferPointer = false; }
        public bool Pointer(object focusedAnchor, Vector2 position, bool valid)
        {
            if (!ReferenceEquals(native, focusedAnchor)) { native = focusedAnchor; preferPointer = false; }
            if (valid)
            {
                if (known && (Math.Abs(position.x - pointer.x) >= 2 || Math.Abs(position.y - pointer.y) >= 2)) preferPointer = true;
                else if (!known && native == null) preferPointer = true;
                pointer = position; known = true;
            }
            return valid && preferPointer;
        }
    }
}
