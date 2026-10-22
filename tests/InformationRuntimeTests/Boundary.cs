using System;
using System.Collections.Generic;
using DiceMaster.LiveLog;
using DiceMaster.Information;
namespace UnityEngine { }
namespace HarmonyLib {
 public class Harmony { public void PatchAll(Type t) {} }
 public class HarmonyPatch:Attribute { public HarmonyPatch(Type t,string n){} }
 public class HarmonyPostfix:Attribute {}
 public class HarmonyPriority:Attribute { public HarmonyPriority(int n) {} }
 public static class Priority { public const int Last=0; }
}
namespace Mortal.Story { }
namespace Fungus { }
namespace Mortal.Core { public class LeanLocalizationResolver {} }
namespace DiceMaster.Config {
 public class ConfigElement<T> { public T Value; public bool Fail; public ConfigElement(T v){Value=v;} }
 public static class ConfigManager {
  public static ConfigElement<bool> EnableOptionInfo=new(true), EnableMapGuide=new(true),EnableOneShotHints=new(true),EnableBranchNotices=new(true),EnableShopEffectInfo=new(true);
  public static T GetConfigValue<T>(ConfigElement<T> e) {if(e.Fail)throw new InvalidOperationException();return e.Value;}
 }
}
namespace DiceMaster {
 public static class DicePlugin {public static List<string> Messages=new();public static void LogWarning(object s){Messages.Add(s.ToString());}public static void LogInfo(object s){Messages.Add(s.ToString());}}
}
namespace DiceMaster.LiveLog {
 public static class LogBridge {public static DiceMaster.LogUI.LogModel Model=new();}
 public static class PreviewConditions {public static int Clears;public static void ClearTalkContext(){Clears++;}}
}
namespace DiceMaster.LuaParse {public class Action {} public class LuaParser {public static LuaParser Instance=new();public Dictionary<string,List<Action>> OptionResult=new();}}
namespace DiceMaster.EventSelection {
 public class EventCandidate {public string Id,Result;public object Target;}
 public static class EventSelectionRuntime {public static bool Waiting,IsSwitch;public static long Token;public static List<EventCandidate> Candidates=new();}
}
namespace DiceMaster.Information {
 public static class InformationVisibility { public static bool TestBlocked; public static int Reads; public static bool Blocked { get { Reads++;return TestBlocked; } } }
 public static class MapInformationPatches {public static void Install(HarmonyLib.Harmony h) {}}
 public static class ShopInformationPatches {public static void Install(HarmonyLib.Harmony h) {}}
 public static class BranchObservationPatches {public static void Install(HarmonyLib.Harmony h) {}}
 public static class MapGuideSource {
  public static int DiagnosticReads; public static InformationRow DiagnosticRow(){DiagnosticReads++;return new InformationRow("map/performance/"+ContextKey,"지도 조회 성능","자료 상태","현재 상태","캐시 진단","");}
  public static void BeginScheduledReads() {} public static void EndScheduledReads() {}
  public static IList<MapRouteEvent> ReadChoicePool(string key)=>new List<MapRouteEvent>();
  public static bool SmallEventGraph(object o)=>true;public static Func<object,bool> MarkerVisible;public static bool MarkerSessionVisible=>SessionVisible;public static string MarkerContextKey=>ContextKey;public static int MarkerReads;public static bool MarkerFail;
  public static List<DisplayQuery> DiscoverMarkers(bool g,bool o)=>new(){new DisplayQuery("markers",()=>{MarkerReads++;if(MarkerFail)throw new InvalidOperationException();return new MapMarkerSnapshot();})};
  public static Func<bool> Enabled;public static bool Visible,NoSelectionSession;public static bool SessionVisible=>Visible||NoSelectionSession;public static string ContextKey="m";public static int Reads,DetailReads,Discoveries,Clears;public static bool Fail;
  public static List<DisplayQuery> Discover(bool g,bool o,bool details=false){Discoveries++;if(!Visible&&(!NoSelectionSession||!g))return new();return new(){new DisplayQuery("map",()=>{Reads++;if(details)DetailReads++;if(Fail)throw new InvalidOperationException();var snapshot=new MapGuideSnapshot{HasSelection=Visible,ContextKey=ContextKey,Summary=Visible?new InformationRow("map","장소","지도","가능","조건과 효과"):null};snapshot.Entries.Add(new InformationRow("mission/first","첫 임무",NoSelectionSession?"장소 무관":"선택 장소","발생 대기",details?"첫 조건과 효과":"상세에서 확인"));snapshot.Entries.Add(new InformationRow("event/second","둘째 이야기","기타 연결","확인 불가",details?"둘째 조건과 효과":"상세에서 확인"));return snapshot;})};}public static void Clear(){Clears++;}public static void ClearSnapshots(){Clears++;}public static void ClearSnapshotsPreservingSyntax(){Clears++;}
 }
 public static class ShopEffectSource {
  public static Func<bool> Enabled;public static bool Visible,Fail;public static string ContextKey="s";public static int Reads;public static string Summary;
  public static List<DisplayQuery> Discover(){return new(){new DisplayQuery("shop",()=>{Reads++;if(Fail)throw new InvalidOperationException();return new InformationRow("shop","책","학습","현재 상태","학습 효과·조건 상세","학습: 근력 +5");})};}
  public static void Clear(){Visible=false;ContextKey+="c";}public static void RenderSummary(string s){Summary=s;}
 }
 public static class BranchHistory {
  public static bool Enabled;public static System.Action Cleared;public static string ContextKey="b";public static int Reads;
  public static List<DisplayQuery> Discover(){return new(){new DisplayQuery("branch",()=>{Reads++;return new InformationRow("branch","분기","실제결과","관찰","당시결과");})};}
  public static void Clear(){ContextKey+="c";Cleared?.Invoke();}
 }
 public class OneShotResult {public bool Failed;}
 public static class OneShotProof {
  public static int Reads,Clears;public static void Clear(){Clears++;}public static string CachedEventTag(object o)=>"";public static string CachedOptionTag(string k)=>"";
  public static string CachedEventDetail(object o)=>"1회성 확인 불가: 활성 조건 미연결";public static string CachedOptionDetail(string k)=>"1회성 확인 불가: 활성 조건 미연결";
  public static OneShotResult ForEvent(object t,string r){Reads++;return new();}public static OneShotResult ForOption(string k,List<DiceMaster.LuaParse.Action> a){Reads++;return new();}
 }
}

namespace DiceMaster.Information {
 public class MapRouteEvent { }
 public class ChoiceStorySnapshot { public string ContextKey; public readonly List<InformationRow> Entries=new(); }
 public static class ChoiceStoryPatches {public static void Install(HarmonyLib.Harmony h) {}}
 public static class ChoiceStorySource {
  public static Func<bool> Enabled,GuideEnabled,OneShotEnabled;public static Func<object,bool> Interactive,CanReadEvent;public static Func<string,IList<MapRouteEvent>> PoolReader; public static bool Visible,Fail;public static bool MenuVisible=>Visible;public static string ContextKey="choice";public static int Reads,DetailReads,Clears;
  public static List<DisplayQuery> Discover(bool details=false)=>new(){new DisplayQuery(ContextKey,()=>{Reads++;if(Fail)throw new InvalidOperationException();if(details)DetailReads++;var snapshot=new ChoiceStorySnapshot{ContextKey=ContextKey};snapshot.Entries.Add(new InformationRow("choice/story","사사형","연결 이야기","선택 조건 충족 (현재)",details?"사사형 → 대화 항목":"상세에서 확인"));return snapshot;})};
  public static void ContextChanged(){Clears++;} public static void ClearSnapshots(){Clears++;} public static void Clear(){Visible=false;Clears++;ContextKey+="c";}
 }
 public static class OptionInformationPatches {public static void Install(HarmonyLib.Harmony h){}}
 public static class InformationLifecyclePatches {public static void Install(HarmonyLib.Harmony h){}}
 public static class OptionHintSource {public static Func<bool> Enabled; public static bool Visible;public static string ContextKey="o";public static void Clear(){}public static void RenderHints(){}public static List<DisplayQuery> Discover()=>new();}
}

namespace DiceMaster.Information { public static class InformationAnchors { public static bool IsVisible(object o)=>true; public static bool IsInteractive(object o)=>true; } }

namespace DiceMaster.Information { internal static class MapDefinitionAssets { internal static void Install() {} internal static void Clear() {} } }
