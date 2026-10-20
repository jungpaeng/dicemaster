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
namespace DiceMaster.LiveLog {public static class LogBridge {public static DiceMaster.LogUI.LogModel Model=new();}}
namespace DiceMaster.LuaParse {public class Action {} public class LuaParser {public static LuaParser Instance=new();public Dictionary<string,List<Action>> OptionResult=new();}}
namespace DiceMaster.EventSelection {
 public class EventCandidate {public string Id,Result;public object Target;}
 public static class EventSelectionRuntime {public static bool Waiting,IsSwitch;public static long Token;public static List<EventCandidate> Candidates=new();}
}
namespace DiceMaster.Information {
 public static class MapInformationPatches {public static void Install(HarmonyLib.Harmony h) {}}
 public static class ShopInformationPatches {public static void Install(HarmonyLib.Harmony h) {}}
 public static class BranchObservationPatches {public static void Install(HarmonyLib.Harmony h) {}}
 public static class MapGuideSource {
  public static Func<bool> Enabled;public static bool Visible;public static string ContextKey="m";public static int Reads,Discoveries,Clears;public static bool Fail;
  public static List<DisplayQuery> Discover(bool g,bool o){Discoveries++;return new(){new DisplayQuery("map",()=>{Reads++;if(Fail)throw new InvalidOperationException();return new InformationRow("map","장소","지도","가능","조건과 효과");})};}public static void Clear(){Clears++;}public static void ClearSnapshots(){Clears++;}
 }
 public static class ShopEffectSource {
  public static Func<bool> Enabled;public static bool Visible;public static string ContextKey="s";public static int Reads;public static string Summary;
  public static List<DisplayQuery> Discover(){return new(){new DisplayQuery("shop",()=>{Reads++;return new InformationRow("shop","책","학습","현재 상태","학습 효과");})};}
  public static void Clear(){Visible=false;ContextKey+="c";}public static void RenderSummary(string s){Summary=s;}
 }
 public static class BranchHistory {
  public static bool Enabled;public static System.Action Cleared;public static string ContextKey="b";public static int Reads;
  public static List<DisplayQuery> Discover(){return new(){new DisplayQuery("branch",()=>{Reads++;return new InformationRow("branch","분기","실제결과","관찰","당시결과");})};}
  public static void Clear(){ContextKey+="c";Cleared?.Invoke();}
 }
 public class OneShotResult {public bool Failed;}
 public static class OneShotProof {
  public static int Reads,Clears;public static void Clear(){Clears++;}public static string CachedEventTag(object o)=>" · 1회성 확인 불가";public static string CachedOptionTag(string k)=>" · 1회성 확인 불가";
  public static OneShotResult ForEvent(object t,string r){Reads++;return new();}public static OneShotResult ForOption(string k,List<DiceMaster.LuaParse.Action> a){Reads++;return new();}
 }
}

namespace DiceMaster.Information {
 public static class OptionInformationPatches {public static void Install(HarmonyLib.Harmony h){}}
 public static class InformationLifecyclePatches {public static void Install(HarmonyLib.Harmony h){}}
 public static class OptionHintSource {public static Func<bool> Enabled; public static bool Visible;public static string ContextKey="o";public static void Clear(){}public static void RenderHints(){}public static List<DisplayQuery> Discover()=>new();}
}
