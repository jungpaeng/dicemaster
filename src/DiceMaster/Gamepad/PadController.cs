using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace DiceMaster.Gamepad
{
    public static class PadButtons
    {
        public const ushort Up = 0x0001, Down = 0x0002, Left = 0x0004, Right = 0x0008;
        public const ushort LeftThumb = 0x0040, RightThumb = 0x0080, ThumbClicks = LeftThumb | RightThumb;
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
        private bool openerEnabled, openerFocused, openerOpen, openerOpening;
        private int pendingOpen = -1;
        private readonly bool[] openerAwaitNeutral = { true, true, true, true };
        private readonly ushort[] openerFirst = new ushort[4];

        public bool LogOpening { get { lock (gate) return openerOpening; } }

        public void ConfigureLogOpener(bool enabled, bool focused, bool open)
        {
            lock (gate)
            {
                if (enabled == openerEnabled && focused == openerFocused && open == openerOpen) return;
                bool reserved = openerEnabled && openerFocused;
                // Hand off without a frame in which the gesture is released to
                // the game before the ordinary log owner takes over.
                if (open && focused && openerOpening) logCapture = true;
                ResetOpener(reserved || openerOpening);
                openerEnabled = enabled; openerFocused = focused; openerOpen = open;
                if (enabled && focused)
                    foreach (Slot slot in slots) slot.AwaitNeutral = true;
            }
        }

        public bool TryConsumeLogOpen(out int owner)
        {
            lock (gate)
            {
                owner = -1;
                if (!openerEnabled || !openerFocused || openerOpen || pendingOpen < 0) return false;
                owner = pendingOpen;
                pendingOpen = -1;
                // The consumer may invalidate pad navigation before its next
                // TickLog. Commit capture now so that cancellation cannot open
                // a gap between native recognition and UI opening.
                logCapture = true;
                return true;
            }
        }

        public void CancelLogOpener()
        {
            lock (gate) ResetOpener(openerOpening || openerEnabled && openerFocused);
        }

        private void ResetOpener(bool requireGameNeutral)
        {
            pendingOpen = -1; openerOpening = false;
            for (int i = 0; i < 4; i++)
            {
                openerFirst[i] = 0; openerAwaitNeutral[i] = true;
                if (requireGameNeutral) slots[i].AwaitNeutral = true;
            }
        }

        private void ObserveOpener(uint index, PadState raw)
        {
            if (!openerEnabled || !openerFocused || openerOpen || openerOpening || logCapture) return;
            if (openerAwaitNeutral[index])
            {
                if (IsNeutral(raw)) openerAwaitNeutral[index] = false;
                return;
            }
            ushort thumbs = (ushort)(raw.Buttons & PadButtons.ThumbClicks);
            bool invalid = (raw.Buttons & ~PadButtons.ThumbClicks) != 0
                || raw.LeftTrigger > 30 || raw.RightTrigger > 30
                || !PadStick.IsNeutral(raw.LeftX, raw.LeftY) || !PadStick.IsNeutral(raw.RightX, raw.RightY);
            if (invalid)
            {
                openerFirst[index] = 0; openerAwaitNeutral[index] = true;
                return;
            }
            if (thumbs == 0)
            {
                openerFirst[index] = 0;
                return;
            }
            if (openerFirst[index] == 0)
            {
                openerFirst[index] = thumbs;
            }
            else if ((thumbs & openerFirst[index]) == 0)
            {
                // Sequential non-overlapping clicks are not a chord.
                openerFirst[index] = 0; openerAwaitNeutral[index] = true;
                return;
            }
            if (thumbs != PadButtons.ThumbClicks) return;
            // The first click may be held indefinitely. Only overlap matters;
            // ordering, packet count and elapsed time do not gate this chord.
            pendingOpen = (int)index; openerOpening = true;
            foreach (Slot slot in slots) slot.AwaitNeutral = true;
        }

        // Only an actual game caller invokes this path. UI raw polling and the
        // compatibility Filter method cannot create or advance an open request.
        public PadState FilterGame(uint index, PadState state, bool windowActive)
        {
            if (index >= 4) return state;
            lock (gate)
            {
                ObserveOpener(index, state);
                return Filter(index, state, windowActive);
            }
        }

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
                if (active && openerOpening) ResetOpener(true);
            }
        }
        // Used on focus/mode/context boundaries, including previously blocked
        // non-owner pads. Reconnection cannot turn a held input into a new press.
        public void RequireNeutral()
        {
            lock (gate)
            {
                ResetOpener(false);
                foreach (Slot slot in slots) slot.AwaitNeutral = true;
            }
        }
        public bool CanUseRaw(uint index)
        {
            if (index >= 4) return false;
            lock (gate)
            {
                Slot slot = slots[index];
                return !logCapture && !openerOpening && !slot.AwaitNeutral && slot.LogButtons == 0
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
            if (logCapture || openerOpening) Capture(slot, raw);
            // These two clicks are reserved even when the chord is incomplete.
            // Preserve that latch through configuration/focus changes too.
            if (openerEnabled && openerFocused) slot.LogButtons |= (ushort)(raw.Buttons & PadButtons.ThumbClicks);
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
                if (logCapture || openerOpening || slot.AwaitNeutral)
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
                openerEnabled = openerFocused = openerOpen = false;
                ResetOpener(false);
                for (int i = 0; i < slots.Length; i++) slots[i] = new Slot();
            }
        }
        public void Disconnected(uint index)
        {
            if (index >= 4) return;
            lock (gate)
            {
                Slot old = slots[index];
                bool wasOpening = openerOpening;
                if (wasOpening && pendingOpen == (int)index) ResetOpener(true);
                openerFirst[index] = 0; openerAwaitNeutral[index] = true;
                slots[index] = new Slot { AwaitNeutral = logCapture || wasOpening || old.AwaitNeutral || old.LogButtons != 0
                    || old.LogLeft || old.LogRight || old.LogLT || old.LogRT };
            }
        }
    }
}
