using System;
using DiceMaster.LiveLog;

namespace DiceMaster.EventSelection
{
    // This boundary never invokes gameplay GetRate/GetValue/GetResult/Check.
    // In particular, RandomPreview's sampled description is not evidence of
    // eligibility. Strict reads classify random/unsupported graphs as unknown.
    public static class EventEligibility
    {
        private static object Read(object target, string field) { return LogBridge.ReadFieldStrict(target, field); }

        // Passive map notices need the decision, not the popup's full condition
        // description. Keep both surfaces on exactly the same strict reader.
        public static EventEligibilitySnapshot EvaluateCurrent(object eventData) => EvaluateState(eventData);

        public static EventEligibilitySnapshot Evaluate(object eventData)
        {
            // Description failures cannot change a decision established by the
            // strict reader. Presentation never supplies execution authority.
            EventEligibilitySnapshot decision = EvaluateState(eventData);
            string conditionSummary, detail;
            try { detail = PopupConditionText.Event(eventData, decision.Rate, out conditionSummary); }
            catch (Exception) { conditionSummary = detail = "조건 설명을 읽지 못했습니다."; }
            if (string.IsNullOrWhiteSpace(conditionSummary)) conditionSummary = "조건 설명을 읽지 못했습니다.";
            if (string.IsNullOrWhiteSpace(detail)) detail = conditionSummary;
            string summary = decision.IsEligible ? conditionSummary :
                decision.Reason == EventEligibilityReason.ConditionsNotMet ? "미충족 · " + conditionSummary :
                decision.Reason == EventEligibilityReason.ZeroWeight ? "가중치 0" :
                decision.Reason == EventEligibilityReason.Random ? "? 난수 · " + conditionSummary :
                decision.Reason == EventEligibilityReason.Unsupported ? "? 미지원 계산 · " + conditionSummary :
                "! 대상/결과 확인 필요 · " + conditionSummary;
            return new EventEligibilitySnapshot(decision.State, decision.Reason, decision.Rate,
                summary, decision.CanUseResult, detail);
        }

        private static EventEligibilitySnapshot EvaluateState(object eventData)
        {
            if (eventData == null) return EventEligibilitySnapshot.Unreadable();
            try
            {
                if ((bool)Read(eventData, "_toggleCondition"))
                {
                    object condition = Read(eventData, "_activeCondition");
                    if (!SafeDisplayEvaluation.CheckCondition(condition))
                        return new EventEligibilitySnapshot(EventEligibilityState.Ineligible,
                            EventEligibilityReason.ConditionsNotMet, 0, "조건 미충족");
                }
                int rate = SafeDisplayEvaluation.ReadEventRate(eventData);
                return rate > 0 ? new EventEligibilitySnapshot(EventEligibilityState.Eligible,
                    EventEligibilityReason.None, rate, "현재 조건 충족 · 가중치 " + rate) :
                    new EventEligibilitySnapshot(EventEligibilityState.Ineligible,
                        EventEligibilityReason.ZeroWeight, 0, "활성 조건은 충족했지만 최종 가중치가 0입니다.");
            }
            catch (RandomDisplayValueException)
            {
                return new EventEligibilitySnapshot(EventEligibilityState.Unknown, EventEligibilityReason.Random,
                    null, "난수에 의존하는 조건·가중치는 실제 판정 전에 확정할 수 없습니다.");
            }
            catch (UnsupportedDisplayStatException)
            {
                return Unsupported();
            }
            catch (InvalidOperationException error)
            {
                // Known but unsupported semantics are different from malformed
                // or unreadable data: only the former can be explicitly forced.
                if (error.Message.StartsWith("Unknown display", StringComparison.Ordinal)) return Unsupported();
                return EventEligibilitySnapshot.Unreadable();
            }
            catch (Exception)
            {
                return EventEligibilitySnapshot.Unreadable();
            }
        }

        private static EventEligibilitySnapshot Unsupported()
        { return new EventEligibilitySnapshot(EventEligibilityState.Unknown, EventEligibilityReason.Unsupported,
            null, "지원하지 않는 조건·가중치 계산으로 발동 자격을 확정할 수 없습니다."); }

    }
}
