using System;
using System.Collections.Generic;
using System.Linq;
using DiceMaster.Information;
using DiceMaster.LiveLog;

internal static class StoryTopicCases
{
    private const string Npc = "setcharacter(characters.Get('renamed_person'));say(luamanager.GetStoryText('renamed_text'));";
    private const string Activity = Npc + "local s=checkpointmanager.Switch('renamed_eligibility');if s==1 then statmodifymanager.Player('money',-100,'',1);" + Npc + "luamanager.SetCurrentStoryScript('receipt');luamanager.ChangeScene('Combat','renamed_encounter','Story') elseif s==2 then luamanager.ChangeScene('Free','','') end";
    private static MapRouteDefinitions Build(string body, string receipt = "")
    {
        var d = new MapRouteDefinitions("root", key => ScriptReadResult.Success(key == "root" ? "local o={'person'};local r=choose(o);if r==1 then luamanager.SetNextScript('topic') end" : key == "topic" ? body : receipt), key => new MapRouteEvent[0]);
        for (int i = 0; i < 20000 && d.IsPreparing; i++) d.Advance(0, 64);
        return d;
    }
    internal static void Run(Action<bool, string> check)
    {
        var d = Build(Activity);
        check(d.StoryEntries.Count == 1 && d.StoryEntries[0].Paths.All(x => x.StoryKind == MapStoryKind.ActivityResponse),
            "generic paid activity/refusal structure classifies all of its fixed dialogue paths without NPC, flag or checkpoint names");
        check(d.StoryEntries[0].Paths.All(x => x.TopicStep != null && x.TopicStep.RawLabel == "person"), "individual selected topic metadata survives a literal followup");
        foreach (string body in new[] { Activity.Replace("local s=", "unknown_callback();local s="), Activity.Replace("local s=", "evil 'argument';local s="),
            Activity.Replace("local s=", "characters={};local s="), Activity.Replace("local s=", "statmodifymanager.Player=function() end;local s="),
            Activity.Replace("local s=", "statmodifymanager.SetFlag('new_story_flag',1);local s="), Activity + ";unknown_callback()" })
        {
            d = Build(body);
            check(d.StoryEntries.SelectMany(x => x.Paths).All(x => x.StoryKind != MapStoryKind.ActivityResponse),
                "callback, sugar, rebinding, consumed flag and unmodelled tail prevent fixed-activity proof");
        }
        d = Build(Npc + "if checkpointmanager.Condition('new_story') then " + Npc + "statmodifymanager.SetFlag('seen',1) end;" + Activity);
        check(d.StoryEntries.SelectMany(x => x.Paths).All(x => x.StoryKind != MapStoryKind.ActivityResponse),
            "an additional conditional story within an activity remains a dialogue candidate");
        d = Build(Activity, "local outcome=checkpointmanager.Switch('renamed_result');if outcome==1 then statmodifymanager.Player('mental',-20,'',1);" + Npc +
            "elseif outcome==2 then statmodifymanager.Player('point',4,'',1);" + Npc + "statmodifymanager.SetFlag('renamed_receipt',1) end;luamanager.ChangeScene('Free','','')");
        check(d.StoryEntries.Any(x => x.Script == "receipt") && d.StoryEntries.Where(x => x.Script == "receipt").SelectMany(x => x.Paths).All(x => x.StoryKind == MapStoryKind.ActivityResponse),
            "a proven activity parent plus closed mechanical result receipt classifies fixed result dialogue without key names");
        d = Build(Activity, "local outcome=checkpointmanager.Switch('renamed_result');if outcome==1 then statmodifymanager.Player('mental',-20,'',1);" + Npc +
            "if checkpointmanager.Condition('special') then " + Npc + "statmodifymanager.SetFlag('seen',1) end elseif outcome==2 then statmodifymanager.Player('point',4,'',1);" + Npc + "end;luamanager.ChangeScene('Free','','')");
        check(d.StoryEntries.Where(x => x.Script == "receipt").SelectMany(x => x.Paths).All(x => x.StoryKind != MapStoryKind.ActivityResponse),
            "an extra condition inside an activity callback is preserved rather than blindly inheriting activity classification");
        FullStoryConditionCases.FlagCondition("topic_closed", "topic_closed_flag", false);
        const string menu = "runwait(characters.LoadCharacterAsset('renamed_person'));characters.LoadCharacterPortrait('renamed_person','normal');stage.show{character=characters.Get('renamed_person'),fromPosition='Talk',fadeDuration=0};characters.RestoreRotation('renamed_person');setmenudialog(menudialogs.Talk);local o={};o[1]=checkpointmanager.Condition('topic_closed') and 'topic' or '~x';local c=choose(o);if c==1 then " + Npc + " end";
        d = Build(menu);
        check(d.StoryEntries.Single().Paths.All(x => x.TopicStep.ReadTopicBlocker(new MapRouteRead())),
            "closed lower-menu presentation plus complete own false option suppresses its individual topic before NPC aggregation");
        d = Build("unknown_callback();" + menu);
        check(d.StoryEntries.SelectMany(x => x.Paths).All(x => !x.TopicStep.ReadTopicBlocker(new MapRouteRead())),
            "arbitrary lower-menu callback cannot grant a current topic blocker");
        d = Build("statmodifymanager.SetFlag('topic_closed_flag',1);" + menu);
        check(d.StoryEntries.SelectMany(x => x.Paths).All(x => !x.TopicStep.ReadTopicBlocker(new MapRouteRead())),
            "a lower guard affected by an earlier flag write remains unknown");
        d = Build("statmodifymanager.SetFlag('unrelated',1);" + menu);
        check(d.StoryEntries.Single().Paths.All(x => x.TopicStep.ReadTopicBlocker(new MapRouteRead())),
            "an unrelated literal flag write preserves the closed lower-menu negative proof");
        FullStoryConditionCases.FlagCondition("activity_parent", "consumed_parent", true);
        var activityGate = PreviewConditions.Parse("checkpointmanager.Condition('activity_parent')");
        var activityPath = new MapRoutePath("activity-proof", "activity-proof", true, true, activityGate,
            storyKind: MapStoryKind.ActivityResponse, activityPrerequisite: activityGate, precedingFlagWrites: new[] { "consumed_parent" });
        int proofBudget = 1, stateReads = SafeDisplayEvaluation.ConditionReads;
        var sameRead = new MapRouteRead();
        check(activityPath.ReadStoryKind(sameRead, ref proofBudget) == MapStoryKind.Unclassified && proofBudget == 0,
            "consumed parent topic condition preserves a structurally matching service wrapper as a story candidate");
        check(activityPath.ReadStoryKind(sameRead, ref proofBudget) == MapStoryKind.Unclassified && proofBudget == 0,
            "the same dynamic dependency proof is memoized within one scheduled read");
        var parentDefinition = Mortal.Story.CheckPointManager.Instance._condition._list.Single(x => x.name == "activity_parent");
        var previousItems = parentDefinition._items;
        parentDefinition._items = new Mortal.Core.ConditionResultData { Current = true }._items;
        proofBudget = 1;
        check(activityPath.ReadStoryKind(new MapRouteRead(), ref proofBudget) == MapStoryKind.ActivityResponse,
            "a fresh scheduled read observes replacement serialized dependency definitions instead of a cached native classification");
        parentDefinition._items = previousItems;
        proofBudget = 0;
        check(activityPath.ReadStoryKind(new MapRouteRead(), ref proofBudget) == MapStoryKind.Unclassified && SafeDisplayEvaluation.ConditionReads == stateReads,
            "dependency budget exhaustion conservatively retains the story and never calls current stat/condition getters");
        var nested = new MapRouteDefinitions("root", key => ScriptReadResult.Success(key == "root"
            ? "local o={};o[1]=checkpointmanager.Condition('activity_parent') and 'outer' or '~x';local c=choose(o);if c==1 then statmodifymanager.SetFlag('consumed_parent',0);luamanager.SetNextScript('nested_menu') end"
            : key == "nested_menu" ? "local o={'inner'};local c=choose(o);if c==1 then luamanager.SetNextScript('activity') end" : Activity), key => new MapRouteEvent[0]);
        for (int i = 0; i < 10000 && nested.IsPreparing; i++) nested.Advance(0, 64);
        proofBudget = 32; var nestedRead = new MapRouteRead();
        bool retainedConsumedAncestor = true;
        foreach (var path in nested.StoryEntries.SelectMany(x => x.Paths)) retainedConsumedAncestor &= path.ReadStoryKind(nestedRead, ref proofBudget) != MapStoryKind.ActivityResponse;
        check(nested.StoryEntries.Count > 0 && retainedConsumedAncestor,
            "a later unconditional subtopic never discards a consumed ancestor story prerequisite when classifying its service-shaped body");
        int reads = d.ScriptReadCount, parses = d.ParseCount, work = d.WorkCount;
        for (int i = 0; i < 160; i++) { d.Advance(i); foreach (var p in d.StoryEntries.SelectMany(x => x.Paths)) { var kind = p.StoryKind; } }
        check(reads == d.ScriptReadCount && parses == d.ParseCount && work == d.WorkCount, "warm topic classification performs no Lua reads, parses or definition work");
    }
}
