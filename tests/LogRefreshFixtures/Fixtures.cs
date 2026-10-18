using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Game/Unity boundaries only. The test runner replaces marked method bodies
// with the actual IL read from the patched DiceMaster DLL before execution.
namespace UnityEngine
{
    public class Object
    {
        public static bool operator ==(Object a, Object b) => ReferenceEquals(a, b);
        public static bool operator !=(Object a, Object b) => !ReferenceEquals(a, b);
        public override bool Equals(object value) => ReferenceEquals(this, value);
        public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
    }
    public static class Time { public static float unscaledTime { get; set; } }
    public enum KeyCode { Tab = 9 }
    public struct Vector2Int
    {
        public int x { get; set; }
        public int y { get; set; }
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
    }
    public static class Screen
    {
        public static int width { get; set; } = 1920;
        public static int height { get; set; } = 1080;
    }
    public static class Input
    {
        public static bool NextKeyDown;
        public static bool GetKeyDown(KeyCode key) { bool value = NextKeyDown; NextKeyDown = false; return value; }
    }
}
namespace Mortal.Story
{
    public class PositionEventData : UnityEngine.Object
    {
        public int State;
        public int GetRate()
        {
            int result = State * 10;
            DiceMaster.OptionInfoPatch.PositionEventData_GetRate_Postfix(this, ref result);
            return result;
        }
    }
}
namespace Mortal.Core { public class MissionCheckData : UnityEngine.Object { } }
namespace DiceMaster.Config
{
    public class ConfigElement<T> { public T Value { get; set; } }
    public static class ConfigManager
    {
        public static ConfigElement<bool> EnableOptionInfo = new ConfigElement<bool> { Value = true };
        public static ConfigElement<UnityEngine.KeyCode> LogMenuToggleKey = new ConfigElement<UnityEngine.KeyCode> { Value = UnityEngine.KeyCode.Tab };
    }
}
namespace DiceMaster
{
    public class DicePlugin
    {
        public List<(string, string, string)> optionEntries = new List<(string, string, string)>();
        public List<(string, string)> switchEntries = new List<(string, string)>();
        public List<(Mortal.Story.PositionEventData, string, string)> eventEntries = new List<(Mortal.Story.PositionEventData, string, string)>();
        public List<(Mortal.Core.MissionCheckData, string, string)> missionEntries = new List<(Mortal.Core.MissionCheckData, string, string)>();
        public bool showLogWindow;
        public float nextLiveLogRefresh;
        public UnityEngine.Vector2Int lastScreenSize;
        public static int Errors;
        public static void LogError(object error) { Errors++; }
        public void RefreshLiveLogDisplay() => throw new Exception("IL not installed");
        public void RefreshGameData() => throw new Exception("IL not installed");
        public void ClearLogEntries() => throw new Exception("IL not installed");
        public void Update() => throw new Exception("IL not installed");
        public void OnScreenSizeChanged(float width, float height) { lastScreenSize = new UnityEngine.Vector2Int((int)width, (int)height); }
    }
    public static class DicePatch { public static int Ticks; public static void UpdateGamepad() { Ticks++; } }
    public static class OptionInfoPatch
    {
        public static int DisplayRefreshDepth;
        public static Mortal.Story.PositionEventData TargetEvent;
        public static Mortal.Core.MissionCheckData TargetMission;
        public static void PositionEventData_GetRate_Postfix(Mortal.Story.PositionEventData value, ref int result) => throw new Exception("IL not installed");
        public static void MissionCheckData_Postfix(Mortal.Core.MissionCheckData value, ref bool result) => throw new Exception("IL not installed");
    }
    public class PositionEventCache
    {
        public static PositionEventCache Instance = new PositionEventCache();
        public Dictionary<string, List<Mortal.Story.PositionEventData>> positionEventDataCache = new Dictionary<string, List<Mortal.Story.PositionEventData>>();
        public Dictionary<Mortal.Story.PositionEventData, string> eventKeyCache = new Dictionary<Mortal.Story.PositionEventData, string>();
        public Dictionary<Mortal.Story.PositionEventData, string> eventDescCache = new Dictionary<Mortal.Story.PositionEventData, string>();
        public Dictionary<Mortal.Story.PositionEventData, string> scriptPrefixCache = new Dictionary<Mortal.Story.PositionEventData, string>();
        public static bool Ready, ThrowDynamic;
        public static int DynamicCalls, StaticCalls, LoadCalls;
        public static Mortal.Story.PositionEventData Event = new Mortal.Story.PositionEventData();
        public static PositionEventCache get_Instance() => Instance;
        public List<Mortal.Story.PositionEventData> LoadPositionEvents(string position)
        {
            LoadCalls++;
            return Ready ? new List<Mortal.Story.PositionEventData> { Event } : new List<Mortal.Story.PositionEventData>();
        }
        public string GenerateDynamicDesc(Mortal.Story.PositionEventData value)
        {
            DynamicCalls++;
            if (ThrowDynamic) throw new InvalidOperationException("simulated game evaluation failure");
            return "G3000(" + value.State + ") " + (value.State >= 2 ? "green" : "red") + " rate=" + value.GetRate();
        }
        public string GenerateStaticDesc(Mortal.Story.PositionEventData value) { StaticCalls++; return "fixed result"; }
        public string GeneratePreviewDynamicDesc(Mortal.Story.PositionEventData value) => throw new Exception("IL not installed");
        public string GetPositionEventDesc(Mortal.Story.PositionEventData value) => throw new Exception("IL not installed");
        public List<Mortal.Story.PositionEventData> GetPositionEvents(string position) => throw new Exception("IL not installed");
        public void ClearDisplayCaches() => throw new Exception("IL not installed");
    }
    public static class DataHelper
    {
        public static string GetFlagName(string id) => PositionEventCache.Event.State >= 2 ? "완료" : "미완료";
    }
    namespace EmbeddedTranslations
    {
        public static class Display
        {
            public static string PreTranslate(string value) => value;
            public static string PostTranslate(string value) => value;
        }
    }
    public static class UiTextMapper
    {
        public static object translationCacheLock = new object();
        public static Dictionary<string, string> translationCache = new Dictionary<string, string>();
        public static Dictionary<string, string> exactReplacements = new Dictionary<string, string>();
        public static List<KeyValuePair<string, string>> orderedReplacements = new List<KeyValuePair<string, string>>();
        public static List<Tuple<Regex, MatchEvaluator>> regexRules = new List<Tuple<Regex, MatchEvaluator>> {
            Tuple.Create(new Regex("(?:ext\\.)?GetFlag\\(\\s*['\"]([^'\"]+)['\"]\\s*\\)"), new MatchEvaluator(ConvertGetFlagExpression))
        };
        public static string ConvertGetFlagExpression(Match match) => throw new Exception("IL not installed");
        public static string ApplyTranslations(string value) => throw new Exception("IL not installed");
        public static bool TryGetCachedTranslation(string key, out string value) { value = null; throw new Exception("IL not installed"); }
        public static void CacheTranslation(string key, string value) => throw new Exception("IL not installed");
    }

    public static class Scenario
    {
        static int assertions;
        static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException(message);
        }
        public static int Run()
        {
            var cache = PositionEventCache.Instance; var value = PositionEventCache.Event;
            Check(cache.GetPositionEventDesc(null) == "", "Null event handling changed");
            value.State = 0;
            Check(cache.GetPositionEventDesc(value).Contains("G3000(0) red"), "Initial event value/color");
            value.State = 2;
            Check(cache.GetPositionEventDesc(value).Contains("G3000(2) green"), "Changed event value/color stale");
            Check(PositionEventCache.StaticCalls == 1 && PositionEventCache.DynamicCalls == 2, "Only fixed description should be cached");
            Check(OptionInfoPatch.DisplayRefreshDepth == 0, "Preview depth leaked");

            OptionInfoPatch.TargetEvent = value;
            Check(cache.GetPositionEventDesc(value).Contains("rate=20"), "Preview used forced event rate");
            Check(ReferenceEquals(OptionInfoPatch.TargetEvent, value), "Preview consumed event request");
            Check(value.GetRate() == 99999 && OptionInfoPatch.TargetEvent == null, "Normal event execution no longer consumes request");
            Check(value.GetRate() == 20, "Forced rate was not one-shot");
            var mission = new Mortal.Core.MissionCheckData(); OptionInfoPatch.TargetMission = mission;
            bool passed = false; OptionInfoPatch.DisplayRefreshDepth = 1;
            OptionInfoPatch.MissionCheckData_Postfix(mission, ref passed);
            Check(!passed && ReferenceEquals(OptionInfoPatch.TargetMission, mission), "Preview consumed mission request");
            OptionInfoPatch.DisplayRefreshDepth = 0; OptionInfoPatch.MissionCheckData_Postfix(mission, ref passed);
            Check(passed && OptionInfoPatch.TargetMission == null, "Normal mission execution changed");

            PositionEventCache.Ready = false;
            Check(cache.GetPositionEvents("late").Count == 0, "Unexpected events before game data ready");
            PositionEventCache.Ready = true;
            Check(cache.GetPositionEvents("late").Count == 1, "Empty event list was cached permanently");
            int loads = PositionEventCache.LoadCalls; cache.GetPositionEvents("late");
            Check(PositionEventCache.LoadCalls == loads, "Populated definitions should remain cached");
            cache.positionEventDataCache["old-empty"] = new List<Mortal.Story.PositionEventData>();
            Check(cache.GetPositionEvents("old-empty").Count == 1, "Existing empty cache entry not retried");

            value.State = 0;
            Check(UiTextMapper.ApplyTranslations("GetFlag('G3000')").Contains("미완료"), "Initial dynamic flag label");
            value.State = 2;
            string translated = UiTextMapper.ApplyTranslations("GetFlag('G3000')");
            Check(translated.StartsWith("완료"), "State-dependent translation stayed cached");
            UiTextMapper.translationCache["GetFlag('G3000')"] = "stale";
            Check(UiTextMapper.ApplyTranslations("GetFlag('G3000')").StartsWith("완료"), "Old state-dependent cache entry was reused");
            UiTextMapper.CacheTranslation("plain text", "translated");
            Check(UiTextMapper.TryGetCachedTranslation("plain text", out string plain) && plain == "translated", "Pure translation cache disabled");

            return assertions;
        }
    }
}
