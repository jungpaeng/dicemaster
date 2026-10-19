using System;
using System.Collections.Generic;
using System.Globalization;
using DiceMaster.LiveLog;

namespace DiceMaster.EventSelection
{
    // Permission comes from an exact call site, not a checkpoint-key cache.
    // Only an immediately following, pure integer equality chain is admitted.
    internal sealed class EventSwitchSite
    {
        internal readonly Dictionary<int, string> Bodies = new Dictionary<int, string>();
        internal string ElseBody;
        internal string Body(int result)
        { string value; return Bodies.TryGetValue(result, out value) ? value : ElseBody; }

        internal static EventSwitchSite Read(string source, List<PreviewToken> tokens, int call, int close)
        {
            if (call < 3 || tokens[call - 1].Text != "=" || !tokens[call - 2].IsIdentifier ||
                tokens[call - 3].Text != "local") return null;
            string variable = tokens[call - 2].Text;
            int next = close + 1;
            if (next < tokens.Count && tokens[next].Text == ";") next++;
            if (next >= tokens.Count || tokens[next].Text != "if") return null;
            int end;
            var branches = PreviewSyntax.Branches(tokens, next, out end);
            if (end >= tokens.Count || branches.Count == 0) return null;
            var site = new EventSwitchSite();
            foreach (PreviewBranch branch in branches)
            {
                string body = PreviewSyntax.Slice(source, tokens, branch.Start, branch.End);
                if (branch.IsElse) { site.ElseBody = body; continue; }
                int start = branch.HeaderStart + 1, finish = branch.HeaderEnd;
                while (start < finish && tokens[start].Text == "(" && tokens[finish - 1].Text == ")") { start++; finish--; }
                int result;
                if (finish - start != 3 || tokens[start].Text != variable || tokens[start + 1].Text != "==" ||
                    !int.TryParse(tokens[start + 2].Text, NumberStyles.None, CultureInfo.InvariantCulture, out result) || result < 1 ||
                    site.Bodies.ContainsKey(result)) return null;
                site.Bodies.Add(result, body);
            }
            return site.Bodies.Count == 0 ? null : site;
        }
    }
}
