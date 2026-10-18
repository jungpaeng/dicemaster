using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Mono.Cecil;
using Mono.Cecil.Cil;

// These checks inspect the delivered assembly, in addition to behavioral model
// and actual bridge IL tests. A standalone, disconnected UI implementation must
// not satisfy the candidate build gate.
static class Program
{
    static int checks;
    static void Check(bool condition, string message)
    { checks++; if (!condition) throw new InvalidDataException(message); }
    static IEnumerable<TypeDefinition> All(IEnumerable<TypeDefinition> types)
    { foreach (var type in types) { yield return type; foreach (var child in All(type.NestedTypes)) yield return child; } }
    static IEnumerable<MethodReference> Calls(MethodDefinition method) => method.HasBody
        ? method.Body.Instructions.Select(i => i.Operand).OfType<MethodReference>() : Enumerable.Empty<MethodReference>();
    static void Main(string[] args)
    {
        using var assembly = AssemblyDefinition.ReadAssembly(args.Single());
        var module = assembly.MainModule;
        var types = All(module.Types).ToArray();
        TypeDefinition Type(string name) => types.Single(t => t.FullName == name);
        MethodDefinition Method(string owner, string name) => Type(owner).Methods.Single(m => m.Name == name);
        const string plugin = "DiceMaster.DicePlugin", bridge = "DiceMaster.LiveLog.LogBridge";
        const string renderer = "DiceMaster.LogUI.LogRenderer", pad = "DiceMaster.Gamepad.GamepadBridge";
        var update = Method(plugin, "Update");
        var updateCalls = Calls(update).ToArray();
        int log = Array.FindIndex(updateCalls, c => c.DeclaringType.FullName == pad && c.Name == "TickLog");
        int dice = Array.FindIndex(updateCalls, c => c.DeclaringType.FullName == "DiceMaster.DicePatch" && c.Name == "UpdateGamepad");
        Check(log >= 0 && dice > log, "actual Update must assign log ownership before dice input");
        Check(update.Body.Instructions.Any(i => i.Operand is FieldReference f && f.Name == "LogMenuToggleKey"), "existing configurable open key is used");
        Check(updateCalls.Any(c => c.Name == "GetKeyDown"), "keyboard open entry point retained");
        Check(Calls(Method(plugin, "OnGUI")).Any(c => c.DeclaringType.FullName == renderer && c.Name == "Draw"), "actual OnGUI calls replacement renderer");
        // BeginArea content does not contribute entries to GUILayout.Window's
        // root layout. Inspect the shipped Draw call itself: fixture rendering
        // alone cannot catch Unity shrinking that empty automatic root.
        var draw = Method(renderer, "Draw");
        var fixedWindowParameters = new[] { "System.Int32", "UnityEngine.Rect", "UnityEngine.GUI/WindowFunction", "System.String", "UnityEngine.GUIStyle" };
        Check(draw.Body.Instructions.Where(i => i.OpCode == OpCodes.Call).Select(i => i.Operand).OfType<MethodReference>()
            .Count(c => c.DeclaringType.FullName == "UnityEngine.GUI" && c.Name == "Window" && !c.HasThis
                && c.ReturnType.FullName == "UnityEngine.Rect"
                && c.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(fixedWindowParameters)) == 1,
            "actual renderer Draw must call the five-argument fixed-rect GUI.Window API exactly once");
        Check(!Type(renderer).Methods.SelectMany(Calls).Any(c => c.DeclaringType.FullName == "UnityEngine.GUILayout" && c.Name == "Window"),
            "actual renderer must not call automatic GUILayout.Window: BeginArea leaves its root layout empty");
        Check(Type(plugin).Fields.Any(f => f.FieldType.FullName == renderer), "plugin retains renderer instance across frames");
        Check(Calls(Method(bridge, "Publish")).Any(c => c.DeclaringType.Namespace == "DiceMaster.LogUI" && c.Name == "Publish"), "actual data publication reaches structured UI model");
        Check(Type(renderer).Methods.SelectMany(Calls).Any(c => c.DeclaringType.FullName == bridge && c.Name == "TryReserve"), "renderer reservation uses current bridge validation");
        Check(Type(renderer).Methods.SelectMany(Calls).Any(c => c.DeclaringType.FullName == bridge && c.Name == "IsReserved"), "reservation display reads actual reservation source");
        Check(types.SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions)
            .Count(i => i.OpCode == OpCodes.Newobj && i.Operand is MethodReference c && c.DeclaringType.FullName == "DiceMaster.Gamepad.NativeXInput") == 1,
            "log and dice must share one native XInput instance");
        var resolver = Type("DiceMaster.Gamepad.GameInputModeResolver");
        Check(!resolver.Methods.SelectMany(Calls).Any(c => c.Name.StartsWith("set_") || c.Name == "OnControlsChanged"), "mode resolver never forces game control scheme");
        Check(module.AssemblyReferences.All(r => r.Name != "Unity.InputSystem"), "read-only mode reflection adds no unpinned assembly reference");
        var resources = module.Resources.OfType<EmbeddedResource>().ToArray();
        Check(resources.Length == 8 && resources.All(r => r.Name.StartsWith("DiceMaster.")), "only eight existing mod resources embedded");
        Check(!resources.Any(r => r.Name.Contains("Mortal.Core") || r.Name.Contains("Mortal.Story")), "game DLLs are not embedded");
        var preservedResources = new Dictionary<string, string> {
            ["DiceMaster.Assets.mortaldice"] = "5cb5c10adea77234bbef4f8f348514526c9765e9c45fe2b4e8d3c334ecfc0ff6",
            ["DiceMaster.Gamepad.Native.LICENSE.txt"] = "d5c2224982b0b95a16b098a561335fa93f3eaf4d0aa964b7843edb986df78dc8",
            ["DiceMaster.Gamepad.Native.MinHook.x64.dll"] = "033e55af8995c3b260e56de73daf99ff4a7ce2fcbe4b3872db2270678d8c6585",
            ["DiceMaster.Gamepad.Native.MinHook.x86.dll"] = "1f28d13b8133551c8896cc556db768c94ebfaae0ad38e86719f3151f7b4ce1fc",
            ["DiceMaster.Translations.ModTranslation.txt"] = "7441c94d83df9f2ebe6ecdfe0489c752afe7ebeadc7b28ae6d15aca460acf5eb",
            ["DiceMaster.Translations.Pairs"] = "172489a3a34fd27d93410b71b23dcfeb6d597723e5d2e0f4878eef464899ec00",
            ["DiceMaster.Translations.TriggerTranslation1.0.5v.txt"] = "a528b4740da8fe1fd5218e50d8113720306ea1db21b959b0ab1545c13587dbd4",
            ["DiceMaster.Translations._Substitutions.txt"] = "640d5fc2a5fae476b766c5b59481a10e7e7d68a605557485f5bf2277e6569909"
        };
        foreach (var resource in resources)
            Check(Convert.ToHexString(SHA256.HashData(resource.GetResourceData())).Equals(preservedResources[resource.Name], StringComparison.OrdinalIgnoreCase),
                "PRD 2 resource bytes preserved: " + resource.Name);
        Console.WriteLine($"PASS: {checks} actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested.");
    }
}
