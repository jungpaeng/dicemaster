using System;
using System.Collections.Generic;
using System.Globalization;

namespace DiceMaster.LiveLog
{
    // PartialDescription is display text only. Never include exception messages,
    // argument source text, complete Lua, or save data in diagnostics.
    public sealed class PreviewReadException : Exception
    {
        public string PartialDescription { get; private set; }
        public bool IsWaiting { get; private set; }
        public PreviewReadException(string description, bool waiting)
            : base("Preview data is unavailable.") { PartialDescription = description; IsWaiting = waiting; }
    }

    public static class PreviewDisplay
    {
        public const string PartialStatus = "추가 영향 가능";
        public const string UnsupportedStatus = "결과는 진행 후 확인";

        private static readonly Dictionary<string, string> statSignatures = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            {"Player", "sisi"}, {"Character", "sii"}, {"SetPlayer", "si"}, {"SetCharacter", "si"},
            {"AddBook", "si"}, {"AddMisc", "si"}, {"AddSpecial", "si"},
            {"RemoveBook", "s"}, {"RemoveMisc", "s"}, {"RemoveSpecial", "s"},
            {"AddFlag", "si"}, {"SetFlag", "si"}, {"AddTalent", "si"},
            {"Mission", "ss"}, {"IsTriggerSubMission", "si"}, {"ModifyEnemyId", "s"},
            {"ModifyEnemyPeople", "sii"}, {"ModifyEnemyTeam", "sii"}, {"ModifyEnemyLevel", "sii"},
            {"AddDeadLib", "s"}, {"AddEndGameLib", "s"}, {"AddAchievementLib", "s"},
            {"ResetTalkFlag", ""}, {"UpdateShopFlag", ""}, {"UpdateAssetStatus", ""}, {"AddStory", "s"},
            // Display helpers do not change a stat, inventory, or mission.
            {"GetDisplayTime", "s"}, {"GetModifyString", "ssi"}, {"UpdateSetPlayerStat", "si"}
        };
        private static readonly Dictionary<string, string> luaSignatures = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            {"ResetBattleSkill", ""}, {"SetPlayerBattleSkill", "si"}, {"SetBattleSkillActive", "si"},
            {"SetBattleSkillLevel", "si"}, {"SwapUpgradeItem", "si"}, {"RestoreUpgradeItem", "s"},
            {"RestoreAllUpgradeItems", ""}, {"SetGameTime", "iii"}, {"SetGameDayEnvironment", "i"},
            {"NextMonth", ""}, {"NextRound", ""}, {"SetMissionTime", "siii"}, {"SetNextScript", "s"},
            {"ChangeScene", "sss"}, {"SetCurrentStoryScript", "s"}, {"SetEnableActions", "i"}
        };

        public static bool IsDisplayOnly(PreviewEffect effect)
        {
            return effect.Owner == "statmodifymanager" && (effect.Name == "GetDisplayTime" ||
                effect.Name == "GetModifyString" || effect.Name == "UpdateSetPlayerStat");
        }

        // Routing is still validated and traced, but is not a player-facing reward.
        public static bool IsRoutingOnly(PreviewEffect effect)
        { return effect.Owner == "luamanager" && IsStoryRegistration(effect.Name); }

        // Both methods call PlayerStatManagerData.SetStoryScript in the supplied
        // Mortal.Story DLL. This describes/traces registration, never calls it.
        public static bool IsStoryRegistration(string name)
        { return name == "SetNextScript" || name == "SetCurrentStoryScript"; }

        public static bool IsKnownEffect(string owner, string name)
        {
            return owner == "statmodifymanager" ? statSignatures.ContainsKey(name) :
                owner == "luamanager" && luaSignatures.ContainsKey(name);
        }

        private static void AddIssue(PreviewAnalysis analysis, PreviewEffect effect, string reason)
        {
            string function = effect.Owner + "." + effect.Name;
            foreach (PreviewIssue existing in analysis.Issues)
                if (existing.Function == function && existing.Reason == reason && existing.Source == effect.Source) return;
            analysis.Issues.Add(new PreviewIssue { Function = function, Reason = reason, Source = effect.Source });
        }

        public static void Validate(PreviewAnalysis analysis)
        {
            if (analysis == null) return;
            foreach (PreviewEffect effect in analysis.Effects)
            {
                ValidateEffect(effect);
                if (effect.ValidationError != null) AddIssue(analysis, effect, effect.ValidationError);
            }
        }

        // Context-free validation can be shared as an immutable script template.
        // Attaching an issue to each analysis remains the caller's responsibility.
        public static void ValidateEffect(PreviewEffect effect)
        {
            if (effect.Validated) return;
            effect.Validated = true;
            effect.Constants = new List<object>();
            // An explicitly identified display-only call is not an effect,
            // even when its displayed number is a variable.
            if (IsDisplayOnly(effect)) return;
            Dictionary<string, string> signatures = effect.Owner == "statmodifymanager" ? statSignatures :
                effect.Owner == "luamanager" ? luaSignatures : null;
            string signature = "";
            if (signatures == null || !signatures.TryGetValue(effect.Name, out signature))
                effect.ValidationError = "지원 밖 효과 함수";
            else if (effect.Owner == "luamanager" && effect.Name == "ChangeScene" && effect.Arguments.Count == 1)
                signature = "s";
            if (effect.ValidationError == null && effect.Arguments.Count != signature.Length)
                effect.ValidationError = "함수 인자 수 불일치";
            if (effect.ValidationError == null)
            {
                for (int i = 0; i < signature.Length; i++)
                {
                    if (signature[i] == 's')
                    {
                        string value;
                        List<PreviewToken> tokens = PreviewSyntax.Tokenize(effect.Arguments[i]);
                        if (tokens.Count != 1 || !PreviewSyntax.TryString(tokens[0].Text, out value))
                        { effect.ValidationError = "문자열 리터럴 외 인자"; break; }
                        effect.Constants.Add(value);
                    }
                    else
                    {
                        int value; string reason;
                        if (!PreviewArithmetic.TryInteger(effect.Arguments[i], out value, out reason))
                        { effect.ValidationError = reason; break; }
                        effect.Constants.Add(value);
                    }
                }
            }
        }

        private static string Signed(int value) { return (value > 0 ? "+" : "") + value.ToString(CultureInfo.InvariantCulture); }
        private static string Number(int value) { return value.ToString(CultureInfo.InvariantCulture); }
        private static string Setting(int value)
        {
            int last = Math.Abs(value % 10);
            return Number(value) + (last == 0 || last == 3 || last == 6 ? "으로 설정" : "로 설정");
        }
        private static string S(PreviewEffect effect, int index) { return (string)effect.Constants[index]; }
        private static int N(PreviewEffect effect, int index) { return (int)effect.Constants[index]; }
        private static string Helper(string type, string name, params object[] arguments)
        { return (string)LogBridge.CallDisplayHelper("DiceMaster." + type, name, arguments); }
        private static string Stat(string key) { return Helper("PlayerinfoTranslations", "TranslatePlayerstats", key); }
        private static string Character(string key) { return Helper("CharacterTranslations", "TranslateCharacter", key); }
        private static string Team(string key) { return PreviewState.TeamName(key); }
        private static string Color(string text) { return Helper("RichText", "ColorText", text); }
        private static string Item(int kind, string key)
        {
            return PreviewState.ItemName(kind, key);
        }
        private static string Flag(string key, int value)
        {
            string note = PreviewState.LooksLikeFlagId(key) ? PreviewState.FlagDevNote(key) : key;
            // TranslateDevNote itself resolves ID-shaped arguments through
            // MissionManagerData.Instance. Only its pure dev-note branch is
            // permitted here; unavailable notes retain the literal flag ID.
            string name = string.IsNullOrEmpty(note) || PreviewState.LooksLikeFlagId(note) ? key :
                Helper("FlagTranslations", "TranslateDevNote", note, value);
            if (string.IsNullOrEmpty(name)) name = key;
            if (!name.EndsWith("(" + key + ")", StringComparison.Ordinal)) name += "(" + key + ")";
            return name;
        }
        private static string Local(string key, string fallback)
        { return Helper("DataHelper", "GetLocalizedSystemText", key, fallback, "시스템_미리보기", key); }

        private static string Player(PreviewEffect effect, bool priorStateChange)
        {
            string name = Stat(S(effect, 0)), mode = S(effect, 2);
            int amount = N(effect, 1);
            if (mode != "center" && mode != "mental") return Color(name + " " + Signed(amount));
            string meaning = mode == "center" ? name + " 50 방향으로 변화(입력 " + Number(amount) + ")" :
                name + " " + Signed(amount) + " (심리위생 20 이하이면 입력에서 1 감소)";
            // Followup registration, branching and prior writes break the
            // relationship between the current snapshot and execution state.
            if (priorStateChange || effect.Conditional || effect.Followup)
                return meaning + " · 실행 순서·조건에 따라 변경량 결정";
            int current;
            try { current = PreviewState.ReadPlayerStat(mode == "center" ? S(effect, 0) : "mental"); }
            catch (UnsupportedDisplayStatException)
            { return meaning + " · 부가 능력치 계산 경계 미지원으로 변경량은 실행 시 결정"; }
            long result;
            if (mode == "center")
            {
                long distance = 50L - current;
                if (distance < int.MinValue || distance > int.MaxValue || amount == int.MinValue)
                    return meaning + " · 계산 범위·능력치 제한에 따라 변경량 결정";
                result = distance == 0 ? 0 : Math.Abs(Math.Min(Math.Abs(distance), (long)amount)) * Math.Sign(distance);
            }
            else result = (long)amount - (current <= 20 ? 1 : 0);
            if (result < int.MinValue || result > int.MaxValue)
                return meaning + " · 계산 범위·능력치 제한에 따라 변경량 결정";
            return Color(name + " 예상 변경량 " + Signed((int)result)) +
                " (현재 상태 기준 · 능력치 제한·부가값 적용 전)";
        }

        private static string Effect(PreviewEffect e, bool priorStateChange)
        {
            if (e.Owner == "statmodifymanager")
            {
                switch (e.Name)
                {
                    case "Player": return Player(e, priorStateChange);
                    case "SetPlayer": return Stat(S(e, 0)) + " " + Setting(N(e, 1)) + "(능력치 제한 적용)";
                    case "Character": return Color(Character(S(e, 0)) + Local("System/Relation/FavorSuffix", " 호감도 ") + Signed(N(e, 1)));
                    case "SetCharacter": return Character(S(e, 0)) + " 호감도 " + Setting(N(e, 1));
                    case "AddBook": return Item(1, S(e, 0)) + " x" + Number(N(e, 1));
                    case "AddMisc": return Item(2, S(e, 0)) + " x" + Number(N(e, 1));
                    case "AddSpecial": return Item(3, S(e, 0)) + " x" + Number(N(e, 1));
                    case "RemoveBook": return Local("System/Action/DiscardPrefix", "버림:") + Item(1, S(e, 0)) + " x1";
                    case "RemoveMisc": return Local("System/Action/DiscardPrefix", "버림:") + Item(2, S(e, 0)) + " x1";
                    case "RemoveSpecial": return Local("System/Action/DiscardPrefix", "버림:") + Item(3, S(e, 0)) + " x1";
                    case "AddFlag": return Flag(S(e, 0), N(e, 1)) + " " + Signed(N(e, 1));
                    case "SetFlag": return Local("System/Flag/ActivatedPrefix", "") + Flag(S(e, 0), N(e, 1)) + "(" + Number(N(e, 1)) + ") 설정";
                    case "AddTalent": return string.Format(Local("PlayerTalent/Format/LevelUp", "기능 {0} Lv{1}"), PreviewState.TalentName(S(e, 0)), Signed(N(e, 1))) + "(하위 기능 초기화 가능)";
                    case "ModifyEnemyId": return "적 부대 선택: " + Team(S(e, 0));
                    case "ModifyEnemyPeople": return Color(Team(S(e, 0)) + " 병력 " + Signed(N(e, 1)));
                    case "ModifyEnemyTeam": return Color(Team(S(e, 0)) + Local("System/Team/CohesionSuffix", "단결력 ") + Signed(N(e, 1)));
                    case "ModifyEnemyLevel": return Team(S(e, 0)) + " 레벨 " + Signed(N(e, 1));
                    case "Mission": return "미션 " + S(e, 0) + " 단계 " + S(e, 1) + "로 설정";
                    case "IsTriggerSubMission": return "서브 미션 " + S(e, 0) + (N(e, 1) == 1 ? " 발동 허용" : " 발동 해제");
                    case "AddDeadLib": return "사망 도감 기록 +1: " + S(e, 0);
                    case "AddEndGameLib": return "결말 도감 기록 +1: " + S(e, 0);
                    case "AddAchievementLib": return "업적 기록: " + S(e, 0) + "(미획득 항목만)";
                    case "ResetTalkFlag": return "대화 선택지 플래그 초기화";
                    case "UpdateShopFlag": return "상점 누적 플래그 1000마다 " + Character("brother4") + " 호감도 +1 · 사용량 차감(1000 미만이면 변화 없음)";
                    case "UpdateAssetStatus": return "활성 시설 수입·효과 적용 · 구역 플래그에 따른 단결력 효과";
                    case "AddStory": return "이야기 키 등록: " + S(e, 0) + "(중복 제외)";
                }
            }
            else if (e.Owner == "luamanager")
            {
                switch (e.Name)
                {
                    case "ResetBattleSkill": return "전투 기술 슬롯·활성 상태 초기화 및 조건 재평가";
                    case "SetPlayerBattleSkill": return "전투 기술 " + S(e, 0) + " 활성화 · 첫 빈 슬롯에 배치(index 인자 미사용)";
                    case "SetBattleSkillActive": return "전투 기술 " + S(e, 0) + (N(e, 1) == 1 ? " 활성화" : " 비활성화");
                    case "SetBattleSkillLevel": return "전투 기술 " + S(e, 0) + " 레벨 " + Setting(N(e, 1));
                    case "SwapUpgradeItem": return "장비 교체: " + S(e, 0) + (N(e, 1) < 0 ? "(교체 대상의 기존 레벨 유지)" : "(레벨 " + Number(N(e, 1)) + " · 유효 범위 제한)") + " · 종류별 원래 장비 백업";
                    case "RestoreUpgradeItem": return "장비 " + S(e, 0) + "에 해당하는 종류의 백업 복원(백업이 있을 때)";
                    case "RestoreAllUpgradeItems": return "모든 장비 종류별 백업 복원(백업이 있을 때)";
                    case "SetGameTime": return "시간 " + Number(N(e, 0)) + "년 " + Number(N(e, 1)) + "월 " + Number(N(e, 2)) + "순으로 설정 · 행동 횟수·플래그·이야기 시간 갱신";
                    case "SetGameDayEnvironment": return "시간대 " + (N(e, 0) == 1 ? "낮" : N(e, 0) == 2 ? "밤" : Number(N(e, 0))) + "로 설정";
                    case "NextMonth": return "다음 달 진행 · 행동 횟수·회차 플래그·상점·이야기 시간 갱신";
                    case "NextRound": return "다음 순 진행 · 행동 횟수·회차 플래그·필요 시 상점·이야기 시간 갱신";
                    case "SetMissionTime": return "미션 시간 " + S(e, 0) + ": 현재 시각 복사 후 " + Number(Math.Max(0, N(e, 2))) + "개월·" + Number(Math.Max(0, N(e, 3))) + "순 진행(year 인자 미사용)";
                    case "SetEnableActions": return N(e, 0) == 1 ? "이야기 조작 허용" : "이야기 조작 차단";
                    case "ChangeScene":
                        if (S(e, 0) == "Combat") return Local("System/Combat/Duel", "결투");
                        if (S(e, 0) == "Battle") return Local("System/Combat/Skirmish", "난전");
                        return "장면 이동: " + S(e, 0) + "(후속 대상 변경 가능)";
                }
            }
            return null;
        }

        private sealed class DisplayRow
        {
            public PreviewEffect Effect;
            public string Text;
        }

        private static bool SameBranches(PreviewEffect left, PreviewEffect right)
        {
            if (left.Branches.Count != right.Branches.Count) return false;
            for (int i = 0; i < left.Branches.Count; i++)
                if (left.Branches[i].Group != right.Branches[i].Group || left.Branches[i].Alternative != right.Branches[i].Alternative)
                    return false;
            return true;
        }

        private static string BranchLabel(PreviewEffect effect, bool numbered, PreviewConditionState current)
        {
            if (effect.Branches.Count == 0) return "";
            var path = new List<string>();
            foreach (PreviewBranchContext branch in effect.Branches)
            {
                string alternative = branch.Alternative < 26 ? ((char)('A' + branch.Alternative)).ToString() : Number(branch.Alternative + 1);
                path.Add((numbered ? Number(branch.Group) + "-" : "") + alternative + (branch.IsElse ? " · 그 외" : ""));
            }
            // Neutral branch colors distinguish alternatives, never pass/fail.
            string[] colors = { "#7FC8D6", "#C5A2E0", "#E1BF79" };
            int color = effect.Branches[effect.Branches.Count - 1].Alternative % colors.Length;
            string verdict = current == PreviewConditionState.Met ? " · 현재 조건 충족" :
                current == PreviewConditionState.NotMet ? " · 현재 조건 미충족" : "";
            string selectedColor = current == PreviewConditionState.Met ? "#80D89B" :
                current == PreviewConditionState.NotMet ? "#A4A7B0" : colors[color];
            return "<color=" + selectedColor + ">[경우 " + string.Join(" → ", path) + verdict + "]</color> ";
        }

        private static int Section(PreviewEffect effect)
        {
            if (effect.Followup) return 2;
            return effect.Conditional || effect.ExecutionUncertain || effect.Branches.Count > 0 ? 1 : 0;
        }

        private static PreviewConditionState CurrentBranch(PreviewEffect effect, bool allowed,
            Dictionary<PreviewCondition, PreviewConditionState> values, PreviewConditions.Reader reader)
        {
            if (!allowed || effect.Followup || effect.Branches.Count == 0) return PreviewConditionState.Unknown;
            bool unknown = false;
            foreach (PreviewBranchContext branch in effect.Branches)
            {
                PreviewConditionState current = PreviewConditionState.Unknown;
                if (branch.CurrentConditionAllowed && branch.CurrentCondition != null)
                {
                    if (!values.TryGetValue(branch.CurrentCondition, out current))
                    { current = reader.Current(branch.CurrentCondition); values.Add(branch.CurrentCondition, current); }
                }
                if (current == PreviewConditionState.NotMet) return current;
                unknown |= current == PreviewConditionState.Unknown;
            }
            return unknown ? PreviewConditionState.Unknown : PreviewConditionState.Met;
        }

        private static string DescribeRows(List<DisplayRow> rows, PreviewAnalysis analysis)
        {
            var lines = new List<string>();
            var groups = new HashSet<int>();
            var currentValues = new Dictionary<PreviewCondition, PreviewConditionState>();
            var currentReader = PreviewConditions.CreateReader();
            bool hasCurrentHint = false;
            foreach (DisplayRow row in rows)
                foreach (PreviewBranchContext branch in row.Effect.Branches) groups.Add(branch.Group);
            int previousSection = -1;
            bool heading = false;
            for (int i = 0; i < rows.Count;)
            {
                PreviewEffect effect = rows[i].Effect;
                int section = Section(effect);
                if (section != previousSection)
                {
                    heading = section != 0 || previousSection != -1;
                    if (heading) lines.Add(section == 2 ? "이후 가능한 변화" : section == 1 ? "가능한 변화" : "현재 선택");
                    previousSection = section;
                }
                var values = new List<string> { rows[i++].Text };
                while (i < rows.Count && rows[i].Effect.Source == effect.Source && Section(rows[i].Effect) == section && SameBranches(effect, rows[i].Effect))
                    values.Add(rows[i++].Text);
                // Possibility is stated once by the section heading. Preserve
                // source order and the real branch path without repeated badges.
                PreviewConditionState current = CurrentBranch(effect, analysis.SnapshotAllowed && !analysis.RootConditional, currentValues, currentReader);
                hasCurrentHint |= current != PreviewConditionState.Unknown;
                lines.Add((heading ? "  " : "") + BranchLabel(effect, groups.Count > 1, current) + string.Join(" | ", values));
            }
            if (hasCurrentHint) lines.Add("조건 표시는 현재 상태 기준 · 진행 중 상태가 바뀌면 결과가 달라질 수 있음");
            return string.Join("\n", lines);
        }

        public static string Describe(PreviewAnalysis analysis)
        {
            if (analysis == null) return "확인된 효과 없음";
            Validate(analysis);
            var descriptions = new List<DisplayRow>();
            bool failed = false, waiting = false, priorStateChange = !analysis.SnapshotAllowed, unsupported = false;
            foreach (PreviewIssue issue in analysis.Issues)
            {
                if (issue.IsReadFailure || issue.IsWaiting)
                { failed = true; waiting |= issue.IsWaiting; }
                else unsupported = true;
            }
            foreach (PreviewEffect effect in analysis.Effects)
            {
                if (IsDisplayOnly(effect)) continue;
                if (effect.ValidationError != null) { priorStateChange = true; continue; }
                if (IsRoutingOnly(effect)) continue;
                try
                {
                    string text = Effect(effect, priorStateChange);
                    if (!string.IsNullOrEmpty(text))
                    {
                        descriptions.Add(new DisplayRow { Effect = effect, Text = text });
                    }
                }
                catch (DataNotReadyException) { failed = true; waiting = true; }
                catch (Exception) { failed = true; }
                priorStateChange = true;
            }
            string status = unsupported ? (descriptions.Count > 0 ? PartialStatus : UnsupportedStatus) :
                descriptions.Count == 0 && !failed ? "확인된 효과 없음" : "";
            string result = status;
            if (descriptions.Count > 0) result += (result.Length > 0 ? "\n" : "") + DescribeRows(descriptions, analysis);
            if (failed) throw new PreviewReadException(result, waiting);
            return result;
        }
    }
}
