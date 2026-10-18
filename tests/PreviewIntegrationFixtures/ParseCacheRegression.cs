using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace DiceMaster
{
    // The same synthetic assertions run against production cache/tokenizer IL
    // copied into the fixture assembly. No script or state API is executed.
    public static class ParseCacheRegression
    {
        static readonly Assembly Self = typeof(ParseCacheRegression).Assembly;
        static int assertions;
        static Type T(string name) => Self.GetType("DiceMaster.LiveLog." + name, true);
        static object Cache(int entries, int characters, int tokens) => Activator.CreateInstance(T("PreviewParseCache"), entries, characters, tokens);
        static object Value(object value, string name) => value.GetType().GetField(name)?.GetValue(value) ?? value.GetType().GetProperty(name).GetValue(value);
        static long Count(object cache, string name) => Convert.ToInt64(Value(cache, name));
        static IList Tokens(object cache, string source) => (IList)cache.GetType().GetMethod("Tokens").Invoke(cache, new object[] { source });
        static object Plan(object cache, string source) => cache.GetType().GetMethod("Plan").Invoke(cache, new object[] { source });
        static void Clear(object cache) => cache.GetType().GetMethod("Clear").Invoke(cache, null);
        static void Check(bool value, string context) { assertions++; if (!value) throw new InvalidOperationException(context); }
        static void SameTokens(IList actual, IList expected, string context)
        {
            Check(actual.Count == expected.Count, context + ": token count");
            for (int i = 0; i < expected.Count; i++)
                foreach (string name in new[] { "Text", "Start", "End", "IsString", "IsIdentifier" })
                    Check(Equals(Value(actual[i], name), Value(expected[i], name)), context + ": lexical metadata " + name);
        }
        static IList Direct(string source) => (IList)T("PreviewSyntax").GetMethod("Tokenize").Invoke(null, new object[] { source });
        static object Read(string state, string source = null) => T("ScriptReadResult").GetMethod(state).Invoke(null, state == "Success" ? new object[] { source } : new object[0]);
        static object Limits(string field, int value)
        {
            var limits = Activator.CreateInstance(T("PreviewTraceLimits"));
            limits.GetType().GetField(field).SetValue(limits, value);
            return limits;
        }
        static object Analyze(object cache, string script, Func<string, object> reader = null, object limits = null)
        {
            var method = T("PreviewAnalyzer").GetMethod("AnalyzeWithCache");
            object callback = null;
            if (reader != null)
            {
                var input = Expression.Parameter(typeof(string), "name");
                var resultType = method.GetParameters()[2].ParameterType.GetGenericArguments()[1];
                callback = Expression.Lambda(method.GetParameters()[2].ParameterType,
                    Expression.Convert(Expression.Invoke(Expression.Constant(reader), input), resultType), input).Compile();
            }
            return method.Invoke(null, new object[] { "parse-cache-root", script, callback, limits, cache });
        }
        // Compare the production result, including effects, issues and logical
        // work accounting. Cache costs and elapsed time are deliberately absent.
        static string Result(object analysis) => JsonSerializer.Serialize(analysis, analysis.GetType(), new JsonSerializerOptions { IncludeFields = true });

        static void ProductionReuse()
        {
            Check(T("PreviewAnalyzer").GetMethod("Analyze").GetParameters().Length == 4, "Cache integration changed the original reflection entry point");
            var cache = Cache(8, 4096, 1024);
            string root = "statmodifymanager.SetFlag(\"root\",1)\nluamanager.SetNextScript(\"child\")";
            string child = "statmodifymanager.SetFlag(\"child\",2)";
            int reads = 0;
            Func<string, object> reader = name => { reads++; return Read("Success", child); };
            var cold = Analyze(cache, root, reader);
            long tokenizations = Count(cache, "Tokenizations"), hits = Count(cache, "CacheHits");
            long syntaxBuilds = Count(cache, "SyntaxBuilds"), validationBuilds = Count(cache, "ValidationBuilds");
            var warm = Analyze(cache, root, reader);
            Check(Result(cold) == Result(warm), "Warm lexical reuse changed effects, issues or analysis budgets");
            Check(Count(cache, "Tokenizations") == tokenizations && Count(cache, "CacheHits") >= hits + 2, "Production analyzer did not reuse root and followup tokens");
            Check(reads == 2, "Lexical reuse cached a script read or its analysis context");
            Check(syntaxBuilds > 0 && Count(cache, "SyntaxBuilds") == syntaxBuilds && Count(cache, "SyntaxHits") > 0, "A shared source rebuilt its static call/argument plan");
            Check(validationBuilds == 3 && Count(cache, "ValidationBuilds") == validationBuilds && Count(cache, "ValidationHits") == 3, "A shared source repeated constant effect validation");

            // Effect lists are analysis-owned. A consumer changing one result
            // must never change another option's static template or constants.
            var previousEffect = ((IList)Value(cold, "Effects"))[0];
            ((IList)Value(previousEffect, "Arguments"))[0] = "consumer_changed_argument";
            ((IList)Value(previousEffect, "Constants"))[0] = "consumer_changed_constant";
            Check(Result(warm) == Result(Analyze(cache, root, reader)), "An analysis consumer mutated a reusable argument or constant template");
            string invalid = "statmodifymanager.SetFlag(unknown,1)";
            var invalidCold = Analyze(cache, invalid);
            Check(Result(invalidCold) == Result(Analyze(cache, invalid)) && ((IList)Value(invalidCold, "Issues")).Count > 0, "A cached validation error lost its analysis-specific issue");

            string sugar = "choose 'past_cap'\nstatmodifymanager.SetFlag(\"outside\",1)";
            var owned = Tokens(cache, sugar);
            int originalCount = owned.Count;
            var small = Analyze(cache, sugar, null, Limits("MaxTokens", 1));
            Check(!(bool)Value(small, "StoppedAtChoice"), "Token-limited traversal used a sugar argument outside its prefix");
            Check((int)Value(small, "TokenCount") == 1 && ((IList)Value(small, "Effects")).Count == 0, "Token prefix enlarged its logical budget or collected outside effects");
            Check(owned.Count == originalCount && originalCount > 1, "Token-limited traversal removed tokens from its shared cache list");
            var full = Analyze(cache, sugar);
            Check((bool)Value(full, "StoppedAtChoice"), "Earlier token-limited analysis damaged a subsequent full analysis");
            Check(Result(small) == Result(Analyze(cache, sugar, null, Limits("MaxTokens", 1))), "A larger-budget analysis changed a later small-budget result");
            Check(ReferenceEquals(owned, Tokens(cache, sugar)) && owned.Count == originalCount, "Budget changes replaced or truncated reusable lexical data");

            var shortCharacters = Analyze(cache, root, reader, Limits("MaxCharacters", 1));
            Check((int)Value(shortCharacters, "CharacterCount") == 1 && ((IList)Value(shortCharacters, "Effects")).Count == 0, "Warm full-source cache bypassed the character prefix guard");
            Check(Result(shortCharacters) == Result(Analyze(cache, root, reader, Limits("MaxCharacters", 1))), "Character-prefix cold and warm analysis differ");

            bool waiting = true;
            var retry = Analyze(cache, root, name => waiting ? Read("Waiting") : Read("Success", child));
            Check((bool)Value(retry, "HasReadFailures"), "Retry fixture did not reach its waiting read boundary");
            hits = Count(cache, "CacheHits");
            waiting = false;
            T("PreviewAnalyzer").GetMethod("RetryReads").Invoke(null, new[] { retry });
            Check(!(bool)Value(retry, "HasReadFailures") && ((IList)Value(retry, "Effects")).Count == 3, "A waiting script read became sticky in the lexical cache");
            Check(Count(cache, "CacheHits") >= hits + 2, "Read retry discarded reusable lexical data");
        }

        static void StaticPlanBoundaries()
        {
            var cache = Cache(4, 8192, 4096);
            string source = "if flag == 'branch' then statmodifymanager.SetFlag(\"x\", 1 + (2 * 3)) elseif flag == 2 then f(g(1,2), {a=3}) else choose 'stop' end";
            var plan = Plan(cache, source);
            Check(ReferenceEquals(plan, Plan(cache, new string(source.ToCharArray()))), "Equal source content did not reuse its static plan");
            var tokens = (IList)Value(plan, "Tokens");
            var syntax = T("PreviewSyntax");
            foreach (string name in new[] { "TryCall", "TrySugarCall" })
            {
                for (int index = 0; index < tokens.Count; index++)
                {
                    object[] expected = { tokens, index, null, null, 0, 0 };
                    object[] actual = { index, null, null, 0, 0 };
                    var expectedFound = syntax.GetMethod(name).Invoke(null, expected);
                    var actualFound = plan.GetType().GetMethod(name).Invoke(plan, actual);
                    Check(Equals(expectedFound, actualFound), name + " plan changed call recognition");
                    if (!(bool)expectedFound) continue;
                    for (int field = 0; field < 4; field++) Check(Equals(expected[field + 2], actual[field + 1]), name + " plan changed call metadata");
                    if (name != "TryCall" || (int)actual[4] < 0) continue;
                    int open = (int)actual[3], close = (int)actual[4];
                    var directArguments = (IList)syntax.GetMethod("Arguments").Invoke(null, new object[] { source, tokens, open, close });
                    var plannedArguments = (IList)plan.GetType().GetMethod("Arguments").Invoke(plan, new object[] { open, close });
                    Check(JsonSerializer.Serialize(directArguments) == JsonSerializer.Serialize(plannedArguments), "Static argument normalization changed nested or arithmetic input");
                    plannedArguments.Add("consumer mutation");
                    Check(JsonSerializer.Serialize(directArguments) == JsonSerializer.Serialize(plan.GetType().GetMethod("Arguments").Invoke(plan, new object[] { open, close })), "Argument access exposed a reusable mutable list");
                }
            }
            object[] branchArguments = { 0, 0 };
            var branchList = (IList)plan.GetType().GetMethod("Branches").Invoke(plan, branchArguments);
            var firstBranch = branchList[0];
            firstBranch.GetType().GetField("Start").SetValue(firstBranch, -77);
            branchList.Clear();
            var freshBranches = (IList)plan.GetType().GetMethod("Branches").Invoke(plan, new object[] { 0, 0 });
            Check(freshBranches.Count == 3 && (int)Value(freshBranches[0], "Start") >= 0, "Branch access exposed reusable topology or branch nodes");
            Check(Count(cache, "MemoUnits") <= tokens.Count * 8L && Count(cache, "MemoCharacters") <= source.Length * 4L, "Static memo ownership exceeded its source/token bounds");

            // The same call opener is balanced in the complete plan, and
            // unbalanced in a private prefix. A warm full plan cannot leak it.
            int callOpen = -1;
            for (int i = 0; i < tokens.Count; i++) if ((string)Value(tokens[i], "Text") == "(") { callOpen = i; break; }
            int fullClose = (int)plan.GetType().GetMethod("MatchClose").Invoke(plan, new object[] { callOpen, -1 });
            Check(fullClose > callOpen && (int)plan.GetType().GetMethod("MatchClose").Invoke(plan, new object[] { callOpen, fullClose }) == -1, "Static matching ignored its private token-prefix limit");
            long builds = Count(cache, "PlanBuilds"), hits = Count(cache, "SyntaxHits");
            Clear(cache);
            Check(Count(cache, "MemoUnits") == 0 && Count(cache, "MemoCharacters") == 0 && Count(cache, "SyntaxHits") == hits, "Context clear retained static payload or reset lifetime work counters");
            Check(!ReferenceEquals(plan, Plan(cache, source)) && Count(cache, "PlanBuilds") == builds + 1, "Context clear retained a previous static plan");

            cache = Cache(1, 512, 128);
            plan = Plan(cache, "f(1)");
            Plan(cache, "f(2)");
            Check(!ReferenceEquals(plan, Plan(cache, "f(1)")) && Count(cache, "Entries") == 1, "LRU eviction retained an orphaned static plan");
            cache = Cache(0, 512, 128);
            Check(!ReferenceEquals(Plan(cache, "f(1)"), Plan(cache, "f(1)")) && Count(cache, "MemoUnits") == 0, "Disabled cache retained a static plan outside its source budget");
        }

        static void ParserReuseAndClear()
        {
            var parserType = Self.GetType("DiceMaster.LuaParse.LuaParser", true);
            var parser = parserType.GetProperty("Instance").GetValue(null);
            var clear = parserType.GetMethod("ClearCaches");
            var cache = parserType.GetField("previewParseCache", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(parser);
            var previousReader = DataHelper.PreviewScriptReader;
            int reads = 0;
            try
            {
                clear.Invoke(parser, null);
                Check(Count(cache, "Entries") == 0, "Parser ClearCaches retained tokens from a previous context");
                long tokenizations = Count(cache, "Tokenizations"), hits = Count(cache, "CacheHits");
                DataHelper.PreviewScriptReader = name => { reads++; return Read("Success", "statmodifymanager.SetFlag(\"shared_child\",3)"); };
                string source = "--index:O_parsecache_first\nstatmodifymanager.SetFlag(\"first\",1)\nluamanager.SetNextScript(\"shared_child\")\n" +
                    "--index:O_parsecache_second\nstatmodifymanager.SetFlag(\"second\",2)\nluamanager.SetNextScript(\"shared_child\")\n";
                var options = (IDictionary)parserType.GetMethod("ExtractOptionActionsFromLua").Invoke(parser, new object[] { "parse-cache-integration", source });
                Check(options.Count == 2 && reads == 2, "Parser reuse changed option identities or cached per-option script reads");
                Check(Count(cache, "Tokenizations") == tokenizations + 3 && Count(cache, "CacheHits") >= hits + 1, "Separate options did not reuse their common followup's lexical data");
                foreach (IList actions in options.Values)
                    Check(((IList)Value(Value(actions[0], "Analysis"), "Effects")).Count == 3, "Shared followup reuse changed collected effects");
                clear.Invoke(parser, null);
                Check(Count(cache, "Entries") == 0 && Count(cache, "CharacterCount") == 0 && Count(cache, "TokenCount") == 0, "Parser context clear retained shared lexical payload");

                // Event definitions share the owning parser's finite cache,
                // rather than retaining a fresh cache for every definition.
                tokenizations = Count(cache, "Tokenizations"); hits = Count(cache, "CacheHits"); reads = 0;
                DataHelper.PreviewScriptReader = name =>
                {
                    reads++;
                    if (name == "cache_event_child") return Read("Success", "statmodifymanager.SetFlag(\"event_child\",3)");
                    return Read("Success", "statmodifymanager.SetFlag(\"" + name + "\",1)\nluamanager.SetNextScript(\"cache_event_child\")");
                };
                var events = parserType.GetMethod("ExtractActionsFromLua");
                foreach (string name in new[] { "cache_event_first", "cache_event_second" })
                {
                    var actions = (IList)events.Invoke(parser, new object[] { name });
                    var analysis = Value(actions[0], "Analysis");
                    Check(((IList)Value(analysis, "Effects")).Count == 3 && !(bool)Value(analysis, "SnapshotAllowed"), "Event lexical reuse changed effects or snapshot policy");
                    Check(ReferenceEquals(cache, analysis.GetType().GetField("ParseCache", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(analysis)), "Event definition retained a separate lexical cache");
                }
                Check(reads == 4 && Count(cache, "Tokenizations") == tokenizations + 3 && Count(cache, "CacheHits") >= hits + 1, "Event definitions did not share child tokens while preserving independent reads");
                Check(Count(cache, "Entries") == 3, "Event lexical cache retained per-definition duplicate payload");
            }
            finally
            {
                DataHelper.PreviewScriptReader = previousReader;
                clear.Invoke(parser, null);
            }
        }

        public static int Run()
        {
            assertions = 0;
            var cache = Cache(2, 6, 6);
            var first = Tokens(cache, "x=1");
            var equalContent = Tokens(cache, new string(new[] { 'x', '=', '1' }));
            Check(ReferenceEquals(first, equalContent), "Equal source content was tokenized twice");
            Check(Count(cache, "Tokenizations") == 1 && Count(cache, "CacheHits") == 1, "Cold/warm cache work counters changed");
            SameTokens(equalContent, Direct("x=1"), "Warm lexical result");
            var second = Tokens(cache, "x=2");
            Check(!ReferenceEquals(first, second) && (string)Value(second[2], "Text") == "2", "Different source content reused a stale lexical result");
            Tokens(cache, "x=1"); // Touch the first entry: the second must go.
            Tokens(cache, "x=3");
            Check(Count(cache, "Entries") == 2 && Count(cache, "CharacterCount") == 6 && Count(cache, "TokenCount") == 6, "LRU exceeded its finite capacities");
            Check(Count(cache, "Evictions") == 1 && ReferenceEquals(first, Tokens(cache, "x=1")), "LRU evicted a recently used entry");
            var secondAfterEviction = Tokens(cache, "x=2");
            Check(!ReferenceEquals(second, secondAfterEviction), "Least recently used entry survived eviction");
            SameTokens(secondAfterEviction, second, "Eviction lexical result");
            Clear(cache);
            Check(Count(cache, "Entries") == 0 && Count(cache, "CharacterCount") == 0 && Count(cache, "TokenCount") == 0, "Clear retained lexical entries or capacity accounting");
            long before = Count(cache, "Tokenizations");
            var afterClear = Tokens(cache, "x=1");
            Check(!ReferenceEquals(first, afterClear) && Count(cache, "Tokenizations") == before + 1, "Clear reused a previous context's token list");
            SameTokens(afterClear, first, "Clear lexical result");

            // Independently exercise each capacity. Oversized entries must not
            // flush useful cached data and must keep the uncached semantics.
            foreach (var capacity in new[] { new[] { 2, 3, 100 }, new[] { 2, 100, 3 } })
            {
                cache = Cache(capacity[0], capacity[1], capacity[2]);
                var small = Tokens(cache, "x=1");
                var large = Tokens(cache, "x=1+2");
                SameTokens(large, Direct("x=1+2"), "Oversized uncached result");
                Tokens(cache, "x=1+2");
                Check(Count(cache, "Bypasses") == 2 && Count(cache, "Tokenizations") == 3, "Oversized lexical data entered the bounded cache");
                Check(Count(cache, "Evictions") == 0 && ReferenceEquals(small, Tokens(cache, "x=1")), "Oversized input displaced a useful cached result");
            }
            foreach (var capacity in new[] { new[] { 10, 6, 100 }, new[] { 10, 100, 6 } })
            {
                cache = Cache(capacity[0], capacity[1], capacity[2]);
                Tokens(cache, "x=1");
                var retained = Tokens(cache, "x=2");
                Tokens(cache, "x=3");
                Check(Count(cache, "Entries") == 2 && Count(cache, "Evictions") == 1, "Payload capacity did not evict while entry capacity remained available");
                Check(Count(cache, "CharacterCount") <= capacity[1] && Count(cache, "TokenCount") <= capacity[2], "Payload capacity accounting exceeded its bound");
                Check(ReferenceEquals(retained, Tokens(cache, "x=2")), "Payload eviction ignored recency order");
            }
            cache = Cache(0, 100, 100);
            SameTokens(Tokens(cache, "x=1"), Tokens(cache, "x=1"), "Disabled cache");
            Check(Count(cache, "Entries") == 0 && Count(cache, "Bypasses") == 2 && Count(cache, "CacheHits") == 0, "Zero entry capacity retained lexical data");
            cache = Cache(2, 0, 0);
            var empty = Tokens(cache, null);
            Check(empty.Count == 0 && ReferenceEquals(empty, Tokens(cache, "")), "Null and empty source differ from tokenizer semantics");
            Check(Count(cache, "Entries") == 1 && Count(cache, "CharacterCount") == 0 && Count(cache, "TokenCount") == 0, "Empty lexical entry exceeded zero payload capacity");

            string lexical = "-- comment\nlocal text=[=[not a call()]=]\nflag(\"quoted \\\" text\", 1+2)";
            cache = Cache(2, 1024, 1024);
            SameTokens(Tokens(cache, lexical), Direct(lexical), "Cold complex lexical result");
            SameTokens(Tokens(cache, lexical), Direct(lexical), "Warm complex lexical result");
            // Punctuation lookahead must preserve the original two-character
            // operators and a final one-character token without allocations.
            string[] expectedOperators = { "==", "~=", "<=", ">=", "..", "=", "~", "<", ">", ".", "+", "-", "*", "/" };
            var operators = Direct("== ~= <= >= .. = ~ < > . + - * /");
            Check(operators.Count == expectedOperators.Length, "Punctuation lookahead changed token boundaries");
            for (int i = 0; i < expectedOperators.Length; i++)
                Check((string)Value(operators[i], "Text") == expectedOperators[i], "Punctuation lookahead changed an operator or final character");
            ProductionReuse();
            ParserReuseAndClear();
            StaticPlanBoundaries();
            return assertions;
        }
    }
}
