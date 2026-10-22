using System;
using System.Linq;
using DiceMaster;
using DiceMaster.Information;
using Mortal.Core;
using UnityEngine;

internal static partial class Program
{
    private static void LoadingCases()
    {
        Reset();
        ObserveCompiled(new string(';', 16000) + "local a={'O_person','O_leave'};local r=choose(a);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_loading')) end", "loading_menu.LuaScript");
        var person = Normal("O_person", "인물"); var leave = Normal("O_leave", "떠나다");
        var value = Read();
        Check(value.Preparing.Contains(person) && value.Preparing.Contains(leave), "all actual unselected buttons publish pending anchors while the current menu is still preparing");
        bool ready = false;
        for (int at = 0; at < 90; at++)
        {
            value = Read();
            Check(value.Preparing.All(anchor => !value.Entries.Any(row => ReferenceEquals(anchor, row.Anchor) && row.HasBadge)), "choice badges and loading are mutually exclusive in the same publication");
            if (value.Preparing.Count == 0 && value.Entries.Any(row => ReferenceEquals(row.Anchor, person) && row.HasBadge)) { ready = true; break; }
        }
        Check(ready && !value.Entries.Any(row => ReferenceEquals(row.Anchor, leave)), "settled empty buttons stop loading while the real dialogue becomes a light badge");
        Check(value.Anchors.Contains(person) && value.Anchors.Contains(leave), "tooltip obstacles include empty native choices independently of their story results");
        ChoiceStorySource.Interactive = control => !ReferenceEquals(control, leave);
        Check(Read().Anchors.Contains(leave), "disabled native choices remain tooltip obstacles");
        leave.gameObject.activeInHierarchy = false;
        Check(!Read().Anchors.Contains(leave), "hidden native choices stop being tooltip obstacles on the next scheduled read");
        leave.gameObject.activeInHierarchy = true; ChoiceStorySource.Interactive = _ => true;
        int reads = DataHelper.Reads;
        for (int at = 0; at < 30; at++) Check(Read().Preparing.Count == 0, "normal quarter-second refresh does not restart choice loading");
        Check(DataHelper.Reads == reads && Counters.GameCalls == 0, "choice loading never initiates extra source reads or game calls");
        Reset();
        ObserveCompiled("local a={'O_person'};local r=choose(a);if r==1 then luamanager.SetNextScript('missing_loading_tail');luamanager.Init() end", "loading_fail.LuaScript");
        Normal("O_person", "인물");
        for (int at = 0; at < 15; at++) value = Read();
        Check(ChoiceRoutes().HasReadFailure && value.Preparing.Count == 0 && !value.Entries.Any(row => row.HasBadge), "a missing child stops spinning and retains the separate failure/retry diagnostic");
        Check(value.Entries.Any(row => row.Status.Contains("실패", StringComparison.Ordinal)), "failure reason remains available in the map log");
        Reset();
    }
}
