using System;
using System.Collections.Generic;

namespace DiceMaster.LiveLog
{
    public static class PreviewAnalyzer
    {
        private sealed class Target
        {
            public string Name;
            public string Function;
            public bool Conditional;
            public bool Exited;
        }

        private sealed class Job
        {
            public string Name;
            public string Script;
            public string Function;
            public int Depth;
            public HashSet<string> Ancestors;
            public HashSet<string> InvalidOwners;
        }

        private sealed class Context
        {
            public PreviewAnalysis Analysis;
            public PreviewTraceLimits Limits;
            public Func<string, ScriptReadResult> Reader;
            public HashSet<string> Expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Scheduled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public Queue<Job> Pending = new Queue<Job>();
            public PreviewParseCache Cache;
            public Dictionary<string, ScriptReadResult> Reads = new Dictionary<string, ScriptReadResult>(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> InvalidOwners = new HashSet<string>(StringComparer.Ordinal);
            public bool Exhausted;
            public int BranchCount;
        }

        // Bindings are syntactic descriptions, never values retained from a save.
        // Branches own copies; writes/unknown calls invalidate current-state hints.
        private sealed class ConditionScope
        {
            internal Dictionary<string, PreviewCondition> Bindings = new Dictionary<string, PreviewCondition>(StringComparer.Ordinal);
            internal HashSet<string> Assigned = new HashSet<string>(StringComparer.Ordinal);
            internal bool Allowed = true;
            internal int TableDepth;
            internal ConditionScope Copy()
            {
                return new ConditionScope { Bindings = new Dictionary<string, PreviewCondition>(Bindings, StringComparer.Ordinal), Allowed = Allowed };
            }
        }

        private static readonly HashSet<string> DisplayCalls = new HashSet<string>(StringComparer.Ordinal)
        {
            "GetStoryText", "GetCurrentTravelScript", "GetTempScript", "GetStatData", "GetFlagData",
            "PlaySound", "PlayMusic", "PlayEnvSound", "FadeOutMusic", "FadeOutEnvSound", "StopMusic",
            "ToggleSaveButton", "CloseDialog"
        };

        public static PreviewAnalysis Analyze(string sourceName, string script, Func<string, ScriptReadResult> reader = null, PreviewTraceLimits limits = null)
        {
            return AnalyzeWithCache(sourceName, script, reader, limits, null);
        }

        // Keep Analyze's four-argument reflection contract. Lexical reuse has a
        // separate entry point; no state-dependent effects enter this cache.
        public static PreviewAnalysis AnalyzeWithCache(string sourceName, string script, Func<string, ScriptReadResult> reader, PreviewTraceLimits limits, PreviewParseCache cache)
        {
            var result = new PreviewAnalysis { RootName = sourceName ?? "", RootScript = script ?? "", Reader = reader,
                Limits = limits ?? new PreviewTraceLimits(), ParseCache = cache ?? new PreviewParseCache() };
            var context = new Context { Analysis = result, Reader = reader, Limits = result.Limits, Cache = result.ParseCache };
            var root = new Job { Name = result.RootName, Script = result.RootScript, Function = "trace", Depth = 0,
                Ancestors = new HashSet<string>(StringComparer.OrdinalIgnoreCase), InvalidOwners = new HashSet<string>(StringComparer.Ordinal) };
            context.Pending.Enqueue(root);
            context.Scheduled.Add(ContextKey(root.Name, root.InvalidOwners));
            while (context.Pending.Count > 0 && !context.Exhausted) AnalyzeJob(context, context.Pending.Dequeue());
            PreviewDisplay.Validate(result);
            bool earlierInvalid = false;
            foreach (PreviewEffect effect in result.Effects)
            {
                if (earlierInvalid) { effect.Conditional = true; effect.ExecutionUncertain = true; effect.Condition = "선행 미지원 호출의 정상 종료 여부에 따름"; }
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
            var next = AnalyzeWithCache(analysis.RootName, script, analysis.Reader, analysis.Limits, analysis.ParseCache);
            next.SnapshotAllowed = analysis.SnapshotAllowed;
            if (analysis.RootConditional) next.MarkConditionalRoot();
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

        private static string ContextKey(string source, HashSet<string> invalidOwners)
        {
            // These are the only two mutable manager bindings inspected by the
            // analyzer. A source therefore has at most four lexical contexts.
            int mask = (invalidOwners.Contains("statmodifymanager") ? 1 : 0) | (invalidOwners.Contains("luamanager") ? 2 : 0);
            return source + "\0" + mask;
        }

        private static void AnalyzeJob(Context context, Job job)
        {
            string source = job.Name;
            if (job.Depth > context.Limits.MaxDepth) { context.Analysis.AddIssue(source, job.Function, "trace-depth-limit"); return; }
            if (job.Ancestors.Contains(source)) { context.Analysis.AddIssue(source, job.Function, "trace-cycle"); return; }
            string key = ContextKey(source, job.InvalidOwners);
            if (context.Expanded.Contains(key)) return;
            bool newSource = !context.Sources.Contains(source);
            if (newSource && context.Analysis.ScriptCount >= context.Limits.MaxScripts)
            { context.Analysis.AddIssue(source, job.Function, "trace-script-limit"); return; }
            string script = job.Script;
            if (script == null)
            {
                if (context.Reader == null) { context.Analysis.AddIssue(source, job.Function, "script-data-waiting", true, true); return; }
                ScriptReadResult read;
                if (!context.Reads.TryGetValue(source, out read))
                { try { read = context.Reader(source) ?? ScriptReadResult.Failed(); } catch { read = ScriptReadResult.Failed(); } context.Reads[source] = read; }
                if (read.State != ScriptReadState.Success)
                {
                    bool waiting = read.State == ScriptReadState.Waiting;
                    context.Analysis.AddIssue(source, job.Function, read.State == ScriptReadState.Missing ? "script-target-missing" : waiting ? "script-data-waiting" : "script-read-failed", true, waiting);
                    return;
                }
                script = read.Text ?? "";
            }
            int remainingCharacters = Math.Max(0, context.Limits.MaxCharacters - context.Analysis.CharacterCount);
            if (script.Length > remainingCharacters)
            {
                // Preserve the finite readable prefix. Apply this guard before
                // lexical caching so a warm cache never enlarges the budget.
                context.Analysis.AddIssue(source, "trace", "trace-character-limit");
                if (remainingCharacters == 0) return;
                script = script.Substring(0, remainingCharacters);
            }
            if (newSource) { context.Sources.Add(source); context.Analysis.ScriptCount++; }
            context.Analysis.ContextCount++;
            context.Analysis.CharacterCount += script.Length;
            context.Expanded.Add(key);
            context.InvalidOwners = new HashSet<string>(job.InvalidOwners, StringComparer.Ordinal);
            var plan = context.Cache.Plan(script);
            var tokens = plan.Tokens;
            int remaining = Math.Max(0, context.Limits.MaxTokens - context.Analysis.TokenCount);
            int end = Math.Min(tokens.Count, remaining);
            if (tokens.Count > end)
            {
                context.Analysis.AddIssue(source, "trace", "trace-token-limit");
                // Helpers inspect list.Count while finding calls/branches. A
                // bounded private view must also stop that lookahead; never
                // trim the cached list shared with another analysis.
                tokens = tokens.GetRange(0, end);
                plan = new PreviewScriptPlan(script, tokens);
            }
            context.Analysis.TokenCount += end;
            var targets = new List<Target>();
            bool stopped = false;
            ParseRange(context, source, script, plan, 0, end, job.Depth > 0, job.Depth > 0, targets, ref stopped, 0, new List<PreviewBranchContext>(), false, new ConditionScope { Allowed = job.Depth == 0 });
            var path = new HashSet<string>(job.Ancestors, StringComparer.OrdinalIgnoreCase) { source };
            foreach (Target target in targets)
            {
                if (!Step(context, source)) break;
                if (stopped && !target.Exited) continue;
                if (path.Contains(target.Name)) { context.Analysis.AddIssue(target.Name, target.Function, "trace-cycle"); continue; }
                if (job.Depth >= context.Limits.MaxDepth) { context.Analysis.AddIssue(target.Name, target.Function, "trace-depth-limit"); continue; }
                string nextKey = ContextKey(target.Name, context.InvalidOwners);
                if (context.Scheduled.Contains(nextKey)) continue;
                // Bound the admitted frontier as well as parsed work, including
                // targets whose static reads fail. It cannot grow unboundedly
                // while MaxScripts counts only successfully parsed sources.
                if (context.Scheduled.Count >= Math.Max(1L, (long)context.Limits.MaxScripts * 4))
                { context.Analysis.AddIssue(source, "trace", "trace-frontier-limit"); continue; }
                context.Scheduled.Add(nextKey);
                context.Pending.Enqueue(new Job { Name = target.Name, Function = target.Function, Depth = job.Depth + 1,
                    Ancestors = new HashSet<string>(path, StringComparer.OrdinalIgnoreCase),
                    InvalidOwners = new HashSet<string>(context.InvalidOwners, StringComparer.Ordinal) });
            }
        }

        private static bool ParseRange(Context context, string source, string script, PreviewScriptPlan plan, int start, int end, bool conditional, bool followup, List<Target> targets, ref bool stopped, int parseDepth, List<PreviewBranchContext> branchPath, bool inheritedExecutionUncertainty, ConditionScope conditions)
        {
            var tokens = plan.Tokens;
            if (parseDepth > 64) { context.Analysis.AddIssue(source, "if", "parse-nesting-limit"); return false; }
            bool afterPossibleReturn = false;
            bool conditionalExpression = false;
            for (int i = start; i < end && !stopped; i++)
            {
                if (!Step(context, source)) return false;
                PreviewToken token = tokens[i]; if (token.IsString) continue;
                string t = token.Text;
                if (t == "{") conditions.TableDepth++;
                else if (t == "}" && conditions.TableDepth > 0) conditions.TableDepth--;
                if (conditions.Allowed && conditions.TableDepth == 0) TrackConditionBinding(script, tokens, i, end, conditions);
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
                    var branches = plan.Branches(i, out int closing);
                    if (closing >= end) { context.Analysis.AddIssue(source, "if", "unbalanced-branch"); return false; }
                    if (!Step(context, source, Math.Max(0, closing - i))) return false;
                    bool anyReturn = false; bool everyReturn = branches.Count > 0; bool hasElse = false;
                    bool everyExit = branches.Count > 0; int processedBranches = 0;
                    bool unknownCondition = false;
                    bool anyChoice = false; bool everyChoice = branches.Count > 0;
                    var branchTargets = new List<Target>();
                    int branchGroup = ++context.BranchCount;
                    int alternative = 0;
                    var conditionBase = conditions.Copy();
                    PreviewCondition remainingCondition = PreviewConditions.Constant(true);
                    foreach (var branch in branches)
                    {
                        if (!Step(context, source)) return false;
                        bool headerChoice = false;
                        for (int header = branch.HeaderStart + 1; header < branch.HeaderEnd; header++)
                        {
                            if (!Step(context, source)) return false;
                            if (!tokens[header].IsString && tokens[header].Text == "function")
                            { context.Analysis.AddIssue(source, "if", "unsupported-condition-function"); stopped = true; return false; }
                            if (plan.TryCall(header, out string conditionOwner, out string conditionName, out int conditionOpen, out int conditionClose) || plan.TrySugarCall(header, out conditionOwner, out conditionName, out conditionOpen, out conditionClose))
                            {
                                if (conditionOwner == "" && conditionName == "choose")
                                {
                                    context.Analysis.StoppedAtChoice = true;
                                    context.Analysis.AddIssue(source, "choose", "nested-choice-boundary");
                                    if (processedBranches == 0) { stopped = true; return false; }
                                    headerChoice = true; break;
                                }
                                if (!IsSnapshotCheckpoint(conditionOwner, conditionName, plan.Arguments(conditionOpen, conditionClose)))
                                { conditionBase.Allowed = false; conditions.Allowed = false; }
                                if (!IsKnownPure(conditionOwner, conditionName))
                                { context.Analysis.AddIssue(source, conditionName, "condition-invocation-not-evaluated"); unknownCondition = true; }
                            }
                        }
                        if (headerChoice) { anyChoice = true; everyReturn = false; hasElse = true; break; }
                        var localTargets = new List<Target>();
                        foreach (Target target in targets) localTargets.Add(new Target { Name = target.Name, Function = target.Function, Conditional = true, Exited = target.Exited });
                        bool branchStopped = false;
                        var path = new List<PreviewBranchContext>(branchPath);
                        bool hintAllowed = conditionBase.Allowed && !followup &&
                            !inheritedExecutionUncertainty && !conditionalExpression;
                        PreviewCondition predicate = !hintAllowed ? PreviewConditions.Unknown : branch.IsElse ? PreviewConditions.Constant(true) :
                            PreviewConditions.ParseTokens(tokens, branch.HeaderStart + 1, branch.HeaderEnd, conditionBase.Bindings);
                        // An unknown header can run Lua indexing/comparison
                        // metamethods before later elseif arms. Do not treat an
                        // unknown value as a proven side-effect-free expression.
                        if (hintAllowed && !predicate.IsKnownSyntax)
                        { hintAllowed = false; conditionBase.Allowed = false; conditions.Allowed = false; }
                        PreviewCondition selected = hintAllowed ? PreviewConditions.All(remainingCondition, predicate) : null;
                        path.Add(new PreviewBranchContext { Group = branchGroup, Alternative = alternative++, IsElse = branch.IsElse,
                            CurrentCondition = selected, CurrentConditionAllowed = hintAllowed });
                        if (hintAllowed) remainingCondition = PreviewConditions.All(remainingCondition, PreviewConditions.Not(predicate));
                        var branchConditions = conditionBase.Copy();
                        bool returns = ParseRange(context, source, script, plan, branch.Start, branch.End, true, followup, localTargets, ref branchStopped, parseDepth + 1, path, inheritedExecutionUncertainty || afterPossibleReturn || conditionalExpression, branchConditions);
                        conditions.Allowed &= branchConditions.Allowed;
                        foreach (string assigned in branchConditions.Assigned)
                        { conditions.Bindings.Remove(assigned); conditions.Assigned.Add(assigned); }
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
                    conditions.Allowed &= !anyReturn && !anyChoice && !unknownCondition;
                    continue;
                }
                if (t == "for" || t == "while" || t == "repeat" || t == "function" || t == "do")
                {
                    context.Analysis.AddIssue(source, t, t == "function" ? "function-body-not-executed" : "unsupported-loop-or-block");
                    if (t == "function" && i + 1 < end && IsReservedOwner(tokens[i + 1].Text))
                    { context.InvalidOwners.Add(tokens[i + 1].Text); context.Analysis.AddIssue(source, tokens[i + 1].Text, "manager-binding-not-evaluated"); }
                    int closing = plan.SkipBlock(i, end);
                    if (closing < 0) { context.Analysis.AddIssue(source, t, "unbalanced-block"); return false; }
                    if (!Step(context, source, Math.Max(0, closing - i))) return false;
                    i = closing; afterPossibleReturn = true; conditions.Allowed = false; conditions.Bindings.Clear(); continue;
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
                        afterPossibleReturn = true; conditions.Allowed = false;
                        if (plan.TryCall(i, out _, out _, out _, out int qualifiedClose) && qualifiedClose >= i)
                            i = qualifiedClose;
                        continue;
                    }
                    if (IsManagerAssignment(tokens, i, end))
                    { context.InvalidOwners.Add(t); context.Analysis.AddIssue(source, t, "manager-binding-not-evaluated"); afterPossibleReturn = true; conditions.Allowed = false; }
                    if (i + 1 < end && tokens[i + 1].Text == "[")
                    { context.Analysis.AddIssue(source, t, "dynamic-call"); int closing = plan.MatchClose(i + 1, end); if (closing >= 0) i = closing; afterPossibleReturn = true; conditions.Allowed = false; continue; }
                }
                if (plan.TrySugarCall(i, out string sugarOwner, out string sugarName, out int sugarArgument, out int sugarClose))
                {
                    if (sugarOwner == "" && sugarName == "choose")
                    {
                        context.Analysis.StoppedAtChoice = true; stopped = true;
                        if (conditional || afterPossibleReturn || conditionalExpression) context.Analysis.AddIssue(source, "choose", "conditional-choice-boundary");
                        break;
                    }
                    conditions.Allowed = false; // Sugar/display calls are not audited snapshot boundaries.
                    if (context.InvalidOwners.Contains(sugarOwner))
                    { context.Analysis.AddIssue(source, sugarOwner, "manager-binding-not-evaluated"); afterPossibleReturn = true; conditions.Allowed = false; }
                    if (!IsKnownPure(sugarOwner, sugarName))
                    { context.Analysis.AddIssue(source, sugarOwner.Length == 0 ? sugarName : sugarOwner + "." + sugarName, "unsupported-call-syntax"); afterPossibleReturn = true; conditions.Allowed = false; }
                    if (sugarClose < 0 || sugarClose >= end) { context.Analysis.AddIssue(source, sugarName, "unbalanced-call"); return false; }
                    if (!tokens[sugarArgument].IsString)
                        for (int nested = sugarArgument + 1; nested < sugarClose; nested++)
                        {
                            if (!Step(context, source)) return false;
                            if (plan.TryCall(nested, out string nestedOwner, out string nestedName, out int nestedOpen, out int nestedClose) || plan.TrySugarCall(nested, out nestedOwner, out nestedName, out nestedOpen, out nestedClose))
                            {
                                if (nestedOwner == "" && nestedName == "choose")
                                { context.Analysis.StoppedAtChoice = true; context.Analysis.AddIssue(source, "choose", "nested-choice-boundary"); stopped = true; return false; }
                                if (!IsKnownPure(nestedOwner, nestedName)) { context.Analysis.AddIssue(source, nestedName, "nested-invocation-not-evaluated"); afterPossibleReturn = true; conditions.Allowed = false; }
                            }
                        }
                    i = sugarClose; continue;
                }
                if (!plan.TryCall(i, out string owner, out string name, out int open, out int close)) continue;
                if (close < 0 || close >= end) { context.Analysis.AddIssue(source, name, "unbalanced-call"); return false; }
                var args = plan.Arguments(open, close);
                bool nestedUnknown = false;
                for (int nested = open + 1; nested < close; nested++)
                {
                    if (!Step(context, source)) return false;
                    if (plan.TryCall(nested, out string nestedOwner, out string nestedName, out int nestedOpen, out int nestedClose) || plan.TrySugarCall(nested, out nestedOwner, out nestedName, out nestedOpen, out nestedClose))
                    {
                        if (nestedOwner == "" && nestedName == "choose")
                        { context.Analysis.StoppedAtChoice = true; context.Analysis.AddIssue(source, "choose", "nested-choice-boundary"); stopped = true; return false; }
                        if (!IsKnownPure(nestedOwner, nestedName)) { context.Analysis.AddIssue(source, nestedName, "nested-invocation-not-evaluated"); nestedUnknown = true; }
                    }
                }
                if (nestedUnknown) { afterPossibleReturn = true; conditions.Allowed = false; }
                if (owner == "" && name == "choose")
                {
                    context.Analysis.StoppedAtChoice = true; stopped = true;
                    if (conditional || afterPossibleReturn || conditionalExpression) context.Analysis.AddIssue(source, "choose", "conditional-choice-boundary");
                    break;
                }
                if (context.InvalidOwners.Contains(owner))
                { context.Analysis.AddIssue(source, owner, "manager-binding-not-evaluated"); afterPossibleReturn = true; conditions.Allowed = false; i = close; continue; }
                if (!IsSnapshotCheckpoint(owner, name, args)) conditions.Allowed = false;
                bool knownPure = IsKnownPure(owner, name);
                if (owner == "statmodifymanager" && !knownPure || owner == "luamanager" && !knownPure)
                {
                    var effect = new PreviewEffect { Owner = owner, Name = name, Arguments = args, Conditional = conditional || afterPossibleReturn || conditionalExpression, Followup = followup, ExecutionUncertain = inheritedExecutionUncertainty || afterPossibleReturn || conditionalExpression, Condition = conditional || afterPossibleReturn || conditionalExpression ? "분기 조건 또는 후속 실행 여부에 따름" : "", Source = source, Sequence = context.Analysis.Effects.Count };
                    plan.ValidateEffect(effect, open, close);
                    effect.Branches.AddRange(branchPath);
                    conditions.Allowed = false;
                    context.Analysis.Effects.Add(effect);
                    if (!PreviewDisplay.IsKnownEffect(owner, name)) afterPossibleReturn = true;
                    if (owner == "luamanager" && PreviewDisplay.IsStoryRegistration(name))
                    {
                        if (!afterPossibleReturn && !conditionalExpression) targets.RemoveAll(x => !x.Exited);
                        if (args.Count != 1 || !PreviewSyntax.TryString(args[0], out string next) || next.Length > 0 && !IsLocalScriptName(next)) context.Analysis.AddIssue(source, name, "dynamic-or-invalid-script-target");
                        else if (next.Length > 0)
                        {
                            if (!targets.Exists(x => string.Equals(x.Name, next, StringComparison.OrdinalIgnoreCase))) targets.Add(new Target { Name = next, Function = name, Conditional = conditional || afterPossibleReturn || conditionalExpression });
                        }
                    }
                    if (targets.Count > 0 && (owner == "statmodifymanager" && name == "Mission" || owner == "luamanager" && name == "ChangeScene" && args.Count > 0 && PreviewSyntax.TryString(args[0], out string scene) && (scene == "Story" || scene == "Free")))
                        context.Analysis.AddIssue(source, name, "mission-or-scene-may-replace-followup");
                }
                else if (!knownPure)
                {
                    context.Analysis.AddIssue(source, owner.Length == 0 ? name : owner + "." + name, "unsupported-call");
                    afterPossibleReturn = true;
                    if (!IsSnapshotCheckpoint(owner, name, args)) conditions.Allowed = false;
                }
                i = close;
            }
            return false;
        }

        private static void TrackConditionBinding(string script, List<PreviewToken> tokens, int at, int end, ConditionScope scope)
        {
            if (!tokens[at].IsIdentifier) return;
            string name = tokens[at].Text;
            bool local = at > 0 && tokens[at - 1].Text == "local";
            bool qualified = at > 0 && (tokens[at - 1].Text == "." || tokens[at - 1].Text == ":");
            bool assignment = at + 1 < end && tokens[at + 1].Text == "=";
            if (qualified)
            {
                if (assignment) { scope.Allowed = false; scope.Bindings.Clear(); }
                return;
            }
            if (assignment && at > 0 && tokens[at - 1].Text == ",")
            { scope.Bindings.Clear(); scope.Allowed = false; return; }
            // Index writes may override a manager method or mutate an unknown
            // object. Reads are conservatively gated too rather than invoking Lua.
            if (at + 1 < end && tokens[at + 1].Text == "[")
            { scope.Bindings.Clear(); scope.Allowed = false; return; }
            if (!local && !assignment) return;
            scope.Bindings.Remove(name); scope.Assigned.Add(name);
            if (name == "checkpointmanager") { scope.Allowed = false; scope.Bindings.Clear(); return; }
            if (!assignment) return;
            // Only a complete, small single assignment may establish a binding.
            // Complex/multiple assignments cannot inherit a previous proof.
            int first = at + 2, last = first, depth = 0;
            for (; last < end && last - first < 128; last++)
            {
                string t = tokens[last].Text;
                if (depth == 0 && (t == ";" || t == "then" || t == "end" || t == "else" || t == "elseif" || t == "," ||
                    last > first && script.IndexOf('\n', tokens[last - 1].End, tokens[last].Start - tokens[last - 1].End) >= 0)) break;
                if (!tokens[last].IsString)
                { if (t == "(" || t == "[" || t == "{") depth++; else if (t == ")" || t == "]" || t == "}") depth--; }
                if (depth < 0) return;
            }
            if (last - first >= 128 || depth != 0 || last == first)
            { scope.Allowed = false; return; }
            if (last < end && IsExpressionContinuation(tokens[last].Text))
            { scope.Allowed = false; return; }
            PreviewCondition descriptor = PreviewConditions.ParseTokens(tokens, first, last, scope.Bindings);
            if (descriptor.IsKnownSyntax) scope.Bindings[name] = descriptor;
            else scope.Allowed = false; // Unknown indexing/arithmetic may run Lua metamethods.
        }

        private static bool IsExpressionContinuation(string token)
        {
            return token == "+" || token == "-" || token == "*" || token == "/" || token == "%" || token == "^" ||
                token == ".." || token == "==" || token == "~=" || token == "<" || token == "<=" || token == ">" || token == ">=" ||
                token == "and" || token == "or" || token == "." || token == ":" || token == "[" || token == "(" || token == "{";
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

        // Extraction purity means 'no displayed reward', not runtime purity:
        // UI/display calls may invoke callbacks or write story state.
        private static bool IsSnapshotCheckpoint(string owner, string name, List<string> arguments)
        {
            return owner == "checkpointmanager" && (name == "Switch" || name == "Condition") &&
                arguments.Count == 1 && PreviewSyntax.TryString(arguments[0], out string key) && key.Length > 0;
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
