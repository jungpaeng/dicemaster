using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using DiceMaster.LiveLog;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class Program
{
    private static int assertions;
    private static int preservedMethods;
    private static int normalizedMethods;

    private static readonly HashSet<string> replacements = new HashSet<string>(StringComparer.Ordinal) {
        "DiceMaster.DicePlugin::RefreshGameData", "DiceMaster.DicePlugin::RefreshLiveLogDisplay",
        "DiceMaster.OptionInfoPatch::LuaEnvironment_LoadLuaFunction_Prefix",
        "DiceMaster.OptionInfoPatch::ReprocessLastLuaFunction", "DiceMaster.OptionInfoPatch::RebuildLiveLogEntries",
        "DiceMaster.PositionEventCache::GetPositionEventDesc", "DiceMaster.PositionEventCache::GeneratePreviewDynamicDesc",
        "DiceMaster.PositionEventCache::ClearDisplayCaches", "DiceMaster.PositionEventCache::GetPositionEvents",
        "DiceMaster.UiTextMapper::TryGetCachedTranslation", "DiceMaster.UiTextMapper::CacheTranslation",
        "DiceMaster.DataHelper::GetMissions", "DiceMaster.DataHelper::ExtractConditions",
        "DiceMaster.PositionEventCache::GenerateDynamicDesc",
        "DiceMaster.OptionInfoPatch::LeanLocalizationResolver_GetStoryText_Postfix",
        // These wrappers are checked instruction-for-instruction below. Source
        // builds can include a compiler nop/local around the same pure call.
        "DiceMaster.OptionInfoPatch::PreviewReadGroup", "DiceMaster.OptionInfoPatch::PreviewCheckComparison",
        "DiceMaster.OptionInfoPatch::PreviewReadEventRate",
        "DiceMaster.PositionEventCache::GenerateStaticDesc",
        "DiceMaster.PositionEventCache::GetDefinitionParser",
        "DiceMaster.LuaParse.Action::.ctor", "DiceMaster.LuaParse.Action::get_Description", "DiceMaster.LuaParse.Action::set_Description",
        "DiceMaster.LuaParse.LuaParser::.ctor", "DiceMaster.LuaParse.LuaParser::.cctor",
        "DiceMaster.LuaParse.LuaParser::ClearCaches", "DiceMaster.LuaParse.LuaParser::ExtractOptionActionsFromLua",
        "DiceMaster.LuaParse.LuaParser::ExtractOptionActionsFromChooseBlocks", "DiceMaster.LuaParse.LuaParser::BuildActionListFromBlock",
        "DiceMaster.LuaParse.LuaParser::ExtractSwitchBranchFirstStoryTextsFromLua", "DiceMaster.LuaParse.LuaParser::ExtractFirstStoryTextKey",
        "DiceMaster.LuaParse.LuaParser::AddActionsToList", "DiceMaster.LuaParse.LuaParser::ExtractSwitchCheckFromLua",
        "DiceMaster.LuaParse.LuaParser::ExtractPositionWorkFromLua", "DiceMaster.LuaParse.LuaParser::ParseFirstLuaArgument",
        "DiceMaster.LuaParse.LuaParser::ExtractActionsFromLua", "DiceMaster.LuaParse.LuaParser::Description"
    };

    private static readonly Dictionary<string, string> lifecycleDelegates = LifecycleDelegates();
    private static Dictionary<string, string> LifecycleDelegates()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string target in new[] { "SaveSystem_NewGameData", "SaveSystem_NewActGameData", "SaveSystem_LoadGameData", "SaveSystem_AutoLoadGameData", "SaveSystem_LoadRestoreSaveData", "SaveSystem_ExecuteLoadGameData", "SceneController_StartLoadScene" })
        {
            result.Add("LiveLog_" + target + "_Prefix", "EnterTransition");
            result.Add("LiveLog_" + target + "_Finalizer", "ExitTransition");
        }
        result.Add("LiveLog_LuaStage_Prefix", "StageLua");
        result.Add("LiveLog_LuaComplete_Postfix", "CompleteLua");
        result.Add("LiveLog_LuaAbort_Finalizer", "AbortLua");
        return result;
    }

    private static void Check(bool value, string message)
    {
        assertions++;
        if (!value) throw new InvalidOperationException(message);
    }

    private static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> roots)
    {
        foreach (TypeDefinition type in roots)
        {
            yield return type;
            foreach (TypeDefinition nested in Types(type.NestedTypes)) yield return nested;
        }
    }

    private static bool IsDepth(Instruction instruction)
    {
        return instruction != null && instruction.OpCode == OpCodes.Ldsfld && instruction.Operand is FieldReference field &&
            field.DeclaringType.FullName == "DiceMaster.OptionInfoPatch" && field.Name == "DisplayRefreshDepth";
    }

    private static bool IsBrFalse(Instruction instruction)
    {
        return instruction != null && (instruction.OpCode == OpCodes.Brfalse || instruction.OpCode == OpCodes.Brfalse_S);
    }
    private static bool IsBrTrue(Instruction instruction)
    {
        return instruction != null && (instruction.OpCode == OpCodes.Brtrue || instruction.OpCode == OpCodes.Brtrue_S);
    }

    private static bool HasPreviewGuard(IList<Instruction> code)
    {
        if (code.Count < 4 || !IsDepth(code[0])) return false;
        if (IsBrFalse(code[1])) return code[2].OpCode == OpCodes.Ret && ReferenceEquals(code[1].Operand, code[3]) && !IsDepth(code[3]);
        // The full-source compiler can invert the condition and branch to the
        // existing final ret. This also skips the entire gameplay body.
        return IsBrTrue(code[1]) && ReferenceEquals(code[1].Operand, code[code.Count - 1]) && code[code.Count - 1].OpCode == OpCodes.Ret && !IsDepth(code[2]);
    }

    private static string OriginalDisplayCall(MethodReference call)
    {
        if (call == null || call.DeclaringType.FullName != "DiceMaster.OptionInfoPatch" || call.HasThis || call.Parameters.Count != 1) return null;
        string owner, name, result;
        switch (call.Name)
        {
            case "PreviewReadGroup": owner = "Mortal.Core.StatGroupVariable"; name = "GetResult"; result = "System.Int32"; break;
            case "PreviewCheckComparison": owner = "Mortal.Core.StatCompareItem"; name = "Check"; result = "System.Boolean"; break;
            case "PreviewReadEventRate": owner = "Mortal.Story.PositionEventData"; name = "GetRate"; result = "System.Int32"; break;
            default: return null;
        }
        Check(call.Parameters[0].ParameterType.FullName == owner && call.ReturnType.FullName == result, "Invalid safe display wrapper signature.");
        return "method:" + result + " " + owner + "::" + name + "()";
    }

    private static HashSet<Instruction> Guards(MethodDefinition method)
    {
        HashSet<Instruction> removed = new HashSet<Instruction>();
        string owner = method.DeclaringType.FullName;
        if (owner == "DiceMaster.OptionInfoPatch" && new[] { "PrefixExecuteConditionResult", "PrefixExecuteSwitchResult", "PrefixCheckMission", "PositionEventData_GetRate_Postfix", "MissionCheckData_Postfix" }.Contains(method.Name))
        {
            IList<Instruction> code = method.Body.Instructions;
            if (IsDepth(code[0]))
            {
                Check(code.Count > 3 && IsBrFalse(code[1]) && code[2].OpCode == OpCodes.Ret && ReferenceEquals(code[1].Operand, code[3]),
                    "Malformed preview callback guard: " + method.FullName);
                removed.Add(code[0]); removed.Add(code[1]); removed.Add(code[2]);
                Check(!IsDepth(code[3]), "Duplicate preview callback guard: " + method.FullName);
            }
        }
        if (owner == "DiceMaster.DicePlugin" && (method.Name == "Update" || method.Name == "DrawLogWindow"))
        {
            foreach (Instruction instruction in method.Body.Instructions)
            {
                if (instruction.OpCode != OpCodes.Call || !(instruction.Operand is MethodReference call) ||
                    call.DeclaringType.FullName != owner || call.Name != "RefreshLiveLogDisplay") continue;
                Check(instruction.Previous != null && instruction.Previous.OpCode == OpCodes.Ldarg_0, "Malformed display hook: " + method.FullName);
                removed.Add(instruction.Previous); removed.Add(instruction);
            }
        }
        if (owner == "DiceMaster.DicePlugin" && (method.Name == "DrawEventEntries" || method.Name == "DrawMissionEntries"))
        {
            Instruction button = method.Body.Instructions.First(i => i.OpCode == OpCodes.Ldstr &&
                ((string)i.Operand == "이벤트 실행" || (string)i.Operand == "미션 실행"));
            Instruction guard = button.Previous;
            if (IsBrFalse(guard))
            {
                Instruction originalSkip = method.Body.Instructions.SkipWhile(i => i != button).First(IsBrFalse);
                Check(guard.Previous.OpCode == OpCodes.Ldloc && guard.Previous.Operand is VariableDefinition variable &&
                    variable.VariableType.FullName == (method.Name == "DrawEventEntries" ? "Mortal.Story.PositionEventData" : "Mortal.Core.MissionCheckData") &&
                    ReferenceEquals(guard.Operand, originalSkip.Operand), "Malformed status-only row button guard: " + method.FullName);
                removed.Add(guard.Previous); removed.Add(guard);
            }
        }
        if (owner == "DiceMaster.DataHelper" || owner == "DiceMaster.OptionInfoPatch" || owner == "DiceMaster.UiTextMapper")
        {
            foreach (ExceptionHandler handler in method.Body.ExceptionHandlers)
            {
                if (handler.HandlerType != ExceptionHandlerType.Catch) continue;
                Instruction first = handler.HandlerStart;
                Instruction guard = first == null ? null : first.Next;
                if (!IsDepth(guard)) continue;
                Check(IsBrFalse(guard.Next) && guard.Next.Next.OpCode == OpCodes.Rethrow &&
                    ReferenceEquals(guard.Next.Operand, guard.Next.Next.Next), "Malformed preview catch guard: " + method.FullName);
                removed.Add(guard); removed.Add(guard.Next); removed.Add(guard.Next.Next);
                Check(!IsDepth(guard.Next.Next.Next), "Duplicate preview catch guard: " + method.FullName);
            }
        }
        if (owner == "DiceMaster.ReflectionExtensions" && method.Name == "GetFieldValue" && IsDepth(method.Body.Instructions[0]))
        {
            IList<Instruction> code = method.Body.Instructions;
            Check(code.Count >= 8 && IsBrFalse(code[1]) && ReferenceEquals(code[1].Operand, code[7]) &&
                code[2].OpCode == OpCodes.Ldarg_0 && code[3].OpCode == OpCodes.Ldarg_1 &&
                code[4].OpCode == OpCodes.Call && code[4].Operand is MethodReference call &&
                call.DeclaringType.FullName == "DiceMaster.LiveLog.LogBridge" && call.Name == "ReadFieldStrict" &&
                code[5].OpCode == OpCodes.Unbox_Any && code[6].OpCode == OpCodes.Ret,
                "Malformed strict reflection prefix: " + method.FullName);
            for (int index = 0; index < 7; index++) removed.Add(code[index]);
            Check(!IsDepth(code[7]), "Duplicate strict reflection prefix.");
        }
        return removed;
    }

    private static string Fingerprint(MethodDefinition method, bool normalizeGuards)
    {
        if (!method.HasBody) return "no-body";
        HashSet<Instruction> removed = normalizeGuards ? Guards(method) : new HashSet<Instruction>();
        List<Instruction> retained = method.Body.Instructions.Where(i => !removed.Contains(i)).ToList();
        Dictionary<Instruction, int> indices = new Dictionary<Instruction, int>();
        for (int index = 0; index < retained.Count; index++) indices.Add(retained[index], index);
        string Target(Instruction instruction)
        {
            if (instruction == null) return "end";
            while (instruction != null && removed.Contains(instruction)) instruction = instruction.Next;
            return instruction == null ? "end" : indices[instruction].ToString(CultureInfo.InvariantCulture);
        }
        string Operand(object operand)
        {
            if (operand == null) return string.Empty;
            if (operand is Instruction branch) return "target:" + Target(branch);
            if (operand is Instruction[] branches) return "targets:" + string.Join(",", branches.Select(Target));
            if (operand is VariableDefinition variable) return "local:" + variable.Index;
            if (operand is ParameterDefinition parameter) return "arg:" + parameter.Index;
            if (operand is MethodReference call)
            {
                if (normalizeGuards)
                {
                    string original = OriginalDisplayCall(call);
                    if (original != null) return original;
                }
                if (normalizeGuards && method.DeclaringType.FullName == "DiceMaster.PositionEventCache" && method.Name == "GenerateStaticDesc" &&
                    call.DeclaringType.FullName == "DiceMaster.PositionEventCache" && call.Name == "GetDefinitionParser")
                {
                    Check(!call.HasThis && call.Parameters.Count == 0 && call.ReturnType.FullName == "DiceMaster.LuaParse.LuaParser", "Invalid event parser replacement call.");
                    return "method:DiceMaster.LuaParse.LuaParser DiceMaster.LuaParse.LuaParser::get_Instance()";
                }
                return "method:" + call.FullName;
            }
            if (operand is FieldReference field) return "field:" + field.FullName;
            if (operand is TypeReference type) return "type:" + type.FullName;
            if (operand is CallSite site) return "callsite:" + site.FullName;
            if (operand is float single) return single.ToString("R", CultureInfo.InvariantCulture);
            if (operand is double number) return number.ToString("R", CultureInfo.InvariantCulture);
            if (operand is IFormattable formatted) return formatted.ToString(null, CultureInfo.InvariantCulture);
            return operand.ToString();
        }
        StringBuilder body = new StringBuilder();
        body.Append("locals:").Append(method.Body.InitLocals).Append('\n');
        foreach (VariableDefinition local in method.Body.Variables) body.Append(local.VariableType.FullName).Append('\n');
        foreach (Instruction instruction in retained)
        {
            string code = instruction.OpCode.Code.ToString();
            if (normalizeGuards && instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference safe && OriginalDisplayCall(safe) != null)
                code = "Callvirt";
            if (instruction.OpCode.OperandType == OperandType.ShortInlineBrTarget && code.EndsWith("_S", StringComparison.Ordinal))
                code = code.Substring(0, code.Length - 2);
            body.Append(code).Append('|').Append(Operand(instruction.Operand)).Append('\n');
        }
        foreach (ExceptionHandler handler in method.Body.ExceptionHandlers)
        {
            body.Append("handler:").Append(handler.HandlerType).Append('|').Append(handler.CatchType == null ? "" : handler.CatchType.FullName)
                .Append('|').Append(Target(handler.TryStart)).Append('|').Append(Target(handler.TryEnd))
                .Append('|').Append(Target(handler.HandlerStart)).Append('|').Append(Target(handler.HandlerEnd))
                .Append('|').Append(Target(handler.FilterStart)).Append('\n');
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body.ToString())));
    }

    private static int Calls(MethodDefinition method, string owner, string name)
    {
        return method.Body.Instructions.Count(i => i.Operand is MethodReference call && call.DeclaringType.FullName == owner && call.Name == name);
    }

    private static MethodDefinition Method(ModuleDefinition module, string owner, string name)
    {
        TypeDefinition type = module.GetType(owner);
        Check(type != null, "Missing type: " + owner);
        MethodDefinition[] methods = type.Methods.Where(m => m.Name == name).ToArray();
        Check(methods.Length == 1, "Missing/ambiguous method: " + owner + "::" + name);
        return methods[0];
    }

    private static void Verify(string inputPath, string patchedPath)
    {
        using AssemblyDefinition input = AssemblyDefinition.ReadAssembly(inputPath);
        using AssemblyDefinition patched = AssemblyDefinition.ReadAssembly(patchedPath);
        ModuleDefinition original = input.MainModule, output = patched.MainModule;
        Check(input.Name.FullName == patched.Name.FullName, "Assembly identity changed.");
        Dictionary<string, TypeDefinition> actualTypes = new Dictionary<string, TypeDefinition>(StringComparer.Ordinal);
        foreach (TypeDefinition type in Types(output.Types))
        {
            Check(!actualTypes.ContainsKey(type.FullName), "Duplicate type: " + type.FullName);
            actualTypes.Add(type.FullName, type);
            Check(type.Methods.Select(m => m.FullName).Distinct(StringComparer.Ordinal).Count() == type.Methods.Count, "Duplicate method signature in " + type.FullName);
        }
        foreach (TypeDefinition type in Types(original.Types))
        {
            if (type.Namespace == "DiceMaster.LiveLog" || type.FullName.StartsWith("DiceMaster.LiveLog.", StringComparison.Ordinal)) continue;
            Check(actualTypes.TryGetValue(type.FullName, out TypeDefinition actual), "Existing type removed: " + type.FullName);
            Check(type.Attributes == actual.Attributes && (type.BaseType == null ? null : type.BaseType.FullName) ==
                (actual.BaseType == null ? null : actual.BaseType.FullName), "Existing type shape changed: " + type.FullName);
            foreach (FieldDefinition field in type.Fields)
            {
                FieldDefinition[] matches = actual.Fields.Where(f => f.Name == field.Name).ToArray();
                Check(matches.Length == 1 && matches[0].FieldType.FullName == field.FieldType.FullName && matches[0].Attributes == field.Attributes,
                    "Existing field changed: " + field.FullName);
            }
            foreach (MethodDefinition method in type.Methods)
            {
                MethodDefinition[] matches = actual.Methods.Where(m => m.FullName == method.FullName).ToArray();
                Check(matches.Length == 1 && matches[0].Attributes == method.Attributes && matches[0].ImplAttributes == method.ImplAttributes,
                    "Existing method signature/attributes changed: " + method.FullName);
                MethodDefinition after = matches[0];
                if (type.FullName == "DiceMaster.OptionInfoPatch" && lifecycleDelegates.TryGetValue(method.Name, out string lifecycleCall))
                {
                    Check(Calls(after, "DiceMaster.LiveLog.LogBridge", lifecycleCall) == 1 &&
                        after.Body.Instructions.Where(i => i.Operand is MethodReference).All(i => ((MethodReference)i.Operand).DeclaringType.FullName == "DiceMaster.LiveLog.LogBridge"),
                        "Lifecycle wrapper contains a missing/duplicated or foreign call: " + after.FullName);
                    continue;
                }
                if (replacements.Contains(type.FullName + "::" + method.Name) &&
                    !(type.FullName == "DiceMaster.DataHelper" && method.Name == "ExtractConditions" &&
                      method.Parameters[0].ParameterType.FullName != "Mortal.Core.ConditionResultItem")) continue;
                string before = Fingerprint(method, false), rawAfter = Fingerprint(after, false);
                if (before == rawAfter) { preservedMethods++; continue; }
                Check(Fingerprint(method, true) == Fingerprint(after, true), "Unexpected method body change: " + method.FullName);
                normalizedMethods++;
            }
        }

        Check(original.Resources.Count == output.Resources.Count, "Resource count changed.");
        for (int index = 0; index < original.Resources.Count; index++)
        {
            Resource before = original.Resources[index], after = output.Resources[index];
            Check(before.Name == after.Name && before.Attributes == after.Attributes && before.ResourceType == after.ResourceType,
                "Resource identity changed: " + before.Name);
            Check(before is EmbeddedResource && after is EmbeddedResource, "Expected embedded resources only.");
            Check(((EmbeddedResource)before).GetResourceData().SequenceEqual(((EmbeddedResource)after).GetResourceData()),
                "Embedded resource bytes changed: " + before.Name);
        }
        Check(!output.AssemblyReferences.Any(r => r.Name == "DiceMaster.LiveLogPayload" || r.Name == "System.Private.CoreLib"), "Payload/net8 assembly reference leaked into the .NET Framework mod.");
        foreach (var entry in new[] { ("PreviewReadGroup", "ReadGroup"), ("PreviewCheckComparison", "CheckComparison"), ("PreviewReadEventRate", "ReadEventRate") })
        {
            var safe = Method(output, "DiceMaster.OptionInfoPatch", entry.Item1);
            var code = safe.Body.Instructions;
            Check(code.Count == 3 && code[0].OpCode == OpCodes.Ldarg_0 && code[1].OpCode == OpCodes.Call &&
                code[1].Operand is MethodReference call && call.DeclaringType.FullName == "DiceMaster.LiveLog.SafeDisplayEvaluation" &&
                call.Name == entry.Item2 && code[2].OpCode == OpCodes.Ret, "Safe display wrapper has an unsafe fallback.");
        }
        var conditionFormatter = output.GetType("DiceMaster.DataHelper").Methods.Single(m => m.Name == "ExtractConditions" &&
            m.Parameters[0].ParameterType.FullName == "Mortal.Core.ConditionResultItem");
        var conditionCode = conditionFormatter.Body.Instructions;
        Check(conditionCode.Count == 4 && conditionCode[0].OpCode == OpCodes.Ldarg_0 && conditionCode[1].OpCode == OpCodes.Ldarg_1 &&
            conditionCode[2].OpCode == OpCodes.Call && conditionCode[2].Operand is MethodReference conditionCall &&
            conditionCall.DeclaringType.FullName == "DiceMaster.LiveLog.PreviewFormatting" && conditionCall.Name == "ExtractConditions" &&
            conditionCode[3].OpCode == OpCodes.Ret, "Condition formatter must preserve the shared numeric display snapshot boundary.");
        var eventCode = Method(output, "DiceMaster.PositionEventCache", "GenerateDynamicDesc").Body.Instructions;
        Check(eventCode.Count == 3 && eventCode[0].OpCode == OpCodes.Ldarg_1 && eventCode[1].OpCode == OpCodes.Call &&
            eventCode[1].Operand is MethodReference eventCall && eventCall.DeclaringType.FullName == "DiceMaster.LiveLog.PreviewFormatting" &&
            eventCall.Name == "EventDescription" && eventCode[2].OpCode == OpCodes.Ret, "Event formatter must delegate header and conditions to one display snapshot.");
        var boundaryCode = Method(output, "DiceMaster.PositionEventCache", "GeneratePreviewDynamicDesc").Body.Instructions;
        Check(boundaryCode.Count == 4 && boundaryCode[0].OpCode == OpCodes.Ldarg_0 && boundaryCode[1].OpCode == OpCodes.Ldarg_1 &&
            boundaryCode[2].OpCode == OpCodes.Call && boundaryCode[2].Operand is MethodReference boundaryCall &&
            boundaryCall.DeclaringType.FullName == "DiceMaster.LiveLog.LogBridge" && boundaryCall.Name == "DescribeEvent" &&
            boundaryCode[3].OpCode == OpCodes.Ret, "Event description must preserve incoming protection depth.");
        Check(Method(output, "DiceMaster.LiveLog.LogBridge", "DescribeEvent").Body.ExceptionHandlers.Any(handler => handler.HandlerType == ExceptionHandlerType.Finally),
            "Event description scope must restore the captured depth in finally.");
        Check(Calls(Method(output, "DiceMaster.DicePlugin", "Update"), "DiceMaster.DicePlugin", "RefreshLiveLogDisplay") == 1, "Update automatic display hook missing/duplicated.");
        Check(Calls(Method(output, "DiceMaster.DicePlugin", "Update"), "DiceMaster.DicePatch", "UpdateGamepad") == 1, "Update gamepad hook missing/duplicated.");
        Check(Calls(Method(output, "DiceMaster.DicePlugin", "DrawLogWindow"), "DiceMaster.DicePlugin", "RefreshLiveLogDisplay") == 1, "GUI tab refresh hook missing/duplicated.");
        foreach (string name in new[] { "LuaEnvironment_LoadLuaFunction_Prefix", "PrefixExecuteConditionResult", "PrefixExecuteSwitchResult", "PrefixCheckMission", "PositionEventData_GetRate_Postfix", "MissionCheckData_Postfix" })
        {
            MethodDefinition method = Method(output, "DiceMaster.OptionInfoPatch", name);
            IList<Instruction> code = method.Body.Instructions;
            Check(HasPreviewGuard(code),
                "Preview guard missing/invalid: " + method.FullName);
        }
        MethodDefinition tick = Method(output, "DiceMaster.LiveLog.RefreshEngine", "Tick");
        Check(tick.Parameters.Count == 9 && tick.Parameters[0].ParameterType.FullName == "System.String" && tick.Parameters[3].ParameterType.FullName == "System.Single",
            "Refresh engine API shape changed.");
        Check(tick.Body.ExceptionHandlers.Any(h => h.HandlerType == ExceptionHandlerType.Finally), "Refresh engine reentry guard lacks a finally boundary.");
        TypeDefinition row = output.GetType("DiceMaster.LiveLog.DisplayRow");
        Check(row != null && row.Fields.Any(f => f.Name == "Id" && f.FieldType.FullName == "System.String") &&
            row.Fields.Any(f => f.Name == "Value" && f.FieldType.FullName == "System.Object") &&
            row.Fields.Any(f => f.Name == "Status" && f.FieldType.FullName == "System.String"), "Display row no longer separates identity, opaque value, status.");
        Check(output.GetType("DiceMaster.LiveLog.RefreshEngine/ItemState").Fields.Any(f => f.Name == "RetryAt" && f.FieldType.FullName == "System.Double"), "Per-item retry timestamp missing.");
        Check(output.GetType("DiceMaster.LiveLog.RefreshEngine").Fields.Any(f => f.Name == "tabs" && f.FieldType.FullName.Contains("Dictionary`2<System.String,")), "Per-tab engine state missing.");
        Check(output.GetType("DiceMaster.DataHelper").Fields.Count(f => f.Name == "LiveMissionFilter" && f.IsStatic && f.FieldType.FullName == "System.Object") == 1, "Mission preview filter missing/duplicated.");
        MethodDefinition definitionParser = Method(output, "DiceMaster.PositionEventCache", "GetDefinitionParser");
        IList<Instruction> parserCode = definitionParser.Body.Instructions;
        Check(definitionParser.IsStatic && definitionParser.Parameters.Count == 0 && definitionParser.ReturnType.FullName == "DiceMaster.LuaParse.LuaParser" &&
            parserCode.Count == 3 && parserCode[0].OpCode == OpCodes.Call && parserCode[0].Operand is MethodReference parserCall &&
            parserCall.DeclaringType.FullName == "DiceMaster.LiveLog.LogBridge" && parserCall.Name == "GetEventParser" &&
            parserCode[1].OpCode == OpCodes.Castclass && parserCode[1].Operand is TypeReference parserType && parserType.FullName == "DiceMaster.LuaParse.LuaParser" &&
            parserCode[2].OpCode == OpCodes.Ret, "The event parser helper does more than return the bridge's isolated parser.");
        Check(Calls(Method(output, "DiceMaster.PositionEventCache", "GenerateStaticDesc"), "DiceMaster.PositionEventCache", "GetDefinitionParser") == 1 &&
            Calls(Method(output, "DiceMaster.PositionEventCache", "GenerateStaticDesc"), "DiceMaster.LuaParse.LuaParser", "get_Instance") == 0,
            "Fixed event definitions still mutate the shared parser singleton.");
        Console.WriteLine("PASS: " + assertions + " artifact assertions; " + preservedMethods + " existing method bodies unchanged; " + normalizedMethods + " bodies differ only by verified display guards/parser routing; " + original.Resources.Count + " embedded resources byte-identical.");
    }

    private static void Benchmark()
    {
        const int rows = 100, iterations = 1000;
        int reads = 0;
        object[] models = new object[rows];
        List<DisplayQuery> queries = new List<DisplayQuery>();
        for (int index = 0; index < rows; index++)
        {
            int id = index; models[index] = new object();
            queries.Add(new DisplayQuery("row/" + index, delegate { reads++; return models[id]; }));
        }
        int published = 0;
        Func<List<DisplayQuery>> discover = delegate { return queries; };
        Action<List<DisplayRow>> publish = delegate(List<DisplayRow> values) { published += values.Count; };
        Action<string> log = delegate { throw new InvalidOperationException("The healthy benchmark must not log."); };
        int[] coldCollections = { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
        long coldAllocated = GC.GetAllocatedBytesForCurrentThread(), coldStart = Stopwatch.GetTimestamp();
        RefreshEngine engine = new RefreshEngine();
        engine.Tick("mock", true, true, 0, false, false, discover, publish, log);
        TimeSpan coldElapsed = Stopwatch.GetElapsedTime(coldStart);
        coldAllocated = GC.GetAllocatedBytesForCurrentThread() - coldAllocated;
        int cold0 = GC.CollectionCount(0) - coldCollections[0], cold1 = GC.CollectionCount(1) - coldCollections[1], cold2 = GC.CollectionCount(2) - coldCollections[2];
        for (int index = 1; index <= 20; index++) engine.Tick("mock", true, true, index * 0.25f, false, false, discover, publish, log);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        int beforeReads = reads, beforePublished = published;
        int[] collections = { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
        long allocated = GC.GetAllocatedBytesForCurrentThread(), started = Stopwatch.GetTimestamp();
        for (int index = 1; index <= iterations; index++) engine.Tick("mock", true, true, 5 + index * 0.25f, false, false, discover, publish, log);
        TimeSpan duration = Stopwatch.GetElapsedTime(started);
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        int gen0 = GC.CollectionCount(0) - collections[0], gen1 = GC.CollectionCount(1) - collections[1], gen2 = GC.CollectionCount(2) - collections[2];
        Check(reads - beforeReads == rows * iterations && published - beforePublished == rows * iterations, "Benchmark skipped rows/ticks.");
        Console.WriteLine("MOCK BENCHMARK ONLY: standalone engine; no Unity, game APIs, parser, reflection bridge, or Windows game runtime.");
        Console.WriteLine("ENV " + RuntimeInformation.OSDescription + "; " + RuntimeInformation.FrameworkDescription + "; arch=" + RuntimeInformation.ProcessArchitecture);
        Console.WriteLine("COLD rows=" + rows + " ticks=1 elapsed_ms=" + coldElapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture) + " allocated_bytes=" + coldAllocated +
            " gc0=" + cold0 + " gc1=" + cold1 + " gc2=" + cold2);
        Console.WriteLine("WARM rows=" + rows + " ticks=" + iterations + " elapsed_ms=" + duration.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture) +
            " mean_tick_ms=" + (duration.TotalMilliseconds / iterations).ToString("F6", CultureInfo.InvariantCulture) +
            " allocated_bytes=" + allocated + " bytes_per_tick=" + (allocated / (double)iterations).ToString("F1", CultureInfo.InvariantCulture) +
            " gc0=" + gen0 + " gc1=" + gen1 + " gc2=" + gen2 + " reads=" + (reads - beforeReads));
    }

    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--benchmark") { Benchmark(); return 0; }
        if (args.Length < 2 || args.Length > 3 || (args.Length == 3 && args[2] != "--benchmark"))
            throw new ArgumentException("Usage: LiveLogArtifactTests input.dll patched.dll [--benchmark] | --benchmark");
        Verify(args[0], args[1]);
        if (args.Length == 3) Benchmark();
        return 0;
    }
}
