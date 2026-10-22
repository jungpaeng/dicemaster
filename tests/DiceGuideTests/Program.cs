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
 static DiceMenuDialog Open(){var dice=new DiceMenuDialog{cachedButtons=raw.Select(s=>new Button{Option=new DiceOption{_sourceText=new Text{text=s}}}).ToArray()};MenuDialog.ActiveMenuDialog=dice;typeof(DiceGuideSource).GetNestedType("Roll",BindingFlags.NonPublic).GetMethod("Postfix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{dice,raw,"roll"});return dice;}
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
  Console.WriteLine("PASS: dice guide native boundaries "+checks);
 }
}
