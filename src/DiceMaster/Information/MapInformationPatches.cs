using HarmonyLib;
using Mortal.Free;

namespace DiceMaster.Information
{
    public static class MapInformationPatches
    {
        private static bool installed;
        public static void Install(Harmony harmony)
        {
            if (installed) return;
            harmony.PatchAll(typeof(PlaceStarted));
            harmony.PatchAll(typeof(PlaceSelected));
            harmony.PatchAll(typeof(PlaceDeselected));
            installed = true;
        }
        [HarmonyPatch(typeof(PositionController), "Start")]
        private static class PlaceStarted
        {
            [HarmonyPostfix]
            private static void Postfix(PositionController __instance) { MapGuideSource.Register(__instance); }
        }
        [HarmonyPatch(typeof(PositionController), nameof(PositionController.OnPositionEnter))]
        private static class PlaceSelected
        {
            [HarmonyPostfix]
            private static void Postfix(PositionController __instance) { MapGuideSource.Select(__instance); }
        }
        [HarmonyPatch(typeof(PositionController), nameof(PositionController.OnPositionExit))]
        private static class PlaceDeselected
        {
            [HarmonyPostfix]
            private static void Postfix(PositionController __instance) { MapGuideSource.Deselect(__instance); }
        }
    }
}
