using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mono.Cecil;
using Mono.Cecil.Cil;

// This deliberately stricter comparison supplements the alpha.3 source-recovery
// contract. It compares every generated and named method's IL, with no semantic
// rewrites. The sole content change allowed from the Windows-tested alpha.4 is
// the exact, previously reviewed three-comment translation resource cleanup.
internal static class Program
{
    const string Alpha4Hash = "24c27953e4156b6c5ce9090947fd25a34aabc08a979fe6b2b0a525ae39257fe1";
    const string Translation = "DiceMaster.Translations.ModTranslation.txt";
    const string OldTranslationHash = "6fc71ac1780be28b78e5ee193835a38e47d1526cbb9a143d05ff5206eceef1ec";
    const string NewTranslationHash = "7441c94d83df9f2ebe6ecdfe0489c752afe7ebeadc7b28ae6d15aca460acf5eb";
    static readonly JsonSerializerOptions Json = new JsonSerializerOptions { WriteIndented = true };

    static string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
    static string Serialize(object value) => JsonSerializer.Serialize(value);
    static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
    static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> roots)
    {
        foreach (var type in roots) { yield return type; foreach (var child in Types(type.NestedTypes)) yield return child; }
    }
    static string Scope(IMetadataScope scope) => scope is ModuleDefinition module
        ? module.Assembly.Name.FullName + "/" + module.Name : scope?.ToString();
    static string Type(TypeReference type)
    {
        if (type == null) return null;
        if (type is GenericParameter parameter) return parameter.Type + ":" + parameter.Position;
        if (type is GenericInstanceType generic) return Type(generic.ElementType) + "<" + string.Join(",", generic.GenericArguments.Select(Type)) + ">";
        if (type is RequiredModifierType required) return Type(required.ElementType) + " modreq(" + Type(required.ModifierType) + ")";
        if (type is OptionalModifierType optional) return Type(optional.ElementType) + " modopt(" + Type(optional.ModifierType) + ")";
        if (type is TypeSpecification specification) return type.FullName + ":element=" + Type(specification.ElementType);
        return type.FullName + "@" + Scope(type.Scope);
    }
    static string MethodReference(MethodReference method)
    {
        if (method == null) return null;
        string value = Type(method.DeclaringType) + "::" + method.Name + "`" + method.GenericParameters.Count +
            "(" + string.Join(",", method.Parameters.Select(parameter => Type(parameter.ParameterType))) + "):" + Type(method.ReturnType) +
            ":this=" + method.HasThis + ":explicit=" + method.ExplicitThis + ":calling=" + method.CallingConvention;
        if (method is GenericInstanceMethod generic) value += "<" + string.Join(",", generic.GenericArguments.Select(Type)) + ">";
        return value;
    }
    static string Value(object value)
    {
        if (value is TypeReference type) return Type(type);
        if (value is CustomAttributeArgument argument) return Type(argument.Type) + "=" + Value(argument.Value);
        if (value is CustomAttributeArgument[] arguments) return "[" + string.Join(",", arguments.Select(item => Value(item))) + "]";
        if (value is byte[] bytes) return Convert.ToHexString(bytes);
        if (value is float single) return "float-bits:" + BitConverter.SingleToInt32Bits(single).ToString("X8", CultureInfo.InvariantCulture);
        if (value is double number) return "double-bits:" + BitConverter.DoubleToInt64Bits(number).ToString("X16", CultureInfo.InvariantCulture);
        if (value is IFormattable formatted) return value.GetType().FullName + ":" + formatted.ToString(null, CultureInfo.InvariantCulture);
        return Serialize(value);
    }
    static string Attributes(ICustomAttributeProvider owner) => Serialize(owner.CustomAttributes.Select(attribute => new {
        constructor = MethodReference(attribute.Constructor), arguments = attribute.ConstructorArguments.Select(item => Value(item)).ToArray(),
        fields = attribute.Fields.Select(item => item.Name + "=" + Value(item.Argument)).ToArray(),
        properties = attribute.Properties.Select(item => item.Name + "=" + Value(item.Argument)).ToArray()
    }).ToArray());
    static string Security(ISecurityDeclarationProvider owner) => Serialize(owner.SecurityDeclarations.Select(item =>
        item.Action + ":" + Convert.ToHexString(item.GetBlob())).ToArray());
    static string Marshal(MarshalInfo marshal) => marshal == null ? null : marshal.GetType().FullName + ":" + Serialize(marshal.GetType().GetProperties()
        .Where(property => property.CanRead && property.GetIndexParameters().Length == 0).OrderBy(property => property.Name, StringComparer.Ordinal)
        .Select(property => property.Name + "=" + Value(property.GetValue(marshal))).ToArray());
    static string Generics(IGenericParameterProvider owner) => Serialize(owner.GenericParameters.Select(parameter => new {
        parameter.Name, parameter.Position, flags = parameter.Attributes.ToString(), attributes = Attributes(parameter),
        constraints = parameter.Constraints.Select(constraint => Type(constraint.ConstraintType) + ":" + Attributes(constraint)).ToArray()
    }).ToArray());
    static string Parameter(ParameterDefinition parameter) => Serialize(new {
        parameter.Name, parameter.Index, type = Type(parameter.ParameterType), flags = parameter.Attributes.ToString(),
        parameter.HasConstant, constant = parameter.HasConstant ? Value(parameter.Constant) : null,
        marshal = Marshal(parameter.MarshalInfo), attributes = Attributes(parameter)
    });
    static string Body(MethodDefinition method)
    {
        if (!method.HasBody) return null;
        var body = method.Body;
        var indices = body.Instructions.Select((instruction, index) => new { instruction, index }).ToDictionary(item => item.instruction, item => item.index);
        int Target(Instruction instruction) => instruction == null ? -1 : indices[instruction];
        string Operand(object operand)
        {
            if (operand is Instruction instruction) return "target:" + Target(instruction);
            if (operand is Instruction[] targets) return "targets:" + string.Join(",", targets.Select(Target));
            if (operand is VariableDefinition local) return "local:" + local.Index;
            if (operand is ParameterDefinition parameter) return "arg:" + parameter.Index;
            if (operand is MethodReference reference) return "method:" + MethodReference(reference);
            if (operand is FieldReference field) return "field:" + Type(field.DeclaringType) + "::" + field.Name + ":" + Type(field.FieldType);
            if (operand is TypeReference type) return "type:" + Type(type);
            if (operand is CallSite site) return "callsite:" + site.HasThis + ":" + site.ExplicitThis + ":" + site.CallingConvention + ":" +
                Type(site.ReturnType) + "(" + string.Join(",", site.Parameters.Select(parameter => Type(parameter.ParameterType))) + ")";
            return Value(operand);
        }
        return Serialize(new {
            body.InitLocals, body.MaxStackSize,
            locals = body.Variables.Select(local => Type(local.VariableType)).ToArray(),
            instructions = body.Instructions.Select(instruction => instruction.OpCode.Code + "|" + Operand(instruction.Operand)).ToArray(),
            handlers = body.ExceptionHandlers.Select(handler => new {
                kind = handler.HandlerType.ToString(), catchType = Type(handler.CatchType),
                tryStart = Target(handler.TryStart), tryEnd = Target(handler.TryEnd), handlerStart = Target(handler.HandlerStart),
                handlerEnd = Target(handler.HandlerEnd), filterStart = Target(handler.FilterStart)
            }).ToArray()
        });
    }
    static Dictionary<string, string> Inventory(AssemblyDefinition assembly)
    {
        var inventory = new Dictionary<string, string>(StringComparer.Ordinal);
        void Add(string key, object value) { Require(inventory.TryAdd(key, Serialize(value)), "Duplicate inventory entry: " + key); }
        Add("assembly", new { assembly.Name.FullName, flags = assembly.Name.Attributes.ToString(), assembly.Name.Culture,
            algorithm = assembly.Name.HashAlgorithm.ToString(), key = Value(assembly.Name.PublicKey), attributes = Attributes(assembly), security = Security(assembly) });
        Add("modules", assembly.Modules.Select(module => module.Name).ToArray());
        foreach (var module in assembly.Modules)
        {
            string prefix = module.Name + "/";
            Add(prefix + "module", new { module.Name, kind = module.Kind.ToString(), architecture = module.Architecture.ToString(),
                flags = module.Attributes.ToString(), characteristics = module.Characteristics.ToString(), module.RuntimeVersion,
                entryPoint = MethodReference(module.EntryPoint), attributes = Attributes(module) });
            Add(prefix + "assembly-references", module.AssemblyReferences.Select(reference => new { reference.FullName,
                flags = reference.Attributes.ToString(), algorithm = reference.HashAlgorithm.ToString(), hash = Value(reference.Hash),
                key = Value(reference.PublicKey), token = Value(reference.PublicKeyToken) }).ToArray());
            Add(prefix + "module-references", module.ModuleReferences.Select(reference => reference.Name).ToArray());
            Add(prefix + "type-references", module.GetTypeReferences().Select(Type).ToArray());
            Add(prefix + "member-references", module.GetMemberReferences().Select(reference => reference is MethodReference method
                ? MethodReference(method) : reference is FieldReference field ? Type(field.DeclaringType) + "::" + field.Name + ":" + Type(field.FieldType) : reference.FullName).ToArray());
            Add(prefix + "exported-types", module.ExportedTypes.Select(type => new { type.FullName, flags = type.Attributes.ToString(),
                type.Identifier, scope = Scope(type.Scope), declaringType = type.DeclaringType?.FullName }).ToArray());
            Add(prefix + "type-order", Types(module.Types).Select(type => type.FullName).ToArray());
            foreach (var type in Types(module.Types))
            {
                string key = prefix + "type:" + type.FullName;
                Add(key, new { type.FullName, flags = type.Attributes.ToString(), baseType = Type(type.BaseType),
                    type.PackingSize, type.ClassSize, attributes = Attributes(type), security = Security(type), generics = Generics(type),
                    interfaces = type.Interfaces.Select(item => Type(item.InterfaceType) + ":" + Attributes(item)).ToArray() });
                Add(key + "/field-order", type.Fields.Select(field => field.FullName).ToArray());
                foreach (var field in type.Fields) Add(key + "/field:" + field.Name, new { field.Name, fieldType = Type(field.FieldType),
                    flags = field.Attributes.ToString(), field.Offset, field.HasConstant, constant = field.HasConstant ? Value(field.Constant) : null,
                    initial = Value(field.InitialValue), marshal = Marshal(field.MarshalInfo), attributes = Attributes(field) });
                Add(key + "/method-order", type.Methods.Select(method => method.FullName).ToArray());
                foreach (var method in type.Methods) Add(key + "/method:" + method.FullName, new {
                    reference = MethodReference(method), flags = method.Attributes.ToString(), implementation = method.ImplAttributes.ToString(),
                    semantics = method.SemanticsAttributes.ToString(), attributes = Attributes(method), security = Security(method), generics = Generics(method),
                    overrides = method.Overrides.Select(MethodReference).ToArray(), parameters = method.Parameters.Select(Parameter).ToArray(),
                    returnType = Type(method.ReturnType), returnFlags = method.MethodReturnType.Attributes.ToString(),
                    returnAttributes = Attributes(method.MethodReturnType), returnMarshal = Marshal(method.MethodReturnType.MarshalInfo),
                    returnHasConstant = method.MethodReturnType.HasConstant,
                    returnConstant = method.MethodReturnType.HasConstant ? Value(method.MethodReturnType.Constant) : null,
                    pinvoke = method.PInvokeInfo == null ? null : method.PInvokeInfo.Module.Name + ":" + method.PInvokeInfo.EntryPoint + ":" + method.PInvokeInfo.Attributes,
                    body = Body(method)
                });
                Add(key + "/properties", type.Properties.Select(property => new { property.FullName,
                    flags = property.Attributes.ToString(), property.HasConstant, constant = property.HasConstant ? Value(property.Constant) : null,
                    attributes = Attributes(property), getter = MethodReference(property.GetMethod), setter = MethodReference(property.SetMethod),
                    others = property.OtherMethods.Select(MethodReference).ToArray(), parameters = property.Parameters.Select(Parameter).ToArray() }).ToArray());
                Add(key + "/events", type.Events.Select(item => new { item.FullName, eventType = Type(item.EventType), flags = item.Attributes.ToString(),
                    attributes = Attributes(item), add = MethodReference(item.AddMethod), remove = MethodReference(item.RemoveMethod), invoke = MethodReference(item.InvokeMethod),
                    others = item.OtherMethods.Select(MethodReference).ToArray() }).ToArray());
            }
        }
        return inventory;
    }
    static object[] CompareResources(AssemblyDefinition before, AssemblyDefinition after)
    {
        Require(before.MainModule.Resources.Count == 8 && after.MainModule.Resources.Count == 8, "Expected exactly eight resources.");
        var reports = new List<object>();
        foreach (var pair in before.MainModule.Resources.Zip(after.MainModule.Resources))
        {
            Require(pair.First is EmbeddedResource && pair.Second is EmbeddedResource, "Only embedded resources are allowed.");
            var a = (EmbeddedResource)pair.First; var b = (EmbeddedResource)pair.Second;
            Require(a.Name == b.Name && a.Attributes == b.Attributes, "Resource name/order/visibility changed: " + a.Name);
            byte[] left = a.GetResourceData(), right = b.GetResourceData();
            string oldHash = Hash(left), newHash = Hash(right);
            if (a.Name == Translation)
            {
                Require(oldHash == OldTranslationHash && newHash == NewTranslationHash, "Translation resource is not the exact reviewed three-comment change.");
                string[] lines = new UTF8Encoding(false, true).GetString(left).Split(new[] { "\r\n" }, StringSplitOptions.None);
                Require(lines[0].StartsWith("//", StringComparison.Ordinal) && lines[37].StartsWith("//", StringComparison.Ordinal) &&
                    lines[88].StartsWith("//", StringComparison.Ordinal), "Expected comment lines are absent.");
                lines[0] = "// 활협전 내장 UI 번역";
                lines[37] = "// DiceMaster 플래그 설명";
                lines[88] = "// 플래그 참고문 번역";
                Require(Encoding.UTF8.GetBytes(string.Join("\r\n", lines)).SequenceEqual(right), "Bytes other than the three comments changed.");
            }
            else Require(left.SequenceEqual(right), "Embedded resource changed: " + a.Name);
            reports.Add(new { name = a.Name, flags = a.Attributes.ToString(), alpha4_sha256 = oldHash,
                candidate_sha256 = newHash, comparison = a.Name == Translation ? "exact reviewed three-comment replacement; every other byte preserved" : "byte-identical" });
        }
        return reports.ToArray();
    }
    static Dictionary<string, object> Compare(AssemblyDefinition before, AssemblyDefinition after)
    {
        var expected = Inventory(before); var actual = Inventory(after);
        Require(expected.Count == actual.Count, "Metadata/IL inventory size changed.");
        foreach (var pair in expected)
            Require(actual.TryGetValue(pair.Key, out string value) && pair.Value == value, "Metadata/IL changed: " + pair.Key);
        var resources = CompareResources(before, after);
        var types = Types(before.MainModule.Types).ToArray();
        return new Dictionary<string, object> {
            ["status"] = "pass", ["metadata_and_il_inventory_entries"] = expected.Count,
            ["all_types_including_compiler_generated"] = types.Length,
            ["all_methods_including_compiler_generated"] = types.Sum(type => type.Methods.Count),
            ["method_bodies"] = types.SelectMany(type => type.Methods).Count(method => method.HasBody),
            ["pinvoke_methods"] = types.SelectMany(type => type.Methods).Count(method => method.IsPInvokeImpl),
            ["harmony_attributes"] = types.Sum(type => type.CustomAttributes.Count(attribute => attribute.AttributeType.Namespace == "HarmonyLib") +
                type.Methods.Sum(method => method.CustomAttributes.Count(attribute => attribute.AttributeType.Namespace == "HarmonyLib"))),
            ["runtime_inventory_sha256"] = Hash(Encoding.UTF8.GetBytes(Serialize(expected.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray()))),
            ["resources"] = resources,
            ["comparison_exclusions"] = new[] { "module MVID", "PE container headers/timestamps/checksum/section offsets", "metadata token/RVA/heap locations", "debug symbol/container data" },
            ["scope"] = "Static runtime equivalence to the exact alpha.4 DLL; this does not claim a separate Windows run of the candidate. All custom attributes, including informational version, are compared."
        };
    }
    static string[] SelfTest(string baseline, string candidate)
    {
        var cases = new (string Name, Action<AssemblyDefinition> Mutate)[] {
            ("managed IL", assembly => Types(assembly.MainModule.Types).SelectMany(type => type.Methods).First(method => method.HasBody).Body.Instructions.Insert(0, Instruction.Create(OpCodes.Nop))),
            ("Harmony attribute", assembly => { var owner = Types(assembly.MainModule.Types).SelectMany(type => type.Methods).First(method => method.CustomAttributes.Any(attribute => attribute.AttributeType.Namespace == "HarmonyLib")); owner.CustomAttributes.Remove(owner.CustomAttributes.First(attribute => attribute.AttributeType.Namespace == "HarmonyLib")); }),
            ("field layout", assembly => Types(assembly.MainModule.Types).SelectMany(type => type.Fields).First().Offset = 123),
            ("native entry point", assembly => Types(assembly.MainModule.Types).SelectMany(type => type.Methods).First(method => method.IsPInvokeImpl).PInvokeInfo.EntryPoint += "Changed"),
            ("assembly reference", assembly => assembly.MainModule.AssemblyReferences[0].Version = new Version(99, 0, 0, 0)),
            ("native resource", assembly => ChangeResource(assembly, "DiceMaster.Gamepad.Native.MinHook.x64.dll", bytes => { bytes[0] ^= 1; return bytes; })),
            ("translation data", assembly => ChangeResource(assembly, Translation, bytes => Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace("角色數值=조활수치", "角色數值=changed")))),
            ("unreviewed translation comment", assembly => ChangeResource(assembly, Translation, bytes => bytes.Concat(Encoding.UTF8.GetBytes("\r\n// unreviewed\r\n")).ToArray()))
        };
        foreach (var item in cases)
        {
            using var before = AssemblyDefinition.ReadAssembly(baseline);
            using var after = AssemblyDefinition.ReadAssembly(candidate);
            item.Mutate(after);
            bool rejected = false;
            try { Compare(before, after); } catch (InvalidDataException) { rejected = true; }
            Require(rejected, "Audit accepted mutation: " + item.Name);
        }
        return cases.Select(item => item.Name + ": rejected").ToArray();
    }
    static void ChangeResource(AssemblyDefinition assembly, string name, Func<byte[], byte[]> change)
    {
        var resources = assembly.MainModule.Resources;
        var resource = resources.OfType<EmbeddedResource>().Single(item => item.Name == name);
        resources[resources.IndexOf(resource)] = new EmbeddedResource(resource.Name, resource.Attributes, change(resource.GetResourceData()));
    }
    static int Main(string[] args)
    {
        try
        {
            Require(args.Length >= 2 && args.Length <= 4, "Usage: ReleaseAudit Alpha4DiceMaster.dll CandidateDiceMaster.dll [--self-test] [Report.json]");
            byte[] baseline = File.ReadAllBytes(args[0]), candidate = File.ReadAllBytes(args[1]);
            Require(Hash(baseline) == Alpha4Hash, "Input is not the exact published v1.0.3-alpha.4 DLL.");
            using var before = AssemblyDefinition.ReadAssembly(new MemoryStream(baseline));
            using var after = AssemblyDefinition.ReadAssembly(new MemoryStream(candidate));
            var report = Compare(before, after);
            report["alpha4"] = new { sha256 = Hash(baseline), bytes = baseline.Length };
            report["candidate"] = new { sha256 = Hash(candidate), bytes = candidate.Length };
            if (args.Contains("--self-test", StringComparer.Ordinal)) report["negative_cases"] = SelfTest(args[0], args[1]);
            string json = JsonSerializer.Serialize(report, Json);
            string output = args.Skip(2).SingleOrDefault(value => value != "--self-test");
            if (output != null) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))); File.WriteAllText(output, json + "\n"); }
            Console.WriteLine(json);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("ReleaseAudit failed: " + error.Message); return 1; }
    }
}
