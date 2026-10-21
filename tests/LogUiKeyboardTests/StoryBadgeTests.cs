using System;
using DiceMaster.Information;
using UnityEngine;

internal static partial class Program
{
    private static void StoryBadgeRendering()
    {
        var previous = Event.current;
        try
        {
            var viewport = new Rect(0, 0, 1000, 700);
            var anchor = new Rect(100, 200, 120, 32);
            var pin = InformationLayout.Badge(anchor, viewport, 24, true);
            var corner = InformationLayout.Badge(anchor, viewport, 24, false);
            Check(pin == new Rect(148, 169, 24, 24) && corner == new Rect(214, 182, 24, 24), "map pin is centered above the native place while menu badge uses its corner");
            Check(InformationLayout.Stem(pin, anchor) == new Rect(159.5f, 193, 1, 7), "map pin has a short native-place connector");
            var edge = new Rect(0, 0, 100, 30);
            Check(InformationLayout.Stem(InformationLayout.Badge(edge, viewport, 24, true), edge).height == 0, "clamped top-edge pin never draws an inverted connector across the place");
            foreach (float size in new[] { 20f, 24f, 28f })
            foreach (var rect in new[] { anchor, edge, new Rect(990, 690, 10, 10) })
            foreach (bool map in new[] { false, true })
            {
                var badge = InformationLayout.Badge(rect, viewport, size, map);
                var combined = InformationLayout.IncludingBadge(rect, badge);
                var panel = InformationLayout.Near(combined, viewport, 200, 80);
                Check(badge.x >= 0 && badge.y >= 0 && badge.x + badge.width <= 1000 && badge.y + badge.height <= 700, "pin and corner badge stay inside viewport at font and screen edges");
                Check(panel.x + panel.width <= combined.x || panel.x >= combined.x + combined.width || panel.y + panel.height <= combined.y || panel.y >= combined.y + combined.height, "focused description avoids both native item and its badge");
            }
            InformationBadge.Release();
            int creates = Texture2D.Created, uploads = Texture2D.Uploads;
            Event.current = new Event { type = EventType.Layout };
            InformationBadge.Draw(pin, InformationLayout.Stem(pin, anchor));
            Check(Texture2D.Created == creates, "layout/input passes do not load images or create textures");
            Event.current.type = EventType.Repaint;
            var savedColor = new Color(.2f, .4f, .6f); GUI.color = savedColor;
            for (int at = 0; at < 1000; at++) InformationBadge.Draw(pin, InformationLayout.Stem(pin, anchor));
            Check(Texture2D.Created == creates + 1 && Texture2D.Uploads == uploads + 1 && Texture2D.Bytes.Length == 16384, "one thousand badge repaints share exactly one embedded image upload");
            Check(GUI.color.r == savedColor.r && GUI.color.g == savedColor.g && GUI.color.b == savedColor.b, "icon and connector always restore caller GUI color");
            int destroyed = UnityEngine.Object.Destroyed;
            InformationBadge.Release(); Check(UnityEngine.Object.Destroyed == destroyed + 1, "plugin shutdown releases its own badge texture");
            Texture2D.FailUpload = true; creates = Texture2D.Created; int failures = 0;
            for (int at = 0; at < 100; at++) try { InformationBadge.Draw(pin, default); } catch { failures++; }
            Check(failures == 1 && Texture2D.Created == creates + 1, "texture upload failure is reported once instead of retried every repaint");
        }
        finally { Event.current = previous; Texture2D.FailUpload = false; InformationBadge.Release(); GUI.color = Color.white; GUI.BadgeLabels.Clear(); GUI.BadgeStems.Clear(); }
    }
}
namespace UnityEngine
{
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Bilinear }
    public enum TextureWrapMode { Clamp }
    public enum HideFlags { HideAndDontSave }
    public class Texture2D : Object
    {
        public static int Created, Uploads;
        public static bool FailUpload;
        public static byte[] Bytes;
        public static readonly Texture2D whiteTexture = new Texture2D();
        static Texture2D() { GUI.WhiteTexture = whiteTexture; }
        public FilterMode filterMode; public TextureWrapMode wrapMode; public HideFlags hideFlags;
        private Texture2D() { }
        public Texture2D(int width, int height, TextureFormat format, bool mipmaps) { Created++; }
        public void LoadRawTextureData(byte[] bytes) { Bytes = bytes; Uploads++; if (FailUpload) throw new InvalidOperationException("Upload failed"); }
        public void Apply(bool mipmaps, bool releaseCpuCopy) { }
    }
}
