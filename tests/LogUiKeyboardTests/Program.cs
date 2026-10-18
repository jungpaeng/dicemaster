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
        RendererNavigation();
        RendererRenderingBoundaries();
        Console.WriteLine("PASS: LogUiKeyboardTests: " + checks + " checks passed; actual renderer/model/local keyboard sources with explicit Unity/game boundaries; all 11,172 modern Hangul syllables.");
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
        GUILayout.CardBodyHeight = 5000;
        Invoke(renderer, "DrawCards");
        Check(GUILayout.Buttons.Exists(text => text.Contains(card.Text, StringComparison.Ordinal)), "collapsed/expanded choice header uses actual option sentence");
        Check(model.Tab("선택지").ListScroll == 0, "expanding very long card follows header rather than jumping to body end");
        Check(GUILayout.LiteralLabels.Contains("원본 ID: " + card.RawId), "raw ID is rendered literally without replacing event-result identifiers or markup");
        Check(!DiceMaster.EmbeddedTranslations.Display.TranslatedLabels.Exists(text => text.Contains(card.RawId, StringComparison.Ordinal)), "raw ID never enters translation/logger label boundary");
        Check(((GUIStyle)Field(renderer, "literalStyle")).richText == false, "user input/raw ID style disables rich-text interpretation");
        GUILayout.CardBodyHeight = 30;

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
}
