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
    }
}
