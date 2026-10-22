using System;
using System.Collections.Generic;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;

namespace DiceMaster
{
    public static partial class PreviewScenario
    {
        private static void PositionEntryRegression()
        {
            var manager = CheckPointManager.Instance; var previous = manager._condition;
            var target = new FlagData { name = "delivered_renamed_entry_flag", State = 0 };
            StatGroupVariable Number(int n) => new StatGroupVariable { _value = new List<StatValueReference> {
                new StatValueReference { _checkType = 0, _constant = n } } };
            ConditionResultItem Equals(FlagData flag, int n) => new ConditionResultItem { _items = new List<StatCompareItem> {
                new StatCompareItem { _value1 = new StatGroupVariable { _value = new List<StatValueReference> {
                    new StatValueReference { _checkType = 3, _flag = flag } } }, _value2 = Number(n) } } };
            var entryCondition = new ConditionResultItem { _items = new List<StatCompareItem> {
                new StatCompareItem { _value1 = Number(1), _value2 = Number(1) } } };
            var checkpoint = new ConditionResultData { name = "delivered_entry_gate", _count = 37,
                _items = new List<ConditionResultItem> { Equals(target, 1) } };
            manager._condition = new ConditionResultConfig { _list = new List<ConditionResultData> { checkpoint } };
            var item = new PositionFlagItem { _flagData = target, _trueState = 1, _falseState = 0 };
            var data = new FreePositionData { _conditionFlags = new[] { new PositionConditionFlagItem {
                _condition = entryCondition, _flagItem = new[] { item } } } };
            object predicate = Invoke(Type("LiveLog.PreviewConditions"), null, "Parse", "checkpointmanager.Condition('delivered_entry_gate')", null);
            string Forecast()
            {
                object reader = Invoke(Type("Information.MapRouteRead"), null, "ForSelectedPlace", data, null);
                return reader == null ? "NoEntry" : Invoke(reader.GetType(), reader, "Current", predicate).ToString();
            }
            int writes = target.StateWrites, checks = PositionConditionFlagItem.Executions + PositionFlagItem.Executions;
            int rng = UnityEngine.Random.Calls, original = ConditionResultData.Executions + StatGroupVariable.OriginalReads + StatCompareItem.OriginalReads;
            try
            {
                Check(Invoke(Type("LiveLog.PreviewConditions"), null, "Current", predicate).ToString() == "NotMet" && Forecast() == "Met",
                    "actual delivered IL forecasts a 0-to-1 entry assignment without changing strict current state");
                string diagnostic = (string)Invoke(Type("LiveLog.PreviewConditions"), null, "DescribePositionEntry", predicate, data);
                Check(diagnostic.Contains("진입 후 가상 조건 진단: 충족") && diagnostic.Contains("진입 가상 플래그 delivered_renamed_entry_flag = 1"),
                    "actual delivered IL exposes the private entry diagnostic, not a fabricated current State");
                entryCondition._items[0]._value1._value[0]._constant = 0;
                Check(Forecast() == "NotMet", "actual delivered IL rereads a now-false native entry predicate and uses its false target");
                entryCondition._items[0]._value1._value[0]._checkType = 7;
                Check(Forecast() == "Unknown", "actual delivered IL does not draw RNG to guess an entry assignment");
                item._falseState = 1;
                Check(Forecast() == "Met", "actual delivered IL proves equal true/false targets under an unknown predicate");
                entryCondition._items = null;
                Check(Forecast() == "NoEntry", "actual delivered IL rejects broken condition data even when both assignment targets match");
                data._conditionFlags = null;
                Check(Forecast() == "NoEntry", "actual delivered IL rejects missing serialized entry data");
                Check(target.State == 0 && target.StateWrites == writes && checkpoint._count == 37 &&
                    PositionConditionFlagItem.Executions + PositionFlagItem.Executions == checks && UnityEngine.Random.Calls == rng &&
                    ConditionResultData.Executions + StatGroupVariable.OriginalReads + StatCompareItem.OriginalReads == original,
                    "actual delivered entry forecast calls no native condition/setup/RNG and performs no temporary flag writes or counter changes");
            }
            finally { manager._condition = previous; }
        }
    }
}

namespace Mortal.Free
{
    public class FreePositionData : UnityEngine.Object { public PositionConditionFlagItem[] _conditionFlags; }
    public class PositionConditionFlagItem
    {
        public ConditionResultItem _condition; public PositionFlagItem[] _flagItem; public static int Executions;
        public void Execute() { Executions++; throw new InvalidOperationException("Entry Execute is forbidden"); }
    }
    public class PositionFlagItem
    {
        public FlagData _flagData; public int _trueState, _falseState; public static int Executions;
        public void Setup(bool value) { Executions++; throw new InvalidOperationException("Entry Setup is forbidden"); }
    }
}
