using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace DiceMaster.Gamepad
{
    public static class PadButtons
    {
        public const ushort Up = 0x0001, Down = 0x0002, Left = 0x0004, Right = 0x0008;
        public const ushort LB = 0x0100, RB = 0x0200, A = 0x1000, B = 0x2000, X = 0x4000, Y = 0x8000;
        public const ushort Directions = Up | Down | Left | Right;
        public const ushort Captured = Directions | LB | B;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PadState
    {
        public uint Packet;
        public ushort Buttons;
        public byte LeftTrigger, RightTrigger;
        public short LeftX, LeftY, RightX, RightY;
    }

    public sealed class PadStick
    {
        public const int PressThreshold = 16000, ReleaseThreshold = 10000, NeutralThreshold = 8000;
        private ushort direction;

        public void Reset() { direction = 0; }
        public static bool IsNeutral(short x, short y)
        {
            return Math.Abs((int)x) <= NeutralThreshold && Math.Abs((int)y) <= NeutralThreshold;
        }
        public ushort Read(short x, short y)
        {
            int horizontal = Math.Abs((int)x), vertical = Math.Abs((int)y);
            // Select one direction on diagonals rather than adding a +/-11 step.
            ushort candidate = horizontal >= vertical
                ? (x >= 0 ? PadButtons.Right : PadButtons.Left)
                : (y >= 0 ? PadButtons.Up : PadButtons.Down);
            int magnitude = Math.Max(horizontal, vertical);
            int threshold = candidate == direction ? ReleaseThreshold : PressThreshold;
            direction = magnitude >= threshold ? candidate : (ushort)0;
            return direction;
        }
    }

    // Independent of Unity and native input; all timing uses unscaled seconds.
    public sealed class PadAdjustment
    {
        private readonly PadStick stick = new PadStick();
        private int direction;
        private double repeatAt;
        private bool waitForNeutral;

        public void Reset() { direction = 0; repeatAt = 0; waitForNeutral = false; stick.Reset(); }

        public int Tick(bool active, ushort buttons, double time, int current, short leftX = 0, short leftY = 0)
        {
            if (!active || (buttons & PadButtons.LB) == 0) { Reset(); return current; }
            if ((buttons & PadButtons.B) != 0) { Reset(); waitForNeutral = true; return 0; }
            ushort dpad = (ushort)(buttons & PadButtons.Directions);
            ushort analog = stick.Read(leftX, leftY);
            ushort directions = dpad != 0 ? dpad : analog;
            if (waitForNeutral)
            {
                if (dpad == 0 && PadStick.IsNeutral(leftX, leftY)) Reset();
                return current;
            }
            int delta = ((directions & PadButtons.Right) != 0 ? 1 : 0)
                - ((directions & PadButtons.Left) != 0 ? 1 : 0)
                + ((directions & PadButtons.Up) != 0 ? 10 : 0)
                - ((directions & PadButtons.Down) != 0 ? 10 : 0);
            if (delta == 0) { Reset(); return current; }
            if (delta == direction && time < repeatAt) return current;
            repeatAt = time + (delta == direction ? 0.08 : 0.35);
            direction = delta;
            // Zero selects automatic rolls; negative overrides are also valid.
            // Preserve the full signed integer range without wrapping.
            long next = (long)current + delta;
            return next < Int32.MinValue ? Int32.MinValue : next > Int32.MaxValue ? Int32.MaxValue : (int)next;
        }
    }

    // Filtering is shared by the log and dice owners. The log always captures
    // every XInput channel on all four slots; release latches survive closing.
    public sealed class PadInputFilter
    {
        private readonly object gate = new object();
        private sealed class Slot
        {
            internal ushort DiceButtons, LogButtons;
            internal bool DiceStick, LogLeft, LogRight, LogLT, LogRT, AwaitNeutral;
            internal bool Initialized, Virtualized, Seen;
            internal uint Packet, OriginalPacket;
            internal PadState Previous, Raw;
        }
        private readonly Slot[] slots = { new Slot(), new Slot(), new Slot(), new Slot() };
        private bool logCapture;

        public static bool IsNeutral(PadState state)
        {
            return state.Buttons == 0 && state.LeftTrigger <= 30 && state.RightTrigger <= 30
                && PadStick.IsNeutral(state.LeftX, state.LeftY) && PadStick.IsNeutral(state.RightX, state.RightY);
        }
        private static void Capture(Slot slot, PadState state)
        {
            slot.LogButtons |= state.Buttons;
            slot.LogLeft |= !PadStick.IsNeutral(state.LeftX, state.LeftY);
            slot.LogRight |= !PadStick.IsNeutral(state.RightX, state.RightY);
            slot.LogLT |= state.LeftTrigger > 30; slot.LogRT |= state.RightTrigger > 30;
        }
        public void SetLogCapture(bool active)
        {
            lock (gate)
            {
                if (active && !logCapture)
                    foreach (Slot slot in slots) if (slot.Seen) Capture(slot, slot.Raw);
                logCapture = active;
            }
        }
        // Used on focus/mode/context boundaries, including previously blocked
        // non-owner pads. Reconnection cannot turn a held input into a new press.
        public void RequireNeutral()
        {
            lock (gate) foreach (Slot slot in slots) slot.AwaitNeutral = true;
        }
        public bool CanUseRaw(uint index)
        {
            if (index >= 4) return false;
            lock (gate)
            {
                Slot slot = slots[index];
                return !logCapture && !slot.AwaitNeutral && slot.LogButtons == 0
                    && !slot.LogLeft && !slot.LogRight && !slot.LogLT && !slot.LogRT;
            }
        }
        public void ObserveRaw(uint index, PadState state)
        {
            if (index >= 4) return;
            lock (gate) Observe(slots[index], state);
        }
        private void Observe(Slot slot, PadState raw)
        {
            slot.Raw = raw; slot.Seen = true;
            slot.LogButtons &= raw.Buttons;
            if (PadStick.IsNeutral(raw.LeftX, raw.LeftY)) slot.LogLeft = false;
            if (PadStick.IsNeutral(raw.RightX, raw.RightY)) slot.LogRight = false;
            if (raw.LeftTrigger <= 30) slot.LogLT = false;
            if (raw.RightTrigger <= 30) slot.LogRT = false;
            if (slot.AwaitNeutral && IsNeutral(raw)) slot.AwaitNeutral = false;
            if (logCapture) Capture(slot, raw);
        }
        public PadState Filter(uint index, PadState state, bool windowActive)
        {
            if (index >= 4) return state;
            lock (gate)
            {
                Slot slot = slots[index];
                PadState raw = state;
                Observe(slot, raw);
                if (!windowActive) { slot.DiceButtons = 0; slot.DiceStick = false; }
                else
                {
                    slot.DiceButtons &= raw.Buttons;
                    if ((raw.Buttons & PadButtons.LB) != 0)
                    {
                        slot.DiceButtons |= (ushort)(raw.Buttons & PadButtons.Captured);
                        slot.DiceStick = true;
                    }
                    else if (PadStick.IsNeutral(raw.LeftX, raw.LeftY)) slot.DiceStick = false;
                }
                if (logCapture || slot.AwaitNeutral)
                {
                    state.Buttons = 0; state.LeftTrigger = state.RightTrigger = 0;
                    state.LeftX = state.LeftY = state.RightX = state.RightY = 0;
                }
                else
                {
                    state.Buttons &= (ushort)~(slot.LogButtons | slot.DiceButtons);
                    if (slot.LogLeft || slot.DiceStick) state.LeftX = state.LeftY = 0;
                    if (slot.LogRight) state.RightX = state.RightY = 0;
                    if (slot.LogLT) state.LeftTrigger = 0;
                    if (slot.LogRT) state.RightTrigger = 0;
                }
                if (!slot.Initialized)
                {
                    slot.Packet = raw.Packet; slot.OriginalPacket = raw.Packet;
                    slot.Previous = raw; slot.Initialized = true;
                }
                if (!SameInput(raw, state)) slot.Virtualized = true;
                if (slot.Virtualized)
                {
                    if (raw.Packet != slot.OriginalPacket || !SameInput(slot.Previous, state))
                        slot.Packet = unchecked(slot.Packet + 1);
                    state.Packet = slot.Packet;
                }
                slot.OriginalPacket = raw.Packet; slot.Previous = state;
                return state;
            }
        }
        private static bool SameInput(PadState a, PadState b)
        {
            return a.Buttons == b.Buttons && a.LeftTrigger == b.LeftTrigger && a.RightTrigger == b.RightTrigger
                && a.LeftX == b.LeftX && a.LeftY == b.LeftY && a.RightX == b.RightX && a.RightY == b.RightY;
        }
        public void Reset()
        {
            lock (gate)
            {
                logCapture = false;
                for (int i = 0; i < slots.Length; i++) slots[i] = new Slot();
            }
        }
        public void Disconnected(uint index)
        {
            if (index >= 4) return;
            lock (gate)
            {
                Slot old = slots[index];
                slots[index] = new Slot { AwaitNeutral = logCapture || old.AwaitNeutral || old.LogButtons != 0
                    || old.LogLeft || old.LogRight || old.LogLT || old.LogRT };
            }
        }
    }
}
