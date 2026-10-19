using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DiceMaster.LiveLog;
using Mortal.Story;
using UnityEngine;

namespace DiceMaster.EventSelection
{
    public static partial class EventSelectionRuntime
    {
        private static SwitchResultData ResolveSwitch(CheckPointManager manager, string name, out SwitchResultConfig config)
        {
            config = null;
            var list = Field(manager, "_switchList") as SwitchResultConfig[];
            if (list == null) return null;
            foreach (SwitchResultConfig item in list)
            {
                if (item == null) continue;
                SwitchResultData result = item.GetData(name);
                if (result == null) continue;
                if (result.GetType() != typeof(SwitchResultData)) return null;
                config = item; return result;
            }
            return null;
        }

        private static bool ValidSwitch(ActionContext action)
        {
            if (action.Epoch != epoch || action.Manager == null || !ReferenceEquals(CheckPointManager.Instance, action.Manager)) return false;
            SwitchResultConfig config;
            var pool = ResolveSwitch(action.Manager, action.Name, out config);
            return ReferenceEquals(pool, action.SwitchPool) && ReferenceEquals(config, action.SwitchConfig) &&
                (bool)Field(pool, "_useCount") == action.SwitchUseCount;
        }

        private static IList SwitchItems(ActionContext action)
        {
            var items = Field(action.SwitchPool, action.SwitchUseCount ? "_countItems" : "_items") as IList;
            if (items == null) throw new InvalidOperationException("Missing Switch branch collection.");
            return items;
        }

        private static object SwitchTarget(ActionContext action, int result)
        {
            IList items = SwitchItems(action);
            return result > 0 && result <= items.Count ? items[result - 1] : result == items.Count + 1 ? action.SwitchPool : null;
        }

        private static EventEligibilitySnapshot SwitchCondition(object condition, bool countMode, int nextCount)
        {
            try
            {
                bool eligible;
                if (!countMode) eligible = SafeDisplayEvaluation.CheckCondition(condition);
                else
                {
                    int right = Convert.ToInt32(Field(condition, "_value"));
                    switch (Convert.ToInt32(Field(condition, "_compareOp")))
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
                    (eligible ? "현재 조건 충족 · " : "현재 조건 미충족 · ") + SwitchConditionText(condition, countMode, nextCount));
            }
            catch (RandomDisplayValueException)
            { return new EventEligibilitySnapshot(EventEligibilityState.Unknown, EventEligibilityReason.Random, null, "난수 조건은 원래 판정 전에 확정할 수 없습니다. " + SwitchConditionText(condition, countMode, nextCount)); }
            catch (UnsupportedDisplayStatException)
            { return new EventEligibilitySnapshot(EventEligibilityState.Unknown, EventEligibilityReason.Unsupported, null, "지원하지 않는 조건 계산입니다."); }
            catch (InvalidOperationException error)
            {
                if (error.Message.StartsWith("Unknown display", StringComparison.Ordinal))
                    return new EventEligibilitySnapshot(EventEligibilityState.Unknown, EventEligibilityReason.Unsupported, null, "지원하지 않는 조건 계산입니다.");
                return EventEligibilitySnapshot.Unreadable("분기 조건을 읽지 못했습니다.");
            }
            catch (Exception) { return EventEligibilitySnapshot.Unreadable("분기 조건을 읽지 못했습니다."); }
        }

        private static string SwitchConditionText(object condition, bool countMode, int nextCount)
        {
            try
            {
                if (countMode)
                {
                    string[] operators = { "=", "≠", "<", "≤", ">", "≥" };
                    int op = Convert.ToInt32(Field(condition, "_compareOp"));
                    return "이번 실행 횟수 " + nextCount + " " + operators[op] + " " + Convert.ToInt32(Field(condition, "_value"));
                }
                bool ignored;
                return EventResultPreview.PlainText(DataHelper.ExtractConditions((Mortal.Core.ConditionResultItem)condition, out ignored));
            }
            catch (Exception) { return "조건 설명은 조건 탭에서 확인하세요."; }
        }

        private static List<EventEligibilitySnapshot> SwitchEligibility(ActionContext action, out int count)
        {
            count = (int)Field(action.SwitchPool, "_count");
            IList items = SwitchItems(action);
            var result = new List<EventEligibilitySnapshot>();
            bool previousTrue = false, previousUnknown = false, previousFailure = false;
            for (int index = 0; index <= items.Count; index++)
            {
                EventEligibilitySnapshot condition = index == items.Count ?
                    new EventEligibilitySnapshot(EventEligibilityState.Eligible, EventEligibilityReason.None, null, "위 분기가 모두 불충족일 때 진행합니다.") :
                    SwitchCondition(items[index], action.SwitchUseCount, unchecked(count + 1));
                EventEligibilitySnapshot snapshot;
                if (!condition.CanUseResult || previousFailure) snapshot = EventEligibilitySnapshot.Unreadable("이 분기 또는 앞 분기의 조건을 읽지 못했습니다.");
                else if (previousTrue) snapshot = new EventEligibilitySnapshot(EventEligibilityState.Ineligible, EventEligibilityReason.ConditionsNotMet, null, "앞 분기가 먼저 선택됩니다. " + condition.Summary);
                else if (!condition.IsEligible && condition.Reason == EventEligibilityReason.ConditionsNotMet) snapshot = condition;
                else if (previousUnknown) snapshot = new EventEligibilitySnapshot(EventEligibilityState.Unknown, EventEligibilityReason.Random, null, "앞 분기의 판정이 확정되지 않았습니다. " + condition.Summary);
                else snapshot = condition;
                result.Add(snapshot);
                previousFailure |= !condition.CanUseResult;
                previousTrue |= condition.IsEligible;
                previousUnknown |= condition.State == EventEligibilityState.Unknown;
            }
            return result;
        }

        private static void RefreshSwitch(ActionContext action, float now, int previewBudget)
        {
            int count;
            List<EventEligibilitySnapshot> states = SwitchEligibility(action, out count);
            action.SwitchCount = count;
            var result = new List<EventCandidate>();
            for (int index = 0; index < states.Count; index++)
            {
                int branch = index + 1;
                string body = action.SwitchSite.Body(branch);
                if (body == null) continue; // Do not offer a return value that this exact Lua site does not handle.
                object target = SwitchTarget(action, branch);
                if (target == null) continue;
                string id = action.Token + "/switch/" + branch;
                EligibilityRead read;
                if (!action.Reads.TryGetValue(body, out read)) { read = new EligibilityRead(); action.Reads.Add(body, read); }
                if (read.Preview == null) read.Preview = EventResultPreview.Pending;
                if (previewBudget > 0 && now >= read.PreviewRetryAt)
                {
                    previewBudget--;
                    read.Preview = EventResultPreview.ReadSwitchBody(action.Name + "/" + branch, body);
                    read.PreviewRetryAt = read.Preview.Retry ? now + 5f : float.PositiveInfinity;
                }
                result.Add(new EventCandidate(id, action.Name + ": 분기 " + branch,
                    branch.ToString(CultureInfo.InvariantCulture), target, states[index], read.Preview, count));
            }
            candidates = result.AsReadOnly();
        }

        internal static bool WaitSwitch(string name, long script, int site, object coroutine, long scriptEpoch, EventSwitchSite definition)
        {
            if (scriptEpoch != epoch) return true;
            if (current != null && (!ReferenceEquals(current.Coroutine, coroutine) || current.Script != script || current.Site != site || current.SwitchPool == null)) return false;
            try
            {
                if (BypassForReservation(true, name)) return false;
                if (current == null)
                {
                    if (!IsEnabled(true) || definition == null) return false;
                    CheckPointManager manager = CheckPointManager.Instance;
                    if (manager == null) return false;
                    SwitchResultConfig config;
                    var pool = ResolveSwitch(manager, name, out config);
                    if (pool == null) return false;
                    var action = new ActionContext { Token = checked(++sequence), Script = script, Site = site, Epoch = epoch,
                        Coroutine = coroutine, Manager = manager, Name = name, SwitchConfig = config, SwitchPool = pool,
                        SwitchUseCount = (bool)Field(pool, "_useCount"), SwitchSite = definition };
                    RefreshSwitch(action, Time.unscaledTime, 0);
                    if (candidates.Count < 2) { candidates = Empty; return false; }
                    current = action;
                    Status = "이번 조건 판정의 분기를 선택하세요.";
                    SetWaiting(true);
                }
                ActionContext active = current;
                if (!Valid(active)) { Invalidate("분기 또는 게임 문맥 변경"); return true; }
                if (!IsEnabled(true)) ReleaseRandom("switch-setting-disabled");
                if (active.RandomRequested) ReleaseRandom("user");
                if (active.Released) { active.Permit = true; SetWaiting(false); return false; }
                if (active.SelectedId != null && PrepareSelection(active))
                { active.Permit = true; SetWaiting(false); return false; }
                return true;
            }
            catch (Exception error)
            {
                Failure("switch-execution-gate", error);
                if (current != null) { ReleaseRandom("switch-read-failure"); current.Permit = true; }
                else candidates = Empty;
                SetWaiting(false); return false;
            }
        }

        private static bool PrepareSwitchSelection(ActionContext action)
        {
            EventCandidate confirmed = action.SelectedCandidate;
            action.SelectedId = null; action.SelectedCandidate = null;
            int branch, count;
            var states = SwitchEligibility(action, out count);
            if (confirmed == null || !int.TryParse(confirmed.Result, out branch) || branch < 1 || branch > states.Count ||
                action.SwitchSite.Body(branch) == null || confirmed.Revision != count || !ReferenceEquals(SwitchTarget(action, branch), confirmed.Target))
            { Status = "분기 대상 또는 실행 횟수가 바뀌었습니다. 다시 선택하세요."; RefreshSwitch(action, Time.unscaledTime, 0); return false; }
            EventEligibilitySnapshot state = states[branch - 1];
            if (action.Force ? !state.CanUseResult || state.IsEligible : !state.IsEligible)
            { Status = "현재 분기 조건이 바뀌었습니다. 다시 선택하세요."; RefreshSwitch(action, Time.unscaledTime, 0); return false; }
            action.SwitchCount = count; action.SwitchDirectTarget = confirmed.Target; action.SwitchDirectResult = branch;
            return true;
        }

        internal static ActionContextHandle BeginSwitch(CheckPointManager manager, string name)
        {
            ActionContextHandle reserved = BeginReserved(true, manager, name);
            if (reserved != null) return reserved;
            ActionContext action = current;
            if (action == null || action.SwitchPool == null || !action.Permit || action.Executing || !ReferenceEquals(manager, action.Manager) || action.Name != name) return null;
            action.Permit = false; action.Executing = true;
            return new ActionContextHandle(action.Token);
        }

        internal static bool TryDirectSwitchResult(SwitchResultData pool, out int result)
        {
            result = 0;
            ActionContext action = current;
            if (action == null || !action.Executing || action.Consumed || !ReferenceEquals(pool, action.SwitchPool)) return false;
            action.Consumed = true;
            if (action.Released || action.SwitchDirectResult < 1) return false;
            if (!Valid(action) || (int)Field(pool, "_count") != action.SwitchCount ||
                !ReferenceEquals(SwitchTarget(action, action.SwitchDirectResult), action.SwitchDirectTarget))
                throw new InvalidOperationException("Selected Switch changed during execution boundary.");
            // This is the only original Execute side effect retained for a direct
            // branch: no condition, nested counter, or game RNG is evaluated.
            var field = pool.GetType().GetField("_count", Fields);
            if (field == null) throw new MissingFieldException(pool.GetType().FullName, "_count");
            field.SetValue(pool, unchecked(action.SwitchCount + 1));
            result = action.SwitchDirectResult;
            Log("consumed token=" + action.Token + " kind=switch mode=" + (action.Force ? "force" : "normal") + " result=" + result);
            return true;
        }
    }
}
