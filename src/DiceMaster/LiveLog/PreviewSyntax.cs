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
        public bool Conditional;
    }

    // Root discovery is independent of the cumulative per-option trace
    // budget. The observed 2,667-script corpus peaks at 502,124 UTF-16 chars
    // and 86,919 tokens; these bounds cover it while remaining finite.
    public sealed class PreviewDiscoveryLimits
    {
        public int MaxCharacters = 524288;
        public int MaxTokens = 131072;
        public int MaxOptions = 1024;
        public int MaxNesting = 128;
        public int MaxMaterializedCharacters = 1048576;
        public int MaxWork = 2097152;
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
            return Tokenize(source, int.MaxValue, out bool ignored);
        }

        private static List<PreviewToken> Tokenize(string source, int maxTokens, out bool limited)
        {
            source = source ?? "";
            limited = false;
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
                if (tokens.Count >= maxTokens) { limited = true; break; }
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
                else if (i + 1 < source.Length && (((c == '=' || c == '~' || c == '<' || c == '>') && source[i + 1] == '=') || c == '.' && source[i + 1] == '.')) i += 2;
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
            return OptionBlocks(source, new PreviewDiscoveryLimits(), out incomplete);
        }

        public static List<PreviewOptionBlock> OptionBlocks(string source, PreviewDiscoveryLimits limits, out bool incomplete)
        {
            source = source ?? "";
            limits = limits ?? new PreviewDiscoveryLimits();
            var budget = new DiscoveryBudget(limits);
            if (source.Length > Math.Max(0, limits.MaxCharacters))
            { source = source.Substring(0, Math.Max(0, limits.MaxCharacters)); budget.Limited = true; }
            var tokens = Tokenize(source, Math.Max(0, limits.MaxTokens), out bool tokenLimit);
            if (tokenLimit)
            { source = source.Substring(0, tokens.Count == 0 ? 0 : tokens[tokens.Count - 1].End); budget.Limited = true; }
            var result = new List<PreviewOptionBlock>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var delimiters = DiscoveryDelimiters(tokens, budget);
            if (budget.WorkExhausted) return EndDiscovery(result, budget, out incomplete);
            var branches = DiscoveryBranches(tokens, budget, out Dictionary<int, int> controlEnds, out int[] depths, out DiscoveryScopes scopes);
            if (budget.WorkExhausted) return EndDiscovery(result, budget, out incomplete);
            var boundaries = DiscoveryBoundaries(tokens, controlEnds, budget);
            if (budget.WorkExhausted) return EndDiscovery(result, budget, out incomplete);
            var stories = DiscoveryStories(tokens, delimiters, budget);
            if (budget.WorkExhausted) return EndDiscovery(result, budget, out incomplete);
            // Marker starts advance monotonically. A marker inside a nested
            // block must not cut the enclosing option before its matching end.
            // Both marker and choose bodies use precomputed lexical boundaries.
            var markers = IndexMarkers(source);
            var markerTokens = new int[markers.Count];
            var markerDepths = new int[markers.Count];
            var markerEnds = new int[markers.Count];
            var markerScopes = new Dictionary<string, int>(StringComparer.Ordinal);
            int markerToken = 0;
            for (int m = 0; m < markers.Count && budget.Spend(); m++)
            {
                while (markerToken < tokens.Count && tokens[markerToken].Start < markers[m].End && budget.Spend()) markerToken++;
                markerTokens[m] = markerToken;
                markerDepths[m] = markerToken < depths.Length ? depths[markerToken] : 0;
            }
            if (budget.WorkExhausted) return EndDiscovery(result, budget, out incomplete);
            var nextMarkers = new Stack<int>();
            for (int m = markers.Count - 1; m >= 0 && budget.Spend(); m--)
            {
                while (nextMarkers.Count > 0 && markerDepths[nextMarkers.Peek()] > markerDepths[m] && budget.Spend()) nextMarkers.Pop();
                markerEnds[m] = nextMarkers.Count == 0 ? source.Length : markers[nextMarkers.Peek()].Start;
                nextMarkers.Push(m);
            }
            if (budget.WorkExhausted) return EndDiscovery(result, budget, out incomplete);
            for (int m = 0; m < markers.Count && budget.Spend(); m++)
            {
                int token = markerTokens[m];
                int boundary = token < boundaries.Length ? boundaries[token] : tokens.Count;
                int last = boundary < tokens.Count ? tokens[boundary].Start : source.Length;
                last = Math.Min(last, markerEnds[m]);
                if (!keys.Contains(markers[m].Text)) markerScopes[markers[m].Text] = token < scopes.Lexical.Length ? scopes.Lexical[token] : 0;
                AddDiscoveryBlock(source, markers[m].Text, markers[m].End, last, stories, keys, result, budget, true);
            }
            if (budget.WorkExhausted) return EndDiscovery(result, budget, out incomplete);
            // Capture only assignment histories and table generations. A choose
            // references its generation and position, so later edits/resetting
            // the same table cannot change that choose's option identities.
            var bindings = new DiscoveryBindings(scopes);
            var tables = new Dictionary<DiscoveryBinding, DiscoveryTable>();
            var conditionBindings = new Dictionary<int, DiscoveryBinding>();
            var assignmentBindings = new Dictionary<int, DiscoveryBinding>();
            var choices = new Dictionary<int, DiscoveryChoice>();
            for (int i = 0; i < tokens.Count && budget.Spend(); i++)
            {
                if (!tokens[i].IsIdentifier || i > 0 && (tokens[i - 1].Text == "." || tokens[i - 1].Text == ":")) continue;
                if (tokens[i].Text == "local")
                {
                    int declaration = i + 1;
                    if (declaration < tokens.Count && tokens[declaration].Text == "function") declaration++;
                    while (declaration < tokens.Count && tokens[declaration].IsIdentifier && budget.Spend())
                    {
                        bindings.Declare(tokens[declaration].Text, i);
                        if (declaration + 1 >= tokens.Count || tokens[declaration + 1].Text != ",") break;
                        declaration += 2;
                    }
                }
                if (tokens[i].Text == "for")
                {
                    int declaration = i + 1;
                    while (declaration < tokens.Count && tokens[declaration].IsIdentifier && budget.Spend())
                    {
                        // Numeric and generic for variables are implicit locals
                        // in the loop scope, even without the local keyword.
                        bindings.Declare(tokens[declaration].Text, i + 1);
                        if (declaration + 1 >= tokens.Count || tokens[declaration + 1].Text != ",") break;
                        declaration += 2;
                    }
                }
                if (tokens[i].Text == "if")
                {
                    int header = i + 1;
                    while (header < tokens.Count && tokens[header].Text == "(" && budget.Spend()) header++;
                    if (header + 1 < tokens.Count && tokens[header].IsIdentifier && tokens[header + 1].Text == "==")
                        conditionBindings[i] = bindings.Resolve(tokens[header].Text, i, budget);
                }
                var binding = bindings.Resolve(tokens[i].Text, i, budget);
                if (binding == null) continue;
                if (i + 1 < tokens.Count && tokens[i + 1].Text == "=")
                {
                    assignmentBindings[i] = binding;
                    // Unsupported replacement expressions invalidate a prior
                    // table; they must not leave old choice identities behind.
                    if (i + 2 < tokens.Count && tokens[i + 2].Text == "{") tables[binding] = new DiscoveryTable();
                    else tables.Remove(binding);
                }
                if (i + 5 < tokens.Count && tokens[i + 1].Text == "[" && tokens[i + 3].Text == "]" && tokens[i + 4].Text == "=" && tokens[i + 5].IsString &&
                    TryString(tokens[i + 5].Text, out string option))
                {
                    if (!tables.TryGetValue(binding, out DiscoveryTable table)) tables[binding] = table = new DiscoveryTable();
                    table.Add(tokens[i + 2].Text, i, option);
                }
                if (tokens[i].Text != "choose" || i < 2 || tokens[i - 1].Text != "=" || !tokens[i - 2].IsIdentifier ||
                    i > 2 && (tokens[i - 3].Text == "." || tokens[i - 3].Text == ":") ||
                    i + 2 >= tokens.Count || tokens[i + 1].Text != "(" || !tokens[i + 2].IsIdentifier ||
                    !delimiters.TryGetValue(i + 1, out int close) || i + 3 >= tokens.Count || tokens[i + 3].Text != "," && i + 3 != close) continue;
                var tableBinding = bindings.Resolve(tokens[i + 2].Text, i, budget);
                var resultBinding = bindings.Resolve(tokens[i - 2].Text, i, budget);
                if (tableBinding != null && resultBinding != null && tables.TryGetValue(tableBinding, out DiscoveryTable selected))
                    choices[close] = new DiscoveryChoice { Variable = resultBinding, VariableName = tokens[i - 2].Text, Position = i, Scope = scopes.Lexical[i], Table = selected };
            }
            if (budget.WorkExhausted) return EndDiscovery(result, budget, out incomplete);
            var nextBranch = new Dictionary<DiscoveryBinding, Dictionary<int, int>>();
            var resolvedChoices = new List<DiscoveryChoice>();
            for (int i = tokens.Count - 1; i >= 0 && budget.Spend(); i--)
            {
                if (assignmentBindings.TryGetValue(i, out DiscoveryBinding assigned)) nextBranch.Remove(assigned);
                if (conditionBindings.TryGetValue(i, out DiscoveryBinding condition) && condition != null)
                {
                    if (!nextBranch.TryGetValue(condition, out Dictionary<int, int> byScope)) nextBranch[condition] = byScope = new Dictionary<int, int>();
                    byScope[scopes.Lexical[i]] = i;
                }
                // A nested result test may have an additional gate/loop around
                // it. Extracting only its body would erase that condition.
                if (!choices.TryGetValue(i, out DiscoveryChoice choice) || !nextBranch.TryGetValue(choice.Variable, out Dictionary<int, int> candidates) ||
                    !candidates.TryGetValue(choice.Scope, out int branch) || !branches.TryGetValue(branch, out List<PreviewBranch> alternatives)) continue;
                choice.Branches = alternatives; choice.Close = i; choice.Branch = branch; resolvedChoices.Add(choice);
            }
            if (budget.WorkExhausted) return EndDiscovery(result, budget, out incomplete);
            var provenChoiceScopes = new HashSet<int>();
            for (int c = resolvedChoices.Count - 1; c >= 0 && budget.Spend(); c--)
            {
                var choice = resolvedChoices[c];
                bool pureChoice = true;
                // Common statements between the visible choice and its result
                // branch are not part of the extracted option body. They may
                // mutate state before an effect whose amount uses a snapshot.
                for (int between = choice.Close + 1; between < choice.Branch && budget.Spend(); between++)
                    if (tokens[between].Text != ";") pureChoice = false;
                if (budget.WorkExhausted) pureChoice = false;
                foreach (var alternative in choice.Branches)
                    if (!alternative.IsElse && !PureChoiceHeader(tokens, delimiters, alternative, choice.VariableName, budget)) pureChoice = false;
                foreach (var alternative in choice.Branches)
                {
                    if (!budget.Spend()) break;
                    if (!choice.Table.TryGet(alternative.Label, choice.Position, budget, out string option)) continue;
                    int first = alternative.Start < tokens.Count ? tokens[alternative.Start].Start : source.Length;
                    int last = alternative.End < tokens.Count ? tokens[alternative.End].Start : source.Length;
                    AddDiscoveryBlock(source, option, first, last, stories, keys, result, budget, !pureChoice);
                    if (pureChoice && alternative.Start < scopes.Lexical.Length) provenChoiceScopes.Add(scopes.Lexical[alternative.Start]);
                }
            }
            foreach (var block in result)
                if (markerScopes.TryGetValue(block.Key, out int markerScope)) block.Conditional = DiscoveryMarkerConditional(markerScope, scopes, provenChoiceScopes, budget);
            return EndDiscovery(result, budget, out incomplete);
        }

        static bool PureChoiceHeader(List<PreviewToken> tokens, Dictionary<int, int> delimiters, PreviewBranch branch, string variable, DiscoveryBudget budget)
        {
            int first = branch.HeaderStart + 1, last = branch.HeaderEnd;
            while (first < last && tokens[first].Text == "(" && delimiters.TryGetValue(first, out int close) && close == last - 1 && budget.Spend()) { first++; last--; }
            return !budget.WorkExhausted && last - first == 3 && tokens[first].IsIdentifier && tokens[first].Text == variable && tokens[first + 1].Text == "==" &&
                (tokens[first + 2].IsString || !tokens[first + 2].IsIdentifier);
        }

        static bool DiscoveryMarkerConditional(int scope, DiscoveryScopes scopes, HashSet<int> provenChoices, DiscoveryBudget budget)
        {
            bool uncertain = false;
            int function = scopes.FunctionOfScope.TryGetValue(scope, out int owner) ? owner : 0;
            while (scope > 0 && budget.Spend())
            {
                if (provenChoices.Contains(scope)) return uncertain;
                if (!scopes.FunctionOfScope.TryGetValue(scope, out owner) || owner != function) return true;
                string kind = scopes.Kinds.TryGetValue(scope, out string value) ? value : "";
                if (kind == "if" || kind == "loop" || kind == "repeat" || kind == "for" || kind == "while") uncertain = true;
                scope = scopes.Parents[scope];
            }
            return budget.WorkExhausted || uncertain;
        }

        static List<PreviewOptionBlock> EndDiscovery(List<PreviewOptionBlock> result, DiscoveryBudget budget, out bool incomplete)
        {
            if (budget.Limited) foreach (var block in result) block.LimitReached = true;
            incomplete = budget.Limited; return result;
        }

        sealed class DiscoveryBudget
        {
            internal readonly PreviewDiscoveryLimits Limits;
            internal bool Limited, WorkExhausted;
            internal int Work, Materialized;
            internal DiscoveryBudget(PreviewDiscoveryLimits limits) { Limits = limits; }
            internal bool Spend()
            {
                if (Work >= Math.Max(0, Limits.MaxWork)) { Limited = true; WorkExhausted = true; return false; }
                Work++; return true;
            }
        }
        sealed class DiscoveryBinding { }
        sealed class DiscoveryScopes
        {
            internal readonly int[] Lexical, Functions;
            internal readonly Dictionary<int, int> Parents = new Dictionary<int, int>();
            internal readonly Dictionary<int, int> FunctionOfScope = new Dictionary<int, int>();
            internal readonly Dictionary<int, string> Kinds = new Dictionary<int, string>();
            internal DiscoveryScopes(int count) { Lexical = new int[count]; Functions = new int[count]; Parents[0] = -1; FunctionOfScope[0] = 0; }
            internal void Add(int scope, int parent, int function, string kind) { Parents[scope] = parent; FunctionOfScope[scope] = function; Kinds[scope] = kind; }
        }
        sealed class DiscoveryBindings
        {
            readonly DiscoveryScopes scopes;
            readonly Dictionary<int, Dictionary<string, DiscoveryBinding>> locals = new Dictionary<int, Dictionary<string, DiscoveryBinding>>();
            readonly Dictionary<int, Dictionary<string, DiscoveryBinding>> globals = new Dictionary<int, Dictionary<string, DiscoveryBinding>>();
            internal DiscoveryBindings(DiscoveryScopes value) { scopes = value; }
            internal void Declare(string name, int token)
            {
                int scope = scopes.Lexical[token];
                if (!locals.TryGetValue(scope, out Dictionary<string, DiscoveryBinding> names)) locals[scope] = names = new Dictionary<string, DiscoveryBinding>(StringComparer.Ordinal);
                names[name] = new DiscoveryBinding();
            }
            internal DiscoveryBinding Resolve(string name, int token, DiscoveryBudget budget)
            {
                int scope = scopes.Lexical[token], function = scopes.Functions[token];
                while (scope >= 0 && scopes.FunctionOfScope.TryGetValue(scope, out int owner) && owner == function && budget.Spend())
                {
                    if (locals.TryGetValue(scope, out Dictionary<string, DiscoveryBinding> names) && names.TryGetValue(name, out DiscoveryBinding local)) return local;
                    scope = scopes.Parents[scope];
                }
                if (budget.WorkExhausted) return null;
                // Do not infer captured variables across a function boundary.
                if (!globals.TryGetValue(function, out Dictionary<string, DiscoveryBinding> globalNames)) globals[function] = globalNames = new Dictionary<string, DiscoveryBinding>(StringComparer.Ordinal);
                if (!globalNames.TryGetValue(name, out DiscoveryBinding global)) globalNames[name] = global = new DiscoveryBinding();
                return global;
            }
        }
        sealed class DiscoveryValue { internal int Position; internal string Value; }
        sealed class DiscoveryTable
        {
            readonly Dictionary<string, List<DiscoveryValue>> values = new Dictionary<string, List<DiscoveryValue>>(StringComparer.Ordinal);
            internal void Add(string key, int position, string value)
            {
                if (!values.TryGetValue(key, out List<DiscoveryValue> history)) values[key] = history = new List<DiscoveryValue>();
                history.Add(new DiscoveryValue { Position = position, Value = value });
            }
            internal bool TryGet(string key, int position, DiscoveryBudget budget, out string value)
            {
                value = null;
                if (!values.TryGetValue(key, out List<DiscoveryValue> history)) return false;
                int low = 0, high = history.Count;
                while (low < high && budget.Spend()) { int middle = low + (high - low) / 2; if (history[middle].Position < position) low = middle + 1; else high = middle; }
                if (budget.WorkExhausted || low == 0) return false;
                value = history[low - 1].Value; return true;
            }
        }
        sealed class DiscoveryChoice { internal DiscoveryBinding Variable; internal string VariableName; internal int Position, Scope, Close, Branch; internal DiscoveryTable Table; internal List<PreviewBranch> Branches; }
        sealed class DiscoveryControl
        {
            internal int Start, Header, Body = -1, ParentScope, ParentFunction;
            internal string Kind;
            internal bool IsElse;
            internal readonly List<PreviewBranch> Branches = new List<PreviewBranch>();
        }
        sealed class DiscoveryStory { internal int Position; internal string Key; }

        static Dictionary<int, int> DiscoveryDelimiters(List<PreviewToken> tokens, DiscoveryBudget budget)
        {
            var result = new Dictionary<int, int>(); var opens = new Stack<int>();
            for (int i = 0; i < tokens.Count && budget.Spend(); i++)
            {
                if (tokens[i].IsString) continue;
                string text = tokens[i].Text;
                if (text == "(" || text == "[" || text == "{")
                { if (opens.Count >= Math.Max(0, budget.Limits.MaxNesting)) { budget.Limited = true; return result; } opens.Push(i); }
                else if (text == ")" || text == "]" || text == "}")
                {
                    if (opens.Count == 0) continue;
                    int open = opens.Pop();
                    if (tokens[open].Text == (text == ")" ? "(" : text == "]" ? "[" : "{")) result[open] = i;
                }
            }
            return result;
        }

        static Dictionary<int, List<PreviewBranch>> DiscoveryBranches(List<PreviewToken> tokens, DiscoveryBudget budget, out Dictionary<int, int> controlEnds, out int[] depths, out DiscoveryScopes scopes)
        {
            controlEnds = new Dictionary<int, int>(); depths = new int[tokens.Count]; scopes = new DiscoveryScopes(tokens.Count);
            int lexical = 0, function = 0;
            var result = new Dictionary<int, List<PreviewBranch>>(); var controls = new Stack<DiscoveryControl>();
            for (int i = 0; i < tokens.Count && budget.Spend(); i++)
            {
                depths[i] = controls.Count; scopes.Lexical[i] = lexical; scopes.Functions[i] = function;
                if (tokens[i].IsString) continue;
                string text = tokens[i].Text;
                var current = controls.Count == 0 ? null : controls.Peek();
                if (text == "then" && current != null && current.Kind == "if" && current.Body < 0) { current.Body = i + 1; continue; }
                if ((text == "else" || text == "elseif") && current != null && current.Kind == "if")
                {
                    AddDiscoveryBranch(tokens, current, i);
                    current.Header = i; current.IsElse = text == "else"; current.Body = current.IsElse ? i + 1 : -1;
                    lexical = i + 1; scopes.Add(lexical, current.ParentScope, function, "if"); continue;
                }
                if (text == "end" || text == "until")
                { if (current != null) { controls.Pop(); lexical = current.ParentScope; function = current.ParentFunction; controlEnds[current.Start] = i; if (current.Kind == "if") { AddDiscoveryBranch(tokens, current, i); result[current.Start] = current.Branches; } } continue; }
                if (text == "do" && current != null && current.Kind == "loop-awaiting-do") { current.Kind = "loop"; continue; }
                if (text != "if" && text != "function" && text != "for" && text != "while" && text != "repeat" && text != "do") continue;
                if (controls.Count >= Math.Max(0, budget.Limits.MaxNesting)) { budget.Limited = true; return result; }
                controls.Push(new DiscoveryControl { Start = i, Header = i, ParentScope = lexical, ParentFunction = function, Kind = text == "for" || text == "while" ? "loop-awaiting-do" : text });
                int parent = lexical; lexical = i + 1; if (text == "function") function = i + 1; scopes.Add(lexical, parent, function, text);
            }
            // A truncated source may still contain a useful, explicitly bounded
            // branch prefix. Incomplete status remains attached to every block.
            foreach (var control in controls)
                if (control.Kind == "if") { AddDiscoveryBranch(tokens, control, tokens.Count); result[control.Start] = control.Branches; }
            return result;
        }

        static void AddDiscoveryBranch(List<PreviewToken> tokens, DiscoveryControl control, int end)
        {
            if (control.Body < 0) return;
            control.Branches.Add(new PreviewBranch { Label = Label(tokens, control.Header, control.Body), Start = control.Body, End = end, IsElse = control.IsElse, HeaderStart = control.Header, HeaderEnd = control.Body - 1 });
        }

        static List<DiscoveryStory> DiscoveryStories(List<PreviewToken> tokens, Dictionary<int, int> delimiters, DiscoveryBudget budget)
        {
            var result = new List<DiscoveryStory>();
            for (int i = 0; i + 2 < tokens.Count && budget.Spend(); i++)
            {
                if (!tokens[i].IsIdentifier || tokens[i].Text != "GetStoryText" || tokens[i + 1].Text != "(" || !tokens[i + 2].IsString ||
                    !delimiters.TryGetValue(i + 1, out int close) || i + 3 >= tokens.Count || i + 3 != close && tokens[i + 3].Text != ",") continue;
                if (TryString(tokens[i + 2].Text, out string key)) result.Add(new DiscoveryStory { Position = tokens[i].Start, Key = key });
            }
            return result;
        }

        static int[] DiscoveryBoundaries(List<PreviewToken> tokens, Dictionary<int, int> controlEnds, DiscoveryBudget budget)
        {
            var result = new int[tokens.Count + 1]; result[tokens.Count] = tokens.Count;
            for (int i = tokens.Count - 1; i >= 0 && budget.Spend(); i--)
            {
                result[i] = result[i + 1];
                if (tokens[i].IsString) continue;
                string text = tokens[i].Text;
                if (controlEnds.TryGetValue(i, out int close)) result[i] = result[close + 1];
                else if (text == "end" || text == "else" || text == "elseif" || text == "until") result[i] = i;
            }
            return result;
        }

        static void AddDiscoveryBlock(string source, string key, int first, int last, List<DiscoveryStory> stories,
            HashSet<string> keys, List<PreviewOptionBlock> result, DiscoveryBudget budget, bool conditional = false)
        {
            if (keys.Contains(key)) return;
            if (result.Count >= Math.Max(0, budget.Limits.MaxOptions)) { budget.Limited = true; return; }
            int available = Math.Max(0, Math.Max(0, budget.Limits.MaxMaterializedCharacters) - budget.Materialized);
            int length = Math.Max(0, last - first);
            if (length > available) { length = available; budget.Limited = true; }
            string story = "";
            int low = 0, high = stories.Count;
            while (low < high && budget.Spend()) { int middle = low + (high - low) / 2; if (stories[middle].Position < first) low = middle + 1; else high = middle; }
            if (!budget.WorkExhausted && low < stories.Count && stories[low].Position < first + length) story = stories[low].Key;
            result.Add(new PreviewOptionBlock { Key = key, Body = source.Substring(first, length), Story = story, Conditional = conditional });
            budget.Materialized += length; keys.Add(key);
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
