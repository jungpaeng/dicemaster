using System;
using System.Reflection;
using DiceMaster.Information;
using Mortal.Free;
using Mortal.Story;

internal static partial class Program
{
    private static void NativeInformationWindows()
    {
        try
        {
            Check(!InformationVisibility.Blocked, "no live native window owner is not itself an occlusion");
            var free = new FreeManager(); FreeManager.Instance = free;
            var story = new StoryManager(); StoryManager.Instance = story;
            foreach (var owner in new object[] { free, story })
            foreach (var field in owner.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (field.FieldType != typeof(bool) || !field.Name.EndsWith("Open")) continue;
                field.SetValue(owner, true);
                Check(InformationVisibility.Blocked, "each actual root/inner window flag independently hides information: " + field.Name);
                field.SetValue(owner, false);
                Check(!InformationVisibility.Blocked, "closing the window restores native visibility without counters: " + field.Name);
            }
            free._statOpen = true; free.gameObject.activeInHierarchy = false;
            Check(!InformationVisibility.Blocked, "inactive prior-scene owner cannot block a current menu");
            free.gameObject.activeInHierarchy = true; free._statOpen = false;
            var place = new PositionController { _freeManager = free };
            place.transform.position = new UnityEngine.Vector3(100, 100, 1);
            UnityEngine.Rect rect;
            Check(InformationAnchors.IsVisible(place), "idle live map has visible geometry");
            free.IsProcessing = true;
            Check(!InformationVisibility.Blocked, "map processing alone does not hide a legitimate action-menu choice screen");
            Check(!InformationAnchors.IsVisible(place) && !InformationAnchors.IsInteractive(place) && InformationAnchors.TryRect(place, out rect), "processing suppresses a place even though its transform still projects");
            free.IsProcessing = false; free.EnableHotKey = false;
            Check(!InformationAnchors.IsVisible(place), "map startup/panel hotkey gate also suppresses active place objects");
            free.EnableHotKey = true;
            Check(InformationAnchors.IsVisible(place), "native map readiness restores current place visibility");
            Check(!free._menuOpen && !free._statOpen && !free._saveOpen && !story._panelOpen, "visibility reads never open or close native windows");
        }
        finally { FreeManager.Instance = null; StoryManager.Instance = null; }
    }
}
