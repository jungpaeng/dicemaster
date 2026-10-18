using System.Collections.Generic;

namespace DiceMaster.LiveLog
{
    // Pure admission state: it never reads a manager, invokes Lua or mutates game
    // data. Identity is an invocation token, not the reused game's object identity.
    public sealed class LifecycleState
    {
        private readonly HashSet<long> transitions = new HashSet<long>();
        private long sequence;
        private long latestLua;
        private bool luaConsumed;
        private bool ready;

        public bool IsReady { get { return ready; } }

        private long NextToken()
        {
            // Zero is Harmony's uninitialised __state value. Never issue it.
            // Overflow fails closed instead of reusing an old invocation token.
            return checked(++sequence);
        }

        public long EnterTransition()
        {
            ready = false;
            latestLua = 0;
            luaConsumed = true;
            long token = NextToken();
            transitions.Add(token);
            return token;
        }

        public void ExitTransition(long token)
        {
            // A finalizer may be repeated or see zero when another prefix skipped
            // ours. Removing a token once cannot release a different transition.
            transitions.Remove(token);
            // Even a normal load return says nothing about scene/Lua readiness.
        }

        public long StageLua()
        {
            ready = false;
            latestLua = 0;
            luaConsumed = true;
            long token = NextToken();
            latestLua = token;
            luaConsumed = false;
            return token;
        }

        public bool CompleteLua(long token, bool success)
        {
            if (token == 0 || token != latestLua || luaConsumed) return false;
            luaConsumed = true;
            ready = success && transitions.Count == 0;
            return ready;
        }

        public void AbortLua(long token)
        {
            if (token == 0 || token != latestLua) return;
            // Harmony finalizers run after postfixes. A later patch can throw
            // after our successful CompleteLua; revoke this current token only.
            luaConsumed = true;
            ready = false;
        }
    }
}
