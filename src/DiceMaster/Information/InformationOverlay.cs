using System;
using System.Collections.Generic;
using DiceMaster.Config;
using DiceMaster.LiveLog;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Runtime.CompilerServices;
namespace DiceMaster.Information
{
    public static partial class InformationRuntime
    {
        private static GUIStyle informationStyle;
        private static int informationFont;
        private static readonly List<Rect> informationRects = new List<Rect>();
        private sealed class AnchorGeometry { internal RectTransform[] Rectangles; }
        private static readonly ConditionalWeakTable<Component, AnchorGeometry> geometry = new ConditionalWeakTable<Component, AnchorGeometry>();
        private static readonly Vector3[] corners = new Vector3[4];
        private static AnchorGeometry CaptureGeometry(Component anchor)
        {
            var rectangles = new List<RectTransform>();
            if (anchor.transform is RectTransform own) rectangles.Add(own);
            foreach (string name in new[] { "_costActionText", "_tooltip" })
            {
                FieldInfo field = anchor.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                object value = field?.GetValue(anchor);
                RectTransform rect = value is Text text ? text.rectTransform : value is GameObject game ? game.GetComponent<RectTransform>() : null;
                if (rect != null) rectangles.Add(rect);
            }
            return new AnchorGeometry { Rectangles = rectangles.ToArray() };
        }
        private static void ReserveGeometry(Component anchor, Camera camera, List<Rect> occupied)
        {
            AnchorGeometry item = geometry.GetValue(anchor, CaptureGeometry);
            foreach (RectTransform rect in item.Rectangles)
            {
                if (rect == null || !rect.gameObject.activeInHierarchy) continue;
                rect.GetWorldCorners(corners);
                float left = float.PositiveInfinity, top = float.PositiveInfinity, right = float.NegativeInfinity, bottom = float.NegativeInfinity;
                foreach (Vector3 corner in corners)
                {
                    Vector3 point = camera.WorldToScreenPoint(corner);
                    left = Math.Min(left, point.x); right = Math.Max(right, point.x);
                    top = Math.Min(top, Screen.height - point.y); bottom = Math.Max(bottom, Screen.height - point.y);
                }
                // A whole-map layout container is not an individual UI obstacle.
                if (right > left && bottom > top && right - left < Screen.width * .7f && bottom - top < Screen.height * .7f)
                    occupied.Add(new Rect(left - 4, top - 4, right - left + 8, bottom - top + 8));
            }
        }
        public static void Draw(bool logOpen, bool popupOpen, Rect logRect, float now)
        {
            try { DrawCore(logOpen, popupOpen, logRect, now); }
            catch (Exception error) { Failure("정보 배치 경계", error); }
        }
        private static void DrawCore(bool logOpen, bool popupOpen, Rect logRect, float now)
        {
            if (!Application.isFocused || Screen.width <= 0 || Screen.height <= 0) return;
            bool drawMap = !logOpen && !popupOpen && map.Visible && (Enabled(0) || Enabled(1));
            var latest = Enabled(2) ? BranchHistory.Latest(now) : null;
            if (!drawMap && latest == null) return;
            int font = Mathf.Clamp(ConfigManager.LogFontSize.Value, 12, 64);
            if (informationStyle == null || informationFont != font)
            { informationFont = font; informationStyle = new GUIStyle(GUI.skin.box) { fontSize = font, wordWrap = true, richText = false }; }
            GUIStyle style = informationStyle;
            var occupied = informationRects; occupied.Clear();
            Camera camera = drawMap ? Camera.main : null;
            if (logOpen) occupied.Add(logRect);
            if (drawMap && camera != null)
                foreach (DisplayRow source in map.Rows)
                {
                    var anchor = (source.Value as InformationRow)?.Anchor as Component;
                    if (anchor != null && anchor.gameObject.activeInHierarchy) ReserveGeometry(anchor, camera, occupied);
                }
            if (drawMap)
                foreach (DisplayRow source in map.Rows)
                {
                    var row = source.Value as InformationRow;
                    var anchor = row?.Anchor as Component;
                    if (anchor == null || !anchor.gameObject.activeInHierarchy || string.IsNullOrEmpty(row.AutoText)) continue;
                    if (camera == null) continue;
                    Vector3 point = camera.WorldToScreenPoint(anchor.transform.position);
                    if (point.z < 0) continue;
                    string text = row.AutoText + (string.IsNullOrEmpty(source.Status) ? "" : " · " + source.Status);
                    DrawPassive(text, point.x + 12, Screen.height - point.y, style, occupied, row.Available && string.IsNullOrEmpty(source.Status));
                }
            if (latest != null) DrawPassive(latest.AutoText, Screen.width - Math.Min(420, Screen.width * .4f) - 12, 12, style, occupied, latest.Available);
        }
        private static void DrawPassive(string text, float x, float y, GUIStyle style, List<Rect> occupied, bool available)
        {
            float marginX = Math.Min(8, Math.Max(0, (Screen.width - 1) * .5f));
            float marginY = Math.Min(8, Math.Max(0, (Screen.height - 1) * .5f));
            var viewport = new Rect(marginX, marginY, Screen.width - 2 * marginX, Screen.height - 2 * marginY);
            float width = Math.Max(1, Math.Min(viewport.width, Math.Max(160, Math.Min(420, Screen.width * .4f))));
            float height = Math.Max(1, Math.Min(viewport.height, style.CalcHeight(new GUIContent(text), width)));
            Rect rect = InformationLayout.Place(new Rect(x, y, width, height), viewport, occupied);
            style.normal.textColor = available ? new Color(.75f, 1f, .8f) : new Color(1f, .88f, .55f);
            occupied.Add(rect);
            GUI.Label(rect, text, style); // Label has no event, focus or action callback.
        }
    }
}
