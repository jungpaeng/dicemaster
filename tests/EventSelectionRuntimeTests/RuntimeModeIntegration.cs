using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

// The production plugin depends on Unity startup, Harmony and BepInEx. Copy
// only its actual scheme observer into these explicit boundaries; the runtime,
// popup and Lua gate remain the linked production implementations. This makes
// a delayed observation run through real request acceptance and Lua completion,
// instead of treating a mock command receiver's click count as execution.
internal static class RuntimeModeIntegration
{
    internal static void Run(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("EventSelectionRuntimeTests DiceMaster.dll");
        string fixture = Assembly.GetExecutingAssembly().Location;
        using var source = AssemblyDefinition.ReadAssembly(args[0]);
        using var target = AssemblyDefinition.ReadAssembly(fixture);
        var copier = new PayloadCopier(source.MainModule, target.MainModule, type => false);
        copier.CopyMethod(source.MainModule.GetType("DiceMaster.DicePlugin").Methods.Single(method => method.Name == "ObserveEventInputMode"),
            target.MainModule.GetType("DiceMaster.DicePlugin"));
        target.Name.Name += ".Candidate";
        string output = Path.Combine(Path.GetDirectoryName(fixture), target.Name.Name + ".dll");
        target.Write(output);
        var assembly = Assembly.LoadFile(output);
        try { assembly.GetType("Program", true).GetMethod("Run", BindingFlags.Public | BindingFlags.Static).Invoke(null, null); }
        catch (TargetInvocationException error)
        { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException ?? error).Throw(); throw; }
    }
}
