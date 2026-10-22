using DiceMaster.Information;
using UnityEngine;
using UnityEngine.EventSystems;
internal static partial class Program {
 static void DiceGuideRendering(){
  var old=Event.current;var selected=EventSystem.current.currentSelectedGameObject;
  try{
   Event.current=new Event{type=EventType.Repaint};InformationRuntime.TestMap(null);InformationRuntime.TestChoices(null);InformationRuntime.TestMarkers(null);
   var anchor=UiAnchor();anchor.gameObject.Components.Add(new UnityEngine.UI.Selectable{Interactable=false});
   var snapshot=new DiceGuideSnapshot{ContextKey="dice"};snapshot.Entries.Add(new InformationRow("dice/1","결과 1","연결","후보","상세","이야기",anchor,false,false,true));
   InformationRuntime.TestDice(snapshot);GUI.BadgeLabels.Clear();InformationRuntime.Draw(false,false,default,1000);
   Check(GUI.BadgeLabels.Count==1&&!InformationAnchors.IsInteractive(anchor),"non-interactable dice result can carry a passive badge without making a native button selectable");
   GUI.BadgeLabels.Clear();InformationRuntime.Draw(true,false,default,1001);Check(GUI.BadgeLabels.Count==0,"log occludes dice badges");
   GUI.BadgeLabels.Clear();InformationRuntime.Draw(false,true,default,1002);Check(GUI.BadgeLabels.Count==0,"event popup occludes dice badges");
   Mortal.Story.StoryManager.Instance=new Mortal.Story.StoryManager{_statusOpen=true};GUI.BadgeLabels.Clear();InformationRuntime.Draw(false,false,default,1003);Check(GUI.BadgeLabels.Count==0,"status window occludes dice badges");
   Mortal.Story.StoryManager.Instance=null;DiceGuideSource.ContextKey="other";GUI.BadgeLabels.Clear();InformationRuntime.Draw(false,false,default,1004);Check(GUI.BadgeLabels.Count==0,"stale dice snapshot cannot paint on another screen");
   Check(EventSystem.current.currentSelectedGameObject==selected&&Event.current.type==EventType.Repaint,"passive dice painting does not consume event or change focus");
  }finally{InformationRuntime.TestDice(null);DiceGuideSource.ContextKey="dice";Mortal.Story.StoryManager.Instance=null;Event.current=old;GUI.BadgeLabels.Clear();}
 }
}
