using UnityEngine;

namespace DiceMaster.LogUI
{
    // The renderer supplies its allocated layout rect. Native IMGUI keeps
    // ownership of clipped hit testing/capture; renderers keep action authority.
    internal static class ImGuiPointerButton
    {
        internal readonly struct Result
        {
            internal readonly bool Down, Up, Clicked;
            internal Result(bool down, bool up, bool clicked)
            { Down = down; Up = up; Clicked = clicked; }
        }

        internal static Result Draw(Rect rect, GUIContent content, GUIStyle style)
        {
            Event evt = Event.current;
            // rawType can retain a press during layout, repaint or a Used
            // callback. Only this callback's effective event is fresh input.
            EventType original = evt.type;
            bool down = original == EventType.MouseDown || original == EventType.TouchDown;
            bool up = original == EventType.MouseUp || original == EventType.TouchUp;
            bool touch = original == EventType.TouchDown || original == EventType.TouchUp || original == EventType.TouchMove;
            if (original == EventType.TouchDown) evt.type = EventType.MouseDown;
            else if (original == EventType.TouchUp) evt.type = EventType.MouseUp;
            else if (original == EventType.TouchMove) evt.type = EventType.MouseDrag;
            bool clicked;
            try { clicked = GUI.Button(rect, content, style); }
            finally
            {
                // Translate for this one native call. Preserve consumption;
                // otherwise later controls must still see the original touch.
                if (touch && evt.type != EventType.Used) evt.type = original;
            }
            return new Result(down, up, clicked);
        }
    }
}
