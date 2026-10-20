using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using DiceMaster.Information;
using DiceMaster.LiveLog;

internal static class Program
{
    private static int checks;
    private static readonly Dictionary<string, object> targets = new Dictionary<string, object>(StringComparer.Ordinal);
    private static void Check(bool value, string label)
    { checks++; if (!value) throw new Exception(label); }
    private static IList<MapRouteEvent> Pool(string key)
    {
        object target; if (!targets.TryGetValue(key, out target)) targets.Add(key, target = new object());
        return new[] { new MapRouteEvent(target, key.ToLowerInvariant() + "_result", key) };
    }
    private static MapRouteDefinitions Build(string source, Dictionary<string, string> scripts = null, MapRouteLimits limits = null)
    {
        scripts = scripts ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); scripts["root"] = source;
        var d = new MapRouteDefinitions("root", key => scripts.ContainsKey(key) ? ScriptReadResult.Success(scripts[key]) : key.EndsWith("_result", StringComparison.Ordinal) ? ScriptReadResult.Success("") : ScriptReadResult.Missing(), Pool, limits);
        for (int i = 0; i < 10000 && d.IsPreparing; i++) d.Advance(0, 128);
        return d;
    }
    private static string Use(string pool) { return "local r=checkpointmanager.Position(\"" + pool + "\"); luamanager.SetNextScript(r)"; }
    private static void Main()
    {
        var manager = Mortal.Story.CheckPointManager.Instance = new Mortal.Story.CheckPointManager();
        var condition = new Mortal.Core.ConditionResultData { name = "gate", Current = true };
        manager._condition._list.Add(condition);
        var d = Build(Use("pool"));
        Check(d.Entries.Count == 1 && d.Entries[0].Pool == "pool", "Position result consumed as exact symbolic pool");
        Check(d.Entries[0].Current == MapRouteState.Met, "pure ungated route currently met");
        Check(!d.IsPreparing && !d.IsPartial, "simple route prepared completely");
        int reads = d.ScriptReadCount, parses = d.ParseCount, work = d.WorkCount;
        for (int i = 0; i < 100; i++) d.Advance(i);
        Check(d.ScriptReadCount == reads && d.ParseCount == parses && d.WorkCount == work, "warm refresh never reloads/reparses");
        Check(Build("local r=checkpointmanager.Position('unused')").Entries.Count == 0, "unconsumed Position not a terminal script");
        Check(Build("local r=checkpointmanager.Position('old'); r=''; luamanager.SetNextScript(r)").Entries.Count == 0, "overwritten Position return discarded");
        Check(Build("local a=checkpointmanager.Position('pool'); local b=a; luamanager.SetNextScript(b)").Entries.Count == 1, "scoped symbolic alias");
        Check(Build("luamanager.SetNextScript(checkpointmanager.Position('pool'))").Entries.Count == 1, "nested symbolic result expression");
        var scripts = new Dictionary<string, string> { { "a", Use("old") }, { "b", Use("new") } };
        d = Build("luamanager.SetNextScript('a'); luamanager.SetNextScript('b')", scripts);
        Check(d.Entries.Count == 1 && d.Entries[0].Pool == "new", "last registration wins");
        Check(Build("luamanager.SetNextScript('a'); luamanager.SetNextScript('')", scripts).Entries.Count == 0, "empty cancels");
        Check(Build("luamanager.SetNextScript('a'); luamanager.SetNextScript(dynamic)", scripts).Entries.Count == 0, "unknown replacement discards old target");
        Check(Build("luamanager.SetNextScript('a'); return; luamanager.SetNextScript('b')", scripts).Entries[0].Pool == "old", "return retains its own prior registration");
        Check(Build("return; " + Use("unreachable")).Entries.Count == 0, "unreachable after return excluded");
        Check(Build("function unused() " + Use("function") + " end").Entries.Count == 0, "unused function excluded");
        Check(Build("local function unused() " + Use("function") + " end").Entries.Count == 0, "unused local function excluded");
        Check(Build("local unused=function() " + Use("function") + " end").Entries.Count == 0, "unused anonymous function excluded");
        Check(Build("local a,b=checkpointmanager.Position('wrong');luamanager.SetNextScript(b)").Entries.Count == 0, "multiple return assignment never promotes wrong b to pool");
        d = Build("local x=\"same\";if x=='same' then " + Use("yes") + " else " + Use("wrong") + " end");
        Check(d.Entries.Count == 1 && d.Entries[0].Pool == "yes", "quoted literals compare by decoded value");
        d = Build("local x=1;if x==01 then " + Use("yes") + " else " + Use("wrong") + " end");
        Check(d.Entries.Count == 1 && d.Entries[0].Pool == "yes", "numeric literals compare by numeric value");
        d = Build("luamanager.SetNextScript('a');return luamanager.SetNextScript('b')", scripts);
        Check(d.Entries.Count == 0 && d.IsPartial, "unmodeled return call cannot preserve stale registration");
        d = Build("luamanager.SetNextScript('a');return unknown_callback()", scripts);
        Check(d.Entries.Count == 0 && d.IsPartial, "return callback cannot assert prior registration");
        d = Build("goto done;" + Use("unreachable") + ";::done::");
        Check(d.Entries.Count == 0 && d.IsPartial, "unsupported goto never scans skipped statements as Met");
        d = Build("local choose=function(x)return 1 end;local opts={'A','B'};local r=choose(opts);if r==1 then " + Use("yes") + " else " + Use("wrong") + " end");
        Check(d.Entries.Count == 0 && d.IsPartial, "shadowed choose not treated as trusted game choice");
        d = Build("local opts={'A','B'};opts[1],opts[2]='~x','~x';local r=choose(opts);" + Use("disabled"));
        Check(d.Entries.Count == 0 && d.IsPartial, "multiple option writes never retain enabled arm");
        d = Build("local opts={'A'};local alias=opts;opts[1]='~x';local r=choose(alias);" + Use("disabled"));
        Check(d.Entries.Count == 0, "same-job table alias retains shared identity");
        d = Build("local opts={'A'};local key=1;opts[key]='~x';local r=choose(opts);" + Use("disabled"));
        Check(d.Entries.Count == 0, "constant dynamic option index resolved by value");
        foreach (string options in new[] { "local opts={'~disabled'};", "local opts={'~'};" })
            Check(Build(options + "local r=choose(opts);" + Use("disabled")).Entries.Count == 0, "actual choose ipairs and tilde-prefix disabled rules");
        foreach (string options in new[] { "local opts={};", "local opts={};opts['foo']='A';" })
        {
            d = Build(options + "local r=choose(opts);if r==0 then " + Use("after_empty") + " else " + Use("wrong") + " end");
            Check(d.Entries.Count == 1 && d.Entries[0].Pool == "after_empty" && d.Entries[0].Current == MapRouteState.Met, "empty/nonarray choose returns zero and continues its tail");
        }
        d = Build("local opts={};opts[2]='A';local r=choose(opts);if r==2 then " + Use("unselectable") + " else " + Use("possible_zero_tail") + " end");
        Check(d.Entries.Count == 1 && d.Entries[0].Pool == "possible_zero_tail" && d.Entries[0].Current == MapRouteState.Unknown && d.IsPartial, "sparse length not guessed; possible zero tail retained as unknown");
        d = Build("local opts={};opts['foo']='A';local r=choose(opts);if r=='foo' then " + Use("unselectable") + " end");
        Check(d.Entries.Count == 0, "nonarray option key never returned as a selected index");
        d = Build("local opts={};opts[1]='A';opts[3]='C';local r=choose(opts);if r==3 then " + Use("hole") + " else " + Use("yes") + " end");
        Check(d.Entries.Count == 1 && d.Entries[0].Pool == "yes", "ipairs stops at the first absent array index");
        Check(Build("unknown_callback()").IsPartial, "unsupported call without any route remains incomplete");
        Check(Build("checkpointmanager={}").IsPartial, "manager mutation without any route remains incomplete");
        d = Build("local r=checkpointmanager:Position('colon');luamanager.SetNextScript(r)");
        Check(d.IsPartial && d.Entries.Count == 0, "colon Position has no proved dot-call argument contract");
        d = Build("luamanager.SetNextScript('a');luamanager:SetNextScript('b')", scripts);
        Check(d.IsPartial && d.Entries.Count == 0, "unproved colon registration invalidates prior pending target");
        foreach (string callback in new[] { "local t={unknown_callback()};", "unknown_callback 'argument';", "stage.hide{character=existing};" })
        {
            d = Build(callback + Use("after_callback"));
            Check(d.Entries.Count == 1 && d.Entries[0].Current == MapRouteState.Unknown, "nested/sugar callback is a current-state barrier");
            Check(d.IsPartial, "nested/sugar unsupported call diagnostic preserved");
        }
        Check(Build("-- " + Use("line") + "\n--[==[ " + Use("comment") + " ]==]\nlocal s=[=[" + Use("string") + "]=]; local t='not a call'").Entries.Count == 0, "comments and long strings excluded");
        foreach (string broken in new[] { Use("prefix") + "; --[[ unterminated", Use("prefix") + "; --[==[ missing close", "--[" + new string('=', 64) + "[\n" + Use("comment_body"), Use("prefix") + "; local text='unterminated", Use("prefix") + "; function incomplete()" })
        {
            d = Build(broken);
            Check(d.IsPartial && d.Entries.All(x => x.Current == MapRouteState.Unknown), "malformed/oversized lexical boundary never proves prefix or comment-body route");
        }
        Check(Build("checkpointmanager={}; " + Use("spoof")).Entries.Count == 0, "reassigned manager excluded");
        Check(Build("checkpointmanager.Position=other; " + Use("spoof")).Entries.Count == 0, "method write excluded");
        Check(Build("while true do " + Use("loop") + " end").Entries.Count == 0, "loop body not unrolled");
        d = Build("if false then " + Use("false") + " else " + Use("true") + " end");
        Check(d.Entries.Count == 1 && d.Entries[0].Pool == "true", "pure unreachable branch excluded");
        d = Build("local ok=checkpointmanager.Condition('gate'); if ok then " + Use("yes") + " else " + Use("no") + " end");
        Check(d.Entries.Count == 2, "conditional routes retained");
        Check(d.Entries.First(x => x.Pool == "yes").Current == MapRouteState.Met, "current condition met through actual safe reader");
        Check(d.Entries.First(x => x.Pool == "no").Current == MapRouteState.NotMet, "elseif exclusion uses negated previous guard");
        condition.Current = false;
        Check(d.Entries.First(x => x.Pool == "yes").Current == MapRouteState.NotMet && d.Entries.First(x => x.Pool == "no").Current == MapRouteState.Met, "current values never enter definition cache");
        condition.Current = true;
        d = Build("unknown_callback(); if checkpointmanager.Condition('gate') then " + Use("unknown") + " end");
        Check(d.Entries.Count == 1 && d.Entries[0].Current == MapRouteState.Unknown, "callback barrier prevents future current-state assumption");
        d = Build("local opts={}; opts[1]='A'; opts[2]='B'; local result=choose(opts); if result==1 then --index:O_alias\n" + Use("a") + " elseif result==2 then " + Use("b") + " end");
        Check(d.Entries.Count == 2 && d.Entries.All(x => x.Paths.Count == 1 && x.HasChoice), "choose arms canonical, marker creates no alias route");
        d = Build("local opts={'A','B'}; local result=choose(opts); if result==1 then luamanager.SetNextScript('a') else luamanager.SetNextScript('a') end", scripts);
        Check(d.Entries.Count == 1 && d.Entries[0].Paths.Count == 2, "shared descendant deduplicates event but retains distinct paths");
        d = Build("local r=checkpointmanager.Position('outer'); do local r=checkpointmanager.Position('inner') end; luamanager.SetNextScript(r)");
        Check(d.Entries.Count == 1 && d.Entries[0].Pool == "outer", "block local shadow restored");
        d = Build("luamanager.SetNextScript('root')");
        Check(d.IsPartial && !d.HasReadFailure && d.Entries.Count == 0, "cycle finite diagnostic");
        var overflow = new MapRouteEntry(Pool("overflow")[0], "overflow");
        for (int i = 0; i < 32; i++) Check(overflow.Add(new MapRoutePath(i.ToString(), "known false", false, false, PreviewConditions.Constant(false))), "bounded path admission");
        Check(overflow.Current == MapRouteState.NotMet, "complete all-false paths may be unavailable");
        Check(!overflow.Add(new MapRoutePath("33", "unknown omitted", false, true, PreviewConditions.Unknown)), "overflow explicitly reported");
        Check(overflow.Paths.Count == 32 && overflow.Current == MapRouteState.Unknown, "omitted path cannot become global unavailability");
        d = Build("luamanager.SetNextScript('missing')");
        Check(d.HasReadFailure && d.IsPartial, "missing resource not successful zero");
        reads = d.ScriptReadCount; d.Advance(4.99f); Check(d.ScriptReadCount == reads, "read failure blocked before five seconds");
        d.Advance(5); Check(d.ScriptReadCount == reads + 1, "read failure retry at five seconds");
        bool ready = false; int poolReads = 0;
        d = new MapRouteDefinitions("root", key => ScriptReadResult.Success(key == "root" ? Use("pool") : ""), key => { poolReads++; return ready ? Pool(key) : null; });
        for (int i = 0; i < 1000 && d.IsPreparing; i++) d.Advance(0, 64);
        Check(d.HasReadFailure && poolReads == 1, "pool preparation failure isolated");
        d.Advance(4); Check(poolReads == 1, "pool retry not per healthy tick"); ready = true; d.Advance(5);
        Check(!d.HasReadFailure && d.Entries.Count == 1, "pool recovers at owned retry");
        d = new MapRouteDefinitions("root", key => ScriptReadResult.Success(key == "root" ? new string(' ', 10000) + Use("late") : ""), Pool);
        d.Advance(0, 17); Check(d.IsPreparing && d.Entries.Count == 0 && d.WorkCount <= 17, "cold lexical preparation is resumable including whitespace");
        for (int i = 0; i < 5000 && d.IsPreparing; i++) { int before = d.WorkCount; d.Advance(0, 17); Check(d.WorkCount - before <= 17, "each preparation slice bounded"); }
        Check(d.Entries.Count == 1, "resumed preparation reaches same terminal");
        d = Build(Use("limit"), null, new MapRouteLimits { MaxWork = 8 });
        Check(d.IsPartial && !d.IsPreparing && d.Entries.Count == 0, "work limit diagnostic, never successful empty");
        d = Build(Use("limit"), null, new MapRouteLimits { MaxScriptTokens = 4 });
        Check(d.IsPartial && d.Entries.Count == 0, "lexical token limit safe prefix");
        string largeEnvironment = "";
        for (int i = 0; i < MapRouteLimits.MaxBindings + 1; i++) largeEnvironment += "local v" + i + "=1;";
        d = Build(largeEnvironment + Use("not_admitted"));
        Check(d.IsPartial && d.Entries.Count == 0, "environment cap rejects work before large copies");
        string manyArms = "if unknown==1 then " + Use("not_admitted");
        for (int i = 2; i <= MapRouteLimits.MaxBranchArms + 1; i++) manyArms += " elseif unknown==" + i + " then " + Use("not_admitted");
        d = Build(manyArms + " end");
        Check(d.IsPartial && d.Entries.Count == 0, "arm count rejected before per-arm environment clone");
        string longHeader = "if " + string.Join(" and ", Enumerable.Repeat("true", MapRouteLimits.MaxHeaderTokens)) + " then " + Use("not_admitted") + " end";
        d = Build(longHeader);
        Check(d.IsPartial && d.Entries.Count == 0, "oversized condition rejected before parser/clone work");
        string hugeChoice = "local opts={" + string.Join(",", Enumerable.Repeat("'A'", MapRouteLimits.MaxChoiceArms + 1)) + "};local r=choose(opts);" + Use("not_admitted");
        d = Build(hugeChoice);
        Check(d.IsPartial && d.Entries.Count == 0, "choice arm cap bounds eager cloning");
        Check(Build("luamanager.SetNextScript('../outside')").Entries.Count == 0, "local resource path restriction");
        var follow = new Dictionary<string, string> { { "first_result", "luamanager.SetNextScript('submenu')" }, { "submenu", Use("second") } };
        d = Build(Use("first"), follow);
        Check(d.Entries.Count == 2 && d.Entries.Any(x => x.Pool == "second"), "terminal result to literal submenu to next pool followed");
        Check(d.Entries.First(x => x.Pool == "second").Current == MapRouteState.Unknown, "post-event descendants never inherit current state certainty");
        d = Build(Use("clear")); d.Clear(); d.Advance(100); Check(d.Entries.Count == 0 && !d.IsPreparing, "lifecycle clear releases all route state");
        SharedGateAndProofBudget();
        MapStoryCases.Run(Check);
        FullStoryConditionCases.Run(Check);
        PartialConditionCases.Run(Check);
        CurrentBlockerCases.Run(Check);
        Corpus();
        Check(manager.GameplayCalls == 0 && manager.RngCalls == 0 && manager.Counter == 0, "no gameplay condition/Position/RNG/counter changes");
        Console.WriteLine("PASS: MapRouteTests " + checks + " actual-route/syntax/current-reader assertions; resource/stat/game boundaries are explicit fixtures; Lua/Windows/native not executed.");
    }
    private static void SharedGateAndProofBudget()
    {
        var d = new MapRouteDefinitions("root", key => ScriptReadResult.Success(key == "root" ? "if checkpointmanager.Condition('gate') then " + Use("shared") + " end" : ""),
            key => Enumerable.Range(0, 32).Select(i => new MapRouteEvent(new object(), "shared_result", "shared/" + i)).ToArray());
        for (int i = 0; i < 1000 && d.IsPreparing; i++) d.Advance(0, 128);
        int before = SafeDisplayEvaluation.ConditionReads;
        foreach (var entry in d.Entries) Check(entry.Current == MapRouteState.Met, "independent legacy-shaped current read");
        int separate = SafeDisplayEvaluation.ConditionReads - before;
        before = SafeDisplayEvaluation.ConditionReads; var snapshot = new MapRouteRead();
        foreach (var entry in d.Entries) Check(entry.ReadCurrent(snapshot) == MapRouteState.Met, "shared snapshot preserves current route state");
        Check(separate == 32 && SafeDisplayEvaluation.ConditionReads - before == 1, "production shared-tail session reduces 32 repeated checkpoint condition reads to one");
        var bodies = new Dictionary<string, bool>
        {
            { "statmodifymanager.SetFlag('f',1)", true },
            { "statmodifymanager.SetFlag('f',1);statmodifymanager.AddBook('b',1)", true },
            { "if true then statmodifymanager.SetFlag('f',1) end", false },
            { "statmodifymanager.SetFlag('f',1);luamanager.SetNextScript('later')", false },
            { "statmodifymanager.SetFlag('f',unknown_callback())", false },
            { "statmodifymanager:SetFlag('f',1)", false },
            { new string(' ', 8200) + "statmodifymanager.SetFlag('f',1)", false },
            { "function unused() statmodifymanager.SetFlag('f',1) end", false }
        };
        foreach (var pair in bodies)
        {
            d = Build(Use("proof"), new Dictionary<string, string> { { "proof_result", pair.Key } });
            Check(d.CanBuildBoundedProof("proof_result") == pair.Value, "passive proof only admits small closed straight-line effects");
            int parses = d.ParseCount;
            bool identical = true;
            for (int i = 0; i < 100; i++) identical &= d.CanBuildBoundedProof("proof_result") == pair.Value;
            Check(identical, "passive proof preflight result reused");
            Check(d.ParseCount == parses, "passive proof checks never build new syntax");
        }
    }
    private static void Corpus()
    {
        string index = Environment.GetEnvironmentVariable("DICEMASTER_CORPUS_INDEX");
        if (string.IsNullOrEmpty(index)) { Console.WriteLine("Real Lua corpus not requested; set DICEMASTER_CORPUS_INDEX for mapped SHA-pinned samples."); return; }
        var scripts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using (var doc = JsonDocument.Parse(File.ReadAllText(index))) foreach (var row in doc.RootElement.EnumerateArray())
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(index), row.GetProperty("path").GetString()));
            Check(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() == row.GetProperty("sha256").GetString(), "mapped corpus SHA");
            Check(row.GetProperty("key").GetString().StartsWith("story/chinesetraditional/", StringComparison.Ordinal), "traditional exact resource key");
            scripts[row.GetProperty("name").GetString()] = System.Text.Encoding.UTF8.GetString(bytes);
        }
        foreach (string root in new[] { "room_001", "door_001_01", "pharmacy_01_01" })
        {
            // Pool targets are explicit fixtures. The menu bodies are real;
            // fixture terminal bodies are empty unless a real indexed name
            // matches. This does not pretend to supply actual game pool data.
            var d = new MapRouteDefinitions(root, key => scripts.ContainsKey(key) ? ScriptReadResult.Success(scripts[key]) : key.EndsWith("_result", StringComparison.Ordinal) ? ScriptReadResult.Success("") : ScriptReadResult.Missing(), Pool);
            for (int i = 0; i < 10000 && d.IsPreparing; i++) d.Advance(0, 4096);
            string expected = root == "room_001" ? "Room_Work_001" : root == "pharmacy_01_01" ? "Pharmacy_Work_001" : "Door_Work_001";
            Check(d.Entries.Any(x => x.Pool == expected), "real corpus symbolic selected-pool " + root);
            Check(d.Entries.All(x => x.Current == MapRouteState.Unknown), "real UI/prelude barriers remain unknown " + root);
            Check(d.Entries.Select(x => x.Id).Distinct().Count() == d.Entries.Count, "real marker/choose aliases not duplicated " + root);
            Console.WriteLine("Real mapped Lua route sample " + root + ": " + d.Entries.Count + " distinct fixture terminals [" + string.Join(", ", d.Entries.Select(x => x.Pool)) + "], " + d.Diagnostic + "; eligibility/one-shot not asserted.");
            if (root == "room_001")
            {
                Check(d.StoryEntries.Any(x => x.Script == "room_001" && x.Paths.Any(p => p.Steps.Count == 1 && p.Steps[0].Index == 2 && p.Steps[0].RawLabel == "talk+talk+brother4")), "real male disciples room second selection exposes its inline brother dialogue");
                Check(d.StoryEntries.All(x => x.Paths.All(p => p.Steps.Count == 0 || p.Steps[0].Script != "room_001" || p.Steps[0].Index != 1)), "real male room sleep branch has zero meaningful NPC story paths");
                Check(d.StoryEntries.Any(x => x.Script == "room_001" && x.Paths.Any(p => p.DialogueCharacter == "brother4" && p.Steps[0].Index == 2)), "real male room brother route keeps explicit NPC source metadata");
                Check(d.StoryEntries.Any(x => x.Script.StartsWith("talk_brother4_option_", StringComparison.Ordinal) && x.Paths.Any(p => p.Steps.Count >= 2 && p.Steps[0].Script == "room_001" && p.Steps[0].Index == 2 && p.Steps[1].Script == "talk_brother4")), "real brother menu literal followup retains both required selections");
                Check(d.StoryEntries.All(x => x.Script != "talk_brother4"), "real brother menu itself is not mislabeled as dialogue");
                Check(d.StoryEntries.All(x => x.Current == MapRouteState.Unknown), "real visual/state-write barriers never become guaranteed current stories");
                Check(d.StoryEntries.Select(x => x.Identity).Distinct().Count() == d.StoryEntries.Count, "real selected story alternatives have unique canonical identities");
                Console.WriteLine("Real mapped selected stories " + root + ": " + d.StoryEntries.Count + " groups in [" + string.Join(", ", d.StoryEntries.Select(x => x.Script).Distinct()) + "]; only static text references and selection metadata inspected.");
            }
            if (root == "door_001_01")
            {
                Check(d.Entries.Select(x => x.Pool).Distinct().Count() >= 7, "real consumed nested destination pools found");
                Check(d.Entries.All(x => x.Pool != "Fortress_Work_001"), "real discarded fortress Position result excluded");
            }
        }
        foreach (string monologue in new[] { "ch1_1_break_option", "ch2_2_3_4_3_break_01" })
        {
            Check(scripts[monologue].Contains("GetStoryText(\"D_", StringComparison.Ordinal) && scripts[monologue].Contains("choose(", StringComparison.Ordinal), "real player monologue counterexample has selected D_ speech " + monologue);
            var d = new MapRouteDefinitions(monologue, key => scripts.ContainsKey(key) ? ScriptReadResult.Success(scripts[key]) : ScriptReadResult.Missing(), Pool);
            for (int i = 0; i < 10000 && d.IsPreparing; i++) d.Advance(0, 4096);
            Check(d.StoryEntries.All(x => x.Script != monologue), "real selected player-only D_ monologue is not an NPC conversation " + monologue);
        }
    }
}
