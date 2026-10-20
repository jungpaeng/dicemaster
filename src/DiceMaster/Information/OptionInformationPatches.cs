using System;
using System.Collections.Generic;
using Fungus;
using HarmonyLib;
using Mortal.Core;

namespace DiceMaster.Information
{
    public static class OptionInformationPatches
    {
        private static bool installed;
        private static readonly Dictionary<string, string> failures = new Dictionary<string, string>();
        private static void Observe(string boundary, System.Action callback)
        {
            try
            {
                callback();
                if (failures.Remove(boundary)) DicePlugin.LogWarning("Information option " + boundary + " recovered");
            }
            catch (Exception error)
            {
                string cause = error.GetType().FullName, previous;
                if (!failures.TryGetValue(boundary, out previous) || previous != cause)
                { failures[boundary] = cause; DicePlugin.LogWarning("Information option " + boundary + " failure type=" + cause); }
            }
        }
        public static void Install(Harmony harmony)
        {
            if (installed) return;
            harmony.PatchAll(typeof(ContentUpdated));
            harmony.PatchAll(typeof(MenuReset));
            installed = true;
        }
        [HarmonyPatch(typeof(NormalOptionButton), nameof(NormalOptionButton.UpdateContent))]
        private static class ContentUpdated
        {
            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void Postfix(NormalOptionButton __instance)
            { Observe("content", () => OptionHintSource.Observe(__instance)); }
        }
        [HarmonyPatch(typeof(MenuDialog), nameof(MenuDialog.Clear))]
        private static class MenuReset
        {
            [HarmonyPrefix]
            private static void Prefix(MenuDialog __instance) { Observe("clear", () => OptionHintSource.MenuCleared(__instance)); }
        }
    }
}
