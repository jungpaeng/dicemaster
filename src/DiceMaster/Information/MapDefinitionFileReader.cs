using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using DiceMaster.LiveLog;

namespace DiceMaster.Information
{
    // The embedded file contains only reviewed offsets/types and fingerprints.
    // Game content is read from the installation, never bundled with the mod.
    // This class does no Unity work and can run entirely on a worker thread.
    internal sealed class MapDefinitionFileReader : IDisposable
    {
        private sealed class FileIndex
        { internal string Name; internal long Length; internal byte[] Hash; internal int[] Externals; internal FileStream Stream; }
        private sealed class Entry
        { internal int File; internal long Id, Offset; internal int Length; internal string Type; }
        private readonly List<FileIndex> files = new List<FileIndex>();
        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();
        private readonly Dictionary<string, object> live;
        private readonly CancellationToken cancellation;
        private int depth;
        private static readonly UTF8Encoding utf8 = new UTF8Encoding(false, true);
        internal long BytesHashed { get; private set; }
        private MapDefinitionFileReader(Dictionary<string, object> bindings, CancellationToken token) { live = bindings; cancellation = token; }
        private static string Key(int file, long id) { return file + ":" + id; }
        internal static object[][] Read(string directory, Stream profile, Dictionary<string, object> bindings, CancellationToken token, out long bytes)
        {
            using (var reader = new MapDefinitionFileReader(bindings, token))
            {
                object[][] result = reader.Open(directory, profile); bytes = reader.BytesHashed; return result;
            }
        }
        private object[][] Open(string directory, Stream profile)
        {
            if (profile == null) throw new InvalidDataException("Definition index missing.");
            using (var input = new BinaryReader(profile, utf8, true))
            {
                if (Encoding.ASCII.GetString(input.ReadBytes(4)) != "DMD1") throw new InvalidDataException("Definition index format.");
                int count = Count(input, 8);
                var approved = new HashSet<string>(StringComparer.Ordinal) { "globalgamemanagers.assets", "sharedassets2.assets", "level2", "Managed/Mortal.Core.dll", "Managed/Mortal.Story.dll", "Managed/Mortal.Free.dll" };
                for (int i = 0; i < count; i++)
                {
                    string name = input.ReadString();
                    if (!approved.Remove(name)) throw new InvalidDataException("Definition file path.");
                    var file = new FileIndex { Name = name, Length = input.ReadInt64(), Hash = input.ReadBytes(32) };
                    if (file.Length < 1 || file.Length > 268435456 || file.Hash.Length != 32) throw new InvalidDataException("Definition file size.");
                    file.Externals = new int[Count(input, 16)];
                    for (int at = 0; at < file.Externals.Length; at++) { int index = input.ReadInt32(); if (index < -1 || index >= count) throw new InvalidDataException("External file index."); file.Externals[at] = index; }
                    files.Add(file);
                }
                if (approved.Count != 0) throw new InvalidDataException("Incomplete definition fingerprint set.");
                var types = new string[Count(input, 32)];
                for (int i = 0; i < types.Length; i++) { types[i] = input.ReadString(); if (!AllowedType(types[i])) throw new InvalidDataException("Definition type."); }
                int objects = Count(input, 20000);
                for (int i = 0; i < objects; i++)
                {
                    var entry = new Entry { File = input.ReadByte(), Id = input.ReadInt64(), Offset = input.ReadInt64(), Length = input.ReadInt32() };
                    int type = input.ReadByte();
                    if (entry.File >= files.Count || type >= types.Length || entry.Id <= 0 || entry.Offset < 0 || entry.Length < 32 || entry.Length > 1048576 || entry.Offset > files[entry.File].Length - entry.Length) throw new InvalidDataException("Definition object bounds.");
                    entry.Type = types[type]; entries.Add(Key(entry.File, entry.Id), entry);
                }
                int roots = Count(input, 18); if (roots < 3) throw new InvalidDataException("Definition roots.");
                var rootKeys = new string[roots];
                for (int i = 0; i < roots; i++) rootKeys[i] = Key(input.ReadByte(), input.ReadInt64());
                if (input.BaseStream.Position != input.BaseStream.Length) throw new InvalidDataException("Definition index suffix.");
                foreach (var file in files)
                {
                    cancellation.ThrowIfCancellationRequested();
                    file.Stream = new FileStream(Path.Combine(directory, file.Name), FileMode.Open, FileAccess.Read, FileShare.Read);
                    if (file.Stream.Length != file.Length) throw new InvalidDataException("Installed definition version differs: " + file.Name);
                    using (var hash = SHA256.Create())
                    {
                        var buffer = new byte[65536]; int read;
                        while ((read = file.Stream.Read(buffer, 0, buffer.Length)) > 0)
                        { cancellation.ThrowIfCancellationRequested(); hash.TransformBlock(buffer, 0, read, buffer, 0); BytesHashed += read; }
                        hash.TransformFinalBlock(System.Array.Empty<byte>(), 0, 0);
                        if (!hash.Hash.SequenceEqual(file.Hash)) throw new InvalidDataException("Installed definition fingerprint differs: " + file.Name);
                    }
                }
                var result = new object[4][];
                result[0] = new[] { Node(rootKeys[0]) }; result[1] = new object[roots - 2];
                for (int i = 1; i < roots - 1; i++) result[1][i - 1] = Node(rootKeys[i]);
                result[3] = new[] { Node(rootKeys[roots - 1]) };
                Require(result[0][0], "Mortal.Story.ConditionResultConfig"); Require(result[3][0], "Mortal.Core.FlagCollectionData");
                foreach (object value in result[1]) Require(value, "Mortal.Story.SwitchResultConfig");
                return result;
            }
        }
        private static void Require(object value, string type)
        { if (!(value is DisplayDefinition node) || node.TypeName != type) throw new InvalidDataException("Invalid definition root type."); }
        private static bool AllowedType(string type)
        {
            switch (type)
            {
                case "Mortal.Story.ConditionResultConfig": case "Mortal.Story.SwitchResultConfig": case "Mortal.Core.FlagCollectionData":
                case "Mortal.Core.ConditionResultData": case "Mortal.Story.SwitchResultData": case "Mortal.Core.StatGroupVariable":
                case "Mortal.Core.FlagData": case "Mortal.Core.GameStat": case "Mortal.Core.RelationshipStat": case "Mortal.Core.GameTimeData":
                case "Mortal.Core.Book": case "Mortal.Core.Misc": case "Mortal.Core.SpecialItem": case "Mortal.Core.PlayerTalentData":
                case "Mortal.Core.UpgradeItemData": case "Mortal.Core.BookDataCollection": case "Mortal.Core.LibraryItemData": return true;
                default: return false;
            }
        }
        private static int Count(BinaryReader r, int max)
        { int n = r.ReadInt32(); if (n < 0 || n > max) throw new InvalidDataException("Definition collection bounds."); return n; }
        private static string Text(BinaryReader r)
        {
            int n = Count(r, 65536); byte[] bytes = r.ReadBytes(n); if (bytes.Length != n) throw new EndOfStreamException();
            string text = utf8.GetString(bytes); r.BaseStream.Position = (r.BaseStream.Position + 3) & ~3L; return text;
        }
        private static bool Boolean(BinaryReader r)
        { byte b = r.ReadByte(); if (b > 1) throw new InvalidDataException("Definition bool."); r.BaseStream.Position = (r.BaseStream.Position + 3) & ~3L; return b == 1; }
        private object Ref(BinaryReader r, int file)
        {
            int external = r.ReadInt32(); long id = r.ReadInt64(); if (id == 0) return null;
            if (external < 0 || external > files[file].Externals.Length) throw new InvalidDataException("Definition PPtr.");
            int target = external == 0 ? file : files[file].Externals[external - 1];
            return target < 0 ? null : Node(Key(target, id));
        }
        private object Node(string key)
        {
            cancellation.ThrowIfCancellationRequested();
            if (values.TryGetValue(key, out object cached)) return cached;
            if (!entries.TryGetValue(key, out Entry entry)) return null;
            if (++depth > 64) throw new InvalidDataException("Definition graph depth.");
            try
            {
                var file = files[entry.File]; file.Stream.Position = entry.Offset; byte[] bytes = new byte[entry.Length];
                int at = 0, n; while (at < bytes.Length && (n = file.Stream.Read(bytes, at, bytes.Length - at)) > 0) at += n;
                if (at != bytes.Length) throw new EndOfStreamException();
                using (var r = new BinaryReader(new MemoryStream(bytes, false), utf8))
                {
                    if (r.ReadInt32() != 0 || r.ReadInt64() != 0) throw new InvalidDataException("Definition is not a standalone asset.");
                    r.BaseStream.Position = 28; string name = Text(r);
                    bool metadata = entry.Type.EndsWith("Config", StringComparison.Ordinal) || entry.Type.EndsWith("ConditionResultData", StringComparison.Ordinal) || entry.Type.EndsWith("SwitchResultData", StringComparison.Ordinal) || entry.Type.EndsWith("StatGroupVariable", StringComparison.Ordinal) || entry.Type.EndsWith("FlagCollectionData", StringComparison.Ordinal);
                    if (!metadata)
                    {
                        // Only identity is read. Serialized State/count/time/item
                        // values are NEVER used as current game inputs.
                        live.TryGetValue(entry.Type + "/" + name, out object native); values.Add(key, native); return native;
                    }
                    var node = new DisplayDefinition(entry.Type, name); values.Add(key, node);
                    if (entry.Type.EndsWith("Config", StringComparison.Ordinal) || entry.Type.EndsWith("FlagCollectionData", StringComparison.Ordinal))
                        node.Fields.Add("_list", Array(r, () => Ref(r, entry.File), 8192));
                    else if (entry.Type == "Mortal.Core.StatGroupVariable")
                    { node.Fields.Add("_groupType", r.ReadInt32()); node.Fields.Add("_value", Array(r, () => Reference(r, entry.File), 256)); }
                    else
                    {
                        Text(r); r.ReadInt32(); // dev note and serialized execution count: discarded
                        if (entry.Type == "Mortal.Story.SwitchResultData") node.Fields.Add("_useCount", Boolean(r));
                        node.Fields.Add("_items", Array(r, () => Condition(r, entry.File), 256));
                        if (entry.Type == "Mortal.Story.SwitchResultData") node.Fields.Add("_countItems", Array(r, () => {
                            var c = new DisplayDefinition("Mortal.Core.ValueCompareItem"); c.Fields.Add("_compareOp", r.ReadInt32()); c.Fields.Add("_value", r.ReadInt32()); return c;
                        }, 256));
                    }
                    if (r.BaseStream.Position != r.BaseStream.Length) throw new InvalidDataException("Definition payload/schema differs.");
                    return node;
                }
            }
            finally { depth--; }
        }
        private static List<object> Array(BinaryReader r, Func<object> item, int max)
        { int count = Count(r, max); var result = new List<object>(count); for (int i = 0; i < count; i++) result.Add(item()); return result; }
        private object Condition(BinaryReader r, int file)
        {
            Text(r); var result = new DisplayDefinition("Mortal.Core.ConditionResultItem"); result.Fields.Add("_logicOp", r.ReadInt32());
            result.Fields.Add("_items", Array(r, () => {
                var comparison = new DisplayDefinition("Mortal.Core.StatCompareItem"); comparison.Fields.Add("_compareOp", r.ReadInt32());
                comparison.Fields.Add("_value1", Ref(r, file)); comparison.Fields.Add("_value2", Ref(r, file)); return comparison;
            }, 256)); return result;
        }
        private object Reference(BinaryReader r, int file)
        {
            var value = new DisplayDefinition("Mortal.Core.StatValueReference"); value.Fields.Add("_checkType", r.ReadInt32()); value.Fields.Add("_constant", r.ReadInt32());
            foreach (string field in new[] { "_stat", "_relationship", "_flag" }) value.Fields.Add(field, Ref(r, file));
            r.ReadInt32(); r.ReadInt32(); // random bounds are not sampled
            foreach (string field in new[] { "_book", "_misc", "_specialItem", "_talentData", "_statGroup" }) value.Fields.Add(field, Ref(r, file));
            value.Fields.Add("_negative", Boolean(r));
            foreach (string field in new[] { "_gameTime", "_upgradeItemData", "_bookCollection", "_libraryAchieve", "_condition" }) value.Fields.Add(field, Ref(r, file));
            return value;
        }
        public void Dispose() { foreach (var file in files) file.Stream?.Dispose(); }
    }
}
