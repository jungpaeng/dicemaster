using System;
using System.Collections.Generic;
using System.Linq;
using DiceMaster.Information;
using DiceMaster.LiveLog;

internal static class CurrentBlockerCases
{
    private const string Prelude = "runwait(flowcharts.LoadView('room'));getvar(flowcharts.view,'ViewName').value='room';runblock(flowcharts.view,'view');mainui.DisplayTitle('P_Room');setmenudialog(menudialogs.Room);";
    private const string Npc = "setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_SnapshotFixture'))";
    private const string GuardedMenu = "local opts={};opts[1]=checkpointmanager.Condition('snapshot_false') and 'talk+talk+brother4' or '~x';local r=choose(opts);menudialogs.Room.SetActive(false);if r==1 then " + Npc + " end";
    private const string PlainMenu = "local opts={'talk+talk+brother4'};local r=choose(opts);menudialogs.Room.SetActive(false);";
    private static MapRouteDefinitions Build(string source, MapRouteLimits limits = null)
    {
        var d = new MapRouteDefinitions("root", key => ScriptReadResult.Success(key == "root" ? source : Npc), key => new MapRouteEvent[0], limits);
        for (int i = 0; i < 10000 && d.IsPreparing; i++) d.Advance(0, 128);
        return d;
    }
    private static MapRoutePath Path(MapRouteDefinitions d) => d.StoryEntries.First().Paths.First();
    private static MapRouteDefinitions Dispatch(string source, string child = "")
    {
        var d = new MapRouteDefinitions("root", key => ScriptReadResult.Success(key == "root" ? source : child), key => new MapRouteEvent[0]);
        for (int i = 0; i < 10000 && d.IsPreparing; i++) d.Advance(0, 128);
        return d;
    }
    private static bool OmissionsBlocked(MapRouteDefinitions d)
    { int budget = 32; return d.CurrentOmissionsBlocked(d.ChoiceSteps.First(), new MapRouteRead(), ref budget); }
    private static void TerminalDispatch(Action<bool, string> check)
    {
        const string RegisterPool = "local next=checkpointmanager.Position('empty');luamanager.SetNextScript(next);";
        var d = Dispatch(Prelude + PlainMenu + RegisterPool + "luamanager.Init()");
        check(OmissionsBlocked(d) && d.IsPartial && d.HasIncompleteStoryPaths,
            "fresh terminal pool dispatch removes only a duplicate missing target while retaining execution uncertainty");
        d = Dispatch(Prelude + PlainMenu + "if r==1 then do " + RegisterPool + "luamanager.Init();end end;");
        check(OmissionsBlocked(d), "fresh terminal dispatch may finish verified empty current and outer frames without inventing an unwind cut");
        d = Dispatch(Prelude + PlainMenu + "luamanager.SetNextScript('child');luamanager.Init()", Npc);
        check(OmissionsBlocked(d) && d.StoryEntries.Count == 1 && Path(d).ReadStoryConditions(new MapRouteRead()) == MapRouteState.Unknown,
            "complete literal target traversal preserves full execution uncertainty without inventing an omitted current-topic alternative");
        foreach (string suffix in new[] {
            RegisterPool + "local intervening=1;luamanager.Init()",
            RegisterPool + "unknown_callback();luamanager.Init()",
            RegisterPool + "luamanager.Init();local tail=1",
            "if r==1 then " + RegisterPool + "luamanager.Init() end;local outertail=1",
            "luamanager.SetNextScript(dynamic);luamanager.Init()",
            RegisterPool + "luamanager:Init()",
            "luamanager.Init=function() end;" + RegisterPool + "luamanager.Init()"
        })
        {
            d = Dispatch(Prelude + PlainMenu + suffix);
            check(!OmissionsBlocked(d), "stale/callback/nonterminal/outer-tail/dynamic/colon/rebound dispatch preserves its generic omission cut");
        }
        d = Dispatch(Prelude + PlainMenu + "unknown_callback();" + RegisterPool + "luamanager.Init()");
        check(!OmissionsBlocked(d), "a fresh terminal dispatch cannot erase an earlier callback's already recorded cut");
    }
    private static void CommonPrerequisites(Action<bool, string> check)
    {
        var flag = FullStoryConditionCases.FlagCondition("snapshot_compressed", "snapshot_compressed_flag", false);
        var common = PreviewConditions.All(PreviewConditions.Constant(true), PreviewConditions.CaptureCurrent(
            PreviewConditions.Parse("checkpointmanager.Condition('snapshot_compressed')"), new string[0], false));
        var compressed = MapRoutePath.CommonPrerequisite(PreviewConditions.All(common, PreviewConditions.Unknown),
            PreviewConditions.All(common, PreviewConditions.Constant(true)));
        bool initiallyFalse = PreviewConditions.Current(compressed) == PreviewConditionState.NotMet;
        flag.State = 1;
        check(ReferenceEquals(compressed, common) && initiallyFalse && PreviewConditions.Current(compressed) == PreviewConditionState.Met,
            "shared necessary AND prefix compression retains syntax identity and resamples current values");
        var bypass = MapRoutePath.CommonPrerequisite(PreviewConditions.All(common, PreviewConditions.Constant(false)), PreviewConditions.Constant(true));
        check(PreviewConditions.Current(bypass) == PreviewConditionState.Unknown,
            "an observed bypass cannot inherit another alternative's false prerequisite through compression");
    }
    internal static void Run(Action<bool, string> check)
    {
        FullStoryConditionCases.FlagCondition("snapshot_false", "snapshot_flag", false);
        FullStoryConditionCases.FlagCondition("snapshot_true", "snapshot_true_flag", true);
        var d = Build(Prelude + GuardedMenu); var path = Path(d);
        check(path.Steps[0].ReadCurrentBlocker(new MapRouteRead()) && path.ReadCurrentBlocker(new MapRouteRead()),
            "exact root presentation prefix permits a negative-only current prerequisite projection");
        check(path.ReadStoryConditions(new MapRouteRead()) == MapRouteState.Unknown,
            "presentation projection cannot erase full-story opaque execution uncertainty");
        d = Build(Prelude + "statmodifymanager.SetFlag('snapshot_flag',1);" + GuardedMenu);
        check(!Path(d).Steps[0].ReadCurrentBlocker(new MapRouteRead()) && !Path(d).ReadCurrentBlocker(new MapRouteRead()),
            "same-flag write before a guard masks that current negative projection");
        d = Build(Prelude + "statmodifymanager.SetFlag('unrelated',1);" + GuardedMenu);
        check(!Path(d).Steps[0].ReadCurrentBlocker(new MapRouteRead()) && Path(d).ReadCurrentBlocker(new MapRouteRead()),
            "a disjoint write preserves the pre-entry map proof but cannot recheck an already open menu");
        d = Build(Prelude + "local saved=checkpointmanager.Condition('snapshot_false');statmodifymanager.SetFlag('snapshot_flag',1);local opts={};opts[1]=saved and 'talk+talk+brother4' or '~x';local r=choose(opts);if r==1 then " + Npc + " end");
        check(!Path(d).Steps[0].ReadCurrentBlocker(new MapRouteRead()) && Path(d).ReadCurrentBlocker(new MapRouteRead()),
            "an earlier bound read preserves the pre-entry map proof while a later write blocks current-menu rechecking");
        d = Build(Prelude + "unknown_callback();" + GuardedMenu);
        check(!Path(d).Steps[0].ReadCurrentBlocker(new MapRouteRead()) && !Path(d).ReadCurrentBlocker(new MapRouteRead()),
            "arbitrary callback before a prerequisite cannot yield a current false proof");
        foreach (string prefix in new[] {
            Prelude.Replace("runblock(flowcharts.view", "unknown_callback();runblock(flowcharts.view"),
            "local flowcharts={};" + Prelude,
            "setmenudialog=function() end;" + Prelude,
            Prelude.Replace("value='room'", "value='different'"),
            Prelude.Replace("LoadView('room')", "LoadView(dynamic())")
        })
        {
            d = Build(prefix + GuardedMenu);
            check(!Path(d).Steps[0].ReadCurrentBlocker(new MapRouteRead()) && !Path(d).ReadCurrentBlocker(new MapRouteRead()),
                "interleaved/rebound/dynamic/mismatched presentation profile cannot grant negative projection authority");
        }
        d = Build(Prelude + "local opts={};if checkpointmanager.Condition('snapshot_true') then opts[1]=checkpointmanager.Condition('snapshot_false') and 'talk+talk+brother4' or '~x' else opts[1]=checkpointmanager.Condition('missing_snapshot') and 'talk+talk+brother4' or '~x' end;local r=choose(opts);if r==1 then " + Npc + " end");
        check(d.ChoiceSteps.Count == 2 && d.ChoiceSteps.All(x => !x.ReadCurrentBlocker(new MapRouteRead())),
            "a choice behind alternative root branches is not a complete unique root entry");
        d = Build(Prelude + "local prior=choose(globalOptions);" + GuardedMenu);
        check(!Path(d).Steps[0].ReadCurrentBlocker(new MapRouteRead()),
            "an unindexed earlier menu cannot be confused with the later known false menu slot");
        d = Build(Prelude + PlainMenu + "if checkpointmanager.Condition('snapshot_false') then " + Npc + " end;luamanager.SetNextScript('unseen')", new MapRouteLimits { MaxScripts = 1 });
        path = Path(d); int budget = 32;
        check(path.ReadCurrentBlocker(new MapRouteRead()) && !d.CurrentOmissionsBlocked(path.Steps[0], new MapRouteRead(), ref budget),
            "one false admitted path cannot exclude an omitted continuation with a possibly true necessary prefix");
        d = Build(Prelude + PlainMenu + "if checkpointmanager.Condition('snapshot_false') then " + Npc + ";luamanager.SetNextScript('unseen') end", new MapRouteLimits { MaxScripts = 1 });
        path = Path(d); budget = 32;
        check(path.ReadCurrentBlocker(new MapRouteRead()) && d.CurrentOmissionsBlocked(path.Steps[0], new MapRouteRead(), ref budget),
            "a proven false necessary prefix can exclude the same omitted continuation without upgrading it to available");
        d = Build(Prelude + PlainMenu + "if checkpointmanager.Condition('snapshot_false') then " + Npc + " elseif checkpointmanager.Condition('missing_snapshot') then " + Npc + " end");
        var alternatives = d.StoryEntries.Single().Paths;
        check(alternatives.Count == 2 && alternatives.Any(x => x.ReadCurrentBlocker(new MapRouteRead())) && alternatives.Any(x => !x.ReadCurrentBlocker(new MapRouteRead())),
            "a false and an unknown alternative remain distinct instead of rejecting the whole NPC group");
        d = Build(Prelude + GuardedMenu + ";local malformed='unterminated");
        check(d.ChoiceSteps.All(x => !x.ReadCurrentBlocker(new MapRouteRead())), "malformed syntax cannot grant complete-root negative authority");
        TerminalDispatch(check);
        CommonPrerequisites(check);
    }
}
