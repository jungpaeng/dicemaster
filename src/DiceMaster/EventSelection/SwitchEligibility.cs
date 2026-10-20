using System;
using System.Collections;
using System.Collections.Generic;
using DiceMaster.LiveLog;

namespace DiceMaster.EventSelection
{
    // A display-only view of the next Switch judgment. Both the popup and
    // live log use the same ordered verdicts; this never executes a branch,
    // advances its count, samples random values or grants execution authority.
    public static class SwitchEligibility
    {
        private static object Read(object value, string name) { return LogBridge.ReadFieldStrict(value, name); }

        private static EventEligibilitySnapshot Condition(object condition, bool countMode, int nextCount)
        {
            // Keep the condition alongside its verdict, including unknown/read
            // failures. A result description alone cannot explain this route.
            string text = ConditionText(condition, countMode, nextCount);
            try
            {
                bool eligible;
                if (!countMode) eligible = SafeDisplayEvaluation.CheckCondition(condition);
                else
                {
                    int right = Convert.ToInt32(Read(condition, "_value"));
                    switch (Convert.ToInt32(Read(condition, "_compareOp")))
                    {
                        case 0: eligible = nextCount == right; break;
                        case 1: eligible = nextCount != right; break;
                        case 2: eligible = nextCount < right; break;
                        case 3: eligible = nextCount <= right; break;
                        case 4: eligible = nextCount > right; break;
                        case 5: eligible = nextCount >= right; break;
                        default: throw new InvalidOperationException("Unreadable Switch comparison.");
                    }
                }
                return new EventEligibilitySnapshot(eligible ? EventEligibilityState.Eligible : EventEligibilityState.Ineligible,
                    eligible ? EventEligibilityReason.None : EventEligibilityReason.ConditionsNotMet, null,
                    eligible ? text : "미충족 · " + text, detail: text);
            }
            catch (RandomDisplayValueException)
            { return new EventEligibilitySnapshot(EventEligibilityState.Unknown, EventEligibilityReason.Random, null, "? 난수 · " + text, detail: text); }
            catch (UnsupportedDisplayStatException)
            { return new EventEligibilitySnapshot(EventEligibilityState.Unknown, EventEligibilityReason.Unsupported, null, "? 미지원 계산 · " + text, detail: text); }
            catch (InvalidOperationException error)
            {
                if (error.Message.StartsWith("Unknown display", StringComparison.Ordinal))
                    return new EventEligibilitySnapshot(EventEligibilityState.Unknown, EventEligibilityReason.Unsupported, null, "? 미지원 계산 · " + text, detail: text);
                return EventEligibilitySnapshot.Unreadable("! 분기 조건 확인 필요", text);
            }
            catch (Exception) { return EventEligibilitySnapshot.Unreadable("! 분기 조건 확인 필요", text); }
        }

        private static string ConditionText(object condition, bool countMode, int nextCount)
        {
            try
            {
                if (countMode)
                {
                    string[] operators = { "=", "≠", "<", "≤", ">", "≥" };
                    int op = Convert.ToInt32(Read(condition, "_compareOp"));
                    return "이번 실행 횟수 " + nextCount + " " + operators[op] + " " + Convert.ToInt32(Read(condition, "_value"));
                }
                string text = PopupConditionText.Condition(condition);
                return string.IsNullOrWhiteSpace(text) ? "조건 설명이 비어 있습니다." : text;
            }
            catch (Exception) { return "조건 설명을 읽지 못했습니다."; }
        }

        public static List<EventEligibilitySnapshot> Evaluate(object pool, bool useCount, out int count)
        {
            count = (int)Read(pool, "_count");
            var items = Read(pool, useCount ? "_countItems" : "_items") as IList;
            if (items == null) throw new InvalidOperationException("Missing Switch branch collection.");
            var result = new List<EventEligibilitySnapshot>();
            bool previousTrue = false, previousUnknown = false, previousFailure = false;
            EventEligibilityReason previousUnknownReason = EventEligibilityReason.None;
            for (int index = 0; index <= items.Count; index++)
            {
                EventEligibilitySnapshot condition = index == items.Count ?
                    new EventEligibilitySnapshot(EventEligibilityState.Eligible, EventEligibilityReason.None, null,
                        items.Count == 0 ? "앞 분기가 없는 기본 분기입니다." : "앞의 1~" + items.Count + "번 분기 조건이 모두 미충족일 때 진행합니다.") :
                    Condition(items[index], useCount, unchecked(count + 1));
                EventEligibilitySnapshot snapshot;
                if (!condition.CanUseResult || previousFailure) snapshot = EventEligibilitySnapshot.Unreadable("! 이 분기 또는 앞 분기 확인 필요",
                    "이 분기 또는 앞 분기의 조건을 읽지 못했습니다.\n" + condition.Detail);
                else if (previousTrue) snapshot = new EventEligibilitySnapshot(EventEligibilityState.Ineligible, EventEligibilityReason.ConditionsNotMet, null,
                    "앞 분기 우선 · " + condition.Detail, detail: "앞 분기가 먼저 선택됩니다.\n" + condition.Detail);
                else if (!condition.IsEligible && condition.Reason == EventEligibilityReason.ConditionsNotMet) snapshot = condition;
                else if (previousUnknown) snapshot = new EventEligibilitySnapshot(EventEligibilityState.Unknown, previousUnknownReason, null,
                    "? 앞 분기 미확정 · " + condition.Detail, detail: "앞 분기의 판정이 확정되지 않았습니다.\n" + condition.Detail);
                else snapshot = condition;
                result.Add(snapshot);
                previousFailure |= !condition.CanUseResult;
                previousTrue |= condition.IsEligible;
                if (!previousUnknown && condition.State == EventEligibilityState.Unknown) previousUnknownReason = condition.Reason;
                previousUnknown |= condition.State == EventEligibilityState.Unknown;
            }
            return result;
        }
    }
}
