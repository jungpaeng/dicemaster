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
        private enum Area { List, Tools, PopupSetting, ConditionDetails }
        private static readonly string[] Tabs = { "선택지", "조건", "이벤트", "미션" };
        private const string ConditionListHint = "현재: 체크포인트 목록 · ↑↓ 항목 이동 / A · Enter · → 상세 분기 선택 / 목록 맨 위에서 ↑ 팝업 설정 / 오른쪽 스틱 본문 / LB·RB 탭 / Y 도구 / X 새로고침 / B 닫기";
        private const string ConditionDetailHint = "현재: 조건 상세 · ↑↓ 분기 이동 / A · Enter · 선택된 분기 다시 클릭·터치 예약·해제 / ← · B 체크포인트 목록 / 오른쪽 스틱 본문 / LB·RB 탭 / Y 도구";
        private readonly LogModel model;
        private readonly Action refresh, close;
        private readonly Dictionary<string, Rect> rowRects = new Dictionary<string, Rect>(StringComparer.Ordinal);
        private readonly HashSet<KeyCode> heldKeys = new HashSet<KeyCode>();
        private Rect window;
        private Rect savedWindow;
        private Area area, toolsReturnArea;
        private int currentTabIndex, toolIndex, renderedFont, chromeFont;
        private float toolScroll, lastSave;
        private bool ensureVisible, stylesReady, layoutInitialized;
        private long context = long.MinValue;
        private string observedSelection, observedBranch, pointerIdentity;
        private long pointerContext;
        private ReservationTicket pointerReservation;
        private Action pendingAction;
        private GUIStyle textStyle, literalStyle, titleStyle, buttonStyle, selectedStyle, branchStyle, statusStyle, boxStyle, windowStyle;
        private GUIStyle chromeTitleStyle, chromeButtonStyle, chromeSelectedStyle, scrollStyle;
        private bool releaseTextFocus;
        private LogSnapshot cachedSnapshot;
        private string cachedSummary;
        private readonly List<Rect> toolRects = new List<Rect>();
        private LogLayout layout;
        private Rect lastSelectedBounds;
        private Rect lastListViewport;
        private float lastListViewportHeight;
        private string lastMeasuredId, lastMeasuredTab;
        private bool manualListScroll, ensureBranchVisible;
        private readonly Dictionary<string, Rect> branchRects = new Dictionary<string, Rect>(StringComparer.Ordinal);
        private float BodyHeight => layout == null ? Math.Max(80, window.height * .7f) : layout.Content.height;
        private float BodyWidth => layout == null ? window.width - 24 : layout.Content.width;
        public string InputHint { get; set; } = "";
        public Action ReselectController { get; set; }
        public Action<string> TabChanged { get; set; }
        public string CurrentTab => Tabs[currentTabIndex];
        public Rect WindowRect => window;
        private LogTabState State => model.Tab(CurrentTab);
        private bool IsChoiceTab => currentTabIndex == 0;
        private bool IsEventTab => currentTabIndex == 2;
        private bool IsConditionTab => currentTabIndex == 1;
        private bool HasPopupSetting => currentTabIndex == 1 || IsEventTab;
        private bool HasStoryReadState => currentTabIndex < 2;
        private bool SupportsReservation => State.SupportsReservation;
        private IList<LogItem> VisibleRows => IsEventTab ? State.VisibleRows : State.Snapshot.Rows;
        private float RowHeight => renderedFont * 2f + 14f;
        private float ChoiceGap => Math.Max(8, renderedFont * .6f);

        public LogRenderer(LogModel model, Action refresh, Action close)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.refresh = refresh;
            this.close = close;
            // Search/filter controls were removed. Do not inherit a hidden
            // projection from a previously used model.
            model.Tab("이벤트").SearchQuery = "";
            model.Tab("이벤트").AvailableOnly = false;
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

        public void ResetInputFocus(bool preservePointerTransaction = false, bool preserveNavigationArea = false)
        {
            // A scheme change can be caused by the very touch/click being
            // handled. Its own down/up identity and ticket stay valid; pad
            // authority was already canceled on pointer-down. Hard lifecycle
            // boundaries and invalid targets never take this path.
            if (preservePointerTransaction)
            {
                // Update can observe the takeover after the accepted release
                // already completed. Preserve the navigation area explicitly
                // chosen by that completed gesture.
                // Cancel only pad authority when there is no active press.
                if (pointerIdentity == null)
                { model.CancelExecution(); pointerReservation = null; }
                LatchPhysicallyHeldKeys();
                return;
            }
            model.CancelExecution();
            releaseTextFocus = true;
            pointerIdentity = null;
            pointerReservation = null;
            // Keep held keys latched across context/focus changes. A KeyDown
            // repeat must never become confirmation for the replacement target.
            LatchPhysicallyHeldKeys();
            // Reacquiring a pad invalidates action authority and text entry,
            // but it does not navigate out of the tools opened by a touch.
            if (!preserveNavigationArea)
            {
                area = Area.List;
                ensureVisible = true;
                manualListScroll = false;
                lastMeasuredId = null;
            }
        }

        private bool Synchronize()
        {
            bool invalidated = false;
            if (context != model.ContextId)
            {
                context = model.ContextId;
                ResetInputFocus();
                observedSelection = observedBranch = null;
                GamepadBridge.CancelLogInput();
                invalidated = true;
            }
            string selected = State.SelectedId;
            if (observedSelection != null && selected == null)
            {
                ResetInputFocus(preserveNavigationArea: area == Area.PopupSetting);
                GamepadBridge.CancelLogInput();
                invalidated = true;
            }
            if (area == Area.ConditionDetails && observedBranch != null && State.SelectedBranchId == null)
            {
                ResetInputFocus();
                GamepadBridge.CancelLogInput();
                invalidated = true;
            }
            observedSelection = selected;
            observedBranch = State.SelectedBranchId;
            return invalidated;
        }

        private ReservationTicket ArmSelectedReservation()
        {
            CancelPending();
            if (!SupportsReservation || State.PublicationFailed || !State.CanReserveSelected)
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
            area = toolsReturnArea == Area.ConditionDetails && IsConditionTab && State.SelectedBranch != null ?
                Area.ConditionDetails : Area.List;
        }

        private void EnterTools()
        {
            if (area == Area.Tools) return;
            toolsReturnArea = area == Area.ConditionDetails ? area : Area.List;
            area = Area.Tools; toolIndex = 0; ensureVisible = true;
        }

        private bool EnterConditionDetails()
        {
            if (!IsConditionTab || State.Selected == null || State.Selected.ConditionDetails == null) return false;
            if (State.SelectedBranch == null)
                foreach (LogConditionBranch branch in State.Selected.ConditionDetails.Branches)
                    if (!string.IsNullOrEmpty(branch.Id) && State.SelectBranch(branch.Id)) break;
            if (State.SelectedBranch == null) return false;
            area = Area.ConditionDetails;
            observedBranch = State.SelectedBranchId;
            ensureBranchVisible = true;
            return true;
        }

        private void MoveConditionBranch(int direction)
        {
            LogConditionDetails details = State.Selected?.ConditionDetails;
            if (details == null) return;
            int current = -1;
            for (int index = 0; index < details.Branches.Count; index++)
                if (details.Branches[index].Id == State.SelectedBranchId) { current = index; break; }
            for (int index = current < 0 ? (direction > 0 ? 0 : details.Branches.Count - 1) : current + direction;
                index >= 0 && index < details.Branches.Count; index += direction)
                if (SelectBranch(details.Branches[index].Id)) return;
        }

        private bool SelectBranch(string id, bool follow = true)
        {
            if (string.IsNullOrEmpty(id) || !State.SelectBranch(id)) return false;
            CancelPending();
            area = Area.ConditionDetails;
            observedBranch = State.SelectedBranchId;
            ensureBranchVisible = follow;
            return true;
        }

        public void Handle(LogPadCommand command)
        {
            if (Synchronize()) return;
            if (command == LogPadCommand.None) return;
            releaseTextFocus = true;
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
                EnterTools();
                return;
            }
            if (command == LogPadCommand.Back)
            {
                CancelPending();
                if (area == Area.Tools) { ReturnFromTools(); return; }
                if (area == Area.ConditionDetails) { area = Area.List; return; }
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
            if (area == Area.PopupSetting)
            {
                if (command == LogPadCommand.Activate) TogglePopupSetting();
                else if (command == LogPadCommand.Down)
                {
                    area = Area.List;
                    foreach (LogItem row in VisibleRows) if (row.CanSelect) { Select(row.Id); break; }
                }
                return;
            }
            if (area == Area.ConditionDetails)
            {
                if (command == LogPadCommand.Left) area = Area.List;
                else if (command == LogPadCommand.Up || command == LogPadCommand.Down)
                    MoveConditionBranch(command == LogPadCommand.Down ? 1 : -1);
                else if (command == LogPadCommand.Activate) Reserve(ArmSelectedReservation());
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
            if (command == LogPadCommand.Right) { EnterConditionDetails(); return; }
            if (command == LogPadCommand.Left) return;
            IList<LogItem> rows = VisibleRows;
            if (command == LogPadCommand.Up || command == LogPadCommand.Down)
            {
                int direction = command == LogPadCommand.Down ? 1 : -1;
                int current = IndexOf(rows, State.SelectedId);
                int firstSelectable = -1;
                for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                    if (rows[rowIndex].CanSelect) { firstSelectable = rowIndex; break; }
                if (HasPopupSetting && direction < 0 && (current < 0 || current == firstSelectable))
                { area = Area.PopupSetting; return; }
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
                if (IsConditionTab) EnterConditionDetails();
                else if (SupportsReservation) Reserve(ArmSelectedReservation());
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
            else { State.DetailScroll = Math.Max(0, State.DetailScroll + pixels); if (value != 0) ensureBranchVisible = false; }
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
            branchStyle = new GUIStyle(buttonStyle) { fontStyle = FontStyle.Bold };
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
                branchStyle, statusStyle, boxStyle, windowStyle, chromeTitleStyle, chromeButtonStyle, chromeSelectedStyle, scrollStyle })
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
            // Focus changes belong to explicit transitions, not to the tab's
            // continuous draw callbacks. In particular, do not touch native
            // focus between an accepted pointer down and its matching release.
            if (releaseTextFocus && pointerIdentity == null && Event.current.type == EventType.Layout)
            { GUI.FocusControl(null); releaseTextFocus = false; }
            string summary = "문맥 " + model.ContextId + " · " + CurrentTab + " · " + Summary();
            float width = Math.Max(1, window.width - Math.Max(8, chromeFont * .55f) * 2);
            layout = LogLayout.Calculate(window, chromeFont,
                TextHeight(summary, statusStyle, width),
                string.IsNullOrEmpty(InputHint) ? 0 : TextHeight(InputHint, statusStyle, width),
                HintHeight(width), chromeFont + 14 + renderedFont + 24 + 10 + (renderedFont + 12) * 2);
            GUILayout.BeginArea(layout.Tabs);
            GUILayout.BeginHorizontal();
            for (int index = 0; index < Tabs.Length; index++)
            {
                int tabIndex = index;
                bool active = area != Area.Tools && index == currentTabIndex;
                SafeButton((active ? "● " : "") + Tabs[index], "tab:" + index, () => SwitchTab(tabIndex), active, chrome: true);
            }
            SafeButton(area == Area.Tools ? "● 도구" : "도구 [Y]", "tools", () => { EnterTools(); releaseTextFocus = true; }, area == Area.Tools, chrome: true);
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
            Event evt = Event.current;
            // A native window callback can have a different effective event
            // from rawType. Layout/repaint/filtered callbacks cannot finish a
            // press before its real release callback. Native controls consume
            // accepted input, so Used still needs the original release type.
            bool released = IsPointerRelease(evt.type) || evt.type == EventType.Used && IsPointerRelease(evt.rawType);
            if (released && pendingAction == null) { pointerIdentity = null; pointerReservation = null; PersistLayout(true); }
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
            if (IsConditionTab && (area == Area.List || area == Area.ConditionDetails))
            {
                float width = Math.Max(1, BodyWidth - Math.Max(140, BodyWidth * .36f) - boxStyle.padding.horizontal);
                Display.Label(Hint(), statusStyle, new[] { GUILayout.Height(HintHeight(width)) });
            }
            else Label(Hint(), statusStyle);
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
            float paneHeight = Math.Max(1, BodyHeight - (HasPopupSetting ? DrawPopupSetting() : 0));
            GUILayout.BeginHorizontal(GUILayout.Height(paneHeight), GUILayout.ExpandHeight(true));
            GUILayout.BeginVertical(GUILayout.Width(listWidth), GUILayout.Height(paneHeight), GUILayout.ExpandHeight(true));
            string listTitle = CurrentTab == "조건" ? "체크포인트" : "목록";
            Display.Label((area == Area.List ? "▶ " : "") + listTitle, chromeTitleStyle, new[] { GUILayout.Height(headingHeight) });
            Vector2 list = BeginListScroll(Math.Max(1, paneHeight - headingHeight));
            State.ListScroll = list.y;
            IList<LogItem> rows = VisibleRows;
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
                string caption = (selected && area == Area.List ? "▶ " : "") + Title(row) +
                    (RowReserved(row) ? " (예약 중)" : "") +
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
            GUILayout.BeginVertical(boxStyle, GUILayout.ExpandWidth(true), GUILayout.Height(paneHeight), GUILayout.ExpandHeight(true));
            LogItem selectedRow = State.Selected;
            string detailTitle = CurrentTab == "조건" ? "조건 상세" : "상세";
            Display.Label((area == Area.ConditionDetails ? "▶ " : "") + detailTitle, chromeTitleStyle, new[] { GUILayout.Height(headingHeight) });
            Vector2 detail = BeginScroll(State.DetailScroll, Math.Max(1, paneHeight - headingHeight - 10));
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
                    if (IsConditionTab) Label("아래 상세에서 예약할 분기를 선택하세요. 목록에서는 체크포인트만 선택합니다.", statusStyle);
                    else Label(ReservationStatus(selectedRow), statusStyle);
                    Label(CurrentTab == "미션" ?
                        "해당 미션의 조건 통과를 예약합니다. 즉시 실행하거나 행동을 재개하는 기능은 아닙니다." :
                        "조건을 무시하고 다음 해당 판정에 한 번 적용하도록 예약합니다. 즉시 실행하거나 행동을 재개하는 기능은 아닙니다.", statusStyle);
                }
                if (IsEventTab && selectedRow.EventEligibility != null && !string.IsNullOrEmpty(selectedRow.EventEligibility.Summary))
                    Label((selectedRow.RefreshState == LogRefreshState.Healthy ? "" : "이전 조회 설명: ") + selectedRow.EventEligibility.Summary, statusStyle);
                GUILayout.Label("원본 ID: " + selectedRow.RawId, literalStyle);
                if (!string.IsNullOrEmpty(selectedRow.Text)) Label(selectedRow.Text, textStyle);
                if (CurrentTab == "조건" && selectedRow.ConditionDetails != null)
                    DrawConditionDetails(selectedRow.ConditionDetails);
                else Label(selectedRow.Description, textStyle);
            }
            GUILayout.EndScrollView();
            CompleteBranchScroll(GUILayoutUtility.GetLastRect());
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private string PopupSettingLabel() => IsEventTab ?
            "이벤트 선택 팝업: " + (ConfigManager.EnableEventSelectionPopup.Value ? "ON" : "OFF") :
            "조건 분기 선택 팝업: " + (ConfigManager.EnableSwitchSelectionPopup.Value ? "ON" : "OFF");

        private float DrawPopupSetting()
        {
            string label = PopupSettingLabel();
            float height = TextHeight(label, chromeButtonStyle, BodyWidth);
            SafeButton(label, "popup:" + CurrentTab, TogglePopupSetting,
                area == Area.PopupSetting, height: height, chrome: true);
            GUILayout.Space(8);
            return height + 8;
        }

        private void TogglePopupSetting()
        {
            CancelPending();
            area = Area.PopupSetting;
            if (IsEventTab)
                ConfigManager.EnableEventSelectionPopup.Value = !ConfigManager.EnableEventSelectionPopup.Value;
            else if (currentTabIndex == 1)
                ConfigManager.EnableSwitchSelectionPopup.Value = !ConfigManager.EnableSwitchSelectionPopup.Value;
        }

        private void DrawConditionDetails(LogConditionDetails details)
        {
            branchRects.Clear();
            float paneWidth = Math.Max(1, BodyWidth - Math.Max(140, BodyWidth * .36f) - boxStyle.padding.horizontal);
            float textWidth = Math.Max(1, paneWidth - boxStyle.padding.horizontal);
            if (!string.IsNullOrEmpty(details.Hint)) BoundedLabel(details.Hint, statusStyle, paneWidth);
            if (details.Branches.Count == 0)
            {
                if (!string.IsNullOrEmpty(details.DescriptionFallback)) BoundedLabel(details.DescriptionFallback, textStyle, paneWidth);
                return;
            }
            float statusHeight = ConditionReservationStatusHeight(textWidth);
            bool first = true;
            foreach (LogConditionBranch branch in details.Branches)
            {
                if (!first) GUILayout.Space(Math.Max(10, renderedFont * .6f));
                first = false;
                // Keep complete branch context/body in its own wrapped block.
                // Only its structured identity is an action target; localized
                // text and visual line positions never establish a branch.
                GUILayout.BeginVertical(boxStyle, GUILayout.MinWidth(1), GUILayout.MaxWidth(paneWidth), GUILayout.ExpandWidth(true));
                if (string.IsNullOrEmpty(branch.Id)) BoundedLabel(branch.Title, titleStyle, textWidth);
                else
                {
                    string id = branch.Id;
                    bool selected = area == Area.ConditionDetails && State.SelectedBranchId == id;
                    // Selection changes only color. Keep identical heading
                    // text/font and a status block for every branch so the
                    // next touch lands on the same physical heading.
                    SafeButton(branch.Title, BranchIdentity(id), () => ActivateBranch(id), selected, maxWidth: textWidth);
                    Display.Label(ConditionReservationStatus(branch), statusStyle, new[] {
                        GUILayout.MinWidth(1), GUILayout.MaxWidth(textWidth), GUILayout.ExpandWidth(true), GUILayout.Height(statusHeight) });
                }
                if (!string.IsNullOrEmpty(branch.Context)) BoundedLabel(branch.Context, statusStyle, textWidth);
                if (!string.IsNullOrEmpty(branch.Description)) BoundedLabel(branch.Description, textStyle, textWidth);
                GUILayout.EndVertical();
                if (!string.IsNullOrEmpty(branch.Id)) branchRects[branch.Id] = GUILayoutUtility.GetLastRect();
            }
        }

        private string BranchIdentity(string id) => "branch:" + State.SelectedId + ":" + id;

        private void ActivateBranch(string id)
        {
            if (area != Area.ConditionDetails || State.SelectedBranchId != id) { SelectBranch(id, false); return; }
            Reserve(pointerReservation);
        }

        private string ConditionReservationStatus(LogConditionBranch branch)
        {
            bool reserved = LogBridge.IsReserved(CurrentTab, branch.Target);
            string state = reserved ? "활성화 (예약 중)" : "비활성화 (예약 없음)";
            return "조건 분기 결과 예약 · " + state + "\n" +
                (State.Selected != null && State.Selected.CanReserve && branch.CanReserve && !State.PublicationFailed ?
                    reserved ? "예약 해제 가능" : "예약 가능" : "예약 변경 불가 · 최신 대상 확인 필요");
        }

        private float ConditionReservationStatusHeight(float width)
        {
            float height = 0;
            foreach (string state in new[] { "활성화 (예약 중)", "비활성화 (예약 없음)" })
                foreach (string action in new[] { "예약 해제 가능", "예약 가능", "예약 변경 불가 · 최신 대상 확인 필요" })
                    height = Math.Max(height, TextHeight("조건 분기 결과 예약 · " + state + "\n" + action, statusStyle, width));
            return height;
        }

        private bool RowReserved(LogItem row)
        {
            if (!SupportsReservation) return false;
            if (!IsConditionTab) return LogBridge.IsReserved(CurrentTab, row.Target);
            if (row.ConditionDetails != null)
                foreach (LogConditionBranch branch in row.ConditionDetails.Branches)
                    if (LogBridge.IsReserved(CurrentTab, branch.Target)) return true;
            return false;
        }

        private void CompleteBranchScroll(Rect viewport)
        {
            if (Event.current.type != EventType.Repaint || area != Area.ConditionDetails || !ensureBranchVisible || viewport.height <= 0) return;
            Rect selected;
            if (State.SelectedBranchId == null || !branchRects.TryGetValue(State.SelectedBranchId, out selected)) return;
            float bottom = selected.yMax;
            foreach (Rect bounds in branchRects.Values) bottom = Math.Max(bottom, bounds.yMax);
            State.DetailScroll = LogLayout.BringIntoView(State.DetailScroll, selected, viewport.height, bottom);
            ensureBranchVisible = false;
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

        private int ToolCount => 19;
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
                case 16: return "로그 창 닫기";
                case 17: return "이벤트 선택 팝업 사용: " + (ConfigManager.EnableEventSelectionPopup.Value ? "ON" : "OFF");
                default: return "조건 분기 선택 팝업 사용: " + (ConfigManager.EnableSwitchSelectionPopup.Value ? "ON" : "OFF");
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
                case 17: ConfigManager.EnableEventSelectionPopup.Value = !ConfigManager.EnableEventSelectionPopup.Value; break;
                case 18: ConfigManager.EnableSwitchSelectionPopup.Value = !ConfigManager.EnableSwitchSelectionPopup.Value; break;
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
            GUIStyle style = chrome ? (highlighted ? chromeSelectedStyle : chromeButtonStyle) :
                highlighted ? selectedStyle : identity.StartsWith("branch:", StringComparison.Ordinal) ? branchStyle : buttonStyle;
            // MinHeight also forces maxHeight in Unity's word-wrap sizer. Let
            // rows measure their full wrapped caption at the final viewport
            // width, with no fixed height or permanent scrollbar gutter.
            // Button consumes the press/release event. GetLastRect after it
            // therefore returns Unity's dummy rect for EventType.Used instead
            // of the control's hit area. Allocate that same styled layout entry
            // first, then use its rect for both native input and our identity.
            var content = new GUIContent(label);
            Rect rect = maxWidth > 0 ?
                GUILayoutUtility.GetRect(content, style, GUILayout.MinWidth(1), GUILayout.MaxWidth(maxWidth), GUILayout.ExpandWidth(true)) :
                height > 0 ? GUILayoutUtility.GetRect(content, style, GUILayout.ExpandWidth(true), GUILayout.Height(height)) :
                GUILayoutUtility.GetRect(content, style, GUILayout.ExpandWidth(true));
            ImGuiPointerButton.Result input = ImGuiPointerButton.Draw(rect, content, style);
            if (input.Down && evt.button == 0 && rect.Contains(evt.mousePosition) && GUI.enabled)
            {
                pointerIdentity = identity;
                pointerContext = model.ContextId;
                GamepadBridge.NotifyPointerActivity();
                CancelPending();
                bool selectedAction = IsConditionTab ? area == Area.ConditionDetails && State.SelectedBranchId != null && identity == BranchIdentity(State.SelectedBranchId) :
                    State.SelectedId != null && identity == "row:" + State.SelectedId;
                if (SupportsReservation && selectedAction) pointerReservation = ArmSelectedReservation();
            }
            if (!input.Clicked || !GUI.enabled) return;
            // Button order may change during refresh. Only its original context
            // and stable control identity may consume a physical mouse release.
            if (!input.Up || pointerIdentity != identity || pointerContext != model.ContextId) return;
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
                area = IsConditionTab && State.SelectedBranch != null ? Area.ConditionDetails : Area.List;
            }
        }

        private void ActivateRow(string id)
        {
            area = Area.List;
            if (State.SelectedId != id) { Select(id); area = Area.List; return; }
            // Re-clicking a selected row uses only the authority captured on
            // pointer-down. A release never creates fresh action authority.
            if (!IsConditionTab && SupportsReservation) Reserve(pointerReservation);
        }

        private bool Select(string id)
        {
            if (!State.Select(id)) return false;
            observedSelection = State.SelectedId; observedBranch = State.SelectedBranchId; ensureVisible = true; manualListScroll = false; CancelPending();
            return true;
        }

        private void SwitchTab(int index)
        {
            if (index == currentTabIndex && area == Area.List) return;
            CancelPending();
            currentTabIndex = index;
            releaseTextFocus = true;
            area = Area.List;
            observedSelection = State.SelectedId;
            observedBranch = State.SelectedBranchId;
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
            if (IsEventTab)
            {
                string eligibility = row.RefreshState != LogRefreshState.Healthy || State.PublicationFailed || row.EventEligibility == null ?
                    "판정 미확인" : row.EventEligibility.StatusText;
                result = eligibility + (string.IsNullOrEmpty(result) ? "" : " · " + result);
            }
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
            string action = CurrentTab == "미션" ? "미션 조건 통과 예약" : IsEventTab ? "이벤트 결과 예약" : "조건 분기 결과 예약";
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

        private float HintHeight(float width)
        {
            // Entering a branch by touch must not resize its scroll viewport
            // or the compact reading header and clamp the current scroll.
            if (IsConditionTab && (area == Area.List || area == Area.ConditionDetails))
                return Math.Max(TextHeight(ConditionListHint, statusStyle, width), TextHeight(ConditionDetailHint, statusStyle, width));
            return TextHeight(Hint(), statusStyle, width);
        }

        private string Hint()
        {
            if (area == Area.Tools) return "현재: 도구 · ↑↓ 이동 / A 작동 / B 이전 영역 / LB·RB 탭";
            if (area == Area.PopupSetting) return "현재: 팝업 설정 · A / Enter 켜기·끄기 / ↓ 목록 / LB·RB 탭 / Y 도구 / B 닫기";
            if (IsChoiceTab) return "현재: 선택지 읽기 · ↑↓·오른쪽 스틱 본문 스크롤 / LB·RB 탭 / Y 도구 / X 새로고침 / B 닫기";
            if (area == Area.ConditionDetails) return ConditionDetailHint;
            if (IsConditionTab) return ConditionListHint;
            if (HasPopupSetting)
            {
                string reservationAction = State.Selected == null ? "" : !State.Selected.CanReserve || State.PublicationFailed ? " / 예약 변경 불가" :
                    " / A · Enter · 선택된 항목 다시 클릭·터치 " + (LogBridge.IsReserved(CurrentTab, State.Selected.Target) ? "예약 해제" : "예약");
                return "현재: " + CurrentTab + " 목록 · ↑↓ 항목 이동" + reservationAction +
                    " / 목록 맨 위에서 ↑ 팝업 설정 / 오른쪽 스틱 본문 / LB·RB 탭 / Y 도구 / X 새로고침 / B 닫기";
            }
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
