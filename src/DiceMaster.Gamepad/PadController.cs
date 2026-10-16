using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace DiceMaster.Gamepad
{
    public static class PadButtons
    {
        public const ushort Up = 0x0001, Down = 0x0002, Left = 0x0004, Right = 0x0008;
        public const ushort LB = 0x0100, B = 0x2000;
        public const ushort Captured = Up | Down | Left | Right | LB | B;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PadState
    {
        public uint Packet;
        public ushort Buttons;
        public byte LeftTrigger, RightTrigger;
        public short LeftX, LeftY, RightX, RightY;
    }

    // Independent of Unity and native input; all timing uses unscaled seconds.
    public sealed class PadAdjustment
    {
        private int direction;
        private double repeatAt;
        private bool waitForNeutral;

        public void Reset() { direction = 0; repeatAt = 0; waitForNeutral = false; }

        public int Tick(bool active, ushort buttons, double time, int current)
        {
            if (!active || (buttons & PadButtons.LB) == 0) { Reset(); return current; }
            if ((buttons & PadButtons.B) != 0) { Reset(); waitForNeutral = true; return 0; }
            int delta = ((buttons & PadButtons.Right) != 0 ? 1 : 0)
                - ((buttons & PadButtons.Left) != 0 ? 1 : 0)
                + ((buttons & PadButtons.Up) != 0 ? 10 : 0)
                - ((buttons & PadButtons.Down) != 0 ? 10 : 0);
            if (waitForNeutral && delta != 0) return current;
            if (delta == 0) { Reset(); return current; }
            if (delta == direction && time < repeatAt) return current;
            repeatAt = time + (delta == direction ? 0.08 : 0.35);
            direction = delta;
            // Zero is reserved for automatic rolls. Preserve the existing mod's
            // ability to force positive values above 100, without integer wrap.
            long next = (long)current + delta;
            return next < 1 ? 1 : next > Int32.MaxValue ? Int32.MaxValue : (int)next;
        }
    }

    // Each physical button remains hidden until released, even if LB is released
    // first. This prevents a delayed B/DPAD press from reaching the original UI.
    public sealed class PadInputFilter
    {
        private readonly object gate = new object();
        private readonly ushort[] latched = new ushort[4];
        private readonly uint[] packets = new uint[4];
        private readonly uint[] originalPackets = new uint[4];
        private readonly ushort[] previousButtons = new ushort[4];
        private readonly bool[] initialized = new bool[4];
        private readonly bool[] virtualized = new bool[4];

        public PadState Filter(uint index, PadState state, bool windowActive)
        {
            if (index >= 4) return state;
            lock (gate)
            {
                ushort raw = state.Buttons;
                uint rawPacket = state.Packet;
                if (!windowActive) latched[index] = 0;
                else
                {
                    latched[index] &= raw;
                    if ((raw & PadButtons.LB) != 0) latched[index] |= (ushort)(raw & PadButtons.Captured);
                }
                ushort filtered = (ushort)(raw & ~latched[index]);
                if (!initialized[index])
                {
                    packets[index] = state.Packet;
                    originalPackets[index] = state.Packet;
                    previousButtons[index] = raw;
                    initialized[index] = true;
                }
                if (filtered != raw) virtualized[index] = true;
                if (virtualized[index])
                {
                    if (state.Packet != originalPackets[index] || filtered != previousButtons[index])
                        packets[index] = unchecked(packets[index] + 1);
                    state.Packet = packets[index];
                }
                originalPackets[index] = rawPacket;
                previousButtons[index] = filtered;
                state.Buttons = filtered;
                return state;
            }
        }

        public void Reset()
        {
            lock (gate)
            {
                Array.Clear(latched, 0, 4); Array.Clear(initialized, 0, 4);
                Array.Clear(virtualized, 0, 4);
            }
        }

        public void Disconnected(uint index)
        {
            if (index >= 4) return;
            lock (gate)
            {
                latched[index] = 0; initialized[index] = false; virtualized[index] = false;
            }
        }
    }

    public static class GamepadBridge
    {
        private static readonly PadAdjustment adjustment = new PadAdjustment();
        private static readonly NativeXInput native = new NativeXInput();
        private static bool initialized, failed, visible, connected, ready, adjusting;
        private static int controller = -1;
        private static string warning;
        private static int value;

        public static int Tick(bool enabled, bool windowVisible, int current, float time, string cacheRoot)
        {
            visible = enabled && windowVisible;
            value = current;
            if (!enabled) { native.WindowActive = false; ResetAdjustment(); return current; }
            if (!initialized)
            {
                initialized = true;
                try { native.Initialize(cacheRoot); }
                catch (Exception e) { failed = true; warning = "패드 조정을 시작하지 못했습니다: " + e.Message; }
            }
            native.WindowActive = visible && !failed;
            if (failed) { ResetAdjustment(); return current; }
            // Do not activate controls if the game is not actually polling the
            // filtered XInput path. Avoid modifying values without capture.
            ready = native.GamePollingObserved;
            PadState state = default(PadState);
            bool found = false;
            int selected = -1;
            for (uint i = 0; i < 4; i++)
            {
                PadState next;
                if (!native.ReadRaw(i, out next)) continue;
                if (!found || (next.Buttons & PadButtons.LB) != 0)
                {
                    found = true; state = next; selected = (int)i;
                    if ((next.Buttons & PadButtons.LB) != 0) break;
                }
            }
            connected = found;
            if (selected != controller) { adjustment.Reset(); controller = selected; }
            adjusting = visible && ready && connected && (state.Buttons & PadButtons.LB) != 0;
            value = adjustment.Tick(adjusting, state.Buttons, time, current);
            return value;
        }

        public static bool ShowHint { get { return visible; } }
        public static string Hint()
        {
            if (failed) return "패드 조정을 사용할 수 없습니다. 로그를 확인해 주세요.";
            if (!connected) return "패드를 연결하면 LB를 누른 채 주사위 수치를 조정할 수 있습니다.";
            if (!ready) return "게임의 패드 입력을 기다리는 중입니다.";
            string current = value == 0 ? "자동" : value.ToString(CultureInfo.InvariantCulture);
            return "주사위: " + current + (adjusting ? "  [LB 조정 중]" : "")
                + "\nLB + ←/→: ±1   ↑/↓: ±10   LB+B: 자동";
        }
        public static string ConsumeWarning() { string result = warning; warning = null; return result; }
        public static void ResetAdjustment() { adjustment.Reset(); adjusting = false; controller = -1; }
        public static void Shutdown()
        {
            native.WindowActive = false; native.Dispose();
            visible = ready = connected = adjusting = false;
            ResetAdjustment();
        }
    }
}
