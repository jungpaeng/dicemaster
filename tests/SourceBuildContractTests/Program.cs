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

// A source compiler may change IL encodings, token order, closure names and MVIDs.
// Compare the contracts consumed by reflection, Harmony, BepInEx and native code
// instead. The separate behavior suites execute the new assembly's managed IL.
internal static class Program
{
    const string BaselineHash = "485581c2b6d2eb9270e7e11ee5315a9e987dcc3a9b93f79e78a7d0ced4ee3633";
    const string TranslationResource = "DiceMaster.Translations.ModTranslation.txt";
    const string BaselineTranslationHash = "6fc71ac1780be28b78e5ee193835a38e47d1526cbb9a143d05ff5206eceef1ec";
    const string SourceTranslationHash = "7441c94d83df9f2ebe6ecdfe0489c752afe7ebeadc7b28ae6d15aca460acf5eb";
    static int assertions, typeCount, methodCount, hookCount, nativeCount;
    static readonly HashSet<string> acceptedDifferences = new HashSet<string>(StringComparer.Ordinal);
    static readonly List<string> differences = new List<string>();
    static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> roots)
    {
        foreach (var type in roots) { yield return type; foreach (var child in Types(type.NestedTypes)) yield return child; }
    }
    static bool Generated(TypeDefinition type) => type.Name.StartsWith("<", StringComparison.Ordinal) ||
        (type.DeclaringType != null && Generated(type.DeclaringType));
    static bool SourceMethod(MethodDefinition method) => !method.Name.StartsWith("<", StringComparison.Ordinal);
    static string TypeIdentity(TypeReference type)
    {
        if (type == null) return "none";
        if (type is GenericParameter parameter) return parameter.Type + ":" + parameter.Position;
        if (type is GenericInstanceType generic) return TypeIdentity(generic.ElementType) + "<" + string.Join(",", generic.GenericArguments.Select(TypeIdentity)) + ">";
        if (type is TypeSpecification specification) return type.FullName + ":element=" + TypeIdentity(specification.ElementType);
        return type.FullName + "@" + (type.Scope is ModuleDefinition module ? module.Assembly.Name.FullName : type.Scope?.ToString());
    }
    static string Value(object value)
    {
        if (value is TypeReference type) return type.FullName.StartsWith("<", StringComparison.Ordinal) || type.FullName.Contains("/<") ? "<compiler-generated-type>" : TypeIdentity(type);
        if (value is CustomAttributeArgument argument) return Argument(argument);
        if (value is CustomAttributeArgument[] array) return "[" + string.Join(",", array.Select(Argument)) + "]";
        if (value is byte[] bytes) return Convert.ToHexString(bytes);
        if (value is IFormattable formattable) return formattable.ToString(null, CultureInfo.InvariantCulture);
        return JsonSerializer.Serialize(value);
    }
    static string Argument(CustomAttributeArgument argument) => argument.Type.FullName + ":" + Value(argument.Value);
    static string FieldName(FieldDefinition field)
    {
        if (field.DeclaringType.FullName != "DiceMaster.EmbeddedTranslations.Display") return field.Name;
        // ILSpy escapes five pre-existing compiler cache identifiers
        // which are illegal C# identifiers. Only these exact aliases are allowed;
        // compare each field's type, flags and initial data normally below.
        string original = field.Name switch {
            "CS_0024_003C_003E9__CachedAnonymousMethodDelegate1" => "CS$<>9__CachedAnonymousMethodDelegate1",
            "CS_0024_003C_003E9__CachedAnonymousMethodDelegate4" => "CS$<>9__CachedAnonymousMethodDelegate4",
            "CS_0024_003C_003E9__CachedAnonymousMethodDelegate6" => "CS$<>9__CachedAnonymousMethodDelegate6",
            "CS_0024_003C_003E9__CachedAnonymousMethodDelegate9" => "CS$<>9__CachedAnonymousMethodDelegate9",
            "_0024_0024method0x600000e_002D1" => "$$method0x600000e-1", _ => field.Name
        };
        if (original != field.Name) acceptedDifferences.Add("legacy compiler cache field: " + original + " -> " + field.Name);
        return original;
    }
    static string Attribute(CustomAttribute attribute) => attribute.AttributeType.FullName + "(" +
        string.Join(",", attribute.ConstructorArguments.Select(Argument)) + ")" +
        string.Concat(attribute.Fields.OrderBy(field => field.Name, StringComparer.Ordinal).Select(field => " field:" + field.Name + "=" + Argument(field.Argument))) +
        string.Concat(attribute.Properties.OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => " property:" + property.Name + "=" + Argument(property.Argument)));
    static string Attributes(ICustomAttributeProvider provider) => string.Join("\n", provider.CustomAttributes.Select(Attribute).OrderBy(value => value, StringComparer.Ordinal));
    static string Security(ISecurityDeclarationProvider provider) => string.Join("\n", provider.SecurityDeclarations
        .Select(declaration => declaration.Action + ":" + Convert.ToHexString(declaration.GetBlob())).OrderBy(value => value, StringComparer.Ordinal));
    static string Marshal(MarshalInfo marshal)
    {
        if (marshal == null) return "none";
        // Capture every public scalar descriptor on Cecil's concrete marshal
        // types, including size, array element, safe array and custom marshalers.
        return marshal.GetType().Name + ":" + string.Join(",", marshal.GetType().GetProperties()
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => property.Name + "=" + Value(property.GetValue(marshal))));
    }
    static string Generics(IGenericParameterProvider provider) => string.Join("\n", provider.GenericParameters.Select(parameter =>
        parameter.Position + ":" + parameter.Name + ":" + parameter.Attributes + ":" + Attributes(parameter) + ":" +
        string.Join(",", parameter.Constraints.Select(constraint => TypeIdentity(constraint.ConstraintType) + ":" + Attributes(constraint)).OrderBy(value => value, StringComparer.Ordinal))));
    static string Parameter(ParameterDefinition parameter)
    {
        string method = ((MethodReference)parameter.Method).DeclaringType.FullName + "::" + ((MethodReference)parameter.Method).Name;
        string name = parameter.Name, attributes = Attributes(parameter);
        if (name == "P_0" && method is "DiceMaster.OptionInfoPatch::PreviewReadGroup" or "DiceMaster.OptionInfoPatch::PreviewCheckComparison" or
            "DiceMaster.OptionInfoPatch::PreviewReadEventRate" or "DiceMaster.PositionEventCache::GeneratePreviewDynamicDesc")
        {
            acceptedDifferences.Add("non-Harmony internal wrapper parameter: " + method + " empty -> P_0");
            name = "";
        }
        if (parameter.Name is "args" or "arguments" && method is "DiceMaster.LiveLog.LogBridge::Call" or "DiceMaster.LiveLog.LogBridge::CallDisplayHelper" or "DiceMaster.LiveLog.PreviewFormatting::Helper" &&
            attributes == "System.ParamArrayAttribute()")
        {
            // PayloadCopier historically omitted this source-level annotation.
            // The direct build retains it from the reviewed linked C# sources.
            acceptedDifferences.Add("linked source restores ParamArrayAttribute: " + method);
            attributes = "";
        }
        return name + ":" + TypeIdentity(parameter.ParameterType) + ":" + parameter.Attributes +
            ":constant=" + parameter.HasConstant + ":" + (parameter.HasConstant ? Value(parameter.Constant) : "") + ":marshal=" + Marshal(parameter.MarshalInfo) + ":" + attributes;
    }
    static string Method(MethodDefinition method) => method.Attributes + ":" + method.ImplAttributes + ":" + method.CallingConvention +
        ":this=" + method.HasThis + ":explicit=" + method.ExplicitThis + ":body=" + method.HasBody + ":" + Attributes(method) +
        "\nsecurity:" + Security(method) +
        "\noverrides:" + string.Join("\n", method.Overrides.Select(item => item.FullName + "@" + TypeIdentity(item.DeclaringType)).OrderBy(value => value, StringComparer.Ordinal)) +
        "\nreturn:" + TypeIdentity(method.ReturnType) + ":" + method.MethodReturnType.Attributes + ":" + Attributes(method.MethodReturnType) + ":" + Marshal(method.MethodReturnType.MarshalInfo) +
        "\ngenerics:" + Generics(method) + "\n" + string.Join("\n", method.Parameters.Select(Parameter)) +
        (method.PInvokeInfo == null ? "" : "\npinvoke:" + method.PInvokeInfo.Module.Name + ":" + method.PInvokeInfo.EntryPoint + ":" + method.PInvokeInfo.Attributes);
    static void Equal(string key, object before, object after)
    {
        assertions++;
        if (!Equals(before, after)) differences.Add(key + "\n  baseline: " + before + "\n  candidate: " + after);
    }
    static void Require(bool ok, string message)
    {
        assertions++;
        if (!ok) differences.Add(message);
    }
    static string ResourceHash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    static string TranslationData(byte[] bytes) => string.Join("\n", Encoding.UTF8.GetString(bytes).Split('\n')
        .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal)));
    static void SetEqual<T>(string label, IEnumerable<T> before, IEnumerable<T> after, Func<T, string> key, Action<T, T> compare)
    {
        var expected = before.GroupBy(key).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var actual = after.GroupBy(key).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        foreach (string name in expected.Keys.Union(actual.Keys).OrderBy(value => value, StringComparer.Ordinal))
        {
            expected.TryGetValue(name, out T[] a); actual.TryGetValue(name, out T[] b);
            Require(a?.Length == 1 && b?.Length == 1, label + " missing/extra/duplicate " + name + " baseline=" + (a?.Length ?? 0) + " candidate=" + (b?.Length ?? 0));
            if (a?.Length == 1 && b?.Length == 1) compare(a[0], b[0]);
        }
    }
    static void CompareType(TypeDefinition before, TypeDefinition after)
    {
        typeCount++;
        string key = "type " + before.FullName;
        Equal(key, before.Attributes, after.Attributes);
        Equal(key + " base", TypeIdentity(before.BaseType), TypeIdentity(after.BaseType));
        Equal(key + " generic parameters", Generics(before), Generics(after));
        Equal(key + " attributes", Attributes(before), Attributes(after));
        Equal(key + " security", Security(before), Security(after));
        Equal(key + " layout", before.PackingSize + ":" + before.ClassSize, after.PackingSize + ":" + after.ClassSize);
        Equal(key + " interfaces", string.Join("\n", before.Interfaces.Select(item => TypeIdentity(item.InterfaceType) + ":" + Attributes(item)).OrderBy(value => value)),
            string.Join("\n", after.Interfaces.Select(item => TypeIdentity(item.InterfaceType) + ":" + Attributes(item)).OrderBy(value => value)));
        SetEqual(key + " field", before.Fields, after.Fields, FieldName, (a, b) => {
            Equal("field " + a.FullName, TypeIdentity(a.FieldType) + ":" + a.Attributes + ":" + a.HasConstant + ":" + (a.HasConstant ? Value(a.Constant) : "") +
                ":offset=" + a.Offset + ":initial=" + Value(a.InitialValue) + ":marshal=" + Marshal(a.MarshalInfo) + ":" + Attributes(a),
                TypeIdentity(b.FieldType) + ":" + b.Attributes + ":" + b.HasConstant + ":" + (b.HasConstant ? Value(b.Constant) : "") +
                ":offset=" + b.Offset + ":initial=" + Value(b.InitialValue) + ":marshal=" + Marshal(b.MarshalInfo) + ":" + Attributes(b));
        });
        SetEqual(key + " method", before.Methods.Where(SourceMethod), after.Methods.Where(SourceMethod), method => method.FullName, (a, b) => {
            methodCount++;
            if (a.CustomAttributes.Any(attribute => attribute.AttributeType.Namespace == "HarmonyLib")) hookCount++;
            if (a.IsPInvokeImpl || before.BaseType?.FullName == "System.MulticastDelegate") nativeCount++;
            Equal("method " + a.FullName, Method(a), Method(b));
        });
        SetEqual(key + " property", before.Properties, after.Properties, property => property.FullName, (a, b) => {
            Equal("property " + a.FullName, a.Attributes + ":" + a.GetMethod?.FullName + ":" + a.SetMethod?.FullName + ":" + Attributes(a),
                b.Attributes + ":" + b.GetMethod?.FullName + ":" + b.SetMethod?.FullName + ":" + Attributes(b));
        });
        SetEqual(key + " event", before.Events, after.Events, item => item.FullName, (a, b) => {
            Equal("event " + a.FullName, a.Attributes + ":" + a.AddMethod?.FullName + ":" + a.RemoveMethod?.FullName + ":" + Attributes(a),
                b.Attributes + ":" + b.AddMethod?.FullName + ":" + b.RemoveMethod?.FullName + ":" + Attributes(b));
        });
    }
    static MethodDefinition Find(ModuleDefinition module, string type, string method) => module.GetType(type)?.Methods.SingleOrDefault(value => value.Name == method)
        ?? throw new InvalidDataException("Missing contract method " + type + "::" + method);
    static string ConfigInitializer(MethodDefinition method)
    {
        // This straight-line initializer is the complete persisted setting list:
        // section/name/description/default/type/store. Normalize integer opcode
        // spelling only; an omitted, reordered or changed setting still fails.
        return string.Join("\n", method.Body.Instructions.Where(instruction => instruction.OpCode != OpCodes.Nop).Select(instruction => {
            int? number = instruction.OpCode.Code switch {
                Code.Ldc_I4_M1 => -1, Code.Ldc_I4_0 => 0, Code.Ldc_I4_1 => 1, Code.Ldc_I4_2 => 2, Code.Ldc_I4_3 => 3, Code.Ldc_I4_4 => 4,
                Code.Ldc_I4_5 => 5, Code.Ldc_I4_6 => 6, Code.Ldc_I4_7 => 7, Code.Ldc_I4_8 => 8,
                Code.Ldc_I4 or Code.Ldc_I4_S => Convert.ToInt32(instruction.Operand, CultureInfo.InvariantCulture), _ => null
            };
            if (number.HasValue) return "integer:" + number.Value;
            return instruction.OpCode.Code + ":" + (instruction.Operand is MemberReference member ? member.FullName : Value(instruction.Operand));
        }));
    }
    static void CompareHooks(ModuleDefinition baseline, ModuleDefinition candidate)
    {
        string Inventory(ModuleDefinition module) => string.Join("\n", Types(module.Types).SelectMany(type =>
            type.CustomAttributes.Where(attribute => attribute.AttributeType.Namespace == "HarmonyLib").Select(attribute => type.FullName + ":" + Attribute(attribute))
                .Concat(type.Methods.SelectMany(method => method.CustomAttributes.Where(attribute => attribute.AttributeType.Namespace == "HarmonyLib")
                    .Select(attribute => method.FullName + ":" + Attribute(attribute))))).OrderBy(value => value, StringComparer.Ordinal));
        // Include generated types/methods in this inventory so a second hook
        // cannot hide in a closure excluded from named API shape comparison.
        Equal("complete Harmony hook inventory", Inventory(baseline), Inventory(candidate));
        string[] sourceOwners = { "DiceMaster.DicePlugin", "DiceMaster.DicePatch", "DiceMaster.OptionInfoPatch" };
        foreach (string owner in sourceOwners)
        {
            var original = baseline.GetType(owner); var output = candidate.GetType(owner);
            foreach (var method in original.Methods.Where(method => method.HasBody && SourceMethod(method)))
            {
                var matches = output.Methods.Where(item => item.FullName == method.FullName).ToArray();
                var actual = matches.Length == 1 ? matches[0] : null;
                if (actual?.HasBody != true) continue;
                // Every explicit route into managed extension subsystems must
                // have exactly the same multiplicity at its source hook.
                string[] Routing(MethodDefinition value) => value.Body.Instructions.Select(instruction => instruction.Operand).OfType<MethodReference>()
                    .Where(call => call.DeclaringType.Namespace == "DiceMaster.Gamepad" || call.DeclaringType.Namespace == "DiceMaster.LiveLog" ||
                        call.DeclaringType.FullName == "HarmonyLib.Harmony" || call.Name is "UpdateGamepad" or "RefreshLiveLogDisplay")
                    .Select(call => call.FullName).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                Equal("managed/hook route " + method.FullName, string.Join("\n", Routing(method)), string.Join("\n", Routing(actual)));
            }
        }
        // Verify duplicates independently, including methods with generated
        // names. Exact attribute sets above also preserve paired __state names,
        // hook roles, target overloads and priorities from the verified baseline.
        foreach (var type in Types(candidate.Types))
        {
            Require(type.Methods.Select(method => method.FullName).Distinct().Count() == type.Methods.Count, "Duplicate method signature in " + type.FullName);
            foreach (var method in type.Methods)
                Require(method.CustomAttributes.Where(attribute => attribute.AttributeType.Namespace == "HarmonyLib").Select(Attribute).Distinct().Count() ==
                    method.CustomAttributes.Count(attribute => attribute.AttributeType.Namespace == "HarmonyLib"), "Duplicate Harmony marker on " + method.FullName);
        }
    }
    static int Verify(string[] args)
    {
        assertions = typeCount = methodCount = hookCount = nativeCount = 0;
        differences.Clear(); acceptedDifferences.Clear();
        if (args.Length != 2) throw new ArgumentException("Usage: SourceBuildContractTests BaselineDiceMaster.dll CandidateDiceMaster.dll");
        string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))).ToLowerInvariant();
        if (hash != BaselineHash) throw new InvalidDataException("Baseline is not verified v1.0.3-alpha.3: " + hash);
        using var before = AssemblyDefinition.ReadAssembly(args[0]);
        using var after = AssemblyDefinition.ReadAssembly(args[1]);
        Console.WriteLine("BASELINE SHA256=" + hash);
        Console.WriteLine("CANDIDATE SHA256=" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1]))).ToLowerInvariant());
        Equal("assembly identity", before.Name.FullName, after.Name.FullName);
        Equal("assembly attributes", Attributes(before), Attributes(after));
        Equal("assembly security", Security(before), Security(after));
        Equal("module attributes", Attributes(before.MainModule), Attributes(after.MainModule));
        Equal("module runtime", before.MainModule.RuntimeVersion, after.MainModule.RuntimeVersion);
        Equal("module architecture", before.MainModule.Architecture, after.MainModule.Architecture);
        Equal("assembly reference identities", string.Join("\n", before.MainModule.AssemblyReferences.Select(reference => reference.FullName).OrderBy(value => value, StringComparer.Ordinal)),
            string.Join("\n", after.MainModule.AssemblyReferences.Select(reference => reference.FullName).OrderBy(value => value, StringComparer.Ordinal)));
        Require(!after.MainModule.AssemblyReferences.Any(reference => reference.Name is "DiceMaster.GamepadPayload" or "DiceMaster.LiveLogPayload" or "System.Private.CoreLib" or "DiceMaster"), "Payload, net8 runtime or old DiceMaster assembly leaked into candidate references.");
        Require(Types(after.MainModule.Types).Select(type => type.FullName).Distinct().Count() == Types(after.MainModule.Types).Count(), "Duplicate candidate type full name.");
        SetEqual("source type", Types(before.MainModule.Types).Where(type => type.Name != "<Module>" && !Generated(type)),
            Types(after.MainModule.Types).Where(type => type.Name != "<Module>" && !Generated(type)), type => type.FullName, CompareType);
        Equal("configuration names/descriptions/types/defaults", ConfigInitializer(Find(before.MainModule, "DiceMaster.Config.ConfigManager", "CreateConfigElements")),
            ConfigInitializer(Find(after.MainModule, "DiceMaster.Config.ConfigManager", "CreateConfigElements")));
        CompareHooks(before.MainModule, after.MainModule);
        SetEqual("resource", before.MainModule.Resources, after.MainModule.Resources, resource => resource.Name, (a, b) => {
            Equal("resource " + a.Name + " flags/type", a.Attributes + ":" + a.ResourceType, b.Attributes + ":" + b.ResourceType);
            Require(a is EmbeddedResource && b is EmbeddedResource, "Expected embedded resource " + a.Name);
            if (a is EmbeddedResource x && b is EmbeddedResource y)
            {
                byte[] original = x.GetResourceData(), actual = y.GetResourceData();
                if (a.Name == TranslationResource)
                {
                    // Three section headings were reviewed separately from the
                    // pinned recovery. Pin both complete byte sequences; keep
                    // every translation row, blank line and line ending equal.
                    Equal("resource " + a.Name + " baseline bytes", BaselineTranslationHash, ResourceHash(original));
                    Equal("resource " + a.Name + " bytes (reviewed section headings)", SourceTranslationHash, ResourceHash(actual));
                    Equal("resource " + a.Name + " translation data rows", TranslationData(original), TranslationData(actual));
                    acceptedDifferences.Add("translation resource section headings: " + a.Name + " (three comments; translation data unchanged)");
                }
                else Equal("resource " + a.Name + " bytes", ResourceHash(original), ResourceHash(actual));
            }
        });
        if (differences.Count != 0)
        {
            foreach (string difference in differences.Take(120)) Console.Error.WriteLine("FAIL " + difference);
            Console.Error.WriteLine("FAIL: " + differences.Count + " contract differences / " + assertions + " assertions.");
            return 1;
        }
        foreach (string difference in acceptedDifferences.OrderBy(value => value, StringComparer.Ordinal)) Console.WriteLine("ACCEPTED SOURCE REPRESENTATION: " + difference);
        Console.WriteLine($"PASS: {assertions} source-build contract assertions; {typeCount} named types, {methodCount} named methods, {hookCount} Harmony-marked methods, {nativeCount} P/Invoke/delegate methods, {before.MainModule.Resources.Count} resource names/flags identical; 7 resource byte sequences identical and 1 has only three reviewed comment changes with all translation data unchanged. Compiler-generated identities, instruction encodings, MVID and PE bytes are outside this comparison; managed behavior and native runtime require separate checks.");
        return 0;
    }
    static int Main(string[] args)
    {
        if (args.Length != 3 || args[0] != "--self-test") return Verify(args);
        if (Verify(args.Skip(1).ToArray()) != 0) throw new InvalidDataException("The unmodified source candidate must pass before mutation checks.");
        string directory = Path.Combine(Path.GetTempPath(), "dicemaster-contract-negative-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        int mutations = 0;
        try
        {
            void Reject(string name, Action<AssemblyDefinition> mutate, string expectedFailure)
            {
                string path = Path.Combine(directory, name + ".dll");
                using (var assembly = AssemblyDefinition.ReadAssembly(args[2])) { mutate(assembly); assembly.Write(path); }
                var stdout = Console.Out; var stderr = Console.Error;
                using var captured = new StringWriter(CultureInfo.InvariantCulture);
                int exitCode;
                try { Console.SetOut(captured); Console.SetError(captured); exitCode = Verify(new[] { args[1], path }); }
                finally { Console.SetOut(stdout); Console.SetError(stderr); }
                if (exitCode == 0 || !captured.ToString().Contains(expectedFailure, StringComparison.Ordinal))
                    throw new InvalidDataException("Mutation was not rejected for the expected contract: " + name + "\n" + captured);
                mutations++;
                Console.WriteLine("NEGATIVE CHECK REJECTED: " + name);
            }
            Reject("resource-flags", assembly => assembly.MainModule.Resources[0].Attributes ^= ManifestResourceAttributes.Public | ManifestResourceAttributes.Private, " flags/type");
            Reject("resource-bytes", assembly => {
                var resource = (EmbeddedResource)assembly.MainModule.Resources[0];
                byte[] bytes = resource.GetResourceData(); bytes[0] ^= 1;
                assembly.MainModule.Resources[0] = new EmbeddedResource(resource.Name, resource.Attributes, bytes);
            }, " bytes");
            Reject("translation-data", assembly => {
                int index = assembly.MainModule.Resources.ToList().FindIndex(resource => resource.Name == TranslationResource);
                var resource = (EmbeddedResource)assembly.MainModule.Resources[index];
                byte[] bytes = resource.GetResourceData();
                int separator = Array.IndexOf(bytes, (byte)'=');
                if (separator < 0) throw new InvalidDataException("Translation fixture has no key/value separator.");
                bytes[separator] = (byte)':';
                assembly.MainModule.Resources[index] = new EmbeddedResource(resource.Name, resource.Attributes, bytes);
            }, " translation data rows");
            Reject("restored-translation-headings", assembly => {
                using var baseline = AssemblyDefinition.ReadAssembly(args[1]);
                var original = (EmbeddedResource)baseline.MainModule.Resources.Single(resource => resource.Name == TranslationResource);
                int index = assembly.MainModule.Resources.ToList().FindIndex(resource => resource.Name == TranslationResource);
                var resource = assembly.MainModule.Resources[index];
                assembly.MainModule.Resources[index] = new EmbeddedResource(resource.Name, resource.Attributes, original.GetResourceData());
            }, " bytes (reviewed section headings)");
            Reject("missing-type", assembly => assembly.MainModule.GetType("DiceMaster.RichText").Name = "RichTextMissing", "source type missing/extra/duplicate DiceMaster.RichText");
            Reject("duplicate-type", assembly => assembly.MainModule.Types.Add(new TypeDefinition("DiceMaster", "RichText", TypeAttributes.Public)), "Duplicate candidate type full name.");
            Reject("duplicate-hook", assembly => {
                var owner = assembly.MainModule.GetType("DiceMaster.OptionInfoPatch");
                var hook = owner.Methods.First(method => method.Name.StartsWith("LiveLog_") && method.ReturnType.FullName == "System.Void");
                var duplicate = new MethodDefinition(hook.Name, hook.Attributes, hook.ReturnType);
                foreach (var parameter in hook.Parameters) duplicate.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.ParameterType));
                duplicate.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                owner.Methods.Add(duplicate);
            }, "method missing/extra/duplicate");
            Reject("harmony-parameter-name", assembly => assembly.MainModule.GetType("DiceMaster.OptionInfoPatch").Methods
                .First(method => method.Name.StartsWith("LiveLog_")).Parameters.Single(parameter => parameter.Name == "__state").Name = "wrongState", "method ");
            Reject("config-default", assembly => {
                var initializer = Find(assembly.MainModule, "DiceMaster.Config.ConfigManager", "CreateConfigElements");
                initializer.Body.Instructions.First(instruction => instruction.OpCode == OpCodes.Ldc_I4_1).OpCode = OpCodes.Ldc_I4_0;
            }, "configuration names/descriptions/types/defaults");
            Reject("native-entrypoint", assembly => Types(assembly.MainModule.Types).SelectMany(type => type.Methods).First(method => method.IsPInvokeImpl).PInvokeInfo.EntryPoint += "_wrong", "pinvoke:");
            Reject("native-struct-pack", assembly => assembly.MainModule.GetType("DiceMaster.Gamepad.PadState").PackingSize = 1, "type DiceMaster.Gamepad.PadState layout");
            Reject("native-delegate-calling-convention", assembly => {
                var attribute = assembly.MainModule.GetType("DiceMaster.Gamepad.GetState").CustomAttributes
                    .Single(attribute => attribute.AttributeType.FullName == "System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute");
                attribute.ConstructorArguments[0] = new CustomAttributeArgument(attribute.ConstructorArguments[0].Type, 2);
            }, "type DiceMaster.Gamepad.GetState attributes");
            Console.WriteLine("PASS: " + mutations + " independently mutated contract violations rejected for their expected reason.");
            return 0;
        }
        finally { Directory.Delete(directory, true); }
    }
}
