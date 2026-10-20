using System;
using HarmonyLib;
using Mortal.Story;

namespace DiceMaster.Information
{
    public static class ShopInformationPatches
    {
        public static void Install(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(ShopPanel), "OnItemHover"), postfix: new HarmonyMethod(typeof(ShopInformationPatches), "Selected"));
            foreach (string name in new[] { "HideDialog", "OnDisable", "CloseButtonClick" })
                harmony.Patch(AccessTools.Method(typeof(ShopPanel), name), postfix: new HarmonyMethod(typeof(ShopInformationPatches), "Closed"));
        }
        private static void Selected(object __instance, object __0)
        {
            try { ShopEffectSource.ObserveSelection(__instance, __0); }
            catch (Exception) { ShopEffectSource.Clear(); } // Never interrupt the game's selection callback.
        }
        private static void Closed(object __instance)
        { try { ShopEffectSource.ObserveClosed(__instance); } catch (Exception) { } }
    }
}
