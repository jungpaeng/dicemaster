using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DiceMaster.LiveLog;
using LuaAction = DiceMaster.LuaParse.Action;
namespace UnityEngine
{
    public class Transform { public Transform Parent; public bool IsChildOf(Transform other) => ReferenceEquals(Parent, other) || ReferenceEquals(this, other); }
    public class Object { public Transform transform = new Transform(); public string name { get; set; } public int GetInstanceID() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this); }
    public class GameObject { public bool activeInHierarchy = true; }
    public static class Time { public static float unscaledTime; }
}
namespace Mortal.Core
{
    public class ValueCompareItem { public int _compareOp, _value; }
    public enum PositionType { 無, Hall, Kitchen }
    public enum StatKind { 行動次數 = 19 }
    public class FlagData : UnityEngine.Object { private int state; public int _default, StateReads, StateWrites; public int State { get { StateReads++; return state; } set { StateWrites++; state = value; } } }
    public class StatValueReference
    { public int _checkType, _constant; public bool _negative; public object _flag, _stat, _statGroup, _gameTime, _condition; }
    public class StatGroupVariable { public int _groupType = 1; public List<StatValueReference> _value = new List<StatValueReference>(); }
    public class StatCompareItem { public int _compareOp; public StatGroupVariable _value1, _value2; }
    public class ConditionResultItem
    { public int _logicOp; public List<StatCompareItem> _items = new List<StatCompareItem>(); public bool Check() { Counters.GameCalls++; throw new Exception(); } }
    public class ConditionResultData : UnityEngine.Object
    { public List<ConditionResultItem> _items = new List<ConditionResultItem>(); }
    public class GameStat { public StatKind _type = StatKind.行動次數; public object _statAdditionData; public int _value = 3; public int FinalValue => _value; }
    public class PlayerStatData { public List<GameStat> _list = new List<GameStat> { new GameStat() }; }
    public class GameTime { public int Year = 1, Month = 5, Stage = 1, _roundsPerMonth = 3; }
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
        public PositionFlagItem[] _flagItem = Array.Empty<PositionFlagItem>();
        public void Execute() { Mortal.Core.Counters.GameCalls++; throw new Exception("Entry flag execution is forbidden"); }
    }
    public class PositionFlagItem { public Mortal.Core.FlagData _flagData; public int _trueState, _falseState; }
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
    public class SwitchResultConfig { public List<SwitchResultData> _list = new List<SwitchResultData>(); }
    public class SwitchResultData : UnityEngine.Object { public bool _useCount; public int _count; public List<Mortal.Core.ValueCompareItem> _countItems = new List<Mortal.Core.ValueCompareItem>(); public List<Mortal.Core.ConditionResultItem> _items = new List<Mortal.Core.ConditionResultItem>(); }
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
    public class ScriptReadResult { public ScriptReadState State; public string Text; }
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
    { public static int PendingNames; public static string Text(string value) => value; public static string LookupName(string key, string value) => value; }
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
    public class MenuDialog : UnityEngine.Object { public static MenuDialog ActiveMenuDialog; public UnityEngine.GameObject gameObject = new UnityEngine.GameObject(); }
}
namespace Mortal.Core
{
    public class NormalOptionButton : UnityEngine.Object
    {
        public bool isActiveAndEnabled = true; public UnityEngine.GameObject gameObject = new UnityEngine.GameObject();
        public UnityEngine.UI.Text _source = new UnityEngine.UI.Text(); public UnityEngine.UI.Text[] _text = new[] { new UnityEngine.UI.Text() };
    }
}

#if !MAP_ROUTE_INTEGRATION
namespace DiceMaster.Information
{
    public static partial class MapGuideSource
    {
        // Route integration tests compile the production followup provider.
        private static void CollectSelectedRoute(Place place, Mortal.Free.PositionController controller, Source source, List<Candidate> candidates) { }
    }
}
namespace DiceMaster.LiveLog
{
    public enum PreviewConditionState { Unknown, Met, NotMet }
    public class PreviewCondition { }
    public static class PreviewConditions
    {
        public static readonly PreviewCondition Unknown = new PreviewCondition();
        public static string DescribeCurrent(PreviewCondition condition) => "fixture current diagnostic";
        public static string DescribePositionEntry(PreviewCondition condition, object data) => "fixture private entry diagnostic";
        public static string DescribeTalkResetSource() => "fixture talk reset source diagnostic";
    }
}
namespace DiceMaster.Information
{
    // Provider tests isolate the route builder. The real builder/parser and
    // its bounded preparation are covered by MapRouteTests and DLL linkage.
    public enum MapRouteState { Met, NotMet, Unknown }
    public enum MapStoryKind { Story, ActivityResponse, Unclassified }
    public class MapRouteRead { public PreviewConditionState Current(PreviewCondition condition) => PreviewConditionState.Unknown; }
    public class MapChoiceStep { public string Script, RawLabel; public int SourceOffset, Index; public bool ReadCurrentBlocker(MapRouteRead read) => false; public bool ReadTopicBlocker(MapRouteRead read) => false; }
    public class MapRoutePath
    {
        public string DialogueCharacter = ""; public List<MapChoiceStep> Steps = new List<MapChoiceStep>();
        public MapStoryKind StoryKind = MapStoryKind.Story;
        public MapStoryKind ReadStoryKind(MapRouteRead read, ref int budget) => StoryKind;
        public MapChoiceStep TopicStep => Steps.Count == 0 ? null : Steps[Steps.Count - 1];
        public MapRouteState StoryState = MapRouteState.Unknown;
        public int StoryReads;
        public MapRouteState ReadEntryConditions(MapRouteRead read) => MapRouteState.Met;
        public MapRouteState ReadStoryConditions(MapRouteRead read) { StoryReads++; return StoryState; }
        public bool ReadCurrentBlocker(MapRouteRead read) => false;
        public string Description = "fixture path";
        public MapRouteState ReadChoiceAvailability(MapRouteRead read, ref int budget, int afterStep = -1) => MapRouteState.Met;
        public PreviewCondition CurrentPrerequisite => PreviewConditions.Unknown;
        public static PreviewCondition CommonPrerequisite(PreviewCondition left, PreviewCondition right) => PreviewConditions.Unknown;
    }
    public class MapStoryEntry
    {
        public List<MapRoutePath> Paths = new List<MapRoutePath>();
        public MapRouteState ReadStoryConditions(MapRouteRead read)
        {
            bool unknown = false; foreach (var path in Paths)
            { var state = path.ReadStoryConditions(read); if (state == MapRouteState.Met) return state; unknown |= state == MapRouteState.Unknown; }
            return unknown || Paths.Count == 0 ? MapRouteState.Unknown : MapRouteState.NotMet;
        }
    }
    public class MapRouteEvent
    {
        public object Event; public string ResultScript, Id;
        public MapRouteEvent(object target, string resultScript, string id = null) { Event = target; ResultScript = resultScript; Id = id; }
    }
    public class MapRouteEntry
    {
        public object Event; public string ResultScript, Pool, Id, Path;
        public List<MapRoutePath> Paths = new List<MapRoutePath>();
        public bool HasChoice; public MapRouteState Current = MapRouteState.Met;
        public MapRouteState ReadCurrent(MapRouteRead read) => Current;
    }
    public class MapRouteDefinitions
    {
        public bool UseSharedSyntax { get; set; }
        public int SharedSyntaxHits => 0;
        public static void ClearSharedSyntaxCache() { }
        public sealed class PreparationSlice { public bool ShouldYield() => false; }
        public static int Builds, Advances;
        public static MapRouteState State = MapRouteState.Met;
        public static bool Choice;
        public static bool StoryIncomplete;
        public static bool FixturePreparing, FixturePartial, FixtureReadFailure;
        public static readonly List<MapStoryEntry> StoryFixtures = new List<MapStoryEntry>();
        public readonly List<MapRouteEntry> Entries = new List<MapRouteEntry>();
        public readonly List<MapStoryEntry> StoryEntries = new List<MapStoryEntry>();
        public int WorkCount => Advances;
        private string root; private Func<string,ScriptReadResult> reader; private Func<string,IList<MapRouteEvent>> pools;
        private bool done;
        public bool IsPreparing { get; private set; } = true;
        public bool IsPartial, HasReadFailure; public string Diagnostic = "";
        public bool HasIncompleteStoryPaths => StoryIncomplete;
        public bool CurrentOmissionsBlocked(MapChoiceStep first, MapRouteRead read) => false;
        public bool CurrentOmissionsBlocked(MapChoiceStep first, MapRouteRead read, ref int budget) => false;
        public bool CurrentOmissionsBlocked(MapChoiceStep first, IList<MapChoiceStep> observedTopics, MapRouteRead read, ref int budget) => false;
        public bool CanBuildBoundedProof(string script) => true;
        public MapRouteDefinitions(string key, Func<string,ScriptReadResult> source, Func<string,IList<MapRouteEvent>> poolReader)
        { root = key; reader = source; pools = poolReader; Builds++; }
        public void Advance(float now, int maxWork = 8192)
        {
            Advances++;
            if (!done)
            {
                var script = reader(root);
                if (script.State != ScriptReadState.Success) throw new DataNotReadyException();
                StoryEntries.AddRange(StoryFixtures);
                if (!string.IsNullOrEmpty(script.Text))
                    foreach (var value in pools(script.Text)) Entries.Add(new MapRouteEntry { Event = value.Event, ResultScript = value.ResultScript, Id = value.Id, Pool = script.Text, Path = root + " -> " + script.Text });
                done = true; IsPreparing = false;
            }
            foreach (var value in Entries) { value.Current = State; value.HasChoice = Choice; }
            IsPreparing = FixturePreparing; IsPartial = FixturePartial; HasReadFailure = FixtureReadFailure;
        }
        public void AdvanceBudgeted(float now, int maxWork, Func<bool> shouldYield) => Advance(now, maxWork);
        public void Clear() { Entries.Clear(); }
    }
}

#endif

namespace DiceMaster.Information
{
    // Production localization is exercised by ChoiceStoryTests. These provider
    // tests keep game locale/UI objects at an explicit identity boundary.
    public static class ChoiceStoryLabels
    {
        public static Func<string,string,string> DialogueNameReader;
        public static string Resolve(string raw) => raw;
        public static string DialogueTarget(string character) => character + " 대화";
        public static string DialogueName(string raw, string character) => DialogueNameReader == null ? raw : DialogueNameReader(raw, character);
        public static string Path(System.Collections.Generic.IList<MapChoiceStep> steps)
        { var labels = new System.Collections.Generic.List<string>(); foreach (var step in steps) labels.Add(step.RawLabel); return string.Join(" → ", labels); }
    }
}
