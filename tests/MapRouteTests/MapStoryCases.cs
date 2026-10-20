using System;
using System.Collections.Generic;
using System.Linq;
using DiceMaster.Information;
using DiceMaster.LiveLog;

internal static class MapStoryCases
{
    private const string Say = "say(luamanager.GetStoryText('dialogue-key'))";
    private static MapRouteDefinitions Build(string root, Dictionary<string, string> scripts = null, MapRouteLimits limits = null)
    {
        scripts = scripts ?? new Dictionary<string, string>(); scripts["root"] = root;
        var definitions = new MapRouteDefinitions("root", key => scripts.ContainsKey(key) ? ScriptReadResult.Success(scripts[key]) : ScriptReadResult.Missing(),
            key => new MapRouteEvent[0], limits);
        for (int i = 0; i < 10000 && definitions.IsPreparing; i++) definitions.Advance(0, 128);
        return definitions;
    }
    internal static void Run(Action<bool, string> check)
    {
        string choice = "local options={'rest','brother'};local r=choose(options);";
        var scripts = new Dictionary<string, string>
        {
            { "menu", "local opts={};opts[1]='exit';opts[2]=checkpointmanager.Condition('gate') and 'talk+topic+0+0' or '~x';local r=choose(opts);if r==2 then statmodifymanager.SetFlag('changed',1);luamanager.SetNextScript('leaf') end" },
            { "leaf", Say + ";" + Say },
            { "utility", "local x=1;luamanager.SetNextScript('empty')" },
            { "empty", "" }
        };
        var d = Build(choice + "if r==2 then " + Say + ";luamanager.SetNextScript('menu') end", scripts);
        check(d.StoryEntries.Count == 2 && d.Entries.Count == 0, "selected inline and literal dialogue are separate from native pool events");
        var inline = d.StoryEntries.Single(x => x.Script == "root");
        var leaf = d.StoryEntries.Single(x => x.Script == "leaf");
        check(inline.Paths.Count == 1 && inline.Paths[0].Steps.Count == 1, "inline dialogue retains one canonical selected path");
        check(inline.TitleRawLabel == "brother" && inline.StoryKey == "dialogue-key", "story identity uses selected label and literal text key without reading dialogue");
        check(leaf.Paths.Count == 1 && leaf.Paths[0].Steps.Count == 2, "literal followup retains each structured selection step");
        var step = leaf.Paths[0].Steps[1];
        check(step.Script == "menu" && step.Index == 2 && step.RawLabel == "talk+topic+0+0" && step.SourceOffset > 0, "step retains script, choose offset, original Lua index and untouched source label");
        check(step.ReadConditions(new MapRouteRead()) == MapRouteState.Met, "own option guard current snapshot is available separately");
        check(leaf.Current == MapRouteState.Unknown && leaf.ReadEntry(new MapRouteRead()) == MapRouteState.Met, "later state writes do not invalidate the recorded first-entry snapshot or prove the later route");
        check(leaf.ReadEntryConditions(new MapRouteRead()) == MapRouteState.Met, "current first-entry conditions are separate from strict future certainty");
        check(inline.Paths[0].Steps.Count == 1, "fork and followup copies cannot append to earlier story steps");
        int reads = d.ScriptReadCount, parses = d.ParseCount, work = d.WorkCount;
        for (int i = 0; i < 100; i++) { d.Advance(i); leaf.ReadEntryConditions(new MapRouteRead()); }
        check(d.ScriptReadCount == reads && d.ParseCount == parses && d.WorkCount == work, "warm story and choice reads never load or parse Lua");

        d = Build("unknown_callback();local opts={};opts[1]=checkpointmanager.Condition('gate') and 'brother' or '~x';local r=choose(opts);" + Say);
        inline = d.StoryEntries.Single();
        check(inline.Current == MapRouteState.Unknown && inline.ReadEntry(new MapRouteRead()) == MapRouteState.Unknown, "pre-entry callback remains an execution-certainty barrier");
        check(inline.ReadEntryConditions(new MapRouteRead()) == MapRouteState.Met, "guard-only snapshot never pretends to execute a pre-entry callback");
        check(inline.Paths[0].EntryUncertain && inline.Paths[0].Steps[0].Uncertain, "entry and step uncertainty are explicit immutable metadata");

        d = Build("local opts={};opts[1]=checkpointmanager.Condition('gate') and 'brother' or '~x';local r=choose(opts);" + Say);
        inline = d.StoryEntries.Single();
        check(!d.IsPartial && inline.Current == MapRouteState.Met, "supported closed dialogue is structurally complete while the dialogue start remains eligible");
        var condition = Mortal.Story.CheckPointManager.Instance._condition._list.Single(x => x.name == "gate");
        condition.Current = false;
        check(inline.ReadEntry(new MapRouteRead()) == MapRouteState.NotMet && inline.ReadEntryConditions(new MapRouteRead()) == MapRouteState.NotMet && inline.Current == MapRouteState.NotMet,
            "supported closed dialogue with a false first guard is unavailable, not unsupported merely because it displays text");
        condition.Current = true;

        d = Build("local opts={'same'};local r=choose(opts);" + Say + ";local later={'same'};local n=choose(later);local utility=1");
        check(d.StoryEntries.Count == 1 && d.ChoiceSteps.Count == 2 && d.ChoiceSteps.All(x => x.RawLabel == "same") &&
            d.ChoiceSteps.Select(x => x.SourceOffset).Distinct().Count() == 2,
            "all-choice index includes a same-label utility node that has no story path");
        d = Build("local opts={'utility'};local r=choose(opts);local unused=1");
        check(d.StoryEntries.Count == 0 && d.ChoiceSteps.Count == 1, "utility-only choices retain their static identity without inventing a story");
        d = Build("local opts={};if checkpointmanager.Condition('gate') then opts[1]=checkpointmanager.Condition('gate') and 'same' or '~x' else opts[1]=true and 'same' or '~x' end;local r=choose(opts);" + Say);
        check(d.ChoiceSteps.Count == 2 && d.ChoiceSteps[0].SourceOffset == d.ChoiceSteps[1].SourceOffset &&
            d.ChoiceSteps[0].Index == d.ChoiceSteps[1].Index && d.ChoiceSteps.All(x => x.RawLabel == "same") &&
            !ReferenceEquals(d.ChoiceSteps[0].OptionGate, d.ChoiceSteps[1].OptionGate),
            "all-choice index preserves alternative guard definitions at the same source node and raw label");
        d = Build("local opts={'A','B','C'};local r=choose(opts);" + Say, null, new MapRouteLimits { MaxChoices = 2 });
        check(d.IsPartial && d.ChoiceSteps.Count == 2, "all-choice materialization respects the existing total choice bound");

        foreach (string source in new[]
        {
            Say,
            choice + "luamanager.SetNextScript('utility')",
            choice + "function unused() " + Say + " end",
            choice + "local unused=function() " + Say + " end",
            choice + "if false then " + Say + " end",
            choice + "local say=function() end;" + Say,
            choice + "function say() end;" + Say,
            choice + "local function say() end;" + Say,
            choice + "luamanager={};" + Say,
            choice + "local unused=luamanager.GetStoryText('dialogue-key')",
            choice + "unknown_callback(" + Say + ")",
            "local opts={'~disabled'};local r=choose(opts);" + Say,
            "-- " + Say + "\n" + choice + "local text=\"say(luamanager.GetStoryText('dialogue-key'))\""
        })
            check(Build(source, scripts).StoryEntries.Count == 0, "menus, utilities, unreachable/shadowed/nested/disabled/string bodies never invent a selected story");

        d = Build(choice + "luamanager.SetNextScript('leaf');luamanager.SetNextScript('empty')", scripts);
        check(d.StoryEntries.Count == 0, "overwritten literal story registration is never followed");
        d = Build(choice + "if checkpointmanager.Condition('gate') then " + Say + " else " + Say + " end");
        check(d.StoryEntries.Count == 2 && d.StoryEntries.All(x => x.Paths.Count == 2), "dialogue lines and conditional alternatives merge by script and selected option");
        check(d.StoryEntries.All(x => x.Current == MapRouteState.Met), "supported current branch union retains its known eligible path");
        var pooled = new MapRouteDefinitions("root", key => ScriptReadResult.Success(key == "root" ? choice + "local result=checkpointmanager.Position('p');luamanager.SetNextScript(result)" : Say),
            key => new[] { new MapRouteEvent(new object(), "pooled_result") });
        for (int i = 0; i < 10000 && pooled.IsPreparing; i++) pooled.Advance(0, 128);
        check(pooled.Entries.Count == 1 && pooled.Entries[0].Paths.Count == 2 && pooled.StoryEntries.Count == 0, "pool result dialogue remains a native event and never duplicates as selected literal story");

        string repeated = "";
        for (int i = 0; i <= MapRouteLimits.MaxChoiceSteps; i++) repeated += "local opts={'A'};local r=choose(opts);";
        d = Build(repeated + Say);
        check(d.IsPartial && d.StoryEntries.Count == 0, "choice step cap bounds copied path metadata before admitting another fork");
        d = Build(choice + Say + "; local invalid='unterminated");
        check(d.IsPartial && d.StoryEntries.All(x => x.Current == MapRouteState.Unknown), "malformed script never proves a prefix dialogue route");
        d.Clear();
        check(d.StoryEntries.Count == 0 && d.ChoiceSteps.Count == 0 && !d.IsPreparing, "lifecycle clear drops selected stories and all choice steps");
    }
}
