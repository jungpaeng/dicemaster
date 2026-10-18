using System;
using DiceMaster.LiveLog;

internal static class Program
{
    private static int assertions;

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Ready(LifecycleState state)
    {
        long lua = state.StageLua();
        Check(state.CompleteLua(lua, true) && state.IsReady, "A new successful Lua must admit queries.");
    }

    private static void Main()
    {
        var state = new LifecycleState();
        Check(!state.IsReady, "Startup has no verified Lua context.");
        Ready(state);

        long outer = state.EnterTransition();
        Check(!state.IsReady, "Transition start must invalidate even identical game objects.");
        long inner = state.EnterTransition();
        Check(inner != outer && outer != 0, "Nested transition tokens must be unique and nonzero.");
        state.ExitTransition(outer);
        state.ExitTransition(outer);
        state.ExitTransition(0);
        state.ExitTransition(long.MaxValue);
        long duringLoad = state.StageLua();
        Check(!state.CompleteLua(duringLoad, true) && !state.IsReady, "A duplicate exit must not release the inner transition.");
        state.ExitTransition(inner);
        Check(!state.IsReady, "Exit cannot implicitly ready Lua compiled during a transition.");
        Check(!state.CompleteLua(duringLoad, true), "A previously rejected token must remain consumed.");
        Ready(state);

        long failedLoad = state.EnterTransition();
        state.ExitTransition(failedLoad);
        Check(!state.IsReady, "Missing, failed or normal void load return must stay blocked.");
        Ready(state);
        long sameObjects = state.EnterTransition();
        state.ExitTransition(sameObjects);
        Check(!state.IsReady, "Same-object, same-script reload cannot preserve old readiness.");

        long invalidCompile = state.StageLua();
        Check(!state.CompleteLua(invalidCompile, false) && !state.IsReady, "Null compile result must not activate.");
        Check(!state.CompleteLua(invalidCompile, true) && !state.IsReady, "Duplicate success must not revive a failed compile.");
        long abortedCompile = state.StageLua();
        state.AbortLua(abortedCompile);
        state.AbortLua(abortedCompile);
        Check(!state.CompleteLua(abortedCompile, true) && !state.IsReady, "Exception abort must consume stage once.");

        long staleLua = state.StageLua();
        long currentLua = state.StageLua();
        Check(!state.CompleteLua(staleLua, true) && !state.IsReady, "Older compilation cannot activate the current context.");
        state.AbortLua(staleLua);
        Check(state.CompleteLua(currentLua, true) && state.IsReady, "Stale abort must not consume current stage.");
        state.AbortLua(staleLua);
        Check(state.IsReady, "Stale finalizer must not invalidate a newer successful Lua.");
        Check(!state.CompleteLua(currentLua, true) && state.IsReady, "Duplicate successful postfix cannot reactivate or disrupt.");
        state.AbortLua(currentLua);
        Check(!state.IsReady, "Current finalizer error after successful postfix must revoke readiness.");

        long beforeTransition = state.StageLua();
        long replacement = state.EnterTransition();
        state.ExitTransition(replacement);
        Check(!state.CompleteLua(beforeTransition, true) && !state.IsReady, "Transition must invalidate staged old Lua.");
        state.AbortLua(beforeTransition);
        Check(!state.IsReady, "Old failed finalizer cannot undo the transition gate.");
        Check(!state.CompleteLua(0, true) && !state.CompleteLua(long.MaxValue, true), "Unknown postfix tokens must fail closed.");
        state.AbortLua(0);
        state.AbortLua(long.MaxValue);
        Ready(state);

        // Model repeated originals plus out-of-order/finally callbacks with
        // reused fixture data, without needing any game/Unity runtime.
        for (int iteration = 0; iteration < 1000; iteration++)
        {
            long first = state.EnterTransition();
            long second = state.EnterTransition();
            state.ExitTransition(second);
            state.ExitTransition(second);
            long blocked = state.StageLua();
            Check(!state.CompleteLua(blocked, true), "Out-of-order finalizers released active outer load.");
            state.ExitTransition(first);
            Check(!state.IsReady, "Finishing outer load cannot release rejected stage.");
            long next = state.StageLua();
            Check(state.CompleteLua(next, true), "Following verified Lua did not recover.");
            state.AbortLua(blocked);
            Check(state.IsReady, "Old finalizer corrupted recovered context.");
        }

        Console.WriteLine("Lifecycle assertions passed: " + assertions);
    }
}
