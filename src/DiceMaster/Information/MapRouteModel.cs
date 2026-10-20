using System;
using System.Collections.Generic;
using DiceMaster.LiveLog;

namespace DiceMaster.Information
{
    public enum MapRouteState { Unknown, Met, NotMet }

    // Owned by one scheduled map read. Shared tails/pools often have exactly
    // the same gate, so neither its predicate nor checkpoint graph is read
    // again for each terminal. Never keep this object across refresh ticks.
    public sealed class MapRouteRead
    {
        private PreviewConditions.Reader reader;
        private readonly Dictionary<PreviewCondition, PreviewConditionState> values = new Dictionary<PreviewCondition, PreviewConditionState>();
        private readonly Dictionary<Func<bool>, bool> completeness = new Dictionary<Func<bool>, bool>();
        internal bool Incomplete(Func<bool> check)
        {
            if (check == null) return false;
            bool value; if (!completeness.TryGetValue(check, out value)) { value = check(); completeness.Add(check, value); }
            return value;
        }
        internal PreviewConditionState Current(PreviewCondition gate)
        {
            PreviewConditionState state;
            if (!values.TryGetValue(gate, out state))
            {
                if (reader == null) reader = PreviewConditions.CreateReader();
                state = reader.Current(gate); values.Add(gate, state);
            }
            return state;
        }
    }

    // Definition DTOs retain no sampled game values or execution authority.
    public sealed class MapRouteEvent
    {
        public readonly object Target;
        public readonly string ResultScript, Id;
        public MapRouteEvent(object target, string resultScript, string id = null)
        { Target = target; ResultScript = resultScript ?? ""; Id = id ?? ""; }
    }
    public sealed class MapChoiceStep
    {
        public readonly string Script, RawLabel;
        public readonly int SourceOffset, Index;
        public readonly bool Uncertain;
        public readonly PreviewCondition Gate, OptionGate;
        internal MapChoiceStep(string script, int offset, int index, string label,
            PreviewCondition gate, PreviewCondition optionGate, bool uncertain)
        { Script = script; SourceOffset = offset; Index = index; RawLabel = label; Gate = gate; OptionGate = optionGate; Uncertain = uncertain; }
        public MapRouteState Current { get { return ReadCurrent(new MapRouteRead()); } }
        public MapRouteState ReadCurrent(MapRouteRead read)
        { if (read == null) throw new ArgumentNullException("read"); return Uncertain ? MapRouteState.Unknown : MapRoutePath.State(read.Current(Gate)); }
        // A condition snapshot, never a promise about state after entering a
        // script. In an already displayed menu this is the original option's
        // own guard, without speculative effects from any earlier selection.
        public MapRouteState ReadConditions(MapRouteRead read)
        { if (read == null) throw new ArgumentNullException("read"); return MapRoutePath.State(read.Current(OptionGate)); }
    }
    public sealed class MapRoutePath
    {
        public readonly string Identity, Description;
        public readonly bool HasChoice, Uncertain;
        internal readonly PreviewCondition Gate;
        public readonly PreviewCondition EntryGate;
        public readonly bool EntryUncertain;
        public readonly IList<MapChoiceStep> Steps;
        internal MapRoutePath(string identity, string description, bool choice, bool uncertain, PreviewCondition gate,
            IList<MapChoiceStep> steps = null, PreviewCondition entryGate = null, bool entryUncertain = false)
        {
            Identity = identity; Description = description; HasChoice = choice; Uncertain = uncertain; Gate = gate;
            Steps = new List<MapChoiceStep>(steps ?? new MapChoiceStep[0]).AsReadOnly();
            EntryGate = entryGate ?? gate; EntryUncertain = entryUncertain;
        }
        internal static MapRouteState State(PreviewConditionState value)
        { return value == PreviewConditionState.Met ? MapRouteState.Met : value == PreviewConditionState.NotMet ? MapRouteState.NotMet : MapRouteState.Unknown; }
        public MapRouteState ReadEntry(MapRouteRead read)
        { if (read == null) throw new ArgumentNullException("read"); return EntryUncertain ? MapRouteState.Unknown : State(read.Current(EntryGate)); }
        // Deliberately separate current guard values from route certainty.
        public MapRouteState ReadEntryConditions(MapRouteRead read)
        { if (read == null) throw new ArgumentNullException("read"); return State(read.Current(EntryGate)); }
    }
    // A selected, statically observed dialogue segment. It has no fabricated
    // PositionEventData, activation condition, event weight or one-shot proof.
    public sealed class MapStoryEntry
    {
        public readonly string Identity, Script, StoryKey, TitleRawLabel;
        public readonly int SourceOffset;
        private readonly List<MapRoutePath> paths = new List<MapRoutePath>();
        private readonly Func<bool> incomplete;
        private bool omittedPaths;
        private string pathText;
        public IList<MapRoutePath> Paths { get { return paths.AsReadOnly(); } }
        public string Path
        { get { if (pathText == null) { var text = new List<string>(); foreach (var p in paths) if (!text.Contains(p.Description)) text.Add(p.Description); pathText = string.Join("\n", text); } return pathText; } }
        public MapRouteState Current { get { return ReadCurrent(new MapRouteRead()); } }
        internal MapStoryEntry(string identity, string script, int offset, string storyKey, string label, Func<bool> hasIncompletePaths)
        { Identity = identity; Script = script; SourceOffset = offset; StoryKey = storyKey; TitleRawLabel = label; incomplete = hasIncompletePaths; }
        internal bool HasPath(string identity) { foreach (var path in paths) if (path.Identity == identity) return true; return false; }
        internal bool Add(MapRoutePath path)
        {
            foreach (var p in paths) if (p.Identity == path.Identity) return true;
            if (paths.Count < 32) { paths.Add(path); pathText = null; return true; }
            omittedPaths = true; return false;
        }
        public MapRouteState ReadCurrent(MapRouteRead read) { return Read(read, 0); }
        public MapRouteState ReadEntry(MapRouteRead read) { return Read(read, 1); }
        public MapRouteState ReadEntryConditions(MapRouteRead read) { return Read(read, 2); }
        private MapRouteState Read(MapRouteRead read, int mode)
        {
            if (read == null) throw new ArgumentNullException("read");
            bool unknown = omittedPaths || read.Incomplete(incomplete);
            foreach (var path in paths)
            {
                MapRouteState value = mode == 2 ? path.ReadEntryConditions(read) : mode == 1 ? path.ReadEntry(read) :
                    path.Uncertain ? MapRouteState.Unknown : MapRoutePath.State(read.Current(path.Gate));
                if (value == MapRouteState.Met) return value;
                unknown |= value == MapRouteState.Unknown;
            }
            return unknown || paths.Count == 0 ? MapRouteState.Unknown : MapRouteState.NotMet;
        }
    }
    public sealed class MapRouteEntry
    {
        public readonly object Event;
        public readonly string ResultScript, Pool, Id;
        private readonly List<MapRoutePath> paths = new List<MapRoutePath>();
        private readonly Func<bool> incomplete;
        private bool omittedPaths;
        private string pathText;
        public IList<MapRoutePath> Paths { get { return paths.AsReadOnly(); } }
        public string Path
        { get { if (pathText == null) { var text = new List<string>(); foreach (var p in paths) if (!text.Contains(p.Description)) text.Add(p.Description); pathText = string.Join("\n", text); } return pathText; } }
        public bool HasChoice { get { foreach (var p in paths) if (p.HasChoice) return true; return false; } }
        public MapRouteState Current
        {
            get { return ReadCurrent(new MapRouteRead()); }
        }
        public MapRouteState ReadCurrent(MapRouteRead read)
        {
                if (read == null) throw new ArgumentNullException("read");
                bool unknown = omittedPaths || read.Incomplete(incomplete);
                foreach (var path in paths)
                {
                    if (path.Uncertain) { unknown = true; continue; }
                    var state = read.Current(path.Gate);
                    if (state == PreviewConditionState.Met) return MapRouteState.Met;
                    unknown |= state == PreviewConditionState.Unknown;
                }
                return unknown || paths.Count == 0 ? MapRouteState.Unknown : MapRouteState.NotMet;
        }
        internal MapRouteEntry(MapRouteEvent item, string pool, Func<bool> hasIncompletePaths = null)
        { Event = item.Target; ResultScript = item.ResultScript; Pool = pool; Id = item.Id.Length == 0 ? pool + ":" + item.ResultScript : item.Id; incomplete = hasIncompletePaths; }
        internal bool Add(MapRoutePath path)
        {
            foreach (var p in paths) if (p.Identity == path.Identity) return true;
            if (paths.Count < 32) { paths.Add(path); pathText = null; return true; }
            omittedPaths = true; return false;
        }
    }
    public sealed class MapRouteLimits
    {
        // A semantic operation is one bounded instruction, not a CPU-time
        // measurement. These independent caps bound its scans/copies even
        // when the caller gives Advance a one-unit instruction budget.
        public const int MaxBindings = 48, MaxFrames = 12, MaxEnvironmentCells = 128;
        public const int MaxBranchArms = 16, MaxChoiceArms = 16, MaxHeaderTokens = 128, MaxForkCells = 1024;
        public const int MaxTableCells = 64;
        public const int MaxChoiceSteps = 16;
        public int MaxDepth = 8, MaxScripts = 32, MaxContexts = 128, MaxEntries = 256, MaxChoices = 1024;
        public int MaxCharacters = 1310720, MaxTokens = 262144, MaxWork = 524288;
        public int MaxScriptCharacters = 524288, MaxScriptTokens = 131072;
    }
}
