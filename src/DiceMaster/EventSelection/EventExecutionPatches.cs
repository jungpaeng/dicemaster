using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fungus;
using HarmonyLib;
using MoonSharp.Interpreter;
using Mortal.Story;
using Closure = MoonSharp.Interpreter.Closure;

namespace DiceMaster.EventSelection
{
    // MoonSharp is part of the already referenced Fungus.dll in the supplied
    // game. No new external assembly or second input hook is introduced.
    public static class EventExecutionPatches
    {
        internal sealed class ScriptContext
        {
            internal long Token, Epoch;
            internal bool Instrumented;
            internal bool Finalized;
            internal Script Interpreter;
            internal Dictionary<int, EventSwitchSite> SwitchSites = new Dictionary<int, EventSwitchSite>();
        }
        private static readonly ConditionalWeakTable<Closure, ScriptContext> scripts = new ConditionalWeakTable<Closure, ScriptContext>();
        private static readonly List<ScriptContext> registrations = new List<ScriptContext>();
        private static bool installed;

        public static void Install(Harmony harmony)
        {
            if (installed) return;
            harmony.PatchAll(typeof(LuaLoadPatch));
            harmony.PatchAll(typeof(LuaCoroutinePatch));
            harmony.PatchAll(typeof(PositionExecutionPatch));
            harmony.PatchAll(typeof(PositionResultPatch));
            harmony.PatchAll(typeof(SwitchExecutionPatch));
            harmony.PatchAll(typeof(SwitchResultPatch));
            installed = true;
        }

        [HarmonyPatch(typeof(LuaEnvironment), nameof(LuaEnvironment.LoadLuaFunction))]
        private static class LuaLoadPatch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.Last)]
            private static void Prefix(LuaEnvironment __instance, ref string luaString, out ScriptContext __state)
            {
                __state = null;
                // Existing original-source/lifecycle prefixes run before us.
                // Scripts registered before a new load can no longer resume.
                foreach (ScriptContext registration in registrations)
                {
                    try
                    {
                        registration.Interpreter.Globals.Set(EventLuaGate.CallbackKey(registration.Token), DynValue.Nil);
                        registration.Interpreter.Globals.Set(EventLuaGate.YieldKey(registration.Token), DynValue.Nil);
                    }
                    catch (Exception error) { EventSelectionRuntime.Failure("lua-cleanup", error); }
                }
                registrations.Clear();
                try
                {
                    long token = EventSelectionRuntime.NewScript();
                    bool changed;
                    Dictionary<int, EventSwitchSite> switchSites;
                    string updated = EventLuaGate.Instrument(luaString, token, out changed, out switchSites);
                    if (!changed) return;
                    var context = new ScriptContext { Token = token, Epoch = EventSelectionRuntime.Epoch, Instrumented = true, SwitchSites = switchSites };
                    // Bind before compiling so any unsupported interpreter or
                    // reserved-key collision keeps the original source intact.
                    __instance.InitEnvironment();
                    if (!BindInterpreter(__instance.Interpreter, context)) return;
                    __state = context;
                    luaString = updated;
                }
                catch (Exception error) { EventSelectionRuntime.Failure("lua-instrumentation", error); }
            }

            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void Postfix(LuaEnvironment __instance, Closure __result, ScriptContext __state)
            {
                if (__state == null || !__state.Instrumented || __result == null) return;
                try
                {
                    scripts.Remove(__result);
                    scripts.Add(__result, __state);
                }
                catch (Exception error)
                {
                    // If binding fails, the guard in the Lua expression sees
                    // nil and invokes the original Position once as usual.
                    EventSelectionRuntime.Failure("lua-callback", error);
                }
            }

            [HarmonyFinalizer]
            private static Exception Finalizer(Exception __exception, Closure __result, ScriptContext __state)
            {
                if (__exception != null && __state != null)
                    EventSelectionRuntime.Invalidate("Lua 컴파일 실패");
                else if (__state != null && __result != null)
                {
                    // Finalizers run after every load postfix, including the
                    // existing successful-publication context reset. Capture
                    // once here; starting an old closure cannot reauthorize it.
                    __state.Epoch = EventSelectionRuntime.Epoch;
                    __state.Finalized = true;
                }
                return __exception;
            }
        }

        internal static bool BindInterpreter(Script interpreter, ScriptContext context)
        {
            string callbackKey = EventLuaGate.CallbackKey(context.Token);
            string yieldKey = EventLuaGate.YieldKey(context.Token);
            if (!interpreter.Globals.Get(callbackKey).IsNil() || !interpreter.Globals.Get(yieldKey).IsNil()) return false;
            DynValue yieldFunction = interpreter.Globals.Get("coroutine").Table.Get("yield");
            if (yieldFunction.Type != DataType.ClrFunction && yieldFunction.Type != DataType.Function) return false;
            interpreter.Globals[yieldKey] = yieldFunction;
            interpreter.Globals[callbackKey] = DynValue.NewCallback((execution, arguments) =>
            {
                Coroutine coroutine = execution.GetCallingCoroutine();
                if (coroutine == null || coroutine.State == CoroutineState.Main || arguments.Count != 5 ||
                    arguments[0].Type != DataType.String || arguments[1].Type != DataType.Number ||
                    arguments[2].Type != DataType.Number || arguments[3].Type != DataType.UserData ||
                    !ReferenceEquals(arguments[3].UserData.Object, CheckPointManager.Instance) || arguments[4].Type != DataType.String) return DynValue.False;
                long script = (long)arguments[1].Number;
                if (script != context.Token) return DynValue.False;
                if (arguments[4].String == "Switch")
                {
                    EventSwitchSite definition;
                    if (!context.SwitchSites.TryGetValue((int)arguments[2].Number, out definition)) return DynValue.False;
                    return DynValue.NewBoolean(EventSelectionRuntime.WaitSwitch(arguments[0].String, script,
                        (int)arguments[2].Number, coroutine, context.Epoch, definition));
                }
                if (arguments[4].String != "Position") return DynValue.False;
                return DynValue.NewBoolean(EventSelectionRuntime.Wait(arguments[0].String, script,
                    (int)arguments[2].Number, coroutine, context.Epoch));
            });
            context.Interpreter = interpreter;
            registrations.Add(context);
            return true;
        }

        [HarmonyPatch(typeof(LuaEnvironment), "RunLuaCoroutine")]
        private static class LuaCoroutinePatch
        {
            [HarmonyPostfix]
            private static void Postfix(Closure closure, ref IEnumerator __result)
            {
                ScriptContext context;
                if (closure != null && __result != null && scripts.TryGetValue(closure, out context))
                {
                    __result = Guard(__result, context);
                }
            }

            private static IEnumerator Guard(IEnumerator original, ScriptContext context)
            {
                try
                {
                    while (context.Finalized && context.Epoch == EventSelectionRuntime.Epoch && original.MoveNext())
                    {
                        if (context.Epoch != EventSelectionRuntime.Epoch) yield break;
                        yield return original.Current;
                    }
                }
                finally { (original as IDisposable)?.Dispose(); }
            }
        }

        [HarmonyPatch(typeof(CheckPointManager), nameof(CheckPointManager.Position))]
        private static class PositionExecutionPatch
        {
            [HarmonyPrefix]
            private static void Prefix(CheckPointManager __instance, string name,
                out EventSelectionRuntime.ActionContextHandle __state)
            { __state = EventSelectionRuntime.BeginPosition(__instance, name); }

            [HarmonyFinalizer]
            private static Exception Finalizer(Exception __exception, EventSelectionRuntime.ActionContextHandle __state)
            {
                EventSelectionRuntime.CompletePosition(__state, __exception);
                return __exception;
            }
        }

        [HarmonyPatch(typeof(CheckPointManager), nameof(CheckPointManager.Switch))]
        private static class SwitchExecutionPatch
        {
            [HarmonyPrefix]
            private static void Prefix(CheckPointManager __instance, string name, out EventSelectionRuntime.ActionContextHandle __state)
            { __state = EventSelectionRuntime.BeginSwitch(__instance, name); }
            [HarmonyFinalizer]
            private static Exception Finalizer(Exception __exception, EventSelectionRuntime.ActionContextHandle __state)
            { EventSelectionRuntime.CompletePosition(__state, __exception); return __exception; }
        }

        [HarmonyPatch(typeof(SwitchResultData), nameof(SwitchResultData.Execute))]
        private static class SwitchResultPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(SwitchResultData __instance, ref int __result)
            {
                int result;
                if (!EventSelectionRuntime.TryDirectSwitchResult(__instance, out result)) return true;
                __result = result; return false;
            }
        }

        [HarmonyPatch(typeof(PositionResultData), nameof(PositionResultData.GetResult))]
        private static class PositionResultPatch
        {
            [HarmonyPrefix]
            private static bool Prefix(PositionResultData __instance, ref string __result)
            {
                string result;
                if (!EventSelectionRuntime.TryDirectResult(__instance, out result)) return true;
                __result = result;
                return false;
            }
        }
    }
}
