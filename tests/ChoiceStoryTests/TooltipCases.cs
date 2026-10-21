using System;
using System.Collections.Generic;
using System.Linq;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Story;
using UnityEngine.EventSystems;

internal static partial class Program
{
    private const string TooltipChoice = "local a={'practice+free+practice'};local r=choose(a);if r==1 then ";
    private const string TooltipNpc = "setcharacter(characters.Get('girl5'));say(luamanager.GetStoryText('D_tooltip'))";
    private const string TooltipGuard = "local s=checkpointmanager.Switch('tooltip_guard');if s==1 then ";
    private const string TooltipView = "runwait(flowcharts.LoadView('center'));getvar(flowcharts.view,'ViewName').value='center';runblock(flowcharts.view,'view');mainui.DisplayTitle('P_Center_01');setmenudialog(menudialogs.Center);";
    private static FlagData TooltipSwitch(string name, bool randomFirst)
    {
        var present = new FlagData { name = "tooltip_person_present", State = 0 };
        var random = new StatCompareItem { _compareOp = 3,
            _value1 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 7 } } },
            _value2 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = 50 } } } };
        var presence = new StatCompareItem {
            _value1 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 3, _flag = present } } },
            _value2 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = 1 } } } };
        var date = new StatCompareItem { _compareOp = 3,
            _value1 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = 1 } } },
            _value2 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = 2 } } } };
        CheckPointManager.Instance._switchList = new object[] { new SwitchResultConfig { _list = new List<SwitchResultData> {
            new SwitchResultData { name = name, _items = new List<ConditionResultItem> { new ConditionResultItem {
                _items = randomFirst ? new List<StatCompareItem> { random, presence, date } : new List<StatCompareItem> { presence, date, random }
            } } }
        } } };
        return present;
    }
    private static void TooltipConditions()
    {
        foreach (bool randomFirst in new[] { true, false })
        foreach (var sample in new[] {
            (Name: "plain", Prelude: "", Before: "", After: ""),
            (Name: "disjoint_write", Prelude: "", Before: "statmodifymanager.AddFlag('unrelated',1);", After: ""),
            (Name: "presentation", Prelude: TooltipView, Before: "", After: ""),
            (Name: "unrelated_cut", Prelude: "", Before: "", After: "unknown_callback();")
        })
        {
            Reset(); var present = TooltipSwitch("tooltip_guard", randomFirst);
            DisplayNames.Labels["Position/title/girl5"] = "우소매";
            ChoiceStorySource.ObserveScript(sample.Prelude + TooltipChoice + sample.Before + TooltipGuard + TooltipNpc + " end " + sample.After + " end", "tooltip_" + sample.Name + ".LuaScript");
            var option = Position("practice+free+practice", "단련"); EventSystem.current.currentSelectedGameObject = option.gameObject;
            var detail = Ready(true); var row = Linked(detail, "단련");
            Check(row.Status == "지금 진행 불가" && row.AutoText == "" && !row.Available && !row.Opportunity,
                "stable false NPC prerequisite survives random input and " + sample.Name + " in explicit detail");
            Check(!detail.Hints.Any(x => x.Title == "단련") && ChoiceStorySource.CurrentRow(detail) == null,
                "a detail-only blocked row cannot publish a hint or capture native pad focus");
            var child = new UnityEngine.GameObject(); child.transform.Parent = option.transform;
            EventSystem.current.currentSelectedGameObject = child;
            Check(ChoiceStorySource.CurrentRow(detail) == null, "a selected child cannot revive a blocked detail-only tooltip");
            int reads = DataHelper.Reads, parses = DiceMaster.LuaParse.LuaParser.Parses;
            for (int at = 0; at < 40; at++)
                Check(!Read().Entries.Any(x => x.Title == "단련") && !Read().Hints.Any(x => x.Title == "단련"),
                    "blocked NPC is absent from automatic rows and tooltip hints on warm refresh");
            Check(DataHelper.Reads == reads && DiceMaster.LuaParse.LuaParser.Parses == parses && present.State == 0 && Counters.GameCalls == 0,
                "negative suppression neither reloads/reparses Lua nor executes RNG, native judgment or a flag write");
            present.State = 1;
            row = Linked(Read(), "단련");
            Check(row.Status == "진행 가능성 있음" && !row.Available && !row.Opportunity && row.AutoText.Contains("우소매 대화", StringComparison.Ordinal),
                "fresh true presence restores a qualified random dialogue without certifying the random result");
            present.State = 0;
            Check(!Read().Entries.Any(x => x.Title == "단련"), "a fresh false presence removes the restored tooltip again without changing menu identity");
            Check(option.gameObject.activeInHierarchy && option.isActiveAndEnabled && option._source.text == "practice+free+practice",
                "suppressing an NPC tooltip preserves the native training action and its source label");
        }
        foreach (string effect in new[] { "statmodifymanager.SetFlag('tooltip_person_present',1);", "unknown_callback();" })
        {
            Reset(); var present = TooltipSwitch("tooltip_guard", true);
            ChoiceStorySource.ObserveScript(TooltipChoice + effect + TooltipGuard + TooltipNpc + " end end", "tooltip_affected.LuaScript");
            Position("practice+free+practice", "선행 변경 대화"); var row = Linked(Ready(), "선행 변경 대화");
            Check(row.Status == "진행 가능성 있음" && row.AutoText.Length != 0 && present.State == 0 && Counters.GameCalls == 0,
                "a write affecting the required input or arbitrary callback remains unknown instead of using an obsolete false value");
        }
        TooltipWitnesses();
        TooltipLargeBindings();
    }
    private static void TooltipWitnesses()
    {
        Reset(); var unavailable = Condition("tooltip_unavailable", 1); var available = Condition("tooltip_available", 0);
        DisplayNames.Labels["Position/title/girl5"] = "우소매";
        DataHelper.Scripts["tooltip_girl"] = TooltipNpc;
        DataHelper.Scripts["tooltip_brother"] = "setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_brother'))";
        ChoiceStorySource.ObserveScript(TooltipChoice + "if checkpointmanager.Condition('tooltip_unavailable') then luamanager.SetNextScript('tooltip_girl') elseif checkpointmanager.Condition('tooltip_available') then luamanager.SetNextScript('tooltip_brother') end end", "tooltip_witness.LuaScript");
        var option = Position("practice+free+practice", "단련"); EventSystem.current.currentSelectedGameObject = option.gameObject;
        var value = Ready(); var row = Linked(value, "단련");
        Check(row.Available && row.Status == "진행 가능" && row.AutoText.Contains("사사형 대화", StringComparison.Ordinal) && !row.AutoText.Contains("우소매", StringComparison.Ordinal) && !row.AutoText.Contains("외 ", StringComparison.Ordinal),
            "an eligible second NPC supplies both the displayed identity and readiness; a blocked first NPC supplies neither title nor count");
        Check(ReferenceEquals(ChoiceStorySource.CurrentRow(value), row) && !row.Opportunity, "witness selection preserves passive pad focus and does not turn general dialogue into an opportunity badge");
        int resources = DataHelper.Reads;
        unavailable.State = 0; row = Linked(Read(), "단련");
        Check(row.Available && row.AutoText.Contains("우소매 대화", StringComparison.Ordinal) && !row.AutoText.Contains("사사형", StringComparison.Ordinal),
            "a changed first branch switches the current witness rather than retaining another path's title");
        unavailable.State = 1; available.State = 1;
        Check(!Read().Entries.Any(x => x.Title == "단련"), "all false conversation witnesses remove the automatic tooltip");
        CheckPointManager.Instance._condition._list.RemoveAt(1); row = Linked(Read(), "단련");
        Check(row.Status == "진행 가능성 있음" && row.AutoText.Contains("사사형 대화", StringComparison.Ordinal) && !row.AutoText.Contains("우소매", StringComparison.Ordinal),
            "an unknown second witness cannot borrow the blocked first NPC's display identity");
        Check(DataHelper.Reads == resources && unavailable.State == 1 && available.State == 1 && Counters.GameCalls == 0,
            "witness transitions resample current data and reuse definitions without any native action");
    }
    private static void TooltipLargeBindings()
    {
        string inner = "local t=checkpointmanager.Switch('many_unknown');" + string.Join(" ", Enumerable.Range(1, 12).Select(i =>
            (i == 1 ? "if" : "elseif") + " t==" + i + " then " + TooltipNpc + ";")) + " end";
        string branches = "local g=checkpointmanager.Switch('many_group');" + string.Join(" ", Enumerable.Range(1, 3).Select(i =>
            (i == 1 ? "if" : "elseif") + " g==" + i + " then luamanager.SetNextScript('tooltip_many_" + i + "')")) + " end";
        foreach (bool bypass in new[] { false, true })
        {
            Reset(); Condition("many_npc", 1);
            for (int i = 1; i <= 3; i++) DataHelper.Scripts["tooltip_many_" + i] = inner;
            ChoiceStorySource.ObserveScript(TooltipChoice + "if checkpointmanager.Condition('many_npc') then " + branches +
                (bypass ? " else setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_bypass'))" : "") + " end end", "many_tooltip_paths.LuaScript");
            Position("practice+free+practice", "많은 대화"); var value = Ready(true); var row = Linked(value, "많은 대화");
            Check(row.Detail.Contains("32개 초과", StringComparison.Ordinal), "the large-binding fixture actually observes more than 32 paths within the existing per-branch and context limits");
            if (!bypass)
                Check(row.Status == "지금 진행 불가" && row.AutoText == "" && !Read().Entries.Any(x => x.Title == "많은 대화"),
                    "a prepared common false prerequisite suppresses more than 32 observed paths without expanding current-query bounds");
            else
                Check(row.Status != "지금 진행 불가" && row.AutoText.Length != 0 && Read().Entries.Any(x => x.Title == "많은 대화"),
                    "an independently observed unlocked bypass prevents the first 32 blocked paths from suppressing its actual witness");
            int reads = DataHelper.Reads;
            for (int at = 0; at < 40; at++) Read();
            Check(DataHelper.Reads == reads && Counters.GameCalls == 0,
                "large-binding prerequisites are cached definitions rather than source scans or game execution during warm refresh");
        }
    }
}
