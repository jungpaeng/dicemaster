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
    private static void ColdMapDefinitionCases()
    {
        MapGuideSource.Clear(); MapDefinitionAssets.Clear(); PreviewConditions.ClearTalkContext();
        DataHelper.Scripts.Clear(); DataHelper.ExternalReader = null;
        MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = _ => true;
        CheckPointManager.Instance = null; LuaManager.Instance = null;
        Resources.Loaded.Clear(); MapDefinitionAssets.Install();
        var persistent = new FlagData { name = "persistent_access", State = 0 };
        var topic = new FlagData { name = "temporary_topic", State = 7, _default = 0 };
        var predicate = IsZero(persistent); predicate._items.Add(IsZero(topic)._items[0]);
        var condition = new ConditionResultData { name = "cold_topic", _items = new List<ConditionResultItem> { predicate } };
        var conditions = new ConditionResultConfig { _list = new List<ConditionResultData> { condition } };
        var switches = new SwitchResultConfig { _list = new List<SwitchResultData> {
            new SwitchResultData { name = "ordinary", _items = new List<ConditionResultItem> { IsZero(persistent) } },
            new SwitchResultData { name = "counted", _useCount = true, _count = 0, _countItems = new List<ValueCompareItem> { new ValueCompareItem { _value = 1 } } } } };
        var pool = new PositionResultData { name = "cold_pool", _scriptPrefix = "cold_result", _list = new List<PositionEventData> {
            new PositionEventData { name = "cold event", _toggleCondition = false } } };
        var positions = new PositionResultConfig { _list = new List<PositionResultData> { pool } };
        var reset = new FlagCollectionData { _list = new List<FlagData> { topic } };
        var modifier = new StatModifyManager { _talkOptionFlag = reset };
        Resources.Loaded[typeof(ConditionResultConfig)] = new object[] { conditions };
        Resources.Loaded[typeof(SwitchResultConfig)] = new object[] { switches };
        Resources.Loaded[typeof(PositionResultConfig)] = new object[] { positions };
        Resources.Loaded[typeof(StatModifyManager)] = new object[] { modifier };
        DataHelper.Scripts["cold_root"] = "statmodifymanager.ResetTalkFlag();local a={};a[1]=checkpointmanager.Condition('cold_topic') and 'O_cold_topic' or '~x';local r=choose(a);if r==1 then setcharacter(characters.Get('person'));say(luamanager.GetStoryText('D_cold')) end";
        var place = new PositionController { _positionData = new FreePositionData { name = "ColdRoom", _defaultScript = "cold_root" } };
        MapGuideSource.Register(place);
        try
        {
            MapMarkerSnapshot marker = null;
            for (int at = 0; at < 40; at++) marker = GuidanceMarkers();
            Check(marker.Entries.Count == 1 && marker.Entries[0].Guidance && !marker.Entries[0].Opportunity && CheckPointManager.Instance == null && LuaManager.Instance == null,
                "a fresh process loading directly into the map discovers a reset-dependent dialogue from loaded definitions without creating Story managers");
            var source = PreviewConditions.DescribeTalkResetSource();
            Check(source.Contains("로드된 대화 초기화 설정", StringComparison.Ordinal), "details distinguish the cold loaded-definition reset source from a current Lua environment");
            int searches = Resources.Searches, reads = DataHelper.Reads;
            for (int at = 0; at < 60; at++) marker = GuidanceMarkers();
            Check(marker.Entries.Count == 1 && searches == Resources.Searches && reads == DataHelper.Reads,
                "warm cold-load markers reread current values with zero native asset scans and zero Lua rereads");
            persistent.State = 1; marker = GuidanceMarkers();
            Check(marker.Entries.Count == 0, "a persistent false prerequisite still removes the marker when a reset-owned temporary flag becomes virtually true");
            persistent.State = 0; marker = GuidanceMarkers();
            Check(marker.Entries.Count == 1 && topic.State == 7, "restored current access reappears without resetting the actual topic flag");
            var gate = PreviewConditions.Parse("checkpointmanager.Condition('cold_topic')");
            Check(PreviewConditions.CreateReader().Current(gate) == PreviewConditionState.Unknown,
                "strict readers and opportunity/one-shot judgments cannot borrow cold route-only definitions");
            var route = PreviewConditions.CreateRouteReader();
            Check(route.Current(PreviewConditions.Parse("checkpointmanager.Switch('ordinary')==1")) == PreviewConditionState.Met,
                "cold non-counted Switch definitions use current inputs without executing the native Switch");
            bool ignored;
            var countGate = PreviewConditions.ScopeNextSwitch(PreviewConditions.Parse("checkpointmanager.Switch('counted')==1"), true, out ignored);
            Check(PreviewConditions.CreateRouteReader().Current(countGate) == PreviewConditionState.Unknown,
                "a cold loaded counter asset never invents the execution count of a not-yet-created Story manager");
            Check(MapGuideSource.ReadChoicePool("cold_pool").Count == 1, "cold route preparation resolves native event pools from the same loaded metadata boundary");
            conditions._list.Add(new ConditionResultData { name = "cold_topic", _items = new List<ConditionResultItem> { IsZero(new FlagData { State = 1 }) } });
            // One config has native first-match semantics, even on the asset path.
            Check(PreviewConditions.CreateRouteReader().Current(PreviewConditions.CaptureAfterTalkReset(gate, null, false)) == PreviewConditionState.Met,
                "duplicate rows inside one config preserve the native first-match ordering");
            conditions._list.RemoveAt(1);
            var duplicate = new ConditionResultConfig { _list = new List<ConditionResultData> { new ConditionResultData { name = "cold_topic", _items = new List<ConditionResultItem> { IsZero(persistent) } } } };
            Resources.Loaded[typeof(ConditionResultConfig)] = new object[] { conditions, duplicate }; MapDefinitionAssets.Clear();
            Check(PreviewConditions.CreateRouteReader().Current(PreviewConditions.CaptureAfterTalkReset(gate, null, false)) == PreviewConditionState.Unknown,
                "conflicting keys across loaded configurations remain unknown rather than accepting resource enumeration order");
            Resources.Loaded[typeof(ConditionResultConfig)] = new object[] { conditions }; MapDefinitionAssets.Clear();
            var second = new StatModifyManager { _talkOptionFlag = new FlagCollectionData() };
            Resources.Loaded[typeof(StatModifyManager)] = new object[] { modifier, second };
            Check(PreviewConditions.CreateRouteReader().Current(PreviewConditions.CaptureAfterTalkReset(gate, null, false)) == PreviewConditionState.Unknown,
                "different loaded reset footprints cannot be merged or guessed from flag names");
            Resources.Loaded[typeof(StatModifyManager)] = Array.Empty<object>(); MapDefinitionAssets.Clear();
            marker = GuidanceMarkers(); searches = Resources.Searches;
            for (int at = 0; at < 10; at++) marker = GuidanceMarkers();
            Check(marker.Entries.Count == 0 && Resources.Searches == searches, "missing cold assets retry at five seconds, not every healthy quarter-second query");
            Resources.Loaded[typeof(StatModifyManager)] = new object[] { modifier };
            for (int at = 0; at < 24; at++) marker = GuidanceMarkers();
            Check(marker.Entries.Count == 1, "late already-loaded metadata recovers after its retry window without a scene reload");
            var current = CheckPointManager.Instance = new CheckPointManager();
            current._condition = new ConditionResultConfig { _list = new List<ConditionResultData> { new ConditionResultData { name = "cold_topic", _items = new List<ConditionResultItem> { IsZero(new FlagData { State = 1 }) } } } };
            Check(PreviewConditions.CreateRouteReader().Current(gate) == PreviewConditionState.NotMet,
                "an actual current Story owner is authoritative over loaded fallback definitions");
            LuaManager.Instance = new LuaManager { _luaEnvironment = null };
            Check(PreviewConditions.CreateRouteReader().Current(PreviewConditions.CaptureAfterTalkReset(gate, null, false)) == PreviewConditionState.Unknown,
                "a present but broken current Lua owner cannot bypass its failure by borrowing cold reset metadata");
            current.Destroyed = true; LuaManager.Instance.Destroyed = true;
            Check(PreviewConditions.CreateRouteReader().Current(PreviewConditions.CaptureAfterTalkReset(gate, null, false)) == PreviewConditionState.Met,
                "destroyed Unity owners retained by static CLR fields do not prevent a new cold metadata lookup");
            Resources.Loaded[typeof(PositionResultConfig)] = Array.Empty<object>();
            MapDefinitionAssets.Clear(); MapGuideSource.ClearSnapshots();
            for (int at = 0; at < 30; at++) marker = GuidanceMarkers();
            reads = DataHelper.Reads;
            for (int at = 0; at < 60; at++) marker = GuidanceMarkers();
            Check(marker.Entries.Count == 1 && DataHelper.Reads == reads,
                "an unused missing event-pool catalog has a stable absent identity across retries and cannot repeatedly restart a dialogue-only route");
            Check(topic.State == 7 && topic.StateWrites == 1 && persistent.StateWrites == 3 && Counters.GameCalls == 0,
                "cold recovery executes no Lua, reset, condition, RNG or gameplay mutation; only explicit fixture inputs were assigned");
            GC.KeepAlive(place); GC.KeepAlive(modifier);
        }
        finally
        {
            MapDefinitionAssets.Clear(); MapGuideSource.Clear(); PreviewConditions.ClearTalkContext(); Resources.Loaded.Clear();
            PreviewConditions.LoadedDefinitionReader = null; PreviewConditions.ExistingOwnerAlive = null;
            MapGuideSource.MarkerVisible = null; CheckPointManager.Instance = new CheckPointManager(); LuaManager.Instance = null;
        }
    }
}
