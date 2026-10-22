using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DiceMaster.Information;

static partial class Program
{
    private static void LoadingStateCases()
    {
        var state = new InformationLoadingState(); var anchor = new object(); var other = new object(); var pending = new List<object> { anchor, other };
        state.Update(pending, "map", 1, 64);
        Check(!state.Visible(anchor, 1.19f) && state.Visible(anchor, 1.21f), "loading waits 0.2 seconds instead of flashing for short preparation");
        state.Update(pending, "map", 1.25f, 64);
        Check(state.Visible(anchor, 1.26f), "new quarter-second snapshots preserve the same anchor's pending timer");
        pending.Remove(anchor); state.Update(pending, "map", 1.3f, 64);
        Check(!state.Visible(anchor, 10) && state.Visible(other, 1.31f), "settled positive/empty/failed anchors disappear independently");
        pending.Add(anchor); state.Update(pending, "map", 1.4f, 64);
        Check(!state.Visible(anchor, 1.41f), "a genuinely new pending interval starts a fresh delay");
        state.Update(pending, "other-save", 2, 64);
        Check(!state.Visible(other, 2.1f), "load/menu context changes discard previous pending time");
        state.Clear(); Check(!state.Visible(anchor, 20), "hidden/OFF/focus cleanup releases all loading state");
        state.Update(pending, "map", 4, 1); Check(state.Visible(other, 4.3f) && !state.Visible(anchor, 4.3f), "loading storage and work are capped by the visible-anchor limit");
        state.Update(pending, "map", 1, 64); Check(!state.Visible(other, 1.1f), "clock rollback resets delay without stale or negative timing");
        state.Update(pending, "map", float.NaN, 64); Check(!state.Visible(anchor, 10), "invalid presentation time clears state");
        var pixels = InformationLoadingState.Pixels();
        Check(pixels.Length == 32 * 32 * 8 * 4 && pixels[3] == 0 && pixels[(16 * 32 + 16) * 4 + 3] == 0,
            "one 32KiB eight-frame spinner atlas has transparent corners and a hollow center");
        Check(!pixels.Take(4096).SequenceEqual(pixels.Skip(4096).Take(4096)), "cached frame cells animate without regenerating textures");
    }
    private static void LoadingBenchmark(string output)
    {
        Directory.CreateDirectory(output); var state = new InformationLoadingState(); var pending = Enumerable.Range(0,64).Select(_ => new object()).ToList();
        for (int at = 0; at < 1000; at++) state.Update(pending, "map", 1, 64);
        long bytes = GC.GetAllocatedBytesForCurrentThread(); var watch = Stopwatch.StartNew();
        for (int at = 0; at < 100000; at++) { state.Update(pending, "map", 1, 64); foreach (var item in pending) state.Visible(item, 2); }
        watch.Stop(); bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        File.WriteAllBytes(Path.Combine(output,"spinner.rgba"), InformationLoadingState.Pixels());
        Console.WriteLine("LOADING STATE: 100000 frames, 64 anchors, " + watch.Elapsed.TotalMilliseconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture) + " ms total, " + bytes + " B (includes Stopwatch); no Unity/game/source reads. Native GUI/FPS not executed.");
    }
}
