using System;
using System.Linq;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using UnityEngine;

internal static partial class Program
{
    private static void SwitchPopupRegressions()
    {
        foreach (var pair in new[] {
            (EventType.MouseDown, EventType.MouseUp, EventType.MouseDown, EventType.MouseUp),
            (EventType.MouseDown, EventType.MouseUp, EventType.TouchDown, EventType.TouchUp),
            (EventType.TouchDown, EventType.TouchUp, EventType.TouchDown, EventType.TouchUp) })
        foreach (bool forced in new[] { false, true })
        {
            var popup = Start();
            EventSelectionRuntime.IsSwitch = true;
            string id = forced ? "blocked" : "eligible";
            Draw(popup);
            Check(GUILayout.LiteralLabels.Contains("이번 대화의 조건 분기 선택") &&
                GUILayout.LiteralLabels.Contains("[A / Enter] 분기를 선택하세요") &&
                GUILayout.LiteralLabels.Contains("[B / Esc] 원래 판정으로 진행") &&
                !GUILayout.LiteralLabels.Contains("[B / Esc] 무작위로 진행"),
                "Switch popup distinguishes the decision and the original-game fallback");
            Click(popup, text => RowCaption(text, id), pair.Item1, pair.Item2, pair.Item3, pair.Item4);
            Check(EventSelectionRuntime.Commands == 0, "First switch row press only selects");
            Draw(popup);
            Check(GUILayout.LiteralLabels.Contains("분기 조건") && !GUILayout.LiteralLabels.Contains("발동 조건"),
                "Switch detail describes branch conditions");
            Check(GUILayout.LiteralLabels.Contains(forced ? "[A / Enter] 조건 무시하고 이번만 진행" : "[A / Enter] 이 분기로 진행"),
                "Switch confirmation preserves the displayed normal or forced intent");
            Check(GUILayout.Buttons.All(text => !InstructionCaption(text)), "Switch footer stays literal guidance");
            Click(popup, SelectedRowCaption, pair.Item1, pair.Item2, pair.Item3, pair.Item4);
            Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Selected == id && EventSelectionRuntime.Forced == forced,
                "The next fresh switch row press confirms once with its captured mode");
        }
        var original = Start(); EventSelectionRuntime.IsSwitch = true;
        original.Handle(LogPadCommand.Back);
        Check(EventSelectionRuntime.Commands == 1 && EventSelectionRuntime.Random, "Switch B requests the original game decision");
        Start(); // Leave the shared runtime boundary in its default event mode.
    }
}
