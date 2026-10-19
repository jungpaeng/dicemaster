using System;
using DiceMaster.Config;
using DiceMaster.Gamepad;
using DiceMaster.LogUI;
using UnityEngine;

internal static partial class Program
{
    private static void RendererExplicitFocusLifecycle()
    {
        Rect saved = ConfiguredRect(); int font = ConfigManager.LogFontSize.Value;
        int touchCount = Input.touchCount;
        try
        {
            ConfigureLayout(new Rect(20, 20, 1200, 1000), 20);
            GUILayout.ButtonInput = null;
            foreach (int tab in new[] { 0, 1, 2, 3 })
                foreach (bool tools in new[] { false, true })
                    foreach (int kind in new[] { 0, 1, 2 })
                        FocusLifecyclePointerCase(tab, tools, kind);
            FocusLifecycleLateControllerTakeover();
            FocusLifecycleSearchIntent();
        }
        finally
        {
            ConfigureLayout(saved, font); Input.touchCount = touchCount;
            GamepadBridge.PointerActivity = null; GamepadBridge.KeyboardActivity = null; GamepadBridge.Cancelled = null;
            GUILayout.ButtonInput = null; GUILayout.ResetPointerCapture();
            GUI.FocusControl(null); GUI.FocusCalls.Clear();
        }
    }

    private static LogRenderer FocusLifecycleRenderer(int tab, Action refresh, out LogModel model)
    {
        GUILayout.ResetPointerCapture();
        model = new LogModel(); model.ResetContext(880);
        foreach (string name in new[] { "선택지", "조건", "이벤트", "미션" })
            Publish(model, name, Item("focus-first", new object()), Item("focus-second", new object()));
        var renderer = new LogRenderer(model, refresh, null);
        renderer.Handle(LogPadCommand.None);
        for (int index = 0; index < tab; index++) renderer.Handle(LogPadCommand.NextTab);
        Repaint(renderer);
        GUI.FocusCalls.Clear();
        return renderer;
    }

    private static void NoFocusRelease(string scenario)
    {
        Check(!GUI.FocusCalls.Exists(call => call.Name == null), scenario);
    }

    private static void OneLayoutFocusRelease(string scenario)
    {
        Check(GUI.FocusCalls.Count == 1 && GUI.FocusCalls[0].Name == null
            && GUI.FocusCalls[0].Type == EventType.Layout,
            scenario + ": one explicit focus release occurs on Layout");
    }

    private static void FocusLifecyclePointerCase(int tab, bool tools, int kind)
    {
        int refreshes = 0; LogModel model;
        var renderer = FocusLifecycleRenderer(tab, () => refreshes++, out model);
        if (tools) { renderer.Handle(LogPadCommand.Tools); Repaint(renderer); GUI.FocusCalls.Clear(); }
        string scenario = renderer.CurrentTab + (tools ? " tools" : " list") + " pointer form " + kind;
        EventType down = kind == 2 ? EventType.TouchDown : EventType.MouseDown;
        EventType up = kind == 2 ? EventType.TouchUp : EventType.MouseUp;
        EventType rawDown = kind == 0 ? EventType.MouseDown : EventType.TouchDown;
        EventType rawUp = kind == 0 ? EventType.MouseUp : EventType.TouchUp;
        Input.touchCount = kind == 0 ? 0 : 1;
        foreach (EventType phase in new[] { EventType.Layout, EventType.Repaint, EventType.Ignore })
            DrawPointer(renderer, phase, rawDown, new Vector2(-100, -100));
        NoFocusRelease(scenario + ": idle callbacks do not continuously release native focus");

        Func<GUILayout.ControlRecord, bool> target = tools
            ? control => control.Button && control.Text.Contains("게임 데이터 새로고침", StringComparison.Ordinal)
            : tab == 0 ? control => control.Button && control.Text == "조건"
            : control => control.Button && control.Text.Contains("focus-second", StringComparison.Ordinal);
        Vector2 point = PointerTarget(renderer, target);
        // A pending explicit transition must wait if a fresh pointer event
        // arrives before the next Layout. This does not model native focus as
        // clearing mouse capture; the assertion is about our own call timing.
        SetField(renderer, "releaseTextFocus", true);
        DrawPointer(renderer, down, rawDown, point);
        Check(Field(renderer, "pointerIdentity") != null, scenario + ": down captures its stable control identity");
        foreach (EventType phase in new[] { EventType.Layout, EventType.Repaint, EventType.Ignore })
            DrawPointer(renderer, phase, rawUp, point);
        SoftResetForPointer(renderer);
        DrawPointer(renderer, up, rawUp, point);
        NoFocusRelease(scenario + ": down through matching up never changes native focus");
        Check(Field(renderer, "pointerIdentity") == null, scenario + ": accepted up completes the transaction once");
        Check(tools ? refreshes == 1 && Area(renderer) == "Tools"
            : tab == 0 ? renderer.CurrentTab == "조건" : model.Tab(renderer.CurrentTab).SelectedId == "focus-second",
            scenario + ": real native-button boundary accepts the intended tab, list or tool action without injected success");
        Repaint(renderer); OneLayoutFocusRelease(scenario);
        Repaint(renderer); Repaint(renderer); OneLayoutFocusRelease(scenario + " after idle redraws");

        // Every source tab must also be able to leave via the same chrome.
        int next = (tab + 1) % 4;
        if (renderer.CurrentTab == new[] { "선택지", "조건", "이벤트", "미션" }[next]) next = (next + 1) % 4;
        string destination = new[] { "선택지", "조건", "이벤트", "미션" }[next];
        point = PointerTarget(renderer, control => control.Button && control.Text == destination);
        GUI.FocusCalls.Clear();
        DrawPointer(renderer, down, rawDown, point);
        DrawPointer(renderer, EventType.Layout, rawUp, point);
        DrawPointer(renderer, EventType.Repaint, rawUp, point);
        DrawPointer(renderer, up, rawUp, point);
        Check(renderer.CurrentTab == destination && Area(renderer) == "List", scenario + ": chrome leaves the current tab/tools on one gesture");
        NoFocusRelease(scenario + ": tab chrome does not release focus inside the gesture");
        Repaint(renderer); Repaint(renderer); OneLayoutFocusRelease(scenario + " tab transition");
    }

    private static void FocusLifecycleLateControllerTakeover()
    {
        foreach (string action in new[] { "search", "tools" })
            foreach (int kind in new[] { 0, 1, 2 })
            {
                LogModel model; var renderer = FocusLifecycleRenderer(2, null, out model);
                var controller = new LogPadController(); var states = new PadState[4]; bool[] connected = { true, false, false, false };
                controller.Tick(true, true, true, LogInputMode.Gamepad, 0, states, connected);
                controller.Tick(true, true, true, LogInputMode.Gamepad, .1, states, connected);
                GamepadBridge.PointerActivity = controller.PointerActivity;
                GamepadBridge.KeyboardActivity = controller.KeyboardActivity;
                GamepadBridge.Cancelled = controller.Cancel;
                EventType down = kind == 2 ? EventType.TouchDown : EventType.MouseDown;
                EventType up = kind == 2 ? EventType.TouchUp : EventType.MouseUp;
                EventType rawDown = kind == 0 ? EventType.MouseDown : EventType.TouchDown;
                EventType rawUp = kind == 0 ? EventType.MouseUp : EventType.TouchUp;
                Input.touchCount = kind == 0 ? 0 : 1;
                Vector2 search = PointerTarget(renderer, control => !control.Button && control.Text == "");
                DrawPointer(renderer, down, rawDown, search);
                DrawPointer(renderer, up, rawUp, search);
                GUI.FocusCalls.Clear();
                if (action == "tools")
                {
                    Vector2 tool = PointerTarget(renderer, control => control.Button && control.Text == "도구 [Y]");
                    DrawPointer(renderer, down, rawDown, tool);
                    DrawPointer(renderer, EventType.Layout, rawUp, tool);
                    DrawPointer(renderer, EventType.Repaint, rawUp, tool);
                    DrawPointer(renderer, up, rawUp, tool);
                    NoFocusRelease("search-to-tools pointer gesture does not clear focus before its release");
                }
                // Both events arrive before the next Update. The soft reset
                // is delivered only after pointerIdentity was cleared by up.
                LogPadFrame frame = controller.Tick(true, true, true, LogInputMode.KeyboardMouse, .2, states, connected);
                Check(frame.ResetFocus && frame.PreservePointerTransaction && Field(renderer, "pointerIdentity") == null,
                    "actual controller emits a late soft takeover after the complete " + action + " gesture");
                renderer.ResetInputFocus(frame.PreservePointerTransaction, frame.PreserveNavigationArea);
                renderer.Handle(frame.Command); renderer.Scroll(frame.Scroll);
                Repaint(renderer);
                if (action == "search")
                {
                    Check(GUI.GetNameOfFocusedControl() == "DiceMaster.EventSearch" && Area(renderer) == "List",
                        "late controller takeover keeps the newly focused search caret");
                    NoFocusRelease("late search takeover does not queue a focus release after up");
                }
                else
                {
                    Check(Area(renderer) == "Tools" && GUI.GetNameOfFocusedControl() == null,
                        "late controller takeover preserves pointer-opened tools and completes its explicit search-focus exit");
                    OneLayoutFocusRelease("pointer search-to-tools transition");
                }
                GUI.FocusCalls.Clear();
                renderer.ResetInputFocus(); Repaint(renderer); Repaint(renderer);
                Check(Area(renderer) == "List" && GUI.GetNameOfFocusedControl() == null,
                    "hard reset still returns to list and clears the old search caret");
                OneLayoutFocusRelease("hard focus/context reset");
                GamepadBridge.PointerActivity = null; GamepadBridge.KeyboardActivity = null; GamepadBridge.Cancelled = null;
            }
    }

    private static void FocusLifecycleSearchIntent()
    {
        LogModel model; var renderer = FocusLifecycleRenderer(2, null, out model);
        Vector2 search = PointerTarget(renderer, control => !control.Button && control.Text == "");
        renderer.ResetInputFocus(); GUI.FocusCalls.Clear();
        // A fresh field press can precede the Layout requested by opening or
        // resetting the log. It intentionally chooses a new text focus.
        DrawPointer(renderer, EventType.TouchDown, EventType.TouchDown, search);
        DrawPointer(renderer, EventType.Layout, EventType.TouchUp, search);
        DrawPointer(renderer, EventType.Repaint, EventType.TouchUp, search);
        DrawPointer(renderer, EventType.TouchUp, EventType.TouchUp, search);
        Repaint(renderer); Repaint(renderer);
        Check(GUI.GetNameOfFocusedControl() == "DiceMaster.EventSearch",
            "fresh search press supersedes an older queued focus release even before its first Layout");
        NoFocusRelease("new search focus survives down/layout/repaint/up and subsequent idle frames");
        GUI.FocusCalls.Clear();
        GUILayout.TextFieldInput = previous => "focus";
        Repaint(renderer); GUILayout.TextFieldInput = null; Repaint(renderer);
        Check(model.Tab("이벤트").SearchQuery == "focus" && GUI.GetNameOfFocusedControl() == "DiceMaster.EventSearch",
            "search typing and publication filtering retain the caret");
        NoFocusRelease("typing does not enqueue a focus release");
        TapKey(renderer, KeyCode.Escape);
        NoFocusRelease("search Escape queues its explicit exit until a safe Layout");
        Repaint(renderer); Repaint(renderer);
        Check(GUI.GetNameOfFocusedControl() == null, "search Escape exits text entry on its next Layout");
        OneLayoutFocusRelease("explicit search Escape");
    }
}
