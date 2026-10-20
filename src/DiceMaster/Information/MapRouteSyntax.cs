using System;
using System.Collections.Generic;
using DiceMaster.LiveLog;

namespace DiceMaster.Information
{
    // A resumable lexical front end. Steps count lexical/indexing units,
    // including comments and long strings, with bounded long-delimiter
    // lookahead. They are not CPU-time or exact character-count budgets.
    // Shared PreviewToken/constant readers are used without a Lua VM.
    internal sealed class MapRouteSyntax
    {
        internal sealed class Branch { internal int Header, HeaderEnd, Start, End; internal bool Else; }
        internal sealed class Control
        { internal string Kind; internal int Start, End; internal readonly List<Branch> Branches = new List<Branch>(); }
        internal readonly string Source;
        internal readonly List<PreviewToken> Tokens = new List<PreviewToken>();
        internal readonly Dictionary<int, int> Closes = new Dictionary<int, int>();
        internal readonly Dictionary<int, Control> Controls = new Dictionary<int, Control>();
        internal bool Ready, Limited, Malformed;
        internal int Characters, Work;
        private readonly int maxCharacters, maxTokens;
        private int position, first, mode, index, longEquals;
        private char quote;
        private bool escape;
        private readonly Stack<int> delimiters = new Stack<int>();
        private readonly Stack<Control> blocks = new Stack<Control>();
        internal MapRouteSyntax(string source, int chars, int tokens)
        { Source = source ?? ""; maxCharacters = Math.Max(0, chars); maxTokens = Math.Max(0, tokens); }
        private int LongOpen(int at)
        {
            if (at >= Source.Length || Source[at] != '[') return -1;
            int p = at + 1; while (p < Source.Length && p - at < 64 && Source[p] == '=') p++;
            if (p < Source.Length && p - at >= 64 && Source[p] == '=') Limited = true;
            return p < Source.Length && Source[p] == '[' ? p - at - 1 : -1;
        }
        private bool LongClose(int at)
        {
            if (Source[at] != ']' || at + longEquals + 1 >= Source.Length) return false;
            for (int j = 1; j <= longEquals; j++) if (Source[at + j] != '=') return false;
            return Source[at + longEquals + 1] == ']';
        }
        private static bool Ident(char c) { return c == '_' || char.IsLetterOrDigit(c); }
        private void Emit(bool str)
        {
            if (position - first > 16384) { Limited = Ready = true; return; }
            string text = Source.Substring(first, position - first);
            Tokens.Add(new PreviewToken { Text = text, Start = first, End = position, IsString = str,
                IsIdentifier = !str && text.Length != 0 && (text[0] == '_' || char.IsLetter(text[0])) });
            if (Tokens.Count >= maxTokens) { Limited = true; mode = 0; position = Source.Length; }
        }
        internal int Step(int budget)
        {
            int spent = 0;
            while (!Ready && spent < budget)
            {
                if (index >= 0 && mode == 6)
                {
                    if (index == Tokens.Count)
                    { Malformed |= blocks.Count != 0 || delimiters.Count != 0; Ready = true; break; }
                    Index(index++); spent++; Work++; continue;
                }
                if (position >= Source.Length || position >= maxCharacters)
                {
                    Limited |= position < Source.Length;
                    if (mode == 1 || mode == 2) Emit(false);
                    else if (mode == 3 || mode == 5 || mode == 7) Malformed = true;
                    Characters = position; mode = 6; index = 0; continue;
                }
                if (position - first > 16384 && (mode == 1 || mode == 2 || mode == 3 || mode == 5))
                { Limited = true; Characters = position; mode = 6; index = 0; continue; }
                char c = Source[position]; spent++; Work++;
                if (mode == 1)
                { if (Ident(c)) { position++; continue; } Emit(false); mode = 0; continue; }
                if (mode == 2)
                { if (Ident(c) || c == '.') { position++; continue; } Emit(false); mode = 0; continue; }
                if (mode == 3)
                {
                    position++;
                    if (escape) { escape = false; continue; }
                    if (c == '\\') { escape = true; continue; }
                    if (c == quote) { Emit(true); mode = 0; } continue;
                }
                if (mode == 4) { position++; if (c == '\n' || c == '\r') mode = 0; continue; }
                if (mode == 5 || mode == 7)
                {
                    if (LongClose(position))
                    { position += longEquals + 2; if (mode == 5) Emit(true); mode = 0; }
                    else position++;
                    continue;
                }
                if (char.IsWhiteSpace(c)) { position++; continue; }
                if (c == '-' && position + 1 < Source.Length && Source[position + 1] == '-')
                {
                    int eq = LongOpen(position + 2);
                    if (eq >= 0) { longEquals = eq; position += eq + 4; mode = 7; }
                    else { position += 2; mode = 4; }
                    continue;
                }
                first = position;
                if (c == '\'' || c == '"') { quote = c; position++; mode = 3; continue; }
                int equals = c == '[' ? LongOpen(position) : -1;
                if (equals >= 0) { longEquals = equals; position += equals + 2; mode = 5; continue; }
                if (c == '_' || char.IsLetter(c)) { mode = 1; position++; continue; }
                if (char.IsDigit(c)) { mode = 2; position++; continue; }
                position++;
                if (position < Source.Length && (((c == '=' || c == '~' || c == '<' || c == '>') && Source[position] == '=') || c == '.' && Source[position] == '.')) position++;
                Emit(false);
            }
            return spent;
        }
        private void Index(int at)
        {
            var token = Tokens[at]; if (token.IsString) return;
            string t = token.Text;
            if (t == "(" || t == "[" || t == "{") delimiters.Push(at);
            else if (t == ")" || t == "]" || t == "}")
            {
                if (delimiters.Count == 0) Malformed = true;
                else
                {
                    int open = delimiters.Pop(); string wanted = t == ")" ? "(" : t == "]" ? "[" : "{";
                    if (Tokens[open].Text != wanted) Malformed = true; else Closes[open] = at;
                }
            }
            if (t == "if" || t == "function" || t == "while" || t == "for" || t == "repeat")
            {
                var control = new Control { Kind = t == "while" || t == "for" ? "loop-await" : t, Start = at, End = Tokens.Count };
                if (t == "if") control.Branches.Add(new Branch { Header = at + 1, HeaderEnd = -1 });
                blocks.Push(control); Controls[at] = control;
            }
            else if (t == "do")
            {
                if (blocks.Count > 0 && blocks.Peek().Kind == "loop-await") blocks.Peek().Kind = "loop";
                else { var control = new Control { Kind = "do", Start = at, End = Tokens.Count }; blocks.Push(control); Controls[at] = control; }
            }
            else if (t == "then" && blocks.Count > 0 && blocks.Peek().Kind == "if")
            { var b = blocks.Peek().Branches; if (b[b.Count - 1].HeaderEnd < 0) { b[b.Count - 1].HeaderEnd = at; b[b.Count - 1].Start = at + 1; } }
            else if ((t == "elseif" || t == "else") && blocks.Count > 0 && blocks.Peek().Kind == "if")
            {
                var b = blocks.Peek().Branches; b[b.Count - 1].End = at;
                b.Add(new Branch { Header = at + 1, HeaderEnd = t == "else" ? at : -1, Start = at + 1, Else = t == "else" });
            }
            else if (t == "end" || t == "until")
            {
                if (blocks.Count == 0) { Malformed = true; return; }
                var control = blocks.Pop(); control.End = at;
                if (control.Kind == "if") control.Branches[control.Branches.Count - 1].End = at;
                if (t == "until" && control.Kind != "repeat" || t == "end" && control.Kind == "repeat") Malformed = true;
            }
        }
    }
}
