using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Mono.Cecil;
using Mono.Cecil.Cil;

// These checks inspect the delivered assembly, in addition to behavioral model
// and actual bridge IL tests. A standalone, disconnected UI implementation must
// not satisfy the candidate build gate.
static class Program
{
    static int checks;
    static void Check(bool condition, string message)
    { checks++; if (!condition) throw new InvalidDataException(message); }
    static IEnumerable<TypeDefinition> All(IEnumerable<TypeDefinition> types)
    { foreach (var type in types) { yield return type; foreach (var child in All(type.NestedTypes)) yield return child; } }
    static IEnumerable<MethodReference> Calls(MethodDefinition method) => method.HasBody
        ? method.Body.Instructions.Select(i => i.Operand).OfType<MethodReference>() : Enumerable.Empty<MethodReference>();
    static int? IntegerConstant(Instruction instruction)
    {
        while (instruction?.OpCode == OpCodes.Nop) instruction = instruction.Previous;
        if (instruction == null) return null;
        if (instruction.OpCode == OpCodes.Ldc_I4_M1) return -1;
        if (instruction.OpCode.Code >= Code.Ldc_I4_0 && instruction.OpCode.Code <= Code.Ldc_I4_8)
            return (int)instruction.OpCode.Code - (int)Code.Ldc_I4_0;
        return instruction.OpCode == OpCodes.Ldc_I4 || instruction.OpCode == OpCodes.Ldc_I4_S
            ? Convert.ToInt32(instruction.Operand) : null;
    }
    static IEnumerable<MethodDefinition> WithinType(MethodDefinition entry)
    {
        var pending = new Stack<MethodDefinition>();
        var visited = new HashSet<MethodDefinition>();
        pending.Push(entry);
        while (pending.Count > 0)
        {
            var method = pending.Pop();
            if (!visited.Add(method)) continue;
            yield return method;
            foreach (var call in Calls(method).Where(c => c.DeclaringType.FullName == entry.DeclaringType.FullName))
            {
                var target = entry.DeclaringType.Methods.FirstOrDefault(m => m.FullName == call.FullName);
                if (target != null) pending.Push(target);
            }
        }
    }
    // IMGUI actions are delegates stored for the end of the window draw. Their
    // compiler-generated closure methods are part of the delivered UI wiring,
    // too; following only direct methods on LogRenderer would miss them.
    static IEnumerable<MethodDefinition> WithinTypeTree(MethodDefinition entry, IEnumerable<TypeDefinition> types)
    {
        var methods = types.Where(t => t.FullName == entry.DeclaringType.FullName ||
                t.FullName.StartsWith(entry.DeclaringType.FullName + "/", StringComparison.Ordinal))
            .SelectMany(t => t.Methods).ToDictionary(m => m.FullName);
        var pending = new Stack<MethodDefinition>();
        var visited = new HashSet<MethodDefinition>();
        pending.Push(entry);
        while (pending.Count > 0)
        {
            var method = pending.Pop();
            if (!visited.Add(method)) continue;
            yield return method;
            foreach (var call in Calls(method))
                if (methods.TryGetValue(call.FullName, out var target)) pending.Push(target);
        }
    }
    // Walk only the delivered Draw prefix up to layout initialization. Unknown
    // game/UI values take both branches; an unsupported instruction fails the
    // proof conservatively. This checks display guards without executing Unity
    // or reproducing the renderer's geometry/default calculation.
    static bool CanReachInitialization(MethodDefinition draw, MethodDefinition initialize, int screenWidth, int screenHeight) =>
        CanReachCall(draw, initialize, call => call.DeclaringType.FullName == "UnityEngine.Screen"
            ? call.Name == "get_width" ? screenWidth : call.Name == "get_height" ? screenHeight : null : null);

    // Follow the compiled caller's branches with selected known input facts;
    // all other values remain unknown. This is a wiring proof, not a second
    // implementation of popup/log ownership or Unity event processing.
    static bool CanReachCall(MethodDefinition entry, MethodDefinition target,
        Func<MethodReference, int?> callValue, Func<FieldReference, int?> fieldValue = null)
    {
        var instructions = entry.Body.Instructions;
        var pending = new Stack<(int Index, List<int?> Stack, Dictionary<int, int?> Locals)>();
        pending.Push((0, new List<int?>(), new Dictionary<int, int?>()));
        int remaining = 10000;
        while (pending.Count > 0 && remaining-- > 0)
        {
            var state = pending.Pop();
            int index = state.Index;
            var stack = state.Stack;
            var locals = state.Locals;
            int? Pop() { if (stack.Count == 0) return null; int? value = stack[^1]; stack.RemoveAt(stack.Count - 1); return value; }
            void PushPath(int destination) => pending.Push((destination, new List<int?>(stack), new Dictionary<int, int?>(locals)));
            if (index < 0 || index >= instructions.Count) continue;
            Instruction instruction = instructions[index];
            int? left, right, condition;
            int local;
            switch (instruction.OpCode.Code)
            {
                case Code.Nop: break;
                case Code.Ret: case Code.Throw: continue;
                case Code.Ldc_I4_M1: stack.Add(-1); break;
                case Code.Ldc_I4_0: case Code.Ldc_I4_1: case Code.Ldc_I4_2: case Code.Ldc_I4_3: case Code.Ldc_I4_4:
                case Code.Ldc_I4_5: case Code.Ldc_I4_6: case Code.Ldc_I4_7: case Code.Ldc_I4_8:
                    stack.Add((int)instruction.OpCode.Code - (int)Code.Ldc_I4_0); break;
                case Code.Ldc_I4: case Code.Ldc_I4_S: stack.Add(Convert.ToInt32(instruction.Operand)); break;
                case Code.Ldarg: case Code.Ldarg_S: case Code.Ldarg_0: case Code.Ldarg_1: case Code.Ldarg_2: case Code.Ldarg_3:
                case Code.Ldarga: case Code.Ldarga_S: case Code.Ldstr: case Code.Ldnull: stack.Add(null); break;
                case Code.Ldsfld: stack.Add(fieldValue?.Invoke((FieldReference)instruction.Operand)); break;
                case Code.Ldfld: Pop(); stack.Add(fieldValue?.Invoke((FieldReference)instruction.Operand)); break;
                case Code.Ldflda: Pop(); stack.Add(null); break;
                case Code.Stfld: Pop(); Pop(); break;
                case Code.Stsfld: Pop(); break;
                case Code.Pop: Pop(); break;
                case Code.Dup: stack.Add(stack.Count == 0 ? null : stack[^1]); break;
                case Code.Ldloc: case Code.Ldloc_S:
                    stack.Add(locals.GetValueOrDefault(((VariableDefinition)instruction.Operand).Index)); break;
                case Code.Ldloca: case Code.Ldloca_S: stack.Add(null); break;
                case Code.Ldloc_0: case Code.Ldloc_1: case Code.Ldloc_2: case Code.Ldloc_3:
                    stack.Add(locals.GetValueOrDefault((int)instruction.OpCode.Code - (int)Code.Ldloc_0)); break;
                case Code.Stloc: case Code.Stloc_S:
                    locals[((VariableDefinition)instruction.Operand).Index] = Pop(); break;
                case Code.Stloc_0: case Code.Stloc_1: case Code.Stloc_2: case Code.Stloc_3:
                    local = (int)instruction.OpCode.Code - (int)Code.Stloc_0; locals[local] = Pop(); break;
                case Code.Call: case Code.Callvirt:
                    var call = (MethodReference)instruction.Operand;
                    if (call.FullName == target.FullName) return true;
                    for (int parameter = 0; parameter < call.Parameters.Count + (call.HasThis ? 1 : 0); parameter++) Pop();
                    if (call.ReturnType.FullName != "System.Void") stack.Add(callValue(call));
                    break;
                case Code.Newobj:
                    var created = (MethodReference)instruction.Operand;
                    for (int parameter = 0; parameter < created.Parameters.Count; parameter++) Pop();
                    stack.Add(null); break;
                case Code.Initobj: Pop(); break;
                case Code.Br: case Code.Br_S:
                    PushPath(instructions.IndexOf((Instruction)instruction.Operand)); continue;
                case Code.Brfalse: case Code.Brfalse_S: case Code.Brtrue: case Code.Brtrue_S:
                    condition = Pop();
                    bool jumpOnTrue = instruction.OpCode.Code == Code.Brtrue || instruction.OpCode.Code == Code.Brtrue_S;
                    if (!condition.HasValue || (condition.Value != 0) == jumpOnTrue)
                        PushPath(instructions.IndexOf((Instruction)instruction.Operand));
                    if (condition.HasValue && (condition.Value != 0) == jumpOnTrue) continue;
                    break;
                case Code.Beq: case Code.Beq_S: case Code.Bne_Un: case Code.Bne_Un_S:
                case Code.Bgt: case Code.Bgt_S: case Code.Bge: case Code.Bge_S:
                case Code.Blt: case Code.Blt_S: case Code.Ble: case Code.Ble_S:
                    right = Pop(); left = Pop();
                    bool? take = !left.HasValue || !right.HasValue ? null : instruction.OpCode.Code switch {
                        Code.Beq or Code.Beq_S => left == right,
                        Code.Bne_Un or Code.Bne_Un_S => left != right,
                        Code.Bgt or Code.Bgt_S => left > right,
                        Code.Bge or Code.Bge_S => left >= right,
                        Code.Blt or Code.Blt_S => left < right,
                        _ => left <= right };
                    if (!take.HasValue || take.Value) PushPath(instructions.IndexOf((Instruction)instruction.Operand));
                    if (take == true) continue;
                    break;
                case Code.Ceq: case Code.Cgt: case Code.Clt:
                    right = Pop(); left = Pop();
                    stack.Add(!left.HasValue || !right.HasValue ? null : instruction.OpCode.Code switch {
                        Code.Ceq => left == right ? 1 : 0, Code.Cgt => left > right ? 1 : 0, _ => left < right ? 1 : 0 });
                    break;
                case Code.And: case Code.Or:
                    right = Pop(); left = Pop();
                    stack.Add(instruction.OpCode.Code == Code.And
                        ? left == 0 || right == 0 ? 0 : left.HasValue && right.HasValue ? left & right : null
                        : left.HasValue && right.HasValue ? left | right : null);
                    break;
                default: return true;
            }
            PushPath(index + 1);
        }
        return remaining <= 0;
    }
    static void Main(string[] args)
    {
        using var assembly = AssemblyDefinition.ReadAssembly(args.Single());
        var module = assembly.MainModule;
        var types = All(module.Types).ToArray();
        TypeDefinition Type(string name) => types.Single(t => t.FullName == name);
        MethodDefinition Method(string owner, string name) => Type(owner).Methods.Single(m => m.Name == name);
        const string plugin = "DiceMaster.DicePlugin", bridge = "DiceMaster.LiveLog.LogBridge";
        const string renderer = "DiceMaster.LogUI.LogRenderer", pad = "DiceMaster.Gamepad.GamepadBridge";
        var update = Method(plugin, "Update");
        var updateCalls = Calls(update).ToArray();
        int log = Array.FindIndex(updateCalls, c => c.DeclaringType.FullName == pad && c.Name == "TickLog");
        int dice = Array.FindIndex(updateCalls, c => c.DeclaringType.FullName == "DiceMaster.DicePatch" && c.Name == "UpdateGamepad");
        Check(log >= 0 && dice > log, "actual Update must assign log ownership before dice input");
        Check(update.Body.Instructions.Any(i => i.Operand is FieldReference f && f.Name == "LogMenuToggleKey"), "existing configurable open key is used");
        Check(updateCalls.Any(c => c.Name == "GetKeyDown"), "keyboard open entry point retained");
        Check(Calls(Method(plugin, "OnGUI")).Any(c => c.DeclaringType.FullName == renderer && c.Name == "Draw"), "actual OnGUI calls replacement renderer");
        const string runtime = "DiceMaster.EventSelection.EventSelectionRuntime", popup = "DiceMaster.LogUI.EventPopupRenderer";
        const string resetPolicy = "DiceMaster.LogUI.ModalInputResetPolicy", resetEffects = "DiceMaster.LogUI.ModalInputReset";
        bool SuppliesResetReason(MethodDefinition caller, string reason)
        {
            int value = Convert.ToInt32(Type("DiceMaster.LogUI.ModalInputResetReason").Fields.Single(field => field.Name == reason).Constant);
            return caller.Body.Instructions.Any(instruction => instruction.Operand is MethodReference called &&
                called.DeclaringType.FullName == plugin && called.Name == "ResetEventInput" && IntegerConstant(instruction.Previous) == value);
        }
        Check(Calls(Method(plugin, "Awake")).Any(c => c.DeclaringType.FullName == runtime && c.Name == "Install") &&
                updateCalls.Any(c => c.DeclaringType.FullName == runtime && c.Name == "Tick") &&
                Calls(Method(plugin, "OnGUI")).Any(c => c.DeclaringType.FullName == popup && c.Name == "Draw"),
            "delivered runtime installation, visible refresh and popup rendering must be connected to the actual plugin");
        Check(SuppliesResetReason(Method(plugin, "SetLogOpen"), "LogVisibilityChanged") &&
                SuppliesResetReason(Method(plugin, "OnApplicationFocus"), "ApplicationFocusChanged") &&
                updateCalls.Any(c => c.DeclaringType.FullName == plugin && c.Name == "ObserveEventInputMode"),
            "actual log/focus callbacks must supply their hard boundary reasons while Update continues observing automatic scheme changes");
        int runtimeTick = Array.FindIndex(updateCalls, c => c.DeclaringType.FullName == runtime && c.Name == "Tick");
        int[] openBoundaries = updateCalls.Select((call, index) => (call, index))
            .Where(pair => pair.call.DeclaringType.FullName == plugin && pair.call.Name == "SetLogOpen")
            .Select(pair => pair.index).ToArray();
        Check(openBoundaries.Length >= 2 && openBoundaries.All(index => index < runtimeTick),
            "keyboard and native log opening must cancel pending popup confirmation before this frame's runtime tick");
        int? PopupFacts(MethodReference call) => call.DeclaringType.FullName == runtime && call.Name == "get_Waiting" ? 1
            : call.DeclaringType.FullName == "UnityEngine.Application" && call.Name == "get_isFocused" ? 1
            : call.DeclaringType.FullName == "UnityEngine.Input" && call.Name == "GetKeyDown" ? 0
            : call.DeclaringType.FullName == pad && call.Name == "PollLogOpener" ? -1 : null;
        int? ForegroundLog(FieldReference field) => field.DeclaringType.FullName == plugin && field.Name == "showLogWindow" ? 1 : null;
        int? ForegroundPopup(FieldReference field) => field.DeclaringType.FullName == plugin && field.Name == "showLogWindow" ? 0 : null;
        foreach (string name in new[] { "UpdateInput", "Handle", "Scroll" })
        {
            var popupEntry = Method(popup, name);
            Check(!CanReachCall(update, popupEntry, PopupFacts, ForegroundLog) &&
                    CanReachCall(update, popupEntry, PopupFacts, ForegroundPopup),
                "actual Update must suspend hidden popup " + name + " while the foreground log owns input and resume it after closing");
        }
        Check(!CanReachCall(Method(plugin, "OnGUI"), Method(popup, "Draw"), PopupFacts, ForegroundLog) &&
                CanReachCall(Method(plugin, "OnGUI"), Method(popup, "Draw"), PopupFacts, ForegroundPopup),
            "actual OnGUI must render only the foreground log or the waiting popup, preserving exclusive keyboard/pointer ownership");
        var popupDrawMethods = WithinTypeTree(Method(popup, "Draw"), types).ToArray();
        Check(popupDrawMethods.SelectMany(method => method.Body.Instructions).Any(instruction =>
                    instruction.OpCode == OpCodes.Ldstr && (string)instruction.Operand == "log") &&
                popupDrawMethods.SelectMany(Calls).Any(call => call.DeclaringType.FullName == popup && call.Name == "SafeButton") &&
                popupDrawMethods.SelectMany(method => method.Body.Instructions).Any(instruction =>
                    instruction.Operand is FieldReference field && field.DeclaringType.FullName == popup && field.Name == "OpenLog") &&
                WithinTypeTree(Method(plugin, "Awake"), types).Any(method => method.Body.Instructions.Any(instruction =>
                    instruction.OpCode == OpCodes.Stfld && instruction.Operand is FieldReference field &&
                    field.DeclaringType.FullName == popup && field.Name == "OpenLog")) &&
                WithinTypeTree(Method(plugin, "Awake"), types).Any(method => Calls(method).Any(call =>
                    call.DeclaringType.FullName == plugin && call.Name == "SetLogOpen")),
            "the visible native popup log control must invoke the actual plugin visibility boundary rather than a disconnected UI callback");
        Check(Calls(Method(plugin, "SetLogOpen")).Any(c => c.DeclaringType.FullName == runtime && c.Name == "SetLogVisible") &&
                new[] { "OnDisable", "OnDestroy" }.All(name => Calls(Method(plugin, name)).Any(c =>
                    c.DeclaringType.FullName == runtime && c.Name == "SetLogVisible")),
            "log visibility and both shutdown boundaries must update the runtime's explicit foreground-log pause ownership");
        foreach (string name in new[] { "Wait", "WaitSwitch" })
        {
            Check(!CanReachCall(Method(runtime, name), Method(runtime, "BypassForReservation"), _ => null,
                    field => field.DeclaringType.FullName == runtime && new[] { "current", "logVisible" }.Contains(field.Name) ? 1 : null),
                "the actual " + name + " gate must pause an existing decision before a new reservation can resume Lua behind the foreground log");
        }
        Check(Calls(Method(pad, "SetModalCapture")).Any(c => c.DeclaringType.FullName == "DiceMaster.Gamepad.NativeXInput" && c.Name == "set_LogActive") &&
                Calls(Method(pad, "SetModalCapture")).Any(c => c.Name == "RequireNeutral") &&
                types.Where(t => t.FullName == plugin || t.FullName.StartsWith(plugin + "/", StringComparison.Ordinal))
                    .SelectMany(t => t.Methods).SelectMany(Calls).Any(c => c.DeclaringType.FullName == pad && c.Name == "SetModalCapture"),
            "Lua admission must immediately capture the existing native instance with a full neutral barrier");
        Check(Calls(Method(popup, "Submit")).Any(c => c.DeclaringType.FullName == runtime && c.Name == "Select") &&
                Calls(Method(popup, "Submit")).Any(c => c.DeclaringType.FullName == runtime && c.Name == "ContinueRandom") &&
                !Type(popup).Methods.SelectMany(Calls).Any(c => c.DeclaringType.Namespace.StartsWith("Mortal.", StringComparison.Ordinal)),
            "popup explicit commands must queue requests in the runtime without invoking game event methods from rendering");
        var applyReset = Method(plugin, "ResetEventInput");
        var padReset = Method(plugin, "ResetEventPadInput");
        Check(updateCalls.Any(c => c.DeclaringType.FullName == plugin && c.Name == padReset.Name) &&
                Calls(padReset).Any(c => c.DeclaringType.FullName == resetPolicy && c.Name == "FromPadFrame") &&
                padReset.Body.Instructions.Any(i => i.Operand is FieldReference field &&
                    field.DeclaringType.FullName == "DiceMaster.Gamepad.LogPadFrame" && field.Name == "ResetFocus"),
            "actual Update must forward controller frames through the guarded reset-reason translation");
        Check(Calls(applyReset).Any(c => c.DeclaringType.FullName == resetPolicy && c.Name == "For") &&
                Calls(applyReset).Any(c => c.DeclaringType.FullName == runtime && c.Name == "CancelConfirmation") &&
                Calls(applyReset).Any(c => c.DeclaringType.FullName == popup && c.Name == "ResetInput") &&
                new[] { "CancelConfirmation", "PreservePointerTransaction" }.All(name => applyReset.Body.Instructions.Any(i =>
                    i.Operand is FieldReference field && field.DeclaringType.FullName == resetEffects && field.Name == name)),
            "the actual plugin applies both cancellation and pointer preservation from the pure policy tested against candidate IL");
        var observeMode = Method(plugin, "ObserveEventInputMode");
        Check(SuppliesResetReason(observeMode, "AutomaticInputModeObserved") &&
                !Calls(observeMode).Any(c => c.DeclaringType.FullName == runtime && c.Name == "CancelConfirmation"),
            "automatic scheme observation supplies its preserving reason without an independent hard cancellation");
        Check(WithinTypeTree(Method(plugin, "Awake"), types).Any(method => SuppliesResetReason(method, "PopupWaitingChanged")),
            "actual waiting-state callback uses its pointer-only reset rather than cancelling the newly admitted or released flow");
        Check(new[] { "OnDisable", "OnDestroy" }.All(name => Calls(Method(plugin, name)).Any(call =>
                    call.DeclaringType.FullName == runtime && call.Name == "Invalidate")) &&
                !Calls(Method(plugin, "SynchronizeLogContext")).Any(call => call.DeclaringType.FullName == runtime ||
                    call.DeclaringType.FullName == plugin && call.Name == "ResetEventInput"),
            "runtime context and shutdown invalidation stay separate from input reset policy");
        foreach (string owner in new[] { renderer, popup })
        {
            var buttonCalls = Calls(Method(owner, "SafeButton")).ToArray();
            int rectangle = Array.FindIndex(buttonCalls, call => call.DeclaringType.FullName == "UnityEngine.GUILayoutUtility" && call.Name == "GetRect");
            int sharedButton = Array.FindIndex(buttonCalls, call => call.DeclaringType.FullName == "DiceMaster.LogUI.ImGuiPointerButton" && call.Name == "Draw");
            Check(rectangle >= 0 && sharedButton > rectangle && !buttonCalls.Any(call =>
                    call.DeclaringType.FullName == "UnityEngine.GUILayout" && call.Name == "Button"),
                "actual " + owner + " must retain its hit rectangle and delegate native event handling to the shared pointer adapter");
        }
        Check(Calls(Method("DiceMaster.LogUI.ImGuiPointerButton", "Draw")).Count(call =>
                call.DeclaringType.FullName == "UnityEngine.GUI" && call.Name == "Button") == 1,
            "the shared pointer adapter must use one native GUI.Button call for capture and clipped hit testing");
        Check(Calls(Method("DiceMaster.LogUI.ImGuiPointerButton", "Draw")).Any(call =>
                call.DeclaringType.FullName == "UnityEngine.Event" && call.Name == "get_type") &&
                !Calls(Method("DiceMaster.LogUI.ImGuiPointerButton", "Draw")).Any(call =>
                    call.DeclaringType.FullName == "UnityEngine.Event" && call.Name == "get_rawType"),
            "the shared pointer adapter must classify only the effective event so stale rawType never creates a fresh press/release");
        const string cursor = "DiceMaster.LogUI.LogCursorScope";
        Check(new[] { "SetLogOpen", "Update", "OnGUI", "OnApplicationFocus" }.All(name =>
                Calls(Method(plugin, name)).Any(c => c.DeclaringType.FullName == cursor && c.Name == "SetActive")) &&
                new[] { "OnDisable", "OnDestroy" }.All(name =>
                Calls(Method(plugin, name)).Any(c => c.DeclaringType.FullName == cursor && c.Name == "Release")),
            "actual plugin owns temporary cursor access during log visibility/focus and releases on disable/destruction");
        Check(update.Body.Instructions.Any(i => i.Operand is FieldReference field &&
                field.DeclaringType.FullName == "DiceMaster.Gamepad.LogPadFrame" && field.Name == "PreservePointerTransaction") &&
                update.Body.Instructions.Any(i => i.Operand is FieldReference field &&
                field.DeclaringType.FullName == "DiceMaster.Gamepad.LogPadFrame" && field.Name == "PreserveNavigationArea") &&
                updateCalls.Any(c => c.DeclaringType.FullName == renderer && c.Name == "ResetInputFocus" &&
                    c.Parameters.Count == 2 && c.Parameters.All(p => p.ParameterType.FullName == "System.Boolean")),
            "actual TickLog frame carries distinct pointer and navigation preservation reasons to the renderer");
        Check(updateCalls.Any(c => c.DeclaringType.FullName == popup && c.Name == "Scroll"),
            "actual popup receives right-stick detail scrolling from Update");
        const string resultPreview = "DiceMaster.EventSelection.EventResultPreview";
        Check(Calls(Method(runtime, "Refresh")).Any(c => c.DeclaringType.FullName == resultPreview && c.Name == "Read") &&
                !Calls(Method(popup, "Draw")).Any(c => c.DeclaringType.FullName == resultPreview && c.Name == "Read" || c.DeclaringType.FullName == "DiceMaster.LuaParse.LuaParser"),
            "actual result text is prepared outside OnGUI and the popup only consumes prepared descriptions");
        const string candidate = "DiceMaster.EventSelection.EventCandidate";
        const string eligibility = "DiceMaster.EventSelection.EventEligibility";
        const string eligibilitySnapshot = "DiceMaster.EventSelection.EventEligibilitySnapshot";
        const string conditionText = "DiceMaster.EventSelection.PopupConditionText";
        const string switchEligibility = "DiceMaster.EventSelection.SwitchEligibility";
        Check(Type(eligibilitySnapshot).Fields.Any(field => field.Name == "Detail" && field.IsInitOnly && field.FieldType.FullName == "System.String") &&
                Method(candidate, "get_ConditionDetail").Body.Instructions.Any(instruction => instruction.Operand is FieldReference field &&
                    field.DeclaringType.FullName == eligibilitySnapshot && field.Name == "Detail"),
            "delivered event and Switch candidates expose immutable full condition details separately from their list summaries");
        Check(Calls(Method(eligibility, "Evaluate")).Any(call => call.DeclaringType.FullName == eligibility && call.Name == "EvaluateState") &&
                Calls(Method(eligibility, "Evaluate")).Any(call => call.DeclaringType.FullName == conditionText && call.Name == "Event") &&
                Calls(Method(switchEligibility, "ConditionText")).Any(call => call.DeclaringType.FullName == conditionText && call.Name == "Condition"),
            "actual event eligibility and Switch refresh both prepare the common named-condition formatter outside OnGUI");
        Check(new[] { "RefreshSwitch", "PrepareSwitchSelection" }.All(name => Calls(Method(runtime, name)).Any(call =>
                    call.DeclaringType.FullName == switchEligibility && call.Name == "Evaluate")) &&
                Type(bridge).Methods.SelectMany(Calls).Any(call => call.DeclaringType.FullName == switchEligibility && call.Name == "Evaluate") &&
                !Type(runtime).Methods.Any(method => new[] { "SwitchCondition", "SwitchConditionText", "SwitchEligibility" }.Contains(method.Name)),
            "popup refresh, confirmation validation and condition-log publication share one Switch judgment implementation");
        Check(!Calls(Method(switchEligibility, "ConditionText")).Any(call => call.DeclaringType.FullName == "DiceMaster.DataHelper" ||
                    call.DeclaringType.FullName == "DiceMaster.LiveLog.RandomPreview") &&
                !types.Where(type => type.FullName == conditionText || type.FullName.StartsWith(conditionText + "/", StringComparison.Ordinal))
                    .SelectMany(type => type.Methods).SelectMany(Calls).Any(call => call.DeclaringType.FullName == "DiceMaster.LiveLog.RandomPreview" ||
                    call.DeclaringType.FullName == "DiceMaster.DataHelper" ||
                    call.DeclaringType.Namespace.StartsWith("Mortal.", StringComparison.Ordinal) &&
                        new[] { "GetRate", "GetResult", "GetValue", "Check", "Execute" }.Contains(call.Name)),
            "popup condition formatting cannot sample the display RNG or invoke gameplay condition, rate, result or counter methods");
        Check(!types.Where(type => type.FullName == popup || type.FullName.StartsWith(popup + "/", StringComparison.Ordinal))
                .SelectMany(type => type.Methods).SelectMany(Calls).Any(call =>
                    call.DeclaringType.FullName == conditionText || call.DeclaringType.FullName == eligibility ||
                    call.DeclaringType.FullName == "DiceMaster.LiveLog.SafeDisplayEvaluation"),
            "both popup layouts consume prepared metadata without condition reads during Unity layout, repaint or input callbacks");
        Check(Calls(Method(popup, "Draw")).Any(call => call.DeclaringType.FullName == candidate && call.Name == "get_ConditionDetail") &&
                !Calls(Method(popup, "Row")).Any(call => call.DeclaringType.FullName == runtime && call.Name == "get_IsSwitch"),
            "the delivered shared popup consumes complete detail text and uses one row layout for events and Switch branches");
        Check(new[] { "Escape", "PlainText", "FitSummary", "NormalizeRich" }.All(name =>
                !Calls(Method(resultPreview, name)).Any(c => c.DeclaringType.FullName == "DiceMaster.LuaParse.LuaParser" ||
                    c.DeclaringType.FullName == "DiceMaster.DataHelper" || c.DeclaringType.FullName.StartsWith("Mortal."))),
            "OnGUI color formatting helpers never load result scripts or invoke game evaluation");
        Check(Calls(Method("DiceMaster.LogUI.GameActionGate", "ReleaseIfNeutral"))
                .Any(c => c.DeclaringType.FullName == "UnityEngine.Input" && c.Name == "get_touchCount"),
            "actual gameplay release gate waits for touchscreen contact as well as held keyboard/mouse input");
        Check(Calls(Method(plugin, "OnDisable")).Any(c => c.DeclaringType.FullName == pad && c.Name == "SuspendLogInput") &&
                Calls(Method(plugin, "OnDisable")).Any(c => c.DeclaringType.FullName == "DiceMaster.LogUI.GameActionGate" && c.Name == "SetLogState") &&
                Calls(Method(pad, "SuspendLogInput")).Any(c => c.Name == "set_LogActive") &&
                Calls(Method(pad, "SuspendLogInput")).Any(c => c.Name == "CancelLogInput"),
            "component disable must release both gameplay gates through the neutral barrier without disposing hooks needed for re-enable");
        Check(Calls(Method(renderer, "HandleKeyboard")).Any(c => c.DeclaringType.FullName == pad && c.Name == "NotifyKeyboardActivity") &&
                Calls(Method(pad, "NotifyKeyboardActivity")).Any(c => c.DeclaringType.FullName == "DiceMaster.Gamepad.LogPadController" && c.Name == "KeyboardActivity"),
            "actual keyboard navigation must use the synchronous-command takeover path rather than a delayed pointer-focus reset");
        // BeginArea content does not contribute entries to GUILayout.Window's
        // root layout. Inspect the shipped Draw call itself: fixture rendering
        // alone cannot catch Unity shrinking that empty automatic root.
        var draw = Method(renderer, "Draw");
        var fixedWindowParameters = new[] { "System.Int32", "UnityEngine.Rect", "UnityEngine.GUI/WindowFunction", "System.String", "UnityEngine.GUIStyle" };
        Check(draw.Body.Instructions.Where(i => i.OpCode == OpCodes.Call).Select(i => i.Operand).OfType<MethodReference>()
            .Count(c => c.DeclaringType.FullName == "UnityEngine.GUI" && c.Name == "Window" && !c.HasThis
                && c.ReturnType.FullName == "UnityEngine.Rect"
                && c.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(fixedWindowParameters)) == 1,
            "actual renderer Draw must call the five-argument fixed-rect GUI.Window API exactly once");
        Check(!Type(renderer).Methods.SelectMany(Calls).Any(c => c.DeclaringType.FullName == "UnityEngine.GUILayout" && c.Name == "Window"),
            "actual renderer must not call automatic GUILayout.Window: BeginArea leaves its root layout empty");
        // Awake can precede the game's final display resolution. The shipped
        // assembly, not only the portable layout fixture, must defer geometry
        // and config writes until a positive display reaches real rendering.
        var constructor = Type(renderer).Methods.Single(m => m.IsConstructor && !m.IsStatic);
        var constructorCalls = WithinType(constructor).SelectMany(Calls).ToArray();
        Check(!constructorCalls.Any(c => c.DeclaringType.FullName == "UnityEngine.Screen" ||
                c.DeclaringType.FullName == renderer && new[] { "InitializeLayoutIfNeeded", "ConstrainWindow", "PersistLayout" }.Contains(c.Name) ||
                c.DeclaringType.FullName == "DiceMaster.LogUI.LogLayout" && new[] { "IsDefaultSeed", "DefaultWindow", "DefaultFont", "ScreenScale" }.Contains(c.Name) ||
                c.DeclaringType.Namespace == "DiceMaster.Config" && c.Name == "set_Value"),
            "actual renderer constructor must not normalize against startup Screen, initialize layout or write configuration");
        var initialize = Method(renderer, "InitializeLayoutIfNeeded");
        Check(new[] { (0, 1080), (-1, 1080), (1920, 0), (1920, -1) }
                .All(size => !CanReachInitialization(draw, initialize, size.Item1, size.Item2)) &&
                CanReachInitialization(draw, initialize, 1920, 1080),
            "actual Draw must reject nonpositive Screen width and height before reaching layout initialization");
        var initializationCalls = Calls(initialize).ToArray();
        string[] initializationStages = { "IsDefaultSeed", "DefaultWindow", "ConstrainWindow", "PersistLayout" };
        int[] stagePositions = initializationStages.Select(name => Array.FindIndex(initializationCalls, c => c.Name == name)).ToArray();
        var layoutFields = new[] { "LogWindowX", "LogWindowY", "LogWindowWidth", "LogWindowHeight", "LogFontSize" };
        var beforeDefaultRecognition = initialize.Body.Instructions.TakeWhile(i => !(i.Operand is MethodReference call && call.Name == "IsDefaultSeed")).ToArray();
        Check(layoutFields.All(name => beforeDefaultRecognition.Any(i => i.Operand is FieldReference field &&
                field.DeclaringType.FullName == "DiceMaster.Config.ConfigManager" && field.Name == name)) &&
                stagePositions.All(index => index >= 0) && stagePositions.SequenceEqual(stagePositions.OrderBy(index => index)),
            "actual first-draw initialization must read current layout config before default recognition, sizing, constraints and persistence");
        var configRegistration = Method("DiceMaster.Config.ConfigManager", "CreateConfigElements");
        Check(!WithinType(configRegistration).SelectMany(Calls).Any(c => c.DeclaringType.FullName == "UnityEngine.Screen" ||
                c.DeclaringType.FullName == "DiceMaster.LogUI.LogLayout" && new[] { "DefaultWindow", "DefaultFont", "ScreenScale" }.Contains(c.Name)),
            "actual configuration registration must bind resolution-independent seeds without sampling startup Screen");
        // Keep the candidate's publication/rendering/permission boundaries
        // connected. Detailed tab navigation and wrapping are behavioral UI
        // tests; these checks do not model their implementation in IL.
        var choices = Method(renderer, "DrawChoices");
        var uiMethods = types.Where(t => t.Namespace == "DiceMaster.LogUI").SelectMany(t => t.Methods).ToArray();
        Check(!types.Any(t => t.FullName == "DiceMaster.LogUI.LocalSearchKeyboard") &&
                new[] { "set_SearchQuery", "set_AvailableOnly" }.All(name =>
                    Method("DiceMaster.LogUI.LogTabState", name).Body.Instructions.Any(i =>
                        i.OpCode == OpCodes.Ldstr && (string)i.Operand == "이벤트")),
            "event query/filter state must be guarded by the event tab and must not restore the old screen keyboard");
        var popupSetting = Method(renderer, "DrawPopupSetting");
        Check(Calls(Method(renderer, "DrawListDetail")).Any(c => c.FullName == popupSetting.FullName) &&
                Calls(popupSetting).Any(c => c.DeclaringType.FullName == renderer && c.Name == "SafeButton") &&
                !Type(renderer).Methods.SelectMany(Calls).Any(c => c.DeclaringType.FullName == "UnityEngine.GUI" && c.Name == "TextField") &&
                !Type(renderer).Methods.Any(m => new[] { "DrawEventControls", "ToggleEventFilter", "SetEventQuery" }.Contains(m.Name)),
            "condition/event panes must expose the native pointer-safe popup button and remove search/filter controls and actions");
        Check(!WithinType(popupSetting).SelectMany(Calls).Any(c => new[] { "GetRate", "GetResult", "Check", "Execute", "Parse" }.Contains(c.Name)) &&
                Calls(Method(renderer, "get_VisibleRows")).Any(c => c.DeclaringType.FullName == "DiceMaster.LogUI.LogTabState" && c.Name == "get_VisibleRows"),
            "tab popup settings consume only published rows and cannot evaluate or execute game decisions");
        Check(new[] { "EnableEventSelectionPopup", "EnableSwitchSelectionPopup" }.All(option =>
                new[] { "PopupSettingLabel", "TogglePopupSetting", "ToolLabel", "ActivateTool" }.All(name =>
                    Method(renderer, name).Body.Instructions.Any(i => i.Operand is FieldReference field &&
                        field.DeclaringType.FullName == "DiceMaster.Config.ConfigManager" && field.Name == option)) &&
                configRegistration.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldstr && (string)i.Operand == option)) &&
                Calls(Method("DiceMaster.Config.ConfigElement`1", "SetValue")).Any(c => c.DeclaringType.FullName == "DiceMaster.Config.ConfigManager" && c.Name == "SaveConfig"),
            "tab and tool popup settings must use both registered BepInEx options through the existing persistent SaveConfig path");
        const string logTabState = "DiceMaster.LogUI.LogTabState";
        Check(Calls(Method(renderer, "get_SupportsReservation")).Any(c => c.DeclaringType.FullName == logTabState && c.Name == "get_SupportsReservation") &&
                new[] { "조건", "이벤트", "미션" }.All(tab => Method(logTabState, "get_SupportsReservation").Body.Instructions.Any(i => i.OpCode == OpCodes.Ldstr && (string)i.Operand == tab)) &&
                new[] { "BeginReservation", "CanActivate" }.All(name => Calls(Method(logTabState, name)).Any(c => c.Name == "get_SupportsReservation")),
            "renderer and both model authority checks share reservation support for conditions, events and missions");
        Check(choices.Body.Instructions.Any(i => i.Operand is FieldReference field &&
                field.DeclaringType.FullName == "DiceMaster.LogUI.LogItem" && field.Name == "Description") &&
                Calls(choices).Any(c => c.Name == "Label") &&
                !Calls(choices).Any(c => new[] { "SafeButton", "Select", "BeginReservation" }.Contains(c.Name)) &&
                !uiMethods.SelectMany(Calls).Any(c => c.Name == "ToggleExpanded") &&
                !uiMethods.Where(m => m.HasBody).SelectMany(m => m.Body.Instructions)
                    .Any(i => i.Operand is FieldReference field && field.Name == "Expanded"),
            "actual choice rendering must publish its description as read-only visible labels without selection/button or collapse-state references");
        const string display = "DiceMaster.EmbeddedTranslations.Display";
        var title = Method(renderer, "Title");
        Check(Calls(title).Any(c => c.DeclaringType.FullName == display && c.Name == "TranslateLabel") &&
                !title.Body.Instructions.Any(i => i.OpCode == OpCodes.Stfld),
            "delivered list captions must apply the same final display translation as detail labels without mutating published row data");
        var listDetailCalls = Calls(Method(renderer, "DrawListDetail")).ToArray();
        Check(listDetailCalls.Any(c => c.DeclaringType.FullName == renderer && c.Name == "RowReserved") &&
                Calls(Method(renderer, "RowReserved")).Any(c => c.DeclaringType.FullName == bridge && c.Name == "IsReserved") &&
                Method(renderer, "DrawListDetail").Body.Instructions.Any(i => i.OpCode == OpCodes.Ldstr &&
                    i.Operand is string value && value.Contains("예약 중")),
            "actual rendered list captions must mark the real currently reserved target, including rows that are not selected");
        int detailTitle = Array.FindLastIndex(listDetailCalls, c => c.DeclaringType.FullName == renderer && c.Name == "Title");
        var detailHeadingSink = listDetailCalls.Skip(detailTitle + 1).First(c => c.Name == "Label" &&
            new[] { renderer, display, "UnityEngine.GUILayout" }.Contains(c.DeclaringType.FullName));
        Check(listDetailCalls.Count(c => c.DeclaringType.FullName == renderer && c.Name == "Title") == 2 &&
                detailHeadingSink.DeclaringType.FullName == "UnityEngine.GUILayout",
            "actual list/detail must share the title boundary and display its literal-ID fallback without a second translation pass");
        Check(Calls(choices).Any(c => c.DeclaringType.FullName == display && c.Name == "TranslateLabel") &&
                Calls(choices).Any(c => c.DeclaringType.FullName == "UnityEngine.GUILayout" && c.Name == "Label"),
            "delivered read-only choice headings must finalize named text at the display boundary while retaining a literal fallback sink");
        Check(Calls(Method(display, "TranslateLabel")).Any(c => c.DeclaringType.FullName == display && c.Name == "RecordUnknown") &&
                !Calls(Method(display, "Label")).Any(c => c.DeclaringType.FullName == display && c.Name == "RecordUnknown"),
            "delivered display-label finalization must retain unknown-text diagnostics for button, narrative and label consumers without duplicate label logging");
        var eventName = Method("DiceMaster.PositionEventCache", "GetPositionEventName");
        Check(Calls(eventName).Any(c => c.DeclaringType.FullName == display && c.Name == "TranslateLabel") &&
                !eventName.Body.Instructions.Any(i => i.OpCode == OpCodes.Stfld && i.Operand is FieldReference field &&
                    field.DeclaringType.Namespace.StartsWith("Mortal.", StringComparison.Ordinal)),
            "delivered event display-name cache must finalize Korean result labels without rewriting game event identity");
        foreach (string entry in new[] { "LeanLocalizationResolver_GetStoryText_Postfix", "PrintToNarrativeLog" })
        {
            var calls = Calls(Method("DiceMaster.OptionInfoPatch", entry)).ToArray();
            int mapped = Array.FindIndex(calls, c => c.DeclaringType.FullName == "DiceMaster.UiTextMapper" && c.Name == "ApplyTranslations");
            int finalized = Array.FindIndex(calls, c => c.DeclaringType.FullName == display && c.Name == "TranslateLabel");
            Check(mapped >= 0 && finalized > mapped,
                "delivered option summary/narrative entry must finalize display-only labels after the identifier-preserving mapper: " + entry);
        }
        var conditionBlocks = Method(renderer, "DrawConditionDetails");
        Check(Calls(Method(renderer, "DrawListDetail")).Any(c => c.DeclaringType.FullName == renderer && c.Name == "DrawConditionDetails") &&
                Method(renderer, "DrawListDetail").Body.Instructions.Any(i => i.Operand is FieldReference field &&
                    field.DeclaringType.FullName == "DiceMaster.LogUI.LogItem" && field.Name == "ConditionDetails"),
            "delivered condition detail must consume the published structured checkpoint data through its actual render dispatch");
        Check(new[] { "Title", "Context", "Description" }.All(name => conditionBlocks.Body.Instructions.Any(i =>
                    i.Operand is FieldReference field && field.DeclaringType.FullName == "DiceMaster.LogUI.LogConditionBranch" && field.Name == name)) &&
                new[] { "BeginVertical", "EndVertical", "Space" }.All(name => Calls(conditionBlocks).Any(c =>
                    c.DeclaringType.FullName == "UnityEngine.GUILayout" && c.Name == name)) &&
                Calls(conditionBlocks).Any(c => c.DeclaringType.FullName == renderer && c.Name == "SafeButton") &&
                !Calls(conditionBlocks).Any(c => new[] { "Select", "Reserve", "Split" }.Contains(c.Name)),
            "delivered condition branches must render full title/context/body in spaced blocks with identity-based selectable headings and no parsing or render-time reservation");
        Check(Type("DiceMaster.LogUI.LogConditionBranch").Fields.Any(field => field.Name == "Eligibility" && field.IsInitOnly &&
                    field.FieldType.FullName == eligibilitySnapshot) &&
                conditionBlocks.Body.Instructions.Any(instruction => instruction.Operand is FieldReference field &&
                    field.DeclaringType.FullName == "DiceMaster.LogUI.LogConditionBranch" && field.Name == "Eligibility") &&
                Calls(conditionBlocks).Any(call => call.DeclaringType.FullName == renderer && call.Name == "MutedJudgment") &&
                Calls(conditionBlocks).Any(call => call.DeclaringType.FullName == renderer && call.Name == "JudgmentStatus"),
            "condition detail consumes immutable shared Switch judgments for muted known-false titles and explicit unknown/failure labels");
        Check(!Type(renderer).Methods.SelectMany(Calls).Any(call => call.DeclaringType.FullName == switchEligibility ||
                    call.DeclaringType.FullName == conditionText || call.DeclaringType.FullName == eligibility),
            "log rendering consumes prepared condition judgments without evaluating conditions or sampling display values during OnGUI");
        Check(types.Where(type => type.FullName == bridge || type.FullName.StartsWith(bridge + "/", StringComparison.Ordinal))
                    .SelectMany(type => type.Methods).Where(method => method.HasBody).SelectMany(method => method.Body.Instructions).Any(instruction =>
                    instruction.OpCode == OpCodes.Ldstr && (string)instruction.Operand == "GetPositionEventResultDesc") &&
                !types.Where(type => type.FullName == bridge || type.FullName.StartsWith(bridge + "/", StringComparison.Ordinal))
                    .SelectMany(type => type.Methods).Where(method => method.HasBody).SelectMany(method => method.Body.Instructions).Any(instruction =>
                    instruction.OpCode == OpCodes.Ldstr && (string)instruction.Operand == "GetPositionEventDesc") &&
                WithinTypeTree(Method(renderer, "DrawListDetail"), types).SelectMany(method => method.Body.Instructions).Any(instruction =>
                    instruction.Operand is FieldReference field && field.DeclaringType.FullName == eligibilitySnapshot && field.Name == "Detail"),
            "event log displays strict named condition details and result-only cached text without a sampled condition duplicate");
        Check(Method(bridge, "Publish").Body.Instructions.Any(i => i.OpCode == OpCodes.Stfld && i.Operand is FieldReference field &&
                    field.DeclaringType.FullName == "DiceMaster.LogUI.LogItem" && field.Name == "ConditionDetails") &&
                Calls(Method(bridge, "Publish")).Any(c => c.DeclaringType.FullName == bridge && c.Name == "MapConditionDetails") &&
                Calls(Method(bridge, "MapConditionDetails")).Any(c => c.DeclaringType.FullName == "DiceMaster.LogUI.LogConditionDetails" && c.Name == ".ctor"),
            "actual data publication must carry fresh structured condition details into the model instead of leaving a disconnected UI-only DTO");
        var drawWindowCalls = Calls(Method(renderer, "DrawWindow")).ToArray();
        Check(new[] { "get_IsChoiceTab", "DrawChoices", "DrawListDetail" }
                .All(name => drawWindowCalls.Any(c => c.DeclaringType.FullName == renderer && c.Name == name)) &&
                Calls(Method(renderer, "RowState")).Any(c => c.DeclaringType.FullName == renderer && c.Name == "get_HasStoryReadState"),
            "actual renderer must connect choice comparison/split detail dispatch and retain the separate story-read-state boundary");
        // Reservation authority is one fresh operation on an event/mission
        // row or the selected checkpoint's explicit detail branch. Inspect the
        // delivered call graph as well as the portable renderer behavior tests
        // so a disconnected helper cannot satisfy this build gate.
        var rendererType = Type(renderer);
        const string tabState = "DiceMaster.LogUI.LogTabState";
        var armReservation = rendererType.Methods.Single(m => Calls(m).Any(c =>
            c.DeclaringType.FullName == tabState && c.Name == "BeginReservation"));
        var reserve = Method(renderer, "Reserve");
        var safeButton = Method(renderer, "SafeButton");
        Check(new[] { Method(renderer, "DrawListDetail"), armReservation, reserve }.All(method =>
                Calls(method).Any(c => c.DeclaringType.FullName == renderer && c.Name == "get_SupportsReservation")),
            "actual reservation status, one-shot list arming and final activation must all consult reservation support for their tab");
        Check(rendererType.NestedTypes.Single(t => t.Name == "Area").Fields.Any(f => f.Name == "ConditionDetails") &&
                !rendererType.NestedTypes.Single(t => t.Name == "Area").Fields.Any(f => f.Name == "Detail") &&
                !rendererType.Fields.Any(f => f.Name == "detailIndex") &&
                !rendererType.Methods.Any(m => new[] { "EnterDetail", "get_CanFocusDetailControl" }.Contains(m.Name)),
            "delivered renderer adds a checkpoint branch area while preserving removal of the obsolete event/mission detail action path");
        Check(Calls(Method(renderer, "Handle")).Any(c => c.FullName == armReservation.FullName) &&
                Calls(Method(renderer, "Handle")).Any(c => c.FullName == reserve.FullName) &&
                Calls(safeButton).Any(c => c.FullName == armReservation.FullName),
            "actual list A/Enter and selected-row pointer down must connect to the same production one-shot arming boundary");
        var armingCalls = Calls(armReservation).ToArray();
        Check(armingCalls.Any(c => c.DeclaringType.FullName == tabState && c.Name == "get_PublicationFailed") &&
                armingCalls.Any(c => c.DeclaringType.FullName == tabState && c.Name == "get_CanReserveSelected") &&
                armReservation.Body.Instructions.Any(i => i.OpCode == OpCodes.Stfld && i.Operand is FieldReference f &&
                    f.DeclaringType.FullName == tabState && f.Name == "ExecutionFocus"),
            "actual one-shot arming must check published reservation eligibility before granting model execution authority");
        int captureReservation = Array.FindIndex(armingCalls, c => c.DeclaringType.FullName == bridge && c.Name == "TryCaptureReservation");
        int beginReservation = Array.FindIndex(armingCalls, c => c.DeclaringType.FullName == tabState && c.Name == "BeginReservation");
        Check(captureReservation >= 0 && beginReservation > captureReservation &&
                armingCalls[beginReservation].Parameters.Count == 1 &&
                armingCalls[beginReservation].Parameters[0].ParameterType.FullName == "System.Object",
            "actual confirmation/pointer-down must capture the reservation slot before constructing a ticket with the opaque snapshot");
        var ticketType = Type("DiceMaster.LogUI.ReservationTicket");
        Check(ticketType.Fields.Any(f => f.Name == "ReservationState" && f.IsInitOnly && f.FieldType.FullName == "System.Object") &&
                ticketType.Methods.Where(m => m.IsConstructor).SelectMany(m => m.Body.Instructions).Any(i =>
                    i.OpCode == OpCodes.Stfld && i.Operand is FieldReference f && f.Name == "ReservationState"),
            "delivered reservation tickets must retain an immutable snapshot across delayed pointer release");
        var reserveCalls = Calls(reserve).ToArray();
        int validateTicket = Array.FindIndex(reserveCalls, c => c.DeclaringType.FullName == tabState && c.Name == "CanActivate");
        int tryReserve = Array.FindIndex(reserveCalls, c => c.DeclaringType.FullName == bridge && c.Name == "TryToggleReservation");
        int cancelAfterReserve = Array.FindLastIndex(reserveCalls, c => c.DeclaringType.FullName == renderer && c.Name == "CancelPending");
        Check(validateTicket >= 0 && tryReserve > validateTicket && cancelAfterReserve > tryReserve &&
                reserve.Body.ExceptionHandlers.Any(handler => handler.HandlerType == ExceptionHandlerType.Finally &&
                    reserve.Body.Instructions.SkipWhile(i => i != handler.HandlerStart).TakeWhile(i => i != handler.HandlerEnd)
                        .Any(i => i.Operand is MethodReference c && c.DeclaringType.FullName == renderer && c.Name == "CancelPending")) &&
                Calls(Method(renderer, "CancelPending")).Any(c => c.DeclaringType.FullName == tabState && c.Name == "CancelExecution") &&
                reserve.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldfld && i.Operand is FieldReference f &&
                    f.DeclaringType.FullName == ticketType.FullName && f.Name == "ReservationState") &&
                !reserve.Body.Instructions.Any(i => i.OpCode == OpCodes.Stfld && i.Operand is FieldReference f &&
                    f.DeclaringType.FullName == tabState && f.Name == "ExecutionFocus"),
            "actual final toggle must validate the original ticket and captured slot through the current bridge, then consume authority in a finally boundary");
        var uiReservationEntries = uiMethods.Where(m => Calls(m).Any(c => c.DeclaringType.FullName == bridge &&
            new[] { "TryReserve", "TryToggleReservation" }.Contains(c.Name))).ToArray();
        Check(uiReservationEntries.Length == 1 && uiReservationEntries[0].FullName == reserve.FullName &&
                !Calls(Method(renderer, "DrawListDetail")).Any(c => new[] { "Reserve", "BeginReservation" }.Contains(c.Name)) &&
                !Method(renderer, "DrawListDetail").Body.Instructions.Any(i => i.OpCode == OpCodes.Ldstr &&
                    i.Operand is string value && value.StartsWith("reserve:", StringComparison.Ordinal)),
            "actual UI must have one current-data toggle sink and no legacy blind reservation, footer action button or render-time activation");
        var captureApi = Method(bridge, "TryCaptureReservation");
        var toggleApi = Method(bridge, "TryToggleReservation");
        Check(captureApi.ReturnType.FullName == "System.Boolean" && captureApi.Parameters.Count == 2 &&
                captureApi.Parameters[1].ParameterType.FullName == "System.Object&" &&
                toggleApi.ReturnType.FullName == "System.Boolean" && toggleApi.Parameters.Count == 5 &&
                toggleApi.Parameters[^1].ParameterType.FullName == "System.Object" &&
                Method(bridge, "TryReserve").Parameters.Count == 4 &&
                Method(bridge, "TryReserve").ReturnType.FullName == "System.Boolean",
            "delivered bridge must distinguish failed capture from an empty slot, accept an opaque toggle snapshot and retain the legacy reservation API");
        Check(new[] { "MissionCheckData_Postfix", "PositionController_OnPositionClick_Prefix" }
                .All(name => Calls(Method("DiceMaster.OptionInfoPatch", name)).Any(c =>
                    c.DeclaringType.FullName == bridge && c.Name == "NotifyReservationChanged")),
            "actual mission consumption and automatic mission assignment must invalidate captured reservation revisions");
        Check(types.Single(t => t.FullName == "DiceMaster.OptionInfoPatch").Fields.All(f => f.Name != "TargetEvent") &&
                types.Single(t => t.FullName == "DiceMaster.OptionInfoPatch").Methods.All(m => m.Name != "PositionEventData_GetRate_Postfix"),
            "event reservation field and weight override must be absent from the delivered DLL");
        Check(Calls(Method("DiceMaster.OptionInfoPatch", "PositionController_OnPositionClick_Prefix")).Any(c =>
                c.DeclaringType.FullName == bridge && c.Name == "ShouldAutoReserveMission"),
            "actual location-click automatic mission assignment must respect an explicit cancellation in the current context");
        var reservationStatus = Method(renderer, "ReservationStatus");
        Check(Calls(Method(renderer, "DrawListDetail")).Any(c => c.FullName == reservationStatus.FullName) &&
                Calls(reservationStatus).Any(c => c.DeclaringType.FullName == bridge && c.Name == "IsReserved") &&
                Calls(reservationStatus).Any(c => c.DeclaringType.FullName == "DiceMaster.LogUI.LogItem" && c.Name == "get_CanReserve") &&
                Calls(reservationStatus).Any(c => c.DeclaringType.FullName == tabState && c.Name == "get_PublicationFailed") &&
                !WithinType(reservationStatus).SelectMany(Calls).Any(c => new[] { "TryReserve", "TryToggleReservation", "Reserve", "BeginReservation" }.Contains(c.Name)),
            "actual read-only detail status must use real reservation state and current publication eligibility without creating an action");
        var armCallers = types.Where(t => t.FullName == renderer || t.FullName.StartsWith(renderer + "/", StringComparison.Ordinal))
            .SelectMany(t => t.Methods).Where(m => Calls(m).Any(c => c.FullName == armReservation.FullName)).ToArray();
        Check(armCallers.Length == 2 && armCallers.All(m => m.FullName == safeButton.FullName || m.FullName == Method(renderer, "Handle").FullName),
            "pointer release/deferred callbacks must never mint a fresh ticket; only explicit list commands and pointer down may arm");
        var captureCallers = types.Where(t => t.FullName == renderer || t.FullName.StartsWith(renderer + "/", StringComparison.Ordinal))
            .SelectMany(t => t.Methods).Where(m => Calls(m).Any(c => c.FullName == captureApi.FullName)).ToArray();
        Check(captureCallers.Length == 1 && captureCallers[0].FullName == armReservation.FullName &&
                !Calls(reserve).Any(c => c.FullName == captureApi.FullName || c.DeclaringType.FullName == bridge && c.Name == "IsReserved"),
            "release/deferred/final activation must use the original reservation intent rather than recapturing or flipping it from current display state");
        var activateRow = Method(renderer, "ActivateRow");
        Check(WithinTypeTree(Method(renderer, "DrawListDetail"), types).Any(m => m.FullName == activateRow.FullName) &&
                Calls(activateRow).Any(c => c.DeclaringType.FullName == renderer && c.Name == "Select") &&
                Calls(activateRow).Any(c => c.FullName == reserve.FullName) &&
                activateRow.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldfld && i.Operand is FieldReference f &&
                    f.DeclaringType.FullName == renderer && f.Name == "pointerReservation") &&
                !Calls(activateRow).Any(c => c.FullName == armReservation.FullName || c.Name == "BeginReservation"),
            "actual rendered row release callback must select a new row or consume the saved pointer ticket without rearming");
        var pointerInstructions = safeButton.Body.Instructions.ToArray();
        int pointerArm = Array.FindIndex(pointerInstructions, i => i.Operand is MethodReference c && c.FullName == armReservation.FullName);
        int ticketStore = Array.FindIndex(pointerInstructions, pointerArm + 1, i => i.OpCode == OpCodes.Stfld &&
            i.Operand is FieldReference f && f.DeclaringType.FullName == renderer && f.Name == "pointerReservation");
        int deferredAction = Array.FindIndex(pointerInstructions, i => i.OpCode == OpCodes.Stfld &&
            i.Operand is FieldReference f && f.DeclaringType.FullName == renderer && f.Name == "pendingAction");
        Check(pointerArm >= 0 && ticketStore > pointerArm && deferredAction > ticketStore &&
                new[] { "Down", "Up" }.All(name => pointerInstructions.Any(i => i.Operand is FieldReference f &&
                    f.DeclaringType.FullName == "DiceMaster.LogUI.ImGuiPointerButton/Result" && f.Name == name)) &&
                pointerInstructions.Any(i => i.Operand is FieldReference f && f.DeclaringType.FullName == renderer && f.Name == "pointerContext") &&
                pointerInstructions.Any(i => i.Operand is FieldReference f && f.DeclaringType.FullName == renderer && f.Name == "pointerIdentity"),
            "actual row pointer transaction must store its down-time ticket before deferring an identity/context-checked release action");
        Check(Type(plugin).Fields.Any(f => f.FieldType.FullName == renderer), "plugin retains renderer instance across frames");
        Check(Type("DiceMaster.LogUI.LogConditionBranch").Fields.Any(field => field.Name == "Eligibility" && field.IsInitOnly &&
                    field.FieldType.FullName == eligibilitySnapshot) &&
                conditionBlocks.Body.Instructions.Any(instruction => instruction.Operand is FieldReference field &&
                    field.DeclaringType.FullName == "DiceMaster.LogUI.LogConditionBranch" && field.Name == "Eligibility") &&
                Calls(conditionBlocks).Any(call => call.DeclaringType.FullName == renderer && call.Name == "MutedJudgment") &&
                Calls(conditionBlocks).Any(call => call.DeclaringType.FullName == renderer && call.Name == "JudgmentStatus"),
            "condition detail consumes immutable shared Switch judgments for muted known-false titles and explicit unknown/failure labels");
        Check(!Type(renderer).Methods.SelectMany(Calls).Any(call => call.DeclaringType.FullName == switchEligibility ||
                    call.DeclaringType.FullName == conditionText || call.DeclaringType.FullName == eligibility),
            "log rendering consumes prepared condition judgments without evaluating conditions or sampling display values during OnGUI");
        Check(types.Where(type => type.FullName == bridge || type.FullName.StartsWith(bridge + "/", StringComparison.Ordinal))
                    .SelectMany(type => type.Methods).Where(method => method.HasBody).SelectMany(method => method.Body.Instructions).Any(instruction =>
                    instruction.OpCode == OpCodes.Ldstr && (string)instruction.Operand == "GetPositionEventResultDesc") &&
                !types.Where(type => type.FullName == bridge || type.FullName.StartsWith(bridge + "/", StringComparison.Ordinal))
                    .SelectMany(type => type.Methods).Where(method => method.HasBody).SelectMany(method => method.Body.Instructions).Any(instruction =>
                    instruction.OpCode == OpCodes.Ldstr && (string)instruction.Operand == "GetPositionEventDesc") &&
                WithinTypeTree(Method(renderer, "DrawListDetail"), types).SelectMany(method => method.Body.Instructions).Any(instruction =>
                    instruction.Operand is FieldReference field && field.DeclaringType.FullName == eligibilitySnapshot && field.Name == "Detail"),
            "event log displays strict named condition details and result-only cached text without a sampled condition duplicate");
        Check(Method(bridge, "Publish").Body.Instructions.Any(i => i.OpCode == OpCodes.Stfld && i.Operand is FieldReference field &&
                field.DeclaringType.FullName == "DiceMaster.LogUI.LogItem" && field.Name == "IsStatusOnly") &&
                Calls(Method(bridge, "Publish")).Any(c => c.DeclaringType.FullName == "DiceMaster.LogUI.LogSnapshot" && c.Name == ".ctor"),
            "actual publication must classify status-only mission rows before constructing the ordered UI snapshot");
        Check(new[] { "Handle", "DrawListDetail" }.All(name =>
                Calls(Method(renderer, name)).Any(c => c.DeclaringType.FullName == "DiceMaster.LogUI.LogItem" && c.Name == "get_CanSelect")) &&
                new[] { "Select", "get_Selected" }.All(name =>
                Calls(Method("DiceMaster.LogUI.LogTabState", name)).Any(c => c.DeclaringType.FullName == "DiceMaster.LogUI.LogItem" && c.Name == "get_CanSelect")),
            "actual input/rendering and model selection must share the status-only selection guard");
        Check(Calls(Method(bridge, "Publish")).Any(c => c.DeclaringType.Namespace == "DiceMaster.LogUI" && c.Name == "Publish"), "actual data publication reaches structured UI model");
        Check(Type(renderer).Methods.SelectMany(Calls).Any(c => c.DeclaringType.FullName == bridge && c.Name == "TryToggleReservation"), "renderer toggle uses current bridge validation");
        Check(Type(renderer).Methods.SelectMany(Calls).Any(c => c.DeclaringType.FullName == bridge && c.Name == "IsReserved"), "reservation display reads actual reservation source");
        Check(types.SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions)
            .Count(i => i.OpCode == OpCodes.Newobj && i.Operand is MethodReference c && c.DeclaringType.FullName == "DiceMaster.Gamepad.NativeXInput") == 1,
            "log and dice must share one native XInput instance");
        Check(Calls(update).Any(c => c.DeclaringType.FullName == pad && c.Name == "PollLogOpener") &&
                update.Body.Instructions.Any(i => i.Operand is FieldReference field &&
                    field.DeclaringType.FullName == "DiceMaster.Config.ConfigManager" && field.Name == "EnableGamepadLogOpen") &&
                Calls(Method(plugin, "SetLogOpen")).Any(c => c.DeclaringType.FullName == pad && c.Name == "BeginPadLog"),
            "delivered plugin must connect configured native open requests to the actual pad-origin window entry");
        var pollOpen = update.Body.Instructions.Single(instruction => instruction.Operand is MethodReference call &&
            call.DeclaringType.FullName == pad && call.Name == "PollLogOpener");
        Check(((MethodReference)pollOpen.Operand).Parameters.Count == 5 &&
                pollOpen.Previous.Operand is MethodReference popupPermission &&
                popupPermission.DeclaringType.FullName == runtime && popupPermission.Name == "get_Waiting" &&
                update.Body.Instructions.TakeWhile(instruction => instruction != pollOpen).TakeLast(10).Any(instruction =>
                    instruction.Operand is FieldReference field && field.DeclaringType.FullName == plugin && field.Name == "showLogWindow"),
            "native opener admission must distinguish actual foreground-log visibility from explicit waiting-popup capture permission");
        Check(Calls(Method("DiceMaster.Gamepad.XInputHook", "Invoke")).Any(c =>
                    c.DeclaringType.FullName == "DiceMaster.Gamepad.PadInputFilter" && c.Name == "FilterGame") &&
                new[] { "ConfigureLogOpener", "TryConsumeLogOpen" }.All(name => Calls(Method(pad, "PollLogOpener")).Any(c =>
                    c.DeclaringType.FullName == "DiceMaster.Gamepad.NativeXInput" && c.Name == name)),
            "delivered opening requests must be detected on the real game-facing native hook and consumed by the actual bridge");
        var openerFilter = Type("DiceMaster.Gamepad.PadInputFilter");
        var openerMethods = new[] { Method(openerFilter.FullName, "FilterGame"), Method(openerFilter.FullName, "ObserveOpener") };
        Check(Method(openerFilter.FullName, "FilterGame").Parameters.Count == 3 &&
                !openerMethods.SelectMany(Calls).Any(c => new[] { "System.Diagnostics.Stopwatch", "System.DateTime", "System.DateTimeOffset", "System.Threading.Timer" }.Contains(c.DeclaringType.FullName)) &&
                !openerMethods.SelectMany(m => m.Body.Instructions).Select(i => i.Operand).OfType<FieldReference>()
                    .Any(field => new[] { "System.Double", "System.Double[]", "System.Single", "System.Single[]", "System.TimeSpan", "System.DateTime" }.Contains(field.FieldType.FullName)),
            "delivered thumb overlap detection must have no elapsed-time input, clock call or timestamp state; native polling freshness remains a separate boundary");
        Check(Calls(Method(pad, "BeginPadLog")).Any(c => c.DeclaringType.FullName == "DiceMaster.Gamepad.LogPadController" && c.Name == "BeginPadOpen") &&
                Calls(Method("DiceMaster.LogUI.GameActionGate", "BeforeActionListeners")).Any(c => c.DeclaringType.FullName == pad && c.Name == "get_LogOpening"),
            "actual opening device identity and pre-Update queued action capture must reach their production controller/gate boundaries");
        Check(Calls(Method(renderer, "HandleKeyboard")).Any(c => c.DeclaringType.FullName == pad && c.Name == "NotifyPointerMotion") &&
                Calls(Method(pad, "NotifyPointerMotion")).Any(c => c.DeclaringType.FullName == "DiceMaster.Gamepad.LogPadController" && c.Name == "PointerMotion"),
            "actual renderer hover events must use the entry-preserving motion path instead of unconditional pointer takeover");
        var resolver = Type("DiceMaster.Gamepad.GameInputModeResolver");
        Check(!resolver.Methods.SelectMany(Calls).Any(c => c.Name.StartsWith("set_") || c.Name == "OnControlsChanged"), "mode resolver never forces game control scheme");
        Check(module.AssemblyReferences.All(r => r.Name != "Unity.InputSystem"), "read-only mode reflection adds no unpinned assembly reference");
        var resources = module.Resources.OfType<EmbeddedResource>().ToArray();
        Check(resources.Length == 8 && resources.All(r => r.Name.StartsWith("DiceMaster.")), "only eight existing mod resources embedded");
        Check(!resources.Any(r => r.Name.Contains("Mortal.Core") || r.Name.Contains("Mortal.Story")), "game DLLs are not embedded");
        var preservedResources = new Dictionary<string, string> {
            ["DiceMaster.Assets.mortaldice"] = "5cb5c10adea77234bbef4f8f348514526c9765e9c45fe2b4e8d3c334ecfc0ff6",
            ["DiceMaster.Gamepad.Native.LICENSE.txt"] = "d5c2224982b0b95a16b098a561335fa93f3eaf4d0aa964b7843edb986df78dc8",
            ["DiceMaster.Gamepad.Native.MinHook.x64.dll"] = "033e55af8995c3b260e56de73daf99ff4a7ce2fcbe4b3872db2270678d8c6585",
            ["DiceMaster.Gamepad.Native.MinHook.x86.dll"] = "1f28d13b8133551c8896cc556db768c94ebfaae0ad38e86719f3151f7b4ce1fc",
            ["DiceMaster.Translations.ModTranslation.txt"] = "7441c94d83df9f2ebe6ecdfe0489c752afe7ebeadc7b28ae6d15aca460acf5eb",
            ["DiceMaster.Translations.Pairs"] = "172489a3a34fd27d93410b71b23dcfeb6d597723e5d2e0f4878eef464899ec00",
            ["DiceMaster.Translations.TriggerTranslation1.0.5v.txt"] = "a528b4740da8fe1fd5218e50d8113720306ea1db21b959b0ab1545c13587dbd4",
            ["DiceMaster.Translations._Substitutions.txt"] = "640d5fc2a5fae476b766c5b59481a10e7e7d68a605557485f5bf2277e6569909"
        };
        foreach (var resource in resources)
            Check(Convert.ToHexString(SHA256.HashData(resource.GetResourceData())).Equals(preservedResources[resource.Name], StringComparison.OrdinalIgnoreCase),
                "PRD 2 resource bytes preserved: " + resource.Name);
        Console.WriteLine($"PASS: {checks} actual candidate GUI/publication/input wiring and package boundary assertions; Unity/native/Windows execution remains untested.");
    }
}
