using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

// Only game, Unity, translation and existing plugin UI boundaries are modeled.
// The runner installs the actual DLL parser, Action and LiveLog payload types.
namespace UnityEngine
{
    public class Object
    {
        public string name { get; set; }
        public bool Destroyed;
        public static bool operator ==(Object a, Object b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (ReferenceEquals(a, null)) return b.Destroyed;
            if (ReferenceEquals(b, null)) return a.Destroyed;
            return false;
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object value) => ReferenceEquals(this, value);
        public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
    }
    public static class Time { public static float unscaledTime { get; set; } }
    public static class Random
    {
        public static int Calls;
        public static int Range(int minimum, int maximum) { Calls++; throw new InvalidOperationException("Display called Unity RNG boundary."); }
    }
}
namespace BepInEx { public static class Utility { public static bool IsNullOrWhiteSpace(string value) => string.IsNullOrWhiteSpace(value); } }
namespace HarmonyLib
{
    public static class AccessTools
    {
        public static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
    }
}
namespace Mortal.Core
{
    public enum PreviewStatKind
    {
        [OBB.Framework.Attributes.StringValue("disposition")] Disposition,
        [OBB.Framework.Attributes.StringValue("mental")] Mental
    }
    public class PlayerStatManagerData
    {
        private static PlayerStatManagerData _instance = new PlayerStatManagerData();
        public object _stats = new PlayerStatData();
        public static int InstanceReads;
        public static PlayerStatManagerData Instance { get { InstanceReads++; throw new InvalidOperationException("Preview called game singleton getter"); } }
        public static void Ready(bool ready) { _instance = ready ? new PlayerStatManagerData() : null; }
        public static GameStat Primary => (GameStat)((PlayerStatData)_instance._stats).List[0];
    }
    public class PlayerStatData
    {
        public IList List = new object[] { new GameStat { _type = PreviewStatKind.Disposition }, new GameStat { _type = PreviewStatKind.Mental } };
    }
    public class ItemDatabase
    {
        private static ItemDatabase _instance;
        public static int InstanceReads;
        public static ItemDatabase Instance { get { InstanceReads++; throw new InvalidOperationException("Preview initialized ItemDatabase"); } }
        public static bool IsAbsent => _instance == null;
    }
    public class StatConvertCollectionData { public IList List = new List<object>(); }
    public enum GameItemType { None, Book, Misc, Special }
    public interface ILocaleResolver { string GetString(string key); string GetStoryText(string key); }
    public class LocalizationManager : UnityEngine.Object
    {
        public static LocalizationManager Instance { get; set; }
        public ILocaleResolver LocaleResolver { get; set; }
    }
    public enum StatCompareType { Equal, NotEqual, Less, LessEqual, Greater, GreaterEqual }
    public enum LogicOperatorType { And, Or }
    public enum PositionType { None, Fixture }
    public class ConditionResultItem
    {
        public List<StatCompareItem> _items = new List<StatCompareItem>();
        public LogicOperatorType _logicOp;
    }
    public class ConditionResultData : UnityEngine.Object
    {
        public int _count;
        public List<ConditionResultItem> _items = new List<ConditionResultItem>();
        public static int Executions;
        public bool Execute() { Executions++; throw new InvalidOperationException("Preview executed original condition"); }
    }
    public class StatGroupVariable : UnityEngine.Object
    {
        public int _groupType = 1;
        public List<StatValueReference> _value = new List<StatValueReference> { new StatValueReference() };
        public static int OriginalReads;
        public int Reads => _value[0]._stat.Reads;
        public bool Throw { get => _value[0]._stat.Throw; set => _value[0]._stat.Throw = value; }
        public int GetResult()
        {
            OriginalReads++;
            if (Throw) throw new InvalidOperationException("fixture GetResult failure");
            return DiceMaster.DataHelper.CurrentValue;
        }
    }
    public class StatValueReference
    {
        public int _checkType = 1;
        public bool _negative;
        public int _constant, _randomMin, _randomMax;
        public StatGroupVariable _statGroup;
        public object _condition;
        public GameStat _stat = new GameStat();
    }
    public class GameStat
    {
        public PreviewStatKind _type;
        public object _statAdditionData;
        public int Reads;
        public bool Throw;
        public int FinalValue
        {
            get
            {
                Reads++;
                if (Throw) throw new InvalidOperationException("fixture readonly stat failure");
                return DiceMaster.DataHelper.CurrentValue;
            }
        }
    }
    public class StatCompareItem
    {
        public StatGroupVariable _value1 = new StatGroupVariable { name = "fixture_left" };
        public StatGroupVariable _value2 = new StatGroupVariable { name = "fixture_right" };
        public StatCompareType _compareOp;
        public static int OriginalReads;
        public bool Throw { get => _value1.Throw; set => _value1.Throw = value; }
        public bool Check() { OriginalReads++; if (Throw) throw new InvalidOperationException("fixture Check failure"); return true; }
    }
    public class MissionCheckData : UnityEngine.Object
    {
        public string _desc = "fixture stage";
        public bool _dayCheck;
        public bool _active = true;
        public string Key { get; set; } = "stage";
        public ConditionResultItem[] _conditions = new ConditionResultItem[0];
        public PositionType Position { get; set; } = PositionType.Fixture;
        public string StartScript { get; set; } = "fixture_start";
    }
    public class CollectionData<T> : UnityEngine.Object { public List<T> List { get; set; } = new List<T>(); }
    public class SubMissionsData : CollectionData<MissionData> { }
    public class MissionData : UnityEngine.Object
    {
        public string _desc = "fixture mission";
        public MissionCheckData Stage = new MissionCheckData();
        public MissionCheckData[] _missions => new[] { Stage };
        public int Reads;
        public bool Throw;
        public string CurrentKey { get; set; } = "stage";
        public MissionCheckData GetByKey(string key) { Reads++; if (Throw) throw new InvalidOperationException("fixture GetByKey failure"); return Stage; }
    }
    public class MissionManagerData : UnityEngine.Object
    {
        private static MissionManagerData _instance = new MissionManagerData();
        public static int InstanceReads;
        public static MissionManagerData Instance { get { InstanceReads++; return _instance; } set { _instance = value; } }
        public Dictionary<string, MissionCheckData> ActiveSubMissions { get; set; } = new Dictionary<string, MissionCheckData>();
        public object _subMissions = new MissionCollection();
    }
    public class MissionCollection { public IList List = new List<object> { new MissionData() }; }
}
namespace OBB.Framework.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class StringValueAttribute : Attribute { public StringValueAttribute(string value) { } }
}
namespace Mortal.Story
{
    public class SwitchResultData : UnityEngine.Object
    {
        public List<Mortal.Core.ConditionResultItem> _items = new List<Mortal.Core.ConditionResultItem>();
        public bool _useCount;
        public int _count;
        public static int Executions;
        public int Execute() { Executions++; throw new InvalidOperationException("Preview executed original switch"); }
    }
    public class SwitchResultConfig : UnityEngine.Object
    {
        public List<SwitchResultData> _list = new List<SwitchResultData>();
        public Dictionary<string, SwitchResultData> Items = new Dictionary<string, SwitchResultData>();
        public SwitchResultData GetData(string key) { Items.TryGetValue(key, out var value); return value; }
    }
    public class ConditionResultConfig : UnityEngine.Object
    {
        public List<Mortal.Core.ConditionResultData> _list = new List<Mortal.Core.ConditionResultData>();
    }
    public class PositionEventData : UnityEngine.Object
    {
        public string ResultType { get; set; } = "event";
        public int State, _defaultRate = 10;
        public bool _toggleCondition;
        public Mortal.Core.ConditionResultItem _activeCondition;
        public PositionEventRateItem[] _eventRateItem = new PositionEventRateItem[0];
    }
    public class PositionEventRateItem { public Mortal.Core.ConditionResultItem Condition; public int Rate; }
    public class CheckPointManager : UnityEngine.Object
    {
        public static CheckPointManager Instance { get; set; } = new CheckPointManager();
        public object _position = new PositionCollection();
        public object _switchList = new object[0];
        public ConditionResultConfig _condition = new ConditionResultConfig();
    }
    public class PositionCollection { public IList List = new List<object>(); }
    public class PositionDefinition : UnityEngine.Object { public IList List = new List<object>(); }
}
namespace DiceMaster.Config
{
    public class ConfigElement<T> { public T Value { get; set; } }
    public static class ConfigManager { public static ConfigElement<bool> EnableOptionInfo = new ConfigElement<bool> { Value = true }; }
}
namespace DiceMaster
{
    namespace EmbeddedTranslations
    {
        public static class Display
        {
            public static bool Throw;
            public static string Translate(string value)
            { if (Throw) throw new InvalidOperationException("fixture translation failure"); return value; }
        }
    }
    public static class Probe { public static int ParserEntries; }
    public enum StoryReadState { NoStory, Unread, Read }
    public class DicePlugin : UnityEngine.Object
    {
        public static DicePlugin Instance { get; set; }
        public List<(string Key, string OptionText, string Description)> optionEntries = new List<(string, string, string)>();
        public List<(string Key, string Description)> switchEntries = new List<(string, string)>();
        public List<(Mortal.Story.PositionEventData EventData, string Key, string Description)> eventEntries = new List<(Mortal.Story.PositionEventData, string, string)>();
        public List<(Mortal.Core.MissionCheckData MissionData, string Key, string Description)> missionEntries = new List<(Mortal.Core.MissionCheckData, string, string)>();
        public bool showLogWindow;
        public string currentTab = "선택지";
        public static List<string> Info = new List<string>(), Warnings = new List<string>(), Errors = new List<string>();
        public static void LogInfo(object value) => Info.Add(value?.ToString() ?? "");
        public static void LogWarning(object value) => Warnings.Add(value?.ToString() ?? "");
        public static void LogError(object value) => Errors.Add(value?.ToString() ?? "");
        public void ClearLogEntries() { optionEntries.Clear(); switchEntries.Clear(); eventEntries.Clear(); missionEntries.Clear(); }
        public void AddOptionEntry(string key, string description = "", StoryReadState state = StoryReadState.NoStory, string optionText = "") => optionEntries.Add((key, optionText, description));
        public void AddSwitchEntry(string key, string description, StoryReadState state = StoryReadState.NoStory, string branchStateText = "") => switchEntries.Add((key, description));
        public void AddEventEntry(Mortal.Story.PositionEventData value, string key, string description) => eventEntries.Add((value, key, description));
        public void AddMissionEntry(Mortal.Core.MissionCheckData value, string key, string description) => missionEntries.Add((value, key, description));
        static string ColorizeKeyByStoryReadState(string key, StoryReadState state) => throw new InvalidOperationException("Actual color helper IL not installed");
        static string NormalizeCheckpointKey(string value) => throw new InvalidOperationException("Actual checkpoint helper IL not installed");
    }
    public static class OptionInfoPatch
    {
        public static int DisplayRefreshDepth;
        public static Mortal.Story.PositionEventData TargetEvent;
        public static Mortal.Core.MissionCheckData TargetMission;
        public static bool _suppressStoryTextPostfix;
        public static string _lastLuaString, _lastFriendlyName;
        public static void LeanLocalizationResolver_GetStoryText_Postfix(string key, ref string result) => throw new InvalidOperationException("Actual summary IL not installed");
        static string GetLocalizedSystemText(string key, string fallback) => fallback;
        static string ResolveOptionStoryText(string key) => "선택 문구 " + key;
        static StoryReadState EvaluateStoryReadState(object source, string key) => StoryReadState.Unread;
        static StoryReadState EvaluateSwitchStoryReadState(object source, string key) => StoryReadState.Unread;
        static string BuildSwitchBranchStateText(object source, string key) => "1 2";
        public static int PreviewReadGroup(Mortal.Core.StatGroupVariable value) => throw new InvalidOperationException("Actual pure group wrapper IL not installed");
        public static bool PreviewCheckComparison(Mortal.Core.StatCompareItem value) => throw new InvalidOperationException("Actual pure compare wrapper IL not installed");
    }
    public static class DataHelper
    {
        public static int CurrentValue, ConditionReads, MissionReads, ScriptReads;
        public static bool Ready = true;
        public static object LiveMissionFilter;
        public static string FailingCheckpoint;
        public static Func<string, object> PreviewScriptReader;
        public static object ReadPreviewScriptBoundary(string name)
        {
            if (PreviewScriptReader != null) { ScriptReads++; return PreviewScriptReader(name); }
            Type result = typeof(DataHelper).Assembly.GetType("DiceMaster.LiveLog.ScriptReadResult", true);
            return result.GetMethod("Success").Invoke(null, new object[] { LoadLuaScript(name) });
        }
        public static string LoadLuaScript(string name) { ScriptReads++; return "statmodifymanager.SetFlag(\"G9000\", 2)"; }
        public static string GetItemName(Mortal.Core.GameItemType type, string id) => id;
        public static string GetTalentName(string id) => id;
        public static string GetTeamName(string id) => id;
        public static string ParseSwitchCheckpoint(string checkpoint)
        {
            ConditionReads++;
            if (checkpoint == FailingCheckpoint) throw new InvalidOperationException("fixture condition failure");
            return "current=" + CurrentValue;
        }
        public static Dictionary<Mortal.Core.MissionCheckData, (string, string)> GetMissions()
        {
            MissionReads++;
            return new Dictionary<Mortal.Core.MissionCheckData, (string, string)> { [Scenario.Mission] = ("fixture mission", "current=" + CurrentValue) };
        }
        public static string ExtractConditions(Mortal.Core.ConditionResultItem value, out bool inPeriod) => throw new InvalidOperationException("Actual condition helper IL not installed");
        static List<string> ExtractConditions(Mortal.Core.MissionCheckData value, out bool inPeriod) => throw new InvalidOperationException("Actual mission condition helper IL not installed");
        static bool IsValidMission(Mortal.Core.MissionCheckData value) => throw new InvalidOperationException("Actual mission validity helper IL not installed");
        public static bool GetMissionPeriod(Mortal.Core.MissionCheckData value, out string period) { period = "fixture period"; return true; }
        static string GetLocalizedSystemText(string key, string fallback, string category, string original) => fallback;
        static string FormatConditionValName(Mortal.Core.StatGroupVariable value) => value?.name ?? "?";
        static string GetCompareOperator(Mortal.Core.StatCompareType value) => new[] { "==", "!=", "<", "<=", ">", ">=" }[(int)value];
        static string GetLogicOperator(Mortal.Core.LogicOperatorType value) => "&&";
    }
    public static class ReflectionExtensions
    {
        public static T GetFieldValue<T>(this object value, string name) => throw new InvalidOperationException("Actual field helper IL not installed");
    }
    public static class LocationTranslations { public static string TranslateLocation(string value) => value; }
    public class PositionEventCache
    {
        public static PositionEventCache Instance { get; set; } = new PositionEventCache();
        public static int ListReads, DescriptionReads;
        public static int StaticReads;
        public static bool ThrowDescription;
        public Dictionary<Mortal.Story.PositionEventData, string> eventDescCache = new Dictionary<Mortal.Story.PositionEventData, string>();
        public Dictionary<Mortal.Story.PositionEventData, string> scriptPrefixCache = new Dictionary<Mortal.Story.PositionEventData, string>();
        public List<Mortal.Story.PositionEventData> GetPositionEvents(string position) { ListReads++; return new List<Mortal.Story.PositionEventData> { Scenario.Event }; }
        public string GetPositionEventName(Mortal.Story.PositionEventData value) => "fixture event";
        public string GetPositionEventDesc(Mortal.Story.PositionEventData value)
        {
            DescriptionReads++;
            if (ThrowDescription) throw new InvalidOperationException("fixture event failure");
            return "rate=" + DataHelper.CurrentValue;
        }
        string GenerateStaticDesc(Mortal.Story.PositionEventData value) { StaticReads++; scriptPrefixCache[value] = "fixture"; return Scenario.ExtractEventDescription(); }
        string GenerateDynamicDesc(Mortal.Story.PositionEventData value) => throw new InvalidOperationException("Actual event formatter IL not installed");
        public void ClearDisplayCaches() { eventDescCache.Clear(); }
        public void RetryDefinitionReads(Mortal.Story.PositionEventData value) { }
    }
    public static class UiTextMapper { public static string ApplyTranslations(string value) => value; }
    public static class UntranslatedLogger { public static void LogUntranslated(string category, string key, string original) { } }
    public static class RichText
    {
        public static int ConditionColors;
        public static readonly List<(string Text, bool Passed)> ConditionVerdicts = new List<(string, bool)>();
        public static string ColorText(string value) => value;
        public static string Green(string value) { ConditionColors++; ConditionVerdicts.Add((value, true)); return value; }
        public static string Red(string value) { ConditionColors++; ConditionVerdicts.Add((value, false)); return value; }
        public static string Golden(string value) => value;
    }
    public static class FlagTranslations { public static string TranslateDevNote(string id, int value) => id; }
    public static class PlayerinfoTranslations { public static string TranslatePlayerstats(string value) => value; }
    public static class CharacterTranslations { public static string TranslateCharacter(string value) => value; }
    public static class Scenario
    {
        public static Mortal.Story.PositionEventData Event = new Mortal.Story.PositionEventData { name = "event" };
        public static Mortal.Core.MissionCheckData Mission = new Mortal.Core.MissionCheckData { name = "mission" };
        static int assertions;
        static readonly Assembly Self = typeof(Scenario).Assembly;
        static object Invoke(string type, string method, params object[] arguments)
        {
            try { return Self.GetType(type, true).GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, arguments); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }
        static object Parser => Self.GetType("DiceMaster.LuaParse.LuaParser", true).GetProperty("Instance").GetValue(null);
        static object EventParser => Invoke("DiceMaster.LiveLog.LogBridge", "GetEventParser");
        static IDictionary ModificationCache(object parser) => (IDictionary)parser.GetType().GetField("luaModification", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(parser);
        static IDictionary Options => (IDictionary)Parser.GetType().GetField("OptionResult").GetValue(Parser);
        static void Check(bool condition, string message) { assertions++; if (!condition) throw new InvalidOperationException(message); }
        static void SeedDisplayRandom(int seed) => Self.GetType("DiceMaster.LiveLog.RandomPreview", true)
            .GetField("random", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, new System.Random(seed));
        static int NumericRandom(string text, int minimum, int maximum)
        {
            Match match = Regex.Match(text, @"隨機\((-?\d+)\)");
            Check(match.Success, "Actual rewritten helper did not render a numeric random operand: " + text);
            int value = int.Parse(match.Groups[1].Value);
            Check(value >= minimum && value <= maximum, "Actual rewritten helper random operand is outside configured inclusive bounds");
            Check(!text.Contains("표시용") && !text.Contains("실행 시 판정") && !text.Contains("미지원") && !text.Contains("이론 확률") && !text.Contains("실패"),
                "Supported random description retained an annotation or entered the generic failure path");
            return value;
        }
        static void Tick(DicePlugin plugin, bool manual = false) => Invoke("DiceMaster.LiveLog.LogBridge", "Tick", plugin, manual);
        static object Member(object value, string name)
        {
            var type = value.GetType();
            return type.GetProperty(name)?.GetValue(value) ?? type.GetField(name)?.GetValue(value);
        }
        static object InstanceCall(object value, string method, params object[] arguments)
        {
            try { return value.GetType().GetMethod(method).Invoke(value, arguments); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }
        static object LogModel => Self.GetType("DiceMaster.LiveLog.LogBridge", true).GetField("Model").GetValue(null);
        static object LogTab(string tab) => InstanceCall(LogModel, "Tab", tab);
        static IList ModelRows(string tab) => (IList)Member(Member(LogTab(tab), "Snapshot"), "Rows");
        static void NewLua(string lua, string name) => Invoke("DiceMaster.LiveLog.LogBridge", "NewLua", lua, name);
        static long TransitionPrefix(string name)
        {
            object[] arguments = { 0L };
            Invoke("DiceMaster.OptionInfoPatch", "LiveLog_" + name + "_Prefix", arguments);
            return (long)arguments[0];
        }
        static Exception TransitionFinalizer(string name, Exception error, long token) =>
            (Exception)Invoke("DiceMaster.OptionInfoPatch", "LiveLog_" + name + "_Finalizer", error, token);
        static long LuaPrefix(string lua, string name)
        {
            object[] arguments = { lua, name, 0L };
            Invoke("DiceMaster.OptionInfoPatch", "LiveLog_LuaStage_Prefix", arguments);
            return (long)arguments[2];
        }
        static void LuaPostfix(long token, object result) => Invoke("DiceMaster.OptionInfoPatch", "LiveLog_LuaComplete_Postfix", result, token);
        static Exception LuaFinalizer(long token, Exception error) =>
            (Exception)Invoke("DiceMaster.OptionInfoPatch", "LiveLog_LuaAbort_Finalizer", error, token);
        static int AnalysisCount => (int)Self.GetType("DiceMaster.LiveLog.LogBridge", true).GetField("AnalysisCount").GetValue(null);
        static string Lua(string first = "O_fixture_one") => "--index:" + first + "\nstatmodifymanager.SetFlag(\"G3000\", 2)\nstatmodifymanager.AddFlag(\"G4000\", -3)\ncheckpointmanager.Switch(\"switch_one\")\ncheckpointmanager.Switch(\"switch_two\")\ncheckpointmanager.Position(\"fixture_position\")\n";
        public static string ExtractEventDescription()
        {
            object parser = EventParser;
            object actions = parser.GetType().GetMethod("ExtractActionsFromLua").Invoke(parser, new object[] { "fixture_event" });
            return (string)parser.GetType().GetMethod("Description").Invoke(null, new[] { actions });
        }

        // Production DLL parser -> RefreshEngine -> LogBridge -> structured
        // model -> guarded action boundary. Only Unity/game containers are fake.
        public static int RunLogModel()
        {
            int before = assertions;
            var plugin = new DicePlugin { showLogWindow = true }; DicePlugin.Instance = plugin;
            Config.ConfigManager.EnableOptionInfo.Value = true;
            DataHelper.Ready = true; DataHelper.FailingCheckpoint = null;
            PositionEventCache.ThrowDescription = false;
            var position = new Mortal.Story.PositionDefinition { name = "fixture_position" };
            position.List.Add(Event);
            var positions = new Mortal.Story.PositionCollection(); positions.List.Add(position);
            Mortal.Story.CheckPointManager.Instance = new Mortal.Story.CheckPointManager { _position = positions };
            var mission = new Mortal.Core.MissionData { name = "raw_mission", Stage = Mission, CurrentKey = Mission.Key };
            var missions = new Mortal.Core.MissionCollection(); missions.List.Clear(); missions.List.Add(mission);
            Mortal.Core.MissionManagerData.Instance = new Mortal.Core.MissionManagerData { _subMissions = missions };
            UnityEngine.Time.unscaledTime = 100;
            NewLua(Lua("O_model"), "model"); Tick(plugin);
            long context = (long)Member(LogModel, "ContextId");
            object row = ModelRows("선택지")[0];
            Check((string)Member(row, "Id") == "O_model" && (string)Member(row, "RawId") == "O_model", "Actual bridge discarded stable/raw option IDs");
            Check(((string)Member(row, "Description")).Contains("G3000"), "Actual bridge did not publish analyzed description");
            Check(((IList)Member(row, "AnalysisStates")).Count > 0, "Actual structured analysis states never reached model");
            Check((string)Member(row, "RefreshStatus") == "" && Member(row, "RefreshState").ToString() == "Healthy", "Healthy query status was conflated with analysis");
            plugin.currentTab = "이벤트"; UnityEngine.Time.unscaledTime = 101; Tick(plugin);
            row = ModelRows("이벤트")[0]; string id = (string)Member(row, "Id");
            Check((string)Member(row, "RawId") == Event.name && ReferenceEquals(Member(row, "Target"), Event), "Actual event identity/target was lost at publication");
            Check(((IList)Member(row, "AnalysisStates")).Count > 0, "Fixed event analysis did not reach structured model");
            object state = LogTab("이벤트"); InstanceCall(state, "Select", id);
            Check((bool)Invoke("DiceMaster.LiveLog.LogBridge", "TryReserve", context, "이벤트", id, Event), "Valid event did not reach real reservation field");
            Check(ReferenceEquals(OptionInfoPatch.TargetEvent, Event) && (bool)Invoke("DiceMaster.LiveLog.LogBridge", "IsReserved", "이벤트", Event), "Reservation UI source diverged from real field");
            OptionInfoPatch.TargetEvent = null;
            Check(!(bool)Invoke("DiceMaster.LiveLog.LogBridge", "IsReserved", "이벤트", Event), "Consumed actual reservation remained in UI state");
            EmbeddedTranslations.Display.Throw = true; UnityEngine.Time.unscaledTime = 101.5f; Tick(plugin);
            Check(!(bool)Invoke("DiceMaster.LiveLog.LogBridge", "TryReserve", context, "이벤트", id, Event), "Failed actual publication left previous healthy target executable");
            Check((bool)Member(LogTab("이벤트"), "PublicationFailed") && !(bool)Member(LogTab("이벤트"), "ExecutionFocus"), "Actual publication failure did not reach UI state/cancel execution focus");
            EmbeddedTranslations.Display.Throw = false; Tick(plugin, true);
            Check(!(bool)Member(LogTab("이벤트"), "PublicationFailed"), "Complete actual publication did not clear UI publication failure");
            position.List.Clear();
            Check(!(bool)Invoke("DiceMaster.LiveLog.LogBridge", "TryReserve", context, "이벤트", id, Event), "Removed event in retained row was still reservable");
            position.List.Add(Event);
            Event.Destroyed = true;
            Check(!(bool)Invoke("DiceMaster.LiveLog.LogBridge", "TryReserve", context, "이벤트", id, Event), "Destroyed Unity target remained reservable through retained CLR reference");
            Event.Destroyed = false;
            PositionEventCache.ThrowDescription = true; UnityEngine.Time.unscaledTime = 102; Tick(plugin);
            row = ModelRows("이벤트")[0];
            Check(Member(row, "RefreshState").ToString() == "RetainedFailure" && (bool)Member(row, "HasLastGood") && !(bool)Member(row, "CanReserve"), "Actual refresh failure did not disable last-good action");
            Check(!(bool)Invoke("DiceMaster.LiveLog.LogBridge", "TryReserve", context, "이벤트", id, Event), "Failed latest event query permitted reservation");
            PositionEventCache.ThrowDescription = false; Tick(plugin, true);
            Check((string)Member(Member(LogTab("이벤트"), "Selected"), "Id") == id, "Same-target manual recovery lost selection");
            plugin.currentTab = "미션"; UnityEngine.Time.unscaledTime = 103; Tick(plugin);
            row = ModelRows("미션")[0]; id = (string)Member(row, "Id");
            InstanceCall(LogTab("미션"), "Select", id);
            Check((bool)Invoke("DiceMaster.LiveLog.LogBridge", "TryReserve", context, "미션", id, Mission), "Valid mission reservation failed live membership");
            Check(ReferenceEquals(OptionInfoPatch.TargetMission, Mission) && Mortal.Core.MissionManagerData.Instance.ActiveSubMissions.Values.Contains(Mission), "Mission reservation semantics changed");
            mission.CurrentKey = "different_stage";
            Check(!(bool)Invoke("DiceMaster.LiveLog.LogBridge", "TryReserve", context, "미션", id, Mission), "Stale mission stage was reservable");
            mission.CurrentKey = Mission.Key; missions.List.Clear();
            Check(!(bool)Invoke("DiceMaster.LiveLog.LogBridge", "TryReserve", context, "미션", id, Mission), "Removed mission was reservable");
            plugin.showLogWindow = false;
            NewLua(Lua("O_next_model"), "model-next");
            Check((long)Member(LogModel, "ContextId") != context && Member(LogTab("미션"), "Selected") == null && ModelRows("미션").Count == 0, "Closed-window context boundary did not invalidate UI state");
            Check(!(bool)Invoke("DiceMaster.LiveLog.LogBridge", "TryReserve", context, "미션", id, Mission), "Prior-context target survived actual Lua publication boundary");
            return assertions - before;
        }

        public static int Run()
        {
            var plugin = new DicePlugin(); DicePlugin.Instance = plugin;
            Config.ConfigManager.EnableOptionInfo.Value = true;
            Invoke("DiceMaster.LiveLog.LogBridge", "ResetContext");
            NewLua(Lua(), "fixture");
            Check(!ReferenceEquals(Parser, EventParser), "Event definitions reused the shared option parser");
            Check(ModificationCache(Parser).Count == 0 && ModificationCache(EventParser).Count == 1,
                "Cold event preparation wrote into the shared parser's modification cache");
            plugin.showLogWindow = true;
            UnityEngine.Time.unscaledTime = 0;
            Tick(plugin);
            Check(Options.Contains("O_fixture_one"), "Actual parser did not produce the option key");
            var originalActions = Options["O_fixture_one"];
            Check(plugin.optionEntries.Count == 1, "Actual display path did not show the parsed option");
            string expected = plugin.optionEntries[0].Description;
            Check(expected.Contains("G3000)(2)") && expected.Contains("G4000") && expected.Contains("-3"), "SetFlag/AddFlag planned values changed: " + expected);
            int analyses = AnalysisCount;
            int parserEntries = Probe.ParserEntries;
            for (int i = 1; i <= 8; i++) { DataHelper.CurrentValue = i * 10; UnityEngine.Time.unscaledTime = i; Tick(plugin); }
            Check(ReferenceEquals(originalActions, Options["O_fixture_one"]), "Periodic display changed shared parser action ownership");
            Check(plugin.optionEntries[0].Description == expected, "Repeated warm-cache display lost planned results");
            Check(AnalysisCount == analyses, "Normal periodic display reparsed Lua");
            Check(Probe.ParserEntries == parserEntries, "Normal display called the actual Lua extraction methods");
            string story = "선택 문구";
            OptionInfoPatch.LeanLocalizationResolver_GetStoryText_Postfix("O_fixture_one", ref story);
            Check(story.Contains("G3000)(2)") && story.Contains("G4000") && story.Contains("-3"), "Actual game option summary diverged from parser results: " + story);
            Tick(plugin, true);
            Check(AnalysisCount == analyses + 1, "Manual refresh did not regenerate fixed analysis");
            Check(Probe.ParserEntries > parserEntries, "Manual refresh did not call the real Lua parser");
            Check(plugin.optionEntries.Count == 1 && plugin.optionEntries[0].Description == expected, "Manual refresh changed the option result");
            UnityEngine.Time.unscaledTime = 10; Tick(plugin);
            Check(plugin.optionEntries[0].Description == expected, "Automatic refresh after manual regeneration lost results");
            int reads = DataHelper.ConditionReads + DataHelper.MissionReads + PositionEventCache.DescriptionReads;
            plugin.showLogWindow = false; UnityEngine.Time.unscaledTime = 20; Tick(plugin);
            Check(reads == DataHelper.ConditionReads + DataHelper.MissionReads + PositionEventCache.DescriptionReads, "Hidden window evaluated current values");
            plugin.showLogWindow = true; plugin.currentTab = "조건"; UnityEngine.Time.unscaledTime = 21; Tick(plugin);
            Check(DataHelper.ConditionReads > 0, "Visible condition tab was not evaluated");
            Check(DataHelper.MissionReads == 0 && PositionEventCache.DescriptionReads == 0, "Inactive tabs evaluated current values");
            Check(plugin.switchEntries.Count == 2, "Condition tab lost real-parser checkpoint identities");
            DataHelper.CurrentValue = 999; UnityEngine.Time.unscaledTime = 22; Tick(plugin);
            Check(plugin.switchEntries.All(row => row.Description.Contains("999")), "Conditions did not refresh current values");
            DataHelper.FailingCheckpoint = "switch_one"; DataHelper.CurrentValue = 1000;
            UnityEngine.Time.unscaledTime = 22.5f; Tick(plugin);
            Check(plugin.switchEntries[0].Description.Contains("999") && plugin.switchEntries[0].Description.Contains("갱신 실패 · 이전 값"), "Failed item did not retain its last normal value");
            Check(plugin.switchEntries[1].Description.Contains("1000"), "A failed first item blocked its healthy sibling");
            int failureReads = DataHelper.ConditionReads;
            plugin.showLogWindow = false; UnityEngine.Time.unscaledTime = 23; Tick(plugin);
            plugin.showLogWindow = true; UnityEngine.Time.unscaledTime = 24; Tick(plugin);
            Check(DataHelper.ConditionReads == failureReads + 1, "Reopening retried a failed row before its 5-second deadline");
            plugin.currentTab = "선택지"; UnityEngine.Time.unscaledTime = 24.5f; Tick(plugin);
            plugin.currentTab = "조건"; UnityEngine.Time.unscaledTime = 25; Tick(plugin);
            Check(DataHelper.ConditionReads == failureReads + 2, "Tab switching retried a failed row before its deadline");
            DataHelper.FailingCheckpoint = null;
            Tick(plugin, true);
            Check(plugin.switchEntries.All(row => row.Description == "current=1000"), "Manual refresh did not bypass the failed item's wait and recover");
            Check(OptionInfoPatch.DisplayRefreshDepth == 0, "Display protection leaked after a failure");
            plugin.currentTab = "이벤트"; UnityEngine.Time.unscaledTime = 26; Tick(plugin);
            Check(plugin.eventEntries.Count == 1 && plugin.eventEntries[0].Description == "rate=1000", "Visible event tab was not evaluated");
            parserEntries = Probe.ParserEntries;
            int staticReads = PositionEventCache.StaticReads;
            UnityEngine.Time.unscaledTime = 26.5f; Tick(plugin);
            Check(Probe.ParserEntries == parserEntries && PositionEventCache.StaticReads == staticReads,
                "Normal event refresh reentered static script parsing");
            Check(ModificationCache(Parser).Count == 0, "Warm event refresh changed the shared parser's modification cache");
            plugin.currentTab = "미션"; UnityEngine.Time.unscaledTime = 27; Tick(plugin);
            Check(plugin.missionEntries.Count == 1 && plugin.missionEntries[0].Description == "current=1000", "Visible mission tab was not evaluated");
            Check(DataHelper.LiveMissionFilter == null, "Per-mission display filter leaked");
            Check(DicePlugin.Warnings.Count(line => line.Contains("item=switch_one") && line.Contains("failure")) == 1,
                "Repeated condition failure emitted duplicate diagnostics");
            Check(DicePlugin.Warnings.Count(line => line.Contains("item=switch_one") && line.Contains("recovered")) == 1,
                "Condition recovery was not recorded exactly once");
            NewLua(Lua("O_fixture_next"), "fixture_next"); UnityEngine.Time.unscaledTime = 28; plugin.currentTab = "선택지"; Tick(plugin);
            Check(!Options.Contains("O_fixture_one") && Options.Contains("O_fixture_next"), "New Lua retained old shared results");
            Check(plugin.optionEntries.Count == 1 && plugin.optionEntries[0].Key.Contains("O_fixture_next"), "New Lua mixed display contexts");
            Config.ConfigManager.EnableOptionInfo.Value = false;
            reads = DataHelper.ConditionReads + DataHelper.MissionReads + PositionEventCache.DescriptionReads;
            plugin.currentTab = "조건"; UnityEngine.Time.unscaledTime = 30; Tick(plugin);
            Check(reads == DataHelper.ConditionReads + DataHelper.MissionReads + PositionEventCache.DescriptionReads, "Disabled option information still evaluated values");
            Config.ConfigManager.EnableOptionInfo.Value = true;
            Mortal.Story.CheckPointManager.Instance = null;
            NewLua(Lua("O_fixture_waiting"), "waiting"); UnityEngine.Time.unscaledTime = 31; Tick(plugin);
            Check(plugin.switchEntries.Count == 2 && plugin.switchEntries.All(row => row.Description.Contains("게임 데이터 대기 중")),
                "Unready game data was treated as a normal empty result");
            Mortal.Story.CheckPointManager.Instance = new Mortal.Story.CheckPointManager();
            Tick(plugin, true);
            Check(plugin.switchEntries.All(row => row.Description == "current=1000"), "Manual refresh did not recover ready game data");
            DataHelper.FailingCheckpoint = "switch_one";
            NewLua(Lua("O_fixture_failed"), "failed"); UnityEngine.Time.unscaledTime = 32; Tick(plugin);
            Check(plugin.switchEntries[0].Description.Contains("읽기 실패") && !plugin.switchEntries[0].Description.Contains("이전 값"),
                "First failure reused a prior context's last value");
            Check(plugin.switchEntries[1].Description == "current=1000", "First failure blocked a sibling in the new context");
            DataHelper.FailingCheckpoint = null;
            NewLua(Lua("O_fixture_reset"), "reset"); UnityEngine.Time.unscaledTime = 32.1f; Tick(plugin);
            Check(plugin.switchEntries.All(row => row.Description == "current=1000"), "A new Lua context inherited the previous failure wait");
            Check(OptionInfoPatch.DisplayRefreshDepth == 0 && DicePlugin.Errors.Count == 0,
                "Real parser reported a cache inconsistency or leaked display protection");

            plugin.currentTab = "이벤트";
            Mortal.Story.CheckPointManager.Instance = null;
            UnityEngine.Time.unscaledTime = 40;
            NewLua(Lua("O_fixture_late_event"), "late_event");
            object lateEventParser = EventParser;
            int sourceFailures = DicePlugin.Warnings.Count(line => line.Contains("item=source/0") && line.Contains("failure"));
            int sourceRecoveries = DicePlugin.Warnings.Count(line => line.Contains("item=source/0") && line.Contains("recovered"));
            Tick(plugin);
            Check(plugin.eventEntries.Count == 1 && plugin.eventEntries[0].Description.Contains("게임 데이터 대기 중"),
                "Late event source did not publish its waiting state");
            Check(ModificationCache(Parser).Count == 0 && ModificationCache(lateEventParser).Count == 0,
                "Waiting event definitions unexpectedly changed a parser cache");
            staticReads = PositionEventCache.StaticReads;
            Mortal.Story.CheckPointManager.Instance = new Mortal.Story.CheckPointManager();
            UnityEngine.Time.unscaledTime = 41; Tick(plugin);
            Check(PositionEventCache.StaticReads == staticReads, "Late event source retried before its five-second wait");
            UnityEngine.Time.unscaledTime = 45.1f; Tick(plugin);
            Check(plugin.eventEntries.Count == 1 && plugin.eventEntries[0].Description == "rate=1000",
                "Late event source did not recover into its real event row");
            Check(ModificationCache(Parser).Count == 0 && ModificationCache(lateEventParser).Count == 1,
                "Late event preparation changed shared Lua analysis data");
            Check(DicePlugin.Warnings.Count(line => line.Contains("item=source/0") && line.Contains("failure")) == sourceFailures + 1 &&
                DicePlugin.Warnings.Count(line => line.Contains("item=source/0") && line.Contains("recovered")) == sourceRecoveries + 1,
                "Late event source did not log one failure and one recovery");
            parserEntries = Probe.ParserEntries;
            UnityEngine.Time.unscaledTime = 46; Tick(plugin);
            Check(Probe.ParserEntries == parserEntries && ModificationCache(Parser).Count == 0,
                "Recovered warm event refresh reentered parsing or wrote shared caches");
            // The actual game-choice postfix must retain uncertainty and fit
            // grouped detail into its existing single-line 50-character space.
            NewLua("--index:O_readable_summary\nif secret_predicate then statmodifymanager.SetPlayer(\"money\",100) else statmodifymanager.SetPlayer(\"money\",200) end\nunknown_effect()", "readable_summary");
            story = "choice";
            OptionInfoPatch.LeanLocalizationResolver_GetStoryText_Postfix("O_readable_summary", ref story);
            string summary = story.Substring(story.IndexOf("<size=18>(", StringComparison.Ordinal) + "<size=18>(".Length);
            summary = summary.Substring(0, summary.IndexOf(")</size>", StringComparison.Ordinal));
            Check(summary.StartsWith("추가 영향 가능"), "Truncation hid uncertainty or retained developer unsupported wording");
            Check(!summary.Contains('\n') && !summary.Contains('\r') && !summary.Contains("<color=") && !summary.Contains("secret_predicate"),
                "Grouped actual summary leaked detail layout, markup or Lua condition text");
            Check(summary.Contains(" / ") && summary.Length <= 53, "Grouped summary lost separators or exceeded the existing 50-character plus ellipsis boundary");
            Check(summary.Contains("가능한 변화") && summary.Contains("경우 A") && !summary.Contains("조건부"),
                "Actual choice summary lost concise possibility/alternative wording");
            NewLua("--index:O_unpreviewable_summary\nunknown_effect()", "unpreviewable_summary");
            story = "choice";
            OptionInfoPatch.LeanLocalizationResolver_GetStoryText_Postfix("O_unpreviewable_summary", ref story);
            Check(story.Contains("결과는 진행 후 확인") && !story.Contains("미지원") && !story.Contains("???"),
                "Entirely unpreviewable actual summary looks like a mod error");
            return assertions;
        }

        public static int RunLifecycle()
        {
            assertions = 0;
            var plugin = new DicePlugin { showLogWindow = true, currentTab = "선택지" }; DicePlugin.Instance = plugin;
            Config.ConfigManager.EnableOptionInfo.Value = true;
            var checkpoint = new Mortal.Story.CheckPointManager(); Mortal.Story.CheckPointManager.Instance = checkpoint;
            var missions = new Mortal.Core.MissionManagerData(); Mortal.Core.MissionManagerData.Instance = missions;
            UnityEngine.Time.unscaledTime = 100;
            NewLua(Lua("O_lifecycle_old"), "lifecycle_old"); Tick(plugin);
            Check(plugin.optionEntries.Count == 1 && Options.Contains("O_lifecycle_old"), "Lifecycle fixture did not start with an active real-parser context");
            OptionInfoPatch.TargetEvent = Event; OptionInfoPatch.TargetMission = Mission;
            PositionEventCache.Instance.eventDescCache[Event] = "old fixed description";
            long outer = TransitionPrefix("SaveSystem_LoadGameData");
            Check(outer != 0, "Production load prefix did not return a finalizer state token");
            Check(plugin.optionEntries.Count + plugin.switchEntries.Count + plugin.eventEntries.Count + plugin.missionEntries.Count == 0,
                "Production load prefix retained old UI rows");
            Check(Options.Count == 0 && PositionEventCache.Instance.eventDescCache.Count == 0,
                "Production load prefix retained shared parser or fixed event caches");
            Check(OptionInfoPatch.TargetEvent == null && OptionInfoPatch.TargetMission == null &&
                OptionInfoPatch._lastLuaString == null && OptionInfoPatch._lastFriendlyName == null,
                "Production load prefix retained cross-session requests or last source");
            int parses = Probe.ParserEntries, analyses = AnalysisCount;
            int queries = DataHelper.ConditionReads + DataHelper.MissionReads + PositionEventCache.DescriptionReads + PositionEventCache.ListReads;
            foreach (string tab in new[] { "선택지", "조건", "이벤트", "미션" })
            {
                plugin.currentTab = tab;
                UnityEngine.Time.unscaledTime += 10;
                Tick(plugin); Tick(plugin, true);
            }
            Check(parses == Probe.ParserEntries && analyses == AnalysisCount,
                "Automatic/manual refresh parsed old Lua while transition was active");
            Check(queries == DataHelper.ConditionReads + DataHelper.MissionReads + PositionEventCache.DescriptionReads + PositionEventCache.ListReads,
                "Automatic/manual refresh queried game values or event definitions while transition was active");
            Check(plugin.optionEntries.Single().Description.Contains("게임 데이터 대기 중") &&
                plugin.switchEntries.Single().Description.Contains("게임 데이터 대기 중") &&
                plugin.eventEntries.Single().Description.Contains("게임 데이터 대기 중") &&
                plugin.missionEntries.Single().Description.Contains("게임 데이터 대기 중"), "Blocked tabs did not publish waiting rows");

            long inner = TransitionPrefix("SaveSystem_ExecuteLoadGameData");
            var loadFailure = new InvalidOperationException("original load failure");
            Check(ReferenceEquals(TransitionFinalizer("SaveSystem_LoadGameData", loadFailure, outer), loadFailure),
                "Production load finalizer replaced an original exception");
            TransitionFinalizer("SaveSystem_LoadGameData", null, outer);
            long whileLoading = LuaPrefix(Lua("O_lifecycle_early"), "lifecycle_early");
            LuaPostfix(whileLoading, new object()); LuaFinalizer(whileLoading, null);
            Check(Options.Count == 0 && AnalysisCount == analyses, "Nested transition or duplicate outer finalizer admitted early Lua");
            TransitionFinalizer("SaveSystem_ExecuteLoadGameData", null, inner);
            Tick(plugin, true);
            Check(Options.Count == 0 && AnalysisCount == analyses && plugin.missionEntries.Single().Description.Contains("게임 데이터 대기 중"),
                "Finishing inner transition implicitly resumed a consumed early Lua");
            Check(ReferenceEquals(checkpoint, Mortal.Story.CheckPointManager.Instance) && ReferenceEquals(missions, Mortal.Core.MissionManagerData.Instance),
                "Lifecycle regression accidentally replaced the same-object game fixtures");

            long nullCompile = LuaPrefix(Lua("O_lifecycle_null"), "lifecycle_null");
            LuaPostfix(nullCompile, null); LuaFinalizer(nullCompile, null);
            Tick(plugin, true);
            Check(Options.Count == 0 && Probe.ParserEntries == parses && AnalysisCount == analyses,
                "Null compilation result activated or parsed source");
            long stale = LuaPrefix(Lua("O_lifecycle_stale"), "lifecycle_stale");
            long current = LuaPrefix(Lua("O_lifecycle_current"), "lifecycle_current");
            LuaPostfix(stale, new object());
            Check(Options.Count == 0 && AnalysisCount == analyses, "Stale production Lua postfix activated another stage");
            LuaPostfix(current, new object()); LuaFinalizer(current, null);
            plugin.currentTab = "선택지"; Tick(plugin);
            Check(Options.Contains("O_lifecycle_current") && !Options.Contains("O_lifecycle_old") && !Options.Contains("O_lifecycle_stale") &&
                plugin.optionEntries.Single().Key.Contains("O_lifecycle_current"), "Verified replacement Lua did not activate a clean parser/UI context");
            Check(AnalysisCount == analyses + 1, "Successful real-result boundary did not analyze exactly once");
            var staleFailure = new ArgumentException("stale compile pipeline failure");
            Check(ReferenceEquals(LuaFinalizer(stale, staleFailure), staleFailure) && Options.Contains("O_lifecycle_current"),
                "Old production Lua finalizer replaced an exception or revoked newer context");
            var pipelineFailure = new InvalidOperationException("later postfix failure");
            Check(ReferenceEquals(LuaFinalizer(current, pipelineFailure), pipelineFailure), "Current Lua finalizer did not preserve original exception identity");
            Check(Options.Count == 0 && plugin.optionEntries.Count == 0 && PositionEventCache.Instance.eventDescCache.Count == 0,
                "A later postfix exception retained current source/UI/cache after abort");
            parses = Probe.ParserEntries; analyses = AnalysisCount;
            Tick(plugin, true);
            Check(Probe.ParserEntries == parses && AnalysisCount == analyses && plugin.optionEntries.Single().Description.Contains("게임 데이터 대기 중"),
                "Manual refresh reactivated an aborted compiler pipeline");

            long recover = LuaPrefix(Lua("O_lifecycle_recover"), "lifecycle_recover");
            LuaPostfix(recover, new object()); LuaFinalizer(recover, null); Tick(plugin);
            Check(Options.Contains("O_lifecycle_recover"), "A following verified Lua failed to recover after aborted pipeline");
            long restore = TransitionPrefix("SaveSystem_LoadRestoreSaveData");
            TransitionFinalizer("SaveSystem_LoadRestoreSaveData", null, restore);
            parses = Probe.ParserEntries; analyses = AnalysisCount;
            Tick(plugin); Tick(plugin, true);
            Check(Options.Count == 0 && Probe.ParserEntries == parses && AnalysisCount == analyses && plugin.optionEntries.Single().Description.Contains("게임 데이터 대기 중"),
                "In-place restore without a new Lua resumed stale source");
            Check(OptionInfoPatch.DisplayRefreshDepth == 0, "Lifecycle callbacks leaked display protection");
            return assertions;
        }

        static Mortal.Core.MissionData MissionWithCondition(string name, out Mortal.Core.StatCompareItem compare)
        {
            compare = new Mortal.Core.StatCompareItem();
            var condition = new Mortal.Core.ConditionResultItem(); condition._items.Add(compare);
            return new Mortal.Core.MissionData {
                name = name,
                Stage = new Mortal.Core.MissionCheckData { name = name + "_stage", _conditions = new[] { condition } }
            };
        }

        public static int RunHelpers()
        {
            var plugin = new DicePlugin { showLogWindow = true, currentTab = "미션" }; DicePlugin.Instance = plugin;
            Config.ConfigManager.EnableOptionInfo.Value = true;
            var first = MissionWithCondition("first", out var firstCondition);
            var middle = MissionWithCondition("middle", out var middleCondition);
            var last = MissionWithCondition("last", out var lastCondition);
            var subMissions = new Mortal.Core.SubMissionsData { List = new List<Mortal.Core.MissionData> { first, middle, last } };
            Mortal.Core.MissionManagerData.Instance = new Mortal.Core.MissionManagerData { _subMissions = subMissions };
            Mortal.Story.CheckPointManager.Instance = new Mortal.Story.CheckPointManager();
            firstCondition._value1.Throw = true;
            DataHelper.CurrentValue = 10; UnityEngine.Time.unscaledTime = 0;
            NewLua(Lua("O_actual_helpers"), "actual_helpers"); Tick(plugin);
            Check(plugin.missionEntries.Count == 3, "Actual mission loop lost an item after its first failure");
            Check(plugin.missionEntries[0].Description.Contains("읽기 실패"), "Actual inner ExtractConditions error became a successful empty mission");
            Check(plugin.missionEntries[1].Description.Contains("(10)") && plugin.missionEntries[2].Description.Contains("(10)"),
                "First mission failure blocked later actual helper evaluations");
            Check(first.Reads == 1 && middle.Reads == 1 && last.Reads == 1,
                "Actual GetMissions filter evaluated other mission owners");
            Check(DataHelper.LiveMissionFilter == null && OptionInfoPatch.DisplayRefreshDepth == 0,
                "Actual mission exception leaked filter or display protection");
            firstCondition._value1.Throw = false; middleCondition.Throw = true; DataHelper.CurrentValue = 20;
            Tick(plugin, true);
            Check(plugin.missionEntries[0].Description.Contains("(20)"), "First mission did not recover through its actual helper");
            Check(plugin.missionEntries[1].Description.Contains("(10)") && plugin.missionEntries[1].Description.Contains("갱신 실패 · 이전 값"),
                "Middle mission helper failure discarded its previous normal value");
            Check(plugin.missionEntries[2].Description.Contains("(20)"), "Middle mission failure blocked its later sibling");
            Check(first.Reads == 2 && middle.Reads == 2 && last.Reads == 2,
                "Manual refresh's actual helper filter evaluated a mission more than once");
            Check(DataHelper.LiveMissionFilter == null, "Actual failing mission helper leaked its owner filter");
            middleCondition.Throw = false; Tick(plugin, true);
            Check(plugin.missionEntries.All(row => row.Description.Contains("(20)") && !row.Description.Contains("실패")),
                "Actual mission helper did not replace retained values on recovery");

            var switchConfig = new Mortal.Story.SwitchResultConfig();
            var broken = new Mortal.Core.StatCompareItem(); broken._value1.Throw = true;
            var good = new Mortal.Core.StatCompareItem();
            var brokenConditions = new Mortal.Core.ConditionResultItem(); brokenConditions._items.Add(broken);
            var goodConditions = new Mortal.Core.ConditionResultItem(); goodConditions._items.Add(good);
            switchConfig.Items["switch_one"] = new Mortal.Story.SwitchResultData { _items = new List<Mortal.Core.ConditionResultItem> { brokenConditions } };
            switchConfig.Items["switch_two"] = new Mortal.Story.SwitchResultData { _items = new List<Mortal.Core.ConditionResultItem> { goodConditions } };
            Mortal.Story.CheckPointManager.Instance._switchList = new[] { switchConfig };
            plugin.currentTab = "조건"; UnityEngine.Time.unscaledTime = 1;
            NewLua(Lua("O_actual_switch"), "actual_switch"); Tick(plugin);
            Check(plugin.switchEntries[0].Description.Contains("읽기 실패"), "Actual switch helper swallowed a nested condition error");
            Check(plugin.switchEntries[1].Description.Contains("(20)") && good._value1.Reads > 0,
                "Actual switch helper failure blocked its sibling");
            Check(DicePlugin.Errors.Count == 0, "Protected actual helper logged-and-returned instead of reporting the row failure");

            OptionInfoPatch.DisplayRefreshDepth = 1;
            bool missing = false;
            try { ReflectionExtensions.GetFieldValue<int>(new object(), "missing"); }
            catch (MissingMemberException) { missing = true; }
            finally { OptionInfoPatch.DisplayRefreshDepth = 0; }
            Check(missing, "Actual generic field helper converted a missing preview field to a default value");
            Check(DataHelper.LiveMissionFilter == null && OptionInfoPatch.DisplayRefreshDepth == 0,
                "Actual nested helper failures left shared protection state behind");
            Check(Mortal.Core.StatGroupVariable.OriginalReads == 0 && Mortal.Core.StatCompareItem.OriginalReads == 0,
                "Actual protected helper IL invoked original GetResult/Check instead of pure evaluation");

            plugin.currentTab = "미션";
            Tick(plugin, true);
            int beforeMissingStage = first.Reads;
            first.CurrentKey = "undefined_stage";
            DataHelper.CurrentValue = 30; Tick(plugin, true);
            Check(plugin.missionEntries.Count == 3 && plugin.missionEntries[0].Description.Contains("갱신 실패 · 이전 값") &&
                plugin.missionEntries[1].Description.Contains("(30)") && plugin.missionEntries[2].Description.Contains("(30)"),
                "Unknown current mission stage became normal absence or blocked healthy siblings");
            Check(first.Reads == beforeMissingStage, "Invalid current key reached original GetByKey instead of failing before its log-and-null path");
            first.CurrentKey = ""; Tick(plugin, true);
            Check(plugin.missionEntries[0].Description.Contains("게임 데이터 대기 중"), "Blank current key was treated as a ready mission");
            first.CurrentKey = "clear"; Tick(plugin, true);
            Check(plugin.missionEntries.Count == 2 && plugin.missionEntries.All(row => row.MissionData != first.Stage),
                "Completed mission key was treated as failure instead of successful absence");
            first.CurrentKey = "stage";
            middle.Stage._conditions = null; Tick(plugin, true);
            Check(plugin.missionEntries.Count == 3 && plugin.missionEntries[1].Description.Contains("갱신 실패 · 이전 값") &&
                plugin.missionEntries[0].Description.Contains("(30)") && plugin.missionEntries[2].Description.Contains("(30)"),
                "Null active mission conditions became empty success or blocked sibling recovery");
            middle.Stage._conditions = new[] { new Mortal.Core.ConditionResultItem { _items = new List<Mortal.Core.StatCompareItem> { middleCondition } } };
            Tick(plugin, true);
            Check(plugin.missionEntries.All(row => !row.Description.Contains("실패") && !row.Description.Contains("대기")),
                "Corrected mission stage/condition source did not recover independently");
            var random = new Mortal.Core.StatValueReference { _checkType = 7, _randomMin = 1, _randomMax = 100 };
            var randomGroup = new Mortal.Core.StatGroupVariable { name = "隨機", _value = new List<Mortal.Core.StatValueReference> { random } };
            var constant = new Mortal.Core.StatGroupVariable { name = "常數_50", _value = new List<Mortal.Core.StatValueReference> { new Mortal.Core.StatValueReference { _checkType = 0, _constant = 50 } } };
            var randomCompare = new Mortal.Core.StatCompareItem { _value1 = randomGroup, _value2 = constant, _compareOp = Mortal.Core.StatCompareType.GreaterEqual };
            var randomCondition = new Mortal.Core.ConditionResultItem { _items = new List<Mortal.Core.StatCompareItem> { randomCompare } };
            SeedDisplayRandom(1237);
            int colors = RichText.ConditionColors;
            bool included;
            string randomText = DataHelper.ExtractConditions(randomCondition, out included);
            int sampled = NumericRandom(randomText, 1, 100);
            Check(RichText.ConditionColors == colors + 1 && included, "Random numeric comparison did not receive one verdict color or was hidden");
            Check(RichText.ConditionVerdicts.Last().Passed == (sampled >= 50), "Actual rewritten condition color disagrees with its printed sample");
            randomCondition._items.Add(good);
            randomText = DataHelper.ExtractConditions(randomCondition, out included);
            NumericRandom(randomText, 1, 100);
            Check(RichText.ConditionColors == colors + 3, "Compound numeric conditions did not color each evaluated comparison");
            randomCondition._items.Remove(good);
            var previewEvent = new Mortal.Story.PositionEventData { _toggleCondition = true, _activeCondition = randomCondition,
                _eventRateItem = new[] { new Mortal.Story.PositionEventRateItem { Condition = randomCondition, Rate = 5 } } };
            string eventPreview = (string)typeof(PositionEventCache).GetMethod("GenerateDynamicDesc", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(PositionEventCache.Instance, new object[] { previewEvent });
            sampled = NumericRandom(eventPreview, 1, 100);
            var eventNumbers = Regex.Matches(eventPreview, @"隨機\((-?\d+)\)");
            Check(eventNumbers.Count == 2 && eventNumbers[0].Groups[1].Value == eventNumbers[1].Groups[1].Value,
                "Actual event formatter did not share one sample across active and rate-item descriptions");
            Check(eventPreview.Split('\n')[0].Contains("발동 확률：" + (sampled >= 50 ? "15" : "0") + "(10)"),
                "Actual event rate header disagrees with its printed active and rate-item samples");
            Check(RichText.ConditionVerdicts.Skip(RichText.ConditionVerdicts.Count - 2).All(result => result.Passed == (sampled >= 50)),
                "Actual event active and rate-item colors disagree with the shared displayed sample");
            var inactiveCondition = new Mortal.Core.ConditionResultItem { _items = new List<Mortal.Core.StatCompareItem> {
                new Mortal.Core.StatCompareItem { _value1 = constant, _value2 = randomCompare._value2, _compareOp = Mortal.Core.StatCompareType.NotEqual }
            } };
            var inactiveEvent = new Mortal.Story.PositionEventData { _toggleCondition = true, _activeCondition = inactiveCondition, _eventRateItem = null };
            eventPreview = (string)typeof(PositionEventCache).GetMethod("GenerateDynamicDesc", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(PositionEventCache.Instance, new object[] { inactiveEvent });
            Check(eventPreview.Contains("발동 확률：0"), "Inactive event's unreachable null rate list became a new description failure");
            var narrativeFailure = new Mortal.Core.ConditionResultItem { _items = null };
            int narrativeWarnings = DicePlugin.Warnings.Count;
            string narrativeText = DataHelper.ExtractConditions(narrativeFailure, out included);
            Check(narrativeText == "읽기 실패" && included, "Narrative formatter error aborted gameplay or became empty/false");
            DataHelper.ExtractConditions(narrativeFailure, out included);
            Check(DicePlugin.Warnings.Count == narrativeWarnings + 1, "Repeated narrative description failure flooded diagnostics");
            narrativeFailure._items = new List<Mortal.Core.StatCompareItem>();
            DataHelper.ExtractConditions(narrativeFailure, out included);
            DataHelper.ExtractConditions(narrativeFailure, out included);
            Check(DicePlugin.Warnings.Count == narrativeWarnings + 2, "Narrative description recovery was missing or duplicated");
            var badEvent = new Mortal.Story.PositionEventData { _toggleCondition = true, _activeCondition = new Mortal.Core.ConditionResultItem { _items = null } };
            var eventBoundary = typeof(PositionEventCache).GetMethod("GeneratePreviewDynamicDesc", BindingFlags.NonPublic | BindingFlags.Instance);
            int eventWarnings = DicePlugin.Warnings.Count;
            string narrativeEvent = (string)eventBoundary.Invoke(PositionEventCache.Instance, new object[] { badEvent });
            Check(narrativeEvent == "읽기 실패" && OptionInfoPatch.DisplayRefreshDepth == 0, "Narrative event description error escaped its incoming-depth boundary");
            eventBoundary.Invoke(PositionEventCache.Instance, new object[] { badEvent });
            Check(DicePlugin.Warnings.Count == eventWarnings + 1, "Narrative event error flooded diagnostics");
            bool protectedEventFailed = false;
            OptionInfoPatch.TargetEvent = previewEvent;
            OptionInfoPatch.DisplayRefreshDepth = 1;
            try { eventBoundary.Invoke(PositionEventCache.Instance, new object[] { badEvent }); }
            catch (TargetInvocationException failure) { protectedEventFailed = failure.InnerException is InvalidOperationException; }
            Check(protectedEventFailed && OptionInfoPatch.DisplayRefreshDepth == 1 && ReferenceEquals(OptionInfoPatch.TargetEvent, previewEvent),
                "Protected event error was swallowed, leaked depth, or consumed execution request");
            OptionInfoPatch.DisplayRefreshDepth = 0;
            badEvent._activeCondition = new Mortal.Core.ConditionResultItem { _items = new List<Mortal.Core.StatCompareItem> { good } };
            eventBoundary.Invoke(PositionEventCache.Instance, new object[] { badEvent });
            eventBoundary.Invoke(PositionEventCache.Instance, new object[] { badEvent });
            Check(DicePlugin.Warnings.Count == eventWarnings + 2, "Narrative event recovery was missing or duplicated");
            first.Stage._conditions = new[] { randomCondition };
            plugin.currentTab = "미션"; Tick(plugin, true);
            int warnings = DicePlugin.Warnings.Count;
            int previous = NumericRandom(plugin.missionEntries[0].Description, 1, 100);
            string previousDescription = plugin.missionEntries[0].Description;
            random._randomMin = random._randomMax = previous + 100;
            UnityEngine.Time.unscaledTime += 0.24f; Tick(plugin);
            Check(plugin.missionEntries[0].Description == previousDescription, "Normal random row refreshed before the 0.25 second cadence");
            UnityEngine.Time.unscaledTime += 0.02f; Tick(plugin);
            Check(NumericRandom(plugin.missionEntries[0].Description, previous + 100, previous + 100) == previous + 100,
                "Normal random row did not refresh after the 0.25 second cadence");
            random._randomMin = 5; random._randomMax = 3;
            UnityEngine.Time.unscaledTime += 0.3f; Tick(plugin);
            NumericRandom(plugin.missionEntries[0].Description, 3, 5);
            random._randomMin = 1; random._randomMax = 10;
            UnityEngine.Time.unscaledTime += 0.3f; Tick(plugin);
            NumericRandom(plugin.missionEntries[0].Description, 1, 10);
            Check(DicePlugin.Warnings.Count == warnings, "Supported numeric random refresh generated failure/recovery diagnostics");
            Check(UnityEngine.Random.Calls == 0 && Mortal.Core.StatGroupVariable.OriginalReads == 0 && Mortal.Core.StatCompareItem.OriginalReads == 0,
                "Numeric helper/mission refresh reached Unity RNG or original game evaluation methods");
            NewLua(Lua("O_random_reset"), "random_reset");
            Check(plugin.missionEntries.Count == 0, "New Lua retained old random preview rows");
            return assertions;
        }
    }
}
