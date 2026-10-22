using System;
using DiceMaster.Information;
using UnityEngine;
using UnityEngine.EventSystems;

internal static partial class Program
{
    private static void LoadingRendering()
    {
        CardAnchorRendering();
        DiceNoticeRendering();
        int width = Screen.width, height = Screen.height; var oldEvent = Event.current;
        try
        {
            Screen.width = 1920; Screen.height = 1080; Application.isFocused = true;
            InformationRuntime.TestFlags = new[] { true, true, false, true };
            InformationRuntime.TestMap(null); InformationRuntime.TestChoices(null); MapGuideSource.Selected = null;
            ChoiceStorySource.MenuVisible = false; EventSystem.current.currentSelectedGameObject = null;
            Mortal.Free.FreeManager.Instance = null; Mortal.Story.StoryManager.Instance = null;
            var first = UiAnchor(150,300); var second = UiAnchor(650,400);
            var snapshot = new MapMarkerSnapshot { ContextKey = "loading-render" };
            snapshot.Preparing.Add(first); snapshot.Preparing.Add(second); InformationRuntime.TestMarkers(snapshot);
            Event.current = new Event { type = EventType.Repaint, mousePosition = new Vector2(0,0) };
            void Draw(float now, bool log = false) { GUI.Spinners.Clear(); GUI.BadgeLabels.Clear(); GUI.BadgeStems.Clear(); GUI.PassiveLabels.Clear(); InformationRuntime.Draw(log,false,default,now); }
            InformationSpinner.Release(); int creates = Texture2D.Created, uploads = Texture2D.Uploads;
            Draw(0); Draw(.19f);
            Check(GUI.Spinners.Count == 0 && Texture2D.Created == creates, "short map preparation has no loading flash or early texture allocation");
            Draw(.21f);
            Check(GUI.Spinners.Count == 2 && GUI.PassiveLabels.Count == 0 && GUI.BadgeLabels.Count == 0, "unfocused pending map anchors draw spinners and no generic tooltip");
            Check(Texture2D.Created == creates + 1 && Texture2D.Uploads == uploads + 1, "all pending anchors share one prepared spinner atlas");
            var uv = GUI.Spinners[0].Uv; Draw(.4f);
            Check(GUI.Spinners.Count == 2 && GUI.Spinners[0].Uv != uv && Texture2D.Uploads == uploads + 1, "animation selects cached UV frames without uploading/regenerating pixels");
            snapshot.Preparing.Remove(first); snapshot.Entries.Add(new InformationRow("ready", "인물", "연결", "진행 가능성 있음", "", "대화 안내", first, false, false, true));
            Draw(.41f); Check(GUI.Spinners.Count == 1 && GUI.BadgeLabels.Count == 1, "the same publication replaces only the completed anchor's spinner with its actual badge");
            snapshot.Preparing.Clear(); Draw(.42f);
            Check(GUI.Spinners.Count == 0 && GUI.BadgeLabels.Count == 1, "a settled empty anchor vanishes while independent ready badges remain");
            snapshot.Entries.Clear(); snapshot.Preparing.Add(first); InformationRuntime.TestMarkers(snapshot,"갱신 실패 · 이전 값"); Draw(.8f);
            Check(GUI.Spinners.Count == 0, "a failed retained publication cannot keep spinning");
            InformationRuntime.TestMarkers(snapshot); Draw(1); Draw(1.3f); Check(GUI.Spinners.Count == 1, "a new healthy pending interval has its own delay");
            Draw(1.4f,true); Check(GUI.Spinners.Count == 0, "log occlusion hides and clears loading timers");
            Draw(1.5f); Check(GUI.Spinners.Count == 0, "restoring the map does not flash an old elapsed loading timer");
            Draw(1.8f); Check(GUI.Spinners.Count == 1, "ongoing visible work resumes loading after the delay");
            var window = new Mortal.Free.FreeManager { _statOpen = true }; Mortal.Free.FreeManager.Instance = window;
            Draw(1.9f); Check(GUI.Spinners.Count == 0, "native status windows hide pending map spinners even with active underlying geometry");
            window._statOpen = false; Draw(2); Check(GUI.Spinners.Count == 0, "native-window closure starts a fresh loading interval");
            Application.isFocused = false; Draw(2.3f); Check(GUI.Spinners.Count == 0, "lost application focus draws no spinner"); Application.isFocused = true;
            snapshot.ContextKey = "other-save"; Draw(3); Check(GUI.Spinners.Count == 0, "a new save context cannot reuse previous loading timers");
            InformationRuntime.TestMarkers(null);
            var choice = new ChoiceStorySnapshot { ContextKey = "choice-loading" }; choice.Preparing.Add(first); choice.Preparing.Add(second);
            ChoiceStorySource.Current = choice; ChoiceStorySource.MenuVisible = true; ChoiceStorySource.Selected = null; InformationRuntime.TestChoices(choice);
            Draw(4); Draw(4.21f);
            Check(GUI.Spinners.Count == 2 && GUI.PassiveLabels.Count == 0, "actual menu buttons use the same delayed spinner semantics without requiring focus");
            Event.current.type = EventType.MouseMove; int created = Texture2D.Created; Draw(4.3f);
            Check(GUI.Spinners.Count == 0 && Texture2D.Created == created && Event.current.type == EventType.MouseMove && EventSystem.current.currentSelectedGameObject == null,
                "loading paints only on repaint and never consumes mouse input, changes selection or allocates new textures");
            Event.current.type = EventType.Repaint; InformationRuntime.TestFlags[0] = InformationRuntime.TestFlags[1] = false; Draw(4.5f);
            Check(GUI.Spinners.Count == 0, "OFF suppresses pending menu spinners");
        }
        finally
        {
            InformationSpinner.Release(); Screen.width = width; Screen.height = height; Event.current = oldEvent; Application.isFocused = true;
            InformationRuntime.TestMap(null); InformationRuntime.TestMarkers(null); InformationRuntime.TestChoices(null);
            ChoiceStorySource.MenuVisible = false; ChoiceStorySource.Current = null; ChoiceStorySource.Selected = null;
            Mortal.Free.FreeManager.Instance = null; Mortal.Story.StoryManager.Instance = null;
            GUI.Spinners.Clear(); GUI.PassiveLabels.Clear(); GUI.BadgeLabels.Clear();
        }
    }
}
