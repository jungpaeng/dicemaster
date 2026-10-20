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
            Console.WriteLine("PREVIEW actual map route IL passed; resource and pool definitions are fixtures");
        }
    }
}
