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
        public static bool CheckConditionData(object value) { ConditionReads++; return ((Mortal.Core.ConditionResultData)value).Current; }
        public static bool CheckCondition(object value) { return ((Mortal.Core.ConditionResultItem)value).Current; }
    }
}
namespace UnityEngine { public class Object { public string name { get; set; } } }
namespace Mortal.Core
{
    public sealed class ConditionResultData : UnityEngine.Object
    { public List<ConditionResultItem> _items = new List<ConditionResultItem>(); public bool Current; }
    public sealed class ConditionResultItem { public List<object> _items = new List<object>(); public bool Current; }
}
namespace Mortal.Story
{
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
