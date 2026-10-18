using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DiceMaster.LiveLog
{
    // Only mod description methods call these formatters. Unknown previews are
    // rendered as text, never coerced into a gameplay int or bool.
    public static class PreviewFormatting
    {
        private static object Read(object value, string name) { return LogBridge.ReadFieldStrict(value, name); }
        private static IList Items(object value, string name)
        {
            IList items = Read(value, name) as IList;
            if (items == null) throw new InvalidOperationException("Invalid display collection.");
            return items;
        }
        private static object Helper(string name, params object[] arguments)
        {
            return LogBridge.CallDisplayHelper("DiceMaster.DataHelper", name, arguments);
        }
        private static string Local(string key, string fallback)
        {
            return (string)Helper("GetLocalizedSystemText", key, fallback, "시스템_미리보기", key);
        }
        private static string Value(object group, PreviewValue value)
        {
            string name = (string)Helper("FormatConditionValName", group);
            return name + "(" + (value.IsKnown ? value.Value.ToString(CultureInfo.InvariantCulture) : value.Text.Replace(" · 실행 시 판정", "")) + ")";
        }

        public static string ExtractConditions(object condition, out bool isInPeriod)
        {
            bool protectedDisplay = LogBridge.DisplayIsProtected();
            try
            {
                string result = ExtractConditionsCore(condition, out isInPeriod);
                if (!protectedDisplay) LogBridge.RecordDescriptionResult(condition, null);
                return result;
            }
            catch (Exception error)
            {
                if (protectedDisplay) throw;
                // This helper also runs inside gameplay Harmony prefixes. A
                // description error must remain text, rather than abort Execute.
                isInPeriod = true;
                LogBridge.RecordDescriptionResult(condition, error);
                return RefreshEngine.ReadFailureStatus;
            }
        }

        private static string ExtractConditionsCore(object condition, out bool isInPeriod)
        {
            IList comparisons = Items(condition, "_items");
            object logic = Read(condition, "_logicOp");
            int logicValue = Convert.ToInt32(logic);
            if (logicValue != 0 && logicValue != 1) throw new InvalidOperationException("Unknown display logic.");
            List<string> descriptions = new List<string>();
            // This out parameter controls description visibility, not condition
            // truth. Unknown time checks remain visible with their pending text.
            isInPeriod = true;
            foreach (object comparison in comparisons)
            {
                object left = Read(comparison, "_value1"), right = Read(comparison, "_value2");
                PreviewComparison preview = RandomPreview.Compare(comparison);
                string text = Value(left, preview.Left) + " " +
                    (string)Helper("GetCompareOperator", Read(comparison, "_compareOp")) + " " + Value(right, preview.Right);
                if (preview.Verdict.HasValue)
                {
                    string leftName = (string)Read(left, "name") ?? "";
                    if (leftName.Contains("時間_") && !preview.Verdict.Value) isInPeriod = false;
                    text = (string)LogBridge.CallDisplayHelper("DiceMaster.RichText", preview.Verdict.Value ? "Green" : "Red", text);
                }
                else
                {
                    string status = preview.IsSupported && comparisons.Count == 1 ? "실행 시 판정" : "난수 조건 · 미리보기 미지원";
                    if (!text.Contains(status)) text += " · " + status;
                    // This is a probability of one comparison, never the
                    // aggregate AND/OR condition or an event trigger rate.
                    if (comparisons.Count == 1 && !string.IsNullOrEmpty(preview.ProbabilityText)) text += " · " + preview.ProbabilityText;
                }
                descriptions.Add(text);
            }
            return string.Join(" " + (string)Helper("GetLogicOperator", logic) + " ", descriptions);
        }

        public static string EventDescription(object data)
        {
            PreviewValue rate = RandomPreview.EventRate(data);
            string rateText = rate.IsKnown ? rate.Value.ToString(CultureInfo.InvariantCulture) : rate.Text;
            StringBuilder description = new StringBuilder();
            description.AppendLine(string.Format(Local("System/Event/TriggerRateFormat", "발동 확률：{0}({1})"), rateText, Read(data, "_defaultRate")));
            bool inPeriod;
            if ((bool)Read(data, "_toggleCondition"))
                description.AppendLine(Local("System/Event/ActiveConditionPrefix", "활성 조건：") + ExtractConditions(Read(data, "_activeCondition"), out inPeriod));
            description.AppendLine(Local("System/Event/JudgmentConditionPrefix", "판정 조건："));
            bool inactive = rate.IsKnown && rate.Value == 0 && (bool)Read(data, "_toggleCondition") &&
                RandomPreview.ConditionVerdict(Read(data, "_activeCondition")) == false;
            IList rateItems = Read(data, "_eventRateItem") as IList;
            if (rateItems == null)
            {
                if (inactive) return description.ToString();
                throw new InvalidOperationException("Invalid display collection.");
            }
            foreach (object item in rateItems)
            {
                if (item == null)
                {
                    if (inactive) continue;
                    throw new InvalidOperationException("Null event rate item.");
                }
                object condition = Read(item, "Condition");
                if (condition == null && inactive) continue;
                int addition = Convert.ToInt32(Read(item, "Rate"));
                string additionText = addition > 0 ? "+" + addition.ToString(CultureInfo.InvariantCulture) : addition.ToString(CultureInfo.InvariantCulture);
                description.AppendLine(string.Format(Local("System/Event/RateItemFormat", "    {0}：{1}"), ExtractConditions(condition, out inPeriod), additionText));
            }
            return description.ToString();
        }
    }
}
