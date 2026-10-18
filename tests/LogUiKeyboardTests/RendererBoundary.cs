// Explicit managed substitutes for Unity/native/config/game boundaries. Tests
// compile and execute the actual renderer, model and keyboard sources; these
// substitutes cannot validate Unity layout, font rendering or XInput blocking.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public enum KeyCode { None, Tab, UpArrow, DownArrow, LeftArrow, RightArrow, Return, KeypadEnter, Space, Escape, Y, X }
    public enum EventType { Layout, Repaint, MouseDown, MouseUp, MouseMove, ScrollWheel, KeyDown, KeyUp, Used }
    public enum FontStyle { Normal, Bold }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } }
    public struct Rect : IEquatable<Rect>
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
        public float yMax => y + height;
        public bool Contains(Vector2 value) => value.x >= x && value.x <= x + width && value.y >= y && value.y <= yMax;
        public bool Equals(Rect value) => x == value.x && y == value.y && width == value.width && height == value.height;
        public override bool Equals(object value) => value is Rect rect && Equals(rect);
        public override int GetHashCode() => x.GetHashCode();
        public static bool operator ==(Rect first, Rect second) => first.Equals(second);
        public static bool operator !=(Rect first, Rect second) => !first.Equals(second);
    }
    public struct Color { public Color(float r, float g, float b) { } public static Color white => new Color(1, 1, 1); }
    public sealed class RectOffset { public RectOffset(int a, int b, int c, int d) { } }
    public sealed class GUIStyleState { public Color textColor; }
    public sealed class GUIStyle
    {
        public int fontSize; public bool richText, wordWrap; public RectOffset padding; public FontStyle fontStyle;
        public GUIStyleState normal = new GUIStyleState();
        public GUIStyle() { } public GUIStyle(GUIStyle source) { }
    }
    public sealed class GUISkin { public GUIStyle label = new GUIStyle(), button = new GUIStyle(), textField = new GUIStyle(), box = new GUIStyle(), window = new GUIStyle(); }
    public static class GUI
    {
        public static GUISkin skin = new GUISkin(); public static bool enabled = true;
        public static void FocusControl(string name) { } public static void SetNextControlName(string name) { } public static void DragWindow(Rect rect) { }
    }
    public sealed class GUILayoutOption { }
    public static class GUILayout
    {
        public static readonly List<string> Buttons = new List<string>(), LiteralLabels = new List<string>();
        public static Rect LastRect = new Rect(0, 0, 200, 30);
        public static float CardBodyHeight = 30;
        public static Rect Window(int id, Rect rect, Action<int> draw, string title, GUIStyle style, params GUILayoutOption[] options) { draw(id); return rect; }
        public static void BeginHorizontal(params GUILayoutOption[] options) { } public static void EndHorizontal() { }
        public static void BeginVertical(params GUILayoutOption[] options) { } public static void BeginVertical(GUIStyle style, params GUILayoutOption[] options) { } public static void EndVertical() { LastRect = new Rect(0, 0, 200, CardBodyHeight); }
        public static Vector2 BeginScrollView(Vector2 scroll, params GUILayoutOption[] options) => scroll;
        public static void EndScrollView() { }
        public static bool Button(string text, GUIStyle style, params GUILayoutOption[] options) { Buttons.Add(text); LastRect = new Rect(0, 0, 200, 30); return false; }
        public static string TextField(string text, int maxLength, GUIStyle style, params GUILayoutOption[] options) => text;
        public static void Label(string text, GUIStyle style, params GUILayoutOption[] options) { LiteralLabels.Add(text); } public static void FlexibleSpace() { }
        public static GUILayoutOption Width(float value) => new GUILayoutOption(); public static GUILayoutOption Height(float value) => new GUILayoutOption();
        public static GUILayoutOption MinWidth(float value) => new GUILayoutOption(); public static GUILayoutOption MinHeight(float value) => new GUILayoutOption();
        public static GUILayoutOption ExpandWidth(bool value) => new GUILayoutOption(); public static GUILayoutOption ExpandHeight(bool value) => new GUILayoutOption();
    }
    public static class GUILayoutUtility { public static Rect GetLastRect() => GUILayout.LastRect; }
    public static class Screen { public static int width = 1920, height = 1080; }
    public static class Time { public static float unscaledDeltaTime = .016f, unscaledTime = 1; }
    public static class Mathf
    {
        public static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
    public static class Input { public static readonly HashSet<KeyCode> Held = new HashSet<KeyCode>(); public static bool GetKey(KeyCode key) => Held.Contains(key); }
    public sealed class Event
    {
        public static Event current = new Event(); public EventType type = EventType.Layout, rawType = EventType.Layout;
        public KeyCode keyCode; public int button; public Vector2 mousePosition; public void Use() { type = EventType.Used; }
    }
}
namespace DiceMaster.Config
{
    public sealed class Element<T> { public T Value; public Element(T value) { Value = value; } }
    public static class ConfigManager
    {
        public static Element<float> LogWindowX = new Element<float>(20), LogWindowY = new Element<float>(20), LogWindowWidth = new Element<float>(750), LogWindowHeight = new Element<float>(750);
        public static Element<int> LogFontSize = new Element<int>(14);
        public static Element<bool> EnableOptionInfo = new Element<bool>(true);
        public static Element<UnityEngine.KeyCode> LogMenuToggleKey = new Element<UnityEngine.KeyCode>(UnityEngine.KeyCode.Tab);
    }
}
namespace DiceMaster.EmbeddedTranslations
{
    public static class Display
    {
        public static readonly List<string> TranslatedLabels = new List<string>();
        public static void Label(string text, UnityEngine.GUIStyle style, UnityEngine.GUILayoutOption[] options) { TranslatedLabels.Add(text); }
    }
}
namespace DiceMaster.Gamepad
{
    public enum LogPadCommand { None, Up, Down, Left, Right, Activate, Back, Tools, Refresh, PreviousTab, NextTab }
    public static class GamepadBridge
    {
        public static int Cancellations;
        public static void CancelLogInput() { Cancellations++; }
        public static void NotifyPointerActivity() { }
    }
}
namespace DiceMaster.LiveLog
{
    public static class LogBridge
    {
        public static int Reservations;
        public static object LastTarget;
        public static bool IsReserved(string tab, object target) => target != null && ReferenceEquals(target, LastTarget);
        public static bool TryReserve(long context, string tab, string id, object target) { Reservations++; LastTarget = target; return true; }
    }
}
