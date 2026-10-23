using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;
using UnityEngine;

internal static partial class Program
{
    private static IList RegisteredPlaces() => (IList)typeof(MapGuideSource)
        .GetField("places", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

    private static void MapPreparationSchedulingCases()
    {
        MapGuideSource.Clear(); MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = _ => true;
        var hidden = new PositionController(); hidden.gameObject.activeInHierarchy = false;
        MapGuideSource.Register(hidden);
        var live = new List<PositionController>();
        for (int visit = 0; visit < 100; visit++)
        {
            foreach (var controller in live) controller.Destroyed = true;
            live.Clear(); MapGuideSource.ClearSnapshots();
            for (int at = 0; at < 11; at++)
            { var controller = new PositionController(); live.Add(controller); MapGuideSource.Register(controller); }
            Check(RegisteredPlaces().Count == 12, "scene recreation releases destroyed registrations while retaining hidden live controls");
        }
        MapGuideSource.Select(live[0]);
        var stale = MapGuideSource.Discover(true, false)[0];
        live[0].Destroyed = true;
        MapGuideSource.Register(live[1]);
        Check(MapGuideSource.SelectedAnchor == null && !MapGuideSource.IsCurrent(stale.Id),
            "pruning a destroyed selected controller invalidates its selection and publication identity");
        bool rejected = false;
        try { stale.Read(); } catch (DiceMaster.LiveLog.DataNotReadyException) { rejected = true; }
        Check(rejected, "a captured query cannot publish after its native controller was destroyed");
        hidden.gameObject.activeInHierarchy = true; MapGuideSource.Select(hidden);
        Check(ReferenceEquals(MapGuideSource.SelectedAnchor, hidden) && RegisteredPlaces().Count == 11,
            "a retained hidden controller can reopen without duplicate registration");
        GC.KeepAlive(live); GC.KeepAlive(hidden);
        MapGuideSource.Clear(); MapGuideSource.MarkerVisible = null;
        RetryQueueScheduling();
        FocusedPreparationScheduling();
    }
    private static PositionController SchedulingPlace(PositionController owner, string script)
    {
        var controller = new PositionController { _positionData = new FreePositionData { name = script, _defaultScript = script },
            _missionsManager = owner._missionsManager, _gameStatManager = owner._gameStatManager, _freeManager = owner._freeManager };
        MapGuideSource.Register(controller); return controller;
    }
    private static MapRouteDefinitions SchedulingRoutes(PositionController controller)
    {
        foreach (var place in RegisteredPlaces())
            if (ReferenceEquals(((WeakReference)Private(place, "Controller")).Target, controller))
            { var source = Private(place, "Definition"); return source == null ? null : (MapRouteDefinitions)Private(source, "Routes"); }
        return null;
    }
    private static void SchedulingReset()
    {
        MapGuideSource.Clear(); MapDefinitionAssets.Clear(); DiceMaster.LiveLog.PreviewConditions.ClearTalkContext();
        MapGuideSource.Enabled = () => true; MapGuideSource.MarkerVisible = _ => true;
        DataHelper.Scripts.Clear(); DataHelper.ExternalReader = null; Time.unscaledTime = 0;
        CheckPointManager.Instance = new CheckPointManager(); LuaManager.Instance = null;
    }
    private static void SchedulingGate(string key)
    { CheckPointManager.Instance._condition._list.Add(new ConditionResultData { name = key, _items = new List<ConditionResultItem> { IsZero(new FlagData { name = key }) } }); }
    private static string SchedulingChoice(string gate, string body) =>
        "local a={};a[1]=checkpointmanager.Condition('" + gate + "') and 'O_" + gate + "' or '~x';local r=choose(a);if r==1 then " + body + " end";
    private const string SchedulingTalk = "setcharacter(characters.Get('person'));say(luamanager.GetStoryText('D_scheduling'))";
    private static MapMarkerSnapshot SchedulingRead(bool focus, bool markers = true)
    {
        Time.unscaledTime += .25f; int reads = DataHelper.Reads, work = AllRouteWork();
        MapMarkerSnapshot value = null;
        MapGuideSource.BeginScheduledReads();
        try
        {
            if (focus) MapGuideSource.Discover(true, true)[0].Read();
            if (markers) value = (MapMarkerSnapshot)MapGuideSource.DiscoverMarkers(true, true)[0].Read();
        }
        finally { MapGuideSource.EndScheduledReads(); }
        Check(DataHelper.Reads - reads <= 1 && AllRouteWork() - work <= 8192,
            "focus, background and deferred completion share one cold read and 8192 work including EndScheduledReads");
        return value;
    }
    private static void RetryQueueScheduling()
    {
        SchedulingReset(); var owner = new PositionController(); var keep = new List<PositionController>();
        for (int at = 0; at < 8; at++) keep.Add(SchedulingPlace(owner, "retry_missing_" + at));
        for (int at = 0; at < 3; at++)
        {
            string key = "retry_good_" + at; SchedulingGate(key);
            DataHelper.Scripts[key] = SchedulingChoice(key, at < 2 ? "luamanager.SetNextScript('retry_good_" + (at + 1) + "');luamanager.Init()" : SchedulingTalk);
        }
        var good = SchedulingPlace(owner, "retry_good_0"); keep.Add(good);
        var attempts = new Dictionary<string, float>(); int tooEarly = 0;
        DataHelper.ExternalReader = key => {
            if (key.StartsWith("retry_missing_", StringComparison.Ordinal))
            { if (attempts.TryGetValue(key, out float prior) && Time.unscaledTime - prior < 5) tooEarly++; attempts[key] = Time.unscaledTime; }
            return DataHelper.Scripts.TryGetValue(key, out string text) ? new ScriptReadResult { State = ScriptReadState.Success, Text = text } : new ScriptReadResult { State = ScriptReadState.Waiting };
        };
        int first = 0;
        for (int at = 1; at <= 50; at++)
            if (SchedulingRead(false).Entries.Any(row => ReferenceEquals(row.Anchor, good)) && first == 0) first = at;
        Check(first > 0 && first < 19, "eight retry-waiting roots cannot waste eight extra screen turns before a short valid peer");
        Check(tooEarly == 0 && attempts.Count == 8, "skipping blocked slots neither retries missing Lua early nor forgets failed places");
        GC.KeepAlive(keep); MapGuideSource.Clear(); DataHelper.ExternalReader = null;
    }
    private static void FocusedPreparationScheduling()
    {
        SchedulingReset(); var owner = new PositionController();
        var text = new System.Text.StringBuilder("local a={};");
        for (int at = 1; at <= 16; at++)
        { SchedulingGate("focus_" + at); text.Append("a[" + at + "]=checkpointmanager.Condition('focus_" + at + "') and 'O_" + at + "' or '~x';"); }
        text.Append("local r=choose(a);");
        for (int at = 1; at <= 16; at++)
        {
            text.Append((at == 1 ? "if " : "elseif ") + "r==" + at + " then ");
            text.Append(at == 1 ? SchedulingTalk : "luamanager.SetNextScript('focus_tail_" + at + "');luamanager.Init()");
            text.Append(' '); DataHelper.Scripts["focus_tail_" + at] = SchedulingTalk;
        }
        DataHelper.Scripts["focus_root"] = text.Append("end").ToString();
        SchedulingGate("peer"); DataHelper.Scripts["peer"] = SchedulingChoice("peer", SchedulingTalk);
        var focus = SchedulingPlace(owner, "focus_root"); var peer = SchedulingPlace(owner, "peer");
        MapGuideSource.Select(focus); int first = 0, completed = 0;
        for (int at = 1; at <= 60; at++)
        {
            var snapshot = SchedulingRead(true);
            if (first == 0 && snapshot.Entries.Any(row => ReferenceEquals(row.Anchor, peer))) first = at;
            if (completed == 0 && SchedulingRoutes(focus)?.HasPendingWork == false) completed = at;
        }
        Check(first > 0 && first < 8, "a focused place with a valid witness yields before exploring fifteen unrelated tails");
        Check(completed > 0, "a successful guidance witness does not permanently stop selected proof and route preparation");
        var peers = new List<PositionController>();
        for (int at = 0; at < 12; at++) peers.Add(SchedulingPlace(owner, "peer"));
        MapGuideSource.ClearSnapshots(); completed = 0;
        for (int at = 1; at <= 65; at++)
        { SchedulingRead(true); if (completed == 0 && SchedulingRoutes(focus)?.HasPendingWork == false) completed = at; }
        Check(completed > 0, "many runnable peers cannot permanently starve the focused place after its first witness");
        MapGuideSource.ClearSnapshots(); SchedulingRead(true, false);
        int beforeExpired = SchedulingRoutes(focus).WorkCount;
        for (int at = 0; at < 4; at++)
        {
            Time.unscaledTime += .25f; MapGuideSource.BeginScheduledReads();
            MapGuideSource.Discover(true, true)[0].Read();
            var expired = (MapRouteDefinitions.PreparationSlice)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(MapRouteDefinitions.PreparationSlice));
            typeof(MapGuideSource).GetField("scheduledSlice", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, expired);
            MapGuideSource.EndScheduledReads();
        }
        SchedulingRead(true, false);
        Check(SchedulingRoutes(focus).WorkCount > beforeExpired,
            "four exhausted background deadlines cannot indefinitely postpone selected preparation");
        // A non-due marker engine must not throw away the selected slot.
        MapGuideSource.ClearSnapshots();
        for (int at = 0; at < 50; at++) SchedulingRead(true, false);
        Check(SchedulingRoutes(focus)?.HasPendingWork == false, "selected preparation completes when the marker engine is not due at all");
        foreach (bool reset in new[] { false, true })
        {
            MapGuideSource.ClearSnapshots(); bool deferred = false;
            for (int at = 0; at < 5; at++)
            {
                Time.unscaledTime += .25f; MapGuideSource.BeginScheduledReads();
                MapGuideSource.Discover(true, true)[0].Read();
                deferred = typeof(MapGuideSource).GetField("deferredSelected", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) != null;
                if (deferred) break;
                MapGuideSource.EndScheduledReads();
            }
            int reads = DataHelper.Reads;
            if (reset) MapGuideSource.ClearSnapshots(); else focus.gameObject.activeInHierarchy = false;
            MapGuideSource.EndScheduledReads();
            Check(deferred && reads == DataHelper.Reads, "hidden or invalidated selected work cannot resume at the end of an old scheduled scope");
            focus.gameObject.activeInHierarchy = true;
        }
        GC.KeepAlive(peers);
        GC.KeepAlive(focus); GC.KeepAlive(peer); MapGuideSource.Clear(); MapGuideSource.MarkerVisible = null;
        Check(Counters.GameCalls == 0, "scheduling never executes gameplay judgments or effects");
    }
}
