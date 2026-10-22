using System;
using System.Collections;
using System.Collections.Generic;

namespace HarmonyLib { [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyPostfix : Attribute { } }
namespace UnityEngine
{
    public class Transform { public Transform Parent; public GameObject Owner; }
    public class GameObject
    {
        public bool activeInHierarchy = true;
        public readonly List<Component> Components = new();
        public readonly Transform transform;
        public GameObject() { transform = new Transform { Owner = this }; }
        public void Attach(Component c) { c.gameObject.Components.Remove(c); c.gameObject = this; Components.Add(c); }
    }
    public class Component
    {
        public GameObject gameObject = new(); public Transform transform => gameObject.transform;
        public Component() { gameObject.Components.Add(this); }
        public T GetComponent<T>() where T : class { foreach(var c in gameObject.Components) if(c is T match)return match; return null; }
        public T GetComponentInParent<T>() where T : class { for(var at=transform;at!=null;at=at.Parent) foreach(var c in at.Owner.Components) if(c is T match)return match; return null; }
    }
    public class Behaviour : Component { public bool isActiveAndEnabled = true; }
}
namespace UnityEngine.UI
{
    public class Text : UnityEngine.Behaviour { public string text; }
    public class Button : UnityEngine.Behaviour
    { public bool interactable=true; public bool IsInteractable()=>interactable; private void Press(){throw new Exception("Observer invoked input");} }
}
namespace Fungus
{
    public class MenuDialog : UnityEngine.Behaviour
    {
        public static MenuDialog ActiveMenuDialog;
        public UnityEngine.UI.Button[] cachedButtons;
        public virtual void Clear(){throw new Exception("Observer cleared native menu");}
        public bool AddOption(string x)=>true;
        public bool AddOption(string x,bool y)=>true;
        public bool AddOption(string x,bool y,object z)=>true;
    }
}
namespace Mortal.Core
{
    public class NormalOptionButton : UnityEngine.Behaviour { public UnityEngine.UI.Text[] _text; }
    public class PositionOptionButton : UnityEngine.Behaviour { public UnityEngine.UI.Text[] _titleText; }
    public class BreakOptionButton : UnityEngine.Behaviour { public UnityEngine.UI.Text[] _titleText; }
}
namespace Mortal.Story
{
    public class DiceHeader : UnityEngine.Behaviour { public UnityEngine.UI.Text _front = new(); }
    public class FakeTmp : UnityEngine.Behaviour { public string text {get;set;} }
    public class DiceOption : UnityEngine.Behaviour { public UnityEngine.UI.Text _sourceText = new(); public FakeTmp _normalText = new(); }
    public class DiceMenuDialog : Fungus.MenuDialog
    {
        public string _header="DiceHeader/check"; public DiceHeader _diceHeader=new();
        public int _resultSelection; public bool _triggerNextInput;
        public IEnumerator ExecuteRoll(string[] options,int destinyPoint,string checkName){throw new Exception("Observer rolled dice");}
        public IEnumerator ShowTimer(float duration,object env,object callback){throw new Exception("Observer started timer");}
    }
}
namespace DiceMaster.Information
{
    public static class InformationRuntime { public static void Failure(string key,Exception e){DiceMaster.DicePlugin.LogInfo(key+" "+e.GetType().Name);} }
    public static class ChoiceStoryLabels
    { public static string Rendered(string value)=>DiceRollRecord.Clean((value??"").Split(" <size=18>")[0],160); }
}
