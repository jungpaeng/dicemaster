using System;
using DiceMaster.Config;
using DiceMaster.Gamepad;
using DiceMaster.LogUI;
using UnityEngine;

internal static partial class Program
{
    private static void RendererNonInputPassesPreservePointer()
    {
        GUILayout.ButtonInput = null; GUILayout.ResetPointerCapture();
        var model = new LogModel(); model.ResetContext(791);
        Publish(model, "조건", Item("non-input-pass"));
        var renderer = new LogRenderer(model, null, null);
        renderer.Handle(LogPadCommand.None);

        // rawType identifies the original event, while type is the event this
        // callback is processing. Layout/repaint/filtered callbacks must never
        // finish or create a gesture merely because rawType is a mouse event.
        foreach (EventType pass in new[] { EventType.Layout, EventType.Repaint, EventType.Ignore })
        {
            Vector2 point = PointerTarget(renderer, control => control.Button && control.Text.Contains("조건", StringComparison.Ordinal));
            DrawPointer(renderer, EventType.MouseDown, EventType.MouseDown, point);
            Check((string)Field(renderer, "pointerIdentity") == "tab:1", "physical press starts a tab transaction");
            DrawPointer(renderer, pass, EventType.MouseUp, point);
            Check((string)Field(renderer, "pointerIdentity") == "tab:1",
                pass + " retaining raw mouse-up cannot finish the physical press before its input callback");
            SoftResetForPointer(renderer);
            renderer.Handle(LogPadCommand.None); renderer.Scroll(0);
            DrawPointer(renderer, EventType.MouseUp, EventType.MouseUp, point);
            Check(renderer.CurrentTab == "조건" && Field(renderer, "pointerIdentity") == null,
                "matching input release activates the tab once after " + pass);
            renderer.Handle(LogPadCommand.PreviousTab);
        }

        renderer.Handle(LogPadCommand.NextTab);
        Repaint(renderer);
        GUILayout.ControlRecord first = GUILayout.Controls.Find(control => control.Button && control.Text == "선택지");
        var corner = new Vector2(first.WindowLocalRect.x + .5f, first.WindowLocalRect.y + .5f);
        foreach (EventType pass in new[] { EventType.Layout, EventType.Repaint, EventType.Ignore })
        {
            DrawPointer(renderer, pass, EventType.MouseDown, corner);
            Check(Field(renderer, "pointerIdentity") == null,
                pass + " retaining raw mouse-down cannot arm a real or dummy control rectangle");
        }
    }

    private static void RendererNativeConsumedCornerPress()
    {
        GUILayout.ButtonInput = null; GUILayout.ResetPointerCapture();
        var model = new LogModel(); model.ResetContext(790);
        var renderer = new LogRenderer(model, null, null);
        renderer.Handle(LogPadCommand.None); renderer.Handle(LogPadCommand.NextTab);
        Repaint(renderer);
        GUILayout.ControlRecord first = GUILayout.Controls.Find(control => control.Button && control.Text == "선택지");
        var point = new Vector2(first.WindowLocalRect.x + .5f, first.WindowLocalRect.y + .5f);
        DrawPointer(renderer, EventType.MouseDown, EventType.MouseDown, point);
        Check((string)Field(renderer, "pointerIdentity") == "tab:0",
            "later Used-event dummy rectangles cannot steal a valid press at the first tab's local 0.5px corner");
        DrawPointer(renderer, EventType.MouseUp, EventType.MouseUp, point);
        Check(renderer.CurrentTab == "선택지", "the original corner press still activates its actual native button");

        GUILayout.ResetPointerCapture();
        DrawPointer(renderer, EventType.Used, EventType.MouseDown, point);
        Check(Field(renderer, "pointerIdentity") == null,
            "an already-consumed raw mouse-down cannot create a new transaction from any dummy layout rectangle");
    }

    private static void NativeButtonEventContract()
    {
        var style = new GUIStyle { fontSize = 20, wordWrap = true };
        var point = new Vector2(40, 15);
        GUILayout.ButtonInput = null;
        GUILayout.ResetPointerCapture();
        GUILayout.ResetLayout();
        Event.current = new Event { type = EventType.MouseDown, rawType = EventType.MouseDown, mousePosition = point };
        Check(!GUILayout.Button("native capture", style) && Event.current.type == EventType.Used
            && Event.current.rawType == EventType.MouseDown,
            "Unity contract: native button consumes accepted mouse-down while preserving rawType");
        Check(GUILayout.LastRect.Contains(point) && GUILayoutUtility.GetLastRect() == new Rect(0, 0, 1, 1),
            "negative control: reading last layout rect after the native button returns Used dummy outside the real hit area");
        GUILayout.ResetLayout();
        Event.current = new Event { type = EventType.MouseUp, rawType = EventType.MouseUp, mousePosition = point };
        Check(GUILayout.Button("native capture", style) && Event.current.type == EventType.Used,
            "Unity contract: matching native mouse-up activates once and consumes the release");
        GUILayout.ResetLayout();
        Event.current = new Event { type = EventType.MouseUp, rawType = EventType.MouseUp, mousePosition = point };
        Check(!GUILayout.Button("native capture", style), "native release cannot activate again without a new capture");

        GUILayout.ResetLayout();
        Event.current = new Event { type = EventType.Layout, rawType = EventType.Layout };
        GUILayout.Label("layout", style);
        Check(GUILayoutUtility.GetLastRect() == new Rect(0, 0, 1, 1), "Layout also returns Unity's dummy last rect");
        foreach (EventType type in new[] { EventType.TouchDown, EventType.TouchUp })
        {
            GUILayout.ResetLayout();
            Event.current = new Event { type = type, rawType = type, mousePosition = point };
            Check(!GUILayout.Button("native capture", style) && Event.current.type == type,
                "Unity DoControl does not manufacture button success for untranslated touch types");
        }
    }

    private static void RendererNativePointerTransactions()
    {
        int savedFont = ConfigManager.LogFontSize.Value, savedTouchCount = Input.touchCount;
        float savedWidth = ConfigManager.LogWindowWidth.Value, savedHeight = ConfigManager.LogWindowHeight.Value;
        float savedX = ConfigManager.LogWindowX.Value, savedY = ConfigManager.LogWindowY.Value;
        try
        {
            ConfigManager.LogWindowWidth.Value = 1000; ConfigManager.LogWindowHeight.Value = 900;
            ConfigManager.LogWindowX.Value = ConfigManager.LogWindowY.Value = 20;
            // No synthetic ButtonInput success is supplied in these cases.
            // Mouse types cover both a trackpad and mouse-emulated touchscreen;
            // The third form covers raw touch translated to effective mouse;
            // the fourth is untranslated touch sent through the renderer's
            // explicit native-button adapter, with no injected success value.
            foreach (int kind in new[] { 0, 1, 2, 3 })
            {
                ConfigManager.LogFontSize.Value = 20;
                Input.touchCount = kind == 1 || kind == 3 ? 1 : 0;
                EventType down = kind == 3 ? EventType.TouchDown : EventType.MouseDown;
                EventType up = kind == 3 ? EventType.TouchUp : EventType.MouseUp;
                EventType rawDown = kind >= 2 ? EventType.TouchDown : EventType.MouseDown;
                EventType rawUp = kind >= 2 ? EventType.TouchUp : EventType.MouseUp;
                string scenario = kind == 0 ? "trackpad mouse" : kind == 1 ? "mouse-emulated monitor touch" :
                    kind == 2 ? "effective mouse translated from touch" : "direct touch native-button adapter";
                GUILayout.ResetPointerCapture();
                var model = new LogModel(); model.ResetContext(760 + kind);
                Publish(model, "조건", Item("native-first"), Item("native-second"));
                int closes = 0;
                var renderer = new LogRenderer(model, null, () => closes++);
                renderer.Handle(LogPadCommand.None);

                Action<Func<GUILayout.ControlRecord, bool>> click = matches =>
                {
                    Vector2 point = PointerTarget(renderer, matches);
                    DrawPointer(renderer, down, rawDown, point);
                    Check(Event.current.type == EventType.Used && Field(renderer, "pointerIdentity") != null,
                        scenario + ": consumed down retains the identity from its actual pre-consumption rectangle");
                    SoftResetForPointer(renderer);
                    Repaint(renderer);
                    foreach (EventType pass in new[] { EventType.Layout, EventType.Repaint, EventType.Ignore })
                    {
                        DrawPointer(renderer, pass, rawUp, point);
                        Check(Field(renderer, "pointerIdentity") != null,
                            scenario + ": " + pass + " with an unchanged raw release cannot cancel a chrome, row or tool press");
                    }
                    renderer.Handle(LogPadCommand.None); renderer.Scroll(0);
                    DrawPointer(renderer, up, rawUp, point);
                    Check(Event.current.type == EventType.Used && Field(renderer, "pointerIdentity") == null,
                        scenario + ": consumed up completes and clears the same pointer transaction");
                };

                click(control => control.Button && control.Text == "조건");
                Check(renderer.CurrentTab == "조건", scenario + ": chrome tab selection reaches the intended tab");
                click(control => control.Button && control.Text.Contains("native-second", StringComparison.Ordinal));
                Check(model.Tab("조건").SelectedId == "native-second", scenario + ": list selection reaches the touched row");
                click(control => control.Button && control.Text == "도구 [Y]");
                Check(Area(renderer) == "Tools", scenario + ": tools chrome is clickable");
                Vector2 toolPoint = PointerTarget(renderer, control => control.Button && control.Text.Contains("본문 글자 크게", StringComparison.Ordinal));
                click(control => control.Button && control.Text.Contains("본문 글자 크게", StringComparison.Ordinal));
                Check(ConfigManager.LogFontSize.Value == 21, scenario + ": tool item performs its action once");
                DrawPointer(renderer, up, rawUp, toolPoint);
                Check(ConfigManager.LogFontSize.Value == 21, scenario + ": duplicate release never repeats a tool action");

                // Native capture alone does not authorize replay into a newly
                // published context, even when its control ordinal is the same.
                Vector2 closePoint = PointerTarget(renderer, control => control.Button && control.Text == "닫기");
                DrawPointer(renderer, down, rawDown, closePoint);
                model.ResetContext(860 + kind);
                DrawPointer(renderer, up, rawUp, closePoint);
                Check(closes == 0 && Field(renderer, "pointerIdentity") == null,
                    scenario + ": context replacement invalidates an otherwise matching native release");
            }
        }
        finally
        {
            ConfigManager.LogFontSize.Value = savedFont;
            ConfigManager.LogWindowWidth.Value = savedWidth; ConfigManager.LogWindowHeight.Value = savedHeight;
            ConfigManager.LogWindowX.Value = savedX; ConfigManager.LogWindowY.Value = savedY;
            Input.touchCount = savedTouchCount;
            GUILayout.ButtonInput = null; GUILayout.ResetPointerCapture();
        }
    }
}
