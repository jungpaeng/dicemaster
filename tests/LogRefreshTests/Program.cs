using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

static class LogRefreshTests
{
    static ModuleDefinition target;
    static TypeReference Type(TypeReference type)
    {
        if (type is GenericParameter) return type;
        if (type is ByReferenceType byref) return new ByReferenceType(Type(byref.ElementType));
        if (type is ArrayType array) return new ArrayType(Type(array.ElementType), array.Rank);
        if (type is GenericInstanceType generic)
        {
            var copy = new GenericInstanceType(Type(generic.ElementType));
            foreach (var arg in generic.GenericArguments) copy.GenericArguments.Add(Type(arg));
            return copy;
        }
        return target.GetType(type.FullName) ?? target.ImportReference(type);
    }
    static MethodReference Method(MethodReference method)
    {
        if (method is GenericInstanceMethod generic)
        {
            var copy = new GenericInstanceMethod(Method(generic.ElementMethod));
            foreach (var arg in generic.GenericArguments) copy.GenericArguments.Add(Type(arg));
            return copy;
        }
        var owner = Type(method.DeclaringType);
        var copyMethod = new MethodReference(method.Name, Type(method.ReturnType), owner) {
            HasThis = method.HasThis, ExplicitThis = method.ExplicitThis, CallingConvention = method.CallingConvention
        };
        foreach (var arg in method.Parameters) copyMethod.Parameters.Add(new ParameterDefinition(Type(arg.ParameterType)));
        return copyMethod;
    }
    static void Install(MethodDefinition source, MethodDefinition destination)
    {
        destination.Body = new Mono.Cecil.Cil.MethodBody(destination) {
            InitLocals = source.Body.InitLocals, MaxStackSize = source.Body.MaxStackSize
        };
        var variables = new Dictionary<VariableDefinition, VariableDefinition>();
        foreach (var variable in source.Body.Variables)
        {
            var copy = new VariableDefinition(Type(variable.VariableType)); variables[variable] = copy; destination.Body.Variables.Add(copy);
        }
        var instructions = source.Body.Instructions.ToDictionary(i => i, i => Instruction.Create(OpCodes.Nop));
        foreach (var item in source.Body.Instructions)
        {
            var copy = instructions[item]; copy.OpCode = item.OpCode; object operand = item.Operand;
            if (operand is Instruction branch) operand = instructions[branch];
            else if (operand is Instruction[] branches) operand = branches.Select(i => instructions[i]).ToArray();
            else if (operand is VariableDefinition variable) operand = variables[variable];
            else if (operand is ParameterDefinition parameter) operand = destination.Parameters[parameter.Index];
            else if (operand is TypeReference type) operand = Type(type);
            else if (operand is MethodReference method) operand = Method(method);
            else if (operand is FieldReference field) operand = new FieldReference(field.Name, Type(field.FieldType), Type(field.DeclaringType));
            copy.Operand = operand; destination.Body.Instructions.Add(copy);
        }
        Instruction Map(Instruction i) => i == null ? null : instructions[i];
        foreach (var handler in source.Body.ExceptionHandlers) destination.Body.ExceptionHandlers.Add(new ExceptionHandler(handler.HandlerType) {
            TryStart = Map(handler.TryStart), TryEnd = Map(handler.TryEnd), HandlerStart = Map(handler.HandlerStart), HandlerEnd = Map(handler.HandlerEnd),
            FilterStart = Map(handler.FilterStart), CatchType = handler.CatchType == null ? null : Type(handler.CatchType)
        });
    }
    static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static MethodDefinition Find(ModuleDefinition module, string owner, string name) => module.GetType(owner).Methods.Single(m => m.Name == name);
    static bool Calls(MethodDefinition method, string owner, string name) => method.Body.Instructions.Any(i => i.Operand is MethodReference called && called.DeclaringType.FullName == owner && called.Name == name);
    static bool HasPreviewGuard(MethodDefinition method)
    {
        var code = method.Body.Instructions.Where(instruction => instruction.OpCode != OpCodes.Nop).ToArray();
        bool Depth(Instruction instruction) => instruction.OpCode == OpCodes.Ldsfld && instruction.Operand is FieldReference field &&
            field.DeclaringType.FullName == "DiceMaster.OptionInfoPatch" && field.Name == "DisplayRefreshDepth";
        // An empty old Lua prefix has no gameplay side effects. Roslyn emits a
        // read/pop for its empty if block; the injected assembly uses two rets.
        if (method.Name == "LuaEnvironment_LoadLuaFunction_Prefix" &&
            (code.Length == 1 && code[0].OpCode == OpCodes.Ret ||
             code.Length == 3 && Depth(code[0]) && code[1].OpCode == OpCodes.Pop && code[2].OpCode == OpCodes.Ret)) return true;
        if (code.Length < 4 || !Depth(code[0]) || code.Count(Depth) != 1) return false;
        // Both encodings return immediately for every nonzero depth. Check the
        // exact branch destination as well as its polarity, including short IL.
        if (code[1].OpCode == OpCodes.Brfalse || code[1].OpCode == OpCodes.Brfalse_S)
            return code[2].OpCode == OpCodes.Ret && ReferenceEquals(code[1].Operand, code[3]);
        if (code[1].OpCode == OpCodes.Brtrue || code[1].OpCode == OpCodes.Brtrue_S)
            return code[1].Operand is Instruction target && target.OpCode == OpCodes.Ret &&
                ReferenceEquals(target, code[^1]);
        return false;
    }
    static int Main(string[] args)
    {
        if (args.Length != 2) throw new ArgumentException("Usage: LogRefreshTests DiceMaster.dll Fixtures.dll");
        using var source = AssemblyDefinition.ReadAssembly(args[0]); var module = source.MainModule;
        foreach (var entry in new[] { ("DiceMaster.DicePlugin", "RefreshLiveLogDisplay"),
            ("DiceMaster.DicePlugin", "RefreshGameData"), ("DiceMaster.OptionInfoPatch", "RebuildLiveLogEntries") })
        {
            var method = Find(module, entry.Item1, entry.Item2);
            Check(Calls(method, "DiceMaster.LiveLog.LogBridge", "Tick"), "Refresh must use the per-item bridge");
            Check(!Calls(method, "DiceMaster.LuaParse.LuaParser", "ClearCaches"), "Display wrapper mutates parser caches");
        }
        Check(Calls(Find(module, "DiceMaster.OptionInfoPatch", "LiveLog_LuaStage_Prefix"),
            "DiceMaster.LiveLog.LogBridge", "StageLua"), "Lua staging boundary missing");
        Check(Calls(Find(module, "DiceMaster.OptionInfoPatch", "LiveLog_LuaComplete_Postfix"),
            "DiceMaster.LiveLog.LogBridge", "CompleteLua"), "Successful Lua boundary missing");
        Check(module.GetType("DiceMaster.OptionInfoPatch").Fields.All(f => f.Name != "TargetEvent") &&
            module.GetType("DiceMaster.OptionInfoPatch").Methods.All(m => m.Name != "PositionEventData_GetRate_Postfix"),
            "Event weight reservation and its 99999 hook must be removed");
        foreach (var name in new[] { "LuaEnvironment_LoadLuaFunction_Prefix", "PrefixExecuteConditionResult", "PrefixExecuteSwitchResult", "PrefixCheckMission", "MissionCheckData_Postfix" })
        {
            Check(HasPreviewGuard(Find(module, "DiceMaster.OptionInfoPatch", name)), "Missing preview guard: " + name);
        }
        var update = Find(module, "DiceMaster.DicePlugin", "Update");
        Check(update.Body.Instructions.Count(i => i.Operand is MethodReference r && r.Name == "RefreshLiveLogDisplay") == 1, "Live hook missing or duplicated");
        Check(update.Body.Instructions.Count(i => i.Operand is MethodReference r && r.Name == "UpdateGamepad") == 1, "Gamepad hook missing or duplicated");
        Check(module.Resources.Count == 8, "Embedded resources changed");

        string output = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1]))!, "LogRefreshFixtures.patched.dll");
        using (var fixtures = AssemblyDefinition.ReadAssembly(args[1]))
        {
            target = fixtures.MainModule;
            new PayloadCopier(module, target, type => (type.Namespace == "DiceMaster.LiveLog" ||
                type.Namespace == "DiceMaster.EventSelection" && (type.Name.StartsWith("EventEligibility") || type.Name.StartsWith("EventEligibilityReason") ||
                    type.Name == "CheckpointReservation" || type.Name == "CheckpointReservationStore" || type.Name == "PopupConditionText") ||
                new[] { "LogReadState", "LogRefreshState", "LogConditionBranch", "LogConditionDetails", "LogItem", "LogSnapshot", "ReservationTicket", "LogTabState", "LogModel" }.Contains(type.Name) && type.Namespace == "DiceMaster.LogUI"));
            var groups = new Dictionary<string, string[]> {
                ["DiceMaster.PositionEventCache"] = new[] { "GetPositionEventDesc", "GeneratePreviewDynamicDesc", "ClearDisplayCaches", "GetPositionEvents" },
                ["DiceMaster.OptionInfoPatch"] = new[] { "MissionCheckData_Postfix" },
                ["DiceMaster.DicePlugin"] = new[] { "ClearLogEntries" },
                ["DiceMaster.UiTextMapper"] = new[] { "TryGetCachedTranslation", "CacheTranslation", "ApplyTranslations", "ConvertGetFlagExpression" }
            };
            foreach (var group in groups)
                foreach (var name in group.Value) Install(Find(module, group.Key, name), Find(target, group.Key, name));
            fixtures.Write(output);
        }
        var assembly = System.Reflection.Assembly.LoadFile(output);
        try
        {
            int assertions = (int)assembly.GetType("DiceMaster.Scenario")!.GetMethod("Run")!.Invoke(null, null)!;
            Console.WriteLine("PASS: " + assertions + " assertions against installed DLL method bodies and boundary fixtures.");
        }
        catch (TargetInvocationException exception) { throw exception.InnerException ?? exception; }
        return 0;
    }
}
