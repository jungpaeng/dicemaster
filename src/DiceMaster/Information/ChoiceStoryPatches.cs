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
            [HarmonyFinalizer, HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(string luaString, string friendlyName, object __result, Exception __exception)
            {
                if (__exception == null && __result != null && OptionInfoPatch.DisplayRefreshDepth == 0)
                    Observe(() => ChoiceStorySource.ObserveScript(luaString, friendlyName));
                return __exception;
            }
        }
    }
}
