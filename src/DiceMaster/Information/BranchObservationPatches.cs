using System;
using System.Collections.Generic;
using System.Reflection;
using DiceMaster.EventSelection;
using HarmonyLib;
using Mortal.Core;
using Mortal.Story;
using UnityEngine;

namespace DiceMaster.Information
{
    // Prefix state exists even when #5's selection prefix skips the original.
    // Finalizers inspect the final return and preserve the exact exception.
    public static class BranchObservationPatches
    {
        internal sealed class CallContext
        {
            internal CallContext Parent;
            internal string Kind, Name, Source, Friendly, Context;
        }
        [ThreadStatic] private static CallContext current;
        private static bool installed;
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        public static void Install(Harmony harmony)
        {
            if (installed) return;
            harmony.PatchAll(typeof(PositionCallPatch));
            harmony.PatchAll(typeof(SwitchCallPatch));
            harmony.PatchAll(typeof(PositionResultPatch));
            harmony.PatchAll(typeof(SwitchResultPatch));
            harmony.PatchAll(typeof(ResetPatch));
            installed = true;
        }

        private static CallContext Enter(string kind, string name)
        {
            if (!BranchHistory.Enabled) return null;
            var frame = new CallContext { Parent = current, Kind = kind, Name = name ?? "", Context = BranchHistory.ContextKey };
            try
            {
                frame.Source = typeof(OptionInfoPatch).GetField("_lastLuaString", Fields)?.GetValue(null) as string;
                frame.Friendly = typeof(OptionInfoPatch).GetField("_lastFriendlyName", Fields)?.GetValue(null) as string;
            }
            catch (Exception error) { BranchHistory.BoundaryFailure("source", name, error); }
            current = frame;
            return frame;
        }
        private static void Exit(CallContext frame)
        {
            if (frame == null) return;
            if (ReferenceEquals(current, frame)) current = frame.Parent;
            else
            {
                // Defensive unwinding is display-only and never repairs game
                // state. Do not attach an unrelated later call to this source.
                current = null;
            }
        }
        private static BranchHistory.Observation Begin(string kind, object pool)
        {
            if (!BranchHistory.Enabled || OptionInfoPatch.DisplayRefreshDepth != 0) return null;
            CallContext frame = current != null && current.Kind == kind && current.Context == BranchHistory.ContextKey ? current : null;
            EventSwitchSite site = null;
            if (kind == "조건")
            {
                try
                {
                    object action = typeof(EventSelectionRuntime).GetField("current", Fields)?.GetValue(null);
                    if (action != null)
                    {
                        Type type = action.GetType();
                        if (ReferenceEquals(type.GetField("SwitchPool", Fields)?.GetValue(action), pool) &&
                            true.Equals(type.GetField("Executing", Fields)?.GetValue(action)))
                            site = type.GetField("SwitchSite", Fields)?.GetValue(action) as EventSwitchSite;
                    }
                }
                catch (Exception error) { BranchHistory.BoundaryFailure("call-site", frame?.Name, error); }
            }
            return BranchHistory.Begin(kind, pool, frame?.Name, frame?.Source, frame?.Friendly, site);
        }
        private static void Finish(BranchHistory.Observation state, string result, int branch, Exception error)
        {
            try { BranchHistory.Complete(state, result, branch, error == null ? Time.unscaledTime : float.NaN, error); }
            catch (Exception failure) { BranchHistory.BoundaryFailure("observation", state?.Name, failure); }
        }

        [HarmonyPatch(typeof(CheckPointManager), nameof(CheckPointManager.Position))]
        private static class PositionCallPatch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void Prefix(string name, out CallContext __state)
            { __state = null; try { __state = Enter("이벤트", name); } catch (Exception error) { BranchHistory.BoundaryFailure("position-call", name, error); } }
            [HarmonyFinalizer, HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(Exception __exception, CallContext __state)
            { Exit(__state); return __exception; }
        }
        [HarmonyPatch(typeof(CheckPointManager), nameof(CheckPointManager.Switch))]
        private static class SwitchCallPatch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void Prefix(string name, out CallContext __state)
            { __state = null; try { __state = Enter("조건", name); } catch (Exception error) { BranchHistory.BoundaryFailure("switch-call", name, error); } }
            [HarmonyFinalizer, HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(Exception __exception, CallContext __state)
            { Exit(__state); return __exception; }
        }
        [HarmonyPatch(typeof(PositionResultData), nameof(PositionResultData.GetResult))]
        private static class PositionResultPatch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void Prefix(PositionResultData __instance, out BranchHistory.Observation __state)
            { __state = null; try { __state = Begin("이벤트", __instance); } catch (Exception error) { BranchHistory.BoundaryFailure("position-begin", null, error); } }
            [HarmonyFinalizer, HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(Exception __exception, string __result, BranchHistory.Observation __state)
            { Finish(__state, __result, 0, __exception); return __exception; }
        }
        [HarmonyPatch(typeof(SwitchResultData), nameof(SwitchResultData.Execute))]
        private static class SwitchResultPatch
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void Prefix(SwitchResultData __instance, out BranchHistory.Observation __state)
            { __state = null; try { __state = Begin("조건", __instance); } catch (Exception error) { BranchHistory.BoundaryFailure("switch-begin", null, error); } }
            [HarmonyFinalizer, HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(Exception __exception, int __result, BranchHistory.Observation __state)
            { Finish(__state, null, __result, __exception); return __exception; }
        }

        // These are actual installed-game entry points, not guessed save APIs.
        // Both SaveGameData overloads and all autosave wrappers are included;
        // common sinks cover legend/universe/current-slot and direct writes.
        internal static IEnumerable<MethodBase> ResetTargets()
        {
            var names = new HashSet<string>(StringComparer.Ordinal) {
                "SaveGameData", "AutoSaveData", "AutoSaveStoryData", "AutoSaveFreeData", "AutoSaveBattleData",
                "SaveLegendData", "SaveUniverseData", "SaveCurrentSlotData", "BackupLibraryData", "UpdateRestoreData", "ExecuteSaveData", "ExecuteSaveJsonData",
                "NewGameData", "NewActGameData", "LoadGameData", "AutoLoadGameData", "LoadRestoreSaveData", "ExecuteLoadGameData", "LoadUniverseData"
            };
            foreach (MethodInfo method in typeof(SaveSystem).GetMethods(Fields | BindingFlags.DeclaredOnly))
                if (names.Contains(method.Name)) yield return method;
            yield return AccessTools.Method(typeof(SceneController), "StartLoadScene", new[] { typeof(string), typeof(bool) })
                ?? throw new MissingMethodException(typeof(SceneController).FullName, "StartLoadScene");
        }
        [HarmonyPatch]
        private static class ResetPatch
        {
            [HarmonyTargetMethods]
            private static IEnumerable<MethodBase> TargetMethods() { return ResetTargets(); }
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void Prefix() { BranchHistory.Clear(); }
        }
    }
}
