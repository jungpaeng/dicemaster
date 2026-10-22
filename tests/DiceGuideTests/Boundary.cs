using DiceMaster.Information;
using DiceMaster.LiveLog;
namespace UnityEngine {
 public class Component:Object {public Transform transform=new RectTransform();public GameObject gameObject=new();}
 public class Behaviour:Component {public bool isActiveAndEnabled=true;}
 public class GameObject {public bool activeInHierarchy=true;}
 public class Transform {}
 public class RectTransform:Transform {public Rect rect=new(){width=3000,yMax=100};public Vector3 Origin;public Vector3 InverseTransformPoint(Vector3 v)=>v;public void GetWorldCorners(Vector3[] v){v[0]=Origin;v[2]=new(){x=Origin.x+rect.width,y=rect.yMax};}}
 public struct Rect {public float width,xMin,yMin,yMax;}
 public struct Vector3 {public float x,y,z;}
 public struct Vector2 {public float x,y;}
 public static class Time {public static float unscaledTime;}
}
namespace UnityEngine.UI {
 public class Text:UnityEngine.Component {public string text {get;set;}="";}
 public class Button:UnityEngine.Behaviour {public bool interactable;public object Option;public T GetComponent<T>() where T:class=>Option as T;}
}
namespace Fungus {
 public class MenuDialog:UnityEngine.Behaviour {public static MenuDialog ActiveMenuDialog;public UnityEngine.UI.Button[] cachedButtons;public void Awake()=>throw new Exception("Native Awake must not run");public void Clear()=>throw new Exception("Native clear must not run");}
}
namespace Mortal.Story {
 public class FakeTmp:UnityEngine.Component {public string text {get;set;}="native effects";public float fontSize {get;set;}=18;public int Measures;public UnityEngine.Vector2 GetPreferredValues(string value){Measures++;return new(){x=value.Length*7};}}
 public class DiceOption:UnityEngine.Behaviour {public UnityEngine.UI.Text _sourceText=new();public FakeTmp _normalText=new(),_highText=new();public void UpdateContent()=>throw new Exception("Native content must not run");}
 public class DiceMenuDialog:Mortal.Core.CustomMenuDialog {public void OnDisable()=>throw new Exception("Native disable must not run");public System.Collections.IEnumerator ExecuteRoll(string[] options,int destinyPoint,string checkName)=>throw new Exception("Native roll must not run");}
}
namespace Mortal.Core {
 public class CustomMenuDialog:Fungus.MenuDialog {public void UpdateOptions()=>throw new Exception("Native UpdateOptions must not run");}
 public class PositionEventData {}
 public class MissionManagerData {public static MissionManagerData _instance;public bool MainMissionStart;public object MainCheckData;public Dictionary<string,object> ActiveSubMissions=new();}
}
namespace HarmonyLib {
 public class Harmony {public void PatchAll(Type t){}}
 public class HarmonyPatch:Attribute {public HarmonyPatch(Type t,string method){}}
 public class HarmonyFinalizer:Attribute {} public class HarmonyPrefix:Attribute {} public class HarmonyPostfix:Attribute {} public class HarmonyPriority:Attribute {public HarmonyPriority(int n){}}
 public static class Priority {public const int First=800,Last=0;}
}
namespace DiceMaster {public static class OptionInfoPatch {public static int DisplayRefreshDepth;}}
namespace DiceMaster.LiveLog {
 public static class LogBridge {public sealed class ModelType {public long ContextId;}public static ModelType Model=new();}
 public static class DisplayNames {public static string Text(string text)=>text;public static string Locale(string key,string fallback)=>key.StartsWith("Position/title/")?key.Substring(15):key.StartsWith("Story/")?key.Substring(6):fallback;}
}
namespace DiceMaster {
 public static class DataHelper {public static string GetEventName(Mortal.Core.PositionEventData item)=>"event";public static Dictionary<string,string> Scripts=new(StringComparer.OrdinalIgnoreCase);public static ScriptReadResult ReadPreviewScript(string name)=>Scripts.TryGetValue(name,out var text)?ScriptReadResult.Success(text):ScriptReadResult.Missing();}
}
namespace DiceMaster.Information {
 public static class ChoiceStorySource {internal sealed class PendingChoice{internal string Name,Kind;}internal static Dictionary<string,PendingChoice> Pending=new();internal static Dictionary<string,PendingChoice> WaitingScripts()=>Pending;}
 public static class InformationRuntime {public static List<Exception> Errors=new();public static void Failure(string key,Exception e)=>Errors.Add(e);}
 public static class MapGuideSource {public static IList<MapRouteEvent> ReadChoicePool(string key)=>new List<MapRouteEvent>();public static bool SmallEventGraph(object target)=>false;}
 public class OneShotResult {public bool Proven,Failed;public string DetailText="fixture";}
 public static class OneShotProof {public static bool TryGetEvent(object target,string script,out OneShotResult result){result=null;return false;}public static OneShotResult ForEvent(object target,string script)=>new();}
}
