using System;
using System.Collections.Generic;

namespace DiceMaster.LiveLog
{
    // Context-local BepInEx diagnostics. Never include arguments, source text,
    // exception messages or save data. Manual reanalysis keeps this dedup set.
    public static class PreviewDiagnostics
    {
        private static readonly HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
        private static long context;

        public static void ResetContext() { reported.Clear(); context++; }

        public static void Report(PreviewAnalysis analysis, string option)
        {
            if (analysis == null) return;
            foreach (PreviewIssue issue in analysis.Issues)
            {
                // Actual read errors use RefreshEngine's first/recovery policy.
                if (issue.IsReadFailure) continue;
                string key = issue.Source + "\n" + option + "\n" + issue.Function + "\n" + issue.Reason;
                if (!reported.Add(key)) continue;
                try
                {
                    LogBridge.CallDisplayHelper("DiceMaster.DicePlugin", "LogWarning",
                        "Preview context=" + context + " script=" + Identity(issue.Source) +
                        " option=" + Identity(option) + " function=" + Identity(issue.Function) +
                        " unsupported=" + Identity(issue.Reason));
                }
                catch (Exception) { }
            }
        }

        private static string Identity(string value)
        {
            if (value == null) return "";
            char[] output = new char[Math.Min(value.Length, 96)];
            for (int i = 0; i < output.Length; i++)
                output[i] = char.IsControl(value[i]) ? '_' : value[i];
            return new string(output);
        }
    }
}
