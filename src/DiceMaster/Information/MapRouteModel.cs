using System;
using System.Collections.Generic;
using DiceMaster.LiveLog;

namespace DiceMaster.Information
{
    public enum MapRouteState { Unknown, Met, NotMet }

    // Definition DTOs retain no sampled game values or execution authority.
    public sealed class MapRouteEvent
    {
        public readonly object Target;
        public readonly string ResultScript, Id;
        public MapRouteEvent(object target, string resultScript, string id = null)
        { Target = target; ResultScript = resultScript ?? ""; Id = id ?? ""; }
    }
    public sealed class MapRoutePath
    {
        public readonly string Identity, Description;
        public readonly bool HasChoice, Uncertain;
        internal readonly PreviewCondition Gate;
        internal MapRoutePath(string identity, string description, bool choice, bool uncertain, PreviewCondition gate)
        { Identity = identity; Description = description; HasChoice = choice; Uncertain = uncertain; Gate = gate; }
    }
    public sealed class MapRouteEntry
    {
        public readonly object Event;
        public readonly string ResultScript, Pool, Id;
        private readonly List<MapRoutePath> paths = new List<MapRoutePath>();
        private readonly Func<bool> incomplete;
        private bool omittedPaths;
        public IList<MapRoutePath> Paths { get { return paths.AsReadOnly(); } }
        public string Path
        { get { var text = new List<string>(); foreach (var p in paths) if (!text.Contains(p.Description)) text.Add(p.Description); return string.Join("\n", text); } }
        public bool HasChoice { get { foreach (var p in paths) if (p.HasChoice) return true; return false; } }
        public MapRouteState Current
        {
            get
            {
                // One fresh bounded checkpoint snapshot per published entry.
                // The reader never calls the game's Switch/Condition chain.
                var reader = PreviewConditions.CreateReader(); bool unknown = omittedPaths || incomplete != null && incomplete();
                foreach (var path in paths)
                {
                    if (path.Uncertain) { unknown = true; continue; }
                    var state = reader.Current(path.Gate);
                    if (state == PreviewConditionState.Met) return MapRouteState.Met;
                    unknown |= state == PreviewConditionState.Unknown;
                }
                return unknown || paths.Count == 0 ? MapRouteState.Unknown : MapRouteState.NotMet;
            }
        }
        internal MapRouteEntry(MapRouteEvent item, string pool, Func<bool> hasIncompletePaths = null)
        { Event = item.Target; ResultScript = item.ResultScript; Pool = pool; Id = item.Id.Length == 0 ? pool + ":" + item.ResultScript : item.Id; incomplete = hasIncompletePaths; }
        internal bool Add(MapRoutePath path)
        {
            foreach (var p in paths) if (p.Identity == path.Identity) return true;
            if (paths.Count < 32) { paths.Add(path); return true; }
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
        public int MaxDepth = 8, MaxScripts = 32, MaxContexts = 128, MaxEntries = 256, MaxChoices = 1024;
        public int MaxCharacters = 1310720, MaxTokens = 262144, MaxWork = 524288;
        public int MaxScriptCharacters = 524288, MaxScriptTokens = 131072;
    }
}
