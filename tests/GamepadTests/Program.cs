using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

static class GamepadTests
{
    static int assertions;
    static void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
    static void Run(string path, string label)
    {
        var context = new AssemblyLoadContext(label, true);
        var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(path));
        var adjustmentType = assembly.GetType("DiceMaster.Gamepad.PadAdjustment", true);
        var adjustment = Activator.CreateInstance(adjustmentType);
        var tick = adjustmentType.GetMethod("Tick"); var reset = adjustmentType.GetMethod("Reset");
        int Step(bool active, int buttons, double time, int current, short x = 0, short y = 0)
            => (int)tick.Invoke(adjustment, new object[] { active, (ushort)buttons, time, current, x, y });
        const int LB = 0x0100, B = 0x2000, A = 0x1000, X = 0x4000, Up = 1, Down = 2, Left = 4, Right = 8;
        Check(Step(false, LB|Right, 0, 20) == 20, "No adjustment outside active window");
        Check(Step(true, Right, 0, 20) == 20, "No adjustment without LB");
        Check(Step(true, B, 0, 20) == 20, "B alone keeps the selected value");
        Check(Step(true, LB, 0, 20) == 20, "LB alone keeps the selected value");
        Check(Step(true, LB|Right, 0, 20) == 21, "Right +1");
        Check(Step(true, LB|Right, 0.1, 21) == 21, "Initial repeat delay");
        Check(Step(true, LB|Right, 0.36, 21) == 22, "Repeat after initial delay");
        Check(Step(true, LB|Right, 0.40, 22) == 22, "Repeat rate limit");
        Check(Step(true, LB|Right, 0.45, 22) == 23, "Held key repeats");
        Check(Step(true, LB|Left, 0.46, 23) == 22, "Direction change acts immediately");
        Check(Step(true, LB|Up, 0.47, 22) == 32, "Up +10");
        Check(Step(true, LB|Down, 0.48, 32) == 22, "Down -10");
        Check(Step(true, LB|B|Up, 0.49, 22) == 0, "LB+B wins over directions");
        Check(Step(true, LB|Up, 0.495, 0) == 0, "Held direction does not immediately undo automatic reset");
        Check(Step(true, 0, 0.50, 22) == 22, "Releasing LB preserves value");
        Check(Step(true, LB|X|A, 0.51, 22) == 22, "A and X do not edit values");
        Check(Step(true, LB|Down, 0.52, 0) == -10, "Decrementing automatic mode enters negative values");
        reset.Invoke(adjustment, null);
        Check(Step(true, LB|Right, 1, Int32.MaxValue) == Int32.MaxValue, "No integer overflow");
        Check(Step(true, LB|Left, 2, 1) == 0, "Left from one selects automatic mode");
        Check(Step(true, LB|Down, 2.1, 1) == -9, "Down from one crosses zero without clamping");
        Check(Step(true, LB|Down, 2.2, 8) == 8, "Same direction still obeys repeat delay");
        reset.Invoke(adjustment, null);
        Check(Step(true, LB|Down, 2.3, 8) == -2, "Down preserves the full ten-step across zero");
        Check(Step(true, LB|Right, 2.4, 0) == 1, "Right exits automatic mode at one");
        Check(Step(true, LB|Up, 2.5, 0) == 10, "Up exits automatic mode at ten");
        Check(Step(true, LB, 3, -50) == -50, "Existing keyboard value preserved without a step");

        reset.Invoke(adjustment, null);
        Check(Step(true, LB|Left, 3.01, 0) == -1, "Left from automatic mode enters minus one");
        Check(Step(true, LB|Left, 3.37, -1) == -2, "Held left repeats below zero");
        Check(Step(true, LB|Right, 3.38, -2) == -1, "Right increases a negative value");
        reset.Invoke(adjustment, null);
        Check(Step(true, LB|Right, 3.4, -1) == 0, "Increasing minus one reaches automatic mode");
        Check(Step(true, LB|Up, 3.41, -5) == 5, "Up crosses zero with a full ten-step");
        Check(Step(true, LB|Down, 3.42, Int32.MinValue) == Int32.MinValue, "No integer underflow on down");
        Check(Step(true, LB|Left, 3.43, Int32.MinValue) == Int32.MinValue, "No integer underflow on left");
        reset.Invoke(adjustment, null);
        Check(Step(true, LB|Down, 3.44, Int32.MinValue + 5) == Int32.MinValue, "Negative integer boundary saturates");
        Check(Step(true, LB|Up, 3.45, Int32.MaxValue - 5) == Int32.MaxValue, "Positive integer boundary saturates");
        Check(Step(true, LB|B, 3.46, -50) == 0, "LB+B restores automatic mode from negative values");
        Check(Step(true, 0, 3.47, -50) == -50, "Releasing LB preserves a negative override");

        reset.Invoke(adjustment, null);
        Check(Step(true, LB, 4, 20, x:15999) == 20, "Stick ignores small displacement");
        Check(Step(true, LB, 4.01, 20, x:16000) == 21, "Stick right +1 at press threshold");
        Check(Step(true, LB, 4.1, 21, x:20000) == 21, "Stick uses initial repeat delay");
        Check(Step(true, LB, 4.37, 21, x:12000) == 22, "Hysteresis retains a held stick direction");
        Check(Step(true, LB, 4.40, 22, x:12000) == 22, "Stick repeat rate limit");
        Check(Step(true, LB, 4.46, 22, x:12000) == 23, "Held stick repeats");
        Check(Step(true, LB, 4.5, 23, x:9999) == 23, "Stick releases below release threshold");
        Check(Step(true, LB, 4.51, 23, x:16000) == 24, "Stick re-engages without waiting for old repeat");
        Check(Step(true, LB, 4.52, 24, x:-32768) == 23, "Full stick left -1 without short overflow");
        Check(Step(true, LB, 4.53, 23, y:32767) == 33, "Stick up +10");
        Check(Step(true, LB, 4.54, 33, y:-32768) == 23, "Stick down -10");
        Check(Step(true, LB, 4.55, 23, x:32767, y:18000) == 24, "Diagonal chooses dominant horizontal direction");
        Check(Step(true, LB, 4.56, 24, x:18000, y:32767) == 34, "Diagonal chooses dominant vertical direction");
        Check(Step(true, LB|Left, 4.57, 34, x:32767) == 33, "D-pad takes precedence over stick");
        Check(Step(true, LB, 4.58, 33, x:-32768) == 33, "Switching sources in same direction shares repeat timer");
        Check(Step(true, 0, 4.59, 33, x:32767) == 33, "Stick cannot adjust without LB");
        Check(Step(false, LB, 4.6, 33, x:32767) == 33, "Stick cannot adjust outside dice window");
        Check(Step(true, LB, 4.61, 1, x:-32768) == 0, "Stick left from one selects automatic mode");
        reset.Invoke(adjustment, null);
        Check(Step(true, LB, 4.62, 1, y:-32768) == -9, "Stick down from one crosses zero without clamping");
        Check(Step(true, LB, 4.63, 0, x:-32768) == -1, "Stick left from automatic mode enters negative values");
        Check(Step(true, LB, 4.64, 0, y:-32768) == -10, "Stick down from automatic mode enters negative values");
        Check(Step(true, LB, 4.65, -1, x:32767) == 0, "Stick right from minus one selects automatic mode");
        Check(Step(true, LB|B, 5, 50, x:32767) == 0, "LB+B overrides stick");
        Check(Step(true, LB, 5.1, 0, x:32767) == 0, "Held stick does not undo automatic reset");
        Check(Step(true, LB, 5.2, 0, x:9000) == 0, "Reset waits for centered stick rather than press threshold");
        Check(Step(true, LB, 5.3, 0) == 0, "Centered stick rearms adjustment");
        Check(Step(true, LB, 5.4, 0, x:32767) == 1, "Fresh stick tilt exits automatic mode");

        var stateType = assembly.GetType("DiceMaster.Gamepad.PadState", true);
        var filterType = assembly.GetType("DiceMaster.Gamepad.PadInputFilter", true);
        Check(Marshal.SizeOf(stateType) == 16, "XINPUT_STATE ABI size");
        var filter = Activator.CreateInstance(filterType); var filterMethod = filterType.GetMethod("Filter");
        var filterReset = filterType.GetMethod("Reset");
        var buttonsField = stateType.GetField("Buttons"); var packetField = stateType.GetField("Packet");
        object State(int buttons, uint packet = 1, short x = 0, short y = 0)
        {
            var state = Activator.CreateInstance(stateType);
            buttonsField.SetValue(state, (ushort)buttons); packetField.SetValue(state, packet);
            stateType.GetField("LeftTrigger").SetValue(state, (byte)99);
            stateType.GetField("RightTrigger").SetValue(state, (byte)88);
            stateType.GetField("LeftX").SetValue(state, x);
            stateType.GetField("LeftY").SetValue(state, y);
            stateType.GetField("RightX").SetValue(state, (short)-1234);
            stateType.GetField("RightY").SetValue(state, (short)4321);
            return state;
        }
        object Filter(int buttons, bool active, uint index = 0, uint packet = 1, short x = 0, short y = 0)
            => filterMethod.Invoke(filter, new object[] { index, State(buttons, packet, x, y), active });
        int Buttons(object value) => (ushort)buttonsField.GetValue(value);
        uint Packet(object value) => (uint)packetField.GetValue(value);
        short LeftX(object value) => (short)stateType.GetField("LeftX").GetValue(value);
        short LeftY(object value) => (short)stateType.GetField("LeftY").GetValue(value);
        const int Captured = LB|B|Up|Down|Left|Right;
        // All possible digital button combinations, including reserved bits.
        for (int mask = 0; mask <= UInt16.MaxValue; mask++)
        {
            filterReset.Invoke(filter, null);
            Check(Buttons(Filter(mask, false)) == mask, "Outside-window pass-through " + mask);
            filterReset.Invoke(filter, null);
            var filtered = Filter(mask, true);
            int expected = (mask & LB) != 0 ? mask & ~Captured : mask;
            Check(Buttons(filtered) == expected, "Scoped button capture " + mask);
            Check((byte)stateType.GetField("LeftTrigger").GetValue(filtered) == 99, "Trigger untouched");
            Check((short)stateType.GetField("RightX").GetValue(filtered) == -1234, "Stick untouched");
        }
        filterReset.Invoke(filter, null);
        Check(Buttons(Filter(LB|B|Right|A|X, true)) == (A|X), "A and X pass during adjustment");
        Check(Buttons(Filter(B|Right|A, true, packet:2)) == A, "B/DPAD stay captured after releasing LB first");
        Check(Buttons(Filter(A, true, packet:3)) == A, "Release clears latch");
        Check(Buttons(Filter(B|A, true, packet:4)) == (B|A), "Next B press returns to original action");
        Filter(LB|B, true, packet:5);
        Check(Buttons(Filter(LB|B, false, packet:6)) == (LB|B), "Closing the dice window clears capture");
        filterReset.Invoke(filter, null);
        uint first = Packet(Filter(LB|B, true, packet:8));
        Check(Packet(Filter(LB|B, true, packet:8)) == first, "Stable packet number for unchanged filtered state");
        Check(Packet(Filter(0, true, packet:9)) != first, "Packet advances when effective input changes");
        filterReset.Invoke(filter, null);
        Filter(LB|B, true, index:0);
        Check(Buttons(Filter(B, true, index:1)) == B, "Controller capture state is independent");
        filterType.GetMethod("Disconnected").Invoke(filter, new object[] { 0u });
        Check(Buttons(Filter(B, true, index:0)) == B, "Reconnect does not retain a stale captured B");

        short[] axes = { -32768, -16000, -10000, -8000, 0, 8000, 10000, 16000, 32767 };
        foreach (short x in axes) foreach (short y in axes)
        {
            filterReset.Invoke(filter, null);
            var outside = Filter(LB|A|X, false, x:x, y:y);
            Check(LeftX(outside) == x && LeftY(outside) == y, "Both left-stick axes unchanged outside window");
            var captured = Filter(LB|A|X, true, x:x, y:y);
            Check(LeftX(captured) == 0 && LeftY(captured) == 0, "Left-stick axes captured while adjusting");
            Check(Buttons(captured) == (A|X), "A/X untouched during stick adjustment");
            Check((short)stateType.GetField("RightX").GetValue(captured) == -1234
                && (short)stateType.GetField("RightY").GetValue(captured) == 4321, "Right stick untouched");
            Check((byte)stateType.GetField("LeftTrigger").GetValue(captured) == 99
                && (byte)stateType.GetField("RightTrigger").GetValue(captured) == 88, "Both triggers untouched");
            filterReset.Invoke(filter, null);
            var ordinary = Filter(A|X, true, x:x, y:y);
            Check(LeftX(ordinary) == x && LeftY(ordinary) == y, "Stick unchanged without LB");
        }
        filterReset.Invoke(filter, null);
        Filter(LB, true, x:32767, y:-16000);
        var heldAfterLB = Filter(0, true, x:32767, y:-16000);
        Check(LeftX(heldAfterLB) == 0 && LeftY(heldAfterLB) == 0, "Stick stays captured when LB is released first");
        Check(LeftX(Filter(0, true, x:8001)) == 0, "Stick latch persists outside neutral threshold");
        Check(LeftX(Filter(0, true, x:8000)) == 8000, "Neutral stick releases latch");
        Check(LeftX(Filter(0, true, x:32767)) == 32767, "Next tilt returns to original game input");
        Filter(LB, true, x:32767);
        Check(LeftX(Filter(0, true, index:1, x:32767)) == 32767, "Stick latch is per controller");
        filterType.GetMethod("Disconnected").Invoke(filter, new object[] { 0u });
        Check(LeftX(Filter(0, true, x:32767)) == 32767, "Disconnect clears stick latch");
        filterReset.Invoke(filter, null);
        var hidden = Filter(LB, true, packet:55, x:32767);
        var shown = Filter(LB, false, packet:55, x:32767);
        Check(LeftX(shown) == 32767, "Closing dice window restores stick immediately");
        Check(Packet(hidden) != Packet(shown), "Packet updates when a stick is restored without physical input change");

        if (!OperatingSystem.IsWindows())
        {
            var bridge = assembly.GetType("DiceMaster.Gamepad.GamepadBridge", true);
            var bridgeTick = bridge.GetMethod("Tick");
            Check((int)bridgeTick.Invoke(null, new object[]{ false, true, 17, 0f, Path.GetTempPath() }) == 17, "Disabled config keeps original value");
            Check((int)bridgeTick.Invoke(null, new object[]{ true, true, 17, 0f, Path.GetTempPath() }) == 17, "Unsupported platform fails without changing value");
            Check(bridge.GetMethod("ConsumeWarning").Invoke(null, null) != null, "Unsupported platform logs once");
            Check(bridge.GetMethod("ConsumeWarning").Invoke(null, null) == null, "No repeated warning");
            bridge.GetMethod("Shutdown").Invoke(null, null);
        }
        Console.WriteLine(label + ": signed adjustment and integer bounds, stick adjustment/capture, all digital button combinations, release order, ABI, and shutdown pass.");
    }
    public static void Main(string[] args)
    {
        foreach (string p in args) Run(p, Path.GetFileName(p));
        Console.WriteLine("PASS: " + assertions + " assertions.");
    }
}
