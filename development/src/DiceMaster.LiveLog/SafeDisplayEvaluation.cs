using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DiceMaster.LiveLog
{
    public sealed class RandomDisplayValueException : InvalidOperationException { }

    // Read semantics audited against Steam build 26021712. Never call the
    // game's GetValue/GetResult/Check/Execute chain from a display evaluation:
    // it can consume Unity RNG and increment ConditionResultData._count.
    public static class SafeDisplayEvaluation
    {
        private sealed class Identity : IEqualityComparer<object>
        {
            public new bool Equals(object left, object right) { return ReferenceEquals(left, right); }
            public int GetHashCode(object value) { return RuntimeHelpers.GetHashCode(value); }
        }

        private sealed class Evaluation
        {
            private readonly HashSet<object> active = new HashSet<object>(new Identity());
            private static object Read(object value, string name) { return LogBridge.ReadFieldStrict(value, name); }
            private static int Number(object value, string name) { return Convert.ToInt32(Read(value, name)); }
            private static IList List(object value, string name)
            {
                IList result = Read(value, name) as IList;
                if (result == null) throw new InvalidOperationException("Invalid display collection.");
                return result;
            }

            private void Enter(object value)
            {
                if (value == null || active.Count >= 128 || !active.Add(value))
                    throw new InvalidOperationException("Invalid or cyclic display data.");
            }

            public int Group(object value)
            {
                Enter(value);
                try
                {
                    IList references = List(value, "_value");
                    List<int> numbers = new List<int>();
                    foreach (object reference in references) numbers.Add(Reference(reference));
                    int type = Number(value, "_groupType");
                    int result = type == 4 ? 1 : 0;
                    if ((type == 2 || type == 3) && numbers.Count == 0) throw new InvalidOperationException("Empty extrema.");
                    for (int i = 0; i < numbers.Count; i++)
                    {
                        int number = numbers[i];
                        switch (type)
                        {
                            case 1: result = checked(result + number); break;
                            case 2: result = i == 0 ? number : Math.Max(result, number); break;
                            case 3: result = i == 0 ? number : Math.Min(result, number); break;
                            case 4: result = unchecked(result * number); break;
                            default: throw new InvalidOperationException("Unknown display group type.");
                        }
                    }
                    if (type < 1 || type > 4) throw new InvalidOperationException("Unknown display group type.");
                    return result;
                }
                finally { active.Remove(value); }
            }

            internal int Reference(object value)
            {
                Enter(value);
                try
                {
                    int type = Number(value, "_checkType");
                    int sign = (bool)Read(value, "_negative") ? -1 : 1;
                    switch (type)
                    {
                        case 0: return unchecked(Number(value, "_constant") * sign);
                        case 1:
                            object stat = Read(value, "_stat");
                            ValidateStat(stat);
                            return unchecked(Number(stat, "FinalValue") * sign);
                        case 2: return unchecked(Number(Read(value, "_relationship"), "Value") * sign);
                        case 3: return unchecked(Number(Read(value, "_flag"), "State") * sign);
                        case 4: return Number(Read(value, "_book"), "CurrentCount");
                        case 5: return Number(Read(value, "_misc"), "CurrentCount");
                        case 6: return Number(Read(value, "_specialItem"), "CurrentCount");
                        // Strict primitive callers have no random snapshot.
                        // Description formatters use RandomPreview instead.
                        case 7: throw new RandomDisplayValueException();
                        case 8: return Group(Read(value, "_statGroup"));
                        case 9:
                            object time = Read(Read(value, "_gameTime"), "GameTime");
                            int rounds = Number(time, "_roundsPerMonth");
                            return unchecked((Number(time, "Year") - 1) * 12 * rounds +
                                (Number(time, "Month") - 1) * rounds + Number(time, "Stage"));
                        case 10: return Number(Read(value, "_talentData"), "Level");
                        case 11: return Number(Read(value, "_book"), "CurrentMartialLevel");
                        case 12: return (bool)Read(Read(value, "_upgradeItemData"), "AlreadyDevelop") ? 1 : 0;
                        case 13: return Number(Read(value, "_upgradeItemData"), "CurrentLevel");
                        case 14:
                            int count = 0;
                            foreach (object book in List(Read(value, "_bookCollection"), "List"))
                                if ((bool)Read(book, "IsMaxLevel")) count++;
                            return count;
                        case 15: return Number(Read(value, "_libraryAchieve"), "Count");
                        case 16: return ConditionData(Read(value, "_condition")) ? 1 : 0;
                        default: throw new InvalidOperationException("Unknown display value type.");
                    }
                }
                finally { active.Remove(value); }
            }

            // These concrete conversion getters are arithmetic/curve reads in
            // the supplied DLL. Validate recursive GameStat graphs before using
            // FinalValue so malformed cycles or new virtual implementations do
            // not escape into the original recursive/native getter chain.
            private void ValidateStat(object stat)
            {
                Enter(stat);
                try
                {
                    if (stat.GetType().FullName != "Mortal.Core.GameStat") throw new InvalidOperationException("Unknown display stat.");
                    object collections = Read(stat, "_statAdditionData");
                    if (collections == null) return;
                    foreach (object collection in (IEnumerable)collections)
                    {
                        if (collection == null || collection.GetType().FullName != "Mortal.Core.StatConvertCollectionData")
                            throw new InvalidOperationException("Unknown display conversion collection.");
                        foreach (object conversion in List(collection, "List"))
                        {
                            if (conversion == null) throw new InvalidOperationException("Null display conversion.");
                            string type = conversion.GetType().FullName;
                            if (type == "Mortal.Core.GameStatConvertData" || type == "Mortal.Core.GameStatRateConvertData")
                                ValidateStat(Read(conversion, "_statData"));
                            else if (type != "Mortal.Core.FlagDataRateConvertData" && type != "Mortal.Core.BookLevelStatConvertData" &&
                                     type != "Mortal.Core.UpgradeItemStatConvertData")
                                throw new InvalidOperationException("Unknown display conversion.");
                        }
                    }
                }
                finally { active.Remove(stat); }
            }

            public bool Compare(object value)
            {
                Enter(value);
                try
                {
                    int left = Group(Read(value, "_value1"));
                    int right = Group(Read(value, "_value2"));
                    switch (Number(value, "_compareOp"))
                    {
                        case 0: return left == right;
                        case 1: return left != right;
                        case 2: return left < right;
                        case 3: return left <= right;
                        case 4: return left > right;
                        case 5: return left >= right;
                        default: throw new InvalidOperationException("Unknown display comparison.");
                    }
                }
                finally { active.Remove(value); }
            }

            public bool Condition(object value)
            {
                Enter(value);
                try
                {
                    IList comparisons = List(value, "_items");
                    List<bool> results = new List<bool>();
                    foreach (object comparison in comparisons) results.Add(Compare(comparison));
                    if (results.Count == 0) return false;
                    int logic = Number(value, "_logicOp");
                    if (logic != 0 && logic != 1) throw new InvalidOperationException("Unknown display logic.");
                    bool result = results[0];
                    for (int i = 1; i < results.Count; i++) result = logic == 0 ? result & results[i] : result | results[i];
                    return result;
                }
                finally { active.Remove(value); }
            }

            public bool ConditionData(object value)
            {
                Enter(value);
                try
                {
                    foreach (object item in List(value, "_items")) if (Condition(item)) return true;
                    return false;
                }
                finally { active.Remove(value); }
            }

            public int Rate(object value)
            {
                Enter(value);
                try
                {
                    int rate = Number(value, "_defaultRate");
                    if ((bool)Read(value, "_toggleCondition") && !Condition(Read(value, "_activeCondition"))) return 0;
                    foreach (object item in List(value, "_eventRateItem"))
                        if (Condition(Read(item, "Condition"))) rate = unchecked(rate + Number(item, "Rate"));
                    return Math.Max(rate, 0);
                }
                finally { active.Remove(value); }
            }
        }

        public static int ReadGroup(object value) { return new Evaluation().Group(value); }
        public static bool CheckComparison(object value) { return new Evaluation().Compare(value); }
        public static bool CheckCondition(object value) { return new Evaluation().Condition(value); }
        public static bool CheckConditionData(object value) { return new Evaluation().ConditionData(value); }
        public static int ReadEventRate(object value) { return new Evaluation().Rate(value); }
        // Shared only with the structured preview. This retains the audited
        // known-reference semantics without calling a gameplay evaluator.
        internal static int ReadReference(object value) { return new Evaluation().Reference(value); }
    }
}
