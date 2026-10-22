using System;
using System.Collections.Generic;

namespace DiceMaster.LiveLog
{
    // Private display metadata, never a Unity/game object or execution target.
    // Only the fingerprint-checked file reader creates these definitions. Live
    // reference targets remain the game's existing objects, not serialized State.
    internal sealed class DisplayDefinition
    {
        internal readonly string TypeName, Name;
        // Builders own this dictionary; bound views share the immutable raw
        // template. Runtime consumers must use Read, not inspect raw Fields.
        internal readonly Dictionary<string, object> Fields;
        private readonly DisplayDefinition template;
        private readonly Func<object, object> bind;
        private Dictionary<string, object> resolved;
        internal DisplayDefinition(string type, string name = "")
        { TypeName = type; Name = name; Fields = new Dictionary<string, object>(StringComparer.Ordinal); }
        internal DisplayDefinition(DisplayDefinition source, Func<object, object> binder)
        { TypeName = source.TypeName; Name = source.Name; template = source; Fields = source.Fields; bind = binder; }
        internal object Read(string name)
        {
            if (template == null) return Fields[name];
            object raw = Fields[name];
            if (raw == null || raw is int || raw is bool || raw is string) return raw;
            if (resolved == null || !resolved.TryGetValue(name, out object value))
            {
                // This private view belongs only to the current binding set.
                // Resolve identity lazily, never cache a native current value.
                value = bind(raw);
                if (resolved == null) resolved = new Dictionary<string, object>(StringComparer.Ordinal);
                resolved.Add(name, value);
                return value;
            }
            return value;
        }
    }
}
