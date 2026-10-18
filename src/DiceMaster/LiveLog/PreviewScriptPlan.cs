using System;
using System.Collections.Generic;
using System.Text;

namespace DiceMaster.LiveLog
{
    // A context-independent, lazily materialized syntax/effect plan. It never
    // holds a reader, current state, branch identity, result or traversal job.
    // All mutable lists handed to the analyzer are private copies. Memo units
    // and retained string characters are bounded by the source/token payload,
    // so eviction of its owning parse-cache entry releases the whole plan.
    public sealed class PreviewScriptPlan
    {
        sealed class Call
        {
            internal string Owner, Name;
            internal int Open, Close;
        }
        sealed class BranchSet
        {
            internal List<PreviewBranch> Branches;
            internal int Close;
        }
        sealed class ArgumentsSet
        {
            internal List<string> Arguments;
            internal int Close;
        }
        sealed class Validation
        {
            internal string Owner, Name, Error;
            internal List<string> Arguments;
            internal List<object> Constants;
            internal int Close;
        }

        readonly string source;
        readonly List<PreviewToken> tokens;
        readonly Dictionary<int, int> closes = new Dictionary<int, int>();
        readonly Dictionary<int, Call> calls = new Dictionary<int, Call>();
        readonly Dictionary<int, Call> sugarCalls = new Dictionary<int, Call>();
        readonly Dictionary<int, BranchSet> branches = new Dictionary<int, BranchSet>();
        readonly Dictionary<int, ArgumentsSet> arguments = new Dictionary<int, ArgumentsSet>();
        readonly Dictionary<int, int> blocks = new Dictionary<int, int>();
        readonly Dictionary<int, Validation> validations = new Dictionary<int, Validation>();
        readonly long maxMemoUnits, maxMemoCharacters;
        long memoUnits, memoCharacters;
        public List<PreviewToken> Tokens { get { return tokens; } }
        public long MemoUnits { get { return memoUnits; } }
        public long MemoCharacters { get { return memoCharacters; } }
        public long SyntaxBuilds { get; private set; }
        public long SyntaxHits { get; private set; }
        public long ValidationBuilds { get; private set; }
        public long ValidationHits { get; private set; }

        public PreviewScriptPlan(string source, List<PreviewToken> tokens)
        {
            this.source = source ?? "";
            this.tokens = tokens ?? new List<PreviewToken>();
            maxMemoUnits = (long)this.tokens.Count * 8;
            maxMemoCharacters = (long)this.source.Length * 4;
        }

        bool Reserve(long units, long characters = 0)
        {
            if (units > maxMemoUnits - memoUnits || characters > maxMemoCharacters - memoCharacters) return false;
            memoUnits += units; memoCharacters += characters;
            return true;
        }

        public int MatchClose(int open, int limit = -1)
        {
            if (limit < 0) limit = tokens.Count;
            int close;
            if (closes.TryGetValue(open, out close)) SyntaxHits++;
            else
            {
                SyntaxBuilds++;
                close = PreviewSyntax.MatchClose(tokens, open);
                if (Reserve(1)) closes.Add(open, close);
            }
            return close < limit ? close : -1;
        }

        static void Assign(Call call, out string owner, out string name, out int open, out int close)
        { owner = call.Owner; name = call.Name; open = call.Open; close = call.Close; }

        public bool TryCall(int index, out string owner, out string name, out int open, out int close)
        {
            owner = ""; name = ""; open = close = -1;
            Call call;
            if (calls.TryGetValue(index, out call)) { SyntaxHits++; Assign(call, out owner, out name, out open, out close); return true; }
            if (index >= tokens.Count || !tokens[index].IsIdentifier || index > 0 && (tokens[index - 1].Text == "." || tokens[index - 1].Text == ":")) return false;
            if (index + 3 < tokens.Count && (tokens[index + 1].Text == "." || tokens[index + 1].Text == ":") && tokens[index + 2].IsIdentifier && tokens[index + 3].Text == "(")
            { owner = tokens[index].Text; name = tokens[index + 2].Text; open = index + 3; }
            else if (index + 1 < tokens.Count && tokens[index + 1].Text == "(") { name = tokens[index].Text; open = index + 1; }
            else return false;
            close = MatchClose(open); SyntaxBuilds++;
            if (Reserve(1)) calls.Add(index, new Call { Owner = owner, Name = name, Open = open, Close = close });
            return true;
        }

        public bool TrySugarCall(int index, out string owner, out string name, out int argument, out int close)
        {
            owner = ""; name = ""; argument = close = -1;
            Call call;
            if (sugarCalls.TryGetValue(index, out call)) { SyntaxHits++; Assign(call, out owner, out name, out argument, out close); return true; }
            if (index >= tokens.Count || !tokens[index].IsIdentifier || index > 0 && (tokens[index - 1].Text == "." || tokens[index - 1].Text == ":")) return false;
            int next = index + 1;
            if (index + 2 < tokens.Count && (tokens[next].Text == "." || tokens[next].Text == ":") && tokens[next + 1].IsIdentifier)
            { owner = tokens[index].Text; name = tokens[next + 1].Text; next += 2; }
            else name = tokens[index].Text;
            if (next >= tokens.Count || !tokens[next].IsString && tokens[next].Text != "{") return false;
            argument = next; close = tokens[next].IsString ? next : MatchClose(next); SyntaxBuilds++;
            if (Reserve(1)) sugarCalls.Add(index, new Call { Owner = owner, Name = name, Open = argument, Close = close });
            return true;
        }

        static PreviewBranch CopyBranch(PreviewBranch value)
        { return new PreviewBranch { Label = value.Label, Start = value.Start, End = value.End, IsElse = value.IsElse, HeaderStart = value.HeaderStart, HeaderEnd = value.HeaderEnd }; }

        public List<PreviewBranch> Branches(int start, out int closing)
        {
            BranchSet entry;
            if (branches.TryGetValue(start, out entry)) { SyntaxHits++; closing = entry.Close; }
            else
            {
                SyntaxBuilds++;
                var parsed = PreviewSyntax.Branches(tokens, start, out closing);
                long characters = 0; foreach (var branch in parsed) characters += (branch.Label ?? "").Length;
                entry = new BranchSet { Branches = parsed, Close = closing };
                if (Reserve(1L + parsed.Count, characters)) branches.Add(start, entry);
            }
            var result = new List<PreviewBranch>(entry.Branches.Count);
            foreach (var branch in entry.Branches) result.Add(CopyBranch(branch));
            return result;
        }

        string Normalize(int start, int end)
        {
            var result = new StringBuilder();
            for (int i = start; i < end; i++) { if (i > start) result.Append(' '); result.Append(tokens[i].Text); }
            return result.ToString();
        }

        List<string> ParsedArguments(int open, int close)
        {
            ArgumentsSet entry;
            if (arguments.TryGetValue(open, out entry) && entry.Close == close) { SyntaxHits++; return entry.Arguments; }
            SyntaxBuilds++;
            var result = new List<string>();
            if (close >= 0 && close > open)
            {
                int first = open + 1;
                for (int i = first; i < close; i++)
                {
                    if (!tokens[i].IsString && (tokens[i].Text == "(" || tokens[i].Text == "[" || tokens[i].Text == "{"))
                    { int nested = MatchClose(i, close); if (nested >= 0) { i = nested; continue; } }
                    if (!tokens[i].IsString && tokens[i].Text == ",") { result.Add(Normalize(first, i)); first = i + 1; }
                }
                string last = Normalize(first, close);
                if (last.Length != 0 || result.Count != 0) result.Add(last);
            }
            long characters = 0; foreach (string value in result) characters += value.Length;
            if (!arguments.ContainsKey(open) && Reserve(1L + result.Count, characters)) arguments.Add(open, new ArgumentsSet { Arguments = result, Close = close });
            return result;
        }

        public List<string> Arguments(int open, int close)
        { return new List<string>(ParsedArguments(open, close)); }

        static bool SameArguments(List<string> first, List<string> second)
        {
            if (first.Count != second.Count) return false;
            for (int i = 0; i < first.Count; i++) if (!string.Equals(first[i], second[i], StringComparison.Ordinal)) return false;
            return true;
        }

        public void ValidateEffect(PreviewEffect effect, int open, int close)
        {
            Validation entry;
            if (validations.TryGetValue(open, out entry) && entry.Close == close && entry.Owner == effect.Owner && entry.Name == effect.Name && SameArguments(entry.Arguments, effect.Arguments))
            {
                ValidationHits++;
                effect.Validated = true; effect.ValidationError = entry.Error;
                effect.Constants = new List<object>(entry.Constants);
                return;
            }
            ValidationBuilds++;
            PreviewDisplay.ValidateEffect(effect);
            long characters = 0;
            foreach (string value in effect.Arguments) characters += value.Length;
            foreach (object value in effect.Constants) if (value is string) characters += ((string)value).Length;
            if (!validations.ContainsKey(open) && Reserve(1L + effect.Arguments.Count + effect.Constants.Count, characters))
                validations.Add(open, new Validation { Owner = effect.Owner, Name = effect.Name, Close = close, Error = effect.ValidationError,
                    Arguments = new List<string>(effect.Arguments), Constants = new List<object>(effect.Constants) });
        }

        public int SkipBlock(int start, int end)
        {
            int closing;
            if (blocks.TryGetValue(start, out closing)) SyntaxHits++;
            else
            {
                SyntaxBuilds++;
                var stack = new Stack<string>();
                PreviewSyntax.TrackBlockOpen(tokens[start].Text, stack); closing = -1;
                for (int i = start + 1; i < tokens.Count; i++)
                {
                    if (tokens[i].IsString) continue;
                    string token = tokens[i].Text;
                    if (token == "end" || token == "until") { if (stack.Count == 0) break; stack.Pop(); if (stack.Count == 0) { closing = i; break; } }
                    else PreviewSyntax.TrackBlockOpen(token, stack);
                }
                if (Reserve(1)) blocks.Add(start, closing);
            }
            return closing < end ? closing : -1;
        }
    }
}
