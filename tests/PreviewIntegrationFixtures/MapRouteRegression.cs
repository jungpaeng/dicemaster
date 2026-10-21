using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DiceMaster
{
    public static partial class PreviewScenario
    {
        // The fixture supplies definitions only. All route traversal, syntax,
        // registration ownership and limit handling execute the candidate IL.
        private static void MapRouteRegression()
        {
            var constructor = Type("Information.MapRouteDefinitions").GetConstructors().Single();
            var parameters = constructor.GetParameters();
            object Callback(System.Type kind, Func<string, object> action)
            {
                var arg = Expression.Parameter(typeof(string), "key");
                return Expression.Lambda(kind, Expression.Convert(Expression.Invoke(Expression.Constant(action), arg),
                    kind.GetGenericArguments()[1]), arg).Compile();
            }
            var scripts = new Dictionary<string, string>(StringComparer.Ordinal) {
                { "root", "local opts={'A','B'}; local n=choose(opts); if n==1 then luamanager.SetNextScript('submenu') else local dropped=checkpointmanager.Position('discarded'); local current=checkpointmanager.Position('Work'); luamanager.SetNextScript(current) end" },
                { "submenu", "local current=checkpointmanager.Position('Rest'); luamanager.SetNextScript(current)" }
            };
            int reads = 0, poolReads = 0;
            object Script(string key) { reads++; return scripts.TryGetValue(key, out string text) ? Read("Success", text) : Read("Missing"); }
            object Pool(string key)
            {
                poolReads++;
                if (!scripts.ContainsKey(key + "_result")) scripts.Add(key + "_result", "");
                var itemType = Type("Information.MapRouteEvent");
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
                list.Add(Activator.CreateInstance(itemType, new object[] { new object(), key + "_result", key + "/entry" }));
                return list;
            }
            object Create(object limits = null) => constructor.Invoke(new object[] {
                "root", Callback(parameters[1].ParameterType, Script), Callback(parameters[2].ParameterType, Pool), limits });
            void Finish(object graph)
            {
                for (int i = 0; i < 5000 && (bool)Member(graph, "IsPreparing"); i++) Invoke(graph.GetType(), graph, "Advance", 0f, 64);
                Check(!(bool)Member(graph, "IsPreparing"), "actual DLL route preparation terminates");
            }
            var route = Create(); Finish(route);
            var entries = Items(route, "Entries").Cast<object>().ToList();
            Check(entries.Count == 2 && entries.Any(x => (string)Member(x, "Pool") == "Work") &&
                entries.Any(x => (string)Member(x, "Pool") == "Rest"), "actual DLL choice/followup/symbolic return reaches both terminals");
            Check(entries.All(x => (string)Member(x, "Pool") != "discarded"), "actual DLL discarded Position return never becomes an event");
            Check(entries.All(x => (bool)Member(x, "HasChoice") && Items(x, "Paths").Count == 1), "actual DLL distinct canonical choice paths");
            int warmReads = reads, warmPools = poolReads, warmParses = (int)Member(route, "ParseCount");
            for (int i = 0; i < 100; i++) Invoke(route.GetType(), route, "Advance", (float)i, 8192);
            Check(reads == warmReads && poolReads == warmPools && (int)Member(route, "ParseCount") == warmParses,
                "actual DLL warm refresh does not reload or reparse definitions");

            scripts["root"] = "local a={'A'};local r=choose(a);if r==1 then luamanager.SetNextScript('registered_story');luamanager.Init() end";
            scripts["registered_story"] = ""; route = Create(); Finish(route);
            var targets = Items(route, "ScriptEntries").Cast<object>().ToList();
            Check(targets.Count == 1 && (string)Member(targets[0], "Script") == "registered_story" && Items(targets[0], "Paths").Count == 1,
                "actual DLL literal adjacent Init records the selected script handoff without executing it");
            Check((bool)Member(route, "HasIncompleteStoryPaths") && !(bool)Member(route, "HasIncompleteOpportunityPaths"),
                "actual DLL keeps future story uncertainty separate from the witnessed immediate registration handoff");
            scripts["root"] = "local a={'A'};local r=choose(a);if r==1 then luamanager.SetNextScript('registered_story') end"; route = Create(); Finish(route);
            Check(Items(route, "ScriptEntries").Count == 0, "actual DLL script registration without execution handoff cannot masquerade as a waiting mission route");

            scripts["root"] = "luamanager.SetNextScript('submenu'); luamanager.SetNextScript('')";
            route = Create(); Finish(route);
            Check(Items(route, "Entries").Count == 0 && !(bool)Member(route, "IsPartial"), "actual DLL empty registration cancels previous target");
            scripts["root"] = "return; local r=checkpointmanager.Position('unreachable'); luamanager.SetNextScript(r)";
            route = Create(); Finish(route);
            Check(Items(route, "Entries").Count == 0, "actual DLL return excludes unreachable pool");
            scripts["root"] = new string(' ', 10000) + "local r=checkpointmanager.Position('late'); luamanager.SetNextScript(r)";
            route = Create(); Invoke(route.GetType(), route, "Advance", 0f, 17);
            Check((bool)Member(route, "IsPreparing") && (int)Member(route, "WorkCount") <= 17, "actual DLL cold lexer yields within work slice");
            var limits = Activator.CreateInstance(Type("Information.MapRouteLimits"));
            limits.GetType().GetField("MaxWork").SetValue(limits, 8);
            route = Create(limits); Finish(route);
            Check((bool)Member(route, "IsPartial") && Items(route, "Entries").Count == 0, "actual DLL exhausted budget is partial rather than successful absence");
            Invoke(route.GetType(), route, "Clear");
            Check(Items(route, "Entries").Count == 0 && !(bool)Member(route, "IsPreparing"), "actual DLL lifecycle clear releases route state");
            scripts["old"] = "local r=checkpointmanager.Position('old'); luamanager.SetNextScript(r)";
            scripts["new"] = "local r=checkpointmanager.Position('new'); luamanager.SetNextScript(r)";
            void NoFalseMet(string code, string forbidden, string label)
            {
                scripts["root"] = code; var graph = Create(); Finish(graph);
                Check(!Items(graph, "Entries").Cast<object>().Any(x =>
                    (forbidden == "*" || (string)Member(x, "Pool") == forbidden) && Member(x, "Current").ToString() == "Met"), label);
            }
            const string Wrong = "local r=checkpointmanager.Position('wrong'); luamanager.SetNextScript(r)";
            NoFalseMet("local a,b=checkpointmanager.Position('wrong'); luamanager.SetNextScript(b)", "wrong", "actual DLL multiple assignment cannot duplicate first returned value");
            NoFalseMet("local x=\"same\"; if x=='same' then return else " + Wrong + " end", "wrong", "actual DLL string comparison uses decoded literal values");
            NoFalseMet("local x=1; if x==01 then return else " + Wrong + " end", "wrong", "actual DLL numeric comparison uses values rather than spelling");
            NoFalseMet("luamanager.SetNextScript('old'); return luamanager.SetNextScript('new')", "old", "actual DLL return expression cannot preserve an overwritten registration as certain");
            NoFalseMet("goto done; " + Wrong + "; ::done::", "wrong", "actual DLL unsupported jump is not a certain reachable path");
            NoFalseMet("local function choose(x) return 1 end; local opts={'A','B'}; local n=choose(opts); if n==2 then " + Wrong + " end", "*", "actual DLL shadowed choose is not the trusted game menu");
            NoFalseMet("local opts={'~disabled'}; local n=choose(opts); if n==1 then " + Wrong + " end", "wrong", "actual DLL tilde-prefixed option cannot become a selected branch");
            NoFalseMet("local opts={}; opts[2]='A'; local n=choose(opts); if n==2 then " + Wrong + " end", "wrong", "actual DLL sparse table cannot invent an ipairs choice");
            NoFalseMet("luamanager:SetNextScript('old')", "old", "actual DLL unsupported colon invocation cannot become a certain registration");
            NoFalseMet(Wrong + "; --[[ unterminated", "wrong", "actual DLL unterminated long comment cannot publish a certain executable prefix");
            NoFalseMet("--[" + new string('=', 70) + "[\n" + Wrong, "wrong", "actual DLL over-budget long delimiter cannot expose its comment body as executable code");
            scripts["root"] = "local opts={}; local n=choose(opts); if n==0 then local r=checkpointmanager.Position('zero'); luamanager.SetNextScript(r) end";
            route = Create(); Finish(route);
            Check(Items(route, "Entries").Cast<object>().Any(x => (string)Member(x, "Pool") == "zero" && Member(x, "Current").ToString() == "Met" && !(bool)Member(x, "HasChoice")),
                "actual DLL empty menu returns zero and preserves its non-choice continuation");

            // Current-story conditions execute candidate IL against explicit
            // synthetic checkpoint definitions. This is not a claim about the
            // serialized configuration of any named game checkpoint.
            var manager = Mortal.Story.CheckPointManager.Instance;
            var previousConditions = manager._condition;
            int groupCalls = Mortal.Core.StatGroupVariable.OriginalReads, compareCalls = Mortal.Core.StatCompareItem.OriginalReads;
            int conditionCalls = Mortal.Core.ConditionResultData.Executions, randomCalls = UnityEngine.Random.Calls;
            var comparison = new Mortal.Core.StatCompareItem {
                _value1 = new Mortal.Core.StatGroupVariable { _value = new List<Mortal.Core.StatValueReference> { new Mortal.Core.StatValueReference { _checkType = 0, _constant = 0 } } },
                _value2 = new Mortal.Core.StatGroupVariable { _value = new List<Mortal.Core.StatValueReference> { new Mortal.Core.StatValueReference { _checkType = 0, _constant = 1 } } }
            };
            var checkpoint = new Mortal.Core.ConditionResultData { name = "route_story_fixture", _count = 29,
                _items = new List<Mortal.Core.ConditionResultItem> { new Mortal.Core.ConditionResultItem { _items = new List<Mortal.Core.StatCompareItem> { comparison } } } };
            manager._condition = new Mortal.Story.ConditionResultConfig { _list = new List<Mortal.Core.ConditionResultData> { checkpoint } };
            try
            {
                const string Menu = "local opts={'practice+free+practice'};local r=choose(opts);";
                const string Dialogue = "setcharacter(characters.Get('girl5'));say(luamanager.GetStoryText('D_Fixture'))";
                const string Guarded = "if checkpointmanager.Condition('route_story_fixture') then " + Dialogue + " end";
                string StoryState(object graph)
                {
                    object story = Items(graph, "StoryEntries").Cast<object>().Single();
                    return Invoke(story.GetType(), story, "ReadStoryConditions", Activator.CreateInstance(Type("Information.MapRouteRead"))).ToString();
                }
                scripts["root"] = Menu + Guarded; route = Create(); Finish(route);
                Check(StoryState(route) == "NotMet" && !(bool)Member(route, "HasIncompleteStoryPaths"),
                    "actual DLL false internal story gate rejects a plain enabled training option");
                string Guidance(object graph)
                {
                    var witnesses = Items(graph, "StoryPaths");
                    object witness = witnesses[0], path = Member(witness, "Path");
                    Check(witnesses.Count == 1 && ReferenceEquals(Member(witness, "Story"), Items(graph, "StoryEntries")[0]),
                        "actual DLL append-only witness index refers to the original retained story/path");
                    var method = path.GetType().GetMethod("ReadGuidance", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    object[] arguments = { Activator.CreateInstance(Type("Information.MapRouteRead")), -1, true, true, 32, 32,
                        Enum.Parse(Type("Information.MapRouteState"), "Unknown") };
                    return method.Invoke(path, arguments).ToString();
                }
                Check(Guidance(route) == "Conditions", "actual DLL shared map/choice guidance rejects a known false prerequisite");
                comparison._value1._value[0]._constant = 1;
                Check(StoryState(route) == "Met", "actual DLL full-story snapshot updates with readonly current checkpoint data");
                Check(Guidance(route) == "None", "actual DLL shared guidance freshly accepts the changed condition without executing it");
                Check(Items(route, "StoryEntries").Cast<object>().All(x => Member(x, "Current").ToString() == "Unknown"),
                    "actual DLL current-condition proof does not upgrade strict route execution certainty");
                scripts["root"] = Menu + "unknown_callback();" + Dialogue; route = Create(); Finish(route);
                Check(StoryState(route) == "Unknown", "actual DLL opaque callback cannot turn an unconditional NPC story green");
                comparison._value1._value[0]._constant = 0;
                scripts["root"] = Menu + "statmodifymanager.AddFlag('unrelated',1);" + Guarded; route = Create(); Finish(route);
                Check(StoryState(route) == "NotMet", "actual DLL validated checkpoint without flag inputs stays false across a disjoint modeled flag write");
                scripts["root"] = "luamanager.SetNextScript(checkpointmanager.Position('opaque_pool'))";
                scripts["opaque_pool_result"] = Menu + Dialogue;
                route = Create(); Finish(route);
                Check(StoryState(route) == "Unknown", "actual DLL unselected pool result cannot promote a later literal-positive menu story");
                var random = new Mortal.Core.StatCompareItem {
                    _value1 = new Mortal.Core.StatGroupVariable { _value = new List<Mortal.Core.StatValueReference> { new Mortal.Core.StatValueReference { _checkType = 7 } } },
                    _value2 = comparison._value2 };
                var nestedItem = new Mortal.Core.ConditionResultItem { _items = new List<Mortal.Core.StatCompareItem> { random, comparison } };
                var nested = new Mortal.Core.ConditionResultData { name = "nested_dll_fixture", _count = 17,
                    _items = new List<Mortal.Core.ConditionResultItem> { nestedItem } };
                checkpoint._items = new List<Mortal.Core.ConditionResultItem> { new Mortal.Core.ConditionResultItem { _items = new List<Mortal.Core.StatCompareItem> {
                    new Mortal.Core.StatCompareItem {
                        _value1 = new Mortal.Core.StatGroupVariable { _value = new List<Mortal.Core.StatValueReference> { new Mortal.Core.StatValueReference { _checkType = 16, _condition = nested } } },
                        _value2 = comparison._value2 } } } };
                var conditionType = Type("LiveLog.PreviewConditions");
                object expression = Invoke(conditionType, null, "Parse", "checkpointmanager.Condition('route_story_fixture')", null);
                Check(Invoke(conditionType, null, "Current", expression).ToString() == "NotMet", "actual DLL nested RNG AND false resolves to false without executing its getters");
                string diagnostic = (string)Invoke(conditionType, null, "DescribeCurrent", expression);
                Check(diagnostic.Contains("미충족") && diagnostic.Contains("난수 · 추첨하지 않음") && diagnostic.Contains("중첩 조건 = 0"),
                    "actual DLL explicit condition report preserves the nested false evidence and unsampled RNG");
                comparison._value1._value[0]._constant = 1;
                Check(Invoke(conditionType, null, "Current", expression).ToString() == "Unknown", "actual DLL fresh true prerequisite cannot resolve its independent RNG");
                nestedItem._logicOp = (Mortal.Core.LogicOperatorType)1;
                Check(Invoke(conditionType, null, "Current", expression).ToString() == "Met" && nested._count == 17,
                    "actual DLL nested RNG OR true preserves a known true without changing the nested execution counter");
                Check(checkpoint._count == 29 && conditionCalls == Mortal.Core.ConditionResultData.Executions &&
                    groupCalls == Mortal.Core.StatGroupVariable.OriginalReads && compareCalls == Mortal.Core.StatCompareItem.OriginalReads && randomCalls == UnityEngine.Random.Calls,
                    "actual DLL full-story condition reads never call gameplay/RNG or increment checkpoint counters");
            }
            finally { manager._condition = previousConditions; }
            Console.WriteLine("PREVIEW actual map route IL passed; resource and pool definitions are fixtures");
        }
    }
}
