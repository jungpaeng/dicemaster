using System;
using UnityEngine;

namespace DiceMaster.LogUI
{
    // Physical pixel settings remain physical pixels. Only a new/default layout
    // is scaled from the 1080p reference, as in the referenced ToolWindow.
    public sealed class LogLayout
    {
        public const int MinimumFont = 12, MaximumFont = 64;
        public const float DefaultWidth = 1280, DefaultHeight = 960;
        public const int DefaultFontSize = 20;
        public Rect Tabs, Status, InputStatus, Search, Content, Footer;
        public float Gap, Padding;
        public bool CompactChrome;

        public static float ScreenScale(int screenHeight)
        { return Mathf.Clamp(screenHeight / 1080f, .85f, 4f); }

        public static int DefaultFont(int screenHeight)
        { return Mathf.Clamp((int)Math.Round(DefaultFontSize * ScreenScale(screenHeight)), MinimumFont, MaximumFont); }

        public static Rect DefaultWindow(int screenWidth, int screenHeight)
        {
            float scale = ScreenScale(screenHeight);
            return LogRenderer.ConstrainWindow(new Rect(20 * scale, 20 * scale,
                DefaultWidth * scale, DefaultHeight * scale), screenWidth, screenHeight);
        }

        public static bool IsDefaultSeed(Rect rect, int font)
        {
            if (SameLayout(rect, font, new Rect(20, 20, 750, 750), 14) ||
                SameLayout(rect, font, new Rect(20, 20, 1200, 860), DefaultFontSize) ||
                SameLayout(rect, font, new Rect(20, 20, DefaultWidth, DefaultHeight), DefaultFontSize)) return true;
            float scale = ScreenScale(Screen.height);
            Rect previousDefault = LogRenderer.ConstrainWindow(new Rect(20 * scale, 20 * scale,
                1200 * scale, 860 * scale), Screen.width, Screen.height);
            return SameLayout(rect, font, previousDefault, DefaultFont(Screen.height));
        }

        private static bool SameLayout(Rect left, int font, Rect right, int expectedFont)
        {
            // BepInEx serializes fractional physical pixels. Only tolerate that
            // small round trip, not a moved/resized/custom-font layout.
            return font == expectedFont && Math.Abs(left.x - right.x) < .01f && Math.Abs(left.y - right.y) < .01f &&
                Math.Abs(left.width - right.width) < .01f && Math.Abs(left.height - right.height) < .01f;
        }

        public static LogLayout Calculate(Rect window, int font, bool search,
            float statusHeight, float inputHeight, float footerHeight, float minimumBodyHeight = 0)
        {
            var result = new LogLayout();
            result.Gap = Math.Max(4, (float)Math.Round(font * .25f));
            result.Padding = Math.Max(8, (float)Math.Round(font * .55f));
            float x = result.Padding, width = Math.Max(1, window.width - x * 2);
            float y = font + 18 + result.Gap; // Window title remains draggable.
            float row = font + 22;
            result.Tabs = new Rect(x, y, width, row); y += row + result.Gap;
            result.Status = new Rect(x, y, width, statusHeight); y += statusHeight + result.Gap;
            if (inputHeight > 0)
            { result.InputStatus = new Rect(x, y, width, inputHeight); y += inputHeight + result.Gap; }
            if (search)
            { result.Search = new Rect(x, y, width, row); y += row + result.Gap; }
            float footerY = window.height - result.Padding - footerHeight;
            if (footerY - result.Gap - y < Math.Max(font * 3 + 32, minimumBodyHeight))
            {
                // Keep the chosen body font and a usable, scrollable recovery
                // area. Status and search remain reachable inside that area.
                result.CompactChrome = true;
                result.Status = result.InputStatus = result.Search = new Rect();
                y = result.Tabs.yMax + result.Gap;
                footerHeight = font + 10;
                footerY = window.height - result.Padding - footerHeight;
            }
            result.Footer = new Rect(x, footerY, width, footerHeight);
            result.Content = new Rect(x, y, width, Math.Max(1, footerY - result.Gap - y));
            return result;
        }

        public static float BringIntoView(float scroll, Rect row, float viewportHeight, float contentHeight)
        {
            if (viewportHeight <= 0) return Math.Max(0, scroll);
            if (row.height > viewportHeight || row.y < scroll) scroll = row.y;
            else if (row.yMax > scroll + viewportHeight) scroll = row.yMax - viewportHeight;
            return Mathf.Clamp(scroll, 0, Math.Max(0, contentHeight - viewportHeight));
        }
    }
}
