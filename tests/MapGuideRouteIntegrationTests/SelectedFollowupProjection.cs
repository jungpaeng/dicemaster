using System;
using System.Collections.Generic;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;
using UnityEngine;

internal static partial class Program
{
    private static void SelectedFollowupProjection()
    {
        MapGuideSource.Clear(); OneShotProof.Clear(); DataHelper.Scripts.Clear();
        MapGuideSource.Enabled = () => true;
        var topic = new FlagData { name = "reset_topic", State = 1, _default = 0 };
        var persistent = new FlagData { name = "permanent_unlock", State = 0 };
        var manager = CheckPointManager.Instance = new CheckPointManager();
        var condition = IsZero(topic); condition._items.Add(IsZero(persistent)._items[0]);
        manager._condition._list.Add(new ConditionResultData { name = "topic_gate", _items = new List<ConditionResultItem> { condition } });
        var lua = new MoonSharp.Interpreter.Script();
        lua.m_GlobalTable.Values["checkpointmanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(manager) };
        lua.m_GlobalTable.Values["statmodifymanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(
            new StatModifyManager { _talkOptionFlag = new FlagCollectionData { _list = new List<FlagData> { topic } } }) };
        observedTalkEnvironment = new Fungus.LuaEnvironment(lua); PreviewConditions.ObserveLuaEnvironment(observedTalkEnvironment);
        DataHelper.Scripts["preview_room"] = "local a={'O_entry'};local r=choose(a);if r==1 then statmodifymanager.ResetTalkFlag();luamanager.SetNextScript('preview_topics');luamanager.Init() end";
        DataHelper.Scripts["preview_topics"] = "local a={};a[1]=checkpointmanager.Condition('topic_gate') and 'O_topic' or '~x';local r=choose(a);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_topic')) end";
        var controller = new PositionController { _positionData = new FreePositionData { name = "Room", _defaultScript = "preview_room" } };
        MapGuideSource.Select(controller);
        MapGuideSnapshot value = null;
        for (int at = 0; at < 40; at++) { Time.unscaledTime += .25f; value = (MapGuideSnapshot)MapGuideSource.Discover(true, false)[0].Read(); }
        Check(value.Summary.AutoText.Contains("경로 후보: O_entry → O_topic", StringComparison.Ordinal) && !value.Summary.Opportunity && !value.Summary.Available,
            "selected map place exposes a verified unlocked reset-dependent descendant as route-only guidance, never a marker or available event: " + value.Summary.AutoText + " / " + value.Summary.Status);
        int resources = DataHelper.Reads, parses = DiceMaster.LuaParse.LuaParser.Parses;
        persistent.State = 1; value = (MapGuideSnapshot)MapGuideSource.Discover(true, false)[0].Read();
        Check(!value.Summary.AutoText.Contains("경로 후보", StringComparison.Ordinal), "a locked persistent prerequisite removes the selected-place route");
        persistent.State = 0;
        // A readable unknown condition, not a malformed null native object.
        var random = IsZero(new FlagData()); random._items[0]._value1._value[0]._checkType = 7;
        controller._positionData._conditionFlags = new[] { new PositionConditionFlagItem { _condition = random,
            _flagItem = new[] { new PositionFlagItem { _flagData = persistent, _trueState = 0, _falseState = 1 } } } };
        value = (MapGuideSnapshot)MapGuideSource.Discover(true, false)[0].Read();
        Check(!value.Summary.AutoText.Contains("경로 후보", StringComparison.Ordinal), "an unresolved entry assignment stays unknown even when its present value passes; RNG and conditions are not executed");
        controller._positionData._conditionFlags[0]._flagItem[0]._flagData = new FlagData { name = "unrelated_visit" };
        value = (MapGuideSnapshot)MapGuideSource.Discover(true, false)[0].Read();
        Check(value.Summary.AutoText.Contains("경로 후보", StringComparison.Ordinal), "proven disjoint entry writes do not hide an otherwise verified route");
        controller._positionData._conditionFlags[0]._flagItem[0]._flagData = topic;
        value = (MapGuideSnapshot)MapGuideSource.Discover(true, false)[0].Read();
        Check(value.Summary.AutoText.Contains("경로 후보", StringComparison.Ordinal), "a later concrete talk reset safely overrides the earlier unknown write to that same reset-owned flag");
        controller._positionData._conditionFlags = null;
        value = (MapGuideSnapshot)MapGuideSource.Discover(true, false)[0].Read();
        Check(!value.Summary.AutoText.Contains("경로 후보", StringComparison.Ordinal), "an unreadable entry footprint cannot assume a persistent unlock is unchanged");
        controller._positionData._conditionFlags = Array.Empty<PositionConditionFlagItem>();
        controller._gameStatManager._stats._list[0]._value = 0;
        Check(!((MapGuideSnapshot)MapGuideSource.Discover(true, false)[0].Read()).Summary.AutoText.Contains("경로 후보", StringComparison.Ordinal), "insufficient action suppresses the default selected-place route");
        controller._gameStatManager._stats._list[0]._value = 3;
        controller._missionsManager.ActiveSubMissions.Add("first", new MissionCheckData { _startScript = "waiting" });
        Check(!((MapGuideSnapshot)MapGuideSource.Discover(true, false)[0].Read()).Summary.AutoText.Contains("경로 후보", StringComparison.Ordinal), "a real waiting mission takes priority over the default room dialogue");
        controller._missionsManager.ActiveSubMissions.Clear();
        for (int at = 0; at < 50; at++) MapGuideSource.Discover(true, false)[0].Read();
        Check(DataHelper.Reads == resources && DiceMaster.LuaParse.LuaParser.Parses == parses && topic.State == 1 && persistent.State == 0 && Counters.GameCalls == 0,
            "warm selected-place projection neither reloads/reparses Lua nor mutates flags, action or game state");
        PreviewConditions.ObserveLuaEnvironment(null); observedTalkEnvironment = null;
    }
}
