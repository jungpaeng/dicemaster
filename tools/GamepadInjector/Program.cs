using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

static class InjectGamepad
{
    sealed class Copier
    {
        readonly ModuleDefinition target;
        readonly Dictionary<string, TypeDefinition> types = new();
        readonly Dictionary<string, MethodDefinition> methods = new();
        readonly Dictionary<string, FieldDefinition> fields = new();
        readonly List<(TypeDefinition from, TypeDefinition to)> pairs = new();
        public Copier(ModuleDefinition source, ModuleDefinition target)
        {
            this.target = target;
            foreach (var type in source.Types.Where(t => t.Name != "<Module>")) Shell(type, null);
            foreach (var p in pairs)
            {
                p.to.BaseType = Type(p.from.BaseType);
                p.to.PackingSize = p.from.PackingSize; p.to.ClassSize = p.from.ClassSize;
                foreach (var item in p.from.Interfaces) p.to.Interfaces.Add(new InterfaceImplementation(Type(item.InterfaceType)));
                Attributes(p.from.CustomAttributes, p.to.CustomAttributes);
                foreach (var f in p.from.Fields)
                {
                    var n = new FieldDefinition(f.Name, f.Attributes, Type(f.FieldType));
                    if (f.HasConstant) n.Constant = f.Constant;
                    if (f.InitialValue?.Length > 0) n.InitialValue = f.InitialValue.ToArray();
                    if (f.HasLayoutInfo) n.Offset = f.Offset;
                    Attributes(f.CustomAttributes, n.CustomAttributes);
                    p.to.Fields.Add(n); fields[f.FullName] = n;
                }
                foreach (var m in p.from.Methods)
                {
                    if (m.HasGenericParameters) throw new InvalidOperationException("Unexpected generic payload method.");
                    var n = new MethodDefinition(m.Name, m.Attributes, Type(m.ReturnType)) { ImplAttributes = m.ImplAttributes, CallingConvention = m.CallingConvention };
                    foreach (var a in m.Parameters)
                    {
                        var b = new ParameterDefinition(a.Name, a.Attributes, Type(a.ParameterType));
                        if (a.HasConstant) b.Constant = a.Constant;
                        n.Parameters.Add(b);
                    }
                    Attributes(m.CustomAttributes, n.CustomAttributes);
                    if (m.IsPInvokeImpl)
                    {
                        var module = target.ModuleReferences.FirstOrDefault(x => x.Name == m.PInvokeInfo.Module.Name);
                        if (module == null) { module = new ModuleReference(m.PInvokeInfo.Module.Name); target.ModuleReferences.Add(module); }
                        n.PInvokeInfo = new PInvokeInfo(m.PInvokeInfo.Attributes, m.PInvokeInfo.EntryPoint, module);
                    }
                    p.to.Methods.Add(n); methods[m.FullName] = n;
                }
            }
            foreach (var pair in pairs)
            {
                foreach (var property in pair.from.Properties)
                {
                    var copy = new PropertyDefinition(property.Name, property.Attributes, Type(property.PropertyType));
                    if (property.GetMethod != null) copy.GetMethod = methods[property.GetMethod.FullName];
                    if (property.SetMethod != null) copy.SetMethod = methods[property.SetMethod.FullName];
                    Attributes(property.CustomAttributes, copy.CustomAttributes);
                    pair.to.Properties.Add(copy);
                }
                foreach (var method in pair.from.Methods) if (method.HasBody) Body(method, methods[method.FullName]);
            }
        }
        void Shell(TypeDefinition from, TypeDefinition parent)
        {
            var to = new TypeDefinition(from.Namespace, from.Name, from.Attributes);
            if (parent == null) target.Types.Add(to); else parent.NestedTypes.Add(to);
            types[from.FullName] = to; pairs.Add((from, to));
            foreach (var n in from.NestedTypes) Shell(n, to);
        }
        void Attributes(Mono.Collections.Generic.Collection<CustomAttribute> from, Mono.Collections.Generic.Collection<CustomAttribute> to)
        { foreach (var a in from) to.Add(new CustomAttribute(target.ImportReference(a.Constructor), a.GetBlob())); }
        TypeReference Type(TypeReference t)
        {
            if (t == null) return null;
            if (types.TryGetValue(t.FullName, out var local)) return local;
            if (t is ByReferenceType b) return new ByReferenceType(Type(b.ElementType));
            if (t is PointerType p) return new PointerType(Type(p.ElementType));
            if (t is ArrayType a) return new ArrayType(Type(a.ElementType), a.Rank);
            if (t is RequiredModifierType r) return new RequiredModifierType(Type(r.ModifierType), Type(r.ElementType));
            if (t is OptionalModifierType o) return new OptionalModifierType(Type(o.ModifierType), Type(o.ElementType));
            if (t is GenericInstanceType g)
            {
                var n = new GenericInstanceType(Type(g.ElementType));
                foreach (var x in g.GenericArguments) n.GenericArguments.Add(Type(x));
                return n;
            }
            return target.ImportReference(t);
        }
        MethodReference Method(MethodReference m)
        {
            if (m is GenericInstanceMethod g)
            {
                var n = new GenericInstanceMethod(Method(g.ElementMethod));
                foreach (var a in g.GenericArguments) n.GenericArguments.Add(Type(a));
                return n;
            }
            return methods.TryGetValue(m.FullName, out var local) ? local : target.ImportReference(m);
        }
        void Body(MethodDefinition from, MethodDefinition to)
        {
            to.Body = new MethodBody(to) { InitLocals = from.Body.InitLocals, MaxStackSize = from.Body.MaxStackSize };
            var variables = new Dictionary<VariableDefinition, VariableDefinition>();
            foreach (var v in from.Body.Variables) { var n = new VariableDefinition(Type(v.VariableType)); variables[v] = n; to.Body.Variables.Add(n); }
            var instructions = new Dictionary<Instruction, Instruction>();
            foreach (var i in from.Body.Instructions) { var n = Instruction.Create(OpCodes.Nop); n.OpCode = i.OpCode; instructions[i] = n; to.Body.Instructions.Add(n); }
            foreach (var i in from.Body.Instructions)
            {
                object value = i.Operand;
                if (value is Instruction b) value = instructions[b];
                else if (value is Instruction[] branches) value = branches.Select(b => instructions[b]).ToArray();
                else if (value is VariableDefinition v) value = variables[v];
                else if (value is ParameterDefinition p) value = p.Index < 0 ? to.Body.ThisParameter : to.Parameters[p.Index];
                else if (value is TypeReference t) value = Type(t);
                else if (value is MethodReference m) value = Method(m);
                else if (value is FieldReference f) value = fields.TryGetValue(f.FullName, out var local) ? local : target.ImportReference(f);
                else if (value is CallSite) throw new InvalidOperationException("Unexpected calli.");
                instructions[i].Operand = value;
            }
            Instruction Map(Instruction i) => i == null ? null : instructions[i];
            foreach (var h in from.Body.ExceptionHandlers)
                to.Body.ExceptionHandlers.Add(new ExceptionHandler(h.HandlerType) {
                    TryStart = Map(h.TryStart), TryEnd = Map(h.TryEnd), HandlerStart = Map(h.HandlerStart), HandlerEnd = Map(h.HandlerEnd),
                    CatchType = Type(h.CatchType), FilterStart = Map(h.FilterStart)
                });
        }
    }

    static MethodReference Ref(TypeReference owner, string name, TypeReference result, bool instance, params TypeReference[] parameters)
    {
        var method = new MethodReference(name, result, owner) { HasThis = instance };
        foreach (var p in parameters) method.Parameters.Add(new ParameterDefinition(p));
        return method;
    }
    static void AppendCall(MethodDefinition method, MethodReference target)
    {
        var il = method.Body.GetILProcessor();
        foreach (var ret in method.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray())
        {
            // Existing branches target the call too, so none skip the extension.
            ret.OpCode = OpCodes.Call; ret.Operand = target;
            il.InsertAfter(ret, Instruction.Create(OpCodes.Ret));
        }
    }

    // Rebuild against an already-patched DLL without losing unrelated fixes.
    // Reverse only the extension points installed by this tool.
    static void RemoveExisting(ModuleDefinition m)
    {
        if (!m.Types.Any(t => t.Namespace == "DiceMaster.Gamepad")) return;
        var plugin = m.GetType("DiceMaster.DicePlugin");
        var patch = m.GetType("DiceMaster.DicePatch");
        var config = m.GetType("DiceMaster.Config.ConfigManager");
        void RestoreReturn(MethodDefinition method, string owner, string name)
        {
            foreach (var i in method.Body.Instructions.ToArray())
            {
                if (i.OpCode != OpCodes.Call || i.Operand is not MethodReference r || r.DeclaringType.FullName != owner || r.Name != name) continue;
                var next = i.Next;
                if (next == null || next.OpCode != OpCodes.Ret) throw new InvalidOperationException("Modified extension suffix: " + method.FullName);
                i.OpCode = OpCodes.Ret; i.Operand = null; method.Body.Instructions.Remove(next);
            }
        }
        RestoreReturn(plugin.Methods.Single(x => x.Name == "Update"), "DiceMaster.DicePatch", "UpdateGamepad");
        RestoreReturn(plugin.Methods.Single(x => x.Name == "OnGUI"), "DiceMaster.DicePatch", "DrawGamepadHint");
        RestoreReturn(patch.Methods.Single(x => x.Name == "DiceMenuDialog_OnEnable_Postfix"), "DiceMaster.Gamepad.GamepadBridge", "ResetAdjustment");
        var destroy = plugin.Methods.FirstOrDefault(x => x.Name == "OnDestroy");
        if (destroy != null)
        {
            if (destroy.Body.Instructions.Count == 2) plugin.Methods.Remove(destroy);
            else RestoreReturn(destroy, "DiceMaster.Gamepad.GamepadBridge", "Shutdown");
        }
        var create = config.Methods.Single(x => x.Name == "CreateConfigElements");
        var store = create.Body.Instructions.Single(i => i.OpCode == OpCodes.Stsfld && i.Operand is FieldReference f && f.DeclaringType.FullName == config.FullName && f.Name == "EnableGamepadAdjustment");
        int position = create.Body.Instructions.IndexOf(store) - 4;
        if (position < 0 || create.Body.Instructions[position].OpCode != OpCodes.Ldstr || (string)create.Body.Instructions[position].Operand != "EnableGamepadAdjustment")
            throw new InvalidOperationException("Modified gamepad config initializer.");
        for (int i = 0; i < 5; i++) create.Body.Instructions.RemoveAt(position);
        config.Fields.Remove(config.Fields.Single(f => f.Name == "EnableGamepadAdjustment"));
        patch.Methods.Remove(patch.Methods.Single(x => x.Name == "UpdateGamepad"));
        patch.Methods.Remove(patch.Methods.Single(x => x.Name == "DrawGamepadHint"));
        patch.Fields.Remove(patch.Fields.Single(x => x.Name == "_gamepadHintStyle"));
        foreach (var type in m.Types.Where(t => t.Namespace == "DiceMaster.Gamepad").ToArray()) m.Types.Remove(type);
        foreach (var resource in m.Resources.Where(r => r.Name.StartsWith("DiceMaster.Gamepad.Native.")).ToArray()) m.Resources.Remove(resource);
    }

    public static void Main(string[] args)
    {
        if (args.Length != 4) throw new ArgumentException("input.dll payload.dll native-directory output.dll");
        using var assembly = AssemblyDefinition.ReadAssembly(args[0]);
        using var payload = AssemblyDefinition.ReadAssembly(args[1]);
        var m = assembly.MainModule;
        RemoveExisting(m);
        new Copier(payload.MainModule, m);
        foreach (string arch in new[] { "x64", "x86" })
            m.Resources.Add(new EmbeddedResource("DiceMaster.Gamepad.Native.MinHook." + arch + ".dll", ManifestResourceAttributes.Private,
                File.ReadAllBytes(Path.Combine(args[2], "MinHook." + arch + ".dll"))));
        m.Resources.Add(new EmbeddedResource("DiceMaster.Gamepad.Native.LICENSE.txt", ManifestResourceAttributes.Private,
            File.ReadAllBytes(Path.Combine(args[2], "MinHook.LICENSE.txt"))));
        var plugin = m.GetType("DiceMaster.DicePlugin");
        var patch = m.GetType("DiceMaster.DicePatch");
        var config = m.GetType("DiceMaster.Config.ConfigManager");
        var bridge = m.GetType("DiceMaster.Gamepad.GamepadBridge");
        MethodDefinition Bridge(string name) => bridge.Methods.Single(x => x.Name == name);
        var existingEnabled = config.Fields.Single(f => f.Name == "EnableDiceController");
        var enabled = new FieldDefinition("EnableGamepadAdjustment", existingEnabled.Attributes, existingEnabled.FieldType); config.Fields.Add(enabled);
        var create = config.Methods.Single(x => x.Name == "CreateConfigElements");
        var constructor = (MethodReference)create.Body.Instructions.First(i => i.OpCode == OpCodes.Newobj).Operand;
        var createIL = create.Body.GetILProcessor();
        var oldRet = create.Body.Instructions.Last();
        if (oldRet.OpCode != OpCodes.Ret) throw new InvalidOperationException("Unexpected config initializer.");
        oldRet.OpCode = OpCodes.Ldstr; oldRet.Operand = "EnableGamepadAdjustment";
        createIL.Emit(OpCodes.Ldstr, "Xbox 패드로 주사위를 조정합니다. LB+방향키: 수치 조정, LB+B: 자동.");
        createIL.Emit(OpCodes.Ldc_I4_1); createIL.Emit(OpCodes.Newobj, constructor); createIL.Emit(OpCodes.Stsfld, enabled); createIL.Emit(OpCodes.Ret);

        var field = patch.Fields.Single(f => f.Name == "_curInputField");
        var dice = patch.Fields.Single(f => f.Name == "_diceValue");
        var objectType = m.GetTypeReferences().First(t => t.FullName == "UnityEngine.Object");
        var component = m.GetTypeReferences().First(t => t.FullName == "UnityEngine.Component");
        var gameObject = m.GetTypeReferences().First(t => t.FullName == "UnityEngine.GameObject");
        var paths = m.GetTypeReferences().First(t => t.FullName == "BepInEx.Paths");
        var coreScope = objectType.Scope;
        var time = new TypeReference("UnityEngine", "Time", m, coreScope);
        var getter = (MethodReference)plugin.Methods.Single(x => x.Name == "Awake").Body.Instructions
            .First(i => i.OpCode == OpCodes.Callvirt && i.Operand is MethodReference mr && mr.Name == "get_Value").Operand;
        var update = new MethodDefinition("UpdateGamepad", MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig, m.TypeSystem.Void);
        patch.Methods.Add(update); update.Body.InitLocals = true; update.Body.MaxStackSize = 6;
        var visible = new VariableDefinition(m.TypeSystem.Boolean); var next = new VariableDefinition(m.TypeSystem.Int32); var warning = new VariableDefinition(m.TypeSystem.String);
        update.Body.Variables.Add(visible); update.Body.Variables.Add(next); update.Body.Variables.Add(warning);
        var il = update.Body.GetILProcessor();
        var invisible = Instruction.Create(OpCodes.Ldc_I4_0); var check = Instruction.Create(OpCodes.Ldsfld, existingEnabled);
        var afterSet = Instruction.Create(OpCodes.Call, Bridge("ConsumeWarning")); var done = Instruction.Create(OpCodes.Ret);
        var objectExists = Ref(objectType, "op_Inequality", m.TypeSystem.Boolean, false, objectType, objectType);
        il.Emit(OpCodes.Ldsfld, field); il.Emit(OpCodes.Ldnull); il.Emit(OpCodes.Call, objectExists); il.Emit(OpCodes.Brfalse, invisible);
        il.Emit(OpCodes.Ldsfld, field); il.Emit(OpCodes.Callvirt, Ref(component, "get_gameObject", gameObject, true));
        il.Emit(OpCodes.Callvirt, Ref(gameObject, "get_activeInHierarchy", m.TypeSystem.Boolean, true));
        il.Emit(OpCodes.Call, Ref(new TypeReference("UnityEngine", "Application", m, coreScope), "get_isFocused", m.TypeSystem.Boolean, false));
        il.Emit(OpCodes.And); il.Emit(OpCodes.Stloc, visible); il.Emit(OpCodes.Br, check);
        il.Append(invisible); il.Emit(OpCodes.Stloc, visible); il.Append(check); il.Emit(OpCodes.Callvirt, getter);
        il.Emit(OpCodes.Ldsfld, enabled); il.Emit(OpCodes.Callvirt, getter); il.Emit(OpCodes.And);
        il.Emit(OpCodes.Ldloc, visible); il.Emit(OpCodes.Ldsfld, dice);
        il.Emit(OpCodes.Call, Ref(time, "get_unscaledTime", m.TypeSystem.Single, false));
        il.Emit(OpCodes.Call, Ref(paths, "get_CachePath", m.TypeSystem.String, false));
        il.Emit(OpCodes.Call, Bridge("Tick")); il.Emit(OpCodes.Stloc, next);
        il.Emit(OpCodes.Ldloc, next); il.Emit(OpCodes.Ldsfld, dice); il.Emit(OpCodes.Beq, afterSet);
        il.Emit(OpCodes.Ldloc, next); il.Emit(OpCodes.Stsfld, dice);
        il.Emit(OpCodes.Ldsfld, field); il.Emit(OpCodes.Ldnull); il.Emit(OpCodes.Call, objectExists); il.Emit(OpCodes.Brfalse, afterSet);
        il.Emit(OpCodes.Ldsfld, field); il.Emit(OpCodes.Ldloca, next);
        il.Emit(OpCodes.Call, Ref(m.TypeSystem.Int32, "ToString", m.TypeSystem.String, true));
        il.Emit(OpCodes.Callvirt, Ref(field.FieldType, "set_text", m.TypeSystem.Void, true, m.TypeSystem.String));
        il.Append(afterSet); il.Emit(OpCodes.Stloc, warning); il.Emit(OpCodes.Ldloc, warning);
        il.Emit(OpCodes.Call, Ref(m.TypeSystem.String, "IsNullOrEmpty", m.TypeSystem.Boolean, false, m.TypeSystem.String)); il.Emit(OpCodes.Brtrue, done);
        il.Emit(OpCodes.Ldloc, warning); il.Emit(OpCodes.Call, plugin.Methods.Single(x => x.Name == "LogWarning")); il.Append(done);
        AppendCall(plugin.Methods.Single(x => x.Name == "Update"), update);

        var gui = m.GetTypeReferences().First(t => t.FullName == "UnityEngine.GUI");
        var skin = m.GetTypeReferences().First(t => t.FullName == "UnityEngine.GUISkin");
        var style = m.GetTypeReferences().First(t => t.FullName == "UnityEngine.GUIStyle");
        var screen = m.GetTypeReferences().First(t => t.FullName == "UnityEngine.Screen");
        var rect = plugin.Fields.Single(f => f.Name == "logWindowRect").FieldType;
        var hintStyle = new FieldDefinition("_gamepadHintStyle", FieldAttributes.Private | FieldAttributes.Static, style); patch.Fields.Add(hintStyle);
        var draw = new MethodDefinition("DrawGamepadHint", MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig, m.TypeSystem.Void); patch.Methods.Add(draw);
        var dl = draw.Body.GetILProcessor(); var drawNow = Instruction.Create(OpCodes.Call, Ref(screen, "get_width", m.TypeSystem.Int32, false)); var end = Instruction.Create(OpCodes.Ret);
        dl.Emit(OpCodes.Call, Bridge("get_ShowHint")); dl.Emit(OpCodes.Brfalse, end);
        dl.Emit(OpCodes.Ldsfld, hintStyle); dl.Emit(OpCodes.Brtrue, drawNow);
        dl.Emit(OpCodes.Call, Ref(gui, "get_skin", skin, false)); dl.Emit(OpCodes.Callvirt, Ref(skin, "get_label", style, true));
        dl.Emit(OpCodes.Newobj, Ref(style, ".ctor", m.TypeSystem.Void, true, style));
        dl.Emit(OpCodes.Dup); dl.Emit(OpCodes.Ldc_I4, 18); dl.Emit(OpCodes.Callvirt, Ref(style, "set_fontSize", m.TypeSystem.Void, true, m.TypeSystem.Int32));
        dl.Emit(OpCodes.Dup); dl.Emit(OpCodes.Ldc_I4_1); dl.Emit(OpCodes.Callvirt, Ref(style, "set_wordWrap", m.TypeSystem.Void, true, m.TypeSystem.Boolean)); dl.Emit(OpCodes.Stsfld, hintStyle);
        dl.Append(drawNow); dl.Emit(OpCodes.Conv_R4); dl.Emit(OpCodes.Ldc_R4, 600f); dl.Emit(OpCodes.Sub);
        dl.Emit(OpCodes.Call, Ref(screen, "get_height", m.TypeSystem.Int32, false)); dl.Emit(OpCodes.Conv_R4); dl.Emit(OpCodes.Ldc_R4, 100f); dl.Emit(OpCodes.Sub);
        dl.Emit(OpCodes.Ldc_R4, 580f); dl.Emit(OpCodes.Ldc_R4, 90f);
        dl.Emit(OpCodes.Newobj, Ref(rect, ".ctor", m.TypeSystem.Void, true, m.TypeSystem.Single, m.TypeSystem.Single, m.TypeSystem.Single, m.TypeSystem.Single));
        dl.Emit(OpCodes.Call, Bridge("Hint")); dl.Emit(OpCodes.Ldsfld, hintStyle);
        dl.Emit(OpCodes.Call, Ref(gui, "Label", m.TypeSystem.Void, false, rect, m.TypeSystem.String, style)); dl.Append(end);
        AppendCall(plugin.Methods.Single(x => x.Name == "OnGUI"), draw);
        AppendCall(patch.Methods.Single(x => x.Name == "DiceMenuDialog_OnEnable_Postfix"), Bridge("ResetAdjustment"));
        var destroy = plugin.Methods.FirstOrDefault(x => x.Name == "OnDestroy");
        if (destroy == null)
        {
            destroy = new MethodDefinition("OnDestroy", MethodAttributes.Private | MethodAttributes.HideBySig, m.TypeSystem.Void); plugin.Methods.Add(destroy);
            destroy.Body.GetILProcessor().Emit(OpCodes.Ret);
        }
        AppendCall(destroy, Bridge("Shutdown"));
        m.Mvid = Guid.NewGuid();
        assembly.Write(args[3]);
        Console.WriteLine("Embedded Xbox controls, scoped XInput capture, hint UI, and config switch.");
    }
}
