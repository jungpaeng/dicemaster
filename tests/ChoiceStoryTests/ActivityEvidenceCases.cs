using System;
using System.Collections.Generic;
using System.Linq;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Story;

internal static partial class Program
{
    private static object EntryEnvironment()
    {
        var manager = new StatModifyManager { _talkOptionFlag = new FlagCollectionData() };
        var script = new MoonSharp.Interpreter.Script();
        script.m_GlobalTable.Values["statmodifymanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(manager) };
        var environment = new Fungus.LuaEnvironment(script); PreviewConditions.ObserveLuaEnvironment(environment); return environment;
    }
    private static void ActivityEvidence()
    {
        Reset(); var count = EntryCounter("renamed_entry", 1);
        var gate = PreviewConditions.Parse("checkpointmanager.Switch('renamed_entry') == 1");
        var reader = PreviewConditions.CreateReader(); var writes = new[] { "unrelated" };
        Check(reader.ReadFlagWriteIndependence(gate, writes, null, false) == true && reader.Current(gate) == PreviewConditionState.Unknown,
            "integer count tables prove flag independence without evaluating plain Switch or increasing its stored count");
        count._countItems = null;
        Check(!PreviewConditions.CreateReader().ReadFlagWriteIndependence(gate, writes, null, false).HasValue,
            "missing count metadata is unknown dependency evidence, not a consumed story condition");
        var consumed = Condition("consumed_guard", 0);
        gate = PreviewConditions.Parse("checkpointmanager.Condition('consumed_guard')");
        reader = PreviewConditions.CreateReader();
        Check(reader.ReadFlagWriteIndependence(gate, new[] { "consumed_guard" }, null, false) == false && reader.Current(gate) == PreviewConditionState.Met,
            "known consumed eligibility differs from unknown data and restores masks for the next current read");
        Check(!reader.ReadFlagWriteIndependence(PreviewConditions.Parse("checkpointmanager.Condition('missing')"), writes, null, false).HasValue,
            "missing eligibility definitions are not treated as evidence of an individual story");

        Reset(); count = EntryCounter("renamed_entry", 1);
        var config = (SwitchResultConfig)CheckPointManager.Instance._switchList[0];
        config._list.Add(new SwitchResultData { name = "service_guard", _items = new List<ConditionResultItem> { IsZero(new FlagData { name = "disjoint", State = 0 }) } });
        const string speaker = "setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_fixed'));";
        DataHelper.Scripts["fixed_activity"] = speaker + "local s=checkpointmanager.Switch('service_guard');if s==1 then statmodifymanager.Player('money',-100,'',1);luamanager.SetCurrentStoryScript('activity_result');luamanager.ChangeScene('Combat','combat','Story') elseif s==2 then luamanager.ChangeScene('Free','','') end";
        DataHelper.Scripts["activity_result"] = "";
        DataHelper.Scripts["topic_menu"] = "local a={'O_spar'};local r=choose(a);if r==1 then statmodifymanager.SetFlag('unrelated',1);luamanager.SetNextScript('fixed_activity');luamanager.Init() end";
        string root = "local a={'O_entry'};local r=choose(a);if r==1 then local first=checkpointmanager.Switch('renamed_entry');if first==1 then " + EntryNpc + " end;luamanager.SetNextScript('topic_menu');luamanager.Init() end";
        ChoiceStorySource.ObserveScript(root, "counter_then_activity.LuaScript"); Normal("O_entry", "진입 후 반복 활동");
        for (int at = 0; at < 160; at++)
            Check(!Read().Entries.Any(x => x.Title == "진입 후 반복 활동" && x.AutoText.Length != 0),
                "no preparation or final refresh revives completed entry or fixed service through its count predicate");
        var detail = Linked(Read(true), "진입 후 반복 활동");
        Check(detail.AutoText == "" && detail.Detail.Contains("제외 표본", StringComparison.Ordinal) && detail.Detail.Contains("D_", StringComparison.Ordinal),
            "detail-only excluded evidence identifies a real script/story witness instead of only repeating a path label");
        count._count = 0;
        var remaining = Linked(Read(true), "진입 후 반복 활동");
        Check(remaining.AutoText.Length != 0 && remaining.Detail.Contains("표시 근거: counter_then_activity · D_entry", StringComparison.Ordinal) && remaining.Detail.Contains("Switch renamed_entry", StringComparison.Ordinal),
            "a remaining introduction has the exact selected story key and readonly checkpoint trace only in explicit detail");
        count._count = 1; count._countItems = null;
        Check(!Read().Entries.Any(x => x.Title == "진입 후 반복 활동" && x.AutoText.Length != 0),
            "unreadable completion plus unknown activity independence cannot manufacture another possible conversation");
        count._countItems = new List<ValueCompareItem> { new ValueCompareItem { _compareOp = 0, _value = 1 } };
        int resources = DataHelper.Reads, parses = DiceMaster.LuaParse.LuaParser.Parses;
        for (int at = 0; at < 60; at++) Read();
        Check(DataHelper.Reads == resources && DiceMaster.LuaParse.LuaParser.Parses == parses && count._count == 1 && consumed.State == 0 && Counters.GameCalls == 0,
            "all ordinary refreshes retain warm definitions and perform no Lua/evaluator/RNG/combat/count/flag mutation");
    }
}

// Observed raw-table boundaries only. No Lua interpreter or reset executes.
namespace Mortal.Core { public sealed class FlagCollectionData { public List<FlagData> _list = new List<FlagData>(); } }
namespace Mortal.Story { public sealed class StatModifyManager { public Mortal.Core.FlagCollectionData _talkOptionFlag; } }
namespace MoonSharp.Interpreter
{
    public sealed class Script { public Table m_GlobalTable = new Table(); }
    public sealed class Table { public Dictionary<string, DynValue> Values = new Dictionary<string, DynValue>(); public DynValue RawGet(string key) { Values.TryGetValue(key, out var value); return value; } }
    public sealed class DynValue { public object m_Object; }
    public sealed class UserData { public object Object { get; private set; } public UserData(object value) { Object = value; } }
}
namespace Fungus { public sealed class LuaEnvironment { private MoonSharp.Interpreter.Script interpreter; public LuaEnvironment(MoonSharp.Interpreter.Script value) { interpreter = value; } } }
