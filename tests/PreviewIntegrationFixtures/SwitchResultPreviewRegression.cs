using System;
using System.Collections.Generic;

namespace DiceMaster
{
    public static partial class PreviewScenario
    {
        static void SwitchResultPreviews()
        {
            var scripts = new Dictionary<string, string> {
                ["switch_preview_child"] = "statmodifymanager.SetFlag(\"switch_after\",3)"
            };
            var readKeys = new List<string>();
            DataHelper.PreviewScriptReader = name => {
                readKeys.Add(name);
                return scripts.TryGetValue(name, out string source) ? Read("Success", source) : Read("Missing");
            };
            try
            {
                object Preview(string body) => Invoke(Type("EventSelection.EventResultPreview"), null,
                    "ReadSwitchBody", "switch_preview_scene", body);
                string Detail(object value) => (string)Member(value, "Detail");
                bool Retry(object value) => (bool)Member(value, "Retry");

                var left = Preview("statmodifymanager.SetFlag(\"switch_left\",1)");
                var right = Preview("statmodifymanager.SetFlag(\"switch_right\",2)");
                Contains(Detail(left), "switch_left", "Exact Switch arm includes its own effect");
                Excludes(Detail(left), "switch_right", "Exact Switch arm excludes its sibling");
                Contains(Detail(right), "switch_right", "Another arm at the same source has its own preview");
                Excludes(Detail(right), "switch_left", "Source identity does not reuse another arm's preview");
                Check(readKeys.Count == 0, "Inline Switch bodies do not reread a checkpoint name as a result script");
                Check(!Retry(left) && !Retry(right), "Successfully read inline arms do not request retries");

                var nested = Preview("if checkpointmanager.Switch(\"preview_only\") == 1 then " +
                    "statmodifymanager.SetFlag(\"switch_nested_left\",1) else " +
                    "statmodifymanager.SetFlag(\"switch_nested_right\",2) end");
                Contains(Detail(nested), "switch_nested_left", "Nested conditional keeps its first possibility");
                Contains(Detail(nested), "switch_nested_right", "Nested conditional keeps its alternate possibility");
                Contains(Detail(nested), "가능한 변화", "Nested branches remain possible effects");
                Excludes(Detail(nested), "현재 조건 충족", "Switch preview does not claim a live nested result");
                Excludes(Detail(nested), "현재 조건 미충족", "Switch preview does not reject a nested result from a stale snapshot");

                var follow = Preview("statmodifymanager.SetFlag(\"switch_now\",1)\n" +
                    "luamanager.SetNextScript(\"switch_preview_child\")\nluamanager.Init()");
                Contains(Detail(follow), "switch_now", "Switch preview retains local changes before routing");
                Contains(Detail(follow), "switch_after", "Switch preview follows the exact literal child script");
                Check(readKeys.Contains("switch_preview_child") && !readKeys.Contains("switch_preview_scene"),
                    "Only actual followup targets are read from resources");

                const string partialBody = "statmodifymanager.SetFlag(\"switch_partial\",1)\n" +
                    "luamanager.SetNextScript(\"switch_preview_missing\")";
                var partial = Preview(partialBody);
                Contains(Detail(partial), "switch_partial", "Unreadable followup retains known arm effects");
                Contains(Detail(partial), "읽지 못", "Unreadable followup reports its limitation");
                Check(Retry(partial), "Unreadable Switch followup can retry");
                scripts["switch_preview_missing"] = "statmodifymanager.SetFlag(\"switch_recovered\",4)";
                var recovered = Preview(partialBody);
                Contains(Detail(recovered), "switch_recovered", "Retry can read a newly available Switch followup");
                Check(!Retry(recovered), "Successful Switch followup retry clears the failure state");

                DataHelper.PreviewScriptReader = name => Read("Waiting");
                var waiting = Preview("luamanager.SetNextScript(\"switch_preview_waiting\")");
                Check(Retry(waiting) && Detail(waiting).Contains("준비 중"),
                    "Pending followup data is distinct from no effects");
                var unknown = Preview("statmodifymanager.Player(\"money\",unresolved_amount,\"\",1)");
                Contains(Detail(unknown), "결과는 진행 후 확인", "Unsupported arm effect reports uncertainty");
                Excludes(Detail(unknown), "확인된 효과 없음", "Unsupported arm does not claim no effects");
                Contains(Detail(Preview("-- empty arm")), "확인된 효과 없음", "A proven empty arm has a distinct description");
                Excludes(Detail(Preview(null)), "확인된 효과 없음", "An unavailable arm body does not claim no effects");
                Check(UnityEngine.Random.Calls == 0 && Mortal.Core.StatGroupVariable.OriginalReads == 0 &&
                    Mortal.Core.StatCompareItem.OriginalReads == 0,
                    "Switch result previews do not execute original RNG or comparison methods");
            }
            finally { DataHelper.PreviewScriptReader = null; }
            Console.WriteLine("PREVIEW actual Switch arm result summary/detail regression passed");
        }
    }
}
