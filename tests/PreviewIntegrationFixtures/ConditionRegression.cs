using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Mortal.Core;
using Mortal.Story;

namespace DiceMaster
{
    // These cases run the candidate's copied descriptor and readonly reader IL.
    // The fixture's original evaluators throw, making accidental execution fail.
    public static class ConditionRegression
    {
        static readonly Assembly Self = typeof(ConditionRegression).Assembly;
        static int assertions;
        static Type T(string name) => Self.GetType("DiceMaster.LiveLog." + name, true);
        static object Parse(string text, object bindings = null) => T("PreviewConditions").GetMethod("Parse").Invoke(null, new[] { text, bindings });
        static string Current(object condition) => T("PreviewConditions").GetMethod("Current").Invoke(null, new[] { condition }).ToString();
        static bool Known(object condition) => (bool)condition.GetType().GetProperty("IsKnownSyntax").GetValue(condition);
        static void Check(bool value, string reason) { assertions++; if (!value) throw new InvalidOperationException(reason); }
        static void State(string expected, object condition, string reason) => Check(Current(condition) == expected, reason);
        static StatGroupVariable Literal(int value) => new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 0, _constant = value } } };
        static ConditionResultItem ConstantCondition(int left, int right, StatCompareType operation = StatCompareType.Equal) => new ConditionResultItem
        {
            _items = new List<StatCompareItem> { new StatCompareItem { _value1 = Literal(left), _value2 = Literal(right), _compareOp = operation } }
        };
        static ConditionResultItem CurrentAtLeast(int minimum) => new ConditionResultItem
        {
            _items = new List<StatCompareItem> { new StatCompareItem { _value1 = new StatGroupVariable(), _value2 = Literal(minimum), _compareOp = StatCompareType.GreaterEqual } }
        };
        static object Bindings(params (string Name, object Condition)[] values)
        {
            var map = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), T("PreviewCondition")));
            foreach (var pair in values) map.Add(pair.Name, pair.Condition);
            return map;
        }
        static object Analyze(string script, Func<string, object> reader = null)
        {
            MethodInfo method = T("PreviewAnalyzer").GetMethod("Analyze");
            object callback = null;
            if (reader != null)
            {
                var name = Expression.Parameter(typeof(string), "name");
                Type callbackType = method.GetParameters()[2].ParameterType;
                callback = Expression.Lambda(callbackType, Expression.Convert(Expression.Invoke(Expression.Constant(reader), name), callbackType.GetGenericArguments()[1]), name).Compile();
            }
            return method.Invoke(null, new object[] { "current-condition-root", script, callback, null });
        }
        static string Describe(object analysis) => (string)T("PreviewDisplay").GetMethod("Describe").Invoke(null, new[] { analysis });
        static void Neutral(object analysis, string reason)
        {
            string text = Describe(analysis);
            Check(!text.Contains("현재 조건 충족") && !text.Contains("현재 조건 미충족"), reason);
        }

        static void Syntax()
        {
            State("Met", Parse("true"), "Literal true current condition");
            State("NotMet", Parse("false"), "Literal false current condition");
            State("Met", Parse("0"), "Lua zero must remain truthy");
            State("NotMet", Parse("not 0"), "Lua not zero must be false");
            State("Met", Parse("(1 == 1) and not (2 < 1)"), "Boolean composition");
            State("NotMet", Parse("1 == true"), "Lua integer and boolean equality cannot coerce");
            State("Met", Parse("(false and 1) == false"), "Lua and must preserve operand values");
            State("Met", Parse("(0 or false) == 0"), "Lua or must preserve truthy zero operand");
            State("Met", Parse("-2147483648 < -1"), "Negative integer boundary");
            State("Unknown", Parse("unbound"), "Unknown identifier");
            State("NotMet", Parse("false and unbound"), "Known false dominates an unknown condition");
            State("NotMet", Parse("unbound and false"), "False right operand proves false truth without inventing its exact value");
            State("Met", Parse("unbound or true"), "Known true dominates unknown condition truth");
            State("Unknown", Parse("(unbound or true) == true"), "Known truth cannot become an invented exact operand");
            State("Unknown", Parse("(unbound and false) == false"), "False truth cannot become an invented false operand");
            foreach (string expression in new[] { "checkpointmanager:Switch(id) == 1", "checkpointmanager:Switch('key', 1) == 1",
                "checkpointmanager:Switch('key')()", "checkpointmanager:Switch('key') + 1 == 2", "checkpointmanager:Require('key')",
                "other:Switch('key') == 1", "checkpointmanager.Switch('key') trailing", "true; false", "1 == 1 == 1", "true = false", "'raw'" })
            { Check(!Known(Parse(expression)), "Unsupported condition syntax was accepted"); State("Unknown", Parse(expression), "Unsupported condition syntax was evaluated"); }
            State("Unknown", Parse(new string('(', 90) + "true" + new string(')', 90)), "Condition parse nesting bound");
            State("Unknown", Parse(new string(' ', 4097) + "true"), "Condition expression size bound");
            var trueValue = Parse("true");
            State("Met", Parse("alias and not missing", Bindings(("alias", trueValue), ("missing", Parse("false")))), "Known alias composition");
            State("Unknown", Parse("Alias", Bindings(("alias", trueValue))), "Lua variable names are case sensitive");
            object node = trueValue;
            for (int i = 0; i < 80; i++) node = T("PreviewConditions").GetMethod("Not").Invoke(null, new[] { node });
            State("Unknown", node, "Condition descriptor evaluation nesting bound");
        }

        static void CurrentFields()
        {
            CheckPointManager previous = CheckPointManager.Instance;
            int previousValue = DataHelper.CurrentValue;
            int groups = StatGroupVariable.OriginalReads, comparisons = StatCompareItem.OriginalReads;
            int switches = SwitchResultData.Executions, conditions = ConditionResultData.Executions, rng = UnityEngine.Random.Calls;
            try
            {
                var data = new SwitchResultData { name = "switch_fixture", _count = 41,
                    _items = new List<ConditionResultItem> { CurrentAtLeast(50), ConstantCondition(1, 1) } };
                var condition = new ConditionResultData { name = "condition_fixture", _count = 37,
                    _items = new List<ConditionResultItem> { CurrentAtLeast(50) } };
                var manager = new CheckPointManager
                {
                    _switchList = new[] { new SwitchResultConfig { _list = new List<SwitchResultData> { data } } },
                    _condition = new ConditionResultConfig { _list = new List<ConditionResultData> { condition } }
                };
                CheckPointManager.Instance = manager;
                object readSwitch = Parse("checkpointmanager:Switch('switch_fixture')");
                object bindings = Bindings(("switch_result", readSwitch), ("required", Parse("checkpointmanager.Condition('condition_fixture')")));
                object first = Parse("switch_result == 1", bindings), second = Parse("switch_result == 2", bindings);
                DataHelper.CurrentValue = 60;
                State("Met", first, "First satisfied switch alternative");
                State("NotMet", second, "Later satisfied alternatives must not outrank the first");
                State("Met", Parse("required", bindings), "Condition reads existing current state");
                DataHelper.CurrentValue = 20;
                State("NotMet", first, "Cached syntax must not retain a prior current condition value");
                State("Met", second, "Switch alternative updates when readonly state changes");
                State("NotMet", Parse("required", bindings), "Condition updates when readonly state changes");
                data._items[1] = ConstantCondition(1, 2);
                State("Met", Parse("switch_result == 3", bindings), "Switch fallthrough returns count plus one");
                condition._items.Clear();
                State("NotMet", Parse("required", bindings), "An existing empty condition result is false");
                data._items.Clear();
                State("Met", first, "An empty switch returns one");
                data._useCount = true;
                State("Unknown", first, "Execution-count switches must not be simulated");
                data._useCount = false;
                var random = ConstantCondition(1, 1);
                random._items[0]._value1._value[0]._checkType = 7;
                data._items.Add(ConstantCondition(1, 1)); data._items.Add(random);
                State("Unknown", first, "A random later switch alternative keeps the entire group neutral");
                condition._items.Add(ConstantCondition(1, 1)); condition._items.Add(random);
                State("Unknown", Parse("required", bindings), "Random condition alternatives must not be sampled");
                data._items.Clear();
                var recursive = ConstantCondition(1, 1);
                var group = recursive._items[0]._value1;
                group._value = new List<StatValueReference> { new StatValueReference { _checkType = 8, _statGroup = group } };
                data._items.Add(recursive);
                State("Unknown", first, "Cyclic condition groups must stay neutral");
                data._items = null;
                State("Unknown", first, "Null switch condition list stays neutral");
                State("Unknown", Parse("checkpointmanager.Switch('missing') == 1"), "Missing switch data stays neutral");
                State("Unknown", Parse("checkpointmanager.Condition('missing')"), "Missing condition data stays neutral");
                manager._switchList = null;
                State("Unknown", first, "Unavailable existing switch list stays neutral");
                CheckPointManager.Instance = null;
                State("Unknown", first, "Unavailable existing singleton stays neutral");
                Check(CheckPointManager.Instance == null, "Condition display initialized an absent singleton");
                Check(data._count == 41 && condition._count == 37, "Condition display changed original execution counters");
                Check(groups == StatGroupVariable.OriginalReads && comparisons == StatCompareItem.OriginalReads,
                    "Condition display executed an original group or comparison");
                Check(switches == SwitchResultData.Executions && conditions == ConditionResultData.Executions,
                    "Condition display executed the original checkpoint chain");
                Check(rng == UnityEngine.Random.Calls, "Condition display called Unity RNG");
            }
            finally { CheckPointManager.Instance = previous; DataHelper.CurrentValue = previousValue; }
        }

        static void TraceAndDisplay()
        {
            CheckPointManager previous = CheckPointManager.Instance;
            int previousValue = DataHelper.CurrentValue;
            try
            {
                var data = new SwitchResultData { name = "branch_fixture", _items = new List<ConditionResultItem> { CurrentAtLeast(50), ConstantCondition(1, 2) } };
                CheckPointManager.Instance = new CheckPointManager { _switchList = new[] { new SwitchResultConfig { _list = new List<SwitchResultData> { data } } } };
                const string binding = "local s = checkpointmanager:Switch('branch_fixture')\n";
                const string branches = "if s == 1 then statmodifymanager.SetFlag('first',1) elseif s == 2 then statmodifymanager.SetFlag('second',2) else statmodifymanager.SetFlag('last',3) end";
                DataHelper.CurrentValue = 60;
                object analysis = Analyze(binding + branches);
                string before = Describe(analysis);
                Check(before.Contains("경우 A · 현재 조건 충족") && before.Contains("경우 B · 현재 조건 미충족") && before.Contains("그 외 · 현재 조건 미충족"), "Current branch labels must show ordered switch selection");
                DataHelper.CurrentValue = 20;
                string after = Describe(analysis);
                Check(after.Contains("경우 A · 현재 조건 미충족") && after.Contains("그 외 · 현재 조건 충족"), "The same cached analysis must refresh its current branch hints");
                Check(before != after, "State refresh retained stale condition colors or text");
                IList effects = (IList)analysis.GetType().GetField("Effects").GetValue(analysis);
                Check(effects.Cast<object>().All(effect => (bool)effect.GetType().GetField("Conditional").GetValue(effect)), "Current condition hints must not promote future effects to guaranteed results");
                foreach (string prefix in new[] { "statmodifymanager.SetFlag('before',1)\n", "untrusted()\n", "local q = custom.value\n", "checkpointmanager['Switch'] = replacement\n",
                    "luamanager.ToggleSaveButton(true)\n", "GetStoryText('unused')\n", "luamanager.CloseDialog()\n", "tostring(1)\n" })
                    Neutral(Analyze(prefix + binding + branches), "A prior write or unproved execution boundary must disable current branch hints");
                foreach (string middle in new[] { "s = unbound\n", "s, other = 1, 2\n", "s[1] = 2\n", "local t = {s = 2,}\n", "local t = {s = 2;}\n" })
                    Neutral(Analyze(binding + middle + branches), "An unsupported assignment or table field must not manufacture a current branch value");
                Neutral(Analyze("local s = 2\n + 3\n" + branches), "A multiline RHS continuation must not be read as a standalone constant binding");
                Neutral(Analyze("local s = checkpointmanager:Switch('branch_fixture'); local alias = s; alias = unknown\nif alias == 1 then statmodifymanager.SetFlag('first',1) end"), "An invalidated alias cannot retain its previous switch proof");
                Type actionType = Self.GetType("DiceMaster.LuaParse.Action", true);
                object conditionalAction = Activator.CreateInstance(actionType, new object[] { Enum.Parse(Self.GetType("DiceMaster.LuaParse.ActionType", true), "ModifyAction"), binding + branches, true });
                Neutral(actionType.GetProperty("Analysis").GetValue(conditionalAction), "Legacy conditional Action constructor must preserve an unproved enclosing context");
                object hidden = Analyze(binding + branches);
                hidden.GetType().GetField("SnapshotAllowed").SetValue(hidden, false);
                Neutral(hidden, "Snapshot-disabled offline audit must remain neutral");
                object conditionalRoot = Analyze(binding + branches);
                conditionalRoot.GetType().GetMethod("MarkConditionalRoot").Invoke(conditionalRoot, null);
                Neutral(conditionalRoot, "Unproved enclosing option context must remain neutral");
                object followup = Analyze("luamanager.SetNextScript('child')", name => T("ScriptReadResult").GetMethod("Success").Invoke(null, new object[] { binding + branches }));
                Neutral(followup, "Followup condition hints cannot imply that current values survive until execution");
                object lexical = Analyze(binding + "if true then local s = 2 end\n" + branches);
                Check(!Describe(lexical).Contains("경우 B · 현재 조건 충족"), "A nested local shadow escaped its lexical scope");
                object unknownFirst = Analyze("if unbound then statmodifymanager.SetFlag('a',1) elseif false then statmodifymanager.SetFlag('b',2) else statmodifymanager.SetFlag('c',3) end");
                Neutral(unknownFirst, "Unknown earlier source predicates can run unproved Lua behavior before later alternatives");
                Neutral(Analyze(binding + "if custom.value then statmodifymanager.SetFlag('a',1) elseif s == 2 then statmodifymanager.SetFlag('b',2) end"),
                    "An unknown header index can invoke Lua metamethods before a later checkpoint comparison");
                CheckPointManager.Instance._condition._list.Add(new ConditionResultData { name = "condition_fixture", _items = new List<ConditionResultItem> { CurrentAtLeast(50) } });
                DataHelper.CurrentValue = 60;
                object conditionAnalysis = Analyze("local required = checkpointmanager.Condition('condition_fixture')\nif required then statmodifymanager.SetFlag('met',1) else statmodifymanager.SetFlag('unmet',2) end");
                Check(Describe(conditionAnalysis).Contains("경우 A · 현재 조건 충족"), "Safe readonly Condition hint was blocked by the preserved execution issue");
                IList conditionEffects = (IList)conditionAnalysis.GetType().GetField("Effects").GetValue(conditionAnalysis);
                IList conditionIssues = (IList)conditionAnalysis.GetType().GetField("Issues").GetValue(conditionAnalysis);
                Check(conditionIssues.Count > 0 && conditionEffects.Cast<object>().All(effect => (bool)effect.GetType().GetField("Conditional").GetValue(effect)), "Readonly Condition hint changed its execution-uncertainty classification");
                Check(UnityEngine.Random.Calls == 0 && StatGroupVariable.OriginalReads == 0 && StatCompareItem.OriginalReads == 0 &&
                    ConditionResultData.Executions == 0 && SwitchResultData.Executions == 0, "Trace or display called an original game evaluator or RNG");
            }
            finally { CheckPointManager.Instance = previous; DataHelper.CurrentValue = previousValue; }
        }

        static void FlatAlternativesRemainBounded()
        {
            CheckPointManager previous = CheckPointManager.Instance;
            int switches = SwitchResultData.Executions, conditions = ConditionResultData.Executions;
            int groups = StatGroupVariable.OriginalReads, comparisons = StatCompareItem.OriginalReads, rng = UnityEngine.Random.Calls;
            try
            {
                var data = new SwitchResultData { name = "flat_fixture", _count = 19,
                    _items = new List<ConditionResultItem> { ConstantCondition(1, 1) } };
                CheckPointManager.Instance = new CheckPointManager { _switchList = new[] { new SwitchResultConfig { _list = new List<SwitchResultData> { data } } } };
                var source = new StringBuilder("local s = checkpointmanager:Switch('flat_fixture')\n");
                // Flat source nesting does not imply a small ordered-predicate
                // graph: each elseif must exclude every preceding alternative.
                for (int i = 0; i <= 1000; i++)
                    source.Append(i == 0 ? "if" : "elseif").Append(" s == 2 then statmodifymanager.SetFlag('flat_").Append(i).Append("',1)\n");
                source.Append("else statmodifymanager.SetFlag('flat_default',1) end");
                object analysis = Analyze(source.ToString());
                IList effects = (IList)analysis.GetType().GetField("Effects").GetValue(analysis);
                Check(effects.Count == 1002, "A thousand flat elseif alternatives lost effects at a decorative condition limit");
                Check(effects.Cast<object>().All(effect => (bool)effect.GetType().GetField("Conditional").GetValue(effect)), "Flat condition hints promoted possible effects to guaranteed execution");
                object FirstBranch(object effect) => ((IList)effect.GetType().GetField("Branches").GetValue(effect))[0];
                object Predicate(object branch) => branch.GetType().GetField("CurrentCondition", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(branch);
                State("NotMet", Predicate(FirstBranch(effects[0])), "An early flat alternative should retain its safe current hint");
                State("Unknown", Predicate(FirstBranch(effects[effects.Count - 1])), "A deep ordered predicate must stop at its independent current-read depth bound");
                string rendered = Describe(analysis);
                Check(rendered.Contains("경우 A · 현재 조건 미충족"), "Bounded rendering discarded a valid early hint");
                string fallback = rendered.Split('\n').Single(line => line.Contains("flat_default"));
                Check(!fallback.Contains("현재 조건 충족") && !fallback.Contains("현재 조건 미충족"), "A later alternative must retain neutral presentation after the condition-read bound");
                Check(data._count == 19 && switches == SwitchResultData.Executions && conditions == ConditionResultData.Executions,
                    "A large alternative group executed or changed an original checkpoint");
                Check(groups == StatGroupVariable.OriginalReads && comparisons == StatCompareItem.OriginalReads && rng == UnityEngine.Random.Calls,
                    "A large alternative group called an original comparison or RNG");
            }
            finally { CheckPointManager.Instance = previous; }
        }

        public static int Run()
        {
            assertions = 0;
            Syntax(); CurrentFields(); TraceAndDisplay(); FlatAlternativesRemainBounded();
            Console.WriteLine("PREVIEW readonly current condition backend passed");
            return assertions;
        }
    }
}
