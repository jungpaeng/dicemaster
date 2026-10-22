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
            internal bool CanDraw;
            private string text;
            private int font, screenWidth, screenHeight;
            private bool right, richText;
            private Rect avoid;
            private float width, height;
            private readonly List<Rect> obstacles = new List<Rect>(1);
            internal void Prepare(string value, GUIStyle style, bool topRight, Rect occlusion, Rect? anchor = null, IList<Rect> cards = null)
            {
                CanDraw = true;
                float margin = Math.Min(12, Math.Max(0, (Math.Min(Screen.width, Screen.height) - 1) * .5f));
                var viewport = new Rect(margin, margin, Math.Max(1, Screen.width - margin * 2), Math.Max(1, Screen.height - margin * 2));
                if (text != value || font != style.fontSize || screenWidth != Screen.width || screenHeight != Screen.height || right != topRight || richText != style.richText || avoid != occlusion)
                {
                text = value; font = style.fontSize; screenWidth = Screen.width; screenHeight = Screen.height;
                right = topRight; richText = style.richText; avoid = occlusion; Content.text = value;
                width = Math.Min(viewport.width, Math.Min(420, Math.Max(160, Screen.width * (topRight ? .35f : .5f))));
                float maximumHeight = viewport.height * .35f;
                float measured = style.CalcHeight(Content, width);
                if (!topRight && measured > maximumHeight)
                {
                    Content.text = MapGuidePresentation.Fit(value, candidate => style.CalcHeight(new GUIContent(candidate), width), maximumHeight);
                    measured = style.CalcHeight(Content, width);
                }
                height = Math.Min(maximumHeight, Math.Max(1, measured));
                }
                if (anchor.HasValue)
                {
                    if (cards != null) CanDraw = InformationLayout.TryNearCards(anchor.Value, viewport, width, height, cards, out Rect);
                    else Rect = InformationLayout.Near(anchor.Value, viewport, width, height);
                    return;
                }
                var preferred = new Rect(topRight ? viewport.x + viewport.width - width : viewport.x, viewport.y, width, height);
                obstacles.Clear();
                if (occlusion.width > 0 && occlusion.height > 0) obstacles.Add(occlusion);
                Rect = InformationLayout.Place(preferred, viewport, obstacles);
            }
        }
        private static GUIStyle informationStyle;
        private static int informationFont;
        private static float mapBadgeSize, choiceBadgeSize;
        private static readonly InformationFocus mapFocus = new InformationFocus(), choiceFocus = new InformationFocus();
        private static readonly InformationLoadingState mapLoading = new InformationLoadingState(), choiceLoading = new InformationLoadingState();
        private static readonly List<InformationRow> mapFocusRows = new List<InformationRow>();
        private static readonly List<Rect> choiceCards = new List<Rect>(64);
        private static readonly PassiveLabel mapLabel = new PassiveLabel(), branchLabel = new PassiveLabel(), choiceLabel = new PassiveLabel();
        private static InformationRow lastMapRow;
        private static string lastMapStatus, mapText;
        private static ChoiceStorySnapshot lastChoice;
        private static InformationRow lastChoiceRow;
        private static string choiceText, lastChoiceStatus;

        public static void Draw(bool logOpen, bool popupOpen, Rect logRect, float now)
        {
            try { InformationAnchors.BeginPaint(); DrawCore(logOpen, popupOpen, logRect, now); }
            catch (Exception error) { Failure("정보 배치 경계", error); }
            finally { InformationAnchors.EndPaint(); choiceCards.Clear(); }
        }
        private static void DrawCore(bool logOpen, bool popupOpen, Rect logRect, float now)
        {
            if (!Application.isFocused || Screen.width <= 0 || Screen.height <= 0) { mapLoading.Clear(); choiceLoading.Clear(); return; }
            bool nativeWindow = (Enabled(0) || Enabled(1)) && InformationVisibility.Blocked;
            DiceGuideSnapshot dice = !nativeWindow && !logOpen && !popupOpen && DiceGuideSource.Active && diceGuide.Visible && diceGuide.Rows.Count == 1 && string.IsNullOrEmpty(diceGuide.Rows[0].Status)
                ? diceGuide.Rows[0].Value as DiceGuideSnapshot : null;
            if (dice != null && dice.ContextKey != DiceGuideSource.ContextKey) dice = null;
            InformationRow selected = null; string status = "";
            ChoiceStorySnapshot choice = null; string choiceStatus = "";
            if (!DiceGuideSource.Active && !nativeWindow && !logOpen && !popupOpen && choices.Visible && (Enabled(0) || Enabled(1)) && choices.Rows.Count == 1)
            {
                var source = choices.Rows[0];
                var snapshot = source.Value as ChoiceStorySnapshot;
                if (snapshot != null && ChoiceStorySource.IsCurrent(snapshot)) { choice = snapshot; choiceStatus = source.Status ?? ""; }
            }
            if (!DiceGuideSource.Active && !nativeWindow && choice == null && !logOpen && !popupOpen && !ChoiceStorySource.MenuVisible && map.Visible && (Enabled(0) || Enabled(1)) && map.Rows.Count == 1)
            {
                var source = map.Rows[0];
                status = source.Status ?? "";
                if (MapGuideSource.IsCurrent(source.Id))
                {
                    var snapshot = source.Value as MapGuideSnapshot;
                    selected = snapshot != null && snapshot.HasSelection ? snapshot.Summary : null;
                    if (selected != null && !MapGuideSource.IsCurrent(selected)) selected = null;
                }
            }
            MapMarkerSnapshot markerSnapshot = null;
            if (!DiceGuideSource.Active && !nativeWindow && !logOpen && !popupOpen && !ChoiceStorySource.MenuVisible && markers.Visible && (Enabled(0) || Enabled(1)) && markers.Rows.Count == 1)
            {
                var source = markers.Rows[0];
                var snapshot = source.Value as MapMarkerSnapshot;
                if (string.IsNullOrEmpty(source.Status) && MapGuideSource.IsCurrent(snapshot)) markerSnapshot = snapshot;
            }
            var latest = Enabled(2) ? BranchHistory.Latest(now) : null;
            mapLoading.Update(markerSnapshot == null ? null : markerSnapshot.Preparing, markerSnapshot == null ? null : markerSnapshot.ContextKey, now, 64);
            choiceLoading.Update(choice == null || choiceStatus.Length != 0 ? null : choice.Preparing, choice == null ? null : choice.ContextKey, now, 256);
            if (selected == null && latest == null && choice == null && markerSnapshot == null && dice == null) { lastMapRow = null; mapText = null; lastChoice = null; lastChoiceRow = null; choiceText = null; mapFocusRows.Clear(); mapFocus.Clear(); choiceFocus.Clear(); return; }
            int font = Mathf.Clamp(ConfigManager.LogFontSize.Value, 12, 24);
            if (informationStyle == null || informationFont != font)
            {
                informationFont = font; informationStyle = new GUIStyle(GUI.skin.box) { fontSize = font, wordWrap = true, richText = false, alignment = TextAnchor.UpperLeft };
            }
            mapBadgeSize = InformationLayout.BadgeSize(Screen.width, Screen.height, true);
            choiceBadgeSize = InformationLayout.BadgeSize(Screen.width, Screen.height, false);
            if (dice != null)
            {
                // Dice outcome buttons are deliberately non-interactable.
                // Passive geometry must not change their focus/input state.
                foreach (var row in dice.Entries)
                    if (row.HasBadge && row.Anchor != null && InformationAnchors.TryBadgeRect(row.Anchor, choiceBadgeSize, false, out Rect badge))
                        InformationBadge.Draw(badge, default(Rect), row.Guidance && !row.Opportunity);
                if (Event.current != null && Event.current.type == EventType.Repaint)
                    foreach (var anchor in dice.Preparing)
                        if (InformationAnchors.TryBadgeRect(anchor, choiceBadgeSize * .65f, false, out Rect spinner)) InformationSpinner.Draw(spinner, now);
            }
            mapFocusRows.Clear();
            if (markerSnapshot != null) foreach (InformationRow row in markerSnapshot.Entries)
                if (selected == null || string.IsNullOrEmpty(selected.AutoText) || !ReferenceEquals(row.Anchor, selected.Anchor)) mapFocusRows.Add(row);
            if (selected != null && !string.IsNullOrEmpty(selected.AutoText)) mapFocusRows.Add(selected);
            if (markerSnapshot != null) DrawLoading(markerSnapshot.Preparing, mapFocusRows, mapLoading, true, now);
            if (choice != null && choiceStatus.Length == 0) DrawLoading(choice.Preparing, choice.Entries, choiceLoading, false, now);
            InformationRow mapNative = null;
            foreach (InformationRow row in mapFocusRows) if (InformationAnchors.IsFocused(row.Anchor)) { mapNative = row; break; }
            if (mapNative == null) mapNative = selected;
            InformationRow mapCurrent = Focused(mapFocusRows, mapNative, mapFocus, true);
            foreach (InformationRow row in mapFocusRows)
                if (row.HasBadge && (!ReferenceEquals(row, selected) || status.Length == 0)) DrawBadge(row, true);
            if (mapCurrent != null && !string.IsNullOrEmpty(mapCurrent.AutoText))
            {
                string currentStatus = ReferenceEquals(mapCurrent, selected) ? status : "";
                if (!ReferenceEquals(lastMapRow, mapCurrent) || lastMapStatus != currentStatus)
                {
                    lastMapRow = mapCurrent; lastMapStatus = currentStatus;
                    string heading = mapCurrent.Title.Length <= 48 ? mapCurrent.Title : mapCurrent.Title.Substring(0, 45) + "…";
                    // The provider already limits complete notice rows. Never
                    // cut through a status or through the detail navigation hint.
                    mapText = EntryConditionColors("선택 장소: " + heading + (currentStatus.Length == 0 ? "" : " · " + currentStatus) + "\n" + mapCurrent.AutoText, currentStatus.Length == 0);
                }
                // Only the exact positive entry status is green. Mixed rows,
                // headings and stale publications keep their neutral color.
                Rect anchor;
                if (InformationAnchors.TryRect(mapCurrent.Anchor, out anchor)) DrawPassive(mapLabel, mapText, false, default(Rect), false, true, NoticeAnchor(anchor, mapCurrent.HasBadge && currentStatus.Length == 0, true));
            }
            if (choice != null)
            {
                InformationRow current = Focused(choice.Entries, ChoiceStorySource.CurrentRow(choice), choiceFocus, false);
                if (choiceStatus.Length == 0) foreach (InformationRow row in choice.Entries) if (row.HasBadge) DrawBadge(row, false);
                if (!ReferenceEquals(lastChoice, choice) || !ReferenceEquals(lastChoiceRow, current) || lastChoiceStatus != choiceStatus)
                {
                    lastChoice = choice; lastChoiceRow = current; lastChoiceStatus = choiceStatus;
                    var lines = new List<string>();
                    lines.Add("현재 선택의 안내" + (current != null && InformationAnchors.IsCard(current.Anchor) ? ": " + current.Title : "") + (choiceStatus.Length == 0 ? "" : " · " + choiceStatus));
                    if (current != null) lines.Add("• " + current.AutoText);
                    lines.Add("자세히: 로그 → 도구 → 지도 안내");
                    choiceText = EntryConditionColors(string.Join("\n", lines), choiceStatus.Length == 0);
                }
                Rect anchor;
                if (current != null && InformationAnchors.IsCard(current.Anchor))
                {
                    choiceCards.Clear();
                    foreach (object target in choice.Anchors)
                        if (InformationAnchors.TryCardRect(target, out anchor)) { choiceCards.Add(anchor); if (choiceCards.Count > 64) break; }
                    if (InformationAnchors.TryCardRect(current.Anchor, out anchor))
                    {
                        if (choiceCards.Count == 0) choiceCards.Add(anchor);
                        DrawPassive(choiceLabel, choiceText, false, default(Rect), false, true, anchor, choiceCards);
                    }
                }
                else if (current != null && InformationAnchors.TryRect(current.Anchor, out anchor)) DrawPassive(choiceLabel, choiceText, false, default(Rect), false, true, NoticeAnchor(anchor, current.HasBadge && choiceStatus.Length == 0, false));
            }
            if (latest != null) DrawPassive(branchLabel, latest.AutoText, true, logOpen ? logRect : default(Rect), latest.Available);
        }
        private static InformationRow Focused(IList<InformationRow> rows, InformationRow native, InformationFocus focus, bool map)
        {
            if (native != null && string.IsNullOrEmpty(native.AutoText)) native = null;
            var ev = Event.current;
            Vector2 pointer = ev == null ? default(Vector2) : ev.mousePosition;
            if (!focus.Pointer(native == null ? null : native.Anchor, pointer, ev != null)) return native;
            foreach (InformationRow row in rows)
            {
                if (string.IsNullOrEmpty(row.AutoText)) continue;
                Rect anchor;
                if (row.Anchor == null || !InformationAnchors.IsInteractive(row.Anchor) || !InformationAnchors.TryRect(row.Anchor, out anchor)) continue;
                Rect card, badge;
                if (!map && InformationAnchors.TryCardRect(row.Anchor, out card)) anchor = card;
                if (anchor.Contains(pointer) || row.HasBadge && InformationAnchors.TryBadgeRect(row.Anchor, map ? mapBadgeSize : choiceBadgeSize, map, out badge) && badge.Contains(pointer)) return row;
            }
            return null;
        }
        private static Rect NoticeAnchor(Rect anchor, bool opportunity, bool map)
        { return opportunity ? InformationLayout.IncludingBadge(anchor, InformationLayout.Badge(anchor, new Rect(0, 0, Screen.width, Screen.height), map ? mapBadgeSize : choiceBadgeSize, map)) : anchor; }
        private static void DrawBadge(InformationRow row, bool map)
        {
            Rect anchor;
            if (row.Anchor == null || !InformationAnchors.IsInteractive(row.Anchor) || !InformationAnchors.TryRect(row.Anchor, out anchor)) return;
            Rect badge;
            if (!InformationAnchors.TryBadgeRect(row.Anchor, map ? mapBadgeSize : choiceBadgeSize, map, out badge)) return;
            try { InformationBadge.Draw(badge, map ? InformationLayout.Stem(badge, anchor) : default(Rect), row.Guidance && !row.Opportunity); }
            catch (Exception error) { Failure("안내 아이콘", error); }
        }
        private static void DrawLoading(IList<object> pending, IList<InformationRow> entries, InformationLoadingState loading, bool map, float now)
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            for (int at = 0; at < Math.Min(pending.Count, map ? 64 : 256); at++)
            {
                object anchor = pending[at];
                if (!loading.Visible(anchor, now) || !InformationAnchors.IsInteractive(anchor)) continue;
                bool hasBadge = false;
                foreach (InformationRow row in entries) if (ReferenceEquals(row.Anchor, anchor) && row.HasBadge) { hasBadge = true; break; }
                // A known positive always wins, including a newer selected row.
                if (hasBadge) continue;
                Rect badge;
                if (!InformationAnchors.TryBadgeRect(anchor, map ? mapBadgeSize : choiceBadgeSize, map, out badge)) continue;
                float size = badge.width * .65f;
                Rect spinner = new Rect(badge.x + (badge.width - size) * .5f, badge.y + (badge.height - size) * .5f, size, size);
                try { InformationSpinner.Draw(spinner, now); }
                catch (Exception error) { Failure("안내 준비 표시", error); }
            }
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
        private static void DrawPassive(PassiveLabel label, string text, bool right, Rect occlusion, bool available, bool neutral = false, Rect? anchor = null, IList<Rect> cards = null)
        {
            informationStyle.richText = neutral;
            label.Prepare(text, informationStyle, right, occlusion, anchor, cards);
            if (!label.CanDraw || string.IsNullOrEmpty(label.Content.text)) return;
            informationStyle.normal.textColor = neutral ? new Color(.95f, .95f, .95f) : available ? new Color(.75f, 1f, .8f) : new Color(1f, .88f, .55f);
            GUI.Label(label.Rect, label.Content, informationStyle);
        }
    }
}
