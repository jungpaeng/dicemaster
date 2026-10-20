using System;
using System.Text.RegularExpressions;
using DiceMaster.LiveLog;
using DiceMaster.LuaParse;

namespace DiceMaster.EventSelection
{
    // Immutable display data, independent of eligibility and confirmation.
    // Read the exact live pool's result key, never an event-name/prefix cache.
    public sealed partial class EventResultPreview
    {
        private static readonly Regex Tags = new Regex(@"</?(?:color|size|b|i|material)(?:=[^<>]*)?>", RegexOptions.IgnoreCase);
        private static readonly Regex Spaces = new Regex(@"\s+");
        public readonly string Summary, Detail;
        public readonly string RichSummary, RichDetail;
        public readonly bool Retry;
        public static readonly EventResultPreview Pending = new EventResultPreview("결과 확인 중입니다.", true);
        public static readonly EventResultPreview Unavailable = new EventResultPreview("결과를 읽을 수 없어 예상 변화를 확인하지 못했습니다.", false);

        private EventResultPreview(string detail, bool retry)
        {
            detail = DisplayNames.Text(detail);
            RichDetail = NormalizeRich(detail).Trim();
            if (RichDetail.Length == 0) RichDetail = "결과는 진행 후 확인";
            RichSummary = FitSummary(Spaces.Replace(RichDetail, " "), 160);
            Detail = Tags.Replace(detail ?? "", "").Trim();
            if (Detail.Length == 0) Detail = "결과는 진행 후 확인";
            string summary = Spaces.Replace(Detail, " ");
            int length = Math.Min(160, summary.Length);
            if (length < summary.Length && length > 0 && char.IsHighSurrogate(summary[length - 1])) length--;
            Summary = summary.Substring(0, length) + (length < summary.Length ? "…" : "");
            Retry = retry;
        }

        internal static EventResultPreview Read(string result)
        {
            if (string.IsNullOrEmpty(result)) return Unavailable;
            try
            {
                DisplayNames.RefreshMappings();
                int pending = DisplayNames.PendingNames;
                var actions = PositionEventCache.GetDefinitionParser().ExtractActionsFromLua(result);
                LuaParser.RefreshPreviewReads(actions);
                string detail = LuaParser.Description(actions);
                return new EventResultPreview(detail, DisplayNames.PendingNames != pending);
            }
            catch (PreviewReadException error)
            {
                string state = error.IsWaiting ? "결과 자료 준비 중 · 일부 결과는 아직 확인하지 못했습니다."
                    : "결과 설명 일부를 읽지 못했습니다. 결과는 진행 후 확인하세요.";
                return new EventResultPreview(state + (string.IsNullOrWhiteSpace(error.PartialDescription) ? "" : "\n" + error.PartialDescription), true);
            }
            catch (Exception)
            {
                return new EventResultPreview("결과 설명을 읽지 못했습니다. 결과는 진행 후 확인하세요.", true);
            }
        }
    }
}
