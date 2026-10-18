using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class LogRefreshInjector
{
    static void InstallSafeEvaluationHooks()
    {
        var evaluator = module.GetType("DiceMaster.LiveLog.SafeDisplayEvaluation");
        foreach (var item in new[] {
            (Owner: "Mortal.Core.StatGroupVariable", Original: "GetResult", Safe: "ReadGroup", Result: module.TypeSystem.Int32),
            (Owner: "Mortal.Core.StatCompareItem", Original: "Check", Safe: "CheckComparison", Result: module.TypeSystem.Boolean),
            (Owner: "Mortal.Story.PositionEventData", Original: "GetRate", Safe: "ReadEventRate", Result: module.TypeSystem.Int32)
        })
        {
            string wrapperName = "Preview" + item.Safe;
            var existing = patch.Methods.FirstOrDefault(m => m.Name == wrapperName);
            TypeReference inputType = existing == null ? module.GetTypeReferences().First(t => t.FullName == item.Owner)
                : existing.Parameters[0].ParameterType;
            var wrapper = NewMethod(patch, wrapperName, item.Result, false, inputType);
            var il = Body(wrapper);
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, Method(evaluator, item.Safe)); il.Emit(OpCodes.Ret);
            // All these mod call sites build descriptions, including narrative
            // checkpoint descriptions outside the periodic preview scope. The
            // real game's original evaluation methods are never rewritten.
            foreach (var type in new[] { cache, module.GetType("DiceMaster.DataHelper"), mapper, patch })
                foreach (var method in type.Methods.Where(m => m.HasBody && m != wrapper))
                    foreach (var instruction in method.Body.Instructions)
                        if (instruction.Operand is MethodReference called && called.DeclaringType.FullName == item.Owner && called.Name == item.Original)
                        {
                            instruction.OpCode = OpCodes.Call;
                            instruction.Operand = wrapper;
                            changed.Add(method);
                        }
        }
        // Replace complete description boundaries so numeric random previews
        // and their colors share one independent display evaluation session.
        var formatting = module.GetType("DiceMaster.LiveLog.PreviewFormatting");
        var conditions = module.GetType("DiceMaster.DataHelper").Methods.Single(m => m.Name == "ExtractConditions" &&
            m.Parameters[0].ParameterType.FullName == "Mortal.Core.ConditionResultItem");
        var conditionBody = Body(conditions);
        conditionBody.Emit(OpCodes.Ldarg_0); conditionBody.Emit(OpCodes.Ldarg_1);
        conditionBody.Emit(OpCodes.Call, Method(formatting, "ExtractConditions")); conditionBody.Emit(OpCodes.Ret);
        var dynamic = Method(cache, "GenerateDynamicDesc");
        var eventBody = Body(dynamic);
        eventBody.Emit(OpCodes.Ldarg_1); eventBody.Emit(OpCodes.Call, Method(formatting, "EventDescription")); eventBody.Emit(OpCodes.Ret);
        var boundary = Body(Method(cache, "GeneratePreviewDynamicDesc"));
        boundary.Emit(OpCodes.Ldarg_0); boundary.Emit(OpCodes.Ldarg_1);
        boundary.Emit(OpCodes.Call, Method(module.GetType("DiceMaster.LiveLog.LogBridge"), "DescribeEvent")); boundary.Emit(OpCodes.Ret);
    }
}
