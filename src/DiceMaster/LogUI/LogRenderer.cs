using System;
using System.Collections.Generic;
using DiceMaster.Config;
using DiceMaster.Gamepad;
using DiceMaster.LiveLog;
using UnityEngine;
using Display = DiceMaster.EmbeddedTranslations.Display;

namespace DiceMaster.LogUI
{
    // This view consumes already-published data only. Layout, searching and focus
    // never discover game rows, evaluate Lua or change a reservation themselves.
    public sealed class LogRenderer
    {
        private enum Area { List, Detail, Tools, Keyboard }
        private static readonly string[] Tabs = { "선택지", "조건", "이벤트", "미션" };
        private readonly LogModel model;
        private readonly Action refresh, close;
        private readonly LocalSearchKeyboard keyboard = new LocalSearchKeyboard();
        private readonly Dictionary<string, Rect> rowRects = new Dictionary<string, Rect>(StringComparer.Ordinal);
        private readonly HashSet<KeyCode> heldKeys = new HashSet<KeyCode>();
        private Rect window;
        private Rect savedWindow;
        private Area area, previousArea;
        private int currentTabIndex, toolIndex, detailIndex, keyboardIndex, renderedFont, chromeFont;
        private float toolScroll, keyboardScroll, lastSave;
        private bool ensureVisible, stylesReady, searchFieldFocused;
        private long context = long.MinValue;
        private string observedSelection, pointerIdentity;
        private long pointerContext;
        private ReservationTicket pointerReservation;
        private Action pendingAction;
        private GUIStyle textStyle, literalStyle, titleStyle, buttonStyle, selectedStyle, statusStyle, fieldStyle, boxStyle, windowStyle;
        private GUIStyle chromeTitleStyle, chromeButtonStyle, chromeSelectedStyle, scrollStyle;
        private LogSnapshot cachedSnapshot;
        private string cachedSearch, cachedSummary;
        private IList<LogItem> cachedRows;
        private readonly List<Rect> toolRects = new List<Rect>();
        private readonly List<KeyboardCell> keyboardCells = new List<KeyboardCell>();
        private LogLayout layout;
        private Rect lastSelectedBounds;
        private Rect lastListViewport;
        private float lastListViewportHeight;
        private string lastMeasuredId, lastMeasuredTab;
        private bool manualListScroll;
        private float BodyHeight => layout == null ? Math.Max(80, window.height * .7f) : layout.Content.height;
        private float BodyWidth => layout == null ? window.width - 24 : layout.Content.width;
        public string InputHint { get; set; } = "";
        public Action ReselectController { get; set; }
        public Action<string> TabChanged { get; set; }
        public string CurrentTab => Tabs[currentTabIndex];
        public Rect WindowRect => window;
        private LogTabState State => model.Tab(CurrentTab);
        private bool Cards => currentTabIndex < 2;
        private float RowHeight => renderedFont * 2f + 14f;

        public LogRenderer(LogModel model, Action refresh, Action close)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.refresh = refresh;
            this.close = close;
            window = new Rect(ConfigManager.LogWindowX.Value, ConfigManager.LogWindowY.Value,
                ConfigManager.LogWindowWidth.Value, ConfigManager.LogWindowHeight.Value);
            bool useDefaults = LogLayout.IsDefaultSeed(window, ConfigManager.LogFontSize.Value);
            if (useDefaults)
            {
                window = LogLayout.DefaultWindow(Screen.width, Screen.height);
                ConfigManager.LogFontSize.Value = LogLayout.DefaultFont(Screen.height);
            }
            window = ConstrainWindow(window, Screen.width, Screen.height);
            savedWindow = new Rect(float.NaN, 0, 0, 0);
            if (useDefaults) PersistLayout(true);
        }

        public static Rect ConstrainWindow(Rect value, int screenWidth, int screenHeight)
        {
            float width = Math.Max(1, screenWidth), height = Math.Max(1, screenHeight);
            value.width = Mathf.Clamp(Finite(value.width, LogLayout.DefaultWidth), Math.Min(540, width), width);
            value.height = Mathf.Clamp(Finite(value.height, LogLayout.DefaultHeight), Math.Min(440, height), height);
            value.x = Mathf.Clamp(Finite(value.x, 20), 0, Math.Max(0, width - value.width));
            value.y = Mathf.Clamp(Finite(value.y, 20), 0, Math.Max(0, height - value.height));
            return value;
        }

        private static float Finite(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;

        public void ResetInputFocus()
        {
            model.CancelExecution();
            area = Area.List;
            detailIndex = 0;
            pointerIdentity = null;
            pointerReservation = null;
            // Keep held keys latched across context/focus changes. A KeyDown
            // repeat must never become confirmation for the replacement target.
            LatchPhysicallyHeldKeys();
            searchFieldFocused = false;
            ensureVisible = true;
            manualListScroll = false;
            lastMeasuredId = null;
        }

        private bool Synchronize()
        {
            bool invalidated = false;
            if (context != model.ContextId)
            {
                context = model.ContextId;
                ResetInputFocus();
                observedSelection = null;
                GamepadBridge.CancelLogInput();
                invalidated = true;
            }
            string selected = State.SelectedId;
            if (observedSelection != null && selected == null)
            {
                ResetInputFocus();
                GamepadBridge.CancelLogInput();
                invalidated = true;
            }
            observedSelection = selected;
            return invalidated;
        }

        public void Handle(LogPadCommand command)
        {
            if (Synchronize()) return;
            if (command == LogPadCommand.None) return;
            // Pad commands and explicit keyboard navigation release text-field
            // focus, so a later Enter cannot operate an unrelated IMGUI control.
            searchFieldFocused = false;
            if (command == LogPadCommand.PreviousTab || command == LogPadCommand.NextTab)
            {
                if (area == Area.Keyboard) return;
                SwitchTab((currentTabIndex + (command == LogPadCommand.NextTab ? 1 : 3)) % 4);
                return;
            }
            if (command == LogPadCommand.Refresh) { CancelPending(); refresh?.Invoke(); return; }
            if (command == LogPadCommand.Tools)
            {
                if (area != Area.Tools && area != Area.Keyboard) { previousArea = area; area = Area.Tools; toolIndex = 0; ensureVisible = true; }
                return;
            }
            if (command == LogPadCommand.Back)
            {
                CancelPending();
                if (area == Area.Keyboard) { area = Area.Tools; return; }
                if (area == Area.Tools) { area = previousArea == Area.Detail && State.Selected != null ? Area.Detail : Area.List; return; }
                if (area == Area.Detail) { area = Area.List; detailIndex = 0; return; }
                close?.Invoke();
                return;
            }
            if (area == Area.Keyboard) { NavigateKeyboard(command); return; }
            if (area == Area.Tools)
            {
                if (command == LogPadCommand.Up || command == LogPadCommand.Down)
                { toolIndex = Mathf.Clamp(toolIndex + (command == LogPadCommand.Down ? 1 : -1), 0, ToolCount - 1); ensureVisible = true; }
                else if (command == LogPadCommand.Activate) ActivateTool(toolIndex);
                return;
            }
            if (command == LogPadCommand.Left) { State.CancelExecution(); area = Area.List; detailIndex = 0; return; }
            if (command == LogPadCommand.Right)
            {
                if (State.Selected != null) { State.CancelExecution(); area = Area.Detail; detailIndex = 0; if (Cards) State.Expanded.Add(State.SelectedId); }
                return;
            }
            if (area == Area.Detail)
            {
                if (!Cards && (command == LogPadCommand.Down || command == LogPadCommand.Up))
                {
                    detailIndex = command == LogPadCommand.Down && State.Selected != null && State.Selected.CanReserve && !State.PublicationFailed ? 1 : 0;
                    State.ExecutionFocus = detailIndex == 1;
                }
                else if (command == LogPadCommand.Activate && detailIndex == 1) Reserve(State.BeginReservation());
                return;
            }
            IList<LogItem> rows = VisibleRows();
            if (command == LogPadCommand.Up || command == LogPadCommand.Down)
            {
                if (rows.Count == 0) return;
                int index = IndexOf(rows, State.SelectedId);
                index = index < 0 ? (command == LogPadCommand.Down ? 0 : rows.Count - 1) : Mathf.Clamp(index + (command == LogPadCommand.Down ? 1 : -1), 0, rows.Count - 1);
                Select(rows[index].Id);
            }
            else if (command == LogPadCommand.Activate)
            {
                if (State.Selected == null) { if (rows.Count > 0) Select(rows[0].Id); return; }
                bool expanded = !Cards || State.ToggleExpanded(State.SelectedId);
                area = expanded ? Area.Detail : Area.List;
                detailIndex = 0;
                State.CancelExecution();
                ensureVisible = true;
            }
        }

        public void Scroll(float value)
        {
            if (Synchronize()) return;
            float pixels = value * 520f * Mathf.Clamp(Time.unscaledDeltaTime, 0f, 0.1f);
            if (area == Area.Tools) toolScroll = Math.Max(0, toolScroll + pixels);
            else if (area == Area.Keyboard) keyboardScroll = Math.Max(0, keyboardScroll + pixels);
            else if (Cards)
            {
                State.ListScroll = Math.Max(0, State.ListScroll + pixels);
                if (value != 0) manualListScroll = true;
            }
            else State.DetailScroll = Math.Max(0, State.DetailScroll + pixels);
        }

        public void Draw()
        {
            Synchronize();
            window = ConstrainWindow(window, Screen.width, Screen.height);
            EnsureStyles();
            HandleKeyboard();
            window = GUILayout.Window(140404, window, DrawWindow, "다이스마스터 로그 · 창 안을 끌어 이동", windowStyle,
                GUILayout.Width(window.width), GUILayout.Height(window.height));
            window = ConstrainWindow(window, Screen.width, Screen.height);
            if (pendingAction != null)
            {
                Action action = pendingAction;
                pendingAction = null;
                action();
                pointerIdentity = null;
                pointerReservation = null;
            }
            PersistLayout(false);
        }

        private void EnsureStyles()
        {
            // The selected physical font size is honored. Reserve actual chrome
            // and content rectangles instead of shrinking text to fit the window.
            int font = Mathf.Clamp(ConfigManager.LogFontSize.Value, LogLayout.MinimumFont, LogLayout.MaximumFont);
            int controls = Math.Min(font, Math.Max(12, Math.Min(LogLayout.DefaultFont(Screen.height), (int)(window.width / 28f))));
            if (stylesReady && renderedFont == font && chromeFont == controls) return;
            renderedFont = font;
            chromeFont = controls;
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = font, richText = true, wordWrap = true, padding = new RectOffset(4, 4, 4, 4) };
            textStyle.normal.textColor = Color.white;
            literalStyle = new GUIStyle(textStyle) { richText = false };
            titleStyle = new GUIStyle(textStyle) { fontStyle = FontStyle.Bold };
            statusStyle = new GUIStyle(textStyle) { fontSize = Math.Max(12, chromeFont - 1) };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = font, richText = true, wordWrap = true, padding = new RectOffset(6, 6, 6, 6) };
            buttonStyle.normal.textColor = Color.white;
            selectedStyle = new GUIStyle(buttonStyle) { fontStyle = FontStyle.Bold };
            selectedStyle.normal.textColor = new Color(1f, .86f, .36f);
            fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = chromeFont, richText = false };
            boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(6, 6, 5, 5) };
            windowStyle = new GUIStyle(GUI.skin.window) { fontSize = chromeFont, padding = new RectOffset(8, 8, chromeFont + 14, 8) };
            chromeTitleStyle = new GUIStyle(titleStyle) { fontSize = chromeFont };
            chromeButtonStyle = new GUIStyle(buttonStyle) { fontSize = chromeFont };
            chromeSelectedStyle = new GUIStyle(selectedStyle) { fontSize = chromeFont };
            scrollStyle = new GUIStyle { padding = new RectOffset(0, 0, 0, 0) };
            // GUILayout includes inherited skin margins in its minimum sizes.
            // Our measured rectangles budget padding explicitly, so margins
            // must not make a supposedly fixed pane grow past its clip area.
            foreach (GUIStyle style in new[] { textStyle, literalStyle, titleStyle, buttonStyle, selectedStyle,
                statusStyle, fieldStyle, boxStyle, windowStyle, chromeTitleStyle, chromeButtonStyle, chromeSelectedStyle, scrollStyle })
                style.margin = new RectOffset(0, 0, 0, 0);
            stylesReady = true;
        }

        private float TextHeight(string text, GUIStyle style, float width)
        { return Math.Max(style.fontSize + 10, style.CalcHeight(new GUIContent(text), Math.Max(1, width))); }

        private void DrawWindow(int id)
        {
            if (!searchFieldFocused) GUI.FocusControl(null);
            string summary = "문맥 " + model.ContextId + " · " + CurrentTab + " · " + Summary();
            float width = Math.Max(1, window.width - Math.Max(8, chromeFont * .55f) * 2);
            layout = LogLayout.Calculate(window, chromeFont, !Cards && area != Area.Keyboard,
                TextHeight(summary, statusStyle, width),
                string.IsNullOrEmpty(InputHint) ? 0 : TextHeight(InputHint, statusStyle, width),
                TextHeight(Hint(), statusStyle, width), chromeFont + 14 + renderedFont + 24 + 10 + (renderedFont + 12) * 2);
            GUILayout.BeginArea(layout.Tabs);
            GUILayout.BeginHorizontal();
            for (int index = 0; index < Tabs.Length; index++)
            {
                int tabIndex = index;
                SafeButton((index == currentTabIndex ? "● " : "") + Tabs[index], "tab:" + index, () => SwitchTab(tabIndex), index == currentTabIndex, chrome: true);
            }
            SafeButton(area == Area.Tools ? "● 도구" : "도구 [Y]", "tools", () => { if (area != Area.Tools) previousArea = area; area = Area.Tools; ensureVisible = true; }, area == Area.Tools, chrome: true);
            SafeButton("닫기", "close", () => close?.Invoke(), chrome: true);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            if (!layout.CompactChrome)
            {
                GUILayout.BeginArea(layout.Status);
                Label(summary, statusStyle);
                GUILayout.EndArea();
            }
            if (!layout.CompactChrome && !string.IsNullOrEmpty(InputHint))
            {
                GUILayout.BeginArea(layout.InputStatus);
                Label(InputHint, statusStyle);
                GUILayout.EndArea();
            }
            if (!layout.CompactChrome && !Cards && area != Area.Keyboard)
            {
                GUILayout.BeginArea(layout.Search);
                DrawSearch();
                GUILayout.EndArea();
            }
            // This rectangle is exactly the space remaining after the title,
            // tabs, status, search and footer. Each scroll pane gets its height.
            GUILayout.BeginArea(layout.Content);
            if (area == Area.Tools) DrawTools();
            else if (area == Area.Keyboard) DrawKeyboard();
            else if (Cards) DrawCards();
            else DrawListDetail();
            GUILayout.EndArea();
            GUILayout.BeginArea(layout.Footer);
            Label(layout.CompactChrome ? "Y 도구 · B 복귀/닫기" : Hint(), statusStyle);
            GUILayout.EndArea();
            // Restore the old whole-window drag area. Keep this after controls
            // so buttons, text input and scrollbars retain their mouse capture.
            GUI.DragWindow(new Rect(0, 0, window.width, window.height));
            if (Event.current.rawType == EventType.MouseUp && pendingAction == null) { pointerIdentity = null; pointerReservation = null; PersistLayout(true); }
        }

        private void DrawSearch()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("검색", statusStyle, GUILayout.Width(chromeFont * 3));
            GUI.SetNextControlName("DiceMasterLogSearch");
            string value = GUILayout.TextField(State.Search, 256, fieldStyle, GUILayout.MinWidth(40), GUILayout.ExpandWidth(true));
            Rect searchRect = GUILayoutUtility.GetLastRect();
            if (Event.current.rawType == EventType.MouseDown && searchRect.Contains(Event.current.mousePosition))
            { searchFieldFocused = true; State.CancelExecution(); GamepadBridge.NotifyPointerActivity(); }
            if (value != State.Search) { State.SetSearch(value); CancelPending(); }
            SafeButton("패드 입력", "search-keyboard", OpenKeyboard, chrome: true);
            SafeButton("지우기", "clear-search", ClearSearch, chrome: true);
            GUILayout.EndHorizontal();
        }

        private Vector2 BeginScroll(float y, float height)
        {
            // Only vertical scrolling is used. GUIStyle.none gives a horizontal
            // scrollbar zero height, so it cannot cover the selected row.
            return GUILayout.BeginScrollView(new Vector2(0, y), false, true,
                GUIStyle.none, GUI.skin.verticalScrollbar, scrollStyle,
                GUILayout.Height(height), GUILayout.ExpandWidth(true));
        }

        private Vector2 BeginListScroll(float height)
        {
            float previous = State.ListScroll;
            Event evt = Event.current;
            bool mouse = evt != null && (evt.rawType == EventType.MouseDown || evt.rawType == EventType.MouseDrag || evt.rawType == EventType.ScrollWheel);
            bool inside = mouse && lastListViewport.Contains(evt.mousePosition);
            Vector2 next = BeginScroll(previous, height);
            // A captured scrollbar thumb can be dragged outside its viewport.
            // Its changed return value still belongs to the user's gesture.
            if (mouse && next.y != previous || inside && evt.rawType == EventType.ScrollWheel) manualListScroll = true;
            return next;
        }

        private void DrawOverflowChrome(bool includeSearch = false)
        {
            if (layout == null || !layout.CompactChrome) return;
            Label("문맥 " + model.ContextId + " · " + CurrentTab + " · " + Summary(), statusStyle);
            if (!string.IsNullOrEmpty(InputHint)) Label(InputHint, statusStyle);
            Label(Hint(), statusStyle);
            if (includeSearch && !Cards) DrawSearch();
        }

        private void DrawCards()
        {
            IList<LogItem> rows = VisibleRows();
            Vector2 scroll = BeginListScroll(BodyHeight);
            State.ListScroll = scroll.y;
            DrawOverflowChrome();
            rowRects.Clear();
            float contentBottom = 0;
            if (rows.Count == 0) DrawEmpty();
            foreach (LogItem row in rows)
            {
                bool selected = row.Id == State.SelectedId;
                bool expanded = State.Expanded.Contains(row.Id);
                GUILayout.BeginVertical(boxStyle);
                string heading = currentTabIndex == 0 && !string.IsNullOrWhiteSpace(row.Text) ? row.Text : Title(row);
                SafeButton((selected ? "▶ " : "") + (expanded ? "▼ " : "▷ ") + heading, "card:" + row.Id,
                    () => { Select(row.Id); State.ToggleExpanded(row.Id); area = Area.List; }, selected && area != Area.Tools);
                Rect headerRect = GUILayoutUtility.GetLastRect();
                Label(RowState(row), statusStyle);
                if (expanded)
                {
                    if (!string.IsNullOrEmpty(row.Text) && row.Text != row.Title) Label(row.Text, titleStyle);
                    GUILayout.Label("원본 ID: " + row.RawId, literalStyle);
                    Label(row.Description, textStyle);
                }
                GUILayout.EndVertical();
                Rect rect = GUILayoutUtility.GetLastRect();
                rowRects[row.Id] = headerRect;
                contentBottom = Math.Max(contentBottom, rect.yMax);
            }
            GUILayout.EndScrollView();
            CompleteListScroll(GUILayoutUtility.GetLastRect(), contentBottom);
        }

        private void DrawListDetail()
        {
            float headingHeight = chromeFont + 14;
            float listWidth = Math.Max(140, BodyWidth * .36f);
            float reserveHeight = 0;
            GUILayout.BeginHorizontal(GUILayout.Height(BodyHeight), GUILayout.ExpandHeight(true));
            GUILayout.BeginVertical(GUILayout.Width(listWidth), GUILayout.Height(BodyHeight), GUILayout.ExpandHeight(true));
            Display.Label(area == Area.List ? "▶ 목록" : "목록", chromeTitleStyle, new[] { GUILayout.Height(headingHeight) });
            Vector2 list = BeginListScroll(Math.Max(1, BodyHeight - headingHeight));
            State.ListScroll = list.y;
            IList<LogItem> rows = VisibleRows();
            rowRects.Clear();
            float contentBottom = 0;
            if (rows.Count == 0) DrawEmpty();
            foreach (LogItem row in rows)
            {
                bool selected = row.Id == State.SelectedId;
                SafeButton((selected ? "▶ " : "") + Title(row) + "\n" + RowState(row), "row:" + row.Id,
                    () => { Select(row.Id); area = Area.List; }, selected && area == Area.List);
                Rect rect = GUILayoutUtility.GetLastRect();
                rowRects[row.Id] = rect;
                contentBottom = Math.Max(contentBottom, rect.yMax);
            }
            GUILayout.EndScrollView();
            CompleteListScroll(GUILayoutUtility.GetLastRect(), contentBottom);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(boxStyle, GUILayout.ExpandWidth(true), GUILayout.Height(BodyHeight), GUILayout.ExpandHeight(true));
            LogItem selectedRow = State.Selected;
            Display.Label(area == Area.Detail ? "▶ 상세" : "상세", chromeTitleStyle, new[] { GUILayout.Height(headingHeight) });
            bool reservable = selectedRow != null && selectedRow.CanReserve && !State.PublicationFailed;
            if (selectedRow != null)
            {
                string label = CurrentTab == "이벤트" ? "이벤트 우선 예약" : "미션 조건 통과 예약";
                if (LogBridge.IsReserved(CurrentTab, selectedRow.Target)) label += " · 예약 중";
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && reservable;
                bool focused = area == Area.Detail && detailIndex == 1;
                string buttonLabel = (focused ? "▶ " : "") + label;
                GUIStyle reserveStyle = focused ? chromeSelectedStyle : chromeButtonStyle;
                float detailWidth = Math.Max(1, BodyWidth - listWidth - boxStyle.padding.left - boxStyle.padding.right);
                reserveHeight = Math.Max(chromeFont + 24, reserveStyle.CalcHeight(new GUIContent(buttonLabel), detailWidth));
                SafeButton(buttonLabel, "reserve:" + selectedRow.Id,
                    () => Reserve(pointerReservation), focused, reserveHeight, chrome: true);
                GUI.enabled = enabled;
            }
            Vector2 detail = BeginScroll(State.DetailScroll, Math.Max(1, BodyHeight - headingHeight - (selectedRow == null ? 0 : reserveHeight) - 10));
            State.DetailScroll = detail.y;
            DrawOverflowChrome();
            if (selectedRow == null)
                Label("목록에서 대상을 선택하세요.\n자동 갱신·검색으로 대상이 없어지면 다시 선택해야 합니다.", textStyle);
            else
            {
                Label(Title(selectedRow), titleStyle);
                Label(RowState(selectedRow), statusStyle);
                Label(CurrentTab == "이벤트" ? "다음 해당 판정의 가중치를 높입니다. 즉시 발생하거나 지정 이벤트가 발생함을 보장하지 않습니다." :
                    "해당 미션의 조건 통과를 예약합니다. 즉시 실행하거나 행동을 재개하는 기능은 아닙니다.", statusStyle);
                if (!reservable) Label("최신 조회 성공 상태의 대상만 예약할 수 있습니다.", statusStyle);
                GUILayout.Label("원본 ID: " + selectedRow.RawId, literalStyle);
                if (!string.IsNullOrEmpty(selectedRow.Text)) Label(selectedRow.Text, textStyle);
                Label(selectedRow.Description, textStyle);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawEmpty()
        {
            string message = !ConfigManager.EnableOptionInfo.Value ? "선택지 정보 표시가 설정에서 꺼져 있습니다(EnableOptionInfo=false)." :
                !State.HasPublished ? "게임 데이터 게시 대기 중입니다. 새로고침으로 다시 조회할 수 있습니다." :
                State.Snapshot.Rows.Count == 0 ? "현재 문맥에 항목이 없습니다." :
                "검색 결과가 없습니다. 검색 지우기 또는 도구 [Y]로 이동하세요.";
            Label(message, textStyle);
        }

        private int ToolCount => 19;
        private void DrawTools()
        {
            Vector2 scroll = BeginScroll(toolScroll, BodyHeight);
            toolScroll = scroll.y;
            DrawOverflowChrome(includeSearch: true);
            toolRects.Clear();
            float contentBottom = 0;
            for (int index = 0; index < ToolCount; index++)
            {
                int item = index;
                SafeButton((toolIndex == index ? "▶ " : "") + ToolLabel(index), "tool:" + index,
                    () => { toolIndex = item; ActivateTool(item); }, toolIndex == index);
                Rect rect = GUILayoutUtility.GetLastRect();
                toolRects.Add(rect);
                contentBottom = Math.Max(contentBottom, rect.yMax);
            }
            GUILayout.EndScrollView();
            Rect viewport = GUILayoutUtility.GetLastRect();
            if (ensureVisible && Event.current.type == EventType.Repaint && toolIndex < toolRects.Count)
            {
                toolScroll = LogLayout.BringIntoView(toolScroll, toolRects[toolIndex], viewport.height, contentBottom);
                ensureVisible = false;
            }
        }

        private string ToolLabel(int index)
        {
            switch (index)
            {
                case 0: return "검색 문자 입력 · 로컬 한글/영문/ID 자판";
                case 1: return "검색 지우기";
                case 2: return "게임 데이터 새로고침 [X]";
                case 3: return "본문 글자 작게 · 현재 " + ConfigManager.LogFontSize.Value;
                case 4: return "본문 글자 크게 · 현재 " + ConfigManager.LogFontSize.Value;
                case 5: return "창 너비 줄이기 · 현재 " + (int)window.width;
                case 6: return "창 너비 늘리기 · 현재 " + (int)window.width;
                case 7: return "창 높이 줄이기 · 현재 " + (int)window.height;
                case 8: return "창 높이 늘리기 · 현재 " + (int)window.height;
                case 9: return "창 왼쪽으로";
                case 10: return "창 오른쪽으로";
                case 11: return "창 위로";
                case 12: return "창 아래로";
                case 13: return "창 위치·크기·글자 기본값 복구";
                case 14: return "패드 소유자 다시 선택 · 모든 버튼 해제·스틱 중립 필요";
                case 15: return "선택지 탭으로";
                case 16: return "이벤트 탭으로";
                case 17: return "이전 영역으로 돌아가기";
                default: return "로그 창 닫기";
            }
        }

        private void ActivateTool(int index)
        {
            CancelPending();
            switch (index)
            {
                case 0: OpenKeyboard(); break;
                case 1: ClearSearch(); break;
                case 2: refresh?.Invoke(); break;
                case 3: ConfigManager.LogFontSize.Value = Mathf.Clamp(ConfigManager.LogFontSize.Value - 1, LogLayout.MinimumFont, LogLayout.MaximumFont); break;
                case 4: ConfigManager.LogFontSize.Value = Mathf.Clamp(ConfigManager.LogFontSize.Value + 1, LogLayout.MinimumFont, LogLayout.MaximumFont); break;
                case 5: window.width -= 40; break;
                case 6: window.width += 40; break;
                case 7: window.height -= 40; break;
                case 8: window.height += 40; break;
                case 9: window.x -= 32; break;
                case 10: window.x += 32; break;
                case 11: window.y -= 32; break;
                case 12: window.y += 32; break;
                case 13: window = LogLayout.DefaultWindow(Screen.width, Screen.height); ConfigManager.LogFontSize.Value = LogLayout.DefaultFont(Screen.height); break;
                case 14: ReselectController?.Invoke(); ResetInputFocus(); break;
                case 15: SwitchTab(0); break;
                case 16: SwitchTab(2); break;
                case 17: area = previousArea == Area.Detail && State.Selected != null ? Area.Detail : Area.List; break;
                case 18: close?.Invoke(); break;
            }
            window = ConstrainWindow(window, Screen.width, Screen.height);
            PersistLayout(true);
            ensureVisible = true;
        }

        private void OpenKeyboard()
        {
            CancelPending();
            keyboard.Start(State.Search);
            keyboardIndex = 0;
            keyboardScroll = 0;
            area = Area.Keyboard;
            BuildKeyboardCells();
        }

        private sealed class KeyboardCell
        {
            internal int Row, Column;
            internal string Label;
            internal Action Press;
        }

        private void BuildKeyboardCells()
        {
            keyboardCells.Clear();
            string[] modes = { "한글", "abc", "ABC", "123/ID" };
            for (int index = 0; index < modes.Length; index++)
            {
                int mode = index;
                keyboardCells.Add(new KeyboardCell { Row = 0, Column = index * 2, Label = (keyboard.Mode == mode ? "● " : "") + modes[index], Press = () => keyboard.SetMode(mode) });
            }
            IList<string> keys = keyboard.Keys();
            for (int index = 0; index < keys.Count; index++)
            {
                int key = index;
                keyboardCells.Add(new KeyboardCell { Row = 1 + index / 7, Column = index % 7, Label = keys[index], Press = () => keyboard.PressKey(key) });
            }
            int actionRow = 1 + (keys.Count + 6) / 7;
            AddKeyboardAction(actionRow, 0, "공백", keyboard.Space);
            AddKeyboardAction(actionRow, 1, "← 지우기", keyboard.Backspace);
            AddKeyboardAction(actionRow, 2, "조합 되돌림", keyboard.UndoComposition);
            AddKeyboardAction(actionRow, 4, "검색 확정", () => { State.SetSearch(keyboard.Commit()); area = Area.Tools; CancelPending(); });
            AddKeyboardAction(actionRow, 6, "취소", () => { area = Area.Tools; CancelPending(); });
            keyboardIndex = Mathf.Clamp(keyboardIndex, 0, keyboardCells.Count - 1);
        }

        private void AddKeyboardAction(int row, int column, string label, Action press)
        { keyboardCells.Add(new KeyboardCell { Row = row, Column = column, Label = label, Press = press }); }

        private void DrawKeyboard()
        {
            Vector2 scroll = BeginScroll(keyboardScroll, BodyHeight);
            keyboardScroll = scroll.y;
            DrawOverflowChrome();
            // Search input is literal user text. Display.Label translates and
            // records unknown Han text, so it must never receive this draft.
            GUILayout.Label("▶ 로컬 검색 자판 · " + (keyboard.Mode == 0 ? keyboard.StageLabel + " (초성→중성→종성)" : "영문·원본 ID") +
                "\n검색어: " + keyboard.Draft + "  [조합: " + keyboard.Composition + "]", literalStyle);
            Label("방향키로 이동 · A 입력 · 확정 전에는 검색에 반영하지 않음 · B 취소", statusStyle);
            int index = 0;
            Rect selectedRect = new Rect();
            float contentBottom = 0;
            while (index < keyboardCells.Count)
            {
                int row = keyboardCells[index].Row;
                GUILayout.BeginHorizontal();
                while (index < keyboardCells.Count && keyboardCells[index].Row == row)
                {
                    int key = index;
                    KeyboardCell cell = keyboardCells[index];
                    SafeButton((index == keyboardIndex ? "▶ " : "") + cell.Label, "key:" + row + ":" + cell.Column + ":" + keyboard.Mode + ":" + keyboard.Stage,
                        () => { keyboardIndex = key; PressKeyboardCell(); }, keyboardIndex == index);
                    Rect keyRect = GUILayoutUtility.GetLastRect();
                    if (keyboardIndex == index) selectedRect = keyRect;
                    contentBottom = Math.Max(contentBottom, keyRect.yMax);
                    index++;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            Rect viewport = GUILayoutUtility.GetLastRect();
            if (ensureVisible && Event.current.type == EventType.Repaint)
            {
                keyboardScroll = LogLayout.BringIntoView(keyboardScroll, selectedRect, viewport.height, contentBottom);
                ensureVisible = false;
            }
        }

        private void NavigateKeyboard(LogPadCommand command)
        {
            if (command == LogPadCommand.Activate) { PressKeyboardCell(); return; }
            int vertical = command == LogPadCommand.Up ? -1 : command == LogPadCommand.Down ? 1 : 0;
            int horizontal = command == LogPadCommand.Left ? -1 : command == LogPadCommand.Right ? 1 : 0;
            KeyboardCell current = keyboardCells[keyboardIndex];
            int best = keyboardIndex, distance = int.MaxValue;
            for (int index = 0; index < keyboardCells.Count; index++)
            {
                KeyboardCell candidate = keyboardCells[index];
                bool match = vertical != 0 ? candidate.Row == current.Row + vertical : horizontal != 0 && candidate.Row == current.Row && Math.Sign(candidate.Column - current.Column) == horizontal;
                if (!match) continue;
                int candidateDistance = Math.Abs(candidate.Column - current.Column);
                if (candidateDistance < distance) { best = index; distance = candidateDistance; }
            }
            keyboardIndex = best;
            ensureVisible = true;
        }

        private void PressKeyboardCell()
        {
            int oldStage = keyboard.Stage, oldMode = keyboard.Mode;
            keyboardCells[keyboardIndex].Press();
            if (area != Area.Keyboard) return;
            BuildKeyboardCells();
            if (oldStage != keyboard.Stage || oldMode != keyboard.Mode) keyboardIndex = 4;
            ensureVisible = true;
        }

        private void SafeButton(string label, string identity, Action action, bool highlighted = false, float height = 0, bool chrome = false)
        {
            GUIStyle style = chrome ? (highlighted ? chromeSelectedStyle : chromeButtonStyle) : (highlighted ? selectedStyle : buttonStyle);
            bool pressed = GUILayout.Button(label, style, height > 0 ? GUILayout.Height(height) : GUILayout.MinHeight((chrome ? chromeFont : renderedFont) + 16), GUILayout.ExpandWidth(true));
            Rect rect = GUILayoutUtility.GetLastRect();
            Event evt = Event.current;
            // rawType remains MouseDown/MouseUp after IMGUI consumes the event.
            if (evt.rawType == EventType.MouseDown && evt.button == 0 && rect.Contains(evt.mousePosition) && GUI.enabled)
            {
                pointerIdentity = identity;
                pointerContext = model.ContextId;
                GamepadBridge.NotifyPointerActivity();
                searchFieldFocused = false;
                State.CancelExecution();
                if (identity.StartsWith("reserve:", StringComparison.Ordinal))
                { area = Area.Detail; detailIndex = 1; State.ExecutionFocus = true; pointerReservation = State.BeginReservation(); }
                else pointerReservation = null;
            }
            if (!pressed) return;
            // Button order may change during refresh. Only its original context
            // and stable control identity may consume a physical mouse release.
            if (evt.rawType != EventType.MouseUp || pointerIdentity != identity || pointerContext != model.ContextId) return;
            // Mutate after GUILayout.Window finishes, so changing tabs or
            // composition pages cannot invalidate its current layout tree.
            pendingAction = action;
        }

        private void Reserve(ReservationTicket ticket)
        {
            if (!State.CanActivate(ticket)) { CancelPending(); return; }
            LogBridge.TryReserve(ticket.ContextId, ticket.Tab, ticket.Id, ticket.Target);
            State.CancelExecution();
            detailIndex = 0;
            pointerReservation = null;
        }

        private void Select(string id)
        { State.Select(id); observedSelection = State.SelectedId; detailIndex = 0; ensureVisible = true; manualListScroll = false; CancelPending(); }

        private void SwitchTab(int index)
        {
            if (index == currentTabIndex) return;
            CancelPending();
            currentTabIndex = index;
            area = Area.List;
            detailIndex = 0;
            observedSelection = State.SelectedId;
            cachedSnapshot = null;
            ensureVisible = true;
            TabChanged?.Invoke(CurrentTab);
        }

        private void ClearSearch()
        { State.SetSearch(""); CancelPending(); ensureVisible = true; }

        private void CancelPending()
        { State.CancelExecution(); pointerReservation = null; }

        private IList<LogItem> VisibleRows()
        {
            if (!ReferenceEquals(cachedSnapshot, State.Snapshot) || cachedSearch != State.Search)
            {
                cachedSnapshot = State.Snapshot;
                cachedSearch = State.Search;
                cachedRows = State.VisibleRows();
                int healthy = 0, waiting = 0, failed = 0, retained = 0;
                foreach (LogItem row in cachedSnapshot.Rows)
                    if (row.RefreshState == LogRefreshState.Healthy) healthy++;
                    else if (row.RefreshState == LogRefreshState.Waiting) waiting++;
                    else if (row.RefreshState == LogRefreshState.RetainedFailure) retained++;
                    else failed++;
                cachedSummary = "현재 " + cachedRows.Count + "/" + cachedSnapshot.Rows.Count + " · 정상 " + healthy + " · 대기 " + waiting + " · 읽기 실패 " + failed + " · 이전 값 " + retained;
                if (State.HiddenErrorCount > 0) cachedSummary += " · 검색 밖 오류 " + State.HiddenErrorCount + " (검색 지우기로 확인)";
            }
            return cachedRows;
        }

        private string Summary()
        {
            if (!ConfigManager.EnableOptionInfo.Value) return "정보 표시 꺼짐";
            if (!State.HasPublished) return "게임 데이터 게시 대기 중";
            VisibleRows();
            return cachedSummary + (State.PublicationFailed ? " · 게시 실패 · 이전 값 (예약 불가)" : "");
        }
        private static void Label(string text, GUIStyle style) => Display.Label(text, style, Array.Empty<GUILayoutOption>());
        private static string Title(LogItem row) => string.IsNullOrWhiteSpace(row.Title) ? string.IsNullOrWhiteSpace(row.Text) ? Escape(row.RawId) : row.Text : row.Title;
        private static string Escape(string value) => (value ?? "").Replace("<", "＜").Replace(">", "＞");
        private static string RowState(LogItem row)
        {
            string read = row.ReadState == LogReadState.Read ? "읽음" : row.ReadState == LogReadState.Unread ? "미독" : "읽음 불명";
            string result = read + " · " + (row.RefreshState == LogRefreshState.Healthy ? "조회 정상" : row.RefreshStatus);
            foreach (string state in row.AnalysisStates)
                result += " · " + (state == "Supported" ? "분석 지원" : state == "Partial" ? "추가 영향 가능" : state == "Unsupported" ? "결과는 진행 후 확인" :
                    state == "NoEffects" ? "표시 효과 없음" : state == "Waiting" ? "분석 대기" : state == "ReadFailure" ? "분석 읽기 실패" : state);
            return result;
        }

        private string Hint()
        {
            if (area == Area.Keyboard) return "현재: 검색 자판 · ↑↓←→ 이동 / A 입력 / B 취소 / 오른쪽 스틱 스크롤";
            if (area == Area.Tools) return "현재: 도구 · ↑↓ 이동 / A 작동 / B 이전 영역 / LB·RB 탭";
            if (area == Area.Detail) return Cards ? "현재: 펼친 카드 · 오른쪽 스틱 본문 스크롤 / B 카드 목록 / Y 도구" :
                "현재: 상세 · ↑↓ 본문/예약 버튼 / A 예약 버튼 작동 / 오른쪽 스틱 본문 / B 목록 / Y 도구";
            return "현재: " + (Cards ? "카드" : "목록") + " · ↑↓ 이동 / A " + (Cards ? "펼침" : "상세") + " / → 상세 / LB·RB 탭 / Y 도구 / X 새로고침 / B 닫기";
        }

        private void CompleteListScroll(Rect viewport, float contentBottom)
        {
            if (Event.current.type != EventType.Repaint || viewport.height <= 0) return;
            lastListViewport = viewport;
            Rect selected;
            if (State.SelectedId == null || !rowRects.TryGetValue(State.SelectedId, out selected)) return;
            bool changed = lastMeasuredId == State.SelectedId && lastMeasuredTab == CurrentTab &&
                (selected != lastSelectedBounds || viewport.height != lastListViewportHeight);
            if (ensureVisible || changed && area == Area.List && !manualListScroll)
            {
                State.ListScroll = LogLayout.BringIntoView(State.ListScroll, selected, viewport.height, contentBottom);
                ensureVisible = false;
            }
            lastSelectedBounds = selected;
            lastListViewportHeight = viewport.height;
            lastMeasuredId = State.SelectedId;
            lastMeasuredTab = CurrentTab;
        }

        private static int IndexOf(IList<LogItem> rows, string id)
        { for (int index = 0; index < rows.Count; index++) if (rows[index].Id == id) return index; return -1; }

        private void PersistLayout(bool immediate)
        {
            if (window == savedWindow && ConfigManager.LogFontSize.Value >= LogLayout.MinimumFont && ConfigManager.LogFontSize.Value <= LogLayout.MaximumFont) return;
            if (!immediate && Time.unscaledTime - lastSave < .5f) return;
            lastSave = Time.unscaledTime;
            ConfigManager.LogWindowX.Value = window.x;
            ConfigManager.LogWindowY.Value = window.y;
            ConfigManager.LogWindowWidth.Value = window.width;
            ConfigManager.LogWindowHeight.Value = window.height;
            ConfigManager.LogFontSize.Value = Mathf.Clamp(ConfigManager.LogFontSize.Value, LogLayout.MinimumFont, LogLayout.MaximumFont);
            savedWindow = window;
        }

        private void HandleKeyboard()
        {
            Event evt = Event.current;
            if (evt == null) return;
            if (evt.type == EventType.Layout) heldKeys.RemoveWhere(key => !Input.GetKey(key));
            if (evt.rawType == EventType.MouseDown || evt.rawType == EventType.MouseMove || evt.rawType == EventType.ScrollWheel)
                GamepadBridge.NotifyPointerActivity();
            if (evt.type == EventType.KeyUp) { heldKeys.Remove(evt.keyCode); return; }
            if (evt.type != EventType.KeyDown || evt.keyCode == ConfigManager.LogMenuToggleKey.Value) return;
            if (searchFieldFocused)
            {
                GamepadBridge.NotifyPointerActivity();
                if (evt.keyCode == KeyCode.Escape || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                { searchFieldFocused = false; GUI.FocusControl(null); evt.Use(); }
                return;
            }
            LogPadCommand command = evt.keyCode == KeyCode.UpArrow ? LogPadCommand.Up : evt.keyCode == KeyCode.DownArrow ? LogPadCommand.Down :
                evt.keyCode == KeyCode.LeftArrow ? LogPadCommand.Left : evt.keyCode == KeyCode.RightArrow ? LogPadCommand.Right :
                evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Space ? LogPadCommand.Activate :
                evt.keyCode == KeyCode.Escape ? LogPadCommand.Back : evt.keyCode == KeyCode.Y ? LogPadCommand.Tools :
                evt.keyCode == KeyCode.X ? LogPadCommand.Refresh : LogPadCommand.None;
            if (command == LogPadCommand.None) return;
            GamepadBridge.NotifyPointerActivity();
            if (heldKeys.Add(evt.keyCode)) Handle(command);
            evt.Use();
        }

        private void LatchPhysicallyHeldKeys()
        {
            KeyCode[] keys = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
                KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space, KeyCode.Escape, KeyCode.Y, KeyCode.X };
            foreach (KeyCode key in keys) if (Input.GetKey(key)) heldKeys.Add(key);
        }
    }
}
