using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DiceMaster
{
    // Calls the copied production boundary reader; synthetic source contains no
    // game content and no Lua interpreter or current-player reads are involved.
    public static class DiscoveryRegression
    {
        static readonly Assembly Self = typeof(DiscoveryRegression).Assembly;
        static int assertions;
        static Type T(string name) => Self.GetType("DiceMaster.LiveLog." + name, true);
        static object Field(object value, string name) => value.GetType().GetField(name).GetValue(value);
        static void Check(bool value, string context) { assertions++; if (!value) throw new InvalidOperationException(context); }
        static IList Discover(string source, out bool incomplete, object limits = null)
        {
            limits = limits ?? Activator.CreateInstance(T("PreviewDiscoveryLimits"));
            object[] args = { source, limits, false };
            try
            {
                var method = T("PreviewSyntax").GetMethods().Single(m => m.Name == "OptionBlocks" && m.GetParameters().Length == 3);
                var result = (IList)method.Invoke(null, args); incomplete = (bool)args[2]; return result;
            }
            catch (TargetInvocationException error) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException ?? error).Throw(); throw; }
        }
        static object Limits(string name, int value)
        { var limits = Activator.CreateInstance(T("PreviewDiscoveryLimits")); limits.GetType().GetField(name).SetValue(limits, value); return limits; }
        static object Find(IList blocks, string key) => blocks.Cast<object>().SingleOrDefault(b => (string)Field(b, "Key") == key);
        static string Body(IList blocks, string key) => (string)Field(Find(blocks, key), "Body");
        static string Choose(int number, string key, string value)
        {
            return "local t" + number + "={}\nt" + number + "[1]=\"" + key + "\"\nlocal r" + number + "=choose(t" + number + ")\nif r" + number + "==1 then\nstatmodifymanager.SetFlag(\"" + value + "\",1)\nend\n";
        }
        public static int Run()
        {
            assertions = 0;
            // Actual roots reach 502,124 UTF-16 chars. Root discovery has its
            // own budget, separate from cumulative per-option tracing.
            string longRoot = "--" + new string('x', 300000) + "\n--index:O_discovery_late\nstatmodifymanager.SetFlag(\"late\",1)\n";
            var blocks = Discover(longRoot, out bool incomplete);
            Check(!incomplete && Find(blocks, "O_discovery_late") != null, "Root discovery reused the smaller per-option character budget");
            var manyTokens = new StringBuilder();
            for (int i = 0; i < 17000; i++) manyTokens.Append("local x=1\n");
            manyTokens.Append("--index:O_discovery_tokens\nstatmodifymanager.SetFlag(\"late\",1)\n");
            blocks = Discover(manyTokens.ToString(), out incomplete);
            Check(!incomplete && Find(blocks, "O_discovery_tokens") != null, "Root discovery reused the smaller per-option token budget");
            string nested = "if result==1 then\n--index:O_outer\nif gate then\nlocal t={}\nt[1]=\"O_inner\"\nlocal r=choose(t)\nif r==1 then\n--index:O_inner\nstatmodifymanager.SetFlag(\"inside\",1)\nend\nend\nstatmodifymanager.SetFlag(\"after_nested\",1)\nelseif result==2 then\n--index:O_sibling\nstatmodifymanager.SetFlag(\"sibling\",1)\nend\n";
            blocks = Discover(nested, out incomplete);
            Check(!incomplete && blocks.Count == 3, "Nested marker discovery changed option identities");
            Check(Body(blocks, "O_outer").Contains("after_nested"), "Nested marker cut the enclosing option before its real branch boundary");
            Check(Body(blocks, "O_outer").Contains("\nend\nend\n"), "Nested marker removed required closing blocks");
            Check(!Body(blocks, "O_outer").Contains("sibling"), "Enclosing option crossed into a sibling outcome");
            Check(!Body(blocks, "O_inner").Contains("after_nested"), "Nested option crossed its own branch boundary");
            Check(Body(blocks, "O_inner").Contains("inside"), "Marker precedence lost the actual nested option body");
            blocks = Discover("--index:O_first\nstatmodifymanager.SetFlag(\"first\",1)\n--index:O_second\nstatmodifymanager.SetFlag(\"second\",1)\n", out incomplete);
            Check(!Body(blocks, "O_first").Contains("second"), "Same-depth marker crossed into the next option");
            string generations = "local t={}\nt[1]=\"O_first_generation\"\nlocal r=choose(t)\nt[1]=\"O_later_assignment\"\nif r==1 then\nstatmodifymanager.SetFlag(\"first_body\",1)\nend\nt={}\nt[1]=\"O_second_generation\"\nlocal r=choose(t)\nif r==1 then\nstatmodifymanager.SetFlag(\"second_body\",1)\nend\n";
            blocks = Discover(generations, out incomplete);
            Check(!incomplete && blocks.Count == 2, "Table generations did not preserve choose-time option identities");
            Check(Find(blocks, "O_first_generation") != null && Find(blocks, "O_later_assignment") == null, "Assignment after choose changed its captured table value");
            Check(Body(blocks, "O_first_generation").Contains("first_body") && !Body(blocks, "O_first_generation").Contains("second_body"), "Repeated result variable matched the wrong if block");
            Check(Body(blocks, "O_second_generation").Contains("second_body"), "Reset table generation was lost");
            string qualified = "local t={}\nt[1]=\"O_expected_table\"\nother.t[1]=\"O_false_suffix\"\nlocal r=choose(t)\nif r==1 then\nstatmodifymanager.SetFlag(\"real\",1)\nend\n";
            blocks = Discover(qualified, out incomplete);
            Check(!incomplete && Find(blocks, "O_expected_table") != null && Find(blocks, "O_false_suffix") == null, "Qualified table suffix replaced the actual option table");
            string reassigned = "local t={}\nt[1]=\"O_stale_result\"\nlocal r=choose(t)\nr=0\nif r==1 then\nstatmodifymanager.SetFlag(\"unrelated\",1)\nend\n";
            blocks = Discover(reassigned, out incomplete);
            Check(!incomplete && blocks.Count == 0, "Reassigned choose result captured an unrelated later if block");
            string reassignedProperty = "local t={}\nt[1]=\"O_preserved_result\"\nlocal r=choose(t)\nother.r=0\nif r==1 then\nstatmodifymanager.SetFlag(\"related\",1)\nend\n";
            blocks = Discover(reassignedProperty, out incomplete);
            Check(!incomplete && Find(blocks, "O_preserved_result") != null, "Qualified assignment incorrectly invalidated the local choose result");
            string functionResult = "local t={}\nt[1]=\"O_function_result\"\nlocal r=choose(t)\nlocal function later(r)\nif r==1 then\nstatmodifymanager.SetFlag(\"wrong_function\",1)\nend\nend\nif r==1 then\nstatmodifymanager.SetFlag(\"right_function\",1)\nend\n";
            blocks = Discover(functionResult, out incomplete);
            Check(!incomplete && Body(blocks, "O_function_result").Contains("right_function") && !Body(blocks, "O_function_result").Contains("wrong_function"), "Nested function stole the outer choose result's branch");
            string functionTable = "local t={}\nt[1]=\"O_outer_table\"\nlocal function later()\nlocal t={}\nt[1]=\"O_wrong_function_table\"\nend\nlocal r=choose(t)\nif r==1 then\nstatmodifymanager.SetFlag(\"outer_table\",1)\nend\n";
            blocks = Discover(functionTable, out incomplete);
            Check(!incomplete && Find(blocks, "O_outer_table") != null && Find(blocks, "O_wrong_function_table") == null, "Nested function's local table polluted the outer option table");
            string blockResult = "local t={}\nt[1]=\"O_block_result\"\nlocal r=choose(t)\ndo\nlocal r=1\nif r==1 then\nstatmodifymanager.SetFlag(\"wrong_block\",1)\nend\nend\nif r==1 then\nstatmodifymanager.SetFlag(\"right_block\",1)\nend\n";
            blocks = Discover(blockResult, out incomplete);
            Check(!incomplete && Body(blocks, "O_block_result").Contains("right_block") && !Body(blocks, "O_block_result").Contains("wrong_block"), "A block-local result shadow stole the outer branch");
            string blockTable = "local t={}\nt[1]=\"O_outer_block_table\"\ndo\nlocal t={}\nt[1]=\"O_shadow_block_table\"\nend\nlocal r=choose(t)\nif r==1 then\nstatmodifymanager.SetFlag(\"outer_block\",1)\nend\n";
            blocks = Discover(blockTable, out incomplete);
            Check(!incomplete && Find(blocks, "O_outer_block_table") != null && Find(blocks, "O_shadow_block_table") == null, "Block-local table escaped its lexical scope");
            string escapingResult = "do\nlocal t={}\nt[1]=\"O_escaped_result\"\nlocal r=choose(t)\nend\nif r==1 then\nstatmodifymanager.SetFlag(\"out_of_scope\",1)\nend\n";
            blocks = Discover(escapingResult, out incomplete);
            Check(!incomplete && blocks.Count == 0, "A block-local choose result escaped into an unrelated outside branch");
            string replacement = "local t={}\nt[1]=\"O_replaced_table\"\nt=unknown_table\nlocal r=choose(t)\nif r==1 then\nstatmodifymanager.SetFlag(\"stale_table\",1)\nend\n";
            blocks = Discover(replacement, out incomplete);
            Check(!incomplete && blocks.Count == 0, "Unsupported table replacement retained stale option identities");
            string captured = "local t={}\nt[1]=\"O_captured_table\"\nlocal function later()\nlocal r=choose(t)\nif r==1 then\nstatmodifymanager.SetFlag(\"captured\",1)\nend\nend\n";
            blocks = Discover(captured, out incomplete);
            Check(!incomplete && blocks.Count == 0, "Cross-function captured tables were inferred without a proven invocation path");
            string forResult = "local t={}\nt[1]=\"O_for_result\"\nlocal r=choose(t)\nfor r=1,2 do\nif r==1 then\nstatmodifymanager.SetFlag(\"wrong_for\",1)\nend\nend\nif r==1 then\nstatmodifymanager.SetFlag(\"right_for\",1)\nend\n";
            blocks = Discover(forResult, out incomplete);
            Check(!incomplete && Body(blocks, "O_for_result").Contains("right_for") && !Body(blocks, "O_for_result").Contains("wrong_for"), "An implicit loop-local result stole the outer branch");
            string qualifiedResult = "local t={}\nt[1]=\"O_false_property_result\"\nother.r=choose(t)\nif r==1 then\nstatmodifymanager.SetFlag(\"unrelated_property\",1)\nend\n";
            blocks = Discover(qualifiedResult, out incomplete);
            Check(!incomplete && blocks.Count == 0, "Qualified result assignment was mistaken for a local choose result");
            string gated = "local t={}\nt[1]=\"O_gated_result\"\nlocal r=choose(t)\nif gate then\nif r==1 then\nstatmodifymanager.SetFlag(\"gated\",1)\nend\nend\n";
            blocks = Discover(gated, out incomplete);
            Check(!incomplete && blocks.Count == 0, "A gated result branch lost its enclosing condition during option extraction");
            string gatedThenOuter = gated + "if r==1 then\nstatmodifymanager.SetFlag(\"outer_unguarded\",1)\nend\n";
            blocks = Discover(gatedThenOuter, out incomplete);
            Check(!incomplete && Body(blocks, "O_gated_result").Contains("outer_unguarded") && !Body(blocks, "O_gated_result").Contains("\"gated\""), "A nested gate stole a later result branch at the choose scope");
            string pureChoice = "local t={}\nt[1]=\"O_pure_choice\"\nlocal r=choose(t)\nif ((r==1)) then\n--index:O_pure_choice\nstatmodifymanager.SetFlag(\"pure_choice\",1)\nend\n";
            blocks = Discover(pureChoice, out incomplete);
            Check(!incomplete && !(bool)Field(Find(blocks, "O_pure_choice"), "Conditional"), "A proven pure choice comparison was made uncertain");
            string commonPrefix = pureChoice.Replace("if ((r==1))", "statmodifymanager.Player(\"disposition\",5,\"\",0)\nif ((r==1))");
            blocks = Discover(commonPrefix, out incomplete);
            Check(!incomplete && (bool)Field(Find(blocks, "O_pure_choice"), "Conditional"), "A common statement between choose and result test was erased from marker context");
            blocks = Discover(commonPrefix.Replace("--index:O_pure_choice\n", ""), out incomplete);
            Check(!incomplete && (bool)Field(Find(blocks, "O_pure_choice"), "Conditional"), "Choose-only extraction treated a skipped common prefix as an unchanged snapshot");
            string triviaPrefix = pureChoice.Replace("if ((r==1))", "; -- trivia-only prefix\n--[=[trivia]=]\nif ((r==1))");
            blocks = Discover(triviaPrefix, out incomplete);
            Check(!incomplete && !(bool)Field(Find(blocks, "O_pure_choice"), "Conditional"), "A comment or empty statement invalidated a proven choice context");
            string compoundChoice = pureChoice.Replace("((r==1))", "r==1 and gate");
            blocks = Discover(compoundChoice, out incomplete);
            Check(!incomplete && (bool)Field(Find(blocks, "O_pure_choice"), "Conditional"), "A compound choice header lost its additional gate");
            blocks = Discover(compoundChoice.Replace("--index:O_pure_choice\n", ""), out incomplete);
            Check(!incomplete && (bool)Field(Find(blocks, "O_pure_choice"), "Conditional"), "Choose-table discovery erased a compound result condition");
            string unknownWrapper = "if gate then\n--index:O_unknown_wrapper\nstatmodifymanager.SetFlag(\"unknown_gate\",1)\nend\n";
            blocks = Discover(unknownWrapper, out incomplete);
            Check(!incomplete && (bool)Field(Find(blocks, "O_unknown_wrapper"), "Conditional"), "Marker extraction erased an unproven enclosing gate");
            string nestedGate = pureChoice.Replace("--index:O_pure_choice\n", "if extra_gate then\n--index:O_pure_choice\n").Replace("end\n", "end\nend\n");
            blocks = Discover(nestedGate, out incomplete);
            Check(!incomplete && (bool)Field(Find(blocks, "O_pure_choice"), "Conditional"), "A proven choice incorrectly erased an additional inner marker gate");
            string alreadyGatedChoice = "if visible_gate then\n" + pureChoice + "end\n";
            blocks = Discover(alreadyGatedChoice, out incomplete);
            Check(!incomplete && !(bool)Field(Find(blocks, "O_pure_choice"), "Conditional"), "A gate preceding the visible choose invalidated its pure result proof");
            string plainFunction = "function main()\n--index:O_plain_function\nstatmodifymanager.SetFlag(\"plain\",1)\nend\n";
            blocks = Discover(plainFunction, out incomplete);
            Check(!incomplete && !(bool)Field(Find(blocks, "O_plain_function"), "Conditional"), "A plain story function was treated as an extra choice gate");
            string compoundEarlier = "local t={}\nt[1]=\"O_compound_first\"\nt[2]=\"O_pure_later\"\nlocal r=choose(t)\nif r==1 or gate then\nstatmodifymanager.SetFlag(\"first\",1)\nelseif r==2 then\n--index:O_pure_later\nstatmodifymanager.SetFlag(\"later\",1)\nend\n";
            blocks = Discover(compoundEarlier, out incomplete);
            Check(!incomplete && (bool)Field(Find(blocks, "O_pure_later"), "Conditional"), "An earlier compound branch gate was erased from a later choice alternative");
            string falseMarkers = "local quoted=\"--index:O_false_quote\"\nlocal longtext=[=[\n--index:O_false_long\n]=]\n--[=[\n--index:O_false_comment\n]=]\n--index:O_real\nstatmodifymanager.SetFlag(\"real\",1)\n";
            blocks = Discover(falseMarkers, out incomplete);
            Check(!incomplete && blocks.Count == 1 && Find(blocks, "O_real") != null, "String or long comment was mistaken for an index marker");
            var repeated = new StringBuilder();
            for (int i = 0; i < 200; i++) repeated.Append(Choose(i, "O_linear_" + i, "value_" + i));
            blocks = Discover(repeated.ToString(), out incomplete);
            Check(!incomplete && blocks.Count == 200, "Repeated choose discovery exhausted work by rescanning earlier source");
            Check(Body(blocks, "O_linear_0").Contains("value_0") && !Body(blocks, "O_linear_0").Contains("value_199"), "Repeated choose boundaries were merged");
            string small = "--index:O_limit_one\nstatmodifymanager.SetFlag(\"one\",1)\n--index:O_limit_two\nstatmodifymanager.SetFlag(\"two\",1)\n";
            foreach (var limit in new[] { ("MaxCharacters", 70), ("MaxTokens", 8), ("MaxOptions", 1), ("MaxMaterializedCharacters", 10), ("MaxWork", 0) })
            {
                blocks = Discover(small, out incomplete, Limits(limit.Item1, limit.Item2));
                Check(incomplete, "Discovery budget did not report its finite " + limit.Item1 + " boundary");
                Check(blocks.Cast<object>().All(b => (bool)Field(b, "LimitReached")), "Discovery cutoff lost unsupported status for " + limit.Item1);
            }
            // Every work cutoff must preserve source-aligned known bodies or
            // stop; zero-initialized boundary arrays must never invent a body.
            for (int work = 0; work < 120; work++)
            {
                blocks = Discover(small, out incomplete, Limits("MaxWork", work));
                Check(blocks.Cast<object>().All(b => { string body = (string)Field(b, "Body"); return body.Length == 0 || small.Contains(body); }), "Exhausted discovery work fabricated a body boundary");
                Check(!incomplete || blocks.Cast<object>().All(b => (bool)Field(b, "LimitReached")), "Exhausted discovery work lost unsupported status");
            }
            blocks = Discover("--index:O_depth\ndo\ndo\ndo\nstatmodifymanager.SetFlag(\"deep\",1)\nend\nend\nend\n", out incomplete, Limits("MaxNesting", 2));
            Check(incomplete && blocks.Cast<object>().All(b => (bool)Field(b, "LimitReached")), "Discovery nesting boundary was not explicit");
            blocks = Discover("--index:O_story\nluamanager.GetStoryText(\"story_key\")\n", out incomplete);
            Check(!incomplete && (string)Field(Find(blocks, "O_story"), "Story") == "story_key", "Precomputed story lookup lost the first narrative key");
            return assertions;
        }
    }
}
