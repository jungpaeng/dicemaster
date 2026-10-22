using System.Reflection;
using DiceMaster.Information;
using DiceMaster.LiveLog;
using DiceMaster.EventSelection;
using Mortal.Story;
using UnityEngine;
using UnityEngine.UI;
using Fungus;
static class Program {
 static int checks;static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static string[] raw={"O_one|<50","O_two|>=50"};
 static string script="local d={};d[1]='O_one|<50';d[2]='O_two|>=50';runwait(dicemenudialog.ExecuteRoll(d,1,'roll'));local r=dicemenudialog.ResultSelection;if r==1 then setcharacter(characters.Get('brother'));say(luamanager.GetStoryText('D_one'));elseif r==2 then end";
 static object Hook(string type,string method,params object[] args)=>typeof(DiceGuideSource).GetNestedType(type,BindingFlags.NonPublic).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
 static object Field(string name)=>typeof(DiceGuideSource).GetField(name,BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
 static DiceMenuDialog View(string[] options)=>new(){cachedButtons=options.Select(s=>new Button{Option=new DiceOption{_sourceText=new Text{text=s}}}).ToArray()};
 static DiceMenuDialog Capture(bool active=true,string[] options=null,string key="roll") {options??=raw;var dice=View(options);dice.gameObject.activeInHierarchy=active;MenuDialog.ActiveMenuDialog=dice;Hook("Roll","Postfix",dice,options,key);return dice;}
 static void Initialize(DiceMenuDialog dice,Action during=null,Exception error=null){object[] args={dice,null};Hook("Initialize","Prefix",args);Hook("Close","Prefix",dice);during?.Invoke();Check(ReferenceEquals(error,Hook("Initialize","Finalizer",error,args[1])),"initialization observer preserves the exact native exception");}
 static DiceMenuDialog Open(){var dice=Capture();Hook("Prepared","Postfix",dice);return dice;}
 static DiceGuideSnapshot Read(){DiceGuideSnapshot snapshot=null;for(int n=0;n<100;n++){Time.unscaledTime+=.25f;snapshot=(DiceGuideSnapshot)DiceGuideSource.Discover().Single().Read();if(snapshot.Preparing.Count==0)break;}return snapshot;}
 static void Main(){
  CheckPointManager.Instance=new();DiceGuideSource.Configure(true,true);DiceGuideSource.ObserveScript(script,"root.LuaScript");var dice=Open();var snapshot=Read();
  Check(snapshot.Entries.Count(r=>r.HasBadge)==1&&snapshot.Entries.Single(r=>r.HasBadge).Anchor==dice.cachedButtons[0].Option,"actual dice result row gets only its own connected story");
  var text=((DiceOption)dice.cachedButtons[0].Option)._normalText;string original=text.text;DiceGuideSource.Render(snapshot);
  Check(text.text.StartsWith(original)&&text.text.Contains("연결 후보")&&!dice.cachedButtons[0].interactable,"passive summary preserves existing effects and non-interactable native results");
  int measures=text.Measures;string rendered=text.text;for(int n=0;n<1000;n++)DiceGuideSource.Render(snapshot);
  Check(text.text==rendered&&text.Measures==measures,"one thousand unchanged paints do not append again or remeasure");
  DiceGuideSource.Render(null);Check(text.text==original,"occlusion restores only our rendered suffix");DiceGuideSource.Render(snapshot);
  DiceGuideSource.Configure(false,false);Check(text.text==original&&!DiceGuideSource.Active&&DiceGuideSource.Discover().Count==0,"both guide settings OFF hide and stop queries");
  DiceGuideSource.Configure(true,true);snapshot=Read();Check(snapshot.Entries.Any(r=>r.HasBadge),"re-enabled active dice reconstructs cleared definition without stale state");
  var reservation=new CheckpointReservation(true,new object(),new object(),new object(),new object(),"future","2");CheckpointReservationStore.Set(reservation);var saved=CheckpointReservationStore.Current(true);Read();Check(ReferenceEquals(saved,CheckpointReservationStore.Current(true)),"information never consumes or validates away a checkpoint reservation");
  CheckpointReservationStore.Take(saved);
  LogBridge.Model.ContextId++;Check(DiceGuideSource.Discover().Count==0,"new game context rejects old dice immediately");DiceGuideSource.Render(snapshot);Check(text.text==original,"old publication cannot restore a suffix into a different context");
  DiceGuideSource.Clear();Check(!DiceGuideSource.Active&&DiceGuideSource.Discover().Count==0,"load/scene clear drops native owners and rows");
  DiceGuideSource.Configure(false,true);DiceGuideSource.ObserveScript(script,"root.LuaScript");Open();Check(!Read().Entries.Any(r=>r.HasBadge),"one-shot-only setting never enables ordinary story candidates");
  DiceGuideSource.Configure(true,false);DiceMaster.DataHelper.Scripts["mission"]="";ChoiceStorySource.Pending["mission"]=new(){Name="registered",Kind="mission"};
  DiceGuideSource.ObserveScript(script.Replace("setcharacter(characters.Get('brother'));say(luamanager.GetStoryText('D_one'));","luamanager.SetNextScript('mission');luamanager.Init();"),"root.LuaScript");Open();
  Check(Read().Entries.Any(r=>r.Opportunity&&r.Available),"registered pending mission and a fully known outcome path retain the black opportunity badge");
  CheckpointReservationStore.Set(reservation);Check(!Read().Entries.Any(r=>r.Opportunity),"a pending override prevents a contradictory natural-path opportunity promise");CheckpointReservationStore.Take(CheckpointReservationStore.Current(true));
  DiceGuideSource.ObserveScript(script.Replace("if r==1 then setcharacter", "if r==1 then if missing_local then setcharacter").Replace("elseif r==2", "end elseif r==2"),"root.LuaScript");Open();
  Check(!Read().Entries.Any(r=>r.HasBadge),"unresolved earlier local/unsupported predicate cannot become a temporary story possibility");
  var limited=new FakeTmp();var blocker=new Text();((RectTransform)blocker.transform).Origin=new Vector3{x=100};var fitted=new DiceGuideLabel(limited,blocker);fitted.Apply("a connected story");
  Check(limited.text=="native effects","existing threshold column leaves no room: preserve native effects and omit suffix instead of overlap");
  var resizable=new FakeTmp();var fittedFont=new DiceGuideLabel(resizable);fittedFont.Apply("story");int measured=resizable.Measures;resizable.fontSize=24;fittedFont.Apply("story");Check(resizable.Measures>measured,"font changes invalidate only cached label measurement");
  Check(UnityEngine.Random.Calls==0&&InformationRuntime.Errors.Count==0,"observer never executes RNG/native roll and has no swallowed failures");
  Lifecycle();Corpus();
  Check(UnityEngine.Random.Calls==0&&InformationRuntime.Errors.Count==0,"lifecycle/corpus observers never invoke RNG/native callbacks or swallow failures");
  Console.WriteLine("PASS: dice guide native boundaries "+checks);
 }
 static void Reset(){DiceGuideSource.Clear();DiceGuideSource.Configure(true,true);DiceGuideSource.ObserveScript(script,"root.LuaScript");}
 static void Lifecycle(){
  Reset();var dice=Capture(false);Check(!DiceGuideSource.Active&&DiceGuideSource.Discover().Count==0&&Field("index")==null,"iterator factory captures inactive invocation without parsing or querying hidden view");
  Initialize(dice,()=>{Hook("Prepared","Postfix",dice);Check(Field("owner")==null,"native initialization cannot bind before readiness");});
  dice.gameObject.activeInHierarchy=true;
  var pending=(DiceGuideSnapshot)DiceGuideSource.Discover().Single().Read();
  Check(pending.Entries.Single().Title=="주사위 연결 자료"&&pending.Entries.Single().Status.Contains("준비 대기")&&Field("index")==null,"first Awake/Clear preserves captured invocation and publishes only current dice readiness diagnostics");
  Hook("Close","Prefix",new MenuDialog());Hook("Prepared","Postfix",new Mortal.Core.CustomMenuDialog());
  Hook("Prepared","Postfix",dice);var snapshot=Read();Check(snapshot.Entries.Any(r=>r.HasBadge),"cold first activation survives native Awake/Clear then binds prepared exact result rows");
  DiceGuideSource.Render(snapshot);var text=((DiceOption)dice.cachedButtons[0].Option)._normalText;
  Hook("Close","Prefix",dice);Hook("Prepared","Postfix",dice);Check(!DiceGuideSource.Active&&!text.text.Contains("연결 후보"),"real Clear restores suffix and delayed readiness cannot revive ended roll");
  MenuDialog.ActiveMenuDialog=dice;Hook("Roll","Postfix",dice,raw,"roll");Hook("Prepared","Postfix",dice);Check(Read().Entries.Any(r=>r.HasBadge),"reused initialized instance binds without another Awake");
  Hook("Hide","Prefix",dice);Hook("Prepared","Postfix",dice);Check(!DiceGuideSource.Active,"native OnDisable cancels bound view despite stale selected singleton");
  Reset();dice=Capture();Hook("Close","Prefix",dice);Hook("Prepared","Postfix",dice);Check(!DiceGuideSource.Active,"real Clear before readiness cancels pending invocation");
  Reset();dice=Capture();Initialize(dice,()=>Hook("Hide","Prefix",dice));Hook("Prepared","Postfix",dice);Check(!DiceGuideSource.Active,"disable inside initialization is still a real cancellation");
  Reset();dice=Capture();Initialize(dice,error:new InvalidOperationException("native failure"));Hook("Prepared","Postfix",dice);Check(!DiceGuideSource.Active,"failed native initialization cannot bind later");
  Reset();dice=Capture();Initialize(dice,()=>Initialize(dice));Hook("Prepared","Postfix",dice);Check(Read().Entries.Any(r=>r.HasBadge),"nested initialization depths balance and protect only the matching invocation");
  Reset();dice=Capture();var mismatch=(DiceOption)dice.cachedButtons[1].Option;mismatch._sourceText.text="old row";Hook("Prepared","Postfix",dice);
  Check(Field("owner")==null&&Field("index")==null&&!Read().Entries.Any(r=>r.HasBadge),"partial/stale native rows never activate evidence or start parsing");
  mismatch._sourceText.text=raw[1];Hook("Prepared","Postfix",dice);Check(Read().Entries.Any(r=>r.HasBadge),"later exact native update binds previously incomplete rows");
  Reset();dice=Capture();LogBridge.Model.ContextId++;Hook("Prepared","Postfix",dice);Check(!DiceGuideSource.Active,"load context invalidates pending invocation");
  Reset();dice=Capture();DiceGuideSource.Clear();Hook("Prepared","Postfix",dice);Check(!DiceGuideSource.Active,"source/scene reset cancels pending invocation");
  Reset();dice=Capture();DiceGuideSource.Configure(false,false);Initialize(dice);Hook("Prepared","Postfix",dice);Check(!DiceGuideSource.Active&&Field("index")==null,"settings OFF during activation never start analysis");DiceGuideSource.Configure(true,true);Check(Read().Entries.Any(r=>r.HasBadge),"re-enable already prepared view reconstructs current evidence");
  DiceGuideSource.Clear();dice=Capture();Hook("Prepared","Postfix",dice);Check(Read().Entries.Single().Status=="주사위 원문 미확인","missing source reports current dice diagnostics instead of underlying map rows");
  Reset();dice=Capture(false);Hook("Prepared","Postfix",dice);Check(Field("owner")==null,"hidden native view cannot bind readiness");
  dice.gameObject.activeInHierarchy=true;dice.isActiveAndEnabled=false;Hook("Prepared","Postfix",dice);Check(Field("owner")==null,"disabled native view cannot bind readiness");
  Reset();DiceGuideSource.Clear();
 }
 static void Corpus(){
  string path=Environment.GetEnvironmentVariable("DICEMASTER_CORPUS_INDEX");if(string.IsNullOrEmpty(path)){Console.WriteLine("SKIP: supplied game corpus not configured");return;}
  using var data=System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
  foreach(var row in data.RootElement.EnumerateArray())DiceMaster.DataHelper.Scripts[row.GetProperty("name").GetString()]=File.ReadAllText(row.GetProperty("path").GetString());
  Reset();DiceGuideSource.ObserveScript(DiceMaster.DataHelper.Scripts["s2321_04_03_01"],"S2321_04_03_01.LuaScript");
  string[] options={"O_S2321_04_03_01_001|<50","O_S2321_04_03_01_002|>=50"};
  var dice=Capture(false,options,"S2321_04_03_01_004");Initialize(dice);dice.gameObject.activeInHierarchy=true;Hook("Prepared","Postfix",dice);
  var snapshot=Read();var connected=snapshot.Entries.SingleOrDefault(r=>r.Title=="결과 2"&&r.HasBadge);
  Check(connected!=null&&ReferenceEquals(connected.Anchor,dice.cachedButtons[1].Option)&&connected.Guidance&&!connected.Opportunity&&connected.Detail.Contains("D_S2321_04_03_01_02_001"),"actual Xiangyang training cold lifecycle identifies result 2 sparring dialogue from supplied Lua without gameplay execution");
  Check(!snapshot.Entries.Any(r=>r.Title=="결과 1"&&r.Detail.Contains("D_S2321_04_03_01_02_001")),"actual solo training result does not inherit the other dice outcome's story");
  Hook("Hide","Prefix",dice);Check(!DiceGuideSource.Active,"actual corpus result clears on native hide");
 }

}
