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
                State("Met", first, "A known-true first switch item excludes a later random alternative");
                condition._items.Add(ConstantCondition(1, 1)); condition._items.Add(random);
                State("Met", Parse("required", bindings), "A known-true condition alternative dominates an unknown random alternative without sampling it");
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

        static void PartialLogic()
        {
            CheckPointManager previous = CheckPointManager.Instance;
            int switches = SwitchResultData.Executions, conditions = ConditionResultData.Executions;
            int groups = StatGroupVariable.OriginalReads, comparisons = StatCompareItem.OriginalReads, rng = UnityEngine.Random.Calls;
            try
            {
                StatCompareItem RandomCompare()
                {
                    var item = ConstantCondition(1, 1)._items[0];
                    item._value1._value[0]._checkType = 7; return item;
                }
                var no = ConstantCondition(0, 1)._items[0]; var yes = ConstantCondition(1, 1)._items[0];
                var data = new ConditionResultData { name = "partial_fixture", _count = 31 };
                var sw = new SwitchResultData { name = "partial_switch", _count = 43 };
                CheckPointManager.Instance = new CheckPointManager {
                    _condition = new ConditionResultConfig { _list = new List<ConditionResultData> { data } },
                    _switchList = new[] { new SwitchResultConfig { _list = new List<SwitchResultData> { sw } } }
                };
                const string condition = "checkpointmanager.Condition('partial_fixture')";
                const string branch = "checkpointmanager.Switch('partial_switch')";
                foreach (bool randomFirst in new[] { true, false })
                {
                    var random = RandomCompare();
                    data._items = new List<ConditionResultItem> { new ConditionResultItem {
                        _items = randomFirst ? new List<StatCompareItem> { random, no, yes } : new List<StatCompareItem> { no, yes, random } } };
                    State("NotMet", Parse(condition), "actual DLL random AND false comparison is false in either source order");
                    data._items[0]._logicOp = LogicOperatorType.Or;
                    State("Met", Parse(condition), "actual DLL random OR true comparison is true in either source order");
                }
                data._items = new List<ConditionResultItem> { new ConditionResultItem { _items = new List<StatCompareItem> { RandomCompare() } } };
                State("Unknown", Parse(condition), "actual DLL isolated random comparison remains unknown");
                foreach (string expression in new[] { condition + " and false", "false and " + condition })
                    State("NotMet", Parse(expression), "actual DLL random checkpoint leaf does not abort a false AST sibling");
                foreach (string expression in new[] { condition + " or true", "true or " + condition })
                    State("Met", Parse(expression), "actual DLL random checkpoint leaf does not abort a true AST sibling");
                data._items = null;
                State("NotMet", Parse(condition + " and false"), "actual DLL malformed checkpoint is isolated to its AST leaf");
                State("Met", Parse(condition + " or true"), "actual DLL malformed checkpoint preserves a separate true OR operand");
                sw._items = new List<ConditionResultItem> { ConstantCondition(0, 1), new ConditionResultItem { _items = new List<StatCompareItem> { RandomCompare() } } };
                State("NotMet", Parse(branch + "==1"), "actual DLL false first item excludes switch result one despite later randomness");
                State("Met", Parse(branch + ">1"), "actual DLL possible switch indices retain proven bounds");
                State("Unknown", Parse(branch + "==2"), "actual DLL random second item is possible rather than sampled");
                sw._items = new List<ConditionResultItem> { new ConditionResultItem { _items = new List<StatCompareItem> { RandomCompare() } }, ConstantCondition(1, 1), ConstantCondition(1, 1) };
                State("NotMet", Parse(branch + "==3"), "actual DLL known-true second item excludes a later branch");
                State("Unknown", Parse(branch + "==1"), "actual DLL unknown earlier item retains its possible priority");
                sw._useCount = true;
                State("Unknown", Parse(branch + "==3"), "actual DLL count-dependent switch remains protected");
                Check(data._count == 31 && sw._count == 43 && switches == SwitchResultData.Executions && conditions == ConditionResultData.Executions,
                    "actual DLL partial condition evaluation preserves checkpoint counters and never executes originals");
                Check(groups == StatGroupVariable.OriginalReads && comparisons == StatCompareItem.OriginalReads && rng == UnityEngine.Random.Calls,
                    "actual DLL partial condition evaluation never calls original comparison/group/RNG paths");
            }
            finally { CheckPointManager.Instance = previous; }
        }

        static void TalkResetFootprint()
        {
            CheckPointManager previous = CheckPointManager.Instance;
            int switches = SwitchResultData.Executions, conditions = ConditionResultData.Executions;
            int groups = StatGroupVariable.OriginalReads, comparisons = StatCompareItem.OriginalReads, rng = UnityEngine.Random.Calls;
            object Capture(object condition, bool reset, params string[] writes) => T("PreviewConditions").GetMethod(reset ? "CaptureAfterTalkReset" : "CaptureCurrent")
                .Invoke(null, new object[] { condition, writes, false });
            void Observe(object environment) => T("PreviewConditions").GetMethod("ObserveLuaEnvironment").Invoke(null, new[] { environment });
            bool Disjoint(object condition, params string[] writes) => (bool)T("PreviewConditions").GetMethods().Single(method => method.Name == "AreFlagWritesDisjoint" && method.GetParameters().Length == 2).Invoke(null, new object[] { condition, writes });
            try
            {
                var resetFlag = new FlagData { name = "serialized_reset_flag", State = 1, _default = 0 };
                var persistent = new FlagData { name = "persistent_story_flag", State = 1, _default = 0 };
                ConditionResultItem FlagCondition(FlagData flag) => new ConditionResultItem { _items = new List<StatCompareItem> {
                    new StatCompareItem { _value1 = new StatGroupVariable { _value = new List<StatValueReference> { new StatValueReference { _checkType = 3, _flag = flag } } }, _value2 = Literal(0) } } };
                var topic = new ConditionResultData { name = "checkpoint_with_unrelated_identifier", _items = new List<ConditionResultItem> { FlagCondition(resetFlag) } };
                var persistentCondition = new ConditionResultData { name = "serialized_reset_flag", _items = new List<ConditionResultItem> { FlagCondition(persistent) } };
                var independent = new ConditionResultData { name = "independent_false", _items = new List<ConditionResultItem> { ConstantCondition(0, 1) } };
                var compound = new ConditionResultData { name = "masked_and_false", _items = new List<ConditionResultItem> { FlagCondition(resetFlag) } };
                compound._items[0]._items.Add(ConstantCondition(0, 1)._items[0]);
                CheckPointManager.Instance = new CheckPointManager { _condition = new ConditionResultConfig {
                    _list = new List<ConditionResultData> { topic, persistentCondition, independent, compound } } };
                object Topic() => Parse("checkpointmanager.Condition('checkpoint_with_unrelated_identifier')");
                object Persistent() => Parse("checkpointmanager.Condition('serialized_reset_flag')");
                int resetReads = resetFlag.StateReads, persistentReads = persistent.StateReads;
                Check(!Disjoint(Topic(), "serialized_reset_flag") && Disjoint(Topic(), "unrelated_flag"),
                    "actual DLL parent-topic dependency proof reads the referenced flag definition rather than guessing from its checkpoint key");
                Check(Disjoint(Persistent(), "serialized_reset_flag") && !Disjoint(Persistent(), "persistent_story_flag"),
                    "actual DLL a checkpoint name equal to an unrelated written flag cannot create a false dependency");
                Check(Disjoint(Parse("false"), "serialized_reset_flag") && !Disjoint(Parse("missing"), "serialized_reset_flag") && Disjoint(Parse("missing")),
                    "actual DLL definition dependency proof is conservative about unknown guards and vacuous for no prior writes");
                CheckPointManager.Instance._switchList = new[] { new SwitchResultConfig { _list = new List<SwitchResultData> {
                    new SwitchResultData { name = "all_definition_alternatives", _items = new List<ConditionResultItem> { ConstantCondition(1, 1), FlagCondition(resetFlag) } } } } };
                Check(!Disjoint(Parse("checkpointmanager.Switch('all_definition_alternatives') == 1"), "serialized_reset_flag"),
                    "actual DLL definition dependency proof inspects later alternatives even when the first is already true");
                var randomDefinition = ConstantCondition(1, 1); randomDefinition._items[0]._value1._value[0]._checkType = 7;
                CheckPointManager.Instance._condition._list.Add(new ConditionResultData { name = "random_definition", _items = new List<ConditionResultItem> { randomDefinition } });
                Check(!Disjoint(Parse("checkpointmanager.Condition('random_definition')"), "unrelated_flag"),
                    "actual DLL unsupported random dependency definitions cannot become a disjointness proof");
                Check(!Disjoint(Topic(), Enumerable.Range(0, 33).Select(at => "write_" + at).ToArray()) &&
                    !Disjoint(Parse("checkpointmanager.Condition('absent_definition')"), "some_flag"),
                    "actual DLL dependency definition discovery has bounded write sets and cannot assume missing data is disjoint");
                Check(resetFlag.StateReads == resetReads && persistent.StateReads == persistentReads,
                    "actual DLL parent-topic disjointness proof reads no current flag value");
                var collection = new FlagCollectionData { _list = new List<FlagData> { resetFlag } };
                var manager = new StatModifyManager { _talkOptionFlag = collection };
                var script = new MoonSharp.Interpreter.Script();
                script.m_GlobalTable.Values["statmodifymanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(manager) };
                var environment = new Fungus.LuaEnvironment(script);
                Observe(environment);
                object topicAfter = Capture(Topic(), true), persistentAfter = Capture(Persistent(), true);
                State("Unknown", topicAfter, "actual DLL talk reset masks a serialized flag dependency even when checkpoint and flag identifiers differ");
                State("NotMet", persistentAfter, "actual DLL talk reset leaves a disjoint persistent story flag readable even when its checkpoint name equals a reset flag name");
                Check(resetFlag.StateReads == 0, "actual DLL reset-masked comparisons do not sample a flag value that may be reset");
                object beforeReset = Capture(Topic(), false);
                State("NotMet", Capture(beforeReset, true), "actual DLL later talk reset cannot retroactively mask a captured earlier checkpoint read");
                State("NotMet", Capture(Parse("checkpointmanager.Condition('masked_and_false')"), true), "actual DLL reset-masked input preserves an independent false AND comparison");
                State("Unknown", Capture(Persistent(), true, "persistent_story_flag"), "actual DLL talk reset composes with existing literal write masks");
                int rawReads = script.m_GlobalTable.RawReads;
                object reader = T("PreviewConditions").GetMethod("CreateReader").Invoke(null, null);
                MethodInfo current = reader.GetType().GetMethod("Current");
                current.Invoke(reader, new[] { topicAfter }); current.Invoke(reader, new[] { persistentAfter });
                Check(script.m_GlobalTable.RawReads == rawReads + 1, "actual DLL reader validates the current talk collection once across several captured checkpoint leaves");
                MethodInfo disjoint = reader.GetType().GetMethods().Single(method => method.Name == "AreFlagWritesDisjoint" && method.GetParameters().Length == 2);
                Check(!(bool)disjoint.Invoke(reader, new object[] { Topic(), new[] { "serialized_reset_flag" } }) &&
                    current.Invoke(reader, new[] { Topic() }).ToString() == "NotMet",
                    "actual DLL shared reader restores dependency masks before its next current-value read");
                object boundedReader = T("PreviewConditions").GetMethod("CreateReader").Invoke(null, null);
                int proofs = 0;
                while (proofs < 2048 && (bool)disjoint.Invoke(boundedReader, new object[] { Topic(), new[] { "unrelated_write" } })) proofs++;
                Check(proofs > 0 && proofs < 2048 && current.Invoke(boundedReader, new[] { Parse("true") }).ToString() == "Unknown",
                    "actual DLL dependency proofs and current evaluation share one bounded reader work budget");
                manager._talkOptionFlag = new FlagCollectionData { _list = new List<FlagData> { persistent } };
                State("NotMet", topicAfter, "actual DLL same-environment collection replacement cannot retain an old reset footprint");
                State("Unknown", persistentAfter, "actual DLL current replacement reset collection masks its new dependency");
                Observe(null);
                State("Unknown", topicAfter, "actual DLL missing observed environment conservatively masks flag-dependent comparisons");
                State("Unknown", persistentAfter, "actual DLL unresolved reset footprint cannot assume a persistent flag is disjoint");
                State("NotMet", Capture(Parse("checkpointmanager.Condition('independent_false')"), true), "actual DLL missing reset footprint preserves flag-independent false checkpoints");
                State("NotMet", Capture(Parse("checkpointmanager.Condition('masked_and_false')"), true), "actual DLL unresolved reset footprint does not swallow independent false AND comparisons");
                State("NotMet", Capture(beforeReset, true), "actual DLL missing reset footprint still preserves a checkpoint captured before the reset");
                manager._talkOptionFlag._list = null; Observe(environment);
                State("Unknown", topicAfter, "actual DLL malformed reset collection cannot become an empty known footprint");
                manager._talkOptionFlag._list = Enumerable.Range(0, 1025).Select(at => new FlagData { name = "bounded_" + at }).ToList();
                State("Unknown", topicAfter, "actual DLL reset collection traversal has an independent bounded item count");
                manager._talkOptionFlag = collection; Observe(environment);
                script.m_GlobalTable.Values["statmodifymanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(new object()) };
                Observe(environment);
                State("Unknown", persistentAfter, "actual DLL invalid new environment observation clears previous collection evidence");
                Check(resetFlag.State == 1 && persistent.State == 1 && FlagCollectionData.ResetCalls == 0 && StatModifyManager.ResetCalls == 0 && Fungus.LuaEnvironment.Initializations == 0,
                    "actual DLL talk footprint discovery changes no flag and invokes no reset or environment initializer");
                Check(groups == StatGroupVariable.OriginalReads && comparisons == StatCompareItem.OriginalReads && rng == UnityEngine.Random.Calls &&
                    switches == SwitchResultData.Executions && conditions == ConditionResultData.Executions,
                    "actual DLL talk footprint reads invoke no original comparison, checkpoint chain or RNG");
                GC.KeepAlive(environment);
            }
            finally { Observe(null); CheckPointManager.Instance = previous; }
        }

        static void LiteralNoOpWrites()
        {
            CheckPointManager previous = CheckPointManager.Instance;
            int switches = SwitchResultData.Executions, conditions = ConditionResultData.Executions;
            int groups = StatGroupVariable.OriginalReads, comparisons = StatCompareItem.OriginalReads, rng = UnityEngine.Random.Calls;
            MethodInfo disjoint = T("PreviewConditions").GetMethods().Single(method => method.Name == "AreFlagWritesDisjoint" && method.GetParameters().Length == 3);
            bool CheckWrites(object gate, string[] writes, IDictionary<string, int> sets) => (bool)disjoint.Invoke(null, new object[] { gate, writes, sets });
            try
            {
                var flag = new FlagData { name = "topic_consumed_flag", State = 1 };
                var item = ConstantCondition(0, 0);
                item._items[0]._value1._value[0]._checkType = 3; item._items[0]._value1._value[0]._flag = flag;
                var data = new ConditionResultData { name = "separate_topic_checkpoint", _items = new List<ConditionResultItem> { item } };
                CheckPointManager.Instance = new CheckPointManager { _condition = new ConditionResultConfig { _list = new List<ConditionResultData> { data } } };
                object gate = Parse("checkpointmanager.Condition('separate_topic_checkpoint')");
                var writes = new[] { flag.name };
                var same = new Dictionary<string, int>(StringComparer.Ordinal) { [flag.name] = 1 };
                Check(CheckWrites(gate, writes, same), "actual DLL literal SetFlag to its exact current value is a no-op for that topic dependency");
                Check(flag.StateReads == 1, "actual DLL no-op proof reads only the matching concrete flag's current State");
                flag.State = 0;
                Check(!CheckWrites(gate, writes, same), "actual DLL a literal SetFlag that changes the current value cannot be ignored");
                flag.State = 1;
                Check(!CheckWrites(gate, writes, new Dictionary<string, int>()), "actual DLL AddFlag or conflicting later SetFlag without a same-value candidate remains affected");
                Check(!CheckWrites(gate, writes, new Dictionary<string, int> { [flag.name] = 2 }), "actual DLL a later different literal SetFlag cannot use an earlier same-value exemption");
                Check(CheckWrites(gate, new[] { "unrelated_flag" }, new Dictionary<string, int> { ["unrelated_flag"] = 2 }), "actual DLL no-op metadata does not require unrelated dependency values to be sampled");
                var replacement = new FlagData { name = flag.name, State = 0 };
                var changed = ConstantCondition(0, 0); changed._items[0]._value1._value[0]._checkType = 3; changed._items[0]._value1._value[0]._flag = replacement;
                data._items = new List<ConditionResultItem> { changed };
                Check(!CheckWrites(gate, writes, same), "actual DLL a fresh scheduled reader follows a replaced native condition graph rather than a previous no-op result");
                replacement.State = 1;
                Check(CheckWrites(gate, writes, same), "actual DLL an unchanged descriptor reads the new current flag state without reparsing Lua");
                object reader = T("PreviewConditions").GetMethod("CreateReader").Invoke(null, null);
                MethodInfo readerDisjoint = reader.GetType().GetMethods().Single(method => method.Name == "AreFlagWritesDisjoint" && method.GetParameters().Length == 3);
                Check((bool)readerDisjoint.Invoke(reader, new object[] { gate, writes, same }) && reader.GetType().GetMethod("Current").Invoke(reader, new[] { gate }).ToString() == "NotMet",
                    "actual DLL no-op definition masks are restored before current evaluation in the same bounded reader");
                MethodInfo afterReset = T("PreviewConditions").GetMethods().Single(method => method.Name == "AreFlagWritesDisjoint" && method.GetParameters().Length == 4);
                bool ResetWrites(string[] changedWrites = null) => (bool)afterReset.Invoke(null, new object[] { gate, changedWrites ?? writes, same, true });
                var resetCollection = new FlagCollectionData { _list = new List<FlagData> { replacement } };
                replacement._default = 0;
                var resetManager = new StatModifyManager { _talkOptionFlag = resetCollection };
                var resetScript = new MoonSharp.Interpreter.Script();
                resetScript.m_GlobalTable.Values["statmodifymanager"] = new MoonSharp.Interpreter.DynValue { m_Object = new MoonSharp.Interpreter.UserData(resetManager) };
                var resetEnvironment = new Fungus.LuaEnvironment(resetScript);
                T("PreviewConditions").GetMethod("ObserveLuaEnvironment").Invoke(null, new object[] { resetEnvironment });
                Check(!ResetWrites(), "actual DLL a prior talk reset prevents a current-value no-op exemption for a member flag before its future SetFlag");
                resetCollection._list = new List<FlagData> { new FlagData { name = "different_reset_flag", State = 1 } };
                Check(ResetWrites(), "actual DLL a known disjoint talk-reset footprint permits an otherwise same-value literal SetFlag");
                resetCollection._list.Add(replacement);
                Check(!ResetWrites(), "actual DLL a fresh scheduled proof follows membership changes within the same serialized reset collection");
                resetCollection._list.Remove(replacement);
                Check(ResetWrites(), "actual DLL removing the dependency from the same reset collection restores the proof on a fresh reader");
                T("PreviewConditions").GetMethod("ObserveLuaEnvironment").Invoke(null, new object[] { null });
                Check(!ResetWrites(), "actual DLL an unknown talk-reset footprint cannot excuse a matching same-value future assignment");
                Check(ResetWrites(new[] { "unrelated_flag" }), "actual DLL unknown prior reset metadata does not turn independent disjoint writes into affected topic inputs");
                Check((bool)afterReset.Invoke(null, new object[] { gate, writes, same, false }), "actual DLL four-argument no-reset mode retains the existing literal no-op behavior");
                Check(replacement.State == 1 && flag.State == 1 && groups == StatGroupVariable.OriginalReads && comparisons == StatCompareItem.OriginalReads && rng == UnityEngine.Random.Calls &&
                    switches == SwitchResultData.Executions && conditions == ConditionResultData.Executions && FlagCollectionData.ResetCalls == 0 && StatModifyManager.ResetCalls == 0,
                    "actual DLL literal no-op proof changes no state and executes no game condition, flag write, reset or RNG");
                GC.KeepAlive(resetEnvironment);
            }
            finally { T("PreviewConditions").GetMethod("ObserveLuaEnvironment").Invoke(null, new object[] { null }); CheckPointManager.Instance = previous; }
        }

        static void NextEntryCount()
        {
            CheckPointManager previous = CheckPointManager.Instance;
            int executions = SwitchResultData.Executions, rng = UnityEngine.Random.Calls;
            try
            {
                var data = new SwitchResultData { name = "renamed_entry", _useCount = true, _count = 0,
                    _countItems = new List<ValueCompareItem> { new ValueCompareItem { _value = 1, _compareOp = StatCompareType.Equal } } };
                CheckPointManager.Instance = new CheckPointManager { _switchList = new[] { new SwitchResultConfig { _list = new List<SwitchResultData> { data } } } };
                MethodInfo scope = T("PreviewConditions").GetMethod("ScopeNextSwitch", BindingFlags.NonPublic | BindingFlags.Static);
                object raw = Parse("checkpointmanager.Switch('renamed_entry') == 1");
                object Scoped(object value, bool allowed = true) => scope.Invoke(null, new[] { value, (object)allowed, false });
                State("Unknown", raw, "actual DLL plain count-switch preview remains protected");
                MethodInfo independent = T("PreviewConditions").GetMethods().Single(method => method.Name == "AreFlagWritesDisjoint" && method.GetParameters().Length == 2);
                Check((bool)independent.Invoke(null, new object[] { raw, new[] { "unrelated_flag" } }),
                    "actual DLL integer-count predicates are independent of FlagData writes without predicting the plain Switch result");
                object dependencyReader = T("PreviewConditions").GetMethod("CreateReader").Invoke(null, null);
                MethodInfo dependency = dependencyReader.GetType().GetMethod("ReadFlagWriteIndependence", BindingFlags.NonPublic | BindingFlags.Instance);
                Check((bool?)dependency.Invoke(dependencyReader, new object[] { raw, new[] { "unrelated_flag" }, null, false }) == true,
                    "actual DLL tri-state independence preserves proven count-schema independence");
                Check(dependency.Invoke(dependencyReader, new object[] { Parse("checkpointmanager.Condition('missing')"), new[] { "unrelated_flag" }, null, false }) == null,
                    "actual DLL unavailable definitions are unknown evidence instead of affected story conditions");
                var own = new FlagData { name = "consumed_topic", State = 0 };
                var ownItem = ConstantCondition(0, 0); ownItem._items[0]._value1._value[0]._checkType = 3; ownItem._items[0]._value1._value[0]._flag = own;
                CheckPointManager.Instance._condition = new ConditionResultConfig { _list = new List<ConditionResultData> { new ConditionResultData { name = "consumed", _items = new List<ConditionResultItem> { ownItem } } } };
                Check((bool?)dependency.Invoke(dependencyReader, new object[] { Parse("checkpointmanager.Condition('consumed')"), new[] { own.name }, null, false }) == false,
                    "actual DLL known consumed eligibility remains distinct from missing or unsupported evidence");
                object next = Scoped(raw);
                State("Met", next, "actual DLL a proven next invocation reads the next count without incrementing it");
                data._count = 1;
                State("NotMet", next, "actual DLL a completed entry updates from fresh stored count");
                data._count = 0;
                State("Unknown", Scoped(raw, false), "actual DLL an unsafe segment cannot opt into a next count");
                State("Unknown", Scoped(Parse("checkpointmanager.Switch('renamed_entry') == 1 and checkpointmanager.Switch('renamed_entry') == 1")), "actual DLL repeated invocations cannot share one next count");
                for (int op = 0; op < 6; op++)
                {
                    data._countItems[0]._compareOp = (StatCompareType)op;
                    State(op == 0 || op == 3 || op == 5 ? "Met" : "NotMet", next, "actual DLL native integer comparison " + op);
                }
                data._countItems[0]._compareOp = (StatCompareType)99;
                State("Unknown", next, "actual DLL unsupported comparisons remain unknown");
                data._countItems = null;
                State("Unknown", next, "actual DLL missing count definitions remain unknown");
                Check(!(bool)independent.Invoke(null, new object[] { raw, new[] { "unrelated_flag" } }),
                    "actual DLL missing counter schema cannot prove activity independence");
                data._countItems = new List<ValueCompareItem>();
                State("Met", next, "actual DLL an empty count table falls through to result one");
                foreach (int invalid in new[] { -1, int.MaxValue })
                { data._count = invalid; State("Unknown", next, "actual DLL invalid or overflowing count remains unknown"); }
                Check(data._count == int.MaxValue && executions == SwitchResultData.Executions && rng == UnityEngine.Random.Calls,
                    "actual DLL count preview calls no original evaluator or RNG and writes no stored counter");
            }
            finally { CheckPointManager.Instance = previous; }
        }

        public static int Run()
        {
            assertions = 0;
            Syntax(); CurrentFields(); NextEntryCount(); PartialLogic(); TalkResetFootprint(); LiteralNoOpWrites(); TraceAndDisplay(); FlatAlternativesRemainBounded();
            Console.WriteLine("PREVIEW readonly current condition backend passed");
            return assertions;
        }
    }
}

// Explicit engine boundaries for the candidate's reflected, raw observation
// path. The supplied MoonSharp RawGet(string) implementation is separately
// checked against the actual Fungus.dll metadata; this fixture never runs Lua.
namespace Mortal.Core
{
    public class FlagData : UnityEngine.Object
    {
        private int state;
        public int _default, StateReads;
        public int State { get { StateReads++; return state; } set { state = value; } }
    }
    public class FlagCollectionData
    {
        public List<FlagData> _list = new List<FlagData>();
        public static int ResetCalls;
        public void ResetData() { ResetCalls++; throw new InvalidOperationException("Preview reset talk flags"); }
    }
}
namespace Mortal.Story
{
    public class StatModifyManager
    {
        public Mortal.Core.FlagCollectionData _talkOptionFlag;
        public static int ResetCalls;
        public void ResetTalkFlag() { ResetCalls++; throw new InvalidOperationException("Preview reset talk flags"); }
    }
}
namespace MoonSharp.Interpreter
{
    public sealed class Script { public Table m_GlobalTable = new Table(); }
    public sealed class Table
    {
        public readonly Dictionary<string, DynValue> Values = new Dictionary<string, DynValue>();
        public int RawReads;
        public DynValue RawGet(string key) { RawReads++; DynValue value; return Values.TryGetValue(key, out value) ? value : null; }
    }
    public sealed class DynValue { public object m_Object; }
    public sealed class UserData
    {
        public object Object { get; private set; }
        public UserData(object value) { Object = value; }
    }
}
namespace Fungus
{
    public class LuaEnvironment
    {
        protected MoonSharp.Interpreter.Script interpreter;
        public static int Initializations;
        public LuaEnvironment(MoonSharp.Interpreter.Script script) { interpreter = script; }
        public void InitEnvironment() { Initializations++; throw new InvalidOperationException("Preview initialized a Lua environment"); }
    }
}
