using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using UnityEngine.EventSystems;

internal static partial class Program
{
    private static MapRouteDefinitions ChoiceRoutes()
    { return (MapRouteDefinitions)typeof(ChoiceStorySource).GetField("routes", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null); }
    private static string LocalEntryBody(string prefix = "")
    { return prefix + "local a={'O_entry'};local r=choose(a);if r==1 then local s=checkpointmanager.Switch('local_entry');if s==1 then " + EntryNpc + " end;luamanager.SetNextScript('slow_topics');luamanager.Init() end"; }
    private static void LocalPreparationCases()
    {
        foreach (bool missing in new[] { false, true })
        {
            Reset(); MapRouteDefinitions.ClearSharedSyntaxCache(); var count = EntryCounter("local_entry", 0);
            var unlocked = Condition("future_topic", 1);
            if (!missing) DataHelper.Scripts["slow_topics"] = new string(';', 16000) +
                "local a={};a[1]=checkpointmanager.Condition('future_topic') and 'O_other' or '~x';local r=choose(a);if r==1 then setcharacter(characters.Get('brother4'));say(luamanager.GetStoryText('D_future')) end";
            ObserveCompiled(LocalEntryBody(), "local_first.LuaScript"); var button = Normal("O_entry", "현재 인물");
            EventSystem.current.currentSelectedGameObject = button.gameObject;
            ChoiceStorySnapshot value = null; int first = -1, completed = -1, lastWork = 0, lastSources = 0;
            for (int at = 0; at < 100; at++)
            {
                value = Read(true); var graph = ChoiceRoutes();
                Check(graph.WorkCount - lastWork <= 8192 && graph.ScriptReadCount - lastSources <= 1,
                    "local and downstream phases share the original work and one-resource budget on every refresh");
                lastWork = graph.WorkCount; lastSources = graph.ScriptReadCount;
                var row = value.Entries.FirstOrDefault(x => x.Title == "현재 인물");
                if (row?.AutoText.Length > 0 && first < 0)
                {
                    first = at;
                    Check(graph.RootPreparationComplete && graph.HasPendingWork && DataHelper.Reads == 0,
                        "direct greeting is published after the entire current script closes but BEFORE any downstream resource is read");
                    Check(row.Detail.Contains("표시 근거: local_first · D_entry", StringComparison.Ordinal) &&
                        row.Detail.Contains("직접 대화 준비는 끝났습니다", StringComparison.Ordinal) &&
                        !row.Available && !row.Opportunity && !row.AutoText.Contains("→", StringComparison.Ordinal),
                        "early output names only its real direct witness, reports pending descendants, and grants no full-route or opportunity confidence");
                    Check(ReferenceEquals(ChoiceStorySource.CurrentRow(value), row), "early direct tooltip follows the existing native pad focus without hover");
                }
                Check(row == null || !row.AutoText.Contains("다른 이야기", StringComparison.Ordinal), "a locked downstream topic cannot leak into either early or completed preview");
                if (!graph.HasPendingWork && completed < 0) completed = at;
                if (missing && at >= 4) break;
                if (!missing && completed >= 0) break;
            }
            Check(first >= 0 && (missing || completed > first), "the first direct paint does not wait for slow or failing descendant preparation");
            count._count = 1; value = Read(true);
            Check(!value.Entries.Any(x => x.Title == "현재 인물" && x.AutoText.Length > 0) && ChoiceStorySource.CurrentRow(value) == null,
                "completion immediately removes the early direct greeting, including while a failed child remains pending");
            count._count = 0;
            Check(Linked(Read(true), "현재 인물").AutoText.Length > 0, "an unseen counter state is freshly sampled instead of cached with root readiness");
            count._countItems = null;
            Check(!Read().Entries.Any(x => x.Title == "현재 인물" && x.AutoText.Length > 0), "local readiness cannot substitute for unreadable completion evidence");
            count._countItems = new List<ValueCompareItem> { new ValueCompareItem { _compareOp = 0, _value = 1 } };
            if (!missing)
            {
                unlocked.State = 0; var row = Linked(Read(), "현재 인물");
                Check(row.AutoText.Contains("→ 다른 이야기", StringComparison.Ordinal), "a genuinely unlocked downstream topic is added after full preparation and fresh unlock checking");
                int reads = DataHelper.Reads, parses = DiceMaster.LuaParse.LuaParser.Parses;
                for (int at = 0; at < 30; at++) Read();
                Check(DataHelper.Reads == reads && DiceMaster.LuaParse.LuaParser.Parses == parses,
                    "completed local and downstream definitions remain warm without per-tick source parsing");
            }
            else
            {
                int reads = DataHelper.Reads;
                for (int at = 0; at < 4; at++) Read();
                Check(DataHelper.Reads == reads && ChoiceRoutes().HasReadFailure && Linked(Read(), "현재 인물").AutoText.Length > 0,
                    "an unrelated missing child observes the original five-second retry and cannot erase a safe local tooltip");
            }
            Check(count._count == 0 && Counters.GameCalls == 0, "all local readiness and state transitions execute no game condition, counter, RNG or input mutation");
        }

        Reset(); EntryCounter("local_entry", 0);
        DataHelper.Scripts["slow_topics"] = new string(';', 16000);
        string duplicate = LocalEntryBody() + new string(';', 12000) + "local b={'O_entry'};local n=choose(b);if n==1 then " + EntryNpc + " end";
        ChoiceStorySource.ObserveScript(duplicate, "late_local_duplicate.LuaScript"); Normal("O_entry", "중복 원문");
        bool sawPending = false;
        for (int at = 0; at < 100; at++)
        {
            var value = Read(true); var graph = ChoiceRoutes(); sawPending |= graph.HasPendingWork;
            Check(!value.Entries.Any(x => x.Title == "중복 원문" && x.AutoText.Length > 0),
                "a duplicate menu after a long root tail prevents every provisional direct tooltip");
            if (!graph.HasPendingWork) break;
        }
        Check(sawPending, "the late duplicate regression actually crosses preparation boundaries");

        Reset(); Condition("absent_character", 1); EntryCounter("local_entry", 0);
        DataHelper.Scripts["slow_topics"] = new string(';', 16000);
        string blocked = "local a={'O_entry'};local r=choose(a);if r==1 then if checkpointmanager.Condition('absent_character') and checkpointmanager.Switch('unknown_random')==1 then " + EntryNpc +
            " end;luamanager.SetNextScript('slow_topics');luamanager.Init() end";
        ChoiceStorySource.ObserveScript(blocked, "false_and_unknown.LuaScript"); Normal("O_entry", "확정 불가 대화");
        for (int at = 0; at < 80; at++)
            Check(!Read().Entries.Any(x => x.Title == "확정 불가 대화" && x.AutoText.Length > 0), "local preparation preserves false AND unknown and never draws a random value");

        Reset(); EntryCounter("local_entry", 0); DataHelper.Scripts["slow_topics"] = new string(';', 16000);
        ChoiceStorySource.ObserveScript(LocalEntryBody("local function hidden_menu() local b={'O_entry'};local n=choose(b) end;"), "skipped_body.LuaScript"); Normal("O_entry", "숨은 메뉴 미확정");
        bool sawUnclosed = false;
        for (int at = 0; at < 80; at++)
        {
            var value = Read(true); var graph = ChoiceRoutes();
            if (graph.HasPendingWork)
            {
                sawUnclosed = true;
                Check(!graph.RootPreparationComplete && !value.Entries.Any(x => x.Title == "숨은 메뉴 미확정" && x.AutoText.Length > 0),
                    "a skipped root body cannot claim complete local menu coverage or an early tooltip");
            }
            else break;
        }
        Check(sawUnclosed, "the skipped-body coverage test actually has pending descendants");
        Reset(); EntryCounter("local_entry", 0); DataHelper.Scripts["slow_topics"] = new string(';', 16000);
        ChoiceStorySource.ObserveScript(LocalEntryBody() + "local b={};b[1]=unknown_label();local n=choose(b)", "dynamic_local_menu.LuaScript"); Normal("O_entry", "동적 원문 미확정");
        for (int at = 0; at < 80; at++)
        {
            var value = Read(true); var graph = ChoiceRoutes();
            if (!graph.HasPendingWork) break;
            Check(!graph.RootPreparationComplete && !value.Entries.Any(x => x.Title == "동적 원문 미확정" && x.AutoText.Length > 0),
                "an unreadable later option label cannot prove current-script menu uniqueness for early publication");
        }
        var capped = new MapRouteDefinitions("capped", _ => ScriptReadResult.Success(LocalEntryBody()), null, new MapRouteLimits { MaxWork = 1 }) { PrioritizeRootPreparation = true };
        capped.Advance(0);
        Check(!capped.RootPreparationComplete && !capped.HasPendingWork && capped.IsPartial, "a root work cap never masquerades as completed menu identity");
        capped.Clear(); Check(!capped.RootPreparationComplete, "context cleanup invalidates local readiness together with every deferred job");
    }
}
