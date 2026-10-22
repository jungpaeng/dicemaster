using System;
using System.Collections;
using System.Collections.Generic;
using DiceMaster.Information;
using UnityEngine;

internal static partial class Program
{
    private sealed class CountedRects : IList<Rect>
    {
        public int Reads; public int Count => 500; public bool IsReadOnly => true;
        public Rect this[int i] { get { Reads++; return new Rect(0, 0, 200, 100); } set { throw new NotSupportedException(); } }
        public IEnumerator<Rect> GetEnumerator() { throw new Exception("No obstacle grid enumeration"); }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(Rect x) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(Rect x) => false; public void CopyTo(Rect[] x,int i) => throw new NotSupportedException();
        public int IndexOf(Rect x) => -1; public void Insert(int i,Rect x) => throw new NotSupportedException();
        public bool Remove(Rect x) => false; public void RemoveAt(int i) => throw new NotSupportedException();
    }
    private static UnityEngine.Behaviour UiAnchor(float x = 600, float y = 300)
    { var canvas = new Canvas(); var component = new Behaviour(); component.gameObject.transform = new RectTransform { Owner=component.gameObject, Parent=canvas.transform, position=new Vector3(x, Screen.height-y-32, 1) }; return component; }
    private static void InformationLayoutGeometry()
    {
        var viewport = new Rect(0, 0, 200, 100);
        var preferred = new Rect(12, 12, 70, 20);
        Check(InformationLayout.Place(preferred, viewport, null) == preferred, "selected-place panel has a stable fixed position");
        Check(InformationLayout.Place(new Rect(-900, 1000, 700, 2000), new Rect(10, 20, 90, 70), null) == new Rect(10, 20, 90, 70), "large text stays inside the screen");
        var log = new[] { new Rect(100, 0, 100, 100) };
        Rect moved = InformationLayout.Place(new Rect(150, 0, 50, 25), viewport, log);
        Check(moved == new Rect(0, 0, 50, 25), "branch notice can use a fixed opposite corner to avoid the log");
        var obstacles = new CountedRects();
        Rect first = InformationLayout.Place(preferred, viewport, obstacles);
        Check(obstacles.Reads <= 5 * obstacles.Count, "five candidate placements at most, even on a fully blocked dense map");
        Check(first == InformationLayout.Place(preferred, viewport, obstacles), "blocked-screen fallback never wanders");
        foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -500f, 0f, 12f, float.MaxValue })
        {
            Rect actual = InformationLayout.Place(new Rect(value, value, value, value), viewport, null);
            Check(!float.IsNaN(actual.x) && !float.IsInfinity(actual.x) && actual.x >= 0 && actual.y >= 0 &&
                actual.width >= 0 && actual.height >= 0 && (double)actual.x + actual.width <= 200 &&
                (double)actual.y + actual.height <= 100, "invalid/extreme geometry is finite and clipped");
        }
        InformationOverlayCaching();
    }
    private static void InformationOverlayCaching()
    {
        int width = Screen.width, height = Screen.height;
        try
        {
            Screen.width = 1920; Screen.height = 1080;
            InformationRuntime.TestFlags = new[] { true, true, false, true };
            MapGuideSource.Selected = UiAnchor();
            var initial = new InformationRow("map/one", "선택 장소", "지도", "후보 1", "전체 상세",
                "• <color=red>사사형</color> · 인물 대화 · 진입 조건 충족 (현재)\n• 다른 인물 · 인물 대화 · 진입 조건 미충족 (현재)\n• 미확인 · 인물 대화 · 선택 화면에서 확인\n자세히: 로그 → 도구 → 지도 안내", MapGuideSource.Selected, true);
            InformationRuntime.TestMap(initial);
            int measures = GUIStyle.Measurements;
            for (int i = 0; i < 1000; i++) { GUI.PassiveLabels.Clear(); InformationRuntime.Draw(false, false, default, i); }
            Check(GUIStyle.Measurements == measures + 1, "1000 GUI calls measure the unchanged panel once");
            Check(GUI.PassiveLabels.Count == 1 && GUI.PassiveLabels[0].Text.StartsWith("선택 장소: 선택 장소\n", StringComparison.Ordinal), "one explicitly named selected-place panel only");
            Check(GUI.PassiveLabels[0].RichText && GUI.PassiveLabels[0].Text.Contains("<color=#BFFFCC>진입 조건 충족 (현재)</color>") &&
                !GUI.PassiveLabels[0].Text.Contains("<color=#BFFFCC>진입 조건 미충족") && GUI.PassiveLabels[0].Text.Contains("＜color=red＞사사형＜/color＞"),
                "only the met entry status is green; mixed statuses and literal title markup cannot recolor the panel");
            Rect stable = GUI.PassiveLabels[0].Rect;
            InformationRuntime.Draw(false, false, default, 1001);
            Check(GUI.PassiveLabels[1].Rect == stable, "stable repaint retains exact placement");
            InformationRuntime.TestMap(initial, "갱신 실패 · 이전 값"); GUI.PassiveLabels.Clear();
            InformationRuntime.Draw(false, false, default, 1001.5f);
            Check(!GUI.PassiveLabels[0].Text.Contains("<color="), "a failed refresh never colors an old positive entry as current");
            InformationRuntime.TestMap(new InformationRow("map/conditions", "상태 구분", "지도", "", "상세",
                "• 대화 · 인물 대화 · 진행 가능\n• 임무 · 주 임무 · 발생 조건 충족\n• 미확정 · 인물 대화 · 진행 가능성 있음\n• 차단 · 인물 대화 · 지금 진행 불가", MapGuideSource.Selected));
            GUI.PassiveLabels.Clear(); InformationRuntime.Draw(false, false, default, 1001.6f);
            Check(GUI.PassiveLabels[0].Text.Contains("<color=#BFFFCC>진행 가능</color>") &&
                GUI.PassiveLabels[0].Text.Contains("<color=#BFFFCC>발생 조건 충족</color>") &&
                !GUI.PassiveLabels[0].Text.Contains("<color=#BFFFCC>진행 가능성 있음") &&
                !GUI.PassiveLabels[0].Text.Contains("<color=#BFFFCC>지금 진행 불가"),
                "confirmed availability and met mission conditions are green while possibility and blocked statuses remain neutral");
            MapGuideSource.Selected = UiAnchor(); GUI.PassiveLabels.Clear();
            InformationRuntime.Draw(false, false, default, 1002);
            Check(GUI.PassiveLabels.Count == 0, "selection change hides the previous publication before refresh");
            MapGuideSource.Selected = null; InformationRuntime.Draw(false, false, default, 1003);
            Check(GUI.PassiveLabels.Count == 0, "deselected place has no panel");
            MapGuideSource.Selected = UiAnchor();
            var notices = new List<string>();
            for (int i = 0; i < 10; i++) notices.Add(MapGuidePresentation.Notice(new string('가', 200) + i, "보조 임무", 0, true, true, "대기 등록"));
            InformationRuntime.TestMap(new InformationRow("map/two", "새 장소", "지도", "", "조건과 효과", MapGuidePresentation.Summary(notices, true, false), MapGuideSource.Selected));
            InformationRuntime.Draw(false, false, default, 1004);
            Check(GUI.PassiveLabels.Count == 1 && GUI.PassiveLabels[0].Text.Contains("외 7개") && GUI.PassiveLabels[0].Text.EndsWith("자세히: 로그 → 도구 → 지도 안내"), "long lists keep complete statuses, remaining count and detail path");
            measures = GUIStyle.Measurements; Screen.width = 800; Screen.height = 600;
            ((Behaviour)MapGuideSource.Selected).transform.position = new Vector3(600, 268, 1);
            GUI.PassiveLabels.Clear(); InformationRuntime.Draw(false, false, default, 1005);
            Check(GUIStyle.Measurements > measures && GUI.PassiveLabels[0].Rect.width <= 800, "resize recomputes bounded whole-row geometry");
            var passiveStyle = new GUIStyle(GUI.skin.box) { fontSize = Math.Min(24, Math.Max(12, DiceMaster.Config.ConfigManager.LogFontSize.Value)), wordWrap = true, richText = GUI.PassiveLabels[0].RichText };
            Check(passiveStyle.CalcHeight(new GUIContent(GUI.PassiveLabels[0].Text), GUI.PassiveLabels[0].Rect.width) <= GUI.PassiveLabels[0].Rect.height + .01f,
                "small-screen map notice fits its actual box height instead of clipping the final rows");
            Check(GUI.PassiveLabels[0].Text.EndsWith("자세히: 로그 → 도구 → 지도 안내") && GUI.PassiveLabels[0].Text.Contains("상세에서 확인"),
                "height fitting omits whole entries and retains the complete detail navigation hint");
            measures = GUIStyle.Measurements;
            InformationRuntime.Draw(false, false, default, 1005.1f);
            Check(GUIStyle.Measurements == measures, "warm fitted notice performs no additional text measurement");
            measures = GUIStyle.Measurements; GUI.PassiveLabels.Clear();
            InformationRuntime.Draw(true, false, new Rect(20, 20, 700, 500), 1006);
            InformationRuntime.Draw(false, true, default, 1007);
            Check(GUI.PassiveLabels.Count == 0 && GUIStyle.Measurements == measures, "log and popup hide map without measuring");
            InformationRuntime.TestFlags[0] = InformationRuntime.TestFlags[1] = false;
            InformationRuntime.Draw(false, false, default, 1008);
            Check(GUI.PassiveLabels.Count == 0, "both map features OFF stop drawing");
            InformationRuntime.TestFlags[0] = true;
            InformationRuntime.TestWaiting("waiting"); GUI.PassiveLabels.Clear();
            InformationRuntime.Draw(false, false, default, 1009);
            Check(GUI.PassiveLabels.Count == 0, "first-read waiting no longer invents a generic focus tooltip; loading comes from marker snapshots");
            InformationRuntime.TestGlobal(); GUI.PassiveLabels.Clear();
            InformationRuntime.Draw(false, false, default, 1010);
            Check(GUI.PassiveLabels.Count == 0, "no-selection snapshot with global rows never creates a selected-place panel");
            Screen.width = 1920; Screen.height = 1080;
            var choice = new ChoiceStorySnapshot();
            var firstChoice = new InformationRow("one", "사사형", "연결 이야기", "조건 충족", "전체 경로", "사사형 → 대화 항목 · 진행 가능", UiAnchor(), true);
            var nextChoice = new InformationRow("two", "다른 항목", "연결 이야기", "후보", "전체 경로", "경로 후보: 다른 항목 → 이야기 · 진행 가능성 있음", UiAnchor(600, 400));
            choice.Entries.Add(firstChoice); choice.Entries.Add(nextChoice);
            ChoiceStorySource.MenuVisible = true; ChoiceStorySource.Current = choice; ChoiceStorySource.Selected = firstChoice;
            InformationRuntime.TestChoices(choice); GUI.PassiveLabels.Clear();
            InformationRuntime.Draw(false, false, default, 1011);
            Check(GUI.PassiveLabels.Count == 1 && GUI.PassiveLabels[0].Text.StartsWith("현재 선택의 안내") && GUI.PassiveLabels[0].Text.Contains("사사형 → 대화 항목") && !GUI.PassiveLabels[0].Text.Contains("다른 항목"), "choice overlay follows only the published current pad selection");
            Check(GUI.PassiveLabels[0].RichText && GUI.PassiveLabels[0].Text.Contains("<color=#BFFFCC>진행 가능</color>"),
                "the current choice's positive entry status uses the same green text");
            measures = GUIStyle.Measurements;
            for (int at = 0; at < 1000; at++) { GUI.PassiveLabels.Clear(); InformationRuntime.Draw(false, false, default, 1012); }
            Check(GUIStyle.Measurements == measures, "one thousand unchanged choice draws reuse measured cached text");
            ChoiceStorySource.Selected = nextChoice; GUI.PassiveLabels.Clear(); InformationRuntime.Draw(false, false, default, 1013);
            Check(GUI.PassiveLabels.Count == 1 && GUI.PassiveLabels[0].Text.Contains("다른 항목") && !GUI.PassiveLabels[0].Text.Contains("사사형"), "changing native selection switches cached path without a provider query");
            Check(!GUI.PassiveLabels[0].Text.Contains("<color="), "an uncertain choice path retains its neutral status color");
            ChoiceStorySource.Current = null; GUI.PassiveLabels.Clear(); InformationRuntime.Draw(false, false, default, 1014);
            Check(GUI.PassiveLabels.Count == 0, "old menu publication is hidden immediately and cannot reveal an underlying map");
            InformationRuntime.TestFlags[0] = false; InformationRuntime.TestFlags[1] = true;
            MapGuideSource.Selected = UiAnchor(); InformationRuntime.TestMap(new InformationRow("back", "뒤 지도", "지도", "가능", "상세", "지도 후보", MapGuideSource.Selected));
            InformationRuntime.Draw(false, false, default, 1015);
            Check(GUI.PassiveLabels.Count == 0, "physical menu occludes the one-shot map panel even while map guide is OFF");
            ChoiceStorySource.MenuVisible = false; InformationRuntime.TestChoices(null); GUI.PassiveLabels.Clear();
            InformationRuntime.Draw(false, false, default, 1016);
            Check(GUI.PassiveLabels.Count == 1 && GUI.PassiveLabels[0].Text.StartsWith("선택 장소: 뒤 지도"), "closing the physical menu restores current map information");
            Check(InformationRuntime.TestErrors.Count == 0, "overlay boundaries complete without swallowed failures");
        }
        finally { Screen.width = width; Screen.height = height; InformationRuntime.TestMap(null); InformationRuntime.TestChoices(null); ChoiceStorySource.MenuVisible = false; MapGuideSource.Selected = null; GUI.PassiveLabels.Clear(); }
        AnchoredNotices();
    }
}
namespace UnityEngine { public static class Application { public static bool isFocused = true; } }
namespace DiceMaster.Information
{
    public class ChoiceStorySnapshot { public string ContextKey; public readonly List<object> Preparing = new List<object>(); public readonly List<object> Anchors = new List<object>(); public readonly List<InformationRow> Entries = new List<InformationRow>(); }
    public static class ChoiceStorySource
    {
        public static bool MenuVisible;
        public static ChoiceStorySnapshot Current;
        public static InformationRow Selected;
        public static bool IsCurrent(ChoiceStorySnapshot value) => MenuVisible && ReferenceEquals(Current, value);
        public static InformationRow CurrentRow(ChoiceStorySnapshot value) => IsCurrent(value) ? Selected : null;
    }
    public static class MapGuideSource
    {
        public static object Selected, CurrentAnchor; public static object SelectedAnchor => Selected; public static MapMarkerSnapshot CurrentMarkers; public static bool IsCurrent(MapMarkerSnapshot value)=>ReferenceEquals(CurrentMarkers,value);
        public static string CurrentId;
        public static bool IsCurrent(string id) => Selected != null && ReferenceEquals(Selected, CurrentAnchor) && id == CurrentId;
        public static bool IsCurrent(InformationRow row) => Selected != null && ReferenceEquals(row.Anchor, Selected);
    }
    public static class BranchHistory { public static InformationRow Latest(float now) => null; }
    public static partial class InformationRuntime
    {
        private sealed class Row { internal string Id; internal object Value; internal string Status = ""; }
        private sealed class Page { internal bool Visible; internal List<Row> Rows = new List<Row>(); }
        private static readonly Page map = new Page(), choices = new Page(), markers = new Page(), diceGuide = new Page();
        public static bool[] TestFlags = new[] { true, true, false, true };
        public static readonly List<string> TestErrors = new List<string>();
        public static bool Enabled(int index) => TestFlags[index];
        private static void Failure(string source, Exception error) { TestErrors.Add(source + error); }
        public static void TestMap(InformationRow row, string status = "") { map.Rows.Clear(); map.Visible = row != null; if (row != null) { MapGuideSource.CurrentAnchor = row.Anchor; MapGuideSource.CurrentId = row.Id; var snapshot = new MapGuideSnapshot { Summary = row, HasSelection = true }; snapshot.Entries.Add(row); snapshot.Entries.Add(new InformationRow("other", "장소 무관", "임무", "대기", "상세")); map.Rows.Add(new Row { Id = row.Id, Value = snapshot, Status = status }); } }
        public static void TestWaiting(string id) { MapGuideSource.CurrentAnchor = MapGuideSource.Selected; MapGuideSource.CurrentId = id; map.Rows.Clear(); map.Visible = true; map.Rows.Add(new Row { Id = id, Status = "게임 데이터 대기 중" }); }
        public static void TestGlobal() { var snapshot = new MapGuideSnapshot { HasSelection = false }; snapshot.Entries.Add(new InformationRow("global", "장소 무관", "임무", "대기", "상세")); map.Rows.Clear(); map.Visible = true; map.Rows.Add(new Row { Id = MapGuideSource.CurrentId, Value = snapshot }); }
        public static void TestMarkers(MapMarkerSnapshot value,string status="") { MapGuideSource.CurrentMarkers=value; markers.Visible=value!=null; markers.Rows.Clear();if(value!=null)markers.Rows.Add(new Row { Value=value, Status=status }); }
        public static void TestChoices(ChoiceStorySnapshot value) { choices.Rows.Clear(); choices.Visible = value != null; if (value != null) choices.Rows.Add(new Row { Value = value }); }
    }
}

namespace DiceMaster.Information {
 public sealed class DiceGuideSnapshot {public string ContextKey;public readonly List<InformationRow> Entries=new();public readonly List<object> Preparing=new();}
 public static class DiceGuideSource {public static bool Active;public static string ContextKey="dice";}
 public static partial class InformationRuntime {public static void TestDice(DiceGuideSnapshot value) {diceGuide.Rows.Clear();diceGuide.Visible=value!=null;DiceGuideSource.Active=value!=null;if(value!=null)diceGuide.Rows.Add(new Row{Value=value});}}
}
