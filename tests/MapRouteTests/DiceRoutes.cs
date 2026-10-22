using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using DiceMaster.Information;
using DiceMaster.LiveLog;
internal static partial class Program
{
    private static string Roll(string after) => "local d={};d[1]='O_a|<50';d[2]='O_b|>=50';runwait(dicemenudialog.ExecuteRoll(d,1,'check'));local r=dicemenudialog.ResultSelection;" + after;
    private static DiceRouteIndex Index(string text)
    { var index = new DiceRouteIndex("root",text);for(int n=0;n<100000&&!index.Ready;n++)index.Advance(256,null);return index; }
    private static MapRouteDefinitions Arm(DiceRouteIndex index,int result)
    {
        var site=index.Find("check",new[]{"O_a|<50","O_b|>=50"});
        var graph=index.Arm(site,result,_=>ScriptReadResult.Missing(),Pool);
        if(graph!=null)for(int n=0;n<10000&&graph.IsPreparing;n++)graph.Advance(0,256);
        return graph;
    }
    private static string Speech(string who,string key)=>"setcharacter(characters.Get('"+who+"'));say(luamanager.GetStoryText('D_"+key+"'));";
    static void DiceRoutes()
    {
        string body=Roll("if r==1 then "+Speech("brother","a")+" elseif r==2 then "+Speech("girl","b")+" end");
        var index=Index(body);Check(index.Ready&&!index.Unsupported&&index.Sites.Count==1,"dice literal table/result dispatch indexed");
        var first=Arm(index,1);var second=Arm(index,2);
        Check(first.StoryEntries.Count==1&&first.StoryEntries[0].StoryKey=="D_a"&&second.StoryEntries[0].StoryKey=="D_b","hypothetical outcome cannot leak another result story");
        Check(first.StoryPaths[0].Path.Steps[0].Index==1&&second.StoryPaths[0].Path.Steps[0].Index==2,"result source identity kept separately");
        int work=first.WorkCount;for(int n=0;n<1000;n++)first.Advance(n);Check(first.WorkCount==work&&first.ParseCount==0,"seed shares completed lexical definition; warm reads never parse");
        Check(index.Find("other",new[]{"O_a|<50","O_b|>=50"})==null&&index.Find("check",new[]{"O_b|<50","O_a|>=50"})==null,"native key and ordered raw rows must agree");
        Check(Index(body+body).Find("check",new[]{"O_a|<50","O_b|>=50"})==null,"duplicate identical call signatures fail closed");
        foreach(string text in new[]{body+"dicemenudialog.ExecuteRoll=other",body+"dicemenudialog={}",body+"--[[broken"})
            Check(Index(text).Find("check",new[]{"O_a|<50","O_b|>=50"})==null,"changed API and malformed definitions cannot lend earlier site evidence");
        Check(Index(Roll("if r==1 then "+Speech("npc","a")+" else "+Speech("npc","b")+" end")).Sites.Count==0,"unsupported else mapping is not guessed");
        var manager=Mortal.Story.CheckPointManager.Instance;
        var gate=new Mortal.Core.ConditionResultData{name="dice_gate",Current=false};manager._condition._list.Add(gate);
        var graph=Arm(Index(Roll("if r==1 then if checkpointmanager.Condition('dice_gate') then "+Speech("brother","locked")+" end elseif r==2 then end")),1);
        var path=graph.StoryPaths[0].Path;int gates=128,kinds=128;
        Check(path.ReadGuidance(new MapRouteRead(),0,true,true,ref gates,ref kinds,out _)==MapGuidanceRejection.Conditions,"known false gate excludes a dice story");
        gate.Current=true;gates=kinds=128;
        Check(path.ReadGuidance(new MapRouteRead(),0,true,true,ref gates,ref kinds,out _) == MapGuidanceRejection.None,"current gate is not frozen into definition cache");
        gate.Current=false;
        var mixed=Arm(Index(Roll("if r==1 then local x=checkpointmanager.Switch('unresolved_random');if x==1 and checkpointmanager.Condition('dice_gate') then "+Speech("brother","false_and_unknown")+" end elseif r==2 then end")),1);
        gates=kinds=128;
        Check(mixed.StoryPaths[0].Path.ReadGuidance(new MapRouteRead(),0,true,true,ref gates,ref kinds,out _)!=MapGuidanceRejection.None,"unknown random gate never overrides a false AND prerequisite");
        manager._condition._list.Remove(gate);
        var ordinary=Build(body);Check(ordinary.Entries.All(e=>e.Current!=MapRouteState.Met),"ordinary route parser does not inherit a hypothetical dice outcome");
        // Audit actual, read-only supplied corpus with this production index.
        string file=Environment.GetEnvironmentVariable("DICEMASTER_CORPUS_INDEX");
        if(!string.IsNullOrEmpty(file))
        {
            using var doc=JsonDocument.Parse(File.ReadAllText(file));int files=0,sites=0;long elapsed=0;
            foreach(var row in doc.RootElement.EnumerateArray())
            {
                string source=File.ReadAllText(row.GetProperty("path").GetString());if(!source.Contains("ExecuteRoll("))continue;
                long started=Stopwatch.GetTimestamp();var actual=Index(source);elapsed+=Stopwatch.GetTimestamp()-started;files++;sites+=actual.Sites.Count;
                Check(actual.Ready&&!actual.Syntax.Malformed&&!actual.Syntax.Limited,"provided dice source closes within lexical limits");
            }
            Check(files==288&&sites>350&&sites<=402,"production dice index covers supplied generated shapes without pretending complete support");
            Console.WriteLine("DICE CORPUS: files="+files+" sites="+sites+" index_ms="+(elapsed*1000.0/Stopwatch.Frequency));
        }
    }
}
