using System;
using System.Collections.Generic;
using System.Globalization;
using DiceMaster.LiveLog;
using DiceMaster.LuaParse;
using Fungus;
using Mortal.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DiceMaster.Information
{
    // Observe the game's existing option content update, then decorate only
    // its already-captured text controls. No menu/game action is invoked.
    public static class OptionHintSource
    {
        private sealed class Option
        {
            internal WeakReference Control;
            internal MenuDialog Menu;
            internal string Key;
            internal Text[] Labels;
            internal string[] Original, Applied;
        }
        private static readonly List<Option> options = new List<Option>();
        private static int generation;
        public static Func<bool> Enabled;
        private static bool IsEnabled { get { try { return Enabled == null || Enabled(); } catch (Exception) { return false; } } }
        private static MenuDialog Active
        { get { var menu = MenuDialog.ActiveMenuDialog; return menu != null && menu.gameObject.activeInHierarchy ? menu : null; } }
        public static bool Visible
        {
            get
            {
                if (!IsEnabled || Active == null) return false;
                foreach (Option option in options)
                {
                    var control = option.Control.Target as NormalOptionButton;
                    if (control != null && control.isActiveAndEnabled && control.gameObject.activeInHierarchy) return true;
                }
                return false;
            }
        }
        public static string ContextKey
        { get { var menu = Active; return generation.ToString(CultureInfo.InvariantCulture) + "/" + (menu == null ? "" : menu.GetInstanceID().ToString(CultureInfo.InvariantCulture)); } }
        public static void Observe(NormalOptionButton control)
        {
            if (control == null) return;
            Option found = null;
            foreach (Option option in options) if (ReferenceEquals(option.Control.Target, control)) { found = option; break; }
            if (found == null) { found = new Option { Control = new WeakReference(control) }; options.Add(found); }
            if (!IsEnabled) return; // weak lifecycle identity only while OFF
            Restore(found);
            found.Key = null; found.Labels = null; found.Original = found.Applied = null;
            Capture(found, control, Active);
            generation++;
        }
        private static void Capture(Option option, NormalOptionButton control, MenuDialog menu)
        {
            if (menu == null || !control.isActiveAndEnabled || !control.transform.IsChildOf(menu.transform)) return;
            var source = OneShotProof.Field(control, "_source") as Text;
            var labels = OneShotProof.Field(control, "_text") as Text[];
            if (source == null || labels == null || string.IsNullOrEmpty(source.text) || !source.text.StartsWith("O_", StringComparison.Ordinal)) return;
            option.Menu = menu; option.Key = source.text; option.Labels = labels;
            option.Original = new string[labels.Length]; option.Applied = new string[labels.Length];
            for (int index = 0; index < labels.Length; index++) if (labels[index] != null) option.Original[index] = labels[index].text;
        }
        public static List<DisplayQuery> Discover()
        {
            var queries = new List<DisplayQuery>();
            MenuDialog menu = Active;
            if (!IsEnabled || menu == null) return queries;
            for (int index = options.Count - 1; index >= 0; index--)
                if (options[index].Control.Target as NormalOptionButton == null) options.RemoveAt(index);
            foreach (Option option in options)
            {
                var control = option.Control.Target as NormalOptionButton;
                if (control == null || !control.isActiveAndEnabled || !control.gameObject.activeInHierarchy || !control.transform.IsChildOf(menu.transform)) continue;
                if (option.Key == null || !ReferenceEquals(option.Menu, menu)) Capture(option, control, menu);
                if (option.Key == null) continue;
                Option captured = option;
                string key = option.Key;
                queries.Add(new DisplayQuery("option:" + control.GetInstanceID() + "/" + key, () =>
                {
                    if (!IsEnabled || !ReferenceEquals(Active, captured.Menu) || !control.isActiveAndEnabled || captured.Key != key) throw new DataNotReadyException();
                    List<DiceMaster.LuaParse.Action> actions;
                    if (!LuaParser.Instance.OptionResult.TryGetValue(key, out actions)) throw new DataNotReadyException();
                    OneShotResult proof = OneShotProof.ForOption(key, actions);
                    if (proof.Failed) throw new InvalidOperationException("Option proof read failed.");
                    return proof;
                }));
            }
            return queries;
        }
        public static void RenderHints()
        {
            if (!IsEnabled || Active == null) return;
            foreach (Option option in options)
            {
                // These are captured UI references and cached proof text only.
                // This path never reads _source, conditions, scripts, or actions.
                if (option.Key == null || option.Labels == null || !ReferenceEquals(option.Menu, Active)) continue;
                string tag = OneShotProof.CachedOptionTag(option.Key);
                for (int index = 0; index < option.Labels.Length; index++)
                {
                    Text text = option.Labels[index]; if (text == null) continue;
                    string expected = option.Applied[index] ?? option.Original[index];
                    if (text.text != expected) continue; // another content update owns it
                    string value = option.Original[index] + tag;
                    if (text.text != value) text.text = value;
                    option.Applied[index] = value;
                }
            }
        }
        private static void Restore(Option option)
        {
            if (option.Labels == null || option.Applied == null) return;
            for (int index = 0; index < option.Labels.Length; index++)
                if (option.Labels[index] != null && option.Applied[index] != null && option.Labels[index].text == option.Applied[index])
                    option.Labels[index].text = option.Original[index];
        }
        public static void Clear()
        {
            bool changed = false;
            foreach (Option option in options)
            {
                changed |= option.Key != null || option.Labels != null;
                Restore(option); option.Key = null; option.Menu = null; option.Labels = null; option.Original = option.Applied = null;
            }
            if (changed) generation++;
        }
        public static void MenuCleared(MenuDialog menu)
        {
            bool changed = false;
            foreach (Option option in options)
                if (ReferenceEquals(option.Menu, menu))
                { Restore(option); option.Key = null; option.Menu = null; option.Labels = null; option.Original = option.Applied = null; changed = true; }
            if (changed) { generation++; OneShotProof.ClearOptions(); }
        }
    }
}
