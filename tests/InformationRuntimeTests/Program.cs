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
  ChoiceCadence();
  MarkerCadence();
  NativeWindowCadence();
  MapDiagnosticPublication();
  DiceCadence();
  if(args.Length>0) Metadata(args[0], args.Length>1 ? args[1] : null);
  Console.WriteLine("PASS: InformationRuntimeTests: "+checks+" scheduling/settings/read-only/DLL wiring assertions; provider/game/UI boundaries are fixtures.");
 }
 static void DiceCadence(){
  InformationRuntime.Clear();MapGuideSource.Visible=true;ChoiceStorySource.Visible=false;DiceGuideSource.Active=true;
  var snapshot=DiceGuideSource.Snapshot=new DiceGuideSnapshot{ContextKey=DiceGuideSource.ContextKey};snapshot.Entries.Add(new InformationRow("dice/1","결과 1","연결","후보","연결 상세"));
  int reads=DiceGuideSource.Reads,map=MapGuideSource.Reads;InformationRuntime.Toggle(2);
  InformationRuntime.Tick(null,true,200);Check(DiceGuideSource.Reads==reads+1&&MapGuideSource.Reads==map,"dice guide is independent of branch recording and hides underlying map reads");
  Check(LogBridge.Model.Tab("지도 안내").Snapshot.Rows.Single().Id=="dice/1","dice detail owns existing map tool without creating a fifth tab");
  InformationRuntime.Tick(null,true,200.1f);Check(DiceGuideSource.Reads==reads+1,"dice healthy query keeps quarter-second cadence");
  InformationVisibility.TestBlocked=true;InformationRuntime.Tick(null,true,201);Check(DiceGuideSource.Reads==reads+1,"blocked dice does not query");InformationVisibility.TestBlocked=false;
  InformationRuntime.Tick(null,true,202,false,false);Check(DiceGuideSource.Reads==reads+1,"hidden automatic view stops dice queries");
  InformationRuntime.Tick("지도 안내",true,203,false,false);Check(DiceGuideSource.Reads==reads+2,"existing detail tool can inspect dice without automatic rendering");
  InformationRuntime.Toggle(2);InformationRuntime.Clear();
 }
 static void MapDiagnosticPublication(){
  InformationRuntime.Clear();InformationRuntime.Install(new HarmonyLib.Harmony());ChoiceStorySource.Visible=false;
  MapGuideSource.Visible=false;MapGuideSource.NoSelectionSession=false;MapGuideSource.Fail=false;MapGuideSource.ContextKey="diagnostic-no-session";
  int reads=MapGuideSource.Reads,diagnostics=MapGuideSource.DiagnosticReads;
  InformationRuntime.Tick("지도 안내",true,100,false,false);
  var rows=LogBridge.Model.Tab("지도 안내").Snapshot.Rows;
  Check(MapGuideSource.Reads==reads&&rows.Any(row=>row.Title=="지도 조회 성능"&&!row.CanReserve&&row.Target==null),"explicit map tool publishes read-only diagnostics even with no discovered place query");
  Check(MapGuideSource.DiagnosticReads>diagnostics,"no-session diagnostics have an independent cache-only publication path");
  diagnostics=MapGuideSource.DiagnosticReads;InformationRuntime.Tick(null,true,100.1f,false,false);
  Check(MapGuideSource.DiagnosticReads==diagnostics,"closing detail stops diagnostic formatting");
  MapGuideSource.NoSelectionSession=true;MapGuideSource.ContextKey="diagnostic-failure";MapGuideSource.Fail=true;
  InformationRuntime.Tick("지도 안내",true,101,false,false);rows=LogBridge.Model.Tab("지도 안내").Snapshot.Rows;
  Check(rows.Any(row=>row.RefreshState==DiceMaster.LogUI.LogRefreshState.ReadFailure)&&rows.Any(row=>row.Title=="지도 조회 성능"&&row.RefreshState==DiceMaster.LogUI.LogRefreshState.Healthy),"a first place-read failure remains failed while independent cached diagnostics remain available");
  reads=MapGuideSource.Reads;MapGuideSource.Fail=false;InformationRuntime.Tick("지도 안내",true,105.99f,false,false);
  Check(MapGuideSource.Reads==reads,"publishing diagnostics never bypasses the failed place query's five-second backoff");
  InformationRuntime.Tick("지도 안내",true,106.01f,false,false);rows=LogBridge.Model.Tab("지도 안내").Snapshot.Rows;
  Check(MapGuideSource.Reads==reads+1&&rows.Count(row=>row.Title=="지도 조회 성능")==1&&rows.Any(row=>row.Id=="mission/first"&&row.RefreshState==DiceMaster.LogUI.LogRefreshState.Healthy),"place recovery and one diagnostic row coexist at the retry deadline");
  MapGuideSource.Fail=true;InformationRuntime.Tick("지도 안내",true,106.3f,false,false);rows=LogBridge.Model.Tab("지도 안내").Snapshot.Rows;
  Check(rows.Any(row=>row.Id=="mission/first"&&row.RefreshState==DiceMaster.LogUI.LogRefreshState.RetainedFailure)&&rows.Single(row=>row.Title=="지도 조회 성능").RefreshState==DiceMaster.LogUI.LogRefreshState.Healthy,"retained place values keep their stale status without labeling independently sampled cache diagnostics stale");
  diagnostics=MapGuideSource.DiagnosticReads;InformationVisibility.TestBlocked=true;InformationRuntime.Tick("지도 안내",true,107,false,false);
  Check(MapGuideSource.DiagnosticReads==diagnostics,"native window occlusion stops diagnostic formatting as well as place queries");InformationVisibility.TestBlocked=false;
  InformationRuntime.Toggle(0);InformationRuntime.Toggle(1);diagnostics=MapGuideSource.DiagnosticReads;InformationRuntime.Tick("지도 안내",true,108,false,false);
  Check(MapGuideSource.DiagnosticReads==diagnostics,"both map settings OFF also stop cache diagnostics");InformationRuntime.Toggle(0);InformationRuntime.Toggle(1);
  MapGuideSource.Fail=false;MapGuideSource.NoSelectionSession=false;InformationRuntime.Clear();
 }
 static void NativeWindowCadence(){
  InformationRuntime.Clear();MapGuideSource.Visible=true;MapGuideSource.NoSelectionSession=true;ChoiceStorySource.Visible=false;
  int map=MapGuideSource.Reads,marker=MapGuideSource.MarkerReads,choice=ChoiceStorySource.Reads;
  InformationVisibility.TestBlocked=true;
  for(int i=0;i<100;i++)InformationRuntime.Tick(null,true,80+i*.01f);
  Check(MapGuideSource.Reads==map&&MapGuideSource.MarkerReads==marker,"native status window stops selected/global map queries despite active source objects");
  ChoiceStorySource.Visible=true;InformationRuntime.Tick(null,true,82);
  Check(ChoiceStorySource.Reads==choice,"native story window stops underlying action-menu preparation");
  InformationVisibility.TestBlocked=false;InformationRuntime.Tick(null,true,82.01f);
  Check(ChoiceStorySource.Reads==choice+1,"closing a window resumes the current native menu with a fresh read");
  ChoiceStorySource.Visible=false;InformationRuntime.Tick(null,true,82.02f);
  Check(MapGuideSource.Reads==map+1&&MapGuideSource.MarkerReads==marker+1,"returning to the map resumes its current selected/global publications");
  InformationVisibility.TestBlocked=true;InformationRuntime.Tick("지도 안내",true,83,false,false);
  Check(MapGuideSource.Reads==map+1,"native window also prevents explicit old-map detail queries");
  InformationRuntime.Toggle(0);InformationRuntime.Toggle(1);int reads=InformationVisibility.Reads;
  InformationRuntime.Tick(null,true,84);Check(InformationVisibility.Reads==reads,"both information flags OFF skip native-window reads");
  InformationRuntime.Toggle(0);InformationRuntime.Toggle(1);InformationVisibility.TestBlocked=false;
  MapGuideSource.Visible=false;MapGuideSource.NoSelectionSession=false;
 }
 static void MarkerCadence(){
  MapGuideSource.ContextKey="marker-cadence";MapGuideSource.Visible=false;MapGuideSource.NoSelectionSession=true;
  int before=MapGuideSource.MarkerReads,map=MapGuideSource.Reads;
  InformationRuntime.Tick(null,true,60);
  Check(MapGuideSource.MarkerReads==before+1&&MapGuideSource.Reads==map,"unselected visible map collects its shallow marker batch without starting selected-place analysis");
  InformationRuntime.Tick(null,true,60.1f);Check(MapGuideSource.MarkerReads==before+1,"marker healthy refresh is quarter-second bounded");
  InformationRuntime.Tick(null,true,60.25f);Check(MapGuideSource.MarkerReads==before+2,"marker batch refreshes at normal deadline");
  MapGuideSource.MarkerFail=true;InformationRuntime.Tick(null,true,60.5f);Check(MapGuideSource.MarkerReads==before+3,"marker failure is observed once");
  InformationRuntime.Tick(null,true,64);Check(MapGuideSource.MarkerReads==before+3,"marker failure uses five-second backoff");
  InformationRuntime.Tick(null,false,66);InformationRuntime.Tick(null,true,66,false,false);InformationRuntime.Tick(null,true,66,false,true,true);
  Check(MapGuideSource.MarkerReads==before+3,"unfocused, hidden, and popup-occluded markers cannot retry");
  ChoiceStorySource.Visible=true;InformationRuntime.Tick(null,true,66);Check(MapGuideSource.MarkerReads==before+3,"physical choice menu stops underlying map markers");ChoiceStorySource.Visible=false;
  MapGuideSource.MarkerFail=false;InformationRuntime.Tick(null,true,66.1f);Check(MapGuideSource.MarkerReads==before+4,"visible marker failure recovers at its retry deadline");
  InformationRuntime.Tick("지도 안내",true,67,false,false);Check(MapGuideSource.MarkerReads==before+4,"explicit log does not start a second global automatic marker refresh");
  InformationRuntime.Toggle(0);InformationRuntime.Toggle(1);InformationRuntime.Tick(null,true,68);
  Check(MapGuideSource.MarkerReads==before+4,"both map settings OFF stop marker reads");
  InformationRuntime.Toggle(0);InformationRuntime.Toggle(1);InformationRuntime.Tick(null,true,68.01f);
  Check(MapGuideSource.MarkerReads==before+5,"reenabling clears marker backoff and requests a current snapshot");
  MapGuideSource.NoSelectionSession=false;
 }
 static void ChoiceCadence(){
  ChoiceStorySource.Visible=true;ChoiceStorySource.ContextKey="male-room";
  int map=MapGuideSource.Reads;
  InformationRuntime.Tick(null,true,40);
  Check(ChoiceStorySource.Reads==1&&MapGuideSource.Reads==map,"visible native choice owns information reads while underlying map remains silent");
  var menu=LogBridge.Model.Tab("지도 안내").Snapshot.Rows;
  Check(menu.Count==1&&menu[0].Title=="사사형"&&!menu[0].CanReserve&&menu[0].Target==null,"current choice publishes read-only stories in existing map log tool");
  InformationRuntime.Tick(null,true,40.1f);Check(ChoiceStorySource.Reads==1,"choice uses healthy quarter-second budget");
  InformationRuntime.Tick("지도 안내",true,40.11f);Check(ChoiceStorySource.Reads==2&&ChoiceStorySource.DetailReads==1,"explicit detail upgrades choice paths immediately");
  Check(LogBridge.Model.Tab("지도 안내").Snapshot.Rows[0].Description=="사사형 → 대화 항목","choice detail retains complete selection path");
  InformationRuntime.Tick("지도 안내",true,40.2f);Check(ChoiceStorySource.Reads==2,"choice detail and passive share scheduling owner");
  ChoiceStorySource.Fail=true;InformationRuntime.Tick("지도 안내",true,40.37f);
  Check(ChoiceStorySource.Reads==3&&LogBridge.Model.Tab("지도 안내").Snapshot.Rows[0].RefreshState==DiceMaster.LogUI.LogRefreshState.RetainedFailure,"choice failure retains only current context with stale marker");
  InformationRuntime.Tick("지도 안내",true,41);Check(ChoiceStorySource.Reads==3,"choice failure backoff is five seconds");
  InformationRuntime.Tick("다른 도구",true,46,false,false);Check(ChoiceStorySource.Reads==3,"hidden choice failure cannot retry");
  InformationRuntime.Tick("지도 안내",false,46);Check(ChoiceStorySource.Reads==3,"unfocused choice detail cannot query");
  ChoiceStorySource.ContextKey="next-menu";ChoiceStorySource.Fail=false;
  InformationRuntime.Tick("지도 안내",true,46.01f);Check(ChoiceStorySource.Reads==4,"new menu clears previous failure backoff and old snapshot");
  InformationRuntime.Toggle(0);int before=ChoiceStorySource.Reads;
  InformationRuntime.Tick("지도 안내",true,47);Check(ChoiceStorySource.Reads==before+1&&!ChoiceStorySource.GuideEnabled()&&ChoiceStorySource.OneShotEnabled(),"map-guide OFF stops general/waiting guidance while one-shot menu collection stays independently enabled");
  int occludedMap=MapGuideSource.Reads; InformationRuntime.Tick(null,true,47.25f);
  Check(MapGuideSource.Reads==occludedMap,"physical menu occludes map one-shot reads while guide OFF and one-shot ON");
  Check(InformationRuntime.Enabled(1),"choice guide OFF preserves one-shot setting");
  ChoiceStorySource.Visible=false;InformationRuntime.Toggle(0);
  InformationRuntime.Tick(null,true,48);
  Check(MapGuideSource.Reads>map&&LogBridge.Model.Tab("지도 안내").Snapshot.Rows[0].Id=="mission/first","leaving choice restores current map context without old menu rows");
 }
 static void Metadata(string path, string references){
  using var a=AssemblyDefinition.ReadAssembly(path);var m=a.MainModule;
  var plugin=m.GetType("DiceMaster.DicePlugin");
  foreach(string method in new[]{"Awake","Update","OnGUI"}){
   var calls=plugin.Methods.Single(x=>x.Name==method).Body.Instructions.Select(x=>x.Operand).OfType<MethodReference>();
   Check(calls.Any(x=>x.DeclaringType.FullName=="DiceMaster.Information.InformationRuntime"),"delivered plugin "+method+" wired to real runtime");
  }
  if(references!=null) {
   using(var story=AssemblyDefinition.ReadAssembly(System.IO.Path.Combine(references,"Mortal_Data","Managed","Mortal.Story.dll"))) {
    var dice=story.MainModule.GetType("Mortal.Story.DiceMenuDialog");
    Check(dice.Methods.Any(x=>x.Name=="ExecuteRoll"&&x.ReturnType.FullName=="System.Collections.IEnumerator"&&x.Parameters.Count==3&&x.Parameters[0].ParameterType.FullName=="System.String[]"&&x.Parameters[2].Name=="checkName"),"supplied native dice hook captures exact options/key without replacing its IEnumerator");
    Check(!dice.Methods.Any(x=>x.Name=="UpdateOptions"),"dice inherits the actual CustomMenuDialog readiness method");
    var iterator=dice.NestedTypes.Single(x=>x.Name.Contains("<ExecuteRoll>"));
    var body=iterator.Methods.Single(x=>x.Name=="MoveNext").Body.Instructions;
    int activation=body.ToList().FindIndex(x=>x.Operand is MethodReference c&&c.DeclaringType.FullName=="UnityEngine.GameObject"&&c.Name=="SetActive"&&x.Previous?.OpCode.Code==Mono.Cecil.Cil.Code.Ldc_I4_1);
    int prepared=body.ToList().FindIndex(x=>x.Operand is MethodReference c&&c.DeclaringType.FullName=="Mortal.Core.CustomMenuDialog"&&c.Name=="UpdateOptions");
    Check(activation>=0&&prepared>activation,"supplied real iterator activates the native view before the result-row readiness hook");
    var option=story.MainModule.GetType("Mortal.Story.DiceOption");
    foreach(string name in new[]{"_sourceText","_normalText","_highText","_conditionText"})Check(option.Fields.Any(f=>f.Name==name),"native dice display field exists: "+name);
    var lua=story.MainModule.GetType("Mortal.Story.LuaManager");
    Check(lua.Fields.Single(x=>x.Name=="<Instance>k__BackingField").IsStatic,"canonical Lua singleton is an existing backing field, not a lazy getter");
    Check(lua.Fields.Single(x=>x.Name=="_luaEnvironment").FieldType.FullName=="Fungus.LuaEnvironment","canonical current Lua environment is the supplied serialized field");
    Check(story.MainModule.GetType("Mortal.Story.StatModifyManager").Fields.Single(x=>x.Name=="_talkOptionFlag").FieldType.FullName=="Mortal.Core.FlagCollectionData","canonical reset collection metadata matches the game's actual reset implementation");
   }
   using(var fungus=AssemblyDefinition.ReadAssembly(System.IO.Path.Combine(references,"Mortal_Data","Managed","Fungus.dll"))) {
    var awake=fungus.MainModule.GetType("Fungus.MenuDialog").Methods.Single(x=>x.Name=="Awake");
    Check(awake.Body.Instructions.Any(x=>x.OpCode.Code==Mono.Cecil.Cil.Code.Callvirt&&x.Operand is MethodReference c&&c.Name=="Clear"),"supplied real MenuDialog.Awake clears the menu on its first activation");
   }
   foreach(var spec in new[]{("Mortal.Free.dll","Mortal.Free.FreeManager",new[]{"_menuOpen","_statOpen","_saveOpen"}), ("Mortal.Story.dll","Mortal.Story.StoryManager",new[]{"_panelOpen","_menuOpen","_statusOpen","_logOpen"})}) {
    using var owners=AssemblyDefinition.ReadAssembly(System.IO.Path.Combine(references,"Mortal_Data","Managed",spec.Item1));
    var owner=owners.MainModule.GetType(spec.Item2);
    Check(owner.Fields.Any(f=>f.IsStatic&&f.Name=="<Instance>k__BackingField"),"native window owner has a directly readable existing singleton backing field");
    foreach(string name in spec.Item3)Check(owner.Fields.Any(f=>!f.IsStatic&&f.Name==name&&f.FieldType.FullName=="System.Boolean"),"native window flag metadata matches "+spec.Item2+"."+name);
   }
   using(var free=AssemblyDefinition.ReadAssembly(System.IO.Path.Combine(references,"Mortal_Data","Managed","Mortal.Free.dll"))) {
    Check(free.MainModule.GetType("Mortal.Free.FreePositionData").Fields.Single(x=>x.Name=="_conditionFlags").FieldType.FullName=="Mortal.Free.PositionConditionFlagItem[]","supplied actual activity entry flag array metadata is pinned");
    Check(free.MainModule.GetType("Mortal.Free.PositionFlagItem").Methods.Single(x=>x.Name=="Setup").Body.Instructions.Count(x=>x.OpCode.Code==Mono.Cecil.Cil.Code.Stfld&&x.Operand is FieldReference f&&f.DeclaringType.FullName=="Mortal.Core.FlagData"&&f.Name=="State")==2,"supplied actual entry flag setup mutates both true and false outcomes");
    foreach(var spec in new[]{("Mortal.Free.PositionConditionFlagItem","_condition","Mortal.Core.ConditionResultItem"),("Mortal.Free.PositionConditionFlagItem","_flagItem","Mortal.Free.PositionFlagItem[]"),("Mortal.Free.PositionFlagItem","_flagData","Mortal.Core.FlagData"),("Mortal.Free.PositionFlagItem","_trueState","System.Int32"),("Mortal.Free.PositionFlagItem","_falseState","System.Int32")})
     Check(free.MainModule.GetType(spec.Item1).Fields.Single(x=>x.Name==spec.Item2).FieldType.FullName==spec.Item3,"actual entry projection metadata matches "+spec.Item1+"."+spec.Item2);
   }
   foreach(string typeName in new[]{"DiceMaster.Information.MapInformationPatches","DiceMaster.Information.OptionInformationPatches","DiceMaster.Information.ChoiceStoryPatches","DiceMaster.Information.DiceGuideSource"}) {
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
  var diceSource=m.GetType("DiceMaster.Information.DiceGuideSource");
  foreach(var type in new[]{diceSource}.Concat(diceSource.NestedTypes))foreach(var method in type.Methods.Where(x=>x.HasBody))foreach(var call in method.Body.Instructions.Select(x=>x.Operand).OfType<MethodReference>())
   Check(!new[]{"ExecuteRoll","Execute","Dice","Range","PressStartButton","PressContinueButton","SetSelectedGameObject","AddListener","Take","ValidReservation"}.Contains(call.Name)&&!(call.DeclaringType.FullName=="System.Reflection.FieldInfo"&&call.Name=="SetValue"),"dice observer has no gameplay/input/reservation mutation: "+call.FullName);
  var visibility=m.GetType("DiceMaster.Information.InformationVisibility");
  Check(visibility!=null&&visibility.Methods.Where(x=>x.HasBody).SelectMany(x=>x.Body.Instructions).Select(x=>x.Operand).OfType<MethodReference>().All(x=>x.Name!="FindObjectsOfTypeAll"&&x.Name!="FindObjectsOfType"&&x.Name!="get_Instance"&&x.Name!="SetValue"&&!x.Name.StartsWith("set_")),"delivered native-window reader has no scene scan, singleton initialization or reflected/property writes");
  foreach(string name in new[]{"TickCore","DrawCore"})Check(m.GetType("DiceMaster.Information.InformationRuntime").Methods.Single(x=>x.Name==name).Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.DeclaringType.FullName==visibility.FullName&&c.Name=="get_Blocked"),"delivered query and paint boundaries both check native windows: "+name);
  foreach(string name in new[]{"DiceMaster.StoryBadge.rgba","DiceMaster.StoryGuideBadge.rgba"})
  {
   var badgeResource=m.Resources.OfType<EmbeddedResource>().SingleOrDefault(x=>x.Name==name);
   Check(badgeResource!=null&&badgeResource.GetResourceData().Length==64*64*4,"delivered DLL embeds the complete mod-owned image with no extra installation files: "+name);
  }
  var badgeType=m.GetType("DiceMaster.Information.InformationBadge");
  var spinner=m.GetType("DiceMaster.Information.InformationSpinner");
  Check(spinner!=null&&spinner.Methods.Single(x=>x.Name=="Draw").Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.Name=="DrawTextureWithTexCoords"),"loading uses one cached atlas frame draw per spinner, with no repaint texture generation");
  var drawLoading=m.GetType("DiceMaster.Information.InformationRuntime").Methods.Single(x=>x.Name=="DrawLoading");
  Check(drawLoading.Body.Instructions.Select(i=>i.Operand).OfType<MethodReference>().All(c=>!new[]{"Discover","AdvanceBudgeted","ReadPreviewScript","CreateRouteReader","FindObjectsOfTypeAll"}.Contains(c.Name)),"painting a spinner never advances or rediscovers source/game work");
  Check(badgeType.Methods.Single(x=>x.Name=="Release").Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.DeclaringType.FullName==spinner.FullName&&c.Name=="Release"),"existing shutdown release also destroys the loading texture");
  foreach(string name in new[]{"DiceMaster.Information.MapMarkerSnapshot","DiceMaster.Information.ChoiceStorySnapshot"})
   Check(m.GetType(name).Fields.Any(f=>f.Name=="Preparing"),"pending anchors are published beside actual badges: "+name);
  Check(badgeType!=null&&badgeType.Methods.Any(x=>x.HasBody&&x.Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.DeclaringType.FullName=="UnityEngine.Texture2D"&&c.Name=="LoadRawTextureData")),"delivered badge uploads prepared pixels rather than relying on font or PNG decoder");
  Check(m.GetType("DiceMaster.Information.InformationRuntime").Methods.Single(x=>x.Name=="DrawBadge").Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.DeclaringType.FullName==badgeType.FullName&&c.Name=="Draw"),"delivered native-place and menu marker path calls the actual image renderer");
  var overlay = m.GetType("DiceMaster.Information.InformationRuntime").NestedTypes.Single(x=>x.Name=="PassiveLabel").Methods.Single(x=>x.Name=="Prepare");
  Check(overlay.Body.Instructions.Select(x=>x.Operand).OfType<MethodReference>().Any(x=>x.DeclaringType.FullName=="DiceMaster.Information.InformationLayout"&&x.Name=="Place"),"delivered branch notice retains tested bounded fixed-corner layout");
  Check(overlay.Body.Instructions.Select(x=>x.Operand).OfType<MethodReference>().Any(x=>x.DeclaringType.FullName=="DiceMaster.Information.InformationLayout"&&x.Name=="Near"),"delivered map/choice panel uses tested adjacent native anchor layout");
  Check(m.GetType("DiceMaster.Information.ShopInformationPatches")!=null&&m.GetType("DiceMaster.Information.MapInformationPatches")!=null&&m.GetType("DiceMaster.Information.BranchObservationPatches")!=null,"delivered actual game observers included");
  var runtime=m.GetType("DiceMaster.Information.InformationRuntime");
  var publish=runtime.Methods.Single(x=>x.Name=="Publish");
  Check(publish.Body.Instructions.Any(x=>x.Operand is MethodReference c&&c.DeclaringType.FullName=="DiceMaster.Information.MapGuideSource"&&c.Name=="DiagnosticRow"),"delivered map publication can append cache diagnostics independently of provider success");
  var diagnostic=m.GetType("DiceMaster.Information.MapGuideSource").Methods.Single(x=>x.Name=="DiagnosticRow");
  Check(diagnostic.Body.Instructions.Select(x=>x.Operand).OfType<MethodReference>().All(x=>!new[]{"Discover","GetSource","LoadedDefinitions","ReadPreviewScript","CreateRouteReader","AdvanceBudgeted","FindObjectsOfTypeAll"}.Contains(x.Name)),"delivered diagnostic formatting initiates no source/game preparation or native asset scans");
  Check(publish.Body.Instructions.Any(x=>x.Operand is FieldReference f&&f.DeclaringType.FullName=="DiceMaster.Information.MapGuideSnapshot"&&f.Name=="Entries"),"delivered runtime publishes typed map snapshot entries");
  Check(runtime.Methods.Single(x=>x.Name=="DrawCore").Body.Instructions.Any(x=>x.Operand is FieldReference f&&f.DeclaringType.FullName=="DiceMaster.Information.MapGuideSnapshot"&&f.Name=="Summary"),"delivered overlay reads the same snapshot summary");
  Check(m.GetType("DiceMaster.Information.MapGuideSource").Methods.Any(x=>x.HasBody&&x.Body.Instructions.Any(i=>i.Operand is MethodReference call&&call.DeclaringType.FullName=="DiceMaster.Information.MapRouteDefinitions"&&call.Name=="AdvanceBudgeted")),"delivered map provider prepares actual bounded route engine");
  Check(publish.Body.Instructions.Any(x=>x.Operand is FieldReference f&&f.DeclaringType.FullName=="DiceMaster.Information.ChoiceStorySnapshot"&&f.Name=="Entries"),"delivered choice snapshot uses the existing read-only map log publication");
  var choice=m.GetType("DiceMaster.Information.ChoiceStorySource");
  foreach(var consumer in new[]{choice,m.GetType("DiceMaster.Information.MapGuideSource")})
   Check(DeepTypes(consumer).SelectMany(x=>x.Methods).Where(x=>x.HasBody).SelectMany(x=>x.Body.Instructions).Any(i=>i.Operand is MethodReference c&&c.DeclaringType.FullName=="DiceMaster.Information.MapRoutePath"&&c.Name=="ReadGuidance"),"delivered map and active-choice tooltips share story eligibility: "+consumer.Name);
  var loadHook=m.GetType("DiceMaster.Information.ChoiceStoryPatches").NestedTypes.Single(x=>x.Name=="ScriptLoaded");
  var capture=loadHook.Methods.Single(x=>x.Name=="Prefix");var complete=loadHook.Methods.Single(x=>x.Name=="Finalizer");
  int PriorityOf(MethodDefinition method)=>(int)method.CustomAttributes.Single(x=>x.AttributeType.FullName=="HarmonyLib.HarmonyPriority").ConstructorArguments[0].Value;
  var instrument=m.GetType("DiceMaster.EventSelection.EventExecutionPatches").NestedTypes.Single(x=>x.Name=="LuaLoadPatch").Methods.Single(x=>x.Name=="Prefix");
  Check(PriorityOf(capture)==800&&PriorityOf(instrument)==0&&PriorityOf(complete)==0,"delivered actual Harmony observer captures original Lua before #5 instrumentation and publishes in the late finalizer");
  Check(capture.Parameters.Any(x=>x.Name=="__state"&&x.IsOut)&&complete.Parameters.Any(x=>x.Name=="__state")&&!complete.Parameters.Any(x=>x.Name=="luaString"),"delivered load observation uses per-invocation source state instead of the transformed compiler argument");
  Check(capture.Body.Instructions.Any(x=>x.Operand is FieldReference f&&f.Name=="Text"&&f.DeclaringType.FullName.EndsWith("/OriginalSource",StringComparison.Ordinal)),"delivered early prefix retains the original source reference without rewriting execution input");
  Check(runtime.Methods.Single(x=>x.Name=="DrawCore").Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.DeclaringType.FullName=="DiceMaster.Information.InformationRow"&&c.Name=="get_HasBadge"),"delivered renderer admits explicit opportunity or guidance badges independently of general availability");
  Check(runtime.Methods.Single(x=>x.Name=="DrawBadge").Body.Instructions.Any(i=>i.Operand is FieldReference f&&f.DeclaringType.FullName=="DiceMaster.Information.InformationRow"&&f.Name=="Opportunity"),"delivered badge style preserves opportunity precedence over light guidance");
  Check(runtime.Methods.Single(x=>x.Name=="TickCore").Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.Name=="BeginScheduledReads")&&runtime.Methods.Single(x=>x.Name=="TickCore").Body.ExceptionHandlers.Any(x=>x.HandlerType==Mono.Cecil.Cil.ExceptionHandlerType.Finally),"selected and background map reads share a scheduling budget released in finally");
  var opportunity=choice.Methods.Single(x=>x.Name=="AppendOpportunities");
  Check(opportunity.Body.Instructions.Select(i=>i.Operand).OfType<MethodReference>().Any(c=>c.DeclaringType.FullName=="DiceMaster.Information.OneShotProof"&&c.Name=="TryGetEvent")&&opportunity.Body.Instructions.Select(i=>i.Operand).OfType<MethodReference>().Any(c=>c.DeclaringType.FullName=="DiceMaster.EventSelection.EventEligibility"&&c.Name=="EvaluateCurrent"),"delivered current menu uses the actual strict proof and fresh event eligibility reader");
  Check(choice.Methods.Any(x=>x.HasBody&&x.Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.DeclaringType.FullName=="DiceMaster.Information.MapRouteDefinitions"&&c.Name=="AdvanceBudgeted")),"delivered current-menu source uses the bounded route analyzer");
  var choiceRead=choice.Methods.Single(x=>x.Name=="Read");
  foreach(string call in new[]{"set_PrioritizeRootPreparation","get_RootPreparationComplete","get_HasPendingWork"})
   Check(choiceRead.Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.DeclaringType.FullName=="DiceMaster.Information.MapRouteDefinitions"&&c.Name==call),"delivered choice UI separates local readiness from the same-budget downstream preparation: "+call);
  Check(m.GetType("DiceMaster.Information.MapGuideSource").Methods.Single(x=>x.Name=="GetSource").Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.Name=="set_PrioritizeRootPreparation"),"delivered map scheduling prepares the closed local menu before downstream work");
  Check(m.GetType("DiceMaster.Information.MapGuideSource").Methods.Single(x=>x.Name=="CollectSelectedRoute").Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.DeclaringType.FullName=="DiceMaster.LiveLog.PreviewConditions/ReadStamp"&&c.Name=="IsCurrent"),"delivered map reuses a successful witness only after checking its observed inputs");
  var safeReplay=m.GetType("DiceMaster.LiveLog.SafeDisplayEvaluation").NestedTypes.Single(x=>x.Name=="Evaluation").Methods.Single(x=>x.Name=="ReplayRead");
  Check(safeReplay.Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.DeclaringType.FullName=="DiceMaster.LiveLog.SafeDisplayEvaluation"&&c.Name=="ReadStatFinalValue"),"cached stat reads preserve recursive conversion validation before the native FinalValue getter");
  Check(choice.Methods.Single(x=>x.Name=="CurrentRow").Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.DeclaringType.FullName=="UnityEngine.EventSystems.EventSystem"&&c.Name=="get_currentSelectedGameObject"),"delivered pad focus reads the existing native selected object");
  var forbidden=new[]{"Check","Execute","GetResult","GetRate","Position","Switch","Condition","SetStoryScript","SetNextScript","SetCurrentStoryScript","SetCurrentKey","SetConditionFlag","GetRandomResult","UpdateCheckMissions","GetActiveMissions","GetStoryText","UpdateContent","LoadLuaFunction","SetSelectedGameObject","OnSelect","Select","AddOption","Use","Learn","Apply","Buy"};
  foreach(var type in m.Types.Where(x=>x.Namespace=="DiceMaster.Information"&&(x.Name.StartsWith("MapRoute")||x.Name=="MapGuideSource"||x.Name=="InformationAnchors"||x.Name=="InformationVisibility"||x.Name=="MapDefinitionAssets"||x.Name=="MapFileDefinitions"||x.Name=="MapDefinitionFileReader"||x.Name=="InformationRuntime"||x.Name.StartsWith("ChoiceStory")))) {
   foreach(var method in DeepTypes(type).SelectMany(x=>x.Methods).Where(x=>x.HasBody))
    foreach(var call in method.Body.Instructions.Select(x=>x.Operand).OfType<MethodReference>())
     Check(!((call.DeclaringType.Namespace.StartsWith("Mortal.")||call.DeclaringType.Namespace.StartsWith("Fungus")||call.DeclaringType.Namespace.StartsWith("UnityEngine"))&&forbidden.Contains(call.Name)),"delivered map/choice IL does not invoke game or input mutation "+call.FullName);
  }
  var anchors=m.GetType("DiceMaster.Information.InformationAnchors");
  foreach(string name in new[]{"DrawBadge","DrawLoading","Focused"})
   Check(runtime.Methods.Single(x=>x.Name==name).Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.DeclaringType.FullName==anchors.FullName&&c.Name=="TryBadgeRect"),"delivered badge/spinner/pointer geometry uses the same card name anchor: "+name);
  Check(overlay.Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.Name=="TryNearCards"),"delivered card tooltip uses bounded sibling-aware placement");
  Check(choiceRead.Body.Instructions.Any(i=>i.Operand is FieldReference f&&f.DeclaringType.FullName=="DiceMaster.Information.ChoiceStorySnapshot"&&f.Name=="Anchors"),"scheduled choice read captures all live native anchors independently of hint eligibility");
  Check(anchors.Methods.Where(x=>x.HasBody).SelectMany(x=>x.Body.Instructions).Any(i=>i.OpCode.Code==Mono.Cecil.Cil.Code.Ldstr&&(string)i.Operand=="m_TextCache"),"card geometry reads the existing rendered Text cache");
  Check(anchors.Methods.Where(x=>x.HasBody).SelectMany(x=>x.Body.Instructions).Select(x=>x.Operand).OfType<MethodReference>().All(x=>!new[]{"get_cachedTextGenerator","get_cachedTextGeneratorForLayout","Populate","PopulateWithErrors","Invalidate","get_preferredWidth","get_preferredHeight","SetVerticesDirty","SetLayoutDirty"}.Contains(x.Name)),"card painting never forces native text generation or layout changes");
  Check(runtime.Methods.Single(x=>x.Name=="Draw").Body.ExceptionHandlers.Any(x=>x.HandlerType==Mono.Cecil.Cil.ExceptionHandlerType.Finally),"paint geometry cache always releases native anchor references");
  var backend=m.GetType("DiceMaster.LiveLog.PreviewConditions");
  foreach(var method in DeepTypes(backend).SelectMany(x=>x.Methods).Where(x=>x.HasBody))
   foreach(var instruction in method.Body.Instructions) {
    if(instruction.Operand is MethodReference call)
     Check(!(call.DeclaringType.Namespace.StartsWith("Mortal.")&&(forbidden.Contains(call.Name)||call.Name=="Setup"||call.Name=="set_State"))&&call.Name!="SetValue","delivered private entry backend calls no game evaluator/setup or reflected field write: "+call.FullName);
    if(instruction.OpCode.Code==Mono.Cecil.Cil.Code.Stfld&&instruction.Operand is FieldReference field)
     Check(!field.DeclaringType.Namespace.StartsWith("Mortal."),"delivered private entry backend writes no game field: "+field.FullName);
   }
  Check(m.GetType("DiceMaster.Information.MapRouteRead").Methods.Single(x=>x.Name=="ForSelectedPlace").Body.Instructions.Any(x=>x.Operand is MethodReference call&&call.Name=="ProjectPositionEntry"),"delivered selected-place guidance calls the read-only entry projector");
  Check(anchors.Methods.Where(x=>x.HasBody).SelectMany(x=>x.Body.Instructions).Select(x=>x.Operand).OfType<MethodReference>().All(x=>x.Name!="FindObjectsOfTypeAll"&&x.Name!="FindObjectsOfType"&&x.Name!="Raycast"&&x.Name!="SetValue"),"delivered geometry has no scene-wide scans, raycasts, or reflected field writes");
  var assets=m.GetType("DiceMaster.Information.MapDefinitionAssets");
  Check(assets!=null,"delivered DLL contains the cold-map metadata boundary");
  foreach(string method in new[]{"Install","Clear"})
   Check(runtime.Methods.Single(x=>x.Name==(method=="Install"?"Install":"ResetDisplays")).Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.DeclaringType.FullName==assets.FullName&&c.Name==method),"delivered runtime binds and invalidates loaded metadata: "+method);
  var assetRead=assets.Methods.Single(x=>x.Name=="Read");
  var scanned=assetRead.Body.Instructions.Select(i=>i.Operand).OfType<GenericInstanceMethod>().Where(x=>x.DeclaringType.FullName==assets.FullName&&x.Name=="Objects").Select(x=>x.GenericArguments.Single().FullName).OrderBy(x=>x).ToArray();
  Check(scanned.SequenceEqual(new[]{"Mortal.Story.ConditionResultConfig","Mortal.Story.PositionResultConfig","Mortal.Story.StatModifyManager","Mortal.Story.SwitchResultConfig"}.OrderBy(x=>x)),"cold lookup searches only three exact config types and the serialized reset owner; no broad flag or position/game-object discovery");
  Check(assets.Methods.Where(x=>x.HasBody).SelectMany(x=>x.Body.Instructions).Select(x=>x.Operand).OfType<MethodReference>().All(x=>x.Name!="Load"&&x.Name!="LoadAll"&&x.Name!="Instantiate"&&x.Name!="InitEnvironment"&&x.Name!="get_Instance"&&x.Name!="FindObjectsOfType"),"cold metadata lookup neither loads/instantiates assets nor initializes gameplay owners");
  var disk=m.GetType("DiceMaster.Information.MapDefinitionFileReader");
  Check(disk!=null&&m.Resources.Any(r=>r.Name=="DiceMaster.MapDefinitions.index"),"delivered DLL includes fingerprint-checked offset index and file reader");
  foreach(var method in DeepTypes(disk).SelectMany(x=>x.Methods).Where(x=>x.HasBody)) {
   var il=method.Body.Instructions;
   for(int at=0;at<il.Count;at++) if(il[at].Operand is MethodReference call) {
    Check(!call.DeclaringType.Namespace.StartsWith("UnityEngine")&&!call.DeclaringType.Namespace.StartsWith("Mortal."),"file worker invokes no Unity/game API: "+call.FullName);
    if(call.DeclaringType.FullName=="System.IO.FileStream"&&call.Name==".ctor")
     Check(at>=3&&il[at-3].OpCode.Code==Mono.Cecil.Cil.Code.Ldc_I4_3&&il[at-2].OpCode.Code==Mono.Cecil.Cil.Code.Ldc_I4_1&&il[at-1].OpCode.Code==Mono.Cecil.Cil.Code.Ldc_I4_1,"installed files open only as Open/Read/ShareRead");
   }
  }
  Check(runtime.Methods.Single(x=>x.Name=="TickCore").Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.DeclaringType.FullName==assets.FullName&&c.Name=="SetVisible"),"native-window/hidden/focus visibility owns background file preparation cancellation");
  var markerRead=m.GetType("DiceMaster.Information.MapGuideSource").Methods.Single(x=>x.Name=="ReadMarkers");
  Check(markerRead.Body.Instructions.Select(x=>x.Operand).OfType<MethodReference>().All(x=>x.Name!="GetSource"&&x.Name!="AdvanceBudgeted"&&x.Name!="GetTextFromResources")&&
   markerRead.Body.Instructions.Select(x=>x.Operand).OfType<MethodReference>().Any(x=>x.Name=="CollectGuidanceMarkers"),"delivered marker pass delegates cold dialogue work to the shared bounded guidance scheduler");
 }
 static System.Collections.Generic.IEnumerable<TypeDefinition> DeepTypes(TypeDefinition type) {
  yield return type;
  foreach(var nested in type.NestedTypes)foreach(var value in DeepTypes(nested))yield return value;
 }
}
