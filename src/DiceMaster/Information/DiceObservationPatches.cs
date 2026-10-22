using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fungus;
using HarmonyLib;
using Mortal.Core;
using Mortal.Story;
using UnityEngine;
using UnityEngine.UI;

namespace DiceMaster.Information
{
    public static class DiceObservationPatches
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly FieldInfo buttons = typeof(MenuDialog).GetField("cachedButtons", Fields);
        private static readonly FieldInfo header = typeof(DiceMenuDialog).GetField("_header", Fields);
        private static readonly FieldInfo headerView = typeof(DiceMenuDialog).GetField("_diceHeader", Fields);
        private static readonly FieldInfo headerText = typeof(DiceHeader).GetField("_front", Fields);
        private static readonly FieldInfo result = typeof(DiceMenuDialog).GetField("_resultSelection", Fields);
        private static readonly FieldInfo accepted = typeof(DiceMenuDialog).GetField("_triggerNextInput", Fields);
        private static readonly FieldInfo optionRaw = typeof(DiceOption).GetField("_sourceText", Fields);
        private static readonly FieldInfo optionName = typeof(DiceOption).GetField("_normalText", Fields);
        private static readonly FieldInfo normalName = typeof(NormalOptionButton).GetField("_text", Fields);
        private static readonly FieldInfo positionName = typeof(PositionOptionButton).GetField("_titleText", Fields);
        private static readonly FieldInfo breakName = typeof(BreakOptionButton).GetField("_titleText", Fields);
        private static bool installed;
        private static void Observe(Action action)
        { try { action(); } catch (Exception error) { InformationRuntime.Failure("주사위 관찰", error); } }
        public static void Install(Harmony harmony)
        {
            if (installed) return;
            DiceInformation.Visible = owner => {
                var dice = owner as DiceMenuDialog;
                return dice != null && dice.isActiveAndEnabled && dice.gameObject.activeInHierarchy && ReferenceEquals(MenuDialog.ActiveMenuDialog, dice);
            };
            DiceInformation.Clock = () => Time.unscaledTime;
            DiceInformation.Failure = error => InformationRuntime.Failure("주사위 관찰", error);
            harmony.PatchAll(typeof(Roll)); harmony.PatchAll(typeof(Timer));
            harmony.PatchAll(typeof(Closing)); harmony.PatchAll(typeof(ChoicePress)); harmony.PatchAll(typeof(MenuBuilding));
            installed = true;
        }
        private static bool Enabled { get { return BranchHistory.Enabled && OptionInfoPatch.DisplayRefreshDepth == 0; } }
        private static object Read(FieldInfo field, object owner)
        { if (field == null || owner == null) throw new MissingFieldException("Dice UI field unavailable"); return field.GetValue(owner); }
        private static string TextOf(object text)
        {
            var legacy = text as Text;
            if (legacy != null) return legacy.text;
            // TMP is already populated by the game. Avoid adding a dependency
            // or invoking GetStoryText (which would run effect formatting).
            var property = text == null ? null : text.GetType().GetProperty("text", Fields);
            return property == null ? "" : property.GetValue(text, null) as string ?? "";
        }
        private static DiceInformation.View View(DiceMenuDialog dice)
        {
            var view = new DiceInformation.View { Visible = dice != null && dice.gameObject.activeInHierarchy };
            if (!view.Visible) return view;
            view.Name = TextOf(Read(headerText, Read(headerView, dice))).TrimEnd('.', '…', ' ');
            var native = Read(buttons, dice) as Button[];
            int count = native == null ? 0 : Math.Min(64, native.Length);
            view.Labels = new string[count]; view.Raw = new string[count];
            for (int at = 0; at < count; at++)
            {
                Button button = native[at];
                if (button == null || !button.gameObject.activeInHierarchy) continue;
                DiceOption option = button.GetComponent<DiceOption>();
                if (option == null) continue;
                view.Labels[at] = ChoiceStoryLabels.Rendered(TextOf(Read(optionName, option)));
                view.Raw[at] = TextOf(Read(optionRaw, option));
            }
            return view;
        }
        private static string Key(DiceMenuDialog dice)
        {
            string value = Read(header, dice) as string ?? "";
            return value.StartsWith("DiceHeader/", StringComparison.Ordinal) ? value.Substring("DiceHeader/".Length) : "";
        }
        [HarmonyPatch(typeof(DiceMenuDialog), "ExecuteRoll")]
        private static class Roll
        {
            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void Postfix(DiceMenuDialog __instance, string checkName, ref IEnumerator __result)
            {
                if (!Enabled) return;
                IEnumerator original = __result, wrapped = original;
                Observe(() => wrapped = DiceInformation.Observe(original, __instance, checkName, () => View(__instance)));
                __result = wrapped;
            }
        }
        [HarmonyPatch]
        private static class Timer
        {
            [HarmonyTargetMethods]
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (MethodInfo method in typeof(DiceMenuDialog).GetMethods(Fields | BindingFlags.DeclaredOnly))
                    if (method.Name == "ShowTimer" && method.ReturnType == typeof(IEnumerator)) yield return method;
            }
            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void Postfix(DiceMenuDialog __instance, ref IEnumerator __result)
            {
                if (!Enabled) return;
                IEnumerator original = __result, wrapped = original;
                Observe(() => wrapped = DiceInformation.Observe(original, __instance, Key(__instance), () => View(__instance)));
                __result = wrapped;
            }
        }
        [HarmonyPatch(typeof(MenuDialog), nameof(MenuDialog.Clear))]
        private static class Closing
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void Prefix(MenuDialog __instance, out DiceInformation.Closing __state)
            {
                __state = null;
                if (!Enabled || !(__instance is DiceMenuDialog)) return;
                DiceInformation.Closing state = null;
                Observe(() => {
                    if (true.Equals(Read(accepted, __instance))) state = DiceInformation.BeginClosing(__instance, (int)Read(result, __instance));
                });
                __state = state;
            }
            [HarmonyFinalizer, HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(DiceInformation.Closing __state, Exception __exception)
            { Observe(() => DiceInformation.EndClosing(__state, __exception)); return __exception; }
        }
        [HarmonyPatch(typeof(Button), "Press")]
        private static class ChoicePress
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void Prefix(Button __instance, out DiceInformation.Choice __state)
            {
                __state = null; if (!Enabled) return;
                DiceInformation.Choice state = null;
                Observe(() => {
                    var menu = __instance.GetComponentInParent<MenuDialog>();
                    if (menu == null || menu is DiceMenuDialog || !ReferenceEquals(menu, MenuDialog.ActiveMenuDialog) ||
                        !__instance.isActiveAndEnabled || !__instance.gameObject.activeInHierarchy || !__instance.IsInteractable()) return;
                    var native = Read(buttons, menu) as Button[]; bool belongs = false;
                    if (native != null) for (int at = 0; at < Math.Min(256, native.Length); at++) if (ReferenceEquals(native[at], __instance)) { belongs = true; break; }
                    if (!belongs) return;
                    Component option = __instance.GetComponent<NormalOptionButton>(); FieldInfo name = normalName;
                    if (option == null) { option = __instance.GetComponent<PositionOptionButton>(); name = positionName; }
                    if (option == null) { option = __instance.GetComponent<BreakOptionButton>(); name = breakName; }
                    string label = "";
                    var labels = option == null ? null : Read(name, option) as Text[];
                    if (labels != null) for (int at = 0; at < Math.Min(12, labels.Length); at++)
                        if (labels[at] != null && labels[at].gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(labels[at].text))
                        { label = ChoiceStoryLabels.Rendered(labels[at].text); break; }
                    state = DiceInformation.BeginChoice(label);
                });
                __state = state;
            }
            [HarmonyFinalizer, HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(DiceInformation.Choice __state, Exception __exception)
            { Observe(() => DiceInformation.EndChoice(__state, __exception == null)); return __exception; }
        }
        [HarmonyPatch]
        private static class MenuBuilding
        {
            [HarmonyTargetMethods]
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (MethodInfo method in typeof(MenuDialog).GetMethods(Fields | BindingFlags.DeclaredOnly))
                    if (method.Name == "AddOption") yield return method;
            }
            [HarmonyPrefix]
            private static void Prefix(MenuDialog __instance)
            { if (Enabled && !(__instance is DiceMenuDialog)) DiceInformation.MenuBuilding(); }
        }
    }
}
