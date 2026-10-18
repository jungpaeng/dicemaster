using System;
using System.Collections.Generic;

namespace DiceMaster.LiveLog
{
    public enum PreviewAnalysisState { Supported, Partial, Unsupported, NoEffects, Waiting, ReadFailure }

    // Syntactic alternatives only. These labels never imply a condition verdict.
    // Groups are unique within an analysis, including separately followed scripts.
    public sealed class PreviewBranchContext
    {
        public int Group;
        public int Alternative;
        public bool IsElse;
    }

    public sealed class PreviewEffect
    {
        public string Owner = "";
        public string Name = "";
        public List<string> Arguments = new List<string>();
        public bool Conditional;
        public bool Followup;
        public bool ExecutionUncertain;
        public List<PreviewBranchContext> Branches = new List<PreviewBranchContext>();
        public string Condition = "";
        public string Source = "";
        public int Sequence;
        public bool Validated;
        public string ValidationError;
        public List<object> Constants;
    }

    public sealed class PreviewIssue
    {
        public string Function = "";
        public string Reason = "";
        public string Source = "";
        public bool IsReadFailure;
        public bool IsWaiting;
    }

    public sealed class PreviewAnalysis
    {
        public bool SnapshotAllowed = true;
        public List<PreviewEffect> Effects = new List<PreviewEffect>();
        public List<PreviewIssue> Issues = new List<PreviewIssue>();
        public bool StoppedAtChoice;
        public int ScriptCount;
        public int ContextCount;
        public int CharacterCount;
        public int TokenCount;
        public int WorkCount;
        public bool HasReadFailures
        {
            get { foreach (PreviewIssue issue in Issues) if (issue.IsReadFailure) return true; return false; }
        }
        public PreviewAnalysisState State
        {
            get
            {
                PreviewDisplay.Validate(this);
                bool unsupported = false, failed = false, waiting = false;
                foreach (PreviewIssue issue in Issues)
                {
                    if (issue.IsWaiting) waiting = true;
                    else if (issue.IsReadFailure) failed = true;
                    else unsupported = true;
                }
                if (waiting) return PreviewAnalysisState.Waiting;
                if (failed) return PreviewAnalysisState.ReadFailure;
                int known = 0;
                foreach (PreviewEffect effect in Effects)
                    if (!PreviewDisplay.IsDisplayOnly(effect) && !PreviewDisplay.IsRoutingOnly(effect) && effect.ValidationError == null) known++;
                if (unsupported) return known > 0 ? PreviewAnalysisState.Partial : PreviewAnalysisState.Unsupported;
                return known > 0 ? PreviewAnalysisState.Supported : PreviewAnalysisState.NoEffects;
            }
        }
        internal string RootName;
        internal string OptionKey;
        internal bool RootNeedsRead;
        internal bool RootConditional;
        internal string RootScript;
        internal Func<string, ScriptReadResult> Reader;
        internal PreviewTraceLimits Limits;
        internal PreviewParseCache ParseCache;

        // Option discovery removes surrounding headers and may leave common
        // statements between choose and the result branch outside this body.
        // Preserve that unproved execution context across read retries.
        public void MarkConditionalRoot()
        {
            RootConditional = true;
            foreach (PreviewEffect effect in Effects)
            { effect.Conditional = true; effect.ExecutionUncertain = true; }
            AddIssue(RootName, "options", "option-context-not-evaluated");
        }

        internal void ReplaceWith(PreviewAnalysis next)
        {
            Effects = next.Effects; Issues = next.Issues; StoppedAtChoice = next.StoppedAtChoice;
            ScriptCount = next.ScriptCount; ContextCount = next.ContextCount; CharacterCount = next.CharacterCount;
            TokenCount = next.TokenCount; WorkCount = next.WorkCount;
        }

        public void AddIssue(string source, string function, string reason, bool readFailure = false, bool waiting = false)
        {
            foreach (PreviewIssue issue in Issues)
                if (issue.Source == source && issue.Function == function && issue.Reason == reason && issue.IsReadFailure == readFailure) return;
            Issues.Add(new PreviewIssue { Source = source, Function = function, Reason = reason, IsReadFailure = readFailure, IsWaiting = waiting });
        }
    }

    public enum ScriptReadState { Success, Missing, Waiting, Failed }

    public sealed class ScriptReadResult
    {
        public ScriptReadState State;
        public string Text;
        private ScriptReadResult(ScriptReadState state, string text) { State = state; Text = text; }
        public static ScriptReadResult Success(string text) { return new ScriptReadResult(ScriptReadState.Success, text ?? ""); }
        public static ScriptReadResult Missing() { return new ScriptReadResult(ScriptReadState.Missing, null); }
        public static ScriptReadResult Waiting() { return new ScriptReadResult(ScriptReadState.Waiting, null); }
        public static ScriptReadResult Failed() { return new ScriptReadResult(ScriptReadState.Failed, null); }
    }

    // Finite offline policy measured against 2,198 mapped story bodies. The
    // FIFO/cache profile preserves the compared public and prior local calls.
    // These are logical safeguards, not a certified Unity frame-time budget;
    // runtime validation remains separate (see the real-Lua validation doc).
    public sealed class PreviewTraceLimits
    {
        public int MaxDepth = 8;
        public int MaxScripts = 32;
        public int MaxCharacters = 1310720;
        public int MaxTokens = 262144;
        public int MaxWork = 524288;
    }
}
