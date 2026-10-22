using System;
using System.IO;
using UnityEngine;

namespace DiceMaster.Information
{
    // A mod-owned image, loaded once on the Unity GUI thread. No font lookup,
    // file access, decoder, animation or texture generation on warm repaints.
    public static class InformationBadge
    {
        private static readonly Texture2D[] images = new Texture2D[2];
        private static readonly bool[] attempted = new bool[2];
        public static void Release()
        {
            for (int at = 0; at < images.Length; at++)
            { if (images[at] != null) UnityEngine.Object.Destroy(images[at]); images[at] = null; attempted[at] = false; }
        }
        public static void Draw(Rect badge, Rect stem, bool guidance = false)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            int kind = guidance ? 1 : 0;
            if (!attempted[kind])
            {
                attempted[kind] = true;
                Texture2D pending = null;
                try
                {
                    const int length = 64 * 64 * 4;
                    var bytes = new byte[length];
                    using (var stream = typeof(InformationBadge).Assembly.GetManifestResourceStream(guidance ? "DiceMaster.StoryGuideBadge.rgba" : "DiceMaster.StoryBadge.rgba"))
                    {
                        if (stream == null || stream.Length != length) throw new InvalidDataException("Story badge image unavailable");
                        int at = 0;
                        while (at < length)
                        {
                            int count = stream.Read(bytes, at, length - at);
                            if (count == 0) throw new EndOfStreamException("Story badge image truncated");
                            at += count;
                        }
                    }
                    pending = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                    pending.LoadRawTextureData(bytes);
                    pending.Apply(false, true);
                    images[kind] = pending;
                }
                catch { if (pending != null) UnityEngine.Object.Destroy(pending); throw; }
            }
            // After a resource/upload failure, hide the badge without repeating
            // a failing load or suppressing the independent text description.
            Texture2D image = images[kind];
            if (image == null) return;
            Color previous = GUI.color;
            try
            {
                if (stem.width > 0 && stem.height > 0)
                {
                    GUI.color = guidance ? new Color(.38f, .31f, .17f) : new Color(.89f, .76f, .49f);
                    GUI.DrawTexture(stem, Texture2D.whiteTexture);
                }
                GUI.color = Color.white;
                GUI.DrawTexture(badge, image);
            }
            finally { GUI.color = previous; }
        }
    }
}
