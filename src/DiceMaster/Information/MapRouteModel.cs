using System;
using System.Collections.Generic;
using DiceMaster.LiveLog;

namespace DiceMaster.Information
{
    public enum MapRouteState { Unknown, Met, NotMet }
    public enum MapStoryKind { Unclassified, Story, ActivityResponse }

    // Owned by one scheduled map read. Shared tails/pools often have exactly
    // the same gate, so neither its predicate nor checkpoint graph is read
    // again for each terminal. Never keep this object across refresh ticks.
    public sealed class MapRouteRead
    {
        private PreviewConditions.Reader reader;
        private readonly Dictionary<PreviewCondition, PreviewConditionState> values = new Dictionary<PreviewCondition, PreviewConditionState>();
        private readonly Dictionary<Func<bool>, bool> completeness = new Dictionary<Func<bool>, bool>();
        private readonly Dictionary<PreviewCondition, Dictionary<string, bool>> activityInputs = new Dictionary<PreviewCondition, Dictionary<string, bool>>();
        internal bool ActivityInputsDisjoint(PreviewCondition gate, string key, IList<string> flags, IDictionary<string, int> unchangedSets, bool afterTalkReset, ref int budget)
        {
            if (flags.Count == 0) return true;
            if (gate == null) return false;
            Dictionary<string, bool> effects;
            if (!activityInputs.TryGetValue(gate, out effects)) activityInputs.Add(gate, effects = new Dictionary<string, bool>(StringComparer.Ordinal));
            bool value;
            if (!effects.TryGetValue(key, out value))
            {
                if (budget-- <= 0) return false;
                if (reader == null) reader = PreviewConditions.CreateReader();
                value = reader.AreFlagWritesDisjoint(gate, flags, unchangedSets, afterTalkReset); effects.Add(key, value);
            }
            return value;
        }
        internal bool Incomplete(Func<bool> check)
        {
            if (check == null) return false;
            bool value; if (!completeness.TryGetValue(check, out value)) { value = check(); completeness.Add(check, value); }
            return value;
        }
        internal bool BudgetedCurrent(PreviewCondition gate, ref int budget, out PreviewConditionState state)
        {
            if (values.TryGetValue(gate, out state)) return true;
            if (budget-- <= 0) { state = PreviewConditionState.Unknown; return false; }
            state = Current(gate); return true;
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
        private readonly PreviewCondition snapshotGate;
        private readonly bool rootEntry, topicEntry;
        private readonly PreviewCondition topicGate;
        internal MapChoiceStep(string script, int offset, int index, string label,
            PreviewCondition gate, PreviewCondition optionGate, bool uncertain, PreviewCondition currentSnapshotGate = null, bool completeRootEntry = false, PreviewCondition currentTopicGate = null, bool completeTopicEntry = false)
        { Script = script; SourceOffset = offset; Index = index; RawLabel = label; Gate = gate; OptionGate = optionGate; Uncertain = uncertain; snapshotGate = currentSnapshotGate; rootEntry = completeRootEntry; topicGate = currentTopicGate; topicEntry = completeTopicEntry; }
        // Negative-only current prerequisite projection. Never grants Met.
        internal bool HasCurrentRootSnapshot { get { return rootEntry; } }
        public bool ReadCurrentBlocker(MapRouteRead read)
        { if (read == null) throw new ArgumentNullException("read"); return rootEntry && snapshotGate != null && read.Current(snapshotGate) == PreviewConditionState.NotMet; }
        public bool ReadTopicBlocker(MapRouteRead read)
        { if (read == null) throw new ArgumentNullException("read"); return topicEntry && topicGate != null && read.Current(topicGate) == PreviewConditionState.NotMet; }
        internal MapRouteState ReadAvailability(MapRouteRead read, ref int budget)
        {
            // A future menu must have a closed local entry and a readable,
            // effect-aware guard. Unknown is never evidence of an unlocked option.
            PreviewConditionState state;
            return !topicEntry || topicGate == null || !read.BudgetedCurrent(topicGate, ref budget, out state)
                ? MapRouteState.Unknown : MapRoutePath.State(state);
        }
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
        public readonly string Identity, Description, DialogueCharacter;
        public readonly bool HasChoice, Uncertain;
        public readonly bool StoryConditionsMayHaveChanged;
        public readonly MapStoryKind StoryKind;
        public MapChoiceStep TopicStep { get { return Steps.Count == 0 ? null : Steps[Steps.Count - 1]; } }
        internal readonly PreviewCondition Gate;
        public readonly PreviewCondition EntryGate;
        public readonly bool EntryUncertain;
        public readonly IList<MapChoiceStep> Steps;
        private readonly Func<bool> storyIncomplete;
        private readonly PreviewCondition storyGate, snapshotGate;
        private readonly bool opaqueStoryEffects;
        private readonly PreviewCondition activityGate;
        private readonly IList<string> activityWrites;
        private readonly string activityKey;
        private readonly IDictionary<string, int> unchangedSets;
        private readonly bool afterTalkReset;
        internal MapRoutePath(string identity, string description, bool choice, bool uncertain, PreviewCondition gate,
            IList<MapChoiceStep> steps = null, PreviewCondition entryGate = null, bool entryUncertain = false, string dialogueCharacter = null,
            Func<bool> incompleteStoryPaths = null, PreviewCondition fullStoryGate = null, bool storyConditionsMayHaveChanged = false,
            bool hasOpaqueStoryEffects = false, PreviewCondition currentSnapshotGate = null, MapStoryKind storyKind = MapStoryKind.Story, PreviewCondition activityPrerequisite = null, IEnumerable<string> precedingFlagWrites = null, IDictionary<string, int> sameValueSetCandidates = null, bool precededByTalkReset = false)
        {
            Identity = identity; Description = description; HasChoice = choice; Uncertain = uncertain; Gate = gate;
            Steps = new List<MapChoiceStep>(steps ?? new MapChoiceStep[0]).AsReadOnly();
            EntryGate = entryGate ?? gate; EntryUncertain = entryUncertain;
            DialogueCharacter = dialogueCharacter ?? "";
            storyIncomplete = incompleteStoryPaths;
            storyGate = fullStoryGate ?? gate;
            StoryConditionsMayHaveChanged = storyConditionsMayHaveChanged;
            opaqueStoryEffects = hasOpaqueStoryEffects; snapshotGate = currentSnapshotGate; StoryKind = storyKind;
            activityGate = activityPrerequisite; afterTalkReset = precededByTalkReset;
            unchangedSets = new Dictionary<string, int>(sameValueSetCandidates ?? new Dictionary<string, int>(), StringComparer.Ordinal);
            var writes = new List<string>(precedingFlagWrites ?? new string[0]); writes.Sort(StringComparer.Ordinal); activityWrites = writes.AsReadOnly();
            var key = new System.Text.StringBuilder(); key.Append(afterTalkReset ? "T" : "F"); foreach (string flag in writes) { key.Append(flag.Length).Append(':').Append(flag); int target; key.Append(unchangedSets.TryGetValue(flag, out target) ? target.ToString(System.Globalization.CultureInfo.InvariantCulture) : "?").Append(';'); } activityKey = key.ToString();
        }
        internal bool SameActivityInputs(MapRoutePath other)
        { return other != null && StoryKind == other.StoryKind && ReferenceEquals(activityGate, other.activityGate) && activityKey == other.activityKey; }
        // The structural profile is definition metadata. Consumed parent
        // predicates are checked against the current serialized graph once
        // per scheduled read, under the same caller-owned gate budget.
        public MapStoryKind ReadStoryKind(MapRouteRead read, ref int budget)
        {
            if (read == null) throw new ArgumentNullException("read");
            return StoryKind == MapStoryKind.ActivityResponse && !read.ActivityInputsDisjoint(activityGate, activityKey, activityWrites, unchangedSets, afterTalkReset, ref budget)
                ? MapStoryKind.Unclassified : StoryKind;
        }
        // Intersection of necessary AND-prefixes, computed only while building
        // the bounded definition index. It compresses many known alternatives
        // without discarding an observed bypass; no sampled values are cached.
        public PreviewCondition CurrentPrerequisite { get { return snapshotGate; } }
        public static PreviewCondition CommonPrerequisite(PreviewCondition left, PreviewCondition right)
        {
            if (left == null || right == null) return PreviewConditions.Unknown;
            var ancestors = new HashSet<PreviewCondition>();
            for (int i = 0; right != null && i < 65; i++)
            { ancestors.Add(right); right = right.Operation == "and" ? right.Left : null; }
            for (int i = 0; left != null && i < 65; i++)
            { if (ancestors.Contains(left)) return left; left = left.Operation == "and" ? left.Left : null; }
            return PreviewConditions.Unknown;
        }
        public bool ReadCurrentBlocker(MapRouteRead read)
        { if (read == null) throw new ArgumentNullException("read"); return snapshotGate != null && read.Current(snapshotGate) == PreviewConditionState.NotMet; }
        public MapRouteState ReadChoiceAvailability(MapRouteRead read, ref int budget, int afterStep = -1)
        {
            if (read == null) throw new ArgumentNullException("read");
            if (afterStep < -1 || afterStep >= Steps.Count || Steps.Count > MapRouteLimits.MaxChoiceSteps) return MapRouteState.Unknown;
            bool unknown = false;
            for (int at = afterStep + 1; at < Steps.Count; at++)
            {
                MapRouteState state = Steps[at].ReadAvailability(read, ref budget);
                if (state == MapRouteState.NotMet) return state;
                unknown |= state == MapRouteState.Unknown;
            }
            return unknown ? MapRouteState.Unknown : MapRouteState.Met;
        }
        internal static MapRouteState State(PreviewConditionState value)
        { return value == PreviewConditionState.Met ? MapRouteState.Met : value == PreviewConditionState.NotMet ? MapRouteState.NotMet : MapRouteState.Unknown; }
        public MapRouteState ReadEntry(MapRouteRead read)
        { if (read == null) throw new ArgumentNullException("read"); return EntryUncertain ? MapRouteState.Unknown : State(read.Current(EntryGate)); }
        // Deliberately separate current guard values from route certainty.
        public MapRouteState ReadEntryConditions(MapRouteRead read)
        { if (read == null) throw new ArgumentNullException("read"); return State(read.Current(EntryGate)); }
        // The whole observed story path, with checkpoint inputs affected by
        // preceding writes/callbacks masked. This is a current-condition
        // snapshot, never an execution or future-story guarantee.
        public MapRouteState ReadStoryConditions(MapRouteRead read)
        {
            if (read == null) throw new ArgumentNullException("read");
            return opaqueStoryEffects || read.Incomplete(storyIncomplete) ? MapRouteState.Unknown : State(read.Current(storyGate));
        }
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
        // Each path stores its first qualifying NPC witness, not all dialogue
        // participants. A representative is valid only when retained paths
        // agree; consumers that group alternatives must use path metadata.
        public string DialogueCharacter
        {
            get
            {
                string value = null;
                foreach (var path in paths)
                { if (value == null) value = path.DialogueCharacter; else if (value != path.DialogueCharacter) return ""; }
                return value ?? "";
            }
        }
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
        public MapRouteState ReadStoryConditions(MapRouteRead read)
        {
            if (read == null) throw new ArgumentNullException("read");
            bool unknown = omittedPaths;
            foreach (var path in paths)
            {
                MapRouteState state = path.ReadStoryConditions(read);
                if (state == MapRouteState.Met) return state;
                unknown |= state == MapRouteState.Unknown;
            }
            return unknown || paths.Count == 0 ? MapRouteState.Unknown : MapRouteState.NotMet;
        }
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
    // A literal script registration, not proof that a mission was registered.
    // Consumers must separately intersect this path with live waiting data.
    public sealed class MapScriptEntry
    {
        public readonly string Script;
        private readonly List<MapRoutePath> paths = new List<MapRoutePath>();
        public IList<MapRoutePath> Paths { get { return paths.AsReadOnly(); } }
        internal MapScriptEntry(string script) { Script = script; }
        internal bool Add(MapRoutePath path)
        {
            foreach (var old in paths) if (old.Identity == path.Identity) return true;
            if (paths.Count >= 32) return false;
            paths.Add(path); return true;
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
                bool unknown = omittedPaths || read.Incomplete(incomplete); int choiceBudget = 32;
                foreach (var path in paths)
                {
                    if (path.Uncertain) { unknown = true; continue; }
                    var state = read.Current(path.Gate);
                    if (state == PreviewConditionState.Met)
                    {
                        MapRouteState choices = path.ReadChoiceAvailability(read, ref choiceBudget);
                        if (choices == MapRouteState.Met) return choices;
                        unknown |= choices == MapRouteState.Unknown;
                    }
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
