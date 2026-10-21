using System;
using DiceMaster.Information;
using UnityEngine;
using UnityEngine.EventSystems;

internal static partial class Program
{
    private static void AnchoredNotices()
    {
        int width = Screen.width, height = Screen.height;
        var oldEvent = Event.current;
        var system = EventSystem.current;
        try
        {
            Screen.width = 1000; Screen.height = 700;
            var viewport = new Rect(0, 0, 1000, 700);
            Check(InformationLayout.Near(new Rect(100, 200, 100, 30), viewport, 200, 100) == new Rect(208, 200, 200, 100), "adjacent panel prefers free space immediately right of its native anchor");
            Check(InformationLayout.Near(new Rect(900, 200, 80, 30), viewport, 200, 100) == new Rect(692, 200, 200, 100), "right-edge focus puts panel immediately left without obscuring the native item");
            Rect bottom = InformationLayout.Near(new Rect(400, 680, 80, 20), viewport, 950, 100);
            Check(bottom.x >= 0 && bottom.y >= 0 && bottom.x + bottom.width <= 1000 && bottom.y + bottom.height <= 700 && bottom.y + bottom.height <= 680, "wide bottom-edge description uses space above and remains onscreen");
            Rect invalid = InformationLayout.Near(new Rect(float.NaN, 0, 2, 2), viewport, 200, 100);
            Check(invalid == new Rect(0, 0, 200, 100), "invalid anchor layout has a deterministic finite fallback");
            var tracker = new InformationFocus(); object padA = new object(), padB = new object();
            Check(!tracker.Pointer(padA, new Vector2(10, 10), true), "initial native pad focus wins over parked pointer");
            Check(tracker.Pointer(padA, new Vector2(50, 50), true), "actual pointer movement enables hover descriptions");
            Check(!tracker.Pointer(padB, new Vector2(50, 50), true) && !tracker.Pointer(padB, new Vector2(50, 50), true), "native focus movement immediately regains ownership from a stationary mouse");
            tracker.Clear(); Check(tracker.Pointer(null, new Vector2(10, 10), true), "pointer-only map can reveal an unfocused place description");

            var first = UiAnchor(100, 200); var second = UiAnchor(700, 400);
            Rect projected;
            Check(InformationAnchors.TryRect(first, out projected) && projected == new Rect(100, 200, 120, 32), "real geometry helper projects overlay canvas world corners into top-left GUI coordinates");
            first.gameObject.activeInHierarchy = false; Check(!InformationAnchors.TryRect(first, out projected), "inactive hierarchy cannot retain a marker"); first.gameObject.activeInHierarchy = true;
            first.transform.position = new Vector3(2000, 100, 1); Check(!InformationAnchors.TryRect(first, out projected), "offscreen anchor has no indicator"); first.transform.position = new Vector3(100, 468, 1);
            var world = new Mortal.Free.PositionController { _freeManager = new Mortal.Free.FreeManager { _mainCamera = new Camera { Project = p => new Vector3(p.x + 10, p.y + 20, p.z) } } };
            world.transform.position = new Vector3(90, 180, 1);
            Check(InformationAnchors.TryRect(world, out projected) && projected == new Rect(88, 488, 24, 24), "map geometry uses manager camera and actual live transform without scene scan");
            world.transform.position = new Vector3(90, 180, -1); Check(!InformationAnchors.TryRect(world, out projected), "behind-camera native geometry is suppressed");
            world._colliders = new Component[] { first, second };
            Check(InformationAnchors.TryRect(world, out projected) && projected == new Rect(100, 200, 720, 232), "native collider UI bounds are combined instead of guessed from the controller origin");
            system.currentSelectedGameObject = first.gameObject;
            Check(InformationAnchors.IsFocused(world) && InformationAnchors.IsFocused(first) && !InformationAnchors.IsFocused(second), "native selection recognizes the actual place collider and selected option");

            MapGuideSource.Selected = null; InformationRuntime.TestMap(null); InformationRuntime.TestChoices(null);
            ChoiceStorySource.MenuVisible = false; InformationRuntime.TestFlags = new[] { true, true, false, true };
            var markers = new MapMarkerSnapshot();
            markers.Entries.Add(new InformationRow("marker/a", "첫 장소", "지도", "확인된 기회 2", "상세", "• 첫 임무 · 보조 임무 · 진행 가능\n• 첫 이야기 · 1회성 이야기 · 선택 후 진행 가능", first, true, true));
            markers.Entries.Add(new InformationRow("marker/b", "둘째 장소", "지도", "확인된 기회 1", "상세", "• 둘째 임무 · 보조 임무 · 진행 가능", second, true, true));
            InformationRuntime.TestMarkers(markers); Event.current = new Event { mousePosition = new Vector2(0, 0), type = EventType.Repaint };
            void Draw() { GUI.PassiveLabels.Clear(); GUI.BadgeLabels.Clear(); GUI.BadgeStems.Clear(); InformationRuntime.Draw(false, false, default, 2000); }
            Draw();
            Check(GUI.BadgeStems.Count == 2 && GUI.BadgeLabels[0].Text == "image", "map opportunities use the shared image and one short connector per place");
            Check(GUI.BadgeLabels.Count == 2 && GUI.PassiveLabels.Count == 1 && GUI.PassiveLabels[0].Text.Contains("첫 장소") && GUI.PassiveLabels[0].Text.Contains("첫 이야기"), "all ready places show passive dots while only native focus reveals its multiple opportunities");
            var freeWindow = new Mortal.Free.FreeManager(); Mortal.Free.FreeManager.Instance = freeWindow;
            freeWindow._statOpen = true; Draw();
            Check(GUI.BadgeLabels.Count == 0 && GUI.PassiveLabels.Count == 0 && first.gameObject.activeInHierarchy, "status panel hides cached map pins and description although underlying place objects remain active");
            freeWindow._statOpen = false; Draw();
            Check(GUI.BadgeLabels.Count == 2 && GUI.PassiveLabels.Count == 1, "closing native status restores only current map opportunities");
            Rect original = GUI.PassiveLabels[0].Rect; int measures = GUIStyle.Measurements;
            first.transform.position = new Vector3(150, 468, 1); Draw();
            Check(GUI.PassiveLabels[0].Rect.x == original.x + 50 && GUIStyle.Measurements == measures, "moving native anchor repositions cached text without measuring again");
            Rect secondPin = GUI.BadgeLabels[1].Rect;
            Event.current.mousePosition = new Vector2(secondPin.x + 10, secondPin.y + 10); Draw();
            Check(GUI.PassiveLabels.Count == 1 && GUI.PassiveLabels[0].Text.Contains("둘째 장소"), "hover uses the new top-center map pin bounds outside the native clickable area");
            Event.current.mousePosition = new Vector2(710, 410); Draw();
            Check(GUI.PassiveLabels.Count == 1 && GUI.PassiveLabels[0].Text.Contains("둘째 장소"), "moving mouse reveals another actual place beside its dot");
            system.currentSelectedGameObject = second.gameObject; Draw();
            system.currentSelectedGameObject = first.gameObject; Draw();
            Check(GUI.PassiveLabels.Count == 1 && GUI.PassiveLabels[0].Text.Contains("첫 장소"), "moving pad focus is honored even while pointer remains over another dot");
            var disabled = new UnityEngine.UI.Selectable { gameObject = second.gameObject, Interactable = false }; second.gameObject.Components.Add(disabled); Draw();
            Check(GUI.BadgeLabels.Count == 1, "native disabled option receives no actionable red dot");
            InformationRuntime.TestMarkers(markers, "갱신 실패 · 이전 값"); Draw();
            Check(GUI.BadgeLabels.Count == 0 && GUI.PassiveLabels.Count == 0, "failed global refresh cannot retain positive red markers or old actionable tooltip");
            InformationRuntime.TestMarkers(markers); GUI.BadgeLabels.Clear(); GUI.PassiveLabels.Clear();
            InformationRuntime.Draw(true, false, default, 2001); InformationRuntime.Draw(false, true, default, 2001);
            Check(GUI.BadgeLabels.Count == 0 && GUI.PassiveLabels.Count == 0, "logs and modals hide dots and attached descriptions");
            var choices = new ChoiceStorySnapshot(); choices.Entries.Add(markers.Entries[0]); choices.Entries.Add(new InformationRow("ordinary", "일반 대화", "연결", "진행 가능", "상세", "일반 대화 → 이야기 · 진행 가능", second, true));
            ChoiceStorySource.Current = choices; ChoiceStorySource.Selected = choices.Entries[0]; ChoiceStorySource.MenuVisible = true; InformationRuntime.TestChoices(choices); Draw();
            var storyWindow = new Mortal.Story.StoryManager(); Mortal.Story.StoryManager.Instance = storyWindow;
            storyWindow._panelOpen = true; Draw();
            Check(GUI.BadgeLabels.Count == 0 && GUI.PassiveLabels.Count == 0, "native story window also hides the underlying action-menu badge and path");
            storyWindow._panelOpen = false; Draw();
            Check(GUI.BadgeStems.Count == 0 && GUI.BadgeLabels[0].Rect.x > 250, "choice menu places a corner image without a map connector");
            Check(GUI.BadgeLabels.Count == 1 && GUI.PassiveLabels.Count == 1 && GUI.PassiveLabels[0].Text.StartsWith("현재 선택의 안내"), "choice screen suppresses underlying map and only confirmed choices get dots");
            ChoiceStorySource.Selected = choices.Entries[1]; Draw();
            Check(GUI.BadgeLabels.Count == 1 && GUI.PassiveLabels[0].Text.Contains("일반 대화") && GUI.PassiveLabels[0].Text.Contains("<color=#BFFFCC>진행 가능</color>"), "ordinary confirmed dialogue remains readable in green without granting another red dot");
            var blocked = new InformationRow("blocked/detail", "진행 불가 대화", "인물 대화", "지금 진행 불가", "상세", "", second);
            choices.Entries[1] = blocked; ChoiceStorySource.Selected = blocked;
            Event.current = new Event { mousePosition = new Vector2(710, 410), type = EventType.MouseMove }; Draw();
            Check(GUI.PassiveLabels.Count == 0, "a blocked detail row cannot revive an empty tooltip through native focus or moved pointer");
            Check(Event.current.type == EventType.MouseMove && ReferenceEquals(system.currentSelectedGameObject, first.gameObject), "passive rendering neither consumes native input nor changes selected GameObject");
            Event.current.type = EventType.Repaint; Draw();
            Check(GUI.PassiveLabels.Count == 0 && GUI.BadgeLabels.Count == 1, "hiding the blocked tooltip preserves an independent opportunity badge on repaint");
        }
        finally
        {
            Screen.width = width; Screen.height = height; Event.current = oldEvent; system.currentSelectedGameObject = null;
            InformationRuntime.TestMarkers(null); InformationRuntime.TestMap(null); InformationRuntime.TestChoices(null); MapGuideSource.Selected = null;
            ChoiceStorySource.MenuVisible = false; ChoiceStorySource.Current = null; ChoiceStorySource.Selected = null; GUI.PassiveLabels.Clear(); GUI.BadgeLabels.Clear();
            Mortal.Free.FreeManager.Instance = null; Mortal.Story.StoryManager.Instance = null;
        }
        StoryBadgeRendering();
        NativeInformationWindows();
    }
}
