using System;
using System.Collections.Generic;

namespace DiceMaster.LiveLog
{
    public enum PreviewAnalysisState { Supported, Partial, Unsupported, NoEffects, Waiting, ReadFailure }

    public sealed class PreviewEffect
    {
        public string Owner = "";
        public string Name = "";
        public List<string> Arguments = new List<string>();
        public bool Conditional;
        public bool Followup;
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
                    if (!PreviewDisplay.IsDisplayOnly(effect) && effect.ValidationError == null) known++;
                if (unsupported) return known > 0 ? PreviewAnalysisState.Partial : PreviewAnalysisState.Unsupported;
                return known > 0 ? PreviewAnalysisState.Supported : PreviewAnalysisState.NoEffects;
            }
        }
        internal string RootName;
        internal string OptionKey;
        internal bool RootNeedsRead;
        internal string RootScript;
        internal Func<string, ScriptReadResult> Reader;
        internal PreviewTraceLimits Limits;

        internal void ReplaceWith(PreviewAnalysis next)
        {
            Effects = next.Effects; Issues = next.Issues; StoppedAtChoice = next.StoppedAtChoice;
            ScriptCount = next.ScriptCount; CharacterCount = next.CharacterCount;
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

    // Finite development safeguards, not final game-data-derived production limits.
    // Actual Lua resources are unavailable; changing this status requires real-resource measurements.
    public sealed class PreviewTraceLimits
    {
        public int MaxDepth = 8;
        public int MaxScripts = 32;
        public int MaxCharacters = 262144;
        public int MaxTokens = 65536;
        public int MaxWork = 131072;
    }
}
