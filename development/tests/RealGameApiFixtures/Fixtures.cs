// Explicit managed boundaries for transplanted game IL. These are not Unity or
// the game runtime; random calls and debug logging are observable test doubles.
namespace UnityEngine
{
    public class Object
    {
        public static bool operator ==(Object left, Object right) => ReferenceEquals(left, right);
        public static bool operator !=(Object left, Object right) => !ReferenceEquals(left, right);
        public override bool Equals(object value) => ReferenceEquals(this, value);
        public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
    }
    public class ScriptableObject : Object { }
    public static class Mathf { public static int Max(int a, int b) => System.Math.Max(a, b); }
    public static class Debug
    {
        public static int ErrorCount;
        public static void Log(object value) { }
        public static void LogError(object value) { ErrorCount++; }
    }
    public static class Random
    {
        public static int Calls, LastMin, LastMax, Next;
        public static int Range(int min, int max) { Calls++; LastMin = min; LastMax = max; return Next; }
    }
}
namespace Mortal.Core
{
    public class GameStat
    {
        public object[] _statAdditionData;
        public int FinalReads;
        private int final;
        public int FinalValue { get { FinalReads++; return final; } set { final = value; } }
    }
    public class StatConvertCollectionData { public System.Collections.IList List { get; set; } }
    public class GameStatConvertData { public GameStat _statData; }
    public class GameStatRateConvertData { public GameStat _statData; }
    public class UnknownStatConvertData { }
    public class UnknownStatConvertCollectionData : StatConvertCollectionData { }
    public class RelationshipStat { public int Value { get; set; } }
    public class FlagData { public int State; }
    public class ItemData { public int CurrentCount { get; set; } }
    public class Book : ItemData { public int CurrentMartialLevel { get; set; } public bool IsMaxLevel { get; set; } }
    public class Misc : ItemData { }
    public class SpecialItem : ItemData { }
    public class PlayerTalentData { public int Level { get; set; } }
    public class GameTime
    {
        public int _roundsPerMonth = 3, Year, Month, Stage;
        public int ConvertToRounds() => (Year - 1) * 12 * _roundsPerMonth + (Month - 1) * _roundsPerMonth + Stage;
    }
    public class GameTimeData { public GameTime GameTime; }
    public class UpgradeItemData { public bool AlreadyDevelop { get; set; } public int CurrentLevel { get; set; } }
    public class BookDataCollection
    {
        public System.Collections.Generic.List<Book> List { get; set; } = new System.Collections.Generic.List<Book>();
        public int GetMaxLevelCount() => List.FindAll(book => book.IsMaxLevel).Count;
    }
    public class LibraryItemData { public int Count { get; set; } }
}
namespace OBB.Framework.Data
{
    public class StoryMappingItem { public string Value { get; set; } }
}
