using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DiceMaster.LiveLog
{
    // Only mod description methods call these formatters. Each condition item
    // or complete event description shares one snapshot for numbers and colors.
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
            return name + "(" + value.Value.ToString(CultureInfo.InvariantCulture) + ")";
        }

        public static string ExtractConditions(object condition, out bool isInPeriod)
        {
            return ExtractConditionsWithSession(condition, out isInPeriod, new RandomPreview.Evaluation());
        }

        private static string ExtractConditionsWithSession(object condition, out bool isInPeriod, RandomPreview.Evaluation evaluation)
        {
            bool protectedDisplay = LogBridge.DisplayIsProtected();
            try
            {
                string result = ExtractConditionsCore(condition, out isInPeriod, evaluation);
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

        private static string ExtractConditionsCore(object condition, out bool isInPeriod, RandomPreview.Evaluation evaluation)
        {
            IList comparisons = Items(condition, "_items");
            object logic = Read(condition, "_logicOp");
            int logicValue = Convert.ToInt32(logic);
            if (logicValue != 0 && logicValue != 1) throw new InvalidOperationException("Unknown display logic.");
            evaluation.ConditionVerdict(condition);
            List<string> descriptions = new List<string>();
            // This out parameter controls description visibility. Its time
            // verdict comes from the same snapshot as the displayed number.
            isInPeriod = true;
            foreach (object comparison in comparisons)
            {
                object left = Read(comparison, "_value1"), right = Read(comparison, "_value2");
                PreviewComparison preview = evaluation.Compare(comparison);
                string text = Value(left, preview.Left) + " " +
                    (string)Helper("GetCompareOperator", Read(comparison, "_compareOp")) + " " + Value(right, preview.Right);
                string leftName = (string)Read(left, "name") ?? "";
                if (leftName.Contains("時間_") && !preview.Verdict.Value) isInPeriod = false;
                text = (string)LogBridge.CallDisplayHelper("DiceMaster.RichText", preview.Verdict.Value ? "Green" : "Red", text);
                descriptions.Add(text);
            }
            return string.Join(" " + (string)Helper("GetLogicOperator", logic) + " ", descriptions);
        }

        public static string EventDescription(object data)
        {
            RandomPreview.Evaluation evaluation = new RandomPreview.Evaluation();
            PreviewValue rate = evaluation.Rate(data);
            string rateText = rate.Value.ToString(CultureInfo.InvariantCulture);
            StringBuilder description = new StringBuilder();
            description.AppendLine(string.Format(Local("System/Event/TriggerRateFormat", "발동 확률：{0}({1})"), rateText, Read(data, "_defaultRate")));
            bool inPeriod;
            if ((bool)Read(data, "_toggleCondition"))
                description.AppendLine(Local("System/Event/ActiveConditionPrefix", "활성 조건：") + ExtractConditionsWithSession(Read(data, "_activeCondition"), out inPeriod, evaluation));
            description.AppendLine(Local("System/Event/JudgmentConditionPrefix", "판정 조건："));
            bool inactive = rate.IsKnown && rate.Value == 0 && (bool)Read(data, "_toggleCondition") &&
                evaluation.ConditionVerdict(Read(data, "_activeCondition")) == false;
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
                description.AppendLine(string.Format(Local("System/Event/RateItemFormat", "    {0}：{1}"), ExtractConditionsWithSession(condition, out inPeriod, evaluation), additionText));
            }
            return description.ToString();
        }
    }
}
