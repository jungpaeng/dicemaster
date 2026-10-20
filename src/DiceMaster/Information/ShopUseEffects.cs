using System;
using System.Collections.Generic;
using System.Text;
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
                    InlineText = "사용 효과:\n이 물품의 사용 효과를 아직 확인할 수 없음. 효과가 없거나 사용할 수 없다는 뜻은 아님." };
            var detail = new StringBuilder("음식 사용 메뉴에 연결된 효과 · 실제 사용/판정은 실행하지 않음:\n");
            var inline = new StringBuilder("사용 효과:\n");
            bool known = !partial;
            int alternative = 0;
            foreach (var option in matches)
            {
                LuaParser.RefreshPreviewReads(option.Value);
                string description = LuaParser.Description(option.Value);
                detail.AppendLine(option.Key + ":");
                detail.AppendLine(description);
                if (matches.Count > 1) inline.AppendLine("사용 방법 " + (++alternative) + ":");
                inline.AppendLine(EffectDescription(description));
                foreach (LuaAction action in option.Value)
                    if (action.Analysis == null || action.Analysis.State != PreviewAnalysisState.Supported && action.Analysis.State != PreviewAnalysisState.NoEffects)
                        known = false;
            }
            if (partial) detail.AppendLine("사용 메뉴의 일부 선택지는 분석 한계로 확인하지 못함.");
            detail.AppendLine("조건부·난수·동적 변화는 설명의 조건/미확정 표기를 따름. 현재 사용 가능 여부나 실제 적용량을 확정하지 않음.");
            if (!known) inline.AppendLine("주의: 일부 조건·후속 영향은 확인 불가. 표시된 변화도 실행 경로에 따라 달라질 수 있음.");
            if (partial) inline.AppendLine("다른 사용 방법이 있을 수 있음 · 사용 메뉴의 일부 경로는 확인 불가.");
            inline.AppendLine("사용 시 조건·난수·능력치 제한에 따라 실제 적용량이 달라질 수 있음.");
            return new Result { Status = known ? "사용 효과 자료 확인" : "사용 효과 일부 확인", Detail = detail.ToString().TrimEnd(),
                InlineText = inline.ToString().TrimEnd(), Known = known };
        }

        private static string EffectDescription(string description)
        {
            // Keep the existing formatter's effect order, branch labels and
            // uncertainty. Only its leading analysis verdict belongs in the log.
            string text = (description ?? "").Trim();
            foreach (string verdict in new[] { PreviewDisplay.PartialStatus, PreviewDisplay.UnsupportedStatus })
            {
                if (text == verdict) return "사용 효과를 아직 확인할 수 없음.";
                if (text.StartsWith(verdict + "\n", StringComparison.Ordinal) || text.StartsWith(verdict + "\r\n", StringComparison.Ordinal))
                    return text.Substring(verdict.Length).TrimStart('\r', '\n');
            }
            return text.Length == 0 ? "사용 효과를 아직 확인할 수 없음." : text;
        }
    }
}
