namespace DiceMaster.EventSelection
{
    public enum EventEligibilityState { Unknown, Eligible, Ineligible }
    public enum EventEligibilityReason { None, ConditionsNotMet, ZeroWeight, Random, Unsupported, ReadFailure }

    // Eligibility and refresh freshness are separate. A retained last-good
    // snapshot can still explain an event, but cannot authorize its execution.
    public sealed class EventEligibilitySnapshot
    {
        public readonly EventEligibilityState State;
        public readonly EventEligibilityReason Reason;
        public readonly int? Rate;
        public readonly string StatusText, Summary;
        public readonly bool CanUseResult;
        public bool IsEligible { get { return State == EventEligibilityState.Eligible; } }

        public EventEligibilitySnapshot(EventEligibilityState state, EventEligibilityReason reason,
            int? rate, string summary, bool canUseResult = true)
        {
            State = state; Reason = reason; Rate = rate;
            Summary = summary ?? "";
            CanUseResult = canUseResult;
            StatusText = state == EventEligibilityState.Eligible ? "정상 발동 가능" :
                state == EventEligibilityState.Ineligible ? "발동 불가 · " +
                    (reason == EventEligibilityReason.ConditionsNotMet ? "조건 미충족" : "최종 가중치 0") :
                reason == EventEligibilityReason.ReadFailure ? "판정 미확인 · 조회 실패" : "판정 미확인";
        }

        public static EventEligibilitySnapshot Unreadable(string summary = "이벤트 자료를 읽지 못했습니다.")
        { return new EventEligibilitySnapshot(EventEligibilityState.Unknown, EventEligibilityReason.ReadFailure, null, summary, false); }
    }
}
