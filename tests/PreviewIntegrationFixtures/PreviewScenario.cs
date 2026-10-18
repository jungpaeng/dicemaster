using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DiceMaster
{
    // These scenarios invoke copied production IL. No parser, arithmetic engine,
    // Action or formatter implementation exists in the boundary fixture.
    public static class PreviewScenario
    {
        static readonly Assembly Self = typeof(PreviewScenario).Assembly;
        static int assertions, caseId;
        static Type Type(string name) => Self.GetType("DiceMaster." + name, true);
        static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        static object Member(object value, string name) => value.GetType().GetField(name)?.GetValue(value) ?? value.GetType().GetProperty(name)?.GetValue(value);
        static IList Items(object value, string name) => (IList)Member(value, name);
        static object Invoke(Type type, object owner, string name, params object[] args)
        {
            try { return type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Invoke(owner, args); }
            catch (TargetInvocationException error) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException ?? error).Throw(); throw; }
        }
        static void Check(bool ok, string message) { assertions++; if (!ok) throw new InvalidOperationException(message); }
        static object Parser => Type("LuaParse.LuaParser").GetProperty("Instance").GetValue(null);
        static IList Parse(string code, string key = null)
        {
            key = key ?? "O_preview_case" + (++caseId);
            var result = (IDictionary)Invoke(Type("LuaParse.LuaParser"), Parser, "ExtractOptionActionsFromLua", "preview" + caseId, "--index:" + key + "\n" + code + "\n");
            Check(result.Contains(key), "Actual parser did not preserve index key " + key);
            return (IList)result[key];
        }
        static string Describe(IList actions) => (string)Invoke(Type("LuaParse.LuaParser"), null, "Description", actions);
        static string Text(string code) => Describe(Parse(code));
        static object Analyze(string script, Func<string, object> reader = null, object limits = null)
        {
            var method = Type("LiveLog.PreviewAnalyzer").GetMethod("Analyze");
            var parameters = method.GetParameters();
            object callback = null;
            if (reader != null)
            {
                var input = Expression.Parameter(typeof(string), "name");
                var call = Expression.Invoke(Expression.Constant(reader), input);
                callback = Expression.Lambda(parameters[2].ParameterType, Expression.Convert(call, parameters[2].ParameterType.GetGenericArguments()[1]), input).Compile();
            }
            try { return method.Invoke(null, new[] { (object)"fixture", script, callback, limits }); }
            catch (TargetInvocationException error) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException ?? error).Throw(); throw; }
        }
        static string AnalysisText(object analysis)
        {
            Invoke(Type("LiveLog.PreviewDisplay"), null, "Validate", analysis);
            return (string)Invoke(Type("LiveLog.PreviewDisplay"), null, "Describe", analysis);
        }
        static object Read(string kind, string source = null)
        {
            return Invoke(Type("LiveLog.ScriptReadResult"), null, kind, kind == "Success" ? new object[] { source } : new object[0]);
        }
        static void Contains(string actual, string wanted, string context) => Check(actual.Contains(wanted), context + ": missing " + wanted + ", actual=" + actual);
        static void Excludes(string actual, string unwanted, string context) => Check(!actual.Contains(unwanted), context + ": unexpected " + unwanted + ", actual=" + actual);
        static string Status(string name) => (string)Type("LiveLog.PreviewDisplay").GetField(name).GetValue(null);
        static void SupportedText(string text, string context)
        {
            Excludes(text, Status("PartialStatus"), context);
            Excludes(text, Status("UnsupportedStatus"), context);
        }
        public static int RunBaseline()
        {
            string[] cases = {
                "statmodifymanager.SetPlayer(\"money\", 100)",
                "statmodifymanager.Player(\"money\", 100, \"\", 1)",
                "statmodifymanager.Player(\"money\", reward, \"\", 1)",
                "statmodifymanager.Player(\"money\", 50 + 50, \"\", 1)",
                "statmodifymanager.Player(\"disposition\", 10, \"center\", 0)",
                "luamanager.SetBattleSkillLevel(\"skill\", 2)", "luamanager.NextMonth()", "luamanager.SetNextScript(\"next\")"
            };
            int[] counts = { 1, 1, 1, 1, 1, 0, 0, 0 };
            for (int i = 0; i < cases.Length; i++)
            {
                IList actions = Parse(cases[i]); string description = Describe(actions);
                Check(actions.Count == counts[i], "alpha.2 collection reproduction changed for " + cases[i]);
                if (i == 1 || i == 4) Contains(description, "+" + (i == 1 ? "100" : "10"), "alpha.2 incorrect/missing numeric baseline");
                else Check(description == "", "alpha.2 omission reproduction changed for " + cases[i]);
                Console.WriteLine("BASELINE case=" + i + " collected=" + actions.Count + " description=" + System.Text.Json.JsonSerializer.Serialize(description));
            }
            return assertions;
        }
        static void Arithmetic()
        {
            var method = Type("LiveLog.PreviewArithmetic").GetMethod("TryInteger");
            var accepted = new Dictionary<string, int> {
                ["50 + 50"] = 100, ["(2 + 3) * (9 - 1)"] = 40, ["-(+4) + 2*3"] = 2,
                ["8/2/2"] = 2, ["7/2*2"] = 7, ["2147483647"] = int.MaxValue, ["-2147483648"] = int.MinValue,
                ["2.5*2"] = 5, ["1e2 + .5*2"] = 101, ["50 -- comment\n + 50"] = 100
            };
            foreach (var pair in accepted)
            {
                object[] args = { pair.Key, 0, null };
                Check((bool)method.Invoke(null, args) && (int)args[1] == pair.Value, "Arithmetic precision/precedence: " + pair.Key);
            }
            foreach (string code in new[] { "reward", "a.b", "f()", "2%1", "2^3", "5/2", "1/0", "0/0", "2147483648", "-2147483649", "1e309", "NaN", "math.huge", "1 +", "(1+2", "2 3", "0x10" })
            {
                object[] args = { code, 123, null };
                Check(!(bool)method.Invoke(null, args) && !string.IsNullOrEmpty((string)args[2]), "Unsafe/nonconstant arithmetic accepted or reason missing: " + code);
            }
        }
        static void LexicalAndStates()
        {
            string text = Text("-- statmodifymanager.SetPlayer(\"secret_comment\", 99)\nlocal s = 'statmodifymanager.SetPlayer(\"secret_string\", 33)'\n--[=[ luamanager.NextMonth() ]=]\nlocal t = [==[statmodifymanager.Unknown(9)]==]\nstatmodifymanager.SetPlayer(\"money\",\n (50 + 50) * (2 - 1))");
            Contains(text, "100", "Multiline nested arithmetic"); Excludes(text, "secret", "Comments/strings"); SupportedText(text, "Comments do not create issues");
            text = Text("statmodifymanager.SetCharacter(\"hero,a\", 7)");
            Contains(text, "hero,a", "String comma remains one argument"); SupportedText(text, "String comma valid arity");
            text = Text("statmodifymanager.Player(\"money\", reward, \"\", 1)\nstatmodifymanager.SetFlag(\"G3000\", 2)");
            Check(text.StartsWith(Status("PartialStatus")), "Partial support marker must be before descriptions"); Contains(text, "G3000", "Known effects survive unsupported sibling");
            text = Text("statmodifymanager.Player(\"money\", reward, \"\", 1)");
            Check(text.StartsWith(Status("UnsupportedStatus")), "All unsupported must remain visible");
            Contains(Text("local x = 1\nstatmodifymanager.GetDisplayTime()\nstatmodifymanager.GetModifyString(\"money\",1)\nstatmodifymanager.UpdateSetPlayerStat(\"money\")"), "확인된 효과 없음", "Pure/display calls are not effects");
            Contains(Text("statmodifymanager.UnrecognizedEffect(\"x\")"), Status("UnsupportedStatus"), "Unknown manager effects cannot disappear");
            var flow = Analyze("statmodifymanager.SetFlag(\"before\",1)\nluamanager.ChangeScene(\"Combat\")\nstatmodifymanager.SetFlag(\"after\",2)");
            var effects = Items(flow, "Effects");
            Check(effects.Count == 3 && (string)Member(effects[0], "Name") == "SetFlag" && (string)Member(effects[1], "Name") == "ChangeScene" && (string)Member(effects[2], "Name") == "SetFlag", "Effect collection loses source order");
            text = AnalysisText(Analyze("if condition then\nstatmodifymanager.SetPlayer(\"money\",3)\nelse\nstatmodifymanager.SetPlayer(\"money\",4)\nend"));
            Contains(text, "[분기 1-A]", "First alternative explicitly grouped"); Contains(text, "[분기 1-B", "Else alternative explicitly grouped"); Contains(text, "3", "First branch preserved"); Contains(text, "4", "Else branch preserved");
            var loop = Analyze("statmodifymanager.SetFlag(\"safe\",1)\nfor i=1,3 do\nstatmodifymanager.SetPlayer(\"money\",10)\nend");
            Check(Items(loop, "Issues").Count != 0, "Unsupported loop boundary lost"); Contains(AnalysisText(loop), "safe", "Known effect before loop preserved");
            var ret = Analyze("statmodifymanager.SetFlag(\"before\",1)\nreturn\nstatmodifymanager.SetFlag(\"unreachable\",2)");
            Excludes(AnalysisText(ret), "unreachable", "Early return stops effects");
            Check(Items(Analyze("statmodifymanager[dynamic](\"x\")"), "Issues").Count != 0, "Dynamic manager invocation disappeared");
            var siblings = Analyze("if condition then\nlocal next=choose({})\nelse\nstatmodifymanager.SetFlag(\"alternate\",1)\nend");
            Contains(AnalysisText(siblings), "alternate", "A choice boundary does not erase a reachable sibling branch");
            Check(Items(Analyze("GetStoryText(unknown())"), "Issues").Count > 0, "Unknown call nested in pure display call disappeared");
            var block = Analyze("if condition then do statmodifymanager.SetFlag(\"uninterpreted\",1) end statmodifymanager.SetFlag(\"after_block\",2) end");
            text = AnalysisText(block);
            Excludes(text, "uninterpreted", "Unsupported block body must not be flattened"); Contains(text, "after_block", "Effect following unsupported block preserved");
            Check(Items(block, "Effects").Cast<object>().All(effect => (bool)Member(effect, "Conditional")), "Unsupported block following effects cannot become guaranteed");
            Check(HasReason(Analyze(string.Concat(Enumerable.Repeat("if condition then ",66)) + "statmodifymanager.SetFlag(\"deep\",1) " + string.Concat(Enumerable.Repeat("end ",66))), "parse-nesting-limit"), "Parser nesting guard absent");
            var shortCircuit = Analyze("local x = flag and statmodifymanager.Player(\"money\",1,\"\",1)");
            Check(Items(shortCircuit, "Effects").Count == 1 && (bool)Member(Items(shortCircuit, "Effects")[0], "Conditional"), "Short-circuit operand effect became unconditional");
            var headerCall = Analyze("if luamanager.NextRound() then statmodifymanager.Player(\"money\",1,\"\",1) end");
            Check(HasReason(headerCall, "condition-invocation-not-evaluated") && Items(headerCall, "Effects").Cast<object>().All(effect => (bool)Member(effect, "Conditional")), "Invocation in condition header must be explicit unsupported with conditional body");
            var headerFunction = Analyze("if helper(function() if flag then luamanager.NextRound() end end) then statmodifymanager.Player(\"money\",1,\"\",1) end");
            Check(HasReason(headerFunction, "unsupported-condition-function") && !Items(headerFunction, "Effects").Cast<object>().Any(effect => (string)Member(effect, "Name") == "NextRound"), "Anonymous condition function body leaked effects");
            foreach (string sugar in new[] { "helper \"value\"", "helper {}", "luamanager.NextMonth {}" })
                Check(HasReason(Analyze(sugar), "unsupported-call-syntax"), "Unsupported Lua call sugar disappeared: " + sugar);
            Check(Items(Analyze("luamanager.GetStoryText \"key\""), "Issues").Count == 0, "Known pure Lua call sugar became effect warning");
            Check(HasReason(Analyze("GetStoryText(helper \"x\")"), "nested-invocation-not-evaluated"), "Nested sugar invocation disappeared");
            Check(HasReason(Analyze("return\nluamanager.NextRound()"), "return-expression-not-evaluated"), "Multiline return expression executed in preview");
            foreach (string nestedChoice in new[] { "GetStoryText(choose({}))", "tostring(choose(opts))", "statmodifymanager.SetPlayer(\"money\",choose({}))", "if tostring(choose(opts)) then statmodifymanager.SetFlag(\"header_body\",1) end", "choose {}" })
            {
                var choice = Analyze(nestedChoice + "\nstatmodifymanager.SetFlag(\"beyond\",1)");
                Check((bool)Member(choice, "StoppedAtChoice") && !Items(choice, "Effects").Cast<object>().Any(effect => Items(effect,"Arguments").Cast<object>().Any(arg => ((string)arg).Contains("beyond"))), "Nested choice must stop before following effect: " + nestedChoice);
            }
            foreach (string bind in new[] { "local statmodifymanager=other", "statmodifymanager.SetFlag=helper" })
            {
                var bound = Analyze(bind + "\nstatmodifymanager.SetFlag(\"rebound\",1)\nluamanager.NextMonth()");
                Check(HasReason(bound, "manager-binding-not-evaluated") && !Items(bound, "Effects").Cast<object>().Any(effect => (string)Member(effect,"Owner") == "statmodifymanager"), "Rebound manager call fabricated as game effect");
            }
            var afterInvalid = Analyze("statmodifymanager.Player(\"money\",x,\"\",1)\nstatmodifymanager.SetFlag(\"after_invalid\",1)");
            Check((bool)Member(Items(afterInvalid,"Effects")[1],"Conditional"), "Following invalid call cannot imply guaranteed execution");
            Contains(Text("statmodifymanager.SetPlayer(--comment\n \"money\" --comment\n,100)"), "100", "String literal argument comments");
            foreach (string qualified in new[] { "other.luamanager.NextMonth()", "other.statmodifymanager.SetFlag(\"wrong_owner\",1)" })
            {
                var analysis = Analyze(qualified + "\nstatmodifymanager.SetPlayer(\"money\",100)");
                Check(HasReason(analysis, "qualified-manager-reference") && Items(analysis,"Effects").Count == 1, "Qualified owner suffix was mistaken for canonical game manager");
                Contains(AnalysisText(analysis), Status("PartialStatus"), "Qualified owner retains healthy later effect and issue");
            }
        }
        static void FunctionCoverage()
        {
            string[] stat = {
                "SetPlayer(\"money\",100)", "SetCharacter(\"hero\",2)", "RemoveBook(\"book\")", "RemoveSpecial(\"special\")",
                "Mission(\"mission\",\"stage\")", "IsTriggerSubMission(\"mission\",1)", "ModifyEnemyId(\"enemy\")", "ModifyEnemyPeople(\"enemy\",3,1)", "ModifyEnemyLevel(\"enemy\",2,1)",
                "AddDeadLib(\"hero\")", "AddEndGameLib(\"ending\")", "AddAchievementLib(\"achievement\")", "ResetTalkFlag()", "UpdateShopFlag()", "UpdateAssetStatus()", "AddStory(\"story\")"
            };
            string[] lua = {
                "ResetBattleSkill()", "SetPlayerBattleSkill(\"skill\",99)", "SetBattleSkillActive(\"skill\",2)", "SetBattleSkillLevel(\"skill\",2)",
                "SwapUpgradeItem(\"item\",-1)", "RestoreUpgradeItem(\"item\")", "RestoreAllUpgradeItems()", "SetGameTime(1,2,3)",
                "SetGameDayEnvironment(1)", "NextMonth()", "NextRound()", "SetMissionTime(\"mission\",99,2,3)"
            };
            foreach (string call in stat.Concat(lua))
            {
                bool isStat = stat.Contains(call); string text = Text((isStat ? "statmodifymanager." : "luamanager.") + call);
                SupportedText(text, "Required function: " + call); Excludes(text, "확인된 효과 없음", "Required function: " + call);
                Check(!string.IsNullOrWhiteSpace(text), "Required function silently omitted: " + call);
                int opening = call.IndexOf('(');
                string malformed = call.Substring(0, opening + 1) + (call.EndsWith("()") ? "1)" : ")");
                Contains(Text((isStat ? "statmodifymanager." : "luamanager.") + malformed), Status("UnsupportedStatus"), "Required function invalid arity: " + call);
            }
            Contains(Text("luamanager.SetPlayerBattleSkill(\"skill\",99)"), "빈", "Battle skill index ignored, first empty slot");
            Contains(Text("luamanager.SetBattleSkillActive(\"skill\",2)"), "비활성", "Only active=1 activates");
            Contains(Text("luamanager.SwapUpgradeItem(\"item\",-1)"), "유지", "Negative item level preserves level");
            Contains(Text("luamanager.SetMissionTime(\"mission\",99,2,3)"), "현재 시각 복사", "Mission time progresses relatively");
        }
        static string Plain(string text) => Regex.Replace(text, "<[^>]+>", "");
        static string LineWith(string text, string marker) => text.Split('\n').Single(line => line.Contains(marker));
        static void BranchPresentation()
        {
            Check(Status("PartialStatus") == "추가 영향 가능" && Status("UnsupportedStatus") == "결과는 진행 후 확인", "Unknown-result copy must distinguish extra effects from wholly unknown results");
            string partial = Text("statmodifymanager.SetFlag(\"known_effect\",1)\nunsupported_private_call()");
            Contains(partial, "known_effect", "Unknown additional effect retains known result");
            Contains(partial, "추가 영향 가능", "Unknown extra effect uses compact meaningful copy");
            Excludes(partial, "???", "Unknown result must not look like a broken placeholder");
            Excludes(partial, "미지원", "Parser implementation terms do not leak into result text");

            var analysis = Analyze("statmodifymanager.SetFlag(\"before_groups\",1)\n" +
                "if private_mental >= 100 then\n" +
                "statmodifymanager.SetFlag(\"route_a_first\",1)\nstatmodifymanager.SetFlag(\"route_a_second\",2)\n" +
                "elseif private_relationship > 20 then\n" +
                "statmodifymanager.SetFlag(\"route_b_first\",3)\nstatmodifymanager.SetFlag(\"route_b_second\",4)\n" +
                "else\nstatmodifymanager.SetFlag(\"route_else\",5)\nend\n" +
                "if independent_condition then statmodifymanager.SetFlag(\"independent_effect\",6) end\n" +
                "statmodifymanager.SetFlag(\"after_groups\",7)");
            var effects = Items(analysis, "Effects").Cast<object>().ToArray();
            Check(effects.Length == 8, "Branch presentation must retain all known effects");
            var paths = effects.Select(effect => Items(effect, "Branches")).ToArray();
            Check(paths[0].Count == 0 && paths[7].Count == 0, "Straight-line effects inherited branch membership");
            Check(paths.Skip(1).Take(6).All(path => path.Count == 1), "Each simple conditional effect needs one branch context");
            int group = (int)Member(paths[1][0], "Group");
            for (int i = 1; i <= 5; i++) Check((int)Member(paths[i][0], "Group") == group, "Alternatives in one if need a shared group");
            Check((int)Member(paths[1][0], "Alternative") == 0 && (int)Member(paths[2][0], "Alternative") == 0 &&
                (int)Member(paths[3][0], "Alternative") == 1 && (int)Member(paths[4][0], "Alternative") == 1 &&
                (int)Member(paths[5][0], "Alternative") == 2, "If/elseif/else alternatives must retain their order");
            Check(!(bool)Member(paths[1][0], "IsElse") && !(bool)Member(paths[3][0], "IsElse") && (bool)Member(paths[5][0], "IsElse"), "Only actual else may be described as the remaining case");
            Check((int)Member(paths[6][0], "Group") != group, "Independent if statements must not be presented as exclusive alternatives");
            Check(effects.Skip(1).Take(6).All(effect => (bool)Member(effect, "Conditional")), "Branch grouping must not claim a guaranteed result");
            string text = AnalysisText(analysis), plain = Plain(text);
            string a = LineWith(plain, "route_a_first"), b = LineWith(plain, "route_b_first");
            Contains(a, "[분기 1-A]", "First path label"); Contains(a, " | ", "Effects within one path are visually grouped");
            Contains(a, "route_a_second", "Consecutive same-path effects belong to one line");
            Contains(b, "[분기 1-B]", "Second path label"); Contains(b, "route_b_second", "Second path remains internally grouped");
            Check(!a.Contains("route_b_") && !b.Contains("route_a_"), "Mutually exclusive paths were combined into one reward set");
            string otherwise = LineWith(plain, "route_else"); Contains(otherwise, "분기 1-C", "Else keeps the same group"); Contains(otherwise, "그 외", "Else is labeled without inventing a predicate");
            Contains(LineWith(plain, "independent_effect"), "[분기 2-A]", "Independent if uses a new group");
            Excludes(LineWith(plain, "after_groups"), "분기", "Effects after end do not inherit a branch badge");
            foreach (string privateCondition in new[] { "private_mental", "private_relationship", "independent_condition", ">=", "능력치", "성공", "실패" })
                Excludes(plain, privateCondition, "Preview must not fabricate or expose unvalidated branch predicates");
            Check(Regex.Matches(plain, Regex.Escape("[분기 1-A]")).Count == 1, "Same-branch label repeated once per effect");
            var labels = Regex.Matches(text, @"<color=(#[0-9a-fA-F]{6,8})>\[분기 [^\]]+\]</color>");
            Check(labels.Count >= 4, "Each branch alternative needs a bounded color label");
            Check(labels[0].Groups[1].Value != labels[1].Groups[1].Value, "Alternative branch labels need distinct colors");
            foreach (Match label in labels)
            {
                string color = label.Groups[1].Value.ToLowerInvariant();
                Check(color != "#00ff00" && color != "#ff0000" && color != "#008000", "Branch color must not imply a success/failure verdict");
            }

            var nested = Analyze("if outer_private then\nstatmodifymanager.SetFlag(\"outer_only\",1)\n" +
                "if inner_private then statmodifymanager.SetFlag(\"nested_a\",2) else statmodifymanager.SetFlag(\"nested_b\",3) end\n" +
                "else statmodifymanager.SetFlag(\"outer_else\",4) end");
            var nestedEffects = Items(nested, "Effects").Cast<object>().ToArray();
            var nestedA = Items(nestedEffects[1], "Branches");
            var nestedB = Items(nestedEffects[2], "Branches");
            Check(nestedA.Count == 2 && nestedB.Count == 2, "Nested alternatives must retain the complete path");
            Check((int)Member(nestedA[0], "Group") == (int)Member(nestedB[0], "Group") &&
                (int)Member(nestedA[0], "Alternative") == (int)Member(nestedB[0], "Alternative") &&
                (int)Member(nestedA[1], "Group") == (int)Member(nestedB[1], "Group") &&
                (int)Member(nestedA[1], "Alternative") != (int)Member(nestedB[1], "Alternative"), "Nested siblings lost their shared outer path or distinct inner alternatives");
            string nestedText = Plain(AnalysisText(nested));
            Contains(LineWith(nestedText, "nested_a"), "[분기 1-A → 2-A]", "Nested first path readable");
            Contains(LineWith(nestedText, "nested_b"), "분기 1-A → 2-B", "Nested else path readable");
            Excludes(nestedText, "outer_private", "Nested condition source remains private");
            Excludes(nestedText, "inner_private", "Nested condition source remains private");

            foreach (string uncertain in new[] { "local x = private_flag and statmodifymanager.SetFlag(\"uncertain_effect\",1)", "unknown()\nstatmodifymanager.SetFlag(\"uncertain_effect\",1)" })
            {
                string uncertainText = AnalysisText(Analyze(uncertain));
                Contains(uncertainText, "조건부", "Unclassified conditional execution keeps its separate badge");
                Excludes(uncertainText, "[분기", "Unknown flow must not fabricate an if alternative");
            }
            var uncertainBranch = Analyze("unknown()\nif outer_predicate then if inner_predicate then statmodifymanager.SetFlag(\"uncertain_nested\",1) end end");
            object uncertainNested = Items(uncertainBranch, "Effects")[0];
            Check((bool)Member(uncertainNested, "ExecutionUncertain") && Items(uncertainNested, "Branches").Count == 2,
                "Unknown earlier execution must retain uncertainty independently from nested if membership");
            string uncertainBranchLine = LineWith(Plain(AnalysisText(uncertainBranch)), "uncertain_nested");
            Contains(uncertainBranchLine, "[분기 1-A → 2-A]", "Uncertain nested result retains its real branch path");
            Contains(uncertainBranchLine, "조건부", "Branch grouping must not erase inherited execution uncertainty");
        }
        static void Trace()
        {
            var scripts = new Dictionary<string,string> {
                ["one"] = "statmodifymanager.SetFlag(\"follow_one\",1)\nluamanager.SetNextScript(\"two\")",
                ["two"] = "statmodifymanager.SetFlag(\"follow_two\",2)\nlocal opts={}\nlocal choice=choose(opts)\nstatmodifymanager.SetFlag(\"after_choice\",3)"
            };
            int reads = 0;
            Func<string, object> reader = name => { reads++; return scripts.TryGetValue(name, out var code) ? Read("Success", code) : Read("Missing"); };
            var analysis = Analyze("statmodifymanager.SetFlag(\"root\",0)\nluamanager.SetNextScript(\"one\")", reader);
            string text = AnalysisText(analysis);
            Contains(text, "이어서 진행 시", "Followup provenance visible"); Excludes(text, "후속 스크립트", "Internal script terminology is absent");
            Contains(text, "follow_one", "First followup"); Contains(text, "follow_two", "Second followup"); Excludes(text, "after_choice", "Stop before next user choice");
            Contains(text, "이어서 진행 시 · 구간 1", "First serial followup is a story segment");
            Contains(text, "이어서 진행 시 · 구간 2", "Second serial followup is a later story segment");
            Excludes(text, "경로", "Serial followup scripts must not be presented as exclusive alternative routes");
            Check(text.IndexOf("root", StringComparison.Ordinal) < text.IndexOf("이어서 진행 시", StringComparison.Ordinal) &&
                text.IndexOf("이어서 진행 시", StringComparison.Ordinal) < text.IndexOf("follow_one", StringComparison.Ordinal), "Followup header must separate current effects from future effects");
            Excludes(text, "다음 스크립트", "Internal route registration is not a player-visible reward");
            Excludes(text, "SetNextScript", "Route API name is hidden while trace remains active");
            Check(reads == 2, "Multi-step trace must read each target exactly once");
            int healthyReads = reads;
            for (int i = 0; i < 10; i++) AnalysisText(analysis);
            Check(reads == healthyReads, "Healthy followup display re-read/reparsed resources");
            scripts["two"] = "statmodifymanager.SetFlag(\"before_cycle\",2)\nluamanager.SetNextScript(\"one\")";
            analysis = Analyze("luamanager.SetNextScript(\"one\")", reader); text = AnalysisText(analysis);
            Contains(text, Status("PartialStatus"), "Cycle retains partial support"); Contains(text, "before_cycle", "Cycle preserves preceding effects");
            Check(Items(analysis, "Issues").Count > 0, "Cycle stop cause absent");
            var empty = Analyze("luamanager.SetNextScript(\"empty\")", name => Read("Success", ""));
            Check(Items(empty, "Issues").Count == 0, "Valid empty script is not missing/read failure");
            Check(AnalysisText(empty) == "확인된 효과 없음" && Member(empty, "State").ToString() == "NoEffects", "Route registration alone must not claim a known reward");
            var missing = Analyze("luamanager.SetNextScript(\"missing\")", name => Read("Missing"));
            Check(Items(missing, "Issues").Count > 0, "Missing target disappears");
            foreach (string kind in new[] { "Waiting", "Failed" })
            {
                var failed = Analyze("statmodifymanager.SetFlag(\"partial\",1)\nluamanager.SetNextScript(\"broken\")", name => Read(kind));
                Check((bool)Member(failed, "HasReadFailures"), kind + " read treated as successful unsupported analysis");
                Check(Items(failed, "Effects").Count != 0, kind + " read erased normal effects");
            }
            var dynamic = Analyze("luamanager.SetNextScript(target)", reader);
            Check(Items(dynamic, "Issues").Count > 0, "Dynamic target silently ignored");
            Contains(AnalysisText(dynamic), Status("UnsupportedStatus"), "Hidden route detail must preserve dynamic-target uncertainty");
            reads = 0;
            AnalysisText(Analyze("luamanager.SetNextScript(\"one\")\nluamanager.SetNextScript(\"two\")", reader));
            Check(reads <= 2, "Overwritten next target made redundant reads");
            var literal = Analyze("luamanager.SetNextScript(\"https://example.test/payload\")", name => { throw new Exception("External target reached script reader"); });
            Check(Items(literal, "Issues").Count > 0, "External URL target allowed");
            var names = new List<string>();
            Analyze("luamanager.SetNextScript(\"old\")\nif condition then luamanager.SetNextScript(\"left\") else luamanager.SetNextScript(\"right\") end",
                name => { names.Add(name); return Read("Success", ""); });
            Check(!names.Contains("old") && names.Contains("left") && names.Contains("right"), "Every branch overwrite must remove obsolete next target");
            names.Clear();
            Analyze("luamanager.SetNextScript(\"early\")\nif condition then return else luamanager.SetNextScript(\"later\") end",
                name => { names.Add(name); return Read("Success", ""); });
            Check(names.Contains("early") && names.Contains("later"), "Early-return registration and continuing branch registration must both remain possible");
            names.Clear();
            Analyze("luamanager.SetNextScript(\"old\")\nluamanager.SetNextScript(target)", name => { names.Add(name); return Read("Success", ""); });
            Check(names.Count == 0, "Dynamic replacement must not retain stale literal next target");
            Analyze("luamanager.SetNextScript(\"old\")\nluamanager.SetNextScript(\"\")", name => { names.Add(name); return Read("Success", ""); });
            Check(names.Count == 0, "Empty next-script name must clear registration without reading");
            var commentedTarget = Analyze("luamanager.SetNextScript(--comment\n \"child\" --comment\n)", name => Read("Success", "statmodifymanager.SetFlag(\"comment_follow\",1)"));
            Contains(AnalysisText(commentedTarget), "comment_follow", "Commented string literal trace target");
        }
        static bool HasReason(object analysis, string reason) => Items(analysis, "Issues").Cast<object>().Any(issue => (string)Member(issue, "Reason") == reason);
        static object Limits(string name, int value)
        {
            object limits = Activator.CreateInstance(Type("LiveLog.PreviewTraceLimits"));
            limits.GetType().GetField(name).SetValue(limits, value); return limits;
        }
        static void LimitsAndRetry()
        {
            string source = "statmodifymanager.SetFlag(\"bounded\",1)";
            object measured = Analyze(source);
            foreach (var boundary in new[] { ("MaxCharacters", "CharacterCount", "trace-character-limit"), ("MaxTokens", "TokenCount", "trace-token-limit"), ("MaxWork", "WorkCount", "trace-work-limit"), ("MaxScripts", "ScriptCount", "trace-script-limit") })
            {
                int count = (int)Member(measured, boundary.Item2);
                for (int delta = -1; delta <= 1; delta++)
                {
                    object analysis = Analyze(source, null, Limits(boundary.Item1, count + delta));
                    Check(HasReason(analysis, boundary.Item3) == (delta < 0), boundary.Item1 + " measured below/equal/over boundary " + count + " delta=" + delta);
                }
            }
            for (int depth = 0; depth <= 2; depth++)
            {
                var analysis = Analyze("luamanager.SetNextScript(\"child\")", name => Read("Success", source), Limits("MaxDepth", depth));
                Check(HasReason(analysis, "trace-depth-limit") == (depth == 0), "Depth root/child boundary " + depth);
            }
            var plugin = new DicePlugin { showLogWindow = true }; DicePlugin.Instance = plugin;
            int reads = 0; bool fail = true;
            DataHelper.PreviewScriptReader = name => { reads++; return fail ? Read("Failed") : Read("Success", "statmodifymanager.SetFlag(\"recovered\",2)"); };
            Invoke(Type("LiveLog.LogBridge"), null, "ResetContext");
            Invoke(Type("LiveLog.LogBridge"), null, "NewLua", "--index:O_preview_readretry\nstatmodifymanager.SetFlag(\"safe_partial\",1)\nluamanager.SetNextScript(\"child\")", "retry-preview");
            int analyzedReads = reads;
            UnityEngine.Time.unscaledTime = 0;
            Invoke(Type("LiveLog.LogBridge"), null, "Tick", plugin, false);
            Check(plugin.optionEntries.Count == 1, "Read failure erased option row");
            Contains(plugin.optionEntries[0].Description, "읽기 실패", "Initial real script read failure status");
            Contains(plugin.optionEntries[0].Description, "safe_partial", "Read failure retains parsed effect");
            int firstReads = reads;
            for (int i = 1; i < 20; i++)
            {
                UnityEngine.Time.unscaledTime = i * .25f;
                Invoke(Type("LiveLog.LogBridge"), null, "Tick", plugin, false);
            }
            Check(reads == firstReads && analyzedReads == firstReads, "Failed reads retried before five-second boundary");
            UnityEngine.Time.unscaledTime = 5;
            Invoke(Type("LiveLog.LogBridge"), null, "Tick", plugin, false);
            Check(reads == firstReads + 1, "Failed script read was not retried at five seconds");
            plugin.showLogWindow = false; UnityEngine.Time.unscaledTime = 20;
            Invoke(Type("LiveLog.LogBridge"), null, "Tick", plugin, false);
            Check(reads == firstReads + 1, "Hidden log retried failed script read");
            plugin.showLogWindow = true; fail = false;
            Invoke(Type("LiveLog.LogBridge"), null, "Tick", plugin, true);
            Contains(plugin.optionEntries[0].Description, "recovered", "Manual refresh immediately recovers read boundary");
            Excludes(plugin.optionEntries[0].Description, "실패", "Recovered status cleared");
            int recoveredReads = reads;
            UnityEngine.Time.unscaledTime = 21;
            Invoke(Type("LiveLog.LogBridge"), null, "Tick", plugin, false);
            Check(reads == recoveredReads, "Healthy normal display reparsed followup");
            DataHelper.PreviewScriptReader = null;
        }
        static void State()
        {
            Invoke(Type("LiveLog.PreviewState"), null, "ReadFinalValue", new Mortal.Core.GameStat());
            Mortal.Core.PlayerStatManagerData.Ready(true);
            Invoke(Type("LiveLog.PreviewState"), null, "ReadPlayerStat", "disposition");
            var actions = Parse("statmodifymanager.Player(\"disposition\",10,\"center\",0)");
            foreach (var pair in new[] { (40, "+10"), (48, "+2"), (60, "-10"), (50, "0") })
            {
                DataHelper.CurrentValue = pair.Item1;
                string text = Describe(actions);
                Contains(text, "예상 변경량", "Center is explicitly an estimate");
                Contains(text, pair.Item2, "Center follows current FinalValue " + pair.Item1);
            }
            actions = Parse("statmodifymanager.Player(\"disposition\",10,\"mental\",0)");
            DataHelper.CurrentValue = 20; Contains(Describe(actions), "+9", "Mental penalty applies at 20");
            DataHelper.CurrentValue = 21; Contains(Describe(actions), "+10", "Mental boundary above 20");
            DataHelper.CurrentValue = 48;
            var center = Parse("statmodifymanager.Player(\"disposition\",10,\"center\",0)");
            var stat = Mortal.Core.PlayerStatManagerData.Primary;
            stat._statAdditionData = new[] { new Mortal.Core.StatConvertCollectionData { List = new object[] { new object() } } };
            int reads = stat.Reads;
            string fallback = Describe(center);
            Excludes(fallback, "예상 변경량", "Unknown converter uses semantic fallback");
            Check(stat.Reads == reads, "Unknown converter reached original FinalValue getter");
            stat._statAdditionData = null;
            Contains(Text("statmodifymanager.Player(\"disposition\",10,\"center\",0)\nunknown()"), "예상 변경량", "Later unsupported call does not invalidate earlier safe estimate");
            Excludes(Text("unknown()\nstatmodifymanager.Player(\"disposition\",10,\"center\",0)"), "예상 변경량", "Earlier unsupported call invalidates later snapshot estimate");
            string preceding = Text("statmodifymanager.SetPlayer(\"disposition\",40)\nstatmodifymanager.Player(\"disposition\",10,\"center\",0)");
            Excludes(preceding, "예상 변경량", "Unknown final application of preceding effect forbids snapshot estimate");
            string conditional = AnalysisText(Analyze("if condition then statmodifymanager.Player(\"disposition\",10,\"center\",0) end"));
            Excludes(conditional, "예상 변경량", "Conditional current-state effect uses semantic fallback");
            Mortal.Core.PlayerStatManagerData.Ready(false);
            bool waiting = false;
            try { Text("statmodifymanager.Player(\"disposition\",10,\"center\",0)"); }
            catch (Exception error) { waiting = error.GetType().FullName == "DiceMaster.LiveLog.PreviewReadException" && (bool)Member(error, "IsWaiting"); }
            Check(waiting, "Missing player state is waiting rather than cached unsupported");
            Mortal.Core.PlayerStatManagerData.Ready(true);
            Check(Mortal.Core.PlayerStatManagerData.InstanceReads == 0, "Display called mutable singleton getter");
        }
        static void NamingSafety()
        {
            Mortal.Core.PlayerStatManagerData.Ready(false);
            Mortal.Core.MissionManagerData.Instance = null;
            int playerReads = Mortal.Core.PlayerStatManagerData.InstanceReads;
            int itemReads = Mortal.Core.ItemDatabase.InstanceReads;
            int missionReads = Mortal.Core.MissionManagerData.InstanceReads;
            Contains(Text("statmodifymanager.RemoveBook(\"missing_book\")"),"missing_book","Missing item name keeps literal key");
            Contains(Text("statmodifymanager.AddTalent(\"missing_talent\",1)"),"missing_talent","Missing talent name keeps literal key");
            Contains(Text("statmodifymanager.ModifyEnemyId(\"missing_enemy\")"),"missing_enemy","Missing enemy name keeps literal key");
            Contains(Text("statmodifymanager.SetFlag(\"G987654\",1)"),"G987654","Missing flag name keeps literal key");
            Check(Mortal.Core.PlayerStatManagerData.InstanceReads == playerReads && Mortal.Core.ItemDatabase.InstanceReads == itemReads && Mortal.Core.MissionManagerData.InstanceReads == missionReads,
                "Decorative preview names called game singleton getter");
            Check(Mortal.Core.ItemDatabase.IsAbsent,"Decorative preview name initialized missing ItemDatabase");
            Check(typeof(Mortal.Core.MissionManagerData).GetField("_instance",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)==null &&
                typeof(Mortal.Core.PlayerStatManagerData).GetField("_instance",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)==null,"Decorative naming changed game singleton backing field");
            Mortal.Core.PlayerStatManagerData.Ready(true);
            Mortal.Core.MissionManagerData.Instance = new Mortal.Core.MissionManagerData();
        }
        static void Cache()
        {
            string key = "O_preview_cache";
            string script = "--index:" + key + "\nstatmodifymanager.Player(\"money\", secret_var, \"\",1)\n";
            var parserType = Type("LuaParse.LuaParser");
            var first = (IDictionary)Invoke(parserType, Parser, "ExtractOptionActionsFromLua", "cached-preview", script);
            object actions = first[key];
            int logs = DicePlugin.Warnings.Count + DicePlugin.Info.Count + DicePlugin.Errors.Count;
            for (int i = 0; i < 10; i++)
            {
                var next = (IDictionary)Invoke(parserType, Parser, "ExtractOptionActionsFromLua", "cached-preview", script);
                Check(ReferenceEquals(actions, next[key]), "Unsupported analysis was retried/rebuilt instead of cached");
                Contains(Describe((IList)actions), Status("UnsupportedStatus"), "Cached unsupported status");
            }
            int afterwards = DicePlugin.Warnings.Count + DicePlugin.Info.Count + DicePlugin.Errors.Count;
            Check(afterwards == logs, "Repeated unsupported display emitted duplicate diagnostics");
            Check(!DicePlugin.Warnings.Concat(DicePlugin.Info).Concat(DicePlugin.Errors).Any(log => log.Contains("secret_var")), "Diagnostic leaked Lua expression/source");
        }
        static void Benchmark()
        {
            string script = string.Join("\n", Enumerable.Range(0, 200).Select(i => "statmodifymanager.SetPlayer(\"money\", (50 + 50) * 2)"));
            AnalysisText(Analyze(script));
            long allocated = GC.GetAllocatedBytesForCurrentThread(), started = Stopwatch.GetTimestamp();
            for (int i=0;i<100;i++) AnalysisText(Analyze(script));
            Console.WriteLine("SYNTHETIC PREVIEW BENCHMARK runtime=" + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription + " os=" + System.Runtime.InteropServices.RuntimeInformation.OSDescription + " source_chars=" + script.Length + " effects=200 iterations=100 elapsed_ms=" + Stopwatch.GetElapsedTime(started).TotalMilliseconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture) + " allocated_bytes=" + (GC.GetAllocatedBytesForCurrentThread()-allocated));
        }
        static void EventRetry()
        {
            var plugin = new DicePlugin { showLogWindow = true, currentTab = "이벤트" }; DicePlugin.Instance = plugin;
            int childReads = 0; bool fail = true;
            DataHelper.PreviewScriptReader = name => name == "event_event" ?
                Read("Success", "statmodifymanager.SetFlag(\"event_partial\",1)\nluamanager.SetNextScript(\"event_child\")") :
                name == "event_child" ? EventChild() : Read("Success", "");
            object EventChild() { childReads++; return fail ? Read("Failed") : Read("Success", "statmodifymanager.SetFlag(\"event_recovered\",2)"); }
            Invoke(Type("LiveLog.LogBridge"), null, "ResetContext");
            UnityEngine.Time.unscaledTime = 0;
            Invoke(Type("LiveLog.LogBridge"), null, "NewLua", "--index:O_event_retry\ncheckpointmanager.Position(\"fixture_position\")", "event-retry-root");
            Invoke(Type("LiveLog.LogBridge"), null, "Tick", plugin, false);
            Check(plugin.eventEntries.Count == 1, "Static event read failure erased event row");
            Contains(plugin.eventEntries[0].Description, "event_partial", "Static event failure preserves actual parsed effect");
            Contains(plugin.eventEntries[0].Description, "읽기 실패", "Static event failure status");
            int first = childReads;
            for (int i=1;i<20;i++) { UnityEngine.Time.unscaledTime=i*.25f; Invoke(Type("LiveLog.LogBridge"),null,"Tick",plugin,false); }
            Check(childReads == first, "Event fixed definition retried before five seconds");
            UnityEngine.Time.unscaledTime=5; Invoke(Type("LiveLog.LogBridge"),null,"Tick",plugin,false);
            Check(childReads == first+1, "Event fixed definition failed to retry at five seconds");
            fail=false; UnityEngine.Time.unscaledTime=10; Invoke(Type("LiveLog.LogBridge"),null,"Tick",plugin,false);
            Contains(plugin.eventEntries[0].Description,"event_recovered","Actual static event read recovery");
            Excludes(plugin.eventEntries[0].Description,"실패","Event recovered status removed");
            int recovered=childReads;
            UnityEngine.Time.unscaledTime=10.5f; Invoke(Type("LiveLog.LogBridge"),null,"Tick",plugin,false);
            Check(childReads == recovered,"Healthy static event reparsed its script");
            Check(OptionInfoPatch.DisplayRefreshDepth==0,"Event failure leaked display guard");
            DataHelper.PreviewScriptReader=null;
        }
        public static int Run()
        {
            Check((string)Invoke(Type("LiveLog.LogBridge"), null, "CallDisplayHelper", "DiceMaster.PlayerinfoTranslations", "TranslatePlayerstats", new object[] { "money" }) == "money", "Translation boundary is not bound");
            Arithmetic(); Console.WriteLine("PREVIEW arithmetic passed");
            LexicalAndStates(); Console.WriteLine("PREVIEW lexical/state classification passed");
            FunctionCoverage(); Console.WriteLine("PREVIEW function coverage passed");
            BranchPresentation(); Console.WriteLine("PREVIEW branch presentation passed");
            Trace(); Console.WriteLine("PREVIEW followup trace passed");
            NamingSafety(); State(); Console.WriteLine("PREVIEW state-dependent estimates passed");
            Cache(); LimitsAndRetry(); EventRetry(); Benchmark();
            Check(UnityEngine.Random.Calls == 0 && Mortal.Core.StatGroupVariable.OriginalReads == 0 && Mortal.Core.StatCompareItem.OriginalReads == 0, "Preview called original game RNG/evaluation chain");
            return assertions;
        }
    }
}
