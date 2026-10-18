using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Copies source-owned payload types while binding boundary references to the target.
public sealed class PayloadCopier
{
    readonly ModuleDefinition target;
    readonly Dictionary<string, TypeDefinition> types = new();
    readonly Dictionary<string, MethodDefinition> methods = new();
    readonly Dictionary<string, FieldDefinition> fields = new();
    readonly List<(TypeDefinition from, TypeDefinition to)> pairs = new();
    readonly bool merge;
    readonly Func<MethodDefinition, bool> replaceBody;
    public PayloadCopier(ModuleDefinition source, ModuleDefinition target, Func<TypeDefinition, bool> select,
        bool merge = false, Func<MethodDefinition, bool> replaceBody = null)
    {
        this.target = target;
        this.merge = merge;
        this.replaceBody = replaceBody;
        foreach (var type in source.Types.Where(t => t.Name != "<Module>" && select(t))) Shell(type, null);
        foreach (var p in pairs)
        {
            p.to.BaseType = Type(p.from.BaseType);
            p.to.PackingSize = p.from.PackingSize; p.to.ClassSize = p.from.ClassSize;
            foreach (var item in p.from.Interfaces)
                if (!p.to.Interfaces.Any(i => i.InterfaceType.FullName == item.InterfaceType.FullName))
                    p.to.Interfaces.Add(new InterfaceImplementation(Type(item.InterfaceType)));
            if (!merge || p.to.CustomAttributes.Count == 0) Attributes(p.from.CustomAttributes, p.to.CustomAttributes);
            foreach (var f in p.from.Fields)
            {
                var n = merge ? p.to.Fields.SingleOrDefault(x => x.Name == f.Name) : null;
                if (n != null)
                {
                    if (n.FieldType.FullName != f.FieldType.FullName) throw new InvalidOperationException("Payload field type changed: " + f.FullName);
                    n.FieldType = Type(f.FieldType);
                    fields[f.FullName] = n;
                    continue;
                }
                n = new FieldDefinition(f.Name, f.Attributes, Type(f.FieldType));
                if (f.HasConstant) n.Constant = f.Constant;
                if (f.InitialValue?.Length > 0) n.InitialValue = f.InitialValue.ToArray();
                if (f.HasLayoutInfo) n.Offset = f.Offset;
                Attributes(f.CustomAttributes, n.CustomAttributes);
                p.to.Fields.Add(n); fields[f.FullName] = n;
            }
            foreach (var m in p.from.Methods)
            {
                if (m.HasGenericParameters) throw new InvalidOperationException("Unexpected generic payload method.");
                var n = merge ? p.to.Methods.SingleOrDefault(x => x.FullName == m.FullName) : null;
                if (n != null)
                {
                    n.ReturnType = Type(m.ReturnType);
                    for (int index = 0; index < n.Parameters.Count; index++) n.Parameters[index].ParameterType = Type(m.Parameters[index].ParameterType);
                    methods[m.FullName] = n;
                    continue;
                }
                n = new MethodDefinition(m.Name, m.Attributes, Type(m.ReturnType)) { ImplAttributes = m.ImplAttributes, CallingConvention = m.CallingConvention };
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
                var copy = merge ? pair.to.Properties.SingleOrDefault(x => x.Name == property.Name) : null;
                bool added = copy == null;
                copy = copy ?? new PropertyDefinition(property.Name, property.Attributes, Type(property.PropertyType));
                copy.PropertyType = Type(property.PropertyType);
                if (property.GetMethod != null) copy.GetMethod = methods[property.GetMethod.FullName];
                if (property.SetMethod != null) copy.SetMethod = methods[property.SetMethod.FullName];
                if (added) { Attributes(property.CustomAttributes, copy.CustomAttributes); pair.to.Properties.Add(copy); }
            }
            foreach (var method in pair.from.Methods)
                if (method.HasBody && (replaceBody == null || replaceBody(method))) Body(method, methods[method.FullName]);
        }
    }
    void Shell(TypeDefinition from, TypeDefinition parent)
    {
        var to = merge ? target.GetType(from.FullName) : null;
        if (to == null)
        {
            to = new TypeDefinition(from.Namespace, from.Name, from.Attributes);
            if (parent == null) target.Types.Add(to); else parent.NestedTypes.Add(to);
        }
        types[from.FullName] = to; pairs.Add((from, to));
        foreach (var n in from.NestedTypes) Shell(n, to);
    }
    // Install a small explicit boundary from the same source module. Existing
    // signatures and Harmony metadata remain owned by the destination.
    public MethodDefinition CopyMethod(MethodDefinition from, TypeDefinition owner)
    {
        var to = owner.Methods.SingleOrDefault(m => m.FullName == from.FullName);
        if (to == null)
        {
            to = new MethodDefinition(from.Name, from.Attributes, Type(from.ReturnType)) {
                ImplAttributes = from.ImplAttributes, CallingConvention = from.CallingConvention
            };
            foreach (var parameter in from.Parameters)
                to.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, Type(parameter.ParameterType)));
            owner.Methods.Add(to);
        }
        methods[from.FullName] = to;
        to.ReturnType = Type(from.ReturnType);
        for (int index = 0; index < to.Parameters.Count; index++) to.Parameters[index].ParameterType = Type(from.Parameters[index].ParameterType);
        Body(from, to);
        return to;
    }
    public void RebindReferences()
    {
        IEnumerable<TypeDefinition> Walk(IEnumerable<TypeDefinition> roots)
        {
            foreach (var type in roots) { yield return type; foreach (var nested in Walk(type.NestedTypes)) yield return nested; }
        }
        foreach (var type in Walk(target.Types))
        {
            type.BaseType = Type(type.BaseType);
            foreach (var field in type.Fields) field.FieldType = Type(field.FieldType);
            foreach (var property in type.Properties) property.PropertyType = Type(property.PropertyType);
            foreach (var method in type.Methods)
            {
                method.ReturnType = Type(method.ReturnType);
                foreach (var parameter in method.Parameters) parameter.ParameterType = Type(parameter.ParameterType);
                if (!method.HasBody) continue;
                foreach (var local in method.Body.Variables) local.VariableType = Type(local.VariableType);
                foreach (var handler in method.Body.ExceptionHandlers) handler.CatchType = Type(handler.CatchType);
                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.Operand is MethodReference called) instruction.Operand = Method(called);
                    else if (instruction.Operand is FieldReference field) instruction.Operand = Field(field);
                    else if (instruction.Operand is TypeReference referenced) instruction.Operand = Type(referenced);
                }
            }
        }
    }
    void Attributes(Mono.Collections.Generic.Collection<CustomAttribute> from, Mono.Collections.Generic.Collection<CustomAttribute> to)
    { foreach (var a in from) to.Add(new CustomAttribute(target.ImportReference(a.Constructor), a.GetBlob())); }
    TypeReference Type(TypeReference t)
    {
        if (t == null) return null;
        if (types.TryGetValue(t.FullName, out var local)) return local;
        if (t is GenericParameter) return t;
        var existing = target.GetType(t.FullName);
        if (existing != null) return existing;
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
        if (methods.TryGetValue(m.FullName, out var local)) return local;
        var targetMethod = target.GetType(m.DeclaringType.FullName)?.Methods.SingleOrDefault(method => method.FullName == m.FullName);
        if (targetMethod != null) return targetMethod;
        // A net48 compiler can lower a large string switch to this pure
        // compiler helper outside the selected namespace. Copy the exact
        // required helper rather than leaving an unresolved payload reference.
        if (m.DeclaringType.FullName == "<PrivateImplementationDetails>" && m.Name == "ComputeStringHash")
        {
            var original = m.Resolve();
            var owner = target.GetType(m.DeclaringType.FullName);
            var existing = owner?.Methods.SingleOrDefault(method => method.FullName == m.FullName);
            if (existing != null) { methods[m.FullName] = existing; return existing; }
            if (owner == null)
            {
                owner = new TypeDefinition(original.DeclaringType.Namespace, original.DeclaringType.Name, original.DeclaringType.Attributes,
                    Type(original.DeclaringType.BaseType));
                target.Types.Add(owner);
            }
            return CopyMethod(original, owner);
        }
        var copy = new MethodReference(m.Name, Type(m.ReturnType), Type(m.DeclaringType)) {
            HasThis = m.HasThis, ExplicitThis = m.ExplicitThis, CallingConvention = m.CallingConvention
        };
        foreach (var p in m.Parameters) copy.Parameters.Add(new ParameterDefinition(Type(p.ParameterType)));
        foreach (var p in m.GenericParameters) copy.GenericParameters.Add(new GenericParameter(p.Name, copy));
        return copy;
    }
    FieldReference Field(FieldReference field)
    {
        if (fields.TryGetValue(field.FullName, out var mapped)) return mapped;
        var existing = target.GetType(field.DeclaringType.FullName)?.Fields.SingleOrDefault(item => item.FullName == field.FullName);
        return existing ?? new FieldReference(field.Name, Type(field.FieldType), Type(field.DeclaringType));
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
            else if (value is FieldReference f) value = Field(f);
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
