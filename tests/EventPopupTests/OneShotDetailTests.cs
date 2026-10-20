using System;
using System.Linq;
using System.Reflection;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using UnityEngine;

internal static partial class Program
{
    private static float OneShotMaximumScroll(DiceMaster.LogUI.EventPopupRenderer popup) => (float)typeof(DiceMaster.LogUI.EventPopupRenderer)
        .GetField("maximumDetailScroll", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(popup);

    private static void OneShotDetailPresentation()
    {
        var popup = Start(); int reads = 0; object requested = null;
        string cached = "1회성 확인 불가: 활성 조건과 결과를 안전하게 연결하지 못했습니다.";
        popup.OneShotEventTag = target => "";
        popup.OneShotEventDetail = target => { reads++; requested = target; return cached; };
        Draw(popup);
        Check(reads == 0 && GUILayout.Controls.All(control => !control.Text.Contains("1회성 확인 불가")),
            "Unselected popup candidates do not request or repeat unknown proof diagnostics");
        popup.Handle(LogPadCommand.Down); Draw(popup);
        Check(reads == 1 && ReferenceEquals(requested, EventSelectionRuntime.Candidates[0].Target)
            && GUILayout.ScrollViews[1].Controls.Any(control => control.Text == cached)
            && GUILayout.ScrollViews[0].Controls.All(control => !control.Text.Contains("1회성 확인 불가")),
            "Only the selected event receives its cached explanation inside the detail scroll view");
        float shortHeight = OneShotMaximumScroll(popup);
        cached = "1회성 읽기 실패: " + string.Join("\n", Enumerable.Range(0, 120).Select(index => "근거 설명 " + index));
        Draw(popup);
        Check(OneShotMaximumScroll(popup) > shortHeight + 100 && GUILayout.LiteralLabels.Contains(cached),
            "The complete proof explanation contributes to detail content height rather than overflowing its scroll range");
        popup.Scroll(1); Draw(popup);
        Check(DetailScroll(popup) > 0, "The pad right stick can scroll a long cached proof explanation");

        popup.Handle(LogPadCommand.Down); cached = "1회성 읽기 실패: 교체된 대상 자료"; Draw(popup);
        Check(ReferenceEquals(requested, EventSelectionRuntime.Candidates[1].Target)
            && DetailScroll(popup) == 0 && GUILayout.LiteralLabels.Contains(cached),
            "Changing popup selection reads the new target cache and resets detail scroll");
        int beforeSwitch = reads;
        EventSelectionRuntime.IsSwitch = true; Draw(popup);
        Check(reads == beforeSwitch && GUILayout.Controls.All(control => !control.Text.Contains("1회성 읽기 실패:")),
            "A condition branch popup does not consume an event's cached proof explanation");
        EventSelectionRuntime.IsSwitch = false; cached = ""; Draw(popup);
        Check(GUILayout.Controls.All(control => !control.Text.Contains("1회성 읽기 실패:")),
            "An empty or disabled proof cache removes the selected popup diagnostic");
        Check(EventSelectionRuntime.Commands == 0 && EventSelectionRuntime.Attempts == 0,
            "One-shot explanation rendering and scrolling never submit gameplay input");
        Start();
    }
}
