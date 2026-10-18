// Explicit managed substitutes for Unity/native/config/game boundaries. Tests
// compile and execute the actual renderer, model and controller sources; these
// substitutes cannot validate Unity layout, font rendering or XInput blocking.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public enum KeyCode { None, Tab, UpArrow, DownArrow, LeftArrow, RightArrow, Return, KeypadEnter, Space, Escape, Y, X }
    public enum EventType { Layout, Repaint, MouseDown, MouseUp, MouseMove, MouseDrag, ScrollWheel, KeyDown, KeyUp, Used, TouchDown, TouchUp, TouchMove, TouchEnter, TouchLeave, TouchStationary, Ignore }
    public enum CursorLockMode { None, Locked, Confined }
    public static class Cursor
    {
        public static CursorLockMode lockState;
        public static bool visible;
    }
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
    public sealed class RectOffset
    {
        public int left, right, top, bottom;
        public int horizontal => left + right;
        public int vertical => top + bottom;
        public RectOffset(int a, int b, int c, int d) { left = a; right = b; top = c; bottom = d; }
    }
    public sealed class GUIContent
    {
        public string text;
        public GUIContent(string value) { text = value; }
        public static GUIContent none = new GUIContent("");
    }
    public sealed class GUIStyleState { public Color textColor; }
    public sealed class GUIStyle
    {
        public int fontSize; public bool richText, wordWrap; public RectOffset padding, margin; public FontStyle fontStyle;
        public float fixedWidth, fixedHeight;
        public GUIStyleState normal = new GUIStyleState();
        public static GUIStyle none = new GUIStyle();
        public GUIStyle() { }
        public GUIStyle(GUIStyle source)
        {
            fontSize = source.fontSize; richText = source.richText; wordWrap = source.wordWrap;
            padding = source.padding; margin = source.margin; fontStyle = source.fontStyle;
            fixedWidth = source.fixedWidth; fixedHeight = source.fixedHeight;
        }
        public float CalcHeight(GUIContent content, float width)
        {
            float font = Math.Max(10, fontSize);
            float available = Math.Max(font, width - (padding?.horizontal ?? 0));
            int lines = 0;
            foreach (string line in (content.text ?? "").Split('\n'))
                lines += wordWrap ? Math.Max(1, (int)Math.Ceiling(line.Length * font * (fontStyle == FontStyle.Bold ? .78f : .72f) / available)) : 1;
            return lines * (font + 4) + (padding?.vertical ?? 0);
        }
        public Vector2 CalcSize(GUIContent content) => new Vector2((content.text?.Length ?? 0) * Math.Max(10, fontSize) * .72f, CalcHeight(content, float.MaxValue));
    }
    public sealed class GUISkin { public GUIStyle label = new GUIStyle(), button = new GUIStyle(), box = new GUIStyle(), window = new GUIStyle(), verticalScrollbar = new GUIStyle { fixedWidth = 18, margin = new RectOffset(0, 0, 0, 0) }; }
    public static class GUI
    {
        public static GUISkin skin = new GUISkin(); public static bool enabled = true;
        public static void FocusControl(string name) { } public static void DragWindow(Rect rect) { }
        public static Rect Window(int id, Rect rect, Action<int> draw, string title, GUIStyle style)
            => GUILayout.InvokeWindow(id, rect, draw, style, false, Array.Empty<GUILayoutOption>());
    }
    public sealed class GUILayoutOption
    {
        internal string Name; internal float Value;
        internal GUILayoutOption(string name, float value) { Name = name; Value = value; }
    }
    public static class GUILayout
    {
        // This is an instrumented layout boundary, not a Unity replacement. It
        // records the renderer's explicit viewport contract and assigns distinct
        // content-local positions. No native layout/font correctness is claimed.
        public sealed class ScrollViewRecord
        {
            public Rect Viewport; public Vector2 InputScroll, ReturnedScroll; public float ContentHeight, ContentWidth;
            public bool AlwaysShowHorizontal, AlwaysShowVertical, ReservedVerticalScrollbar; public GUIStyle HorizontalScrollbar;
            internal (int Window, int Index) CacheKey;
            public readonly List<ControlRecord> Controls = new List<ControlRecord>();
        }
        public sealed class ControlRecord
        {
            public string Text; public Rect Rect; public int FontSize; public bool Button, Enabled;
            public Rect WindowLocalRect, AncestorClip, VisibleRect;
        }
        // Observe actual styled vertical groups so branch blocks cannot be
        // replaced by one concatenated label while satisfying layout checks.
        public sealed class BoxRecord
        {
            public Rect Rect; public GUIStyle Style;
            public readonly List<ControlRecord> Controls = new List<ControlRecord>();
        }
        public sealed class WindowRecord
        {
            public int Id, RootEntryCount;
            public bool Automatic;
            public Rect RequestedRect, NativeRect;
            public readonly List<Rect> FreeAreas = new List<Rect>();
        }
        private sealed class Group
        {
            internal Rect Bounds; internal bool Horizontal, Scroll, Area;
            internal float Cursor, Cross, FixedHeight; internal RectOffset Padding;
            internal ScrollViewRecord Record;
            internal GUIStyle Style; internal int ControlStart;
            internal Vector2 SavedMouse;
            internal Rect AreaRect;
            internal int EntryCount;
            internal Vector2 GuiOrigin;
            internal Rect Clip;
        }
        public static readonly List<string> Buttons = new List<string>(), LiteralLabels = new List<string>();
        public static readonly List<ScrollViewRecord> ScrollViews = new List<ScrollViewRecord>();
        public static readonly List<ControlRecord> Controls = new List<ControlRecord>();
        public static readonly List<BoxRecord> Boxes = new List<BoxRecord>();
        public static Func<int, Vector2, Vector2> ScrollInput;
        public static Func<string, bool> ButtonInput;
        public static bool ConsumeButtonEvent;
        private static readonly Stack<Group> Groups = new Stack<Group>();
        private static readonly Dictionary<int, Rect> NativeWindows = new Dictionary<int, Rect>();
        private static readonly Dictionary<(int Window, int Index), bool> PreviousScrollOverflow = new Dictionary<(int Window, int Index), bool>();
        private static WindowRecord activeWindow;
        public static WindowRecord LastWindow;
        public static int WindowCalls;
        public static Rect LastRect = new Rect(0, 0, 200, 30);
        public static void ResetLayout(float width = 1000, float height = 900)
        {
            Groups.Clear(); ScrollViews.Clear(); Controls.Clear(); Boxes.Clear(); Buttons.Clear(); LiteralLabels.Clear();
            Groups.Push(new Group { Bounds = new Rect(0, 0, width, height), FixedHeight = height, Clip = new Rect(0, 0, width, height) });
            LastRect = new Rect();
        }
        public static Rect Window(int id, Rect rect, Action<int> draw, string title, GUIStyle style, params GUILayoutOption[] options)
            => InvokeWindow(id, rect, draw, style, true, options);

        // Narrow managed contract model from Unity 2021.3 GUILayout/LayoutGroup:
        // BeginArea belongs to the free-area list, not root.entries. An automatic
        // window marks root.isWindow; its empty-root CalcWidth/CalcHeight replace
        // fixed options with style padding and LayoutSingleGroup moves the native
        // window. GUI.Window keeps the supplied native rect through the fixed-area
        // path. This models that distinction; it does not execute Unity/native UI.
        internal static Rect InvokeWindow(int id, Rect rect, Action<int> draw, GUIStyle style, bool automatic, GUILayoutOption[] options)
        {
            WindowCalls++;
            ResetLayout(rect.width, rect.height);
            var record = new WindowRecord { Id = id, RequestedRect = rect, Automatic = automatic };
            activeWindow = record;
            try
            {
                draw(id);
                if (Groups.Count != 1) throw new InvalidOperationException("Unbalanced renderer layout groups");
                record.RootEntryCount = Current.EntryCount;
                if (Event.current.type == EventType.Layout)
                {
                    Rect native = rect;
                    if (automatic && record.RootEntryCount == 0)
                    {
                        native.width = style.padding?.horizontal ?? 0;
                        native.height = style.padding?.vertical ?? 0;
                    }
                    else if (automatic)
                    {
                        native.width = Option("Width", rect.width, options);
                        native.height = Option("Height", rect.height, options);
                    }
                    NativeWindows[id] = native;
                }
                Rect cached;
                record.NativeRect = NativeWindows.TryGetValue(id, out cached) ? cached : rect;
                Rect nativeClip = new Rect(0, 0, record.NativeRect.width, record.NativeRect.height);
                foreach (ControlRecord control in Controls)
                    control.VisibleRect = Intersection(control.WindowLocalRect, Intersection(control.AncestorClip, nativeClip));
                LastWindow = record;
                return record.NativeRect;
            }
            finally { activeWindow = null; }
        }
        private static Rect Intersection(Rect first, Rect second)
        {
            float x = Math.Max(first.x, second.x), y = Math.Max(first.y, second.y);
            return new Rect(x, y, Math.Max(0, Math.Min(first.x + first.width, second.x + second.width) - x),
                Math.Max(0, Math.Min(first.yMax, second.yMax) - y));
        }
        private static Rect Offset(Rect rect, Vector2 origin) => new Rect(rect.x + origin.x, rect.y + origin.y, rect.width, rect.height);
        private static Group Current
        {
            get { if (Groups.Count == 0) ResetLayout(); return Groups.Peek(); }
        }
        private static float Option(string name, float fallback, GUILayoutOption[] options)
        {
            foreach (GUILayoutOption option in options) if (option.Name == name) return option.Value;
            return fallback;
        }
        private static Rect Allocate(float naturalHeight, GUILayoutOption[] options)
        {
            Group parent = Current;
            float width = Option("Width", parent.Horizontal ? Math.Max(20, parent.Bounds.width / 6) : parent.Bounds.width, options);
            width = Math.Min(Math.Max(width, Option("MinWidth", 0, options)), Option("MaxWidth", float.MaxValue, options));
            float height = Math.Max(Option("Height", naturalHeight, options), Option("MinHeight", 0, options));
            return new Rect(parent.Bounds.x + (parent.Horizontal ? parent.Cursor : 0), parent.Bounds.y + (parent.Horizontal ? 0 : parent.Cursor), width, height);
        }
        private static void Advance(Rect rect)
        {
            Group parent = Current;
            parent.Cursor += parent.Horizontal ? rect.width : rect.height;
            parent.Cross = Math.Max(parent.Cross, parent.Horizontal ? rect.height : rect.width);
            parent.EntryCount++;
            LastRect = rect;
        }
        private static void Begin(bool horizontal, GUIStyle style, GUILayoutOption[] options)
        {
            Rect bounds = Allocate(0, options);
            // A fixed-width list column has already consumed horizontal space;
            // its expanding detail sibling receives the actual remainder.
            if (!horizontal && Current.Horizontal && Option("Width", -1, options) < 0 && Option("ExpandWidth", 0, options) > 0)
                bounds.width = Math.Max(1, Current.Bounds.width - Current.Cursor);
            RectOffset padding = style?.padding;
            Groups.Push(new Group
            {
                Bounds = new Rect(bounds.x + (padding?.left ?? 0), bounds.y + (padding?.top ?? 0),
                    Math.Max(1, bounds.width - (padding?.horizontal ?? 0)), bounds.height),
                Horizontal = horizontal, FixedHeight = Option("Height", 0, options), Padding = padding, Style = style, ControlStart = Controls.Count,
                GuiOrigin = Current.GuiOrigin, Clip = Current.Clip
            });
        }
        private static void End()
        {
            Group group = Groups.Pop();
            float natural = group.Horizontal ? group.Cross : group.Cursor;
            float height = group.FixedHeight > 0 ? group.FixedHeight : natural + (group.Padding?.vertical ?? 0);
            Rect rect = new Rect(group.Bounds.x - (group.Padding?.left ?? 0), group.Bounds.y - (group.Padding?.top ?? 0),
                group.Bounds.width + (group.Padding?.horizontal ?? 0), height);
            if (!group.Horizontal && group.Style != null)
            {
                var box = new BoxRecord { Rect = rect, Style = group.Style };
                for (int index = group.ControlStart; index < Controls.Count; index++) box.Controls.Add(Controls[index]);
                Boxes.Add(box);
            }
            Advance(rect);
        }
        public static void BeginArea(Rect rect)
        {
            activeWindow?.FreeAreas.Add(Offset(rect, Current.GuiOrigin));
            Rect windowLocal = Offset(rect, Current.GuiOrigin);
            Groups.Push(new Group { Bounds = new Rect(0, 0, rect.width, rect.height), Area = true, FixedHeight = rect.height,
                SavedMouse = Event.current.mousePosition, AreaRect = rect,
                GuiOrigin = new Vector2(windowLocal.x, windowLocal.y), Clip = Intersection(Current.Clip, windowLocal) });
            Event.current.mousePosition = new Vector2(Event.current.mousePosition.x - rect.x, Event.current.mousePosition.y - rect.y);
        }
        public static void BeginArea(Rect rect, GUIStyle style) { BeginArea(rect); }
        public static void EndArea()
        { Group group = Groups.Pop(); Event.current.mousePosition = group.SavedMouse; LastRect = group.AreaRect; }
        public static void BeginHorizontal(params GUILayoutOption[] options) => Begin(true, null, options);
        public static void BeginHorizontal(GUIStyle style, params GUILayoutOption[] options) => Begin(true, style, options);
        public static void EndHorizontal() => End();
        public static void BeginVertical(params GUILayoutOption[] options) => Begin(false, null, options);
        public static void BeginVertical(GUIStyle style, params GUILayoutOption[] options) => Begin(false, style, options);
        public static void EndVertical() => End();
        public static Vector2 BeginScrollView(Vector2 scroll, params GUILayoutOption[] options)
            => BeginScrollCore(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, options);
        private static Vector2 BeginScrollCore(Vector2 scroll, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle scrollbar, GUIStyle background, GUILayoutOption[] options)
        {
            // Unbounded expansion is intentionally not assumed to fill a fixed
            // window; the renderer must supply the measured viewport height.
            Rect viewport = Allocate(Option("MinHeight", 60, options), options);
            Vector2 result = ScrollInput == null ? scroll : ScrollInput(ScrollViews.Count, scroll);
            // Narrow managed layout contract: converge scrollbar-dependent client
            // width across layout/repaint passes. Unity performs its own word-wrap
            // and overflow layout; this boundary does not execute native GUI.
            var cacheKey = (activeWindow?.Id ?? 0, ScrollViews.Count);
            bool reserveBar = alwaysShowVertical || PreviousScrollOverflow.TryGetValue(cacheKey, out bool previousOverflow) && previousOverflow;
            float barWidth = reserveBar ? scrollbar.fixedWidth + (scrollbar.margin?.left ?? 0) : 0;
            float contentWidth = Math.Max(1, viewport.width - barWidth - (background.padding?.horizontal ?? 0));
            var record = new ScrollViewRecord { Viewport = viewport, InputScroll = scroll, ReturnedScroll = result, ContentWidth = contentWidth,
                AlwaysShowHorizontal = alwaysShowHorizontal, AlwaysShowVertical = alwaysShowVertical, HorizontalScrollbar = horizontalScrollbar,
                ReservedVerticalScrollbar = reserveBar, CacheKey = cacheKey };
            ScrollViews.Add(record);
            Rect viewportInWindow = Offset(viewport, Current.GuiOrigin);
            Groups.Push(new Group { Bounds = new Rect(background.padding?.left ?? 0, background.padding?.top ?? 0, contentWidth, viewport.height),
                Scroll = true, Record = record, FixedHeight = viewport.height, SavedMouse = Event.current.mousePosition,
                GuiOrigin = new Vector2(viewportInWindow.x - result.x, viewportInWindow.y - result.y),
                Clip = Intersection(Current.Clip, viewportInWindow) });
            Event.current.mousePosition = new Vector2(Event.current.mousePosition.x - viewport.x + result.x,
                Event.current.mousePosition.y - viewport.y + result.y);
            return result;
        }
        public static Vector2 BeginScrollView(Vector2 scroll, bool alwaysShowHorizontal, bool alwaysShowVertical,
            GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
            => BeginScrollCore(scroll, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background, options);
        public static void EndScrollView()
        {
            Group scroll = Groups.Pop();
            scroll.Record.ContentHeight = scroll.Cursor;
            PreviousScrollOverflow[scroll.Record.CacheKey] = scroll.Record.ContentHeight > scroll.Record.Viewport.height;
            Event.current.mousePosition = scroll.SavedMouse;
            Advance(scroll.Record.Viewport);
        }
        private static void Control(string text, GUIStyle style, bool button, GUILayoutOption[] options)
        {
            Rect initial = Allocate(0, options);
            float width = Option("Width", style.fixedWidth > 0 ? style.fixedWidth : initial.width, options);
            width = Math.Min(Math.Max(width, Option("MinWidth", 0, options)), Option("MaxWidth", float.MaxValue, options));
            float naturalHeight = style.CalcHeight(new GUIContent(text), width);
            float height = Option("Height", style.fixedHeight > 0 ? style.fixedHeight : naturalHeight, options);
            // Unity GUILayoutEntry.MinHeight raises maxHeight too. A wrapping
            // sizer caches both forced values and therefore skips CalcHeight,
            // leaving an old MinHeight(font+16) button at that single-line size.
            if (style.wordWrap && style.fixedHeight == 0 && Option("Height", -1, options) < 0 && Option("MinHeight", 0, options) > 0)
                height = Option("MinHeight", height, options);
            Rect rect = Allocate(height, new[] { Width(width), Height(height) });
            var control = new ControlRecord { Text = text, Rect = rect, FontSize = style.fontSize, Button = button, Enabled = GUI.enabled,
                WindowLocalRect = Offset(rect, Current.GuiOrigin), AncestorClip = Current.Clip };
            control.VisibleRect = Intersection(control.WindowLocalRect, control.AncestorClip);
            Controls.Add(control);
            foreach (Group group in Groups)
                if (group.Scroll) { group.Record.Controls.Add(control); break; }
            Advance(rect);
        }
        public static bool Button(string text, GUIStyle style, params GUILayoutOption[] options)
        {
            Buttons.Add(text); Control(text, style, true, options);
            bool pressed = GUI.enabled && ButtonInput != null && ButtonInput(text);
            if (pressed && ConsumeButtonEvent) Event.current.Use();
            return pressed;
        }
        public static void Label(string text, GUIStyle style, params GUILayoutOption[] options)
        { LiteralLabels.Add(text); Control(text, style, false, options); }
        public static void FlexibleSpace() { }
        public static void Space(float height) => Advance(Allocate(height, Array.Empty<GUILayoutOption>()));
        public static GUILayoutOption Width(float value) => new GUILayoutOption("Width", value);
        public static GUILayoutOption Height(float value) => new GUILayoutOption("Height", value);
        public static GUILayoutOption MinWidth(float value) => new GUILayoutOption("MinWidth", value);
        public static GUILayoutOption MinHeight(float value) => new GUILayoutOption("MinHeight", value);
        public static GUILayoutOption MaxWidth(float value) => new GUILayoutOption("MaxWidth", value);
        public static GUILayoutOption MaxHeight(float value) => new GUILayoutOption("MaxHeight", value);
        public static GUILayoutOption ExpandWidth(bool value) => new GUILayoutOption("ExpandWidth", value ? 1 : 0);
        public static GUILayoutOption ExpandHeight(bool value) => new GUILayoutOption("ExpandHeight", value ? 1 : 0);
    }
    public static class GUILayoutUtility
    {
        public static Rect GetLastRect() => Event.current.type == EventType.Layout ? new Rect(0, 0, 1, 1) : GUILayout.LastRect;
        public static Rect GetRect(float width, float height, params GUILayoutOption[] options)
        { GUILayout.Space(height); return GUILayout.LastRect; }
    }
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
    public sealed class Element<T>
    {
        private T value;
        public int Writes { get; private set; }
        public T Value { get { return value; } set { this.value = value; Writes++; } }
        public Element(T value) { this.value = value; }
    }
    public static class ConfigManager
    {
        public static Element<float> LogWindowX = new Element<float>(20), LogWindowY = new Element<float>(20), LogWindowWidth = new Element<float>(872), LogWindowHeight = new Element<float>(960);
        public static Element<int> LogFontSize = new Element<int>(20);
        public static Element<bool> EnableOptionInfo = new Element<bool>(true);
        public static Element<UnityEngine.KeyCode> LogMenuToggleKey = new Element<UnityEngine.KeyCode>(UnityEngine.KeyCode.Tab);
    }
}
namespace DiceMaster.EmbeddedTranslations
{
    public static class Display
    {
        public static readonly List<string> TranslatedLabels = new List<string>();
        public static void Label(string text, UnityEngine.GUIStyle style, UnityEngine.GUILayoutOption[] options)
        { TranslatedLabels.Add(text); UnityEngine.GUILayout.Label(text, style, options); }
    }
}
namespace DiceMaster.Gamepad
{
    public static class GamepadBridge
    {
        public static int Cancellations;
        public static Action PointerActivity;
        public static Action PointerMotion;
        public static Action KeyboardActivity;
        public static Action Cancelled;
        public static void CancelLogInput() { Cancellations++; Cancelled?.Invoke(); }
        public static void NotifyPointerActivity() { PointerActivity?.Invoke(); }
        public static void NotifyPointerMotion() { (PointerMotion ?? PointerActivity)?.Invoke(); }
        public static void NotifyKeyboardActivity() { KeyboardActivity?.Invoke(); }
    }
}
namespace DiceMaster.LiveLog
{
    public static class LogBridge
    {
        public static int Reservations;
        public static object LastTarget;
        public static bool ReservationResult = true;
        public static Action DuringReservation;
        public static bool IsReserved(string tab, object target) => target != null && ReferenceEquals(target, LastTarget);
        public static bool TryReserve(long context, string tab, string id, object target)
        {
            Reservations++;
            if (ReservationResult) LastTarget = target;
            DuringReservation?.Invoke();
            return ReservationResult;
        }
    }
}
