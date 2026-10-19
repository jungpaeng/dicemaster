using System;
using System.Collections;
using System.Collections.Generic;
using DiceMaster.LiveLog;

namespace DiceMaster.EventSelection
{
    // This boundary never invokes gameplay GetRate/GetValue/GetResult/Check.
    // In particular, RandomPreview's sampled description is not evidence of
    // eligibility. Strict reads classify random/unsupported graphs as unknown.
    public static class EventEligibility
    {
        private static object Read(object target, string field) { return LogBridge.ReadFieldStrict(target, field); }

        public static EventEligibilitySnapshot Evaluate(object eventData)
        {
            if (eventData == null) return EventEligibilitySnapshot.Unreadable();
            try
            {
                if ((bool)Read(eventData, "_toggleCondition"))
                {
                    object condition = Read(eventData, "_activeCondition");
                    if (!SafeDisplayEvaluation.CheckCondition(condition))
                        return new EventEligibilitySnapshot(EventEligibilityState.Ineligible,
                            EventEligibilityReason.ConditionsNotMet, 0, UnmetSummary(condition));
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

        private static string UnmetSummary(object condition)
        {
            IList items = Read(condition, "_items") as IList;
            if (items == null || items.Count == 0) return "활성 조건이 충족되지 않았습니다.";
            var unmet = new List<string>();
            for (int index = 0; index < items.Count && unmet.Count < 3; index++)
            {
                object comparison = items[index];
                if (SafeDisplayEvaluation.CheckComparison(comparison)) continue;
                int left = SafeDisplayEvaluation.ReadGroup(Read(comparison, "_value1"));
                int right = SafeDisplayEvaluation.ReadGroup(Read(comparison, "_value2"));
                int op = Convert.ToInt32(Read(comparison, "_compareOp"));
                string[] operators = { "=", "≠", "<", "≤", ">", "≥" };
                unmet.Add("조건 " + (index + 1) + ": " + left + " " + operators[op] + " " + right);
            }
            return unmet.Count == 0 ? "활성 조건이 충족되지 않았습니다." :
                "미충족 · " + string.Join(" / ", unmet.ToArray());
        }
    }
}
