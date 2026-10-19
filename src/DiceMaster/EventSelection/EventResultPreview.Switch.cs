using System;
using DiceMaster.LiveLog;

namespace DiceMaster.EventSelection
{
    public sealed partial class EventResultPreview
    {
        // The caller supplies the body of this exact Switch invocation's
        // selected arm. A checkpoint-name cache cannot identify that arm.
        internal static EventResultPreview ReadSwitchBody(string sourceName, string body)
        {
            if (body == null) return Unavailable;
            try
            {
                var analysis = PreviewAnalyzer.Analyze(sourceName, body, DataHelper.ReadPreviewScript);
                // This is a possible future path. Keep current-value hints
                // out of cached display data and never run the game branch.
                analysis.SnapshotAllowed = false;
                return new EventResultPreview(PreviewDisplay.Describe(analysis), false);
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
