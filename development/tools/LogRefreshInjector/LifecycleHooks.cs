using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class LogRefreshInjector
{
    static void InstallLifecycleHooks(TypeDefinition bridge)
    {
        var existingLuaPrefix = Method(patch, "LuaEnvironment_LoadLuaFunction_Prefix");
        var patchTemplate = existingLuaPrefix.CustomAttributes.Single(attribute =>
            attribute.AttributeType.FullName == "HarmonyLib.HarmonyPatch" && attribute.ConstructorArguments.Count == 2);
        var prefixTemplate = existingLuaPrefix.CustomAttributes.Single(attribute => attribute.AttributeType.FullName == "HarmonyLib.HarmonyPrefix");
        var postfixTemplate = patch.Methods.SelectMany(method => method.CustomAttributes)
            .First(attribute => attribute.AttributeType.FullName == "HarmonyLib.HarmonyPostfix");
        var exceptionType = new TypeReference("System", "Exception", module, module.TypeSystem.CoreLibrary);
        var finalizerType = new TypeReference("HarmonyLib", "HarmonyFinalizer", module, prefixTemplate.AttributeType.Scope);
        var finalizerConstructor = Ref(finalizerType, ".ctor", module.TypeSystem.Void, true);
        var core = module.AssemblyReferences.Single(reference => reference.Name == "Mortal.Core");
        var saveSystem = new TypeReference("Mortal.Core", "SaveSystem", module, core);
        var sceneController = new TypeReference("Mortal.Core", "SceneController", module, core);
        var luaEnvironment = (TypeReference)patchTemplate.ConstructorArguments[0].Value;

        void Attributes(MethodDefinition method, TypeReference targetType, string targetName, MethodReference marker)
        {
            method.CustomAttributes.Clear();
            var target = new CustomAttribute(patchTemplate.Constructor);
            target.ConstructorArguments.Add(new CustomAttributeArgument(patchTemplate.ConstructorArguments[0].Type, targetType));
            target.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, targetName));
            method.CustomAttributes.Add(target);
            method.CustomAttributes.Add(new CustomAttribute(marker));
        }

        MethodDefinition Hook(string name, TypeReference result, params TypeReference[] parameters)
        {
            var method = NewMethod(patch, name, result, false, parameters);
            if (method.Parameters.Count != parameters.Length) throw new InvalidOperationException("Lifecycle hook parameter mismatch: " + name);
            return method;
        }

        void State(ParameterDefinition parameter)
        {
            parameter.Name = "__state";
            parameter.Attributes = ParameterAttributes.Out;
        }

        // This original hook retains its Harmony metadata/signature, but new
        // paired hooks only activate source after the game's non-null result.
        Body(existingLuaPrefix).Emit(OpCodes.Ret);

        void Transition(TypeReference targetType, string targetName, string suffix)
        {
            var prefix = Hook("LiveLog_" + suffix + "_Prefix", module.TypeSystem.Void, new ByReferenceType(module.TypeSystem.Int64));
            State(prefix.Parameters[0]);
            Attributes(prefix, targetType, targetName, prefixTemplate.Constructor);
            var il = Body(prefix);
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, Method(bridge, "EnterTransition")); il.Emit(OpCodes.Stind_I8); il.Emit(OpCodes.Ret);

            var finalizer = Hook("LiveLog_" + suffix + "_Finalizer", exceptionType, exceptionType, module.TypeSystem.Int64);
            finalizer.Parameters[0].Name = "__exception"; finalizer.Parameters[1].Name = "__state";
            Attributes(finalizer, targetType, targetName, finalizerConstructor);
            il = Body(finalizer);
            il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, Method(bridge, "ExitTransition"));
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ret);
        }

        foreach (string name in new[] { "NewGameData", "NewActGameData", "LoadGameData", "AutoLoadGameData", "LoadRestoreSaveData", "ExecuteLoadGameData" })
            Transition(saveSystem, name, "SaveSystem_" + name);
        Transition(sceneController, "StartLoadScene", "SceneController_StartLoadScene");

        var luaPrefix = Hook("LiveLog_LuaStage_Prefix", module.TypeSystem.Void,
            module.TypeSystem.String, module.TypeSystem.String, new ByReferenceType(module.TypeSystem.Int64));
        luaPrefix.Parameters[0].Name = "luaString"; luaPrefix.Parameters[1].Name = "friendlyName"; State(luaPrefix.Parameters[2]);
        Attributes(luaPrefix, luaEnvironment, "LoadLuaFunction", prefixTemplate.Constructor);
        var luaIl = Body(luaPrefix);
        luaIl.Emit(OpCodes.Ldarg_2); luaIl.Emit(OpCodes.Ldarg_0); luaIl.Emit(OpCodes.Ldarg_1);
        luaIl.Emit(OpCodes.Call, Method(bridge, "StageLua")); luaIl.Emit(OpCodes.Stind_I8); luaIl.Emit(OpCodes.Ret);

        var luaPostfix = Hook("LiveLog_LuaComplete_Postfix", module.TypeSystem.Void, module.TypeSystem.Object, module.TypeSystem.Int64);
        luaPostfix.Parameters[0].Name = "__result"; luaPostfix.Parameters[1].Name = "__state";
        Attributes(luaPostfix, luaEnvironment, "LoadLuaFunction", postfixTemplate.Constructor);
        luaIl = Body(luaPostfix);
        luaIl.Emit(OpCodes.Ldarg_1); luaIl.Emit(OpCodes.Ldarg_0); luaIl.Emit(OpCodes.Call, Method(bridge, "CompleteLua")); luaIl.Emit(OpCodes.Ret);

        var luaFinalizer = Hook("LiveLog_LuaAbort_Finalizer", exceptionType, exceptionType, module.TypeSystem.Int64);
        luaFinalizer.Parameters[0].Name = "__exception"; luaFinalizer.Parameters[1].Name = "__state";
        Attributes(luaFinalizer, luaEnvironment, "LoadLuaFunction", finalizerConstructor);
        luaIl = Body(luaFinalizer);
        var done = Instruction.Create(OpCodes.Ldarg_0);
        luaIl.Emit(OpCodes.Ldarg_0); luaIl.Emit(OpCodes.Brfalse, done);
        luaIl.Emit(OpCodes.Ldarg_1); luaIl.Emit(OpCodes.Call, Method(bridge, "AbortLua"));
        luaIl.Append(done); luaIl.Emit(OpCodes.Ret);
    }
}
