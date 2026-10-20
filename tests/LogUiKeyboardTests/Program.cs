using System;
using System.Collections.Generic;
using System.Reflection;
using DiceMaster.LogUI;
using DiceMaster.Gamepad;
using DiceMaster.LiveLog;
using UnityEngine;

internal static partial class Program
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new InvalidOperationException(message); }

    private static void Main()
    {
        InformationLayoutGeometry();
        RendererInformationPages();
        RendererDeferredStartupLayout();
        WindowRootContract();
        RendererNativeWindowBounds();
        RendererNavigation();
        RendererWithoutSearch();
        RendererPopupSettings();
        RendererJudgmentPresentation();
        RendererMissionStatusRows();
        RendererWaitingStatusExplanation();
        RendererDirectListReservations();
        RendererReservableConditionDetails();
        RendererReservationToggleState();
        RendererListChromePointerTransactions();
        NativeButtonEventContract();
        RendererNativePointerTransactions();
        RendererNativeConsumedCornerPress();
        RendererNonInputPassesPreservePointer();
        RendererExplicitFocusLifecycle();
        RendererToolsDeviceTakeover();
        CursorOwnershipLifecycle();
        RendererListPointerSelectionTransactions();
        RendererPointerTakeoverTransactions();
        RendererActualControllerPointerBoundary();
        RendererActualControllerKeyboardBoundary();
        RendererChoiceAndConditionNavigation();
        RendererStructuredConditionBlocks();
        RendererChoiceAndConditionReflow();
        RendererAutomaticScrollbars();
        RendererRefreshWording();
        RendererRenderingBoundaries();
        RendererLocalizedTitlesAndLiteralIdentities();
        LayoutGeometry();
        RendererViewportAndFocus();
        RendererCompactAndPointerScroll();
        WrappingLayoutBoundary();
        RendererDefaultsAndMeasuredRows();
        Console.WriteLine("PASS: LogUiKeyboardTests: " + checks + " checks passed; actual renderer/model/controller sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution). Physical keyboard navigation, cursor and managed pointer boundaries remain covered.");
    }

    private static object Field(LogRenderer renderer, string name) => typeof(LogRenderer).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(renderer);
    private static void SetField(LogRenderer renderer, string name, object value) => typeof(LogRenderer).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(renderer, value);
    private static string Area(LogRenderer renderer) => Field(renderer, "area").ToString();
    private static LogItem Item(string id, object target = null) => new LogItem { Id = id, RawId = id, Title = "한국어 " + id, Description = "설명", Target = target };
    private static void Publish(LogModel model, string tab, params LogItem[] rows) => model.Publish(new LogSnapshot(model.ContextId, tab, rows));
    private static void Key(LogRenderer renderer, EventType type, KeyCode key)
    {
        Event.current = new Event { type = type, rawType = type, keyCode = key };
        typeof(LogRenderer).GetMethod("HandleKeyboard", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(renderer, null);
    }
    private static void PressKey(LogRenderer renderer, KeyCode key)
    { Input.Held.Add(key); Key(renderer, EventType.KeyDown, key); }
    private static void ReleaseKey(LogRenderer renderer, KeyCode key)
    { Input.Held.Remove(key); Key(renderer, EventType.KeyUp, key); }
    private static void TapKey(LogRenderer renderer, KeyCode key)
    { PressKey(renderer, key); ReleaseKey(renderer, key); }

    private static void WindowRootContract()
    {
        var requested = new Rect(20, 20, 1200, 860);
        var style = new GUIStyle { padding = new RectOffset(8, 8, 34, 8) };
        var label = new GUIStyle { fontSize = 20 };
        Action<int> onlyFreeArea = id =>
        {
            GUILayout.BeginArea(new Rect(40, 80, 400, 300));
            GUILayout.Label("자유 영역의 실제 표시 범위", label);
            GUILayout.EndArea();
        };
        Event.current = new Event { type = EventType.Layout, rawType = EventType.Layout };
        Rect automatic = GUILayout.Window(9001, requested, onlyFreeArea, "automatic", style,
            GUILayout.Width(requested.width), GUILayout.Height(requested.height));
        Check(automatic.width == 16 && automatic.height == 42, "negative control: area-only automatic window collapses to padding despite fixed options");
        Check(GUILayout.LastWindow.RootEntryCount == 0 && GUILayout.LastWindow.FreeAreas.Count == 1,
            "free areas are separate from ordinary root entries");
        Event.current = new Event { type = EventType.Repaint, rawType = EventType.Repaint };
        GUILayout.Window(9001, requested, onlyFreeArea, "automatic", style,
            GUILayout.Width(requested.width), GUILayout.Height(requested.height));
        Check(GUILayout.LastWindow.NativeRect.width == 16 && GUILayout.LastWindow.RequestedRect.width == 1200,
            "repaint keeps native layout result instead of substituting requested rectangle");
        Check(GUILayout.Controls.Count == 1 && GUILayout.Controls[0].VisibleRect.width == 0 && GUILayout.Controls[0].VisibleRect.height == 0,
            "recorded free-area control is clipped away by the collapsed native window");

        Event.current = new Event { type = EventType.Layout, rawType = EventType.Layout };
        Rect fixedWindow = GUI.Window(9002, requested, onlyFreeArea, "fixed", style);
        Check(fixedWindow == requested && !GUILayout.LastWindow.Automatic,
            "positive control: fixed GUI window preserves original native rectangle with area-only content");
        Event.current = new Event { type = EventType.Repaint, rawType = EventType.Repaint };
        GUI.Window(9002, requested, onlyFreeArea, "fixed", style);
        Check(GUILayout.LastWindow.NativeRect == requested && GUILayout.LastWindow.RootEntryCount == 0 && GUILayout.LastWindow.FreeAreas.Count == 1,
            "fixed window retains native bounds through layout and repaint without manufacturing a root child");
        Check(GUILayout.Controls[0].VisibleRect == GUILayout.Controls[0].WindowLocalRect,
            "free-area label inside fixed native window retains its full visible rectangle");
    }

    private static bool ContainsRect(Rect outer, Rect inner)
        => inner.x >= outer.x && inner.y >= outer.y && inner.x + inner.width <= outer.x + outer.width && inner.yMax <= outer.yMax;

    private static void NativeWindowContainsContent(LogRenderer renderer, string scenario)
    {
        GUILayout.WindowRecord actual = GUILayout.LastWindow;
        Check(actual != null && actual.NativeRect == actual.RequestedRect,
            scenario + ": actual renderer native window retains requested bounds before managed ConstrainWindow can mask shrinkage");
        Check(!actual.Automatic && actual.RootEntryCount == 0 && actual.FreeAreas.Count >= 3,
            scenario + ": fixed window supports separately registered free areas and an empty ordinary root");
        var nativeClip = new Rect(0, 0, actual.NativeRect.width, actual.NativeRect.height);
        var layout = (LogLayout)Field(renderer, "layout");
        Check(ContainsRect(nativeClip, layout.Tabs) && ContainsRect(nativeClip, layout.Content) && ContainsRect(nativeClip, layout.Footer),
            scenario + ": native window contains tabs, content and footer");
        Check(actual.FreeAreas.TrueForAll(area => ContainsRect(nativeClip, area)), scenario + ": every free area remains inside native clip");
        Check(GUILayout.Controls.Exists(control => control.Button && control.VisibleRect.width > 0 && control.VisibleRect.height > 0),
            scenario + ": at least one control has visible pixels inside the native clip, not merely a recorded draw call");
    }

    private static void RendererNativeWindowBounds()
    {
        int oldFont = DiceMaster.Config.ConfigManager.LogFontSize.Value, oldScreenWidth = Screen.width, oldScreenHeight = Screen.height;
        float oldX = DiceMaster.Config.ConfigManager.LogWindowX.Value, oldY = DiceMaster.Config.ConfigManager.LogWindowY.Value;
        float oldWidth = DiceMaster.Config.ConfigManager.LogWindowWidth.Value, oldHeight = DiceMaster.Config.ConfigManager.LogWindowHeight.Value;
        try
        {
            Screen.width = 1920; Screen.height = 1080;
            foreach (int[] setting in new[] { new[] { 1200, 860, 20 }, new[] { 540, 440, 64 } })
            {
                DiceMaster.Config.ConfigManager.LogWindowX.Value = DiceMaster.Config.ConfigManager.LogWindowY.Value = 20;
                DiceMaster.Config.ConfigManager.LogWindowWidth.Value = setting[0];
                DiceMaster.Config.ConfigManager.LogWindowHeight.Value = setting[1];
                DiceMaster.Config.ConfigManager.LogFontSize.Value = setting[2];
                var model = new LogModel(); model.ResetContext(900);
                foreach (string tab in new[] { "선택지", "조건", "이벤트", "미션" }) Publish(model, tab, Item("native-row", new object()));
                var renderer = new LogRenderer(model, null, null) { InputHint = setting[2] == 64 ? new string('안', 200) : "패드 탐색 사용 가능" };
                renderer.Handle(LogPadCommand.None);
                for (int index = 0; index < 4; index++)
                {
                    renderer.Handle(LogPadCommand.Activate);
                    Repaint(renderer);
                    NativeWindowContainsContent(renderer, setting[0] + "x" + setting[1] + " " + renderer.CurrentTab);
                    if (index < 3) renderer.Handle(LogPadCommand.NextTab);
                }
                renderer.Handle(LogPadCommand.Tools);
                Repaint(renderer);
                NativeWindowContainsContent(renderer, "tools");
            }
        }
        finally
        {
            DiceMaster.Config.ConfigManager.LogFontSize.Value = oldFont;
            DiceMaster.Config.ConfigManager.LogWindowX.Value = oldX;
            DiceMaster.Config.ConfigManager.LogWindowY.Value = oldY;
            DiceMaster.Config.ConfigManager.LogWindowWidth.Value = oldWidth;
            DiceMaster.Config.ConfigManager.LogWindowHeight.Value = oldHeight;
            Screen.width = oldScreenWidth; Screen.height = oldScreenHeight;
        }
    }

    private static void RendererNavigation()
    {
        var model = new LogModel();
        model.ResetContext(1);
        Publish(model, "선택지", Item("card1"), Item("card2"));
        int closes = 0, refreshes = 0;
        var renderer = new LogRenderer(model, () => refreshes++, () => closes++);
        renderer.Handle(LogPadCommand.None); // initial context consumes pending input
        renderer.Handle(LogPadCommand.Activate);
        Check(model.Tab("선택지").SelectedId == null && Area(renderer) == "List", "choice A leaves the read-only comparison unselected");
        renderer.Handle(LogPadCommand.Activate);
        Check(model.Tab("선택지").SelectedId == null && Area(renderer) == "List", "repeated A leaves the read-only comparison unselected");
        renderer.Handle(LogPadCommand.Down);
        renderer.Handle(LogPadCommand.Activate);
        Check(model.Tab("선택지").SelectedId == null && model.Tab("선택지").ListScroll > 0 && Area(renderer) == "List", "choice direction scrolls its body without creating a selection");
        renderer.Handle(LogPadCommand.Back);
        Check(closes == 1, "B closes directly from the always-visible choice comparison");
        renderer.Handle(LogPadCommand.NextTab);
        renderer.Handle(LogPadCommand.NextTab);
        Check(renderer.CurrentTab == "이벤트", "LB/RB preserve four-tab order");
        object target = new object();
        Publish(model, "이벤트", Item("event1", target), Item("event2", new object()));
        renderer.Handle(LogPadCommand.Activate);
        renderer.Handle(LogPadCommand.Activate);
        Check(Area(renderer) == "List" && !model.Tab("이벤트").ExecutionFocus && LogBridge.Reservations == 1 && LogBridge.IsReserved("이벤트", target),
            "event first A selects and the next fresh A reserves the selected result exactly once");
        renderer.Handle(LogPadCommand.Down);
        Check(model.Tab("이벤트").SelectedId == "event2" && LogBridge.Reservations == 1,
            "event down navigates candidates without changing the current reservation");
        renderer.Handle(LogPadCommand.Up);
        Check(model.Tab("이벤트").SelectedId == "event1", "event up returns to previous candidate");
        renderer.Handle(LogPadCommand.Activate);
        Check(LogBridge.Reservations == 2 && !LogBridge.IsReserved("이벤트", target), "fresh event confirmation releases the selected reservation once");
        renderer.Handle(LogPadCommand.Left);
        renderer.Scroll(1);
        Check(model.Tab("이벤트").DetailScroll > 0 && model.Tab("이벤트").ListScroll == 0, "right stick scrolls selected detail while list owns focus");
        Publish(model, "이벤트", Item("event2", new object()));
        renderer.Handle(LogPadCommand.Activate);
        Check(model.Tab("이벤트").SelectedId == null && Area(renderer) == "List", "target removal consumes same-frame A before replacement selection");
        renderer.Handle(LogPadCommand.Activate);
        Check(model.Tab("이벤트").SelectedId == "event2", "replacement requires a later explicit selection");
        model.ResetContext(2);
        Publish(model, "이벤트", Item("new-context", new object()));
        renderer.Handle(LogPadCommand.Activate);
        Check(model.Tab("이벤트").SelectedId == null && GamepadBridge.Cancellations >= 3, "context change consumes pending input and cancels native owner latch");
        renderer.Handle(LogPadCommand.Tools);
        Check(Area(renderer) == "Tools", "empty/new context still reaches tools");
        renderer.Handle(LogPadCommand.Activate);
        Check(Area(renderer) == "Tools" && refreshes == 1, "first tool requests refresh without leaving tools");
        renderer.Handle(LogPadCommand.Refresh);
        Check(refreshes == 2, "X requests one manual refresh");
        renderer.Handle(LogPadCommand.Back);
        Input.Held.Add(KeyCode.Return);
        Key(renderer, EventType.KeyDown, KeyCode.Return);
        Check(model.Tab("이벤트").SelectedId == "new-context", "fresh Enter selects once");
        model.ResetContext(3);
        Publish(model, "이벤트", Item("third-context", new object()));
        renderer.ResetInputFocus();
        Key(renderer, EventType.KeyDown, KeyCode.Return);
        Check(model.Tab("이벤트").SelectedId == null, "held Enter repeat cannot confirm after focus reset");
        Input.Held.Clear();
        Key(renderer, EventType.KeyUp, KeyCode.Return);
        renderer.Handle(LogPadCommand.None);
        Key(renderer, EventType.KeyDown, KeyCode.Return);
        Check(model.Tab("이벤트").SelectedId == "third-context", "Enter works after release and fresh press");

        Rect bounds = LogRenderer.ConstrainWindow(new Rect(float.NaN, float.PositiveInfinity, -2, 99999), 1920, 1080);
        Check(bounds.x == 20 && bounds.y == 0 && bounds.width == 540 && bounds.height == 1080, "invalid config normalizes to screen bounds");
        bounds = LogRenderer.ConstrainWindow(new Rect(9999, 9999, 750, 750), 400, 300);
        Check(bounds.x == 0 && bounds.y == 0 && bounds.width == 400 && bounds.height == 300, "small viewport retains whole window within bounds");
        renderer.Handle(LogPadCommand.Tools);
        for (int index = 0; index < 16; index++) renderer.Handle(LogPadCommand.Down);
        renderer.Handle(LogPadCommand.Activate);
        Check(closes == 2, "all tools navigable through pad, including close");
    }

    private static void RendererWithoutSearch()
    {
        Rect saved = ConfiguredRect(); int savedFont = DiceMaster.Config.ConfigManager.LogFontSize.Value;
        int oldWidth = Screen.width, oldHeight = Screen.height;
        try
        {
            Screen.width = 2560; Screen.height = 1440;
            foreach (string tab in new[] { "선택지", "조건", "미션" })
            {
                ConfigureLayout(new Rect(120, 100, 1200, 900), 20);
                var model = new LogModel(); model.ResetContext(740);
                var rows = new LogItem[12];
                for (int index = 0; index < rows.Length; index++)
                {
                    rows[index] = Item("whole-row-" + index.ToString("00"), new object());
                    rows[index].Description = "독립적인 본문 " + index;
                }
                Publish(model, tab, rows);
                var renderer = new LogRenderer(model, null, null);
                renderer.Handle(LogPadCommand.None);
                for (int index = 0; index < Array.IndexOf(new[] { "선택지", "조건", "이벤트", "미션" }, tab); index++) renderer.Handle(LogPadCommand.NextTab);
                Repaint(renderer);
                var layout = (LogLayout)Field(renderer, "layout");
                Check(!layout.CompactChrome && Math.Abs(layout.Content.y - layout.Status.yMax - layout.Gap) < .01f,
                    tab + ": body begins immediately after status with no reserved search row");
                Check(Math.Abs(layout.Content.yMax - layout.Footer.y + layout.Gap) < .01f,
                    tab + ": body consumes every pixel remaining before the footer");
                foreach (LogItem row in rows)
                    Check(GUILayout.ScrollViews[0].Controls.Exists(control => control.Button == (tab != "선택지") && control.Text.Contains(row.Id, StringComparison.Ordinal)),
                        tab + ": all published rows remain in the unfiltered rendered list");
                Check(!GUILayout.Controls.Exists(control => control.Text.Contains("검색", StringComparison.Ordinal)
                    || control.Text.Contains("자판", StringComparison.Ordinal) || control.Text == "패드 입력" || control.Text == "지우기"),
                    tab + ": content has no search field, screen keyboard or search action");
                float heading = tab == "선택지" ? 0 : (int)Field(renderer, "chromeFont") + 14;
                if (tab == "조건")
                    heading += GUILayout.Controls.Find(control => control.Button && control.Text.StartsWith("조건 분기 선택 팝업:", StringComparison.Ordinal)).Rect.height + 8;
                Check(Math.Abs(GUILayout.ScrollViews[0].Viewport.height - layout.Content.height + heading) < .01f,
                    tab + ": the list uses the full body except its own visible heading");
                for (int index = 0; index < rows.Length; index++) renderer.Handle(LogPadCommand.Down);
                Repaint(renderer); Repaint(renderer);
                Check(tab == "선택지" ? model.Tab(tab).SelectedId == null && model.Tab(tab).ListScroll > 0 : model.Tab(tab).SelectedId == rows[rows.Length - 1].Id,
                    tab + ": pad navigation scrolls the read-only body or reaches the final selectable row without a filter");
                renderer.Handle(LogPadCommand.Tools);
                Repaint(renderer);
                var tools = GUILayout.ScrollViews[0].Controls.FindAll(control => control.Button);
                Check(tools.Count == 19 && !tools.Exists(control => control.Text.Contains("자판", StringComparison.Ordinal)),
                    tab + ": tools expose nineteen actions including event and condition popup settings, with no screen keyboard");
                Check(tools[0].Text.Contains("게임 데이터 새로고침", StringComparison.Ordinal)
                    && tools[11].Text.Contains("기본값 복구", StringComparison.Ordinal)
                    && tools[12].Text.Contains("패드 소유자", StringComparison.Ordinal)
                    && tools[13].Text.Contains("선택지 탭", StringComparison.Ordinal)
                    && tools[14].Text.Contains("이벤트 탭", StringComparison.Ordinal)
                    && tools[15].Text.Contains("이전 영역", StringComparison.Ordinal)
                    && tools[16].Text.Contains("로그 창 닫기", StringComparison.Ordinal),
                    tab + ": remaining tools retain their documented order and quick-tab actions");
            }

            for (int tool = 0; tool < 17; tool++)
            {
                Rect initial = new Rect(120, 100, 1200, 900);
                ConfigureLayout(initial, 20);
                var model = new LogModel(); model.ResetContext(741);
                int refresh = 0, reselect = 0, close = 0;
                var renderer = new LogRenderer(model, () => refresh++, () => close++) { ReselectController = () => reselect++ };
                renderer.Handle(LogPadCommand.None);
                for (int index = 0; index < 3; index++) renderer.Handle(LogPadCommand.NextTab);
                Repaint(renderer);
                renderer.Handle(LogPadCommand.Tools);
                for (int index = 0; index < tool; index++) renderer.Handle(LogPadCommand.Down);
                renderer.Handle(LogPadCommand.Activate);
                Rect expected = initial;
                if (tool == 3) expected.width -= 40; if (tool == 4) expected.width += 40;
                if (tool == 5) expected.height -= 40; if (tool == 6) expected.height += 40;
                if (tool == 7) expected.x -= 32; if (tool == 8) expected.x += 32;
                if (tool == 9) expected.y -= 32; if (tool == 10) expected.y += 32;
                if (tool == 11) expected = LogLayout.DefaultWindow(Screen.width, Screen.height);
                Check(renderer.WindowRect == expected, "tool " + tool + " changes only its intended window geometry");
                int expectedFont = tool == 1 ? 19 : tool == 2 ? 21 : tool == 11 ? LogLayout.DefaultFont(Screen.height) : 20;
                Check(DiceMaster.Config.ConfigManager.LogFontSize.Value == expectedFont,
                    "tool " + tool + " preserves or changes font according to its new index");
                Check(refresh == (tool == 0 ? 1 : 0) && reselect == (tool == 12 ? 1 : 0) && close == (tool == 16 ? 1 : 0),
                    "tool " + tool + " reaches exactly its intended callback");
                Check(renderer.CurrentTab == (tool == 13 ? "선택지" : tool == 14 ? "이벤트" : "미션")
                    && Area(renderer) == (tool >= 12 && tool <= 15 ? "List" : "Tools"),
                    "tool " + tool + " preserves quick-tab and guarded return behavior after search removal");
            }
        }
        finally { ConfigureLayout(saved, savedFont); Screen.width = oldWidth; Screen.height = oldHeight; }
    }

    private static LogItem StatusRow(string id, object target = null)
    {
        LogItem row = Item(id, target);
        row.Title = "미션 정보 · " + id;
        row.Description = "설명 조회 대기 · " + id;
        row.IsStatusOnly = true;
        return row;
    }

    private static LogRenderer MissionRenderer(LogModel model, LogItem[] rows, Action refresh = null, Action close = null)
    {
        model.ResetContext(750); Publish(model, "미션", rows);
        var renderer = new LogRenderer(model, refresh, close);
        renderer.Handle(LogPadCommand.None);
        for (int index = 0; index < 3; index++) renderer.Handle(LogPadCommand.NextTab);
        return renderer;
    }

    private static void RendererMissionStatusRows()
    {
        Rect saved = ConfiguredRect(); int font = DiceMaster.Config.ConfigManager.LogFontSize.Value;
        int screenWidth = Screen.width, screenHeight = Screen.height;
        int attempts = LogBridge.Reservations; object savedTarget = LogBridge.LastTarget;
        try
        {
            Screen.width = 1920; Screen.height = 1080;
            ConfigureLayout(new Rect(20, 20, 1200, 900), 20);
            object target = new object();
            LogItem first = Item("real-first", target); first.Title = "미션 정보 · 설명 조회 대기 · real-first";
            LogItem waiting = Item("real-waiting"); waiting.RefreshState = LogRefreshState.Waiting;
            LogItem retained = Item("real-retained", new object()); retained.RefreshState = LogRefreshState.RetainedFailure;
            LogItem[] statuses = { StatusRow("status-first"), StatusRow("status-middle", new object()), StatusRow("status-last") };
            var model = new LogModel();
            var renderer = MissionRenderer(model, new[] { statuses[0], first, statuses[1], waiting, retained, statuses[2] });
            LogTabState state = model.Tab("미션");
            Repaint(renderer);
            string[] order = { first.Id, waiting.Id, retained.Id, statuses[0].Id, statuses[1].Id, statuses[2].Id };
            var controls = GUILayout.ScrollViews[0].Controls.FindAll(control => control.Button);
            Check(controls.Count == order.Length, "mission renders every regular and information row");
            for (int index = 0; index < order.Length; index++)
                Check(controls[index].Text.Contains(order[index], StringComparison.Ordinal) && controls[index].Enabled == (index < 3),
                    "interleaved publication preserves regular/status order and disables status rows at the bottom");

            renderer.Handle(LogPadCommand.Up);
            Check(state.SelectedId == retained.Id, "initial up skips bottom information rows and selects the last readable mission");
            Rect mixedWindow = renderer.WindowRect;
            mixedWindow.height = 500; SetField(renderer, "window", mixedWindow);
            Repaint(renderer); Repaint(renderer);
            Check(GUILayout.ScrollViews[0].ContentHeight > GUILayout.ScrollViews[0].Viewport.height,
                "mixed mission footer really extends beyond the list viewport");
            float beforeInformation = state.ListScroll;
            renderer.Handle(LogPadCommand.Down);
            Repaint(renderer);
            Check(state.SelectedId == retained.Id && state.ListScroll > beforeInformation,
                "down after the last readable mission scrolls trailing information into view without selecting it");
            renderer.Handle(LogPadCommand.Up);
            Repaint(renderer); Repaint(renderer);
            GUILayout.ScrollViewRecord followed = ScrollWithButton(waiting.Title);
            GUILayout.ControlRecord focusedRow = followed.Controls.Find(control => control.Button && control.Text.Contains(waiting.Id, StringComparison.Ordinal));
            Check(state.SelectedId == waiting.Id && focusedRow.Rect.y >= state.ListScroll - .01f
                && focusedRow.Rect.yMax <= state.ListScroll + followed.Viewport.height + .01f,
                "up from the information footer resumes readable-row focus following");
            mixedWindow.height = 900; SetField(renderer, "window", mixedWindow);
            renderer.ResetInputFocus(); state.ClearSelection();
            renderer.Handle(LogPadCommand.None);
            renderer.Handle(LogPadCommand.Activate);
            Check(state.SelectedId == first.Id && Area(renderer) == "List", "first A selects the first actual mission despite an information row leading the original publication");
            renderer.Handle(LogPadCommand.Up);
            Check(state.SelectedId == first.Id, "up at the first readable mission remains at the boundary");
            TapKey(renderer, KeyCode.DownArrow);
            Check(state.SelectedId == waiting.Id && Area(renderer) == "List", "keyboard can select a genuine targetless waiting mission for reading");
            renderer.Handle(LogPadCommand.Activate); Repaint(renderer);
            Check(state.SelectedId == waiting.Id && GUILayout.LiteralLabels.Contains(waiting.Description) && !state.ExecutionFocus,
                "targetless waiting rows remain readable while their reservation action stays unavailable");
            TapKey(renderer, KeyCode.DownArrow);
            Check(state.SelectedId == retained.Id, "keyboard selects a genuine retained mission rather than treating its state as informational");
            TapKey(renderer, KeyCode.UpArrow); TapKey(renderer, KeyCode.UpArrow);
            renderer.Handle(LogPadCommand.Right);
            Check(Area(renderer) == "List" && !state.ExecutionFocus && LogBridge.Reservations == attempts,
                "right remains a no-op even for a healthy mission with an information-like title");
            renderer.Handle(LogPadCommand.Left);

            foreach (LogItem status in statuses)
                foreach (bool touch in new[] { false, true })
                {
                    Vector2 point = PointerTarget(renderer, control => control.Button && control.Text.Contains(status.Id, StringComparison.Ordinal));
                    Func<string, bool> pressed = text => text.Contains(status.Id, StringComparison.Ordinal);
                    DrawPointer(renderer, touch ? EventType.TouchDown : EventType.MouseDown, touch ? EventType.TouchDown : EventType.MouseDown, point);
                    SoftResetForPointer(renderer);
                    DrawPointer(renderer, touch ? EventType.TouchUp : EventType.MouseUp, touch ? EventType.TouchUp : EventType.MouseUp, point, pressed, consume: true);
                    Check(state.SelectedId == first.Id && Area(renderer) == "List" && !state.ExecutionFocus
                        && Field(renderer, "pointerIdentity") == null && Field(renderer, "pointerReservation") == null && LogBridge.Reservations == attempts,
                        "mouse/touch release on a disabled information row cannot select it or replace the readable mission");
                }

            var onlyRows = new LogItem[30];
            for (int index = 0; index < onlyRows.Length; index++) onlyRows[index] = StatusRow("only-info-" + index.ToString("00"), new object());
            int refreshes = 0, closes = 0;
            var onlyModel = new LogModel(); var onlyRenderer = MissionRenderer(onlyModel, onlyRows, () => refreshes++, () => closes++);
            foreach (LogPadCommand command in new[] { LogPadCommand.Up, LogPadCommand.Down, LogPadCommand.Activate, LogPadCommand.Right }) onlyRenderer.Handle(command);
            TapKey(onlyRenderer, KeyCode.UpArrow); TapKey(onlyRenderer, KeyCode.DownArrow); TapKey(onlyRenderer, KeyCode.Return);
            Repaint(onlyRenderer);
            Check(onlyModel.Tab("미션").SelectedId == null && !onlyModel.Tab("미션").ExecutionFocus && Area(onlyRenderer) == "List"
                && !GUILayout.Controls.Exists(control => control.Button && control.Text.Contains("미션 조건 통과 예약", StringComparison.Ordinal)),
                "an all-information mission list never acquires selection or a reservation control through pad/keyboard");
            Check(GUILayout.ScrollViews[0].ContentHeight > GUILayout.ScrollViews[0].Viewport.height,
                "all-information scenario actually overflows and requires navigation for reading");
            onlyRenderer.Scroll(1);
            Check(onlyModel.Tab("미션").ListScroll > 0 && onlyModel.Tab("미션").DetailScroll == 0,
                "right stick scrolls the all-information mission list when no selectable detail exists");
            onlyRenderer.Handle(LogPadCommand.Tools); onlyRenderer.Handle(LogPadCommand.Activate);
            Check(Area(onlyRenderer) == "Tools" && refreshes == 1, "all-information missions still expose working refresh tools");
            onlyRenderer.Handle(LogPadCommand.Back); onlyRenderer.Handle(LogPadCommand.Back);
            Check(closes == 1 && LogBridge.Reservations == attempts, "all-information mission list can close without reserving a placeholder");

            var normalModel = new LogModel(); var normalRenderer = MissionRenderer(normalModel, LongRows(30));
            for (int index = 0; index < 31; index++) normalRenderer.Handle(LogPadCommand.Down);
            Repaint(normalRenderer); Repaint(normalRenderer);
            float normalEnd = normalModel.Tab("미션").ListScroll;
            normalRenderer.Handle(LogPadCommand.Down); Repaint(normalRenderer);
            Check(normalModel.Tab("미션").ListScroll == normalEnd,
                "down at the end of an ordinary mission list does not gain placeholder-footer scrolling");

            var replaced = new LogModel(); var replacementRenderer = MissionRenderer(replaced, new[] { Item("changing-mission", target) });
            replacementRenderer.Handle(LogPadCommand.Activate);
            Input.Held.Add(KeyCode.Return);
            replacementRenderer.ResetInputFocus();
            replaced.Tab("미션").ExecutionFocus = true;
            ReservationTicket oldTicket = replaced.Tab("미션").BeginReservation();
            Check(oldTicket != null, "transition regression begins with an explicitly armed model ticket for the selected mission");
            Publish(replaced, "미션", StatusRow("changing-mission", target)); Repaint(replacementRenderer);
            PressKey(replacementRenderer, KeyCode.Return);
            Check(replaced.Tab("미션").SelectedId == null && Area(replacementRenderer) == "List" && !replaced.Tab("미션").CanActivate(oldTicket)
                && !replaced.Tab("미션").ExecutionFocus && LogBridge.Reservations == attempts,
                "healthy mission becoming an information row cancels selection/ticket and consumes held confirmation");
            Publish(replaced, "미션", Item("changing-mission", target)); Repaint(replacementRenderer);
            PressKey(replacementRenderer, KeyCode.Return);
            Check(replaced.Tab("미션").SelectedId == null && LogBridge.Reservations == attempts,
                "healthy recovery neither auto-selects nor accepts the old held confirmation");
            ReleaseKey(replacementRenderer, KeyCode.Return);
            TapKey(replacementRenderer, KeyCode.Return);
            Check(replaced.Tab("미션").SelectedId == "changing-mission" && Area(replacementRenderer) == "List" && !replaced.Tab("미션").ExecutionFocus,
                "recovery requires a new explicit selection before detail entry");
            replacementRenderer.Handle(LogPadCommand.Right); replacementRenderer.Handle(LogPadCommand.Tools);
            Publish(replaced, "미션", StatusRow("changing-mission", target));
            replacementRenderer.Handle(LogPadCommand.Back); Repaint(replacementRenderer);
            Check(Area(replacementRenderer) == "List" && replaced.Tab("미션").SelectedId == null && !replaced.Tab("미션").ExecutionFocus,
                "tools cannot return execution focus to a mission that became an information row");

            foreach (bool touch in new[] { false, true })
            {
                var pending = new LogModel(); var pendingRenderer = MissionRenderer(pending, new[] { Item("pending-status", target) });
                pendingRenderer.Handle(LogPadCommand.Activate);
                Vector2 point = PointerTarget(pendingRenderer, control => control.Button && control.Text.Contains("pending-status", StringComparison.Ordinal));
                DrawPointer(pendingRenderer, touch ? EventType.TouchDown : EventType.MouseDown, touch ? EventType.TouchDown : EventType.MouseDown, point);
                Publish(pending, "미션", StatusRow("pending-status", target));
                SoftResetForPointer(pendingRenderer);
                DrawPointer(pendingRenderer, touch ? EventType.TouchUp : EventType.MouseUp, touch ? EventType.TouchUp : EventType.MouseUp,
                    point, text => text.Contains("pending-status", StringComparison.Ordinal), consume: true);
                Check(LogBridge.Reservations == attempts && pending.Tab("미션").SelectedId == null && Field(pendingRenderer, "pointerReservation") == null,
                    "publication becoming information-only invalidates an in-flight mouse/touch reservation release");
            }
        }
        finally
        {
            Input.Held.Clear(); GUILayout.ButtonInput = null; GUILayout.ConsumeButtonEvent = false;
            LogBridge.Reservations = attempts; LogBridge.LastTarget = savedTarget;
            ConfigureLayout(saved, font); Screen.width = screenWidth; Screen.height = screenHeight;
        }
    }

    private static void RendererWaitingStatusExplanation()
    {
        Rect saved = ConfiguredRect(); int font = DiceMaster.Config.ConfigManager.LogFontSize.Value;
        int screenWidth = Screen.width, screenHeight = Screen.height;
        try
        {
            Screen.width = 1920; Screen.height = 1080;
            ConfigureLayout(new Rect(20, 20, 1200, 900), 20);
            foreach (bool allInformation in new[] { false, true })
            {
                LogItem first = StatusRow("waiting-info-a"), second = StatusRow("waiting-info-b");
                first.RefreshState = second.RefreshState = LogRefreshState.Waiting;
                LogItem readable = Item("readable-waiting"); readable.RefreshState = LogRefreshState.Waiting;
                var model = new LogModel();
                var renderer = MissionRenderer(model, allInformation ? new[] { first, second } : new[] { first, readable, second });
                if (!allInformation) renderer.Handle(LogPadCommand.Activate);
                Repaint(renderer);
                GUILayout.ScrollViewRecord list = GUILayout.ScrollViews[0];
                var explanations = list.Controls.FindAll(control => !control.Button
                    && control.Text.Contains("미션 미완료·조건 미충족을 뜻하지 않습니다", StringComparison.Ordinal)
                    && control.Text.Contains("재조회", StringComparison.Ordinal) && control.Text.Contains("새로고침", StringComparison.Ordinal));
                Check(explanations.Count == 1, "multiple waiting information rows expose the waiting meaning and retry guidance exactly once inside the actual list scroll");
                GUILayout.ControlRecord explanation = explanations[0];
                int firstStatus = list.Controls.FindIndex(control => control.Button && control.Text.Contains(first.Id, StringComparison.Ordinal));
                Check(list.Controls.IndexOf(explanation) < firstStatus && !explanation.Button
                    && list.Controls[firstStatus].Enabled == false,
                    "waiting guidance precedes the first disabled waiting row without becoming a selectable control");
                Check(explanation.VisibleRect.height >= explanation.Rect.height - .01f,
                    "waiting guidance remains readable within the information-list viewport without selecting a placeholder");
                if (allInformation)
                {
                    Check(model.Tab("미션").SelectedId == null && !model.Tab("미션").ExecutionFocus,
                        "all-information waiting guidance is shown with no selection or reservation focus");
                    Check(GUILayout.ScrollViews[1].Controls.Exists(control => !control.Button
                        && control.Text.Replace("\n", " ") == "현재 선택할 수 있는 항목이 없습니다. 목록의 안내를 확인하거나 새로고침하세요."),
                        "all-information detail guidance directs the user to readable list information or refresh rather than an impossible selection");
                }
                else
                    Check(GUILayout.ScrollViews[1].Controls.Exists(control => !control.Button && control.Text == explanation.Text),
                        "waiting guidance in the disabled footer retains the existing readable-detail explanation verbatim");
            }

            var nonWaitingModel = new LogModel();
            var nonWaiting = MissionRenderer(nonWaitingModel, new[] { StatusRow("healthy-info"), StatusRow("other-info") });
            Repaint(nonWaiting);
            Check(!GUILayout.ScrollViews[0].Controls.Exists(control => !control.Button && control.Text.Contains("미션 미완료·조건 미충족을 뜻하지 않습니다", StringComparison.Ordinal)),
                "waiting-specific guidance is not fabricated for information rows in other publication states");
        }
        finally { ConfigureLayout(saved, font); Screen.width = screenWidth; Screen.height = screenHeight; }
    }

    private static void AssertReservationStatus(LogRenderer renderer, LogModel model, string tab, bool enabled, string scenario)
    {
        Repaint(renderer);
        Check(GUILayout.LiteralLabels.Contains(tab == "조건" ? "조건 상세" : "상세") && !GUILayout.LiteralLabels.Contains("▶ 상세"), scenario + ": detail heading remains plain text");
        Check(!GUILayout.ScrollViews[1].Controls.Exists(control => control.Button), scenario + ": detail status and body contain no focusable controls");
        bool reserved = LogBridge.IsReserved(tab, model.Tab(tab).Selected?.Target);
        string expectedAction = enabled ? (reserved ? "예약 해제 가능" : "예약 가능") : "예약 변경 불가";
        Check(GUILayout.ScrollViews[1].Controls.Exists(control => !control.Button
            && control.Text.Contains(expectedAction, StringComparison.Ordinal)),
            scenario + ": noninteractive status exposes current reserve/release eligibility inside the detail body");
        Check(GUILayout.ScrollViews[1].Controls.Exists(control => !control.Button && control.Text.Contains(reserved ? "활성화 (예약 중)" : "비활성화 (예약 없음)", StringComparison.Ordinal)),
            scenario + ": noninteractive activation status reflects the actual bridge reservation rather than current selection");
        Check(!model.Tab(tab).ExecutionFocus && model.Tab(tab).BeginReservation() == null && Area(renderer) == "List",
            scenario + ": list owns navigation and no durable reservation authority remains");
    }

    private static ReservationTicket ArmModelTicket(LogTabState state)
    { state.ExecutionFocus = true; return state.BeginReservation(); }

    private static void RendererDirectListReservations()
    {
        int savedAttempts = LogBridge.Reservations;
        object savedTarget = LogBridge.LastTarget;
        bool savedResult = LogBridge.ReservationResult;
        try
        {
            foreach (string tab in new[] { "이벤트", "미션" })
            {
                int attempts = LogBridge.Reservations;
                object target = new object(); int closes = 0;
                var model = new LogModel(); model.ResetContext(700);
                Publish(model, tab, Item("focused-target", target), Item("second-target", new object()));
                var renderer = new LogRenderer(model, null, () => closes++);
                renderer.Handle(LogPadCommand.None);
                for (int index = 0; index < (tab == "조건" ? 1 : tab == "이벤트" ? 2 : 3); index++) renderer.Handle(LogPadCommand.NextTab);
                renderer.Handle(LogPadCommand.Activate);
                AssertReservationStatus(renderer, model, tab, true, tab + " first A only selects");
                Check(LogBridge.Reservations == attempts, "initial A never reserves an item that was not already selected");
                LogBridge.DuringReservation = () => Check(model.Tab(tab).ExecutionFocus && model.Tab(tab).BeginReservation() != null,
                    "the current selected-target authority exists at the actual bridge boundary");
                renderer.Handle(LogPadCommand.Activate); attempts++;
                LogBridge.DuringReservation = null;
                Check(LogBridge.Reservations == attempts && ReferenceEquals(LogBridge.LastTarget, target), "second fresh A directly reserves the selected target");
                AssertReservationStatus(renderer, model, tab, true, tab + " direct reservation releases authority");
                renderer.Handle(LogPadCommand.Down);
                Check(model.Tab(tab).SelectedId == "second-target" && !model.Tab(tab).ExecutionFocus, "down changes list selection immediately after reservation");
                renderer.Handle(LogPadCommand.Up);
                Check(model.Tab(tab).SelectedId == "focused-target", "up changes list selection immediately after reservation");
                foreach (LogPadCommand horizontal in new[] { LogPadCommand.Right, LogPadCommand.Left }) renderer.Handle(horizontal);
                AssertReservationStatus(renderer, model, tab, true, tab + " horizontal commands never focus details");
                Check(LogBridge.Reservations == attempts, "list directions do not reserve");
                ReservationTicket consumed = ArmModelTicket(model.Tab(tab));
                renderer.Handle(LogPadCommand.Activate); attempts++;
                Check(LogBridge.Reservations == attempts && ReferenceEquals(LogBridge.LastTarget, target) && !model.Tab(tab).CanActivate(consumed)
                    && !LogBridge.IsReserved(tab, target),
                    "direct action cancels older authority, releases the selected reservation once, and consumes its own authority");
                AssertReservationStatus(renderer, model, tab, true, tab + " repeated fresh A is a one-shot reservation");
                renderer.Handle(LogPadCommand.Back);
                Check(closes == 1 && model.Tab(tab).SelectedId == "focused-target" && LogBridge.Reservations == attempts,
                    "B from a selected/reserved item closes immediately without an intermediate detail stop");
                LogBridge.DuringReservation = () => throw new InvalidOperationException("synthetic reservation-boundary failure");
                bool failedAtBoundary = false;
                try { renderer.Handle(LogPadCommand.Activate); }
                catch (InvalidOperationException error) { failedAtBoundary = error.Message == "synthetic reservation-boundary failure"; }
                finally { LogBridge.DuringReservation = null; }
                attempts++;
                Check(failedAtBoundary && LogBridge.Reservations == attempts, "reservation-boundary exception is exercised after exactly one bridge attempt");
                AssertReservationStatus(renderer, model, tab, true, tab + " reservation exception still releases action authority");

                model.MarkPublicationFailure(tab);
                renderer.Handle(LogPadCommand.Activate);
                AssertReservationStatus(renderer, model, tab, false, tab + " failed publication blocks direct A");
                Check(LogBridge.Reservations == attempts && LogBridge.IsReserved(tab, target), "publication failure cannot forward confirmation or release an existing reservation");
                Publish(model, tab, Item("focused-target", target));
                AssertReservationStatus(renderer, model, tab, true, tab + " healthy publication only updates status");
                Check(LogBridge.Reservations == attempts, "recovery never reserves automatically");
                renderer.Handle(LogPadCommand.Tools); renderer.Handle(LogPadCommand.Back);
                AssertReservationStatus(renderer, model, tab, true, tab + " B from tools returns list");
                renderer.Handle(LogPadCommand.Tools);
                for (int index = 0; index < 15; index++) renderer.Handle(LogPadCommand.Down);
                renderer.Handle(LogPadCommand.Activate);
                AssertReservationStatus(renderer, model, tab, true, tab + " tools return control returns list");
                Check(LogBridge.Reservations == attempts, "returning from tools cannot reserve with its entry input");

                LogBridge.ReservationResult = false;
                renderer.Handle(LogPadCommand.Activate); attempts++;
                AssertReservationStatus(renderer, model, tab, true, tab + " rejected reservation releases authority");
                Check(LogBridge.Reservations == attempts, "a rejected reservation is attempted exactly once");
                LogBridge.ReservationResult = true;
                object replacement = new object();
                LogBridge.DuringReservation = () => Publish(model, tab, Item("focused-target", replacement));
                renderer.Handle(LogPadCommand.Activate); attempts++;
                LogBridge.DuringReservation = null;
                Repaint(renderer);
                Check(LogBridge.Reservations == attempts && model.Tab(tab).SelectedId == null && !model.Tab(tab).ExecutionFocus && Area(renderer) == "List",
                    "replacement during accepted reservation cannot select or arm the replacement");
                renderer.Handle(LogPadCommand.Activate);
                ReservationTicket stale = ArmModelTicket(model.Tab(tab));
                Publish(model, tab, Item("focused-target", new object()));
                renderer.Handle(LogPadCommand.Activate);
                Check(model.Tab(tab).SelectedId == null && LogBridge.Reservations == attempts, "same-ID object replacement consumes pending A before selecting a replacement");
                renderer.Handle(LogPadCommand.Activate);
                typeof(LogRenderer).GetMethod("Reserve", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(renderer, new object[] { stale });
                AssertReservationStatus(renderer, model, tab, true, tab + " stale ticket cannot acquire execution focus");
                Check(LogBridge.Reservations == attempts, "stale ticket is rejected before the game boundary");
                Publish(model, tab);
                renderer.Handle(LogPadCommand.Activate);
                Check(model.Tab(tab).SelectedId == null && !model.Tab(tab).ExecutionFocus && Area(renderer) == "List" && LogBridge.Reservations == attempts,
                    "target disappearance consumes pending confirmation without selecting another target");
                Publish(model, tab, Item("focused-target", new object()));
                renderer.Handle(LogPadCommand.Activate);
                ReservationTicket contextTicket = ArmModelTicket(model.Tab(tab));
                model.ResetContext(701); Publish(model, tab, Item("new-context-target", new object()));
                renderer.Handle(LogPadCommand.Activate);
                Check(model.Tab(tab).SelectedId == null && !model.Tab(tab).ExecutionFocus && Area(renderer) == "List" && LogBridge.Reservations == attempts,
                    "new context consumes old confirmation and reservation authority");
                typeof(LogRenderer).GetMethod("Reserve", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(renderer, new object[] { contextTicket });
                Check(LogBridge.Reservations == attempts, "context-stale ticket cannot reach game reservation boundary");

                foreach (string disabled in new[] { "waiting", "read-failure", "retained-failure", "no-target", "disallowed", "publication-failed" })
                {
                    object disabledTarget = disabled == "no-target" ? null : new object();
                    LogItem row = Item("disabled", disabledTarget);
                    if (disabled == "waiting") row.RefreshState = LogRefreshState.Waiting;
                    if (disabled == "read-failure") row.RefreshState = LogRefreshState.ReadFailure;
                    if (disabled == "retained-failure") row.RefreshState = LogRefreshState.RetainedFailure;
                    if (disabled == "disallowed") row.ReservationAllowed = false;
                    var disabledModel = new LogModel(); disabledModel.ResetContext(702); Publish(disabledModel, tab, row);
                    if (disabled == "publication-failed") disabledModel.MarkPublicationFailure(tab);
                    var disabledRenderer = new LogRenderer(disabledModel, null, null);
                    disabledRenderer.Handle(LogPadCommand.None);
                    for (int index = 0; index < (tab == "조건" ? 1 : tab == "이벤트" ? 2 : 3); index++) disabledRenderer.Handle(LogPadCommand.NextTab);
                    disabledRenderer.Handle(LogPadCommand.Activate); disabledRenderer.Handle(LogPadCommand.Activate);
                    disabledRenderer.Handle(LogPadCommand.Right); disabledRenderer.Handle(LogPadCommand.Left);
                    AssertReservationStatus(disabledRenderer, disabledModel, tab, false, tab + " " + disabled + " direct action refused");
                    Check(LogBridge.Reservations == attempts, "disabled selected target never reaches the game reservation boundary");
                    object healthyTarget = disabledTarget ?? new object();
                    Publish(disabledModel, tab, Item("disabled", healthyTarget)); Repaint(disabledRenderer);
                    Check(Area(disabledRenderer) == "List" && !disabledModel.Tab(tab).ExecutionFocus && LogBridge.Reservations == attempts,
                        "healthy refresh updates status without selecting, arming or reserving automatically");
                    if (disabledModel.Tab(tab).SelectedId == null) disabledRenderer.Handle(LogPadCommand.Activate);
                    disabledRenderer.Handle(LogPadCommand.Right);
                    AssertReservationStatus(disabledRenderer, disabledModel, tab, true, tab + " " + disabled + " healthy recovery");
                    Check(LogBridge.Reservations == attempts, "horizontal input after recovery never confirms a reservation");
                    disabledRenderer.Handle(LogPadCommand.Activate); attempts++;
                    Check(LogBridge.Reservations == attempts && ReferenceEquals(LogBridge.LastTarget, healthyTarget) && !disabledModel.Tab(tab).ExecutionFocus,
                        "a new explicit A after recovery reserves once then releases authority");
                }

                var keyModel = new LogModel(); keyModel.ResetContext(703); Publish(keyModel, tab, Item("key-reservation", new object()));
                var keyRenderer = new LogRenderer(keyModel, null, null); keyRenderer.Handle(LogPadCommand.None);
                for (int index = 0; index < (tab == "조건" ? 1 : tab == "이벤트" ? 2 : 3); index++) keyRenderer.Handle(LogPadCommand.NextTab);
                PressKey(keyRenderer, KeyCode.Return); PressKey(keyRenderer, KeyCode.Return);
                Check(LogBridge.Reservations == attempts && keyModel.Tab(tab).SelectedId == "key-reservation", "held first Enter selects only once");
                ReleaseKey(keyRenderer, KeyCode.Return);
                PressKey(keyRenderer, KeyCode.Return); attempts++;
                AssertReservationStatus(keyRenderer, keyModel, tab, true, tab + " fresh Enter directly reserves");
                PressKey(keyRenderer, KeyCode.Return);
                Check(LogBridge.Reservations == attempts, "held Enter cannot repeat a direct reservation");
                ReleaseKey(keyRenderer, KeyCode.Return);
                PressKey(keyRenderer, KeyCode.Space); attempts++; PressKey(keyRenderer, KeyCode.Space);
                Check(LogBridge.Reservations == attempts, "fresh Space toggles the reservation once while held repeat is suppressed");
                ReleaseKey(keyRenderer, KeyCode.Space);
                PressKey(keyRenderer, KeyCode.Return); attempts++;
                keyModel.ResetContext(704); Publish(keyModel, tab, Item("held-context-replacement", new object()));
                keyRenderer.ResetInputFocus(); keyRenderer.Handle(LogPadCommand.None); PressKey(keyRenderer, KeyCode.Return);
                Check(keyModel.Tab(tab).SelectedId == null && LogBridge.Reservations == attempts, "held Enter cannot select or reserve after context reset");
                ReleaseKey(keyRenderer, KeyCode.Return); TapKey(keyRenderer, KeyCode.Return);
                Check(keyModel.Tab(tab).SelectedId == "held-context-replacement" && Area(keyRenderer) == "List" && LogBridge.Reservations == attempts,
                    "fresh Enter after context reset first selects and does not reserve");
            }
        }
        finally
        {
            Input.Held.Clear(); LogBridge.Reservations = savedAttempts; LogBridge.LastTarget = savedTarget;
            LogBridge.ReservationResult = savedResult; LogBridge.DuringReservation = null;
        }
    }

    private static void AssertRowReservation(LogRenderer renderer, LogModel model, string tab, string id, bool reserved, string scenario)
    {
        Repaint(renderer);
        GUILayout.ControlRecord row = GUILayout.ScrollViews[0].Controls.Find(control => control.Button && control.Text.Contains(id, StringComparison.Ordinal));
        Check(row != null && row.Text.Contains("예약 중", StringComparison.Ordinal) == reserved,
            scenario + ": list row exposes current reservation state independently of its selection");
        Check(row.Text.StartsWith("▶ ", StringComparison.Ordinal) == (model.Tab(tab).SelectedId == id),
            scenario + ": the selection marker remains independent of the reservation badge");
    }

    private static void RendererReservationToggleState()
    {
        int savedAttempts = LogBridge.Reservations; object savedTarget = LogBridge.LastTarget;
        bool savedReadable = LogBridge.ReservationReadable, savedResult = LogBridge.ReservationResult;
        object oldCondition = LogBridge.CurrentReservation("조건"), oldEvent = LogBridge.CurrentReservation("이벤트"), oldMission = LogBridge.CurrentReservation("미션");
        try
        {
            foreach (string tab in new[] { "이벤트", "미션" })
            {
                LogBridge.SetReservation(tab, null);
                object firstTarget = new object(), secondTarget = new object();
                var model = new LogModel();
                var renderer = PointerReservationRenderer(model, tab, firstTarget);
                Publish(model, tab, Item("pointer-target", firstTarget), Item("toggle-second", secondTarget));
                int attempts = LogBridge.Reservations;
                AssertRowReservation(renderer, model, tab, "pointer-target", false, tab + " selected with no reservation");
                renderer.Handle(LogPadCommand.Activate);
                Check(LogBridge.Reservations == ++attempts && LogBridge.IsReserved(tab, firstTarget) && !model.Tab(tab).ExecutionFocus,
                    "fresh A reserves the selected item once");
                AssertRowReservation(renderer, model, tab, "pointer-target", true, tab + " first A reserved");
                renderer.Handle(LogPadCommand.Down);
                AssertRowReservation(renderer, model, tab, "pointer-target", true, tab + " reservation remains on the unselected first row");
                AssertRowReservation(renderer, model, tab, "toggle-second", false, tab + " second row is selected but not reserved");
                AssertReservationStatus(renderer, model, tab, true, tab + " unreserved second row detail");
                Check(LogBridge.Reservations == attempts && LogBridge.IsReserved(tab, firstTarget),
                    "selection changes never mutate the current reservation");
                renderer.Handle(LogPadCommand.Activate);
                Check(LogBridge.Reservations == ++attempts && LogBridge.IsReserved(tab, secondTarget) && !LogBridge.IsReserved(tab, firstTarget),
                    "A on another selected row replaces only this tab's reservation");
                AssertRowReservation(renderer, model, tab, "pointer-target", false, tab + " old reservation replaced");
                AssertRowReservation(renderer, model, tab, "toggle-second", true, tab + " replacement reservation shown");
                renderer.Handle(LogPadCommand.Activate);
                Check(LogBridge.Reservations == ++attempts && LogBridge.CurrentReservation(tab) == null && model.Tab(tab).SelectedId == "toggle-second",
                    "fresh A on the reserved row releases the reservation while keeping list selection");
                AssertRowReservation(renderer, model, tab, "toggle-second", false, tab + " A release badge removed");

                foreach (KeyCode key in new[] { KeyCode.Return, KeyCode.Space, KeyCode.KeypadEnter })
                {
                    PressKey(renderer, key); attempts++;
                    Check(LogBridge.Reservations == attempts && LogBridge.IsReserved(tab, secondTarget), "fresh keyboard confirm reserves the selected row");
                    PressKey(renderer, key);
                    Check(LogBridge.Reservations == attempts && LogBridge.IsReserved(tab, secondTarget), "held keyboard repeat cannot immediately release a new reservation");
                    ReleaseKey(renderer, key); PressKey(renderer, key); attempts++;
                    Check(LogBridge.Reservations == attempts && LogBridge.CurrentReservation(tab) == null,
                        "a new keyboard press releases the selected reservation exactly once");
                    PressKey(renderer, key);
                    Check(LogBridge.Reservations == attempts && LogBridge.CurrentReservation(tab) == null,
                        "held release-confirm cannot reserve again");
                    ReleaseKey(renderer, key);
                }
                foreach (bool touch in new[] { false, true })
                {
                    EventType down = touch ? EventType.TouchDown : EventType.MouseDown, up = touch ? EventType.TouchUp : EventType.MouseUp;
                    Func<string, bool> matches = text => text.Contains("toggle-second", StringComparison.Ordinal);
                    Vector2 point = PointerTarget(renderer, control => control.Button && matches(control.Text));
                    foreach (bool reservedAfter in new[] { true, false })
                    {
                        DrawPointer(renderer, down, down, point);
                        Check(Field(renderer, "pointerReservation") != null, "selected row press captures one immutable reservation-state snapshot");
                        SoftResetForPointer(renderer);
                        DrawPointer(renderer, up, up, point, matches, consume: true); attempts++;
                        Check(LogBridge.Reservations == attempts && LogBridge.IsReserved(tab, secondTarget) == reservedAfter && !model.Tab(tab).ExecutionFocus,
                            "a new accepted selected-row click/touch toggles once using its original press snapshot");
                        DrawPointer(renderer, up, up, point, matches, consume: true);
                        Check(LogBridge.Reservations == attempts && LogBridge.IsReserved(tab, secondTarget) == reservedAfter,
                            "repeated mouse/touch release cannot toggle again");
                    }
                }

                LogBridge.SetReservation(tab, secondTarget);
                model.Tab(tab).ClearSelection(); renderer.ResetInputFocus(); renderer.Handle(LogPadCommand.None);
                renderer.Handle(LogPadCommand.Activate);
                Check(LogBridge.Reservations == attempts && model.Tab(tab).SelectedId == "pointer-target" && LogBridge.IsReserved(tab, secondTarget),
                    "first A with no selection selects the first readable row and leaves an existing reservation untouched");
                AssertRowReservation(renderer, model, tab, "toggle-second", true, tab + " existing unselected reservation retained");
                LogBridge.SetReservation(tab, null); Repaint(renderer);
                AssertRowReservation(renderer, model, tab, "toggle-second", false, tab + " game consumer cleared reservation");
                Check(LogBridge.Reservations == attempts && model.Tab(tab).SelectedId == "pointer-target",
                    "consumer changes update the badge without moving selection or firing an action");
                LogBridge.SetReservation(tab, firstTarget); LogBridge.ReservationReadable = false;
                renderer.Handle(LogPadCommand.Activate);
                Check(LogBridge.Reservations == attempts && ReferenceEquals(LogBridge.CurrentReservation(tab), firstTarget) && !model.Tab(tab).ExecutionFocus,
                    "unreadable reservation state refuses both reserve and release without guessing intent");
                foreach (bool touch in new[] { false, true })
                {
                    Func<string, bool> first = text => text.Contains("pointer-target", StringComparison.Ordinal);
                    Vector2 point = PointerTarget(renderer, control => control.Button && first(control.Text));
                    EventType down = touch ? EventType.TouchDown : EventType.MouseDown, up = touch ? EventType.TouchUp : EventType.MouseUp;
                    DrawPointer(renderer, down, down, point);
                    Check(Field(renderer, "pointerReservation") == null, "pointer press refuses to guess a toggle intent while reservation capture is unavailable");
                    LogBridge.ReservationReadable = true;
                    DrawPointer(renderer, up, up, point, first, consume: true);
                    Check(LogBridge.Reservations == attempts && ReferenceEquals(LogBridge.CurrentReservation(tab), firstTarget),
                        "query recovery before release cannot create a ticket that was unavailable on pointer-down");
                    LogBridge.ReservationReadable = false;
                }
                LogBridge.ReservationReadable = true;

                foreach (bool touch in new[] { false, true })
                    foreach (string change in new[] { "consume", "replace", "observed-same-target-write", "consume-and-reinsert-same-target", "became-selected-reservation", "query-failure", "observed-query-failure-recovered" })
                    {
                        LogBridge.SetReservation(tab, change == "became-selected-reservation" ? null : firstTarget);
                        Func<string, bool> first = text => text.Contains("pointer-target", StringComparison.Ordinal);
                        Vector2 point = PointerTarget(renderer, control => control.Button && first(control.Text));
                        EventType down = touch ? EventType.TouchDown : EventType.MouseDown, up = touch ? EventType.TouchUp : EventType.MouseUp;
                        DrawPointer(renderer, down, down, point);
                        object snapshotTicket = Field(renderer, "pointerReservation");
                        Check(snapshotTicket != null, "pointer transaction begins with current selected-target authority and reservation snapshot");
                        object afterChange = change == "consume" ? null : change == "replace" ? secondTarget : firstTarget;
                        if (change == "observed-query-failure-recovered")
                        {
                            LogBridge.ReservationReadable = false;
                            if (touch)
                            {
                                object failedSnapshot;
                                Check(!LogBridge.TryCaptureReservation(tab, out failedSnapshot) && failedSnapshot == null,
                                    "an intermediate failed capture invalidates older successful reservation snapshots");
                            }
                            else
                            {
                                Repaint(renderer);
                                Check(!LogBridge.IsReserved(tab, firstTarget), "a failed IsReserved draw observation cannot claim the slot is reserved");
                            }
                            LogBridge.ReservationReadable = true;
                        }
                        else if (change == "query-failure") LogBridge.ReservationReadable = false;
                        else
                        {
                            if (change == "consume-and-reinsert-same-target") LogBridge.SetReservation(tab, null);
                            LogBridge.SetReservation(tab, afterChange);
                        }
                        SoftResetForPointer(renderer); Repaint(renderer);
                        DrawPointer(renderer, up, up, point, first, consume: true); attempts++;
                        Check(LogBridge.Reservations == attempts && ReferenceEquals(LogBridge.CurrentReservation(tab), afterChange) && !model.Tab(tab).ExecutionFocus,
                            "stale pointer snapshot is refused after " + change + "; release never reverses its captured reserve/release intent");
                        DrawPointer(renderer, up, up, point, first, consume: true);
                        Check(LogBridge.Reservations == attempts && ReferenceEquals(LogBridge.CurrentReservation(tab), afterChange),
                            "failed stale release cannot be retried without a new press");
                        LogBridge.ReservationReadable = true;
                        renderer.Handle(LogPadCommand.Activate); attempts++;
                        Check(LogBridge.Reservations == attempts && (ReferenceEquals(afterChange, firstTarget)
                            ? LogBridge.CurrentReservation(tab) == null : LogBridge.IsReserved(tab, firstTarget)),
                            "a later fresh A captures the new reservation state and performs exactly the currently intended operation");
                    }
            }

            object eventTarget = new object(), missionTarget = new object();
            LogBridge.SetReservation("이벤트", null); LogBridge.SetReservation("미션", null);
            var eventModel = new LogModel(); var eventRenderer = PointerReservationRenderer(eventModel, "이벤트", eventTarget);
            var missionModel = new LogModel(); var missionRenderer = PointerReservationRenderer(missionModel, "미션", missionTarget);
            eventRenderer.Handle(LogPadCommand.Activate); missionRenderer.Handle(LogPadCommand.Activate);
            Check(LogBridge.IsReserved("이벤트", eventTarget) && LogBridge.IsReserved("미션", missionTarget),
                "event confirmation reserves its own result and leaves the independent mission reservation intact");
            eventRenderer.Handle(LogPadCommand.Activate);
            Check(LogBridge.CurrentReservation("이벤트") == null && LogBridge.IsReserved("미션", missionTarget),
                "releasing an event reservation cannot clear the independent mission slot");
            missionRenderer.Handle(LogPadCommand.Activate);
            Check(LogBridge.CurrentReservation("미션") == null, "mission reservation still toggles off");

        }
        finally
        {
            LogBridge.SetReservation("조건", oldCondition); LogBridge.SetReservation("이벤트", oldEvent); LogBridge.SetReservation("미션", oldMission);
            LogBridge.Reservations = savedAttempts; LogBridge.LastTarget = savedTarget;
            LogBridge.ReservationReadable = savedReadable; LogBridge.ReservationResult = savedResult; LogBridge.DuringReservation = null;
            GUILayout.ButtonInput = null; GUILayout.ConsumeButtonEvent = false; Input.Held.Clear();
        }
    }

    private static void RendererListChromePointerTransactions()
    {
        int attempts = LogBridge.Reservations;
        try
        {
            foreach (string tab in new[] { "이벤트", "미션" })
            {
                foreach (string action in new[] { "close", "tab", "tools" })
                {
                    var model = new LogModel(); model.ResetContext(710);
                    Publish(model, tab, Item("mouse-target", new object()));
                    int closes = 0;
                    var renderer = new LogRenderer(model, null, () => closes++);
                    renderer.Handle(LogPadCommand.None);
                    for (int index = 0; index < (tab == "조건" ? 1 : tab == "이벤트" ? 2 : 3); index++) renderer.Handle(LogPadCommand.NextTab);
                    renderer.Handle(LogPadCommand.Activate);
                    renderer.Handle(LogPadCommand.Right);
                    AssertReservationStatus(renderer, model, tab, true, tab + " before mouse " + action);
                    ClickRenderedButton(renderer, action == "close" ? "닫기" : action == "tab" ? "선택지" : "도구", action != "tools");
                    Check(action == "close" ? closes == 1 : action == "tab" ? renderer.CurrentTab == "선택지" && Area(renderer) == "List" : Area(renderer) == "Tools",
                        tab + ": list reservation authority cancellation preserves mouse down/up transaction for " + action);
                    Check(LogBridge.Reservations == attempts, "chrome mouse action is never a reservation confirmation");
                }

            }
        }
        finally
        {
            GUILayout.ButtonInput = null;
            Input.Held.Clear();
        }
    }

    private static void CursorOwnershipLifecycle()
    {
        CursorLockMode savedLock = Cursor.lockState; bool savedVisible = Cursor.visible;
        try
        {
            foreach (LogInputMode mode in new[] { LogInputMode.Gamepad, LogInputMode.KeyboardMouse, LogInputMode.Unknown })
                foreach (CursorLockMode initialLock in new[] { CursorLockMode.Locked, CursorLockMode.Confined, CursorLockMode.None })
                    foreach (bool visible in new[] { false, true })
                    {
                        Cursor.lockState = initialLock; Cursor.visible = visible;
                        var scope = new LogCursorScope();
                        scope.SetActive(false, mode);
                        Check(Cursor.lockState == initialLock && Cursor.visible == visible, "inactive log does not own or rewrite the game's cursor");
                        scope.SetActive(true, mode);
                        Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "focused log unlocks pointer access independently of input mode");
                        for (int frame = 0; frame < 3; frame++) scope.SetActive(true, mode);
                        scope.SetActive(false, mode);
                        Check(Cursor.lockState == initialLock && Cursor.visible == visible, "close/focus loss restores the exact initial cursor including custom Confined/visibility");
                        scope.Release();
                        Check(Cursor.lockState == initialLock && Cursor.visible == visible, "release after close is idempotent");
                    }

            foreach (LogInputMode finalMode in new[] { LogInputMode.KeyboardMouse, LogInputMode.Gamepad })
            {
                LogInputMode firstMode = finalMode == LogInputMode.Gamepad ? LogInputMode.KeyboardMouse : LogInputMode.Gamepad;
                Cursor.lockState = CursorLockMode.Confined; Cursor.visible = false;
                var scope = new LogCursorScope();
                scope.SetActive(true, firstMode);
                scope.SetActive(true, LogInputMode.Unknown);
                scope.SetActive(true, finalMode);
                Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "known mode changes keep the focused log accessible");
                scope.Release();
                Check(Cursor.lockState == (finalMode == LogInputMode.Gamepad ? CursorLockMode.Locked : CursorLockMode.None)
                    && Cursor.visible == (finalMode == LogInputMode.KeyboardMouse), "known mode transition through unknown restores the final game's cursor policy");
            }

            foreach (LogInputMode recovered in new[] { LogInputMode.KeyboardMouse, LogInputMode.Gamepad })
            {
                Cursor.lockState = CursorLockMode.Confined; Cursor.visible = false;
                var scope = new LogCursorScope();
                scope.SetActive(true, LogInputMode.Unknown);
                scope.SetActive(true, recovered);
                scope.Release();
                Check(Cursor.lockState == CursorLockMode.Confined && !Cursor.visible,
                    "initially unknown mode becoming known preserves the original snapshot without inventing a mode transition");
            }

            foreach (bool finalWrite in new[] { false, true })
            {
                Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
                var scope = new LogCursorScope();
                scope.SetActive(true, LogInputMode.Gamepad);
                Cursor.lockState = CursorLockMode.Confined; Cursor.visible = true;
                if (!finalWrite)
                {
                    scope.SetActive(true, LogInputMode.Gamepad);
                    Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "game relock during log ownership is observed and access reapplied");
                    scope.SetActive(true, LogInputMode.Unknown);
                }
                scope.Release();
                Check(Cursor.lockState == CursorLockMode.Confined && Cursor.visible,
                    finalWrite ? "destruction release observes a final external cursor write" : "repeated active frames preserve the latest external cursor restoration target");
            }

            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            var reopened = new LogCursorScope();
            reopened.SetActive(true, LogInputMode.Gamepad); reopened.SetActive(false, LogInputMode.Gamepad);
            Cursor.lockState = CursorLockMode.Confined; Cursor.visible = true;
            reopened.SetActive(true, LogInputMode.Gamepad); reopened.Release();
            Check(Cursor.lockState == CursorLockMode.Confined && Cursor.visible, "reopening snapshots the current cursor rather than retaining an older ownership lifetime");

            Cursor.lockState = CursorLockMode.Confined; Cursor.visible = false;
            var closingModeChange = new LogCursorScope();
            closingModeChange.SetActive(true, LogInputMode.Gamepad);
            closingModeChange.SetActive(false, LogInputMode.KeyboardMouse);
            Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "mode change observed on the closing/focus-loss frame determines restoration");

            foreach (bool activeWhenChanged in new[] { false, true })
                foreach (bool externalVisible in new[] { false, true })
                {
                    Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
                    var simultaneous = new LogCursorScope();
                    simultaneous.SetActive(true, LogInputMode.Gamepad);
                    Cursor.lockState = CursorLockMode.Confined; Cursor.visible = externalVisible;
                    simultaneous.SetActive(activeWhenChanged, LogInputMode.KeyboardMouse);
                    if (activeWhenChanged)
                    {
                        Check(Cursor.lockState == CursorLockMode.None && Cursor.visible,
                            "explicit external cursor request and mode change still leave the active log accessible");
                        simultaneous.SetActive(true, LogInputMode.KeyboardMouse);
                        simultaneous.Release();
                    }
                    Check(Cursor.lockState == CursorLockMode.Confined && Cursor.visible == externalVisible,
                        "observed external Confined/visibility request outranks inferred mode defaults even on the closing tick");
                }
        }
        finally { Cursor.lockState = savedLock; Cursor.visible = savedVisible; }
    }

    private static void SoftResetForPointer(LogRenderer renderer)
    {
        // The fallback reproduces the old unconditional reset while this test
        // is introduced; the source API now takes preservePointerTransaction.
        MethodInfo reset = typeof(LogRenderer).GetMethod("ResetInputFocus");
        reset.Invoke(renderer, reset.GetParameters().Length == 0 ? null :
            reset.GetParameters().Length == 1 ? new object[] { true } : new object[] { true, false });
    }

    private static Vector2 PointerTarget(LogRenderer renderer, Func<GUILayout.ControlRecord, bool> matches)
    {
        Repaint(renderer);
        GUILayout.ControlRecord control = GUILayout.Controls.Find(control => matches(control));
        Check(control != null && control.VisibleRect.height > 0, "pointer transaction starts on a visible control");
        return new Vector2(control.WindowLocalRect.x + 5, control.WindowLocalRect.y + 5);
    }

    private static void DrawPointer(LogRenderer renderer, EventType type, EventType raw, Vector2 point,
        Func<string, bool> pressed = null, bool consume = false)
    {
        try
        {
            GUILayout.ButtonInput = pressed; GUILayout.ConsumeButtonEvent = consume;
            Event.current = new Event { type = type, rawType = raw, button = 0, mousePosition = point };
            renderer.Draw();
        }
        finally { GUILayout.ButtonInput = null; GUILayout.ConsumeButtonEvent = false; }
    }

    private static LogRenderer PointerReservationRenderer(LogModel model, string tab, object target, Action close = null)
    {
        model.ResetContext(730);
        Publish(model, tab, Item("pointer-target", target));
        var renderer = new LogRenderer(model, null, close);
        renderer.Handle(LogPadCommand.None);
        for (int index = 0; index < (tab == "조건" ? 1 : tab == "이벤트" ? 2 : 3); index++) renderer.Handle(LogPadCommand.NextTab);
        renderer.Handle(LogPadCommand.Activate); renderer.Handle(LogPadCommand.Right);
        return renderer;
    }

    private static void RendererListPointerSelectionTransactions()
    {
        int savedAttempts = LogBridge.Reservations; object savedTarget = LogBridge.LastTarget;
        try
        {
            foreach (string tab in new[] { "이벤트", "미션" })
                foreach (bool touch in new[] { false, true })
                {
                    object firstTarget = new object(), secondTarget = new object();
                    var model = new LogModel(); var renderer = PointerReservationRenderer(model, tab, firstTarget);
                    Publish(model, tab, Item("pointer-target", firstTarget), Item("second-pointer-row", secondTarget));
                    model.Tab(tab).ClearSelection(); renderer.ResetInputFocus(); renderer.Handle(LogPadCommand.None);
                    Func<string, bool> first = text => text.Contains("pointer-target", StringComparison.Ordinal);
                    Func<string, bool> second = text => text.Contains("second-pointer-row", StringComparison.Ordinal);
                    EventType down = touch ? EventType.TouchDown : EventType.MouseDown, up = touch ? EventType.TouchUp : EventType.MouseUp;
                    Vector2 point = PointerTarget(renderer, control => control.Button && first(control.Text));
                    int attempts = LogBridge.Reservations;
                    DrawPointer(renderer, down, down, point);
                    Check(Field(renderer, "pointerIdentity") != null && Field(renderer, "pointerReservation") == null,
                        "a press on an unselected row has a selection identity but no reservation authority");
                    DrawPointer(renderer, up, up, point, first, consume: true);
                    Check(model.Tab(tab).SelectedId == "pointer-target" && LogBridge.Reservations == attempts && !model.Tab(tab).ExecutionFocus,
                        "first accepted click/touch only selects and shows details");
                    DrawPointer(renderer, up, up, point, first, consume: true);
                    Check(LogBridge.Reservations == attempts, "a duplicate first-selection release cannot acquire a reservation ticket");
                    DrawPointer(renderer, down, down, point);
                    ReservationTicket pointer = (ReservationTicket)Field(renderer, "pointerReservation");
                    Check(pointer != null && model.Tab(tab).CanActivate(pointer), "a new press on the already selected healthy row grants one current-target ticket");
                    DrawPointer(renderer, up, up, point, first, consume: true);
                    Check(LogBridge.Reservations == attempts + 1 && ReferenceEquals(LogBridge.LastTarget, firstTarget) && !model.Tab(tab).CanActivate(pointer)
                        && !model.Tab(tab).ExecutionFocus, "second accepted click/touch reserves once and consumes all action authority");
                    point = PointerTarget(renderer, control => control.Button && second(control.Text));
                    DrawPointer(renderer, down, down, point);
                    Check(Field(renderer, "pointerReservation") == null, "pressing a different row never arms the previously selected target");
                    SoftResetForPointer(renderer);
                    DrawPointer(renderer, up, up, point, second, consume: true);
                    Check(model.Tab(tab).SelectedId == "second-pointer-row" && LogBridge.Reservations == attempts + 1,
                        "clicking another row remains selection-only through a pointer takeover reset");
                    Vector2 statusPoint = PointerTarget(renderer, control => !control.Button && control.Text.Contains("비활성화 (예약 없음)", StringComparison.Ordinal));
                    DrawPointer(renderer, down, down, statusPoint);
                    DrawPointer(renderer, up, up, statusPoint, text => text.Contains("비활성화 (예약 없음)", StringComparison.Ordinal), consume: true);
                    Check(LogBridge.Reservations == attempts + 1 && Field(renderer, "pointerIdentity") == null && !model.Tab(tab).ExecutionFocus,
                        "clicking/touching the detail activation status cannot select, arm or reserve anything");
                    // A forged/synthetic release without an accepted selected-row
                    // press cannot mint authority from the current selection.
                    DrawPointer(renderer, up, up, point, second, consume: true);
                    Check(LogBridge.Reservations == attempts + 1 && Field(renderer, "pointerReservation") == null,
                        "release cannot create reservation authority for an already selected row");
                    DrawPointer(renderer, down, down, point);
                    renderer.Handle(LogPadCommand.Up);
                    DrawPointer(renderer, up, up, point, second, consume: true);
                    Check(model.Tab(tab).SelectedId == "pointer-target" && LogBridge.Reservations == attempts + 1,
                        "a fresh pad navigation command abandons the in-flight pointer reservation and its release");
                }
        }
        finally
        {
            GUILayout.ButtonInput = null; GUILayout.ConsumeButtonEvent = false;
            LogBridge.Reservations = savedAttempts; LogBridge.LastTarget = savedTarget; Input.Held.Clear();
        }
    }

    private static void RendererPointerTakeoverTransactions()
    {
        int savedAttempts = LogBridge.Reservations; object savedTarget = LogBridge.LastTarget;
        int pointerActivities = 0;
        try
        {
            GamepadBridge.PointerActivity = () => pointerActivities++;
            foreach (string tab in new[] { "이벤트", "미션" })
            {
                Func<string, bool> reserve = text => text.Contains("pointer-target", StringComparison.Ordinal);
                foreach (int kind in new[] { 0, 1, 2, 3 })
                {
                    EventType down = kind == 1 || kind == 3 ? EventType.TouchDown : EventType.MouseDown;
                    EventType up = kind == 1 || kind == 3 ? EventType.TouchUp : EventType.MouseUp;
                    EventType rawDown = kind == 0 || kind == 3 ? EventType.MouseDown : EventType.TouchDown;
                    EventType rawUp = kind == 0 || kind == 3 ? EventType.MouseUp : EventType.TouchUp;
                    object target = new object(); var model = new LogModel();
                    var renderer = PointerReservationRenderer(model, tab, target);
                    LogTabState state = model.Tab(tab);
                    ReservationTicket padTicket = ArmModelTicket(state);
                    Vector2 point = PointerTarget(renderer, control => control.Button && reserve(control.Text));
                    int attempts = LogBridge.Reservations, notifications = pointerActivities;
                    DrawPointer(renderer, down, rawDown, point);
                    object pointerTicket = Field(renderer, "pointerReservation");
                    Check(pointerTicket != null && Field(renderer, "pointerIdentity") != null && !state.CanActivate(padTicket),
                        "pointer down owns a new transaction and invalidates the prior pad reservation authority");
                    SoftResetForPointer(renderer);
                    Check(ReferenceEquals(pointerTicket, Field(renderer, "pointerReservation")) && Field(renderer, "pointerIdentity") != null,
                        "mode-only/pointer takeover reset preserves the original pointer ticket without reissuing stale authority");
                    Repaint(renderer);
                    DrawPointer(renderer, up, rawUp, point, reserve, consume: true);
                    Check(LogBridge.Reservations == attempts + 1 && ReferenceEquals(LogBridge.LastTarget, target),
                        tab + " mouse/touch down → Update soft reset → GUI-consumed release reserves the original target once");
                    Check(pointerActivities > notifications && Field(renderer, "pointerIdentity") == null && Field(renderer, "pointerReservation") == null,
                        "pointer transaction notifies ownership and clears its identity/ticket after successful release");
                    DrawPointer(renderer, up, rawUp, point, reserve, consume: true);
                    Check(LogBridge.Reservations == attempts + 1, "repeated release cannot reuse a consumed pointer reservation");

                    foreach (string action in new[] { "close", "tab", "tools" })
                    {
                        int closes = 0; var chromeModel = new LogModel();
                        var chrome = PointerReservationRenderer(chromeModel, tab, new object(), () => closes++);
                        Func<string, bool> matches = text => action == "close" ? text == "닫기" : action == "tab" ? text == "선택지" : text.Contains("도구", StringComparison.Ordinal);
                        Vector2 chromePoint = PointerTarget(chrome, control => control.Button && matches(control.Text));
                        attempts = LogBridge.Reservations;
                        DrawPointer(chrome, down, rawDown, chromePoint);
                        SoftResetForPointer(chrome);
                        DrawPointer(chrome, up, rawUp, chromePoint, matches, consume: true);
                        Check(action == "close" ? closes == 1 : action == "tab" ? chrome.CurrentTab == "선택지" : Area(chrome) == "Tools",
                            "pointer takeover preserves mouse/touch chrome transaction for " + action);
                        Check(LogBridge.Reservations == attempts, "chrome pointer release never confirms a reservation");
                    }

                }

                foreach (string invalidation in new[] { "cancel", "context", "replacement", "publication", "hard", "selection", "disabled" })
                {
                    object target = new object(); var model = new LogModel();
                    var renderer = PointerReservationRenderer(model, tab, target);
                    Vector2 point = PointerTarget(renderer, control => control.Button && reserve(control.Text));
                    int attempts = LogBridge.Reservations;
                    DrawPointer(renderer, EventType.TouchDown, EventType.TouchDown, point);
                    SoftResetForPointer(renderer);
                    if (invalidation == "cancel") DrawPointer(renderer, EventType.TouchLeave, EventType.TouchLeave, point);
                    else if (invalidation == "context") { model.ResetContext(731); Publish(model, tab, Item("pointer-target", new object())); }
                    else if (invalidation == "replacement") Publish(model, tab, Item("pointer-target", new object()));
                    else if (invalidation == "publication") model.MarkPublicationFailure(tab);
                    else if (invalidation == "hard") renderer.ResetInputFocus();
                    else if (invalidation == "selection") { Publish(model, tab, Item("pointer-target", target), Item("other", new object())); model.Tab(tab).Select("other"); }
                    else Publish(model, tab, new LogItem { Id = "pointer-target", Target = target, RefreshState = LogRefreshState.Waiting });
                    Repaint(renderer);
                    DrawPointer(renderer, EventType.TouchUp, EventType.TouchUp, point, reserve, consume: true);
                    Check(LogBridge.Reservations == attempts && Field(renderer, "pointerIdentity") == null && Field(renderer, "pointerReservation") == null,
                        "touch release rejects and clears stale transaction after " + invalidation);
                }

                var updated = new LogModel(); object sameTarget = new object();
                var updateRenderer = PointerReservationRenderer(updated, tab, sameTarget);
                Vector2 updatePoint = PointerTarget(updateRenderer, control => control.Button && reserve(control.Text));
                int before = LogBridge.Reservations;
                DrawPointer(updateRenderer, EventType.TouchDown, EventType.TouchDown, updatePoint);
                Publish(updated, tab, Item("pointer-target", sameTarget));
                SoftResetForPointer(updateRenderer);
                DrawPointer(updateRenderer, EventType.TouchUp, EventType.TouchUp, updatePoint, reserve, consume: true);
                Check(LogBridge.Reservations == before + 1 && ReferenceEquals(LogBridge.LastTarget, sameTarget),
                    "healthy same-target automatic publication preserves the current pointer transaction");

                var rejected = new LogModel(); var rejectedRenderer = PointerReservationRenderer(rejected, tab, new object());
                Vector2 rejectedPoint = PointerTarget(rejectedRenderer, control => control.Button && reserve(control.Text));
                int rejectedAttempts = LogBridge.Reservations;
                DrawPointer(rejectedRenderer, EventType.TouchDown, EventType.TouchDown, rejectedPoint);
                SoftResetForPointer(rejectedRenderer);
                // The renderer now adapts direct touch into native mouse
                // phases. Explicitly reject this boundary release instead of
                // relying on native Button's lack of untranslated touch input.
                DrawPointer(rejectedRenderer, EventType.TouchUp, EventType.TouchUp, rejectedPoint, _ => false);
                DrawPointer(rejectedRenderer, EventType.TouchUp, EventType.TouchUp, rejectedPoint, reserve, consume: true);
                Check(LogBridge.Reservations == rejectedAttempts && Field(rejectedRenderer, "pointerIdentity") == null,
                    "touch events alone do not manufacture success when Unity's button boundary did not accept the release");

                var padOnly = new LogModel(); var padRenderer = PointerReservationRenderer(padOnly, tab, new object());
                ReservationTicket oldPad = ArmModelTicket(padOnly.Tab(tab));
                SoftResetForPointer(padRenderer);
                Check(Area(padRenderer) == "List" && !padOnly.Tab(tab).ExecutionFocus && !padOnly.Tab(tab).CanActivate(oldPad),
                    "soft reset without an active pointer still cancels pad execution focus");
            }
        }
        finally
        {
            GUILayout.ButtonInput = null; GUILayout.ConsumeButtonEvent = false;
            GamepadBridge.PointerActivity = null; Input.Held.Clear();
            LogBridge.Reservations = savedAttempts; LogBridge.LastTarget = savedTarget;
        }
    }

    private static void RendererActualControllerPointerBoundary()
    {
        int savedAttempts = LogBridge.Reservations; object savedTarget = LogBridge.LastTarget;
        try
        {
            foreach (string tab in new[] { "이벤트", "미션" })
                foreach (string transition in new[] { "gamepad-stays", "gamepad-to-mouse", "mouse-pad-override", "unknown" })
                    foreach (PadState held in new[] {
                        new PadState { Buttons = PadButtons.A }, new PadState { Buttons = PadButtons.B },
                        new PadState { Buttons = PadButtons.Up }, new PadState { RightY = 32767 },
                        new PadState { LeftX = 32767, RightTrigger = 255 } })
                    {
                        GamepadBridge.PointerActivity = null; GamepadBridge.KeyboardActivity = null; GamepadBridge.Cancelled = null;
                        var model = new LogModel(); object target = new object(); int closes = 0;
                        var renderer = PointerReservationRenderer(model, tab, target, () => closes++);
                        var controller = new LogPadController();
                        var states = new PadState[4]; bool[] connected = { true, false, false, false };
                        LogInputMode mode = transition == "mouse-pad-override" ? LogInputMode.KeyboardMouse
                            : transition == "unknown" ? LogInputMode.Unknown : LogInputMode.Gamepad;
                        double time = 0;
                        LogPadFrame Tick(PadState state, bool apply = true)
                        {
                            states[0] = state;
                            LogPadFrame frame = controller.Tick(true, true, true, mode, time += .1, states, connected);
                            if (apply)
                            {
                                if (frame.ResetFocus) renderer.ResetInputFocus(frame.PreservePointerTransaction, frame.PreserveNavigationArea);
                                renderer.Handle(frame.Command); renderer.Scroll(frame.Scroll);
                            }
                            return frame;
                        }
                        Tick(default, false); Tick(default, false);
                        if (transition == "mouse-pad-override")
                        {
                            Tick(new PadState { Buttons = PadButtons.A }, false); Tick(default, false);
                            Check(controller.PadMode, "test boundary begins with a physical pad override while the game's scheme remains keyboard/mouse");
                        }
                        GamepadBridge.PointerActivity = controller.PointerActivity; GamepadBridge.KeyboardActivity = controller.KeyboardActivity;
                        GamepadBridge.Cancelled = controller.Cancel;
                        Func<string, bool> reserve = text => text.Contains("pointer-target", StringComparison.Ordinal);
                        Vector2 point = PointerTarget(renderer, control => control.Button && reserve(control.Text));
                        int attempts = LogBridge.Reservations;
                        ReservationTicket priorPadTicket = ArmModelTicket(model.Tab(tab));
                        DrawPointer(renderer, EventType.MouseDown, EventType.TouchDown, point);
                        object pointerTicket = Field(renderer, "pointerReservation");
                        if (transition == "gamepad-to-mouse") mode = LogInputMode.KeyboardMouse;
                        LogPadFrame takeover = Tick(held);
                        Check(takeover.ResetFocus && takeover.PreservePointerTransaction && takeover.Command == LogPadCommand.None && takeover.Scroll == 0,
                            "actual controller pointer/mode transition emits a soft reset and consumes held buttons, sticks and triggers");
                        Check(ReferenceEquals(pointerTicket, Field(renderer, "pointerReservation")) && pointerTicket != null
                            && !model.Tab(tab).CanActivate(priorPadTicket), "actual controller reset preserves only the current pointer ticket, not prior pad authority");
                        DrawPointer(renderer, EventType.MouseUp, EventType.TouchUp, point, reserve, consume: true);
                        Check(LogBridge.Reservations == attempts + 1 && ReferenceEquals(LogBridge.LastTarget, target),
                            "actual controller Tick between touch down/up does not erase or duplicate the pointer reservation");
                        for (int frame = 0; frame < 3; frame++)
                        {
                            LogPadFrame repeat = Tick(held);
                            Check(repeat.Command == LogPadCommand.None && repeat.Scroll == 0 && LogBridge.Reservations == attempts + 1,
                                "held pre-takeover pad input cannot become a second UI action or scroll after pointer release");
                        }
                        Tick(default);
                        LogPadFrame fresh = Tick(new PadState { Buttons = PadButtons.A });
                        if (mode == LogInputMode.KeyboardMouse)
                        {
                            Check(fresh.ResetFocus && !fresh.PreservePointerTransaction && fresh.Command == LogPadCommand.None
                                && LogBridge.Reservations == attempts + 1, "frozen keyboard/mouse scheme requires a safe physical pad takeover before accepting a new A");
                            Tick(default); fresh = Tick(new PadState { Buttons = PadButtons.A });
                        }
                        Check(fresh.Command == LogPadCommand.Activate, "actual controller accepts a new pad press only after complete release/neutral");
                        Check(LogBridge.Reservations == attempts + 2 && !model.Tab(tab).ExecutionFocus && Area(renderer) == "List" && !LogBridge.IsReserved(tab, target),
                            "after safe takeover and full neutral, a new pad A releases the still-selected original reservation once");

                        int afterPad = LogBridge.Reservations;
                        foreach (string pointerAction in new[] { "row", "tools", "close" })
                        {
                            Func<string, bool> actionMatches = text => pointerAction == "row" ? text.Contains("pointer-target", StringComparison.Ordinal)
                                : pointerAction == "tools" ? text.Contains("도구", StringComparison.Ordinal) : text == "닫기";
                            Vector2 actionPoint = PointerTarget(renderer, control => control.Button && actionMatches(control.Text));
                            DrawPointer(renderer, EventType.MouseDown, EventType.TouchDown, actionPoint);
                            LogPadFrame actionFrame = Tick(held);
                            DrawPointer(renderer, EventType.MouseUp, EventType.TouchUp, actionPoint, actionMatches, consume: true);
                            Check(actionFrame.ResetFocus && actionFrame.PreservePointerTransaction,
                                "actual controller keeps mouse/touch " + pointerAction + " transaction during Update");
                            Check(pointerAction == "row" ? model.Tab(tab).SelectedId == "pointer-target" && Area(renderer) == "List"
                                : pointerAction == "tools" ? Area(renderer) == "Tools" : closes == 1,
                                "actual controller integrated pointer release completes " + pointerAction);
                            if (pointerAction == "row") afterPad++;
                            Check(LogBridge.Reservations == afterPad && !model.Tab(tab).ExecutionFocus && LogBridge.IsReserved(tab, target),
                                "a fresh click reserves the selected row again; chrome and held pad input preserve that reservation");
                        }
                    }

            foreach (string boundary in new[] { "focus", "ready", "disconnect", "close", "explicit-cancel" })
            {
                GamepadBridge.PointerActivity = null; GamepadBridge.KeyboardActivity = null; GamepadBridge.Cancelled = null;
                var model = new LogModel(); var renderer = PointerReservationRenderer(model, "미션", new object());
                var controller = new LogPadController(); var states = new PadState[4]; bool[] connected = { true, false, false, false };
                controller.Tick(true, true, true, LogInputMode.Gamepad, 0, states, connected);
                controller.Tick(true, true, true, LogInputMode.Gamepad, .1, states, connected);
                GamepadBridge.PointerActivity = controller.PointerActivity; GamepadBridge.KeyboardActivity = controller.KeyboardActivity; GamepadBridge.Cancelled = controller.Cancel;
                Func<string, bool> reserve = text => text.Contains("pointer-target", StringComparison.Ordinal);
                Vector2 point = PointerTarget(renderer, control => control.Button && reserve(control.Text));
                int attempts = LogBridge.Reservations;
                DrawPointer(renderer, EventType.TouchDown, EventType.TouchDown, point);
                if (boundary == "explicit-cancel") controller.Cancel();
                if (boundary == "disconnect") connected[0] = false;
                states[0] = new PadState { Buttons = PadButtons.A, RightY = 32767 };
                LogPadFrame hard = controller.Tick(boundary != "close", boundary != "focus", boundary != "ready",
                    LogInputMode.KeyboardMouse, .2, states, connected);
                Check(hard.ResetFocus && !hard.PreservePointerTransaction && hard.Command == LogPadCommand.None && hard.Scroll == 0,
                    "hard " + boundary + " boundary wins when mode change and pointer activity happen in the same actual controller tick");
                renderer.ResetInputFocus(hard.PreservePointerTransaction, hard.PreserveNavigationArea);
                DrawPointer(renderer, EventType.TouchUp, EventType.TouchUp, point, reserve, consume: true);
                Check(LogBridge.Reservations == attempts && Field(renderer, "pointerIdentity") == null && Field(renderer, "pointerReservation") == null,
                    "hard " + boundary + " lifecycle boundary prevents the old touch release from reserving");
            }
        }
        finally
        {
            GamepadBridge.PointerActivity = null; GamepadBridge.KeyboardActivity = null; GamepadBridge.Cancelled = null;
            GUILayout.ButtonInput = null; GUILayout.ConsumeButtonEvent = false;
            LogBridge.Reservations = savedAttempts; LogBridge.LastTarget = savedTarget; Input.Held.Clear();
        }
    }

    private static void RendererActualControllerKeyboardBoundary()
    {
        int savedAttempts = LogBridge.Reservations; object savedTarget = LogBridge.LastTarget;
        try
        {
            foreach (string tab in new[] { "이벤트", "미션" })
                foreach (string modeCase in new[] { "Gamepad", "KeyboardMouse", "Unknown", "Gamepad-to-mouse" })
                {
                    GamepadBridge.PointerActivity = null; GamepadBridge.KeyboardActivity = null; GamepadBridge.Cancelled = null;
                    var model = new LogModel(); object target = new object();
                    var renderer = PointerReservationRenderer(model, tab, target);
                    renderer.ResetInputFocus();
                    LogInputMode mode = modeCase == "Gamepad-to-mouse" ? LogInputMode.Gamepad : (LogInputMode)Enum.Parse(typeof(LogInputMode), modeCase);
                    var controller = new LogPadController(); var states = new PadState[4]; bool[] connected = { true, false, false, false };
                    controller.Tick(true, true, true, mode, 0, states, connected);
                    controller.Tick(true, true, true, mode, .1, states, connected);
                    GamepadBridge.PointerActivity = controller.PointerActivity; GamepadBridge.KeyboardActivity = controller.KeyboardActivity; GamepadBridge.Cancelled = controller.Cancel;
                    double time = .1;
                    LogPadFrame UpdateAfterKeyboard()
                    {
                        LogPadFrame frame = controller.Tick(true, true, true, mode, time += .1, states, connected);
                        if (frame.ResetFocus) renderer.ResetInputFocus(frame.PreservePointerTransaction, frame.PreserveNavigationArea);
                        renderer.Handle(frame.Command); renderer.Scroll(frame.Scroll);
                        return frame;
                    }
                    int attempts = LogBridge.Reservations;
                    Event.current = new Event { type = EventType.MouseMove, rawType = EventType.MouseMove, mousePosition = new Vector2(-50, -50) };
                    renderer.Draw();
                    TapKey(renderer, KeyCode.Return);
                    if (modeCase == "Gamepad-to-mouse") mode = LogInputMode.KeyboardMouse;
                    LogPadFrame entered = UpdateAfterKeyboard();
                    Check(Area(renderer) == "List" && !model.Tab(tab).ExecutionFocus && LogBridge.Reservations == attempts + 1 && LogBridge.IsReserved(tab, target),
                        "actual controller Update after mouse movement preserves a completed one-shot keyboard reservation");
                    Check(!entered.ResetFocus && entered.Command == LogPadCommand.None,
                        "already-synchronous keyboard command absorbs only its pending soft mode/pad reset");
                    TapKey(renderer, KeyCode.Return); UpdateAfterKeyboard();
                    Check(LogBridge.Reservations == attempts + 2 && ReferenceEquals(LogBridge.LastTarget, target) && !model.Tab(tab).ExecutionFocus && !LogBridge.IsReserved(tab, target),
                        "a separate fresh keyboard confirmation releases once across actual controller Updates");

                    Func<string, bool> reserve = text => text.Contains("pointer-target", StringComparison.Ordinal);
                    Vector2 point = PointerTarget(renderer, control => control.Button && reserve(control.Text));
                    DrawPointer(renderer, EventType.TouchDown, EventType.TouchDown, point);
                    TapKey(renderer, KeyCode.LeftArrow);
                    Check(Field(renderer, "pointerIdentity") == null && Field(renderer, "pointerReservation") == null && Area(renderer) == "List",
                        "fresh keyboard command takes ownership from an unfinished touch transaction");
                    UpdateAfterKeyboard();
                    DrawPointer(renderer, EventType.TouchUp, EventType.TouchUp, point, reserve, consume: true);
                    Check(LogBridge.Reservations == attempts + 2 && Area(renderer) == "List",
                        "touch release after a fresh keyboard command cannot perform the abandoned pointer action");
                    TapKey(renderer, KeyCode.Return);
                    model.ResetContext(732); Publish(model, tab, Item("new-keyboard-context", new object()));
                    controller.Cancel();
                    LogPadFrame hard = UpdateAfterKeyboard();
                    Check(hard.ResetFocus && !hard.PreservePointerTransaction && Area(renderer) == "List"
                        && model.Tab(tab).SelectedId == null && !model.Tab(tab).ExecutionFocus && LogBridge.Reservations == attempts + 3,
                        "pending keyboard activity never absorbs a hard context/explicit-cancel reset");
                }
        }
        finally
        {
            GamepadBridge.PointerActivity = null; GamepadBridge.KeyboardActivity = null; GamepadBridge.Cancelled = null;
            LogBridge.Reservations = savedAttempts; LogBridge.LastTarget = savedTarget; Input.Held.Clear();
        }
    }

    private static void AssertPublicChoices(LogRenderer renderer, LogModel model, LogItem[] rows, string scenario)
    {
        Repaint(renderer);
        foreach (LogItem row in rows)
        {
            Check(GUILayout.LiteralLabels.Contains(row.Description), scenario + ": every choice body is drawn independently of selection");
            Check(GUILayout.LiteralLabels.Contains("원본 ID: " + row.RawId), scenario + ": every original choice ID remains available");
            string read = row.ReadState == LogReadState.Read ? "읽음" : row.ReadState == LogReadState.Unread ? "미독" : "읽음 정보 없음";
            Check(GUILayout.LiteralLabels.Contains(read), scenario + ": every choice keeps its story-read badge without redundant healthy retrieval wording");
            Check(GUILayout.Controls.Exists(control => control.Text == row.Description && control.VisibleRect.height > 0),
                scenario + ": both short comparison bodies have visible pixels inside the viewport");
        }
        Check(!GUILayout.Buttons.Exists(text => text.Contains("▷", StringComparison.Ordinal) || text.Contains("▼", StringComparison.Ordinal)),
            scenario + ": choice headers have no disclosure triangle");
        Check(Area(renderer) == "List", scenario + ": public choice comparison has no hidden detail-focus state");
    }

    private static void ClickRenderedButton(LogRenderer renderer, string heading, bool exact = false)
    {
        Repaint(renderer);
        Func<string, bool> matches = text => exact ? text == heading : text.Contains(heading, StringComparison.Ordinal);
        GUILayout.ControlRecord header = GUILayout.Controls.Find(control => control.Button && matches(control.Text));
        Check(header != null && header.VisibleRect.height > 0, "mouse target control has visible pixels in the native-window boundary");
        Vector2 point = new Vector2(header.WindowLocalRect.x + 5, header.WindowLocalRect.y + 5);
        try
        {
            Event.current = new Event { type = EventType.MouseDown, rawType = EventType.MouseDown, button = 0, mousePosition = point };
            renderer.Draw();
            GUILayout.ButtonInput = matches;
            Event.current = new Event { type = EventType.MouseUp, rawType = EventType.MouseUp, button = 0, mousePosition = point };
            renderer.Draw();
        }
        finally { GUILayout.ButtonInput = null; }
    }

    private static void RendererChoiceAndConditionNavigation()
    {
        int reservations = LogBridge.Reservations;
        var model = new LogModel(); model.ResetContext(300);
        var choices = new[] { Item("choice-first"), Item("choice-second") };
        choices[0].Text = "선택 비교 첫째"; choices[0].Description = "첫 번째 선택의 모든 영향";
        choices[1].Text = "선택 비교 둘째"; choices[1].Description = "두 번째 선택의 모든 영향";
        choices[0].ReadState = LogReadState.Read;
        choices[1].ReadState = LogReadState.Unread;
        Publish(model, "선택지", choices);
        int closes = 0;
        var renderer = new LogRenderer(model, null, () => closes++);
        renderer.Handle(LogPadCommand.None);
        AssertPublicChoices(renderer, model, choices, "before selection");
        Check(model.Tab("선택지").SelectedId == null, "public choice bodies do not require an initial selection");
        renderer.Handle(LogPadCommand.Activate);
        renderer.Handle(LogPadCommand.Activate);
        renderer.Handle(LogPadCommand.Right);
        renderer.Handle(LogPadCommand.Left);
        AssertPublicChoices(renderer, model, choices, "after repeated A and both horizontal directions");
        Check(model.Tab("선택지").SelectedId == null, "choice activation and horizontal input leave the read-only body unselected");
        renderer.Handle(LogPadCommand.Down);
        float choiceOffset = model.Tab("선택지").ListScroll;
        Check(choiceOffset > 0 && model.Tab("선택지").SelectedId == null, "choice down scrolls text without selecting an item");
        renderer.Handle(LogPadCommand.Up);
        Check(model.Tab("선택지").ListScroll == 0 && model.Tab("선택지").SelectedId == null, "choice up reverses body scrolling without selecting an item");
        AssertPublicChoices(renderer, model, choices, "after directional body scrolling");
        GUILayout.ScrollViewRecord comparison = GUILayout.ScrollViews[0];
        Check(!comparison.Controls.Exists(control => control.Button), "choice comparison has no fake selection or collapse button");
        GUILayout.ControlRecord firstBody = comparison.Controls.Find(control => control.Text == choices[0].Description);
        GUILayout.ControlRecord secondHeading = comparison.Controls.Find(control => control.Text == choices[1].Text);
        Check(secondHeading.Rect.y - firstBody.Rect.yMax >= Math.Max(8, ((GUIStyle)Field(renderer, "textStyle")).fontSize * .6f),
            "choice items have an explicit readable vertical gap in addition to their box padding");
        model.Tab("선택지").Select(choices[1].Id);
        renderer.Handle(LogPadCommand.Activate); renderer.Handle(LogPadCommand.Down); renderer.Handle(LogPadCommand.Right);
        Check(model.Tab("선택지").SelectedId == choices[1].Id && Area(renderer) == "List",
            "read-only renderer navigation does not create or replace model selection");
        model.Tab("선택지").ListScroll = 0;
        renderer.Handle(LogPadCommand.Back);
        Check(closes == 1, "B closes the public choice comparison directly");

        var conditions = new LogModel(); conditions.ResetContext(301);
        var conditionRows = new[] { Item("condition-alpha", new object()), Item("condition-beta", new object()), Item("condition-charlie", new object()) };
        foreach (LogItem row in conditionRows) row.ReservationAllowed = false;
        conditionRows[0].Description = "조건 첫 상세 설명";
        conditionRows[1].Description = "조건 둘 상세 설명";
        conditionRows[2].Description = "조건 셋 상세 설명";
        conditionRows[1].ReadState = LogReadState.Read;
        conditionRows[2].ReadState = LogReadState.Unread;
        Publish(conditions, "조건", conditionRows);
        int conditionCloses = 0;
        var conditionRenderer = new LogRenderer(conditions, null, () => conditionCloses++);
        conditionRenderer.Handle(LogPadCommand.None);
        conditionRenderer.Handle(LogPadCommand.NextTab);
        conditionRenderer.Handle(LogPadCommand.Activate);
        conditionRenderer.Handle(LogPadCommand.Activate);
        Repaint(conditionRenderer);
        Check(GUILayout.ScrollViews.Count == 2 && Area(conditionRenderer) == "List", "condition tab provides independent list and preview scroll regions without a fake detail-control focus");
        Check(GUILayout.LiteralLabels.Contains("조건 상세") && !GUILayout.LiteralLabels.Contains("▶ 조건 상세"), "condition preview heading is plain text rather than a focusable control");
        Check(GUILayout.Buttons.Exists(text => text.Contains("condition-alpha", StringComparison.Ordinal) && text.Contains("읽음 정보 없음", StringComparison.Ordinal)), "condition unknown story-read state remains explicit and neutral");
        Check(GUILayout.Buttons.Exists(text => text.Contains("condition-beta", StringComparison.Ordinal) && text.Contains("읽음", StringComparison.Ordinal))
            && GUILayout.Buttons.Exists(text => text.Contains("condition-charlie", StringComparison.Ordinal) && text.Contains("미독", StringComparison.Ordinal)), "condition read and unread publications keep their meaning");
        Check(!GUILayout.Buttons.Exists(text => text.Contains("예약", StringComparison.Ordinal))
            && !GUILayout.LiteralLabels.Exists(text => text.Contains("해당 미션의 조건 통과", StringComparison.Ordinal)), "condition detail has neither reservation control nor mission reservation guidance");
        conditionRenderer.Handle(LogPadCommand.Down);
        Check(conditions.Tab("조건").SelectedId == "condition-beta" && Area(conditionRenderer) == "List", "condition down changes selected checkpoint in the list");
        TapKey(conditionRenderer, KeyCode.UpArrow);
        Check(conditions.Tab("조건").SelectedId == "condition-alpha" && Area(conditionRenderer) == "List", "keyboard condition up selects previous checkpoint");
        conditionRenderer.Handle(LogPadCommand.Left);
        Check(Area(conditionRenderer) == "List", "condition left returns to checkpoint list");
        conditionRenderer.Handle(LogPadCommand.Right);
        Check(Area(conditionRenderer) == "List" && !conditions.Tab("조건").ExecutionFocus, "condition right cannot enter a preview with no actionable control");
        conditions.Tab("조건").ExecutionFocus = true;
        ReservationTicket dummyConditionTicket = conditions.Tab("조건").BeginReservation();
        Check(dummyConditionTicket == null, "condition model refuses rows without an explicit reservable branch target");
        typeof(LogRenderer).GetMethod("Reserve", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(conditionRenderer, new object[] { dummyConditionTicket });
        Check(LogBridge.Reservations == reservations && !conditions.Tab("조건").ExecutionFocus,
            "condition renderer cannot reserve when the selected row refuses authority");
        conditions.Tab("조건").ExecutionFocus = true;
        conditionRenderer.Handle(LogPadCommand.Activate);
        Check(LogBridge.Reservations == reservations, "read-only condition rows and forced execution focus cannot reserve a condition");
        conditionRenderer.Scroll(1);
        Check(conditions.Tab("조건").DetailScroll > 0 && conditions.Tab("조건").ListScroll == 0, "condition right stick scrolls the detail instead of the checkpoint list");
        conditionRenderer.Handle(LogPadCommand.Tools);
        conditionRenderer.Handle(LogPadCommand.Back);
        Check(Area(conditionRenderer) == "List", "condition tools return to its actionable checkpoint list");
        conditionRenderer.Handle(LogPadCommand.Back);
        Check(conditionCloses == 1, "condition B closes directly from its checkpoint list");

        foreach (string tab in new[] { "이벤트", "미션" })
        {
            var reservationModel = new LogModel(); reservationModel.ResetContext(303);
            Publish(reservationModel, tab, Item("reserve-first", new object()), Item("reserve-second", new object()));
            var reservationRenderer = new LogRenderer(reservationModel, null, null);
            reservationRenderer.Handle(LogPadCommand.None);
            for (int index = 0; index < (tab == "조건" ? 1 : tab == "이벤트" ? 2 : 3); index++) reservationRenderer.Handle(LogPadCommand.NextTab);
            reservationRenderer.Handle(LogPadCommand.Activate);
            reservationRenderer.Handle(LogPadCommand.Right);
            reservationRenderer.Handle(LogPadCommand.Down);
            Check(reservationModel.Tab(tab).SelectedId == "reserve-second" && !reservationModel.Tab(tab).ExecutionFocus, tab + ": down always moves to the next list item");
            reservationRenderer.Handle(LogPadCommand.Up);
            Check(reservationModel.Tab(tab).SelectedId == "reserve-first" && !reservationModel.Tab(tab).ExecutionFocus, tab + ": up always returns to the previous list item");
            TapKey(reservationRenderer, KeyCode.DownArrow);
            Check(reservationModel.Tab(tab).SelectedId == "reserve-second" && !reservationModel.Tab(tab).ExecutionFocus, tab + ": keyboard down moves list selection");
            TapKey(reservationRenderer, KeyCode.UpArrow);
            Check(reservationModel.Tab(tab).SelectedId == "reserve-first" && !reservationModel.Tab(tab).ExecutionFocus, tab + ": keyboard up moves list selection");
        }

        var keyboardModel = new LogModel(); keyboardModel.ResetContext(304);
        Publish(keyboardModel, "선택지", Item("key-first"), Item("key-second"), Item("key-third"));
        int keyboardCloses = 0;
        var keyboardRenderer = new LogRenderer(keyboardModel, null, () => keyboardCloses++);
        keyboardRenderer.Handle(LogPadCommand.None);
        try
        {
            TapKey(keyboardRenderer, KeyCode.Return);
            TapKey(keyboardRenderer, KeyCode.Return);
            TapKey(keyboardRenderer, KeyCode.RightArrow);
            TapKey(keyboardRenderer, KeyCode.LeftArrow);
            Check(keyboardModel.Tab("선택지").SelectedId == null && Area(keyboardRenderer) == "List", "real keyboard activation and horizontal input do not select read-only choices");
            PressKey(keyboardRenderer, KeyCode.DownArrow);
            float keyboardOffset = keyboardModel.Tab("선택지").ListScroll;
            PressKey(keyboardRenderer, KeyCode.DownArrow);
            Check(keyboardModel.Tab("선택지").SelectedId == null && keyboardOffset > 0 && keyboardModel.Tab("선택지").ListScroll == keyboardOffset,
                "held keyboard down scrolls once and ignores repeated KeyDown");
            keyboardModel.ResetContext(305);
            Publish(keyboardModel, "선택지", Item("replacement"));
            keyboardRenderer.ResetInputFocus();
            keyboardRenderer.Handle(LogPadCommand.None);
            PressKey(keyboardRenderer, KeyCode.DownArrow);
            Check(keyboardModel.Tab("선택지").SelectedId == null && keyboardModel.Tab("선택지").ListScroll == 0, "held direction cannot scroll a replacement context");
            ReleaseKey(keyboardRenderer, KeyCode.DownArrow);
            TapKey(keyboardRenderer, KeyCode.DownArrow);
            Check(keyboardModel.Tab("선택지").SelectedId == null && keyboardModel.Tab("선택지").ListScroll > 0, "fresh direction scrolls after release in replacement context");
            TapKey(keyboardRenderer, KeyCode.Escape);
            Check(keyboardCloses == 1, "keyboard Escape closes the public choice list directly");
            Check(LogBridge.Reservations == reservations, "choice and condition navigation never invokes event or mission reservation");
        }
        finally { Input.Held.Clear(); }
    }

    private static void RendererStructuredConditionBlocks()
    {
        Rect saved = ConfiguredRect();
        int oldFont = DiceMaster.Config.ConfigManager.LogFontSize.Value, oldWidth = Screen.width, oldHeight = Screen.height;
        int reservations = LogBridge.Reservations;
        try
        {
            Screen.width = 1920; Screen.height = 1200;
            foreach (int[] size in new[] { new[] { 872, 960, 20 }, new[] { 540, 600, 32 } })
            {
                ConfigureLayout(new Rect(60, 50, size[0], size[1]), size[2]);
                var branches = new[] {
                    new LogConditionBranch("분기 1", "이 조건을 만족하면", "돈 >= 10 | 평판 >= 5\n같은 분기의 다음 줄: 분기 99, 그 외\n" + new string('긴', 800)),
                    new LogConditionBranch("분기 2", "앞 분기가 모두 불충족일 때 검사", "체력 >= 20 | 기력 >= 15\nRAW_CONDITION_" + new string('A', 256)),
                    new LogConditionBranch("그 외 (분기 3)", "위 분기가 모두 불충족일 때", "")
                };
                const string hint = "위에서부터 검사하며, 처음 만족한 분기로 진행합니다.";
                LogItem first = Item("structured-checkpoint");
                first.Text = "1 2 그 외";
                first.Description = "LEGACY_FLAT_NOT_REPEATED\n분기처럼 보이는 줄";
                first.ReadState = LogReadState.Unread;
                first.ConditionDetails = new LogConditionDetails(hint, branches, first.Description);
                LogItem second = Item("other-checkpoint");
                second.ConditionDetails = new LogConditionDetails("", new[] { new LogConditionBranch("다른 분기", "다른 맥락", "다른 설명") });
                var model = new LogModel(); model.ResetContext(310);
                Publish(model, "조건", first, second);
                var renderer = new LogRenderer(model, null, null);
                renderer.Handle(LogPadCommand.None); renderer.Handle(LogPadCommand.NextTab); renderer.Handle(LogPadCommand.Activate);
                Repaint(renderer); Repaint(renderer);
                GUILayout.ScrollViewRecord pane = GUILayout.ScrollViews[1];
                Check(pane.Controls.Exists(control => !control.Button && control.Text == hint)
                    && pane.Controls.Exists(control => !control.Button && control.Text == first.Text),
                    "condition details keep source order guidance and existing branch-read summary");
                Check(!pane.Controls.Exists(control => control.Text == first.Description),
                    "structured condition blocks do not repeat the compatibility flattened description");
                var observed = new List<GUILayout.BoxRecord>();
                foreach (LogConditionBranch branch in branches)
                {
                    List<GUILayout.BoxRecord> boxes = GUILayout.Boxes.FindAll(box => box.Controls.Exists(control => control.Text == branch.Title)
                        && box.Controls.FindAll(control => Array.Exists(branches, candidate => control.Text == candidate.Title)).Count == 1);
                    Check(boxes.Count == 1, "each real condition branch owns exactly one separate styled vertical block");
                    GUILayout.BoxRecord box = boxes[0]; observed.Add(box);
                    Check(box.Style != null && box.Rect.width > 0 && box.Rect.height > 0
                        && box.Controls.Exists(control => !control.Button && control.Text == branch.Context),
                        "branch block has real geometry and its supplied condition/fallback context");
                    Check(!box.Controls.Exists(control => control.Button)
                        && box.Controls.Count == (branch.Description.Length == 0 ? 3 : 4)
                        && box.Controls.Exists(control => !control.Button && control.Text == "? 판정 미확인"),
                        "branch body lines and unknown judgment stay within one read-only block rather than becoming fake branches or controls");
                    if (branch.Description.Length > 0)
                        Check(box.Controls.Exists(control => !control.Button && control.Text == branch.Description),
                            "complete branch description retains line breaks and the existing | operator text");
                }
                for (int index = 1; index < observed.Count; index++)
                    Check(observed[index].Rect.y - observed[index - 1].Rect.yMax >= Math.Max(10, size[2] * .6f) - .01f,
                        "branch blocks keep publication order and a visible gap between complete descriptions");
                Check(pane.ContentHeight > pane.Viewport.height && !pane.Controls.Exists(control => control.Button),
                    "long structured conditions remain readable in an independently scrollable, noninteractive detail pane");
                GUILayout.ControlRecord longToken = pane.Controls.Find(control => control.Text == branches[1].Description);
                Check(observed[1].Rect.width <= pane.ContentWidth + .01f
                    && longToken.Rect.width <= observed[1].Rect.width - observed[1].Style.padding.horizontal + .01f
                    && longToken.Rect.height > size[2] * 4,
                    "unbroken condition identifiers wrap within the actual branch client width instead of widening content beyond its vertical-only pane");
                renderer.Handle(LogPadCommand.Right); renderer.Handle(LogPadCommand.Activate); renderer.Handle(LogPadCommand.Left);
                Check(Area(renderer) == "List" && model.Tab("조건").SelectedId == first.Id && !model.Tab("조건").ExecutionFocus,
                    "condition A and horizontal input cannot focus a branch or nonexistent detail action");
                float beforeScroll = model.Tab("조건").DetailScroll;
                renderer.Scroll(1);
                Check(model.Tab("조건").DetailScroll > beforeScroll && model.Tab("조건").ListScroll == 0,
                    "right stick scrolls the complete condition blocks while checkpoint list keeps focus");
                renderer.Handle(LogPadCommand.Down);
                Check(model.Tab("조건").SelectedId == second.Id && model.Tab("조건").DetailScroll == 0 && Area(renderer) == "List",
                    "condition list navigation remains available after reading expanded branch blocks");
                Check(LogBridge.Reservations == reservations, "read-only condition blocks never reserve game actions");
            }

            ConfigureLayout(new Rect(60, 50, 872, 960), 20);
            var fallbackModel = new LogModel(); fallbackModel.ResetContext(311);
            const string countHint = "이 체크포인트는 조건식 대신 실행 횟수를 기준으로 분기합니다.";
            LogItem countRow = Item("count-checkpoint");
            countRow.Description = "COUNT_LEGACY_SHOULD_NOT_RENDER";
            countRow.ConditionDetails = new LogConditionDetails("", Array.Empty<LogConditionBranch>(), countHint);
            Publish(fallbackModel, "조건", countRow);
            var fallbackRenderer = new LogRenderer(fallbackModel, null, null);
            fallbackRenderer.Handle(LogPadCommand.None); fallbackRenderer.Handle(LogPadCommand.NextTab); fallbackRenderer.Handle(LogPadCommand.Activate);
            Repaint(fallbackRenderer);
            Check(GUILayout.ScrollViews[1].Controls.Exists(control => !control.Button && control.Text == countHint)
                && !GUILayout.ScrollViews[1].Controls.Exists(control => control.Text == countRow.Description || control.Text == "분기 1" || control.Text.StartsWith("그 외", StringComparison.Ordinal)),
                "count-based checkpoint displays neutral supplied guidance without inventing condition alternatives");
            fallbackRenderer.Handle(LogPadCommand.Right); fallbackRenderer.Handle(LogPadCommand.Activate);
            Check(Area(fallbackRenderer) == "List" && !fallbackModel.Tab("조건").ExecutionFocus && LogBridge.Reservations == reservations,
                "count-based guidance has no false selectable or executable detail action");
            countRow.ConditionDetails = null; countRow.Description = "호환 설명 그대로 | 다음 줄\n분기 77처럼 보이는 문자열";
            Publish(fallbackModel, "조건", countRow); Repaint(fallbackRenderer);
            Check(GUILayout.ScrollViews[1].Controls.Exists(control => !control.Button && control.Text == countRow.Description),
                "condition rows without structured publication retain their complete original compatibility description without parsing");
            countRow.ConditionDetails = new LogConditionDetails("", null);
            Publish(fallbackModel, "조건", countRow); Repaint(fallbackRenderer);
            Check(!GUILayout.ScrollViews[1].Controls.Exists(control => control.Text == countRow.Description || control.Text == "분기 1"),
                "successful empty structured condition data does not fabricate branches or revive stale compatibility text");
        }
        finally { ConfigureLayout(saved, oldFont); Screen.width = oldWidth; Screen.height = oldHeight; }
    }

    private static void RendererChoiceAndConditionReflow()
    {
        Rect saved = ConfiguredRect();
        int oldFont = DiceMaster.Config.ConfigManager.LogFontSize.Value, oldWidth = Screen.width, oldHeight = Screen.height;
        try
        {
            Screen.width = 1920; Screen.height = 1080;
            ConfigureLayout(new Rect(20, 20, 1200, 900), 24);
            var targets = new[] { new object(), new object(), new object() };
            Func<int, LogItem[]> publication = length =>
            {
                var rows = new[] { Item("before", targets[0]), Item("selected", targets[1]), Item("after", targets[2]) };
                rows[0].Text = rows[0].Title = "비교 카드 앞"; rows[0].Description = new string('긴', length);
                rows[1].Text = rows[1].Title = "비교 카드 선택"; rows[1].Description = new string('설', 200);
                rows[2].Text = rows[2].Title = "비교 카드 다음";
                return rows;
            };
            var model = new LogModel(); model.ResetContext(306);
            Publish(model, "선택지", publication(40));
            var renderer = new LogRenderer(model, null, null);
            renderer.Handle(LogPadCommand.None);
            renderer.Handle(LogPadCommand.Down);
            renderer.Handle(LogPadCommand.Down);
            Repaint(renderer); Repaint(renderer);
            float initialOffset = model.Tab("선택지").ListScroll;
            Check(Area(renderer) == "List" && model.Tab("선택지").SelectedId == null && initialOffset > 0,
                "choice directions scroll the continuous body without a header-follow request");
            Publish(model, "선택지", publication(8000));
            Repaint(renderer); Repaint(renderer);
            Check(model.Tab("선택지").ListScroll == initialOffset && model.Tab("선택지").SelectedId == null,
                "preceding choice body growth preserves current reading scroll without creating selection");
            renderer.Scroll(1);
            float manualOffset = model.Tab("선택지").ListScroll;
            Publish(model, "선택지", publication(12000));
            Repaint(renderer); Repaint(renderer);
            Check(model.Tab("선택지").ListScroll == manualOffset, "manual choice body scrolling survives preceding-body reflow");
            renderer.Handle(LogPadCommand.Down);
            Check(model.Tab("선택지").ListScroll > manualOffset && model.Tab("선택지").SelectedId == null,
                "fresh direction continues body scrolling after reflow instead of jumping to a header");

            var conditions = new LogModel(); conditions.ResetContext(307);
            LogItem[] conditionRows = LongRows(30);
            Publish(conditions, "조건", conditionRows);
            var conditionRenderer = new LogRenderer(conditions, null, null);
            conditionRenderer.Handle(LogPadCommand.None);
            conditionRenderer.Handle(LogPadCommand.NextTab);
            conditionRenderer.Handle(LogPadCommand.Activate);
            conditionRenderer.Handle(LogPadCommand.Right);
            Repaint(conditionRenderer); Repaint(conditionRenderer);
            conditionRenderer.Scroll(1);
            float detailOffset = conditions.Tab("조건").DetailScroll;
            Array.Reverse(conditionRows);
            Publish(conditions, "조건", conditionRows);
            FocusVisible(conditionRenderer, conditions, "조건", "목록 항목 00", "condition preview scroll does not suppress list reflow following");
            Check(Area(conditionRenderer) == "List" && conditions.Tab("조건").SelectedId == "entry-00" && conditions.Tab("조건").DetailScroll == detailOffset,
                "condition refresh keeps selected preview identity and its independent body scroll");
            conditionRenderer.Handle(LogPadCommand.Up);
            FocusVisible(conditionRenderer, conditions, "조건", "목록 항목 01", "condition list direction follows the next selected checkpoint");
        }
        finally
        {
            ConfigureLayout(saved, oldFont);
            Screen.width = oldWidth; Screen.height = oldHeight;
        }
    }

    private static void RendererAutomaticScrollbars()
    {
        Rect saved = ConfiguredRect();
        int oldFont = DiceMaster.Config.ConfigManager.LogFontSize.Value, oldWidth = Screen.width, oldHeight = Screen.height;
        try
        {
            Screen.width = 1920; Screen.height = 1200;
            foreach (string tab in new[] { "선택지", "조건", "이벤트", "미션" })
                foreach (bool overflowing in new[] { false, true })
                {
                    ConfigureLayout(new Rect(60, 50, 872, 960), 20);
                    var model = new LogModel(); model.ResetContext(308);
                    LogItem[] rows = overflowing ? LongRows(30) : new[] { Item("short", new object()), Item("short-next", new object()) };
                    rows[0].Description = overflowing ? rows[0].Description : "짧은 첫 본문";
                    if (overflowing) foreach (LogItem row in rows) row.Description = new string('설', 5000);
                    Publish(model, tab, rows);
                    var renderer = new LogRenderer(model, null, null);
                    renderer.Handle(LogPadCommand.None);
                    for (int index = 0; index < Array.IndexOf(new[] { "선택지", "조건", "이벤트", "미션" }, tab); index++) renderer.Handle(LogPadCommand.NextTab);
                    renderer.Handle(LogPadCommand.Activate);
                    Repaint(renderer); Repaint(renderer);
                    Check(GUILayout.ScrollViews.Count == (tab == "선택지" ? 1 : 2), tab + ": keeps the complete reading/list and detail panes");
                    foreach (GUILayout.ScrollViewRecord pane in GUILayout.ScrollViews)
                    {
                        Check(!pane.AlwaysShowVertical && !pane.AlwaysShowHorizontal && ReferenceEquals(pane.HorizontalScrollbar, GUIStyle.none),
                            tab + ": actual renderer requests conditional vertical bars and suppresses horizontal bars");
                        Check((pane.ContentHeight > pane.Viewport.height) == overflowing,
                            tab + ": short/long fixtures genuinely fit or overflow their measured viewport");
                        Check(pane.ReservedVerticalScrollbar == overflowing,
                            tab + ": managed repeated-layout contract reserves scrollbar width only for overflowing content (native Unity rendering remains untested)");
                        if (!overflowing) Check(Math.Abs(pane.ContentWidth - pane.Viewport.width) < .01f,
                            tab + ": fitting content reclaims the entire unused scrollbar gutter");
                        if (!overflowing && tab != "선택지")
                            Check(pane.Controls.FindAll(control => control.Button).TrueForAll(control => Math.Abs(control.Rect.width - pane.ContentWidth) < .01f),
                                tab + ": fitting rows use the full available client width without a scrollbar gutter");
                    }
                    if (tab == "선택지")
                        Check(model.Tab(tab).SelectedId == null && !GUILayout.ScrollViews[0].Controls.Exists(control => control.Button),
                            "short and overflowing choice bodies are read-only and never select an entry");
                    if (!overflowing && tab == "선택지")
                    {
                        var pane = GUILayout.ScrollViews[0];
                        var firstBody = pane.Controls.Find(control => control.Text == "짧은 첫 본문");
                        var nextTitle = pane.Controls.Find(control => control.Text == rows[1].Title);
                        Check(((GUIStyle)Field(renderer, "textStyle")).fontSize == 20 && nextTitle.Rect.y - firstBody.Rect.yMax >= 12,
                            "20px choice body has at least a 12px explicit gap between readable entries");
                    }
                }
        }
        finally { ConfigureLayout(saved, oldFont); Screen.width = oldWidth; Screen.height = oldHeight; }
    }

    private static void RendererRefreshWording()
    {
        Rect saved = ConfiguredRect();
        int oldFont = DiceMaster.Config.ConfigManager.LogFontSize.Value, oldWidth = Screen.width, oldHeight = Screen.height;
        try
        {
            Screen.width = 1920; Screen.height = 1200;
            ConfigureLayout(new Rect(60, 50, 872, 960), 20);
            foreach (string tab in new[] { "선택지", "조건", "이벤트", "미션" })
            {
                bool storyRead = tab == "선택지" || tab == "조건";
                var model = new LogModel(); model.ResetContext(309);
                LogItem healthy = Item("healthy", new object()); healthy.Title = "정상 " + tab + " 제목";
                LogItem read = Item("read", new object()); read.ReadState = LogReadState.Read;
                LogItem unread = Item("unread", new object()); unread.ReadState = LogReadState.Unread;
                LogItem analyzed = Item("analyzed", new object()); analyzed.AnalysisStates.Add("Partial");
                LogItem waiting = Item("waiting", new object()); waiting.RefreshState = LogRefreshState.Waiting;
                LogItem failed = Item("failed", new object()); failed.RefreshState = LogRefreshState.ReadFailure; failed.RefreshStatus = "조회 실패 원인";
                LogItem retained = Item("retained", new object()); retained.RefreshState = LogRefreshState.RetainedFailure; retained.RefreshStatus = "이전 값 유지 원인"; retained.HasLastGood = true;
                Publish(model, tab, healthy, read, unread, analyzed, waiting, failed, retained);
                var renderer = new LogRenderer(model, null, null);
                renderer.Handle(LogPadCommand.None);
                while (renderer.CurrentTab != tab) renderer.Handle(LogPadCommand.NextTab);
                renderer.Handle(LogPadCommand.Activate);
                Repaint(renderer);
                Check(!GUILayout.Controls.Exists(control => control.Text.Contains("조회 정상", StringComparison.Ordinal)),
                    tab + ": readable content, list and selected detail omit redundant healthy retrieval wording");
                var pane = GUILayout.ScrollViews[0];
                if (tab == "선택지")
                {
                    Check(pane.Controls.Exists(control => !control.Button && control.Text == "읽음 정보 없음")
                        && pane.Controls.Exists(control => !control.Button && control.Text == "읽음")
                        && pane.Controls.Exists(control => !control.Button && control.Text == "미독")
                        && pane.Controls.Exists(control => !control.Button && control.Text == "읽음 정보 없음 · 추가 영향 가능"),
                        "choice public comparison preserves unknown/read/unread story badges and analysis independently of healthy retrieval state");
                }
                else
                {
                    string eligibilityCaption = tab == "이벤트" ? "? 판정 미확인 · " : "";
                    string healthyCaption = "▶ " + healthy.Title + (storyRead ? "\n읽음 정보 없음" : tab == "이벤트" ? "\n? 판정 미확인" : "");
                    Check(pane.Controls.Exists(control => control.Button && control.Text == healthyCaption),
                        tab + ": healthy caption keeps only applicable story-read information without a trailing blank status row");
                    Check(!GUILayout.ScrollViews[1].Controls.Exists(control => !control.Button && string.IsNullOrEmpty(control.Text)),
                        tab + ": healthy detail does not allocate a blank state label");
                    Check(pane.Controls.Exists(control => control.Button && control.Text == read.Title + "\n" + eligibilityCaption + "읽음")
                        && pane.Controls.Exists(control => control.Button && control.Text == unread.Title + "\n" + eligibilityCaption + "미독"),
                        tab + ": explicit read state remains visible without dangling join punctuation");
                    Check(pane.Controls.Exists(control => control.Button && control.Text == analyzed.Title + "\n" + (storyRead ? "읽음 정보 없음 · " : eligibilityCaption) + "추가 영향 가능")
                        && !pane.Controls.Exists(control => control.Button && control.Text.Contains("\n · ", StringComparison.Ordinal)),
                        tab + ": healthy analysis explanation retains its context without a leading separator");
                    if (!storyRead)
                        Check(!GUILayout.Controls.Exists(control => control.Text.Contains("읽음 정보 없음", StringComparison.Ordinal)),
                            tab + ": absent story-read mapping does not become an unknown-read badge in list or detail");
                }
                Check(pane.Controls.Exists(control => control.Text.Contains("설명 조회 대기", StringComparison.Ordinal))
                    && pane.Controls.Exists(control => control.Text.Contains(failed.RefreshStatus, StringComparison.Ordinal))
                    && pane.Controls.Exists(control => control.Text.Contains(retained.RefreshStatus, StringComparison.Ordinal)),
                    tab + ": waiting, failed and retained status explanations remain available");
                Check(healthy.RefreshState == LogRefreshState.Healthy && healthy.CanReserve
                    && failed.RefreshState == LogRefreshState.ReadFailure && !failed.CanReserve
                    && !waiting.CanReserve && retained.HasLastGood && !retained.CanReserve,
                    tab + ": healthy-label removal preserves publication data and reservation eligibility");
            }
        }
        finally { ConfigureLayout(saved, oldFont); Screen.width = oldWidth; Screen.height = oldHeight; }
    }

    private static void Invoke(LogRenderer renderer, string method) => typeof(LogRenderer).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(renderer, null);
    private static void RendererRenderingBoundaries()
    {
        var model = new LogModel();
        model.ResetContext(10);
        LogItem card = Item("raw-card-id");
        card.RawId = "P_成功_1<raw>";
        card.Text = "실제 선택지 문장으로 비교합니다";
        card.Description = new string('긴', 5000);
        Publish(model, "선택지", card);
        var renderer = new LogRenderer(model, null, null);
        renderer.Handle(LogPadCommand.None);
        Event.current = new Event { type = EventType.Repaint, rawType = EventType.Repaint };
        Invoke(renderer, "EnsureStyles");
        GUILayout.Buttons.Clear();
        GUILayout.LiteralLabels.Clear();
        DiceMaster.EmbeddedTranslations.Display.TranslatedLabels.Clear();
        Invoke(renderer, "DrawChoices");
        Check(GUILayout.LiteralLabels.Contains(card.Text) && !GUILayout.Buttons.Exists(text => text.Contains(card.Text, StringComparison.Ordinal)), "read-only choice heading uses actual option sentence without an inactive selection control");
        Check(GUILayout.LiteralLabels.Contains(card.Description) && model.Tab("선택지").SelectedId == null, "choice body is drawn without requiring any selection");
        Check(model.Tab("선택지").ListScroll == 0, "long public choice body does not force scrolling to its end");
        Check(GUILayout.LiteralLabels.Contains("원본 ID: " + card.RawId), "raw ID is rendered literally without replacing event-result identifiers or markup");
        Check(!DiceMaster.EmbeddedTranslations.Display.TranslatedLabels.Exists(text => text.Contains(card.RawId, StringComparison.Ordinal)), "raw ID never enters translation/logger label boundary");
        Check(((GUIStyle)Field(renderer, "literalStyle")).richText == false, "raw ID style disables rich-text interpretation");

        renderer.Handle(LogPadCommand.NextTab);
        renderer.Handle(LogPadCommand.NextTab);
        Publish(model, "이벤트", Item("event", new object()));
        renderer.ResetInputFocus();
        renderer.Handle(LogPadCommand.Activate);
        renderer.Handle(LogPadCommand.Activate);
        model.MarkPublicationFailure("이벤트");
        renderer.Handle(LogPadCommand.Down);
        Check(!model.Tab("이벤트").ExecutionFocus && Area(renderer) == "List", "failed publication cannot retain reservation authority");
        string summary = (string)typeof(LogRenderer).GetMethod("Summary", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(renderer, null);
        Check(summary.Contains("게시 실패 · 이전 값", StringComparison.Ordinal) && summary.Contains("(예약 불가)", StringComparison.Ordinal), "event publication failure reason and disabled reservation remain visible in summary");
        foreach (string tab in new[] { "선택지", "조건", "미션" })
        {
            while (renderer.CurrentTab != tab) renderer.Handle(LogPadCommand.NextTab);
            Publish(model, tab, Item("failed-publication-" + tab, new object()));
            model.MarkPublicationFailure(tab);
            summary = (string)typeof(LogRenderer).GetMethod("Summary", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(renderer, null);
            Check(summary.Contains("게시 실패 · 이전 값", StringComparison.Ordinal)
                && (tab != "선택지" ? summary.Contains("(예약 불가)", StringComparison.Ordinal) : !summary.Contains("예약", StringComparison.Ordinal)),
                tab + ": failed publication explains retained data and mentions reservation only for tabs that support it");
        }
    }

    private static void RendererLocalizedTitlesAndLiteralIdentities()
    {
        string[,] results =
        {
            { "成功", "성공" }, { "大成功", "대성공" }, { "失敗", "실패" }, { "大失敗", "대실패" },
            { "正常", "일반" }, { "特殊", "특수" }, { "其他", "기타" }
        };
        int savedReservations = LogBridge.Reservations;
        object savedTarget = LogBridge.LastTarget;
        long savedContext = LogBridge.LastReservedContext;
        string savedTab = LogBridge.LastReservedTab, savedId = LogBridge.LastReservedId;
        try
        {
            for (int result = 0; result < results.GetLength(0); result++)
            {
                string originalTitle = "P_" + results[result, 0] + "_42";
                string translatedTitle = "P_" + results[result, 1] + "_42";
                foreach (string tab in new[] { "조건", "이벤트", "미션" })
                    foreach (bool titleFallback in new[] { false, true })
                    {
                        const long context = 951;
                        string identity = "P_" + results[result, 0] + "_7001";
                        string raw = "P_" + results[result, 0] + "_9001<raw>";
                        object target = new object();
                        var row = new LogItem
                        {
                            Id = identity, RawId = raw, Title = titleFallback ? "  " : originalTitle,
                            Text = titleFallback ? originalTitle : "별도 본문", Description = "실제 상세 설명", Target = target
                        };
                        if (tab == "조건") row.ConditionDetails = new LogConditionDetails("", new[] {
                            new LogConditionBranch("분기 1", "조건", "결과", "1", target)
                        });
                        var status = new LogItem
                        {
                            Id = "status", RawId = "P_" + results[result, 0] + "_9002<raw>",
                            Title = originalTitle, IsStatusOnly = true
                        };
                        var model = new LogModel(); model.ResetContext(context);
                        Publish(model, tab, row, status);
                        var renderer = new LogRenderer(model, null, null);
                        renderer.Handle(LogPadCommand.None);
                        while (renderer.CurrentTab != tab) renderer.Handle(LogPadCommand.NextTab);
                        renderer.Handle(LogPadCommand.Activate);
                        Repaint(renderer);
                        string scenario = tab + "/" + results[result, 0] + (titleFallback ? "/Text fallback" : "/Title");
                        var list = GUILayout.ScrollViews[0];
                        var detail = GUILayout.ScrollViews[1];
                        Check(list.Controls.Exists(control => control.Button && control.Enabled && control.Text.Contains(translatedTitle, StringComparison.Ordinal))
                            && !list.Controls.Exists(control => control.Button && control.Enabled && control.Text.Contains(originalTitle, StringComparison.Ordinal)),
                            scenario + ": the selectable list title uses the same Korean display result as its detail");
                        Check(detail.Controls.Exists(control => !control.Button && control.Text == translatedTitle),
                            scenario + ": the selected detail title is localized");
                        Check(detail.Controls.Exists(control => !control.Button && control.Text == "원본 ID: " + raw),
                            scenario + ": detail raw identity remains literal, including markup characters");
                        string escapedStatusRaw = status.RawId.Replace("<", "＜").Replace(">", "＞");
                        Check(list.Controls.Exists(control => control.Button && !control.Enabled
                            && control.Text.Contains(translatedTitle, StringComparison.Ordinal)
                            && control.Text.Contains("원본 ID: " + escapedStatusRaw, StringComparison.Ordinal)),
                            scenario + ": disabled information translates its title while preserving the raw identity embedded in its caption");
                        Check(model.Tab(tab).SelectedId == identity && ReferenceEquals(model.Tab(tab).Selected.Target, target)
                            && row.RawId == raw && row.Title == (titleFallback ? "  " : originalTitle)
                            && row.Text == (titleFallback ? originalTitle : "별도 본문"),
                            scenario + ": localization cannot mutate publication fields, selected identity or target");
                        int before = LogBridge.Reservations;
                        if (tab == "조건") renderer.Handle(LogPadCommand.Right);
                        renderer.Handle(LogPadCommand.Activate);
                        Check(LogBridge.Reservations == before + 1 && LogBridge.LastReservedContext == context
                                && LogBridge.LastReservedTab == tab && LogBridge.LastReservedId == identity
                                && ReferenceEquals(LogBridge.LastTarget, target),
                                scenario + ": an actual renderer reservation retains the original context, tab, identity and target");
                    }

                foreach (string tab in new[] { "선택지", "조건", "이벤트", "미션" })
                {
                    string raw = "P_" + results[result, 0] + "_9003<raw>";
                    var row = new LogItem { Id = "raw-only", RawId = raw, Title = null, Text = "  " };
                    var model = new LogModel(); model.ResetContext(952); Publish(model, tab, row);
                    var renderer = new LogRenderer(model, null, null); renderer.Handle(LogPadCommand.None);
                    while (renderer.CurrentTab != tab) renderer.Handle(LogPadCommand.NextTab);
                    if (tab != "선택지") renderer.Handle(LogPadCommand.Activate);
                    Repaint(renderer);
                    string escaped = raw.Replace("<", "＜").Replace(">", "＞");
                    var list = GUILayout.ScrollViews[0];
                    Check(list.Controls.Exists(control => control.Button == (tab != "선택지") && control.Text.Contains(escaped, StringComparison.Ordinal))
                        && !list.Controls.Exists(control => control.Text.Contains("P_" + results[result, 1] + "_9003", StringComparison.Ordinal)),
                        tab + "/" + results[result, 0] + ": an absent display title uses escaped raw identity without localizing it");
                    if (tab != "선택지")
                        Check(GUILayout.ScrollViews[1].Controls.Exists(control => !control.Button && control.Text == escaped),
                            tab + "/" + results[result, 0] + ": detail raw-title fallback follows the same literal rule as the list");
                }

                foreach (bool textPreferred in new[] { false, true })
                {
                    var row = new LogItem
                    {
                        Id = "choice-localized", RawId = "P_" + results[result, 0] + "_9004<raw>",
                        Title = textPreferred ? "별도 제목" : originalTitle,
                        Text = textPreferred ? originalTitle : null, Description = "읽기 전용 선택지 비교"
                    };
                    var model = new LogModel(); model.ResetContext(953); Publish(model, "선택지", row);
                    var renderer = new LogRenderer(model, null, null); renderer.Handle(LogPadCommand.None);
                    Repaint(renderer);
                    var body = GUILayout.ScrollViews[0];
                    Check(body.Controls.Exists(control => !control.Button && control.Text == translatedTitle)
                        && !body.Controls.Exists(control => control.Button),
                        "선택지/" + results[result, 0] + (textPreferred ? "/Text" : "/Title fallback") + ": read-only headings translate without introducing selection");
                    Check(body.Controls.Exists(control => !control.Button && control.Text == "원본 ID: " + row.RawId)
                        && model.Tab("선택지").SelectedId == null,
                        "선택지/" + results[result, 0] + ": original identity remains literal and the card remains unselected");
                }
            }

            foreach (string tab in new[] { "선택지", "조건", "이벤트", "미션" })
            {
                var row = new LogItem { Id = "null-display", RawId = null, Title = null, Text = null };
                var model = new LogModel(); model.ResetContext(954); Publish(model, tab, row);
                var renderer = new LogRenderer(model, null, null); renderer.Handle(LogPadCommand.None);
                while (renderer.CurrentTab != tab) renderer.Handle(LogPadCommand.NextTab);
                if (tab != "선택지") renderer.Handle(LogPadCommand.Activate);
                Repaint(renderer);
                Check(row.RawId == null && row.Title == null && row.Text == null
                    && (tab == "선택지" ? model.Tab(tab).SelectedId == null : model.Tab(tab).SelectedId == row.Id),
                    tab + ": null display fields render safely without rewriting source data or selection identity");
            }
        }
        finally
        {
            LogBridge.Reservations = savedReservations; LogBridge.LastTarget = savedTarget;
            LogBridge.LastReservedContext = savedContext; LogBridge.LastReservedTab = savedTab; LogBridge.LastReservedId = savedId;
        }
    }

    private static void Repaint(LogRenderer renderer)
    {
        Event.current = new Event { type = EventType.Layout, rawType = EventType.Layout };
        renderer.Draw();
        Event.current = new Event { type = EventType.Repaint, rawType = EventType.Repaint };
        renderer.Draw();
    }

    private static GUILayout.ScrollViewRecord ScrollWithButton(string text)
    {
        foreach (GUILayout.ScrollViewRecord scroll in GUILayout.ScrollViews)
            if (scroll.Controls.Exists(control => control.Button && control.Text.Contains(text, StringComparison.Ordinal))) return scroll;
        throw new InvalidOperationException("Renderer did not draw expected scroll button: " + text);
    }

    private static void FocusVisible(LogRenderer renderer, LogModel model, string tab, string text, string message)
    {
        // A follow request may be applied after drawing the scroll region. A
        // second render must receive the updated scroll rather than overwrite it
        // with BeginScrollView's value from the preceding frame.
        Repaint(renderer);
        Repaint(renderer);
        GUILayout.ScrollViewRecord scroll = ScrollWithButton(text);
        GUILayout.ControlRecord control = scroll.Controls.Find(item => item.Button && item.Text.Contains(text, StringComparison.Ordinal));
        float offset = model.Tab(tab).ListScroll;
        Check(scroll.ContentHeight > scroll.Viewport.height * 2, message + ": fixture contains genuinely overflowing, positioned content");
        Check(Math.Abs(scroll.InputScroll.y - offset) < .01f, message + ": requested scroll reaches following frame");
        Check(control.Rect.y >= offset - .01f && control.Rect.yMax <= offset + scroll.Viewport.height + .01f,
            message + ": focused header stays within its actual scroll viewport (row=" + control.Rect.y + "+" + control.Rect.height + ", scroll=" + offset + ", viewport=" + scroll.Viewport.height + ")");
    }

    private static LogItem[] LongRows(int count)
    {
        var rows = new LogItem[count];
        for (int index = 0; index < rows.Length; index++)
        {
            rows[index] = Item("entry-" + index.ToString("D2"), new object());
            rows[index].Title = "목록 항목 " + index.ToString("D2") + new string('긴', index % 4 * 15);
            rows[index].Text = "비교 카드 " + index.ToString("D2") + new string('문', index % 3 * 20);
            rows[index].Description = new string('설', 500 + index * 30);
        }
        return rows;
    }

    private static void LayoutGeometry()
    {
        int previousFont = 0;
        foreach (int[] screen in new[] { new[] { 1280, 720 }, new[] { 1920, 1080 }, new[] { 2560, 1440 }, new[] { 3840, 2160 } })
        {
            Rect window = LogLayout.DefaultWindow(screen[0], screen[1]);
            int font = LogLayout.DefaultFont(screen[1]);
            Check(font >= previousFont && font >= LogLayout.MinimumFont, "default font scales up with resolution");
            Check(window.x >= 0 && window.y >= 0 && window.x + window.width <= screen[0] && window.yMax <= screen[1],
                "scaled default window stays entirely inside display");
            Check(window.width >= Math.Min(screen[0], 540) && Math.Abs(window.height - screen[1] * .8f) < .01f, "default keeps a readable width and uses 80 percent screen height");
            previousFont = font;
        }
        Check(LogLayout.IsDefaultSeed(new Rect(20, 20, 750, 750), 14), "unchanged alpha1 default is eligible for readable migration");
        Check(!LogLayout.IsDefaultSeed(new Rect(21, 20, 750, 750), 14) && !LogLayout.IsDefaultSeed(new Rect(20, 20, 750, 750), 15),
            "custom position or font does not get silently migrated");

        foreach (int font in new[] { 12, 20, 24, 32 })
        {
            Rect window = new Rect(0, 0, 1200, 900);
            LogLayout ordinary = LogLayout.Calculate(window, font, font + 8, 0, font + 8);
            window.height += 173;
            LogLayout taller = LogLayout.Calculate(window, font, font + 8, 0, font + 8);
            Check(Math.Abs(taller.Content.height - ordinary.Content.height - 173) < .01f,
                "every extra window pixel goes into the content viewport");
            Check(taller.Content.y == ordinary.Content.y && taller.Footer.yMax <= window.height,
                "resizing preserves chrome and keeps footer inside window");
            Check(ordinary.Tabs.yMax <= ordinary.Status.y && ordinary.Status.yMax <= ordinary.Content.y && ordinary.Content.yMax <= ordinary.Footer.y,
                "tabs/status/content/footer occupy ordered non-overlapping regions");
            LogLayout hint = LogLayout.Calculate(window, font, font + 8, font * 2, font + 8);
            Check(Math.Abs(taller.Content.height - hint.Content.height - hint.InputStatus.height - hint.Gap) < .01f,
                "wrapped input hint consumes its measured height without an estimated content offset");
        }

        // Independent visibility/bounds invariants for many row sizes and actual
        // viewport sizes. This catches an estimated window-height threshold even
        // when an example's first or last row happens to align at zero.
        var random = new Random(4024);
        for (int index = 0; index < 200; index++)
        {
            float viewport = random.Next(40, 800), content = viewport + random.Next(200, 8000);
            float rowHeight = random.Next(10, (int)viewport);
            float rowY = random.Next(0, (int)(content - rowHeight));
            float initial = random.Next(0, (int)(content - viewport));
            var row = new Rect(0, rowY, 200, rowHeight);
            float followed = LogLayout.BringIntoView(initial, row, viewport, content);
            Check(followed >= 0 && followed <= content - viewport, "follow scroll remains within actual content bounds");
            Check(row.y >= followed - .01f && row.yMax <= followed + viewport + .01f, "follow makes complete target header visible");
            if (row.y >= initial && row.yMax <= initial + viewport)
                Check(followed == initial, "already visible target does not disturb manual scroll");
        }
        Check(LogLayout.BringIntoView(0, new Rect(0, 500, 200, 1500), 300, 2500) == 500,
            "oversized row aligns its beginning, not its unreachable end");
        Check(LogLayout.BringIntoView(10000, new Rect(0, 20, 200, 30), 300, 100) == 0,
            "stale scroll is clamped after content shrinks below the viewport");
        LogLayout roomy = LogLayout.Calculate(new Rect(0, 0, 1200, 900), 20, 32, 32, 32, 150);
        LogLayout pressured = LogLayout.Calculate(new Rect(0, 0, 540, 440), 18, 60, 1000, 120, 150);
        Check(!roomy.CompactChrome && pressured.CompactChrome, "chrome compacts only when fixed information would crowd the usable body");
        Check(pressured.Content.height >= 150 && pressured.Footer.yMax <= 440,
            "pressure layout retains readable recovery viewport and bounded footer");
    }

    private static void RendererViewportAndFocus()
    {
        var config = DiceMaster.Config.ConfigManager.LogFontSize;
        int savedFont = config.Value;
        float savedWidth = DiceMaster.Config.ConfigManager.LogWindowWidth.Value;
        float savedHeight = DiceMaster.Config.ConfigManager.LogWindowHeight.Value;
        int screenWidth = Screen.width, screenHeight = Screen.height;
        try
        {
            // A readable chosen font must not silently shrink to make chrome
            // fit. Narrow/short windows should use the same text size while the
            // content viewport adapts to the space left by the fixed controls.
            config.Value = 24;
            foreach (int[] size in new[] { new[] { 1280, 720 }, new[] { 1920, 1080 }, new[] { 2560, 1440 } })
            {
                Screen.width = size[0]; Screen.height = size[1];
                DiceMaster.Config.ConfigManager.LogWindowWidth.Value = size[0] * .78f;
                DiceMaster.Config.ConfigManager.LogWindowHeight.Value = size[1] * .88f;
                var model = new LogModel(); model.ResetContext(size[1]);
                LogItem[] rows = LongRows(30);
                Publish(model, "선택지", rows);
                Publish(model, "이벤트", rows);
                var renderer = new LogRenderer(model, null, null);
                renderer.Handle(LogPadCommand.None);
                renderer.Handle(LogPadCommand.Activate);
                Repaint(renderer);
                Check(((GUIStyle)Field(renderer, "textStyle")).fontSize == 24, "requested readable font survives " + size[0] + "x" + size[1]);
                Check(GUILayout.ScrollViews.Count == 1 && GUILayout.ScrollViews[0].Viewport.height > renderer.WindowRect.height * .55f,
                    "cards consume most of the remaining window height at " + size[0] + "x" + size[1]);

                renderer.Handle(LogPadCommand.NextTab);
                renderer.Handle(LogPadCommand.NextTab);
                renderer.Handle(LogPadCommand.Activate);
                Repaint(renderer);
                Check(GUILayout.ScrollViews.Count == 2, "list and selected details each have independent scroll panes");
                float listHeight = GUILayout.ScrollViews[0].Viewport.height, detailHeight = GUILayout.ScrollViews[1].Viewport.height;
                Check(listHeight > renderer.WindowRect.height * .45f && detailHeight > renderer.WindowRect.height * .40f,
                    "event list/details fill their allocated content area at " + size[0] + "x" + size[1]);
                Check(Math.Abs(listHeight - detailHeight) < 100, "reservation control consumes only its own space instead of collapsing detail viewport");
            }

            Screen.width = 1920; Screen.height = 1080;
            DiceMaster.Config.ConfigManager.LogWindowWidth.Value = 1200;
            DiceMaster.Config.ConfigManager.LogWindowHeight.Value = 900;
            var focusModel = new LogModel(); focusModel.ResetContext(100);
            LogItem[] focusRows = LongRows(30);
            Publish(focusModel, "이벤트", focusRows);
            Publish(focusModel, "선택지", focusRows);
            var focusRenderer = new LogRenderer(focusModel, null, null);
            focusRenderer.Handle(LogPadCommand.None);
            focusRenderer.Handle(LogPadCommand.NextTab);
            focusRenderer.Handle(LogPadCommand.NextTab);
            focusRenderer.Handle(LogPadCommand.Activate);
            FocusVisible(focusRenderer, focusModel, "이벤트", "목록 항목 00", "first list item");
            for (int index = 0; index < 14; index++) focusRenderer.Handle(LogPadCommand.Down);
            FocusVisible(focusRenderer, focusModel, "이벤트", "목록 항목 14", "middle list item moving down");
            for (int index = 0; index < 15; index++) focusRenderer.Handle(LogPadCommand.Down);
            FocusVisible(focusRenderer, focusModel, "이벤트", "목록 항목 29", "last list item moving down");
            for (int index = 0; index < 18; index++) focusRenderer.Handle(LogPadCommand.Up);
            FocusVisible(focusRenderer, focusModel, "이벤트", "목록 항목 11", "middle list item moving up");
            for (int index = 0; index < 11; index++) focusRenderer.Handle(LogPadCommand.Up);
            FocusVisible(focusRenderer, focusModel, "이벤트", "목록 항목 00", "first list item moving up");

            Array.Reverse(focusRows);
            Publish(focusModel, "이벤트", focusRows);
            Check(focusModel.Tab("이벤트").SelectedId == "entry-00", "same-context reorder preserves selected stable ID");
            FocusVisible(focusRenderer, focusModel, "이벤트", "목록 항목 00", "selected item moved by automatic publication");
            Rect resizedWindow = focusRenderer.WindowRect;
            resizedWindow.height = 600;
            SetField(focusRenderer, "window", resizedWindow);
            FocusVisible(focusRenderer, focusModel, "이벤트", "목록 항목 00", "selected item after viewport height shrinks");
            config.Value = 32;
            FocusVisible(focusRenderer, focusModel, "이벤트", "목록 항목 00", "selected item after text size grows");
            Check(((GUIStyle)Field(focusRenderer, "textStyle")).fontSize == 32, "larger saved font is drawn without hidden window-size cap");
            resizedWindow.height = 900;
            SetField(focusRenderer, "window", resizedWindow);
            config.Value = 24;
            Array.Reverse(focusRows);

            // Choices are a continuous reading surface. Directions scroll the
            // text without manufacturing a selected/focus-follow header.
            focusRenderer.Handle(LogPadCommand.PreviousTab);
            focusRenderer.Handle(LogPadCommand.PreviousTab);
            for (int index = 0; index < 9; index++) focusRenderer.Handle(LogPadCommand.Down);
            Repaint(focusRenderer); Repaint(focusRenderer);
            float bodyOffset = focusModel.Tab("선택지").ListScroll;
            Check(bodyOffset > 0 && focusModel.Tab("선택지").SelectedId == null,
                "choice directions scroll the complete measured bodies without selecting a header");
            focusRenderer.Handle(LogPadCommand.Activate);
            focusRenderer.Handle(LogPadCommand.Right);
            focusRenderer.Handle(LogPadCommand.Left);
            Check(focusModel.Tab("선택지").ListScroll == bodyOffset && focusModel.Tab("선택지").SelectedId == null,
                "choice activation and horizontal navigation leave the reading position unchanged");
            focusRenderer.Scroll(1);
            float manualScroll = focusModel.Tab("선택지").ListScroll;
            Array.Reverse(focusRows);
            Publish(focusModel, "선택지", focusRows);
            Repaint(focusRenderer); Repaint(focusRenderer);
            Check(focusModel.Tab("선택지").ListScroll == manualScroll, "choice reading scroll survives automatic publication without forced following");
            focusRenderer.Handle(LogPadCommand.Down);
            Check(focusModel.Tab("선택지").ListScroll > manualScroll && focusModel.Tab("선택지").SelectedId == null,
                "fresh choice direction continues scrolling from current reading position");

            focusRenderer.Handle(LogPadCommand.Tools);
            Rect toolsWindow = focusRenderer.WindowRect;
            toolsWindow.height = 600;
            SetField(focusRenderer, "window", toolsWindow);
            for (int index = 0; index < 16; index++) focusRenderer.Handle(LogPadCommand.Down);
            Repaint(focusRenderer); Repaint(focusRenderer);
            GUILayout.ScrollViewRecord tools = ScrollWithButton("로그 창 닫기");
            GUILayout.ControlRecord close = tools.Controls.Find(control => control.Button && control.Text.Contains("로그 창 닫기", StringComparison.Ordinal));
            Check(close.Rect.y >= tools.InputScroll.y && close.Rect.yMax <= tools.InputScroll.y + tools.Viewport.height + .01f,
                "last tools button follows into tools viewport");
            Check(tools.InputScroll.y > 0, "last tool is reached by actual scrolling, not zero-position fixture rows");
            for (int index = 0; index < 16; index++) focusRenderer.Handle(LogPadCommand.Up);
            Repaint(focusRenderer); Repaint(focusRenderer);
            tools = ScrollWithButton("게임 데이터 새로고침");
            Check(tools.InputScroll.y == 0, "returning to first tool restores top of viewport");
        }
        finally
        {
            config.Value = savedFont;
            DiceMaster.Config.ConfigManager.LogWindowWidth.Value = savedWidth;
            DiceMaster.Config.ConfigManager.LogWindowHeight.Value = savedHeight;
            Screen.width = screenWidth; Screen.height = screenHeight;
        }
    }

    private static void RendererCompactAndPointerScroll()
    {
        int fontBefore = DiceMaster.Config.ConfigManager.LogFontSize.Value;
        float xBefore = DiceMaster.Config.ConfigManager.LogWindowX.Value, yBefore = DiceMaster.Config.ConfigManager.LogWindowY.Value;
        float widthBefore = DiceMaster.Config.ConfigManager.LogWindowWidth.Value, heightBefore = DiceMaster.Config.ConfigManager.LogWindowHeight.Value;
        int screenWidth = Screen.width, screenHeight = Screen.height;
        GUIStyle[] skinStyles = { GUI.skin.label, GUI.skin.button, GUI.skin.box, GUI.skin.window };
        var originalMargins = new List<RectOffset>();
        object reservationBefore = LogBridge.LastTarget;
        foreach (GUIStyle style in skinStyles) { originalMargins.Add(style.margin); style.margin = new RectOffset(3, 5, 7, 9); }
        try
        {
            Screen.width = 1920; Screen.height = 1080;
            foreach (int[] setting in new[] { new[] { 540, 440, 32 }, new[] { 1200, 860, 64 }, new[] { 540, 440, 64 } })
            {
                DiceMaster.Config.ConfigManager.LogWindowX.Value = DiceMaster.Config.ConfigManager.LogWindowY.Value = 20;
                DiceMaster.Config.ConfigManager.LogWindowWidth.Value = setting[0];
                DiceMaster.Config.ConfigManager.LogWindowHeight.Value = setting[1];
                DiceMaster.Config.ConfigManager.LogFontSize.Value = setting[2];
                var model = new LogModel(); model.ResetContext(200);
                Publish(model, "선택지", LongRows(30));
                var renderer = new LogRenderer(model, null, null) { InputHint = new string('긴', 180) + "\n" + new string('안', 120) };
                renderer.Handle(LogPadCommand.None);
                renderer.Handle(LogPadCommand.Tools);
                for (int index = 0; index < 11; index++) renderer.Handle(LogPadCommand.Down);
                Repaint(renderer); Repaint(renderer);
                var layout = (LogLayout)Field(renderer, "layout");
                Check(((GUIStyle)Field(renderer, "textStyle")).fontSize == setting[2], "compact chrome preserves explicitly chosen large body font");
                if (setting[0] == 540) Check(layout.CompactChrome, "multiline status compacts when the small window is under pressure");
                bool zeroMargins = true;
                foreach (FieldInfo field in typeof(LogRenderer).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                    if (field.FieldType == typeof(GUIStyle))
                    {
                        var style = (GUIStyle)field.GetValue(renderer);
                        zeroMargins &= style != null && style.margin != null && style.margin.horizontal == 0 && style.margin.vertical == 0;
                    }
                Check(zeroMargins, "created styles clear inherited skin margins so fixed regions retain their allocated height");
                Check(layout.Footer.yMax <= renderer.WindowRect.height && layout.Content.yMax <= layout.Footer.y,
                    "large font and multiline status keep footer/content inside the window");
                GUILayout.ScrollViewRecord tools = ScrollWithButton("기본값 복구");
                GUILayout.ControlRecord restore = tools.Controls.Find(control => control.Button && control.Text.Contains("기본값 복구", StringComparison.Ordinal));
                Check((layout.CompactChrome ? tools.Controls : GUILayout.Controls).Exists(control => control.Text == renderer.InputHint),
                    "status remains available in active scroll content when compact or fixed chrome when roomy");
                Check(tools.Viewport.height >= restore.Rect.height,
                    "compact chrome leaves enough viewport to show a whole restore control at " + setting[0] + "x" + setting[1] + " font " + setting[2]);
                Check(restore.Rect.y >= tools.InputScroll.y - .01f && restore.Rect.yMax <= tools.InputScroll.y + tools.Viewport.height + .01f,
                    "large-font restore control can be navigated fully into view");
                renderer.Handle(LogPadCommand.Activate);
                Repaint(renderer);
                Check(DiceMaster.Config.ConfigManager.LogFontSize.Value == LogLayout.DefaultFont(Screen.height)
                    && renderer.WindowRect.width == LogLayout.DefaultWindow(Screen.width, Screen.height).width,
                    "pad can restore readable window/font defaults from pressure layout");
            }

            foreach (string tab in new[] { "이벤트", "미션" })
            {
                DiceMaster.Config.ConfigManager.LogWindowWidth.Value = 540;
                DiceMaster.Config.ConfigManager.LogWindowHeight.Value = 440;
                DiceMaster.Config.ConfigManager.LogFontSize.Value = 64;
                var reservationModel = new LogModel(); reservationModel.ResetContext(203);
                LogItem reservationItem = Item("reserved-target", new object());
                Publish(reservationModel, tab, reservationItem);
                LogBridge.SetReservation(tab, reservationItem.Target);
                var reservationRenderer = new LogRenderer(reservationModel, null, null);
                reservationRenderer.Handle(LogPadCommand.None);
                for (int index = 0; index < (tab == "조건" ? 1 : tab == "이벤트" ? 2 : 3); index++) reservationRenderer.Handle(LogPadCommand.NextTab);
                reservationRenderer.Handle(LogPadCommand.Activate);
                reservationRenderer.Handle(LogPadCommand.Down);
                Repaint(reservationRenderer); Repaint(reservationRenderer);
                GUILayout.ScrollViewRecord detailPane = GUILayout.ScrollViews[1];
                GUILayout.ControlRecord status = detailPane.Controls.Find(control => !control.Button && control.Text.Contains("활성화 (예약 중)", StringComparison.Ordinal));
                var reservationLayout = (LogLayout)Field(reservationRenderer, "layout");
                var boxStyle = (GUIStyle)Field(reservationRenderer, "boxStyle");
                Check(status != null && status.Text.Contains("예약 해제 가능", StringComparison.Ordinal), "selected " + tab + " has a noninteractive reserved/releasable status inside its detail scroll");
                Check(!GUILayout.Controls.Exists(control => control.Button && control.Text.Contains(tab == "이벤트" ? "이벤트 우선 예약" : "미션 조건 통과 예약", StringComparison.Ordinal)),
                    "there is no separate detail reservation control to steal vertical room or focus");
                Check(status.FontSize == ((GUIStyle)Field(reservationRenderer, "statusStyle")).fontSize
                    && ((GUIStyle)Field(reservationRenderer, "textStyle")).fontSize == 64,
                    "status retains the readable status font while preserving the requested large body font");
                Check(detailPane.Viewport.height > 0 && detailPane.Controls.Exists(control => control.Text == reservationItem.Description),
                    "noninteractive status and description share a usable independently scrollable detail body");
                Check(Math.Abs(detailPane.Viewport.yMax + boxStyle.padding.bottom - reservationLayout.Content.height) < .01f,
                    "detail scroll fills the content down to its bottom padding without a separate action row");
            }

            DiceMaster.Config.ConfigManager.LogWindowWidth.Value = 1200;
            DiceMaster.Config.ConfigManager.LogWindowHeight.Value = 900;
            DiceMaster.Config.ConfigManager.LogFontSize.Value = 24;
            var pointerModel = new LogModel(); pointerModel.ResetContext(201);
            LogItem[] cards = LongRows(30);
            Publish(pointerModel, "선택지", cards);
            var pointerRenderer = new LogRenderer(pointerModel, null, null);
            pointerRenderer.Handle(LogPadCommand.None);
            pointerRenderer.Handle(LogPadCommand.Activate);
            Repaint(pointerRenderer); Repaint(pointerRenderer);
            GUILayout.ScrollInput = (index, input) => index == 0 ? new Vector2(input.x, 220) : input;
            var pointerLayout = (LogLayout)Field(pointerRenderer, "layout");
            Event.current = new Event { type = EventType.MouseDrag, rawType = EventType.MouseDrag,
                mousePosition = new Vector2(pointerLayout.Content.x + pointerLayout.Content.width - 4, pointerLayout.Content.y + 50) };
            pointerRenderer.Draw();
            GUILayout.ScrollInput = null;
            Check(pointerModel.Tab("선택지").ListScroll == 220, "scrollbar drag takes the scroll position returned by the Unity boundary");
            Array.Reverse(cards);
            Publish(pointerModel, "선택지", cards);
            Repaint(pointerRenderer); Repaint(pointerRenderer);
            Check(pointerModel.Tab("선택지").ListScroll == 220, "scrollbar drag stays in ownership through card reflow");
            pointerRenderer.Handle(LogPadCommand.Down);
            Check(pointerModel.Tab("선택지").SelectedId == null && pointerModel.Tab("선택지").ListScroll > 220,
                "pad direction continues choice reading scroll from the mouse scrollbar position without header following");
            GUILayout.ScrollInput = (index, input) => index == 0 ? new Vector2(input.x, 330) : input;
            Event.current = new Event { type = EventType.MouseDrag, rawType = EventType.MouseDrag, mousePosition = new Vector2(-100, -100) };
            pointerRenderer.Draw();
            GUILayout.ScrollInput = null;
            Array.Reverse(cards);
            Publish(pointerModel, "선택지", cards);
            Repaint(pointerRenderer); Repaint(pointerRenderer);
            Check(pointerModel.Tab("선택지").ListScroll == 330,
                "captured scrollbar thumb drag retains manual ownership when pointer leaves the viewport");

            var detailModel = new LogModel(); detailModel.ResetContext(202);
            LogItem[] events = LongRows(30);
            Publish(detailModel, "이벤트", events);
            var detailRenderer = new LogRenderer(detailModel, null, null);
            detailRenderer.Handle(LogPadCommand.None);
            detailRenderer.Handle(LogPadCommand.NextTab);
            detailRenderer.Handle(LogPadCommand.NextTab);
            detailRenderer.Handle(LogPadCommand.Activate);
            Repaint(detailRenderer); Repaint(detailRenderer);
            GUILayout.ScrollInput = (index, input) => index == 1 ? new Vector2(input.x, 240) : input;
            var detailLayout = (LogLayout)Field(detailRenderer, "layout");
            Event.current = new Event { type = EventType.ScrollWheel, rawType = EventType.ScrollWheel,
                mousePosition = new Vector2(detailLayout.Content.x + detailLayout.Content.width * .75f, detailLayout.Content.y + 150) };
            detailRenderer.Draw();
            GUILayout.ScrollInput = null;
            Check(detailModel.Tab("이벤트").DetailScroll == 240 && detailModel.Tab("이벤트").ListScroll == 0,
                "wheel input in detail scrolls only the detail pane");
            Array.Reverse(events);
            Publish(detailModel, "이벤트", events);
            FocusVisible(detailRenderer, detailModel, "이벤트", "목록 항목 00", "detail wheel does not suppress later list focus follow");
        }
        finally
        {
            GUILayout.ScrollInput = null;
            LogBridge.LastTarget = reservationBefore;
            DiceMaster.Config.ConfigManager.LogFontSize.Value = fontBefore;
            DiceMaster.Config.ConfigManager.LogWindowX.Value = xBefore;
            DiceMaster.Config.ConfigManager.LogWindowY.Value = yBefore;
            DiceMaster.Config.ConfigManager.LogWindowWidth.Value = widthBefore;
            DiceMaster.Config.ConfigManager.LogWindowHeight.Value = heightBefore;
            Screen.width = screenWidth; Screen.height = screenHeight;
            for (int index = 0; index < skinStyles.Length; index++) skinStyles[index].margin = originalMargins[index];
        }
    }

    private static void WrappingLayoutBoundary()
    {
        var style = new GUIStyle { fontSize = 24, wordWrap = true, padding = new RectOffset(6, 6, 6, 6) };
        const string caption = "여러 줄로 읽어야 하는 미션 제목과 상태\n설명 조회 대기";
        float required = style.CalcHeight(new GUIContent(caption), 140);
        GUILayout.ResetLayout(140, 900);
        GUILayout.Button(caption, style, GUILayout.Width(140), GUILayout.MinHeight(40));
        Check(GUILayout.LastRect.height == 40 && required > 40,
            "negative wrapping control: Unity forced MinHeight/maxHeight leaves multi-line text clipped");
        GUILayout.ResetLayout(140, 900);
        GUILayout.Button(caption, style, GUILayout.Width(140));
        Check(GUILayout.LastRect.height >= required, "unforced wrapping control uses its full measured text height");
        var bold = new GUIStyle(style) { fontStyle = FontStyle.Bold };
        Check(bold.CalcHeight(new GUIContent("가나다라마바사아"), 160) > style.CalcHeight(new GUIContent("가나다라마바사아"), 160),
            "fixture distinguishes selected bold wrapping from ordinary text wrapping");
        var inherited = new GUIStyle(style) { fixedHeight = 32 };
        GUILayout.ResetLayout(140, 900);
        GUILayout.Button(caption, inherited, GUILayout.Width(140));
        Check(GUILayout.LastRect.height == 32 && required > 32, "negative fixed-skin control models inherited single-line height");
    }

    private static void ConfigureLayout(Rect rect, int font)
    {
        DiceMaster.Config.ConfigManager.LogWindowX.Value = rect.x;
        DiceMaster.Config.ConfigManager.LogWindowY.Value = rect.y;
        DiceMaster.Config.ConfigManager.LogWindowWidth.Value = rect.width;
        DiceMaster.Config.ConfigManager.LogWindowHeight.Value = rect.height;
        DiceMaster.Config.ConfigManager.LogFontSize.Value = font;
    }
    private static Rect ConfiguredRect() => new Rect(DiceMaster.Config.ConfigManager.LogWindowX.Value, DiceMaster.Config.ConfigManager.LogWindowY.Value,
        DiceMaster.Config.ConfigManager.LogWindowWidth.Value, DiceMaster.Config.ConfigManager.LogWindowHeight.Value);
    private static int LayoutWrites() => DiceMaster.Config.ConfigManager.LogWindowX.Writes + DiceMaster.Config.ConfigManager.LogWindowY.Writes
        + DiceMaster.Config.ConfigManager.LogWindowWidth.Writes + DiceMaster.Config.ConfigManager.LogWindowHeight.Writes + DiceMaster.Config.ConfigManager.LogFontSize.Writes;
    private static LogRenderer FirstDrawRenderer()
    { var renderer = new LogRenderer(new LogModel(), null, null); Repaint(renderer); return renderer; }

    private static void RendererDeferredStartupLayout()
    {
        Rect saved = ConfiguredRect();
        int oldFont = DiceMaster.Config.ConfigManager.LogFontSize.Value, oldWidth = Screen.width, oldHeight = Screen.height;
        var seed = new Rect(20, 20, 872, 960);
        try
        {
            foreach (int[] startup in new[] { new[] { 640, 480 }, new[] { 1280, 720 }, new[] { 0, 0 } })
                foreach (int[] actual in new[] { new[] { 1920, 1080 }, new[] { 3840, 2160 } })
                {
                    Screen.width = startup[0]; Screen.height = startup[1];
                    ConfigureLayout(seed, 20);
                    int writes = LayoutWrites();
                    var renderer = new LogRenderer(new LogModel(), null, null);
                    Check(renderer.WindowRect == seed && ConfiguredRect() == seed && DiceMaster.Config.ConfigManager.LogFontSize.Value == 20,
                        "constructor preserves default seed before the final screen exists (startup " + startup[0] + "x" + startup[1] + ")");
                    Check(LayoutWrites() == writes, "constructor does not persist or change any layout setting from startup screen dimensions");
                    Screen.width = actual[0]; Screen.height = actual[1];
                    Repaint(renderer);
                    Rect initialized = renderer.WindowRect;
                    Check(initialized == LogLayout.DefaultWindow(Screen.width, Screen.height) && ConfiguredRect() == initialized
                        && DiceMaster.Config.ConfigManager.LogFontSize.Value == LogLayout.DefaultFont(Screen.height),
                        "first draw initializes window and font from current display rather than startup screen");
                    renderer.Handle(LogPadCommand.Tools);
                    for (int index = 0; index < 11; index++) renderer.Handle(LogPadCommand.Down);
                    renderer.Handle(LogPadCommand.Activate);
                    Repaint(renderer);
                    Check(renderer.WindowRect == initialized, "first-open default equals explicit reset at the same real display");
                    Rect stored = ConfiguredRect();
                    int storedFont = DiceMaster.Config.ConfigManager.LogFontSize.Value;
                    Screen.width = startup[0]; Screen.height = startup[1];
                    writes = LayoutWrites();
                    var restarted = new LogRenderer(new LogModel(), null, null);
                    Check(LayoutWrites() == writes && ConfiguredRect() == stored && DiceMaster.Config.ConfigManager.LogFontSize.Value == storedFont,
                        "restart constructor preserves the stored physical layout through a transient startup resolution");
                    Screen.width = actual[0]; Screen.height = actual[1];
                    Repaint(restarted);
                    Check(restarted.WindowRect == stored && DiceMaster.Config.ConfigManager.LogFontSize.Value == storedFont,
                        "restart first draw restores the same saved window and font");
                }
            var custom = new Rect(120, 100, 2200, 1300);
            foreach (int[] actual in new[] { new[] { 1920, 1080 }, new[] { 3840, 2160 } })
            {
                Screen.width = 640; Screen.height = 480;
                ConfigureLayout(custom, 33);
                int writes = LayoutWrites();
                var renderer = new LogRenderer(new LogModel(), null, null);
                Check(renderer.WindowRect == custom && ConfiguredRect() == custom && LayoutWrites() == writes && DiceMaster.Config.ConfigManager.LogFontSize.Value == 33,
                    "constructor never clamps a large custom window or font to the temporary startup display");
                Screen.width = actual[0]; Screen.height = actual[1];
                Repaint(renderer);
                Check(renderer.WindowRect == LogRenderer.ConstrainWindow(custom, Screen.width, Screen.height) && DiceMaster.Config.ConfigManager.LogFontSize.Value == 33,
                    "custom layout is constrained only against the display used by first draw");
            }
            foreach (int[] previous in new[] { new[] { 750, 750, 14 }, new[] { 1200, 860, 20 }, new[] { 1280, 960, 20 }, new[] { 1152, 756, 20 } })
            {
                Screen.width = 640; Screen.height = 480;
                Rect previousSeed = new Rect(20, 20, previous[0], previous[1]);
                ConfigureLayout(previousSeed, previous[2]);
                int writes = LayoutWrites();
                var renderer = new LogRenderer(new LogModel(), null, null);
                Check(renderer.WindowRect == previousSeed && LayoutWrites() == writes && DiceMaster.Config.ConfigManager.LogFontSize.Value == previous[2],
                    "every supported historical seed remains raw until a valid first draw");
                Screen.width = 3840; Screen.height = 2160;
                Repaint(renderer);
                Check(renderer.WindowRect == LogLayout.DefaultWindow(Screen.width, Screen.height) && DiceMaster.Config.ConfigManager.LogFontSize.Value == 36,
                    "historical seed migrates using final 4K layout and font");
            }
            foreach (int[] screen in new[] { new[] { 1280, 720 }, new[] { 2560, 1440 }, new[] { 3840, 2160 } })
            {
                Screen.width = screen[0]; Screen.height = screen[1];
                float scale = Mathf.Clamp(Screen.height / 1080f, .85f, 4f);
                Rect previousScaled = LogRenderer.ConstrainWindow(new Rect(20 * scale, 20 * scale, 1280 * scale, 960 * scale), Screen.width, Screen.height);
                previousScaled.y += .005f; previousScaled.height += .005f;
                ConfigureLayout(previousScaled, (int)Math.Round(20 * scale));
                var migrated = FirstDrawRenderer();
                Check(migrated.WindowRect == LogLayout.DefaultWindow(Screen.width, Screen.height), "previous 1280 by 960 scaled default migrates after config serialization");
            }
            foreach (int[] display in new[] { new[] { 1920, 1080 }, new[] { 1920, 1200 }, new[] { 2560, 1440 }, new[] { 3840, 2160 }, new[] { 3440, 1440 } })
            {
                Screen.width = display[0]; Screen.height = display[1];
                float oldScale = Mathf.Clamp(Screen.height / 1080f, .85f, 4f);
                int oldScaledFont = (int)Math.Round(20 * oldScale);
                Rect oldRatioDefault = LogRenderer.ConstrainWindow(new Rect(20 * oldScale, 20 * oldScale,
                    (float)Math.Round(Screen.width * .6f), (float)Math.Round(Screen.height * .7f)), Screen.width, Screen.height);
                ConfigureLayout(oldRatioDefault, oldScaledFont);
                Check(FirstDrawRenderer().WindowRect == LogLayout.DefaultWindow(Screen.width, Screen.height)
                    && DiceMaster.Config.ConfigManager.LogFontSize.Value == LogLayout.DefaultFont(Screen.height),
                    "unchanged prior 60/70 percent tuple migrates coherently to the new viewport baseline");
                Rect moved = oldRatioDefault; moved.x += 1;
                ConfigureLayout(moved, oldScaledFont);
                Check(FirstDrawRenderer().WindowRect == moved, "moved old ratio window remains a custom user layout");
                Rect resized = oldRatioDefault; resized.width -= 1;
                ConfigureLayout(resized, oldScaledFont);
                Check(FirstDrawRenderer().WindowRect == resized, "resized old ratio window remains a custom user layout");
                ConfigureLayout(oldRatioDefault, oldScaledFont + 1);
                Check(FirstDrawRenderer().WindowRect == oldRatioDefault && DiceMaster.Config.ConfigManager.LogFontSize.Value == oldScaledFont + 1,
                    "custom-font old ratio window remains a custom user layout");
            }
            Screen.width = 640; Screen.height = 480;
            ConfigureLayout(seed, 20);
            var unopened = new LogRenderer(new LogModel(), null, null);
            int unopenedWrites = LayoutWrites();
            unopened.Handle(LogPadCommand.None);
            unopened.Handle(LogPadCommand.Tools);
            for (int index = 0; index < 11; index++) unopened.Handle(LogPadCommand.Down);
            unopened.Handle(LogPadCommand.Activate);
            Check(unopened.WindowRect == seed && ConfiguredRect() == seed && LayoutWrites() == unopenedWrites,
                "layout tools cannot save startup dimensions before the first visible layout initialization");
            Screen.width = 640; Screen.height = 480;
            ConfigureLayout(seed, 20);
            var delayed = new LogRenderer(new LogModel(), null, null);
            var edited = new Rect(60, 50, 1400, 900);
            ConfigureLayout(edited, 31);
            Screen.width = 1920; Screen.height = 1080;
            Repaint(delayed);
            Check(delayed.WindowRect == edited && ConfiguredRect() == edited && DiceMaster.Config.ConfigManager.LogFontSize.Value == 31,
                "first draw rereads settings changed after construction instead of overwriting them with its constructor snapshot");
            Screen.width = Screen.height = 0;
            ConfigureLayout(seed, 20);
            var unavailable = new LogRenderer(new LogModel(), null, null);
            int unchangedWrites = LayoutWrites(), windowCalls = GUILayout.WindowCalls;
            foreach (int[] invalid in new[] { new[] { 0, 0 }, new[] { 1920, 0 }, new[] { 0, 1080 }, new[] { -1, 1080 } })
            {
                Screen.width = invalid[0]; Screen.height = invalid[1];
                Repaint(unavailable);
                Check(ConfiguredRect() == seed && DiceMaster.Config.ConfigManager.LogFontSize.Value == 20 && LayoutWrites() == unchangedWrites,
                    "draw with a nonpositive screen dimension does not initialize, clamp or save settings");
                Check(GUILayout.WindowCalls == windowCalls, "unavailable display does not submit a tiny native-window boundary call");
            }
            Screen.width = 3840; Screen.height = 2160;
            Repaint(unavailable);
            Check(unavailable.WindowRect == LogLayout.DefaultWindow(Screen.width, Screen.height), "deferred initialization still runs after a usable display becomes available");
            foreach (int[] display in new[] { new[] { 1920, 1080, 872, 864, 18 }, new[] { 2560, 1440, 1163, 1152, 24 },
                new[] { 3840, 2160, 1744, 1728, 36 }, new[] { 3440, 1440, 1562, 1152, 24 }, new[] { 1920, 1200, 872, 960, 20 }, new[] { 5120, 1440, 2325, 1152, 24 } })
            {
                Rect proportional = LogLayout.DefaultWindow(display[0], display[1]);
                Check(Math.Abs(proportional.width - display[2]) < .01f && Math.Abs(proportional.height - display[3]) < .01f,
                    "default window follows the 872 by 960 reference proportions on " + display[0] + "x" + display[1]);
                Check(ContainsRect(new Rect(0, 0, display[0], display[1]), proportional), "proportional default stays inside the complete display");
                Check(LogLayout.DefaultFont(display[1]) == display[4], "font follows 20px at 1200px display height");
                Screen.width = display[0]; Screen.height = display[1];
                ConfigureLayout(seed, 20);
                Check(FirstDrawRenderer().WindowRect == proportional, "actual renderer first draw applies proportional default on this aspect ratio");
            }
        }
        finally
        {
            ConfigureLayout(saved, oldFont);
            Screen.width = oldWidth; Screen.height = oldHeight;
        }
    }

    private static void RendererDefaultsAndMeasuredRows()
    {
        var saved = new Rect(DiceMaster.Config.ConfigManager.LogWindowX.Value, DiceMaster.Config.ConfigManager.LogWindowY.Value,
            DiceMaster.Config.ConfigManager.LogWindowWidth.Value, DiceMaster.Config.ConfigManager.LogWindowHeight.Value);
        int oldFont = DiceMaster.Config.ConfigManager.LogFontSize.Value, oldWidth = Screen.width, oldHeight = Screen.height;
        GUIStyle[] skinStyles = { GUI.skin.label, GUI.skin.button, GUI.skin.box, GUI.skin.window };
        var fixedSizes = new List<Vector2>();
        foreach (GUIStyle style in skinStyles) fixedSizes.Add(new Vector2(style.fixedWidth, style.fixedHeight));
        float oldBarWidth = GUI.skin.verticalScrollbar.fixedWidth;
        RectOffset oldBarMargin = GUI.skin.verticalScrollbar.margin;
        try
        {
            Check(LogLayout.DefaultWidth == 872 && LogLayout.DefaultHeight == 960 && LogLayout.DefaultFontSize == 20, "configuration seed matches the 1920 by 1200 reference");
            foreach (int[] screen in new[] { new[] { 1280, 720 }, new[] { 1920, 1080 }, new[] { 2560, 1440 }, new[] { 3840, 2160 } })
            {
                Screen.width = screen[0]; Screen.height = screen[1];
                float scale = Mathf.Clamp(Screen.height / 1080f, .85f, 4f);
                Rect previous = LogRenderer.ConstrainWindow(new Rect(20 * scale, 20 * scale, 1200 * scale, 860 * scale), Screen.width, Screen.height);
                int previousFont = (int)Math.Round(20 * scale);
                Rect serialized = previous;
                serialized.x += .005f; serialized.width += .005f;
                ConfigureLayout(serialized, previousFont);
                var migrated = new LogRenderer(new LogModel(), null, null);
                Repaint(migrated);
                Check(migrated.WindowRect == LogLayout.DefaultWindow(Screen.width, Screen.height), "previous scaled default migrates after fractional config round-trip at " + Screen.height);
                Check(DiceMaster.Config.ConfigManager.LogWindowWidth.Value == migrated.WindowRect.width, "migrated default persists its coherent new size on first usable draw");
                Rect moved = previous; moved.x += 1;
                ConfigureLayout(moved, previousFont);
                Check(FirstDrawRenderer().WindowRect == LogRenderer.ConstrainWindow(moved, Screen.width, Screen.height), "custom physical window position survives default migration");
                Rect resized = previous; resized.width -= 1;
                ConfigureLayout(resized, previousFont);
                Check(FirstDrawRenderer().WindowRect == resized, "custom physical window size survives default migration");
                ConfigureLayout(previous, previousFont + 1);
                Check(FirstDrawRenderer().WindowRect == previous && DiceMaster.Config.ConfigManager.LogFontSize.Value == previousFont + 1,
                    "custom font makes old physical layout a preserved user setting");
            }
            Screen.width = 1920; Screen.height = 1080;
            ConfigureLayout(new Rect(20, 20, 1200, 860), 20);
            Check(FirstDrawRenderer().WindowRect.width == 872, "unscaled previous default seed migrates to proportional window on first draw");
            Check(!LogLayout.IsDefaultSeed(new Rect(20.02f, 20, 1200, 860), 20), "serialization tolerance does not classify a changed physical position as a default");
            foreach (GUIStyle style in skinStyles) { style.fixedHeight = 32; style.fixedWidth = 96; }
            GUI.skin.verticalScrollbar.fixedWidth = 20;
            GUI.skin.verticalScrollbar.margin = new RectOffset(3, 0, 0, 0);
            foreach (string tab in new[] { "이벤트", "미션" })
            {
                ConfigureLayout(new Rect(20, 20, 540, 600), 24);
                var model = new LogModel(); model.ResetContext(401);
                var rows = new LogItem[12];
                for (int index = 0; index < rows.Length; index++)
                {
                    rows[index] = Item("multi-" + index.ToString("D2"), new object());
                    rows[index].Title = "미션 제목 " + index.ToString("D2") + " " + new string('긴', 8 + index % 3 * 6);
                }
                rows[0].RefreshState = LogRefreshState.Waiting;
                rows[0].RefreshStatus = "원본 대기 사유";
                rows[1].ReadState = LogReadState.Read;
                rows[2].ReadState = LogReadState.Unread;
                rows[11].Title = rows[11].Text = "";
                rows[11].RawId = "LONG_MISSION_ID_" + new string('R', 180) + "<raw>";
                Publish(model, tab, rows);
                var renderer = new LogRenderer(model, null, null);
                renderer.Handle(LogPadCommand.None);
                for (int index = 0; index < (tab == "조건" ? 1 : tab == "이벤트" ? 2 : 3); index++) renderer.Handle(LogPadCommand.NextTab);
                renderer.Handle(LogPadCommand.Activate);
                Repaint(renderer); Repaint(renderer);
                GUILayout.ScrollViewRecord list = GUILayout.ScrollViews[0];
                var normal = (GUIStyle)Field(renderer, "buttonStyle");
                var selected = (GUIStyle)Field(renderer, "selectedStyle");
                Check(normal.fixedHeight == 0 && normal.fixedWidth == 0 && selected.fixedHeight == 0 && selected.fixedWidth == 0,
                    tab + ": renderer clears inherited fixed skin sizes before measuring multi-line rows");
                foreach (GUILayout.ControlRecord row in list.Controls)
                {
                    if (!row.Button) continue;
                    GUIStyle actualStyle = row.Text.StartsWith("▶ ", StringComparison.Ordinal) ? selected : normal;
                    Check(Math.Abs(row.Rect.width - list.ContentWidth) < .01f, tab + ": flexible row uses the actual client width after conditional scrollbar layout");
                    Check(row.Rect.height >= Math.Ceiling(actualStyle.CalcHeight(new GUIContent(row.Text), row.Rect.width)),
                        tab + ": final caption and selected font fit measured multi-line row height");
                }
                GUILayout.ControlRecord waiting = list.Controls.Find(control => control.Button && control.Text.Contains("미션 제목 00", StringComparison.Ordinal));
                Check(waiting.Rect.height > 24 + 16 && waiting.Text.Contains("설명 조회 대기", StringComparison.Ordinal)
                    && !waiting.Text.Contains("읽음", StringComparison.Ordinal) && !waiting.Text.Contains("미독", StringComparison.Ordinal),
                    tab + ": unknown story read state is omitted while waiting means description retrieval");
                Check(list.Controls.Exists(control => control.Button && control.Text.Contains("미션 제목 01", StringComparison.Ordinal) && control.Text.Contains("읽음", StringComparison.Ordinal))
                    && list.Controls.Exists(control => control.Button && control.Text.Contains("미션 제목 02", StringComparison.Ordinal) && control.Text.Contains("미독", StringComparison.Ordinal)),
                    tab + ": explicit read and unread publications remain visible");
                Check(GUILayout.Controls.Exists(control => !control.Button && control.Text.Contains("설명 조회에 필요한 게임 자료", StringComparison.Ordinal)), tab + ": waiting detail explains the missing description data");
                if (tab == "미션") Check(GUILayout.Controls.Exists(control => control.Text.Contains("미션 미완료·조건 미충족을 뜻하지 않습니다", StringComparison.Ordinal)), "mission waiting detail separates retrieval delay from completion and conditions");
                Check(rows[0].ReadState == LogReadState.Unknown && rows[0].RefreshState == LogRefreshState.Waiting, "display wording leaves original read and refresh model states intact");
                Check(list.Controls.Exists(control => control.Button && control.Text.Contains(new string('R', 100), StringComparison.Ordinal) && control.Rect.height > list.Viewport.height),
                    tab + ": long fallback raw ID retains full wrapping height instead of truncating a single-line row");
                for (int index = 0; index < 5; index++) renderer.Handle(LogPadCommand.Down);
                FocusVisible(renderer, model, tab, "미션 제목 05", tab + " measured multi-line selected row");
                list = GUILayout.ScrollViews[0];
                GUILayout.ControlRecord focused = list.Controls.Find(control => control.Button && control.Text.Contains("미션 제목 05", StringComparison.Ordinal));
                Check(focused.VisibleRect.height >= focused.Rect.height - .01f && focused.VisibleRect.width >= focused.Rect.width - .01f,
                    tab + ": focused row has full visible pixels inside both list and native window clip");
                Rect shorter = renderer.WindowRect; shorter.height = 440;
                SetField(renderer, "window", shorter);
                for (int index = 0; index < 6; index++) renderer.Handle(LogPadCommand.Down);
                Repaint(renderer); Repaint(renderer);
                list = GUILayout.ScrollViews[0];
                GUILayout.ControlRecord oversized = list.Controls.Find(control => control.Button && control.Text.Contains(new string('R', 100), StringComparison.Ordinal));
                Check(oversized.Rect.height > list.Viewport.height && oversized.Rect.y == model.Tab(tab).ListScroll && oversized.VisibleRect.height > 0,
                    tab + ": oversized raw-ID row aligns its beginning when the narrow window also becomes short");
            }
            ConfigureLayout(new Rect(20, 20, 540, 440), 24);
            var cards = new LogModel(); cards.ResetContext(402);
            Publish(cards, "선택지", Item("card-unknown"));
            var cardRenderer = new LogRenderer(cards, null, null);
            cardRenderer.Handle(LogPadCommand.None);
            Repaint(cardRenderer);
            Check(GUILayout.LiteralLabels.Exists(label => label.Contains("읽음 정보 없음", StringComparison.Ordinal)), "card unknown read state has a neutral information label");
        }
        finally
        {
            ConfigureLayout(saved, oldFont);
            Screen.width = oldWidth; Screen.height = oldHeight;
            GUI.skin.verticalScrollbar.fixedWidth = oldBarWidth;
            GUI.skin.verticalScrollbar.margin = oldBarMargin;
            for (int index = 0; index < skinStyles.Length; index++) { skinStyles[index].fixedWidth = fixedSizes[index].x; skinStyles[index].fixedHeight = fixedSizes[index].y; }
        }
    }
}
