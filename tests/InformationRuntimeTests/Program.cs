using System;
using System.Linq;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using DiceMaster.Config;
using Mono.Cecil;
static class Program {
 static int checks;static void Check(bool v,string m){checks++;if(!v)throw new InvalidOperationException(m);}
 static void Main(string[] args){
  InformationRuntime.Install(new HarmonyLib.Harmony());
  Check(Enumerable.Range(0,4).All(InformationRuntime.Enabled),"four absent settings use true");
  InformationRuntime.Tick(null,true,0);Check(MapGuideSource.Reads==0&&ShopEffectSource.Reads==0&&BranchHistory.Reads==0,"all hidden means zero queries");
  MapGuideSource.Visible=true;InformationRuntime.Tick(null,true,1);Check(MapGuideSource.Reads==1,"visible current map queried immediately");
  var mapRows=LogBridge.Model.Tab("지도 안내").Snapshot.Rows;
  Check(mapRows.Count==2&&mapRows[0].Id=="mission/first"&&mapRows[1].Id=="event/second","one scheduled snapshot publishes separate stable map entries");
  Check(mapRows.All(x=>x.Target==null&&!x.ReservationAllowed&&!x.CanReserve),"each grouped map row has no execution or reservation authority");
  Check(MapGuideSource.DetailReads==0&&mapRows.All(x=>x.Description=="상세에서 확인"),"automatic map requests do not prepare log-only condition and effect text");
  InformationRuntime.Tick(null,true,1.05f);Check(MapGuideSource.Reads==1,"healthy budget .25");
  InformationRuntime.Tick(null,true,1.1f,false,false);Check(MapGuideSource.Reads==1,"log occlusion hides automatic game information");
  InformationRuntime.Tick("지도 안내",false,1.2f);Check(MapGuideSource.Reads==1,"unfocused detail stops map reads as well as automatic display");
  InformationRuntime.Tick(null,true,1.25f);Check(MapGuideSource.Reads==2,"healthy query at .25");
  MapGuideSource.Fail=true;InformationRuntime.Tick(null,true,1.5f);Check(MapGuideSource.Reads==3,"failure observed once");
  Check(LogBridge.Model.Tab("지도 안내").Snapshot.Rows[0].RefreshState==DiceMaster.LogUI.LogRefreshState.RetainedFailure,"same context retains explicitly stale last good");
  Check(LogBridge.Model.Tab("지도 안내").Snapshot.Rows.All(x=>x.HasLastGood&&x.RefreshState==DiceMaster.LogUI.LogRefreshState.RetainedFailure),"aggregate source failure labels every retained child row stale");
  InformationRuntime.Tick(null,true,5);Check(MapGuideSource.Reads==3,"failure waits five seconds");
  MapGuideSource.Visible=false;InformationRuntime.Tick(null,true,8);Check(MapGuideSource.Reads==3,"hidden failed row cannot retry");
  MapGuideSource.ContextKey="new-map";InformationRuntime.Tick(null,true,8.1f);Check(!LogBridge.Model.Tab("지도 안내").Snapshot.Rows[0].HasLastGood,"target change discards old snapshot even hidden");
  MapGuideSource.Fail=false;MapGuideSource.Visible=true;InformationRuntime.Tick(null,true,9);Check(MapGuideSource.Reads==4,"new context reads without previous failure backoff");
  MapGuideSource.Visible=false;MapGuideSource.NoSelectionSession=true;MapGuideSource.ContextKey="global-session";
  InformationRuntime.Tick(null,true,9.1f);Check(MapGuideSource.Reads==4,"unselected map session does not start hidden global queries");
  InformationRuntime.Tick("지도 안내",true,9.2f);Check(MapGuideSource.Reads==5,"visible detail may read observed no-place session without selection");
  Check(LogBridge.Model.Tab("지도 안내").Snapshot.Rows[0].Text.Contains("장소 무관"),"no-place group is published by same readonly log page");
  InformationRuntime.Tick(null,true,9.6f);Check(MapGuideSource.Reads==5,"hidden no-place detail stops periodic collection");
  MapGuideSource.NoSelectionSession=false;MapGuideSource.Visible=true;
  InformationRuntime.Toggle(0);MapGuideSource.Visible=false;MapGuideSource.NoSelectionSession=true;int beforeOnlyHints=MapGuideSource.Reads;InformationRuntime.Tick("지도 안내",true,9.8f);Check(MapGuideSource.Reads==beforeOnlyHints&&LogBridge.Model.Tab("지도 안내").Snapshot.Rows[0].Title=="현재 지도 장소 없음","one-shot-only without selection is empty rather than perpetual waiting");InformationRuntime.Toggle(1);int read=MapGuideSource.Reads;InformationRuntime.Tick("지도 안내",true,10);Check(MapGuideSource.Reads==read,"both map features off means no collection");
  Check(ConfigManager.EnableBranchNotices.Value&&ConfigManager.EnableShopEffectInfo.Value,"independent settings retained");
  ShopEffectSource.Visible=true;InformationRuntime.Tick(null,true,11);Check(ShopEffectSource.Reads==1,"current shop queried");
  var row=LogBridge.Model.Tab("상점 효과").Snapshot.Rows[0];Check(row.Target==null&&!row.ReservationAllowed&&!row.CanReserve,"readonly publications never issue reservation authority");
  Check(ShopEffectSource.Summary=="학습: 근력 +5","shop receives the compact effect summary without full conditions");
  InformationRuntime.Tick("상점 효과",true,11.1f);Check(ShopEffectSource.Reads==1,"automatic and detail share same snapshot budget");
  ShopEffectSource.Fail=true;InformationRuntime.Tick(null,true,11.26f);
  Check(ShopEffectSource.Summary.StartsWith(RefreshEngine.RetainedFailureStatus+"\n")&&ShopEffectSource.Summary.Contains("근력 +5"),"retained shop effect is explicitly stale before the effect text");
  int failedShopReads=ShopEffectSource.Reads;InformationRuntime.Tick(null,true,11.27f);
  Check(ShopEffectSource.Reads==failedShopReads,"rendering a retained multiline effect does not bypass failure backoff");ShopEffectSource.Fail=false;
  ConfigManager.EnableShopEffectInfo.Fail=true;InformationRuntime.Tick(null,true,11.3f);Check(!InformationRuntime.Enabled(3)&&InformationRuntime.Enabled(2),"read failure disables only affected feature");
  int logs=DiceMaster.DicePlugin.Messages.Count;InformationRuntime.Tick(null,true,11.4f);Check(DiceMaster.DicePlugin.Messages.Count==logs,"repeated setting failure diagnosed once");
  ConfigManager.EnableShopEffectInfo.Fail=false;InformationRuntime.Tick(null,true,11.5f);Check(InformationRuntime.Enabled(3),"setting read recovery");
  InformationRuntime.Tick("분기 기록",true,12);Check(BranchHistory.Reads==1,"branch detail query only while shown");
  InformationRuntime.Tick(null,true,13);Check(BranchHistory.Reads==1,"hidden history has no periodic reads");
  InformationRuntime.Toggle(2);Check(!BranchHistory.Enabled,"branch off immediately stops observation");
  ConfigManager.EnableOptionInfo.Value=false;InformationRuntime.RefreshSettings();Check(InformationRuntime.Enabled(3),"parent startup restart contract latched");
  InformationRuntime.Clear();InformationRuntime.Install(new HarmonyLib.Harmony());Check(Enumerable.Range(0,4).All(i=>!InformationRuntime.Enabled(i)),"parent off on startup suppresses saved true values");
  Check(ConfigManager.EnableShopEffectInfo.Value,"parent suppression preserves individual setting");
  ConfigManager.EnableOptionInfo.Value=true;ConfigManager.EnableMapGuide.Value=true;ConfigManager.EnableOneShotHints.Value=true;
  InformationRuntime.Install(new HarmonyLib.Harmony());MapGuideSource.Visible=true;MapGuideSource.NoSelectionSession=false;MapGuideSource.ContextKey="detail-mode-test";
  int modeReads=MapGuideSource.Reads,detailReads=MapGuideSource.DetailReads;
  InformationRuntime.Tick(null,true,30);
  Check(MapGuideSource.Reads==modeReads+1&&MapGuideSource.DetailReads==detailReads,"passive map uses the lightweight provider mode");
  InformationRuntime.Tick("지도 안내",true,30.01f);
  Check(MapGuideSource.Reads==modeReads+2&&MapGuideSource.DetailReads==detailReads+1,"opening map detail upgrades immediately without waiting for the automatic refresh interval");
  var details=LogBridge.Model.Tab("지도 안내").Snapshot.Rows;
  Check(details[0].Description=="첫 조건과 효과"&&details[1].Description=="둘째 조건과 효과","explicit detail belongs to its individual map entry");
  InformationRuntime.Tick("지도 안내",true,30.02f);
  Check(MapGuideSource.DetailReads==detailReads+1,"detail mode keeps the healthy quarter-second budget");
  InformationRuntime.Tick(null,true,30.03f);
  Check(MapGuideSource.Reads==modeReads+3&&MapGuideSource.DetailReads==detailReads+1,"closing detail returns to lightweight mode and stops detailed work");
  Check(InformationRuntime.OptionTag("O_unknown")==""&&InformationRuntime.EventTag(new object())==""&&InformationRuntime.OptionDetail("O_unknown").Contains("확인 불가"),"automatic badges and cached detail diagnostics use separate APIs");
  if(args.Length>0) Metadata(args[0], args.Length>1 ? args[1] : null);
  Console.WriteLine("PASS: InformationRuntimeTests: "+checks+" scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.");
 }
 static void Metadata(string path, string references){
  using var a=AssemblyDefinition.ReadAssembly(path);var m=a.MainModule;
  var plugin=m.GetType("DiceMaster.DicePlugin");
  foreach(string method in new[]{"Awake","Update","OnGUI"}){
   var calls=plugin.Methods.Single(x=>x.Name==method).Body.Instructions.Select(x=>x.Operand).OfType<MethodReference>();
   Check(calls.Any(x=>x.DeclaringType.FullName=="DiceMaster.Information.InformationRuntime"),"delivered plugin "+method+" wired to real runtime");
  }
  if(references!=null) {
   using(var free=AssemblyDefinition.ReadAssembly(System.IO.Path.Combine(references,"Mortal_Data","Managed","Mortal.Free.dll"))) {
    Check(free.MainModule.GetType("Mortal.Free.FreePositionData").Fields.Single(x=>x.Name=="_conditionFlags").FieldType.FullName=="Mortal.Free.PositionConditionFlagItem[]","supplied actual activity entry flag array metadata is pinned");
    Check(free.MainModule.GetType("Mortal.Free.PositionFlagItem").Methods.Single(x=>x.Name=="Setup").Body.Instructions.Count(x=>x.OpCode.Code==Mono.Cecil.Cil.Code.Stfld&&x.Operand is FieldReference f&&f.DeclaringType.FullName=="Mortal.Core.FlagData"&&f.Name=="State")==2,"supplied actual entry flag setup mutates both true and false outcomes");
   }
   foreach(string typeName in new[]{"DiceMaster.Information.MapInformationPatches","DiceMaster.Information.OptionInformationPatches"}) {
    var root=m.GetType(typeName);var install=root.Methods.Single(x=>x.Name=="Install");
    foreach(var target in root.NestedTypes) {
     var patch=target.CustomAttributes.SingleOrDefault(x=>x.AttributeType.FullName=="HarmonyLib.HarmonyPatch");
     if(patch==null)continue;
     var gameType=(TypeReference)patch.ConstructorArguments[0].Value;
     string method=(string)patch.ConstructorArguments[1].Value;
     var scope=(AssemblyNameReference)gameType.Scope;
     using var game=AssemblyDefinition.ReadAssembly(System.IO.Path.Combine(references,"Mortal_Data","Managed",scope.Name+".dll"));
     var actual=game.MainModule.GetType(gameType.FullName);
     Check(actual!=null&&actual.Methods.Any(x=>x.Name==method),"supplied actual game hook signature "+gameType.FullName+"."+method);
     Check(install.Body.Instructions.Any(x=>x.Operand is TypeReference tr&&tr.FullName==target.FullName),"delivered installer registers "+target.Name);
    }
   }
  }
  var config=m.GetType("DiceMaster.Config.ConfigManager");foreach(string key in new[]{"EnableMapGuide","EnableOneShotHints","EnableBranchNotices","EnableShopEffectInfo"})Check(config.Fields.Any(f=>f.Name==key),"delivered persistent setting "+key);
  var overlay = m.GetType("DiceMaster.Information.InformationRuntime").NestedTypes.Single(x=>x.Name=="PassiveLabel").Methods.Single(x=>x.Name=="Prepare");
  Check(overlay.Body.Instructions.Select(x=>x.Operand).OfType<MethodReference>().Any(x=>x.DeclaringType.FullName=="DiceMaster.Information.InformationLayout"&&x.Name=="Place"),"delivered passive panels use tested bounded fixed-corner layout");
  Check(m.GetType("DiceMaster.Information.ShopInformationPatches")!=null&&m.GetType("DiceMaster.Information.MapInformationPatches")!=null&&m.GetType("DiceMaster.Information.BranchObservationPatches")!=null,"delivered actual game observers included");
  var runtime=m.GetType("DiceMaster.Information.InformationRuntime");
  var publish=runtime.Methods.Single(x=>x.Name=="Publish");
  Check(publish.Body.Instructions.Any(x=>x.Operand is FieldReference f&&f.DeclaringType.FullName=="DiceMaster.Information.MapGuideSnapshot"&&f.Name=="Entries"),"delivered runtime publishes typed map snapshot entries");
  Check(runtime.Methods.Single(x=>x.Name=="DrawCore").Body.Instructions.Any(x=>x.Operand is FieldReference f&&f.DeclaringType.FullName=="DiceMaster.Information.MapGuideSnapshot"&&f.Name=="Summary"),"delivered overlay reads the same snapshot summary");
  Check(m.GetType("DiceMaster.Information.MapGuideSource").Methods.Any(x=>x.HasBody&&x.Body.Instructions.Any(i=>i.Operand is MethodReference call&&call.DeclaringType.FullName=="DiceMaster.Information.MapRouteDefinitions"&&call.Name=="Advance")),"delivered map provider prepares actual bounded route engine");
  var forbidden=new[]{"Check","Execute","GetResult","GetRate","Position","Switch","Condition","SetStoryScript","SetCurrentKey","SetConditionFlag","GetRandomResult","UpdateCheckMissions","GetActiveMissions"};
  foreach(var type in m.Types.Where(x=>x.Namespace=="DiceMaster.Information"&&(x.Name.StartsWith("MapRoute")||x.Name=="MapGuideSource"))) {
   foreach(var method in DeepTypes(type).SelectMany(x=>x.Methods).Where(x=>x.HasBody))
    foreach(var call in method.Body.Instructions.Select(x=>x.Operand).OfType<MethodReference>())
     Check(!(call.DeclaringType.Namespace.StartsWith("Mortal.")&&forbidden.Contains(call.Name)),"delivered map IL does not call gameplay chain "+call.FullName);
  }
 }
 static System.Collections.Generic.IEnumerable<TypeDefinition> DeepTypes(TypeDefinition type) {
  yield return type;
  foreach(var nested in type.NestedTypes)foreach(var value in DeepTypes(nested))yield return value;
 }
}
