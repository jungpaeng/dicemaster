using System;
using System.Collections;
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
    private static void DirectChoiceCases()
    {
        foreach (int earlierTopics in new[] { 31, 32, 64 })
        {
            Reset(); var counter = EntryCounter("late_discovered_entry", 0);
            ChoiceStorySource.ObserveScript("local a={'O_entry'};local r=choose(a);if r==1 then local s=checkpointmanager.Switch('late_discovered_entry');if s==1 then " + EntryNpc + " end end", "direct_entry.LuaScript");
            var option = Normal("O_entry", "현재 인물"); EventSystem.current.currentSelectedGameObject = option.gameObject;
            var initial = Linked(Ready(true), "현재 인물");
            Check(initial.AutoText.Length > 0, "the baseline entry has a real current NPC witness before descendant saturation");
            var owner = typeof(ChoiceStorySource);
            var bindingsMap = (IDictionary)owner.GetField("bindings", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            object old = bindingsMap["O_entry"]; Type type = old.GetType();
            object Field(object target, string name) => type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
            var original = ((List<MapRoutePath>)Field(old, "Paths")).Single();
            var story = ((Dictionary<MapRoutePath, MapStoryEntry>)Field(old, "PathStories"))[original];
            object staged = Activator.CreateInstance(type, true);
            var oldNodes = (IDictionary)Field(old, "Nodes"); var newNodes = (IDictionary)Field(staged, "Nodes");
            foreach (DictionaryEntry node in oldNodes) newNodes.Add(node.Key, node.Value);
            bindingsMap["O_entry"] = staged;
            var register = owner.GetMethod("AddStoryPath", BindingFlags.NonPublic | BindingFlags.Static);
            var yes = PreviewConditions.Constant(true); var no = PreviewConditions.Constant(false);
            for (int at = 0; at < earlierTopics; at++)
            {
                var child = new MapChoiceStep("locked_menu", at, 1, "O_locked_" + at, no, no, false, no, false, no, true);
                var path = new MapRoutePath("locked_" + at, "locked", true, true, yes, new[] { original.Steps[0], child }, yes, false, "brother4", () => false, yes, true, false, yes);
                var topic = new MapStoryEntry("topic_" + at, "talk_brother4_option_01", at, "D_locked", child.RawLabel, () => false);
                register.Invoke(null, new object[] { staged, path, topic, original.Steps[0] });
            }
            // Replay only definition discovery order, not current game state.
            // The actual root witness arrives after more than the read bound.
            register.Invoke(null, new object[] { staged, original, story, original.Steps[0] });
            var current = Read(true); var row = Linked(current, "현재 인물");
            Check(row.AutoText.Length > 0 && row.Detail.Contains("표시 근거: direct_entry · D_entry", StringComparison.Ordinal),
                "a late valid direct entry is retained after " + earlierTopics + " earlier locked descendants without raising the 32-path query bound");
            Check(!row.AutoText.Contains("O_locked", StringComparison.Ordinal) && row.Detail.Contains("직접 대사 우선 배치: 1개", StringComparison.Ordinal) && !row.Opportunity,
                "blocked future topics cannot replace the direct witness or grant an opportunity marker");
            Check(ReferenceEquals(ChoiceStorySource.CurrentRow(current), row), "the retained entry follows passive native pad focus");
            counter._count = 1;
            Check(!Read().Entries.Any(x => x.Title == "현재 인물" && x.AutoText.Length > 0),
                "priority cannot revive an already completed entry behind locked descendants");
            counter._count = 0;
            Check(Linked(Read(), "현재 인물").AutoText.Length > 0, "fresh state restores an unseen entry using the same prepared priority");
            counter._countItems = null;
            Check(!Read().Entries.Any(x => x.Title == "현재 인물" && x.AutoText.Length > 0), "priority never turns unavailable completion data into positive evidence");
            counter._countItems = new List<Mortal.Core.ValueCompareItem> { new Mortal.Core.ValueCompareItem { _compareOp = 0, _value = 1 } };
            int reads = DataHelper.Reads, parses = DiceMaster.LuaParse.LuaParser.Parses;
            for (int at = 0; at < 60; at++) Read();
            Check(DataHelper.Reads == reads && DiceMaster.LuaParse.LuaParser.Parses == parses && counter._count == 0 && Counters.GameCalls == 0,
                "warm priority reads neither rescan/reparse resources nor execute a counter, condition, RNG or flag write");
        }
    }
}
