using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Bind the candidate's typed resource reader delegate to an explicit fixture
// boundary. The returned ScriptReadResult remains the actual copied model type.
static class PreviewFixtureBoundary
{
    public static void Install(ModuleDefinition module)
    {
        var result = module.GetType("DiceMaster.LiveLog.ScriptReadResult");
        if (result == null) return; // alpha.2 reproduction has no new reader API.
        var helper = module.GetType("DiceMaster.DataHelper");
        var method = helper.Methods.SingleOrDefault(m => m.Name == "ReadPreviewScript");
        if (method == null)
        {
            method = new MethodDefinition("ReadPreviewScript", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, result);
            method.Parameters.Add(new ParameterDefinition("name", ParameterAttributes.None, module.TypeSystem.String));
            helper.Methods.Add(method);
        }
        method.ReturnType = result;
        method.Body = new MethodBody(method);
        var il = method.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, helper.Methods.Single(m => m.Name == "ReadPreviewScriptBoundary"));
        il.Emit(OpCodes.Castclass, result);
        il.Emit(OpCodes.Ret);
    }
}
