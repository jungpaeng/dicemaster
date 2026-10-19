using System;
using System.Globalization;
using Mortal.Story;

namespace DiceMaster.EventSelection
{
    public static partial class EventSelectionRuntime
    {
        private static bool CanUseReservations()
        {
            try { return ReservationsEnabled != null && ReservationsEnabled(); }
            catch (Exception error) { Failure("reservation-settings", error); return false; }
        }

        // A reservation names the next matching checkpoint, independently of
        // popup settings or the supported shapes of yielding Lua expressions.
        // Its result is a deliberate one-use override; no eligibility reader
        // or mutable condition graph is involved in validating that intent.
        private static ActionContext ReservedAction(CheckpointReservation reservation)
        {
            if (reservation.Target == null) return null;
            var manager = reservation.Manager as CheckPointManager;
            if (manager == null || !ReferenceEquals(manager, CheckPointManager.Instance)) return null;
            var action = new ActionContext { Epoch = epoch, Manager = manager, Name = reservation.Name, Force = true };
            if (reservation.IsSwitch)
            {
                SwitchResultConfig config;
                SwitchResultData pool = ResolveSwitch(manager, reservation.Name, out config);
                if (pool == null || !ReferenceEquals(config, reservation.Config) || !ReferenceEquals(pool, reservation.Pool) ||
                    (bool)Field(pool, "_useCount") != reservation.UseCount || (int)Field(pool, "_count") != reservation.Count)
                    return null;
                action.SwitchConfig = config; action.SwitchPool = pool; action.SwitchUseCount = reservation.UseCount;
                action.SwitchCount = reservation.Count;
                int branch;
                if (!int.TryParse(reservation.Result, NumberStyles.None, CultureInfo.InvariantCulture, out branch) || branch < 1 ||
                    !ReferenceEquals(SwitchTarget(action, branch), reservation.Target)) return null;
                action.SwitchDirectTarget = reservation.Target; action.SwitchDirectResult = branch;
            }
            else
            {
                PositionResultConfig config;
                PositionResultData pool = Resolve(manager, reservation.Name, out config);
                if (pool == null || !ReferenceEquals(config, reservation.Config) || !ReferenceEquals(pool, reservation.Pool) ||
                    !string.Equals(Field(pool, "_scriptPrefix") as string, reservation.Prefix, StringComparison.Ordinal)) return null;
                action.Config = config; action.Pool = pool; action.Prefix = reservation.Prefix;
                action.DirectTarget = reservation.Target as PositionEventData;
                if (string.IsNullOrEmpty(reservation.Result) || Result(action, action.DirectTarget) != reservation.Result) return null;
                action.DirectResult = reservation.Result;
            }
            return action;
        }

        private static CheckpointReservation ValidReservation(bool isSwitch, CheckPointManager manager, string name, out ActionContext action)
        {
            action = null;
            if (!CanUseReservations()) return null;
            CheckpointReservation reservation = CheckpointReservationStore.Peek(isSwitch, manager, name);
            if (reservation == null) return null;
            try { action = ReservedAction(reservation); }
            catch (Exception error) { Failure("reservation-validation", error); }
            if (action != null) return reservation;
            // A stale matching request is discarded once, never allowed to
            // suppress all later popups or to adopt a replacement result.
            CheckpointReservationStore.Take(reservation);
            Log("reservation-discarded kind=" + (isSwitch ? "switch" : "event") + " pool=" + name);
            return null;
        }

        private static bool BypassForReservation(bool isSwitch, string name)
        {
            ActionContext ignored;
            if (ValidReservation(isSwitch, CheckPointManager.Instance, name, out ignored) == null) return false;
            // The caller has already checked this coroutine/script/site owns
            // the current popup. Do not increment Epoch: that would invalidate
            // the very Lua continuation which must consume the reservation.
            current = null; candidates = Empty; Status = ""; SetWaiting(false);
            return true;
        }

        private static ActionContextHandle BeginReserved(bool isSwitch, CheckPointManager manager, string name)
        {
            if (current != null) return null;
            ActionContext action;
            CheckpointReservation reservation = ValidReservation(isSwitch, manager, name, out action);
            if (reservation == null || !CheckpointReservationStore.Take(reservation)) return null;
            action.Token = checked(++sequence); action.Executing = true;
            current = action; candidates = Empty; Status = ""; SetWaiting(false);
            Log("reservation-consumed token=" + action.Token + " kind=" + (isSwitch ? "switch" : "event") + " pool=" + name);
            return new ActionContextHandle(action.Token);
        }
    }
}
