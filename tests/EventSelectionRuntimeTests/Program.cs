using System;
using System.Collections;
using System.Reflection;
using DiceMaster.EventSelection;
using MoonSharp.Interpreter;
using Mortal.Story;
using UnityEngine;

internal static class Program
{
    private static int assertions, scenarios;
    private static object coroutine;
    private static PositionResultData pool;
    private static long script;
    private static void Check(bool condition, string message)
    { assertions++; if (!condition) throw new Exception(message); }
    private static PositionEventData Event(string name, bool eligible = true, int rate = 10)
    { return new PositionEventData { name = name, ResultType = name, Eligible = eligible, Rate = rate }; }
    private static void Begin(params PositionEventData[] targets)
    {
        scenarios++;
        EventSelectionRuntime.Invalidate("test");
        EventSelectionRuntime.Enabled = () => true;
        EventSelectionRuntime.WaitingChanged = null;
        Time.unscaledTime = 0;
        pool = new PositionResultData { name = "action", _scriptPrefix = "story" };
        pool.List.AddRange(targets);
        CheckPointManager.Instance = new CheckPointManager { _position = new PositionResultConfig() };
        CheckPointManager.Instance._position.List.Add(pool);
        coroutine = new object();
        script = EventSelectionRuntime.NewScript();
    }
    private static bool Wait() => EventSelectionRuntime.Wait("action", script, 1, coroutine, EventSelectionRuntime.Epoch);
    private static EventSelectionRuntime.ActionContextHandle Enter()
        => EventSelectionRuntime.BeginPosition(CheckPointManager.Instance, "action");
    private static void Normal()
    {
        var a = Event("a"); var b = Event("b"); Begin(a, b);
        Check(Wait() && EventSelectionRuntime.Waiting, "actual invocation enters wait");
        Check(a.RealCalls + b.RealCalls == 0, "admission does not execute conditions");
        string id = EventSelectionRuntime.Candidates[1].Id;
        Check(EventSelectionRuntime.Select(id, false), "fresh normal request");
        Check(!EventSelectionRuntime.Select(id, false), "same frame duplicate rejected");
        Check(!Wait() && !EventSelectionRuntime.Waiting, "confirmation releases coroutine");
        Check(b.RealCalls == 0 && a.RealCalls == 0 && b.DisplayCalls == 2, "fresh strict actual-boundary validation without side effects");
        string result;
        Check(!EventSelectionRuntime.TryDirectResult(pool, out result), "permit needs actual Position boundary");
        var handle = Enter(); Check(handle != null, "expected manager admitted");
        Check(EventSelectionRuntime.TryDirectResult(pool, out result) && result == "story_b", "exact direct result");
        Check(!EventSelectionRuntime.TryDirectResult(pool, out result), "direct result consumed once");
        EventSelectionRuntime.CompletePosition(handle, null);
        Check(EventSelectionRuntime.Token == 0, "request cannot survive completed action");
        Check(Wait(), "same callsite subsequent invocation gets fresh action");
        Check(!EventSelectionRuntime.Select(id, false), "prior action identifier rejected");
    }
    private static void ForceAndRandom()
    {
        var blocked = Event("blocked", false); Begin(blocked);
        Check(Wait(), "single blocked candidate must show popup");
        string id = EventSelectionRuntime.Candidates[0].Id;
        Check(!EventSelectionRuntime.Select(id, false), "blocked normal rejected");
        Check(EventSelectionRuntime.Select(id, true), "explicit force accepted");
        Check(!Wait(), "force releases");
        var handle = Enter(); string result;
        Check(EventSelectionRuntime.TryDirectResult(pool, out result) && result == "story_blocked", "force direct result");
        Check(blocked.RealCalls == 0, "force bypasses eligibility only");
        EventSelectionRuntime.CompletePosition(handle, null);

        var random = Event("unknown"); random.Unknown = true; Begin(random);
        Check(Wait(), "single random dependency must show popup");
        Check(!EventSelectionRuntime.Candidates[0].CanSelect && EventSelectionRuntime.Candidates[0].CanForce, "unknown is explicit force only");
        EventSelectionRuntime.ContinueRandom();
        Check(EventSelectionRuntime.Waiting, "random is queued until real coroutine continuation");
        Check(!Wait(), "random releases"); handle = Enter();
        Check(!EventSelectionRuntime.TryDirectResult(pool, out result), "random invokes original result");
        EventSelectionRuntime.CompletePosition(handle, null);
        Check(EventSelectionRuntime.Token == 0 && random.RealCalls == 0, "runtime does not itself roll random");
    }
    private static void Admissions()
    {
        Begin(Event("single")); Check(!Wait() && !EventSelectionRuntime.Waiting, "single eligible passes through");
        Begin(); Check(!Wait(), "empty pool passes through");
        Begin(Event("a"), Event("b")); EventSelectionRuntime.Enabled = () => false;
        Check(!Wait(), "disabled bypasses");
        EventSelectionRuntime.Enabled = () => throw new Exception("settings");
        Check(!Wait(), "configuration failure bypasses");
        Begin(Event("a"), Event("b")); Check(Wait(), "enter before concurrent call");
        Check(!EventSelectionRuntime.Wait("action", script, 2, new object(), EventSelectionRuntime.Epoch), "other coroutine cannot own current action");
        Check(EventSelectionRuntime.Waiting, "other coroutine cannot release owner");
        Check(EventSelectionRuntime.BeginPosition(new CheckPointManager(), "action") == null, "wrong manager lacks permit");
    }
    private static void ChangesAndCancellation()
    {
        var a = Event("a"); var b = Event("b"); Begin(a, b); Check(Wait(), "enter for changes");
        string id = EventSelectionRuntime.Candidates[0].Id;
        Check(EventSelectionRuntime.Select(id, false), "queue normal"); a.Eligible = false;
        Check(Wait() && EventSelectionRuntime.Waiting, "eligibility change keeps wait");
        Check(a.RealCalls == 0 && EventSelectionRuntime.Status.Contains("바뀌"), "invalid safe state never executes rate");
        a.Eligible = true; EventSelectionRuntime.Tick(true, 1);
        Check(EventSelectionRuntime.Select(id, false), "select again");
        EventSelectionRuntime.CancelConfirmation();
        Check(Wait() && a.RealCalls == 0, "focus transition cancels pending confirm");
        EventSelectionRuntime.ContinueRandom(); EventSelectionRuntime.CancelConfirmation();
        Check(Wait(), "focus transition cancels pending random");
        EventSelectionRuntime.Enabled = () => false; EventSelectionRuntime.Tick(false, 2);
        Check(!EventSelectionRuntime.Waiting && !Wait(), "OFF cannot leave hidden wait");
        string result; var handle = Enter();
        Check(!EventSelectionRuntime.TryDirectResult(pool, out result), "OFF resumes original once");
        EventSelectionRuntime.CompletePosition(handle, null);

        Begin(Event("a"), Event("b")); Check(Wait(), "before result metadata change");
        id = EventSelectionRuntime.Candidates[0].Id;
        Check(EventSelectionRuntime.Select(id, true) == false, "eligible target doesn't expose force action");
        Check(EventSelectionRuntime.Select(id, false), "select before target removed"); pool.List.RemoveAt(0);
        Check(Wait() && EventSelectionRuntime.Waiting, "removed target requires reselection");
        Check(!EventSelectionRuntime.Select(id, false), "removed target has no authority");
        long oldEpoch = EventSelectionRuntime.Epoch;
        EventSelectionRuntime.Invalidate("load");
        Check(EventSelectionRuntime.Wait("action", script, 1, coroutine, oldEpoch), "stale callback cannot resume old flow");
        Check(EventSelectionRuntime.Token == 0, "load discards tokens");
    }
    private static void VisibilityAndFailures()
    {
        var bad = Event("bad"); bad.Broken = true; var good = Event("good"); Begin(bad, good);
        Check(Wait(), "partial failures preserve other candidates");
        Check(EventSelectionRuntime.Candidates[0].Target == good && !EventSelectionRuntime.Candidates[1].CanForce, "eligible stable first; read failure blocks force");
        int reads = bad.DisplayCalls + good.DisplayCalls;
        for (int i = 0; i < 20; i++) EventSelectionRuntime.Tick(false, i);
        Check(reads == bad.DisplayCalls + good.DisplayCalls, "hidden display performs zero eligibility reads");
        EventSelectionRuntime.Tick(true, 1);
        Check(bad.DisplayCalls == 1 && good.DisplayCalls == 2, "failed item backs off while good item refreshes");
        EventSelectionRuntime.Tick(true, 1.1f);
        Check(good.DisplayCalls == 2, "normal refresh interval is 0.25 seconds");
        EventSelectionRuntime.Tick(true, 5);
        Check(bad.DisplayCalls == 2, "failed item retries at five seconds");
        EventSelectionRuntime.WaitingChanged = value => throw new Exception("UI");
        EventSelectionRuntime.ContinueRandom(); Check(!Wait(), "input callback failure does not repeat execution");
        EventSelectionRuntime.WaitingChanged = null;
    }
    private static void GateSyntax()
    {
        scenarios++;
        const string literal = "checkpointmanager.Position(\"action\")";
        string source = "-- " + literal + "\nlocal s='" + literal + "'; local t=[=[" + literal + "]=]; return " + literal;
        bool changed;
        string output = EventLuaGate.Instrument(source, 77, out changed);
        Check(changed, "parsed real call transformed");
        Check(output.Contains("-- " + literal) && output.Contains("[=[" + literal + "]=]"), "comments and long strings unchanged");
        Check(output.Contains("77,1,checkpointmanager"), "call token site and actual receiver captured");
        Check(output.Contains("return " + literal + " end)"), "original call remains once at invocation");
        string second = EventLuaGate.Instrument(output, 77, out changed);
        Check(!changed && second == output, "instrumentation does not stack");
        string unsupported = "return other.checkpointmanager.Position(\"a\"),checkpointmanager:Position(\"a\"),checkpointmanager.Position(name)";
        Check(EventLuaGate.Instrument(unsupported, 78, out changed) == unsupported && !changed, "unsupported receivers dynamic calls pass through");
        source = "local coroutine = {}; return checkpointmanager.Position([=[action]=])";
        output = EventLuaGate.Instrument(source, 79, out changed);
        Check(changed && !output.Contains("coroutine.running") && output.IndexOf("local __dm_event_yield_79") < output.IndexOf("local coroutine"), "yield primitive captured before local shadow");
    }

    private static Script Engine(string code, out DynValue co)
    {
        UserData.RegisterType<CheckPointManager>();
        var engine = new Script(CoreModules.Preset_Complete);
        engine.Globals["checkpointmanager"] = CheckPointManager.Instance;
        var context = new EventExecutionPatches.ScriptContext { Token = script, Epoch = EventSelectionRuntime.Epoch, Finalized = true };
        Check(EventExecutionPatches.BindInterpreter(engine, context), "actual MoonSharp callback bound");
        bool changed;
        string transformed = EventLuaGate.Instrument(code, script, out changed);
        Check(changed, "real Lua expression instrumented");
        co = engine.CreateCoroutine(engine.LoadString(transformed).Function);
        return engine;
    }

    private sealed class ProbeEnumerator : IEnumerator, IDisposable
    {
        internal int Moves, Disposals;
        public object Current => null;
        public bool MoveNext() { Moves++; return true; }
        public void Reset() { }
        public void Dispose() { Disposals++; }
    }

    private static void ActualLuaAndGuards()
    {
        Begin(Event("a"), Event("b"));
        DynValue co;
        Engine("return checkpointmanager.Position(\"action\")", out co);
        co.Coroutine.Resume();
        Check(EventSelectionRuntime.Waiting && CheckPointManager.Instance.PositionCalls == 0, "actual MoonSharp yield occurs before real invocation");
        Check(EventSelectionRuntime.Select(EventSelectionRuntime.Candidates[1].Id, false), "actual Lua normal selected");
        DynValue result = co.Coroutine.Resume();
        Check(co.Coroutine.State == CoroutineState.Dead && result.String == "story_b", "actual Lua coroutine resumes to exact result");
        Check(CheckPointManager.Instance.PositionCalls == 1 && CheckPointManager.Instance.OriginalCalls == 0, "single original expression consumes specified result once");

        Begin(Event("a"), Event("b"));
        Script engine = Engine("return checkpointmanager.Position(\"action\")", out co);
        bool changed;
        DynValue function = engine.LoadString(EventLuaGate.Instrument("return checkpointmanager.Position(\"action\")", script, out changed));
        result = engine.Call(function);
        Check(!EventSelectionRuntime.Waiting && result.String == "original" && CheckPointManager.Instance.OriginalCalls == 1, "main-thread Lua call bypasses without illegal yield");

        Begin(Event("a"), Event("b"));
        Engine("local checkpointmanager={Position=function(name) return 'fake' end}; return checkpointmanager.Position(\"action\")", out co);
        result = co.Coroutine.Resume();
        Check(!EventSelectionRuntime.Waiting && result.String == "fake" && CheckPointManager.Instance.PositionCalls == 0, "local fake receiver cannot establish actual action");

        Begin(Event("a"), Event("b"));
        Engine("local coroutine={}; return checkpointmanager.Position(\"action\")", out co);
        co.Coroutine.Resume();
        Check(EventSelectionRuntime.Waiting, "local coroutine table cannot break captured yield primitive");
        EventSelectionRuntime.ContinueRandom(); result = co.Coroutine.Resume();
        Check(result.String == "original" && CheckPointManager.Instance.OriginalCalls == 1, "actual Lua random path invokes original once");

        Begin(Event("a"), Event("b"));
        Engine("local n=0; local function foo() n=n+1 end; foo()\ncheckpointmanager.Position(\"action\")\nreturn n", out co);
        co.Coroutine.Resume();
        Check(EventSelectionRuntime.Waiting, "standalone gated statement after call does not chain prior return");
        EventSelectionRuntime.ContinueRandom(); result = co.Coroutine.Resume();
        Check(result.Number == 1 && CheckPointManager.Instance.PositionCalls == 1, "prior statement retains exact execution count");

        Begin(Event("a"), Event("b"));
        var context = new EventExecutionPatches.ScriptContext { Epoch = EventSelectionRuntime.Epoch, Finalized = true };
        var probe = new ProbeEnumerator();
        Type patch = typeof(EventExecutionPatches).GetNestedType("LuaCoroutinePatch", BindingFlags.NonPublic);
        MethodInfo guard = patch.GetMethod("Guard", BindingFlags.NonPublic | BindingFlags.Static);
        var enumerator = (IEnumerator)guard.Invoke(null, new object[] { probe, context });
        Check(enumerator.MoveNext() && probe.Moves == 1, "live closure advances original enumerator");
        EventSelectionRuntime.Invalidate("load");
        Check(!enumerator.MoveNext() && probe.Moves == 1 && probe.Disposals == 1, "invalidated coroutine disposes before old continuation");
        probe = new ProbeEnumerator();
        enumerator = (IEnumerator)guard.Invoke(null, new object[] { probe, context });
        Check(!enumerator.MoveNext() && probe.Moves == 0 && probe.Disposals == 1, "old closure first started after load cannot reauthorize or hang");
    }
    private static void Main()
    {
        Normal(); ForceAndRandom(); Admissions(); ChangesAndCancellation(); VisibilityAndFailures(); GateSyntax(); ActualLuaAndGuards();
        Console.WriteLine("PASS: event execution runtime " + assertions + " assertions across " + scenarios + " scenarios (actual supplied MoonSharp; explicit game/evaluator boundaries)");
    }
}
