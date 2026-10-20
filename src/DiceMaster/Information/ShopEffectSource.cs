using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using DiceMaster.LiveLog;
using UnityEngine;
using UnityEngine.UI;

namespace DiceMaster.Information
{
    // Observes the game's existing shop selection callback. It never purchases,
    // uses or learns an item, raises a game event, or invokes a condition getter.
    public static class ShopEffectSource
    {
        public static Func<bool> Enabled = delegate { return false; };
        private static object panel, item;
        private static Text description;
        private static string original = "", appended = "", title = "", observedId = "";
        private static long generation;
        public static bool Visible { get { return Live(panel) && !Gone(item) && description != null && SelectionId(item) == observedId; } }
        public static string ContextKey { get { return "shop:" + generation.ToString(CultureInfo.InvariantCulture); } }

        private static bool Allowed() { try { return Enabled != null && Enabled(); } catch (Exception) { return false; } }
        private static bool Gone(object value)
        { return value == null || value is UnityEngine.Object && (UnityEngine.Object)value == null; }
        private static string SelectionId(object value)
        { try { return Identity(value); } catch (MissingFieldException) { return value == null ? "" : value.GetType().FullName; } }
        private static bool Live(object value)
        {
            UnityEngine.Behaviour behaviour = value as UnityEngine.Behaviour;
            return behaviour != null && behaviour.isActiveAndEnabled;
        }
        private static object Field(object value, string name)
        {
            if (value == null) throw new DataNotReadyException();
            for (Type type = value.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(value);
            }
            throw new MissingFieldException(value.GetType().FullName, name);
        }
        private static int Number(object value, string name) { return Convert.ToInt32(Field(value, name), CultureInfo.InvariantCulture); }
        private static bool Flag(object value, string name) { return (bool)Field(value, name); }
        private static void Exact(object value, string name)
        {
            if (Gone(value)) throw new DataNotReadyException();
            if (value.GetType().FullName != "Mortal.Core." + name) throw new UnsupportedEffectException();
        }
        private static IList List(object value, string name)
        {
            IList list = Field(value, name) as IList;
            if (list == null) throw new DataNotReadyException();
            Type type = list.GetType();
            if (!type.IsArray && (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(List<>))) throw new UnsupportedEffectException();
            if (list.Count > 256) throw new UnsupportedEffectException();
            return list;
        }
        private static string Identity(object value) { return Field(value, "_id") as string ?? ""; }
        private static string Plain(string value) { return (value ?? "").Replace("<", "〈").Replace(">", "〉").Replace("\r", " ").Replace("\n", " "); }
        private static string EnumKey(object value)
        {
            if (value == null || !value.GetType().IsEnum || !Enum.IsDefined(value.GetType(), value)) throw new UnsupportedEffectException();
            FieldInfo field = value.GetType().GetField(value.ToString());
            foreach (CustomAttributeData data in CustomAttributeData.GetCustomAttributes(field))
                if (data.AttributeType.FullName == "OBB.Framework.Attributes.StringValueAttribute" && data.ConstructorArguments.Count == 1)
                    return data.ConstructorArguments[0].Value as string ?? value.ToString();
            return value.ToString();
        }
        private static string StatName(object value) { string key = EnumKey(value); return DisplayNames.LookupName("PlayerStat/" + key, key); }
        private static string ItemName(object value, string kind)
        { string id = Identity(value); return DisplayNames.LookupName(kind + "/Name/" + id, id); }
        private static string Kind(object value)
        {
            if (value == null) return "물품";
            if (value.GetType().FullName == "Mortal.Core.Consumables") return "소모품";
            if (value.GetType().FullName == "Mortal.Core.Book") return Flag(value, "_martial") ? "비급" : "책";
            return "물품";
        }

        public static void ObserveSelection(object owner, object selected)
        {
            if (!Allowed()) { Clear(); return; }
            if (owner == null || owner.GetType().FullName != "Mortal.Story.ShopPanel" || !Live(owner)) return;
            // The original callback has already replaced the game's description.
            // Restore our old suffix only if it is still exactly our text.
            Restore();
            string selectedId = SelectionId(selected);
            bool changed = !ReferenceEquals(panel, owner) || !ReferenceEquals(item, selected) || observedId != selectedId;
            panel = owner; item = selected; observedId = selectedId;
            description = Field(owner, "_dialogDesc") as Text;
            Text heading = Field(owner, "_dialogTitle") as Text;
            title = heading == null ? "" : heading.text;
            original = description == null ? "" : description.text ?? "";
            appended = "";
            if (changed) generation++;
        }
        public static void ObserveClosed(object owner) { if (ReferenceEquals(panel, owner)) Clear(); }
        private static void Restore()
        {
            if (description != null && appended.Length != 0 && description.text == appended) description.text = original;
            appended = "";
        }
        public static void Clear()
        {
            Restore(); panel = null; item = null; description = null; original = ""; title = ""; observedId = ""; generation++;
        }
        public static void RenderSummary(string text)
        {
            if (!Allowed() || !Visible) { Restore(); return; }
            if (!ReferenceEquals(description, Field(panel, "_dialogDesc"))) { Clear(); return; }
            string current = description.text ?? "";
            if (current != appended) original = current;
            if (string.IsNullOrEmpty(text)) { Restore(); return; }
            // The runtime passes only the cached name/kind/status AutoText.
            string next = original + (original.Length == 0 ? "" : "\n\n") + Plain(text);
            if (current != next) description.text = next;
            appended = next;
        }
        public static List<DisplayQuery> Discover()
        {
            List<DisplayQuery> rows = new List<DisplayQuery>();
            if (!Allowed() || !Visible) return rows;
            object selected = item, owner = panel;
            long context = generation;
            string name = title;
            string id = "shop:" + context.ToString(CultureInfo.InvariantCulture);
            rows.Add(new DisplayQuery(id, delegate
            {
                if (!Allowed() || !Visible || generation != context || !ReferenceEquals(item, selected) || !ReferenceEquals(panel, owner))
                    throw new DataNotReadyException();
                return Read(selected, name, id);
            }));
            return rows;
        }
        private sealed class UnsupportedEffectException : InvalidOperationException { }
        private sealed class Reference
        {
            internal object Target;
            internal int Type, Current, Original;
            internal string Name;
            internal bool Martial;
        }
        private sealed class Evaluation
        {
            internal bool Unknown;
            private bool uncertainProjection;
            private readonly Dictionary<object, int> projected = new Dictionary<object, int>();
            internal Reference ReadReference(object value)
            {
                Exact(value, "MartialStatReference");
                int type = Number(value, "_type");
                object target;
                string name;
                int current;
                bool martial = false;
                switch (type)
                {
                    case 1:
                        target = Field(value, "_stat"); Exact(target, "GameStat");
                        current = SafeDisplayEvaluation.ReadStatFinalValue(target);
                        name = StatName(Field(target, "_type")); martial = Number(target, "_property") == 3;
                        break;
                    case 2:
                        target = Field(value, "_relationship"); Exact(target, "RelationshipStat");
                        current = Number(target, "_value");
                        name = DisplayNames.LookupName("Character/" + EnumKey(Field(target, "_type")), EnumKey(Field(target, "_type"))) + " 호감도";
                        break;
                    case 3:
                        target = Field(value, "_talent"); Exact(target, "PlayerTalentData");
                        current = Number(target, "_level"); name = DisplayNames.LookupName("PlayerTalent/Name/" + Identity(target), Identity(target)) + " 레벨";
                        break;
                    case 4: case 5: case 6:
                        target = Field(value, type == 4 ? "_book" : type == 5 ? "_misc" : "_specialItem");
                        Exact(target, type == 4 ? "Book" : type == 5 ? "Misc" : "SpecialItem");
                        current = Number(target, "_currentCount"); name = ItemName(target, type == 4 ? "Book" : type == 5 ? "Misc" : "Special") + " 보유 수";
                        break;
                    case 7: case 8: case 9: case 10:
                        target = Field(value, "_bookCollection"); Exact(target, "BookDataCollection");
                        current = 0;
                        foreach (object book in List(target, "_list"))
                        { Exact(book, "Book"); if (Number(book, "_currentMartialLevel") >= List(book, "_martialLevelData").Count) current++; }
                        name = new[] { "유학", "도학", "불학", "형의권" }[type - 7] + " 비급 최대 레벨 수";
                        break;
                    default: throw new UnsupportedEffectException();
                }
                int original = current;
                int predicted;
                if (projected.TryGetValue(target, out predicted)) current = predicted;
                return new Reference { Target = target, Type = type, Current = current, Original = original, Name = name, Martial = martial };
            }
            internal bool? Conditions(object value, StringBuilder detail)
            {
                Exact(value, "MartialLearnCondition");
                bool all = true, unknown = false;
                foreach (object group in List(value, "_list"))
                {
                    Exact(group, "MartialLearnCompareGroup");
                    bool any = false, groupUnknown = false;
                    IList comparisons = List(group, "_itemList");
                    // Audited GameStatUtils.LogicOp(empty, OR) returns false.
                    foreach (object compare in comparisons)
                    {
                        Exact(compare, "MartialLearnCompareItem");
                        try
                        {
                            Reference reference = ReadReference(Field(compare, "_stat"));
                            int right = Number(compare, "_value"), op = Number(compare, "_compareOp");
                            bool result;
                            switch (op)
                            {
                                case 0: result = reference.Current == right; break;
                                case 1: result = reference.Current != right; break;
                                case 2: result = reference.Current < right; break;
                                case 3: result = reference.Current <= right; break;
                                case 4: result = reference.Current > right; break;
                                case 5: result = reference.Current >= right; break;
                                default: throw new UnsupportedEffectException();
                            }
                            detail.AppendLine("  " + reference.Name + " " + new[] { "=", "≠", "<", "≤", ">", "≥" }[op] + " " + right + " (현재 " + reference.Current + ") · " + (result ? "충족" : "미충족"));
                            any |= result;
                        }
                        catch (UnsupportedEffectException) { detail.AppendLine("  확인 불가 · 미지원 학습 조건"); groupUnknown = true; }
                    }
                    detail.AppendLine("  같은 조건 묶음은 하나 이상 충족 · 묶음 사이는 모두 충족");
                    if (!any && !groupUnknown) all = false;
                    if (!any && groupUnknown) unknown = true;
                }
                if (!all) return false;
                return unknown ? (bool?)null : true;
            }
            private static string Signed(int value) { return value > 0 ? "+" + value.ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture); }
            internal string Modification(object value, int grade, bool levelUp)
            {
                Exact(value, "MartialModifyStat");
                if (uncertainProjection) throw new UnsupportedEffectException();
                Reference reference = ReadReference(Field(value, "_stat"));
                int delta = Number(value, "_value");
                if (delta == int.MinValue) throw new UnsupportedEffectException();
                string reason = "";
                if (reference.Type == 1 && Flag(value, "_center"))
                {
                    long distance = 50L - reference.Current;
                    delta = checked((int)(Math.Sign(distance) * Math.Min(Math.Abs(distance), Math.Abs((long)delta))));
                    reason = " · 중앙 50을 향한 변화";
                }
                if (levelUp && reference.Martial && reference.Current / 25 + 1 < grade)
                { delta = 0; reason = " · 현재 무공 수치가 비급 등급의 적용 범위 밖"; }
                // Effects on collections or converted stats have nonlocal meaning.
                // Retain their name but never invent an applied final number.
                if (reference.Type >= 7) throw new UnsupportedEffectException();
                long next = unchecked(reference.Current + delta); // matches the game before its clamp
                switch (reference.Type)
                {
                    case 1:
                        IList conversions = Field(reference.Target, "_statAdditionData") as IList;
                        if (Number(reference.Target, "_value") != reference.Original || (conversions != null && conversions.Count != 0) || Field(reference.Target, "_totalAddData") != null)
                            throw new UnsupportedEffectException();
                        next = Math.Max(Number(reference.Target, "_min"), Math.Min(Number(reference.Target, "_max"), next)); break;
                    case 2: next = Math.Max(0, Math.Min(Number(reference.Target, "_max"), next)); break;
                    case 3: next = Math.Max(0, Math.Min(Number(reference.Target, "_maxLevel"), next)); break;
                    default: next = Math.Max(0, next); break;
                }
                int final = checked((int)next), applied = checked(final - reference.Current);
                projected[reference.Target] = final;
                return reference.Name + " " + Signed(applied) + " (" + reference.Current + " → " + final + ")" + reason;
            }
            internal void AddModification(StringBuilder detail, object effect, int grade, bool levelUp)
            {
                try { detail.AppendLine("  " + Modification(effect, grade, levelUp)); }
                catch (UnsupportedEffectException) { Unknown = true; uncertainProjection = true; detail.AppendLine("  확인 불가 · 미지원·동적 변화"); }
            }
            internal void Costs(object level, StringBuilder detail)
            {
                IList costs = List(level, "_consumable");
                detail.AppendLine("학습 1회 소모 · 학습 조건 충족 시:");
                if (costs.Count == 0) detail.AppendLine("  확인된 소모 없음");
                foreach (object group in costs)
                {
                    Exact(group, "MartialLearnConsumable");
                    int logic = Number(group, "_logicType");
                    if (logic != 0 && logic != 1) { Unknown = true; uncertainProjection = true; detail.AppendLine("  확인 불가 · 미지원 소모 방식"); continue; }
                    bool applied = false;
                    foreach (object cost in List(group, "_consumable"))
                    {
                        try
                        {
                            if (logic == 1 && ReadReference(Field(cost, "_stat")).Current <= 0) continue;
                            AddModification(detail, cost, 0, false); applied = true;
                            if (logic == 1) break;
                        }
                        catch (UnsupportedEffectException) { Unknown = true; uncertainProjection = true; detail.AppendLine("  확인 불가 · 소모 대상 선택 미지원"); break; }
                    }
                    if (!applied) detail.AppendLine("  현재 선택되는 소모 항목 없음");
                }
            }
        }
        private static InformationRow Read(object selected, string observedTitle, string id)
        {
            try { return ReadKnown(selected, observedTitle, id); }
            catch (UnsupportedEffectException)
            {
                string name = Plain(DisplayNames.Text(string.IsNullOrWhiteSpace(observedTitle) ? Identity(selected) : observedTitle));
                string kind = Kind(selected);
                return new InformationRow(id, name, kind, "확인 불가", "미지원·동적 효과 자료 · 게임 판정이나 사용·학습을 실행하지 않고 조회를 중단함.", name + " · " + kind + " · 확인 불가", selected);
            }
        }
        private static InformationRow ReadKnown(object selected, string observedTitle, string id)
        {
            string kind = Kind(selected), name = string.IsNullOrWhiteSpace(observedTitle) ? Identity(selected) : observedTitle;
            StringBuilder detail = new StringBuilder();
            string status;
            bool known = true;
            if (selected.GetType().FullName == "Mortal.Core.Consumables")
            {
                IList effects = List(selected, "_effects");
                detail.AppendLine("소모품 사용 효과 자료:");
                foreach (object effect in effects)
                {
                    Exact(effect, "ItemEffect");
                    detail.AppendLine("  " + StatName(Field(effect, "_statType")) + " " + (Number(effect, "_value") > 0 ? "+" : "") + Number(effect, "_value"));
                }
                if (effects.Count == 0) { status = "확인된 효과 없음"; detail.AppendLine("  등록된 사용 효과 없음"); }
                else
                {
                    status = "확인 불가"; known = false;
                    detail.AppendLine("현재 실제 적용 변화는 확인 불가 · 제공된 게임 코드에서 이 효과 자료의 사용 실행 경로를 확인하지 못함. 자료상 수치를 실제 사용 결과로 단정하지 않음.");
                }
            }
            else if (selected.GetType().FullName == "Mortal.Core.Book")
            {
                IList levels = List(selected, "_martialLevelData");
                int current = Number(selected, "_currentMartialLevel");
                if (current < 0 || current > levels.Count) throw new UnsupportedEffectException();
                if (current == levels.Count)
                { status = "학습 완료"; detail.AppendLine("최대 학습 레벨 · 확인된 다음 학습 효과 없음"); }
                else
                {
                    object level = levels[current]; Exact(level, "MartialLevelData");
                    detail.AppendLine("현재 학습 레벨 " + current + " → 다음 " + (current + 1) + " (현재 학습점 " + Number(selected, "_currentLevelPoint") + " / 필요 " + Number(level, "_points") + ")");
                    detail.AppendLine("학습 조건:");
                    Evaluation evaluation = new Evaluation();
                    bool? condition = evaluation.Conditions(Field(level, "_condition"), detail);
                    evaluation.Costs(level, detail);
                    IList effects = List(level, "_levelupStats");
                    detail.AppendLine("다음 레벨 도달 시 효과 · 현재 상태와 위 소모를 기준으로 한 정적 변화:");
                    foreach (object effect in effects) evaluation.AddModification(detail, effect, Number(selected, "_martialLevel"), true);
                    if (effects.Count == 0) detail.AppendLine("  확인된 효과 없음");
                    known = condition.HasValue && !evaluation.Unknown;
                    status = !known ? "확인 불가" : condition.Value ? "학습 효과 확인" : "학습 조건 미충족";
                    detail.AppendLine("학습량·실제 레벨 도달·후속 이벤트는 예측하지 않음. 학습·소모·능력치 변경을 실행하지 않은 조회.");
                }
            }
            else { status = "확인 불가"; known = false; detail.AppendLine("미지원 물품 종류 · 장비 착용·강화·동적 효과는 조회 범위 밖"); }
            name = Plain(DisplayNames.Text(name));
            return new InformationRow(id, name, kind, status, detail.ToString().TrimEnd(), name + " · " + kind + " · " + status, selected, known);
        }
    }
}
