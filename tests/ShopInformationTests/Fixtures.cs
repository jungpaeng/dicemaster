using System;
using System.Collections.Generic;
using System.Reflection;
namespace UnityEngine
{
    public class Object { }
    public class Behaviour : Object { public bool isActiveAndEnabled = true; }
}
namespace UnityEngine.UI
{
    public class Text : UnityEngine.Behaviour { private string value = ""; public int Writes; public string text { get { return value; } set { this.value = value; Writes++; } } }
}
namespace HarmonyLib
{
    public sealed class HarmonyMethod { public HarmonyMethod(Type type, string name) { if (type.GetMethod(name, BindingFlags.Static|BindingFlags.NonPublic) == null) throw new Exception(name); } }
    public sealed class Harmony { public readonly List<string> Methods = new List<string>(); public void Patch(MethodInfo original, HarmonyMethod postfix = null) { if (original == null) throw new Exception("Missing patch target"); Methods.Add(original.Name); } }
    public static class AccessTools { public static MethodInfo Method(Type type, string name) { return type.GetMethod(name); } }
}
namespace DiceMaster.LiveLog
{
    public static class SafeDisplayEvaluation
    {
        public static int Reads;
        public static int ReadStatFinalValue(object value) { Reads++; return ((Mortal.Core.GameStat)value)._value; }
    }
    public static class DisplayNames
    {
        public static int Reads;
        public static string Text(string value) { Reads++; return value; }
        public static string LookupName(string key, string fallback) { Reads++; return fallback; }
    }
}
namespace Mortal.Story
{
    public sealed class ShopPanel : UnityEngine.Behaviour
    {
        public UnityEngine.UI.Text _dialogDesc = new UnityEngine.UI.Text(), _dialogTitle = new UnityEngine.UI.Text();
        public void OnItemHover(Mortal.Core.IGameItem item) { throw new Exception("Do not execute game callback"); }
        public void HideDialog() { }
        public void OnDisable() { }
        public void CloseButtonClick() { }
    }
}
namespace Mortal.Core
{
    public interface IGameItem { }
    public class ItemData : IGameItem { public string _id = "id"; public int _currentCount; public void AddCount(int n) { throw new Exception("Item mutation"); } }
    public enum GameStatType { Vitality = 0, Knowledge = 5 }
    public sealed class Consumables : ItemData { public ItemEffect[] _effects = Array.Empty<ItemEffect>(); }
    public sealed class ItemEffect { public GameStatType _statType = GameStatType.Knowledge; public int _value; }
    public sealed class Book : ItemData
    {
        public bool _martial = true;
        public int _currentMartialLevel, _currentLevelPoint, _martialLevel = 1;
        public MartialLevelData[] _martialLevelData = Array.Empty<MartialLevelData>();
        public bool Learn(int n) { throw new Exception("Learning mutation"); }
    }
    public sealed class Misc : ItemData { }
    public sealed class SpecialItem : ItemData { }
    public sealed class GameStat
    {
        public GameStatType _type = GameStatType.Knowledge;
        public int _value, _min = 0, _max = 100, _property;
        public GameStat _totalAddData;
        public object[] _statAdditionData = Array.Empty<object>();
        public void AddValue(int n) { throw new Exception("Stat mutation"); }
    }
    public sealed class RelationshipStat { public GameStatType _type = GameStatType.Knowledge; public int _value, _max = 100; }
    public sealed class PlayerTalentData { public string _id = "talent"; public int _level, _maxLevel = 10; }
    public sealed class BookDataCollection { public List<Book> _list = new List<Book>(); }
    public sealed class MartialLevelData
    {
        public int _points = 10;
        public MartialLearnCondition _condition = new MartialLearnCondition();
        public List<MartialModifyStat> _levelupStats = new List<MartialModifyStat>();
        public List<MartialLearnConsumable> _consumable = new List<MartialLearnConsumable>();
        public bool Check() { throw new Exception("Game condition"); }
        public void Consumable() { throw new Exception("Consumption mutation"); }
    }
    public sealed class MartialLearnCondition { public List<MartialLearnCompareGroup> _list = new List<MartialLearnCompareGroup>(); public bool Check() { throw new Exception("Game condition"); } }
    public sealed class MartialLearnCompareGroup { public MartialLearnCompareItem[] _itemList = Array.Empty<MartialLearnCompareItem>(); }
    public sealed class MartialLearnCompareItem { public int _compareOp, _value; public MartialStatReference _stat; public bool Check() { throw new Exception("Game condition"); } }
    public sealed class MartialStatReference
    {
        public int _type = 1;
        public GameStat _stat;
        public RelationshipStat _relationship;
        public PlayerTalentData _talent;
        public Book _book;
        public Misc _misc;
        public SpecialItem _specialItem;
        public BookDataCollection _bookCollection;
        public int GetValue() { throw new Exception("Game reference chain"); }
        public void AddValue(int n) { throw new Exception("Game mutation"); }
    }
    public sealed class MartialModifyStat { public MartialStatReference _stat; public int _value; public bool _center; public void Apply() { throw new Exception("Effect mutation"); } }
    public sealed class MartialLearnConsumable { public int _logicType; public List<MartialModifyStat> _consumable = new List<MartialModifyStat>(); public void Apply() { throw new Exception("Effect mutation"); } }
}

// These are parser/resource boundary fixtures; production ShopUseEffects and
// provider code are linked unchanged. Actual parser IL is tested separately.
namespace DiceMaster.LiveLog
{
    public enum ScriptReadState { Success, Missing, Waiting, Failed }
    public class ScriptReadResult { public ScriptReadState State; public string Text; }
    public enum PreviewAnalysisState { Supported, Partial, Unsupported, NoEffects, Waiting, ReadFailure }
    public class PreviewEffect
    {
        public string Owner = "statmodifymanager", Name, ValidationError;
        public bool Followup;
        public List<object> Constants = new List<object>();
    }
    public class PreviewAnalysis
    {
        public List<PreviewEffect> Effects = new List<PreviewEffect>();
        public PreviewAnalysisState State = PreviewAnalysisState.Supported;
    }
    public static class PreviewDisplay { public static void Validate(PreviewAnalysis value) { } }
}
namespace DiceMaster.LuaParse
{
    public class Action { public DiceMaster.LiveLog.PreviewAnalysis Analysis; public string Description = ""; }
    public class LuaParser
    {
        public static int Parses;
        public static Dictionary<string,List<Action>> Options = new Dictionary<string,List<Action>>();
        public bool IncompleteOptionDiscovery;
        public Dictionary<string,List<Action>> ExtractOptionActionsFromLua(string key, string source) { Parses++; return Options; }
        public static void RefreshPreviewReads(List<Action> actions) { }
        public static string Description(List<Action> actions)
        { var lines = new List<string>(); foreach (var a in actions) lines.Add(a.Description); return string.Join("\n", lines); }
    }
}
namespace DiceMaster
{
    public static class DataHelper
    {
        public static int Reads;
        public static DiceMaster.LiveLog.ScriptReadState State = DiceMaster.LiveLog.ScriptReadState.Success;
        public static DiceMaster.LiveLog.ScriptReadResult ReadPreviewScript(string key)
        { Reads++; return new DiceMaster.LiveLog.ScriptReadResult { State = State, Text = "fixture food-use source" }; }
    }
    public static class PositionEventCache
    {
        private static readonly DiceMaster.LuaParse.LuaParser parser = new DiceMaster.LuaParse.LuaParser();
        public static DiceMaster.LuaParse.LuaParser GetDefinitionParser() => parser;
    }
}
