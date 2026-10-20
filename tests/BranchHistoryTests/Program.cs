using System.Reflection;
using DiceMaster;
using DiceMaster.EventSelection;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mono.Cecil;
using Mortal.Story;
using UnityEngine;

internal static class Program
{
    private static int assertions;
    private static void Check(bool condition, string reason) { assertions++; if (!condition) throw new Exception(reason); }
    private static object Call(string patch, string method, params object[] args)
    {
        Type type = typeof(BranchObservationPatches).GetNestedType(patch, BindingFlags.NonPublic);
        return type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
    }
    private static void Reset()
    {
        BranchHistory.Cleared = null; BranchHistory.Enabled = false; BranchHistory.Clear(); BranchHistory.Enabled = true;
        Time.unscaledTime = 10; OptionInfoPatch.DisplayRefreshDepth = 0;
        DataHelper.NameReads = DataHelper.ScriptReads = PopupConditionText.Reads = EventResultPreview.Reads = PreviewAnalyzer.Analyses = PreviewAnalyzer.Retries = 0;
        EventResultPreview.RetryNext = PreviewAnalyzer.Fail = PopupConditionText.Fail = false;
        OptionInfoPatch._lastLuaString = OptionInfoPatch._lastFriendlyName = null;
        EventSelectionRuntime.Set(null);
        DicePlugin.Logs.Clear();
    }
    private static PositionResultData Pool() { var p = new PositionResultData { _scriptPrefix = "story" }; p.List.Add(new PositionEventData { name = "chosen", ResultType = "chosen" }); return p; }
    private static object Begin(string patch, object pool)
    { object[] args = { pool, null }; Call(patch, "Prefix", args); return args[1]; }
    private static Exception Finish(string patch, object state, object result, Exception error = null)
    { return (Exception)Call(patch, "Finalizer", error, result, state); }
    private static InformationRow Latest() { return BranchHistory.Latest(Time.unscaledTime); }
    private static InformationRow Detail(int index = 0) { return (InformationRow)BranchHistory.Discover()[index].Read(); }
    private static void Outcomes()
    {
        Reset(); var pool = Pool();
        OptionInfoPatch.DisplayRefreshDepth = 1;
        Check(Begin("PositionResultPatch", pool) == null, "protected display callbacks are never actual branch history");
        OptionInfoPatch.DisplayRefreshDepth = 0;
        object state = Begin("PositionResultPatch", pool);
        Check(state != null, "enabled original boundary owns execution state");
        string result = "story_chosen";
        Check(Finish("PositionResultPatch", state, result) == null, "success exception remains null");
        Check(result == "story_chosen" && Latest().Status == BranchHistory.Confirmed && Latest().Title == result, "actual returned event result observed without mutation");
        Check(DataHelper.NameReads + PopupConditionText.Reads + EventResultPreview.Reads + PreviewAnalyzer.Analyses == 0, "actual result observation and minimal summary do not call display chains");
        Finish("PositionResultPatch", state, result);
        Check(BranchHistory.Discover().Count == 1, "duplicate observation state is not a second execution");
        state = Begin("PositionResultPatch", pool); Finish("PositionResultPatch", state, result);
        Check(BranchHistory.Discover().Count == 2, "same result in another actual execution is retained separately");
        var detail = Detail();
        Check(detail.Title == "번역 chosen" && detail.Detail.Contains("이벤트 조건") && detail.Detail.Contains("정적 효과 story_chosen"), "conditions and effects are deferred to log query");
        Check(!Latest().AutoText.Contains("정적 효과") && !Latest().AutoText.Contains("이벤트 조건"), "automatic notice contains only name kind and status");
        int reads = EventResultPreview.Reads; Detail(); Check(EventResultPreview.Reads == reads, "successful static effects are not reparsed on normal refresh");
        var error = new InvalidOperationException("game failure");
        state = Begin("PositionResultPatch", pool);
        Check(ReferenceEquals(Finish("PositionResultPatch", state, "bad", error), error), "exact original exception is preserved");
        Check(BranchHistory.Discover().Count == 2, "failed execution does not become successful history");
        // A #5 direct selection skips original but still runs observer prefix/finalizer.
        state = Begin("PositionResultPatch", pool); string direct = "story_chosen";
        Finish("PositionResultPatch", state, direct);
        Check(BranchHistory.Discover().Count == 3 && Latest().Title == direct, "prefix-skipped original direct selection is observed through final return");
        var duplicate = new PositionEventData { ResultType = "chosen" }; pool.List.Add(duplicate);
        state = Begin("PositionResultPatch", pool); Finish("PositionResultPatch", state, result);
        Check(Latest().Status == BranchHistory.Unmatched, "ambiguous original event mapping stays unknown");
        Check(Detail().Detail.Contains("유일하게") && !Detail().Detail.Contains("정적 효과"), "unknown mapping never fabricates effects");
        pool.List.Remove(duplicate);
        state = Begin("PositionResultPatch", pool); Finish("PositionResultPatch", state, "story_missing");
        Check(Latest().Status == BranchHistory.Unmatched, "unmatched actual returned event is retained as unknown");
        state = Begin("PositionResultPatch", pool); Finish("PositionResultPatch", state, null);
        Check(Latest().Status == BranchHistory.Unmatched && Latest().Title == "(null)", "null event return is retained explicitly rather than fabricated as integer zero");
    }
    private static void Lifetime()
    {
        Reset(); var pool = Pool();
        string context = BranchHistory.ContextKey;
        for (int index = 0; index < 25; index++) { Time.unscaledTime = index; Finish("PositionResultPatch", Begin("PositionResultPatch", pool), "story_chosen"); }
        Check(BranchHistory.Discover().Count == 20, "history retains at most twenty actual calls");
        Check(BranchHistory.ContextKey == context, "new records preserve current display context");
        var ids = BranchHistory.Discover().Select(q => q.Id).ToArray();
        Check(ids.Distinct().Count() == 20, "bounded rows retain distinct execution identities");
        Check(long.Parse(ids[0].Split('/')[2]) > long.Parse(ids[19].Split('/')[2]), "newest actual execution appears first");
        Check(BranchHistory.Latest(28.999f) != null && BranchHistory.Latest(29) == null, "latest notice expires at exactly five seconds");
        Check(BranchHistory.Latest(25) == null, "a clock reset cannot resurrect an expired notice");
        Time.unscaledTime = 30; Finish("PositionResultPatch", Begin("PositionResultPatch", pool), "story_chosen");
        Check(BranchHistory.Latest(34.999f) != null && BranchHistory.Latest(35) == null, "replacement result starts its own five seconds");
        Check(BranchHistory.Latest(float.NaN) == null && BranchHistory.Latest(float.PositiveInfinity) == null, "invalid clocks do not display");
        var oldRead = BranchHistory.Discover()[0];
        object pending = Begin("PositionResultPatch", pool);
        BranchHistory.Enabled = false;
        Check(BranchHistory.ContextKey != context && BranchHistory.Discover().Count == 0 && Latest() == null, "OFF immediately clears notice history and display context");
        BranchHistory.Enabled = true; Finish("PositionResultPatch", pending, "story_chosen");
        Check(BranchHistory.Discover().Count == 0, "callback from before OFF cannot replay after ON");
        bool staleRejected = false; try { oldRead.Read(); } catch (DataNotReadyException) { staleRejected = true; }
        Check(staleRejected, "old display query cannot restore cleared records");
        BranchHistory.Enabled = false;
        Check(Begin("PositionResultPatch", pool) == null, "disabled branch feature does not collect executions");
    }
    private static void SwitchesAndSources()
    {
        Reset();
        var pool = new SwitchResultData { _count = 7 }; pool._items.Add(new TestCondition()); pool._items.Add(new TestCondition());
        OptionInfoPatch._lastFriendlyName = "script";
        OptionInfoPatch._lastLuaString = "local answer = checkpointmanager.Switch('key') if answer == 1 then statmodifymanager.AddMoney(1) elseif answer == 2 then statmodifymanager.AddMoney(2) else statmodifymanager.AddMoney(3) end";
        object[] call = { "key", null }; Call("SwitchCallPatch", "Prefix", call);
        object state = Begin("SwitchResultPatch", pool);
        Check(Finish("SwitchResultPatch", state, 2) == null, "Switch finalizer preserves successful exception");
        Call("SwitchCallPatch", "Finalizer", null, call[1]);
        Check(Latest().Title == "key: 분기 2" && Latest().Status == BranchHistory.Confirmed, "actual Switch name and returned branch are captured");
        Check(PreviewAnalyzer.Analyses == 0 && PopupConditionText.Reads == 0, "original Switch observation does no display or analysis work");
        string oldLua = OptionInfoPatch._lastLuaString;
        OptionInfoPatch._lastLuaString = "different next Lua";
        Check(BranchHistory.Discover().Count == 1, "normal Lua replacement preserves history");
        Check(Detail().Detail.Contains("선택 분기 정적 효과") && PreviewAnalyzer.Analyses == 1, "deferred original Switch matches captured source rather than later Lua");
        Detail(); Check(PreviewAnalyzer.Analyses == 1, "normal refresh reuses successful Switch analysis");
        Check(pool._count == 7, "information never advances execution count");
        foreach (int branch in new[] { 1, 3, 0, 4 })
        {
            state = Begin("SwitchResultPatch", pool); Finish("SwitchResultPatch", state, branch);
            Check(Latest().Available == (branch == 1 || branch == 3), "actual Switch valid/fallback/invalid return mapping " + branch);
        }
        Reset(); pool._useCount = true; pool._count = 42; pool._countItems.Add(new TestCondition { _compareOp = 0, _value = 42 });
        Finish("SwitchResultPatch", Begin("SwitchResultPatch", pool), 1);
        Check(Detail().Detail.Contains("실제 실행 횟수 42 = 42") && pool._count == 42, "count condition explains captured actual count without next-execution evaluation");
        Reset(); pool._useCount = false;
        OptionInfoPatch._lastLuaString = oldLua + "\n" + oldLua;
        call = new object[] { "key", null }; Call("SwitchCallPatch", "Prefix", call);
        Finish("SwitchResultPatch", Begin("SwitchResultPatch", pool), 1); Call("SwitchCallPatch", "Finalizer", null, call[1]);
        Check(Detail().Detail.Contains("유일하게 연결하지 못했습니다") && PreviewAnalyzer.Analyses == 0, "same checkpoint at multiple Lua sites leaves effects unknown");
        Reset(); pool._items.Clear(); pool._items.Add(new TestCondition());
        var tokens = PreviewSyntax.Tokenize(oldLua); int position = tokens.FindIndex(t => t.Text == "checkpointmanager");
        PreviewSyntax.TryCall(tokens, position, out _, out _, out _, out int end);
        EventSwitchSite site = EventSwitchSite.Read(oldLua, tokens, position, end);
        EventSelectionRuntime.Set(new DirectAction { SwitchPool = pool, Executing = true, SwitchSite = site });
        Finish("SwitchResultPatch", Begin("SwitchResultPatch", pool), 1);
        Check(Detail().Detail.Contains("선택 분기 정적 효과"), "direct #5 selection keeps exact original Lua site even without name inference");
        object[] oldCall = { "old-generation-key", null }; Call("SwitchCallPatch", "Prefix", oldCall);
        BranchHistory.Clear();
        Finish("SwitchResultPatch", Begin("SwitchResultPatch", pool), 1);
        Call("SwitchCallPatch", "Finalizer", null, oldCall[1]);
        Check(!Latest().Title.Contains("old-generation-key"), "older outer execution cannot attach source identity after reset");
        var changed = pool._items[0]; pool._items[0] = new TestCondition();
        Check(Detail().Status == BranchHistory.Unmatched, "later source replacement cannot attach old result to new branch details");
    }
    private sealed class DirectAction { public object SwitchPool; public bool Executing; public EventSwitchSite SwitchSite; }
    private static void FailuresAndResetHooks()
    {
        Reset(); var pool = Pool(); Finish("PositionResultPatch", Begin("PositionResultPatch", pool), "story_chosen");
        var engine = new RefreshEngine(); List<DisplayRow> rows = null;
        EventResultPreview.RetryNext = true;
        engine.Tick("history", true, true, 0, false, false, BranchHistory.Discover, value => rows = value, _ => { });
        Check(rows.Count == 1 && rows[0].Status == RefreshEngine.ReadFailureStatus, "first effect read failure is not reported as no effects");
        int reads = EventResultPreview.Reads;
        engine.Tick("history", true, true, 4.99f, false, false, BranchHistory.Discover, value => rows = value, _ => { });
        Check(EventResultPreview.Reads == reads, "failed detail lookup backs off for five seconds");
        EventResultPreview.RetryNext = false;
        engine.Tick("history", true, true, 5, false, false, BranchHistory.Discover, value => rows = value, _ => { });
        Check(rows[0].Status == "" && EventResultPreview.Reads == reads + 1, "failed detail recovers at five seconds");
        engine.Tick("history", false, true, 10, false, false, BranchHistory.Discover, value => rows = value, _ => { });
        Check(EventResultPreview.Reads == reads + 1, "hidden detail lookup does no display work");
        Check(DicePlugin.Logs.Count(l => l.Contains("failure target=")) == 1 && DicePlugin.Logs.Any(l => l.Contains("recovered target=")), "first failure and recovery use existing diagnostic sink");
        int resetNotifications = 0;
        BranchHistory.Cleared = () => { resetNotifications++; Check(BranchHistory.Discover().Count == 0 && Latest() == null, "clear notification sees already discarded store"); };
        BranchHistory.Clear();
        Check(resetNotifications == 1, "clear synchronously invalidates published display owner");
        BranchHistory.Cleared = () => throw new InvalidOperationException("display notification failed");
        BranchHistory.Clear();
        Check(BranchHistory.Discover().Count == 0, "display notification exception cannot interrupt gameplay reset");
        BranchHistory.Cleared = null;
        var targets = BranchObservationPatches.ResetTargets().ToList();
        Check(targets.Count == 21 && targets.Count(m => m.Name == "SaveGameData") == 2, "all actual save entry overloads and common reset boundaries targeted");
        Check(!targets.Any(m => m.Name == "CreateSaveData" || m.Name == "LoadLuaFunction"), "read preparation and ordinary Lua changes are not reset hooks");
        foreach (MethodBase target in targets)
        {
            object pending = Begin("PositionResultPatch", pool);
            Call("ResetPatch", "Prefix");
            Finish("PositionResultPatch", pending, "story_chosen");
            Check(Latest() == null && BranchHistory.Discover().Count == 0, "immediate reset discards pending callbacks: " + target.Name);
            Finish("PositionResultPatch", Begin("PositionResultPatch", pool), "story_chosen");
        }
        var harmony = new HarmonyLib.Harmony(); BranchObservationPatches.Install(harmony); BranchObservationPatches.Install(harmony);
        Check(harmony.Installed.Count == 5, "observer installation is idempotent and creates no input hook");
    }
    private static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> roots) { foreach (var type in roots) { yield return type; foreach (var nested in Types(type.NestedTypes)) yield return nested; } }
    private static void Candidate(string candidate)
    {
        using var module = ModuleDefinition.ReadModule(candidate);
        var types = Types(module.Types).ToArray();
        var observer = types.Single(t => t.FullName == "DiceMaster.Information.BranchObservationPatches");
        foreach (string name in new[] { "PositionResultPatch", "SwitchResultPatch" })
        {
            var patch = observer.NestedTypes.Single(t => t.Name == name);
            var prefix = patch.Methods.Single(m => m.Name == "Prefix");
            var finalizer = patch.Methods.Single(m => m.Name == "Finalizer");
            Check(prefix.ReturnType.FullName == "System.Void" && prefix.CustomAttributes.Any(a => a.AttributeType.Name == "HarmonyPrefix"), "actual candidate non-skipping observer prefix: " + name);
            Check(finalizer.ReturnType.FullName == "System.Exception" && finalizer.Parameters.All(p => !p.ParameterType.IsByReference), "actual candidate finalizer cannot write result or exception arguments: " + name);
            Check(finalizer.CustomAttributes.Any(a => a.AttributeType.Name == "HarmonyFinalizer"), "actual candidate finalizer metadata connected: " + name);
            Check(finalizer.Body.Instructions.TakeLast(2).First().OpCode.Code == Mono.Cecil.Cil.Code.Ldarg_0, "actual candidate returns exact exception argument: " + name);
        }
        string[] banned = { "GetRate", "GetResult", "Execute", "Check", "Learn", "Use", "CreateSaveData", "SetFlag", "SaveGameData", "Position", "Switch" };
        foreach (var type in types.Where(t => t.FullName.StartsWith("DiceMaster.Information.Branch")))
            foreach (var method in type.Methods.Where(m => m.HasBody))
                foreach (var instruction in method.Body.Instructions)
                    if (instruction.Operand is MethodReference invoked)
                        Check(!banned.Contains(invoked.Name), "actual observation/history has no gameplay execution calls: " + invoked.FullName);
        Check(types.Single(t => t.FullName == "DiceMaster.Information.BranchHistory").Properties.Any(p => p.Name == "ContextKey"), "actual candidate exposes reset generation for display invalidation");
        var resetIterator = observer.NestedTypes.Single(t => t.Name.Contains("<ResetTargets>"));
        var literals = resetIterator.Methods.Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).Where(i => i.OpCode.Code == Mono.Cecil.Cil.Code.Ldstr).Select(i => (string)i.Operand).ToHashSet();
        foreach (string expected in BranchObservationPatches.ResetTargets().Select(m => m.Name).Distinct())
            Check(literals.Contains(expected), "actual candidate reset registry contains verified boundary: " + expected);
    }
    private static void ActualGameApi(string referenceRoot)
    {
        using var core = ModuleDefinition.ReadModule(Path.Combine(referenceRoot, "Mortal_Data/Managed/Mortal.Core.dll"));
        using var story = ModuleDefinition.ReadModule(Path.Combine(referenceRoot, "Mortal_Data/Managed/Mortal.Story.dll"));
        var save = core.Types.Single(t => t.FullName == "Mortal.Core.SaveSystem");
        string[] resetNames = { "SaveGameData", "AutoSaveData", "AutoSaveStoryData", "AutoSaveFreeData", "AutoSaveBattleData", "SaveLegendData", "SaveUniverseData", "SaveCurrentSlotData", "BackupLibraryData", "UpdateRestoreData", "ExecuteSaveData", "ExecuteSaveJsonData", "NewGameData", "NewActGameData", "LoadGameData", "AutoLoadGameData", "LoadRestoreSaveData", "ExecuteLoadGameData", "LoadUniverseData" };
        var actual = save.Methods.Where(m => resetNames.Contains(m.Name)).ToList();
        Check(actual.Count == 20 && actual.Count(m => m.Name == "SaveGameData") == 2, "actual supplied SaveSystem has all twenty reset entry overloads");
        foreach (string name in resetNames) Check(actual.Any(m => m.Name == name), "actual supplied reset method exists: " + name);
        // Walk only metadata call edges, never load or invoke gameplay. Every
        // installed save wrapper reaching a real file-write sink must reset.
        var reaches = new HashSet<string>(new[] { "ExecuteSaveData", "ExecuteSaveJsonData" });
        bool changed;
        do
        {
            changed = false;
            foreach (var method in save.Methods.Where(m => m.HasBody))
                if (method.Body.Instructions.Any(i => i.Operand is MethodReference call && call.DeclaringType.FullName == save.FullName && reaches.Contains(call.Name)))
                    changed |= reaches.Add(method.Name);
        } while (changed);
        foreach (string name in reaches) Check(resetNames.Contains(name), "every actual save sink/wrapper is covered: " + name);
        var scene = core.Types.Single(t => t.FullName == "Mortal.Core.SceneController").Methods.Single(m => m.Name == "StartLoadScene");
        Check(scene.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(new[] { "System.String", "System.Boolean" }) && scene.ReturnType.FullName == "System.Collections.IEnumerator", "actual scene reset boundary signature");
        var position = story.Types.Single(t => t.FullName == "Mortal.Story.PositionResultData").Methods.Single(m => m.Name == "GetResult");
        var switches = story.Types.Single(t => t.FullName == "Mortal.Story.SwitchResultData").Methods.Single(m => m.Name == "Execute");
        Check(position.ReturnType.FullName == "System.String" && switches.ReturnType.FullName == "System.Int32", "actual final-return observation signatures");
        var resultGetter = story.Types.Single(t => t.FullName == "Mortal.Story.PositionEventData").Methods.Single(m => m.Name == "get_ResultType");
        Check(resultGetter.Body.Instructions.All(i => i.OpCode.Code != Mono.Cecil.Cil.Code.Call && i.OpCode.Code != Mono.Cecil.Cil.Code.Callvirt && i.OpCode.Code != Mono.Cecil.Cil.Code.Stfld), "actual result-type getter used for matching is a pure field read");
    }
    public static void Main(string[] args)
    {
        Outcomes(); Lifetime(); SwitchesAndSources(); FailuresAndResetHooks();
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])) Candidate(args[0]);
        if (args.Length > 1) ActualGameApi(args[1]);
        string scope = "actual-source history/observation" +
            (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? ", actual candidate DLL IL" : "") +
            (args.Length > 1 ? ", supplied save/scene/result API metadata" : "");
        Console.WriteLine("PASS: BranchHistoryTests: " + assertions + " checks (" + scope + "); game/Unity/display helpers are explicit fixtures, Windows/native not executed.");
    }
}
