using System.Reflection;
using Mortal.Free;
using Mortal.Story;
using UnityEngine;

namespace DiceMaster.Information
{
    // Read the game's existing window owners. No scene search, singleton
    // initialization, input capture, time-scale changes or panel bookkeeping.
    public static class InformationVisibility
    {
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFields = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly FieldInfo free = typeof(FreeManager).GetField("<Instance>k__BackingField", StaticFields);
        private static readonly FieldInfo story = typeof(StoryManager).GetField("<Instance>k__BackingField", StaticFields);
        private static readonly FieldInfo[] freePanels = {
            typeof(FreeManager).GetField("_menuOpen", InstanceFields),
            typeof(FreeManager).GetField("_statOpen", InstanceFields),
            typeof(FreeManager).GetField("_saveOpen", InstanceFields)
        };
        private static readonly FieldInfo[] storyPanels = {
            typeof(StoryManager).GetField("_panelOpen", InstanceFields),
            typeof(StoryManager).GetField("_menuOpen", InstanceFields),
            typeof(StoryManager).GetField("_statusOpen", InstanceFields),
            typeof(StoryManager).GetField("_logOpen", InstanceFields)
        };
        public static bool Blocked
        {
            get
            {
                try
                {
                    if (free == null || story == null) return true;
                    return Open(free.GetValue(null) as Behaviour, freePanels) || Open(story.GetValue(null) as Behaviour, storyPanels);
                }
                catch { return true; }
            }
        }
        private static bool Open(Behaviour owner, FieldInfo[] flags)
        {
            if (owner == null || !owner.isActiveAndEnabled || !owner.gameObject.activeInHierarchy) return false;
            foreach (FieldInfo flag in flags)
            {
                if (flag == null) return true;
                object value = flag.GetValue(owner);
                if (!(value is bool) || (bool)value) return true;
            }
            return false;
        }
    }
}
