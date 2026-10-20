using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using DiceMaster.LiveLog;
using DiceMaster.LuaParse;
using LuaAction = DiceMaster.LuaParse.Action;

namespace DiceMaster.Information
{
    public sealed class OneShotResult
    {
        public readonly bool Proven, Failed;
        public readonly string Status, Detail, DetailText;
        internal OneShotResult(bool proven, string detail, bool failed = false)
        { Proven = proven; Failed = failed; Status = proven ? "1회성" : failed ? "1회성 읽기 실패" : "1회성 확인 불가"; Detail = detail; DetailText = Status + ": " + detail; }
    }

    // A local proof of the PRD's meaning: after this known result completes,
    // its current active condition becomes false. It is not a global claim
    // that a flag can never be reset, or that this is a unique opportunity.
    public static class OneShotProof
    {
        private static readonly Dictionary<object, OneShotResult> events = new Dictionary<object, OneShotResult>();
        private sealed class EventDefinition
        {
            internal string Script;
            internal bool Toggle;
            internal object Condition, Items;
            internal int Count;
        }
        private static readonly Dictionary<object, EventDefinition> eventDefinitions = new Dictionary<object, EventDefinition>();
        private static readonly Dictionary<string, OneShotResult> options = new Dictionary<string, OneShotResult>(StringComparer.Ordinal);
        private static readonly HashSet<string> independentEffects = new HashSet<string>(StringComparer.Ordinal)
        { "Player", "Character", "SetPlayer", "SetCharacter", "AddBook", "AddMisc", "AddSpecial", "RemoveBook", "RemoveMisc", "RemoveSpecial", "AddTalent" };
        public static OneShotResult Unknown(string reason = "활성 조건과 실행 후 변화를 안전하게 연결하지 못했습니다. 반복 가능 판정이 아닙니다.")
        { return new OneShotResult(false, reason); }
        public static string CachedEventTag(object target)
        { OneShotResult value; return target != null && events.TryGetValue(target, out value) && value.Proven ? " · 1회성" : ""; }
        public static string CachedOptionTag(string key)
        { OneShotResult value; return key != null && options.TryGetValue(key, out value) && value.Proven ? " · 1회성" : ""; }
        public static string CachedEventDetail(object target)
        { OneShotResult value; return target != null && events.TryGetValue(target, out value) ? value.DetailText : ""; }
        public static string CachedOptionDetail(string key)
        { OneShotResult value; return key != null && options.TryGetValue(key, out value) ? value.DetailText : ""; }
        public static void Clear() { events.Clear(); eventDefinitions.Clear(); options.Clear(); }
        public static void ClearOptions() { options.Clear(); }
        public static OneShotResult ForOption(string key, IList<LuaAction> actions)
        { return ForOption(key, null, actions); }
        public static OneShotResult ForOption(string key, object activeCondition, IList<LuaAction> actions)
        {
            OneShotResult result = activeCondition == null ? Unknown("선택지의 활성 조건을 안전하게 연결하지 못했습니다. 읽음 여부로 1회성을 판단하지 않습니다.") : Evaluate(activeCondition, actions);
            if (key != null) options[key] = result;
            return result;
        }
        public static OneShotResult ForEvent(object target, string resultScript)
        {
            OneShotResult cached;
            if (TryGetEvent(target, resultScript, out cached)) return cached;
            OneShotResult result;
            EventDefinition definition = null;
            try
            {
                definition = Definition(target, resultScript);
                if (definition == null || !definition.Toggle) result = Unknown("명시적인 활성 조건이 없어 실행 후 비활성화를 증명하지 못했습니다.");
                else if (string.IsNullOrEmpty(resultScript)) result = Unknown("실제 결과 스크립트 연결을 확인하지 못했습니다.");
                else result = Evaluate(definition.Condition, PositionEventCache.GetDefinitionParser().ExtractActionsFromLua(resultScript));
            }
            catch (Exception) { result = new OneShotResult(false, "1회성 판별 자료를 읽지 못했습니다.", true); }
            if (target != null)
            {
                events[target] = result;
                if (!result.Failed && definition != null) eventDefinitions[target] = definition;
                else eventDefinitions.Remove(target);
            }
            return result;
        }

        // The proof reads definition data and constant result effects, never
        // live flag values. Reuse it within the current Lua/map session. A new
        // result or replaced condition invalidates it; failures remain retryable.
        public static bool TryGetEvent(object target, string resultScript, out OneShotResult result)
        {
            result = null;
            EventDefinition previous;
            if (target == null || !eventDefinitions.TryGetValue(target, out previous)) return false;
            try
            {
                EventDefinition current = Definition(target, resultScript);
                if (current != null && current.Script == previous.Script && current.Toggle == previous.Toggle &&
                    ReferenceEquals(current.Condition, previous.Condition) && ReferenceEquals(current.Items, previous.Items) && current.Count == previous.Count &&
                    events.TryGetValue(target, out result) && !result.Failed) return true;
            }
            catch (Exception) { }
            events.Remove(target); eventDefinitions.Remove(target); result = null;
            return false;
        }
        private static EventDefinition Definition(object target, string script)
        {
            if (target == null) return null;
            bool toggle = (bool)Field(target, "_toggleCondition");
            object condition = toggle ? Field(target, "_activeCondition") : null;
            object items = condition == null ? null : Field(condition, "_items");
            return new EventDefinition { Script = script, Toggle = toggle, Condition = condition, Items = items, Count = (items as IList)?.Count ?? -1 };
        }

        public static OneShotResult Evaluate(object activeCondition, IList<LuaAction> actions)
        {
            if (activeCondition == null || actions == null || actions.Count == 0) return Unknown();
            try
            {
                var changes = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (LuaAction action in actions)
                {
                    PreviewAnalysis analysis = action == null ? null : action.Analysis;
                    if (analysis == null || analysis.Issues.Count != 0 || analysis.StoppedAtChoice || analysis.RootConditional)
                        return Unknown("조건부·미지원·선택 대기 또는 불완전한 결과 분석으로 실행 후 상태를 확정하지 못했습니다.");
                    PreviewDisplay.Validate(analysis);
                    if (analysis.Issues.Count != 0) return Unknown();
                    foreach (PreviewEffect effect in analysis.Effects)
                    {
                        if (effect.Conditional || effect.Followup || effect.ExecutionUncertain || effect.Branches.Count != 0 || effect.ValidationError != null)
                            return Unknown("조건부·후속 결과는 실행이 확정되지 않아 1회성 근거로 사용하지 않습니다.");
                        if (PreviewDisplay.IsDisplayOnly(effect)) continue;
                        if (effect.Owner != "statmodifymanager") return Unknown("후속 실행이나 다른 실행 관리자의 영향을 확정하지 못했습니다.");
                        if (effect.Name == "SetFlag")
                            changes[(string)effect.Constants[0]] = (int)effect.Constants[1];
                        else if (effect.Name == "AddFlag")
                        {
                            // AddFlag depends on the live pre-result value. Only
                            // a prior constant SetFlag makes this context-free.
                            string key = (string)effect.Constants[0]; int previous;
                            if (!changes.TryGetValue(key, out previous)) return Unknown("현재값에 의존하는 플래그 증감만으로 실행 후 상태를 확정하지 않습니다.");
                            changes[key] = unchecked(previous + (int)effect.Constants[1]);
                        }
                        else if (!independentEffects.Contains(effect.Name)) return Unknown("플래그에 대한 간접 영향을 확정하지 못했습니다.");
                    }
                }
                if (changes.Count == 0) return Unknown("활성 조건을 끄는 확정 상수 플래그 변경을 찾지 못했습니다.");
                var evaluator = new FutureCondition(changes);
                bool? future = evaluator.Condition(activeCondition);
                return future == false && evaluator.BrokenByChange ? new OneShotResult(true,
                    "결과의 확정 상수 플래그 변경 후 현재 활성 조건이 거짓이 됩니다. 미독·영구 유일 기회 또는 미래 재활성화 불가를 뜻하지 않습니다.") : Unknown();
            }
            catch (Exception) { return new OneShotResult(false, "조건·효과의 자료를 읽지 못해 1회성을 확인하지 못했습니다.", true); }
        }

        internal static object Field(object target, string name)
        {
            if (target == null) throw new DataNotReadyException();
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target);
                // Only Unity's audited nonvirtual name getter is permitted here.
                if (name == "name" && type.FullName == "UnityEngine.Object") return type.GetProperty(name).GetValue(target, null);
            }
            throw new MissingFieldException(target.GetType().FullName, name);
        }
        private static int Number(object target, string field) { return Convert.ToInt32(Field(target, field), CultureInfo.InvariantCulture); }
        private sealed class FutureCondition
        {
            private readonly Dictionary<string, int> changes;
            private readonly HashSet<object> active = new HashSet<object>();
            private int work;
            internal bool BrokenByChange;
            private bool comparisonChanged;
            internal FutureCondition(Dictionary<string, int> changes) { this.changes = changes; }
            private void Enter(object value, string type)
            {
                if (++work > 2048 || value == null || value.GetType().FullName != type || active.Count >= 32 || !active.Add(value))
                    throw new InvalidOperationException("Unsupported proof graph.");
            }
            internal bool? Condition(object condition)
            {
                Enter(condition, "Mortal.Core.ConditionResultItem");
                try
                {
                    IList items = Field(condition, "_items") as IList;
                    if (items == null || items.Count > 256) throw new InvalidOperationException();
                    int logic = Number(condition, "_logicOp");
                    if (logic != 0 && logic != 1) throw new InvalidOperationException();
                    // Game's empty condition list is false for either operator.
                    if (items.Count == 0) return false;
                    bool unknown = false, decisive = false, changedFalse = false, anyChange = false;
                    foreach (object item in items)
                    {
                        comparisonChanged = false;
                        bool? value = Compare(item);
                        anyChange |= comparisonChanged;
                        changedFalse |= comparisonChanged && value == false;
                        if (!value.HasValue) unknown = true;
                        else if (logic == 0 ? !value.Value : value.Value) decisive = true;
                    }
                    bool? result = decisive ? logic == 1 : unknown ? (bool?)null : logic == 0;
                    BrokenByChange = result == false && (logic == 0 ? changedFalse : anyChange);
                    return result;
                }
                finally { active.Remove(condition); }
            }
            private bool? Compare(object comparison)
            {
                Enter(comparison, "Mortal.Core.StatCompareItem");
                try
                {
                    int? left = Group(Field(comparison, "_value1")), right = Group(Field(comparison, "_value2"));
                    if (!left.HasValue || !right.HasValue) return null;
                    switch (Number(comparison, "_compareOp"))
                    {
                        case 0: return left == right; case 1: return left != right;
                        case 2: return left < right; case 3: return left <= right;
                        case 4: return left > right; case 5: return left >= right;
                        default: throw new InvalidOperationException();
                    }
                }
                finally { active.Remove(comparison); }
            }
            private int? Group(object group)
            {
                Enter(group, "Mortal.Core.StatGroupVariable");
                try
                {
                    IList values = Field(group, "_value") as IList;
                    if (values == null || values.Count > 256) throw new InvalidOperationException();
                    int operation = Number(group, "_groupType"), result = operation == 4 ? 1 : 0;
                    if (operation < 1 || operation > 4 || (operation == 2 || operation == 3) && values.Count == 0) throw new InvalidOperationException();
                    bool known = true; int index = 0;
                    foreach (object value in values)
                    {
                        int? number = Reference(value);
                        if (!number.HasValue) known = false;
                        else switch (operation)
                        {
                            case 1: result = checked(result + number.Value); break;
                            case 2: result = index == 0 ? number.Value : Math.Max(result, number.Value); break;
                            case 3: result = index == 0 ? number.Value : Math.Min(result, number.Value); break;
                            case 4: result = unchecked(result * number.Value); break;
                        }
                        index++;
                    }
                    return known ? result : (int?)null;
                }
                finally { active.Remove(group); }
            }
            private int? Reference(object value)
            {
                Enter(value, "Mortal.Core.StatValueReference");
                try
                {
                    int kind = Number(value, "_checkType"), sign = (bool)Field(value, "_negative") ? -1 : 1;
                    if (kind == 0) return unchecked(Number(value, "_constant") * sign);
                    if (kind == 8) return Group(Field(value, "_statGroup")); // original nested-group references ignore _negative
                    if (kind != 3) return null;
                    object flag = Field(value, "_flag");
                    if (flag == null || flag.GetType().FullName != "Mortal.Core.FlagData") throw new InvalidOperationException();
                    string key = (string)Field(flag, "name"); int result;
                    if (!changes.TryGetValue(key, out result)) return null;
                    comparisonChanged = true;
                    return unchecked(result * sign);
                }
                finally { active.Remove(value); }
            }
        }
    }
}
