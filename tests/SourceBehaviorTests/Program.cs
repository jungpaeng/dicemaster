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
    static List<string> Run(string candidate, string fixture, string output, string name)
    {
        using var source = AssemblyDefinition.ReadAssembly(candidate);
        using var target = AssemblyDefinition.ReadAssembly(fixture);
        new PayloadCopier(source.MainModule, target.MainModule, type =>
            type.FullName is "DiceMaster.EmbeddedTranslations.Display" or "DiceMaster.DicePatch");
        foreach (var type in Types(target.MainModule.Types))
        {
            type.CustomAttributes.Clear();
            foreach (var method in type.Methods) method.CustomAttributes.Clear();
            foreach (var field in type.Fields) field.CustomAttributes.Clear();
        }
        foreach (EmbeddedResource resource in source.MainModule.Resources.OfType<EmbeddedResource>())
            target.MainModule.Resources.Add(new EmbeddedResource(resource.Name, resource.Attributes, resource.GetResourceData()));
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
        Console.WriteLine($"PASS: {assertions} source translation/dice behavior assertions; actual mod IL and embedded translation data, Unity/loader/input boundaries are fixtures. Windows/UI/native NOT executed.");
    }
}
