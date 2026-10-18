using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class RealGameApiTests
{
    // Static inspection of the supplied DLLs only. This does not install a
    // Harmony patch, invoke Unity native methods, or establish Windows behavior.
    static void VerifyLogInputContract(AssemblyDefinition candidate, string managedRoot)
    {
        using var input = AssemblyDefinition.ReadAssembly(Path.Combine(managedRoot, "Unity.InputSystem.dll"));
        using var core = AssemblyDefinition.ReadAssembly(Path.Combine(managedRoot, "Mortal.Core.dll"));
        Console.WriteLine("INPUT REFERENCE " + input.Name.FullName + " SHA256="
            + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(managedRoot, "Unity.InputSystem.dll")))));
        var state = input.MainModule.GetType("UnityEngine.InputSystem.InputActionState");
        var dispatch = state.Methods.Single(method => method.Name == "CallActionListeners");
        Check(!dispatch.IsStatic && !dispatch.IsGenericInstance && dispatch.HasBody && dispatch.ReturnType.FullName == "System.Void", "actual action dispatch managed instance boundary");
        Eq(5, dispatch.Parameters.Count, "actual action dispatch arity");
        Eq("System.Int32", dispatch.Parameters[0].ParameterType.FullName, "actual action dispatch action index");
        Eq("UnityEngine.InputSystem.InputActionMap", dispatch.Parameters[1].ParameterType.FullName, "actual action dispatch map");
        Eq("UnityEngine.InputSystem.InputActionPhase", dispatch.Parameters[2].ParameterType.FullName, "actual action dispatch phase");
        Check(dispatch.Parameters[3].ParameterType.IsByReference, "actual action dispatch callback array byref");
        Eq("System.String", dispatch.Parameters[4].ParameterType.FullName, "actual action dispatch callback name");
        Eq(3, Calls(dispatch).Count(call => call.DeclaringType.FullName == "UnityEngine.InputSystem.Utilities.DelegateHelpers"
            && call.Name == "InvokeCallbacksSafe"), "actual dispatch covers global, phase listeners, and action map callbacks");
        var phaseType = input.MainModule.GetType("UnityEngine.InputSystem.InputActionPhase");
        foreach (var expected in new[] { ("Started", 2), ("Performed", 3), ("Canceled", 4) })
            Eq(expected.Item2, Convert.ToInt32(phaseType.Fields.Single(field => field.Name == expected.Item1).Constant), "actual action phase " + expected.Item1);
        foreach (string name in new[] { "Mortal.Core.InputButton", "Mortal.Core.ButtonNavigationInputAction" })
        {
            var enable = core.MainModule.GetType(name).Methods.Single(method => method.Name == "OnEnable");
            Eq(name.EndsWith("InputButton") ? 1 : 2, Calls(enable).Count(call => call.Name == "add_performed"), "game buttons subscribe to performed only " + name);
            Eq(0, Calls(enable).Count(call => call.Name == "add_canceled" || call.Name == "add_started"), "canceled cleanup cannot invoke game buttons " + name);
        }
        var controller = core.MainModule.GetType("Mortal.Core.PlayerInputController");
        Eq("UnityEngine.InputSystem.PlayerInput", controller.Fields.Single(field => field.Name == "_playerInput").FieldType.FullName, "actual current scheme source field");
        var schemeGetter = controller.Methods.Single(method => method.Name == "get_ControlSchemeType");
        Check(Calls(schemeGetter).Any(call => call.Name == "get_currentControlScheme"), "actual mode getter reads currentControlScheme");
        Check(schemeGetter.Body.Instructions.Any(instruction => instruction.OpCode == OpCodes.Ldstr && (string)instruction.Operand == "Gamepad"), "actual Gamepad scheme name");
        Check(schemeGetter.Body.Instructions.Any(instruction => instruction.OpCode == OpCodes.Ldstr && (string)instruction.Operand == "Keyboard&Mouse"), "actual keyboard mouse scheme name");
        var gate = candidate.MainModule.GetType("DiceMaster.LogUI.GameActionGate");
        Check(gate != null, "candidate includes source-owned callback gate");
        Check(Calls(gate.Methods.Single(method => method.Name == "BeforeActionListeners"))
            .Any(call => call.DeclaringType.FullName == "DiceMaster.Gamepad.LogActionPhases" && call.Name == "Allow"), "installed gate calls tested phase policy");
        var bridge = candidate.MainModule.GetType("DiceMaster.Gamepad.GamepadBridge");
        Eq(1, bridge.Methods.Where(method => method.HasBody).SelectMany(method => method.Body.Instructions)
            .Count(instruction => instruction.OpCode == OpCodes.Newobj && instruction.Operand is MethodReference constructor
                && constructor.DeclaringType.FullName == "DiceMaster.Gamepad.NativeXInput"), "log and dice own one shared native hook instance");
        var resolver = candidate.MainModule.GetType("DiceMaster.Gamepad.GameInputModeResolver");
        Check(!resolver.Methods.Where(method => method.HasBody).SelectMany(Calls)
            .Any(call => call.Name == "OnControlsChanged" || call.Name == "set_defaultControlScheme" || call.Name == "SwitchCurrentControlScheme"), "mode resolver never forces game control schemes");
    }
}
