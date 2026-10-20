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
            private bool right, richText;
            private Rect avoid;
            private readonly List<Rect> obstacles = new List<Rect>(1);
            internal void Prepare(string value, GUIStyle style, bool topRight, Rect occlusion)
            {
                if (text == value && font == style.fontSize && screenWidth == Screen.width &&
                    screenHeight == Screen.height && right == topRight && richText == style.richText && avoid == occlusion) return;
                text = value; font = style.fontSize; screenWidth = Screen.width; screenHeight = Screen.height;
                right = topRight; richText = style.richText; avoid = occlusion; Content.text = value;
                float margin = Math.Min(12, Math.Max(0, (Math.Min(Screen.width, Screen.height) - 1) * .5f));
                var viewport = new Rect(margin, margin, Math.Max(1, Screen.width - margin * 2), Math.Max(1, Screen.height - margin * 2));
                float width = Math.Min(viewport.width, Math.Min(420, Math.Max(160, Screen.width * (topRight ? .35f : .5f))));
                float maximumHeight = viewport.height * .35f;
                float measured = style.CalcHeight(Content, width);
                if (!topRight && measured > maximumHeight)
                {
                    Content.text = MapGuidePresentation.Fit(value, candidate => style.CalcHeight(new GUIContent(candidate), width), maximumHeight);
                    measured = style.CalcHeight(Content, width);
                }
                float height = Math.Min(maximumHeight, Math.Max(1, measured));
                var preferred = new Rect(topRight ? viewport.x + viewport.width - width : viewport.x, viewport.y, width, height);
                obstacles.Clear();
                if (occlusion.width > 0 && occlusion.height > 0) obstacles.Add(occlusion);
                Rect = InformationLayout.Place(preferred, viewport, obstacles);
            }
        }
        private static GUIStyle informationStyle;
        private static int informationFont;
        private static readonly PassiveLabel mapLabel = new PassiveLabel(), branchLabel = new PassiveLabel(), choiceLabel = new PassiveLabel();
        private static InformationRow lastMapRow;
        private static string lastMapStatus, mapText;
        private static object lastWaitingSource;
        private static InformationRow waitingRow;
        private static ChoiceStorySnapshot lastChoice;
        private static InformationRow lastChoiceRow;
        private static string choiceText, lastChoiceStatus;

        public static void Draw(bool logOpen, bool popupOpen, Rect logRect, float now)
        {
            try { DrawCore(logOpen, popupOpen, logRect, now); }
            catch (Exception error) { Failure("정보 배치 경계", error); }
        }
        private static void DrawCore(bool logOpen, bool popupOpen, Rect logRect, float now)
        {
            if (!Application.isFocused || Screen.width <= 0 || Screen.height <= 0) return;
            InformationRow selected = null; string status = "";
            ChoiceStorySnapshot choice = null; string choiceStatus = "";
            if (!logOpen && !popupOpen && choices.Visible && Enabled(0) && choices.Rows.Count == 1)
            {
                var source = choices.Rows[0];
                var snapshot = source.Value as ChoiceStorySnapshot;
                if (snapshot != null && ChoiceStorySource.IsCurrent(snapshot)) { choice = snapshot; choiceStatus = source.Status ?? ""; }
            }
            if (choice == null && !logOpen && !popupOpen && !ChoiceStorySource.MenuVisible && map.Visible && (Enabled(0) || Enabled(1)) && map.Rows.Count == 1)
            {
                var source = map.Rows[0];
                status = source.Status ?? "";
                if (MapGuideSource.IsCurrent(source.Id))
                {
                    var snapshot = source.Value as MapGuideSnapshot;
                    selected = snapshot != null && snapshot.HasSelection ? snapshot.Summary : null;
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
            if (selected == null && latest == null && choice == null) { lastMapRow = null; mapText = null; lastChoice = null; choiceText = null; return; }
            int font = Mathf.Clamp(ConfigManager.LogFontSize.Value, 12, 24);
            if (informationStyle == null || informationFont != font)
            { informationFont = font; informationStyle = new GUIStyle(GUI.skin.box) { fontSize = font, wordWrap = true, richText = false, alignment = TextAnchor.UpperLeft }; }
            if (selected != null && !string.IsNullOrEmpty(selected.AutoText))
            {
                if (!ReferenceEquals(lastMapRow, selected) || lastMapStatus != status)
                {
                    lastMapRow = selected; lastMapStatus = status;
                    string heading = selected.Title.Length <= 48 ? selected.Title : selected.Title.Substring(0, 45) + "…";
                    // The provider already limits complete notice rows. Never
                    // cut through a status or through the detail navigation hint.
                    mapText = EntryConditionColors("선택 장소: " + heading + (status.Length == 0 ? "" : " · " + status) + "\n" + selected.AutoText, status.Length == 0);
                }
                // Only the exact positive entry status is green. Mixed rows,
                // headings and stale publications keep their neutral color.
                DrawPassive(mapLabel, mapText, false, default(Rect), false, true);
            }
            if (choice != null)
            {
                InformationRow current = ChoiceStorySource.CurrentRow(choice);
                if (!ReferenceEquals(lastChoice, choice) || !ReferenceEquals(lastChoiceRow, current) || lastChoiceStatus != choiceStatus)
                {
                    lastChoice = choice; lastChoiceRow = current; lastChoiceStatus = choiceStatus;
                    var lines = new List<string>();
                    lines.Add((current == null ? "선택지의 연결 이야기" : "현재 선택의 연결 이야기") + (choiceStatus.Length == 0 ? "" : " · " + choiceStatus));
                    if (current != null) lines.Add("• " + current.AutoText);
                    else for (int at = 0; at < Math.Min(3, choice.Entries.Count); at++) lines.Add("• " + choice.Entries[at].AutoText);
                    lines.Add("자세히: 로그 → 도구 → 지도 안내");
                    choiceText = EntryConditionColors(string.Join("\n", lines), choiceStatus.Length == 0);
                }
                DrawPassive(choiceLabel, choiceText, false, default(Rect), false, true);
            }
            if (latest != null) DrawPassive(branchLabel, latest.AutoText, true, logOpen ? logRect : default(Rect), latest.Available);
        }
        private static string EntryConditionColors(string text, bool current)
        {
            var lines = text.Split('\n');
            for (int at = 0; at < lines.Length; at++)
            {
                string line = lines[at];
                string positive = line.EndsWith(" · 진입 조건 충족 (현재)", StringComparison.Ordinal) ? "진입 조건 충족 (현재)" :
                    line.EndsWith(" · 현재 이야기 진입 조건 충족", StringComparison.Ordinal) ? "현재 이야기 진입 조건 충족" :
                    line.EndsWith(" · 발생 조건 충족", StringComparison.Ordinal) ? "발생 조건 충족" :
                    line.EndsWith(" · 진행 조건 충족", StringComparison.Ordinal) ? "진행 조건 충족" :
                    line.EndsWith(" · 선택 후 진행 가능", StringComparison.Ordinal) ? "선택 후 진행 가능" :
                    line.EndsWith(" · 진행 가능", StringComparison.Ordinal) ? "진행 가능" : null;
                // Titles and names remain literal when trusted color markup
                // is enabled. Apply formatting only while preparing the cache.
                line = line.Replace("<", "＜").Replace(">", "＞");
                if (current && positive != null && line.StartsWith("• ", StringComparison.Ordinal))
                    line = line.Substring(0, line.Length - positive.Length) + "<color=#BFFFCC>" + positive + "</color>";
                lines[at] = line;
            }
            return string.Join("\n", lines);
        }
        private static void DrawPassive(PassiveLabel label, string text, bool right, Rect occlusion, bool available, bool neutral = false)
        {
            informationStyle.richText = neutral;
            label.Prepare(text, informationStyle, right, occlusion);
            if (string.IsNullOrEmpty(label.Content.text)) return;
            informationStyle.normal.textColor = neutral ? new Color(.95f, .95f, .95f) : available ? new Color(.75f, 1f, .8f) : new Color(1f, .88f, .55f);
            GUI.Label(label.Rect, label.Content, informationStyle);
        }
    }
}
