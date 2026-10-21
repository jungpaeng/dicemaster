using System;
using System.Linq;
using System.Reflection;
using DiceMaster;
using DiceMaster.EventSelection;
using DiceMaster.Information;
using Fungus;
using HarmonyLib;
using UnityEngine.EventSystems;

internal static partial class Program
{
    private static readonly Type loadPatch = typeof(ChoiceStoryPatches).GetNestedType("ScriptLoaded", BindingFlags.NonPublic);
    private static readonly MethodInfo loadPrefix = loadPatch.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo loadFinalizer = loadPatch.GetMethod("Finalizer", BindingFlags.Static | BindingFlags.NonPublic);
    private static object CaptureLoad(string text, string name)
    {
        object[] args = { text, name, null }; loadPrefix.Invoke(null, args); return args[2];
    }
    private static Exception CompleteLoad(object state, object result, Exception error = null)
    { return (Exception)loadFinalizer.Invoke(null, new object[] { new LuaEnvironment(new MoonSharp.Interpreter.Script()), state, result, error }); }
    private static string ObservedSource()
    { return (string)typeof(ChoiceStorySource).GetField("source", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null); }
    private static void ObserveCompiled(string text, string name)
    {
        object state = CaptureLoad(text, name);
        string compiled = EventLuaGate.Instrument(text, 71, out bool changed);
        CompleteLoad(state, new object());
        Check(ReferenceEquals(ObservedSource(), text), "the successful load finalizer publishes the exact original reference captured before execution instrumentation");
        Check(!changed || !ReferenceEquals(compiled, text) && compiled.Contains("__dm_event_invoke_71", StringComparison.Ordinal),
            "the actual #5 transform still prepares its independent execution input; the observer neither rewrites nor strips it");
    }
    private static void ChoiceLoadCases()
    {
        Check(loadPrefix.GetCustomAttribute<HarmonyPriority>().Value == Priority.First &&
            loadFinalizer.GetCustomAttribute<HarmonyPriority>().Value == Priority.Last,
            "original source capture precedes the real late #5 prefix and publication follows successful completion");
        Check(!loadFinalizer.GetParameters().Any(p => p.Name == "luaString"), "the finalizer cannot accidentally analyze the compiler's transformed Lua argument");
        Reset(); var count = EntryCounter("actual_load_entry", 0);
        string body = "local a={'O_entry'};local r=choose(a);if r==1 then local s=checkpointmanager.Switch('actual_load_entry');if s==1 then " + EntryNpc + " end end";
        ObserveCompiled(body, "loaded_entry.LuaScript");
        var option = Normal("O_entry", "실제 로드 인사"); EventSystem.current.currentSelectedGameObject = option.gameObject;
        var before = Linked(Ready(true), "실제 로드 인사");
        Check(before.AutoText.Length > 0 && before.Detail.Contains("Switch actual_load_entry", StringComparison.Ordinal), "a captured original load retains the exact entry checkpoint trace before completion");
        count._count = 1;
        Check(!Read().Entries.Any(x => x.Title == "실제 로드 인사") && ChoiceStorySource.CurrentRow(Read()) == null && count._count == 1,
            "completion removes the actual loaded entry from both automatic rows and passive native pad focus");
        int reads = DataHelper.Reads, parses = DiceMaster.LuaParse.LuaParser.Parses;
        for (int at = 0; at < 60; at++) Read();
        Check(reads == DataHelper.Reads && parses == DiceMaster.LuaParse.LuaParser.Parses && count._count == 1 && Mortal.Core.Counters.GameCalls == 0,
            "original load capture adds no warm source parsing, native judgment, counter, RNG or input mutation");

        string retained = ObservedSource(); string key = ChoiceStorySource.ContextKey;
        object failed = CaptureLoad("invalid next source", "failed.LuaScript"); var failure = new InvalidOperationException("compiler fixture failure");
        Check(ReferenceEquals(CompleteLoad(failed, new object(), failure), failure) && ReferenceEquals(retained, ObservedSource()) && ChoiceStorySource.ContextKey == key,
            "failed compilation preserves its exact exception and cannot publish the staged original");
        CompleteLoad(failed, null);
        Check(ReferenceEquals(retained, ObservedSource()) && ChoiceStorySource.ContextKey == key, "a null compiler result also cannot publish staged source");
        OptionInfoPatch.DisplayRefreshDepth = 1; object display = CaptureLoad("display only", "display.LuaScript"); OptionInfoPatch.DisplayRefreshDepth = 0;
        CompleteLoad(display, new object());
        Check(ReferenceEquals(retained, ObservedSource()), "display-protected loading never captures an execution source");
        object protectedCompletion = CaptureLoad("protected completion", "protected.LuaScript"); OptionInfoPatch.DisplayRefreshDepth = 1;
        CompleteLoad(protectedCompletion, new object()); OptionInfoPatch.DisplayRefreshDepth = 0;
        Check(ReferenceEquals(retained, ObservedSource()), "display protection at finalization also prevents publication");

        object outer = CaptureLoad("outer fails", "outer.LuaScript"), inner = CaptureLoad(body, "inner.LuaScript");
        CompleteLoad(inner, new object()); string innerKey = ChoiceStorySource.ContextKey;
        CompleteLoad(outer, null, failure);
        Check(ReferenceEquals(body, ObservedSource()) && ChoiceStorySource.ContextKey == innerKey,
            "a nested successful source is not replaced by the failed outer invocation or a shared last-source slot");
    }
}

// Hook-order fixtures. Production #5 syntax transformation and observer
// prefix/finalizer are compiled above; no Harmony patch or Lua/game UI executes.
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string name) { } }
    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyPrefix : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyPostfix : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyFinalizer : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyPriority : Attribute { public readonly int Value; public HarmonyPriority(int value) { Value = value; } }
    public static class Priority { public const int First = 800, Last = 0; }
    public sealed class Harmony { public void PatchAll(Type type) { } }
}
namespace DiceMaster { public static class OptionInfoPatch { public static int DisplayRefreshDepth; } }
namespace DiceMaster.Information
{
    public static class InformationRuntime
    {
        public static bool Enabled(int index) => true;
        public static void Failure(string where, Exception error) { throw new Exception(where, error); }
    }
}
