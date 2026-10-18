using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Keep the existing mod and its resources; change only log-display evaluation.
static partial class LogRefreshInjector
{
    static ModuleDefinition module;
    static TypeDefinition plugin, patch, cache, mapper;
    static FieldDefinition depth;
    static MethodDefinition previewDynamic, rebuild;
    static readonly HashSet<MethodDefinition> changed = new();
    static readonly HashSet<string> previewBodies = new(StringComparer.Ordinal) {
        "DiceMaster.LuaParse.Action::.ctor", "DiceMaster.LuaParse.Action::get_Description", "DiceMaster.LuaParse.Action::set_Description",
        "DiceMaster.LuaParse.LuaParser::.ctor", "DiceMaster.LuaParse.LuaParser::.cctor",
        "DiceMaster.LuaParse.LuaParser::ClearCaches", "DiceMaster.LuaParse.LuaParser::ExtractOptionActionsFromLua",
        "DiceMaster.LuaParse.LuaParser::ExtractOptionActionsFromChooseBlocks", "DiceMaster.LuaParse.LuaParser::BuildActionListFromBlock",
        "DiceMaster.LuaParse.LuaParser::ExtractSwitchBranchFirstStoryTextsFromLua", "DiceMaster.LuaParse.LuaParser::ExtractFirstStoryTextKey",
        "DiceMaster.LuaParse.LuaParser::AddActionsToList", "DiceMaster.LuaParse.LuaParser::ExtractSwitchCheckFromLua",
        "DiceMaster.LuaParse.LuaParser::ExtractPositionWorkFromLua", "DiceMaster.LuaParse.LuaParser::ParseFirstLuaArgument",
        "DiceMaster.LuaParse.LuaParser::ExtractActionsFromLua", "DiceMaster.LuaParse.LuaParser::Description"
    };
    static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> roots)
    {
        foreach (var type in roots)
        {
            yield return type;
            foreach (var nested in AllTypes(type.NestedTypes)) yield return nested;
        }
    }
    static MethodDefinition Method(TypeDefinition type, string name) => type.Methods.Single(m => m.Name == name);
    static FieldDefinition Field(TypeDefinition type, string name) => type.Fields.Single(f => f.Name == name);
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
        changed.Add(method);
        method.Body = new MethodBody(method) { InitLocals = true, MaxStackSize = 16 };
        return method.Body.GetILProcessor();
    }
    static void GuardPreview(MethodDefinition method)
    {
        if (method.Body.Instructions.First().Operand is FieldReference guard && guard.FullName == depth.FullName) return;
        changed.Add(method);
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
            changed.Add(method);
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

    static void InstallBridge(ModuleDefinition payload)
    {
        foreach (var type in module.Types.Where(t => t.Namespace == "DiceMaster.LiveLog").ToArray()) module.Types.Remove(type);
        new PayloadCopier(payload, module, t => t.Namespace == "DiceMaster.LiveLog");
        if (payload.GetType("DiceMaster.LuaParse.LuaParser") == null)
            throw new InvalidDataException("PRD 2 requires the full source-built payload including LuaParse.");
        // Merge the parser metadata so older callers keep the same field and
        // method definitions. Only this named namespace and two display/read
        // boundaries are installed from the full source candidate.
        var existingParserMethods = new HashSet<string>(AllTypes(module.Types).SelectMany(t => t.Methods).Select(m => m.FullName), StringComparer.Ordinal);
        var preview = new PayloadCopier(payload, module, t => t.Namespace == "DiceMaster.LuaParse", merge: true,
            replaceBody: m => !existingParserMethods.Contains(m.FullName) || previewBodies.Contains(m.DeclaringType.FullName + "::" + m.Name));
        changed.Add(preview.CopyMethod(Method(payload.GetType("DiceMaster.OptionInfoPatch"), "LeanLocalizationResolver_GetStoryText_Postfix"), patch));
        changed.Add(preview.CopyMethod(Method(payload.GetType("DiceMaster.DataHelper"), "ReadPreviewScript"), module.GetType("DiceMaster.DataHelper")));
        changed.Add(preview.CopyMethod(Method(payload.GetType("DiceMaster.PositionEventCache"), "GenerateStaticDesc"), cache));
        changed.Add(preview.CopyMethod(Method(payload.GetType("DiceMaster.PositionEventCache"), "GetPositionEventDesc"), cache));
        changed.Add(preview.CopyMethod(Method(payload.GetType("DiceMaster.PositionEventCache"), "RetryDefinitionReads"), cache));
        preview.RebindReferences();
        // Existing strict-reflection hooks must bind to the replacement payload
        // definitions when applying the patch to an already patched assembly.
        foreach (var type in module.Types.Where(t => t.Namespace != "DiceMaster.LiveLog"))
            foreach (var method in type.Methods.Where(m => m.HasBody))
                foreach (var instruction in method.Body.Instructions)
                    if (instruction.Operand is MethodReference reference && reference.DeclaringType.Namespace == "DiceMaster.LiveLog")
                        instruction.Operand = module.GetType(reference.DeclaringType.FullName).Methods.Single(m => m.FullName == reference.FullName);
        var bridge = module.GetType("DiceMaster.LiveLog.LogBridge");
        var tick = Method(bridge, "Tick");
        // Event scripts discovered after initial data readiness belong to a
        // separate parser. Display preparation must not write the shared Lua
        // parser's modification/options caches, even on a cold source.
        var parserType = module.GetType("DiceMaster.LuaParse.LuaParser");
        var eventParser = NewMethod(cache, "GetDefinitionParser", parserType, false);
        var parserBody = Body(eventParser);
        parserBody.Emit(OpCodes.Call, Method(bridge, "GetEventParser"));
        parserBody.Emit(OpCodes.Castclass, parserType); parserBody.Emit(OpCodes.Ret);
        var staticDescription = Method(cache, "GenerateStaticDesc");
        foreach (var instruction in staticDescription.Body.Instructions)
            if (instruction.Operand is MethodReference reference && reference.DeclaringType.FullName == parserType.FullName && reference.Name == "get_Instance")
            {
                instruction.Operand = eventParser;
                changed.Add(staticDescription);
            }
        void TickBody(MethodDefinition method, bool instance, bool manual)
        {
            var il = Body(method);
            if (instance) il.Emit(OpCodes.Ldarg_0); else il.Emit(OpCodes.Call, Method(plugin, "get_Instance"));
            il.Emit(manual ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, tick); il.Emit(OpCodes.Ret);
        }
        rebuild = NewMethod(patch, "RebuildLiveLogEntries", module.TypeSystem.Void, false);
        TickBody(rebuild, false, false);
        TickBody(Method(patch, "ReprocessLastLuaFunction"), false, true);
        var auto = NewMethod(plugin, "RefreshLiveLogDisplay", module.TypeSystem.Void, true);
        TickBody(auto, true, false);
        TickBody(Method(plugin, "RefreshGameData"), true, true);
        var prefix = Method(patch, "LuaEnvironment_LoadLuaFunction_Prefix");
        var body = Body(prefix);
        body.Emit(OpCodes.Ldarg_0); body.Emit(OpCodes.Ldarg_1);
        body.Emit(OpCodes.Call, Method(bridge, "NewLua")); body.Emit(OpCodes.Ret);

        var update = Method(plugin, "Update");
        if (!update.Body.Instructions.Any(i => i.Operand == auto))
        {
            changed.Add(update);
            var il = update.Body.GetILProcessor();
            var target = update.Body.Instructions.FirstOrDefault(i => i.Operand is MethodReference r && r.Name == "UpdateGamepad")
                ?? update.Body.Instructions.Last(i => i.OpCode == OpCodes.Ret);
            var call = Instruction.Create(OpCodes.Ldarg_0);
            foreach (var i in update.Body.Instructions)
            {
                if (i.Operand == target) i.Operand = call;
                else if (i.Operand is Instruction[] branches)
                    for (int index = 0; index < branches.Length; index++) if (branches[index] == target) branches[index] = call;
            }
            il.InsertBefore(target, call); il.InsertBefore(target, Instruction.Create(OpCodes.Call, auto));
        }
        // OnGUI draws the newly selected tab in the same frame as the toggle.
        var draw = Method(plugin, "DrawLogWindow");
        if (!draw.Body.Instructions.Any(i => i.Operand == auto))
        {
            changed.Add(draw);
            var end = draw.Body.Instructions.First(i => i.Operand is MethodReference r && r.Name == "EndHorizontal");
            var il = draw.Body.GetILProcessor();
            var load = Instruction.Create(OpCodes.Ldarg_0);
            il.InsertAfter(end, load); il.InsertAfter(load, Instruction.Create(OpCodes.Call, auto));
        }
        PatchMissionIsolation();
        foreach (var name in new[] { "DrawEventEntries", "DrawMissionEntries" })
        {
            var method = Method(plugin, name);
            var button = method.Body.Instructions.First(i => i.OpCode == OpCodes.Ldstr &&
                ((string)i.Operand == "이벤트 실행" || (string)i.Operand == "미션 실행"));
            if (button.Previous.OpCode == OpCodes.Brfalse || button.Previous.OpCode == OpCodes.Brfalse_S) continue;
            changed.Add(method);
            var skip = method.Body.Instructions.SkipWhile(i => i != button).First(i =>
                i.OpCode.Code == Code.Brfalse || i.OpCode.Code == Code.Brfalse_S).Operand as Instruction;
            var item = method.Body.Variables.First(v => v.VariableType.FullName ==
                (name == "DrawEventEntries" ? "Mortal.Story.PositionEventData" : "Mortal.Core.MissionCheckData"));
            var il = method.Body.GetILProcessor();
            il.InsertBefore(button, Instruction.Create(OpCodes.Ldloc, item));
            il.InsertBefore(button, Instruction.Create(OpCodes.Brfalse, skip));
        }
        // Old helpers log and return empty/default values after errors. During a
        // preview these failures must reach the individual row's retry boundary.
        foreach (var typeName in new[] { "DiceMaster.DataHelper", "DiceMaster.OptionInfoPatch", "DiceMaster.UiTextMapper" })
            foreach (var method in module.GetType(typeName).Methods.Where(m => m.HasBody))
                foreach (var handler in method.Body.ExceptionHandlers.Where(h => h.HandlerType == ExceptionHandlerType.Catch))
                {
                    var first = handler.HandlerStart;
                    if (first.OpCode.Code != Code.Stloc && first.OpCode.Code != Code.Stloc_S &&
                        first.OpCode.Code != Code.Stloc_0 && first.OpCode.Code != Code.Stloc_1 &&
                        first.OpCode.Code != Code.Stloc_2 && first.OpCode.Code != Code.Stloc_3 && first.OpCode.Code != Code.Pop)
                        throw new InvalidDataException("Unsupported catch entry: " + method.FullName);
                    if (first.Next.Operand == depth) continue;
                    changed.Add(method);
                    var next = first.Next; var il = method.Body.GetILProcessor();
                    il.InsertBefore(next, Instruction.Create(OpCodes.Ldsfld, depth));
                    il.InsertBefore(next, Instruction.Create(OpCodes.Brfalse, next));
                    il.InsertBefore(next, Instruction.Create(OpCodes.Rethrow));
                }
        var reflection = module.GetType("DiceMaster.ReflectionExtensions").Methods.Single(m => m.Name == "GetFieldValue");
        if (!reflection.Body.Instructions.Any(i => i.Operand is MethodReference r && r.Name == "ReadFieldStrict"))
        {
            changed.Add(reflection);
            var first = reflection.Body.Instructions[0]; var il = reflection.Body.GetILProcessor();
            foreach (var instruction in new[] {
                Instruction.Create(OpCodes.Ldsfld, depth), Instruction.Create(OpCodes.Brfalse, first),
                Instruction.Create(OpCodes.Ldarg_0), Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Call, Method(bridge, "ReadFieldStrict")),
                Instruction.Create(OpCodes.Unbox_Any, reflection.GenericParameters[0]), Instruction.Create(OpCodes.Ret)
            }) il.InsertBefore(first, instruction);
        }
    }

    static void PatchMissionIsolation()
    {
        var helper = module.GetType("DiceMaster.DataHelper");
        var filter = NewField(helper, "LiveMissionFilter", module.TypeSystem.Object, true);
        var method = Method(helper, "GetMissions");
        if (method.Body.Instructions.Any(i => i.Operand == filter)) return;
        changed.Add(method);
        var current = method.Body.Instructions.First(i => i.Operand is MethodReference r && r.Name == "get_Current" && r.ReturnType.FullName == "!0");
        // Resolve the foreach local independently of compiler-selected local slot.
        var store = current.Next;
        int index = store.OpCode.Code switch { Code.Stloc_0 => 0, Code.Stloc_1 => 1, Code.Stloc_2 => 2, Code.Stloc_3 => 3,
            Code.Stloc or Code.Stloc_S => ((VariableDefinition)store.Operand).Index,
            _ => throw new InvalidDataException("Unsupported mission loop") };
        var variable = method.Body.Variables[index];
        if (variable.VariableType.FullName != "Mortal.Core.MissionData") throw new InvalidDataException("Unexpected mission loop type");
        var original = store.Next;
        var skip = method.Body.Instructions.SkipWhile(i => i != original).First(i => i.OpCode.FlowControl == FlowControl.Cond_Branch).Operand as Instruction;
        if (skip == null) throw new InvalidDataException("Missing mission null skip");
        var il = method.Body.GetILProcessor();
        foreach (var instruction in new[] {
            Instruction.Create(OpCodes.Ldsfld, filter), Instruction.Create(OpCodes.Brfalse, original),
            Instruction.Create(OpCodes.Ldloc, variable), Instruction.Create(OpCodes.Ldsfld, filter),
            Instruction.Create(OpCodes.Ceq), Instruction.Create(OpCodes.Brfalse, skip)
        }) il.InsertBefore(original, instruction);
    }

    static int Main(string[] args)
    {
        if (args.Length != 3) { Console.Error.WriteLine("Usage: LogRefreshInjector input.dll payload.dll output.dll"); return 2; }
        using var assembly = AssemblyDefinition.ReadAssembly(args[0]); module = assembly.MainModule;
        if (assembly.Name.Name != "DiceMaster") throw new InvalidDataException("Expected DiceMaster assembly.");
        // Cecil loads bodies lazily. Resolve references to the old payload
        // before removing its definitions, then rebind them in InstallBridge.
        foreach (var type in AllTypes(module.Types))
            foreach (var method in type.Methods.Where(m => m.HasBody))
                _ = method.Body.Instructions.Count;
        plugin = module.GetType("DiceMaster.DicePlugin"); patch = module.GetType("DiceMaster.OptionInfoPatch");
        cache = module.GetType("DiceMaster.PositionEventCache"); mapper = module.GetType("DiceMaster.UiTextMapper");
        depth = NewField(patch, "DisplayRefreshDepth", module.TypeSystem.Int32, true);
        PatchEventDescriptions(); RetryEmptyEventLists(); BypassStateDependentTranslations();
        using var payload = ModuleDefinition.ReadModule(args[1]);
        InstallBridge(payload);
        InstallSafeEvaluationHooks();
        InstallLifecycleHooks(module.GetType("DiceMaster.LiveLog.LogBridge"));
        foreach (var name in new[] { "LuaEnvironment_LoadLuaFunction_Prefix", "PrefixExecuteConditionResult", "PrefixExecuteSwitchResult", "PrefixCheckMission", "PositionEventData_GetRate_Postfix", "MissionCheckData_Postfix" })
            GuardPreview(Method(patch, name));
        // Cecil preserves instruction encodings. Inserted guards can make an
        // original short branch exceed its signed-byte range.
        var longBranches = new Dictionary<Code, OpCode> { [Code.Br_S] = OpCodes.Br, [Code.Brfalse_S] = OpCodes.Brfalse,
            [Code.Brtrue_S] = OpCodes.Brtrue, [Code.Beq_S] = OpCodes.Beq, [Code.Bge_S] = OpCodes.Bge,
            [Code.Bgt_S] = OpCodes.Bgt, [Code.Ble_S] = OpCodes.Ble, [Code.Blt_S] = OpCodes.Blt,
            [Code.Bne_Un_S] = OpCodes.Bne_Un, [Code.Bge_Un_S] = OpCodes.Bge_Un, [Code.Bgt_Un_S] = OpCodes.Bgt_Un,
            [Code.Ble_Un_S] = OpCodes.Ble_Un, [Code.Blt_Un_S] = OpCodes.Blt_Un, [Code.Leave_S] = OpCodes.Leave };
        foreach (var method in changed)
            foreach (var instruction in method.Body.Instructions)
                if (longBranches.TryGetValue(instruction.OpCode.Code, out var opcode)) instruction.OpCode = opcode;
        foreach (var type in AllTypes(module.Types))
            foreach (var method in type.Methods.Where(m => m.HasBody))
                if (method.Body.Instructions.Any(i => i.OpCode.FlowControl == FlowControl.Call && i.Operand == null))
                    throw new InvalidDataException("Unresolved call after payload replacement: " + method.FullName);
        module.Mvid = Guid.NewGuid();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!); assembly.Write(args[2]);
        Console.WriteLine("Updated live log display; gamepad code and embedded resources retained."); return 0;
    }
}
