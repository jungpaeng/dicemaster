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
            {"ChangeScene", "sss"}
        };

        public static bool IsDisplayOnly(PreviewEffect effect)
        {
            return effect.Owner == "statmodifymanager" && (effect.Name == "GetDisplayTime" ||
                effect.Name == "GetModifyString" || effect.Name == "UpdateSetPlayerStat");
        }

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
                if (effect.Validated) continue;
                effect.Validated = true;
                effect.Constants = new List<object>();
                // An explicitly identified display-only call is not an effect,
                // even when its displayed number is a variable.
                if (IsDisplayOnly(effect)) continue;
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
                if (effect.ValidationError != null) AddIssue(analysis, effect, effect.ValidationError);
            }
        }

        private static string Signed(int value) { return (value > 0 ? "+" : "") + value.ToString(CultureInfo.InvariantCulture); }
        private static string Number(int value) { return value.ToString(CultureInfo.InvariantCulture); }
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
            string meaning = mode == "center" ? name + " 50 방향 가산(입력 " + Number(amount) + ")" :
                name + " " + Signed(amount) + " 가산(심리위생 20 이하이면 입력에서 1 감소)";
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
                    case "SetPlayer": return Stat(S(e, 0)) + " " + Number(N(e, 1)) + "로 설정(능력치 제한 적용)";
                    case "Character": return Color(Character(S(e, 0)) + Local("System/Relation/FavorSuffix", " 호감도 ") + Signed(N(e, 1)));
                    case "SetCharacter": return Character(S(e, 0)) + " 호감도 " + Number(N(e, 1)) + "로 설정";
                    case "AddBook": return Item(1, S(e, 0)) + " x" + Number(N(e, 1));
                    case "AddMisc": return Item(2, S(e, 0)) + " x" + Number(N(e, 1));
                    case "AddSpecial": return Item(3, S(e, 0)) + " x" + Number(N(e, 1));
                    case "RemoveBook": return Local("System/Action/DiscardPrefix", "버림:") + Item(1, S(e, 0)) + " x1";
                    case "RemoveMisc": return Local("System/Action/DiscardPrefix", "버림:") + Item(2, S(e, 0)) + " x1";
                    case "RemoveSpecial": return Local("System/Action/DiscardPrefix", "버림:") + Item(3, S(e, 0)) + " x1";
                    case "AddFlag": return Flag(S(e, 0), N(e, 1)) + " " + Signed(N(e, 1)) + " 가산";
                    case "SetFlag": return Local("System/Flag/ActivatedPrefix", "") + Flag(S(e, 0), N(e, 1)) + "(" + Number(N(e, 1)) + ") 설정";
                    case "AddTalent": return string.Format(Local("PlayerTalent/Format/LevelUp", "기능 {0} Lv{1}"), PreviewState.TalentName(S(e, 0)), Signed(N(e, 1))) + "(레벨 가산·하위 기능 초기화 가능)";
                    case "ModifyEnemyId": return "적 부대 선택: " + Team(S(e, 0));
                    case "ModifyEnemyPeople": return Color(Team(S(e, 0)) + " 병력 " + Signed(N(e, 1)));
                    case "ModifyEnemyTeam": return Color(Team(S(e, 0)) + Local("System/Team/CohesionSuffix", "단결력 ") + Signed(N(e, 1)));
                    case "ModifyEnemyLevel": return Team(S(e, 0)) + " 레벨 " + Signed(N(e, 1)) + " 가산";
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
                    case "SetBattleSkillLevel": return "전투 기술 " + S(e, 0) + " 레벨 " + Number(N(e, 1)) + "로 설정";
                    case "SwapUpgradeItem": return "장비 교체: " + S(e, 0) + (N(e, 1) < 0 ? "(교체 대상의 기존 레벨 유지)" : "(레벨 " + Number(N(e, 1)) + " · 유효 범위 제한)") + " · 종류별 원래 장비 백업";
                    case "RestoreUpgradeItem": return "장비 " + S(e, 0) + "에 해당하는 종류의 백업 복원(백업이 있을 때)";
                    case "RestoreAllUpgradeItems": return "모든 장비 종류별 백업 복원(백업이 있을 때)";
                    case "SetGameTime": return "시간 " + Number(N(e, 0)) + "년 " + Number(N(e, 1)) + "월 " + Number(N(e, 2)) + "순으로 설정 · 행동 횟수·플래그·이야기 시간 갱신";
                    case "SetGameDayEnvironment": return "시간대 " + (N(e, 0) == 1 ? "낮" : N(e, 0) == 2 ? "밤" : Number(N(e, 0))) + "로 설정";
                    case "NextMonth": return "다음 달 진행 · 행동 횟수·회차 플래그·상점·이야기 시간 갱신";
                    case "NextRound": return "다음 순 진행 · 행동 횟수·회차 플래그·필요 시 상점·이야기 시간 갱신";
                    case "SetMissionTime": return "미션 시간 " + S(e, 0) + ": 현재 시각 복사 후 " + Number(Math.Max(0, N(e, 2))) + "개월·" + Number(Math.Max(0, N(e, 3))) + "순 진행(year 인자 미사용)";
                    case "SetNextScript": return S(e, 0).Length == 0 ? "후속 스크립트 빈 이름 등록(즉시 실행 없음)" :
                        "후속 스크립트 지정: " + S(e, 0) + "(실행 시점·대상 변경 가능)";
                    case "ChangeScene":
                        if (S(e, 0) == "Combat") return Local("System/Combat/Duel", "결투");
                        if (S(e, 0) == "Battle") return Local("System/Combat/Skirmish", "난전");
                        return "장면 이동: " + S(e, 0) + "(후속 대상 변경 가능)";
                }
            }
            return null;
        }

        public static string Describe(PreviewAnalysis analysis)
        {
            if (analysis == null) return "확인된 효과 없음";
            Validate(analysis);
            List<string> descriptions = new List<string>();
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
                try
                {
                    string text = Effect(effect, priorStateChange);
                    if (!string.IsNullOrEmpty(text))
                    {
                        string prefix = effect.Followup ? "후속 스크립트 · " : "";
                        if (effect.Conditional) prefix += "조건부 · ";
                        descriptions.Add(prefix + text);
                    }
                }
                catch (DataNotReadyException) { failed = true; waiting = true; }
                catch (Exception) { failed = true; }
                if (effect.Name != "SetNextScript") priorStateChange = true;
            }
            string status = unsupported ? (descriptions.Count > 0 ? "일부 결과 미리보기 미지원" : "결과 미리보기 미지원") :
                descriptions.Count == 0 && !failed ? "확인된 효과 없음" : "";
            string result = status;
            if (descriptions.Count > 0) result += (result.Length > 0 ? " | " : "") + string.Join(" | ", descriptions);
            if (failed) throw new PreviewReadException(result, waiting);
            return result;
        }
    }
}
