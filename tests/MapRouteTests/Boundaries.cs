using System;
using System.Collections.Generic;

namespace DiceMaster.LiveLog
{
    // Explicit resource/game-stat boundary fixtures, not an alternate route or
    // predicate implementation. The production PreviewConditions reader is linked.
    public enum ScriptReadState { Success, Missing, Waiting, Failed }
    public sealed class ScriptReadResult
    {
        public ScriptReadState State; public string Text;
        public static ScriptReadResult Success(string text) { return new ScriptReadResult { State = ScriptReadState.Success, Text = text }; }
        public static ScriptReadResult Missing() { return new ScriptReadResult { State = ScriptReadState.Missing }; }
        public static ScriptReadResult Waiting() { return new ScriptReadResult { State = ScriptReadState.Waiting }; }
    }
    public sealed class UnsupportedDisplayStatException : Exception { }
    public static class SafeDisplayEvaluation
    {
        public static int ConditionReads;
        public static int ReadReference(object value)
        {
            ConditionReads++;
            var reference = (Mortal.Core.StatValueReference)value;
            int current = reference._checkType == 0 ? reference._constant : reference._checkType == 3 ? reference._flag.State : throw new Exception("Unsupported primitive fixture");
            return reference._negative ? unchecked(-current) : current;
        }
        public static bool CheckConditionData(object value) { ConditionReads++; return ((Mortal.Core.ConditionResultData)value).Current; }
        public static bool CheckCondition(object value) { return ((Mortal.Core.ConditionResultItem)value).Current; }
        public static bool CheckComparison(object value)
        {
            ConditionReads++; var comparison = (Mortal.Core.StatCompareItem)value;
            int Number(Mortal.Core.StatGroupVariable group)
            {
                int n = 0;
                foreach (var reference in group._value)
                {
                    int current = reference._checkType == 0 ? reference._constant : reference._checkType == 3 ? reference._flag.State : throw new Exception("Unsupported primitive fixture");
                    n += reference._negative ? -current : current;
                }
                return n;
            }
            int left = Number(comparison._value1), right = Number(comparison._value2);
            switch (comparison._compareOp)
            {
                case 0: return left == right; case 1: return left != right;
                case 2: return left < right; case 3: return left <= right;
                case 4: return left > right; case 5: return left >= right;
                default: throw new Exception("Unsupported comparison fixture");
            }
        }
    }
}
namespace UnityEngine
{
    public class Object { public string name { get; set; } }
    public static class Random { public static int Calls; public static int Range(int min, int max) { Calls++; throw new Exception("Forbidden RNG"); } }
}
namespace Mortal.Core
{
    public sealed class FlagData : UnityEngine.Object { public int State; }
    public sealed class StatGroupVariable { public int _groupType = 1; public List<StatValueReference> _value = new List<StatValueReference>(); }
    public sealed class StatValueReference
    {
        public int _checkType, _constant; public bool _negative; public FlagData _flag;
        public StatGroupVariable _statGroup; public ConditionResultData _condition;
    }
    public sealed class StatCompareItem { public int _compareOp; public StatGroupVariable _value1 = new StatGroupVariable(), _value2 = new StatGroupVariable(); }
    public sealed class ConditionResultData : UnityEngine.Object
    {
        private readonly StatValueReference fixtureValue = new StatValueReference(); private bool current; public int _count = 29;
        public List<ConditionResultItem> _items = new List<ConditionResultItem>();
        public bool Current { get { return current; } set { current = value; fixtureValue._constant = value ? 1 : 0; } }
        public ConditionResultData()
        {
            var item = new ConditionResultItem();
            item._items.Add(new StatCompareItem {
                _value1 = new StatGroupVariable { _value = new List<StatValueReference> { fixtureValue } },
                _value2 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _constant = 1 } } }
            }); _items.Add(item);
        }
    }
    public sealed class ConditionResultItem { public int _logicOp; public List<object> _items = new List<object>(); public bool Current; }
}
namespace Mortal.Story
{
    public sealed class SwitchResultData : UnityEngine.Object
    { public bool _useCount; public List<Mortal.Core.ConditionResultItem> _items = new List<Mortal.Core.ConditionResultItem>(); }
    public sealed class SwitchResultConfig { public List<SwitchResultData> _list = new List<SwitchResultData>(); }
    public sealed class ConditionResultConfig { public List<Mortal.Core.ConditionResultData> _list = new List<Mortal.Core.ConditionResultData>(); }
    public sealed class CheckPointManager
    {
        public static CheckPointManager Instance { get; set; }
        public ConditionResultConfig _condition = new ConditionResultConfig();
        public object[] _switchList = new object[0];
        public int GameplayCalls, RngCalls, Counter;
        public string Position(string key) { GameplayCalls++; throw new Exception("Forbidden game Position"); }
        public bool Condition(string key) { GameplayCalls++; throw new Exception("Forbidden game Condition"); }
    }
}
