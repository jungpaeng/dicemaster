using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Story;
using Mono.Cecil;

internal static class Program
{
    private static int assertions;
    private static ShopPanel panel;
    private static void Assert(bool value, string name) { assertions++; if (!value) throw new Exception(name); }
    private static void Contains(string text, string value, string name) { Assert(text.Contains(value, StringComparison.Ordinal), name + ": " + text); }
    private static void Select(object value, string name = "시험 물품", string original = "원래 설명")
    { panel._dialogTitle.text = name; panel._dialogDesc.text = original; ShopEffectSource.ObserveSelection(panel, value); }
    private static InformationRow Row()
    { List<DisplayQuery> rows = ShopEffectSource.Discover(); Assert(rows.Count == 1, "one selected target"); return (InformationRow)rows[0].Read(); }
    private static MartialModifyStat Effect(GameStat stat, int delta, bool center = false)
    { return new MartialModifyStat { _stat = new MartialStatReference { _stat = stat }, _value = delta, _center = center }; }
    private static Book Book(params MartialModifyStat[] effects)
    { var level = new MartialLevelData(); level._levelupStats.AddRange(effects); return new Book { _martialLevelData = new[] { level } }; }
    private static void NewContext() { ShopEffectSource.Clear(); panel = new ShopPanel(); ShopEffectSource.Enabled = () => true; }
    private static void VisibilityAndSummary()
    {
        NewContext(); var item = new Consumables(); Select(item); var row = Row();
        Assert(ShopEffectSource.Visible, "gamepad callback creates visible source");
        int names = DisplayNames.Reads, stats = SafeDisplayEvaluation.Reads;
        ShopEffectSource.RenderSummary(row.AutoText); string first = panel._dialogDesc.text;
        ShopEffectSource.RenderSummary(row.AutoText); Assert(panel._dialogDesc.text == first, "same summary never appends twice");
        Contains(first, "원래 설명", "preserve original explanation");
        Assert(!first.Contains("소모품 사용 효과 자료", StringComparison.Ordinal), "automatic text excludes detailed effects");
        Assert(names == DisplayNames.Reads && stats == SafeDisplayEvaluation.Reads, "summary never queries or resolves data");
        var previousQuery = ShopEffectSource.Discover()[0]; string previous = ShopEffectSource.ContextKey;
        var other = Book(); Select(other, "다른 책", "새 설명");
        Assert(previous != ShopEffectSource.ContextKey, "target replacement changes context");
        string reused = ShopEffectSource.ContextKey; other._id = "reused"; Select(other, "재사용 책", "새 설명");
        Assert(reused != ShopEffectSource.ContextKey, "reused object new game ID changes context");
        Assert(panel._dialogDesc.text == "새 설명", "old suffix not attached to new subject");
        try { previousQuery.Read(); Assert(false, "stale selection must fail"); } catch (DataNotReadyException) { assertions++; }
        row = Row(); ShopEffectSource.RenderSummary(row.AutoText);
        ShopEffectSource.ObserveClosed(new ShopPanel()); Assert(ShopEffectSource.Visible, "other panel close ignored");
        ShopEffectSource.ObserveClosed(panel); Assert(!ShopEffectSource.Visible, "own panel close clears");
        Assert(panel._dialogDesc.text == "새 설명", "close restores base explanation");
        Select(item); row = Row(); ShopEffectSource.RenderSummary(row.AutoText); panel._dialogDesc.text = "게임이 갱신한 설명";
        ShopEffectSource.Clear(); Assert(panel._dialogDesc.text == "게임이 갱신한 설명", "never overwrite external later text");
        ShopEffectSource.Enabled = () => false; Select(item); Assert(!ShopEffectSource.Visible && ShopEffectSource.Discover().Count == 0, "OFF does not collect selection");
        ShopEffectSource.Enabled = () => { throw new Exception(); }; Select(item); Assert(!ShopEffectSource.Visible, "setting failure closed");
        NewContext(); Select(item); panel.isActiveAndEnabled = false; Assert(!ShopEffectSource.Visible && ShopEffectSource.Discover().Count == 0, "hidden source zero queries");
        NewContext(); Select(item, "<b>원문</b>"); row = Row(); Assert(!row.AutoText.Contains("<", StringComparison.Ordinal), "name cannot inject rich text");
    }
    private static void ConsumablesAndUnknown()
    {
        NewContext(); var item = new Consumables(); Select(item); var row = Row();
        Assert(row.Status == "확인된 효과 없음", "validated empty effects distinguished");
        item._effects = new[] { new ItemEffect { _value = 12 } }; row = Row();
        Assert(row.Status == "사용 경로 미확인", "unmapped execution not guaranteed"); Contains(row.Detail, "+12", "static effect still readable");
        Contains(row.Detail, "사용 실행 경로", "runtime mapping limitation explicit"); Assert(!row.Available, "unverified effect unavailable");
        item._effects = new[] { new ItemEffect { _statType = (GameStatType)999 } }; row = Row(); Assert(row.Status == "효과 해석 미지원", "unknown enum not effect none");
        item._effects = null;
        try { Row(); Assert(false, "missing array must wait"); } catch (DataNotReadyException) { assertions++; }
        Select(new Misc()); Assert(Row().Status == "사용 경로 미연결", "unmapped use not no effect");
    }

    private static void FoodUses()
    {
        NewContext();
        Func<string,string,string,DiceMaster.LuaParse.Action> use = (method,id,text) => new DiceMaster.LuaParse.Action {
            Description = text, Analysis = new PreviewAnalysis { Effects = new List<PreviewEffect> {
                new PreviewEffect { Name = method, Constants = new List<object> { id } } } } };
        DiceMaster.LuaParse.LuaParser.Options = new Dictionary<string,List<DiceMaster.LuaParse.Action>> {
            { "O_food", new List<DiceMaster.LuaParse.Action> { use("RemoveMisc","same","체력 +8") } },
            { "O_drink", new List<DiceMaster.LuaParse.Action> { use("RemoveSpecial","same","내력 +12") } },
            { "O_other", new List<DiceMaster.LuaParse.Action> { use("RemoveMisc","other","호감 +1") } }
        };
        Select(new Misc { _id = "same" }); var row = Row();
        Contains(row.Detail, "체력 +8", "actual stock Misc resolves food-use effects");
        Assert(!row.Detail.Contains("내력 +12",StringComparison.Ordinal), "same ID in different item kinds cannot cross-match");
        Assert(row.Status == "사용 효과 자료 확인" && !row.AutoText.Contains("+8",StringComparison.Ordinal), "status is explicit and effects remain in the log");
        int parses = DiceMaster.LuaParse.LuaParser.Parses, reads = DiceMaster.DataHelper.Reads;
        for (int i=0;i<100;i++) Row();
        Assert(parses == DiceMaster.LuaParse.LuaParser.Parses && reads == DiceMaster.DataHelper.Reads, "warm shop reads do not reload/reparse food menu");
        Select(new SpecialItem { _id = "same" }); row = Row();
        Contains(row.Detail, "내력 +12", "actual stock SpecialItem has its own use mapping");
        Assert(!row.Detail.Contains("체력 +8",StringComparison.Ordinal), "selection replacement cannot show previous item effects");
        Select(new Misc { _id = "unmapped" }); row = Row();
        Assert(row.Status == "사용 경로 미연결" && !row.Available, "unmapped is not falsely called no effect or unusable");
        DiceMaster.DataHelper.State = ScriptReadState.Waiting; ShopUseEffects.Clear();
        try { Row(); Assert(false, "unready source must wait"); } catch (DataNotReadyException) { assertions++; }
        DiceMaster.DataHelper.State = ScriptReadState.Success;
        DiceMaster.LuaParse.LuaParser.Options = new Dictionary<string,List<DiceMaster.LuaParse.Action>>();
    }

    private static void Conditions()
    {
        NewContext(); var stat = new GameStat { _value = 20 };
        var book = Book(Effect(stat, 5)); var level = book._martialLevelData[0];
        var group = new MartialLearnCompareGroup { _itemList = new[]
        {
            new MartialLearnCompareItem { _stat = new MartialStatReference { _stat = stat }, _value = 30, _compareOp = 5 },
            new MartialLearnCompareItem { _stat = new MartialStatReference { _stat = stat }, _value = 10, _compareOp = 5 }
        }};
        level._condition._list.Add(group); Select(book); Assert(Row().Status == "학습 조건 충족", "alternatives OR within group");
        level._condition._list.Add(new MartialLearnCompareGroup()); Assert(Row().Status == "학습 조건 미충족", "empty OR group false");
        level._condition._list.RemoveAt(1); group._itemList[1]._value = 30; Assert(Row().Status == "학습 조건 미충족", "all alternatives false");
        group._itemList[1]._stat._type = 999; Assert(Row().Status == "학습 조건 일부 미지원", "unknown alternative alongside false uncertain");
        group._itemList[0]._value = 10; Assert(Row().Status == "학습 조건 충족", "true OR dominates unknown");
        Assert(stat._value == 20 && book._currentMartialLevel == 0 && book._currentLevelPoint == 0, "condition info preserves game state");
        book._currentMartialLevel = 1; Assert(Row().Status == "학습 완료", "max level distinct");
        book._currentMartialLevel = 2; Assert(Row().Status == "효과 해석 미지원", "invalid level unknown not max");
    }
    private static void EffectSemantics()
    {
        NewContext();
        foreach (int current in new[] { 0, 24, 25, 49, 50, 51, 75, 100 })
        {
            var stat = new GameStat { _value = current }; var book = Book(Effect(stat, 12, true)); Select(book);
            var row = Row(); int expected = Math.Min(12, Math.Abs(50-current)); int target = current < 50 ? current+expected : current-expected;
            Contains(row.Detail, current + " → " + target, "center moves toward 50");
            Assert(stat._value == current && book._currentMartialLevel == 0, "center never mutates");
        }
        var capped = new GameStat { _value = 97 }; Select(Book(Effect(capped, 10))); Contains(Row().Detail, "97 → 100", "stat maximum applied");
        var overflow = new GameStat { _value = 97 }; Select(Book(Effect(overflow, int.MaxValue))); Contains(Row().Detail, "97 → 0", "unchecked game addition precedes clamp");
        var low = new GameStat { _value = 2 }; Select(Book(Effect(low, -10))); Contains(Row().Detail, "2 → 0", "stat minimum applied");
        var martial = new GameStat { _value = 24, _property = 3 }; var graded = Book(Effect(martial, 5)); graded._martialLevel = 2; Select(graded);
        Contains(Row().Detail, "24 → 24", "grade cap blocks too low current martial"); martial._value = 25;
        Contains(Row().Detail, "25 → 30", "25 boundary allows next grade");
        var dynamicStat = new GameStat { _value = 50, _totalAddData = new GameStat() }; Select(Book(Effect(dynamicStat, 5)));
        Assert(Row().Status == "학습 효과 일부 미지원", "nonlocal linked statistic unknown");
        var costStat = new GameStat { _value = 25 }; var costBook = Book(Effect(costStat, 5)); var level = costBook._martialLevelData[0];
        level._consumable.Add(new MartialLearnConsumable { _consumable = new List<MartialModifyStat> { Effect(costStat, -5) } }); Select(costBook);
        var costRow = Row(); Contains(costRow.Detail, "25 → 20", "cost is read-only projection");
        Contains(costRow.Detail, "20 → 25", "effect sees projected consumed state"); Assert(costStat._value == 25, "cost simulation does not mutate source");
        var orBook = Book(); var first = new Misc { _id = "first", _currentCount = 0 }; var second = new Misc { _id = "second", _currentCount = 1 };
        orBook._martialLevelData[0]._consumable.Add(new MartialLearnConsumable { _logicType = 1, _consumable = new List<MartialModifyStat>
        {
            new MartialModifyStat { _stat = new MartialStatReference { _type = 5, _misc = first }, _value = -1 },
            new MartialModifyStat { _stat = new MartialStatReference { _type = 5, _misc = second }, _value = -1 }
        }}); Select(orBook); var orRow = Row(); Contains(orRow.Detail, "second 보유 수 -1", "OR cost first positive source");
        Assert(!orRow.Detail.Contains("first 보유 수", StringComparison.Ordinal) && second._currentCount == 1, "zero alternative skipped without inventory mutation");
    }
    private static void Scheduling()
    {
        NewContext(); Select(Book(Effect(new GameStat { _value = 10 }, 2)));
        var engine = new RefreshEngine(); int reads = 0; int discovery = 0; bool fail = false;
        Func<List<DisplayQuery>> discover = () => { discovery++; return new List<DisplayQuery> { new DisplayQuery("same", () => { reads++; if (fail) throw new Exception("failure"); return Row(); }) }; };
        for(int i=0;i<20;i++) engine.Tick("shop", true, true, i*.01f, false, false, discover, null, null);
        Assert(reads == 1, "healthy waits quarter second");
        fail=true; engine.Tick("shop", true, true, .26f, false, false, discover, null, null); int failed=reads;
        engine.Tick("shop", true, true, 1f, false, false, discover, null, null); Assert(reads == failed, "failure waits five seconds");
        engine.Tick("shop", true, true, 5.27f, false, false, discover, null, null); Assert(reads == failed+1, "failure retries when due");
        int before=discovery; engine.Tick("shop", false, true, 20f, false, false, discover, null, null); Assert(discovery == before, "hidden discovery stopped");
        engine.Tick("shop", true, false, 30f, false, false, discover, null, null); Assert(discovery == before, "OFF discovery stopped");
    }
    private static void Metadata(string referenceRoot)
    {
        string managed = System.IO.Path.Combine(referenceRoot, "Mortal_Data", "Managed");
        using (var core=AssemblyDefinition.ReadAssembly(System.IO.Path.Combine(managed,"Mortal.Core.dll")))
        using (var story=AssemblyDefinition.ReadAssembly(System.IO.Path.Combine(managed,"Mortal.Story.dll")))
        {
            Assert(core.MainModule.GetType("Mortal.Core.Consumables").Fields.Any(f=>f.Name=="_effects"&&f.FieldType.FullName=="Mortal.Core.ItemEffect[]"), "real consumable field");
            var book=core.MainModule.GetType("Mortal.Core.Book");
            Assert(book.Fields.Any(f=>f.Name=="_martialLevelData"&&f.FieldType.FullName=="Mortal.Core.MartialLevelData[]"),"real level field");
            var shop=story.MainModule.GetType("Mortal.Story.ShopPanel");
            foreach (string stock in new[] { "get_Books", "get_Miscs", "get_Specials" })
                Assert(shop.Methods.Single(m=>m.Name=="Init").Body.Instructions.Any(i=>i.Operand is MethodReference m&&m.Name==stock), "actual shop stock route " + stock);
            Assert(shop.Fields.Any(f=>f.Name=="_dialogDesc"&&f.FieldType.FullName=="UnityEngine.UI.Text"),"real explanation area");
            Assert(shop.Methods.Single(m=>m.Name=="OnItemHover").Parameters.Single().ParameterType.FullName=="Mortal.Core.IGameItem","real selection callback signature");
            var select=story.MainModule.GetType("Mortal.Story.ShopItemPanel").Methods.Single(m=>m.Name=="OnSelect");
            Assert(select.Body.Instructions.Any(i=>i.Operand is MethodReference m&&m.Name=="Raise"),"real gamepad selection forwards notification");
            foreach(string name in new[]{"HideDialog","OnDisable","CloseButtonClick"}) Assert(shop.Methods.Any(m=>m.Name==name),"real close boundary "+name);
        }
        using(var assembly=AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().Location))
        {
            var source=assembly.MainModule.GetType("DiceMaster.Information.ShopEffectSource");
            var forbidden=new HashSet<string>(new[]{"Check","Execute","GetRate","GetResult","Learn","Apply","Consumable","Raise","AddValue","AddCount","SetCount","BuyButtonClick","SellButtonClick"},StringComparer.Ordinal);
            foreach(var type in source.NestedTypes.Concat(new[]{source, assembly.MainModule.GetType("DiceMaster.Information.ShopUseEffects")})) foreach(var method in type.Methods.Where(m=>m.HasBody))
                foreach(var instruction in method.Body.Instructions)
                    if(instruction.Operand is MethodReference called) Assert(!forbidden.Contains(called.Name),"provider IL never calls gameplay "+called.FullName);
        }
    }
    public static int Main(string[] args)
    {
        try
        {
            VisibilityAndSummary(); ConsumablesAndUnknown(); FoodUses(); Conditions(); EffectSemantics(); Scheduling();
            var harmony=new HarmonyLib.Harmony(); ShopInformationPatches.Install(harmony); Assert(harmony.Methods.SequenceEqual(new[]{"OnItemHover","HideDialog","OnDisable","CloseButtonClick"}),"exact observer patch targets");
            if(args.Length>0) Metadata(args[0]);
            Console.WriteLine("PASS: ShopInformationTests "+assertions+" assertions; actual provider and refresh source with explicit Unity/game/Harmony fixtures. Actual metadata contracts checked when reference root supplied; Windows/Unity rendering/physical input NOT executed."); return 0;
        }
        catch(Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
