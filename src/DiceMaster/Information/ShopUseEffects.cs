using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using DiceMaster.LiveLog;
using DiceMaster.LuaParse;
using LuaAction = DiceMaster.LuaParse.Action;

namespace DiceMaster.Information
{
    // Read-only association with the game's food-use menu, not arbitrary
    // inventory changes in story scripts. Parsing/indexing is per context;
    // normal refresh only describes the selected item's matched alternatives.
    public static class ShopUseEffects
    {
        public sealed class Result
        {
            public string Status, Detail, InlineText;
            public bool Known;
        }
        private const string FoodMenu = "food_menu_001";
        private static readonly Regex summaryTags = new Regex(@"</?(?:color|size|b|i)(?:=[^<>\r\n]*)?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        internal static string ShortParts(IList<string> values, int max = 180)
        {
            var parts = new List<string>();
            foreach (string value in values)
            {
                string text = Regex.Replace(summaryTags.Replace(value ?? "", ""), @"\s+", " ").Trim();
                if (text.Length != 0) parts.Add(text);
            }
            var result = new StringBuilder();
            for (int at = 0; at < parts.Count; at++)
            {
                string separator = result.Length == 0 ? "" : "  ";
                string tail = at + 1 < parts.Count ? " 외 " + (parts.Count - at - 1) + "개" : "";
                if (result.Length + separator.Length + parts[at].Length + tail.Length > max)
                {
                    string omitted = "외 " + (parts.Count - at) + "개";
                    return result.Length == 0 ? "효과 " + parts.Count + "개 (상세 조회)" : result + " " + omitted;
                }
                result.Append(separator).Append(parts[at]);
            }
            return result.ToString();
        }
        private static Dictionary<string, List<KeyValuePair<string, List<LuaAction>>>> uses;
        private static bool incomplete;
        public static void Clear() { uses = null; incomplete = false; }
        public static Result Read(string kind, string id)
        {
            if (uses == null)
            {
                ScriptReadResult source = DataHelper.ReadPreviewScript(FoodMenu);
                if (source.State == ScriptReadState.Waiting) throw new DataNotReadyException();
                if (source.State != ScriptReadState.Success) throw new InvalidOperationException("Food-use script unavailable.");
                LuaParser parser = PositionEventCache.GetDefinitionParser();
                var options = parser.ExtractOptionActionsFromLua(FoodMenu, source.Text);
                uses = Index(options);
                incomplete = parser.IncompleteOptionDiscovery;
            }
            return Describe(kind, id, uses, incomplete);
        }
        // Also exercised against the delivered parser/formatter IL.
        public static Result Find(string kind, string id, Dictionary<string, List<LuaAction>> options, bool partial)
        { return Describe(kind, id, Index(options), partial); }
        private static Dictionary<string, List<KeyValuePair<string, List<LuaAction>>>> Index(Dictionary<string, List<LuaAction>> options)
        {
            var index = new Dictionary<string, List<KeyValuePair<string, List<LuaAction>>>>(StringComparer.Ordinal);
            foreach (var option in options)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (LuaAction action in option.Value)
                {
                    if (action.Analysis == null) continue;
                    PreviewDisplay.Validate(action.Analysis);
                    foreach (PreviewEffect effect in action.Analysis.Effects)
                    {
                        if (effect.Owner != "statmodifymanager" || effect.ValidationError != null || effect.Constants == null ||
                            effect.Constants.Count != 1 || !(effect.Constants[0] is string)) continue;
                        string kind = effect.Name == "RemoveMisc" ? "misc" : effect.Name == "RemoveSpecial" ? "special" : null;
                        // Followed scripts may remove a reward or unrelated item;
                        // only this food-menu option's own use is an association.
                        if (kind == null || effect.Followup) continue;
                        string key = kind + ":" + (string)effect.Constants[0];
                        if (!seen.Add(key)) continue;
                        List<KeyValuePair<string, List<LuaAction>>> matches;
                        if (!index.TryGetValue(key, out matches)) index.Add(key, matches = new List<KeyValuePair<string, List<LuaAction>>>());
                        matches.Add(option);
                    }
                }
            }
            return index;
        }
        private static Result Describe(string kind, string id, Dictionary<string, List<KeyValuePair<string, List<LuaAction>>>> index, bool partial)
        {
            List<KeyValuePair<string, List<LuaAction>>> matches;
            if (!index.TryGetValue(kind + ":" + id, out matches))
                return new Result { Status = "사용 경로 미연결", Detail = "이 물품에 연결된 음식 사용 선택지를 확인하지 못함. 효과가 없다는 뜻이 아니며 장비·퀘스트·별도 사용 경로는 이 조회 범위 밖임.",
                    InlineText = "사용 효과 미확인" };
            var detail = new StringBuilder("음식 사용 메뉴에 연결된 효과 · 실제 사용/판정은 실행하지 않음:\n");
            var summaries = new List<string>();
            bool known = !partial;
            int alternative = 0;
            foreach (var option in matches)
            {
                LuaParser.RefreshPreviewReads(option.Value);
                string description = LuaParser.Description(option.Value);
                detail.AppendLine(option.Key + ":");
                detail.AppendLine(description);
                string method = matches.Count > 1 ? "사용법 " + (++alternative) + ": " : "";
                foreach (LuaAction action in option.Value)
                {
                    string shortDescription = action.Analysis == null ? action.Description : SummaryDescription(action.Analysis, kind, id);
                    foreach (string effect in EffectParts(shortDescription)) summaries.Add(method + effect);
                }
                foreach (LuaAction action in option.Value)
                    if (action.Analysis == null || action.Analysis.State != PreviewAnalysisState.Supported && action.Analysis.State != PreviewAnalysisState.NoEffects)
                        known = false;
            }
            if (partial) detail.AppendLine("사용 메뉴의 일부 선택지는 분석 한계로 확인하지 못함.");
            detail.AppendLine("조건부·난수·동적 변화는 설명의 조건/미확정 표기를 따름. 현재 사용 가능 여부나 실제 적용량을 확정하지 않음.");
            return new Result { Status = known ? "사용 효과 자료 확인" : "사용 효과 일부 확인", Detail = detail.ToString().TrimEnd(),
                InlineText = "사용: " + ShortParts(summaries, 176), Known = known };
        }

        private static string SummaryDescription(PreviewAnalysis original, string kind, string id)
        {
            // Match the compact food hint: consuming this selected item is
            // already implicit. Keep it in the full log, omit only that line
            // from a separate descriptor; never alter the cached analysis.
            var summary = new PreviewAnalysis { SnapshotAllowed = original.SnapshotAllowed,
                RootConditional = original.RootConditional, StoppedAtChoice = original.StoppedAtChoice,
                Issues = new List<PreviewIssue>(original.Issues) };
            string remove = kind == "misc" ? "RemoveMisc" : "RemoveSpecial";
            foreach (PreviewEffect effect in original.Effects)
            {
                if (effect.Owner == "statmodifymanager" && effect.Name == remove && effect.ValidationError == null &&
                    effect.Constants != null && effect.Constants.Count == 1 && (string)effect.Constants[0] == id && !effect.Followup)
                {
                    // Removing an earlier write cannot make snapshot-based
                    // numeric estimates look safer than the original order.
                    summary.SnapshotAllowed = false; continue;
                }
                summary.Effects.Add(effect);
            }
            return PreviewDisplay.Describe(summary);
        }
        private static List<string> EffectParts(string description)
        {
            var parts = new List<string>();
            string heading = "";
            foreach (string raw in (description ?? "").Replace("\r", "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line == PreviewDisplay.PartialStatus || line == PreviewDisplay.UnsupportedStatus ||
                    line.StartsWith("조건 표시는 현재 상태 기준", StringComparison.Ordinal)) continue;
                if (line == "가능한 변화") { heading = "가능: "; continue; }
                if (line == "이후 가능한 변화") { heading = "후속 가능: "; continue; }
                if (line == "현재 선택") { heading = ""; continue; }
                line = line.Replace("[경우 ", "[").Replace("현재 조건 충족", "충족").Replace("현재 조건 미충족", "미충족")
                    .Replace(" (심리위생 20 이하이면 입력에서 1 감소)", " (위생≤20: -1)")
                    .Replace(" · 실행 순서·조건에 따라 변경량 결정", " (조건부)")
                    .Replace(" · 부가 능력치 계산 경계 미지원으로 변경량은 실행 시 결정", " (가변)")
                    .Replace(" · 계산 범위·능력치 제한에 따라 변경량 결정", " (가변)")
                    .Replace(" (현재 상태 기준 · 능력치 제한·부가값 적용 전)", " (예상)")
                    .Replace(" 예상 변경량 ", " 예상 ");
                // Preserve a branch's short label on every independently
                // omitted effect, rather than dropping its condition context.
                string branch = "";
                int end = line.IndexOf(']');
                if (end >= 0 && line.Substring(0, end).IndexOf("[", StringComparison.Ordinal) >= 0)
                {
                    int closingTag = line.IndexOf("</color>", end, StringComparison.Ordinal);
                    int split = closingTag >= 0 ? closingTag + "</color>".Length : end + 1;
                    branch = line.Substring(0, split).Trim() + " "; line = line.Substring(split).Trim();
                }
                foreach (string effect in line.Split(new[] { " | ", "|" }, StringSplitOptions.RemoveEmptyEntries))
                    if (effect.Trim().Length != 0) parts.Add(heading + branch + effect.Trim());
            }
            if (parts.Count == 0) parts.Add("효과 미확인");
            return parts;
        }

    }
}
