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
            MapGuideSource.Selected = new object();
            InformationRuntime.TestMap(new InformationRow("map/one", "선택 장소", "지도", "후보 1", "전체 상세", "후보 1 · 진행 가능 1", MapGuideSource.Selected, true));
            int measures = GUIStyle.Measurements;
            for (int i = 0; i < 1000; i++) { GUI.PassiveLabels.Clear(); InformationRuntime.Draw(false, false, default, i); }
            Check(GUIStyle.Measurements == measures + 1, "1000 GUI calls measure the unchanged panel once");
            Check(GUI.PassiveLabels.Count == 1 && GUI.PassiveLabels[0].Text.StartsWith("선택 장소: 선택 장소\n", StringComparison.Ordinal), "one explicitly named selected-place panel only");
            Rect stable = GUI.PassiveLabels[0].Rect;
            InformationRuntime.Draw(false, false, default, 1001);
            Check(GUI.PassiveLabels[1].Rect == stable, "stable repaint retains exact placement");
            MapGuideSource.Selected = new object(); GUI.PassiveLabels.Clear();
            InformationRuntime.Draw(false, false, default, 1002);
            Check(GUI.PassiveLabels.Count == 0, "selection change hides the previous publication before refresh");
            MapGuideSource.Selected = null; InformationRuntime.Draw(false, false, default, 1003);
            Check(GUI.PassiveLabels.Count == 0, "deselected place has no panel");
            MapGuideSource.Selected = new object();
            var notices = new List<string>();
            for (int i = 0; i < 10; i++) notices.Add(MapGuidePresentation.Notice(new string('가', 200) + i, "보조 임무", 0, true, true, "대기 등록"));
            InformationRuntime.TestMap(new InformationRow("map/two", "새 장소", "지도", "", "조건과 효과", MapGuidePresentation.Summary(notices, true, false), MapGuideSource.Selected));
            InformationRuntime.Draw(false, false, default, 1004);
            Check(GUI.PassiveLabels.Count == 1 && GUI.PassiveLabels[0].Text.Contains("외 7개") && GUI.PassiveLabels[0].Text.EndsWith("자세히: 로그 → 도구 → 지도 안내"), "long lists keep complete statuses, remaining count and detail path");
            measures = GUIStyle.Measurements; Screen.width = 800; Screen.height = 600;
            GUI.PassiveLabels.Clear(); InformationRuntime.Draw(false, false, default, 1005);
            Check(GUIStyle.Measurements > measures && GUI.PassiveLabels[0].Rect.width <= 800, "resize recomputes bounded whole-row geometry");
            var passiveStyle = new GUIStyle(GUI.skin.box) { fontSize = Math.Min(24, Math.Max(12, DiceMaster.Config.ConfigManager.LogFontSize.Value)), wordWrap = true, richText = false };
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
            Check(GUI.PassiveLabels.Count == 1 && GUI.PassiveLabels[0].Text.Contains("게임 데이터 대기 중", StringComparison.Ordinal), "first-read waiting is visible without inventing a zero-candidate success");
            InformationRuntime.TestGlobal(); GUI.PassiveLabels.Clear();
            InformationRuntime.Draw(false, false, default, 1010);
            Check(GUI.PassiveLabels.Count == 0, "no-selection snapshot with global rows never creates a selected-place panel");
            Check(InformationRuntime.TestErrors.Count == 0, "overlay boundaries complete without swallowed failures");
        }
        finally { Screen.width = width; Screen.height = height; InformationRuntime.TestMap(null); MapGuideSource.Selected = null; GUI.PassiveLabels.Clear(); }
    }
}
namespace UnityEngine { public static class Application { public static bool isFocused = true; } }
namespace DiceMaster.Information
{
    public static class MapGuideSource
    {
        public static object Selected, CurrentAnchor;
        public static string CurrentId;
        public static bool IsCurrent(string id) => Selected != null && ReferenceEquals(Selected, CurrentAnchor) && id == CurrentId;
        public static bool IsCurrent(InformationRow row) => Selected != null && ReferenceEquals(row.Anchor, Selected);
    }
    public static class BranchHistory { public static InformationRow Latest(float now) => null; }
    public static partial class InformationRuntime
    {
        private sealed class Row { internal string Id; internal object Value; internal string Status = ""; }
        private sealed class Page { internal bool Visible; internal List<Row> Rows = new List<Row>(); }
        private static readonly Page map = new Page();
        public static bool[] TestFlags = new[] { true, true, false, true };
        public static readonly List<string> TestErrors = new List<string>();
        public static bool Enabled(int index) => TestFlags[index];
        private static void Failure(string source, Exception error) { TestErrors.Add(source + error); }
        public static void TestMap(InformationRow row, string status = "") { map.Rows.Clear(); map.Visible = row != null; if (row != null) { MapGuideSource.CurrentAnchor = row.Anchor; MapGuideSource.CurrentId = row.Id; var snapshot = new MapGuideSnapshot { Summary = row, HasSelection = true }; snapshot.Entries.Add(row); snapshot.Entries.Add(new InformationRow("other", "장소 무관", "임무", "대기", "상세")); map.Rows.Add(new Row { Id = row.Id, Value = snapshot, Status = status }); } }
        public static void TestWaiting(string id) { MapGuideSource.CurrentAnchor = MapGuideSource.Selected; MapGuideSource.CurrentId = id; map.Rows.Clear(); map.Visible = true; map.Rows.Add(new Row { Id = id, Status = "게임 데이터 대기 중" }); }
        public static void TestGlobal() { var snapshot = new MapGuideSnapshot { HasSelection = false }; snapshot.Entries.Add(new InformationRow("global", "장소 무관", "임무", "대기", "상세")); map.Rows.Clear(); map.Visible = true; map.Rows.Add(new Row { Id = MapGuideSource.CurrentId, Value = snapshot }); }
    }
}
