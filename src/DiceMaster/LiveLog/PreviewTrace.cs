using System;
using System.Collections.Generic;

namespace DiceMaster.LiveLog
{
    public static class PreviewAnalyzer
    {
        private sealed class Target
        {
            public string Name;
            public bool Conditional;
            public bool Exited;
        }

        private sealed class Context
        {
            public PreviewAnalysis Analysis;
            public PreviewTraceLimits Limits;
            public Func<string, ScriptReadResult> Reader;
            public HashSet<string> Expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Path = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, ScriptReadResult> Reads = new Dictionary<string, ScriptReadResult>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> InvalidOwners = new HashSet<string>(StringComparer.Ordinal);
            public bool Exhausted;
        }

        private static readonly HashSet<string> DisplayCalls = new HashSet<string>(StringComparer.Ordinal)
        {
            "GetStoryText", "GetCurrentTravelScript", "GetTempScript", "GetStatData", "GetFlagData",
            "PlaySound", "PlayMusic", "PlayEnvSound", "FadeOutMusic", "FadeOutEnvSound", "StopMusic",
            "ToggleSaveButton", "CloseDialog"
        };

        public static PreviewAnalysis Analyze(string sourceName, string script, Func<string, ScriptReadResult> reader = null, PreviewTraceLimits limits = null)
        {
            var result = new PreviewAnalysis { RootName = sourceName ?? "", RootScript = script ?? "", Reader = reader, Limits = limits ?? new PreviewTraceLimits() };
            var context = new Context { Analysis = result, Reader = reader, Limits = result.Limits };
            AnalyzeScript(context, sourceName ?? "", script ?? "", 0, false, false);
            PreviewDisplay.Validate(result);
            bool earlierInvalid = false;
            foreach (PreviewEffect effect in result.Effects)
            {
                if (earlierInvalid) { effect.Conditional = true; effect.Condition = "선행 미지원 호출의 정상 종료 여부에 따름"; }
                if (effect.ValidationError != null) earlierInvalid = true;
            }
            return result;
        }

        public static void RetryReads(PreviewAnalysis analysis)
        {
            if (analysis == null || !analysis.HasReadFailures) return;
            string script = analysis.RootScript;
            if (analysis.RootNeedsRead)
            {
                ScriptReadResult read;
                try { read = analysis.Reader == null ? ScriptReadResult.Waiting() : analysis.Reader(analysis.RootName) ?? ScriptReadResult.Failed(); } catch { read = ScriptReadResult.Failed(); }
                if (read.State != ScriptReadState.Success) return;
                script = read.Text ?? ""; analysis.RootScript = script; analysis.RootNeedsRead = false;
            }
            var next = Analyze(analysis.RootName, script, analysis.Reader, analysis.Limits);
            next.SnapshotAllowed = analysis.SnapshotAllowed;
            foreach (PreviewIssue issue in analysis.Issues)
                if (issue.Reason == "option-lexical-limit") next.AddIssue(issue.Source, issue.Function, issue.Reason, issue.IsReadFailure, issue.IsWaiting);
            analysis.ReplaceWith(next);
        }

        public static bool IsLocalScriptName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            foreach (char c in name) if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-')) return false;
            return true;
        }

        private static bool Step(Context context, string source, int work = 1)
        {
            if (context.Exhausted) return false;
            if (work > context.Limits.MaxWork - context.Analysis.WorkCount)
            { context.Analysis.AddIssue(source, "trace", "trace-work-limit"); context.Exhausted = true; return false; }
            context.Analysis.WorkCount += work;
            return true;
        }

        private static void AnalyzeScript(Context context, string source, string script, int depth, bool followup, bool conditional)
        {
            if (context.Exhausted) return;
            if (depth > context.Limits.MaxDepth) { context.Analysis.AddIssue(source, "SetNextScript", "trace-depth-limit"); return; }
            if (context.Path.Contains(source)) { context.Analysis.AddIssue(source, "SetNextScript", "trace-cycle"); return; }
            if (context.Expanded.Contains(source)) return;
            if (context.Analysis.ScriptCount >= context.Limits.MaxScripts) { context.Analysis.AddIssue(source, "SetNextScript", "trace-script-limit"); return; }
            if (script.Length > context.Limits.MaxCharacters - context.Analysis.CharacterCount) { context.Analysis.AddIssue(source, "trace", "trace-character-limit"); return; }
            context.Analysis.ScriptCount++; context.Analysis.CharacterCount += script.Length;
            context.Expanded.Add(source); context.Path.Add(source);
            var tokens = PreviewSyntax.Tokenize(script);
            int remaining = Math.Max(0, context.Limits.MaxTokens - context.Analysis.TokenCount);
            if (tokens.Count > remaining)
            { tokens.RemoveRange(remaining, tokens.Count - remaining); context.Analysis.AddIssue(source, "trace", "trace-token-limit"); }
            context.Analysis.TokenCount += tokens.Count;
            var targets = new List<Target>();
            bool stopped = false;
            ParseRange(context, source, script, tokens, 0, tokens.Count, conditional, followup, targets, ref stopped, 0);
            {
                foreach (Target target in targets)
                {
                    if (!Step(context, source)) break;
                    if (stopped && !target.Exited) continue;
                    if (context.Path.Contains(target.Name)) { context.Analysis.AddIssue(target.Name, "SetNextScript", "trace-cycle"); continue; }
                    if (context.Expanded.Contains(target.Name)) continue;
                    if (depth >= context.Limits.MaxDepth) { context.Analysis.AddIssue(target.Name, "SetNextScript", "trace-depth-limit"); continue; }
                    if (context.Analysis.ScriptCount >= context.Limits.MaxScripts) { context.Analysis.AddIssue(target.Name, "SetNextScript", "trace-script-limit"); continue; }
                    if (context.Reader == null) { context.Analysis.AddIssue(target.Name, "SetNextScript", "script-data-waiting", true, true); continue; }
                    ScriptReadResult read;
                    if (!context.Reads.TryGetValue(target.Name, out read))
                    { try { read = context.Reader(target.Name) ?? ScriptReadResult.Failed(); } catch { read = ScriptReadResult.Failed(); } context.Reads[target.Name] = read; }
                    if (read.State != ScriptReadState.Success)
                    {
                        bool waiting = read.State == ScriptReadState.Waiting;
                        context.Analysis.AddIssue(target.Name, "SetNextScript", read.State == ScriptReadState.Missing ? "script-target-missing" : waiting ? "script-data-waiting" : "script-read-failed", true, waiting);
                        continue;
                    }
                    // Registering a next script does not execute it. Mission/scene processing may replace it.
                    AnalyzeScript(context, target.Name, read.Text ?? "", depth + 1, true, true);
                }
            }
            context.Path.Remove(source);
        }

        private static bool ParseRange(Context context, string source, string script, List<PreviewToken> tokens, int start, int end, bool conditional, bool followup, List<Target> targets, ref bool stopped, int parseDepth)
        {
            if (parseDepth > 64) { context.Analysis.AddIssue(source, "if", "parse-nesting-limit"); return false; }
            bool afterPossibleReturn = false;
            bool conditionalExpression = false;
            for (int i = start; i < end && !stopped; i++)
            {
                if (!Step(context, source)) return false;
                PreviewToken token = tokens[i]; if (token.IsString) continue;
                string t = token.Text;
                if (t == ";" || t == "local" || i > start && tokens[i - 1].Text == ")" && script.Substring(tokens[i - 1].End, token.Start - tokens[i - 1].End).IndexOf('\n') >= 0) conditionalExpression = false;
                if (t == "and" || t == "or") { conditionalExpression = true; continue; }
                if (t == "if")
                {
                    for (int header = i + 1; header < end && tokens[header].Text != "then"; header++)
                    {
                        if (!Step(context, source)) return false;
                        if (!tokens[header].IsString && tokens[header].Text == "function")
                        { context.Analysis.AddIssue(source, "if", "unsupported-condition-function"); stopped = true; return false; }
                    }
                    var branches = PreviewSyntax.Branches(tokens, i, out int closing);
                    if (closing >= end) { context.Analysis.AddIssue(source, "if", "unbalanced-branch"); return false; }
                    if (!Step(context, source, Math.Max(0, closing - i))) return false;
                    bool anyReturn = false; bool everyReturn = branches.Count > 0; bool hasElse = false;
                    bool everyExit = branches.Count > 0; int processedBranches = 0;
                    bool unknownCondition = false;
                    bool anyChoice = false; bool everyChoice = branches.Count > 0;
                    var branchTargets = new List<Target>();
                    foreach (var branch in branches)
                    {
                        if (!Step(context, source)) return false;
                        bool headerChoice = false;
                        for (int header = branch.HeaderStart + 1; header < branch.HeaderEnd; header++)
                        {
                            if (!Step(context, source)) return false;
                            if (!tokens[header].IsString && tokens[header].Text == "function")
                            { context.Analysis.AddIssue(source, "if", "unsupported-condition-function"); stopped = true; return false; }
                            if (PreviewSyntax.TryCall(tokens, header, out string conditionOwner, out string conditionName, out int conditionOpen, out int conditionClose) || PreviewSyntax.TrySugarCall(tokens, header, out conditionOwner, out conditionName, out conditionOpen, out conditionClose))
                            {
                                if (conditionOwner == "" && conditionName == "choose")
                                {
                                    context.Analysis.StoppedAtChoice = true;
                                    context.Analysis.AddIssue(source, "choose", "nested-choice-boundary");
                                    if (processedBranches == 0) { stopped = true; return false; }
                                    headerChoice = true; break;
                                }
                                if (!IsKnownPure(conditionOwner, conditionName))
                                { context.Analysis.AddIssue(source, conditionName, "condition-invocation-not-evaluated"); unknownCondition = true; }
                            }
                        }
                        if (headerChoice) { anyChoice = true; everyReturn = false; hasElse = true; break; }
                        var localTargets = new List<Target>();
                        foreach (Target target in targets) localTargets.Add(new Target { Name = target.Name, Conditional = true, Exited = target.Exited });
                        bool branchStopped = false;
                        bool returns = ParseRange(context, source, script, tokens, branch.Start, branch.End, true, followup, localTargets, ref branchStopped, parseDepth + 1);
                        anyReturn |= returns; everyReturn &= returns; hasElse |= branch.IsElse;
                        anyChoice |= branchStopped; everyChoice &= branchStopped;
                        everyExit &= returns || branchStopped; processedBranches++;
                        foreach (Target target in localTargets)
                            if ((!branchStopped || target.Exited) && !branchTargets.Exists(x => string.Equals(x.Name, target.Name, StringComparison.OrdinalIgnoreCase) && x.Exited == target.Exited)) branchTargets.Add(target);
                    }
                    if (!hasElse) foreach (Target target in targets) if (!branchTargets.Exists(x => string.Equals(x.Name, target.Name, StringComparison.OrdinalIgnoreCase) && x.Exited == target.Exited)) branchTargets.Add(target);
                    targets.Clear(); targets.AddRange(branchTargets);
                    i = closing;
                    if (everyReturn && hasElse) return true;
                    if ((everyChoice || everyExit) && hasElse) { stopped = true; return false; }
                    afterPossibleReturn |= anyReturn || anyChoice || unknownCondition;
                    continue;
                }
                if (t == "for" || t == "while" || t == "repeat" || t == "function" || t == "do")
                {
                    context.Analysis.AddIssue(source, t, t == "function" ? "function-body-not-executed" : "unsupported-loop-or-block");
                    if (t == "function" && i + 1 < end && IsReservedOwner(tokens[i + 1].Text))
                    { context.InvalidOwners.Add(tokens[i + 1].Text); context.Analysis.AddIssue(source, tokens[i + 1].Text, "manager-binding-not-evaluated"); }
                    int closing = SkipBlock(tokens, i, end);
                    if (closing < 0) { context.Analysis.AddIssue(source, t, "unbalanced-block"); return false; }
                    if (!Step(context, source, Math.Max(0, closing - i))) return false;
                    i = closing; afterPossibleReturn = true; continue;
                }
                if (t == "return")
                {
                    if (i + 1 < end && tokens[i + 1].Text != ";" && tokens[i + 1].Text != "end" && tokens[i + 1].Text != "else" && tokens[i + 1].Text != "elseif" && tokens[i + 1].Text != "until")
                        context.Analysis.AddIssue(source, "return", "return-expression-not-evaluated");
                    foreach (Target target in targets) target.Exited = true;
                    return true;
                }
                if (t == "goto" || t == "break") { context.Analysis.AddIssue(source, t, "unsupported-control-flow"); stopped = true; return false; }
                if (IsReservedOwner(t))
                {
                    if (i > start && (tokens[i - 1].Text == "." || tokens[i - 1].Text == ":"))
                    {
                        context.Analysis.AddIssue(source, t, "qualified-manager-reference");
                        afterPossibleReturn = true;
                        if (PreviewSyntax.TryCall(tokens, i, out _, out _, out _, out int qualifiedClose) && qualifiedClose >= i)
                            i = qualifiedClose;
                        continue;
                    }
                    if (IsManagerAssignment(tokens, i, end))
                    { context.InvalidOwners.Add(t); context.Analysis.AddIssue(source, t, "manager-binding-not-evaluated"); afterPossibleReturn = true; }
                    if (i + 1 < end && tokens[i + 1].Text == "[")
                    { context.Analysis.AddIssue(source, t, "dynamic-call"); int closing = PreviewSyntax.MatchClose(tokens, i + 1, end); if (closing >= 0) i = closing; afterPossibleReturn = true; continue; }
                }
                if (PreviewSyntax.TrySugarCall(tokens, i, out string sugarOwner, out string sugarName, out int sugarArgument, out int sugarClose))
                {
                    if (sugarOwner == "" && sugarName == "choose")
                    {
                        context.Analysis.StoppedAtChoice = true; stopped = true;
                        if (conditional || afterPossibleReturn || conditionalExpression) context.Analysis.AddIssue(source, "choose", "conditional-choice-boundary");
                        break;
                    }
                    if (context.InvalidOwners.Contains(sugarOwner))
                    { context.Analysis.AddIssue(source, sugarOwner, "manager-binding-not-evaluated"); afterPossibleReturn = true; }
                    if (!IsKnownPure(sugarOwner, sugarName))
                    { context.Analysis.AddIssue(source, sugarOwner.Length == 0 ? sugarName : sugarOwner + "." + sugarName, "unsupported-call-syntax"); afterPossibleReturn = true; }
                    if (sugarClose < 0 || sugarClose >= end) { context.Analysis.AddIssue(source, sugarName, "unbalanced-call"); return false; }
                    if (!tokens[sugarArgument].IsString)
                        for (int nested = sugarArgument + 1; nested < sugarClose; nested++)
                        {
                            if (!Step(context, source)) return false;
                            if (PreviewSyntax.TryCall(tokens, nested, out string nestedOwner, out string nestedName, out int nestedOpen, out int nestedClose) || PreviewSyntax.TrySugarCall(tokens, nested, out nestedOwner, out nestedName, out nestedOpen, out nestedClose))
                            {
                                if (nestedOwner == "" && nestedName == "choose")
                                { context.Analysis.StoppedAtChoice = true; context.Analysis.AddIssue(source, "choose", "nested-choice-boundary"); stopped = true; return false; }
                                if (!IsKnownPure(nestedOwner, nestedName)) { context.Analysis.AddIssue(source, nestedName, "nested-invocation-not-evaluated"); afterPossibleReturn = true; }
                            }
                        }
                    i = sugarClose; continue;
                }
                if (!PreviewSyntax.TryCall(tokens, i, out string owner, out string name, out int open, out int close)) continue;
                if (close < 0 || close >= end) { context.Analysis.AddIssue(source, name, "unbalanced-call"); return false; }
                var args = PreviewSyntax.Arguments(script, tokens, open, close);
                bool nestedUnknown = false;
                for (int nested = open + 1; nested < close; nested++)
                {
                    if (!Step(context, source)) return false;
                    if (PreviewSyntax.TryCall(tokens, nested, out string nestedOwner, out string nestedName, out int nestedOpen, out int nestedClose) || PreviewSyntax.TrySugarCall(tokens, nested, out nestedOwner, out nestedName, out nestedOpen, out nestedClose))
                    {
                        if (nestedOwner == "" && nestedName == "choose")
                        { context.Analysis.StoppedAtChoice = true; context.Analysis.AddIssue(source, "choose", "nested-choice-boundary"); stopped = true; return false; }
                        if (!IsKnownPure(nestedOwner, nestedName)) { context.Analysis.AddIssue(source, nestedName, "nested-invocation-not-evaluated"); nestedUnknown = true; }
                    }
                }
                if (nestedUnknown) afterPossibleReturn = true;
                if (owner == "" && name == "choose")
                {
                    context.Analysis.StoppedAtChoice = true; stopped = true;
                    if (conditional || afterPossibleReturn || conditionalExpression) context.Analysis.AddIssue(source, "choose", "conditional-choice-boundary");
                    break;
                }
                if (context.InvalidOwners.Contains(owner))
                { context.Analysis.AddIssue(source, owner, "manager-binding-not-evaluated"); afterPossibleReturn = true; i = close; continue; }
                bool knownPure = IsKnownPure(owner, name);
                if (owner == "statmodifymanager" && !knownPure || owner == "luamanager" && !knownPure)
                {
                    var effect = new PreviewEffect { Owner = owner, Name = name, Arguments = args, Conditional = conditional || afterPossibleReturn || conditionalExpression, Followup = followup, Condition = conditional || afterPossibleReturn || conditionalExpression ? "분기 조건 또는 후속 실행 여부에 따름" : "", Source = source, Sequence = context.Analysis.Effects.Count };
                    context.Analysis.Effects.Add(effect);
                    if (!PreviewDisplay.IsKnownEffect(owner, name)) afterPossibleReturn = true;
                    if (owner == "luamanager" && name == "SetNextScript")
                    {
                        if (!afterPossibleReturn && !conditionalExpression) targets.RemoveAll(x => !x.Exited);
                        if (args.Count != 1 || !PreviewSyntax.TryString(args[0], out string next) || next.Length > 0 && !IsLocalScriptName(next)) context.Analysis.AddIssue(source, name, "dynamic-or-invalid-script-target");
                        else if (next.Length > 0)
                        {
                            if (!targets.Exists(x => string.Equals(x.Name, next, StringComparison.OrdinalIgnoreCase))) targets.Add(new Target { Name = next, Conditional = conditional || afterPossibleReturn || conditionalExpression });
                        }
                    }
                    if (targets.Count > 0 && (owner == "statmodifymanager" && name == "Mission" || owner == "luamanager" && name == "ChangeScene" && args.Count > 0 && PreviewSyntax.TryString(args[0], out string scene) && (scene == "Story" || scene == "Free")))
                        context.Analysis.AddIssue(source, name, "mission-or-scene-may-replace-followup");
                }
                else if (!knownPure) { context.Analysis.AddIssue(source, owner.Length == 0 ? name : owner + "." + name, "unsupported-call"); afterPossibleReturn = true; }
                i = close;
            }
            return false;
        }

        private static bool IsReservedOwner(string name)
        {
            return name == "statmodifymanager" || name == "luamanager";
        }

        private static bool IsManagerAssignment(List<PreviewToken> tokens, int index, int end)
        {
            int next = index + 1;
            if (next < end && tokens[next].Text == "=") return true;
            if (index > 0 && tokens[index - 1].Text == "local" && (next >= end || tokens[next].Text != "." && tokens[next].Text != ":" && tokens[next].Text != "[")) return true;
            if (next + 2 < end && (tokens[next].Text == "." || tokens[next].Text == ":") && tokens[next + 1].IsIdentifier && tokens[next + 2].Text == "=") return true;
            if (next < end && tokens[next].Text == "[")
            { int closing = PreviewSyntax.MatchClose(tokens, next, end); if (closing >= 0 && closing + 1 < end && tokens[closing + 1].Text == "=") return true; }
            while (next + 1 < end && tokens[next].Text == "," && tokens[next + 1].IsIdentifier) next += 2;
            return next < end && tokens[next].Text == "=";
        }

        private static bool IsKnownPure(string owner, string name)
        {
            return owner == "luamanager" && DisplayCalls.Contains(name)
                || owner == "statmodifymanager" && (name == "GetDisplayTime" || name == "GetModifyString" || name == "UpdateSetPlayerStat")
                || owner == "checkpointmanager" && (name == "Switch" || name == "Position")
                || owner == "" && (name == "GetStoryText" || name == "tostring" || name == "tonumber" || name == "type");
        }

        private static int SkipBlock(List<PreviewToken> tokens, int start, int end)
        {
            var blocks = new Stack<string>();
            PreviewSyntax.TrackBlockOpen(tokens[start].Text, blocks);
            for (int i = start + 1; i < end; i++)
            {
                if (tokens[i].IsString) continue;
                string t = tokens[i].Text;
                if (t == "end" || t == "until") { if (blocks.Count == 0) return -1; blocks.Pop(); if (blocks.Count == 0) return i; }
                else PreviewSyntax.TrackBlockOpen(t, blocks);
            }
            return -1;
        }
    }
}
