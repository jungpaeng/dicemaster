using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

static class LiveLogIntegrationTests
{
    static ModuleDefinition target;
    static MethodDefinition installingFrom, installingTo;

    static TypeReference Type(TypeReference value)
    {
        if (value == null) return null;
        if (value is GenericParameter parameter)
            return parameter.Type == GenericParameterType.Method && parameter.Owner == installingFrom
                ? installingTo.GenericParameters[parameter.Position] : value;
        if (value is ByReferenceType byref) return new ByReferenceType(Type(byref.ElementType));
        if (value is ArrayType array) return new ArrayType(Type(array.ElementType), array.Rank);
        if (value is GenericInstanceType generic)
        {
            var result = new GenericInstanceType(Type(generic.ElementType));
            foreach (var argument in generic.GenericArguments) result.GenericArguments.Add(Type(argument));
            return result;
        }
        return target.GetType(value.FullName) ?? target.ImportReference(value);
    }

    static MethodReference Method(MethodReference value)
    {
        if (value is GenericInstanceMethod generic)
        {
            var result = new GenericInstanceMethod(Method(generic.ElementMethod));
            foreach (var argument in generic.GenericArguments) result.GenericArguments.Add(Type(argument));
            return result;
        }
        var copy = new MethodReference(value.Name, Type(value.ReturnType), Type(value.DeclaringType)) {
            HasThis = value.HasThis, ExplicitThis = value.ExplicitThis, CallingConvention = value.CallingConvention
        };
        foreach (var parameter in value.Parameters) copy.Parameters.Add(new ParameterDefinition(Type(parameter.ParameterType)));
        foreach (var parameter in value.GenericParameters) copy.GenericParameters.Add(new GenericParameter(parameter.Name, copy));
        return copy;
    }

    // This installs the actual game-choice summary body; the surrounding
    // localization and Unity APIs remain explicit boundary fixtures.
    static void Install(MethodDefinition source, MethodDefinition destination)
    {
        installingFrom = source; installingTo = destination;
        destination.Body = new Mono.Cecil.Cil.MethodBody(destination) {
            InitLocals = source.Body.InitLocals, MaxStackSize = source.Body.MaxStackSize
        };
        var variables = new Dictionary<VariableDefinition, VariableDefinition>();
        foreach (var variable in source.Body.Variables)
        {
            var copy = new VariableDefinition(Type(variable.VariableType)); variables.Add(variable, copy); destination.Body.Variables.Add(copy);
        }
        var instructions = source.Body.Instructions.ToDictionary(value => value, value => Instruction.Create(OpCodes.Nop));
        foreach (var instruction in source.Body.Instructions)
        {
            var copy = instructions[instruction]; copy.OpCode = instruction.OpCode; object operand = instruction.Operand;
            if (operand is Instruction branch) operand = instructions[branch];
            else if (operand is Instruction[] branches) operand = branches.Select(branch => instructions[branch]).ToArray();
            else if (operand is VariableDefinition variable) operand = variables[variable];
            else if (operand is ParameterDefinition parameter) operand = destination.Parameters[parameter.Index];
            else if (operand is TypeReference type) operand = Type(type);
            else if (operand is MethodReference method) operand = Method(method);
            else if (operand is FieldReference field) operand = new FieldReference(field.Name, Type(field.FieldType), Type(field.DeclaringType));
            copy.Operand = operand; destination.Body.Instructions.Add(copy);
        }
        Instruction Map(Instruction value) => value == null ? null : instructions[value];
        foreach (var handler in source.Body.ExceptionHandlers)
            destination.Body.ExceptionHandlers.Add(new ExceptionHandler(handler.HandlerType) {
                TryStart = Map(handler.TryStart), TryEnd = Map(handler.TryEnd), HandlerStart = Map(handler.HandlerStart), HandlerEnd = Map(handler.HandlerEnd),
                FilterStart = Map(handler.FilterStart), CatchType = Type(handler.CatchType)
            });
        installingFrom = installingTo = null;
    }

    static int Main(string[] args)
    {
        if (args.Length != 2) throw new ArgumentException("Usage: LiveLogIntegrationTests DiceMaster.dll Fixtures.dll");
        using var source = AssemblyDefinition.ReadAssembly(args[0]);
        using var fixtures = AssemblyDefinition.ReadAssembly(args[1]);
        target = fixtures.MainModule;
        var module = source.MainModule;
        new PayloadCopier(module, target, type => type.Namespace == "DiceMaster.LuaParse" || type.Namespace == "DiceMaster.LiveLog");
        var probe = target.GetType("DiceMaster.Probe").Fields.Single(field => field.Name == "ParserEntries");
        foreach (var method in target.GetType("DiceMaster.LuaParse.LuaParser").Methods.Where(method => method.Name.StartsWith("Extract") && method.HasBody))
        {
            var il = method.Body.GetILProcessor(); var first = method.Body.Instructions[0];
            il.InsertBefore(first, Instruction.Create(OpCodes.Ldsfld, probe));
            il.InsertBefore(first, Instruction.Create(OpCodes.Ldc_I4_1));
            il.InsertBefore(first, Instruction.Create(OpCodes.Add));
            il.InsertBefore(first, Instruction.Create(OpCodes.Stsfld, probe));
            method.Body.MaxStackSize = Math.Max(method.Body.MaxStackSize, 2);
        }
        string summary = "LeanLocalizationResolver_GetStoryText_Postfix";
        Install(module.GetType("DiceMaster.OptionInfoPatch").Methods.Single(method => method.Name == summary),
            target.GetType("DiceMaster.OptionInfoPatch").Methods.Single(method => method.Name == summary));
        var patchSource = module.GetType("DiceMaster.OptionInfoPatch");
        var patchTarget = target.GetType("DiceMaster.OptionInfoPatch");
        foreach (string wrapper in new[] { "PreviewReadGroup", "PreviewCheckComparison" })
            Install(patchSource.Methods.Single(method => method.Name == wrapper),
                patchTarget.Methods.Single(method => method.Name == wrapper));
        // Execute the production prefix/postfix/finalizer bodies directly around
        // explicit fake compiler/load boundaries. Real Harmony application is
        // checked separately; these fixtures do not run a game or native Unity.
        foreach (var hook in patchSource.Methods.Where(method => method.Name.StartsWith("LiveLog_")))
        {
            var installed = new MethodDefinition(hook.Name, hook.Attributes, Type(hook.ReturnType));
            foreach (var parameter in hook.Parameters)
                installed.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, Type(parameter.ParameterType)));
            patchTarget.Methods.Add(installed);
            Install(hook, installed);
        }
        foreach (string helper in new[] { "ColorizeKeyByStoryReadState", "NormalizeCheckpointKey" })
            Install(module.GetType("DiceMaster.DicePlugin").Methods.Single(method => method.Name == helper),
                target.GetType("DiceMaster.DicePlugin").Methods.Single(method => method.Name == helper));
        foreach (string name in new[] { "DiceMaster.LuaParse.LuaParser", "DiceMaster.LuaParse.Action", "DiceMaster.LiveLog.LogBridge", "DiceMaster.LiveLog.RefreshEngine" })
            if (target.GetType(name) == null) throw new InvalidDataException("Required actual DLL type missing: " + name);
        string output = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1]))!, "LiveLogIntegrationFixtures.patched.dll");
        fixtures.Write(output);
        int assertions = Run(output, "Run");
        assertions += Run(output, "RunLifecycle");
        var helperSource = module.GetType("DiceMaster.DataHelper");
        var helperTarget = target.GetType("DiceMaster.DataHelper");
        foreach (var method in helperSource.Methods.Where(method => method.Name == "GetMissions" || method.Name == "ExtractConditions" || method.Name == "IsValidMission" || method.Name == "ParseSwitchCheckpoint"))
        {
            var destination = helperTarget.Methods.Single(candidate => candidate.Name == method.Name &&
                candidate.Parameters.Count == method.Parameters.Count &&
                candidate.Parameters.Select(parameter => parameter.ParameterType.FullName).SequenceEqual(method.Parameters.Select(parameter => parameter.ParameterType.FullName)));
            Install(method, destination);
        }
        Install(module.GetType("DiceMaster.ReflectionExtensions").Methods.Single(method => method.Name == "GetFieldValue"),
            target.GetType("DiceMaster.ReflectionExtensions").Methods.Single(method => method.Name == "GetFieldValue"));
        // Load the helper scenario independently so its stronger game boundary
        // shapes do not weaken or replace the earlier parser/bridge assertions.
        fixtures.Name.Name += ".Helpers";
        output = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1]))!, "LiveLogIntegrationFixtures.helpers.patched.dll");
        fixtures.Write(output);
        assertions += Run(output, "RunHelpers");
        Console.WriteLine("PASS: " + assertions + " integration assertions using actual DLL parser, Action, refresh engine, bridge, lifecycle hook, pure wrapper, option-summary and dynamic helper IL; game/UI/compiler boundaries are fixtures.");
        return 0;
    }

    static int Run(string output, string method)
    {
        var assembly = Assembly.LoadFile(output);
        try { return (int)assembly.GetType("DiceMaster.Scenario", true).GetMethod(method).Invoke(null, null); }
        catch (TargetInvocationException error) { throw error.InnerException ?? error; }
    }
}
