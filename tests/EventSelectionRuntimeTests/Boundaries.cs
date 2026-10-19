using System;
using System.Collections.Generic;

namespace UnityEngine { public static class Application { public static bool isFocused = true; } }
namespace Mortal.Core {
    public sealed class ConditionResultItem { public bool Value, Random, Broken; public int DisplayReads, GameplayReads; public Func<bool> Actual; }
    public sealed class ValueCompareItem { public int _compareOp, _value; }
}
namespace DiceMaster.LiveLog
{
    public sealed class RandomDisplayValueException : InvalidOperationException { }
    public sealed class UnsupportedDisplayStatException : InvalidOperationException { }
    public static class SafeDisplayEvaluation
    {
        public static bool CheckCondition(object value)
        {
            var condition = (Mortal.Core.ConditionResultItem)value;
            condition.DisplayReads++;
            if (condition.Broken) throw new InvalidOperationException("bad condition");
            if (condition.Random) throw new RandomDisplayValueException();
            return condition.Value;
        }
    }
}
namespace Mortal.Story
{
    public sealed class SwitchResultData
    {
        public string name;
        public bool _useCount;
        public int _count, OriginalCalls;
        public List<Mortal.Core.ConditionResultItem> _items = new List<Mortal.Core.ConditionResultItem>();
        public List<Mortal.Core.ValueCompareItem> _countItems = new List<Mortal.Core.ValueCompareItem>();
        public int Execute()
        {
            int result;
            if (DiceMaster.EventSelection.EventSelectionRuntime.TryDirectSwitchResult(this, out result)) return result;
            OriginalCalls++; _count++;
            if (_useCount)
            {
                for (int index = 0; index < _countItems.Count; index++)
                { var item = _countItems[index]; bool pass = item._compareOp switch { 0 => _count == item._value, 1 => _count != item._value, 2 => _count < item._value, 3 => _count <= item._value, 4 => _count > item._value, 5 => _count >= item._value, _ => throw new Exception("op") }; if (pass) return index + 1; }
                return _countItems.Count + 1;
            }
            for (int index = 0; index < _items.Count; index++)
            { var item = _items[index]; item.GameplayReads++; if (item.Actual == null ? item.Value : item.Actual()) return index + 1; }
            return _items.Count + 1;
        }
    }
    public sealed class SwitchResultConfig
    {
        public Dictionary<string, SwitchResultData> Items = new Dictionary<string, SwitchResultData>();
        public SwitchResultData GetData(string name) { Items.TryGetValue(name, out var result); return result; }
    }
    public sealed class PositionEventData
    {
        public string name, ResultType;
        public bool Eligible = true, Unknown, Broken;
        public int Rate = 10, RealCalls, DisplayCalls;
        public Func<int> ActualRate;
        public int GetRate() { RealCalls++; return ActualRate == null ? Rate : ActualRate(); }
    }
    public sealed class PositionResultData
    {
        public string name;
        public string _scriptPrefix;
        public List<PositionEventData> List = new List<PositionEventData>();
        public string GetResult() => "original";
    }
    public sealed class PositionResultConfig { public List<PositionResultData> List = new List<PositionResultData>(); }
    public sealed class CheckPointManager
    {
        public static CheckPointManager Instance;
        public PositionResultConfig _position;
        public SwitchResultConfig[] _switchList;
        public int SwitchCalls;
        public int Switch(string name)
        {
            SwitchCalls++;
            var handle = DiceMaster.EventSelection.EventSelectionRuntime.BeginSwitch(this, name);
            try
            {
                foreach (var config in _switchList) { var pool = config.GetData(name); if (pool != null) return pool.Execute(); }
                return 0;
            }
            finally { DiceMaster.EventSelection.EventSelectionRuntime.CompletePosition(handle, null); }
        }
        public int OriginalCalls, PositionCalls;
        public string Position(string name)
        {
            PositionCalls++;
            var handle = DiceMaster.EventSelection.EventSelectionRuntime.BeginPosition(this, name);
            string result;
            var pool = _position.List.Find(item => item.name == name);
            if (!DiceMaster.EventSelection.EventSelectionRuntime.TryDirectResult(pool, out result))
            { OriginalCalls++; result = "original"; }
            DiceMaster.EventSelection.EventSelectionRuntime.CompletePosition(handle, null);
            return result;
        }
    }
}
namespace DiceMaster
{
    public static class DataHelper
    { public static string ExtractConditions(Mortal.Core.ConditionResultItem item, out bool period) { period = false; return item.Random ? "A10005 = 0 | 랜덤값0_30 <= 10" : "fixture condition"; } }
    public sealed class DicePlugin
    {
        internal Gamepad.LogInputMode eventInputMode = Gamepad.LogInputMode.Unknown;
        internal bool eventInputModeKnown = false;
        internal readonly LogUI.EventPopupRenderer eventPopup = new LogUI.EventPopupRenderer();
        public static void LogInfo(string value) { }
        internal void RunModeChange(Gamepad.LogInputMode mode) => ObserveEventInputMode(mode);
        internal void RunReset(LogUI.ModalInputResetReason reason) => ResetEventInput(reason);
        internal void RunPadReset(Gamepad.LogPadFrame frame) => ResetEventPadInput(frame);
        private void ObserveEventInputMode(Gamepad.LogInputMode mode)
            => throw new InvalidOperationException("Actual candidate input-mode observer IL was not installed.");
        private void ResetEventInput(LogUI.ModalInputResetReason reason)
            => throw new InvalidOperationException("Actual candidate reset policy application IL was not installed.");
        private void ResetEventPadInput(Gamepad.LogPadFrame frame)
            => throw new InvalidOperationException("Actual candidate pad reset boundary IL was not installed.");
    }
    public sealed class PositionEventCache
    {
        public static readonly PositionEventCache Instance = new PositionEventCache();
        public string GetPositionEventName(Mortal.Story.PositionEventData value) => value.name;
    }
}
namespace DiceMaster.EventSelection
{
    // Result-text preparation is a boundary here; actual reader/parser IL is
    // executed separately by PreviewIntegrationTests.
    public sealed partial class EventResultPreview
    {
        public readonly string Summary, Detail;
        public string RichSummary => Summary;
        public string RichDetail => Detail;
        public readonly bool Retry;
        public static readonly EventResultPreview Pending = new EventResultPreview("pending", true);
        public static readonly EventResultPreview Unavailable = new EventResultPreview("unavailable", false);
        public static readonly List<string> Reads = new List<string>();
        public static Func<string, EventResultPreview> Reader;
        public EventResultPreview(string text, bool retry = false) { Summary = Detail = text; Retry = retry; }
        internal static EventResultPreview ReadSwitchBody(string sourceName, string body) => new EventResultPreview("switch:" + body);
        internal static EventResultPreview Read(string result)
        { Reads.Add(result); return Reader == null ? new EventResultPreview("preview:" + result) : Reader(result); }
    }
    public enum EventEligibilityState { Unknown, Eligible, Ineligible }
    public enum EventEligibilityReason { None, ConditionsNotMet, ZeroWeight, Random, Unsupported, ReadFailure }
    public sealed class EventEligibilitySnapshot
    {
        public bool IsEligible, CanUseResult;
        public EventEligibilityReason Reason;
        public EventEligibilityState State;
        public EventEligibilitySnapshot() { }
        public EventEligibilitySnapshot(EventEligibilityState state, EventEligibilityReason reason, int? rate, string summary, bool canUseResult = true)
        { State = state; IsEligible = state == EventEligibilityState.Eligible; Reason = reason; Summary = summary; CanUseResult = canUseResult; }
        public string StatusText = "state", Summary = "summary";
        public static EventEligibilitySnapshot Unreadable(string summary = "unreadable")
            => new EventEligibilitySnapshot { CanUseResult = false, Reason = EventEligibilityReason.ReadFailure, Summary = summary };
    }
    public static class EventEligibility
    {
        public static EventEligibilitySnapshot Evaluate(object value)
        {
            var target = (Mortal.Story.PositionEventData)value;
            target.DisplayCalls++;
            return new EventEligibilitySnapshot { IsEligible = target.Eligible && !target.Unknown && !target.Broken && target.Rate > 0,
                CanUseResult = !target.Broken, Reason = target.Broken ? EventEligibilityReason.ReadFailure :
                target.Unknown ? EventEligibilityReason.Random : !target.Eligible ? EventEligibilityReason.ConditionsNotMet :
                target.Rate <= 0 ? EventEligibilityReason.ZeroWeight : EventEligibilityReason.None };
        }
    }
}
