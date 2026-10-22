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
        internal readonly Dictionary<string, object> Fields = new Dictionary<string, object>(StringComparer.Ordinal);
        internal DisplayDefinition(string type, string name = "") { TypeName = type; Name = name; }
        internal object Read(string name) { return Fields[name]; }
    }
}
