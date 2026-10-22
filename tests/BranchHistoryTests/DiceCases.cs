using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DiceMaster;
using DiceMaster.Information;
using DiceMaster.EventSelection;
using DiceMaster.LiveLog;
using Mono.Cecil;
using Mortal.Story;
using UnityEngine;
using UnityEngine.UI;
using Fungus;

internal static partial class Program
{
    private sealed class Steps : IEnumerator,IDisposable
    {
        public readonly Queue<Func<bool>> Actions=new(); public object Yield=new object();
        public Exception MoveError,CurrentError,DisposeError;public int Moves,Disposes,Resets;
        public object Current {get{if(CurrentError!=null)throw CurrentError;return Yield;}}
        public bool MoveNext(){Moves++;if(MoveError!=null)throw MoveError;return Actions.Count!=0&&Actions.Dequeue()();}
        public void Reset(){Resets++;}
        public void Dispose(){Disposes++;if(DisposeError!=null)throw DisposeError;}
    }
    private static void DiceReset()
    {
        Reset();DiceInformation.Clear();DiceInformation.Visible=_=>true;DiceInformation.Clock=()=>Time.unscaledTime;
        DiceInformation.Failure=e=>DicePlugin.LogInfo("dice fixture "+e.GetType().Name);
    }
    private static DiceInformation.View DiceView(string title="판정 이름") => new(){Visible=true,Name=title,Labels=new[]{"첫 결과","다른 결과"},Raw=new[]{"O_one|<20","O_two|>=20"}};
    private static void Accept(object owner,int result,Exception failure=null)
    {var closing=DiceInformation.BeginClosing(owner,result);DiceInformation.EndClosing(closing,failure);}
    private static void DiceCases()
    {
        DiceReset();var owner=new object();var steps=new Steps();int reads=0;
        var choice=DiceInformation.BeginChoice("실제로 누른 주제");DiceInformation.EndChoice(choice,true);
        steps.Actions.Enqueue(()=>true);steps.Actions.Enqueue(()=>true);steps.Actions.Enqueue(()=>{Accept(owner,2);return false;});
        var iterator=DiceInformation.Observe(steps,owner,"key",()=>{reads++;return DiceView();});
        Check(DiceInformation.CurrentRow()==null&&reads==0,"creating a dice iterator neither opens a notice nor reads unseen game data");
        Check(iterator.MoveNext()&&ReferenceEquals(iterator.Current,steps.Yield),"observer preserves the exact native yielded object");
        var notice=DiceInformation.CurrentRow();Check(notice.AutoText.Contains("실제로 누른 주제")&&notice.AutoText.Contains("판정 이름")&&!notice.AutoText.Contains("첫 결과"),"rolling notice carries confirmed incoming context without exposing an unaccepted result");
        for(int i=0;i<1000;i++)Check(ReferenceEquals(notice,DiceInformation.CurrentRow()),"warm dice notice reuses the copied row");
        Check(reads==1&&BranchHistory.Discover().Count==0,"notice reads never run native evaluation or record a preliminary result");
        Check(iterator.MoveNext()&&BranchHistory.Discover().Count==0,"reroll/intermediate yield does not create history");
        Check(!iterator.MoveNext()&&BranchHistory.Discover().Count==1&&DiceInformation.CurrentRow()==null,"accepted native clear completes exactly one dice record");
        Check(Latest().Kind=="주사위"&&Latest().AutoText.Contains("다른 결과")&&Detail().Detail.Contains("실제로 누른 주제")&&Detail().Detail.Contains("O_two|＞=20"),"final native result label and literal raw condition are copied to existing history");
        Check(DataHelper.ScriptReads+EventResultPreview.Reads+PreviewAnalyzer.Analyses+PopupConditionText.Reads==0,"dice summary/detail performs no event/Lua/condition/effect lookup");
        Time.unscaledTime+=5;Check(Latest()==null&&BranchHistory.Discover().Count==1,"dice notice expires at five seconds while memory history remains");
        for(int i=0;i<22;i++)BranchHistory.RecordDice(new DiceRollRecord("判定","k","","r","",1),Time.unscaledTime,BranchHistory.ContextKey);
        Check(BranchHistory.Discover().Count==20,"dice entries share the existing twenty-record ring");
        foreach(string invalidation in new[]{"new source","new menu","failed press","no selection"}){
            DiceReset();choice=DiceInformation.BeginChoice("old topic");DiceInformation.EndChoice(choice,true);
            if(invalidation=="new source")DiceInformation.SourceChanged();if(invalidation=="new menu")DiceInformation.MenuBuilding();if(invalidation=="failed press")DiceInformation.EndChoice(choice,false);if(invalidation=="no selection")DiceInformation.BeginChoice("");
            steps=new Steps();steps.Actions.Enqueue(()=>true);steps.Actions.Enqueue(()=>{Accept(owner,1);return false;});iterator=DiceInformation.Observe(steps,owner,"k",()=>DiceView());iterator.MoveNext();
            Check(!DiceInformation.CurrentRow().AutoText.Contains("old topic"),"unproven incoming context omitted: "+invalidation);iterator.MoveNext();Check(Detail().Detail.Contains("연결 미확인"),"automatic/non-marker dice still records itself: "+invalidation);
        }
        foreach(string cancellation in new[]{"dispose","completion without accepted clear","external clear","save/load/scene","OFF/ON","clear failure","hidden initial","bad index"}){
            DiceReset();steps=new Steps();var view=DiceView();if(cancellation=="hidden initial")view.Visible=false;
            steps.Actions.Enqueue(()=>true);steps.Actions.Enqueue(()=>{if(cancellation=="clear failure")Accept(owner,1,new InvalidOperationException());else if(cancellation=="bad index")Accept(owner,0);else if(cancellation=="save/load/scene"||cancellation=="OFF/ON"||cancellation=="hidden initial")Accept(owner,1);return false;});
            iterator=DiceInformation.Observe(steps,owner,"k",()=>view);iterator.MoveNext();
            if(cancellation=="dispose")((IDisposable)iterator).Dispose();
            else {if(cancellation=="external clear")Accept(owner,1);if(cancellation=="save/load/scene")BranchHistory.Clear();if(cancellation=="OFF/ON"){BranchHistory.Enabled=false;BranchHistory.Enabled=true;}iterator.MoveNext();}
            Check(BranchHistory.Discover().Count==0&&DiceInformation.CurrentRow()==null,"no false accepted history on "+cancellation);
        }
        DiceReset();steps=new Steps();steps.Actions.Enqueue(()=>true);iterator=DiceInformation.Observe(steps,owner,"k",()=>DiceView());iterator.MoveNext();
        var gameError=new InvalidOperationException("original failure");steps.MoveError=gameError;try{iterator.MoveNext();Check(false,"must throw");}catch(Exception e){Check(ReferenceEquals(e,gameError),"original MoveNext exception identity preserved");}
        Check(DiceInformation.CurrentRow()==null&&BranchHistory.Discover().Count==0,"failed coroutine abandons display and records nothing");
        DiceReset();steps=new Steps();steps.Actions.Enqueue(()=>true);iterator=DiceInformation.Observe(steps,owner,"k",()=>DiceView());iterator.MoveNext();steps.CurrentError=gameError;
        try{_ = iterator.Current;Check(false,"must throw");}catch(Exception e){Check(ReferenceEquals(e,gameError),"original Current getter failure is preserved");}
        steps.DisposeError=gameError;try{((IDisposable)iterator).Dispose();Check(false,"must throw");}catch(Exception e){Check(ReferenceEquals(e,gameError),"original Dispose failure is preserved");}
        ((IDisposable)iterator).Dispose();Check(steps.Disposes==1,"native dispose forwarded once even when it throws");
        DiceReset();steps=new Steps();steps.Actions.Enqueue(()=>true);iterator=DiceInformation.Observe(steps,owner,"k",()=>throw new InvalidOperationException("UI read"));Check(iterator.MoveNext(),"observer UI failure does not change native iteration");
        iterator.Reset();Check(steps.Resets==1,"Reset delegates to the native enumerator without gameplay replacement");Check(!iterator.MoveNext(),"observation failure preserves the native completion");
        BranchHistory.Enabled=false;Check(ReferenceEquals(DiceInformation.Observe(steps,owner,"k",()=>throw new Exception()),steps),"OFF returns the original enumerator unchanged");
        // Clear stops native coroutines. Re-entrant disposal during an accepted
        // Clear must still forward dispose and retain its final outcome once.
        DiceReset();steps=new Steps();IEnumerator reentrant=null;steps.Actions.Enqueue(()=>true);steps.Actions.Enqueue(()=>{var close=DiceInformation.BeginClosing(owner,1);((IDisposable)reentrant).Dispose();DiceInformation.EndClosing(close,null);return false;});reentrant=DiceInformation.Observe(steps,owner,"k",()=>DiceView());reentrant.MoveNext();reentrant.MoveNext();
        Check(steps.Disposes==1&&BranchHistory.Discover().Count==1,"accepted self-cleanup/disposal records once without changing native disposal");
        DiceReset();steps=new Steps();steps.Actions.Enqueue(()=>true);steps.Actions.Enqueue(()=>{Accept(owner,1);throw gameError;});iterator=DiceInformation.Observe(steps,owner,"k",()=>DiceView());iterator.MoveNext();try{iterator.MoveNext();}catch(Exception e){Check(ReferenceEquals(e,gameError),"later callback error is not swallowed");}
        Check(BranchHistory.Discover().Count==1,"a later callback error does not invent or undo an already accepted dice outcome");
        DiceReset();steps=new Steps();int viewReads=0;steps.Actions.Enqueue(()=>true);steps.Actions.Enqueue(()=>true);steps.Actions.Enqueue(()=>{Accept(owner,2);return false;});iterator=DiceInformation.Observe(steps,owner,"unreadable",()=>++viewReads==1?DiceView():throw new InvalidOperationException("later UI read"));iterator.MoveNext();iterator.MoveNext();
        Check(!DiceInformation.CurrentRow().AutoText.Contains("판정 이름"),"failed later UI read does not retain a stale title as fresh");iterator.MoveNext();Check(Latest().Title.Contains("결과 2")&&!Latest().Title.Contains("다른 결과")&&Detail().Detail.Contains("미확인"),"read failure retains only the actual accepted index and explicitly unknown names");
        DiceReset();
    }
    private static object DiceCall(string patch,string method,params object[] args)
    {return typeof(DiceObservationPatches).GetNestedType(patch,BindingFlags.NonPublic).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);}
    private static void DicePatchCases()
    {
        DiceReset();var harmony=new HarmonyLib.Harmony();DiceObservationPatches.Install(harmony);DiceObservationPatches.Install(harmony);
        Check(harmony.Installed.Count==5,"dice observers install once and never subscribe UI listeners");
        var menu=new MenuDialog();var button=new Button();button.transform.Parent=menu.transform;
        button.gameObject.Attach(new Mortal.Core.NormalOptionButton{_text=new[]{new Text{text="선택한 주제"}}});menu.cachedButtons=new[]{button};MenuDialog.ActiveMenuDialog=menu;
        object[] press={button,null};DiceCall("ChoicePress","Prefix",press);Check(press[1]!=null,"actual enabled standard button press captures its own rendered name");Check(DiceCall("ChoicePress","Finalizer",press[1],null)==null,"choice observer preserves null exception");
        var dice=new DiceMenuDialog{cachedButtons=new[]{new Button(),new Button()}};dice._diceHeader._front.text="판정 제목......";
        for(int n=0;n<2;n++){dice.cachedButtons[n].gameObject.Attach(new DiceOption{_sourceText=new Text{text="O_"+n+"|<20"},_normalText=new FakeTmp{text="결과 "+n}});}
        MenuDialog.ActiveMenuDialog=dice;var steps=new Steps();steps.Actions.Enqueue(()=>true);steps.Actions.Enqueue(()=>{dice._triggerNextInput=true;dice._resultSelection=2;object[] close={dice,null};DiceCall("Closing","Prefix",close);Check(close[1]!=null,"native accepted clear captures final result before labels disappear");dice.cachedButtons[1].gameObject.activeInHierarchy=false;DiceCall("Closing","Finalizer",close[1],null);return false;});
        object[] roll={dice,"actual-key",steps};DiceCall("Roll","Postfix",roll);var iterator=(IEnumerator)roll[2];Check(iterator.MoveNext()&&DiceInformation.CurrentRow().AutoText.Contains("선택한 주제")&&DiceInformation.CurrentRow().AutoText.Contains("판정 제목"),"native adapter copies existing header/TMP text and actual incoming selection");iterator.MoveNext();
        Check(Detail().Detail.Contains("actual-key")&&Detail().Detail.Contains("결과 1")&&!Detail().Detail.Contains("판정 제목......"),"native clear uses last visible copied labels and the exact ExecuteRoll key");
        DiceReset();MenuDialog.ActiveMenuDialog=dice;dice.cachedButtons[1].gameObject.activeInHierarchy=true;steps=new Steps();steps.Actions.Enqueue(()=>true);steps.Actions.Enqueue(()=>{dice._triggerNextInput=true;dice._resultSelection=1;object[] close={dice,null};DiceCall("Closing","Prefix",close);DiceCall("Closing","Finalizer",close[1],null);return false;});
        object[] timer={dice,steps};DiceCall("Timer","Postfix",timer);iterator=(IEnumerator)timer[1];iterator.MoveNext();iterator.MoveNext();Check(Detail().Detail.Contains("판정 키: check")&&Detail().Detail.Contains("연결 미확인"),"legacy timer/automatic dice uses its provided header key without needing a marker or prior choice");
        Check(((IEnumerable<MethodBase>)DiceCall("Timer","TargetMethods")).Count()==1&&((IEnumerable<MethodBase>)DiceCall("MenuBuilding","TargetMethods")).Count()==3,"explicit legacy timer and all native menu builder overloads are covered");
        DiceReset();MenuDialog.ActiveMenuDialog=menu;button.interactable=false;press=new object[]{button,null};DiceCall("ChoicePress","Prefix",press);Check(press[1]==null,"disabled button press cannot create incoming context");button.interactable=true;
        OptionInfoPatch.DisplayRefreshDepth=1;press=new object[]{button,null};DiceCall("ChoicePress","Prefix",press);Check(press[1]==null,"display-only calls cannot create user choice context");OptionInfoPatch.DisplayRefreshDepth=0;
        var error=new InvalidOperationException();Check(ReferenceEquals(DiceCall("ChoicePress","Finalizer",null,error),error)&&ReferenceEquals(DiceCall("Closing","Finalizer",null,error),error),"new native finalizers return the exact original exception");
        DiceReset();
    }
    private static IEnumerable<TypeDefinition> DiceTypes(TypeDefinition root){yield return root;foreach(var child in root.NestedTypes)foreach(var t in DiceTypes(child))yield return t;}
    private static void DiceApiContract(string candidate,string references)
    {
        using var story=ModuleDefinition.ReadModule(Path.Combine(references,"Mortal_Data/Managed/Mortal.Story.dll"));using var ui=ModuleDefinition.ReadModule(Path.Combine(references,"Mortal_Data/Managed/UnityEngine.UI.dll"));using var fungus=ModuleDefinition.ReadModule(Path.Combine(references,"Mortal_Data/Managed/Fungus.dll"));using var mod=ModuleDefinition.ReadModule(candidate);
        var dice=story.GetType("Mortal.Story.DiceMenuDialog");
        var roll=dice.Methods.Single(m=>m.Name=="ExecuteRoll");Check(roll.ReturnType.FullName=="System.Collections.IEnumerator"&&roll.Parameters.Select(p=>p.ParameterType.FullName).SequenceEqual(new[]{"System.String[]","System.Int32","System.String"})&&roll.Parameters[2].Name=="checkName","actual ExecuteRoll observer parameter/return signature");
        var timer=dice.Methods.Single(m=>m.Name=="ShowTimer");Check(timer.ReturnType.FullName=="System.Collections.IEnumerator"&&timer.Parameters.Count==3,"actual legacy timer is a distinct managed IEnumerator");
        foreach(string f in new[]{"_header","_diceHeader","_resultSelection","_triggerNextInput"})Check(dice.Fields.Any(x=>x.Name==f),"actual native field used read-only: "+f);
        Check(story.GetType("Mortal.Story.DiceHeader").Fields.Any(x=>x.Name=="_front")&&story.GetType("Mortal.Story.DiceOption").Fields.Any(x=>x.Name=="_normalText"),"actual header and result names have populated native UI fields");
        var press=ui.GetType("UnityEngine.UI.Button").Methods.Single(m=>m.Name=="Press");Check(press.IsPrivate&&!press.IsStatic&&press.Parameters.Count==0&&press.Body.Instructions.Any(i=>i.Operand is MethodReference c&&c.Name=="Invoke"),"actual shared mouse/touch/submit button press invokes native listeners");
        Check(fungus.GetType("Fungus.MenuDialog").Fields.Any(f=>f.Name=="cachedButtons")&&fungus.GetType("Fungus.MenuDialog").Methods.Count(m=>m.Name=="AddOption")==3,"actual cached button field and menu-building overloads");
        var patches=mod.GetType("DiceMaster.Information.DiceObservationPatches");var core=mod.GetType("DiceMaster.Information.DiceInformation");
        foreach(var t in new[]{patches,core})foreach(var method in DiceTypes(t).SelectMany(x=>x.Methods).Where(m=>m.HasBody))foreach(var call in method.Body.Instructions.Select(i=>i.Operand).OfType<MethodReference>())
            Check(!new[]{"GetResult","Execute","Dice","Range","Check","GetStoryText","ReadPreviewScript","Discover","AdvanceBudgeted","SetValue","SetActive","SetSelectedGameObject","Select","Press","AddListener","RemoveAllListeners","get_CachedButtons"}.Contains(call.Name),"dice observation invokes no gameplay/input/evaluation/discovery: "+call.FullName);
        foreach(string type in new[]{"Roll","Timer"}){var hook=patches.NestedTypes.Single(t=>t.Name==type).Methods.Single(m=>m.Name=="Postfix");Check(hook.Parameters.Last().ParameterType.FullName=="System.Collections.IEnumerator&"&&hook.ReturnType.FullName=="System.Void","only the transparent iterator is wrapped; native dice input/result are never replaced: "+type);}
        Check(core.Methods.Single(m=>m.Name=="CurrentRow").Body.Instructions.Select(i=>i.Operand).OfType<MethodReference>().All(m=>m.Name!="Sample"&&m.Name!="Invoke"||m.DeclaringType.FullName.Contains("Func")),"current notice does not initiate game sampling or analysis");
        Check(mod.GetType("DiceMaster.Information.InformationRuntime").Methods.Single(m=>m.Name=="DrawCore").Body.Instructions.Any(i=>i.Operand is MethodReference m&&m.DeclaringType.FullName==core.FullName&&m.Name=="CurrentRow"),"delivered overlay uses the actual event-driven dice notice");
    }
}
