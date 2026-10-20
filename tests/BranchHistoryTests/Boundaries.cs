using System.Reflection;
using DiceMaster.LiveLog;

// Explicit game/Unity/display-helper substitutes. No native runtime executes.
namespace UnityEngine { public static class Time { public static float unscaledTime; } }
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)] public sealed class HarmonyPatch : Attribute { public HarmonyPatch() { } public HarmonyPatch(Type type, string name) { } }
    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyPrefix : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyFinalizer : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyTargetMethods : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyPriority : Attribute { public HarmonyPriority(int value) { } }
    public static class Priority { public const int First = 800, Last = 0; }
    public sealed class Harmony { public readonly List<Type> Installed = new(); public void PatchAll(Type type) { Installed.Add(type); } }
    public static class AccessTools { public static MethodInfo Method(Type type, string name, Type[] args) { return type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, args, null); } }
}
namespace Mortal.Story
{
    public sealed class PositionEventData { public string ResultType; public string name; }
    public sealed class PositionResultData { public string _scriptPrefix; public List<PositionEventData> List = new(); public string GetResult() { throw new Exception("display invoked game result"); } }
    public sealed class SwitchResultData { public bool _useCount; public int _count; public List<object> _items = new(), _countItems = new(); public int Execute() { throw new Exception("display executed switch"); } }
    public sealed class CheckPointManager { public string Position(string name) { throw new Exception("display executed position"); } public int Switch(string name) { throw new Exception("display executed checkpoint"); } }
    public sealed class TestCondition { public int _compareOp, _value; }
}
namespace Mortal.Core
{
    public sealed class SaveSystem
    {
        public void SaveGameData() { } public void SaveGameData(string slot) { } public void AutoSaveData(string name) { }
        public void AutoSaveStoryData() { } public void AutoSaveFreeData() { } public void AutoSaveBattleData() { }
        public void BackupLibraryData() { } public void UpdateRestoreData() { } public void SaveLegendData(int slot, string key) { } public void SaveUniverseData() { } public void SaveCurrentSlotData(string slot) { }
        private void ExecuteSaveData(string path, object data) { } private void ExecuteSaveJsonData(string path, object data) { }
        public void LoadUniverseData() { } public void NewGameData() { } public void NewActGameData() { } public void LoadGameData() { }
        public void AutoLoadGameData(string name) { } public void LoadRestoreSaveData() { } private void ExecuteLoadGameData(object data) { }
        public object CreateSaveData() { throw new Exception("display saved game"); }
    }
    public sealed class SceneController { private System.Collections.IEnumerator StartLoadScene(string name, bool unload) { return null; } }
}
namespace DiceMaster
{
    public static class DicePlugin { public static readonly List<string> Logs = new(); public static void LogInfo(string text) { Logs.Add(text); } }
    internal static class OptionInfoPatch { internal static string _lastLuaString, _lastFriendlyName; internal static int DisplayRefreshDepth; }
    public static class DataHelper
    {
        public static int NameReads, ScriptReads;
        public static string GetEventName(Mortal.Story.PositionEventData value) { NameReads++; return "번역 " + value.name; }
        public static ScriptReadResult ReadPreviewScript(string name) { ScriptReads++; return new ScriptReadResult(); }
    }
}
namespace DiceMaster.EventSelection
{
    public static class EventSelectionRuntime { private static object current; public static void Set(object value) { current = value; } }
    public static class PopupConditionText
    {
        public static int Reads;
        public static bool Fail;
        public static string Event(object value, int? rate, out string summary) { Reads++; if (Fail) throw new InvalidOperationException("fixture failure"); return summary = "이벤트 조건"; }
        public static string Condition(object value) { Reads++; if (Fail) throw new InvalidOperationException("fixture failure"); return "분기 조건"; }
    }
    public sealed class EventResultPreview
    {
        public static int Reads; public static bool RetryNext;
        public string Detail; public bool Retry;
        public static EventResultPreview Read(string result) { Reads++; return new EventResultPreview { Detail = "정적 효과 " + result, Retry = RetryNext }; }
    }
}
namespace DiceMaster.LiveLog
{
    public sealed class ScriptReadResult { }
    public sealed class PreviewAnalysis { public bool HasReadFailures, SnapshotAllowed; }
    public static class PreviewAnalyzer
    {
        public static int Analyses, Retries;
        public static bool Fail;
        public static PreviewAnalysis AnalyzeWithCache(string name, string body, Func<string, ScriptReadResult> reader, object limits, PreviewParseCache cache) { Analyses++; return new PreviewAnalysis { HasReadFailures = Fail }; }
        public static void RetryReads(PreviewAnalysis analysis) { Retries++; analysis.HasReadFailures = Fail; }
    }
    public static class PreviewDisplay { public static string Describe(PreviewAnalysis analysis) { return "선택 분기 정적 효과"; } }
    public sealed class PreviewParseCache
    {
        public const int DefaultMaxCharacters = 2097152;
        private readonly Dictionary<string, List<PreviewToken>> cache = new();
        public int Tokenizations;
        public List<PreviewToken> Tokens(string source) { if (!cache.TryGetValue(source, out var result)) { Tokenizations++; cache.Add(source, result = PreviewSyntax.Tokenize(source)); } return result; }
    }
    public static class LogBridge
    {
        public sealed class Parser { internal readonly PreviewParseCache previewParseCache = new(); }
        public static readonly Parser Instance = new();
        public static object GetEventParser() { return Instance; }
        public static object ReadFieldStrict(object value, string name)
        {
            if (value == null) throw new DataNotReadyException();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            return value.GetType().GetField(name, flags)?.GetValue(value) ?? throw new MissingFieldException(name);
        }
    }
}
