using System;

namespace DiceMaster.EventSelection
{
    // Immutable authority for one explicitly chosen result. All game objects
    // are opaque here; admission and execution validate their live membership.
    public sealed class CheckpointReservation
    {
        public readonly bool IsSwitch, UseCount;
        public readonly object Manager, Config, Pool, Target, Choice;
        public readonly string Name, Result, Prefix;
        public readonly int Count;

        public CheckpointReservation(bool isSwitch, object manager, object config, object pool, object target,
            string name, string result, string prefix = "", bool useCount = false, int count = 0, object choice = null)
        {
            IsSwitch = isSwitch; Manager = manager; Config = config; Pool = pool; Target = target;
            Name = name ?? ""; Result = result ?? ""; Prefix = prefix ?? "";
            UseCount = useCount; Count = count; Choice = choice ?? this;
        }
    }

    // Like the mission slot, these survive ordinary story compilation but not
    // a load/new-game transition. They are runtime state, never config/save data.
    public static class CheckpointReservationStore
    {
        private static CheckpointReservation condition, position;
        public static CheckpointReservation Current(bool isSwitch) => isSwitch ? condition : position;

        public static CheckpointReservation Peek(bool isSwitch, object manager, string name)
        {
            CheckpointReservation value = Current(isSwitch);
            return value != null && ReferenceEquals(value.Manager, manager) &&
                string.Equals(value.Name, name, StringComparison.Ordinal) ? value : null;
        }

        public static void Set(CheckpointReservation value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            // Even re-reserving the same published row creates new authority.
            // A press spanning consume/reinsert cannot cancel this new request.
            var copy = new CheckpointReservation(value.IsSwitch, value.Manager, value.Config, value.Pool, value.Target,
                value.Name, value.Result, value.Prefix, value.UseCount, value.Count, value.Choice);
            if (value.IsSwitch) condition = copy; else position = copy;
        }

        public static bool Take(CheckpointReservation value)
        {
            if (value == null || !ReferenceEquals(Current(value.IsSwitch), value)) return false;
            if (value.IsSwitch) condition = null; else position = null;
            return true;
        }

        public static void Clear() { condition = position = null; }
    }
}
