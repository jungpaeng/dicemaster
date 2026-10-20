using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mortal.Core;
using Mortal.Story;
namespace DiceMaster.Information
{
    public static class InformationLifecyclePatches
    {
        public static void Install(Harmony harmony) { harmony.PatchAll(typeof(Transition)); }
        [HarmonyPatch]
        private static class Transition
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (var method in typeof(SaveSystem).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    if (method.Name == "NewGameData" || method.Name == "NewActGameData" || method.Name == "LoadGameData" ||
                        method.Name == "AutoLoadGameData" || method.Name == "LoadRestoreSaveData" || method.Name == "ExecuteLoadGameData")
                        yield return method;
                foreach (var method in typeof(SceneController).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    if (method.Name == "StartLoadScene") yield return method;
            }
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void Prefix()
            { try { InformationRuntime.ResetDisplays(); } catch (Exception error) { InformationRuntime.Failure("문맥 무효화", error); } }
        }
    }
}
