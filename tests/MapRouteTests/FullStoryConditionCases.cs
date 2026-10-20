using System;
using System.Collections.Generic;
using System.Linq;
using DiceMaster.Information;
using DiceMaster.LiveLog;

internal static class FullStoryConditionCases
{
    private const string Choice = "local opts={'practice+free+practice'};local r=choose(opts);";
    private const string Npc = "setcharacter(characters.Get('girl5'));say(luamanager.GetStoryText('D_ExplicitNpc'))";
    private static MapRouteDefinitions Build(string source, MapRouteLimits limits = null)
    {
        var d = new MapRouteDefinitions("root", key => ScriptReadResult.Success(key == "root" ? source : ""), key => new MapRouteEvent[0], limits);
        for (int i = 0; i < 10000 && d.IsPreparing; i++) d.Advance(0, 128);
        return d;
    }
    internal static Mortal.Core.FlagData FlagCondition(string key, string flag, bool current)
    {
        var data = new Mortal.Core.FlagData { name = flag, State = current ? 1 : 0 };
        var item = new Mortal.Core.ConditionResultItem { Current = current };
        item._items.Add(new Mortal.Core.StatCompareItem {
            _value1 = new Mortal.Core.StatGroupVariable { _value = new List<Mortal.Core.StatValueReference> { new Mortal.Core.StatValueReference { _checkType = 3, _flag = data } } },
            _value2 = new Mortal.Core.StatGroupVariable { _value = new List<Mortal.Core.StatValueReference> { new Mortal.Core.StatValueReference { _constant = 1 } } }
        });
        Mortal.Story.CheckPointManager.Instance._condition._list.Add(new Mortal.Core.ConditionResultData { name = key, Current = current, _items = new List<Mortal.Core.ConditionResultItem> { item } });
        return data;
    }
    internal static void Run(Action<bool, string> check)
    {
        var manager = Mortal.Story.CheckPointManager.Instance;
        var flag = FlagCondition("story_blocked", "met_person", false);
        FlagCondition("story_ready", "other_person", true);
        const string Guard = "if checkpointmanager.Condition('story_blocked') then ";
        var d = Build(Choice + Guard + Npc + " end");
        var story = d.StoryEntries.Single(); var path = story.Paths.Single();
        check(path.ReadEntryConditions(new MapRouteRead()) == MapRouteState.Met && path.ReadStoryConditions(new MapRouteRead()) == MapRouteState.NotMet,
            "unconditional training choice cannot hide a false internal NPC story guard");
        check(story.ReadStoryConditions(new MapRouteRead()) == MapRouteState.NotMet && d.IsPartial && !d.HasIncompleteStoryPaths,
            "known NPC presentation preserves strict partial execution without inventing omitted story paths");
        check(story.Current == MapRouteState.Unknown && !path.StoryConditionsMayHaveChanged,
            "full current condition snapshot neither strengthens strict execution nor treats rendering as a stat write");
        d = Build(Choice + "if checkpointmanager.Condition('story_ready') then " + Npc + " end");
        check(d.StoryEntries.Single().ReadStoryConditions(new MapRouteRead()) == MapRouteState.Met, "fully observed true internal story conditions can be known");
        d = Build(Choice + "statmodifymanager.AddFlag('unrelated',1);" + Guard + Npc + " end");
        check(d.StoryEntries.Single().ReadStoryConditions(new MapRouteRead()) == MapRouteState.NotMet && d.StoryEntries.Single().Paths.Single().StoryConditionsMayHaveChanged,
            "literal flag writes proven disjoint from checkpoint input do not conceal a false story guard");
        d = Build(Choice + "statmodifymanager.SetFlag('met_person',1);" + Guard + Npc + " end");
        check(d.StoryEntries.Single().ReadStoryConditions(new MapRouteRead()) == MapRouteState.Unknown,
            "a preceding write to a checkpoint dependency masks its old current value");
        d = Build(Choice + "statmodifymanager.AddFlag('met_person',1);if checkpointmanager.Condition('story_ready') and checkpointmanager.Condition('story_blocked') then " + Npc + " end");
        check(d.StoryEntries.Single().ReadStoryConditions(new MapRouteRead()) == MapRouteState.Unknown, "known true sibling cannot upgrade an affected unknown input");
        d = Build(Choice + "statmodifymanager.AddFlag('other_person',1);if checkpointmanager.Condition('story_ready') and checkpointmanager.Condition('story_blocked') then " + Npc + " end");
        check(d.StoryEntries.Single().ReadStoryConditions(new MapRouteRead()) == MapRouteState.NotMet, "a separate stable false necessary guard can reject a path with another masked input");
        d = Build(Choice + "local allowed=checkpointmanager.Condition('story_blocked');statmodifymanager.SetFlag('met_person',1);if allowed then " + Npc + " end");
        check(d.StoryEntries.Single().ReadStoryConditions(new MapRouteRead()) == MapRouteState.NotMet,
            "a checkpoint value bound before a later write keeps the original read moment");
        foreach (string effect in new[] { "unknown_callback();", "statmodifymanager.SetFlag(dynamic,1);", "statmodifymanager.Player('hp',1,'',1);", "runblock(flowcharts.view,'view');" })
        {
            d = Build(Choice + effect + Guard + Npc + " end");
            check(d.StoryEntries.Single().ReadStoryConditions(new MapRouteRead()) == MapRouteState.Unknown && d.HasIncompleteStoryPaths,
                "opaque/dynamic effects and callbacks cannot prove either story availability or rejection: " + effect);
        }
        d = Build(Choice + "if checkpointmanager.Condition('story_blocked') then " + Npc + " else " + Npc + " end");
        check(d.StoryEntries.Single().Paths.Count == 2 && d.StoryEntries.Single().ReadStoryConditions(new MapRouteRead()) == MapRouteState.Met,
            "complete alternative story paths aggregate an independently known true branch");
        d = Build(Choice + Npc + ";if checkpointmanager.Condition('story_ready') then luamanager.SetNextScript('later') end", new MapRouteLimits { MaxContexts = 2 });
        check(d.StoryEntries.Count == 1 && d.HasIncompleteStoryPaths && d.StoryEntries.Single().ReadStoryConditions(new MapRouteRead()) == MapRouteState.Unknown,
            "later omitted contexts cannot turn a partially observed story group green");
        var pooled = new MapRouteDefinitions("root", key => ScriptReadResult.Success(key == "root" ? "luamanager.SetNextScript(checkpointmanager.Position('pool'))" : Choice + Npc),
            key => new[] { new MapRouteEvent(new object(), "pool_result") });
        for (int i = 0; i < 10000 && pooled.IsPreparing; i++) pooled.Advance(0, 128);
        check(pooled.StoryEntries.Count == 1 && !pooled.HasIncompleteStoryPaths && pooled.StoryEntries.Single().ReadStoryConditions(new MapRouteRead()) == MapRouteState.Unknown,
            "an unselected pool result followed by a literal-true menu cannot become a proven current story");
        d = Build(Choice + "statmodifymanager.AddFlag('unrelated',1);" + Guard + Npc + " end");
        int reads = d.ScriptReadCount, parses = d.ParseCount, work = d.WorkCount;
        for (int i = 0; i < 100; i++) { d.Advance(i); check(d.StoryEntries.Single().ReadStoryConditions(new MapRouteRead()) == MapRouteState.NotMet, "warm full story snapshot remains false"); }
        check(d.ScriptReadCount == reads && d.ParseCount == parses && d.WorkCount == work, "full story conditions never reload or reparse Lua on a warm refresh");
        check(flag.State == 0 && manager.GameplayCalls == 0 && manager.RngCalls == 0 && manager.Counter == 0, "full story query never applies the modeled flag write or invokes gameplay/RNG/counters");
    }
}
