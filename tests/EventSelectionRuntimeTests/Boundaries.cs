using System;
using System.Collections.Generic;

namespace UnityEngine { public static class Time { public static float unscaledTime; } }
namespace Mortal.Core { }
namespace Mortal.Story
{
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
    public static class DicePlugin { public static void LogInfo(string value) { } }
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
    public sealed class EventResultPreview
    {
        public readonly string Summary, Detail;
        public readonly bool Retry;
        public static readonly EventResultPreview Pending = new EventResultPreview("pending", true);
        public static readonly EventResultPreview Unavailable = new EventResultPreview("unavailable", false);
        public static readonly List<string> Reads = new List<string>();
        public static Func<string, EventResultPreview> Reader;
        public EventResultPreview(string text, bool retry = false) { Summary = Detail = text; Retry = retry; }
        internal static EventResultPreview Read(string result)
        { Reads.Add(result); return Reader == null ? new EventResultPreview("preview:" + result) : Reader(result); }
    }
    public enum EventEligibilityReason { None, ConditionsNotMet, ZeroWeight, Random, Unsupported, ReadFailure }
    public sealed class EventEligibilitySnapshot
    {
        public bool IsEligible, CanUseResult;
        public EventEligibilityReason Reason;
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
