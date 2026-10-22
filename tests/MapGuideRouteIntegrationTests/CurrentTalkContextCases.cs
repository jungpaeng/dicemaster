using System;
using System.Collections.Generic;
using System.Reflection;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Story;

internal static partial class Program
{
    private static Fungus.LuaEnvironment CurrentTalkEnvironment(FlagCollectionData flags, object checkpoints = null)
    {
        var script = new MoonSharp.Interpreter.Script();
        script.m_GlobalTable.Values["checkpointmanager"] = new MoonSharp.Interpreter.DynValue {
            m_Object = new MoonSharp.Interpreter.UserData(checkpoints ?? CheckPointManager.Instance) };
        script.m_GlobalTable.Values["statmodifymanager"] = new MoonSharp.Interpreter.DynValue {
            m_Object = new MoonSharp.Interpreter.UserData(new StatModifyManager { _talkOptionFlag = flags }) };
        return new Fungus.LuaEnvironment(script);
    }
    private static void CurrentTalkContextCases()
    {
        ScanFixture(1, 1);
        var flag = new FlagData { name = "current_context_topic", State = 8, _default = 0 };
        var manager = CheckPointManager.Instance;
        manager._condition._list.Add(new ConditionResultData { name = "context_topic", _items = new List<ConditionResultItem> { IsZero(flag) } });
        var gate = PreviewConditions.CaptureAfterTalkReset(PreviewConditions.Parse("checkpointmanager.Condition('context_topic')"), null, false);
        var flags = new FlagCollectionData { _list = new List<FlagData> { flag } };
        var old = CurrentTalkEnvironment(new FlagCollectionData());
        PreviewConditions.ObserveLuaEnvironment(old);
        LuaManager.Instance = new LuaManager { _luaEnvironment = CurrentTalkEnvironment(flags) };
        PreviewConditions.ObserveLuaEnvironment(new Fungus.LuaEnvironment(new MoonSharp.Interpreter.Script()));
        Check(PreviewConditions.CreateRouteReader().Current(gate) == PreviewConditionState.Met,
            "current serialized game Lua owner recovers a reset-dependent topic after an unrelated observation cleared the weak observer");
        PreviewConditions.ClearTalkContext(); GC.Collect(); GC.WaitForPendingFinalizers();
        Check(PreviewConditions.CreateRouteReader().Current(gate) == PreviewConditionState.Met && flag.StateReads == 0,
            "current game ownership survives observer loss and managed GC without sampling the reset flag's present State");
        Check(PreviewConditions.DescribeTalkResetSource().Contains("현재 게임 Lua 연결", StringComparison.Ordinal),
            "explicit map detail identifies the authoritative reset source independently of a common-path unknown condition");
        flag._default = 1;
        Check(PreviewConditions.CreateRouteReader().Current(gate) == PreviewConditionState.NotMet,
            "canonical reset defaults are sampled anew, so completed or locked topics are still removed");
        flag._default = 0;
        PreviewConditions.ObserveLuaEnvironment(CurrentTalkEnvironment(flags));
        LuaManager.Instance._luaEnvironment = CurrentTalkEnvironment(flags, new CheckPointManager());
        Check(PreviewConditions.CreateRouteReader().Current(gate) == PreviewConditionState.Unknown,
            "a canonical environment bound to different checkpoints cannot borrow an older valid observer");
        LuaManager.Instance._luaEnvironment = null;
        Check(PreviewConditions.CreateRouteReader().Current(gate) == PreviewConditionState.Unknown,
            "a present current owner with no initialized environment remains unknown instead of falling back");
        LuaManager.Instance._luaEnvironment = CurrentTalkEnvironment(flags);
        Check(PreviewConditions.CreateRouteReader().Current(gate) == PreviewConditionState.Met,
            "late current-environment initialization recovers on the next fresh read without a game reload");
        var observed = CurrentTalkEnvironment(flags);
        LuaManager.Instance = null; PreviewConditions.ObserveLuaEnvironment(observed);
        var observedScript = (MoonSharp.Interpreter.Script)typeof(Fungus.LuaEnvironment).GetField("interpreter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(observed);
        observedScript.m_GlobalTable.Values["checkpointmanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(new CheckPointManager()) };
        Check(PreviewConditions.CreateRouteReader().Current(gate) == PreviewConditionState.Unknown,
            "fallback raw registrations are revalidated, so rebinding the same live environment cannot bypass ownership checks");
        LuaManager.Instance = null; PreviewConditions.ObserveLuaEnvironment(old);
        CheckPointManager.Instance = new CheckPointManager { _condition = manager._condition };
        Check(PreviewConditions.CreateRouteReader().Current(gate) == PreviewConditionState.Unknown,
            "a fallback observation cannot cross checkpoint/session ownership even with identical condition definitions");
        Check(flag.State == 8 && flag.StateWrites == 1 && Counters.GameCalls == 0,
            "current-context recovery executes no game evaluator, Lua, reset or temporary flag writes");
        PreviewConditions.ClearTalkContext();
    }
}

namespace Mortal.Story
{
    // Same existing-owner and serialized field boundary as the supplied game.
    public sealed class LuaManager : UnityEngine.Object
    {
        public static LuaManager Instance { get; set; }
        public Fungus.LuaEnvironment _luaEnvironment;
    }
}
