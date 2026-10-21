using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Mono.Cecil;
using Mono.Cecil.Cil;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length != 2 && args.Length != 3) throw new ArgumentException("PreviewIntegrationTests DiceMaster.dll Fixtures.dll [--baseline]");
        bool baseline = args.Length == 3 && args[2] == "--baseline";
        if (args.Length == 3 && !baseline) throw new ArgumentException("Unknown mode");
        if (baseline && Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))).ToLowerInvariant() !=
            "b6f7ae790cde736dcf1a282e9242c46b8ab0204602c7c462d59c571f321740ec") throw new InvalidDataException("Baseline is not the alpha.2 DLL.");
        using var source = AssemblyDefinition.ReadAssembly(args[0]);
        using var target = AssemblyDefinition.ReadAssembly(args[1]);
        var copier = new PayloadCopier(source.MainModule, target.MainModule,
            type => type.Namespace == "DiceMaster.LuaParse" || !baseline && type.Namespace == "DiceMaster.Information" && (type.Name == "ShopUseEffects" || type.Name == "MapChoiceStep" || type.Name == "MapStoryEntry" || type.Name == "MapStoryKind" || type.Name == "MapStoryProfile" || type.Name.StartsWith("MapRoute", StringComparison.Ordinal)) || (!baseline && (type.Namespace == "DiceMaster.LiveLog" || type.Namespace == "DiceMaster.EventSelection" && new[] { "EventEligibility", "EventEligibilitySnapshot", "EventEligibilityState", "EventEligibilityReason", "PopupConditionText", "SwitchEligibility", "EventResultPreview", "CheckpointReservation", "CheckpointReservationStore" }.Contains(type.Name) || new[] { "LogReadState", "LogRefreshState", "LogConditionBranch", "LogConditionDetails", "LogItem", "LogSnapshot", "ReservationTicket", "LogTabState", "LogModel" }.Contains(type.Name) && type.Namespace == "DiceMaster.LogUI")));
        PreviewFixtureBoundary.Install(target.MainModule);
        if (!baseline)
        {
            foreach (string helper in new[] { "ColorizeKeyByStoryReadState", "NormalizeCheckpointKey" })
                copier.CopyMethod(source.MainModule.GetType("DiceMaster.DicePlugin").Methods.Single(m => m.Name == helper), target.MainModule.GetType("DiceMaster.DicePlugin"));
            // Execute the candidate's complete static/dynamic event cache path.
            // Only local event enumeration and its UI name are game boundaries.
            using var boundaries = AssemblyDefinition.ReadAssembly(args[1]);
            var eventCopier = new PayloadCopier(source.MainModule, target.MainModule,
                type => type.FullName == "DiceMaster.PositionEventCache", merge: true);
            foreach (string boundary in new[] { "GetPositionEvents", "GetPositionEventName" })
                eventCopier.CopyMethod(boundaries.MainModule.GetType("DiceMaster.PositionEventCache").Methods.Single(m => m.Name == boundary), target.MainModule.GetType("DiceMaster.PositionEventCache"));
        }
        foreach (string required in new[] { "DiceMaster.LuaParse.LuaParser", "DiceMaster.LuaParse.Action" })
            if (target.MainModule.GetType(required) == null) throw new InvalidDataException("Actual parser missing: " + required);
        if (!baseline)
            foreach (string required in new[] { "MapRouteDefinitions", "MapChoiceStep", "MapStoryEntry", "MapStoryKind", "MapStoryProfile" })
                if (target.MainModule.GetType("DiceMaster.Information." + required) == null)
                    throw new InvalidDataException("Actual route dependency missing: " + required);
        target.Name.Name += baseline ? ".Baseline" : ".Candidate";
        string output = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1]))!, target.Name.Name + ".dll");
        target.Write(output);
        var assembly = Assembly.LoadFile(output);
        try
        {
            int count = (int)assembly.GetType("DiceMaster.PreviewScenario", true).GetMethod(baseline ? "RunBaseline" : "Run").Invoke(null, null);
            Console.WriteLine("PASS: " + count + (baseline ? " alpha.2 failure reproduction" : " preview integration") + " assertions using actual DLL parser, Action, analyzer and description IL; script/state/translation boundaries are fixtures.");
            return 0;
        }
        catch (TargetInvocationException error) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException ?? error).Throw(); throw; }
    }
}
