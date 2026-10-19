using System;
using DiceMaster.Config;
using DiceMaster.Gamepad;
using DiceMaster.LogUI;
using UnityEngine;

internal static partial class Program
{
    private static readonly string[] ToolTestTabs = { "선택지", "조건", "이벤트", "미션" };

    private static void RendererToolsDeviceTakeover()
    {
        Rect saved = ConfiguredRect(); int font = ConfigManager.LogFontSize.Value;
        try
        {
            ConfigureLayout(new Rect(20, 20, 1200, 1000), 20);
            foreach (int tab in new[] { 0, 1, 2, 3 })
                foreach (string entry in new[] { "Y", "touch" })
                    foreach (LogInputMode mode in new[] { LogInputMode.Gamepad, LogInputMode.KeyboardMouse })
                        ToolsTakeoverCase(tab, entry, mode);
            ToolsIndependentModeChanges();
            ToolsTakeoverCancelsPointer();
            ToolsHardBoundaries();
        }
        finally
        {
            ConfigureLayout(saved, font);
            GamepadBridge.PointerActivity = null; GamepadBridge.KeyboardActivity = null; GamepadBridge.Cancelled = null;
            GUILayout.ButtonInput = null; GUILayout.ResetPointerCapture();
            GUI.FocusControl(null); GUI.FocusCalls.Clear();
        }
    }

    private sealed class ToolSession
    {
        internal readonly LogRenderer Renderer;
        internal readonly LogModel Model;
        internal readonly LogPadController Controller = new LogPadController();
        internal readonly PadState[] States = new PadState[4];
        internal readonly bool[] Connected = { true, true, false, false };
        internal LogInputMode Mode;
        internal bool Open = true, Focused = true, Ready = true;
        internal int Refreshes;
        private double time;

        internal ToolSession(int tab, LogInputMode mode)
        {
            Mode = mode;
            Renderer = FocusLifecycleRenderer(tab, () => Refreshes++, out Model);
            Tick(default); Tick(default);
            GamepadBridge.PointerActivity = Controller.PointerActivity;
            GamepadBridge.KeyboardActivity = Controller.KeyboardActivity;
            GamepadBridge.Cancelled = Controller.Cancel;
        }

        internal LogPadFrame Tick(PadState state)
        {
            States[Controller.Owner < 0 ? 0 : Controller.Owner] = state;
            LogPadFrame frame = Controller.Tick(Open, Focused, Ready, Mode, time += .1, States, Connected);
            Check(!(frame.PreservePointerTransaction && frame.PreserveNavigationArea),
                "pointer authority preservation and navigation-only preservation are distinct reset reasons");
            if (frame.ResetFocus) Renderer.ResetInputFocus(frame.PreservePointerTransaction, frame.PreserveNavigationArea);
            if (Open && Focused) { Renderer.Handle(frame.Command); Renderer.Scroll(frame.Scroll); }
            return frame;
        }

        internal void Enter(string entry)
        {
            if (entry == "Y")
            {
                LogPadFrame frame = Tick(new PadState { Buttons = PadButtons.Y });
                if (frame.Command != LogPadCommand.Tools)
                {
                    Check(frame.ResetFocus && frame.PreserveNavigationArea && !frame.PreservePointerTransaction,
                        "first pad input in a fixed mouse scheme safely reacquires ownership without click authority");
                    Tick(default); Tick(new PadState { Buttons = PadButtons.Y });
                }
                Tick(default);
            }
            else
            {
                Vector2 point = PointerTarget(Renderer, control => control.Button && control.Text == "도구 [Y]");
                DrawPointer(Renderer, EventType.TouchDown, EventType.TouchDown, point);
                DrawPointer(Renderer, EventType.TouchUp, EventType.TouchUp, point);
                LogPadFrame frame = Tick(default);
                Check(frame.ResetFocus && frame.PreservePointerTransaction && !frame.PreserveNavigationArea,
                    "touch entry retains only its own pointer takeover semantics");
            }
            Check(Area(Renderer) == "Tools", entry + " enters tools from " + Renderer.CurrentTab);
            Repaint(Renderer);
        }
    }

    private static void ToolsActiveChrome(LogRenderer renderer, bool tools)
    {
        Repaint(renderer);
        foreach (string tab in ToolTestTabs)
        {
            bool active = !tools && tab == renderer.CurrentTab;
            GUILayout.ControlRecord control = GUILayout.Controls.Find(item => item.Button && item.Text == (active ? "● " : "") + tab);
            Check(control != null && control.FontStyle == (active ? FontStyle.Bold : FontStyle.Normal),
                "content tab marker and style agree with the single active area: " + tab);
        }
        GUILayout.ControlRecord tool = GUILayout.Controls.Find(item => item.Button && item.Text == (tools ? "● 도구" : "도구 [Y]"));
        Check(tool != null && tool.FontStyle == (tools ? FontStyle.Bold : FontStyle.Normal),
            "tools marker and style agree with the single active area");
    }

    private static void ToolsTakeoverCase(int tab, string entry, LogInputMode mode)
    {
        var session = new ToolSession(tab, mode); session.Enter(entry);
        var renderer = session.Renderer;
        ToolsActiveChrome(renderer, true);
        renderer.Handle(LogPadCommand.Down); renderer.Handle(LogPadCommand.Down); Repaint(renderer);
        SetField(renderer, "toolScroll", 123.25f);
        object ticket = tab == 3 ? ArmModelTicket(session.Model.Tab("미션")) : null;
        int index = (int)Field(renderer, "toolIndex");
        LogPadFrame first = session.Tick(new PadState { LeftY = -32767 });
        bool takeover = entry == "touch" && mode == LogInputMode.KeyboardMouse;
        if (takeover)
        {
            Check(first.ResetFocus && first.PreserveNavigationArea && !first.PreservePointerTransaction
                && first.Command == LogPadCommand.None && first.Scroll == 0,
                "touch-to-pad takeover is a navigation-only reset with a full neutral barrier");
            Check(Area(renderer) == "Tools" && (int)Field(renderer, "toolIndex") == index
                && (float)Field(renderer, "toolScroll") == 123.25f,
                "pad reacquisition keeps the touched tools area, selection and reading offset");
            Check(Field(renderer, "pointerIdentity") == null && Field(renderer, "pointerReservation") == null
                && (ticket == null || !session.Model.Tab("미션").CanActivate((ReservationTicket)ticket)),
                "preserving navigation never preserves a mission ticket or abandoned pointer authority");
            LogPadFrame held = session.Tick(new PadState { LeftY = -32767 });
            Check(held.Command == LogPadCommand.None && held.Scroll == 0 && (int)Field(renderer, "toolIndex") == index,
                "held takeover stick cannot navigate or activate before neutral");
            session.Tick(default);
            first = session.Tick(new PadState { LeftY = -32767 });
        }
        Check(first.Command == LogPadCommand.Down && Area(renderer) == "Tools" && (int)Field(renderer, "toolIndex") == index + 1,
            tab + " " + entry + " " + mode + ": fresh stick Down moves inside tools instead of returning to the content pane");
        session.Tick(default); LogPadFrame back = session.Tick(new PadState { Buttons = PadButtons.B });
        Check(back.Command == LogPadCommand.Back && Area(renderer) == "List" && renderer.CurrentTab == ToolTestTabs[tab],
            "explicit B restores the prior content tab after either entry and device takeover");
        ToolsActiveChrome(renderer, false);
    }

    private static void ToolsIndependentModeChanges()
    {
        foreach (LogInputMode before in new[] { LogInputMode.Gamepad, LogInputMode.KeyboardMouse, LogInputMode.Unknown })
            foreach (LogInputMode after in new[] { LogInputMode.Gamepad, LogInputMode.KeyboardMouse, LogInputMode.Unknown })
            {
                if (before == after) continue;
                var session = new ToolSession(2, before); session.Enter("touch");
                session.Renderer.Handle(LogPadCommand.Down); Repaint(session.Renderer);
                SetField(session.Renderer, "toolScroll", 89.5f);
                GUI.FocusControl("ExternalTextControl"); GUI.FocusCalls.Clear();
                session.Mode = after;
                LogPadFrame frame = session.Tick(new PadState { Buttons = PadButtons.A, LeftY = -32767, RightY = 32767 });
                Check(frame.ResetFocus && !frame.PreserveNavigationArea && frame.PreservePointerTransaction
                    && frame.Command == LogPadCommand.None && frame.Scroll == 0,
                    before + " → " + after + " retains the established pointer/entry-barrier preservation path");
                Check(Area(session.Renderer) == "Tools" && (int)Field(session.Renderer, "toolIndex") == 1
                    && (float)Field(session.Renderer, "toolScroll") == 89.5f && session.Refreshes == 0,
                    "device-mode transition retains the current tool and its scroll without performing the held action");
                Repaint(session.Renderer);
                Check(GUI.GetNameOfFocusedControl() == "ExternalTextControl",
                    "automatic mode detection before a fresh OnGUI press does not introduce a new text or pointer barrier");
                NoFocusRelease("automatic device mode transition retains its established focus lifecycle");
                Check(session.Tick(new PadState { Buttons = PadButtons.A }).Command == LogPadCommand.None,
                    "held action after a mode transition cannot confirm the selected tool");
            }
    }

    private static void ToolsTakeoverCancelsPointer()
    {
        var session = new ToolSession(3, LogInputMode.KeyboardMouse); session.Enter("touch");
        var renderer = session.Renderer;
        Vector2 point = PointerTarget(renderer, control => control.Button && control.Text.Contains("게임 데이터 새로고침", StringComparison.Ordinal));
        DrawPointer(renderer, EventType.TouchDown, EventType.TouchDown, point);
        session.Tick(default);
        Check(Field(renderer, "pointerIdentity") != null, "real pointer transaction exists before physical pad reacquisition");
        GUI.FocusControl("ExternalTextControl"); GUI.FocusCalls.Clear();
        LogPadFrame frame = session.Tick(new PadState { Buttons = PadButtons.A });
        Check(frame.PreserveNavigationArea && !frame.PreservePointerTransaction && Area(renderer) == "Tools"
            && Field(renderer, "pointerIdentity") == null && Field(renderer, "pointerReservation") == null,
            "fresh physical pad ownership retains tools while discarding the unfinished touch");
        DrawPointer(renderer, EventType.TouchUp, EventType.TouchUp, point);
        Check(session.Refreshes == 0, "abandoned touch release cannot activate a tool after pad reacquisition");
        Repaint(renderer);
        Check(GUI.GetNameOfFocusedControl() == null, "physical pad reacquisition still exits old text entry");
        OneLayoutFocusRelease("physical pad reacquisition");
        // A stale mouse-up may itself notify no new down. It cannot remove the
        // controller's requirement for complete neutral before another action.
        Check(session.Tick(new PadState { Buttons = PadButtons.A }).Command == LogPadCommand.None,
            "old held A cannot confirm after an abandoned click release");
    }

    private static void ToolsHardBoundaries()
    {
        foreach (int tab in new[] { 0, 1, 2, 3 })
            foreach (string boundary in new[] { "focus", "ready", "disconnect", "owner", "close", "context", "cancel" })
            {
                var session = new ToolSession(tab, LogInputMode.KeyboardMouse); session.Enter("touch");
                Vector2 point = PointerTarget(session.Renderer, control => control.Button && control.Text.Contains("게임 데이터 새로고침", StringComparison.Ordinal));
                DrawPointer(session.Renderer, EventType.TouchDown, EventType.TouchDown, point); session.Tick(default);
                object ticket = tab == 3 ? ArmModelTicket(session.Model.Tab("미션")) : null;
                if (boundary == "focus") session.Focused = false;
                else if (boundary == "ready") session.Ready = false;
                else if (boundary == "disconnect") session.Connected[session.Controller.Owner] = false;
                else if (boundary == "owner") session.Controller.Reselect(session.Connected);
                else if (boundary == "close") session.Open = false;
                else if (boundary == "context")
                {
                    session.Model.ResetContext(881); Publish(session.Model, ToolTestTabs[tab], Item("new-context", new object()));
                    session.Renderer.ResetInputFocus(); session.Controller.Cancel();
                }
                else session.Controller.Cancel();
                session.Controller.PointerActivity(); session.Mode = LogInputMode.Gamepad;
                LogPadFrame frame = session.Tick(new PadState { Buttons = PadButtons.A, LeftY = -32767 });
                Check(frame.ResetFocus && !frame.PreserveNavigationArea && !frame.PreservePointerTransaction
                    && frame.Command == LogPadCommand.None && frame.Scroll == 0 && Area(session.Renderer) == "List",
                    "hard " + boundary + " wins over same-frame pointer/device takeover and returns to the content pane");
                Check(Field(session.Renderer, "pointerIdentity") == null && Field(session.Renderer, "pointerReservation") == null
                    && (ticket == null || !session.Model.Tab("미션").CanActivate((ReservationTicket)ticket)),
                    "hard " + boundary + " invalidates all old pointer and mission authority");
                DrawPointer(session.Renderer, EventType.TouchUp, EventType.TouchUp, point);
                Check(session.Refreshes == 0, "old touch release after " + boundary + " cannot activate a tool");
            }
    }
}
