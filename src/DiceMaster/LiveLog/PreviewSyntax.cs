using System;
using System.Collections.Generic;
using System.Text;

namespace DiceMaster.LiveLog
{
    public sealed class PreviewToken
    {
        public string Text;
        public int Start;
        public int End;
        public bool IsString;
        public bool IsIdentifier;
    }

    public sealed class PreviewOptionBlock
    {
        public string Key;
        public string Body;
        public string Story;
        public bool LimitReached;
    }

    public sealed class PreviewBranch
    {
        public string Label;
        public int Start;
        public int End;
        public bool IsElse;
        public int HeaderStart;
        public int HeaderEnd;
    }

    // A lexical boundary reader, deliberately not an evaluator or Lua interpreter.
    public static class PreviewSyntax
    {
        public static List<PreviewToken> Tokenize(string source)
        {
            source = source ?? "";
            var tokens = new List<PreviewToken>();
            for (int i = 0; i < source.Length;)
            {
                char c = source[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (c == '-' && i + 1 < source.Length && source[i + 1] == '-')
                {
                    int end;
                    if (TryLongEnd(source, i + 2, out end)) i = end;
                    else { i += 2; while (i < source.Length && source[i] != '\n' && source[i] != '\r') i++; }
                    continue;
                }
                int start = i;
                bool str = c == '\'' || c == '"';
                if (str)
                {
                    i++;
                    while (i < source.Length)
                    {
                        if (source[i] == '\\') { i = Math.Min(source.Length, i + 2); continue; }
                        if (source[i++] == c) break;
                    }
                }
                else if (c == '[' && TryLongEnd(source, i, out int longEnd)) { str = true; i = longEnd; }
                else if (IsIdentStart(c)) { i++; while (i < source.Length && IsIdent(source[i])) i++; }
                else if (char.IsDigit(c)) { i++; while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '.')) i++; }
                else if (i + 1 < source.Length && (source.Substring(i, 2) == "==" || source.Substring(i, 2) == "~=" || source.Substring(i, 2) == "<=" || source.Substring(i, 2) == ">=" || source.Substring(i, 2) == "..")) i += 2;
                else i++;
                tokens.Add(new PreviewToken { Text = source.Substring(start, i - start), Start = start, End = i, IsString = str, IsIdentifier = !str && IsIdentStart(c) });
            }
            return tokens;
        }

        private static bool IsIdentStart(char c) { return c == '_' || char.IsLetter(c); }
        private static bool IsIdent(char c) { return IsIdentStart(c) || char.IsDigit(c); }

        private static bool TryLongEnd(string source, int start, out int end)
        {
            end = start;
            if (start >= source.Length || source[start] != '[') return false;
            int i = start + 1;
            while (i < source.Length && source[i] == '=') i++;
            if (i >= source.Length || source[i] != '[') return false;
            string close = "]" + new string('=', i - start - 1) + "]";
            int found = source.IndexOf(close, i + 1, StringComparison.Ordinal);
            end = found < 0 ? source.Length : found + close.Length;
            return true;
        }

        public static bool TryString(string raw, out string value)
        {
            value = "";
            if (raw == null) return false;
            raw = raw.Trim();
            var tokens = Tokenize(raw);
            if (tokens.Count != 1 || !tokens[0].IsString) return false;
            raw = tokens[0].Text;
            if (raw[0] == '[')
            {
                int i = 1; while (i < raw.Length && raw[i] == '=') i++;
                string close = "]" + new string('=', i - 1) + "]";
                if (!raw.EndsWith(close, StringComparison.Ordinal) || raw.Length < (i + 1) * 2) return false;
                value = raw.Substring(i + 1, raw.Length - (i + 1) * 2);
                if (value.StartsWith("\r\n", StringComparison.Ordinal)) value = value.Substring(2);
                else if (value.StartsWith("\n", StringComparison.Ordinal) || value.StartsWith("\r", StringComparison.Ordinal)) value = value.Substring(1);
                return true;
            }
            if (raw.Length < 2 || raw[raw.Length - 1] != raw[0]) return false;
            var text = new StringBuilder();
            for (int i = 1; i < raw.Length - 1; i++)
            {
                char c = raw[i];
                if (c != '\\') { if (c == '\r' || c == '\n' || c == raw[0]) return false; text.Append(c); continue; }
                if (++i >= raw.Length - 1) return false;
                c = raw[i];
                switch (c)
                {
                    case '\\': case '\'': case '"': text.Append(c); break;
                    case 'n': text.Append('\n'); break;
                    case 'r': text.Append('\r'); break;
                    case 't': text.Append('\t'); break;
                    case 'a': text.Append('\a'); break;
                    case 'b': text.Append('\b'); break;
                    case 'f': text.Append('\f'); break;
                    case 'v': text.Append('\v'); break;
                    case '\n': text.Append('\n'); break;
                    case '\r': if (i + 1 < raw.Length - 1 && raw[i + 1] == '\n') i++; text.Append('\n'); break;
                    case 'z': while (i + 1 < raw.Length - 1 && char.IsWhiteSpace(raw[i + 1])) i++; break;
                    default:
                        if (c >= '0' && c <= '9')
                        {
                            int number = c - '0'; int count = 1;
                            while (count < 3 && i + 1 < raw.Length - 1 && char.IsDigit(raw[i + 1])) { number = number * 10 + raw[++i] - '0'; count++; }
                            if (number > 255) return false;
                            text.Append((char)number);
                        }
                        else return false;
                        break;
                }
            }
            value = text.ToString();
            return true;
        }

        public static int MatchClose(List<PreviewToken> tokens, int open, int limit = -1)
        {
            if (limit < 0) limit = tokens.Count;
            string start = tokens[open].Text;
            string close = start == "(" ? ")" : start == "[" ? "]" : "}";
            var stack = new Stack<string>(); stack.Push(close);
            for (int i = open + 1; i < limit; i++)
            {
                if (tokens[i].IsString) continue;
                string t = tokens[i].Text;
                if (t == "(" || t == "[" || t == "{") stack.Push(t == "(" ? ")" : t == "[" ? "]" : "}");
                else if (t == ")" || t == "]" || t == "}") { if (stack.Count == 0 || stack.Pop() != t) return -1; if (stack.Count == 0) return i; }
            }
            return -1;
        }

        public static List<string> Arguments(string source, List<PreviewToken> tokens, int open, int close)
        {
            var args = new List<string>();
            if (close < 0 || close <= open) return args;
            int first = tokens[open].End;
            for (int i = open + 1; i < close; i++)
            {
                if (!tokens[i].IsString && (tokens[i].Text == "(" || tokens[i].Text == "[" || tokens[i].Text == "{"))
                { int nested = MatchClose(tokens, i, close); if (nested >= 0) { i = nested; continue; } }
                if (!tokens[i].IsString && tokens[i].Text == ",") { args.Add(Normalize(source.Substring(first, tokens[i].Start - first))); first = tokens[i].End; }
            }
            string last = Normalize(source.Substring(first, tokens[close].Start - first));
            if (last.Length != 0 || args.Count != 0) args.Add(last);
            return args;
        }

        private static string Normalize(string raw)
        {
            var tokens = Tokenize(raw);
            var parts = new List<string>();
            foreach (var token in tokens) parts.Add(token.Text);
            return string.Join(" ", parts);
        }

        public static bool TryCall(List<PreviewToken> tokens, int index, out string owner, out string name, out int open, out int close)
        {
            owner = ""; name = ""; open = close = -1;
            if (index >= tokens.Count || !tokens[index].IsIdentifier || index > 0 && (tokens[index - 1].Text == "." || tokens[index - 1].Text == ":")) return false;
            if (index + 3 < tokens.Count && (tokens[index + 1].Text == "." || tokens[index + 1].Text == ":") && tokens[index + 2].IsIdentifier && tokens[index + 3].Text == "(")
            { owner = tokens[index].Text; name = tokens[index + 2].Text; open = index + 3; }
            else if (index + 1 < tokens.Count && tokens[index + 1].Text == "(") { name = tokens[index].Text; open = index + 1; }
            else return false;
            close = MatchClose(tokens, open);
            return true;
        }

        public static bool TrySugarCall(List<PreviewToken> tokens, int index, out string owner, out string name, out int argument, out int close)
        {
            owner = ""; name = ""; argument = close = -1;
            if (index >= tokens.Count || !tokens[index].IsIdentifier || index > 0 && (tokens[index - 1].Text == "." || tokens[index - 1].Text == ":")) return false;
            int next = index + 1;
            if (index + 2 < tokens.Count && (tokens[next].Text == "." || tokens[next].Text == ":") && tokens[next + 1].IsIdentifier)
            { owner = tokens[index].Text; name = tokens[next + 1].Text; next += 2; }
            else name = tokens[index].Text;
            if (next >= tokens.Count || !tokens[next].IsString && tokens[next].Text != "{") return false;
            argument = next;
            close = tokens[next].IsString ? next : MatchClose(tokens, next);
            return true;
        }

        public static string FirstStory(string source)
        {
            var tokens = Tokenize(source);
            for (int i = 0; i < tokens.Count; i++)
                if (TryCall(tokens, i, out string owner, out string name, out int open, out int close) && name == "GetStoryText" && close >= 0)
                { var args = Arguments(source, tokens, open, close); if (args.Count > 0 && TryString(args[0], out string key)) return key; }
            return "";
        }

        public static List<PreviewBranch> Branches(List<PreviewToken> tokens, int start, out int closing)
        {
            var branches = new List<PreviewBranch>(); closing = tokens.Count;
            int body = -1; int header = start; var blocks = new Stack<string>(); bool isElse = false;
            for (int i = start + 1; i < tokens.Count; i++)
            {
                if (tokens[i].IsString) continue;
                string t = tokens[i].Text;
                if (body < 0 && t == "then") { body = i + 1; continue; }
                if (body < 0) continue;
                if (t == "end" || t == "until")
                {
                    if (blocks.Count > 0) { blocks.Pop(); continue; }
                    branches.Add(new PreviewBranch { Label = Label(tokens, header, body), Start = body, End = i, IsElse = isElse, HeaderStart = header, HeaderEnd = body - 1 });
                    closing = i; return branches;
                }
                else if (blocks.Count == 0 && (t == "elseif" || t == "else"))
                {
                    branches.Add(new PreviewBranch { Label = Label(tokens, header, body), Start = body, End = i, IsElse = isElse, HeaderStart = header, HeaderEnd = body - 1 });
                    header = i; isElse = t == "else"; body = isElse ? i + 1 : -1;
                }
                else TrackBlockOpen(t, blocks);
            }
            if (body >= 0) branches.Add(new PreviewBranch { Label = Label(tokens, header, body), Start = body, End = tokens.Count, IsElse = isElse, HeaderStart = header, HeaderEnd = body - 1 });
            return branches;
        }

        private static string Label(List<PreviewToken> tokens, int header, int body)
        {
            if (tokens[header].Text == "else") return "else";
            for (int i = header + 1; i + 1 < body; i++) if (tokens[i].Text == "==")
            { string label = tokens[i + 1].Text; return TryString(label, out string value) ? value : label; }
            return "";
        }

        public static string Slice(string source, List<PreviewToken> tokens, int start, int end)
        {
            int first = start < tokens.Count ? tokens[start].Start : source.Length;
            int last = end < tokens.Count ? tokens[end].Start : source.Length;
            return source.Substring(first, Math.Max(0, last - first));
        }

        public static int BranchEnd(List<PreviewToken> tokens, int start, int hardLimit)
        {
            var blocks = new Stack<string>();
            for (int i = start; i < tokens.Count && tokens[i].Start < hardLimit; i++)
            {
                if (tokens[i].IsString) continue;
                string t = tokens[i].Text;
                if (blocks.Count == 0 && (t == "end" || t == "else" || t == "elseif" || t == "until")) return tokens[i].Start;
                if (t == "end" || t == "until") blocks.Pop();
                else TrackBlockOpen(t, blocks);
            }
            return hardLimit;
        }

        public static void TrackBlockOpen(string token, Stack<string> blocks)
        {
            if (token == "for" || token == "while") blocks.Push("loop-awaiting-do");
            else if (token == "if" || token == "function" || token == "repeat") blocks.Push(token);
            else if (token == "do")
            {
                if (blocks.Count > 0 && blocks.Peek() == "loop-awaiting-do") { blocks.Pop(); blocks.Push("loop"); }
                else blocks.Push("do");
            }
        }

        public static List<PreviewOptionBlock> OptionBlocks(string source)
        {
            return OptionBlocks(source, out bool incomplete);
        }

        public static List<PreviewOptionBlock> OptionBlocks(string source, out bool incomplete)
        {
            source = source ?? "";
            var limits = new PreviewTraceLimits();
            bool limited = source.Length > limits.MaxCharacters;
            if (limited) source = source.Substring(0, limits.MaxCharacters);
            var tokens = Tokenize(source);
            if (tokens.Count > limits.MaxTokens) { tokens.RemoveRange(limits.MaxTokens, tokens.Count - limits.MaxTokens); source = source.Substring(0, tokens[tokens.Count - 1].End); limited = true; }
            var result = new List<PreviewOptionBlock>();
            int work = 0;
            // Only a genuine short line comment can carry the game's --index identity.
            var markers = IndexMarkers(source);
            int markerToken = 0;
            for (int m = 0; m < markers.Count; m++)
            {
                int first = markers[m].End; int limit = m + 1 < markers.Count ? markers[m + 1].Start : source.Length;
                while (markerToken < tokens.Count && tokens[markerToken].Start < first) { markerToken++; if (++work > limits.MaxWork) break; }
                if (work > limits.MaxWork) { limited = true; break; }
                int last = BranchEnd(tokens, markerToken, limit);
                int visited = markerToken;
                while (visited < tokens.Count && tokens[visited].Start < last) { visited++; work++; }
                if (work > limits.MaxWork) { limited = true; break; }
                string body = source.Substring(first, Math.Max(0, last - first));
                result.Add(new PreviewOptionBlock { Key = markers[m].Text, Body = body, Story = FirstStory(body) });
            }
            for (int i = 0; i < tokens.Count; i++)
            {
                if (++work > limits.MaxWork) { limited = true; break; }
                if (!TryCall(tokens, i, out string owner, out string name, out int open, out int close) || owner != "" || name != "choose" || close < 0 || i < 2 || tokens[i - 1].Text != "=" || !tokens[i - 2].IsIdentifier) continue;
                var args = Arguments(source, tokens, open, close); if (args.Count == 0) continue;
                string table = args[0].Trim(); string variable = tokens[i - 2].Text;
                var options = new Dictionary<string, string>();
                for (int j = 0; j + 5 < i; j++)
                {
                    if (++work > limits.MaxWork) { limited = true; break; }
                    if (tokens[j].Text == table && tokens[j + 1].Text == "=" && tokens[j + 2].Text == "{") options.Clear();
                    if (tokens[j].Text == table && tokens[j + 1].Text == "[" && tokens[j + 3].Text == "]" && tokens[j + 4].Text == "=" && TryString(tokens[j + 5].Text, out string option)) options[tokens[j + 2].Text] = option;
                }
                if (work > limits.MaxWork) break;
                int branch = FindAssignedBranch(tokens, close + 1, variable);
                if (branch < 0) continue;
                var branches = Branches(tokens, branch, out int ending);
                work += ending - branch;
                if (work > limits.MaxWork) { limited = true; break; }
                foreach (var b in branches)
                    if (options.TryGetValue(b.Label, out string option))
                    { string body = Slice(source, tokens, b.Start, b.End); if (!result.Exists(x => x.Key == option)) result.Add(new PreviewOptionBlock { Key = option, Body = body, Story = FirstStory(body) }); }
            }
            if (limited) foreach (var block in result) block.LimitReached = true;
            incomplete = limited;
            return result;
        }

        public static int FindAssignedBranch(List<PreviewToken> tokens, int start, string variable)
        {
            for (int i = start; i < tokens.Count; i++)
            {
                if (tokens[i].Text != "if") continue;
                int j = i + 1; while (j < tokens.Count && tokens[j].Text == "(") j++;
                if (j + 1 < tokens.Count && tokens[j].Text == variable && tokens[j + 1].Text == "==") return i;
            }
            return -1;
        }

        private static List<PreviewToken> IndexMarkers(string source)
        {
            var result = new List<PreviewToken>();
            for (int i = 0; i < source.Length;)
            {
                if (source[i] == '\'' || source[i] == '"')
                { char quote = source[i++]; while (i < source.Length) { if (source[i] == '\\') { i = Math.Min(source.Length, i + 2); continue; } if (source[i++] == quote) break; } continue; }
                if (source[i] == '[' && TryLongEnd(source, i, out int end)) { i = end; continue; }
                if (source[i] != '-' || i + 1 >= source.Length || source[i + 1] != '-') { i++; continue; }
                int first = i; if (TryLongEnd(source, i + 2, out end)) { i = end; continue; }
                i += 2; while (i < source.Length && source[i] != '\r' && source[i] != '\n') i++;
                string comment = source.Substring(first, i - first);
                int line = first - 1; while (line >= 0 && source[line] != '\n' && source[line] != '\r' && char.IsWhiteSpace(source[line])) line--;
                bool lineStart = line < 0 || source[line] == '\n' || source[line] == '\r';
                if (!lineStart || !comment.StartsWith("--index:O_", StringComparison.Ordinal)) continue;
                string remaining = comment.Substring(8); int keyEnd = 0;
                while (keyEnd < remaining.Length && IsIdent(remaining[keyEnd])) keyEnd++;
                string key = remaining.Substring(0, keyEnd);
                bool valid = key.Length > 2;
                foreach (char c in key) if (!IsIdent(c)) valid = false;
                if (valid) result.Add(new PreviewToken { Text = key, Start = first, End = i });
            }
            return result;
        }
    }
}
