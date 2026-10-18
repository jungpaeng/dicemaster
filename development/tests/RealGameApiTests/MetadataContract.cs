using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class RealGameApiTests
{
    static void Check(bool value, string reason) => Eq(true, value, reason);
    static MethodReference[] Calls(MethodDefinition method) => method.Body.Instructions
        .Where(instruction => instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
        .Select(instruction => (MethodReference)instruction.Operand).ToArray();
    static string[] ParameterTypes(MethodReference method) => method.Parameters.Select(parameter => parameter.ParameterType.FullName).ToArray();
    static bool ParametersAre(MethodReference method, params string[] parameters) => ParameterTypes(method).SequenceEqual(parameters);
    static Instruction[] Instructions(MethodDefinition method) => method.Body.Instructions.Where(instruction => instruction.OpCode != OpCodes.Nop).ToArray();

    // Metadata/IL compatibility checks only. Harmony patch installation and the
    // game's coroutine/native execution are not performed by this test suite.
    static void VerifyMetadataContract(AssemblyDefinition candidate, string managedRoot)
    {
        using var actualCore = AssemblyDefinition.ReadAssembly(Path.Combine(managedRoot, "Mortal.Core.dll"));
        using var actualFungus = AssemblyDefinition.ReadAssembly(Path.Combine(managedRoot, "Fungus.dll"));
        string harmonyPath = Path.GetFullPath(Path.Combine(managedRoot, "..", "..", "BepInEx", "core", "0Harmony.dll"));
        using var actualHarmony = AssemblyDefinition.ReadAssembly(harmonyPath);
        var actualModules = new[] { actualCore.MainModule, actualFungus.MainModule };
        var patch = candidate.MainModule.GetType("DiceMaster.OptionInfoPatch");
        Check(patch != null, "candidate OptionInfoPatch exists");
        var expected = new Dictionary<string, (string Owner, string Name, string Result, string[] Arguments)>
        {
            ["SaveSystem_NewGameData"] = ("Mortal.Core.SaveSystem", "NewGameData", "System.Void", Array.Empty<string>()),
            ["SaveSystem_NewActGameData"] = ("Mortal.Core.SaveSystem", "NewActGameData", "System.Void", Array.Empty<string>()),
            ["SaveSystem_LoadGameData"] = ("Mortal.Core.SaveSystem", "LoadGameData", "System.Void", Array.Empty<string>()),
            ["SaveSystem_AutoLoadGameData"] = ("Mortal.Core.SaveSystem", "AutoLoadGameData", "System.Void", new[] { "System.String" }),
            ["SaveSystem_LoadRestoreSaveData"] = ("Mortal.Core.SaveSystem", "LoadRestoreSaveData", "System.Void", Array.Empty<string>()),
            ["SaveSystem_ExecuteLoadGameData"] = ("Mortal.Core.SaveSystem", "ExecuteLoadGameData", "System.Void", new[] { "Mortal.Core.GameSave" }),
            ["SceneController_StartLoadScene"] = ("Mortal.Core.SceneController", "StartLoadScene", "System.Collections.IEnumerator", new[] { "System.String", "System.Boolean" }),
            ["LuaStage"] = ("Fungus.LuaEnvironment", "LoadLuaFunction", "MoonSharp.Interpreter.Closure", new[] { "System.String", "System.String" }),
            ["LuaComplete"] = ("Fungus.LuaEnvironment", "LoadLuaFunction", "MoonSharp.Interpreter.Closure", new[] { "System.String", "System.String" }),
            ["LuaAbort"] = ("Fungus.LuaEnvironment", "LoadLuaFunction", "MoonSharp.Interpreter.Closure", new[] { "System.String", "System.String" })
        };
        var hooks = patch.Methods.Where(method => method.Name.StartsWith("LiveLog_", StringComparison.Ordinal)).ToArray();
        Eq(17, hooks.Length, "exact added lifecycle hook count");
        Eq(hooks.Length, hooks.Select(method => method.Name).Distinct().Count(), "unique added lifecycle hook names");
        var targetRoles = new HashSet<string>();
        foreach (var hook in hooks)
        {
            int suffixAt = hook.Name.LastIndexOf('_');
            string key = hook.Name.Substring("LiveLog_".Length, suffixAt - "LiveLog_".Length);
            string role = hook.Name.Substring(suffixAt + 1);
            Check(expected.ContainsKey(key), "known lifecycle hook " + hook.Name);
            var contract = expected[key];
            Check(role is "Prefix" or "Postfix" or "Finalizer", "known Harmony hook role " + hook.Name);
            Check(hook.IsStatic && hook.HasBody, "static lifecycle hook body " + hook.Name);
            var attributes = hook.CustomAttributes.Where(attribute => attribute.AttributeType.FullName == "HarmonyLib.HarmonyPatch").ToArray();
            Eq(1, attributes.Length, "single Harmony target " + hook.Name);
            var attribute = attributes.Single();
            Eq(2, attribute.ConstructorArguments.Count, "type/name Harmony target constructor " + hook.Name);
            var targetType = (TypeReference)attribute.ConstructorArguments[0].Value;
            Eq(contract.Owner, targetType.FullName, "actual Harmony target owner " + hook.Name);
            Eq(contract.Name, (string)attribute.ConstructorArguments[1].Value, "actual Harmony target name " + hook.Name);
            var actualModule = actualModules.Single(module => module.GetType(contract.Owner) != null);
            Eq(actualModule.Assembly.Name.Name, targetType.Scope.Name, "actual Harmony target assembly " + hook.Name);
            var targets = actualModule.GetType(contract.Owner).Methods.Where(method => method.Name == contract.Name).ToArray();
            Eq(1, targets.Length, "unambiguous actual target overload " + hook.Name);
            var target = targets.Single();
            Eq(contract.Result, target.ReturnType.FullName, "actual target return type " + hook.Name);
            Check(ParametersAre(target, contract.Arguments), "actual target parameter signature " + hook.Name);
            Check(!target.IsStatic && target.HasBody, "actual target instance managed method " + hook.Name);
            Check(targetRoles.Add(contract.Owner + "::" + contract.Name + ":" + role), "no duplicate added target/role hook " + hook.Name);
            foreach (var marker in hook.CustomAttributes)
            {
                var actualAttribute = actualHarmony.MainModule.GetType(marker.AttributeType.FullName);
                Check(actualAttribute != null, "attribute type exists in supplied Harmony " + marker.AttributeType.FullName);
                Eq(1, actualAttribute.Methods.Count(method => method.IsConstructor && !method.IsStatic &&
                    ParameterTypes(method).SequenceEqual(ParameterTypes(marker.Constructor))), "attribute constructor exists in supplied Harmony " + marker.AttributeType.FullName);
            }
            var markers = hook.CustomAttributes.Where(value => value.AttributeType.FullName != "HarmonyLib.HarmonyPatch").ToArray();
            Eq(1, markers.Length, "single lifecycle role marker " + hook.Name);
            Eq("HarmonyLib.Harmony" + role, markers.Single().AttributeType.FullName, "matching lifecycle role marker " + hook.Name);
            var state = hook.Parameters.Single(parameter => parameter.Name == "__state");
            Eq(role == "Prefix" ? "System.Int64&" : "System.Int64", state.ParameterType.FullName, "paired long state " + hook.Name);
            Eq(role == "Prefix", state.IsOut, "prefix state is out, consumers are values " + hook.Name);
            if (role == "Finalizer")
            {
                Eq("System.Exception", hook.ReturnType.FullName, "finalizer returns original exception type " + hook.Name);
                Check(ParametersAre(hook, "System.Exception", "System.Int64"), "finalizer exception/state parameters " + hook.Name);
                Eq("__exception", hook.Parameters[0].Name, "Harmony exception parameter name " + hook.Name);
                var instructions = Instructions(hook);
                Check(instructions[^2].OpCode == OpCodes.Ldarg_0 && instructions[^1].OpCode == OpCodes.Ret,
                    "finalizer returns unchanged exception input " + hook.Name);
                Check(hook.Body.ExceptionHandlers.Count == 0 && !instructions.Any(instruction => instruction.OpCode == OpCodes.Starg || instruction.OpCode == OpCodes.Starg_S),
                    "finalizer does not overwrite/catch exception input " + hook.Name);
                Eq(1, instructions.Count(instruction => instruction.OpCode == OpCodes.Ret), "single exception-preserving return " + hook.Name);
                Eq(1, Calls(hook).Length, "finalizer only calls lifecycle cleanup " + hook.Name);
                Eq("DiceMaster.LiveLog.LogBridge", Calls(hook).Single().DeclaringType.FullName, "finalizer cleanup owner " + hook.Name);
                Eq(key == "LuaAbort" ? "AbortLua" : "ExitTransition", Calls(hook).Single().Name, "finalizer cleanup target " + hook.Name);
            }
            else
            {
                Eq("System.Void", hook.ReturnType.FullName, "prefix/postfix does not replace game result " + hook.Name);
                if (role == "Postfix")
                {
                    Check(ParametersAre(hook, "System.Object", "System.Int64"), "Lua completion accepts result object and state");
                    Eq("__result", hook.Parameters[0].Name, "Lua result Harmony parameter name");
                    Check(!target.ReturnType.IsValueType && target.ReturnType.FullName != "System.Void", "actual Lua return can be passed as object");
                }
                else if (key == "LuaStage")
                {
                    Check(ParametersAre(hook, "System.String", "System.String", "System.Int64&"), "Lua prefix input/state types");
                    Eq("luaString", hook.Parameters[0].Name, "Lua prefix actual input name");
                    Eq("friendlyName", hook.Parameters[1].Name, "Lua prefix actual friendly name");
                    Check(target.Parameters.Select(parameter => parameter.Name).SequenceEqual(new[] { "luaString", "friendlyName" }), "actual Lua input parameter names match Harmony bindings");
                }
                else Check(ParametersAre(hook, "System.Int64&"), "transition prefix only accepts out state " + hook.Name);
            }
        }
        var oldLua = patch.Methods.Single(method => method.Name == "LuaEnvironment_LoadLuaFunction_Prefix");
        var oldLuaInstructions = Instructions(oldLua);
        bool emptyOldLua = oldLuaInstructions.Length == 1 && oldLuaInstructions[0].OpCode == OpCodes.Ret;
        bool guardedEmptyOldLua = oldLuaInstructions.Length == 4 && oldLuaInstructions[0].OpCode == OpCodes.Ldsfld &&
            oldLuaInstructions[0].Operand is FieldReference guard && guard.DeclaringType.FullName == patch.FullName && guard.Name == "DisplayRefreshDepth" &&
            oldLuaInstructions[1].OpCode == OpCodes.Brfalse && oldLuaInstructions[1].Operand == oldLuaInstructions[3] &&
            oldLuaInstructions[2].OpCode == OpCodes.Ret && oldLuaInstructions[3].OpCode == OpCodes.Ret;
        bool compiledEmptyOldLua = oldLuaInstructions.Length == 3 && oldLuaInstructions[0].OpCode == OpCodes.Ldsfld &&
            oldLuaInstructions[0].Operand is FieldReference read && read.DeclaringType.FullName == patch.FullName && read.Name == "DisplayRefreshDepth" &&
            oldLuaInstructions[1].OpCode == OpCodes.Pop && oldLuaInstructions[2].OpCode == OpCodes.Ret;
        Check(emptyOldLua || guardedEmptyOldLua || compiledEmptyOldLua, "old Lua prefix is a no-op with optional preview guard");
        foreach (var item in new[] { ("LiveLog_LuaStage_Prefix", "StageLua"), ("LiveLog_LuaComplete_Postfix", "CompleteLua") })
        {
            var method = hooks.Single(hook => hook.Name == item.Item1);
            Eq(1, Calls(method).Count(call => call.DeclaringType.FullName == "DiceMaster.LiveLog.LogBridge" && call.Name == item.Item2), "paired Lua lifecycle call " + item.Item1);
        }
        VerifySafeWrapperContract(candidate, patch);
        Console.WriteLine("METADATA CONTRACT: actual game/Harmony target signatures, paired hooks, exception preservation and pure description wrappers verified; Harmony/native game NOT executed.");
    }

    static void VerifySafeWrapperContract(AssemblyDefinition candidate, TypeDefinition patch)
    {
        var wrappers = new[] {
            (Name: "PreviewReadGroup", Safe: "ReadGroup", Owner: "Mortal.Core.StatGroupVariable", Original: "GetResult", Result: "System.Int32"),
            (Name: "PreviewCheckComparison", Safe: "CheckComparison", Owner: "Mortal.Core.StatCompareItem", Original: "Check", Result: "System.Boolean"),
            (Name: "PreviewReadEventRate", Safe: "ReadEventRate", Owner: "Mortal.Story.PositionEventData", Original: "GetRate", Result: "System.Int32")
        };
        var descriptionTypes = new[] { "DiceMaster.DataHelper", "DiceMaster.PositionEventCache", "DiceMaster.UiTextMapper", "DiceMaster.OptionInfoPatch" }
            .Select(name => candidate.MainModule.GetType(name) ?? throw new InvalidDataException("Missing description owner " + name)).ToArray();
        foreach (var item in wrappers)
        {
            var matches = patch.Methods.Where(method => method.Name == item.Name).ToArray();
            Eq(1, matches.Length, "unique pure description wrapper " + item.Name);
            var wrapper = matches.Single();
            Check(wrapper.IsStatic && ParametersAre(wrapper, item.Owner), "typed static wrapper signature " + item.Name);
            Eq(item.Result, wrapper.ReturnType.FullName, "wrapper result signature " + item.Name);
            var instructions = Instructions(wrapper);
            Check(instructions.Length == 3 && instructions[0].OpCode == OpCodes.Ldarg_0 && instructions[1].OpCode == OpCodes.Call && instructions[2].OpCode == OpCodes.Ret,
                "wrapper unconditionally calls safe evaluator without fallback " + item.Name);
            var safe = Calls(wrapper).Single();
            Check(safe.DeclaringType.FullName == "DiceMaster.LiveLog.SafeDisplayEvaluation" && safe.Name == item.Safe && ParametersAre(safe, "System.Object"),
                "wrapper binds object safe evaluator " + item.Name);
            foreach (var type in descriptionTypes)
                foreach (var method in AllTypes(new[] { type }).SelectMany(value => value.Methods).Where(value => value.HasBody && value != wrapper))
                {
                    Eq(0, Calls(method).Count(call => call.DeclaringType.FullName == item.Owner && call.Name == item.Original), "no risky description API fallback " + method.FullName);
                }
        }
        var conditions = candidate.MainModule.GetType("DiceMaster.DataHelper").Methods.Single(method => method.Name == "ExtractConditions" &&
            ParametersAre(method, "Mortal.Core.ConditionResultItem", "System.Boolean&"));
        var conditionInstructions = Instructions(conditions);
        Check(conditions.IsStatic && conditions.ReturnType.FullName == "System.String" && conditions.Parameters[1].IsOut,
            "condition formatter preserves public signature");
        Check(conditionInstructions.Length == 4 && conditionInstructions[0].OpCode == OpCodes.Ldarg_0 && conditionInstructions[1].OpCode == OpCodes.Ldarg_1 &&
            conditionInstructions[2].OpCode == OpCodes.Call && conditionInstructions[3].OpCode == OpCodes.Ret, "condition formatting unconditionally uses preview model");
        var conditionCall = Calls(conditions).Single();
        Check(conditionCall.DeclaringType.FullName == "DiceMaster.LiveLog.PreviewFormatting" && conditionCall.Name == "ExtractConditions" &&
            ParametersAre(conditionCall, "System.Object", "System.Boolean&") && conditionCall.ReturnType.FullName == "System.String", "condition formatter exact payload contract");
        var events = candidate.MainModule.GetType("DiceMaster.PositionEventCache").Methods.Single(method => method.Name == "GenerateDynamicDesc");
        var eventInstructions = Instructions(events);
        Check(!events.IsStatic && ParametersAre(events, "Mortal.Story.PositionEventData") && events.ReturnType.FullName == "System.String", "event formatter preserves instance signature");
        Check(eventInstructions.Length == 3 && eventInstructions[0].OpCode == OpCodes.Ldarg_1 && eventInstructions[1].OpCode == OpCodes.Call && eventInstructions[2].OpCode == OpCodes.Ret,
            "event formatting unconditionally uses preview model");
        var eventCall = Calls(events).Single();
        Check(eventCall.DeclaringType.FullName == "DiceMaster.LiveLog.PreviewFormatting" && eventCall.Name == "EventDescription" &&
            ParametersAre(eventCall, "System.Object") && eventCall.ReturnType.FullName == "System.String", "event formatter exact payload contract");
        var remainingRiskyCalls = AllTypes(candidate.MainModule.Types).SelectMany(type => type.Methods).Where(method => method.HasBody)
            .SelectMany(method => method.Body.Instructions.Select(instruction => instruction.Operand).OfType<MethodReference>()
                .Where(call => wrappers.Any(wrapper => call.DeclaringType.FullName == wrapper.Owner && call.Name == wrapper.Original))
                .Select(call => method.FullName + " -> " + call.FullName)).ToArray();
        Check(remainingRiskyCalls.Length == 0, "no risky original description API references anywhere, including nested/lambda types: " + string.Join("; ", remainingRiskyCalls));
        var evaluators = new[] { "DiceMaster.LiveLog.SafeDisplayEvaluation", "DiceMaster.LiveLog.RandomPreview", "DiceMaster.LiveLog.PreviewFormatting" }
            .Select(name => candidate.MainModule.GetType(name) ?? throw new InvalidDataException("Missing preview payload " + name)).ToArray();
        foreach (var method in AllTypes(evaluators).SelectMany(type => type.Methods).Where(method => method.HasBody))
            Check(!Calls(method).Any(call => wrappers.Any(wrapper => call.DeclaringType.FullName == wrapper.Owner && call.Name == wrapper.Original) ||
                (call.DeclaringType.FullName == "Mortal.Core.ConditionResultData" && call.Name == "Execute") ||
                (call.DeclaringType.FullName == "Mortal.Core.StatValueReference" && call.Name == "GetValue") ||
                call.DeclaringType.FullName == "UnityEngine.Random"), "safe evaluator contains no original risky call " + method.FullName);
        var randomPreview = candidate.MainModule.GetType("DiceMaster.LiveLog.RandomPreview");
        var independentRandom = randomPreview.Fields.Where(field => field.FieldType.FullName == "System.Random").ToArray();
        Eq(1, independentRandom.Length, "exactly one mod-owned independent RNG instance");
        Check(independentRandom.Single().IsPrivate && independentRandom.Single().IsStatic,
            "independent RNG is privately owned by display evaluator");
        var randomInstructions = AllTypes(new[] { randomPreview }).SelectMany(type => type.Methods).Where(method => method.HasBody)
            .SelectMany(method => method.Body.Instructions).ToArray();
        Check(randomInstructions.Any(instruction => instruction.OpCode == OpCodes.Newobj && instruction.Operand is MethodReference constructor &&
            constructor.DeclaringType.FullName == "System.Random"), "display evaluator constructs its own RNG");
        Check(randomInstructions.Any(instruction => instruction.Operand is MethodReference method &&
            method.DeclaringType.FullName == "System.Random" && method.Name == "NextBytes"), "display evaluator samples only its owned managed RNG");
        foreach (var method in AllTypes(evaluators).SelectMany(type => type.Methods).Where(method => method.HasBody))
            Check(!method.Body.Instructions.Any(instruction => (instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Stsfld) &&
                instruction.Operand is FieldReference field && field.DeclaringType.FullName == "Mortal.Core.ConditionResultData" && field.Name == "_count"),
                "display evaluator never writes game condition execution count " + method.FullName);
    }
}
