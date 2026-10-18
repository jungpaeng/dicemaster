using System;

// Only Unity, loader and input boundaries. The actual mod's translation and
// dice methods (and their closures/resources) are copied by the test runner.
namespace UnityEngine
{
    public class Object
    {
        public static Object Found;
        public static bool operator ==(Object a, Object b) => ReferenceEquals(a, b);
        public static bool operator !=(Object a, Object b) => !ReferenceEquals(a, b);
        public static implicit operator bool(Object value) => value != null;
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => base.GetHashCode();
        public static T FindObjectOfType<T>() where T : Object => (T)Found;
        public static T Instantiate<T>(T value, Transform parent, bool worldPositionStays) where T : Object => value;
    }
    public class Transform : Object { public Vector3 localPosition { get; set; } }
    public struct Vector3 { public Vector3(float x, float y, float z) { } }
    public struct Rect { public Rect(float x, float y, float width, float height) { } }
    public class GameObject : Object
    {
        public bool activeInHierarchy = true;
        public T GetComponentInChildren<T>() where T : new() => new T();
    }
    public class Component : Object
    {
        public GameObject gameObject = new GameObject();
        public Transform transform = new Transform();
        public T GetComponentInChildren<T>() where T : new() => new T();
    }
    public class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public bool wordWrap { get; set; }
        public float fixedHeight { get; set; }
        public int fontSize { get; set; }
    }
    public class GUILayoutOption { }
    public static class GUILayout { public static void Label(string text, GUIStyle style, GUILayoutOption[] options) { } }
    public class GUISkin { public GUIStyle label { get; set; } = new GUIStyle(); }
    public static class GUI
    {
        public static GUISkin skin { get; set; } = new GUISkin();
        public static void Label(Rect rect, string text, GUIStyle style) { }
    }
    public static class Screen { public static int width { get; set; } = 1920; public static int height { get; set; } = 1080; }
    public static class Time { public static float unscaledTime { get; set; } }
    public static class Application { public static bool isFocused { get; set; } = true; }
}
namespace UnityEngine.Events
{
    public delegate void UnityAction<T>(T value);
    public class UnityEvent<T> { public void AddListener(UnityAction<T> listener) { } }
}
namespace UnityEngine.UI
{
    public class InputField : UnityEngine.Component
    {
        public class OnChangeEvent : UnityEngine.Events.UnityEvent<string> { }
        public OnChangeEvent onValueChanged { get; set; } = new OnChangeEvent();
        public string text { get; set; }
    }
}
namespace Mortal.Story
{
    public class DiceMenuDialog : UnityEngine.Component { public int _currentRandomValue; }
}
namespace Fungus
{
    // Capture the game-owned narrative sink; its text is produced by copied mod IL.
    public class NarrativeLogEntry { public string name; public string text; }
    public class NarrativeLog : UnityEngine.Object
    {
        public readonly System.Collections.Generic.List<NarrativeLogEntry> Entries = new System.Collections.Generic.List<NarrativeLogEntry>();
        public void AddLine(NarrativeLogEntry entry) => Entries.Add(entry);
    }
    public class FungusManager : UnityEngine.Object
    {
        public static FungusManager Instance { get; set; } = new FungusManager();
        public NarrativeLog NarrativeLog { get; set; } = new NarrativeLog();
    }
}
namespace BepInEx
{
    public static class Paths
    {
        public static string CachePath { get; set; } = "/tmp";
        // UiTextMapper's optional game-table boundary must not read the host game.
        public static string GameRootPath { get; set; } = "/__dicemaster_source_behavior_no_game_tables__";
    }
}
namespace DiceMaster
{
    public static class OptionInfoPatch
    {
        public static int DisplayRefreshDepth;
        // Locale lookup is a game boundary; no locale tables are supplied here.
        private static string GetLocalizedSystemText(string key, string defaultValue) => defaultValue;
    }
    // The mapper's optional dynamic flag-name lookup remains a game boundary.
    public static class DataHelper { public static string GetFlagName(string id) => id; }
    public class DicePlugin
    {
        public static DicePlugin Instance { get; set; } = new DicePlugin();
        public UnityEngine.GameObject DictInputPrefab = new UnityEngine.GameObject();
        public static void LogWarning(object message) { }
    }
    public static class ReflectionExtensions
    {
        public static void SetPrivateField(this object instance, string fieldname, object value) =>
            instance.GetType().GetField(fieldname).SetValue(instance, value);
    }
}
namespace DiceMaster.Config
{
    public class ConfigElement<T> { public T Value { get; set; } }
    public static class ConfigManager
    {
        public static ConfigElement<bool> EnableDiceController = new ConfigElement<bool> { Value = true };
        public static ConfigElement<bool> EnableGamepadAdjustment = new ConfigElement<bool> { Value = true };
    }
}
namespace DiceMaster.Gamepad
{
    public static class GamepadBridge
    {
        public static int Resets;
        public static void ResetAdjustment() { Resets++; }
        public static bool ShowHint => false;
        public static string Hint() => "";
        public static int Tick(bool enabled, bool windowVisible, int current, float time, string cacheRoot) => current;
        public static string ConsumeWarning() => null;
    }
}
