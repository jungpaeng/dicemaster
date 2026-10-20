using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DiceMaster.LiveLog;
using LuaAction = DiceMaster.LuaParse.Action;
namespace UnityEngine
{
    public class Transform { public Transform Parent; public bool IsChildOf(Transform other) => ReferenceEquals(Parent, other) || ReferenceEquals(this, other); }
    public class Component : Object { public Transform transform = new Transform(); }
    public class Object { public string name { get; set; } public int GetInstanceID() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this); }
    public class GameObject { public bool activeInHierarchy = true; public Transform transform = new Transform(); }
    public static class Time { public static float unscaledTime; }
}
namespace Mortal.Core
{
    public enum PositionType { 無, Hall, Kitchen }
    public enum StatKind { 行動次數 = 19 }
    public class FlagData : UnityEngine.Object { private int state; public int StateReads; public int State { get { StateReads++; return state; } set { state = value; } } }
    public class StatValueReference
    { public int _checkType, _constant; public bool _negative; public object _flag, _statGroup; }
    public class StatGroupVariable { public int _groupType = 1; public List<StatValueReference> _value = new List<StatValueReference>(); }
    public class StatCompareItem { public int _compareOp; public StatGroupVariable _value1, _value2; }
    public class ConditionResultItem
    { public int _logicOp; public List<StatCompareItem> _items = new List<StatCompareItem>(); public bool Check() { Counters.GameCalls++; throw new Exception(); } }
    public class ConditionResultData : UnityEngine.Object
    { public List<ConditionResultItem> _items = new List<ConditionResultItem>(); }
    public class GameStat { public StatKind _type = StatKind.行動次數; public object _statAdditionData; public int _value = 3; public int FinalValue => _value; }
    public class PlayerStatData { public List<GameStat> _list = new List<GameStat> { new GameStat() }; }
    public class GameTime { public int Year = 1, Month = 5, Stage = 1; }
    public class GameTimeData { public GameTime GameTime = new GameTime(); }
    public class PlayerStatManagerData { public PlayerStatData _stats = new PlayerStatData(); public GameTimeData _gameTimeData = new GameTimeData(); }
    public class MissionCheckData : UnityEngine.Object
    { public string _key = "start", _startScript = "mission"; public PositionType _position = PositionType.Hall; public bool _active = true, _dayCheck, _beforeMainMission; public int _timeCheckType = 1; public GameTime _specificTime = new GameTime(), _minTime = new GameTime(), _maxTime = new GameTime(); public ConditionResultItem[] _conditions = Array.Empty<ConditionResultItem>(); }
    public class MissionData : UnityEngine.Object
    {
        public string _name = "";
        private string key = "clear"; private MissionCheckData[] stages = Array.Empty<MissionCheckData>();
        public int CurrentReads, DefinitionReads; public bool BrokenCurrent;
        public string _currentKey { get { CurrentReads++; if (BrokenCurrent) throw new InvalidOperationException("unrelated current state must not be read"); return key; } set { key = value; } }
        public MissionCheckData[] _missions { get { DefinitionReads++; return stages; } set { stages = value; } }
    }
    public class SubMissions
    {
        private List<MissionData> entries = new List<MissionData>(); public int ListReads;
        public List<MissionData> _list { get { ListReads++; return entries; } set { entries = value; } }
    }
    public class TrackingMissions : Dictionary<string, MissionCheckData>, IDictionary
    {
        public int Enumerations;
        IDictionaryEnumerator IDictionary.GetEnumerator()
        { Enumerations++; return ((IDictionary)new Dictionary<string, MissionCheckData>(this)).GetEnumerator(); }
    }
    public class MissionManagerData
    { public MissionData _main = new MissionData(); public SubMissions _subMissions = new SubMissions(); public MissionCheckData MainCheckData; public Dictionary<string,MissionCheckData> ActiveSubMissions = new Dictionary<string,MissionCheckData>(); public bool MainMissionStart; public PositionType MainMissionPosition; }
    public static class Counters { public static int GameCalls; }
}
namespace Mortal.Free
{
    public class PositionConditionFlagItem
    {
        public Mortal.Core.ConditionResultItem _condition;
        public void Execute() { Mortal.Core.Counters.GameCalls++; throw new Exception("Entry flag execution is forbidden"); }
    }
    public class FreePositionData : UnityEngine.Object
    { public string _defaultScript = "hall"; public PositionConditionFlagItem[] _conditionFlags = Array.Empty<PositionConditionFlagItem>(); }
    public class UnreadableEntryPositionData : FreePositionData
    { public new PositionConditionFlagItem[] _conditionFlags { get { throw new InvalidOperationException("Entry boundary read failure"); } } }
    public class FreeManager { public bool IsProcessing; }
    public class PositionController : UnityEngine.Object
    {
        public bool isActiveAndEnabled = true, _active = true; public int _costAction = 1;
        public UnityEngine.GameObject gameObject = new UnityEngine.GameObject();
        public FreePositionData _positionData = new FreePositionData { name = "Hall" };
        public string _scriptName = "stale-script-cache";
        public Mortal.Core.PositionType _position = Mortal.Core.PositionType.Hall;
        public Mortal.Core.MissionManagerData _missionsManager = new Mortal.Core.MissionManagerData();
        public Mortal.Core.PlayerStatManagerData _gameStatManager = new Mortal.Core.PlayerStatManagerData();
        public FreeManager _freeManager = new FreeManager();
    }
}
namespace Mortal.Story
{
    public class RateItem { public Mortal.Core.ConditionResultItem Condition; public int Rate; }
    public class PositionEventData : UnityEngine.Object
    {
        public bool _toggleCondition = true; public Mortal.Core.ConditionResultItem _activeCondition;
        public int _defaultRate = 10; public RateItem[] _eventRateItem = Array.Empty<RateItem>();
        private string resultType = "A";
        public int ResultReads;
        public string ResultType { get { ResultReads++; return resultType; } set { resultType = value; } }
        public int GetRate() { Mortal.Core.Counters.GameCalls++; throw new Exception(); }
    }
    public class PositionResultData : UnityEngine.Object { public string _scriptPrefix = "result"; public List<PositionEventData> _list = new List<PositionEventData>(); }
    public class PositionResultConfig { public List<PositionResultData> _list = new List<PositionResultData>(); }
    public class ConditionResultConfig { public List<Mortal.Core.ConditionResultData> _list = new List<Mortal.Core.ConditionResultData>(); }
    public class CheckPointManager
    {
        public static CheckPointManager Instance { get; set; }
        public PositionResultConfig _position = new PositionResultConfig();
        public ConditionResultConfig _condition = new ConditionResultConfig();
        public object[] _switchList = Array.Empty<object>();
        public string Position(string key) { Mortal.Core.Counters.GameCalls++; throw new Exception("Game Position must not execute"); }
        public bool Condition(string key) { Mortal.Core.Counters.GameCalls++; throw new Exception("Game Condition must not execute"); }
    }
}
namespace DiceMaster.LiveLog
{
    public static class LogBridge
    {
        public static object ReadFieldStrict(object target, string name)
        {
            if (target == null) throw new DataNotReadyException();
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target);
                var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (property != null) return property.GetValue(target);
            }
            throw new MissingFieldException(name);
        }
    }
    public class PreviewReadException : Exception { public bool IsWaiting; public string PartialDescription; }
    public enum ScriptReadState { Success, Missing, Waiting, Failed }
    public class ScriptReadResult { public ScriptReadState State; public string Text; public static ScriptReadResult Success(string text) => new ScriptReadResult { State = ScriptReadState.Success, Text = text }; }
    public class PreviewIssue { }
    public class PreviewBranchContext { }
    public class PreviewEffect
    {
        public string Owner = "statmodifymanager", Name; public bool Conditional, Followup, ExecutionUncertain;
        public string ValidationError; public List<PreviewBranchContext> Branches = new List<PreviewBranchContext>();
        public List<object> Constants = new List<object>();
    }
    public class PreviewAnalysis
    { public bool StoppedAtChoice, RootConditional; public List<PreviewIssue> Issues = new List<PreviewIssue>(); public List<PreviewEffect> Effects = new List<PreviewEffect>(); }
    public static class PreviewDisplay { public static void Validate(PreviewAnalysis analysis) { } public static bool IsDisplayOnly(PreviewEffect effect) => effect.Name == "display"; }
    public static class DisplayNames
    { public static int PendingNames; public static string Text(string value) => value; public static string LookupName(string key, string value) => value;
        public static readonly Dictionary<string,string> Labels = new Dictionary<string,string>(); public static readonly List<string> LabelReads = new List<string>();
        public static string Locale(string key, string fallback) { LabelReads.Add(key); return Labels.TryGetValue(key, out string value) ? value : fallback; } }
}
namespace DiceMaster.LuaParse
{
    public class Action { public PreviewAnalysis Analysis; }
    public class LuaParser
    {
        public static readonly LuaParser Instance = new LuaParser();
        public readonly Dictionary<string,List<Action>> OptionResult = new Dictionary<string,List<Action>>();
        public static int Parses, Descriptions;
        public readonly Dictionary<string, List<Action>> Actions = new Dictionary<string, List<Action>>();
        public List<Action> ExtractActionsFromLua(string key) { List<Action> result; if (!Actions.TryGetValue(key, out result)) { Parses++; Actions[key] = result = new List<Action> { new Action { Analysis = new PreviewAnalysis() } }; } return result; }
        public List<string> ExtractPositionWorkFromLua(string key, string text) { Parses++; return text.Length == 0 ? new List<string>() : new List<string> { text }; }
        public static void RefreshPreviewReads(List<Action> value) { }
        public static string Description(List<Action> value) { Descriptions++; return "fixture effects"; }
    }
}
namespace DiceMaster.EventSelection
{
    public static class PopupConditionText
    {
        public static int EventDescriptions, ConditionDescriptions;
        public static string Condition(object condition) { ConditionDescriptions++; return "fixture condition"; }
        public static string Event(object target, int? rate, out string summary) { EventDescriptions++; summary = "fixture condition"; return summary; }
    }
}
namespace DiceMaster
{
    public class PositionEventCache
    { public static readonly DiceMaster.LuaParse.LuaParser Parser = new DiceMaster.LuaParse.LuaParser(); public static DiceMaster.LuaParse.LuaParser GetDefinitionParser() => Parser; }
    public static class DataHelper
    {
        public static Func<string, ScriptReadResult> ExternalReader; public static int Reads; public static readonly Dictionary<string, string> Scripts = new Dictionary<string, string>();
        public static ScriptReadResult ReadPreviewScript(string key) { Reads++; if (ExternalReader != null) return ExternalReader(key); string result; return Scripts.TryGetValue(key, out result) ? new ScriptReadResult { State = ScriptReadState.Success, Text = result } : new ScriptReadResult { State = ScriptReadState.Waiting }; }
        public static string GetEventName(Mortal.Story.PositionEventData value) => value.name;
    }
    public static class LocationTranslations { public static string TranslateLocation(string value) => value; }
    public static class DicePlugin { public static readonly List<string> Logs = new List<string>(); public static void LogWarning(string value) { Logs.Add(value); } }
}

namespace UnityEngine.UI
{
    public class Text : UnityEngine.Object { private string content = ""; public int Writes, Reads; public string text { get { Reads++; return content; } set { content = value; Writes++; } } }
}
namespace Fungus
{
    public class MenuDialog : UnityEngine.Component { public int nextOptionIndex; public static MenuDialog ActiveMenuDialog; public UnityEngine.GameObject gameObject = new UnityEngine.GameObject(); }
}
namespace Mortal.Core
{
    public class NormalOptionButton : UnityEngine.Component
    {
        public bool isActiveAndEnabled = true; public UnityEngine.GameObject gameObject = new UnityEngine.GameObject();
        public UnityEngine.UI.Text _source = new UnityEngine.UI.Text(); public UnityEngine.UI.Text[] _text = new[] { new UnityEngine.UI.Text() };
    }
}


namespace Mortal.Core
{
    public class PositionOptionButton : UnityEngine.Component
    { public bool isActiveAndEnabled = true; public UnityEngine.GameObject gameObject = new UnityEngine.GameObject(); public UnityEngine.UI.Text _source = new UnityEngine.UI.Text(); public UnityEngine.UI.Text[] _titleText = new[] { new UnityEngine.UI.Text() }; }
    public class BreakOptionButton : UnityEngine.Component
    { public bool isActiveAndEnabled = true; public UnityEngine.GameObject gameObject = new UnityEngine.GameObject(); public UnityEngine.UI.Text _source = new UnityEngine.UI.Text(); public UnityEngine.UI.Text[] _titleText = new[] { new UnityEngine.UI.Text() }; }
}
namespace UnityEngine.EventSystems
{
    public class EventSystem { public static EventSystem current = new EventSystem(); public UnityEngine.GameObject currentSelectedGameObject; }
}
