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
        // An actual pointer takeover keeps its fresh press through Update.
        public bool PreservePointerTransaction;
        // A physical pad reacquisition cancels confirmations, but keeps the
        // list/tools area. This never grants authority to an unfinished click.
        public bool PreserveNavigationArea;
    }

    // Pure state machine: a fixed owner, fresh presses for actions, directional
    // repeat only, and a full neutral boundary before accepting transitions.
    public sealed class LogPadController
    {
        private readonly PadStick stick = new PadStick();
        // A real lifetime/owner boundary wins over same-frame input. Automatic
        // scheme changes can be observed before their touch's OnGUI down, so
        // they retain the established pointer/entry-barrier preservation path.
        private enum ResetKind { None, Navigation, Pointer, Hard }
        private ResetKind pendingReset;
        private int owner = -1;
        private bool wasOpen, wasFocused, wasReady, ownerConnected, waiting = true;
        private bool padOverride, pointerPending, keyboardPending, padEntry;
        private int openingOwner = -1;
        private LogInputMode previousMode;
        private ushort previousButtons, previousDirection;
        private double repeatAt;
        public int Owner { get { return owner; } }
        public bool Waiting { get { return waiting; } }
        public bool PadMode { get { return previousMode != LogInputMode.KeyboardMouse || padOverride; } }
        public void Cancel() => Cancel(ResetKind.Hard);
        private void Cancel(ResetKind kind)
        {
            waiting = true;
            if ((int)kind > (int)pendingReset) pendingReset = kind;
            previousButtons = previousDirection = 0; repeatAt = 0; stick.Reset();
        }
        public void PointerActivity() { pointerPending = true; }
        // Unlocking the cursor can produce hover motion without a deliberate
        // device takeover. An explicit pad opener retains UI ownership until
        // an actual click/touch/wheel/key or a hard lifetime boundary.
        public void PointerMotion() { if (!padEntry && openingOwner < 0) PointerActivity(); }
        public void KeyboardActivity() { keyboardPending = true; }
        public void BeginPadOpen(int index)
        {
            if (index < 0 || index >= 4) return;
            openingOwner = index;
            Cancel();
        }
        public void CancelPadEntry()
        { openingOwner = -1; padEntry = padOverride = false; Cancel(); }
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
            // A pad opener is stronger evidence of the log's input owner than
            // an automatic game scheme update. The game's scheme is read only;
            // other entry paths retain the existing device transition barrier.
            if (hardBoundary || (mode != previousMode && !padEntry))
            {
                Cancel(hardBoundary ? ResetKind.Hard : ResetKind.Pointer); padOverride = false;
                if (hardBoundary) padEntry = false;
                if (!open) { owner = -1; ownerConnected = false; }
            }
            if (pointerPending || keyboardPending)
            {
                // Consume held pad actions while a mouse/touch/key transaction
                // takes over, including when the game's scheme stays Gamepad.
                Cancel(ResetKind.Pointer);
                // Keyboard commands already run synchronously in OnGUI. Do
                // not erase the focus they just established in the next Update.
                // An independent hard boundary must still discard that input.
                if (keyboardPending && pendingReset != ResetKind.Hard) pendingReset = ResetKind.None;
                padEntry = padOverride = false; pointerPending = keyboardPending = false;
            }
            if (openingOwner >= 0)
            {
                if (open && focused && ready)
                {
                    owner = openingOwner;
                    ownerConnected = connected[owner];
                    padEntry = padOverride = true;
                    Cancel();
                }
                openingOwner = -1;
            }
            wasOpen = open; wasFocused = focused; wasReady = ready; previousMode = mode;
            if (open && owner < 0)
            {
                for (int i = 0; i < 4; i++) if (connected[i]) { owner = i; ownerConnected = true; Cancel(); break; }
            }
            if (owner >= 0 && ownerConnected != connected[owner])
            {
                ownerConnected = connected[owner]; padEntry = padOverride = false; Cancel();
            }
            frame.ResetFocus = pendingReset != ResetKind.None;
            frame.PreservePointerTransaction = pendingReset == ResetKind.Pointer;
            frame.PreserveNavigationArea = pendingReset == ResetKind.Navigation;
            pendingReset = ResetKind.None;
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
                    padOverride = true; Cancel(ResetKind.Navigation); frame.ResetFocus = true;
                    frame.PreservePointerTransaction = false;
                    frame.PreserveNavigationArea = true;
                    pendingReset = ResetKind.None;
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
