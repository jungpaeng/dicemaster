using System;
using System.Collections.Generic;
using System.Text;

namespace DiceMaster.Information
{
    // One scheduled read owns both surfaces; rendering never discovers entries.
    public sealed class MapGuideSnapshot
    {
        public InformationRow Summary;
        public readonly List<InformationRow> Entries = new List<InformationRow>();
        public bool HasSelection;
        public string ContextKey;
    }

    // Passive UI shows a few complete, actionable rows. Source counts and
    // analysis diagnostics belong to the existing scrollable detail page.
    public static class MapGuidePresentation
    {
        public static string Notice(string name, string kind, int rank, bool waiting, bool mission, string sourceStatus)
        {
            string status = rank == 1 ? "지금 진행 불가" : rank == 2 ? "진행 여부 확인 필요" :
                waiting ? "진행 가능" : mission ? "발생 조건 충족" :
                (sourceStatus ?? "").Contains("선택 후") ? "선택 후 진행 가능" : "진행 조건 충족";
            string title = name ?? "";
            if (title.Length > 28) title = title.Substring(0, 27) + "…";
            return title + " · " + kind + " · " + status;
        }
        public static string Summary(IList<string> notices, bool guide, bool preparing)
        {
            int count = notices == null ? 0 : notices.Count;
            if (!guide && count == 0) return "";
            var text = new StringBuilder();
            if (count == 0) text.Append(preparing ? "이 장소의 안내를 확인하는 중" : "확인된 임무·1회성 이야기 없음");
            for (int index = 0; index < Math.Min(3, count); index++)
            {
                if (text.Length != 0) text.Append('\n');
                text.Append("• ").Append(notices[index]);
            }
            if (count > 3) text.Append("\n외 ").Append(count - 3).Append("개 · 상세에서 확인");
            text.Append("\n자세히: 로그 → 도구 → 지도 안내");
            return text.ToString();
        }
        public static string Fit(string value, Func<string, float> height, float available)
        {
            var lines = new List<string>((value ?? "").Split('\n'));
            while (height(string.Join("\n", lines)) > available)
            {
                int last = lines.FindLastIndex(line => line.StartsWith("• ", StringComparison.Ordinal));
                if (last < 0) break;
                lines.RemoveAt(last);
                lines.RemoveAll(line => line.StartsWith("외 ", StringComparison.Ordinal) || line == "나머지 항목은 상세에서 확인");
                lines.Insert(Math.Min(lines.Count, Math.Max(1, lines.Count - 1)), "나머지 항목은 상세에서 확인");
            }
            string result = string.Join("\n", lines);
            if (height(result) <= available) return result;
            result = (lines.Count == 0 ? "지도 안내" : lines[0]) + "\n로그 → 도구 → 지도 안내";
            if (height(result) <= available) return result;
            result = "지도 안내 · 로그에서 상세 조회";
            return height(result) <= available ? result : "";
        }
    }
}
