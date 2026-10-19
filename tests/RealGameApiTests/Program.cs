using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Mono.Cecil;

// The original reference DLLs are only read. Actual game method bodies and their
// compiler-generated closures are copied into a disposable boundary fixture.
// This executes managed IL on macOS; it cannot establish Unity/game compatibility.
static partial class RealGameApiTests
{
    static Assembly fixture;
    static int assertions, metadataAssertions;
    static readonly string[] CoreTypes = {
        "Mortal.Core.StatGroupVariable", "Mortal.Core.StatCompareItem", "Mortal.Core.StatValueReference",
        "Mortal.Core.ConditionResultItem", "Mortal.Core.ConditionResultData", "Mortal.Core.GameStatUtils",
        "Mortal.Core.StatGroupType", "Mortal.Core.StatCheckType", "Mortal.Core.StatCompareType", "Mortal.Core.LogicOperatorType"
    };
    static readonly string[] StoryTypes = { "Mortal.Story.PositionEventData", "Mortal.Story.PositionEventRateItem" };

    static int Main(string[] args)
    {
        if (args.Length is < 3 or > 4) throw new ArgumentException("Usage: RealGameApiTests ManagedDirectory Fixtures.dll OutputDirectory [CandidateDiceMaster.dll]");
        string root = Path.GetFullPath(args[0]);
        string output = Path.GetFullPath(args[2]);
        Directory.CreateDirectory(output);
        using var core = AssemblyDefinition.ReadAssembly(Path.Combine(root, "Mortal.Core.dll"));
        using var story = AssemblyDefinition.ReadAssembly(Path.Combine(root, "Mortal.Story.dll"));
        using var destination = AssemblyDefinition.ReadAssembly(args[1]);
        foreach (var source in new[] { core, story })
        {
            string file = Path.Combine(root, source.Name.Name + ".dll");
            Console.WriteLine($"REFERENCE {source.Name.FullName} SHA256={Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)))}");
            var selected = source == core ? CoreTypes : StoryTypes;
            foreach (var name in selected)
            {
                var type = source.MainModule.GetType(name) ?? throw new InvalidDataException("Missing reference type " + name);
                foreach (var method in type.Methods.Where(m => m.Name is "GetRate" or "GetResult" or "Check" or "GetValue" or "Execute"))
                    Console.WriteLine($"ACTUAL IL {method.FullName} token={method.MetadataToken}");
            }
            new PayloadCopier(source.MainModule, destination.MainModule, type => selected.Contains(type.FullName));
        }
        if (args.Length == 4)
        {
            using var candidate = AssemblyDefinition.ReadAssembly(args[3]);
            Console.WriteLine($"CANDIDATE {candidate.Name.FullName} SHA256={Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[3])))}");
            if (candidate.Name.Name == "DiceMaster")
            {
                int before = assertions;
                VerifyMetadataContract(candidate, root);
                metadataAssertions += assertions - before;
            }
            new PayloadCopier(candidate.MainModule, destination.MainModule, type => (type.Namespace == "DiceMaster.LiveLog" || type.Namespace == "DiceMaster.EventSelection" && new[] { "EventEligibility", "EventEligibilitySnapshot", "EventEligibilityState", "EventEligibilityReason" }.Contains(type.Name) || new[] { "LogReadState", "LogRefreshState", "LogConditionBranch", "LogConditionDetails", "LogItem", "LogSnapshot", "ReservationTicket", "LogTabState", "LogModel" }.Contains(type.Name) && type.Namespace == "DiceMaster.LogUI"));
            if (destination.MainModule.GetType("DiceMaster.LiveLog.SafeDisplayEvaluation") == null)
                throw new InvalidDataException("Candidate does not contain SafeDisplayEvaluation.");
        }
        // Attributes belong to Unity/editor packages and have no part in this
        // managed method experiment. Remove their metadata, retaining IL bodies.
        foreach (var type in AllTypes(destination.MainModule.Types))
        {
            type.CustomAttributes.Clear();
            foreach (var member in type.Fields) member.CustomAttributes.Clear();
            foreach (var member in type.Methods) member.CustomAttributes.Clear();
            foreach (var member in type.Properties) member.CustomAttributes.Clear();
        }
        string patched = Path.Combine(output, "RealGameApiFixtures.patched.dll");
        if (string.Equals(patched, Path.GetFullPath(args[1]), StringComparison.Ordinal)) throw new ArgumentException("Output must differ from input fixture.");
        destination.Write(patched);
        fixture = Assembly.LoadFile(patched);
        TestGroups();
        TestComparisonsAndConditions();
        TestRates();
        TestSideEffects();
        if (args.Length == 4)
        {
            TestSafeEvaluation();
            TestEventEligibility();
            if (fixture.GetType("DiceMaster.LiveLog.RandomPreview") != null) TestRandomPreview();
        }
        Console.WriteLine($"PASS: {assertions - metadataAssertions} managed behavior assertions and {metadataAssertions} metadata contract assertions; Unity/random/data boundaries are managed fixtures, native game runtime NOT executed.");
        return 0;
    }

    static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            yield return type;
            foreach (var nested in AllTypes(type.NestedTypes)) yield return nested;
        }
    }
    static Type T(string name) => fixture.GetType(name, true);
    static object New(string name) => Activator.CreateInstance(T(name));
    static FieldInfo Field(object obj, string name) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(obj.GetType().FullName, name);
    static void Set(object obj, string name, object value) => Field(obj, name).SetValue(obj, value);
    static void PropertySet(object obj, string name, object value) => obj.GetType().GetProperty(name).SetValue(obj, value);
    static object Get(object obj, string name) => Field(obj, name).GetValue(obj);
    static object EnumValue(string name, string value) => Enum.Parse(T("Mortal.Core." + name), value);
    static void StaticSet(string type, string field, object value) => T(type).GetField(field).SetValue(null, value);
    static object StaticGet(string type, string field) => T(type).GetField(field).GetValue(null);
    static object Call(object obj, string name)
    {
        try { return obj.GetType().GetMethod(name).Invoke(obj, null); }
        catch (TargetInvocationException error) { throw error.InnerException ?? error; }
    }
    static object Safe(string name, object value)
    {
        try { return T("DiceMaster.LiveLog.SafeDisplayEvaluation").GetMethod(name).Invoke(null, new[] { value }); }
        catch (TargetInvocationException error) { throw error.InnerException ?? error; }
    }
    static void Eq(object expected, object actual, string reason)
    {
        assertions++;
        if (!Equals(expected, actual)) throw new Exception(reason + $": expected {expected}, got {actual}");
    }
    static void Throws<TError>(Action call, string reason) where TError : Exception
    {
        assertions++;
        try { call(); }
        catch (TError) { return; }
        throw new Exception(reason + ": expected " + typeof(TError).Name);
    }
    static IList List(string type, params object[] values)
    {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(T("Mortal.Core." + type)));
        foreach (var value in values) list.Add(value);
        return list;
    }
    static object Value(string kind, int value = 0)
    {
        object reference = New("Mortal.Core.StatValueReference");
        Set(reference, "_checkType", EnumValue("StatCheckType", kind));
        Set(reference, "_constant", value);
        return reference;
    }
    static object Group(string kind, params object[] values)
    {
        object group = New("Mortal.Core.StatGroupVariable");
        Set(group, "_groupType", EnumValue("StatGroupType", kind));
        Set(group, "_value", List("StatValueReference", values));
        return group;
    }
    static object Constant(int value) => Group("Sum", Value("常數", value));
    static object Comparison(object left, object right, string op = "Equal")
    {
        object comparison = New("Mortal.Core.StatCompareItem");
        Set(comparison, "_value1", left); Set(comparison, "_value2", right);
        Set(comparison, "_compareOp", EnumValue("StatCompareType", op));
        return comparison;
    }
    static object Condition(string kind, params object[] comparisons)
    {
        object condition = New("Mortal.Core.ConditionResultItem");
        Set(condition, "_logicOp", EnumValue("LogicOperatorType", kind));
        Set(condition, "_items", List("StatCompareItem", comparisons));
        return condition;
    }
    static object True() => Condition("AND", Comparison(Constant(1), Constant(1)));
    static object False() => Condition("AND", Comparison(Constant(1), Constant(2)));
    static object RandomReference(int min = 3, int max = 8)
    {
        object value = Value("隨機值");
        Set(value, "_randomMin", min); Set(value, "_randomMax", max);
        return value;
    }
    static void ResetRandom(int next = 5)
    {
        StaticSet("UnityEngine.Random", "Calls", 0); StaticSet("UnityEngine.Random", "Next", next);
    }

    static void TestGroups()
    {
        Eq(7, Call(Group("Sum", Value("常數", 3), Value("常數", 4)), "GetResult"), "sum");
        Eq(4, Call(Group("Max", Value("常數", 3), Value("常數", 4)), "GetResult"), "max");
        Eq(3, Call(Group("Min", Value("常數", 3), Value("常數", 4)), "GetResult"), "min");
        Eq(12, Call(Group("Multiply", Value("常數", 3), Value("常數", 4)), "GetResult"), "multiply");
        Throws<OverflowException>(() => Call(Group("Sum", Value("常數", int.MaxValue), Value("常數", 1)), "GetResult"), "sum overflow fails");
        Eq(-2, Call(Group("Multiply", Value("常數", int.MaxValue), Value("常數", 2)), "GetResult"), "multiply overflow wraps");
        Eq(0, Call(Group("Sum"), "GetResult"), "empty sum is zero");
        Eq(1, Call(Group("Multiply"), "GetResult"), "empty product is one");
        Throws<InvalidOperationException>(() => Call(Group("Min"), "GetResult"), "empty min fails");
        Throws<InvalidOperationException>(() => Call(Group("Max"), "GetResult"), "empty max fails");
        object bad = Group("Sum"); Set(bad, "_value", null);
        Throws<NullReferenceException>(() => Call(bad, "GetResult"), "null list differs from empty list");
        Throws<NullReferenceException>(() => Call(Group("Sum", new object[] { null }), "GetResult"), "null value reference fails");
        object invalid = Group("Sum", Value("常數", 9));
        Set(invalid, "_groupType", Enum.ToObject(T("Mortal.Core.StatGroupType"), 999));
        Eq(0, Call(invalid, "GetResult"), "unknown group operation returns zero after reading values");
    }

    static void TestComparisonsAndConditions()
    {
        foreach (var test in new[] { ("Equal", 2, 2, true), ("NotEqual", 2, 3, true), ("Less", 2, 3, true),
            ("LessEqual", 2, 2, true), ("Greater", 3, 2, true), ("GreaterEqual", 2, 2, true), ("Equal", 2, 3, false) })
            Eq(test.Item4, Call(Comparison(Constant(test.Item2), Constant(test.Item3), test.Item1), "Check"), test.Item1);
        StaticSet("UnityEngine.Debug", "ErrorCount", 0);
        Throws<NullReferenceException>(() => Call(Comparison(null, Constant(1)), "Check"), "missing group logs then throws");
        Eq(1, StaticGet("UnityEngine.Debug", "ErrorCount"), "missing left group error count");
        Throws<NullReferenceException>(() => Call(Comparison(Constant(1), null), "Check"), "missing right group fails");
        Eq(2, StaticGet("UnityEngine.Debug", "ErrorCount"), "missing right group error count");
        Eq(false, Call(Condition("AND"), "Check"), "empty AND condition is false");
        Eq(false, Call(Condition("OR"), "Check"), "empty OR condition is false");
        object bad = Condition("AND"); Set(bad, "_items", null);
        Throws<ArgumentNullException>(() => Call(bad, "Check"), "null comparison list fails");
        Throws<NullReferenceException>(() => Call(Condition("AND", new object[] { null }), "Check"), "null comparison entry fails");
        Eq(true, Call(True(), "Check"), "nonempty true condition");
        Eq(false, Call(False(), "Check"), "nonempty false condition");
    }

    static object Rate(int rate, bool enabled, object condition, params object[] additions)
    {
        object data = New("Mortal.Story.PositionEventData");
        Set(data, "_defaultRate", rate); Set(data, "_toggleCondition", enabled); Set(data, "_activeCondition", condition);
        var array = Array.CreateInstance(T("Mortal.Story.PositionEventRateItem"), additions.Length);
        for (int i = 0; i < additions.Length; i++) array.SetValue(additions[i], i);
        Set(data, "_eventRateItem", array);
        return data;
    }
    static object RateItem(int rate, object condition)
    {
        object item = New("Mortal.Story.PositionEventRateItem");
        Set(item, "_rate", rate); Set(item, "_condition", condition); return item;
    }
    static void TestRates()
    {
        Eq(45, Call(Rate(45, false, null), "GetRate"), "disabled active condition tolerates null");
        Eq(0, Call(Rate(45, true, False()), "GetRate"), "false active condition blocks");
        Eq(0, Call(Rate(45, true, Condition("AND")), "GetRate"), "empty active condition blocks");
        Eq(45, Call(Rate(45, true, True()), "GetRate"), "true active condition permits");
        Eq(140, Call(Rate(90, false, null, RateItem(50, True())), "GetRate"), "rate is not clamped at 100");
        Eq(0, Call(Rate(10, false, null, RateItem(-50, True())), "GetRate"), "negative rate clamps to zero");
        Eq(45, Call(Rate(45, false, null, RateItem(50, False())), "GetRate"), "false addition skipped");
        Throws<NullReferenceException>(() => Call(Rate(45, true, null), "GetRate"), "enabled missing active condition fails");
        Throws<NullReferenceException>(() => Call(Rate(45, false, null, RateItem(10, null)), "GetRate"), "missing addition condition fails");
        Throws<NullReferenceException>(() => Call(Rate(45, false, null, new object[] { null }), "GetRate"), "null addition entry fails");
        object missing = Rate(45, false, null); Set(missing, "_eventRateItem", null);
        Throws<NullReferenceException>(() => Call(missing, "GetRate"), "null additions array fails");
        Set(missing, "_toggleCondition", true); Set(missing, "_activeCondition", False());
        Eq(0, Call(missing, "GetRate"), "false active condition returns before reading null additions");
    }

    static void TestSideEffects()
    {
        ResetRandom();
        object random = RandomReference();
        Set(random, "_negative", true);
        Eq(5, Call(random, "GetValue"), "negative flag is ignored for random");
        Eq(1, StaticGet("UnityEngine.Random", "Calls"), "random branch consumes Unity RNG boundary");
        Eq(3, StaticGet("UnityEngine.Random", "LastMin"), "random lower bound");
        Eq(9, StaticGet("UnityEngine.Random", "LastMax"), "game converts inclusive upper bound");
        object constant = Value("常數", 5); Set(constant, "_negative", true);
        Eq(-5, Call(constant, "GetValue"), "constant negative flag applies");
        object nested = Value("數值群組"); Set(nested, "_statGroup", Group("Sum", random)); Set(nested, "_negative", true);
        Eq(5, Call(nested, "GetValue"), "nested group ignores outer negative");
        Eq(2, StaticGet("UnityEngine.Random", "Calls"), "nested group consumes RNG");
        ResetRandom();
        object randomComparison = Comparison(Group("Sum", RandomReference()), Constant(5));
        Eq(false, Call(Condition("AND", Comparison(Constant(1), Constant(2)), randomComparison), "Check"), "AND result false");
        Eq(1, StaticGet("UnityEngine.Random", "Calls"), "AND evaluates random comparison after false comparison");
        Eq(true, Call(Condition("OR", Comparison(Constant(1), Constant(1)), randomComparison), "Check"), "OR result true");
        Eq(2, StaticGet("UnityEngine.Random", "Calls"), "OR evaluates random comparison after true comparison");
        object data = New("Mortal.Core.ConditionResultData");
        Set(data, "_items", List("ConditionResultItem", True(), Condition("AND", randomComparison)));
        object conditional = Value("條件設定"); Set(conditional, "_condition", data);
        Eq(1, Call(conditional, "GetValue"), "condition reference executes result data");
        Eq(1, Get(data, "_count"), "condition execution mutates persistent counter");
        Eq(2, StaticGet("UnityEngine.Random", "Calls"), "condition result stops at first true item");
        Eq(1, Call(conditional, "GetValue"), "repeated display-style evaluation repeats execution");
        Eq(2, Get(data, "_count"), "repeated evaluation increments counter again");
        Set(data, "_items", List("ConditionResultItem"));
        Eq(false, Call(data, "Execute"), "empty condition result is false");
        Eq(3, Get(data, "_count"), "empty condition execution still mutates counter");
        Set(data, "_items", null);
        Throws<NullReferenceException>(() => Call(data, "Execute"), "null condition result list fails");
        Eq(4, Get(data, "_count"), "failed execution already incremented counter");
        ResetRandom();
        Eq(40, Call(Rate(40, true, Condition("AND", randomComparison)), "GetRate"), "GetRate accepts RNG-backed condition");
        Eq(1, StaticGet("UnityEngine.Random", "Calls"), "GetRate can consume RNG transitively");
    }

    static void TestEventEligibility()
    {
        object Evaluate(object data) => T("DiceMaster.EventSelection.EventEligibility").GetMethod("Evaluate").Invoke(null, new[] { data });
        void State(object data, string state, string reason, bool force)
        {
            object snapshot = Evaluate(data);
            Eq(state, Get(snapshot, "State").ToString(), "event eligibility state from audited game data");
            Eq(reason, Get(snapshot, "Reason").ToString(), "event eligibility structured cause");
            Eq(force, Get(snapshot, "CanUseResult"), "event eligibility explicit-force readability boundary");
        }
        ResetRandom();
        State(Rate(10, false, null), "Eligible", "None", true);
        State(Rate(0, false, null), "Ineligible", "ZeroWeight", true);
        State(Rate(-4, false, null), "Ineligible", "ZeroWeight", true);
        State(Rate(10, true, False()), "Ineligible", "ConditionsNotMet", true);
        object randomCondition = Condition("AND", Comparison(Group("Sum", RandomReference()), Constant(5)));
        State(Rate(10, true, randomCondition), "Unknown", "Random", true);
        State(Rate(10, false, null, RateItem(2, randomCondition)), "Unknown", "Random", true);
        State(Rate(10, true, null), "Unknown", "ReadFailure", false);
        object malformed = Rate(10, false, null); Set(malformed, "_eventRateItem", null);
        State(malformed, "Unknown", "ReadFailure", false);
        State(null, "Unknown", "ReadFailure", false);
        object counted = New("Mortal.Core.ConditionResultData");
        Set(counted, "_items", List("ConditionResultItem", True())); Set(counted, "_count", 37);
        object backed = Value("條件設定"); Set(backed, "_condition", counted);
        State(Rate(10, true, Condition("AND", Comparison(Group("Sum", backed), Constant(1)))), "Eligible", "None", true);
        Eq(37, Get(counted, "_count"), "eligibility never consumes original condition counters");
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "eligibility never consumes original game RNG");
    }

    static void TestSafeEvaluation()
    {
        TestSafeDataBoundaries();
        foreach (string operation in new[] { "Sum", "Multiply", "Min", "Max" })
        {
            object group = Group(operation, Value("常數", 3), Value("常數", -4), Value("常數", 2));
            Eq(Call(group, "GetResult"), Safe("ReadGroup", group), "safe group matches original " + operation);
        }
        Eq(0, Safe("ReadGroup", Group("Sum")), "safe empty sum");
        Eq(1, Safe("ReadGroup", Group("Multiply")), "safe empty multiply");
        Throws<InvalidOperationException>(() => Safe("ReadGroup", Group("Max")), "safe empty max reports failure");
        Throws<InvalidOperationException>(() => Safe("ReadGroup", Group("Min")), "safe empty min reports failure");
        Throws<OverflowException>(() => Safe("ReadGroup", Group("Sum", Value("常數", int.MaxValue), Value("常數", 1))), "safe sum preserves overflow failure");
        Eq(-2, Safe("ReadGroup", Group("Multiply", Value("常數", int.MaxValue), Value("常數", 2))), "safe multiplication preserves unchecked overflow");
        foreach (string operation in new[] { "Equal", "NotEqual", "Less", "LessEqual", "Greater", "GreaterEqual" })
        {
            object comparison = Comparison(Constant(3), Constant(4), operation);
            Eq(Call(comparison, "Check"), Safe("CheckComparison", comparison), "safe comparison matches " + operation);
        }
        foreach (string operation in new[] { "AND", "OR" })
        {
            foreach (var values in new[] { new object[0], new[] { Comparison(Constant(2), Constant(3)), Comparison(Constant(2), Constant(2)) } })
            {
                object condition = Condition(operation, values);
                Eq(Call(condition, "Check"), Safe("CheckCondition", condition), "safe condition matches empty/eager " + operation);
            }
        }
        foreach (object rate in new[] { Rate(150, false, null), Rate(-5, false, null), Rate(45, true, False()),
            Rate(45, true, True(), RateItem(100, True()), RateItem(-10, False())), Rate(45, true, Condition("AND")) })
            Eq(Call(rate, "GetRate"), Safe("ReadEventRate", rate), "safe event rate matches original");

        object nested = Value("數值群組"); Set(nested, "_statGroup", Constant(7)); Set(nested, "_negative", true);
        object negative = Value("常數", 6); Set(negative, "_negative", true);
        object nestedGroup = Group("Sum", nested, negative);
        Eq(Call(nestedGroup, "GetResult"), Safe("ReadGroup", nestedGroup), "safe nested group/negative semantics match original");

        object data = New("Mortal.Core.ConditionResultData");
        Set(data, "_items", List("ConditionResultItem", False(), True()));
        Set(data, "_count", 17);
        object conditional = Value("條件設定"); Set(conditional, "_condition", data);
        Eq(1, Safe("ReadGroup", Group("Sum", conditional)), "safe condition-backed value can be read");
        Eq(17, Get(data, "_count"), "safe value preserves condition execution counter");
        Eq(true, Safe("CheckConditionData", data), "safe condition data evaluates OR");
        Eq(17, Get(data, "_count"), "safe direct condition evaluation preserves counter");
        Set(data, "_items", List("ConditionResultItem"));
        Eq(false, Safe("CheckConditionData", data), "safe empty condition result matches original false");
        Eq(17, Get(data, "_count"), "safe empty result preserves counter");
        Set(data, "_items", null);
        Throws<InvalidOperationException>(() => Safe("CheckConditionData", data), "safe malformed result list fails");
        Eq(17, Get(data, "_count"), "safe failed result preserves counter");

        ResetRandom();
        object random = Group("Sum", RandomReference());
        Throws<InvalidOperationException>(() => Safe("ReadGroup", random), "safe random value is unavailable");
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "safe random group never consumes RNG");
        object randomComparison = Comparison(random, Constant(5));
        Throws<InvalidOperationException>(() => Safe("CheckCondition", Condition("AND", Comparison(Constant(1), Constant(2)), randomComparison)),
            "safe eager AND preserves random-unavailable failure");
        Throws<InvalidOperationException>(() => Safe("CheckCondition", Condition("OR", Comparison(Constant(1), Constant(1)), randomComparison)),
            "safe eager OR preserves random-unavailable failure");
        Throws<InvalidOperationException>(() => Safe("ReadEventRate", Rate(40, true, Condition("AND", randomComparison))), "safe event refuses nested RNG");
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "safe condition/event checks never consume RNG");
        Eq(40, Safe("ReadEventRate", Rate(40, false, Condition("AND", randomComparison))), "disabled active condition skips random condition");
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "disabled active condition never consumes RNG");
        object blockedRate = Rate(40, true, False()); Set(blockedRate, "_eventRateItem", null);
        Eq(0, Safe("ReadEventRate", blockedRate), "false active condition skips unavailable rate additions");
        Set(data, "_items", List("ConditionResultItem", True(), Condition("AND", randomComparison)));
        Eq(true, Safe("CheckConditionData", data), "safe result data short circuits before later random condition");
        Eq(17, Get(data, "_count"), "safe short circuit preserves count");
        Eq(0, StaticGet("UnityEngine.Random", "Calls"), "safe short circuit avoids later RNG");

        object cycleReference = Value("數值群組");
        object cycle = Group("Sum", cycleReference); Set(cycleReference, "_statGroup", cycle);
        Throws<InvalidOperationException>(() => Safe("ReadGroup", cycle), "recursive value group cycle is reported");
        object conditionCycle = New("Mortal.Core.ConditionResultData");
        object cycleValue = Value("條件設定"); Set(cycleValue, "_condition", conditionCycle);
        Set(conditionCycle, "_items", List("ConditionResultItem", Condition("AND", Comparison(Group("Sum", cycleValue), Constant(1)))));
        Throws<InvalidOperationException>(() => Safe("CheckConditionData", conditionCycle), "recursive condition cycle is reported");
        Eq(0, Get(conditionCycle, "_count"), "failed safe cyclic condition does not mutate counter");
        Throws<InvalidOperationException>(() => Safe("ReadGroup", null), "missing safe group reports unavailable");
        Throws<InvalidOperationException>(() => Safe("CheckCondition", null), "missing safe condition reports unavailable");
        Throws<InvalidOperationException>(() => Safe("ReadEventRate", Rate(45, true, null)), "missing enabled condition reports unavailable");
        Throws<InvalidOperationException>(() => Safe("ReadEventRate", Rate(45, false, null, RateItem(10, null))), "missing rate condition reports unavailable");
        TestSafeStatBoundaries();
    }

    static void TestSafeDataBoundaries()
    {
        // These leaf values are managed data fixtures. The StatValueReference
        // dispatch/negative semantics on the original side remain supplied IL.
        foreach (var test in new[] {
            ("角色數值", "_stat", "GameStat", "FinalValue", (object)12, -12),
            ("好感度", "_relationship", "RelationshipStat", "Value", (object)13, -13),
            ("書籍", "_book", "Book", "CurrentCount", (object)14, 14),
            ("雜物", "_misc", "Misc", "CurrentCount", (object)15, 15),
            ("貴重品", "_specialItem", "SpecialItem", "CurrentCount", (object)16, 16),
            ("技能", "_talentData", "PlayerTalentData", "Level", (object)17, 17),
            ("秘笈等級", "_book", "Book", "CurrentMartialLevel", (object)18, 18),
            ("已開發項目", "_upgradeItemData", "UpgradeItemData", "AlreadyDevelop", (object)true, 1),
            ("開發項目等級", "_upgradeItemData", "UpgradeItemData", "CurrentLevel", (object)19, 19),
            ("風雲史", "_libraryAchieve", "LibraryItemData", "Count", (object)20, 20)
        })
        {
            object data = New("Mortal.Core." + test.Item3); PropertySet(data, test.Item4, test.Item5);
            object reference = Value(test.Item1); Set(reference, test.Item2, data); Set(reference, "_negative", true);
            object group = Group("Sum", reference);
            Eq(test.Item6, Call(group, "GetResult"), "actual value dispatch " + test.Item1);
            Eq(test.Item6, Safe("ReadGroup", group), "safe value dispatch " + test.Item1);
        }
        object flag = New("Mortal.Core.FlagData"); Set(flag, "State", 21);
        object flagReference = Value("旗標"); Set(flagReference, "_flag", flag); Set(flagReference, "_negative", true);
        Eq(-21, Call(Group("Sum", flagReference), "GetResult"), "actual flag sign");
        Eq(-21, Safe("ReadGroup", Group("Sum", flagReference)), "safe flag sign");
        object time = New("Mortal.Core.GameTime"); Set(time, "Year", 2); Set(time, "Month", 5); Set(time, "Stage", 2);
        object timeData = New("Mortal.Core.GameTimeData"); Set(timeData, "GameTime", time);
        object timeReference = Value("遊戲時間"); Set(timeReference, "_gameTime", timeData); Set(timeReference, "_negative", true);
        Eq(Call(Group("Sum", timeReference), "GetResult"), Safe("ReadGroup", Group("Sum", timeReference)), "safe time formula matches boundary rounds");
        object books = New("Mortal.Core.BookDataCollection");
        var list = List("Book", New("Mortal.Core.Book"), New("Mortal.Core.Book"), New("Mortal.Core.Book"));
        PropertySet(list[0], "IsMaxLevel", true); PropertySet(list[2], "IsMaxLevel", true); PropertySet(books, "List", list);
        object bookReference = Value("書籍精通"); Set(bookReference, "_bookCollection", books); Set(bookReference, "_negative", true);
        Eq(2, Call(Group("Sum", bookReference), "GetResult"), "actual book collection dispatch");
        Eq(2, Safe("ReadGroup", Group("Sum", bookReference)), "safe book collection count");
    }

    static void TestSafeStatBoundaries()
    {
        object stat = New("Mortal.Core.GameStat"); PropertySet(stat, "FinalValue", 9);
        object reference = Value("角色數值"); Set(reference, "_stat", stat);
        object group = Group("Sum", reference);
        object collection = New("Mortal.Core.StatConvertCollectionData");
        object conversion = New("Mortal.Core.GameStatConvertData"); Set(conversion, "_statData", stat);
        PropertySet(collection, "List", new ArrayList { conversion }); Set(stat, "_statAdditionData", new[] { collection });
        Throws<InvalidOperationException>(() => Safe("ReadGroup", group), "recursive game stat conversion cycle is reported");
        Eq(0, Get(stat, "FinalReads"), "recursive stat is rejected before FinalValue getter");
        PropertySet(collection, "List", new ArrayList { New("Mortal.Core.UnknownStatConvertData") });
        Throws<InvalidOperationException>(() => Safe("ReadGroup", group), "unknown conversion implementation is rejected");
        Eq(0, Get(stat, "FinalReads"), "unknown conversion is rejected before FinalValue getter");
        PropertySet(collection, "List", new ArrayList { null });
        Throws<InvalidOperationException>(() => Safe("ReadGroup", group), "null conversion is rejected");
        Eq(0, Get(stat, "FinalReads"), "null conversion is rejected before FinalValue getter");
        object unknownCollection = New("Mortal.Core.UnknownStatConvertCollectionData");
        PropertySet(unknownCollection, "List", new ArrayList()); Set(stat, "_statAdditionData", new[] { unknownCollection });
        Throws<InvalidOperationException>(() => Safe("ReadGroup", group), "unknown collection implementation is rejected");
        Eq(0, Get(stat, "FinalReads"), "unknown collection is rejected before FinalValue getter");
        PropertySet(collection, "List", new ArrayList()); Set(stat, "_statAdditionData", new[] { collection });
        Eq(9, Safe("ReadGroup", group), "known empty stat conversion collection is permitted");
        Eq(1, Get(stat, "FinalReads"), "validated stat getter executes once");
    }
}
