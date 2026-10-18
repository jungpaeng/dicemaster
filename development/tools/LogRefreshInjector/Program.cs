using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Keep the existing mod and its resources; change only log-display evaluation.
static class LogRefreshInjector
{
    static ModuleDefinition module;
    static TypeDefinition plugin, patch, cache, mapper;
    static FieldDefinition depth, nextRefresh;
    static MethodDefinition previewDynamic, rebuild;
    static MethodDefinition Method(TypeDefinition type, string name) => type.Methods.Single(m => m.Name == name);
    static FieldDefinition Field(TypeDefinition type, string name) => type.Fields.Single(f => f.Name == name);
    static MethodReference Call(MethodDefinition method, string owner, string name) => method.Body.Instructions
        .Select(i => i.Operand).OfType<MethodReference>().First(m => m.DeclaringType.FullName == owner && m.Name == name);
    static MethodReference Ref(TypeReference owner, string name, TypeReference result, bool instance, params TypeReference[] args)
    {
        var method = new MethodReference(name, result, owner) { HasThis = instance };
        foreach (var arg in args) method.Parameters.Add(new ParameterDefinition(arg));
        return method;
    }
    static MethodDefinition NewMethod(TypeDefinition type, string name, TypeReference result, bool instance, params TypeReference[] args)
    {
        var existing = type.Methods.FirstOrDefault(m => m.Name == name);
        if (existing != null) return existing;
        var method = new MethodDefinition(name, MethodAttributes.Assembly | MethodAttributes.HideBySig |
            (instance ? (MethodAttributes)0 : MethodAttributes.Static), result);
        foreach (var arg in args) method.Parameters.Add(new ParameterDefinition(arg));
        type.Methods.Add(method);
        return method;
    }
    static FieldDefinition NewField(TypeDefinition type, string name, TypeReference value, bool isStatic)
    {
        var existing = type.Fields.FirstOrDefault(f => f.Name == name);
        if (existing != null) return existing;
        var field = new FieldDefinition(name, FieldAttributes.Assembly | (isStatic ? FieldAttributes.Static : (FieldAttributes)0), value);
        type.Fields.Add(field);
        return field;
    }
    static ILProcessor Body(MethodDefinition method)
    {
        method.Body = new MethodBody(method) { InitLocals = true, MaxStackSize = 16 };
        return method.Body.GetILProcessor();
    }
    static void GuardPreview(MethodDefinition method)
    {
        if (method.Body.Instructions.First().Operand == depth) return;
        var il = method.Body.GetILProcessor();
        var original = method.Body.Instructions.First();
        il.InsertBefore(original, Instruction.Create(OpCodes.Ldsfld, depth));
        il.InsertBefore(original, Instruction.Create(OpCodes.Brfalse, original));
        il.InsertBefore(original, Instruction.Create(OpCodes.Ret));
    }
    static void Enter(ILProcessor il)
    {
        il.Emit(OpCodes.Ldsfld, depth); il.Emit(OpCodes.Ldc_I4_1); il.Emit(OpCodes.Add); il.Emit(OpCodes.Stsfld, depth);
    }
    static void Exit(ILProcessor il)
    {
        il.Emit(OpCodes.Ldsfld, depth); il.Emit(OpCodes.Ldc_I4_1); il.Emit(OpCodes.Sub); il.Emit(OpCodes.Stsfld, depth);
    }
    static TypeReference ExceptionType() => module.GetTypeReferences().First(t => t.FullName == "System.Exception");

    static void PatchEventDescriptions()
    {
        var method = Method(cache, "GetPositionEventDesc");
        var original = method.Body.Instructions.ToArray();
        var tryGet = original.Select(i => i.Operand).OfType<MethodReference>().First(m => m.Name == "TryGetValue");
        var setItem = original.Select(i => i.Operand).OfType<MethodReference>().First(m => m.Name == "set_Item");
        var equals = original.Select(i => i.Operand).OfType<MethodReference>().First(m => m.Name == "op_Equality");
        var concat = Ref(module.TypeSystem.String, "Concat", module.TypeSystem.String, false,
            module.TypeSystem.String, module.TypeSystem.String, module.TypeSystem.String);
        var eventType = method.Parameters[0].ParameterType;
        var descriptions = Field(cache, "eventDescCache");

        // GetRate is also patched for one-shot event execution. A display query
        // must not consume that request or append gameplay checkpoint logs.
        previewDynamic = NewMethod(cache, "GeneratePreviewDynamicDesc", module.TypeSystem.String, true, eventType);
        var il = Body(previewDynamic);
        var result = new VariableDefinition(module.TypeSystem.String); previewDynamic.Body.Variables.Add(result);
        Enter(il);
        var start = Instruction.Create(OpCodes.Ldarg_0); il.Append(start);
        il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, Method(cache, "GenerateDynamicDesc")); il.Emit(OpCodes.Stloc, result);
        var done = Instruction.Create(OpCodes.Ldloc, result); il.Emit(OpCodes.Leave, done);
        var finallyStart = Instruction.Create(OpCodes.Nop); il.Append(finallyStart); Exit(il); il.Emit(OpCodes.Endfinally);
        il.Append(done); il.Emit(OpCodes.Ret);
        previewDynamic.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally) {
            TryStart = start, TryEnd = finallyStart, HandlerStart = finallyStart, HandlerEnd = done
        });

        // Only script-derived, fixed results are cached. Current values, colors,
        // activation conditions and probabilities are evaluated on every query.
        il = Body(method);
        var fixedDescription = new VariableDefinition(module.TypeSystem.String); method.Body.Variables.Add(fixedDescription);
        var lookup = Instruction.Create(OpCodes.Ldarg_0);
        var combine = Instruction.Create(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Castclass, equals.Parameters[0].ParameterType);
        il.Emit(OpCodes.Ldnull); il.Emit(OpCodes.Call, equals); il.Emit(OpCodes.Brfalse, lookup);
        il.Emit(OpCodes.Ldstr, ""); il.Emit(OpCodes.Ret);
        il.Append(lookup); il.Emit(OpCodes.Ldfld, descriptions); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldloca, fixedDescription);
        il.Emit(OpCodes.Callvirt, tryGet); il.Emit(OpCodes.Brtrue, combine);
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, Method(cache, "GenerateStaticDesc")); il.Emit(OpCodes.Stloc, fixedDescription);
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, descriptions); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldloc, fixedDescription); il.Emit(OpCodes.Callvirt, setItem);
        il.Append(combine); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, previewDynamic);
        il.Emit(OpCodes.Ldstr, "\n"); il.Emit(OpCodes.Ldloc, fixedDescription); il.Emit(OpCodes.Call, concat); il.Emit(OpCodes.Ret);

        var clear = NewMethod(cache, "ClearDisplayCaches", module.TypeSystem.Void, true);
        il = Body(clear);
        foreach (var name in new[] { "positionEventDataCache", "eventKeyCache", "eventDescCache", "scriptPrefixCache" })
        {
            var field = Field(cache, name);
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Callvirt, Ref(field.FieldType, "Clear", module.TypeSystem.Void, true));
        }
        il.Emit(OpCodes.Ret);
    }

    static void RetryEmptyEventLists()
    {
        var method = Method(cache, "GetPositionEvents");
        var old = method.Body.Instructions.Select(i => i.Operand).OfType<MethodReference>().ToArray();
        var tryGet = old.First(m => m.Name == "TryGetValue"); var setItem = old.First(m => m.Name == "set_Item");
        var field = Field(cache, "positionEventDataCache"); var list = Method(cache, "LoadPositionEvents").ReturnType;
        var count = Ref(list, "get_Count", module.TypeSystem.Int32, true);
        var il = Body(method); var value = new VariableDefinition(list); method.Body.Variables.Add(value);
        var load = Instruction.Create(OpCodes.Ldarg_0); var end = Instruction.Create(OpCodes.Ldloc, value);
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, field); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldloca, value);
        il.Emit(OpCodes.Callvirt, tryGet); il.Emit(OpCodes.Brfalse, load);
        il.Emit(OpCodes.Ldloc, value); il.Emit(OpCodes.Brfalse, load);
        il.Emit(OpCodes.Ldloc, value); il.Emit(OpCodes.Callvirt, count); il.Emit(OpCodes.Brtrue, end);
        il.Append(load); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, Method(cache, "LoadPositionEvents")); il.Emit(OpCodes.Stloc, value);
        il.Emit(OpCodes.Ldloc, value); il.Emit(OpCodes.Callvirt, count); il.Emit(OpCodes.Brfalse, end);
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, field); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldloc, value); il.Emit(OpCodes.Callvirt, setItem);
        il.Append(end); il.Emit(OpCodes.Ret);
    }

    static void BypassStateDependentTranslations()
    {
        var contains = Ref(module.TypeSystem.String, "Contains", module.TypeSystem.Boolean, true, module.TypeSystem.String);
        foreach (var name in new[] { "TryGetCachedTranslation", "CacheTranslation" })
        {
            var method = Method(mapper, name);
            if (method.Body.Instructions.Take(12).Any(i => i.OpCode == OpCodes.Ldstr && (string)i.Operand == "GetFlag")) continue;
            var il = method.Body.GetILProcessor(); var first = method.Body.Instructions.First();
            var prefix = new List<Instruction> {
                Instruction.Create(OpCodes.Ldarg_0), Instruction.Create(OpCodes.Brfalse, first),
                Instruction.Create(OpCodes.Ldarg_0), Instruction.Create(OpCodes.Ldstr, "GetFlag"),
                Instruction.Create(OpCodes.Callvirt, contains), Instruction.Create(OpCodes.Brfalse, first)
            };
            if (name == "TryGetCachedTranslation") prefix.AddRange(new[] {
                Instruction.Create(OpCodes.Ldarg_1), Instruction.Create(OpCodes.Ldnull), Instruction.Create(OpCodes.Stind_Ref), Instruction.Create(OpCodes.Ldc_I4_0)
            });
            prefix.Add(Instruction.Create(OpCodes.Ret));
            foreach (var instruction in prefix) il.InsertBefore(first, instruction);
        }
    }

    static void CopyBody(MethodDefinition from, MethodDefinition to)
    {
        var il = Body(to); to.Body.MaxStackSize = from.Body.MaxStackSize;
        var locals = new Dictionary<VariableDefinition, VariableDefinition>();
        foreach (var item in from.Body.Variables) { var copy = new VariableDefinition(item.VariableType); locals[item] = copy; to.Body.Variables.Add(copy); }
        var instructions = from.Body.Instructions.ToDictionary(i => i, i => Instruction.Create(OpCodes.Nop));
        foreach (var item in from.Body.Instructions)
        {
            var copy = instructions[item]; copy.OpCode = item.OpCode; object operand = item.Operand;
            if (operand is Instruction branch) operand = instructions[branch];
            else if (operand is Instruction[] branches) operand = branches.Select(b => instructions[b]).ToArray();
            else if (operand is VariableDefinition variable) operand = locals[variable];
            else if (operand is ParameterDefinition parameter) operand = to.Parameters[parameter.Index];
            copy.Operand = operand; il.Append(copy);
        }
        Instruction Map(Instruction i) => i == null ? null : instructions[i];
        foreach (var handler in from.Body.ExceptionHandlers) to.Body.ExceptionHandlers.Add(new ExceptionHandler(handler.HandlerType) {
            TryStart = Map(handler.TryStart), TryEnd = Map(handler.TryEnd), HandlerStart = Map(handler.HandlerStart), HandlerEnd = Map(handler.HandlerEnd),
            FilterStart = Map(handler.FilterStart), CatchType = handler.CatchType
        });
    }

    static void MakeLiveRebuild()
    {
        var original = Method(patch, "ReprocessLastLuaFunction");
        // A display refresh must preserve pending event execution. Actual Lua
        // loading still resets TargetEvent through its existing prefix.
        foreach (var instruction in original.Body.Instructions.Where(i => i.OpCode == OpCodes.Stsfld && i.Operand == Field(patch, "TargetEvent")))
        { instruction.OpCode = OpCodes.Pop; instruction.Operand = null; }
        rebuild = NewMethod(patch, "RebuildLiveLogEntries", module.TypeSystem.Void, false);
        CopyBody(original, rebuild);
        foreach (var instruction in rebuild.Body.Instructions)
        {
            if (instruction.Operand is MethodReference called &&
                ((called.DeclaringType.FullName == "DiceMaster.LuaParse.LuaParser" && called.Name == "ClearCaches") ||
                 (called.DeclaringType.FullName == "DiceMaster.DicePlugin" && called.Name == "LogInfo")))
            { instruction.OpCode = OpCodes.Pop; instruction.Operand = null; }
        }
    }

    static void WrapRefresh(MethodDefinition method, Action<ILProcessor> emitWork, bool timed)
    {
        var il = Body(method);
        var done = Instruction.Create(OpCodes.Ret);
        if (timed)
        {
            var enabled = Field(module.GetType("DiceMaster.Config.ConfigManager"), "EnableOptionInfo");
            var value = Call(Method(patch, "PositionController_OnPositionEnter_Prefix"), "DiceMaster.Config.ConfigElement`1<System.Boolean>", "get_Value");
            var timeType = module.GetTypeReferences().First(t => t.FullName == "UnityEngine.Time");
            var time = Ref(timeType, "get_unscaledTime", module.TypeSystem.Single, false);
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, Field(plugin, "showLogWindow")); il.Emit(OpCodes.Brfalse, done);
            il.Emit(OpCodes.Ldsfld, enabled); il.Emit(OpCodes.Brfalse, done);
            il.Emit(OpCodes.Ldsfld, enabled); il.Emit(OpCodes.Callvirt, value); il.Emit(OpCodes.Brfalse, done);
            il.Emit(OpCodes.Ldsfld, depth); il.Emit(OpCodes.Brtrue, done);
            il.Emit(OpCodes.Call, time); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, nextRefresh); il.Emit(OpCodes.Blt, done);
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, time); il.Emit(OpCodes.Ldc_R4, 0.25f); il.Emit(OpCodes.Add); il.Emit(OpCodes.Stfld, nextRefresh);
        }
        else
        {
            il.Emit(OpCodes.Ldsfld, depth); il.Emit(OpCodes.Brtrue, done);
        }
        // Evaluate into new lists. On failure keep all previous rows, rather
        // than exposing a partially cleared or partially rebuilt log window.
        var backups = new List<(FieldDefinition field, VariableDefinition old)>();
        foreach (var name in new[] { "optionEntries", "switchEntries", "eventEntries", "missionEntries" })
        {
            var field = Field(plugin, name); var old = new VariableDefinition(field.FieldType); method.Body.Variables.Add(old); backups.Add((field, old));
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, field); il.Emit(OpCodes.Stloc, old);
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Newobj, Ref(field.FieldType, ".ctor", module.TypeSystem.Void, true)); il.Emit(OpCodes.Stfld, field);
        }
        Enter(il);
        var start = Instruction.Create(OpCodes.Nop); il.Append(start); emitWork(il); il.Emit(OpCodes.Leave, done);
        var catchStart = Instruction.Create(OpCodes.Nop); il.Append(catchStart);
        var error = new VariableDefinition(ExceptionType()); method.Body.Variables.Add(error); il.Emit(OpCodes.Stloc, error);
        foreach (var backup in backups) { il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldloc, backup.old); il.Emit(OpCodes.Stfld, backup.field); }
        if (timed)
        {
            var timeType = module.GetTypeReferences().First(t => t.FullName == "UnityEngine.Time");
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, Ref(timeType, "get_unscaledTime", module.TypeSystem.Single, false));
            il.Emit(OpCodes.Ldc_R4, 5f); il.Emit(OpCodes.Add); il.Emit(OpCodes.Stfld, nextRefresh);
        }
        il.Emit(OpCodes.Ldloc, error); il.Emit(OpCodes.Call, Method(plugin, "LogError")); il.Emit(OpCodes.Leave, done);
        var finallyStart = Instruction.Create(OpCodes.Nop); il.Append(finallyStart); Exit(il); il.Emit(OpCodes.Endfinally); il.Append(done);
        method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch) {
            TryStart = start, TryEnd = catchStart, HandlerStart = catchStart, HandlerEnd = finallyStart, CatchType = ExceptionType()
        });
        method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally) {
            TryStart = start, TryEnd = finallyStart, HandlerStart = finallyStart, HandlerEnd = done
        });
    }

    static void AddRefreshPaths()
    {
        var auto = NewMethod(plugin, "RefreshLiveLogDisplay", module.TypeSystem.Void, true);
        WrapRefresh(auto, il => il.Emit(OpCodes.Call, rebuild), true);
        var manual = Method(plugin, "RefreshGameData");
        WrapRefresh(manual, il => {
            il.Emit(OpCodes.Call, Method(cache, "get_Instance")); il.Emit(OpCodes.Callvirt, Method(cache, "ClearDisplayCaches"));
            il.Emit(OpCodes.Call, Method(patch, "ReprocessLastLuaFunction"));
        }, false);
        var update = Method(plugin, "Update");
        if (!update.Body.Instructions.Any(i => i.Operand == auto))
        {
            var il = update.Body.GetILProcessor();
            // Run after visibility changes, before the existing gamepad tick.
            var target = update.Body.Instructions.FirstOrDefault(i => i.Operand is MethodReference r && r.Name == "UpdateGamepad")
                ?? update.Body.Instructions.Last(i => i.OpCode == OpCodes.Ret);
            var call = Instruction.Create(OpCodes.Ldarg_0);
            // Existing branches targeting the extension point must refresh too.
            foreach (var i in update.Body.Instructions)
            {
                if (i.Operand == target) i.Operand = call;
                else if (i.Operand is Instruction[] branches)
                    for (int index = 0; index < branches.Length; index++) if (branches[index] == target) branches[index] = call;
            }
            il.InsertBefore(target, call); il.InsertBefore(target, Instruction.Create(OpCodes.Call, auto));
        }
    }

    static int Main(string[] args)
    {
        if (args.Length != 2) { Console.Error.WriteLine("Usage: LogRefreshInjector input.dll output.dll"); return 2; }
        using var assembly = AssemblyDefinition.ReadAssembly(args[0]); module = assembly.MainModule;
        if (assembly.Name.Name != "DiceMaster") throw new InvalidDataException("Expected DiceMaster assembly.");
        plugin = module.GetType("DiceMaster.DicePlugin"); patch = module.GetType("DiceMaster.OptionInfoPatch");
        cache = module.GetType("DiceMaster.PositionEventCache"); mapper = module.GetType("DiceMaster.UiTextMapper");
        depth = NewField(patch, "DisplayRefreshDepth", module.TypeSystem.Int32, true);
        nextRefresh = NewField(plugin, "nextLiveLogRefresh", module.TypeSystem.Single, false);
        PatchEventDescriptions(); RetryEmptyEventLists(); BypassStateDependentTranslations(); MakeLiveRebuild();
        foreach (var name in new[] { "LuaEnvironment_LoadLuaFunction_Prefix", "PrefixExecuteConditionResult", "PrefixExecuteSwitchResult", "PrefixCheckMission", "PositionEventData_GetRate_Postfix", "MissionCheckData_Postfix" })
            GuardPreview(Method(patch, name));
        AddRefreshPaths();
        module.Mvid = Guid.NewGuid();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!); assembly.Write(args[1]);
        Console.WriteLine("Updated live log display; gamepad code and embedded resources retained."); return 0;
    }
}
