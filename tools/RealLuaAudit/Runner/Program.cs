using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Mono.Cecil;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length != 5) throw new ArgumentException("CorpusRunner auditedDLL fixtureDLL normalizedIndex outputJSON expectedSHA256");
        string sha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))).ToLowerInvariant();
        if (sha != args[4].ToLowerInvariant() || args[4].Length != 64)
            throw new InvalidDataException("Audited DLL hash mismatch");
        using var source = AssemblyDefinition.ReadAssembly(args[0]);
        using var target = AssemblyDefinition.ReadAssembly(args[1]);
        _ = new PayloadCopier(source.MainModule, target.MainModule,
            type => type.Namespace == "DiceMaster.LuaParse" || type.Namespace == "DiceMaster.LiveLog");
        PreviewFixtureBoundary.Install(target.MainModule);
        target.Name.Name += ".Audit_" + sha.Substring(0, 12);
        string outputAssembly = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1]))!, target.Name.Name + ".dll");
        target.Write(outputAssembly);
        object report;
        try
        {
            report = Assembly.LoadFile(outputAssembly).GetType("DiceMaster.CorpusScenario", true)!
                .GetMethod("Run")!.Invoke(null, new object[] { Path.GetFullPath(args[2]) })!;
        }
        catch (TargetInvocationException error)
        {
            Console.Error.WriteLine("Corpus harness failed: " + (error.InnerException ?? error).GetType().FullName);
            return 1;
        }
        File.WriteAllText(args[3], JsonSerializer.Serialize(new { released_dll_sha256 = sha, analysis = report }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("Corpus static analysis report written; no Lua interpreter or game assembly was loaded.");
        return 0;
    }
}
