using System;
using Fungus;
using HarmonyLib;
using Mortal.Core;
using DiceMaster.LiveLog;

namespace DiceMaster.Information
{
    public static class ChoiceStoryPatches
    {
        private static bool installed;
        private static void Observe(System.Action callback)
        { try { callback(); } catch (Exception error) { InformationRuntime.Failure("선택 화면 관찰", error); } }
        public static void Install(Harmony harmony)
        {
            if (installed) return;
            ChoiceStorySource.ContextReader = () => LogBridge.Model.ContextId;
            harmony.PatchAll(typeof(Normal)); harmony.PatchAll(typeof(Position)); harmony.PatchAll(typeof(Break));
            harmony.PatchAll(typeof(MenuClear)); harmony.PatchAll(typeof(ScriptLoaded)); installed = true;
        }
        [HarmonyPatch(typeof(NormalOptionButton), nameof(NormalOptionButton.UpdateContent))]
        private static class Normal
        { [HarmonyPostfix, HarmonyPriority(Priority.Last)] private static void Postfix(NormalOptionButton __instance) { Observe(() => ChoiceStorySource.Observe(__instance)); } }
        [HarmonyPatch(typeof(PositionOptionButton), nameof(PositionOptionButton.UpdateContent))]
        private static class Position
        { [HarmonyPostfix, HarmonyPriority(Priority.Last)] private static void Postfix(PositionOptionButton __instance) { Observe(() => ChoiceStorySource.Observe(__instance)); } }
        [HarmonyPatch(typeof(BreakOptionButton), nameof(BreakOptionButton.UpdateContent))]
        private static class Break
        { [HarmonyPostfix, HarmonyPriority(Priority.Last)] private static void Postfix(BreakOptionButton __instance) { Observe(() => ChoiceStorySource.Observe(__instance)); } }
        [HarmonyPatch(typeof(MenuDialog), nameof(MenuDialog.Clear))]
        private static class MenuClear
        { [HarmonyPrefix] private static void Prefix(MenuDialog __instance) { Observe(() => ChoiceStorySource.MenuCleared(__instance)); } }
        [HarmonyPatch(typeof(LuaEnvironment), nameof(LuaEnvironment.LoadLuaFunction))]
        private static class ScriptLoaded
        {
            private sealed class OriginalSource
            { internal string Text, Friendly; }

            // #5 wraps checkpoint calls in a later prefix. Keep this exact
            // invocation's original definitions, not its execution wrappers.
            // Per-call state also prevents failed/nested loads from borrowing
            // another invocation's source through a shared last-source field.
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void Prefix(string luaString, string friendlyName, out OriginalSource __state)
            {
                __state = OptionInfoPatch.DisplayRefreshDepth == 0
                    ? new OriginalSource { Text = luaString, Friendly = friendlyName } : null;
            }
            [HarmonyFinalizer, HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(LuaEnvironment __instance, OriginalSource __state, object __result, Exception __exception)
            {
                if (__exception == null && __result != null && __state != null && OptionInfoPatch.DisplayRefreshDepth == 0)
                    Observe(() => {
                        DiceInformation.SourceChanged();
                        if (InformationRuntime.Enabled(0) || InformationRuntime.Enabled(1)) PreviewConditions.ObserveLuaEnvironment(__instance);
                        ChoiceStorySource.ObserveScript(__state.Text, __state.Friendly);
                    });
                return __exception;
            }
        }
    }
}
