using System;
using System.Collections.Generic;
using DiceMaster.LogUI;
using DiceMaster.Gamepad;
using DiceMaster.LiveLog;
using UnityEngine;
internal static partial class Program
{
    private static void RendererInformationPages()
    {
        Rect old = ConfiguredRect(); int font = DiceMaster.Config.ConfigManager.LogFontSize.Value;
        try
        {
            ConfigureLayout(new Rect(20, 20, 1100, 850), 20);
            foreach (var page in new[] { "지도 안내", "분기 기록", "상점 효과" })
            {
                var model = new LogModel(); model.ResetContext(6000);
                var entries = new LogItem[30];
                for (int i = 0; i < entries.Length; i++) entries[i] = new LogItem { Id = "info-" + i,
                    Title = "긴 한국어 정보 항목 " + i, Description = new string('가', 2000),
                    Text = "확인 불가", ReservationAllowed = false, Target = null };
                Publish(model, page, entries);
                int closes = 0, toggled = -1, reservations = LogBridge.Reservations;
                var renderer = new LogRenderer(model, null, () => closes++);
                renderer.InformationSettingLabel = i => "정보 설정 " + i;
                renderer.ToggleInformationSetting = i => toggled = i;
                renderer.Handle(LogPadCommand.None); Repaint(renderer);
                renderer.Handle(LogPadCommand.Tools);
                int tool = 19 + Array.IndexOf(new[] { "지도 안내", "분기 기록", "상점 효과" }, page);
                for (int i = 0; i < tool; i++) renderer.Handle(LogPadCommand.Down);
                renderer.Handle(LogPadCommand.Activate); Repaint(renderer);
                Check(renderer.InformationPage == page && renderer.CurrentTab == "선택지", page + ": tool opens readonly page while retaining original tab");
                renderer.Handle(LogPadCommand.Down); Check(model.Tab(page).SelectedId == "info-0", page + ": left-stick/dpad selects information");
                renderer.Handle(LogPadCommand.Activate); renderer.Handle(LogPadCommand.Activate);
                Check(LogBridge.Reservations == reservations && !model.Tab(page).CanReserveSelected, page + ": confirm never creates gameplay authority");
                for (int i = 0; i < 20; i++) renderer.Handle(LogPadCommand.Down);
                Repaint(renderer); Check(model.Tab(page).SelectedId == "info-20" && model.Tab(page).ListScroll > 0, page + ": long list follows selection");
                renderer.Scroll(1); Check(model.Tab(page).DetailScroll > 0, page + ": right stick reads full detail");
                renderer.Handle(LogPadCommand.Back); Check(renderer.InformationPage == null && Area(renderer) == "Tools" && closes == 0, page + ": B returns to tools without closing or game action");
                SetField(renderer, "toolIndex", 22); renderer.Handle(LogPadCommand.Activate); Check(toggled == 0, "first new setting uses existing tool activation");
                renderer.Handle(LogPadCommand.Back); Check(renderer.CurrentTab == "선택지" && closes == 0, "second B returns to retained tab");
                renderer.Handle(LogPadCommand.Tools); SetField(renderer, "toolIndex", tool); renderer.Handle(LogPadCommand.Activate);
                renderer.Handle(LogPadCommand.NextTab); Check(renderer.InformationPage == null && renderer.CurrentTab == "조건", "tab input leaves extra readonly view");
                renderer.Handle(LogPadCommand.Tools); SetField(renderer, "toolIndex", tool); renderer.Handle(LogPadCommand.Activate);
                renderer.ResetInputFocus(false, true); Check(renderer.InformationPage == page, "controller reacquisition preserves readonly view with neutral boundary");
                Repaint(renderer);
                Check(!GUILayout.Controls.Exists(c => c.Text.Contains("예약 가능", StringComparison.Ordinal)), "readonly view has no reservation control");
            }
        }
        finally { ConfigureLayout(old, font); }
    }
}
