using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DiceMaster.Config;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using UnityEngine;

namespace DiceMaster.LogUI
{
    // A view of the running decision. It never discovers candidates or invokes
    // a game result; explicit commands only queue a request for the Lua gate.
    internal sealed class EventPopupRenderer
    {
        private string selected, pointerControl;
        private long token = -1, pointerToken;
        private string pointerCandidate, pointerResult, pointerSelection;
        private object pointerTarget;
        private bool pointerRowConfirmation, pointerPrimaryAvailable, pointerPrimaryForce;
        private string primaryCandidate, primaryResult;
        private object primaryTarget;
        private bool primaryAvailable, primaryForce;
        private bool neutral = true, follow = true;
        private float scroll, detailScroll, maximumDetailScroll;
        private string detailCandidate, detailResult;
        private object detailTarget;
        private readonly HashSet<KeyCode> keys = new HashSet<KeyCode>();
        private readonly Dictionary<string, Rect> rowBounds = new Dictionary<string, Rect>();
        public Func<object, string> OneShotEventTag { get; set; }
        public Func<object, string> OneShotEventDetail { get; set; }
        private readonly Dictionary<string, string> plainTitles = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<LineKey, string> singleLines = new Dictionary<LineKey, string>();
        private static readonly Regex Markup = new Regex(@"</?(?:color|size|b|i|material)(?:=[^<>]*)?>", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private GUIStyle text, mutedText, resultText, button, highlighted, rowMeasure, titleMeasure, chrome;
        private int font, chromeFont;
        private Action deferred;
        internal string InputHint = "";

        private struct LineKey : IEquatable<LineKey>
        {
            internal readonly string Value;
            internal readonly GUIStyle Style;
            internal readonly float Width;
            internal LineKey(string value, GUIStyle style, float width) { Value = value; Style = style; Width = width; }
            public bool Equals(LineKey other) => Value == other.Value && ReferenceEquals(Style, other.Style) && Width == other.Width;
            public override bool Equals(object other) => other is LineKey && Equals((LineKey)other);
            public override int GetHashCode() => Value.GetHashCode() ^ Style.GetHashCode() ^ Width.GetHashCode();
        }

        internal void ResetInput(bool preservePointerTransaction = false)
        {
            // Pointer takeover can arrive in Update after its up already
            // completed selection. Do not require an extra neutral frame then:
            // the next fresh press must remain usable. Preserve any existing
            // entry/confirmation barrier; invalid active presses still cancel.
            if (preservePointerTransaction && (pointerControl == null || PointerValid())) return;
            neutral = true;
            pointerControl = null;
            keys.Clear();
        }

        internal void UpdateInput()
        {
            Synchronize();
            if (!EventSelectionRuntime.Waiting) return;
            if (neutral && Application.isFocused && !Input.anyKey && Input.touchCount == 0)
                neutral = false;
        }

        private void Synchronize()
        {
            if (token != EventSelectionRuntime.Token)
            {
                token = EventSelectionRuntime.Token;
                selected = null;
                scroll = detailScroll = 0;
                maximumDetailScroll = 0;
                detailCandidate = detailResult = null;
                detailTarget = null;
                plainTitles.Clear();
                singleLines.Clear();
                ResetInput();
                CapturePrimaryIntent();
                follow = true;
            }
            if (selected != null && Find(selected) == null)
            {
                selected = null;
                ResetInput();
                CapturePrimaryIntent();
                follow = true;
            }
            if (!EventSelectionRuntime.Waiting || (pointerControl != null && !PointerValid())) ResetInput();
        }

        private bool PointerValid()
        {
            if (pointerControl == null || !EventSelectionRuntime.Waiting || pointerToken != token
                || token != EventSelectionRuntime.Token || pointerSelection != selected) return false;
            EventCandidate row = Find(pointerCandidate);
            if (pointerCandidate != null && (row == null || !ReferenceEquals(row.Target, pointerTarget)
                || row.Result != pointerResult)) return false;
            if (pointerRowConfirmation && pointerPrimaryAvailable && pointerCandidate != null
                && (row == null || (pointerPrimaryForce ? !row.CanForce : !row.CanSelect))) return false;
            return true;
        }

        // Keep the action the user last saw (or explicitly selected). A normal
        // confirmation must never silently become a condition bypass.
        private void CapturePrimaryIntent()
        {
            primaryCandidate = selected;
            EventCandidate row = Find(selected);
            primaryTarget = row?.Target;
            primaryResult = row?.Result;
            primaryForce = row != null && row.CanForce;
            primaryAvailable = row != null && (row.CanSelect || row.CanForce);
        }

        private void SubmitPrimary()
        {
            EventCandidate row = Find(selected);
            if (!primaryAvailable || primaryCandidate != selected || row == null
                || !ReferenceEquals(row.Target, primaryTarget) || row.Result != primaryResult) return;
            Submit(primaryForce, false);
        }

        private static EventCandidate Find(string id)
        {
            foreach (EventCandidate item in EventSelectionRuntime.Candidates)
                if (item.Id == id) return item;
            return null;
        }

        internal void Handle(LogPadCommand command)
        {
            Synchronize();
            if (!EventSelectionRuntime.Waiting) return;
            if (command != LogPadCommand.None) pointerControl = null;
            if (command == LogPadCommand.Up || command == LogPadCommand.Down)
            {
                var ids = new List<string>();
                foreach (EventCandidate row in EventSelectionRuntime.Candidates) ids.Add(row.Id);
                int current = ids.IndexOf(selected);
                selected = ids.Count == 0 ? null : ids[current < 0 ? 0
                    : Math.Max(0, Math.Min(ids.Count - 1, current + (command == LogPadCommand.Up ? -1 : 1)))];
                detailScroll = 0; follow = true;
                CapturePrimaryIntent();
            }
            else if (command == LogPadCommand.Back) Submit(false, true);
            else if (command == LogPadCommand.Activate) SubmitPrimary();
        }

        internal void Scroll(float value)
        {
            Synchronize();
            if (!EventSelectionRuntime.Waiting || !Application.isFocused || neutral
                || float.IsNaN(value) || float.IsInfinity(value)) return;
            detailScroll = Mathf.Clamp(detailScroll + value * 520f * Mathf.Clamp(Time.unscaledDeltaTime, 0, .1f),
                0, maximumDetailScroll);
        }

        private void Submit(bool force, bool random)
        {
            if (!EventSelectionRuntime.Waiting || token != EventSelectionRuntime.Token) return;
            if (random) EventSelectionRuntime.ContinueRandom();
            else
            {
                EventCandidate row = Find(selected);
                if (row == null || (force ? !row.CanForce : !row.CanSelect)) return;
                EventSelectionRuntime.Select(selected, force);
            }
            ResetInput();
        }

        private void Styles(int body, int small)
        {
            if (text != null && body == font && small == chromeFont) return;
            singleLines.Clear();
            font = body; chromeFont = small;
            text = new GUIStyle(GUI.skin.label) { fontSize = body, wordWrap = true, richText = false };
            mutedText = new GUIStyle(text);
            mutedText.normal.textColor = new Color(.7f, .7f, .7f);
            resultText = new GUIStyle(text) { richText = true };
            resultText.normal.textColor = Color.white;
            button = new GUIStyle(GUI.skin.button) { fontSize = body, wordWrap = true, richText = true };
            button.normal.textColor = Color.white;
            rowMeasure = new GUIStyle(button) { richText = false };
            titleMeasure = new GUIStyle(rowMeasure) { fontStyle = FontStyle.Bold };
            highlighted = new GUIStyle(button);
            highlighted.normal.textColor = Color.yellow;
            chrome = new GUIStyle(text) { fontSize = small };
            foreach (GUIStyle style in new[] { text, mutedText, resultText, button, highlighted, rowMeasure, titleMeasure, chrome })
            {
                style.margin = new RectOffset(0, 0, 0, 0);
                style.fixedWidth = style.fixedHeight = 0;
            }
        }

        internal void Draw()
        {
            Synchronize();
            if (!EventSelectionRuntime.Waiting || !Application.isFocused || Screen.width <= 0 || Screen.height <= 0) return;
            int size = ConfigManager.LogFontSize.Value;
            // Resolve the same startup/default font as the large log without
            // persisting a second popup layout or changing custom settings.
            var saved = new Rect(ConfigManager.LogWindowX.Value, ConfigManager.LogWindowY.Value,
                ConfigManager.LogWindowWidth.Value, ConfigManager.LogWindowHeight.Value);
            if (LogLayout.IsDefaultSeed(saved, size)) size = LogLayout.DefaultFont(Screen.height);
            size = Mathf.Clamp(size, LogLayout.MinimumFont, LogLayout.MaximumFont);
            int small = Math.Min(size, Math.Max(12, Screen.height / 26));
            Styles(size, small);
            float margin = Math.Min(16 * LogLayout.ScreenScale(Screen.height), Screen.width * .02f);
            float width = Math.Min(Screen.width - margin * 2, Math.Max(360, Math.Min(680, Screen.width * .42f)));
            float inner = Math.Max(1, width - 24);
            float contentWidth = Math.Max(1, inner - Math.Max(20,
                GUI.skin.verticalScrollbar.fixedWidth + (GUI.skin.verticalScrollbar.margin?.left ?? 0)));
            float actionHeight = chrome.CalcHeight(new GUIContent("[A / Enter] 조건 무시하고 이번만 진행"), inner) + 6;
            string heading = EventSelectionRuntime.PopupTitle;
            float headingHeight = chrome.CalcHeight(new GUIContent(heading), inner) + 8;
            string hint = "↑↓ 선택 · 오른쪽 스틱·휠로 상세 읽기";
            if (!string.IsNullOrEmpty(InputHint)) hint += "\n" + OneLine(InputHint, chrome, inner);
            float hintHeight = chrome.CalcHeight(new GUIContent(hint), inner) + 6;
            string status = OneLine(EventSelectionRuntime.Status ?? "", chrome, inner);
            float statusHeight = string.IsNullOrEmpty(status) ? 0 : chrome.CalcHeight(new GUIContent(status), inner);
            float controls = 24 + headingHeight + hintHeight + statusHeight + actionHeight * 2 + 20;
            float height = Math.Min(Screen.height - margin * 2, Math.Max(Screen.height * .8f, controls + size * 8 + 24));
            float paneHeight = Math.Max(2, height - controls);
            float listHeight = Math.Max(1, Math.Min(Screen.height * .34f, paneHeight * .45f));
            float detailHeight = Math.Max(1, paneHeight - listHeight);
            Rect bounds = new Rect(margin, Screen.height - margin - height, width, height);
            HandleKeyboard();
            if (Event.current.type == EventType.Repaint) CapturePrimaryIntent();
            deferred = null;
            GUI.Box(bounds, "");
            GUILayout.BeginArea(new Rect(bounds.x + 12, bounds.y + 12, inner, Math.Max(1, bounds.height - 24)));
            GUILayout.Label(heading, chrome);
            rowBounds.Clear();
            Vector2 position = GUILayout.BeginScrollView(new Vector2(0, scroll), false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Height(listHeight));
            scroll = position.y;
            foreach (EventCandidate row in EventSelectionRuntime.Candidates)
            {
                string title = (row.Title ?? "") + (OneShotEventTag?.Invoke(row.Target) ?? "");
                if (!plainTitles.TryGetValue(title, out string plain))
                    plainTitles[title] = plain = Markup.Replace(title, "");
                // A random or unsupported condition is unknown in either
                // decision kind, rather than evidence that it is false.
                bool muted = row.Eligibility?.State == EventEligibilityState.Ineligible;
                Row(row.Id, plain, row.ResultSummaryRich, contentWidth, muted,
                    Reason(row));
            }
            Rect last = GUILayoutUtility.GetLastRect();
            GUILayout.EndScrollView();
            if (follow && selected != null && Event.current.type == EventType.Repaint && rowBounds.TryGetValue(selected, out Rect target))
            {
                scroll = LogLayout.BringIntoView(scroll, target, listHeight, last.yMax);
                follow = false;
            }
            EventCandidate choice = Find(selected);
            if (detailCandidate != selected || !ReferenceEquals(detailTarget, choice?.Target) || detailResult != choice?.Result)
            {
                detailCandidate = selected; detailTarget = choice?.Target; detailResult = choice?.Result;
                detailScroll = 0;
            }
            string selectionExplanation = EventSelectionRuntime.IsSwitch
                ? "분기를 선택하면 예상 결과와 분기 조건을 확인할 수 있습니다."
                : "이벤트를 선택하면 예상 결과와 발동 조건을 확인할 수 있습니다.";
            string conditionHeading = "조건 상세";
            string reason = choice == null ? "" : DetailReason(choice);
            string conditions = choice?.ConditionDetail ?? choice?.Summary ?? "";
            // The delegate returns prepared proof text only. A switch branch
            // has no event activation evidence and never requests this detail.
            string oneShotDetail = choice == null || EventSelectionRuntime.IsSwitch ? "" : OneShotEventDetail?.Invoke(choice.Target) ?? "";
            bool separateReason = choice != null && !string.IsNullOrEmpty(reason) && choice.Summary != conditions;
            if (choice != null && choice.Summary == conditions) conditions = Reason(choice);
            float detailContentHeight = choice == null ? text.CalcHeight(new GUIContent(selectionExplanation), contentWidth)
                : (separateReason ? text.CalcHeight(new GUIContent(reason), contentWidth) + 8 : 0)
                    + text.CalcHeight(new GUIContent(conditionHeading), contentWidth)
                    + text.CalcHeight(new GUIContent(conditions), contentWidth)
                    + text.CalcHeight(new GUIContent("예상 결과"), contentWidth)
                    + text.CalcHeight(new GUIContent(EventResultPreview.PlainText(choice.ResultDetailRich)), contentWidth) + 16
                    + (oneShotDetail.Length == 0 ? 0 : text.CalcHeight(new GUIContent(oneShotDetail), contentWidth) + 8);
            maximumDetailScroll = Math.Max(0, detailContentHeight - detailHeight);
            detailScroll = Mathf.Clamp(detailScroll, 0, maximumDetailScroll);
            Vector2 detail = GUILayout.BeginScrollView(new Vector2(0, detailScroll), false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Height(detailHeight));
            detailScroll = Mathf.Clamp(detail.y, 0, maximumDetailScroll);
            if (choice == null) DetailLabel(selectionExplanation, text, contentWidth);
            else
            {
                GUIStyle conditionStyle = choice.Eligibility?.State == EventEligibilityState.Ineligible ? mutedText : text;
                if (separateReason)
                {
                    DetailLabel(reason, conditionStyle, contentWidth);
                    GUILayout.Space(8);
                }
                DetailLabel(conditionHeading, text, contentWidth);
                DetailLabel(conditions, conditionStyle, contentWidth);
                GUILayout.Space(8);
                DetailLabel("예상 결과", text, contentWidth);
                DetailLabel(choice.ResultDetailRich, resultText, contentWidth);
                GUILayout.Space(8);
                if (oneShotDetail.Length != 0)
                {
                    DetailLabel(oneShotDetail, text, contentWidth);
                    GUILayout.Space(8);
                }
            }
            GUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(status)) GUILayout.Label(status, chrome);
            bool force = choice != null && choice.CanForce;
            string caption = choice == null ? "[A / Enter] " + EventSelectionRuntime.PrimaryNoun + "를 선택하세요"
                : force ? "[A / Enter] 조건 무시하고 이번만 진행"
                : choice.CanSelect ? "[A / Enter] 이 " + EventSelectionRuntime.PrimaryNoun + "로 진행"
                : "[A / Enter] 진행 불가 · 대상/결과 확인 필요";
            GUILayout.Label(caption, chrome);
            GUILayout.Label(EventSelectionRuntime.IsSwitch ? "[B / Esc] 원래 판정으로 진행" : "[B / Esc] 무작위로 진행", chrome);
            GUILayout.Label(hint, chrome);
            GUILayout.EndArea();
            Action action = deferred; deferred = null;
            action?.Invoke();
            Event evt = Event.current;
            if (IsUp(evt) || evt.type == EventType.TouchLeave) pointerControl = null;
            if (bounds.Contains(evt.mousePosition) && (evt.type == EventType.MouseDown || evt.type == EventType.MouseUp || evt.type == EventType.ScrollWheel)) evt.Use();
        }

        private static string Reason(EventCandidate choice)
        {
            string summary = choice.Summary ?? "";
            bool unreadable = choice.Target == null || string.IsNullOrEmpty(choice.Result)
                || choice.Eligibility?.CanUseResult == false;
            string marker = unreadable ? "!" : choice.Eligibility?.State == EventEligibilityState.Unknown ? "?" : null;
            if (marker == null || summary.StartsWith(marker, StringComparison.Ordinal)) return summary;
            return marker + " " + (string.IsNullOrEmpty(summary)
                ? unreadable ? "대상/결과 확인 필요" : "판정 미확정" : summary);
        }

        private static string DetailReason(EventCandidate choice)
        {
            bool unreadable = choice.Target == null || string.IsNullOrEmpty(choice.Result)
                || choice.Eligibility?.CanUseResult == false;
            if (!unreadable && choice.Eligibility?.State == EventEligibilityState.Eligible) return "";
            string reason = Reason(choice);
            int separator = reason.IndexOf(" · ", StringComparison.Ordinal);
            return separator < 0 ? reason : reason.Substring(0, separator);
        }

        private string OneLine(string value, GUIStyle style, float width)
        {
            var key = new LineKey(value ?? "", style, width);
            if (singleLines.TryGetValue(key, out string cached)) return cached;
            string line = (value ?? "").Replace('\r', ' ').Replace('\n', ' ');
            if (line.Length > 0 && style.CalcSize(new GUIContent(line)).x > width)
            {
                int low = 0, high = Math.Min(line.Length, 160);
                while (low < high)
                {
                    int middle = (low + high + 1) / 2;
                    if (style.CalcSize(new GUIContent(line.Substring(0, middle) + "…")).x <= width) low = middle;
                    else high = middle - 1;
                }
                // Avoid separating a UTF-16 surrogate pair when the visible
                // text ends at an emoji or another supplementary character.
                if (low > 0 && char.IsHighSurrogate(line[low - 1])) low--;
                line = line.Substring(0, low) + "…";
            }
            // Repeated native text measurements are unnecessary between GUI
            // callbacks; the small cache belongs to this decision/font only.
            if (singleLines.Count >= 512) singleLines.Clear();
            singleLines[key] = line;
            return line;
        }

        private static void DetailLabel(string value, GUIStyle style, float width)
        {
            GUILayout.Label(value, style, GUILayout.MinWidth(1), GUILayout.MaxWidth(width), GUILayout.ExpandWidth(true));
        }

        private string ResultLine(string rich, float width)
        {
            rich = (rich ?? "").Replace('\r', ' ').Replace('\n', ' ');
            string plain = EventResultPreview.PlainText(rich);
            string visible = OneLine(plain, rowMeasure, width);
            return visible == plain ? rich : EventResultPreview.FitSummary(rich, Math.Max(0, visible.Length - 1));
        }

        private void Row(string id, string title, string resultRich, float width, bool muted, string condition)
        {
            GUIStyle style = selected == id ? highlighted : button;
            float lineWidth = Math.Max(1, width);
            string marker = selected == id ? "▶ " : "  ";
            float markerWidth = Math.Max(rowMeasure.CalcSize(new GUIContent("▶ ")).x,
                rowMeasure.CalcSize(new GUIContent("  ")).x) - rowMeasure.CalcSize(GUIContent.none).x;
            string titleLine = OneLine(EventResultPreview.Escape(title), titleMeasure, Math.Max(1, lineWidth - markerWidth));
            if (muted)
            {
                titleLine = "<color=#B3B3B3>" + titleLine + "</color>";
            }
            // Both decisions own three lines. Condition excerpts
            // stay one line; the selected detail shows the complete plain text
            // before results. Selection never changes row geometry.
            // Literal titles are escaped before
            // this renderer adds bold emphasis; only previews supply color spans.
            // The result's own colors also survive selected-row highlighting.
            string caption = marker + "<b>" + titleLine + "</b>"
                + "\n<color=#FFFFFF>" + ResultLine(resultRich, lineWidth) + "</color>";
            string conditionLine = EventResultPreview.Escape(OneLine("조건: " + (condition ?? ""), rowMeasure, lineWidth));
            caption += "\n<color=" + (muted ? "#B3B3B3" : "#FFFFFF") + ">" + conditionLine + "</color>";
            SafeButton(caption, "row:" + id,
                () => {
                    if (selected == id)
                    {
                        // This intent belongs to the fresh press, rather than
                        // a later repaint whose eligibility may have changed.
                        if (pointerRowConfirmation && pointerPrimaryAvailable) Submit(pointerPrimaryForce, false);
                    }
                    else { selected = id; detailScroll = 0; CapturePrimaryIntent(); }
                }, style, width);
            rowBounds[id] = GUILayoutUtility.GetLastRect();
        }

        private void SafeButton(string caption, string identity, Action action, GUIStyle style, float width = 0)
        {
            Event evt = Event.current;
            var content = new GUIContent(caption);
            // GUI.Button consumes MouseDown/Up. GetLastRect after that would
            // return a dummy Used-event rectangle and lose the transaction.
            Rect rect = GUILayoutUtility.GetRect(content, style, GUILayout.MinWidth(1),
                GUILayout.MaxWidth(width > 0 ? width : Screen.width), GUILayout.ExpandWidth(true));
            ImGuiPointerButton.Result input = ImGuiPointerButton.Draw(rect, content, style);
            if (input.Down && evt.button == 0 && rect.Contains(evt.mousePosition) && GUI.enabled && !neutral)
            {
                pointerControl = identity; pointerToken = token;
                pointerSelection = selected;
                pointerCandidate = identity.StartsWith("row:", StringComparison.Ordinal) ? identity.Substring(4) : selected;
                EventCandidate row = Find(pointerCandidate);
                pointerTarget = row?.Target; pointerResult = row?.Result;
                pointerRowConfirmation = identity.StartsWith("row:", StringComparison.Ordinal) && pointerCandidate == selected;
                pointerPrimaryForce = primaryForce;
                pointerPrimaryAvailable = primaryAvailable && primaryCandidate == pointerCandidate
                    && ReferenceEquals(primaryTarget, pointerTarget) && primaryResult == pointerResult;
                GamepadBridge.NotifyPointerActivity();
            }
            if (input.Clicked && input.Up && GUI.enabled && !neutral && pointerControl == identity && PointerValid())
                deferred = action;
        }

        private static bool IsUp(Event evt) => evt.type == EventType.MouseUp || evt.type == EventType.TouchUp
            || (evt.type == EventType.Used && (evt.rawType == EventType.MouseUp || evt.rawType == EventType.TouchUp));

        private void HandleKeyboard()
        {
            Event evt = Event.current;
            if (evt.type == EventType.KeyUp) keys.Remove(evt.keyCode);
            if (evt.type != EventType.KeyDown) return;
            bool fresh = keys.Add(evt.keyCode);
            if (neutral || !fresh) { evt.Use(); return; }
            LogPadCommand command = evt.keyCode == KeyCode.UpArrow ? LogPadCommand.Up
                : evt.keyCode == KeyCode.DownArrow ? LogPadCommand.Down
                : evt.keyCode == KeyCode.Escape ? LogPadCommand.Back
                : evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter ? LogPadCommand.Activate
                : LogPadCommand.None;
            if (command == LogPadCommand.None) return;
            GamepadBridge.NotifyKeyboardActivity();
            Handle(command);
            evt.Use();
        }
    }
}
