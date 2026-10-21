using System;
using System.Linq;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Story;
using UnityEngine.EventSystems;

internal static partial class Program
{
    private const string UnlockRoot = "local a={'O_entry'};local r=choose(a);if r==1 then ";
    private const string UnlockNpc = "setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_unlock'))";
    private const string UnlockMenu = "local t={};t[1]=checkpointmanager.Condition('unlock_first') and 'talk+O_first+0+0' or '~x';" +
        "t[2]=checkpointmanager.Condition('unlock_second') and 'talk+O_second+0+0' or '~x';local r=choose(t);" +
        "if r==1 or r==2 then " + UnlockNpc + " end";

    private static void ChoiceUnlocks()
    {
        Reset(); var first = Condition("unlock_first", 1); var second = Condition("unlock_second", 0);
        DisplayNames.Labels["Story/O_first"] = "예전에 부탁했던 일";
        DisplayNames.Labels["Story/O_second"] = "다른 대화";
        DataHelper.Scripts["unlock_topics"] = UnlockMenu;
        ChoiceStorySource.ObserveScript(UnlockRoot + "luamanager.SetNextScript('unlock_topics') end", "unlock_room.LuaScript");
        var button = Normal("O_entry", "사사형"); EventSystem.current.currentSelectedGameObject = button.gameObject;
        var value = Ready(true); var row = Linked(value, "사사형");
        Check(row.AutoText.Contains("다른 대화", StringComparison.Ordinal) && !row.AutoText.Contains("예전에 부탁했던 일", StringComparison.Ordinal),
            "an unlocked alternative supplies the current tooltip instead of a structurally present locked topic");
        Check(!row.Detail.Contains("예전에 부탁했던 일", StringComparison.Ordinal) && row.Detail.Contains("다른 대화", StringComparison.Ordinal) && !row.AutoText.Contains("외 ", StringComparison.Ordinal),
            "detail paths and additional counts exclude locked choices using the same witnesses as the tooltip");
        second.State = 1; value = Read();
        Check(!value.Entries.Any(x => x.Title == "사사형") && !value.Hints.Any(x => x.Title == "사사형") && ChoiceStorySource.CurrentRow(value) == null,
            "all locked followups remove the row, hint and passive pad tooltip without disabling the native current option");
        row = Linked(Read(true), "사사형");
        Check(row.AutoText == "" && !row.Detail.Contains("예전에 부탁했던 일", StringComparison.Ordinal) && !row.Detail.Contains("다른 대화", StringComparison.Ordinal),
            "explicit current detail explains an empty unlock result without listing currently absent topics");
        first.State = 0; row = Linked(Read(true), "사사형");
        Check(row.AutoText.Contains("예전에 부탁했던 일", StringComparison.Ordinal) && !row.Detail.Contains("다른 대화", StringComparison.Ordinal),
            "changing the actual unlock input restores exactly its newly unlocked path without a context change");
        int reads = DataHelper.Reads, parses = DiceMaster.LuaParse.LuaParser.Parses;
        for (int at = 0; at < 100; at++) { value = Read(); for (int frame = 0; frame < 20; frame++) ChoiceStorySource.CurrentRow(value); }
        Check(DataHelper.Reads == reads && DiceMaster.LuaParse.LuaParser.Parses == parses && button._source.text == "O_entry" && button.isActiveAndEnabled && Counters.GameCalls == 0,
            "warm unlock refresh and pad rendering reuse source definitions, preserve native controls and execute no gameplay judgment");
        ChoiceStorySource.Interactive = _ => false;
        Check(!Read().Entries.Any(x => x.Title == "사사형"), "a currently noninteractive native option cannot keep an automatic unlocked story tooltip");

        foreach (string effect in new[] { "", "unknown_callback();", "statmodifymanager.ResetTalkFlag();", "statmodifymanager.SetFlag('unlock_first',0);" })
        {
            Reset(); if (effect.Length != 0) Condition("unlock_first", 0); // Empty case deliberately has no readable checkpoint definition.
            DataHelper.Scripts["unlock_topics"] = "local a={};a[1]=checkpointmanager.Condition('unlock_first') and 'talk+O_first+0+0' or '~x';local r=choose(a);if r==1 then " + UnlockNpc + " end";
            ChoiceStorySource.ObserveScript(UnlockRoot + effect + "luamanager.SetNextScript('unlock_topics') end", "uncertain_unlock.LuaScript");
            Normal("O_entry", "사사형"); value = Ready(true); row = Linked(value, "사사형");
            Check(row.AutoText == "" && !row.Detail.Contains("O_first", StringComparison.Ordinal) && !Read().Entries.Any(x => x.Title == "사사형"),
                "missing definitions, arbitrary callbacks, unknown reset footprint and affected writes cannot advertise an unconfirmed topic: " + effect);
        }

        Reset(); Condition("unlock_first", 1); Condition("unlock_second", 0);
        DataHelper.Scripts["unlock_topics"] = "local a={};a[1]=checkpointmanager.Condition('unlock_first') and 'O_first' or '~x';local r=choose(a);if r==1 then luamanager.SetNextScript('unlock_deeper') end";
        DataHelper.Scripts["unlock_deeper"] = "local a={};a[1]=checkpointmanager.Condition('unlock_second') and 'O_second' or '~x';local r=choose(a);if r==1 then " + UnlockNpc + " end";
        ChoiceStorySource.ObserveScript(UnlockRoot + "luamanager.SetNextScript('unlock_topics') end", "intermediate_unlock.LuaScript"); Normal("O_entry", "사사형");
        Check(!Ready().Entries.Any(x => x.Title == "사사형"), "a true final topic cannot bypass a locked intermediate choice on its route");

        Reset(); var gate = Condition("unlock_first", 1);
        DataHelper.Scripts["unlock_topics"] = "local a={};a[1]=checkpointmanager.Condition('unlock_first') and 'talk+O_first+0+0' or '~x';local r=choose(a);if r==1 then " + UnlockNpc + " end";
        ChoiceStorySource.ObserveScript("local saved=checkpointmanager.Condition('unlock_first');statmodifymanager.SetFlag('unlock_first',1);local a={};a[1]=saved and 'talk+O_first+0+0' or '~x';local r=choose(a);if r==1 then " + UnlockNpc + " end", "unlock_topics.LuaScript");
        Break("talk+O_first+0+0", "실제로 열린 대화"); row = Linked(Ready(), "실제로 열린 대화");
        Check(row.AutoText.Length != 0, "an actually rendered active current choice is not rejected by reinterpreting an earlier captured guard");

        Reset(); var present = TooltipSwitch("unlock_random", true); Condition("unlock_first", 0);
        DataHelper.Scripts["unlock_topics"] = "local a={};a[1]=checkpointmanager.Condition('unlock_first') and 'O_first' or '~x';local r=choose(a);if r==1 then local s=checkpointmanager.Switch('unlock_random');if s==1 then " + UnlockNpc + " end end";
        ChoiceStorySource.ObserveScript(UnlockRoot + "luamanager.SetNextScript('unlock_topics') end", "unlocked_random.LuaScript"); Normal("O_entry", "사사형");
        // The native switch fixture includes a separate false presence input.
        Check(!Ready().Entries.Any(x => x.Title == "사사형"), "an unlocked choice still cannot bypass an independently false story prerequisite");

        present.State = 1; row = Linked(Read(), "사사형");
        Check(row.Status == "진행 가능성 있음" && row.AutoText.Length != 0,
            "a confirmed unlocked choice may retain an honestly uncertain internal random story without treating its unlock as uncertain");

        Reset();
        ChoiceStorySource.ObserveScript(UnlockRoot + UnlockNpc + " end", "disabled_unlock.LuaScript");
        Normal("~O_entry", "원래 비활성 선택지");
        Check(!Ready().Entries.Any(x => x.Title == "원래 비활성 선택지"),
            "the native disabled raw prefix cannot become an unlocked current choice through label normalization");

        UnlockMarkers();
    }

    private static void UnlockMarkers()
    {
        foreach (bool pending in new[] { false, true })
        {
            Reset(); var gate = Condition("unlock_first", 1);
            if (pending)
            {
                var missions = MissionManagerData._instance = new MissionManagerData();
                missions.ActiveSubMissions.Add("waiting", new MissionCheckData { name = "현재 대기", _startScript = "unlock_mission" });
                DataHelper.Scripts["unlock_mission"] = "";
            }
            else OncePool("unlock_once", new FlagData { name = "unlock_once", State = 0 });
            DataHelper.Scripts["unlock_topics"] = "local a={};a[1]=checkpointmanager.Condition('unlock_first') and 'O_first' or '~x';local r=choose(a);if r==1 then " +
                (pending ? "luamanager.SetNextScript('unlock_mission');luamanager.Init()" : "local p=checkpointmanager.Position('UnlockPool');luamanager.SetNextScript(p)") + " end";
            ChoiceStorySource.ObserveScript(UnlockRoot + "luamanager.SetNextScript('unlock_topics') end", "unlock_marker.LuaScript"); Normal("O_entry", "입구");
            Check(!WarmOpportunities().Entries.Any(x => x.Opportunity), "registered waiting/proven one-shot cannot mark a route through a locked future choice");
            gate.State = 0;
            Check(WarmOpportunities().Entries.Any(x => x.Title == "입구" && x.Opportunity), "an unlocked future menu preserves independently confirmed waiting/one-shot opportunity markers");
            CheckPointManager.Instance._condition._list.Clear();
            Check(!Read().Entries.Any(x => x.Opportunity), "a no-longer-readable future unlock removes its marker on the next scheduled read");
        }
    }
}
