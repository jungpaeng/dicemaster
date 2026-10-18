using System;
using System.Reflection;

static class LogOwnershipTests
{
    public static int Run(Assembly assembly)
    {
        int assertions = 0;
        void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
        Type stateType = assembly.GetType("DiceMaster.Gamepad.PadState", true);
        Type filterType = assembly.GetType("DiceMaster.Gamepad.PadInputFilter", true);
        object filter = Activator.CreateInstance(filterType);
        MethodInfo filterMethod = filterType.GetMethod("Filter");
        object State(int buttons = 0, short lx = 0, short ly = 0, short rx = 0, short ry = 0, byte lt = 0, byte rt = 0)
        {
            object s = Activator.CreateInstance(stateType);
            stateType.GetField("Buttons").SetValue(s, (ushort)buttons);
            stateType.GetField("LeftX").SetValue(s, lx); stateType.GetField("LeftY").SetValue(s, ly);
            stateType.GetField("RightX").SetValue(s, rx); stateType.GetField("RightY").SetValue(s, ry);
            stateType.GetField("LeftTrigger").SetValue(s, lt); stateType.GetField("RightTrigger").SetValue(s, rt);
            return s;
        }
        bool Neutral(object s) => (bool)filterType.GetMethod("IsNeutral").Invoke(null, new[] { s });
        object Filter(uint slot, object state, bool dice = false) => filterMethod.Invoke(filter, new[] { (object)slot, state, dice });
        void Capture(bool active) => filterType.GetMethod("SetLogCapture").Invoke(filter, new object[] { active });
        void Reset() => filterType.GetMethod("Reset").Invoke(filter, null);
        object held = State(0xffff, -32768, 32767, 32767, -32768, 255, 255);
        for (uint slot = 0; slot < 4; slot++)
        {
            Reset(); Capture(true);
            Check(Neutral(Filter(slot, held, true)), "Log suppresses every channel on slot " + slot);
            Capture(false);
            Check(Neutral(Filter(slot, held, true)), "Close retains full capture on slot " + slot);
            Check(!(bool)filterType.GetMethod("CanUseRaw").Invoke(filter, new object[] { slot }), "Held log cannot edit dice");
            Check(Neutral(Filter(slot, State())), "Neutral releases capture");
            Check(!Neutral(Filter(slot, held)), "New physical input returns to game");
            Reset(); Capture(true); Filter(slot, held);
            filterType.GetMethod("Disconnected").Invoke(filter, new object[] { slot });
            Capture(false);
            Check(Neutral(Filter(slot, held)), "Reconnect held input waits for full neutral");
            Filter(slot, State());
            Check(!Neutral(Filter(slot, held)), "Reconnect accepts only fresh input");
        }
        Reset();
        filterType.GetMethod("RequireNeutral").Invoke(filter, null);
        Check(Neutral(Filter(0, State(0x1000))), "Boundary consumes action held before first callback");
        Check(Neutral(Filter(1, State(rx:20000))), "Boundary also protects non-owner right stick");
        Check(Neutral(Filter(2, State(rt:200))), "Boundary also protects non-owner trigger");
        Filter(0, State()); Filter(1, State()); Filter(2, State());
        Check(!Neutral(Filter(0, State(0x1000))), "Fresh action after boundary passes");
        Check(!Neutral(Filter(1, State(rx:20000))), "Fresh right stick after boundary passes");
        Check(!Neutral(Filter(2, State(rt:200))), "Fresh trigger after boundary passes");
        Reset(); Capture(true); Filter(0, held); Capture(false);
        object partial = Filter(0, State(0x1000, rx:20000, rt:200));
        Check(Neutral(partial), "Independent capture persists for remaining held channels");
        Filter(0, State());
        Check(!Neutral(Filter(0, State(0x1000, rx:20000, rt:200))), "Released channels rearm");

        Type controllerType = assembly.GetType("DiceMaster.Gamepad.LogPadController", true);
        Type modeType = assembly.GetType("DiceMaster.Gamepad.LogInputMode", true);
        Type frameType = assembly.GetType("DiceMaster.Gamepad.LogPadFrame", true);
        object controller = Activator.CreateInstance(controllerType);
        Array states = Array.CreateInstance(stateType, 4);
        bool[] pads = { true, true, false, false };
        for (int i = 0; i < 4; i++) states.SetValue(State(), i);
        object Tick(float time, int buttons = 0, string mode = "Gamepad", bool open = true, bool focused = true, bool ready = true,
            short lx = 0, short ry = 0, int slot = 0)
        {
            states.SetValue(State(buttons, lx:lx, ry:ry), slot);
            return controllerType.GetMethod("Tick").Invoke(controller, new object[] { open, focused, ready,
                Enum.Parse(modeType, mode), (double)time, states, pads });
        }
        string Command(object frame) => frameType.GetField("Command").GetValue(frame).ToString();
        bool ResetFocus(object frame) => (bool)frameType.GetField("ResetFocus").GetValue(frame);
        int Owner() => (int)controllerType.GetProperty("Owner").GetValue(controller);
        const int A = 0x1000, B = 0x2000, X = 0x4000, Up = 1;
        Check(ResetFocus(Tick(0, A)), "Opening cancels focus");
        Check(Command(Tick(.1f, A)) == "None", "Held opening A consumed");
        Tick(.2f); Check(Command(Tick(.3f, A)) == "Activate", "Fresh A activates");
        Check(Command(Tick(.4f, A)) == "None", "Activation never repeats");
        Tick(.5f); Check(Command(Tick(.6f, X)) == "Refresh", "Fresh X refreshes once");
        Check(Command(Tick(.7f, X)) == "None", "Held refresh never repeats");
        Tick(.8f); Check(Command(Tick(.9f, Up)) == "Up", "Direction first press");
        Check(Command(Tick(1f, Up)) == "None", "Direction initial delay");
        Check(Command(Tick(1.3f, Up)) == "Up", "Direction repeats");
        controllerType.GetMethod("Cancel").Invoke(controller, null);
        Check(ResetFocus(Tick(1.4f, A)), "Target/context invalidation resets focus");
        Check(Command(Tick(1.5f, A)) == "None", "Held A cannot activate replacement target");
        Tick(1.6f); Check(Command(Tick(1.7f, A)) == "Activate", "Replacement requires fresh press");
        Tick(1.8f); Check(Command(Tick(1.9f, B)) == "Back", "Fresh B goes back");
        Check(ResetFocus(Tick(2f, B, open:false)), "Closing cancels pending input");
        Tick(2.1f, B); Check(Command(Tick(2.2f, B)) == "None", "Held close cannot act on reopened window");
        Tick(2.3f); Check(Command(Tick(2.4f, A)) == "Activate", "Reopened window requires fresh press");
        Check(ResetFocus(Tick(2.5f, A, focused:false)), "Alt-Tab cancels focus");
        Check(Command(Tick(2.6f, A)) == "None", "Alt-Tab return consumes held button");
        Tick(2.7f); Check(Command(Tick(2.8f, A)) == "Activate", "Alt-Tab return accepts fresh button");
        Check(ResetFocus(Tick(2.9f, A, ready:false)), "Lost native polling cancels focus");
        Check(Command(Tick(3f, A)) == "None", "Capture recovery consumes held button");
        Tick(3.1f); Check(Command(Tick(3.2f, A)) == "Activate", "Capture recovery accepts fresh button");
        Check(Owner() == 0, "First owner is fixed");
        Tick(3.3f); Check(Command(Tick(3.4f, A, slot:1)) == "None" && Owner() == 0, "Other pad cannot steal owner");
        pads[0] = false;
        Check(ResetFocus(Tick(3.5f)) && Owner() == 0, "Disconnect cancels without selecting another owner");
        pads[0] = true;
        Check(ResetFocus(Tick(3.6f, A)) && Command(Tick(3.7f, A)) == "None", "Reconnect held A is consumed");
        controllerType.GetMethod("Reselect").Invoke(controller, new object[] { pads });
        Check(Owner() == 1, "Only explicit reselect changes owner");
        states.SetValue(State(), 1); Tick(3.8f);
        Check(Command(Tick(3.9f, A, slot:1)) == "Activate", "New owner uses fresh input");
        // Valid keyboard/mouse mode wins, but a new physical pad transition can
        // recover when game scheme updates stop because XInput is captured.
        Tick(4f, mode:"KeyboardMouse", slot:1);
        Check(Command(Tick(4.1f, A, mode:"KeyboardMouse", slot:1)) == "None", "Mode change consumes first pad transition");
        Tick(4.2f, mode:"KeyboardMouse", slot:1);
        Check(Command(Tick(4.3f, A, mode:"KeyboardMouse", slot:1)) == "Activate", "Frozen game scheme supports safe physical switch");
        controllerType.GetMethod("PointerActivity").Invoke(controller, null);
        Check(ResetFocus(Tick(4.4f, A, mode:"KeyboardMouse", slot:1)), "Mouse/keyboard activity clears physical override safely");
        Check(Command(Tick(4.5f, A, mode:"KeyboardMouse", slot:1)) == "None", "Pointer transition does not execute held A");
        Tick(4.6f, mode:"Unknown", slot:1);
        Check(Command(Tick(4.7f, A, mode:"Unknown", slot:1)) == "Activate", "Unknown game scheme supports pad");
        Tick(4.8f, mode:"Unknown", slot:1);
        Check((float)frameType.GetField("Scroll").GetValue(Tick(4.9f, mode:"Unknown", ry:32767, slot:1)) < -.9f, "Right stick scrolls without action");
        Type resolver = assembly.GetType("DiceMaster.Gamepad.GameInputModeResolver", true);
        string Mode(string scheme, bool active = true) => resolver.GetMethod("FromScheme").Invoke(null, new object[] { scheme, active }).ToString();
        Check(Mode(null) == "Unknown" && Mode("") == "Unknown" && Mode("unexpected") == "Unknown", "Unknown getter-zero states never imply mouse mode");
        Check(Mode("Keyboard&Mouse") == "KeyboardMouse" && Mode("Gamepad") == "Gamepad", "Recognized active schemes preserved");
        Check(Mode("Gamepad", false) == "Unknown", "Inactive input component falls back");
        Type phases = assembly.GetType("DiceMaster.Gamepad.LogActionPhases", true);
        foreach (bool active in new[] { false, true }) foreach (bool release in new[] { false, true })
            for (int phase = 0; phase <= 4; phase++)
            {
                bool allowed = (bool)phases.GetMethod("Allow").Invoke(null, new object[] { active, release, phase });
                Check(allowed == (!(active || release) || phase != 2 && phase != 3), "Action phases capture activation and preserve canceled cleanup");
            }
        Type nativeType = assembly.GetType("DiceMaster.Gamepad.NativeXInput", true);
        object native = Activator.CreateInstance(nativeType);
        Check(!(bool)nativeType.GetProperty("PollingFresh").GetValue(native), "No game polling evidence initially");
        nativeType.GetMethod("ObserveGamePoll", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(native, null);
        Check((bool)nativeType.GetProperty("PollingFresh").GetValue(native), "Actual game polling creates fresh capture evidence");
        nativeType.GetField("lastPoll", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(native,
            System.Diagnostics.Stopwatch.GetTimestamp() - System.Diagnostics.Stopwatch.Frequency - 1);
        Check(!(bool)nativeType.GetProperty("PollingFresh").GetValue(native), "Capture evidence expires instead of staying sticky");
        Console.WriteLine("Log input: full-channel/all-slot ownership, release/neutral barriers, fixed owner, mode/focus/reconnect safety pass.");
        return assertions;
    }
}
