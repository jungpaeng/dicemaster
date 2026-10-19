using System;
using DiceMaster.Gamepad;

namespace DiceMaster.LogUI
{
    internal enum ModalInputResetReason
    {
        AutomaticInputModeObserved,
        PointerTakeover,
        PhysicalPadTakeover,
        ControllerBoundary,
        LogVisibilityChanged,
        ApplicationFocusChanged,
        PopupWaitingChanged
    }

    internal readonly struct ModalInputReset
    {
        internal readonly bool PreservePointerTransaction;
        internal readonly bool CancelConfirmation;

        internal ModalInputReset(bool preservePointerTransaction, bool cancelConfirmation)
        {
            PreservePointerTransaction = preservePointerTransaction;
            CancelConfirmation = cancelConfirmation;
        }
    }

    // An observed device mode is weaker evidence than a real input-owner or
    // lifetime boundary. Keep that distinction separate from renderer state
    // and from the runtime's context/token invalidation rules.
    internal static class ModalInputResetPolicy
    {
        internal static ModalInputReset For(ModalInputResetReason reason)
        {
            switch (reason)
            {
                case ModalInputResetReason.AutomaticInputModeObserved:
                case ModalInputResetReason.PointerTakeover:
                    return new ModalInputReset(true, false);
                case ModalInputResetReason.PhysicalPadTakeover:
                case ModalInputResetReason.ControllerBoundary:
                case ModalInputResetReason.LogVisibilityChanged:
                case ModalInputResetReason.ApplicationFocusChanged:
                    return new ModalInputReset(false, true);
                case ModalInputResetReason.PopupWaitingChanged:
                    return new ModalInputReset(false, false);
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason));
            }
        }

        // LogPadController remains the source of owner/navigation state. This
        // translates its existing flags without expanding that public API.
        internal static ModalInputResetReason FromPadFrame(LogPadFrame frame)
        {
            if (frame.PreservePointerTransaction) return ModalInputResetReason.PointerTakeover;
            return frame.PreserveNavigationArea ? ModalInputResetReason.PhysicalPadTakeover
                : ModalInputResetReason.ControllerBoundary;
        }
    }
}
