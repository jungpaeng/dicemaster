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
        int Step(bool active, int buttons, double time, int current) => (int)tick.Invoke(adjustment, new object[] { active, (ushort)buttons, time, current });
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
        Check(Step(true, LB|Down, 0.52, 0) == 1, "Automatic transitions to positive fixed value");
        reset.Invoke(adjustment, null);
        Check(Step(true, LB|Right, 1, Int32.MaxValue) == Int32.MaxValue, "No integer overflow");
        Check(Step(true, LB|Left, 2, 1) == 1, "Zero reserved for automatic mode");
        Check(Step(true, LB, 3, -50) == -50, "Existing keyboard value preserved without a step");

        var stateType = assembly.GetType("DiceMaster.Gamepad.PadState", true);
        var filterType = assembly.GetType("DiceMaster.Gamepad.PadInputFilter", true);
        Check(Marshal.SizeOf(stateType) == 16, "XINPUT_STATE ABI size");
        var filter = Activator.CreateInstance(filterType); var filterMethod = filterType.GetMethod("Filter");
        var filterReset = filterType.GetMethod("Reset");
        var buttonsField = stateType.GetField("Buttons"); var packetField = stateType.GetField("Packet");
        object State(int buttons, uint packet = 1)
        {
            var state = Activator.CreateInstance(stateType);
            buttonsField.SetValue(state, (ushort)buttons); packetField.SetValue(state, packet);
            stateType.GetField("LeftTrigger").SetValue(state, (byte)99);
            stateType.GetField("RightX").SetValue(state, (short)-1234);
            return state;
        }
        object Filter(int buttons, bool active, uint index = 0, uint packet = 1) => filterMethod.Invoke(filter, new object[] { index, State(buttons, packet), active });
        int Buttons(object value) => (ushort)buttonsField.GetValue(value);
        uint Packet(object value) => (uint)packetField.GetValue(value);
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
        Console.WriteLine(label + ": input behavior, all digital button combinations, release order, ABI, and shutdown pass.");
    }
    public static void Main(string[] args)
    {
        foreach (string p in args) Run(p, Path.GetFileName(p));
        Console.WriteLine("PASS: " + assertions + " assertions.");
    }
}
