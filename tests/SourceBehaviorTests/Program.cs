using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Mono.Cecil;

static class Program
{
    static int assertions;
    static void Check(bool ok, string label) { assertions++; if (!ok) throw new InvalidOperationException(label); }
    static object Call(Type type, string method, params object[] args) => type.GetMethod(method,
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, args);
    static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> roots)
    { foreach (var type in roots) { yield return type; foreach (var child in Types(type.NestedTypes)) yield return child; } }
    const string ParserFixtureResource = "SourceBehavior.TranslationParserCases";
    const string ParserFixture = """
        ﻿# 주석=적용 안 함
        // 다른 주석=적용 안 함

        일반=일반 값
        Note: 隱藏\=甲=참고: 숨김\=갑
        路徑\\=경로
        旗標\\\=甲=플래그\=갑
        중복=먼저
        중복=나중
        값=중간=등호 유지
        줄\n바꿈=줄\n번역
        未知\q=알 수 없는\q
        구분자없음\=무시
        =빈 키
        빈 값=
        """;
    static void CheckDisplayLabels(Type display)
    {
        string Label(string text) => (string)Call(display, "TranslateLabel", text);
        string[] source = { "大成功", "大失敗", "成功", "失敗", "正常", "特殊", "其他" };
        string[] translated = { "대성공", "대실패", "성공", "실패", "일반", "특수", "기타" };
        string[] wrappers = { "{0}", "<color=#FFD700>{0}</color>", "위치이벤트: <color=#FFD700>{0}</color>", "▶ {0}\n미독" };
        for (int state = 0; state < source.Length; state++)
            foreach (string number in new[] { "0", "1", "42", "9999" })
                foreach (string wrapper in wrappers)
                {
                    string input = string.Format(wrapper, "P_" + source[state] + "_" + number);
                    string expected = string.Format(wrapper, "P_" + translated[state] + "_" + number);
                    Check(Label(input) == expected, "display result title uses Korean: " + input);
                    Check(Label(expected) == expected, "display title conversion is idempotent: " + expected);
                }
        foreach (string identifier in new[] { "Flag_成功", "S_條件_1", "C_成功_1", "P_成功_1_extra", "Mission_特殊_1", "XP_成功_1", "P_成功_1x" })
            foreach (string wrapper in wrappers)
            {
                string input = string.Format(wrapper, identifier);
                Check(Label(input) == input, "non-result identifiers retain their identity: " + input);
            }
        Check(Label(null) == null && Label("") == "", "display label preserves null and empty input");
    }
    static void CheckTranslationLoader(Assembly assembly)
    {
        Type mapper = assembly.GetType("DiceMaster.UiTextMapper", true);
        Dictionary<string, string> Load(string name)
        {
            var result = new Dictionary<string, string>();
            Call(mapper, "LoadPairsFromEmbeddedResource", name, result);
            return result;
        }
        var sample = Load(ParserFixtureResource);
        foreach (var expected in new Dictionary<string, string>
        {
            ["일반"] = "일반 값",
            [@"隱藏\=甲"] = @"숨김\=갑",
            ["隱藏=甲"] = "숨김=갑",
            [@"路徑\\"] = "경로",
            [@"路徑\"] = "경로",
            [@"旗標\\\=甲"] = @"플래그\=갑",
            [@"旗標\=甲"] = "플래그=갑",
            ["중복"] = "먼저",
            ["값"] = "중간=등호 유지",
            [@"줄\n바꿈"] = @"줄\n번역",
            ["줄\n바꿈"] = "줄\n번역",
            [@"未知\q"] = @"알 수 없는\q"
        })
            Check(sample.TryGetValue(expected.Key, out string value) && value == expected.Value,
                "actual resource loader preserves delimiters and escaped variants: " + expected.Key);
        Check(!sample.ContainsKey("") && !sample.ContainsKey("빈 값") && !sample.ContainsKey(@"구분자없음\") &&
            !sample.Keys.Any(key => key.StartsWith("//") || key.StartsWith("#")), "actual loader skips invalid and comment rows");
        Check(!sample.ContainsKey(@"隱藏\") && !sample.ContainsKey(@"旗標\\\"),
            "actual loader does not create a key truncated at an escaped delimiter");

        int rows = 0, escapedRows = 0;
        string escapedSource = null, escapedValue = null;
        foreach (string resource in new[] { "DiceMaster.Translations._Substitutions.txt", "DiceMaster.Translations.ModTranslation.txt", "DiceMaster.Translations.TriggerTranslation1.0.5v.txt" })
        {
            var loaded = Load(resource);
            var seen = new HashSet<string>();
            using var reader = new StreamReader(assembly.GetManifestResourceStream(resource), Encoding.UTF8);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string text = line.TrimStart('\ufeff').Trim();
                if (text.Length == 0 || text.StartsWith("//") || text.StartsWith("#")) continue;
                // Independently find each possible delimiter, then reject one preceded
                // by an odd number of backslashes. Values may contain further '='.
                int split = Enumerable.Range(0, text.Length).Where(index => text[index] == '=')
                    .FirstOrDefault(index => text.Take(index).Reverse().TakeWhile(c => c == '\\').Count() % 2 == 0, -1);
                if (split <= 0 || split >= text.Length - 1) continue;
                string key = text.Substring(0, split).Trim();
                string expected = text.Substring(split + 1).Trim();
                if (key.StartsWith("Note:")) key = key.Substring("Note:".Length).Trim();
                if (expected.StartsWith("참고:")) expected = expected.Substring("참고:".Length).Trim();
                rows++;
                if (key.Contains(@"\=")) { escapedRows++; escapedSource = key; escapedValue = expected; }
                if (seen.Add(key))
                    Check(loaded.TryGetValue(key, out string value) && value == expected,
                        "embedded loader keeps complete source/value pair: " + resource + ":" + key);
            }
        }
        Check(rows == 847 && escapedRows == 1, "embedded translation corpus includes 847 pairs and the escaped-equals row");

        const string source = "大師兄對龍湘的好感\n最終決定大師兄會否和龍湘在一起。\n={{A}}，主角不攻略龍湘，龍湘會追著大師兄跑\n={{B}}，主角攻略龍湘，大師兄會退讓\n={{C}}，主角攻略龍湘，大師兄會和你決鬥";
        const string expectedValue = "용상에 대한 대사형의 호감도\n대사형이 용상과 함께할지 여부 최종 결정\n={{A}}, 주인공이 용상을 공략하지 않으면 용상이 대사형을 쫓아다님\n={{B}}, 주인공이 용상을 공략하면 대사형이 양보함\n={{C}}, 주인공이 용상을 공략하면 대사형과 결투함";
        var modPairs = Load("DiceMaster.Translations.ModTranslation.txt");
        Check(modPairs.TryGetValue(source, out string restored) && restored == expectedValue,
            "actual escaped-equals line loads the complete runtime multiline key and Korean value");
        string applied = (string)Call(mapper, "ApplyTranslations", source);
        Check(applied == expectedValue,
            "actual escaped-equals line reaches the Korean value through ApplyTranslations; actual: " + applied);
        Check((string)Call(mapper, "ApplyTranslations", escapedSource) == escapedValue,
            "raw embedded escaped-equals spelling reaches the complete Korean value");
        string NormalizeNewlines(string value) => value.Replace(@"\r\n", "\n").Replace(@"\n", "\n").Replace(@"\r", "\n");
        Check((string)Call(mapper, "ApplyTranslations", NormalizeNewlines(escapedSource)) == NormalizeNewlines(escapedValue),
            "runtime multiline spelling with escaped equals reaches the complete Korean value");
        foreach (string input in new[] { "Flag_成功", "S_條件_1", "C_成功_1", "Mission_特殊_1" })
            Check((string)Call(mapper, "ApplyTranslations", input) == input, "full mapper keeps internal identifier: " + input);
        Check((string)Call(mapper, "ApplyTranslations", "小師妹") == "소사매", "actual mapper continues normal dictionary translation");
        Type display = assembly.GetType("DiceMaster.EmbeddedTranslations.Display", true);
        foreach (var pair in new Dictionary<string, string>
        {
            ["大成功"] = "대성공", ["大失敗"] = "대실패", ["成功"] = "성공", ["失敗"] = "실패",
            ["正常"] = "일반", ["特殊"] = "특수", ["其他"] = "기타"
        })
        {
            string mapped = (string)Call(mapper, "ApplyTranslations", "위치이벤트: P_" + pair.Key + "_42");
            Check((string)Call(display, "TranslateLabel", mapped) == "위치이벤트: P_" + pair.Value + "_42",
                "full display pipeline converts result titles after mapper masking: " + pair.Key);
        }
    }
    sealed class DisplayNode
    {
        public string name = "", _devNote = "", _note = "", _id = "", _key = "";
        public int _checkType, _constant, _groupType = 1, _logicOp, _compareOp, State, Value;
        public object _flag, _relationship, _book, _talentData, _upgradeItemData, _gameTime, _libraryAchieve;
        public object[] _value = Array.Empty<object>(), _items = Array.Empty<object>();
    }
    static void CheckLiteralDisplay(Assembly assembly)
    {
        Type mapper = assembly.GetType("DiceMaster.UiTextMapper", true);
        string Literal(string s) => (string)Call(mapper, "TranslateLiteral", s);
        string[] chinese = { "旗標", "常數", "時間", "好感度", "郁竹", "第二年十一月上旬", "第二年十二月上旬" };
        foreach (string word in chinese)
        {
            string value = Literal(word);
            Check(value != word && !value.Any(c => c >= '\u3400' && c <= '\u9fff'), "literal display reaches existing translation: " + word);
            Check(Literal(value) == value, "literal translation is idempotent: " + word);
        }
        Type helper = assembly.GetType("DiceMaster.DataHelper", true);
        int reads = (int)helper.GetField("FlagReads").GetValue(null);
        const string expression = "GetFlag('E12003')";
        Check(Literal(expression) == expression && (int)helper.GetField("FlagReads").GetValue(null) == reads,
            "literal descriptions never evaluate GetFlag");
        foreach (string id in new[] { "Flag_成功", "S_條件_1", "Relationship_Girl7_郁竹", "Book999_未知" })
            Check(Literal(id) == id, "unresolved identifiers stay intact: " + id);
        string rich = Literal("<color=#A3B86C>郁竹 好感度 +10</color>");
        Check(rich == "<color=#A3B86C>욱죽 호감도 +10</color>", "translation preserves result color markup");
        var dictionary = (Dictionary<string,string>)mapper.GetField("exactReplacements", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        dictionary["Book123_羅漢拳譜"] = "Book123_나한권보";
        Check(Literal("Book123_羅漢拳譜") == "Book123_나한권보" &&
            (string)Call(mapper, "ApplyTranslations", "Book123_羅漢拳譜") == "Book123_나한권보",
            "known Book display mapping runs before identifier shielding in both paths");
        dictionary.Remove("Book123_羅漢拳譜");
        var names = (Dictionary<string,string>)helper.GetField("Names").GetValue(null);
        names["Character/girl7"] = "郁竹";
        Type formatter = assembly.GetType("DiceMaster.EventSelection.PopupConditionText", true);
        object Group(DisplayNode reference, string name) => new DisplayNode { name=name, _value=new object[] { reference } };
        object Const(int n, string name) => Group(new DisplayNode { _constant=n },name);
        // Use field names consumed by actual Comparison; game values are a boundary.
        var flag = new DisplayNode { name="E12003", _devNote="小梅在唐門", State=0 };
        var flagRef = new DisplayNode { _checkType=3, _flag=flag };
        var comparison = new ConditionComparison { _value1=Group(flagRef,"旗標_E12003"), _value2=Const(1,"常數_1") };
        string condition = (string)Call(formatter,"Condition", new DisplayNode { _items=new object[] { comparison } });
        Check(condition.Contains("우소매") && condition.Contains("E12003") && !condition.Contains("旗標") && !condition.Contains("常數"),
            "actual formatter and actual embedded translations connect for a Door flag");
        var relation = new DisplayNode { name="Relationship_Girl7_郁竹", Value=10 };
        comparison._value1=Group(new DisplayNode { _checkType=2, _relationship=relation },"好感度_郁竹");
        comparison._value2=Const(50,"常數_50");
        condition=(string)Call(formatter,"Condition",new DisplayNode { _items=new object[] { comparison } });
        Check(condition.Contains("욱죽 호감도") && !condition.Contains("Relationship_Girl7"), "typed relationship name replaces raw asset identity");
        Type displayNames=assembly.GetType("DiceMaster.LiveLog.DisplayNames",true);
        names["Book/Name/123"]="羅漢拳譜";
        string book=(string)Call(displayNames,"ReferenceName",new DisplayNode { _book=new DisplayNode { _id="123",name="Book123_羅漢拳譜" } },4);
        Check(book.Contains("나한권보") && !book.Contains("Book123"),"typed book names use audited locale key");
        names["PlayerTalent/Name/test"]="學問";
        string talent=(string)Call(displayNames,"ReferenceName",new DisplayNode { _talentData=new DisplayNode { _id="test" } },10);
        Check(!talent.Contains("test") && talent.Contains("레벨"),"typed talent locale name is connected");
        names.Clear();
        int pending=(int)displayNames.GetProperty("PendingNames").GetValue(null);
        Check((string)Call(displayNames,"LookupName","Book/Name/late","late")=="late" &&
            (int)displayNames.GetProperty("PendingNames").GetValue(null)!=pending,"unresolved names request a presentation retry");
        names["Book/Name/late"]="羅漢拳譜";
        Check((string)Call(displayNames,"LookupName","Book/Name/late","late")=="나한권보","late locale name resolves without an inventory singleton");
        names.Clear();
        // UiTextMapper was initialized without tables. New names become visible
        // through the real loader; a translated prefix cannot hide a full name.
        string root=Path.Combine(Path.GetTempPath(),"dicemaster-late-names-"+Guid.NewGuid().ToString("N"));
        Type paths=assembly.GetType("BepInEx.Paths",true);
        string original=(string)paths.GetProperty("GameRootPath").GetValue(null);
        try
        {
            Directory.CreateDirectory(Path.Combine(root,"DataTable","StoryMappingItem"));
            File.WriteAllText(Path.Combine(root,"StringTable.csv"),"Character/fixtureperson,새 인물\n");
            File.WriteAllText(Path.Combine(root,"DataTable","StoryMappingItem","late.json"),"{\"Key\":\"小師妹遲到測試\",\"Value\":\"fixtureperson\"}");
            paths.GetProperty("GameRootPath").SetValue(null,root);
            mapper.GetField("nextNameRefresh",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,0L);
            Call(mapper,"RefreshNameMappings");
            Check(Literal("小師妹遲到測試")=="새 인물","late external human-name mapping wins before partial prefix translation");
            Check(Literal("fixtureperson")=="새 인물","late character ID mapping is added without singleton initialization");
        }
        finally { paths.GetProperty("GameRootPath").SetValue(null,original); Directory.Delete(root,true); }
    }
    sealed class ConditionComparison { public object _value1, _value2; public int _compareOp; }

    static void CheckNarrativeOutput(Assembly assembly)
    {
        Type patch = assembly.GetType("DiceMaster.OptionInfoPatch", true);
        Type managerType = assembly.GetType("Fungus.FungusManager", true);
        PropertyInfo instanceProperty = managerType.GetProperty("Instance");
        object manager = instanceProperty.GetValue(null);
        PropertyInfo logProperty = managerType.GetProperty("NarrativeLog");
        object log = logProperty.GetValue(manager);
        var entries = (System.Collections.IList)log.GetType().GetField("Entries").GetValue(log);
        foreach (var pair in new Dictionary<string, string>
        {
            ["大成功"] = "대성공", ["大失敗"] = "대실패", ["成功"] = "성공", ["失敗"] = "실패",
            ["正常"] = "일반", ["特殊"] = "특수", ["其他"] = "기타"
        })
            foreach (string number in new[] { "0", "1", "42", "9999" })
                foreach (string wrapper in new[] { "{0}", "<color=#FFD700>{0}</color>", "위치이벤트: <color=#FFD700>{0}</color>", "▶ {0}\n미독" })
                {
                    entries.Clear();
                    string input = "\r\n" + string.Format(wrapper, "P_" + pair.Key + "_" + number) + "\r\n\r\n";
                    string expected = "<size=18>" + string.Format(wrapper, "P_" + pair.Value + "_" + number) + "</size>";
                    Call(patch, "PrintToNarrativeLog", input);
                    Check(entries.Count == 1, "actual narrative route writes exactly one entry: " + input);
                    object entry = entries[0];
                    Check((string)entry.GetType().GetField("name").GetValue(entry) == "DiceMaster" &&
                        (string)entry.GetType().GetField("text").GetValue(entry) == expected,
                        "actual narrative route translates, strips blank lines and preserves formatting: " + input);
                }
        foreach (string identifier in new[] { "Flag_成功", "S_條件_1", "C_成功_1", "P_成功_1_extra", "Mission_特殊_1" })
        {
            entries.Clear();
            Call(patch, "PrintToNarrativeLog", identifier);
            Check(entries.Count == 1 && (string)entries[0].GetType().GetField("text").GetValue(entries[0]) == "<size=18>" + identifier + "</size>",
                "actual narrative route preserves non-result IDs: " + identifier);
        }
        entries.Clear();
        Call(patch, "PrintToNarrativeLog", "\r\n \n\t\n ");
        Check(entries.Count == 0, "actual narrative route does not emit blank entries");
        instanceProperty.SetValue(null, null);
        Call(patch, "PrintToNarrativeLog", "P_成功_1");
        Check(entries.Count == 0, "missing narrative manager is harmless");
        instanceProperty.SetValue(null, manager);
        logProperty.SetValue(manager, null);
        Call(patch, "PrintToNarrativeLog", "P_成功_1");
        Check(entries.Count == 0, "missing narrative sink is harmless");
        logProperty.SetValue(manager, log);
    }
    static List<string> Run(string candidate, string fixture, string output, string name)
    {
        using var source = AssemblyDefinition.ReadAssembly(candidate);
        using var target = AssemblyDefinition.ReadAssembly(fixture);
        var copier = new PayloadCopier(source.MainModule, target.MainModule, type =>
            type.FullName is "DiceMaster.EmbeddedTranslations.Display" or "DiceMaster.DicePatch" or "DiceMaster.UiTextMapper" or "DiceMaster.LiveLog.DisplayNames" or "DiceMaster.EventSelection.PopupConditionText");
        copier.CopyMethod(source.MainModule.GetType("DiceMaster.OptionInfoPatch").Methods.Single(method => method.Name == "PrintToNarrativeLog"),
            target.MainModule.GetType("DiceMaster.OptionInfoPatch"));
        foreach (var type in Types(target.MainModule.Types))
        {
            type.CustomAttributes.Clear();
            foreach (var method in type.Methods) method.CustomAttributes.Clear();
            foreach (var field in type.Fields) field.CustomAttributes.Clear();
        }
        foreach (EmbeddedResource resource in source.MainModule.Resources.OfType<EmbeddedResource>())
            target.MainModule.Resources.Add(new EmbeddedResource(resource.Name, resource.Attributes, resource.GetResourceData()));
        target.MainModule.Resources.Add(new EmbeddedResource(ParserFixtureResource, Mono.Cecil.ManifestResourceAttributes.Private,
            Encoding.UTF8.GetBytes(ParserFixture)));
        string patched = Path.Combine(output, name + ".dll");
        target.Write(patched);
        var context = new AssemblyLoadContext(name, true);
        var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(patched));
        Type display = assembly.GetType("DiceMaster.EmbeddedTranslations.Display", true);
        var observations = new List<string>();
        string Translate(string method, string value) => (string)Call(display, method, new object[] { value });
        Check(Translate("Translate", null) == null, "null translation");
        Check(Translate("Translate", "") == "", "empty translation");
        Check(Translate("Translate", "unchanged 한국어") == "unchanged 한국어", "non-Han text unchanged");
        using (var reader = new StreamReader(assembly.GetManifestResourceStream("DiceMaster.Translations.Pairs"), Encoding.UTF8))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] pair = line.Split('\t');
                if (pair.Length != 2) continue;
                string key = Encoding.UTF8.GetString(Convert.FromBase64String(pair[0]));
                string value = Encoding.UTF8.GetString(Convert.FromBase64String(pair[1]));
                // The actual translator intentionally skips strings without Han/Note.
                if (!key.Contains("Note:") && !key.Any(c => c >= '\u3400' && c <= '\u9fff')) continue;
                string result = Translate("Translate", key);
                Check(result == value, "embedded exact translation: " + key);
                observations.Add(result);
            }
        }
        foreach (string input in new[] { "<color=#fff>成功</color>", "<b>大失敗</b>", "P_大成功_1", "P_失敗_42",
                     "Stat_成功", "成功 Stat_大成功", "Note: 成功", "한국어 Success 123", "<size=20>失敗</size>" })
        {
            foreach (string method in new[] { "Translate", "TranslateLabel", "PreTranslate", "PostTranslate" })
                observations.Add(method + ":" + input + ":" + Translate(method, input));
            string translated = Translate("Translate", input);
            Check(Translate("Translate", input) == translated, "warm translation cache");
            Check(Translate("Translate", translated) == translated, "translation idempotence");
        }
        const string identifier = "Flag_成功";
        Check(Translate("PostTranslate", Translate("PreTranslate", identifier)) == identifier, "identifier mask/unmask");
        // New expected-output checks apply only to the candidate. The historical
        // observation list below stays identical for optional baseline parity.
        if (name == "candidate")
        {
            CheckDisplayLabels(display);
            CheckTranslationLoader(assembly);
            CheckNarrativeOutput(assembly);
            CheckLiteralDisplay(assembly);
        }
        Type dice = assembly.GetType("DiceMaster.DicePatch", true);
        Type plugin = assembly.GetType("DiceMaster.DicePlugin", true);
        object instance = plugin.GetProperty("Instance").GetValue(null);
        var prefab = plugin.GetField("DictInputPrefab");
        var valueField = dice.GetField("_diceValue", BindingFlags.Static | BindingFlags.NonPublic);
        object dialog = Activator.CreateInstance(assembly.GetType("Mortal.Story.DiceMenuDialog", true));
        assembly.GetType("UnityEngine.Object", true).GetField("Found").SetValue(null, dialog);
        foreach (int value in new[] { int.MinValue, -101, -1, 0, 1, 100, 101, int.MaxValue })
        {
            valueField.SetValue(null, value);
            object[] input = { 37 };
            Call(dice, "CheckPointManager_Dice_Prefix", input);
            Check((int)input[0] == (value == 0 ? 37 : value), "signed dice override " + value);
            observations.Add("dice:" + value + ":" + input[0]);
            if (value != 0) Check((int)dialog.GetType().GetField("_currentRandomValue").GetValue(dialog) == value, "dialog dice value");
        }
        prefab.SetValue(instance, null);
        valueField.SetValue(null, 12);
        object[] absent = { 37 };
        Call(dice, "CheckPointManager_Dice_Prefix", absent);
        Check((int)absent[0] == 37, "no prefab leaves original dice value");
        object[] revolution = { false };
        Check(!(bool)Call(dice, "DiceMenuDialog_CheckRevolution_Prefix", revolution) && (bool)revolution[0], "revolution override");
        Call(dice, "DiceMenuDialog_OnEnable_Postfix", dialog);
        Check((int)assembly.GetType("DiceMaster.Gamepad.GamepadBridge").GetField("Resets").GetValue(null) == 1, "OnEnable resets pad once");
        context.Unload();
        return observations;
    }
    static void Main(string[] args)
    {
        if (args.Length is < 3 or > 4) throw new ArgumentException("Usage: SourceBehaviorTests Candidate.dll Fixtures.dll WorkDirectory [Baseline.dll]");
        Directory.CreateDirectory(args[2]);
        var candidate = Run(args[0], args[1], args[2], "candidate");
        if (args.Length == 4)
        {
            var baseline = Run(args[3], args[1], args[2], "baseline");
            Check(candidate.SequenceEqual(baseline), "baseline translation/dice observations differ");
        }
        Console.WriteLine($"PASS: {assertions} source translation/parser/narrative/dice behavior assertions; actual mod IL and embedded translation data, Unity/loader/locale/game-table/narrative-sink/input boundaries are fixtures. Windows/UI/native NOT executed.");
    }
}
