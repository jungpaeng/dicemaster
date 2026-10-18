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
    public PayloadCopier(ModuleDefinition source, ModuleDefinition target, Func<TypeDefinition, bool> select)
    {
        this.target = target;
        foreach (var type in source.Types.Where(t => t.Name != "<Module>" && select(t))) Shell(type, null);
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
        var copy = new MethodReference(m.Name, Type(m.ReturnType), Type(m.DeclaringType)) {
            HasThis = m.HasThis, ExplicitThis = m.ExplicitThis, CallingConvention = m.CallingConvention
        };
        foreach (var p in m.Parameters) copy.Parameters.Add(new ParameterDefinition(Type(p.ParameterType)));
        foreach (var p in m.GenericParameters) copy.GenericParameters.Add(new GenericParameter(p.Name, copy));
        return copy;
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
            else if (value is FieldReference f) value = fields.TryGetValue(f.FullName, out var local) ? local : new FieldReference(f.Name, Type(f.FieldType), Type(f.DeclaringType));
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
