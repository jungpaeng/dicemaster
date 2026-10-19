using System;
using System.Collections.Generic;
using System.Reflection;
using Mortal.Core;

namespace Mortal.Core
{
    // Game data/localization boundaries. Production naming IL is copied by the
    // runner; numeric getters throw so naming cannot silently evaluate gameplay.
    public enum StatGroupType { Sum = 1, Max, Min, Multiply }
    public enum StatCheckType { 常數 = 0, 角色數值 = 1, 旗標 = 3, 數值群組 = 8 }
    public class StatGroupVariable : UnityEngine.Object
    {
        public StatGroupType _groupType = StatGroupType.Sum;
        public List<StatValueReference> _value = new List<StatValueReference>();
        public int GetResult() => throw new Exception("Naming executed a group");
    }
    public class StatValueReference
    {
        public StatCheckType _checkType;
        public GameStat _stat;
        public int _constant;
        public StatGroupVariable _statGroup;
        public FlagData _flag;
        public bool _negative;
        public int GetValue() => throw new Exception("Naming evaluated a value");
    }
    public class GameStat : UnityEngine.Object
    {
        public string LocaleKey;
        public string GetLocaleKey() => LocaleKey;
        public int FinalValue => throw new Exception("Naming read a numeric stat");
    }
    public class FlagData : UnityEngine.Object { }
    public interface ILocaleResolver { string GetString(string key); }
    public class LocalizationManager : UnityEngine.Object
    {
        public static LocalizationManager Instance { get; set; }
        public ILocaleResolver LocaleResolver { get; set; }
    }
}

namespace DiceMaster
{
    // Older name/table heuristics are explicit boundaries for this focused test.
    // Both FormatConditionValName and the new structured resolver run actual IL.
    public static partial class DataHelper
    {
        static string GetLocalizedSystemText(string key, string fallback, string category, string original) => fallback;
        static string AppendFlagIdSuffix(string text, string id) => text + "(" + id + ")";
        static string GetFlagName(FlagData flag) => flag.name;
        static string TryResolveFlagIdToName(string text) => null;
        static string TryResolveStoryIdToName(string text) => null;
        static string TryResolveGetFlagExpressionToName(string text) => null;
        static string TryResolveRelationshipExpressionToName(string text) => null;
        static string TryResolveCharacterIdToName(string text) => null;
        static string TryResolveEmbeddedCharacterIdToName(string text) => null;
    }
    public static class TimeTranslations { public static string TranslateChineseTimeExpression(string text) => text; }
    public static class PlayerinfoTranslations { public static string TranslatePlayerstats(string text) => text; }

    public static class ConditionNameRegression
    {
        const string Checkpoint = "Ch_3_6_4_3_001";
        static int assertions;
        sealed class Locale : ILocaleResolver
        {
            public readonly Dictionary<string, string> Text = new Dictionary<string, string> {
                ["PlayerStat/literacy"] = "學問", ["PlayerStat/talking"] = "화술"
            };
            public string GetString(string key) => Text.TryGetValue(key, out string text) ? text : key;
        }
        static StatValueReference Stat(string id) => new StatValueReference {
            _checkType = StatCheckType.角色數值, _stat = new GameStat { LocaleKey = "PlayerStat/" + id }
        };
        static StatValueReference Constant(int value) => new StatValueReference { _constant = value };
        static StatValueReference Nested(StatGroupVariable value) => new StatValueReference {
            _checkType = StatCheckType.數值群組, _statGroup = value
        };
        static StatGroupVariable Group(params StatValueReference[] values) => new StatGroupVariable {
            name = Checkpoint, _value = new List<StatValueReference>(values)
        };
        static string Render(StatGroupVariable group)
        {
            string name = (string)typeof(DataHelper).GetMethod("FormatConditionValName", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { group });
            var log = Fungus.FungusManager.Instance.NarrativeLog;
            log.Entries.Clear();
            typeof(OptionInfoPatch).GetMethod("PrintToNarrativeLog", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { name });
            if (log.Entries.Count != 1) throw new Exception("Condition name did not reach narrative log");
            return log.Entries[0].text;
        }
        static void Eq(string expected, StatGroupVariable group, string reason)
        {
            assertions++;
            string actual = Render(group);
            if (actual != "<size=18>" + expected + "</size>")
                throw new Exception(reason + ": " + actual);
        }
        public static int Run()
        {
            assertions = 0;
            var previous = LocalizationManager.Instance;
            var locale = new Locale();
            LocalizationManager.Instance = new LocalizationManager { LocaleResolver = locale };
            try
            {
                Eq("학문", Group(Stat("literacy")), "Opaque asset name resolves actual literacy reference");
                Eq("화술", Group(Stat("talking")), "The same checkpoint ID must not imply literacy");
                var unrelated = Group(Stat("literacy"));
                unrelated.name = "Unrelated_Internal_ID";
                Eq("학문", unrelated, "Resolution is independent of checkpoint spelling");
                Eq("(학문 + 화술)", Group(Stat("literacy"), Stat("talking")), "All referenced stats remain visible");
                Eq("(학문 + 10)", Group(Stat("literacy"), Constant(10)), "Constants are retained in expressions");
                var negative = Stat("literacy");
                negative._negative = true;
                Eq("-(학문)", Group(negative), "Negative stat reference");
                var negativeConstant = Constant(int.MinValue);
                negativeConstant._negative = true;
                Eq("(학문 + -2147483648)", Group(Stat("literacy"), negativeConstant), "Constant sign preserves the game's unchecked integer semantics");
                foreach (var operation in new[] { StatGroupType.Sum, StatGroupType.Max, StatGroupType.Min, StatGroupType.Multiply })
                {
                    var group = Group(Stat("literacy"), Constant(2));
                    group._groupType = operation;
                    Eq(operation == StatGroupType.Sum ? "(학문 + 2)" : operation == StatGroupType.Max ? "최댓값(학문, 2)" :
                        operation == StatGroupType.Min ? "최솟값(학문, 2)" : "(학문 × 2)", group, "Group operation: " + operation);
                }
                Eq("((학문 + 2) + 화술)", Group(Nested(Group(Stat("literacy"), Constant(2))), Stat("talking")), "Nested expression");
                var nested = Nested(Group(Stat("literacy")));
                nested._negative = true;
                Eq("학문", Group(nested), "Game ignores negative flag on nested-group references");
                var shared = Group(Stat("literacy"));
                Eq("(학문 + 학문)", Group(Nested(shared), Nested(shared)), "Repeated group is not a cycle");
                Eq(Checkpoint, Group(Stat("literacy"), new StatValueReference { _checkType = StatCheckType.旗標 }), "Unsupported term cannot become partial literacy label");
                Eq(Checkpoint, Group(new StatValueReference { _checkType = StatCheckType.角色數值 }), "Missing stat keeps prior display");
                Eq(Checkpoint, Group(Stat("missing")), "Missing localization keeps prior display");
                var missingKey = Stat("literacy");
                missingKey._stat.LocaleKey = null;
                Eq(Checkpoint, Group(missingKey), "Missing locale key keeps prior display");
                locale.Text["PlayerStat/literacy"] = "";
                Eq(Checkpoint, Group(Stat("literacy")), "Empty localization keeps prior display");
                locale.Text["PlayerStat/literacy"] = "學問";
                Eq(Checkpoint, Group(), "ID alone is never translated to literacy");
                Eq(Checkpoint, Group(Constant(50)), "Constant-only group retains prior name");
                Eq(Checkpoint, Group(new StatValueReference[] { null }), "Malformed reference keeps prior display");
                var cycle = Group(Stat("literacy"));
                cycle._value.Add(Nested(cycle));
                Eq(Checkpoint, cycle, "Cycle stays bounded without partial naming");
                var deep = Group(Stat("literacy"));
                for (int index = 0; index < 20; index++) deep = Group(Nested(deep));
                Eq(Checkpoint, deep, "Depth limit keeps prior display");
                var wide = Group(Stat("literacy"));
                for (int index = 0; index < 8; index++) wide = Group(Nested(wide), Nested(wide));
                Eq(Checkpoint, wide, "Shared graph expansion has a work budget");
                var invalid = Group(Stat("literacy"));
                invalid._groupType = (StatGroupType)999;
                Eq(Checkpoint, invalid, "Unknown operation keeps prior display");
                LocalizationManager.Instance = null;
                Eq(Checkpoint, Group(Stat("literacy")), "Missing localization manager keeps prior display");
                return assertions;
            }
            finally { LocalizationManager.Instance = previous; }
        }
    }
}
