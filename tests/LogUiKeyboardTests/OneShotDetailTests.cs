using System;
using System.Linq;
using DiceMaster.Gamepad;
using DiceMaster.LiveLog;
using DiceMaster.LogUI;
using UnityEngine;

internal static partial class Program
{
    private static void RendererOneShotDetails()
    {
        Rect saved = ConfiguredRect(); int font = DiceMaster.Config.ConfigManager.LogFontSize.Value;
        int reservations = LogBridge.Reservations;
        try
        {
            ConfigureLayout(new Rect(20, 20, 1200, 1000), 20);
            var model = new LogModel(); model.ResetContext(9400);
            Publish(model, "선택지", Item("O_unknown"), Item("O_failure"));
            var renderer = new LogRenderer(model, null, null);
            int optionReads = 0;
            renderer.OneShotOptionTag = key => "";
            renderer.OneShotOptionDetail = key => {
                optionReads++;
                return key == "O_unknown" ? "1회성 확인 불가: 활성 조건을 안전하게 연결하지 못했습니다."
                    : "1회성 읽기 실패: 조건 자료를 읽지 못했습니다.";
            };
            renderer.Handle(LogPadCommand.None); Repaint(renderer);
            Check(optionReads == 4 && GUILayout.LiteralLabels.Any(value => value.StartsWith("1회성 확인 불가:", StringComparison.Ordinal))
                && GUILayout.LiteralLabels.Any(value => value.StartsWith("1회성 읽기 실패:", StringComparison.Ordinal)),
                "Choice bodies retain cached unknown and failure reasons without a proof badge");
            Check(GUILayout.Controls.Where(control => control.FontStyle == FontStyle.Bold).All(control => !control.Text.Contains("1회성 확인 불가")),
                "Unknown one-shot diagnostics are body text rather than repeated choice headings");

            object first = new object(), second = new object();
            Publish(model, "이벤트", Item("event-first", first), Item("event-second", second));
            int eventReads = 0; object requested = null;
            renderer.OneShotEventDetail = target => {
                eventReads++; requested = target;
                return ReferenceEquals(target, first) ? "1회성 확인 불가: 첫 이벤트의 근거 없음" : "1회성 읽기 실패: 다음 이벤트 자료 실패";
            };
            renderer.Handle(LogPadCommand.NextTab); renderer.Handle(LogPadCommand.NextTab); Repaint(renderer);
            Check(eventReads == 0, "Unselected event lists never request or display one-shot diagnostics");
            renderer.Handle(LogPadCommand.Down); Repaint(renderer);
            Check(eventReads == 2 && ReferenceEquals(requested, first)
                && GUILayout.ScrollViews[1].Controls.Any(control => control.Text == "1회성 확인 불가: 첫 이벤트의 근거 없음")
                && GUILayout.ScrollViews[0].Controls.All(control => !control.Text.Contains("1회성")),
                "Selected event diagnostics appear only in its scrollable detail and never in the list");
            renderer.Handle(LogPadCommand.Down); Repaint(renderer);
            Check(ReferenceEquals(requested, second)
                && GUILayout.LiteralLabels.Contains("1회성 읽기 실패: 다음 이벤트 자료 실패")
                && !GUILayout.LiteralLabels.Contains("1회성 확인 불가: 첫 이벤트의 근거 없음"),
                "Changing event selection replaces the cached one-shot explanation");
            renderer.OneShotEventDetail = target => ""; Repaint(renderer);
            Check(GUILayout.LiteralLabels.All(value => !value.StartsWith("1회성", StringComparison.Ordinal)),
                "An empty or disabled proof cache immediately removes its event explanation");
            Check(LogBridge.Reservations == reservations, "Reading one-shot details never changes reservations");
        }
        finally { ConfigureLayout(saved, font); }
    }
}
