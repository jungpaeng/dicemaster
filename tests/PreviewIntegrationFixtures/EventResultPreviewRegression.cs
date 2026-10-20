using System;
using System.Collections.Generic;
using System.Reflection;

namespace DiceMaster
{
    public static partial class PreviewScenario
    {
        static void EventResultPreviews()
        {
            var scripts = new Dictionary<string, string> {
                ["actual_prefix_reward"] = "statmodifymanager.Player(\"money\",5,\"\",1)\nstatmodifymanager.SetFlag(\"popup_flag\",1)",
                ["actual_prefix_branch"] = "if gate then statmodifymanager.SetFlag(\"popup_left\",1) else statmodifymanager.SetFlag(\"popup_right\",2) end",
                ["actual_prefix_follow"] = "statmodifymanager.SetFlag(\"popup_now\",1)\nluamanager.SetNextScript(\"popup_child\")",
                ["popup_child"] = "statmodifymanager.SetFlag(\"popup_after\",3)",
                ["actual_prefix_unknown"] = "statmodifymanager.Player(\"money\",unresolved_amount,\"\",1)",
                ["actual_prefix_empty"] = "-- a successfully read script with no known state changes",
                ["actual_prefix_partial"] = "statmodifymanager.SetFlag(\"popup_partial\",1)\nluamanager.SetNextScript(\"popup_missing_child\")"
            };
            var readKeys = new List<string>();
            DataHelper.PreviewScriptReader = name => { readKeys.Add(name); return scripts.TryGetValue(name, out string source) ? Read("Success", source) : Read("Missing"); };
            try
            {
                Invoke(Type("LiveLog.LogBridge"), null, "ResetContext");
                object Preview(string key) => Invoke(Type("EventSelection.EventResultPreview"), null, "Read", key);
                string Detail(object value) => (string)Member(value, "Detail");
                string Summary(object value) => (string)Member(value, "Summary");
                bool Retry(object value) => (bool)Member(value, "Retry");
                var reward = Preview("actual_prefix_reward");
                Contains(Detail(reward), "+5", "Actual popup result shows stat change");
                Contains(Detail(reward), "popup_flag", "Actual popup result shows flag change");
                Check(readKeys[0] == "actual_prefix_reward", "Popup reads the exact supplied result key without a warmed event-name/prefix cache");
                Check(!Retry(reward) && !Detail(reward).Contains("<color") && !Summary(reward).Contains("\n"), "Popup exposes immutable plain detail and one-line summary");
                int cachedReads = readKeys.Count;
                Preview("actual_prefix_reward");
                Check(readKeys.Count == cachedReads, "Successful preview definitions reuse the existing parser cache");
                scripts["late_named_book"]="statmodifymanager.AddBook(\"late_translation\",1)";
                var unnamed=Preview("late_named_book");
                Check(Retry(unnamed) && Detail(unnamed).Contains("late_translation"),"missing decorative name requests retry without hiding the known item effect");
                DataHelper.LocaleNames["Book/Name/late_translation"]="늦게 준비된 비급";
                var named=Preview("late_named_book");
                Check(!Retry(named) && Detail(named).Contains("늦게 준비된 비급"),"same cached script resolves a late locale name and clears presentation retry");
                DataHelper.LocaleNames.Clear();
                var branch = Preview("actual_prefix_branch");
                Contains(Detail(branch), "popup_left", "Conditional result includes first arm");
                Contains(Detail(branch), "popup_right", "Conditional result includes alternate arm");
                Contains(Detail(branch), "가능", "Conditional results remain possibilities rather than guaranteed rewards");
                var follow = Preview("actual_prefix_follow");
                Contains(Detail(follow), "popup_now", "Result includes root effects");
                Contains(Detail(follow), "popup_after", "Result includes bounded literal followup effects");
                var unknown = Preview("actual_prefix_unknown");
                Contains(Detail(unknown), "결과는 진행 후 확인", "Unsupported result explicitly reports uncertainty");
                Excludes(Detail(unknown), "확인된 효과 없음", "Unsupported is not represented as no effect");
                var empty = Preview("actual_prefix_empty");
                Contains(Detail(empty), "확인된 효과 없음", "A successfully analyzed empty script has a distinct status");
                var missing = Preview("actual_prefix_missing");
                Check(Retry(missing), "A missing resource is retryable");
                Contains(Detail(missing), "읽지 못", "Missing script displays a read-failure status");
                Excludes(Detail(missing), "확인된 효과 없음", "Missing resource never claims no effect");
                scripts["actual_prefix_missing"] = "statmodifymanager.SetFlag(\"popup_recovered\",4)";
                var recovered = Preview("actual_prefix_missing");
                Contains(Detail(recovered), "popup_recovered", "Existing parser retry recovers a previously missing root");
                Check(!Retry(recovered), "Recovered result clears its retry state");
                var partial = Preview("actual_prefix_partial");
                Contains(Detail(partial), "popup_partial", "Read failure retains known partial result effects");
                Check(Retry(partial), "Missing followup stays retryable");
                DataHelper.PreviewScriptReader = name => Read("Waiting");
                var waiting = Preview("actual_prefix_waiting");
                Check(Retry(waiting) && Detail(waiting).Contains("준비 중"), "Not-ready resources are distinct from no effect and read failure");
                var constructor = Type("EventSelection.EventResultPreview").GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(bool) }, null);
                var markup = constructor.Invoke(new object[] { "<color=red>감소 -3</color> · 조건 < 10 그리고 > 5\n" + new string('가', 180), false });
                Contains(Detail(markup), "조건 < 10 그리고 > 5", "Plain conversion preserves comparison operators");
                Excludes(Detail(markup), "<color", "Known Unity rich text is stripped for the plain popup");
                Check(Summary(markup).Length <= 161 && Summary(markup).EndsWith("…"), "Long summary is bounded while detail remains available");
                Check(Detail(markup).Length > Summary(markup).Length, "Summary truncation does not remove full result details");
                var colored = constructor.Invoke(new object[] { "<size=100><color=#A3B86C>동전 +5</color> <color=#CD594A>심상 -2</color></size>\n조건 < 10 그리고 > 5 " + new string('가', 180), false });
                string richDetail = (string)Member(colored, "RichDetail"), richSummary = (string)Member(colored, "RichSummary");
                Contains(richDetail, "<color=#A3B86C>동전 +5</color>", "Popup retains the log's positive result color");
                Contains(richDetail, "<color=#CD594A>심상 -2</color>", "Popup retains the log's negative result color");
                Excludes(richDetail, "<size", "Result font/size tags cannot alter popup row geometry");
                Contains(richDetail, "조건 ＜ 10 그리고 ＞ 5", "Rich comparisons remain visible without being parsed as markup");
                Contains(richSummary, "<color=#A3B86C>", "One-line result summary preserves effect color");
                Check(!richSummary.Contains("\n"), "Colored summary is still a single display line");
                string RichCall(string method, params object[] values) => (string)Invoke(Type("EventSelection.EventResultPreview"), null, method, values);
                string longRich = "<color=#A3B86C>동전 +5 <color=#CD594A>감소 -2</color>🙂</color> " + new string('나', 180);
                for (int length = 0; length < 200; length++)
                {
                    string fit = RichCall("FitSummary", longRich, length);
                    string plain = RichCall("PlainText", fit);
                    int opens = System.Text.RegularExpressions.Regex.Matches(fit, "<color=").Count;
                    int closes = System.Text.RegularExpressions.Regex.Matches(fit, "</color>").Count;
                    Check(opens == closes, "Every colored truncation closes its spans at visible length " + length);
                    Check(plain.Length <= length + 1, "Color markup does not count against the visible-text truncation budget");
                    for (int index = 0; index < plain.Length; index++)
                        if (char.IsHighSurrogate(plain[index])) { Check(index + 1 < plain.Length && char.IsLowSurrogate(plain[index + 1]), "Emoji remains a complete pair"); index++; }
                }
                string malformed = RichCall("FitSummary", "</color><color=#A3B86C>보상 +1 <size=500>큰 글자</size>", 200);
                Check(malformed.StartsWith("<color=#A3B86C>") && malformed.EndsWith("</color>") && !malformed.Contains("<size"),
                    "Malformed color closings and font tags cannot leak into surrounding UI");
                Check(RichCall("Escape", "<b>문자</b>") == "＜b＞문자＜/b＞", "Candidate labels are escaped rather than interpreted as result markup");
                var absent = Preview("");
                Excludes(Detail(absent), "확인된 효과 없음", "Unreadable target/result key has an explicit unavailable result");
            }
            finally { DataHelper.PreviewScriptReader = null; }
            Console.WriteLine("PREVIEW actual popup result summary/detail regression passed");
        }
    }
}
