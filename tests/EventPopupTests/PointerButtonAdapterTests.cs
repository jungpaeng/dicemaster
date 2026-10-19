using System;
using DiceMaster.LogUI;
using UnityEngine;

internal static partial class Program
{
    private static void PointerButtonAdapterContracts()
    {
        var rect = new Rect(5, 7, 120, 35);
        var content = new GUIContent("adapter contract");
        var style = new GUIStyle { fontSize = 23 };
        var point = new Vector2(20, 20);
        ImGuiPointerButton.Result Draw(EventType type, EventType raw, Vector2 position, int button = 0)
        {
            GUILayout.ResetLayout();
            Event.current = new Event { type = type, rawType = raw, button = button, mousePosition = position };
            return ImGuiPointerButton.Draw(rect, content, style);
        }

        GUI.enabled = true; GUILayout.ButtonInput = null;
        foreach (var pair in new[] {
            (EventType.MouseDown, EventType.MouseUp, EventType.MouseDown, EventType.MouseUp),
            (EventType.MouseDown, EventType.MouseUp, EventType.TouchDown, EventType.TouchUp),
            (EventType.TouchDown, EventType.TouchUp, EventType.TouchDown, EventType.TouchUp)
        })
        {
            GUILayout.ResetPointerCapture();
            var press = Draw(pair.Item1, pair.Item3, point);
            Check(press.Down && !press.Up && !press.Clicked && Event.current.type == EventType.Used,
                "Adapter retains native capture/consumption without activating on down: " + pair);
            Check(Event.current.rawType == pair.Item3 && GUILayout.Controls.Count == 1
                && GUILayout.Controls[0].Text == content.text && GUILayout.Controls[0].Rect == rect
                && GUILayout.Controls[0].FontSize == style.fontSize,
                "Adapter submits the supplied rect/content/style once and preserves physical raw input: " + pair);
            var release = Draw(pair.Item2, pair.Item4, point);
            Check(!release.Down && release.Up && release.Clicked && Event.current.type == EventType.Used,
                "One matching native release completes the click: " + pair);
            Check(Event.current.rawType == pair.Item4, "Consumed release preserves raw input: " + pair);
            var duplicate = Draw(pair.Item2, pair.Item4, point);
            Check(duplicate.Up && !duplicate.Clicked && Event.current.type == pair.Item2,
                "A duplicate uncaptured release cannot click and keeps its original event: " + pair);
        }

        foreach (EventType phase in new[] { EventType.Layout, EventType.Repaint, EventType.Used, EventType.Ignore })
        foreach (EventType raw in new[] { EventType.MouseDown, EventType.TouchDown, EventType.MouseUp, EventType.TouchUp })
        {
            GUILayout.ResetPointerCapture();
            var result = Draw(phase, raw, point);
            Check(!result.Down && !result.Up && !result.Clicked && Event.current.type == phase && Event.current.rawType == raw,
                "A non-input callback never becomes fresh input from rawType: " + phase + "/" + raw);
        }

        EventType native = EventType.Ignore;
        GUILayout.ButtonInput = caption => { native = Event.current.type; return false; };
        var move = Draw(EventType.TouchMove, EventType.TouchMove, point);
        Check(native == EventType.MouseDrag && !move.Down && !move.Up && !move.Clicked
            && Event.current.type == EventType.TouchMove && Event.current.rawType == EventType.TouchMove,
            "A direct move is translated only for the native call and restored when unconsumed");
        GUILayout.ButtonInput = null;

        foreach (string boundary in new[] { "disabled", "outside", "right-button" })
        {
            GUILayout.ResetPointerCapture();
            GUI.enabled = boundary != "disabled";
            var result = Draw(EventType.TouchDown, EventType.TouchDown,
                boundary == "outside" ? new Vector2(-20, -20) : point, boundary == "right-button" ? 1 : 0);
            Check(result.Down && !result.Clicked && Event.current.type == EventType.TouchDown,
                "The native button rejects an unaccepted press without consuming it: " + boundary);
        }
        GUI.enabled = true;
        GUILayout.ResetPointerCapture(); GUILayout.ResetLayout();
        Event.current = new Event { type = EventType.TouchDown, rawType = EventType.TouchDown,
            mousePosition = new Vector2(10, 40) };
        GUILayout.BeginArea(new Rect(0, 0, 20, 20));
        var clipped = ImGuiPointerButton.Draw(new Rect(0, 30, 20, 20), content, style);
        GUILayout.EndArea();
        Check(clipped.Down && !clipped.Clicked && Event.current.type == EventType.TouchDown,
            "Native clipping rejects an otherwise geometrically inside press");

        GUILayout.ResetPointerCapture();
        GUILayout.ButtonInput = caption => throw new InvalidOperationException("native call failure");
        bool failed = false;
        try { Draw(EventType.TouchMove, EventType.TouchMove, point); }
        catch (InvalidOperationException error) { failed = error.Message == "native call failure"; }
        Check(failed && Event.current.type == EventType.TouchMove && Event.current.rawType == EventType.TouchMove,
            "A failing unconsumed native call restores the touch phase in finally");

        GUILayout.ButtonInput = null;
        Draw(EventType.TouchDown, EventType.TouchDown, point);
        GUILayout.ButtonInput = caption => throw new InvalidOperationException("native call failure");
        failed = false;
        try { Draw(EventType.TouchUp, EventType.TouchUp, point); }
        catch (InvalidOperationException error) { failed = error.Message == "native call failure"; }
        Check(failed && Event.current.type == EventType.Used && Event.current.rawType == EventType.TouchUp,
            "A failing consumed native call retains Used rather than replaying its touch phase");
        GUILayout.ButtonInput = null; GUILayout.ResetPointerCapture(); GUILayout.ResetLayout();
    }
}
