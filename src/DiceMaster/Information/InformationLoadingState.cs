using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DiceMaster.Information
{
    // Presentation-only timers. No source reads, game calls or scheduler here.
    public sealed class InformationLoadingState
    {
        private sealed class Identity : IEqualityComparer<object>
        {
            public new bool Equals(object a, object b) { return ReferenceEquals(a, b); }
            public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
        }
        private readonly Dictionary<object, float> starts = new Dictionary<object, float>(new Identity());
        private readonly HashSet<object> current = new HashSet<object>(new Identity());
        private readonly List<object> removed = new List<object>();
        private string context;
        private float lastTime;
        public void Clear() { starts.Clear(); current.Clear(); removed.Clear(); context = null; lastTime = 0; }
        public void Update(IList<object> pending, string key, float now, int limit)
        {
            if (float.IsNaN(now) || float.IsInfinity(now)) { Clear(); return; }
            if (context != key || now < lastTime) Clear();
            context = key; lastTime = now; current.Clear();
            if (pending != null)
                for (int at = 0; at < Math.Min(Math.Max(0, limit), pending.Count); at++)
                {
                    object anchor = pending[at];
                    if (anchor == null || !current.Add(anchor)) continue;
                    if (!starts.ContainsKey(anchor)) starts.Add(anchor, now);
                }
            removed.Clear();
            foreach (object anchor in starts.Keys) if (!current.Contains(anchor)) removed.Add(anchor);
            foreach (object anchor in removed) starts.Remove(anchor);
        }
        public bool Visible(object anchor, float now)
        { return anchor != null && starts.TryGetValue(anchor, out float start) && now >= start + .2f; }

        internal const int ImageSize = 32, Frames = 8;
        // Prepared once per texture lifetime, never per repaint. A small muted
        // gold ring fits both existing map-pin and menu-badge positions.
        internal static byte[] Pixels()
        {
            var pixels = new byte[ImageSize * ImageSize * Frames * 4];
            for (int frame = 0; frame < Frames; frame++)
                for (int y = 0; y < ImageSize; y++)
                    for (int x = 0; x < ImageSize; x++)
                    {
                        double dx = x + .5 - ImageSize * .5, dy = y + .5 - ImageSize * .5;
                        double ring = Math.Max(0, Math.Min(1, 1.6 - Math.Abs(Math.Sqrt(dx * dx + dy * dy) - 12)));
                        int sector = (int)((Math.Atan2(dy, dx) + Math.PI) * Frames / (Math.PI * 2)) % Frames;
                        int age = (sector + frame) % Frames;
                        int at = ((frame * ImageSize + y) * ImageSize + x) * 4;
                        pixels[at] = 219; pixels[at + 1] = 197; pixels[at + 2] = 144;
                        pixels[at + 3] = (byte)(ring * (45 + age * 27));
                    }
            return pixels;
        }
    }
}
