using System;
using System.Collections;
using DiceMaster.Information;
using UnityEngine;
internal static partial class Program
{
    private static void DiceNoticeRendering()
    {
        var flags=InformationRuntime.TestFlags;int w=Screen.width,h=Screen.height;
        try
        {
            Screen.width=1920;Screen.height=1200;Application.isFocused=true;
            InformationRuntime.TestFlags=new[]{false,false,true,false};BranchHistory.Clear();
            InformationRuntime.TestMap(null);InformationRuntime.TestMarkers(null);InformationRuntime.TestChoices(null);ChoiceStorySource.MenuVisible=false;
            Mortal.Free.FreeManager.Instance=null;Mortal.Story.StoryManager.Instance=null;
            var owner=new object();bool visible=true;DiceInformation.Visible=o=>ReferenceEquals(o,owner)&&visible;DiceInformation.Clock=()=>1;
            var c=DiceInformation.BeginChoice("실제 선택");DiceInformation.EndChoice(c,true);
            IEnumerator Steps(){yield return new object();var close=DiceInformation.BeginClosing(owner,1);DiceInformation.EndClosing(close,null);}
            var iterator=DiceInformation.Observe(Steps(),owner,"key",()=>new DiceInformation.View{Visible=true,Name="판정 제목",Labels=new[]{"확정 결과"},Raw=new[]{"O_key|<20"}});iterator.MoveNext();
            void Draw(float t=0,bool log=false,bool popup=false){GUI.PassiveLabels.Clear();InformationRuntime.Draw(log,popup,new Rect(12,12,500,500),t);}
            Draw();Check(GUI.PassiveLabels.Count==1&&GUI.PassiveLabels[0].Text.Contains("실제 선택")&&GUI.PassiveLabels[0].Text.Contains("판정 제목")&&!GUI.PassiveLabels[0].Text.Contains("확정 결과"),"dice notice works with both marker settings OFF and never shows a preliminary outcome");
            Check(GUI.PassiveLabels[0].Rect.x>Screen.width*.5f,"active dice context reuses the existing upper-right branch notice position");
            int measures=GUIStyle.Measurements;for(int at=0;at<1000;at++)Draw();Check(GUIStyle.Measurements==measures,"one thousand warm dice draws reuse measured text without resampling game data");
            Draw(log:true);Check(GUI.PassiveLabels.Count==0,"log hides active dice context");Draw(popup:true);Check(GUI.PassiveLabels.Count==0,"event popup hides active dice context");
            var native=new Mortal.Story.StoryManager{_statusOpen=true};Mortal.Story.StoryManager.Instance=native;Draw();Check(GUI.PassiveLabels.Count==0,"native status window hides dice notice even with both marker settings OFF");native._statusOpen=false;
            Application.isFocused=false;Draw();Check(GUI.PassiveLabels.Count==0,"lost application focus hides dice notice");Application.isFocused=true;
            visible=false;Draw();Check(GUI.PassiveLabels.Count==0,"inactive or replaced native dice owner has no current notice");visible=true;
            iterator.MoveNext();Draw(1);Check(GUI.PassiveLabels.Count==1&&GUI.PassiveLabels[0].Text.Contains("확정 결과")&&!GUI.PassiveLabels[0].Text.Contains("진행 중"),"accepted result replaces rolling context in the same branch notice slot");Draw(6);Check(GUI.PassiveLabels.Count==0,"final dice notice expires after five seconds");
            BranchHistory.Clear();Screen.width=400;Screen.height=300;int previousFont=DiceMaster.Config.ConfigManager.LogFontSize.Value;
            DiceMaster.Config.ConfigManager.LogFontSize.Value=24;
            IEnumerator LongSteps(){yield return null;}
            var longRoll=DiceInformation.Observe(LongSteps(),owner,"long",()=>new DiceInformation.View{Visible=true,Name=new string('가',160),Labels=new[]{new string('나',160)}});longRoll.MoveNext();Draw();
            if(GUI.PassiveLabels.Count!=0){var row=GUI.PassiveLabels[0];var style=new GUIStyle(GUI.skin.box){fontSize=24,wordWrap=true,richText=row.RichText};Check(style.CalcHeight(new GUIContent(row.Text),row.Rect.width)<=row.Rect.height+.01f,"long dice names fit the available small viewport instead of clipping");}
            ((IDisposable)longRoll).Dispose();DiceMaster.Config.ConfigManager.LogFontSize.Value=previousFont;
            InformationRuntime.TestFlags[2]=false;Draw(1);Check(GUI.PassiveLabels.Count==0,"existing branch setting OFF suppresses dice notices");
            Check(InformationRuntime.TestErrors.Count==0,"dice rendering has no swallowed boundary errors");
        }
        finally{BranchHistory.Clear();InformationRuntime.TestFlags=flags;Screen.width=w;Screen.height=h;Application.isFocused=true;Mortal.Story.StoryManager.Instance=null;GUI.PassiveLabels.Clear();}
    }
}
