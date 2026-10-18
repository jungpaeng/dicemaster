using System;
using System.Collections.Generic;
using System.Reflection;
using DiceMaster.LogUI;
using DiceMaster.Gamepad;
using DiceMaster.LiveLog;
using UnityEngine;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new InvalidOperationException(message); }

    private static void Main()
    {
        RendererDeferredStartupLayout();
        WindowRootContract();
        RendererNativeWindowBounds();
        RendererNavigation();
        RendererWithoutSearch();
        RendererMissionStatusRows();
        RendererWaitingStatusExplanation();
        RendererReservationControlFocus();
        RendererDetailPointerTransactions();
        CursorOwnershipLifecycle();
        RendererPointerTakeoverTransactions();
        RendererActualControllerPointerBoundary();
        RendererActualControllerKeyboardBoundary();
        RendererChoiceAndConditionNavigation();
        RendererChoiceAndConditionReflow();
        RendererRenderingBoundaries();
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
        Check(model.Tab("선택지").SelectedId == "card1" && Area(renderer) == "List", "first A selects only");
        renderer.Handle(LogPadCommand.Activate);
        Check(model.Tab("선택지").SelectedId == "card1" && Area(renderer) == "List", "repeated A keeps choice comparison in its visible list");
        renderer.Handle(LogPadCommand.Down);
        renderer.Handle(LogPadCommand.Activate);
        Check(model.Tab("선택지").SelectedId == "card2" && Area(renderer) == "List", "A selects another choice without opening a hidden-detail state");
        renderer.Handle(LogPadCommand.Back);
        Check(closes == 1, "B closes directly from the always-visible choice comparison");
        renderer.Handle(LogPadCommand.NextTab);
        renderer.Handle(LogPadCommand.NextTab);
        Check(renderer.CurrentTab == "이벤트", "LB/RB preserve four-tab order");
        object target = new object();
        Publish(model, "이벤트", Item("event1", target), Item("event2", new object()));
        renderer.Handle(LogPadCommand.Activate);
        Check(Area(renderer) == "List" && !model.Tab("이벤트").ExecutionFocus && LogBridge.Reservations == 0, "first A only selects an event without entering or reserving");
        renderer.Handle(LogPadCommand.Activate);
        Check(Area(renderer) == "Detail" && model.Tab("이벤트").ExecutionFocus && LogBridge.Reservations == 0, "A entering details focuses the actual reservation control without reserving");
        renderer.Handle(LogPadCommand.Up);
        Check(model.Tab("이벤트").ExecutionFocus && LogBridge.Reservations == 0, "up keeps focus on the only active detail control");
        renderer.Handle(LogPadCommand.Down);
        Check(model.Tab("이벤트").ExecutionFocus, "down focuses reservation control");
        renderer.Handle(LogPadCommand.Activate);
        Check(LogBridge.Reservations == 1 && ReferenceEquals(LogBridge.LastTarget, target), "new A reserves latest selected target");
        renderer.Handle(LogPadCommand.Activate);
        Check(LogBridge.Reservations == 2 && model.Tab("이벤트").ExecutionFocus, "a separate fresh A can reserve again while the successful control keeps focus");
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
            foreach (string tab in new[] { "선택지", "조건", "이벤트", "미션" })
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
                    Check(GUILayout.ScrollViews[0].Controls.Exists(control => control.Button && control.Text.Contains(row.Id, StringComparison.Ordinal)),
                        tab + ": all published rows remain in the unfiltered rendered list");
                Check(!GUILayout.Controls.Exists(control => control.Text.Contains("검색", StringComparison.Ordinal)
                    || control.Text.Contains("자판", StringComparison.Ordinal) || control.Text == "패드 입력" || control.Text == "지우기"),
                    tab + ": content has no search field, screen keyboard or search action");
                float heading = tab == "선택지" ? 0 : (int)Field(renderer, "chromeFont") + 14;
                Check(Math.Abs(GUILayout.ScrollViews[0].Viewport.height - layout.Content.height + heading) < .01f,
                    tab + ": the list uses the full body except its own visible heading");
                for (int index = 0; index < rows.Length; index++) renderer.Handle(LogPadCommand.Down);
                Repaint(renderer); Repaint(renderer);
                Check(model.Tab(tab).SelectedId == rows[rows.Length - 1].Id,
                    tab + ": pad navigation reaches the final published row without a filter");
                renderer.Handle(LogPadCommand.Tools);
                Repaint(renderer);
                var tools = GUILayout.ScrollViews[0].Controls.FindAll(control => control.Button);
                Check(tools.Count == 17 && !tools.Exists(control => control.Text.Contains("검색", StringComparison.Ordinal) || control.Text.Contains("자판", StringComparison.Ordinal)),
                    tab + ": tools expose exactly the seventeen remaining actions without search or character input");
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
            Check(Area(renderer) == "Detail" && state.ExecutionFocus,
                "a real mission with an information-like title still exposes its healthy reservation action");
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
            PressKey(replacementRenderer, KeyCode.Return);
            ReservationTicket oldTicket = replaced.Tab("미션").BeginReservation();
            Check(oldTicket != null, "transition regression begins with an actual armed mission");
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
                Vector2 point = PointerTarget(pendingRenderer, control => control.Button && control.Text.Contains("미션 조건 통과 예약", StringComparison.Ordinal));
                DrawPointer(pendingRenderer, touch ? EventType.TouchDown : EventType.MouseDown, touch ? EventType.TouchDown : EventType.MouseDown, point);
                Publish(pending, "미션", StatusRow("pending-status", target));
                SoftResetForPointer(pendingRenderer);
                DrawPointer(pendingRenderer, touch ? EventType.TouchUp : EventType.MouseUp, touch ? EventType.TouchUp : EventType.MouseUp,
                    point, text => text.Contains("미션 조건 통과 예약", StringComparison.Ordinal), consume: true);
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

    private static void AssertReservationFocus(LogRenderer renderer, LogModel model, string tab, bool enabled, bool focused, string scenario)
    {
        Repaint(renderer);
        string label = tab == "이벤트" ? "이벤트 우선 예약" : "미션 조건 통과 예약";
        GUILayout.ControlRecord button = GUILayout.Controls.Find(control => control.Button && control.Text.Contains(label, StringComparison.Ordinal));
        Check(GUILayout.LiteralLabels.Contains("상세") && !GUILayout.LiteralLabels.Contains("▶ 상세"), scenario + ": detail heading is plain text rather than a fake focus stop");
        Check(button != null && button.Enabled == enabled, scenario + ": reservation control enabled state matches latest publication");
        Check(button.Text.StartsWith("▶ ", StringComparison.Ordinal) == focused && model.Tab(tab).ExecutionFocus == focused
            && (int)Field(renderer, "detailIndex") == (focused ? 1 : 0), scenario + ": visual control focus, execution focus and index stay synchronized");
        Check(Area(renderer) == (focused ? "Detail" : "List"), scenario + ": focus belongs to an actionable region");
    }

    private static void RendererReservationControlFocus()
    {
        int savedAttempts = LogBridge.Reservations;
        object savedTarget = LogBridge.LastTarget;
        bool savedResult = LogBridge.ReservationResult;
        try
        {
            foreach (string tab in new[] { "이벤트", "미션" })
            {
                int attempts = LogBridge.Reservations;
                object target = new object();
                var model = new LogModel(); model.ResetContext(700);
                Publish(model, tab, Item("focused-target", target));
                var renderer = new LogRenderer(model, null, null);
                renderer.Handle(LogPadCommand.None);
                for (int index = 0; index < (tab == "이벤트" ? 2 : 3); index++) renderer.Handle(LogPadCommand.NextTab);
                renderer.Handle(LogPadCommand.Activate);
                AssertReservationFocus(renderer, model, tab, true, false, tab + " first A only selects");
                renderer.Handle(LogPadCommand.Activate);
                AssertReservationFocus(renderer, model, tab, true, true, tab + " second A enters actual control");
                Check(LogBridge.Reservations == attempts, "entering a reservation detail never executes the entry input");
                ReservationTicket entryTicket = model.Tab(tab).BeginReservation();
                renderer.Handle(LogPadCommand.Up);
                renderer.Handle(LogPadCommand.Down);
                renderer.Handle(LogPadCommand.Right);
                AssertReservationFocus(renderer, model, tab, true, true, tab + " one-control detail navigation");
                Check(model.Tab(tab).CanActivate(entryTicket), "detail directions are true no-ops and do not cancel/re-arm the existing control ticket");
                Check(LogBridge.Reservations == attempts, "detail direction commands do not reserve");
                ReservationTicket consumed = model.Tab(tab).BeginReservation();
                renderer.Handle(LogPadCommand.Activate);
                attempts++;
                Check(LogBridge.Reservations == attempts && ReferenceEquals(LogBridge.LastTarget, target), "fresh A reserves exactly the currently focused target");
                AssertReservationFocus(renderer, model, tab, true, true, tab + " successful reservation retains actual control focus");
                Check(!model.Tab(tab).CanActivate(consumed), "successful reservation consumes the old ticket even when focus stays on the button");

                model.MarkPublicationFailure(tab);
                renderer.Handle(LogPadCommand.Activate);
                AssertReservationFocus(renderer, model, tab, false, false, tab + " failed publication cancels detail and consumes pending A");
                Check(LogBridge.Reservations == attempts, "publication failure cannot forward same-frame confirmation");
                Publish(model, tab, Item("focused-target", target));
                AssertReservationFocus(renderer, model, tab, true, false, tab + " healthy publication alone does not enter detail");
                renderer.Handle(LogPadCommand.Right);
                AssertReservationFocus(renderer, model, tab, true, true, tab + " explicit right re-enters healthy detail");
                renderer.Handle(LogPadCommand.Tools);
                renderer.Handle(LogPadCommand.Back);
                AssertReservationFocus(renderer, model, tab, true, true, tab + " explicit tools return refocuses actual control");
                renderer.Handle(LogPadCommand.Tools);
                for (int index = 0; index < 15; index++) renderer.Handle(LogPadCommand.Down);
                renderer.Handle(LogPadCommand.Activate);
                AssertReservationFocus(renderer, model, tab, true, true, tab + " tools return control refocuses actual action");
                Check(LogBridge.Reservations == attempts, "returning from tools cannot consume its entry input as reservation");

                LogBridge.ReservationResult = false;
                renderer.Handle(LogPadCommand.Activate);
                attempts++;
                AssertReservationFocus(renderer, model, tab, true, false, tab + " rejected reservation returns to list");
                Check(LogBridge.Reservations == attempts, "failed reservation attempts once and does not keep an armed detail");
                LogBridge.ReservationResult = true;
                renderer.Handle(LogPadCommand.Right);
                object replacement = new object();
                LogBridge.DuringReservation = () => Publish(model, tab, Item("focused-target", replacement));
                renderer.Handle(LogPadCommand.Activate);
                attempts++;
                LogBridge.DuringReservation = null;
                Repaint(renderer);
                Check(LogBridge.Reservations == attempts && model.Tab(tab).SelectedId == null && !model.Tab(tab).ExecutionFocus && Area(renderer) == "List",
                    "target replacement during accepted reservation cannot retain focus or arm the replacement");
                renderer.Handle(LogPadCommand.Activate);
                renderer.Handle(LogPadCommand.Activate);
                ReservationTicket stale = model.Tab(tab).BeginReservation();
                Publish(model, tab, Item("focused-target", new object()));
                renderer.Handle(LogPadCommand.Activate);
                Check(model.Tab(tab).SelectedId == null && LogBridge.Reservations == attempts, "same-ID target replacement consumes pending A before replacement selection");
                renderer.Handle(LogPadCommand.Activate);
                renderer.Handle(LogPadCommand.Activate);
                typeof(LogRenderer).GetMethod("Reserve", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(renderer, new object[] { stale });
                Check(Area(renderer) == "List" && !model.Tab(tab).ExecutionFocus && (int)Field(renderer, "detailIndex") == 0,
                    "stale reservation ticket returns to list immediately without waiting for the next GUI synchronization");
                AssertReservationFocus(renderer, model, tab, true, false, tab + " stale target ticket cancels execution focus");
                Check(LogBridge.Reservations == attempts, "stale ticket is rejected before the game reservation boundary");
                renderer.Handle(LogPadCommand.Right);
                Publish(model, tab);
                renderer.Handle(LogPadCommand.Activate);
                Check(model.Tab(tab).SelectedId == null && !model.Tab(tab).ExecutionFocus && Area(renderer) == "List" && LogBridge.Reservations == attempts,
                    "publication target disappearance cancels selected detail and consumes pending confirmation");
                Publish(model, tab, Item("focused-target", new object()));
                renderer.Handle(LogPadCommand.Activate);
                renderer.Handle(LogPadCommand.Activate);
                model.ResetContext(701);
                Publish(model, tab, Item("new-context-target", new object()));
                renderer.Handle(LogPadCommand.Activate);
                Check(model.Tab(tab).SelectedId == null && !model.Tab(tab).ExecutionFocus && Area(renderer) == "List" && LogBridge.Reservations == attempts,
                    "new context consumes armed detail input without selecting or reserving its replacement");

                foreach (string disabled in new[] { "waiting", "read-failure", "retained-failure", "no-target", "disallowed", "publication-failed" })
                {
                    object disabledTarget = disabled == "no-target" ? null : new object();
                    LogItem row = Item("disabled", disabledTarget);
                    if (disabled == "waiting") row.RefreshState = LogRefreshState.Waiting;
                    if (disabled == "read-failure") row.RefreshState = LogRefreshState.ReadFailure;
                    if (disabled == "retained-failure") row.RefreshState = LogRefreshState.RetainedFailure;
                    if (disabled == "disallowed") row.ReservationAllowed = false;
                    var disabledModel = new LogModel(); disabledModel.ResetContext(702);
                    Publish(disabledModel, tab, row);
                    if (disabled == "publication-failed") disabledModel.MarkPublicationFailure(tab);
                    var disabledRenderer = new LogRenderer(disabledModel, null, null);
                    disabledRenderer.Handle(LogPadCommand.None);
                    for (int index = 0; index < (tab == "이벤트" ? 2 : 3); index++) disabledRenderer.Handle(LogPadCommand.NextTab);
                    disabledRenderer.Handle(LogPadCommand.Activate);
                    disabledRenderer.Handle(LogPadCommand.Activate);
                    disabledRenderer.Handle(LogPadCommand.Right);
                    AssertReservationFocus(disabledRenderer, disabledModel, tab, false, false, tab + " " + disabled + " detail entry refused");
                    Check(LogBridge.Reservations == attempts, "disabled detail has no entry-triggered reservation");
                    object healthyTarget = disabledTarget ?? new object();
                    Publish(disabledModel, tab, Item("disabled", healthyTarget));
                    Repaint(disabledRenderer);
                    Check(Area(disabledRenderer) == "List" && !disabledModel.Tab(tab).ExecutionFocus && LogBridge.Reservations == attempts,
                        "healthy refresh never enters or executes a previously disabled detail automatically");
                    if (disabledModel.Tab(tab).SelectedId == null) disabledRenderer.Handle(LogPadCommand.Activate);
                    disabledRenderer.Handle(LogPadCommand.Right);
                    AssertReservationFocus(disabledRenderer, disabledModel, tab, true, true, tab + " " + disabled + " explicitly re-entered after recovery");
                    Check(LogBridge.Reservations == attempts, "explicit detail re-entry still requires a later fresh A to execute");
                }

                var keyModel = new LogModel(); keyModel.ResetContext(703);
                Publish(keyModel, tab, Item("key-reservation", new object()));
                var keyRenderer = new LogRenderer(keyModel, null, null);
                keyRenderer.Handle(LogPadCommand.None);
                for (int index = 0; index < (tab == "이벤트" ? 2 : 3); index++) keyRenderer.Handle(LogPadCommand.NextTab);
                TapKey(keyRenderer, KeyCode.Return);
                PressKey(keyRenderer, KeyCode.Return);
                AssertReservationFocus(keyRenderer, keyModel, tab, true, true, tab + " held Enter detail entry");
                PressKey(keyRenderer, KeyCode.Return);
                Check(LogBridge.Reservations == attempts, "repeated held Enter after entering detail cannot confirm its focused button");
                ReleaseKey(keyRenderer, KeyCode.Return);
                PressKey(keyRenderer, KeyCode.Return);
                attempts++;
                PressKey(keyRenderer, KeyCode.Return);
                Check(LogBridge.Reservations == attempts, "fresh Enter confirms once while held repeat remains suppressed");
                ReleaseKey(keyRenderer, KeyCode.Return);
                PressKey(keyRenderer, KeyCode.Space);
                attempts++;
                PressKey(keyRenderer, KeyCode.Space);
                Check(LogBridge.Reservations == attempts, "fresh Space confirms once while held repeat remains suppressed");
                ReleaseKey(keyRenderer, KeyCode.Space);
                PressKey(keyRenderer, KeyCode.Return);
                attempts++;
                keyModel.ResetContext(704);
                Publish(keyModel, tab, Item("held-context-replacement", new object()));
                keyRenderer.ResetInputFocus();
                keyRenderer.Handle(LogPadCommand.None);
                PressKey(keyRenderer, KeyCode.Return);
                Check(keyModel.Tab(tab).SelectedId == null && LogBridge.Reservations == attempts, "held Enter cannot confirm or select after a context reset");
                ReleaseKey(keyRenderer, KeyCode.Return);
                TapKey(keyRenderer, KeyCode.Return);
                Check(keyModel.Tab(tab).SelectedId == "held-context-replacement" && Area(keyRenderer) == "List" && LogBridge.Reservations == attempts,
                    "fresh Enter after release only selects the new context row");
            }
        }
        finally
        {
            Input.Held.Clear();
            LogBridge.Reservations = savedAttempts;
            LogBridge.LastTarget = savedTarget;
            LogBridge.ReservationResult = savedResult;
            LogBridge.DuringReservation = null;
        }
    }

    private static void RendererDetailPointerTransactions()
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
                    for (int index = 0; index < (tab == "이벤트" ? 2 : 3); index++) renderer.Handle(LogPadCommand.NextTab);
                    renderer.Handle(LogPadCommand.Activate);
                    renderer.Handle(LogPadCommand.Right);
                    AssertReservationFocus(renderer, model, tab, true, true, tab + " before mouse " + action);
                    ClickRenderedButton(renderer, action == "close" ? "닫기" : action == "tab" ? "조건" : "도구", action != "tools");
                    Check(action == "close" ? closes == 1 : action == "tab" ? renderer.CurrentTab == "조건" && Area(renderer) == "List" : Area(renderer) == "Tools",
                        tab + ": detail execution-focus cancellation preserves mouse down/up transaction for " + action);
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
        reset.Invoke(renderer, reset.GetParameters().Length == 0 ? null : new object[] { true });
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
        for (int index = 0; index < (tab == "이벤트" ? 2 : 3); index++) renderer.Handle(LogPadCommand.NextTab);
        renderer.Handle(LogPadCommand.Activate); renderer.Handle(LogPadCommand.Right);
        return renderer;
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
                Func<string, bool> reserve = text => text.Contains(tab == "이벤트" ? "이벤트 우선 예약" : "미션 조건 통과 예약", StringComparison.Ordinal);
                foreach (int kind in new[] { 0, 1, 2, 3 })
                {
                    EventType down = kind == 1 || kind == 3 ? EventType.TouchDown : EventType.MouseDown;
                    EventType up = kind == 1 || kind == 3 ? EventType.TouchUp : EventType.MouseUp;
                    EventType rawDown = kind == 0 || kind == 3 ? EventType.MouseDown : EventType.TouchDown;
                    EventType rawUp = kind == 0 || kind == 3 ? EventType.MouseUp : EventType.TouchUp;
                    object target = new object(); var model = new LogModel();
                    var renderer = PointerReservationRenderer(model, tab, target);
                    LogTabState state = model.Tab(tab);
                    ReservationTicket padTicket = state.BeginReservation();
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
                        Func<string, bool> matches = text => action == "close" ? text == "닫기" : action == "tab" ? text == "조건" : text.Contains("도구", StringComparison.Ordinal);
                        Vector2 chromePoint = PointerTarget(chrome, control => control.Button && matches(control.Text));
                        attempts = LogBridge.Reservations;
                        DrawPointer(chrome, down, rawDown, chromePoint);
                        SoftResetForPointer(chrome);
                        DrawPointer(chrome, up, rawUp, chromePoint, matches, consume: true);
                        Check(action == "close" ? closes == 1 : action == "tab" ? chrome.CurrentTab == "조건" : Area(chrome) == "Tools",
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
                DrawPointer(rejectedRenderer, EventType.TouchUp, EventType.TouchUp, rejectedPoint);
                DrawPointer(rejectedRenderer, EventType.TouchUp, EventType.TouchUp, rejectedPoint, reserve, consume: true);
                Check(LogBridge.Reservations == rejectedAttempts && Field(rejectedRenderer, "pointerIdentity") == null,
                    "touch events alone do not manufacture success when Unity's button boundary did not accept the release");

                var padOnly = new LogModel(); var padRenderer = PointerReservationRenderer(padOnly, tab, new object());
                ReservationTicket oldPad = padOnly.Tab(tab).BeginReservation();
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
                                if (frame.ResetFocus) renderer.ResetInputFocus(frame.PreservePointerTransaction);
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
                        Func<string, bool> reserve = text => text.Contains(tab == "이벤트" ? "이벤트 우선 예약" : "미션 조건 통과 예약", StringComparison.Ordinal);
                        Vector2 point = PointerTarget(renderer, control => control.Button && reserve(control.Text));
                        int attempts = LogBridge.Reservations;
                        ReservationTicket priorPadTicket = model.Tab(tab).BeginReservation();
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
                        Check(LogBridge.Reservations == attempts + (mode == LogInputMode.KeyboardMouse ? 1 : 2),
                            "fresh pad input cannot skip focus reset; only a still-focused original target may be reserved again");

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
                            Check(LogBridge.Reservations == afterPad, "actual controller integrated list/chrome transactions never reserve through held input");
                        }
                    }

            foreach (string boundary in new[] { "focus", "ready", "disconnect", "close", "explicit-cancel" })
            {
                GamepadBridge.PointerActivity = null; GamepadBridge.KeyboardActivity = null; GamepadBridge.Cancelled = null;
                var model = new LogModel(); var renderer = PointerReservationRenderer(model, "이벤트", new object());
                var controller = new LogPadController(); var states = new PadState[4]; bool[] connected = { true, false, false, false };
                controller.Tick(true, true, true, LogInputMode.Gamepad, 0, states, connected);
                controller.Tick(true, true, true, LogInputMode.Gamepad, .1, states, connected);
                GamepadBridge.PointerActivity = controller.PointerActivity; GamepadBridge.KeyboardActivity = controller.KeyboardActivity; GamepadBridge.Cancelled = controller.Cancel;
                Func<string, bool> reserve = text => text.Contains("이벤트 우선 예약", StringComparison.Ordinal);
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
                renderer.ResetInputFocus(hard.PreservePointerTransaction);
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
                        if (frame.ResetFocus) renderer.ResetInputFocus(frame.PreservePointerTransaction);
                        renderer.Handle(frame.Command); renderer.Scroll(frame.Scroll);
                        return frame;
                    }
                    int attempts = LogBridge.Reservations;
                    Event.current = new Event { type = EventType.MouseMove, rawType = EventType.MouseMove, mousePosition = new Vector2(-50, -50) };
                    renderer.Draw();
                    TapKey(renderer, KeyCode.Return);
                    if (modeCase == "Gamepad-to-mouse") mode = LogInputMode.KeyboardMouse;
                    LogPadFrame entered = UpdateAfterKeyboard();
                    Check(Area(renderer) == "Detail" && model.Tab(tab).ExecutionFocus && LogBridge.Reservations == attempts,
                        "actual controller Update after mouse movement and keyboard detail entry preserves the keyboard's focused reservation button");
                    Check(!entered.ResetFocus && entered.Command == LogPadCommand.None,
                        "already-synchronous keyboard command absorbs only its pending soft mode/pad reset");
                    TapKey(renderer, KeyCode.Return); UpdateAfterKeyboard();
                    Check(LogBridge.Reservations == attempts + 1 && ReferenceEquals(LogBridge.LastTarget, target),
                        "keyboard can still confirm its focused reservation across actual controller Updates");

                    Func<string, bool> reserve = text => text.Contains(tab == "이벤트" ? "이벤트 우선 예약" : "미션 조건 통과 예약", StringComparison.Ordinal);
                    Vector2 point = PointerTarget(renderer, control => control.Button && reserve(control.Text));
                    DrawPointer(renderer, EventType.TouchDown, EventType.TouchDown, point);
                    TapKey(renderer, KeyCode.LeftArrow);
                    Check(Field(renderer, "pointerIdentity") == null && Field(renderer, "pointerReservation") == null && Area(renderer) == "List",
                        "fresh keyboard command takes ownership from an unfinished touch transaction");
                    UpdateAfterKeyboard();
                    DrawPointer(renderer, EventType.TouchUp, EventType.TouchUp, point, reserve, consume: true);
                    Check(LogBridge.Reservations == attempts + 1 && Area(renderer) == "List",
                        "touch release after a fresh keyboard command cannot perform the abandoned pointer action");
                    TapKey(renderer, KeyCode.Return);
                    model.ResetContext(732); Publish(model, tab, Item("new-keyboard-context", new object()));
                    controller.Cancel();
                    LogPadFrame hard = UpdateAfterKeyboard();
                    Check(hard.ResetFocus && !hard.PreservePointerTransaction && Area(renderer) == "List"
                        && model.Tab(tab).SelectedId == null && !model.Tab(tab).ExecutionFocus && LogBridge.Reservations == attempts + 1,
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
            Check(GUILayout.LiteralLabels.Contains(read + " · 조회 정상"), scenario + ": every choice read/refresh state is displayed without opening detail");
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
        Check(model.Tab("선택지").SelectedId == "choice-first", "A and horizontal choice input cannot hide a body or change to unrelated selection");
        renderer.Handle(LogPadCommand.Down);
        renderer.Handle(LogPadCommand.Activate);
        AssertPublicChoices(renderer, model, choices, "after choosing another comparison");
        model.Tab("선택지").ListScroll = 0;
        ClickRenderedButton(renderer, "선택 비교 첫째");
        Check(model.Tab("선택지").SelectedId == "choice-first", "mouse header release selects its stable choice ID");
        AssertPublicChoices(renderer, model, choices, "after header mouse click");
        renderer.Handle(LogPadCommand.Back);
        Check(closes == 1, "B closes the public choice comparison directly");

        var conditions = new LogModel(); conditions.ResetContext(301);
        var conditionRows = new[] { Item("condition-alpha", new object()), Item("condition-beta", new object()), Item("condition-charlie", new object()) };
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
        Check(dummyConditionTicket != null, "dummy target supplies a valid generic model ticket to exercise renderer tab safety");
        typeof(LogRenderer).GetMethod("Reserve", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(conditionRenderer, new object[] { dummyConditionTicket });
        Check(LogBridge.Reservations == reservations && !conditions.Tab("조건").ExecutionFocus,
            "condition tab rejects a valid generic reservation ticket at its renderer action boundary");
        conditions.Tab("조건").ExecutionFocus = true;
        SetField(conditionRenderer, "detailIndex", 1);
        conditionRenderer.Handle(LogPadCommand.Activate);
        Check(LogBridge.Reservations == reservations, "dummy reservable targets and forced execution focus cannot reserve a condition");
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
            for (int index = 0; index < (tab == "이벤트" ? 2 : 3); index++) reservationRenderer.Handle(LogPadCommand.NextTab);
            reservationRenderer.Handle(LogPadCommand.Activate);
            reservationRenderer.Handle(LogPadCommand.Right);
            reservationRenderer.Handle(LogPadCommand.Down);
            Check(reservationModel.Tab(tab).SelectedId == "reserve-first" && reservationModel.Tab(tab).ExecutionFocus, tab + ": detail down still focuses reservation instead of selecting another row");
            reservationRenderer.Handle(LogPadCommand.Up);
            Check(reservationModel.Tab(tab).SelectedId == "reserve-first" && reservationModel.Tab(tab).ExecutionFocus, tab + ": detail up stays on the only actual control without moving selection");
            TapKey(reservationRenderer, KeyCode.DownArrow);
            Check(reservationModel.Tab(tab).SelectedId == "reserve-first" && reservationModel.Tab(tab).ExecutionFocus, tab + ": keyboard detail down preserves reservation-focus behavior");
            TapKey(reservationRenderer, KeyCode.UpArrow);
            Check(reservationModel.Tab(tab).SelectedId == "reserve-first" && reservationModel.Tab(tab).ExecutionFocus, tab + ": keyboard detail up retains actual-control focus and selection");
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
            Check(keyboardModel.Tab("선택지").SelectedId == "key-first" && Area(keyboardRenderer) == "List", "real keyboard A and horizontal input keep choices fully public");
            PressKey(keyboardRenderer, KeyCode.DownArrow);
            PressKey(keyboardRenderer, KeyCode.DownArrow);
            Check(keyboardModel.Tab("선택지").SelectedId == "key-second", "held keyboard down selects once and ignores repeated KeyDown");
            keyboardModel.ResetContext(305);
            Publish(keyboardModel, "선택지", Item("replacement"));
            keyboardRenderer.ResetInputFocus();
            keyboardRenderer.Handle(LogPadCommand.None);
            PressKey(keyboardRenderer, KeyCode.DownArrow);
            Check(keyboardModel.Tab("선택지").SelectedId == null, "held direction cannot select a replacement context choice");
            ReleaseKey(keyboardRenderer, KeyCode.DownArrow);
            TapKey(keyboardRenderer, KeyCode.DownArrow);
            Check(keyboardModel.Tab("선택지").SelectedId == "replacement", "fresh direction selects after release in replacement context");
            TapKey(keyboardRenderer, KeyCode.Escape);
            Check(keyboardCloses == 1, "keyboard Escape closes the public choice list directly");
            Check(LogBridge.Reservations == reservations, "choice and condition navigation never invokes event or mission reservation");
        }
        finally { Input.Held.Clear(); }
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
            Check(Area(renderer) == "List" && !(bool)Field(renderer, "ensureVisible"), "choice selection consumes its explicit header-follow request");
            Publish(model, "선택지", publication(8000));
            FocusVisible(renderer, model, "선택지", "비교 카드 선택", "public choice follows preceding body growth");
            renderer.Scroll(1);
            float manualOffset = model.Tab("선택지").ListScroll;
            Publish(model, "선택지", publication(12000));
            Repaint(renderer); Repaint(renderer);
            Check(model.Tab("선택지").ListScroll == manualOffset, "manual public-choice body scrolling survives another preceding-body reflow");
            renderer.Handle(LogPadCommand.Down);
            FocusVisible(renderer, model, "선택지", "비교 카드 다음", "fresh direction resumes public-choice header following");

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
        Check(GUILayout.Buttons.Exists(text => text.Contains(card.Text, StringComparison.Ordinal)), "always-visible choice header uses actual option sentence");
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
        Check(!model.Tab("이벤트").ExecutionFocus && (int)Field(renderer, "detailIndex") == 0, "failed publication cannot focus retained reservation control");
        string summary = (string)typeof(LogRenderer).GetMethod("Summary", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(renderer, null);
        Check(summary.Contains("게시 실패 · 이전 값 (예약 불가)", StringComparison.Ordinal), "publication failure reason remains visible in summary");
        foreach (string tab in new[] { "선택지", "조건", "미션" })
        {
            while (renderer.CurrentTab != tab) renderer.Handle(LogPadCommand.NextTab);
            Publish(model, tab, Item("failed-publication-" + tab, new object()));
            model.MarkPublicationFailure(tab);
            summary = (string)typeof(LogRenderer).GetMethod("Summary", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(renderer, null);
            Check(summary.Contains("게시 실패 · 이전 값", StringComparison.Ordinal)
                && (tab == "미션" ? summary.Contains("(예약 불가)", StringComparison.Ordinal) : !summary.Contains("예약", StringComparison.Ordinal)),
                tab + ": failed publication explains retained data and mentions reservation only for tabs that support it");
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
            Check(font >= previousFont && font >= 16, "default font scales up with resolution");
            Check(window.x >= 0 && window.y >= 0 && window.x + window.width <= screen[0] && window.yMax <= screen[1],
                "scaled default window stays entirely inside display");
            Check(window.width > screen[0] * .55f && window.height > screen[1] * .65f, "default leaves a readable substantial content window");
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

            // Every preceding choice body contributes its full height. Follow
            // the measured header rather than estimating ordinal * rowHeight.
            focusRenderer.Handle(LogPadCommand.PreviousTab);
            focusRenderer.Handle(LogPadCommand.PreviousTab);
            focusRenderer.Handle(LogPadCommand.Activate);
            for (int index = 0; index < 9; index++) focusRenderer.Handle(LogPadCommand.Down);
            focusRenderer.Handle(LogPadCommand.Activate);
            FocusVisible(focusRenderer, focusModel, "선택지", "비교 카드 09", "choice after variable public bodies");
            focusRenderer.Handle(LogPadCommand.Down);
            FocusVisible(focusRenderer, focusModel, "선택지", "비교 카드 10", "card after oversized body");
            for (int index = 0; index < 10; index++) focusRenderer.Handle(LogPadCommand.Up);
            FocusVisible(focusRenderer, focusModel, "선택지", "비교 카드 00", "first public choice header moving up");
            focusModel.Tab("선택지").ListScroll = 200;
            focusRenderer.Scroll(1);
            float manualScroll = focusModel.Tab("선택지").ListScroll;
            Array.Reverse(focusRows);
            Publish(focusModel, "선택지", focusRows);
            Repaint(focusRenderer); Repaint(focusRenderer);
            Check(focusModel.Tab("선택지").ListScroll == manualScroll, "manual body scrolling survives publication until a new focus movement requests following");
            focusRenderer.Handle(LogPadCommand.Down);
            FocusVisible(focusRenderer, focusModel, "선택지", "비교 카드 00", "new focus command resumes following after manual body scrolling");

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
                LogBridge.LastTarget = reservationItem.Target;
                var reservationRenderer = new LogRenderer(reservationModel, null, null);
                reservationRenderer.Handle(LogPadCommand.None);
                for (int index = 0; index < (tab == "이벤트" ? 2 : 3); index++) reservationRenderer.Handle(LogPadCommand.NextTab);
                reservationRenderer.Handle(LogPadCommand.Activate);
                reservationRenderer.Handle(LogPadCommand.Activate);
                reservationRenderer.Handle(LogPadCommand.Down);
                Repaint(reservationRenderer); Repaint(reservationRenderer);
                string label = tab == "이벤트" ? "이벤트 우선 예약" : "미션 조건 통과 예약";
                GUILayout.ControlRecord reserve = GUILayout.Controls.Find(control => control.Button && control.Text.Contains(label, StringComparison.Ordinal));
                var reserveStyle = (GUIStyle)Field(reservationRenderer, "chromeSelectedStyle");
                var reservationLayout = (LogLayout)Field(reservationRenderer, "layout");
                var boxStyle = (GUIStyle)Field(reservationRenderer, "boxStyle");
                Check(reserve != null && reserve.Text.Contains("예약 중", StringComparison.Ordinal), "selected " + tab + " reservation button includes its real reserved status");
                Check(reserve.FontSize == (int)Field(reservationRenderer, "chromeFont") && ((GUIStyle)Field(reservationRenderer, "textStyle")).fontSize == 64,
                    "reservation control uses readable chrome font while the requested large body font is preserved");
                Check(reserve.Rect.height >= reserveStyle.CalcHeight(new GUIContent(reserve.Text), reserve.Rect.width),
                    "whole " + tab + " reserved label fits the actual detail-column button height");
                GUILayout.ScrollViewRecord detailPane = GUILayout.ScrollViews[1];
                Check(detailPane.Viewport.y >= reserve.Rect.yMax && detailPane.Viewport.height > 0,
                    "reservation control and detail body occupy separate usable regions");
                Check(Math.Abs(detailPane.Viewport.yMax + boxStyle.padding.bottom - reservationLayout.Content.height) < .01f,
                    "detail scroll fills exactly the space remaining below the measured reservation control");
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
            FocusVisible(pointerRenderer, pointerModel, "선택지", "비교 카드 00", "pad focus resumes follow after mouse scrollbar drag");
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
        var seed = new Rect(20, 20, 1152, 756);
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
                Check(renderer.WindowRect == LogLayout.DefaultWindow(Screen.width, Screen.height) && DiceMaster.Config.ConfigManager.LogFontSize.Value == 40,
                    "historical seed migrates using final 4K layout and font");
            }
            foreach (int[] screen in new[] { new[] { 1280, 720 }, new[] { 2560, 1440 }, new[] { 3840, 2160 } })
            {
                Screen.width = screen[0]; Screen.height = screen[1];
                float scale = LogLayout.ScreenScale(Screen.height);
                Rect previousScaled = LogRenderer.ConstrainWindow(new Rect(20 * scale, 20 * scale, 1280 * scale, 960 * scale), Screen.width, Screen.height);
                previousScaled.y += .005f; previousScaled.height += .005f;
                ConfigureLayout(previousScaled, LogLayout.DefaultFont(Screen.height));
                var migrated = FirstDrawRenderer();
                Check(migrated.WindowRect == LogLayout.DefaultWindow(Screen.width, Screen.height), "previous 1280 by 960 scaled default migrates after config serialization");
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
            foreach (int[] display in new[] { new[] { 1920, 1080, 1152, 756 }, new[] { 2560, 1440, 1536, 1008 },
                new[] { 3840, 2160, 2304, 1512 }, new[] { 3440, 1440, 2064, 1008 }, new[] { 1920, 1200, 1152, 840 }, new[] { 5120, 1440, 3072, 1008 } })
            {
                Rect proportional = LogLayout.DefaultWindow(display[0], display[1]);
                Check(Math.Abs(proportional.width - display[2]) < .01f && Math.Abs(proportional.height - display[3]) < .01f,
                    "default window independently uses 60 percent width and 70 percent height on " + display[0] + "x" + display[1]);
                Check(ContainsRect(new Rect(0, 0, display[0], display[1]), proportional), "proportional default stays inside the complete display");
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
            Check(LogLayout.DefaultWidth == 1152 && LogLayout.DefaultHeight == 756, "configuration seed represents the 60 percent by 70 percent default at 1080p");
            foreach (int[] screen in new[] { new[] { 1280, 720 }, new[] { 1920, 1080 }, new[] { 2560, 1440 }, new[] { 3840, 2160 } })
            {
                Screen.width = screen[0]; Screen.height = screen[1];
                float scale = LogLayout.ScreenScale(Screen.height);
                Rect previous = LogRenderer.ConstrainWindow(new Rect(20 * scale, 20 * scale, 1200 * scale, 860 * scale), Screen.width, Screen.height);
                int previousFont = LogLayout.DefaultFont(Screen.height);
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
            Check(FirstDrawRenderer().WindowRect.width == 1152, "unscaled previous default seed migrates to proportional window on first draw");
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
                for (int index = 0; index < (tab == "이벤트" ? 2 : 3); index++) renderer.Handle(LogPadCommand.NextTab);
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
                    Check(row.Rect.width == (float)Math.Floor(list.ContentWidth), tab + ": row width excludes actual 20px scrollbar and 3px margin");
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
