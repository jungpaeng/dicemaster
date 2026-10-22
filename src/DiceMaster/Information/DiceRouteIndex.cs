using System;
using System.Collections.Generic;
using System.Globalization;
using DiceMaster.LiveLog;

namespace DiceMaster.Information
{
    // A bounded definition index, not an interpreter. It recognizes the
    // audited ExecuteRoll(table, literal, key) -> ResultSelection -> if form.
    // Exact native raw rows and key must identify one call site. Reused keys,
    // dynamic rows, malformed code and ambiguous sites never grant a marker.
    internal sealed class DiceRouteIndex
    {
        internal sealed class Site
        {
            internal int Offset;
            internal string Key, Variable;
            internal readonly List<string> Raw = new List<string>();
            internal readonly Dictionary<int, MapRouteSyntax.Branch> Arms = new Dictionary<int, MapRouteSyntax.Branch>();
        }
        internal readonly string Script;
        internal readonly MapRouteSyntax Syntax;
        internal readonly List<Site> Sites = new List<Site>();
        internal bool Ready, Unsupported;
        internal int Work;
        private int cursor;
        private static readonly HashSet<string> apis = new HashSet<string>(new[] { "checkpointmanager", "characters", "luamanager", "setcharacter", "say", "dicemenudialog" }, StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<int, string>> tables = new Dictionary<string, Dictionary<int, string>>(StringComparer.Ordinal);
        internal DiceRouteIndex(string script, string source)
        { Script = script; Syntax = new MapRouteSyntax(source, 524288, 131072); }
        private bool Match(int at, params string[] words)
        {
            if (at < 0 || at + words.Length > Syntax.Tokens.Count) return false;
            for (int n = 0; n < words.Length; n++) if (Syntax.Tokens[at + n].IsString || Syntax.Tokens[at + n].Text != words[n]) return false;
            return true;
        }
        internal void Advance(int budget, Func<bool> yield)
        {
            while (!Ready && budget > 0 && (yield == null || !yield()))
            {
                if (!Syntax.Ready) { int used = Syntax.Step(Math.Min(256, budget)); Work += used; budget -= used; continue; }
                if (Syntax.Limited || Syntax.Malformed) { Unsupported = Ready = true; Sites.Clear(); return; }
                if (cursor >= Syntax.Tokens.Count) { Ready = true; return; }
                if (Work >= 1048576) { Unsupported = Ready = true; Sites.Clear(); return; }
                Inspect(cursor++); budget--; Work++;
            }
        }
        private void Inspect(int at)
        {
            var t = Syntax.Tokens;
            string name = t[at].Text;
            if (t[at].IsString) return;
            // An escaped/overwritten API contract cannot be trusted, even if
            // an earlier-looking literal site has the same visible strings.
            if (apis.Contains(name) && at + 1 < t.Count && (t[at + 1].Text == "=" || at + 3 < t.Count && t[at + 1].Text == "." && t[at + 2].IsIdentifier && t[at + 3].Text == "=")) Unsupported = true;
            if (name == "function" && at + 1 < t.Count && apis.Contains(t[at + 1].Text)) Unsupported = true;
            if (at + 4 < t.Count && t[at].IsIdentifier && t[at + 1].Text == "=" && Match(at + 2, "{", "}"))
            { if (tables.Count < 128 || tables.ContainsKey(t[at].Text)) tables[t[at].Text] = new Dictionary<int, string>(); }
            if (at + 5 < t.Count && t[at].IsIdentifier && t[at + 1].Text == "[" && Match(at + 3, "]", "=") && tables.TryGetValue(t[at].Text, out var table))
            {
                int index;
                if (!int.TryParse(t[at + 2].Text, out index) || index < 1 || index > 16) { table.Clear(); return; }
                if (t[at + 5].IsString && PreviewSyntax.TryString(t[at + 5].Text, out string raw)) table[index] = raw;
                else table.Remove(index);
            }
            if (name != "dicemenudialog" || !Match(at + 1, ".", "ExecuteRoll", "(")) return;
            if (Sites.Count >= 32) { Unsupported = true; return; }
            int close;
            if (!Syntax.Closes.TryGetValue(at + 3, out close) || close != at + 9 || !Match(at + 5, ",") || !Match(at + 7, ",") ||
                !t[at + 8].IsString || !PreviewSyntax.TryString(t[at + 8].Text, out string key) || !tables.TryGetValue(t[at + 4].Text, out table)) return;
            int next = close + 1;
            if (Match(next, ")")) next++; // runwait wrapper
            if (Match(next, ";")) next++;
            if (!Match(next, "local") || next + 8 >= t.Count || !t[next + 1].IsIdentifier || !Match(next + 2, "=", "dicemenudialog", ".", "ResultSelection")) return;
            string variable = t[next + 1].Text; next += 6;
            if (Match(next, ";")) next++;
            if (!Syntax.Controls.TryGetValue(next, out var control) || control.Kind != "if" || control.Branches.Count > 16) return;
            var site = new Site { Offset = t[at].Start, Key = key, Variable = variable };
            for (int n = 1; n <= table.Count; n++) { if (!table.TryGetValue(n, out string raw) || raw.Length > 2048) return; site.Raw.Add(raw); }
            if (site.Raw.Count == 0) return;
            foreach (var branch in control.Branches)
            {
                int n;
                if (branch.Else || branch.HeaderEnd - branch.Header != 3 || !Match(branch.Header, variable, "==") ||
                    !int.TryParse(t[branch.Header + 2].Text, NumberStyles.None, CultureInfo.InvariantCulture, out n) || n < 1 || n > site.Raw.Count || site.Arms.ContainsKey(n)) return;
                site.Arms.Add(n, branch);
            }
            Sites.Add(site);
        }
        internal Site Find(string key, IList<string> raw)
        {
            if (!Ready || Unsupported || raw == null) return null;
            Site found = null;
            foreach (var site in Sites)
            {
                if (site.Key != key || site.Raw.Count != raw.Count) continue;
                bool same = true; for (int n = 0; n < raw.Count; n++) same &= site.Raw[n] == raw[n];
                if (!same) continue;
                if (found != null) return null;
                found = site;
            }
            return found;
        }
        internal MapRouteDefinitions Arm(Site site, int index, Func<string, ScriptReadResult> reader, Func<string, IList<MapRouteEvent>> pools)
        {
            if (site == null || !site.Arms.TryGetValue(index, out var arm)) return null;
            string label = site.Raw[index - 1]; int bar = label.IndexOf('|'); if (bar >= 0) label = label.Substring(0, bar);
            return MapRouteDefinitions.DiceArm(Script, Syntax, arm, site.Offset, index, label, site.Variable, reader, pools);
        }
    }
}
