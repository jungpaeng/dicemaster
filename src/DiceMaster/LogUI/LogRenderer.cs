using System;
using System.Collections.Generic;
using DiceMaster.Config;
using DiceMaster.Gamepad;
using DiceMaster.LiveLog;
using UnityEngine;
using Display = DiceMaster.EmbeddedTranslations.Display;

namespace DiceMaster.LogUI
{
    // This view consumes already-published data only. Layout and focus
    // never discover game rows, evaluate Lua or change a reservation themselves.
    public sealed class LogRenderer
    {
        private enum Area { List, Tools }
        private static readonly string[] Tabs = { "선택지", "조건", "이벤트", "미션" };
        private readonly LogModel model;
        private readonly Action refresh, close;
        private readonly Dictionary<string, Rect> rowRects = new Dictionary<string, Rect>(StringComparer.Ordinal);
        private readonly HashSet<KeyCode> heldKeys = new HashSet<KeyCode>();
        private Rect window;
        private Rect savedWindow;
        private Area area;
        private int currentTabIndex, toolIndex, renderedFont, chromeFont;
        private float toolScroll, lastSave;
        private bool ensureVisible, stylesReady, layoutInitialized;
        private long context = long.MinValue;
        private string observedSelection, pointerIdentity;
        private long pointerContext;
        private ReservationTicket pointerReservation;
        private Action pendingAction;
        private GUIStyle textStyle, literalStyle, titleStyle, buttonStyle, selectedStyle, statusStyle, boxStyle, windowStyle;
        private GUIStyle chromeTitleStyle, chromeButtonStyle, chromeSelectedStyle, scrollStyle;
        private LogSnapshot cachedSnapshot;
        private string cachedSummary;
        private readonly List<Rect> toolRects = new List<Rect>();
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
        private bool IsChoiceTab => currentTabIndex == 0;
        private bool HasStoryReadState => currentTabIndex < 2;
        private bool SupportsReservation => currentTabIndex >= 2;
        private float RowHeight => renderedFont * 2f + 14f;
        private float ChoiceGap => Math.Max(8, renderedFont * .6f);

        public LogRenderer(LogModel model, Action refresh, Action close)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.refresh = refresh;
            this.close = close;
            window = new Rect(ConfigManager.LogWindowX.Value, ConfigManager.LogWindowY.Value,
                ConfigManager.LogWindowWidth.Value, ConfigManager.LogWindowHeight.Value);
            savedWindow = new Rect(float.NaN, 0, 0, 0);
        }

        private void InitializeLayoutIfNeeded()
        {
            if (layoutInitialized) return;
            // Construction happens in Awake, whose Screen can still be zero or
            // a temporary startup resolution. Read the loaded settings afresh
            // and resolve/clamp them only at the first visible, valid viewport.
            window = new Rect(ConfigManager.LogWindowX.Value, ConfigManager.LogWindowY.Value,
                ConfigManager.LogWindowWidth.Value, ConfigManager.LogWindowHeight.Value);
            bool useDefaults = LogLayout.IsDefaultSeed(window, ConfigManager.LogFontSize.Value);
            if (useDefaults)
            {
                window = LogLayout.DefaultWindow(Screen.width, Screen.height);
                ConfigManager.LogFontSize.Value = LogLayout.DefaultFont(Screen.height);
            }
            window = ConstrainWindow(window, Screen.width, Screen.height);
            layoutInitialized = true;
            PersistLayout(true);
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

        public void ResetInputFocus(bool preservePointerTransaction = false)
        {
            // A scheme change can be caused by the very touch/click being
            // handled. Its own down/up identity and ticket stay valid; pad
            // authority was already canceled on pointer-down. Hard lifecycle
            // boundaries and invalid targets never take this path.
            if (preservePointerTransaction && pointerIdentity != null)
            {
                LatchPhysicallyHeldKeys();
                return;
            }
            model.CancelExecution();
            area = Area.List;
            pointerIdentity = null;
            pointerReservation = null;
            // Keep held keys latched across context/focus changes. A KeyDown
            // repeat must never become confirmation for the replacement target.
            LatchPhysicallyHeldKeys();
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

        private ReservationTicket ArmSelectedReservation()
        {
            CancelPending();
            if (!SupportsReservation || State.PublicationFailed || State.Selected == null || !State.Selected.CanReserve)
                return null;
            object reservationState;
            if (!LogBridge.TryCaptureReservation(CurrentTab, out reservationState)) return null;
            // The authority exists only for this fresh confirmation or the
            // selected row's pointer-down/up transaction, never for a pane.
            State.ExecutionFocus = true;
            return State.BeginReservation(reservationState);
        }

        private void ReturnFromTools()
        {
            area = Area.List;
        }

        public void Handle(LogPadCommand command)
        {
            if (Synchronize()) return;
            if (command == LogPadCommand.None) return;
            // A fresh key/pad command supersedes an unfinished mouse/touch
            // press. Its later release cannot reserve the abandoned target.
            pointerIdentity = null;
            CancelPending();
            if (command == LogPadCommand.PreviousTab || command == LogPadCommand.NextTab)
            {
                SwitchTab((currentTabIndex + (command == LogPadCommand.NextTab ? 1 : 3)) % 4);
                return;
            }
            if (command == LogPadCommand.Refresh) { CancelPending(); refresh?.Invoke(); return; }
            if (command == LogPadCommand.Tools)
            {
                if (area != Area.Tools) { area = Area.Tools; toolIndex = 0; ensureVisible = true; }
                return;
            }
            if (command == LogPadCommand.Back)
            {
                CancelPending();
                if (area == Area.Tools) { ReturnFromTools(); return; }
                close?.Invoke();
                return;
            }
            if (area == Area.Tools)
            {
                if (command == LogPadCommand.Up || command == LogPadCommand.Down)
                { toolIndex = Mathf.Clamp(toolIndex + (command == LogPadCommand.Down ? 1 : -1), 0, ToolCount - 1); ensureVisible = true; }
                else if (command == LogPadCommand.Activate) ActivateTool(toolIndex);
                return;
            }
            if (IsChoiceTab)
            {
                // Every choice is already readable. There is no selection or
                // action behind its heading, so directional input only reads
                // the continuous document; confirm/left/right do nothing.
                if (command == LogPadCommand.Up || command == LogPadCommand.Down)
                {
                    State.ListScroll = Math.Max(0, State.ListScroll +
                        (command == LogPadCommand.Down ? RowHeight : -RowHeight));
                    manualListScroll = true;
                    ensureVisible = false;
                }
                return;
            }
            // Details are always readable and have no focusable controls.
            if (command == LogPadCommand.Left || command == LogPadCommand.Right) return;
            IList<LogItem> rows = State.Snapshot.Rows;
            if (command == LogPadCommand.Up || command == LogPadCommand.Down)
            {
                int direction = command == LogPadCommand.Down ? 1 : -1;
                int current = IndexOf(rows, State.SelectedId);
                int index = current < 0 ? (direction > 0 ? 0 : rows.Count - 1) : current + direction;
                for (; index >= 0 && index < rows.Count; index += direction)
                    if (rows[index].CanSelect) { Select(rows[index].Id); return; }
                // Status rows remain readable without becoming navigation or
                // execution targets. Scroll past the last real item to read
                // the disabled footer; a new selection resumes following.
                if (rows.Count > 0 && (current < 0 || direction > 0 && current < rows.Count - 1))
                { State.ListScroll = Math.Max(0, State.ListScroll + direction * RowHeight); manualListScroll = true; ensureVisible = false; }
                else if (current >= 0) Select(rows[current].Id);
            }
            else if (command == LogPadCommand.Activate)
            {
                if (State.Selected == null)
                {
                    foreach (LogItem row in rows) if (row.CanSelect) { Select(row.Id); break; }
                    return;
                }
                Reserve(ArmSelectedReservation());
            }
        }

        public void Scroll(float value)
        {
            if (Synchronize()) return;
            float pixels = value * 520f * Mathf.Clamp(Time.unscaledDeltaTime, 0f, 0.1f);
            if (area == Area.Tools) toolScroll = Math.Max(0, toolScroll + pixels);
            else if (IsChoiceTab || State.Selected == null)
            {
                State.ListScroll = Math.Max(0, State.ListScroll + pixels);
                if (value != 0) manualListScroll = true;
            }
            else State.DetailScroll = Math.Max(0, State.DetailScroll + pixels);
        }

        public void Draw()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            InitializeLayoutIfNeeded();
            Synchronize();
            window = ConstrainWindow(window, Screen.width, Screen.height);
            EnsureStyles();
            HandleKeyboard();
            // The contents use absolute BeginArea rectangles. They do not add
            // children to GUILayout.Window's root, whose empty-group sizing
            // would collapse the native window to its style padding.
            window = GUI.Window(140404, window, DrawWindow, "다이스마스터 로그 · 창 안을 끌어 이동", windowStyle);
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
                statusStyle, boxStyle, windowStyle, chromeTitleStyle, chromeButtonStyle, chromeSelectedStyle, scrollStyle })
            {
                style.margin = new RectOffset(0, 0, 0, 0);
                style.fixedWidth = style.fixedHeight = 0;
            }
            stylesReady = true;
        }

        private float TextHeight(string text, GUIStyle style, float width)
        { return Math.Max(style.fontSize + 10, style.CalcHeight(new GUIContent(text), Math.Max(1, width))); }

        private void DrawWindow(int id)
        {
            GUI.FocusControl(null);
            string summary = "문맥 " + model.ContextId + " · " + CurrentTab + " · " + Summary();
            float width = Math.Max(1, window.width - Math.Max(8, chromeFont * .55f) * 2);
            layout = LogLayout.Calculate(window, chromeFont,
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
            SafeButton(area == Area.Tools ? "● 도구" : "도구 [Y]", "tools", () => { area = Area.Tools; ensureVisible = true; }, area == Area.Tools, chrome: true);
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
            // This rectangle is exactly the space remaining after the title,
            // tabs, status and footer. Each scroll pane gets its height.
            GUILayout.BeginArea(layout.Content);
            if (area == Area.Tools) DrawTools();
            else if (IsChoiceTab) DrawChoices();
            else DrawListDetail();
            GUILayout.EndArea();
            GUILayout.BeginArea(layout.Footer);
            Label(layout.CompactChrome ? "Y 도구 · B 복귀/닫기" : Hint(), statusStyle);
            GUILayout.EndArea();
            // Restore the old whole-window drag area. Keep this after controls
            // so buttons and scrollbars retain their mouse capture.
            GUI.DragWindow(new Rect(0, 0, window.width, window.height));
            if (IsPointerRelease(Event.current.rawType) && pendingAction == null) { pointerIdentity = null; pointerReservation = null; PersistLayout(true); }
        }

        private Vector2 BeginScroll(float y, float height)
        {
            // Let IMGUI show the vertical bar only when the laid-out contents
            // overflow. Horizontal bars have no style/height in these panes.
            return GUILayout.BeginScrollView(new Vector2(0, y), false, false,
                GUIStyle.none, GUI.skin.verticalScrollbar, scrollStyle,
                GUILayout.Height(height), GUILayout.ExpandWidth(true));
        }

        private Vector2 BeginListScroll(float height)
        {
            float previous = State.ListScroll;
            Event evt = Event.current;
            bool mouse = evt != null && (IsPointerPress(evt.rawType) || IsPointerDrag(evt.rawType) || evt.rawType == EventType.ScrollWheel);
            bool inside = mouse && lastListViewport.Contains(evt.mousePosition);
            Vector2 next = BeginScroll(previous, height);
            // A captured scrollbar thumb can be dragged outside its viewport.
            // Its changed return value still belongs to the user's gesture.
            if (mouse && next.y != previous || inside && evt.rawType == EventType.ScrollWheel) manualListScroll = true;
            return next;
        }

        private void DrawOverflowChrome()
        {
            if (layout == null || !layout.CompactChrome) return;
            Label("문맥 " + model.ContextId + " · " + CurrentTab + " · " + Summary(), statusStyle);
            if (!string.IsNullOrEmpty(InputHint)) Label(InputHint, statusStyle);
            Label(Hint(), statusStyle);
        }

        private void DrawChoices()
        {
            IList<LogItem> rows = State.Snapshot.Rows;
            Vector2 scroll = BeginListScroll(BodyHeight);
            State.ListScroll = scroll.y;
            DrawOverflowChrome();
            rowRects.Clear();
            if (rows.Count == 0) DrawEmpty();
            bool first = true;
            foreach (LogItem row in rows)
            {
                if (!first) GUILayout.Space(ChoiceGap);
                first = false;
                GUILayout.BeginVertical(boxStyle);
                string heading = !string.IsNullOrWhiteSpace(row.Text) ? Display.TranslateLabel(row.Text) : Title(row);
                GUILayout.Label(heading, titleStyle);
                Label(RowState(row), statusStyle);
                GUILayout.Label("원본 ID: " + row.RawId, literalStyle);
                Label(row.Description, textStyle);
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            // Choice headings are labels, never focus/selection anchors. A
            // publication or resized viewport must not pull reading elsewhere.
            if (Event.current.type == EventType.Repaint)
                lastListViewport = GUILayoutUtility.GetLastRect();
            ensureVisible = false;
        }

        private void DrawListDetail()
        {
            float headingHeight = chromeFont + 14;
            float listWidth = Math.Max(140, BodyWidth * .36f);
            GUILayout.BeginHorizontal(GUILayout.Height(BodyHeight), GUILayout.ExpandHeight(true));
            GUILayout.BeginVertical(GUILayout.Width(listWidth), GUILayout.Height(BodyHeight), GUILayout.ExpandHeight(true));
            string listTitle = SupportsReservation ? "목록" : "체크포인트";
            Display.Label((area == Area.List ? "▶ " : "") + listTitle, chromeTitleStyle, new[] { GUILayout.Height(headingHeight) });
            Vector2 list = BeginListScroll(Math.Max(1, BodyHeight - headingHeight));
            State.ListScroll = list.y;
            IList<LogItem> rows = State.Snapshot.Rows;
            rowRects.Clear();
            float contentBottom = 0;
            bool waitingExplanationShown = false;
            bool hasSelectable = false;
            if (rows.Count == 0) DrawEmpty();
            foreach (LogItem row in rows)
            {
                if (row.CanSelect) hasSelectable = true;
                if (row.IsStatusOnly && row.RefreshState == LogRefreshState.Waiting && !waitingExplanationShown)
                {
                    // Disabled information cannot be opened in the detail
                    // pane, so retain its meaning/retry guidance in the list.
                    Label(WaitingExplanation(), statusStyle);
                    waitingExplanationShown = true;
                }
                bool selected = row.Id == State.SelectedId;
                string state = RowState(row);
                string caption = (selected ? "▶ " : "") + Title(row) +
                    (SupportsReservation && LogBridge.IsReserved(CurrentTab, row.Target) ? " (예약 중)" : "") +
                    (string.IsNullOrEmpty(state) ? "" : "\n" + state);
                if (!row.CanSelect) caption += "\n원본 ID: " + Escape(row.RawId);
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && row.CanSelect;
                // Native word-wrap sizing uses the actual client width. When
                // the vertical bar is hidden the row can fill that space, and
                // when it appears IMGUI recomputes wrapping at the narrower
                // width. Constrain the minimum too, so a long raw ID cannot
                // force a hidden horizontal overflow.
                SafeButton(caption, "row:" + row.Id, () => ActivateRow(row.Id),
                    selected && area == Area.List, maxWidth: listWidth);
                GUI.enabled = enabled;
                Rect rect = GUILayoutUtility.GetLastRect();
                rowRects[row.Id] = rect;
                contentBottom = Math.Max(contentBottom, rect.yMax);
            }
            GUILayout.EndScrollView();
            CompleteListScroll(GUILayoutUtility.GetLastRect(), contentBottom);
            GUILayout.EndVertical();
            GUILayout.BeginVertical(boxStyle, GUILayout.ExpandWidth(true), GUILayout.Height(BodyHeight), GUILayout.ExpandHeight(true));
            LogItem selectedRow = State.Selected;
            string detailTitle = SupportsReservation ? "상세" : "조건 상세";
            Display.Label(detailTitle, chromeTitleStyle, new[] { GUILayout.Height(headingHeight) });
            Vector2 detail = BeginScroll(State.DetailScroll, Math.Max(1, BodyHeight - headingHeight - 10));
            State.DetailScroll = detail.y;
            DrawOverflowChrome();
            if (selectedRow == null)
                Label(rows.Count > 0 && !hasSelectable ?
                    "현재 선택할 수 있는 항목이 없습니다.\n목록의 안내를 확인하거나 새로고침하세요." :
                    "목록에서 대상을 선택하세요.\n자동 갱신으로 대상이 없어지면 다시 선택해야 합니다.", textStyle);
            else
            {
                GUILayout.Label(Title(selectedRow), titleStyle);
                string state = RowState(selectedRow);
                if (!string.IsNullOrEmpty(state)) Label(state, statusStyle);
                if (selectedRow.RefreshState == LogRefreshState.Waiting)
                    Label(WaitingExplanation(), statusStyle);
                if (SupportsReservation)
                {
                    Label(ReservationStatus(selectedRow), statusStyle);
                    Label(CurrentTab == "이벤트" ? "다음 해당 판정의 가중치를 높입니다. 즉시 발생하거나 지정 이벤트가 발생함을 보장하지 않습니다." :
                        "해당 미션의 조건 통과를 예약합니다. 즉시 실행하거나 행동을 재개하는 기능은 아닙니다.", statusStyle);
                }
                GUILayout.Label("원본 ID: " + selectedRow.RawId, literalStyle);
                if (!string.IsNullOrEmpty(selectedRow.Text)) Label(selectedRow.Text, textStyle);
                if (CurrentTab == "조건" && selectedRow.ConditionDetails != null)
                    DrawConditionDetails(selectedRow.ConditionDetails);
                else Label(selectedRow.Description, textStyle);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawConditionDetails(LogConditionDetails details)
        {
            float paneWidth = Math.Max(1, BodyWidth - Math.Max(140, BodyWidth * .36f) - boxStyle.padding.horizontal);
            float textWidth = Math.Max(1, paneWidth - boxStyle.padding.horizontal);
            if (!string.IsNullOrEmpty(details.Hint)) BoundedLabel(details.Hint, statusStyle, paneWidth);
            if (details.Branches.Count == 0)
            {
                if (!string.IsNullOrEmpty(details.DescriptionFallback)) BoundedLabel(details.DescriptionFallback, textStyle, paneWidth);
                return;
            }
            bool first = true;
            foreach (LogConditionBranch branch in details.Branches)
            {
                if (!first) GUILayout.Space(Math.Max(10, renderedFont * .6f));
                first = false;
                // Each alternative owns a real layout block. Wrapped text in
                // one branch cannot look like a new alternative or merge into
                // the next one; no text parsing or interactive focus is used.
                GUILayout.BeginVertical(boxStyle, GUILayout.MinWidth(1), GUILayout.MaxWidth(paneWidth), GUILayout.ExpandWidth(true));
                BoundedLabel(branch.Title, titleStyle, textWidth);
                if (!string.IsNullOrEmpty(branch.Context)) BoundedLabel(branch.Context, statusStyle, textWidth);
                if (!string.IsNullOrEmpty(branch.Description)) BoundedLabel(branch.Description, textStyle, textWidth);
                GUILayout.EndVertical();
            }
        }

        private static void BoundedLabel(string text, GUIStyle style, float width)
        {
            // Long unbroken identifiers must wrap in the detail viewport,
            // rather than giving this vertical-only document a wide minimum.
            Display.Label(text, style, new[] { GUILayout.MinWidth(1), GUILayout.MaxWidth(width), GUILayout.ExpandWidth(true) });
        }

        private void DrawEmpty()
        {
            string message = !ConfigManager.EnableOptionInfo.Value ? "선택지 정보 표시가 설정에서 꺼져 있습니다(EnableOptionInfo=false)." :
                !State.HasPublished ? "게임 데이터 게시 대기 중입니다. 새로고침으로 다시 조회할 수 있습니다." :
                "현재 문맥에 항목이 없습니다.";
            Label(message, textStyle);
        }

        private string WaitingExplanation() => CurrentTab == "미션" ?
            "현재 미션 단계가 없거나 설명 조회에 필요한 게임 자료를 얻지 못했습니다. 미션 미완료·조건 미충족을 뜻하지 않습니다. 이 탭을 연 동안 재조회하며 새로고침으로 다시 확인할 수 있습니다." :
            "설명 조회에 필요한 게임 자료를 아직 얻지 못했습니다. 이 탭을 연 동안 재조회하며 새로고침으로 다시 확인할 수 있습니다.";

        private int ToolCount => 17;
        private void DrawTools()
        {
            Vector2 scroll = BeginScroll(toolScroll, BodyHeight);
            toolScroll = scroll.y;
            DrawOverflowChrome();
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
                case 0: return "게임 데이터 새로고침 [X]";
                case 1: return "본문 글자 작게 · 현재 " + ConfigManager.LogFontSize.Value;
                case 2: return "본문 글자 크게 · 현재 " + ConfigManager.LogFontSize.Value;
                case 3: return "창 너비 줄이기 · 현재 " + (int)window.width;
                case 4: return "창 너비 늘리기 · 현재 " + (int)window.width;
                case 5: return "창 높이 줄이기 · 현재 " + (int)window.height;
                case 6: return "창 높이 늘리기 · 현재 " + (int)window.height;
                case 7: return "창 왼쪽으로";
                case 8: return "창 오른쪽으로";
                case 9: return "창 위로";
                case 10: return "창 아래로";
                case 11: return "창 위치·크기·글자 기본값 복구";
                case 12: return "패드 소유자 다시 선택 · 모든 버튼 해제·스틱 중립 필요";
                case 13: return "선택지 탭으로";
                case 14: return "이벤트 탭으로";
                case 15: return "이전 영역으로 돌아가기";
                default: return "로그 창 닫기";
            }
        }

        private void ActivateTool(int index)
        {
            CancelPending();
            bool hasViewport = Screen.width > 0 && Screen.height > 0;
            if ((!layoutInitialized || !hasViewport) && index >= 1 && index <= 11) return;
            switch (index)
            {
                case 0: refresh?.Invoke(); break;
                case 1: ConfigManager.LogFontSize.Value = Mathf.Clamp(ConfigManager.LogFontSize.Value - 1, LogLayout.MinimumFont, LogLayout.MaximumFont); break;
                case 2: ConfigManager.LogFontSize.Value = Mathf.Clamp(ConfigManager.LogFontSize.Value + 1, LogLayout.MinimumFont, LogLayout.MaximumFont); break;
                case 3: window.width -= 40; break;
                case 4: window.width += 40; break;
                case 5: window.height -= 40; break;
                case 6: window.height += 40; break;
                case 7: window.x -= 32; break;
                case 8: window.x += 32; break;
                case 9: window.y -= 32; break;
                case 10: window.y += 32; break;
                case 11: window = LogLayout.DefaultWindow(Screen.width, Screen.height); ConfigManager.LogFontSize.Value = LogLayout.DefaultFont(Screen.height); break;
                case 12: ReselectController?.Invoke(); ResetInputFocus(); break;
                case 13: SwitchTab(0); break;
                case 14: SwitchTab(2); break;
                case 15: ReturnFromTools(); break;
                case 16: close?.Invoke(); break;
            }
            if (layoutInitialized && hasViewport)
            {
                window = ConstrainWindow(window, Screen.width, Screen.height);
                PersistLayout(true);
            }
            ensureVisible = true;
        }

        private void SafeButton(string label, string identity, Action action, bool highlighted = false, float height = 0, bool chrome = false, float maxWidth = 0)
        {
            Event evt = Event.current;
            // Unity may translate touch to an effective mouse event; Button
            // can consume that event. Capture both forms before drawing it.
            bool down = IsPointerPress(evt.rawType) || IsPointerPress(evt.type);
            bool up = IsPointerRelease(evt.rawType) || IsPointerRelease(evt.type);
            GUIStyle style = chrome ? (highlighted ? chromeSelectedStyle : chromeButtonStyle) : (highlighted ? selectedStyle : buttonStyle);
            // MinHeight also forces maxHeight in Unity's word-wrap sizer. Let
            // rows measure their full wrapped caption at the final viewport
            // width, with no fixed height or permanent scrollbar gutter.
            bool pressed = maxWidth > 0 ?
                GUILayout.Button(label, style, GUILayout.MinWidth(1), GUILayout.MaxWidth(maxWidth), GUILayout.ExpandWidth(true)) :
                height > 0 ? GUILayout.Button(label, style, GUILayout.ExpandWidth(true), GUILayout.Height(height)) :
                GUILayout.Button(label, style, GUILayout.ExpandWidth(true));
            Rect rect = GUILayoutUtility.GetLastRect();
            if (down && evt.button == 0 && rect.Contains(evt.mousePosition) && GUI.enabled)
            {
                pointerIdentity = identity;
                pointerContext = model.ContextId;
                GamepadBridge.NotifyPointerActivity();
                CancelPending();
                if (SupportsReservation && State.SelectedId != null && identity == "row:" + State.SelectedId)
                    pointerReservation = ArmSelectedReservation();
            }
            if (!pressed || !GUI.enabled) return;
            // Button order may change during refresh. Only its original context
            // and stable control identity may consume a physical mouse release.
            if (!up || pointerIdentity != identity || pointerContext != model.ContextId) return;
            // Mutate after GUI.Window finishes, so changing tabs or
            // tool actions cannot invalidate its current layout tree.
            pendingAction = action;
        }

        private void Reserve(ReservationTicket ticket)
        {
            try
            {
                if (SupportsReservation && State.CanActivate(ticket))
                    LogBridge.TryToggleReservation(ticket.ContextId, ticket.Tab, ticket.Id, ticket.Target, ticket.ReservationState);
            }
            finally
            {
                CancelPending();
                area = Area.List;
            }
        }

        private void ActivateRow(string id)
        {
            if (State.SelectedId != id) { Select(id); area = Area.List; return; }
            // Re-clicking a selected row uses only the authority captured on
            // pointer-down. A release never creates fresh action authority.
            if (SupportsReservation) Reserve(pointerReservation);
        }

        private bool Select(string id)
        {
            if (!State.Select(id)) return false;
            observedSelection = State.SelectedId; ensureVisible = true; manualListScroll = false; CancelPending();
            return true;
        }

        private void SwitchTab(int index)
        {
            if (index == currentTabIndex) return;
            CancelPending();
            currentTabIndex = index;
            area = Area.List;
            observedSelection = State.SelectedId;
            cachedSnapshot = null;
            ensureVisible = true;
            TabChanged?.Invoke(CurrentTab);
        }

        private void CancelPending()
        { State.CancelExecution(); pointerReservation = null; }

        private void UpdateSummary()
        {
            if (ReferenceEquals(cachedSnapshot, State.Snapshot)) return;
            cachedSnapshot = State.Snapshot;
            int healthy = 0, waiting = 0, failed = 0, retained = 0;
            foreach (LogItem row in cachedSnapshot.Rows)
                if (row.RefreshState == LogRefreshState.Healthy) healthy++;
                else if (row.RefreshState == LogRefreshState.Waiting) waiting++;
                else if (row.RefreshState == LogRefreshState.RetainedFailure) retained++;
                else failed++;
            cachedSummary = "항목 " + cachedSnapshot.Rows.Count + "개 · 정상 " + healthy + " · 대기 " + waiting + " · 읽기 실패 " + failed + " · 이전 값 " + retained;
        }

        private string Summary()
        {
            if (!ConfigManager.EnableOptionInfo.Value) return "정보 표시 꺼짐";
            if (!State.HasPublished) return "게임 데이터 게시 대기 중";
            UpdateSummary();
            return cachedSummary + (State.PublicationFailed ? " · 게시 실패 · 이전 값" + (SupportsReservation ? " (예약 불가)" : "") : "");
        }
        private static void Label(string text, GUIStyle style) => Display.Label(text, style, Array.Empty<GUILayoutOption>());
        // Finalize only display names. A literal ID fallback and the separate
        // original-ID labels must never pass through label translation.
        private static string Title(LogItem row) => string.IsNullOrWhiteSpace(row.Title) ?
            string.IsNullOrWhiteSpace(row.Text) ? Escape(row.RawId) : Display.TranslateLabel(row.Text) : Display.TranslateLabel(row.Title);
        private static string Escape(string value) => (value ?? "").Replace("<", "＜").Replace(">", "＞");
        private string RowState(LogItem row)
        {
            // Event/mission publications have no story-read mapping. Unknown
            // there must not look like an unread or unfinished mission result.
            string read = row.ReadState == LogReadState.Read ? "읽음" : row.ReadState == LogReadState.Unread ? "미독" : HasStoryReadState ? "읽음 정보 없음" : "";
            string status = row.RefreshState == LogRefreshState.Healthy ? "" :
                row.RefreshState == LogRefreshState.Waiting ? "설명 조회 대기" : row.RefreshStatus;
            string result = string.IsNullOrEmpty(read) ? status :
                string.IsNullOrEmpty(status) ? read : read + " · " + status;
            foreach (string state in row.AnalysisStates)
            {
                string analysis = state == "Supported" ? "분석 지원" : state == "Partial" ? "추가 영향 가능" : state == "Unsupported" ? "결과는 진행 후 확인" :
                    state == "NoEffects" ? "표시 효과 없음" : state == "Waiting" ? "분석 대기" : state == "ReadFailure" ? "분석 읽기 실패" : state;
                if (!string.IsNullOrEmpty(analysis)) result += (string.IsNullOrEmpty(result) ? "" : " · ") + analysis;
            }
            return result;
        }

        private string ReservationStatus(LogItem row)
        {
            string action = CurrentTab == "이벤트" ? "이벤트 우선 예약" : "미션 조건 통과 예약";
            bool reserved = LogBridge.IsReserved(CurrentTab, row.Target);
            string state = reserved ? "활성화 (예약 중)" : "비활성화 (예약 없음)";
            if (row.CanReserve && !State.PublicationFailed)
                return action + " · " + state + "\n" + (reserved ? "예약 해제 가능" : "예약 가능") +
                    " · 목록에서 A / 선택된 항목 다시 클릭·터치";
            string reason = State.PublicationFailed ? "게시 실패 · 최신 대상 확인 필요" :
                row.IsStatusOnly ? "안내 항목" : row.Target == null ? "대상 없음" :
                row.RefreshState == LogRefreshState.Waiting ? "설명 조회 대기" :
                row.RefreshState == LogRefreshState.RetainedFailure ? "갱신 실패 · 이전 값" :
                row.RefreshState == LogRefreshState.ReadFailure ? "조회 실패" : "최신 대상 확인 필요";
            return action + " · " + state + "\n예약 변경 불가 · " + reason;
        }

        private string Hint()
        {
            if (area == Area.Tools) return "현재: 도구 · ↑↓ 이동 / A 작동 / B 이전 영역 / LB·RB 탭";
            if (IsChoiceTab) return "현재: 선택지 읽기 · ↑↓·오른쪽 스틱 본문 스크롤 / LB·RB 탭 / Y 도구 / X 새로고침 / B 닫기";
            if (State.Selected == null) return "현재: " + (SupportsReservation ? "목록" : "체크포인트 목록") + " · ↑↓ 항목 이동 / 오른쪽 스틱 목록 / LB·RB 탭 / Y 도구 / X 새로고침 / B 닫기";
            if (!SupportsReservation) return "현재: 체크포인트 목록 · ↑↓ 항목 이동 / 오른쪽 스틱 상세 본문 / LB·RB 탭 / Y 도구 / X 새로고침 / B 닫기";
            if (!State.Selected.CanReserve || State.PublicationFailed) return "현재: 목록 · 예약 변경 불가 · ↑↓ 항목 이동 / 오른쪽 스틱 상세 본문 / LB·RB 탭 / Y 도구 / X 새로고침 / B 닫기";
            string action = LogBridge.IsReserved(CurrentTab, State.Selected.Target) ? "예약 해제" : "예약";
            return "현재: 목록 · ↑↓ 이동 / A " + action + " · 선택된 항목 다시 클릭·터치 " + action + " / 오른쪽 스틱 상세 본문 / LB·RB 탭 / Y 도구 / X 새로고침 / B 닫기";
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
            if (!layoutInitialized) return;
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
            if (evt.rawType == EventType.TouchLeave)
            {
                pointerIdentity = null;
                CancelPending();
            }
            if (evt.rawType == EventType.MouseMove) GamepadBridge.NotifyPointerMotion();
            if (IsPointerPress(evt.rawType) || IsPointerDrag(evt.rawType) ||
                evt.rawType == EventType.TouchStationary || evt.rawType == EventType.ScrollWheel)
                GamepadBridge.NotifyPointerActivity();
            if (evt.type == EventType.KeyUp) { heldKeys.Remove(evt.keyCode); return; }
            if (evt.type != EventType.KeyDown || evt.keyCode == ConfigManager.LogMenuToggleKey.Value) return;
            LogPadCommand command = evt.keyCode == KeyCode.UpArrow ? LogPadCommand.Up : evt.keyCode == KeyCode.DownArrow ? LogPadCommand.Down :
                evt.keyCode == KeyCode.LeftArrow ? LogPadCommand.Left : evt.keyCode == KeyCode.RightArrow ? LogPadCommand.Right :
                evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Space ? LogPadCommand.Activate :
                evt.keyCode == KeyCode.Escape ? LogPadCommand.Back : evt.keyCode == KeyCode.Y ? LogPadCommand.Tools :
                evt.keyCode == KeyCode.X ? LogPadCommand.Refresh : LogPadCommand.None;
            if (command == LogPadCommand.None) return;
            GamepadBridge.NotifyKeyboardActivity();
            if (heldKeys.Add(evt.keyCode))
            {
                // The latest explicit key command takes ownership from an
                // unfinished click. Its later release cannot act a second time.
                pointerIdentity = null;
                pointerReservation = null;
                Handle(command);
            }
            evt.Use();
        }

        private void LatchPhysicallyHeldKeys()
        {
            KeyCode[] keys = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
                KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space, KeyCode.Escape, KeyCode.Y, KeyCode.X };
            foreach (KeyCode key in keys) if (Input.GetKey(key)) heldKeys.Add(key);
        }

        private static bool IsPointerPress(EventType type) => type == EventType.MouseDown || type == EventType.TouchDown;
        private static bool IsPointerRelease(EventType type) => type == EventType.MouseUp || type == EventType.TouchUp;
        private static bool IsPointerDrag(EventType type) => type == EventType.MouseDrag || type == EventType.TouchMove;
    }
}
