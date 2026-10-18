using System;

namespace DiceMaster.Gamepad
{
    public enum LogInputMode { Unknown, KeyboardMouse, Gamepad }
    public enum LogPadCommand { None, Up, Down, Left, Right, Activate, Back, Tools, Refresh, PreviousTab, NextTab }
    public static class LogActionPhases
    {
        // InputActionPhase.Started=2, Performed=3, Canceled=4. State cleanup
        // remains visible while gameplay activation callbacks are captured.
        public static bool Allow(bool active, bool releasing, int phase)
        {
            return !(active || releasing) || (phase != 2 && phase != 3);
        }
    }
    public struct LogPadFrame
    {
        public LogPadCommand Command;
        public float Scroll;
        public bool ResetFocus;
        // A device-mode transition still cancels pad input, but must not tear
        // down a fresh pointer press owned by the open log.
        public bool PreservePointerTransaction;
    }

    // Pure state machine: a fixed owner, fresh presses for actions, directional
    // repeat only, and a full neutral boundary before accepting transitions.
    public sealed class LogPadController
    {
        private readonly PadStick stick = new PadStick();
        private int owner = -1;
        private bool wasOpen, wasFocused, wasReady, ownerConnected, waiting = true, resetPending, hardResetPending;
        private bool padOverride, pointerPending, keyboardPending;
        private LogInputMode previousMode;
        private ushort previousButtons, previousDirection;
        private double repeatAt;
        public int Owner { get { return owner; } }
        public bool Waiting { get { return waiting; } }
        public bool PadMode { get { return previousMode != LogInputMode.KeyboardMouse || padOverride; } }
        public void Cancel() => Cancel(false);
        private void Cancel(bool preservePointerTransaction)
        {
            waiting = true; resetPending = true;
            if (!preservePointerTransaction) hardResetPending = true;
            previousButtons = previousDirection = 0; repeatAt = 0; stick.Reset();
        }
        public void PointerActivity() { pointerPending = true; }
        public void KeyboardActivity() { keyboardPending = true; }
        public void Reselect(bool[] connected)
        {
            for (int offset = 1; offset <= 4; offset++)
            {
                int candidate = (owner + offset + 4) % 4;
                if (connected[candidate]) { owner = candidate; ownerConnected = true; break; }
            }
            Cancel();
        }
        public LogPadFrame Tick(bool open, bool focused, bool ready, LogInputMode mode,
            double time, PadState[] states, bool[] connected)
        {
            LogPadFrame frame = new LogPadFrame();
            bool hardBoundary = open != wasOpen || focused != wasFocused || ready != wasReady;
            if (hardBoundary || mode != previousMode)
            {
                Cancel(!hardBoundary); padOverride = false;
                if (!open) { owner = -1; ownerConnected = false; }
            }
            if (pointerPending || keyboardPending)
            {
                // Consume held pad actions while a mouse/touch/key transaction
                // takes over, including when the game's scheme stays Gamepad.
                Cancel(true);
                // Keyboard commands already run synchronously in OnGUI. Do
                // not erase the focus they just established in the next Update.
                // An independent hard boundary must still discard that input.
                if (keyboardPending && !hardResetPending) resetPending = false;
                padOverride = false; pointerPending = keyboardPending = false;
            }
            wasOpen = open; wasFocused = focused; wasReady = ready; previousMode = mode;
            if (open && owner < 0)
            {
                for (int i = 0; i < 4; i++) if (connected[i]) { owner = i; ownerConnected = true; Cancel(); break; }
            }
            if (owner >= 0 && ownerConnected != connected[owner])
            {
                ownerConnected = connected[owner]; Cancel();
            }
            frame.ResetFocus = resetPending;
            frame.PreservePointerTransaction = resetPending && !hardResetPending;
            resetPending = hardResetPending = false;
            if (!open || !focused || !ready || owner < 0 || !ownerConnected) return frame;
            PadState state = states[owner];
            if (waiting)
            {
                if (PadInputFilter.IsNeutral(state)) waiting = false;
                previousButtons = state.Buttons;
                return frame;
            }
            // Suppression can freeze the game's own scheme detector. A fresh
            // physical pad transition may take over without changing game state;
            // consume that transition, then require neutral and a new press.
            if (!PadMode)
            {
                if (!PadInputFilter.IsNeutral(state))
                {
                    padOverride = true; Cancel(); frame.ResetFocus = true;
                    frame.PreservePointerTransaction = false;
                    resetPending = hardResetPending = false;
                }
                return frame;
            }
            ushort pressed = (ushort)(state.Buttons & ~previousButtons);
            previousButtons = state.Buttons;
            if ((pressed & PadButtons.B) != 0) frame.Command = LogPadCommand.Back;
            else if ((pressed & PadButtons.Y) != 0) frame.Command = LogPadCommand.Tools;
            else if ((pressed & PadButtons.X) != 0) frame.Command = LogPadCommand.Refresh;
            else if ((pressed & PadButtons.LB) != 0) frame.Command = LogPadCommand.PreviousTab;
            else if ((pressed & PadButtons.RB) != 0) frame.Command = LogPadCommand.NextTab;
            else if ((pressed & PadButtons.A) != 0) frame.Command = LogPadCommand.Activate;
            ushort dpad = (ushort)(state.Buttons & PadButtons.Directions);
            ushort direction = dpad != 0 ? dpad : stick.Read(state.LeftX, state.LeftY);
            if (direction == 0) { previousDirection = 0; repeatAt = 0; }
            else if (frame.Command == LogPadCommand.None && (direction != previousDirection || time >= repeatAt))
            {
                repeatAt = time + (direction == previousDirection ? 0.09 : 0.35);
                frame.Command = (direction & PadButtons.Up) != 0 ? LogPadCommand.Up
                    : (direction & PadButtons.Down) != 0 ? LogPadCommand.Down
                    : (direction & PadButtons.Left) != 0 ? LogPadCommand.Left : LogPadCommand.Right;
                previousDirection = direction;
            }
            // Actions never repeat and cannot cascade into a different target.
            if (frame.Command != LogPadCommand.None && frame.Command != LogPadCommand.Up && frame.Command != LogPadCommand.Down
                && frame.Command != LogPadCommand.Left && frame.Command != LogPadCommand.Right)
            {
                waiting = true; stick.Reset(); previousDirection = 0;
            }
            if (Math.Abs((int)state.RightY) > PadStick.ReleaseThreshold)
                frame.Scroll = -(float)state.RightY / 32768f;
            return frame;
        }
    }
}
