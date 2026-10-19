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
        private const string Random = "";
        private string selected = Random, pointerControl;
        private long token = -1, pointerToken;
        private bool neutral = true, follow = true;
        private float scroll, detailScroll;
        private readonly HashSet<KeyCode> keys = new HashSet<KeyCode>();
        private readonly Dictionary<string, Rect> rowBounds = new Dictionary<string, Rect>();
        private readonly Dictionary<string, string> plainTitles = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Regex Markup = new Regex("<[^>]*>", RegexOptions.CultureInvariant);
        private GUIStyle text, button, highlighted, chrome, chromeButton, chromeSelected;
        private int font, chromeFont;
        private Action deferred;
        internal string InputHint = "";

        internal void ResetInput()
        {
            neutral = true;
            pointerControl = null;
            keys.Clear();
        }

        internal void UpdateInput()
        {
            Synchronize();
            if (neutral && Application.isFocused && !Input.anyKey && Input.touchCount == 0)
                neutral = false;
        }

        private void Synchronize()
        {
            if (token != EventSelectionRuntime.Token)
            {
                token = EventSelectionRuntime.Token;
                selected = Random;
                scroll = detailScroll = 0;
                plainTitles.Clear();
                ResetInput();
                follow = true;
            }
            if (selected != Random && Find(selected) == null)
            {
                selected = Random;
                ResetInput();
                follow = true;
            }
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
                var ids = new List<string> { Random };
                foreach (EventCandidate row in EventSelectionRuntime.Candidates) ids.Add(row.Id);
                int current = Math.Max(0, ids.IndexOf(selected));
                selected = ids[Math.Max(0, Math.Min(ids.Count - 1, current + (command == LogPadCommand.Up ? -1 : 1)))];
                detailScroll = 0; follow = true;
            }
            else if (command == LogPadCommand.Back) Submit(false, true);
            else if (command == LogPadCommand.Activate) Submit(false, selected == Random);
            else if (command == LogPadCommand.Refresh) Submit(true, false); // Existing shared X mapping.
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
            font = body; chromeFont = small;
            text = new GUIStyle(GUI.skin.label) { fontSize = body, wordWrap = true, richText = false };
            button = new GUIStyle(GUI.skin.button) { fontSize = body, wordWrap = true, richText = false };
            highlighted = new GUIStyle(button);
            highlighted.normal.textColor = Color.yellow;
            chrome = new GUIStyle(text) { fontSize = small };
            chromeButton = new GUIStyle(button) { fontSize = small };
            chromeSelected = new GUIStyle(chromeButton);
            chromeSelected.normal.textColor = Color.yellow;
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
            float actionHeight = chromeButton.CalcHeight(new GUIContent("조건 무시하고 이번만 진행 [X / Shift+Enter]"), inner) + 6;
            float headingHeight = chrome.CalcHeight(new GUIContent("이번 행동의 이벤트 선택"), inner) + 8;
            string hint = "↑↓ 선택 · A/Enter 진행 · X/Shift+Enter 이번만 무시 · B/Esc 무작위 · Tab 로그";
            if (!string.IsNullOrEmpty(InputHint)) hint += "\n" + InputHint;
            float hintHeight = chrome.CalcHeight(new GUIContent(hint), inner) + 6;
            float statusHeight = chrome.CalcHeight(new GUIContent(EventSelectionRuntime.Status ?? ""), inner);
            float detailHeight = Math.Min(size * 3.5f + 8, Screen.height * .18f);
            float controls = 24 + headingHeight + hintHeight + statusHeight + detailHeight + actionHeight * 3 + 20;
            float listHeight = Math.Min(Screen.height * .34f, Math.Max(size * 2 + 24, EventSelectionRuntime.Candidates.Count * (size * 2 + 12)));
            float height = Math.Min(Screen.height - margin * 2, controls + listHeight);
            listHeight = Math.Max(1, height - controls);
            Rect bounds = new Rect(margin, Screen.height - margin - height, width, height);
            HandleKeyboard();
            deferred = null;
            GUI.Box(bounds, "");
            GUILayout.BeginArea(new Rect(bounds.x + 12, bounds.y + 12, inner, Math.Max(1, bounds.height - 24)));
            GUILayout.Label("이번 행동의 이벤트 선택", chrome);
            rowBounds.Clear();
            Vector2 position = GUILayout.BeginScrollView(new Vector2(0, scroll), false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Height(listHeight));
            scroll = position.y;
            Row(Random, "무작위로 진행 · 원래 게임 판정", inner);
            foreach (EventCandidate row in EventSelectionRuntime.Candidates)
            {
                string title = row.Title ?? "";
                if (!plainTitles.TryGetValue(title, out string plain))
                    plainTitles[title] = plain = Markup.Replace(title, "");
                Row(row.Id, plain + "\n" + row.State, inner);
            }
            Rect last = GUILayoutUtility.GetLastRect();
            GUILayout.EndScrollView();
            if (follow && Event.current.type == EventType.Repaint && rowBounds.TryGetValue(selected, out Rect target))
            {
                scroll = LogLayout.BringIntoView(scroll, target, listHeight, last.yMax);
                follow = false;
            }
            EventCandidate choice = Find(selected);
            Vector2 detail = GUILayout.BeginScrollView(new Vector2(0, detailScroll), false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Height(detailHeight));
            detailScroll = detail.y;
            GUILayout.Label(choice == null ? "지정 없이 게임의 조건·가중치·난수 판정으로 진행합니다." : choice.Summary, text,
                GUILayout.MinWidth(1), GUILayout.MaxWidth(inner - 20), GUILayout.ExpandWidth(true));
            GUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(EventSelectionRuntime.Status)) GUILayout.Label(EventSelectionRuntime.Status, chrome);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && choice != null && choice.CanSelect;
            SafeButton("이 이벤트로 진행 [A / Enter]", "normal:" + selected, () => Submit(false, false), chromeButton);
            GUI.enabled = enabled && choice != null && choice.CanForce;
            SafeButton("조건 무시하고 이번만 진행 [X / Shift+Enter]", "force:" + selected, () => Submit(true, false), chromeButton);
            GUI.enabled = enabled;
            SafeButton("무작위로 진행 [B / Esc]", "random", () => Submit(false, true), selected == Random ? chromeSelected : chromeButton);
            GUILayout.Label(hint, chrome);
            GUILayout.EndArea();
            Action action = deferred; deferred = null;
            action?.Invoke();
            Event evt = Event.current;
            if (evt.type == EventType.MouseUp || evt.rawType == EventType.MouseUp) pointerControl = null;
            if (bounds.Contains(evt.mousePosition) && (evt.type == EventType.MouseDown || evt.type == EventType.MouseUp || evt.type == EventType.ScrollWheel)) evt.Use();
        }

        private void Row(string id, string caption, float width)
        {
            SafeButton((selected == id ? "▶ " : "") + caption, "row:" + id,
                () => { selected = id; detailScroll = 0; }, selected == id ? highlighted : button, width - 20);
            rowBounds[id] = GUILayoutUtility.GetLastRect();
        }

        private void SafeButton(string caption, string identity, Action action, GUIStyle style, float width = 0)
        {
            Event evt = Event.current;
            bool down = evt.type == EventType.MouseDown || evt.rawType == EventType.MouseDown;
            bool up = evt.type == EventType.MouseUp || evt.rawType == EventType.MouseUp;
            bool clicked = GUILayout.Button(caption, style, GUILayout.MinWidth(1), GUILayout.MaxWidth(width > 0 ? width : Screen.width), GUILayout.ExpandWidth(true));
            Rect rect = GUILayoutUtility.GetLastRect();
            if (down && evt.button == 0 && rect.Contains(evt.mousePosition) && GUI.enabled && !neutral)
            {
                pointerControl = identity; pointerToken = token;
                GamepadBridge.NotifyPointerActivity();
            }
            if (clicked && up && GUI.enabled && !neutral && pointerControl == identity && pointerToken == token)
                deferred = action;
        }

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
                : evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter ? (evt.shift ? LogPadCommand.Refresh : LogPadCommand.Activate)
                : LogPadCommand.None;
            if (command == LogPadCommand.None) return;
            GamepadBridge.NotifyKeyboardActivity();
            Handle(command);
            evt.Use();
        }
    }
}
