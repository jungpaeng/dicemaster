using System;
using System.Collections.Generic;
using DiceMaster.Information;
using UnityEngine;

internal static partial class Program
{
    private static bool InformationFinite(Rect value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.width) && !float.IsInfinity(value.width) &&
            !float.IsNaN(value.height) && !float.IsInfinity(value.height);
    }
    private static double InformationOverlap(Rect first, Rect second)
    {
        return Math.Max(0, Math.Min((double)first.x + first.width, (double)second.x + second.width) - Math.Max(first.x, second.x)) *
            Math.Max(0, Math.Min((double)first.y + first.height, (double)second.y + second.height) - Math.Max(first.y, second.y));
    }
    private static double InformationTotalOverlap(Rect value, IList<Rect> occupied)
    { double area = 0; foreach (Rect obstacle in occupied) area += InformationOverlap(value, obstacle); return area; }
    private static void InformationInside(Rect value, Rect viewport, string message)
    {
        Check(InformationFinite(value) && value.width >= 0 && value.height >= 0, message + ": finite nonnegative geometry");
        Check(value.x >= viewport.x && value.y >= viewport.y &&
            (double)value.x + value.width <= (double)viewport.x + viewport.width &&
            (double)value.y + value.height <= (double)viewport.y + viewport.height, message + ": inside viewport");
    }
    private static void InformationLayoutGeometry()
    {
        var viewport = new Rect(0, 0, 200, 100);
        var preferred = new Rect(25, 30, 70, 20);
        Check(InformationLayout.Place(preferred, viewport, null) == preferred, "passive information retains its free preferred placement");
        Rect clamped = InformationLayout.Place(new Rect(-900, 1000, 700, 2000), new Rect(10, 20, 90, 70), null);
        Check(clamped == new Rect(10, 20, 90, 70), "long text width and height clamp to a nonzero-origin viewport");
        var log = new List<Rect> { new Rect(100, 0, 100, 100) };
        Rect moved = InformationLayout.Place(new Rect(150, 75, 50, 25), viewport, log);
        InformationInside(moved, viewport, "leftward escape from full-height log");
        Check(InformationTotalOverlap(moved, log) == 0 && moved.x <= 50, "label escapes sideways when downward clamping would remain over the log");
        Check(log.Count == 1 && log[0] == new Rect(100, 0, 100, 100), "placement never edits occupied UI rectangles");
        foreach (var obstacle in new[] { new Rect(0, 0, 100, 100), new Rect(0, 0, 200, 50), new Rect(0, 50, 200, 50) })
        {
            var occupied = new List<Rect> { obstacle };
            moved = InformationLayout.Place(new Rect(45, 40, 60, 30), viewport, occupied);
            InformationInside(moved, viewport, "right/above/below free-space placement");
            Check(InformationTotalOverlap(moved, occupied) == 0, "right/above/below free-space candidate avoids existing UI");
        }
        // This lower-right cavity needs both axes to move. A one-pass push
        // under the first collision would fall back into another rectangle.
        var cavity = new List<Rect> { new Rect(0, 0, 200, 35), new Rect(0, 35, 110, 65), new Rect(160, 35, 40, 65) };
        moved = InformationLayout.Place(new Rect(20, 5, 50, 45), viewport, cavity);
        InformationInside(moved, viewport, "two-axis cavity search");
        Check(InformationTotalOverlap(moved, cavity) == 0 && moved.x == 110 && moved.y >= 35, "all obstacle edge combinations find a fitting interior cavity");

        var packed = new List<Rect>();
        for (int index = 0; index < 16; index++)
        {
            Rect item = InformationLayout.Place(new Rect(15, 0, 5, 5), new Rect(0, 0, 20, 20), packed);
            InformationInside(item, new Rect(0, 0, 20, 20), "multiple information labels " + index);
            Check(InformationTotalOverlap(item, packed) == 0, "sixteen passive labels fit without overlap " + index);
            packed.Add(item);
        }
        moved = InformationLayout.Place(new Rect(15, 0, 5, 5), new Rect(0, 0, 20, 20), packed);
        Check(InformationTotalOverlap(moved, packed) == 25, "full viewport falls back to the minimum unavoidable covered area");
        Check(moved == InformationLayout.Place(new Rect(15, 0, 5, 5), new Rect(0, 0, 20, 20), packed), "impossible placement is deterministic");

        var blocked = new List<Rect> { new Rect(10, 0, 80, 100) };
        moved = InformationLayout.Place(new Rect(35, 35, 30, 30), new Rect(0, 0, 100, 100), blocked);
        Check(InformationTotalOverlap(moved, blocked) == 600 && moved == new Rect(0, 35, 30, 30), "impossible screen minimizes overlap, then distance, then coordinates");
        blocked.Reverse();
        Check(moved == InformationLayout.Place(new Rect(35, 35, 30, 30), new Rect(0, 0, 100, 100), blocked), "fallback does not depend on obstacle enumeration order");

        Rect one = InformationLayout.Place(new Rect(20, 30, 800, 1500), new Rect(0, 0, 1, 1), new[] { new Rect(0, 0, 1, 1) });
        Check(one == new Rect(0, 0, 1, 1), "one-pixel screen has finite in-screen minimum-overlap placement");
        Rect invalid = InformationLayout.Place(new Rect(float.NaN, float.PositiveInfinity, float.PositiveInfinity, float.NaN), viewport,
            new[] { new Rect(float.NaN, 0, 20, 20), new Rect(0, 0, float.PositiveInfinity, 100), new Rect(0, 0, -20, 20) });
        InformationInside(invalid, viewport, "nonfinite preferred and invalid occupied rectangles");
        Check(invalid == new Rect(0, 0, 200, 0), "invalid origin and size normalization is explicit and deterministic");
        invalid = InformationLayout.Place(new Rect(0, 0, 30, 40), new Rect(float.NaN, float.NegativeInfinity, float.PositiveInfinity, -30), null);
        Check(invalid == new Rect(0, 0, 0, 0), "invalid viewport safely collapses instead of leaking NaN infinity or negative extent");
        invalid = InformationLayout.Place(new Rect(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue),
            new Rect(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue), null);
        Check(InformationFinite(invalid) && invalid.width == 0 && invalid.height == 0, "extreme finite viewport cannot overflow a representable Rect");

        // Exhaustive small integer positions independently verify global
        // minimum overlap, including fragmented and fully blocked screens.
        var random = new Random(601);
        for (int sample = 0; sample < 120; sample++)
        {
            var obstacles = new List<Rect>();
            for (int index = 0; index < 5; index++) obstacles.Add(new Rect(random.Next(10), random.Next(9), random.Next(1, 7), random.Next(1, 6)));
            int width = random.Next(1, 7), height = random.Next(1, 6);
            preferred = new Rect(random.Next(-5, 15), random.Next(-5, 14), width, height);
            Rect actual = InformationLayout.Place(preferred, new Rect(0, 0, 10, 9), obstacles);
            InformationInside(actual, new Rect(0, 0, 10, 9), "enumerated placement sample " + sample);
            double minimum = double.MaxValue;
            for (int y = 0; y <= 9 - height; y++) for (int x = 0; x <= 10 - width; x++)
                minimum = Math.Min(minimum, InformationTotalOverlap(new Rect(x, y, width, height), obstacles));
            Check(InformationTotalOverlap(actual, obstacles) == minimum, "all edge candidates achieve the exhaustive minimum " + sample);
            obstacles.Reverse();
            Check(actual == InformationLayout.Place(preferred, new Rect(0, 0, 10, 9), obstacles), "deterministic placement across obstacle order " + sample);
        }
    }
}
