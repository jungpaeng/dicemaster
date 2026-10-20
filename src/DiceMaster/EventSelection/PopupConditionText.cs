using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using DiceMaster.LiveLog;

namespace DiceMaster.EventSelection
{
    // Popup descriptions are explanations, never evidence for execution. Keep
    // their failures local and never use the sampled narrative preview here.
    public static class PopupConditionText
    {
        private sealed class Identity : IEqualityComparer<object>
        {
            public new bool Equals(object left, object right) { return ReferenceEquals(left, right); }
            public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
        }

        private sealed class Format
        {
            private readonly HashSet<object> active = new HashSet<object>(new Identity());
            private int remaining = 4096;
            private static object Read(object value, string name) { return LogBridge.ReadFieldStrict(value, name); }
            private static int Number(object value, string name)
            {
                object number = Read(value, name);
                if (number == null) throw new InvalidOperationException();
                return Convert.ToInt32(number, CultureInfo.InvariantCulture);
            }
            private static string N(int value) { return value.ToString(CultureInfo.InvariantCulture); }
            private static IList Items(object value, string name)
            {
                IList list = Read(value, name) as IList;
                if (list == null) throw new InvalidOperationException();
                return list;
            }
            private static string Guard(Func<string> read)
            {
                try { return read(); }
                catch (RandomDisplayValueException) { return "미확정 · 난수"; }
                catch (UnsupportedDisplayStatException) { return "미확인 · 지원하지 않는 계산"; }
                catch (Exception) { return "미확인 · 읽기 실패"; }
            }
            private string Node(object value, Func<string> read)
            {
                if (value == null) return "미확인 · 자료 없음";
                if (remaining-- <= 0) return "미확인 · 표시 한도 초과";
                if (active.Count >= 32 || !active.Add(value)) return "미확인 · 순환 또는 깊이 한도 초과";
                try { return Guard(read); }
                finally { active.Remove(value); }
            }
            private static string RawName(object value)
            {
                try { return Convert.ToString(Read(value, "name"), CultureInfo.InvariantCulture) ?? ""; }
                catch (Exception) { return ""; }
            }
            private static string GroupName(object value)
            {
                string raw = RawName(value);
                if (string.IsNullOrEmpty(raw)) return raw;
                // FormatConditionValName expands GetFlag expressions through
                // flag DevNote translations. A self-referencing translated
                // name can recurse outside this formatter's graph guard.
                // Only finite literal-name lookups are admitted here.
                string translated = NameHelper("DiceMaster.TimeTranslations", "TranslateChineseTimeExpression", raw);
                if (!string.IsNullOrEmpty(translated) && translated != raw) return translated;
                string name = raw.Replace("數值_", "").Replace("時間_", "").Replace("常數_", "");
                bool flag = raw.StartsWith("旗標_", StringComparison.Ordinal);
                if (flag)
                {
                    name = raw.Substring("旗標_".Length);
                    try
                    {
                        IList references = Items(value, "_value");
                        string id = references.Count > 0 ? RawName(Read(references[0], "_flag")) : "";
                        if (!string.IsNullOrEmpty(id)) name = id;
                    }
                    catch (Exception) { }
                }
                else
                {
                    translated = NameHelper("DiceMaster.PlayerinfoTranslations", "TranslatePlayerstats", name);
                    if (!string.IsNullOrEmpty(translated) && translated != name) return translated;
                }
                foreach (string prefix in flag ? new[] { "Flag/Name/" } : new[] { "Flag/Name/", "LegendInfo/", "Character/" })
                {
                    translated = NameHelper("DiceMaster.DataHelper", "GetLocalizedSystemText", prefix + name, name, "팝업 조건 이름", name);
                    if (!string.IsNullOrWhiteSpace(translated) && translated != name)
                        return prefix == "Flag/Name/" ? translated + "(" + name + ")" : translated;
                }
                return raw;
            }
            private static string NameHelper(string owner, string method, params object[] arguments)
            {
                try { return LogBridge.CallDisplayHelper(owner, method, arguments) as string; }
                catch (Exception) { return null; }
            }
            private static string Current(Func<int> read) { return Guard(() => N(read())); }

            public string Condition(object condition)
            {
                return Node(condition, () =>
                {
                    IList comparisons = Items(condition, "_items");
                    int logic = Number(condition, "_logicOp");
                    string separator = logic == 0 ? " AND " : logic == 1 ? " OR " : " [미확인 논리 연산] ";
                    if (comparisons.Count == 0) return "등록된 조건 없음 (불충족)";
                    List<string> result = new List<string>();
                    foreach (object comparison in comparisons)
                    {
                        result.Add("(" + Comparison(comparison) + ")");
                        if (remaining <= 0) { result.Add("미확인 · 이후 조건 표시 한도 초과"); break; }
                    }
                    return string.Join(separator, result);
                });
            }
            private string Comparison(object comparison)
            {
                return Node(comparison, () =>
                {
                    string left = Guard(() => Group(Read(comparison, "_value1")));
                    string right = Guard(() => Group(Read(comparison, "_value2")));
                    string[] operators = { "=", "!=", "<", "<=", ">", ">=" };
                    string op = Guard(() =>
                    {
                        int operation = Number(comparison, "_compareOp");
                        return operation >= 0 && operation < operators.Length ? operators[operation] : "[미확인 비교 연산]";
                    });
                    return left + " " + op + " " + right;
                });
            }
            private string Group(object group)
            {
                return Node(group, () =>
                {
                    string name = GroupName(group);
                    IList references = Items(group, "_value");
                    List<string> expressions = new List<string>();
                    foreach (object reference in references)
                    {
                        expressions.Add(Reference(reference));
                        if (remaining <= 0) { expressions.Add("미확인 · 이후 값 표시 한도 초과"); break; }
                    }
                    int operation = Number(group, "_groupType");
                    string expression = operation == 1 ? string.Join(" + ", expressions) : operation == 4 ? string.Join(" × ", expressions) :
                        (operation == 2 ? "최댓값" : operation == 3 ? "최솟값" : "미확인 그룹 연산") + "(" + string.Join(", ", expressions) + ")";
                    // Show the original expression even when the strict current
                    // value is unavailable. Never synthesize a composite range.
                    string current = Current(() => SafeDisplayEvaluation.ReadGroup(group));
                    return (string.IsNullOrEmpty(name) ? "" : name + ": ") + "[" + expression + "] (현재 " + current + ")";
                });
            }
            private string Reference(object reference)
            {
                return Node(reference, () =>
                {
                    int type = Number(reference, "_checkType");
                    if (type == 7)
                    {
                        int min = Number(reference, "_randomMin"), max = Number(reference, "_randomMax");
                        return "랜덤(" + N(min) + "~" + N(max) + ") · " +
                            (min > max ? "미확인 · 잘못된 범위" : max == int.MaxValue ? "미확정 · 상한 계산 확인 필요" : "미확정");
                    }
                    if (type == 8) return "{" + Group(Read(reference, "_statGroup")) + "}";
                    if (type == 16)
                    {
                        object data = Read(reference, "_condition");
                        string expression = Node(data, () =>
                        {
                            IList items = Items(data, "_items");
                            List<string> conditions = new List<string>();
                            foreach (object item in items)
                            {
                                conditions.Add("(" + Condition(item) + ")");
                                if (remaining <= 0) { conditions.Add("미확인 · 이후 조건 표시 한도 초과"); break; }
                            }
                            return conditions.Count == 0 ? "등록된 조건 없음 (불충족)" : string.Join(" OR ", conditions);
                        });
                        return "조건값 {" + expression + "} (현재 " + Current(() => SafeDisplayEvaluation.ReadReference(reference)) + ")";
                    }
                    if (type < 0 || type > 16) return "미확인 · 지원하지 않는 값 유형 " + N(type);
                    string value = Current(() => SafeDisplayEvaluation.ReadReference(reference));
                    if (type == 0) return value;
                    string[] fields = { "", "_stat", "_relationship", "_flag", "_book", "_misc", "_specialItem", "", "", "_gameTime",
                        "_talentData", "_book", "_upgradeItemData", "_upgradeItemData", "_bookCollection", "_libraryAchieve" };
                    string name = type > 0 && type < fields.Length ? RawName(Read(reference, fields[type])) : "";
                    return (string.IsNullOrEmpty(name) ? "값 유형 " + N(type) : name) + "(" + value + ")";
                });
            }
            public string Event(object data, int? finalRate, out string conditionSummary)
            {
                conditionSummary = "이벤트 조건 미확인";
                if (data == null) return "이벤트 조건: 미확인 · 자료 없음";
                StringBuilder result = new StringBuilder();
                string active = Guard(() => (bool)Read(data, "_toggleCondition") ? Condition(Read(data, "_activeCondition")) : "추가 활성 조건 없음");
                string final = finalRate.HasValue ? N(finalRate.Value) : "미확인";
                conditionSummary = active + " · 가중치 " + final;
                result.AppendLine("활성 조건: " + active);
                result.AppendLine("기본 가중치: " + Guard(() => N(Number(data, "_defaultRate"))));
                result.AppendLine("조건별 가중치:");
                string additions = Guard(() =>
                {
                    IList list = Items(data, "_eventRateItem");
                    if (list.Count == 0) return "없음";
                    List<string> lines = new List<string>();
                    int index = 0;
                    foreach (object item in list)
                    {
                        index++;
                        lines.Add(index + ". " + Guard(() => Condition(Read(item, "Condition"))) + " → " + Guard(() =>
                        {
                            int rate = Number(item, "Rate");
                            return (rate > 0 ? "+" : "") + N(rate);
                        }));
                        if (remaining <= 0) { lines.Add("미확인 · 이후 가중치 표시 한도 초과"); break; }
                    }
                    return string.Join("\n", lines);
                });
                result.AppendLine(additions);
                result.Append("최종 가중치: " + final);
                return result.ToString();
            }
        }

        public static string Condition(object condition) { return new Format().Condition(condition); }
        public static string Event(object data, int? finalRate)
        { string summary; return Event(data, finalRate, out summary); }
        public static string Event(object data, int? finalRate, out string conditionSummary)
        {
            try { return new Format().Event(data, finalRate, out conditionSummary); }
            catch (Exception)
            {
                conditionSummary = "이벤트 조건 미확인 · 읽기 실패";
                return conditionSummary;
            }
        }
    }
}
