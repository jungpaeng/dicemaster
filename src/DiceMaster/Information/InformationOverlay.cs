using System;
using System.Collections.Generic;
using DiceMaster.Config;
using DiceMaster.LiveLog;
using UnityEngine;

namespace DiceMaster.Information
{
    public static partial class InformationRuntime
    {
        private sealed class PassiveLabel
        {
            internal readonly GUIContent Content = new GUIContent("");
            internal Rect Rect;
            private string text;
            private int font, screenWidth, screenHeight;
            private bool right;
            private Rect avoid;
            private readonly List<Rect> obstacles = new List<Rect>(1);
            internal void Prepare(string value, GUIStyle style, bool topRight, Rect occlusion)
            {
                if (text == value && font == style.fontSize && screenWidth == Screen.width &&
                    screenHeight == Screen.height && right == topRight && avoid == occlusion) return;
                text = value; font = style.fontSize; screenWidth = Screen.width; screenHeight = Screen.height;
                right = topRight; avoid = occlusion; Content.text = value;
                float margin = Math.Min(12, Math.Max(0, (Math.Min(Screen.width, Screen.height) - 1) * .5f));
                var viewport = new Rect(margin, margin, Math.Max(1, Screen.width - margin * 2), Math.Max(1, Screen.height - margin * 2));
                float width = Math.Min(viewport.width, Math.Min(420, Math.Max(160, Screen.width * .35f)));
                float height = Math.Min(viewport.height * .35f, Math.Max(1, style.CalcHeight(Content, width)));
                var preferred = new Rect(topRight ? viewport.x + viewport.width - width : viewport.x, viewport.y, width, height);
                obstacles.Clear();
                if (occlusion.width > 0 && occlusion.height > 0) obstacles.Add(occlusion);
                Rect = InformationLayout.Place(preferred, viewport, obstacles);
            }
        }
        private static GUIStyle informationStyle;
        private static int informationFont;
        private static readonly PassiveLabel mapLabel = new PassiveLabel(), branchLabel = new PassiveLabel();
        private static InformationRow lastMapRow;
        private static string lastMapStatus, mapText;
        private static object lastWaitingSource;
        private static InformationRow waitingRow;

        public static void Draw(bool logOpen, bool popupOpen, Rect logRect, float now)
        {
            try { DrawCore(logOpen, popupOpen, logRect, now); }
            catch (Exception error) { Failure("정보 배치 경계", error); }
        }
        private static void DrawCore(bool logOpen, bool popupOpen, Rect logRect, float now)
        {
            if (!Application.isFocused || Screen.width <= 0 || Screen.height <= 0) return;
            InformationRow selected = null; string status = "";
            if (!logOpen && !popupOpen && map.Visible && (Enabled(0) || Enabled(1)) && map.Rows.Count == 1)
            {
                var source = map.Rows[0];
                status = source.Status ?? "";
                if (MapGuideSource.IsCurrent(source.Id))
                {
                    selected = source.Value as InformationRow;
                    if (selected == null && status.Length != 0)
                    {
                        if (!ReferenceEquals(lastWaitingSource, source))
                        {
                            lastWaitingSource = source;
                            waitingRow = new InformationRow(source.Id, "선택한 장소", "지도", status, "", status);
                        }
                        selected = waitingRow;
                    }
                    else if (selected != null && !MapGuideSource.IsCurrent(selected)) selected = null;
                }
            }
            var latest = Enabled(2) ? BranchHistory.Latest(now) : null;
            if (selected == null && latest == null) { lastMapRow = null; mapText = null; return; }
            int font = Mathf.Clamp(ConfigManager.LogFontSize.Value, 12, 64);
            if (informationStyle == null || informationFont != font)
            { informationFont = font; informationStyle = new GUIStyle(GUI.skin.box) { fontSize = font, wordWrap = true, richText = false }; }
            if (selected != null && !string.IsNullOrEmpty(selected.AutoText))
            {
                if (!ReferenceEquals(lastMapRow, selected) || lastMapStatus != status)
                {
                    lastMapRow = selected; lastMapStatus = status;
                    string heading = selected.Title.Length <= 48 ? selected.Title : selected.Title.Substring(0, 45) + "…";
                    string text = heading + (status.Length == 0 ? "" : " · " + status) + "\n" + selected.AutoText;
                    // Keep automatic UI compact; full names/conditions/effects
                    // stay available in the existing scrollable log tools.
                    mapText = text.Length <= 180 ? text : text.Substring(0, 177) + "…";
                }
                DrawPassive(mapLabel, mapText, false, default(Rect), selected.Available && status.Length == 0);
            }
            if (latest != null) DrawPassive(branchLabel, latest.AutoText, true, logOpen ? logRect : default(Rect), latest.Available);
        }
        private static void DrawPassive(PassiveLabel label, string text, bool right, Rect occlusion, bool available)
        {
            label.Prepare(text, informationStyle, right, occlusion);
            informationStyle.normal.textColor = available ? new Color(.75f, 1f, .8f) : new Color(1f, .88f, .55f);
            GUI.Label(label.Rect, label.Content, informationStyle);
        }
    }
}
