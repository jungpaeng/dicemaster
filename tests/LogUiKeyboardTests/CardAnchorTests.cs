using System;
using System.Collections.Generic;
using DiceMaster.Information;
using Mortal.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal static partial class Program
{
    private static void Area(Component c, Canvas canvas, Rect rect)
    { c.gameObject.transform = new RectTransform { Owner = c.gameObject, Parent = canvas.transform, position = new Vector3(rect.x, Screen.height - rect.y - rect.height, 1), Width = rect.width, Height = rect.height }; }
    private static Text CardTitle(Canvas canvas, Rect box, Rect ink)
    {
        var text = new Text { text = "사사형" }; Area(text, canvas, box);
        var mesh = new TextGenerator();
        float x = ink.x - box.x, y = box.y + box.height - ink.y - ink.height;
        foreach (var p in new[] { new Vector3(x,y,0), new Vector3(x,y+ink.height,0), new Vector3(x+ink.width,y+ink.height,0), new Vector3(x+ink.width,y,0), default,default,default,default }) mesh.Vertices.Add(new UIVertex { position = p });
        text.Render(mesh); return text;
    }
    private static bool Overlaps(Rect a, Rect b) => a.x < b.x + b.width && a.x + a.width > b.x && a.y < b.y + b.height && a.y + a.height > b.y;
    private static void CardAnchorRendering()
    {
        int w = Screen.width, h = Screen.height; var ev = Event.current;
        try
        {
            Screen.width = 1920; Screen.height = 1200; var canvas = new Canvas();
            var first = new PositionOptionButton(); Area(first, canvas, new Rect(1270,400,431,141));
            var title = CardTitle(canvas, new Rect(1500,395,240,80), new Rect(1570,420,65,32));
            var hidden = CardTitle(canvas, new Rect(50,50,240,80), new Rect(100,60,65,32)); hidden.gameObject.activeInHierarchy = false;
            first._titleText = new[] { hidden,title };
            var picture = new Image(); Area(picture,canvas,new Rect(1000,278,355,350)); first._iconImage = new[] { picture };
            var desc = new Text(); Area(desc,canvas,new Rect(1215,505,525,125)); first._descText = new[] { desc };
            var second = new PositionOptionButton(); Area(second,canvas,new Rect(1010,675,431,141));
            second._titleText = new[] { CardTitle(canvas,new Rect(1220,670,240,80),new Rect(1280,695,100,32)) };
            var picture2 = new Image(); Area(picture2,canvas,new Rect(735,548,355,350)); second._iconImage = new[] { picture2 };
            var desc2 = new Text(); Area(desc2,canvas,new Rect(1040,770,525,45)); second._descText = new[] { desc2 };
            Rect badge, body1, body2;
            Check(InformationAnchors.TryBadgeRect(first,32,false,out badge) && badge == new Rect(1530,420,32,32), "card badge is beside rendered glyphs, not the oversized title box or root corner");
            Check(InformationAnchors.TryCardRect(first,out body1) && body1.x == 1000 && body1.y == 278 && body1.y+body1.height == 630, "card obstacle includes artwork and description outside the native button root");
            InformationAnchors.TryCardRect(second,out body2);
            Rect tooltip;
            Check(InformationLayout.TryNearCards(body1,new Rect(12,12,1896,1176),420,110,new[] { body1,body2 },out tooltip) && !Overlaps(tooltip,body1) && !Overlaps(tooltip,body2) && tooltip.y >= 910,
                "screenshot-shaped cards place the tooltip below the group, clear of the other card and room title");
            Check(!InformationLayout.TryNearCards(body1,new Rect(0,0,100,100),100,100,new[] { new Rect(0,0,2000,2000) },out tooltip), "crowded viewport hides the passive tooltip rather than covering native choices");
            var tooMany = new List<Rect>(); for(int n=0;n<65;n++)tooMany.Add(body1);
            Check(!InformationLayout.TryNearCards(body1,new Rect(0,0,1920,1200),420,100,tooMany,out tooltip), "card obstacle count has a hard bound");
            var rest = new BreakOptionButton(); Area(rest,canvas,new Rect(250,300,450,55));
            rest._titleText = new[] { CardTitle(canvas,new Rect(377,300,370,55),new Rect(550,312,80,30)) };
            Check(InformationAnchors.TryBadgeRect(rest,32,false,out badge) && badge == new Rect(510,311,32,32), "break/activity cards share the same actual-name anchor");
            ((RectTransform)title.transform).ScaleX = 1.5f; ((RectTransform)title.transform).ScaleY = 1.5f;
            Check(InformationAnchors.TryBadgeRect(first,32,false,out badge) && Math.Abs(badge.x - 1565)<.01f && Math.Abs(badge.y-400.5f)<.01f, "glyph projection respects native transform scale");
            ((RectTransform)title.transform).ScaleX = ((RectTransform)title.transform).ScaleY = 1;
            // A real renderer pass: ready/restoring focus, delayed spinner,
            // unbadged sibling obstacles, no input capture and no warm metrics.
            Application.isFocused = true; InformationRuntime.TestFlags = new[] {true,true,false,true};
            InformationRuntime.TestMap(null); InformationRuntime.TestMarkers(null); MapGuideSource.Selected = null;
            Mortal.Free.FreeManager.Instance = null; Mortal.Story.StoryManager.Instance = null;
            var snapshot = new ChoiceStorySnapshot { ContextKey="card-geometry" }; snapshot.Anchors.Add(first); snapshot.Anchors.Add(second); snapshot.Preparing.Add(first);
            ChoiceStorySource.MenuVisible = true; ChoiceStorySource.Current = snapshot; ChoiceStorySource.Selected = null; InformationRuntime.TestChoices(snapshot);
            Event.current = new Event { type=EventType.Repaint,mousePosition=new Vector2(0,0) }; EventSystem.current.currentSelectedGameObject = null;
            void Draw(float time) { GUI.PassiveLabels.Clear(); GUI.BadgeLabels.Clear(); GUI.Spinners.Clear(); InformationRuntime.Draw(false,false,default,time); }
            Draw(10); Draw(10.3f);
            Check(GUI.Spinners.Count==1 && Math.Abs(GUI.Spinners[0].Rect.x+GUI.Spinners[0].Rect.width*.5f-1546)<.01f && Math.Abs(GUI.Spinners[0].Rect.y+GUI.Spinners[0].Rect.height*.5f-436)<.01f,
                "pending card spinner is centered at the future badge beside its name");
            var row = new InformationRow("card","휴식","연결","","","경로 후보: 휴식 → 이야기 · 진행 가능성 있음",first,false,false,true);
            snapshot.Preparing.Clear(); snapshot.Entries.Add(row); ChoiceStorySource.Selected = row;
            Draw(10.4f);
            Check(GUI.BadgeLabels.Count==1 && GUI.Spinners.Count==0 && GUI.PassiveLabels.Count==1 && GUI.PassiveLabels[0].Text.StartsWith("현재 선택의 안내: 휴식") && !Overlaps(GUI.PassiveLabels[0].Rect,body1) && !Overlaps(GUI.PassiveLabels[0].Rect,body2),
                "ready card replaces spinner in place and names its tooltip outside every visible card, including an empty sibling");
            int measures = GUIStyle.Measurements, uploads = Texture2D.Uploads;
            for(int at=0;at<1000;at++) Draw(11);
            Check(GUIStyle.Measurements==measures && Texture2D.Uploads==uploads && Event.current.type==EventType.Repaint && EventSystem.current.currentSelectedGameObject==null,
                "warm card painting reuses tooltip metrics/textures and never changes native selection or consumes input");
            // Selecting another toggle variant must use its new visible glyphs
            // immediately, not cache a previous frame's name/position.
            title.gameObject.activeInHierarchy=false; hidden.gameObject.activeInHierarchy=true; Draw(12);
            Check(GUI.BadgeLabels.Count==1 && GUI.BadgeLabels[0].Rect.x==60 && GUI.BadgeLabels[0].Rect.y==60, "native hover/focus variant changes refresh glyph geometry on the next paint");
            hidden.Render(null); Draw(13); Check(GUI.BadgeLabels.Count==0, "unrendered title cache hides the card marker without forcing text generation or using the wrong root");
            InformationAnchors.EndPaint();
            foreach(var size in new[] { new Vector2(1280,720),new Vector2(1920,1200),new Vector2(2560,1080),new Vector2(800,600) })
            {
                var view = new Rect(0,0,size.x,size.y); Rect inline = InformationLayout.InlineBadge(new Rect(100,200,70,40),view,InformationLayout.BadgeSize((int)size.x,(int)size.y,false));
                Check(inline.x>=0 && inline.y>=0 && inline.x+inline.width<=size.x && inline.y+inline.height<=size.y && inline.x+inline.width<=92, "inline markers remain beside the title across viewport aspect ratios");
            }
            Check(InformationRuntime.TestErrors.Count==0,"card renderer completes without boundary failures");
        }
        finally
        {
            InformationAnchors.EndPaint(); Screen.width=w;Screen.height=h;Event.current=ev;
            InformationRuntime.TestChoices(null); ChoiceStorySource.Current=null;ChoiceStorySource.Selected=null;ChoiceStorySource.MenuVisible=false;
            GUI.PassiveLabels.Clear();GUI.BadgeLabels.Clear();GUI.Spinners.Clear();
        }
    }
}
