using System;
using System.Collections.Generic;
using System.Linq;
using DiceMaster.Information;
using DiceMaster.LiveLog;

internal static class MeaningfulStoryCases
{
    private const string Choice = "local opts={'sleep+free+sleep'};local r=choose(opts);";
    private const string Speech = "say(luamanager.GetStoryText('D_Monologue'))";
    private const string Npc = "setcharacter(characters.Get('brother4'));";
    private static MapRouteDefinitions Build(string source, string leaf = null)
    {
        var d = new MapRouteDefinitions("root", key => ScriptReadResult.Success(key == "root" ? source : leaf ?? ""), key => new MapRouteEvent[0]);
        for (int i = 0; i < 10000 && d.IsPreparing; i++) d.Advance(0, 128);
        return d;
    }
    internal static void Run(Action<bool, string> check)
    {
        foreach (string source in new[]
        {
            Speech,
            "setcharacter(narrative);" + Speech,
            "setcharacter(characters.Get('player'));" + Speech,
            "setcharacter(characters.Get('player_unknown'));" + Speech,
            "setcharacter(characters.Get('mega_player'));" + Speech,
            "characters.Focus('brother4');" + Speech,
            "setcharacter(characters.Get(who));" + Speech,
            "setcharacter(characters:Get('brother4'));" + Speech,
            "characters.setcharacter(characters.Get('brother4'));" + Speech,
            "local setcharacter=function() end;" + Npc + Speech,
            "function setcharacter() end;" + Npc + Speech,
            "local function setcharacter() end;" + Npc + Speech,
            "local characters=other;" + Npc + Speech,
            "characters={};" + Npc + Speech,
            "characters.Get=other;" + Npc + Speech,
            "characters['Get']=other;" + Npc + Speech,
            "local c=characters;c.Get=function() return narrative end;" + Npc + Speech,
            "local c=(characters);c.Get=function() return narrative end;" + Npc + Speech,
            "local c={characters};c[1].Get=function() return narrative end;" + Npc + Speech,
            "local c={};c[1]=characters;c[1].Get=function() return narrative end;" + Npc + Speech,
            "local c={unknown_callback(),characters};c[2].Get=function() return narrative end;" + Npc + Speech,
            Npc + "local c=characters;c.Focus=function() setcharacter(narrative) end;characters.Focus('brother4');" + Speech,
            Npc + "setcharacter(narrative);" + Speech,
            Npc + "setcharacter(characters.Get('player'));characters.Focus('brother4');" + Speech,
            Npc + "setcharacter(dynamic);" + Speech,
            Npc + "unknown_callback();" + Speech,
            Npc + "unknown:change();" + Speech,
            Npc + "local c=unknown:change();" + Speech,
            Npc + "local t={setcharacter(narrative)};" + Speech,
            Npc + "local t={unknown_callback()};" + Speech,
            Npc + "local t={};t[1]=setcharacter(narrative);" + Speech,
            Npc + "unknown[1]=setcharacter(narrative);" + Speech,
            Npc + "while flag do setcharacter(narrative);break end;" + Speech,
            Npc + "for i=1,1 do setcharacter(narrative) end;" + Speech,
            Npc + "if unknown_callback() then " + Speech + " else " + Speech + " end"
        })
            check(Build(Choice + source).StoryEntries.Count == 0, "only an unshadowed explicit NPC followed by supported speech admits a story: " + source);

        var d = Build("local opts={'talk4'};local r=choose(opts);characters.Focus('brother4');" + Speech);
        check(d.StoryEntries.Count == 0, "talk4 option and D_ prefix plus Focus cannot establish NPC speech");
        d = Build(Choice + "setcharacter(narrative);say(luamanager.GetStoryText('N_Intro'));setcharacter(characters.Get('player'));" + Speech + ";" + Npc + "characters.Focus('brother4');say(luamanager.GetStoryText('N_ExplicitNpc'))");
        check(d.StoryEntries.Count == 1 && d.StoryEntries[0].StoryKey == "N_ExplicitNpc" && d.StoryEntries[0].DialogueCharacter == "brother4",
            "initial narration and player lines do not prune later explicit NPC speech even behind a sleep choice");
        check(d.StoryEntries[0].Current == MapRouteState.Unknown, "explicit NPC metadata does not prove UI execution or current availability");
        int reads = d.ScriptReadCount, parses = d.ParseCount, work = d.WorkCount;
        for (int i = 0; i < 100; i++) { d.Advance(i); foreach (var s in d.StoryEntries) { var name = s.DialogueCharacter; s.ReadCurrent(new MapRouteRead()); } }
        check(d.ScriptReadCount == reads && d.ParseCount == parses && d.WorkCount == work, "warm meaningful story reads reuse source and syntax caches");

        d = Build(Choice + "if checkpointmanager.Condition('gate') then " + Npc + " else setcharacter(narrative) end;" + Speech);
        check(d.StoryEntries.Count == 1 && d.StoryEntries[0].Paths.Count == 1 && d.StoryEntries[0].Paths[0].DialogueCharacter == "brother4",
            "NPC witness cannot leak into narrative sibling paths");
        d = Build(Choice + "if checkpointmanager.Condition('gate') then " + Npc + " else setcharacter(characters.Get('girl7')) end;" + Speech);
        check(d.StoryEntries.Count == 1 && d.StoryEntries[0].Paths.Count == 2 && d.StoryEntries[0].DialogueCharacter == "" &&
            d.StoryEntries[0].Paths.Select(x => x.DialogueCharacter).Distinct().Count() == 2,
            "same selected segment keeps distinct NPC metadata per source branch without inventing one representative");
        d = Build(Choice + Npc + "luamanager.SetNextScript('leaf')", Speech);
        check(d.StoryEntries.Count == 0, "new literal script cannot inherit a previous script speaker");
        d = Build(Choice + Npc + "luamanager.SetNextScript('leaf')", Npc + Speech);
        check(d.StoryEntries.Count == 1 && d.StoryEntries[0].Script == "leaf", "literal followup can establish its own NPC evidence");
        d = Build(Choice + Npc + "local t={unknown_callback()};" + Npc + Speech);
        check(d.StoryEntries.Count == 1, "a fresh explicit setter restores evidence after an opaque callback boundary");
        d = Build(Choice + Npc + "local function unused() setcharacter(narrative) end;" + Speech);
        check(d.StoryEntries.Count == 1 && d.StoryEntries[0].DialogueCharacter == "brother4", "an uncalled function body cannot change the observed speaker");
        d = Build(Choice + Npc + Speech + ";setcharacter(characters.Get('brother2'));" + Speech);
        check(d.StoryEntries.Count == 1 && d.StoryEntries[0].Paths.Count == 1 && d.StoryEntries[0].DialogueCharacter == "brother4",
            "one selected segment retains its first NPC witness rather than duplicating each conversation participant");
    }
}
