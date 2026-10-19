using System;
using System.Collections.Generic;
using System.Linq;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using DiceMaster.LogUI;
using UnityEngine;

namespace UnityEngine { public static class Application { public static bool isFocused = true; } }

// Only the runtime command receiver is substituted. These tests run the actual
// popup view against the existing managed Unity layout/input boundary.
namespace DiceMaster.EventSelection
{
    public sealed class EventCandidate
    {
        public string Id, Title, State = "정상 발동 가능", Summary = "현재 조건 충족";
        public bool CanSelect, CanForce;
    }
    public static class EventSelectionRuntime
    {
        public static bool Waiting = true;
        public static long Token = 1;
        public static IList<EventCandidate> Candidates = new List<EventCandidate>();
        public static string Status = "현재 판정";
        public static int Commands;
        public static string Selected;
        public static bool Forced, Random;
        public static bool Select(string id, bool force)
        { Commands++; Selected = id; Forced = force; Random = false; return true; }
        public static void ContinueRandom() { Commands++; Random = true; }
    }
}

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new InvalidOperationException(message); }
    private static EventPopupRenderer Start()
    {
        EventSelectionRuntime.Token++;
        EventSelectionRuntime.Waiting = true;
        EventSelectionRuntime.Commands = 0;
        EventSelectionRuntime.Selected = null;
        EventSelectionRuntime.Random = EventSelectionRuntime.Forced = false;
        EventSelectionRuntime.Candidates = new List<EventCandidate> {
            new EventCandidate { Id = "eligible", Title = "정상 후보", CanSelect = true },
            new EventCandidate { Id = "blocked", Title = "미충족 후보", State = "발동 불가 · 조건 미충족", CanForce = true },
            new EventCandidate { Id = "broken", Title = "읽기 실패 후보" }
        };
        Input.Held.Clear(); Application.isFocused = true; Input.touchCount = 0;
        var popup = new EventPopupRenderer(); popup.UpdateInput(); return popup;
    }
    private static void Draw(EventPopupRenderer popup, EventType type = EventType.Repaint, KeyCode key = KeyCode.None, bool shift = false)
    {
        GUILayout.ResetLayout(Screen.width, Screen.height);
        Event.current = new Event { type = type, rawType = type, keyCode = key, shift = shift };
        popup.Draw();
    }
    private static void Key(EventPopupRenderer popup, KeyCode key, bool shift = false)
    { Draw(popup, EventType.KeyDown, key, shift); Draw(popup, EventType.KeyUp, key); }
    private static void Main()
    {
        var popup = Start();
        popup.Handle(LogPadCommand.Activate);
        Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Random, "Initial focus must use original random decision");

        popup = Start(); popup.Handle(LogPadCommand.Down); popup.Handle(LogPadCommand.Activate);
        Check(EventSelectionRuntime.Selected == "eligible" && !EventSelectionRuntime.Forced, "A must submit normal current candidate");
        popup = Start(); popup.Handle(LogPadCommand.Down); popup.Handle(LogPadCommand.Down); popup.Handle(LogPadCommand.Activate);
        Check(EventSelectionRuntime.Commands == 0, "A must not force a blocked candidate");
        popup.Handle(LogPadCommand.Refresh);
        Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Forced && EventSelectionRuntime.Selected == "blocked", "X must explicitly force current blocked candidate");
        popup.Handle(LogPadCommand.Down); popup.Handle(LogPadCommand.Refresh);
        Check(EventSelectionRuntime.Commands == 1, "Unreadable result must not accept forced request");
        popup.Handle(LogPadCommand.Back);
        Check(EventSelectionRuntime.Commands == 2 && EventSelectionRuntime.Random, "B must discard choice and request original decision");

        popup = Start(); Input.Held.Add(KeyCode.Return); popup.ResetInput(); popup.UpdateInput();
        Draw(popup, EventType.KeyDown, KeyCode.Return);
        Check(EventSelectionRuntime.Commands == 0, "Entry held Enter must not confirm popup");
        Input.Held.Clear(); Draw(popup, EventType.KeyUp, KeyCode.Return); popup.UpdateInput();
        Key(popup, KeyCode.DownArrow); Key(popup, KeyCode.DownArrow); Key(popup, KeyCode.Return, true);
        Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Forced, "Shift+Enter must explicitly force selected candidate");
        Draw(popup, EventType.KeyDown, KeyCode.Return, true);
        Check(EventSelectionRuntime.Commands == 1, "Queued confirmation must require neutral before new key");

        popup = Start(); Key(popup, KeyCode.DownArrow);
        EventSelectionRuntime.Candidates = new List<EventCandidate> { new EventCandidate { Id = "replacement", Title = "교체", CanSelect = true } };
        Draw(popup, EventType.KeyDown, KeyCode.Return);
        Check(EventSelectionRuntime.Commands == 0, "Target removal must cancel keyboard authority");
        Draw(popup, EventType.KeyUp, KeyCode.Return);
        popup.UpdateInput(); Key(popup, KeyCode.Return);
        Check(EventSelectionRuntime.Random, "Removed target must return focus to random without auto execution");
        popup = Start(); Key(popup, KeyCode.DownArrow); EventSelectionRuntime.Token++;
        Draw(popup, EventType.KeyDown, KeyCode.Return);
        Check(EventSelectionRuntime.Commands == 0, "Old token input must never execute new decision");

        popup = Start(); Input.Held.Add(KeyCode.Escape); popup.ResetInput(); popup.UpdateInput();
        Key(popup, KeyCode.Escape);
        Check(EventSelectionRuntime.Commands == 0, "Log-close B/Esc equivalent must wait for release on return");
        Input.Held.Clear(); popup.UpdateInput(); Key(popup, KeyCode.Escape);
        Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Random, "New Esc after neutral must use random");

        popup = Start();
        EventSelectionRuntime.Candidates[0].Title = "<color=#BCA136>긴 한국어 제목</color>";
        Draw(popup);
        Check(GUILayout.Buttons.Any(b => b.Contains("긴 한국어 제목")) && GUILayout.Buttons.All(b => !b.Contains("<color=")),
            "Popup must show translated game names without raw rich text markup");

        foreach (var size in new[] { (640, 480), (1280, 720), (1920, 1200), (2560, 1440) })
        foreach (int font in new[] { 12, 20, 64 })
        {
            Screen.width = size.Item1; Screen.height = size.Item2;
            DiceMaster.Config.ConfigManager.LogFontSize.Value = font;
            popup = Start();
            EventSelectionRuntime.Candidates = Enumerable.Range(0, 50).Select(i => new EventCandidate {
                Id = i.ToString(), Title = new string('한', 120), Summary = new string('글', 300), CanSelect = true }).ToList();
            Draw(popup);
            Check(GUILayout.ScrollViews.Count == 2 && GUILayout.ScrollViews.All(s => s.Viewport.width > 0 && s.Viewport.height > 0), "List and detail must have usable scroll viewports");
            Check(GUILayout.Buttons.Any(b => b.StartsWith("무작위로 진행")), "Random recovery must remain present at every size");
            foreach (var control in GUILayout.Controls.Where(c => c.Button &&
                (c.Text.StartsWith("이 이벤트로 진행") || c.Text.StartsWith("조건 무시하고") || c.Text.StartsWith("무작위로 진행 ["))))
                Check(control.VisibleRect.height >= control.Rect.height - .1f,
                    "All explicit progress/recovery buttons must fit at " + size + " font=" + font);
            Check(EventSelectionRuntime.Commands == 0, "Rendering must never queue event execution");
        }
        Console.WriteLine("PASS: EventPopupTests: " + checks + " actual popup input/layout boundary checks; managed substitutes, not Windows Unity rendering.");
    }
}
