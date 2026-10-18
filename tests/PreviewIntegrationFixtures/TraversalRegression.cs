using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DiceMaster
{
    // Synthetic graph scenarios invoke copied production IL. The reader only
    // returns strings; it cannot execute Lua, game callbacks or player state.
    public static class TraversalRegression
    {
        static readonly Assembly Self = typeof(TraversalRegression).Assembly;
        static int assertions;
        static Type T(string name) => Self.GetType("DiceMaster.LiveLog." + name, true);
        static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        static IList Items(object value, string name) => (IList)Field(value, name);
        static void Check(bool value, string message) { assertions++; if (!value) throw new InvalidOperationException(message); }
        static object Invoke(MethodInfo method, object owner, params object[] args)
        {
            try { return method.Invoke(owner, args); }
            catch (TargetInvocationException error) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException ?? error).Throw(); throw; }
        }
        static object Read(string state, string source = null) => Invoke(T("ScriptReadResult").GetMethod(state), null, state == "Success" ? new object[] { source } : new object[0]);
        static object Limits(string name, int value)
        {
            object result = Activator.CreateInstance(T("PreviewTraceLimits"));
            result.GetType().GetField(name).SetValue(result, value);
            return result;
        }
        static object Analyze(string script, Func<string, object> reader = null, object limits = null)
        {
            var method = T("PreviewAnalyzer").GetMethod("Analyze");
            object callback = null;
            if (reader != null)
            {
                var input = Expression.Parameter(typeof(string), "name");
                var callbackType = method.GetParameters()[2].ParameterType;
                callback = Expression.Lambda(callbackType,
                    Expression.Convert(Expression.Invoke(Expression.Constant(reader), input), callbackType.GetGenericArguments()[1]), input).Compile();
            }
            return Invoke(method, null, "traversal_root", script, callback, limits);
        }
        static bool HasIssue(object analysis, string reason) => Items(analysis, "Issues").Cast<object>().Any(x => (string)Field(x, "Reason") == reason);
        static List<object> Flags(object analysis, string name) => Items(analysis, "Effects").Cast<object>().Where(x =>
            (string)Field(x, "Owner") == "statmodifymanager" && (string)Field(x, "Name") == "SetFlag" &&
            Items(x, "Arguments").Count > 0 && (string)Items(x, "Arguments")[0] == "\"" + name + "\"").ToList();
        static string Flag(string name) => "statmodifymanager.SetFlag(\"" + name + "\",1)\n";
        static string Next(string name) => "luamanager.SetNextScript(\"" + name + "\")\n";
        static string Fork(string a, string b) => "if gate then\n" + Next(a) + "else\n" + Next(b) + "end\n";
        static Func<string, object> Reader(Dictionary<string, string> scripts, List<string> calls)
        {
            return name => { calls.Add(name); return scripts.TryGetValue(name, out string source) ? Read("Success", source) : Read("Missing"); };
        }
        static void BreadthAndLimits()
        {
            string root = Flag("root") + Fork("deep_a", "near_b");
            var scripts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["deep_a"] = Flag("first_depth") + Next("deep_b"),
                ["near_b"] = Flag("near_neighbor"),
                ["deep_b"] = Flag("second_depth")
            };
            var calls = new List<string>();
            object analysis = Analyze(root, Reader(scripts, calls));
            Check(calls.SequenceEqual(new[] { "deep_a", "near_b", "deep_b" }), "A deep descendant ran before a queued nearer followup");
            Check(Flags(analysis, "root").Count == 1 && Flags(analysis, "near_neighbor").Count == 1 && Flags(analysis, "second_depth").Count == 1, "Breadth traversal lost a reachable synthetic effect");
            var effects = Items(analysis, "Effects").Cast<object>().ToList();
            Check(effects.Select((effect, index) => (int)Field(effect, "Sequence") == index).All(x => x), "Worklist effect sequences are not unique and contiguous");
            Check(effects.Where(effect => (bool)Field(effect, "Followup")).All(effect => (bool)Field(effect, "Conditional")), "Registered followup was promoted to guaranteed execution");

            calls.Clear(); analysis = Analyze(root, Reader(scripts, calls), Limits("MaxScripts", 3));
            Check(calls.SequenceEqual(new[] { "deep_a", "near_b" }), "Finite script budget was spent on a descendant before a nearer neighbor");
            Check((int)Field(analysis, "ScriptCount") == 3 && (int)Field(analysis, "ContextCount") == 3, "Unique-script budget did not include the root exactly once");
            Check(Flags(analysis, "near_neighbor").Count == 1 && Flags(analysis, "second_depth").Count == 0, "Script cutoff erased completed near work or admitted an unread descendant");
            Check(HasIssue(analysis, "trace-script-limit"), "Script cutoff did not retain incomplete status");

            calls.Clear(); analysis = Analyze(root, Reader(scripts, calls), Limits("MaxDepth", 1));
            Check(calls.SequenceEqual(new[] { "deep_a", "near_b" }), "Depth cutoff read a forbidden descendant");
            Check(HasIssue(analysis, "trace-depth-limit") && Flags(analysis, "near_neighbor").Count == 1, "Depth cutoff erased nearer results or lost incomplete status");

            calls.Clear(); analysis = Analyze(root, Reader(scripts, calls), Limits("MaxScripts", 1));
            Check(calls.Count == 0 && (int)Field(analysis, "ScriptCount") == 1, "Root-only script budget read a followup");
            Check(Flags(analysis, "root").Count == 1 && HasIssue(analysis, "trace-script-limit"), "Root-only cutoff did not preserve the parsed root");
        }
        static void BindingContexts()
        {
            var calls = new List<string>();
            object analysis = Analyze(Next("child") + "local statmodifymanager=other\n",
                Reader(new Dictionary<string, string> { ["child"] = Flag("after_parent_rebind") }, calls));
            Check(calls.SequenceEqual(new[] { "child" }), "Parent rebind unexpectedly erased its independent story registration");
            Check(Flags(analysis, "after_parent_rebind").Count == 0 && HasIssue(analysis, "manager-binding-not-evaluated"), "A target inherited binding state from registration time instead of script exit");

            var scripts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["invalid_path"] = "local statmodifymanager=other\n" + Next("shared"),
                ["valid_path"] = Next("shared"),
                ["shared"] = Flag("valid_shared")
            };
            calls.Clear(); analysis = Analyze(Fork("invalid_path", "valid_path"), Reader(scripts, calls));
            Check(Flags(analysis, "valid_shared").Count == 1, "One path's manager rebind hid the same script's valid sibling context");
            Check(calls.Count(name => name == "shared") == 1, "Context expansion reread the same static script");
            Check((int)Field(analysis, "ScriptCount") == 4 && (int)Field(analysis, "ContextCount") == 5, "Distinct binding contexts were merged or charged as distinct script sources");
            Check(!HasIssue(analysis, "trace-cycle"), "Different incoming binding contexts were mistaken for a cycle");

            calls.Clear(); analysis = Analyze(Fork("valid_path", "invalid_path"), Reader(scripts, calls));
            Check(Flags(analysis, "valid_shared").Count == 1 && (int)Field(analysis, "ContextCount") == 5, "Changing sibling order changed the valid shared-script result");
            Check(calls.Count(name => name == "shared") == 1, "Reversed context order bypassed the per-analysis read cache");
        }
        static void DiamondAndCycle()
        {
            var scripts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["left"] = Next("shared"), ["right"] = Next("shared"), ["shared"] = Flag("diamond")
            };
            var calls = new List<string>();
            object analysis = Analyze(Fork("left", "right"), Reader(scripts, calls));
            Check(Flags(analysis, "diamond").Count == 1 && calls.Count(x => x == "shared") == 1, "Equivalent diamond paths duplicated the shared result or read");
            Check(!HasIssue(analysis, "trace-cycle"), "A diamond join was reported as a recursion cycle");
            Check((int)Field(analysis, "ContextCount") == 4, "Equivalent diamond contexts were expanded more than once");

            scripts["shared"] += Next("left");
            calls.Clear(); analysis = Analyze(Fork("left", "right"), Reader(scripts, calls));
            Check(Flags(analysis, "diamond").Count == 1 && HasIssue(analysis, "trace-cycle"), "A true ancestor loop was missed or erased pre-cycle effects");
            Check(calls.Count(x => x == "left") == 1 && calls.Count(x => x == "shared") == 1, "Cycle detection reread an already resolved ancestor");

            calls.Clear(); analysis = Analyze(Next("TRAVERSAL_ROOT"), name => { calls.Add(name); return Read("Success", ""); });
            Check(calls.Count == 0 && HasIssue(analysis, "trace-cycle"), "Root cycle comparison became case-sensitive");
        }
        static void LocalControlFlow()
        {
            var calls = new List<string>();
            Func<string, object> reader = name => { calls.Add(name); return Read("Success", Flag(name)); };
            object analysis = Analyze(Next("old") + "luamanager.SetCurrentStoryScript(\"effective\")\n", reader);
            Check(calls.SequenceEqual(new[] { "effective" }), "FIFO traversal ignored the final local registration overwrite");
            Check(Flags(analysis, "old").Count == 0 && Flags(analysis, "effective").Count == 1, "Overwritten registration leaked a result");

            foreach (string replacement in new[] { "\"\"", "unknown_target" })
            {
                calls.Clear(); analysis = Analyze(Flag("safe_root") + Next("old") + "luamanager.SetCurrentStoryScript(" + replacement + ")\n", reader);
                Check(calls.Count == 0 && Flags(analysis, "safe_root").Count == 1, "Cleared or dynamic final registration retained the stale followup");
            }

            calls.Clear(); analysis = Analyze(Flag("before_choice") + Next("stale") + "local selected=choose({})\n" + Flag("after_choice") + Next("beyond_choice"), reader);
            Check(calls.Count == 0 && Flags(analysis, "before_choice").Count == 1 && Flags(analysis, "after_choice").Count == 0, "Queued work crossed the next player-choice boundary");
            Check((bool)Field(analysis, "StoppedAtChoice"), "Player-choice boundary was not retained in the analysis");

            calls.Clear(); analysis = Analyze("if gate then\n" + Next("returned") + "return\nelse\n" + Next("not_returned") + "local selected=choose({})\nend\n", reader);
            Check(calls.SequenceEqual(new[] { "returned" }), "Choice boundary discarded a returned branch reservation or admitted a non-returned branch");
            Check(Flags(analysis, "returned").Count == 1 && Flags(analysis, "not_returned").Count == 0, "Returned and choice-stopped targets lost their separate local control-flow status");
        }
        static void FiniteFrontierAndPrefixes()
        {
            var source = "";
            for (int i = 0; i < 7; i++) source += (i == 0 ? "if gate0 then\n" : "elseif gate" + i + " then\n") + Next("branch_" + i);
            source = Flag("frontier_root") + source + "else\n" + Next("branch_last") + "end\n";
            int reads = 0;
            object analysis = Analyze(source, name => { reads++; return Read("Success", Flag("unread")); }, Limits("MaxScripts", 1));
            Check(reads == 0 && Flags(analysis, "frontier_root").Count == 1, "Frontier saturation erased the root or read beyond the script cap");
            Check(HasIssue(analysis, "trace-frontier-limit"), "Finite pending-context bound silently dropped queued alternatives");
            Check((int)Field(analysis, "ContextCount") <= 4 * (int)Field(analysis, "ScriptCount"), "Binding-context expansion exceeded the finite four-mask bound");
            Check(analysis.GetType().GetProperty("State").GetValue(analysis).ToString() == "Partial", "Frontier cutoff was presented as a complete analysis");

            string root = Flag("prefix_root") + Next("prefix_child");
            string prefix = Flag("prefix_child_safe");
            analysis = Analyze(root, name => Read("Success", prefix + "--" + new string('x', 1000) + "\n" + Flag("outside_prefix")), Limits("MaxCharacters", root.Length + prefix.Length));
            Check(Flags(analysis, "prefix_root").Count == 1 && Flags(analysis, "prefix_child_safe").Count == 1, "Character cutoff erased completed root or followup-prefix effects");
            Check(Flags(analysis, "outside_prefix").Count == 0 && HasIssue(analysis, "trace-character-limit"), "Character cutoff admitted an effect outside the measured prefix or lost incomplete status");
            Check((int)Field(analysis, "CharacterCount") <= root.Length + prefix.Length, "Worklist exceeded its finite character budget");
        }
        public static int Run()
        {
            assertions = 0;
            BreadthAndLimits(); BindingContexts(); DiamondAndCycle(); LocalControlFlow(); FiniteFrontierAndPrefixes();
            return assertions;
        }
    }
}
