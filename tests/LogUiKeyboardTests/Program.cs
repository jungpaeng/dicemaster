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

    private static void Compose(LocalSearchKeyboard keyboard, int initial, int medial, int final)
    { keyboard.PressKey(initial); keyboard.PressKey(medial); keyboard.PressKey(final); }

    private static void Main()
    {
        // Exercise the same public key-selection path used by renderer actions,
        // including all complex vowels and double/compound final consonants.
        var keyboard = new LocalSearchKeyboard();
        int expected = 0xAC00;
        for (int initial = 0; initial < 19; initial++)
            for (int medial = 0; medial < 21; medial++)
                for (int final = 0; final < 28; final++)
                {
                    keyboard.Start("");
                    Compose(keyboard, initial, medial, final);
                    Check(keyboard.Commit() == ((char)expected++).ToString(), "Hangul key path did not reach expected syllable");
                }
        Check(expected == 0xD7A4, "modern Hangul coverage endpoint");
        keyboard.Start("");
        Compose(keyboard, 18, 0, 4); // 한
        Compose(keyboard, 0, 18, 8); // 글
        keyboard.Space();
        Compose(keyboard, 0, 0, 18); // 값
        Check(keyboard.Commit() == "한글 값", "Korean search and compound final");
        keyboard.Start("확정 전 원문");
        keyboard.PressKey(0);
        Check(keyboard.Stage == 1 && keyboard.Composition == "ㄱ", "initial draft");
        keyboard.PressKey(0);
        Check(keyboard.Stage == 2 && keyboard.Composition == "가", "medial draft");
        keyboard.Backspace();
        Check(keyboard.Stage == 1 && keyboard.Draft == "확정 전 원문", "backspace removes composition before text");
        keyboard.UndoComposition();
        Check(keyboard.Stage == 0, "undo initial");
        keyboard.Backspace();
        Check(keyboard.Draft == "확정 전 원", "backspace committed text");
        keyboard.Start("검색"); // cancel is renderer leaving the draft unapplied
        keyboard.SetMode(1);
        keyboard.PressKey(0);
        Check(keyboard.Draft == "검색a", "mixed text composition");
        keyboard.Start("검색");
        Check(keyboard.Commit() == "검색", "reopen after cancelled draft keeps original query");
        keyboard.Start("");
        keyboard.PressKey(2); // ㄴ
        keyboard.PressKey(0); // 나
        Check(keyboard.Commit() == "나", "confirm unfinished syllable uses no final");
        keyboard.Start("");
        keyboard.PressKey(2);
        Check(keyboard.Commit() == "ㄴ", "confirm initial-only query preserves the input");
        keyboard.Start("");
        keyboard.SetMode(2);
        keyboard.PressKey(0);
        keyboard.SetMode(1);
        keyboard.PressKey(25);
        keyboard.SetMode(3);
        keyboard.PressKey(10); // underscore
        keyboard.PressKey(4);
        Check(keyboard.Commit() == "Az_4", "case-sensitive raw ID spelling available");
        keyboard.Start("query");
        keyboard.PressKey(0);
        keyboard.SetMode(3);
        Check(keyboard.Stage == 0 && keyboard.Draft == "query", "changing keyboard page discards only unfinished composition");
        keyboard.Start(new string('a', 256));
        keyboard.SetMode(1);
        keyboard.PressKey(0);
        Check(keyboard.Draft.Length == 256, "bounded search draft");
        keyboard.Backspace();
        keyboard.PressKey(1);
        Check(keyboard.Draft.EndsWith("b", StringComparison.Ordinal), "editing query at size limit");
        foreach (int[] indices in new[] { new[] { -1, 0, 0 }, new[] { 19, 0, 0 }, new[] { 0, 21, 0 }, new[] { 0, 0, 28 } })
        {
            bool rejected = false;
            try { LocalSearchKeyboard.Syllable(indices[0], indices[1], indices[2]); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "invalid syllable indices rejected");
        }
        WindowRootContract();
        RendererNativeWindowBounds();
        RendererNavigation();
        RendererRenderingBoundaries();
        LayoutGeometry();
        RendererViewportAndFocus();
        RendererCompactAndPointerScroll();
        Console.WriteLine("PASS: LogUiKeyboardTests: " + checks + " checks passed; actual renderer/model/local keyboard sources with explicit Unity/game boundaries and managed window/clip contract model (not native Unity execution); all 11,172 modern Hangul syllables.");
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
                renderer.Handle(LogPadCommand.Activate);
                Repaint(renderer);
                NativeWindowContainsContent(renderer, "local search keyboard");
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
        Check(model.Tab("선택지").Expanded.Contains("card1") && Area(renderer) == "Detail", "second A expands selected card");
        renderer.Handle(LogPadCommand.Back);
        renderer.Handle(LogPadCommand.Down);
        renderer.Handle(LogPadCommand.Activate);
        Check(model.Tab("선택지").Expanded.SetEquals(new[] { "card1", "card2" }), "multiple cards remain expanded");
        renderer.Handle(LogPadCommand.Back);
        renderer.Handle(LogPadCommand.Back);
        Check(closes == 1, "B returns from card before closing list");
        renderer.Handle(LogPadCommand.NextTab);
        renderer.Handle(LogPadCommand.NextTab);
        Check(renderer.CurrentTab == "이벤트", "LB/RB preserve four-tab order");
        object target = new object();
        Publish(model, "이벤트", Item("event1", target), Item("event2", new object()));
        renderer.Handle(LogPadCommand.Activate);
        renderer.Handle(LogPadCommand.Activate);
        Check(Area(renderer) == "Detail" && !model.Tab("이벤트").ExecutionFocus && LogBridge.Reservations == 0, "A entering details never reserves");
        renderer.Handle(LogPadCommand.Activate);
        Check(LogBridge.Reservations == 0, "separate reservation focus required");
        renderer.Handle(LogPadCommand.Down);
        Check(model.Tab("이벤트").ExecutionFocus, "down focuses reservation control");
        renderer.Handle(LogPadCommand.Activate);
        Check(LogBridge.Reservations == 1 && ReferenceEquals(LogBridge.LastTarget, target), "new A reserves latest selected target");
        renderer.Handle(LogPadCommand.Activate);
        Check(LogBridge.Reservations == 1, "reservation consumes execution focus");
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
        Check(Area(renderer) == "Keyboard", "search is reachable from pad tools");
        renderer.Handle(LogPadCommand.Down);
        renderer.Handle(LogPadCommand.Activate); // selects initial ㄱ
        renderer.Handle(LogPadCommand.Back);
        Check(Area(renderer) == "Tools" && model.Tab("이벤트").Search == "", "B cancels unconfirmed search draft");
        renderer.Handle(LogPadCommand.Refresh);
        Check(refreshes == 1, "X requests one manual refresh");
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
        for (int index = 0; index < 18; index++) renderer.Handle(LogPadCommand.Down);
        renderer.Handle(LogPadCommand.Activate);
        Check(closes == 2, "all tools navigable through pad, including close");
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
        renderer.Handle(LogPadCommand.Activate);
        renderer.Handle(LogPadCommand.Activate);
        Event.current = new Event { type = EventType.Repaint, rawType = EventType.Repaint };
        Invoke(renderer, "EnsureStyles");
        GUILayout.Buttons.Clear();
        GUILayout.LiteralLabels.Clear();
        DiceMaster.EmbeddedTranslations.Display.TranslatedLabels.Clear();
        Invoke(renderer, "DrawCards");
        Check(GUILayout.Buttons.Exists(text => text.Contains(card.Text, StringComparison.Ordinal)), "collapsed/expanded choice header uses actual option sentence");
        Check(model.Tab("선택지").ListScroll == 0, "expanding very long card follows header rather than jumping to body end");
        Check(GUILayout.LiteralLabels.Contains("원본 ID: " + card.RawId), "raw ID is rendered literally without replacing event-result identifiers or markup");
        Check(!DiceMaster.EmbeddedTranslations.Display.TranslatedLabels.Exists(text => text.Contains(card.RawId, StringComparison.Ordinal)), "raw ID never enters translation/logger label boundary");
        Check(((GUIStyle)Field(renderer, "literalStyle")).richText == false, "user input/raw ID style disables rich-text interpretation");

        renderer.Handle(LogPadCommand.NextTab);
        renderer.Handle(LogPadCommand.NextTab);
        const string privateSearch = "敏感검색<raw-query>";
        model.Tab("이벤트").SetSearch(privateSearch);
        Invoke(renderer, "OpenKeyboard");
        GUILayout.LiteralLabels.Clear();
        DiceMaster.EmbeddedTranslations.Display.TranslatedLabels.Clear();
        Invoke(renderer, "DrawKeyboard");
        Check(GUILayout.LiteralLabels.Exists(text => text.Contains(privateSearch, StringComparison.Ordinal)), "local keyboard displays exact draft text literally");
        Check(!DiceMaster.EmbeddedTranslations.Display.TranslatedLabels.Exists(text => text.Contains(privateSearch, StringComparison.Ordinal)), "private search draft never reaches translation diagnostics");

        model.Tab("이벤트").SetSearch("");
        Publish(model, "이벤트", Item("event", new object()));
        renderer.ResetInputFocus();
        renderer.Handle(LogPadCommand.Activate);
        renderer.Handle(LogPadCommand.Activate);
        model.MarkPublicationFailure("이벤트");
        renderer.Handle(LogPadCommand.Down);
        Check(!model.Tab("이벤트").ExecutionFocus && (int)Field(renderer, "detailIndex") == 0, "failed publication cannot focus retained reservation control");
        string summary = (string)typeof(LogRenderer).GetMethod("Summary", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(renderer, null);
        Check(summary.Contains("게시 실패 · 이전 값 (예약 불가)", StringComparison.Ordinal), "publication failure reason remains visible in summary");
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
            message + ": focused header stays within its actual scroll viewport");
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
            LogLayout ordinary = LogLayout.Calculate(window, font, false, font + 8, 0, font + 8);
            window.height += 173;
            LogLayout taller = LogLayout.Calculate(window, font, false, font + 8, 0, font + 8);
            Check(Math.Abs(taller.Content.height - ordinary.Content.height - 173) < .01f,
                "every extra window pixel goes into the content viewport");
            Check(taller.Content.y == ordinary.Content.y && taller.Footer.yMax <= window.height,
                "resizing preserves chrome and keeps footer inside window");
            Check(ordinary.Tabs.yMax <= ordinary.Status.y && ordinary.Status.yMax <= ordinary.Content.y && ordinary.Content.yMax <= ordinary.Footer.y,
                "tabs/status/content/footer occupy ordered non-overlapping regions");
            LogLayout searched = LogLayout.Calculate(window, font, true, font + 8, 0, font + 8);
            Check(Math.Abs(taller.Content.height - searched.Content.height - searched.Search.height - searched.Gap) < .01f,
                "search consumes exactly its own row and gap from the shared remaining area");
            LogLayout hint = LogLayout.Calculate(window, font, false, font + 8, font * 2, font + 8);
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
        LogLayout roomy = LogLayout.Calculate(new Rect(0, 0, 1200, 900), 20, true, 32, 32, 32, 150);
        LogLayout pressured = LogLayout.Calculate(new Rect(0, 0, 540, 440), 18, true, 60, 1000, 120, 150);
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

            // Expanded bodies before a selected card make ordinal * rowHeight
            // estimates wrong by thousands of pixels. Follow the actual header.
            focusRenderer.Handle(LogPadCommand.PreviousTab);
            focusRenderer.Handle(LogPadCommand.PreviousTab);
            focusRenderer.Handle(LogPadCommand.Activate);
            focusRenderer.Handle(LogPadCommand.Activate);
            focusRenderer.Handle(LogPadCommand.Back);
            for (int index = 0; index < 9; index++) focusRenderer.Handle(LogPadCommand.Down);
            focusRenderer.Handle(LogPadCommand.Activate);
            focusRenderer.Handle(LogPadCommand.Back);
            FocusVisible(focusRenderer, focusModel, "선택지", "비교 카드 09", "card after variable expanded bodies");
            focusRenderer.Handle(LogPadCommand.Down);
            FocusVisible(focusRenderer, focusModel, "선택지", "비교 카드 10", "card after oversized body");
            for (int index = 0; index < 10; index++) focusRenderer.Handle(LogPadCommand.Up);
            FocusVisible(focusRenderer, focusModel, "선택지", "비교 카드 00", "expanded first card header moving up");
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
            for (int index = 0; index < 18; index++) focusRenderer.Handle(LogPadCommand.Down);
            Repaint(focusRenderer); Repaint(focusRenderer);
            GUILayout.ScrollViewRecord tools = ScrollWithButton("로그 창 닫기");
            GUILayout.ControlRecord close = tools.Controls.Find(control => control.Button && control.Text.Contains("로그 창 닫기", StringComparison.Ordinal));
            Check(close.Rect.y >= tools.InputScroll.y && close.Rect.yMax <= tools.InputScroll.y + tools.Viewport.height + .01f,
                "last tools button follows into tools viewport");
            Check(tools.InputScroll.y > 0, "last tool is reached by actual scrolling, not zero-position fixture rows");
            for (int index = 0; index < 18; index++) focusRenderer.Handle(LogPadCommand.Up);
            Repaint(focusRenderer); Repaint(focusRenderer);
            tools = ScrollWithButton("검색 문자 입력");
            Check(tools.InputScroll.y == 0, "returning to first tool restores top of viewport");
            Rect shortWindow = focusRenderer.WindowRect;
            shortWindow.height = 440;
            SetField(focusRenderer, "window", shortWindow);
            focusRenderer.Handle(LogPadCommand.Activate);
            for (int index = 0; index < 8; index++) focusRenderer.Handle(LogPadCommand.Down);
            Repaint(focusRenderer); Repaint(focusRenderer);
            GUILayout.ScrollViewRecord keyboardView = GUILayout.ScrollViews[0];
            GUILayout.ControlRecord selectedKey = keyboardView.Controls.Find(control => control.Button && control.Text.StartsWith("▶ ", StringComparison.Ordinal));
            Check(selectedKey != null && selectedKey.Rect.y >= keyboardView.InputScroll.y && selectedKey.Rect.yMax <= keyboardView.InputScroll.y + keyboardView.Viewport.height + .01f,
                "last local keyboard row remains visible when focused");
            Check(keyboardView.ContentHeight > keyboardView.Viewport.height && keyboardView.InputScroll.y > 0,
                "short window keyboard requires and performs real content scrolling");
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
        GUIStyle[] skinStyles = { GUI.skin.label, GUI.skin.button, GUI.skin.textField, GUI.skin.box, GUI.skin.window };
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
                for (int index = 0; index < 13; index++) renderer.Handle(LogPadCommand.Down);
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
            pointerRenderer.Handle(LogPadCommand.Activate);
            pointerRenderer.Handle(LogPadCommand.Back);
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
}
