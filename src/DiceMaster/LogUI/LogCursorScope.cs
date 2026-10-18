using DiceMaster.Gamepad;
using UnityEngine;

namespace DiceMaster.LogUI
{
    // Temporary pointer access belongs to the visible log's lifetime. This is
    // independent of reading the game's scheme and never calls game controls,
    // changes action maps, or writes a game input mode.
    internal sealed class LogCursorScope
    {
        private bool ownsCursor;
        private CursorLockMode restoreLock;
        private bool restoreVisible;
        private LogInputMode lastKnownMode = LogInputMode.Unknown;

        internal void SetActive(bool active, LogInputMode mode)
        {
            if (!ownsCursor)
            {
                if (!active) return;
                // Preserve custom Confined/visibility settings even when the
                // current known scheme has a different usual cursor policy.
                restoreLock = Cursor.lockState;
                restoreVisible = Cursor.visible;
                lastKnownMode = IsKnown(mode) ? mode : LogInputMode.Unknown;
                ownsCursor = true;
            }
            else
            {
                bool observedRequest = ObserveExternalRequest();
                ObserveMode(mode, !observedRequest);
            }

            if (!active)
            {
                Restore();
                return;
            }

            // Reapply only when another writer has changed the state. The log
            // does not repeatedly relock/unlock an already accessible cursor.
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;
        }

        private static bool IsKnown(LogInputMode mode)
        {
            return mode == LogInputMode.Gamepad || mode == LogInputMode.KeyboardMouse;
        }

        private bool ObserveExternalRequest()
        {
            CursorLockMode currentLock = Cursor.lockState;
            bool currentVisible = Cursor.visible;
            // None/visible is our temporary state. Re-observing it must not
            // overwrite the real restoration target on the next frame.
            if (currentLock == CursorLockMode.None && currentVisible) return false;
            restoreLock = currentLock;
            restoreVisible = currentVisible;
            return true;
        }

        private void ObserveMode(LogInputMode mode, bool inferCursorPolicy)
        {
            if (!IsKnown(mode)) return;
            // Recovery from an initially unknown lookup only establishes the
            // baseline. It does not prove that the game changed its scheme.
            // An observed game request is stronger evidence than the usual
            // policy inferred from its scheme, including on the closing tick.
            // Still update the baseline so the following unchanged-mode frame
            // cannot replace that exact request with a deferred inference.
            if (inferCursorPolicy && IsKnown(lastKnownMode) && mode != lastKnownMode)
            {
                restoreLock = mode == LogInputMode.Gamepad ? CursorLockMode.Locked : CursorLockMode.None;
                restoreVisible = mode == LogInputMode.KeyboardMouse;
            }
            lastKnownMode = mode;
        }

        internal void Release()
        {
            if (!ownsCursor) return;
            // A final game write may occur after the last active Update and
            // before close, focus loss, or plugin destruction.
            ObserveExternalRequest();
            Restore();
        }

        private void Restore()
        {
            ownsCursor = false;
            if (Cursor.lockState != restoreLock) Cursor.lockState = restoreLock;
            if (Cursor.visible != restoreVisible) Cursor.visible = restoreVisible;
            lastKnownMode = LogInputMode.Unknown;
        }
    }
}
