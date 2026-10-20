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
  InformationRuntime.Tick(null,true,1.05f);Check(MapGuideSource.Reads==1,"healthy budget .25");
  InformationRuntime.Tick(null,true,1.1f,false,false);Check(MapGuideSource.Reads==1,"log occlusion hides automatic game information");
  InformationRuntime.Tick(null,true,1.25f);Check(MapGuideSource.Reads==2,"healthy query at .25");
  MapGuideSource.Fail=true;InformationRuntime.Tick(null,true,1.5f);Check(MapGuideSource.Reads==3,"failure observed once");
  Check(LogBridge.Model.Tab("지도 안내").Snapshot.Rows[0].RefreshState==DiceMaster.LogUI.LogRefreshState.RetainedFailure,"same context retains explicitly stale last good");
  InformationRuntime.Tick(null,true,5);Check(MapGuideSource.Reads==3,"failure waits five seconds");
  MapGuideSource.Visible=false;InformationRuntime.Tick(null,true,8);Check(MapGuideSource.Reads==3,"hidden failed row cannot retry");
  MapGuideSource.ContextKey="new-map";InformationRuntime.Tick(null,true,8.1f);Check(!LogBridge.Model.Tab("지도 안내").Snapshot.Rows[0].HasLastGood,"target change discards old snapshot even hidden");
  MapGuideSource.Fail=false;MapGuideSource.Visible=true;InformationRuntime.Tick(null,true,9);Check(MapGuideSource.Reads==4,"new context reads without previous failure backoff");
  InformationRuntime.Toggle(0);InformationRuntime.Toggle(1);int read=MapGuideSource.Reads;InformationRuntime.Tick("지도 안내",true,10);Check(MapGuideSource.Reads==read,"both map features off means no collection");
  Check(ConfigManager.EnableBranchNotices.Value&&ConfigManager.EnableShopEffectInfo.Value,"independent settings retained");
  ShopEffectSource.Visible=true;InformationRuntime.Tick(null,true,11);Check(ShopEffectSource.Reads==1,"current shop queried");
  var row=LogBridge.Model.Tab("상점 효과").Snapshot.Rows[0];Check(row.Target==null&&!row.ReservationAllowed&&!row.CanReserve,"readonly publications never issue reservation authority");
  InformationRuntime.Tick("상점 효과",true,11.1f);Check(ShopEffectSource.Reads==1,"automatic and detail share same snapshot budget");
  ConfigManager.EnableShopEffectInfo.Fail=true;InformationRuntime.Tick(null,true,11.3f);Check(!InformationRuntime.Enabled(3)&&InformationRuntime.Enabled(2),"read failure disables only affected feature");
  int logs=DiceMaster.DicePlugin.Messages.Count;InformationRuntime.Tick(null,true,11.4f);Check(DiceMaster.DicePlugin.Messages.Count==logs,"repeated setting failure diagnosed once");
  ConfigManager.EnableShopEffectInfo.Fail=false;InformationRuntime.Tick(null,true,11.5f);Check(InformationRuntime.Enabled(3),"setting read recovery");
  InformationRuntime.Tick("분기 기록",true,12);Check(BranchHistory.Reads==1,"branch detail query only while shown");
  InformationRuntime.Tick(null,true,13);Check(BranchHistory.Reads==1,"hidden history has no periodic reads");
  InformationRuntime.Toggle(2);Check(!BranchHistory.Enabled,"branch off immediately stops observation");
  ConfigManager.EnableOptionInfo.Value=false;InformationRuntime.RefreshSettings();Check(InformationRuntime.Enabled(3),"parent startup restart contract latched");
  InformationRuntime.Clear();InformationRuntime.Install(new HarmonyLib.Harmony());Check(Enumerable.Range(0,4).All(i=>!InformationRuntime.Enabled(i)),"parent off on startup suppresses saved true values");
  Check(ConfigManager.EnableShopEffectInfo.Value,"parent suppression preserves individual setting");
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
 }
}
