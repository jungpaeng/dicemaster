using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DiceMaster.LiveLog;
using HarmonyLib;
using Mortal.Core;
using Mortal.Story;
using UnityEngine;

namespace DiceMaster.EventSelection
{
    public sealed class EventCandidate
    {
        public readonly string Id, Title, Result;
        public readonly object Target;
        public readonly EventEligibilitySnapshot Eligibility;
        private readonly EventResultPreview preview;
        internal readonly int Revision;
        internal EventCandidate(string id, string title, string result, object target, EventEligibilitySnapshot eligibility, EventResultPreview preview = null, int revision = 0)
        { Id = id; Title = title; Result = result; Target = target; Eligibility = eligibility; this.preview = preview ?? EventResultPreview.Pending; Revision = revision; }
        public string State => Eligibility.StatusText;
        public string Summary => Eligibility.Summary;
        public string ResultSummary => preview.Summary;
        public string ResultDetail => preview.Detail;
        public string ResultSummaryRich => preview.RichSummary;
        public string ResultDetailRich => preview.RichDetail;
        public bool CanSelect => Eligibility.IsEligible && Eligibility.CanUseResult && !string.IsNullOrEmpty(Result);
        public bool CanForce => !Eligibility.IsEligible && Eligibility.CanUseResult && !string.IsNullOrEmpty(Result);
    }

    // Single actual action owner. All calls are on the Unity/Lua main thread;
    // no render path executes the game and no request survives a context reset.
    public static partial class EventSelectionRuntime
    {
        private sealed class ActionContext
        {
            internal long Token, Script, Epoch;
            internal int Site;
            internal object Coroutine;
            internal CheckPointManager Manager;
            internal PositionResultConfig Config;
            internal PositionResultData Pool;
            internal SwitchResultConfig SwitchConfig;
            internal SwitchResultData SwitchPool;
            internal EventSwitchSite SwitchSite;
            internal bool SwitchUseCount;
            internal int SwitchCount;
            internal object SwitchDirectTarget;
            internal int SwitchDirectResult;
            internal string Name, Prefix;
            internal readonly Dictionary<object, string> Identities = new Dictionary<object, string>(ReferenceComparer.Instance);
            internal readonly Dictionary<object, EligibilityRead> Reads = new Dictionary<object, EligibilityRead>(ReferenceComparer.Instance);
            internal string SelectedId;
            internal EventCandidate SelectedCandidate;
            internal bool Force, RandomRequested, Released, Permit, Consumed, Executing;
            internal string DirectResult;
            internal PositionEventData DirectTarget;
            internal int NextIdentity;
            internal float NextRefresh;
        }
        private sealed class EligibilityRead
        {
            internal EventEligibilitySnapshot Snapshot;
            internal float RetryAt;
            internal string Result = "", Title = "이벤트 자료 읽기 실패";
            internal string PreviewResult;
            internal EventResultPreview Preview;
            internal float PreviewRetryAt;
        }
        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();
            public new bool Equals(object left, object right) => ReferenceEquals(left, right);
            public int GetHashCode(object value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
        }

        private static readonly BindingFlags Fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly IList<EventCandidate> Empty = new List<EventCandidate>().AsReadOnly();
        private static ActionContext current;
        private static IList<EventCandidate> candidates = Empty;
        private static long epoch, sequence, scriptSequence;
        private static bool waiting, displayVisible;
        private static readonly Dictionary<string, string> failures = new Dictionary<string, string>(StringComparer.Ordinal);
        public static Func<bool> Enabled;
        public static Func<bool> SwitchEnabled;
        public static bool IsSwitch => current != null && current.SwitchPool != null;
        public static string PopupTitle => IsSwitch ? "이번 대화의 조건 분기 선택" : "이번 행동의 이벤트 선택";
        public static string PrimaryNoun => IsSwitch ? "분기" : "이벤트";
        public static Action<bool> WaitingChanged;
        public static bool Waiting => waiting;
        public static long Token => current == null ? 0 : current.Token;
        public static IList<EventCandidate> Candidates => candidates;
        public static string Status { get; private set; } = "";
        internal static long Epoch => epoch;
        public static void Install(Harmony harmony) { EventExecutionPatches.Install(harmony); }

        private static void SetWaiting(bool value)
        {
            if (waiting == value) return;
            waiting = value;
            try { WaitingChanged?.Invoke(value); }
            catch (Exception error) { Failure("input-state", error); }
        }

        internal static void Failure(string boundary, Exception error)
        {
            string cause = error.GetType().FullName;
            string previous;
            if (failures.TryGetValue(boundary, out previous) && previous == cause) return;
            failures[boundary] = cause;
            Log("failure boundary=" + boundary + " type=" + cause);
        }

        private static void Recovered(string boundary)
        { if (failures.Remove(boundary)) Log("recovered boundary=" + boundary); }
        private static void Log(string message)
        { try { DicePlugin.LogInfo("EventSelection " + message); } catch (Exception) { } }

        private static bool IsEnabled(bool switchAction = false)
        {
            try
            {
                Func<bool> setting = switchAction ? SwitchEnabled : Enabled;
                bool enabled = setting != null && setting();
                Recovered("settings");
                return enabled;
            }
            catch (Exception error) { Failure("settings", error); return false; }
        }

        public static void Invalidate(string reason)
        {
            checked { epoch++; }
            if (current != null) Log("invalidated token=" + current.Token + " reason=" + reason);
            current = null;
            candidates = Empty;
            Status = "";
            SetWaiting(false);
        }

        internal static long NewScript()
        {
            // Lua numbers are doubles; keep baked tokens exact.
            if (scriptSequence >= 9007199254740990L) throw new InvalidOperationException("Lua token exhausted.");
            return ++scriptSequence;
        }

        private static object Field(object value, string name)
        {
            if (value == null) throw new InvalidOperationException("Missing event data.");
            for (Type at = value.GetType(); at != null; at = at.BaseType)
            {
                FieldInfo field = at.GetField(name, Fields | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(value);
            }
            throw new MissingFieldException(value.GetType().FullName, name);
        }

        private static PositionResultData Resolve(CheckPointManager manager, string name, out PositionResultConfig config)
        {
            config = Field(manager, "_position") as PositionResultConfig;
            if (config == null || config.List == null) return null;
            foreach (PositionResultData pool in config.List)
                if (pool != null && pool.name == name) return pool;
            return null;
        }

        private static bool Valid(ActionContext action)
        {
            if (action != null && action.SwitchPool != null) return ValidSwitch(action);
            if (action == null || action.Epoch != epoch || action.Manager == null || action.Pool == null ||
                !ReferenceEquals(CheckPointManager.Instance, action.Manager)) return false;
            PositionResultConfig config;
            PositionResultData pool = Resolve(action.Manager, action.Name, out config);
            return ReferenceEquals(config, action.Config) && ReferenceEquals(pool, action.Pool) &&
                string.Equals(Field(pool, "_scriptPrefix") as string, action.Prefix, StringComparison.Ordinal);
        }

        private static bool Contains(ActionContext action, PositionEventData target)
        {
            if (target == null || action.Pool.List == null) return false;
            foreach (PositionEventData item in action.Pool.List) if (ReferenceEquals(item, target)) return true;
            return false;
        }

        private static string Result(ActionContext action, PositionEventData target)
        {
            if (!Contains(action, target)) return "";
            string kind = target.ResultType;
            return string.IsNullOrEmpty(kind) ? "" : action.Prefix + "_" + kind;
        }

        private static void Refresh(ActionContext action, float now, bool force = false, int previewBudget = 0)
        {
            if (action.SwitchPool != null) { RefreshSwitch(action, now, previewBudget); return; }
            var eligible = new List<EventCandidate>();
            var remaining = new List<EventCandidate>();
            if (action.Pool.List == null) throw new InvalidOperationException("Missing candidate collection.");
            foreach (PositionEventData target in action.Pool.List)
            {
                if (target == null) continue;
                string id;
                if (!action.Identities.TryGetValue(target, out id))
                {
                    id = action.Token + "/" + (++action.NextIdentity);
                    action.Identities.Add(target, id);
                }
                EligibilityRead read;
                if (!action.Reads.TryGetValue(target, out read))
                { read = new EligibilityRead(); action.Reads.Add(target, read); }
                if (force || read.Snapshot == null || now >= read.RetryAt)
                {
                    read.Snapshot = EventEligibility.Evaluate(target);
                    read.RetryAt = now + (read.Snapshot.Reason == EventEligibilityReason.ReadFailure ? 5f : .25f);
                    try
                    {
                        read.Result = Result(action, target);
                        if (string.IsNullOrEmpty(read.Result)) throw new InvalidOperationException("Missing event result.");
                        read.Title = PositionEventCache.Instance.GetPositionEventName(target) ?? target.name;
                        Recovered("candidate-result/" + id);
                    }
                    catch (Exception error)
                    {
                        read.Snapshot = EventEligibilitySnapshot.Unreadable("이벤트 대상 또는 결과를 읽지 못했습니다.");
                        read.Result = "";
                        read.RetryAt = now + 5f;
                        Failure("candidate-result/" + id, error);
                    }
                }
                if (read.PreviewResult != read.Result || read.Preview == null)
                {
                    read.PreviewResult = read.Result;
                    read.Preview = string.IsNullOrEmpty(read.Result) ? EventResultPreview.Unavailable : EventResultPreview.Pending;
                    read.PreviewRetryAt = 0;
                }
                // Prepare at most two new/retrying definitions per visible
                // Update; OnGUI only consumes immutable text. Successful
                // definitions stay cached for this actual action/target/result.
                if (previewBudget > 0 && !string.IsNullOrEmpty(read.Result) && now >= read.PreviewRetryAt)
                {
                    previewBudget--;
                    read.Preview = EventResultPreview.Read(read.Result);
                    read.PreviewRetryAt = read.Preview.Retry ? now + 5f : float.PositiveInfinity;
                }
                EventEligibilitySnapshot eligibility = read.Snapshot;
                var candidate = new EventCandidate(id, read.Title, read.Result, target, eligibility, read.Preview);
                (candidate.CanSelect ? eligible : remaining).Add(candidate);
            }
            eligible.AddRange(remaining);
            candidates = eligible.AsReadOnly();
        }

        // Called only by a running instrumented Lua expression. A script's
        // discovered Position strings are never admission authority.
        internal static bool Wait(string name, long script, int site, object coroutine, long scriptEpoch)
        {
            if (scriptEpoch != epoch) return true; // The enumerator guard disposes it before another resume.
            if (current != null && (!ReferenceEquals(current.Coroutine, coroutine) || current.Script != script || current.Site != site || current.SwitchPool != null))
            {
                Log("pass-through reason=concurrent-action");
                return false;
            }
            try
            {
                if (current == null)
                {
                    if (!IsEnabled()) return false;
                    CheckPointManager manager = CheckPointManager.Instance;
                    if (manager == null) return false;
                    PositionResultConfig config;
                    PositionResultData pool = Resolve(manager, name, out config);
                    if (pool == null || pool.List == null || pool.List.Count == 0) return false;
                    string prefix = Field(pool, "_scriptPrefix") as string;
                    if (prefix == null) return false;
                    var action = new ActionContext { Token = checked(++sequence), Script = script, Site = site,
                        Epoch = epoch, Coroutine = coroutine, Manager = manager, Config = config, Pool = pool,
                        Name = name, Prefix = prefix };
                    Refresh(action, Time.unscaledTime);
                    // Exactly one readable, eligible entry is the only skip
                    // case; never collapse a larger pool to its visible rows.
                    if (pool.List.Count == 1 && candidates.Count == 1 && candidates[0].CanSelect)
                    { candidates = Empty; return false; }
                    if (candidates.Count == 0) { candidates = Empty; return false; }
                    current = action;
                    Status = "이번 행동의 이벤트를 선택하세요.";
                    Log("wait token=" + action.Token + " pool=" + name + " candidates=" + pool.List.Count);
                    SetWaiting(true);
                }
                ActionContext active = current;
                if (!Valid(active)) { Invalidate("후보 또는 게임 문맥 변경"); return true; }
                if (!IsEnabled()) ReleaseRandom("setting-disabled");
                if (active.Pool.List == null || active.Pool.List.Count == 0) ReleaseRandom("empty-candidates");
                if (active.RandomRequested) ReleaseRandom("user");
                if (active.Released)
                { active.Permit = true; SetWaiting(false); return false; }
                if (active.SelectedId != null && PrepareSelection(active))
                { active.Permit = true; SetWaiting(false); return false; }
                return true;
            }
            catch (Exception error)
            {
                Failure("execution-gate", error);
                // No original decision/effect has run at this point. Release
                // this one invocation to the game's original path once.
                if (current != null)
                { current.SelectedId = null; current.SelectedCandidate = null; current.DirectResult = null; current.Released = true; current.Permit = true; }
                else candidates = Empty;
                SetWaiting(false);
                return false;
            }
        }

        private static bool PrepareSelection(ActionContext action)
        {
            if (action.SwitchPool != null) return PrepareSwitchSelection(action);
            string selected = action.SelectedId;
            EventCandidate confirmed = action.SelectedCandidate;
            action.SelectedId = null;
            action.SelectedCandidate = null;
            EventCandidate candidate = null;
            foreach (EventCandidate item in candidates) if (item.Id == selected) { candidate = item; break; }
            PositionEventData target = candidate == null ? null : candidate.Target as PositionEventData;
            string result = target == null ? "" : Result(action, target);
            // A display refresh may publish a new result under the same stable
            // row id. It must not replace the target/result the user confirmed.
            if (confirmed == null || candidate == null || confirmed.Id != selected ||
                !ReferenceEquals(confirmed.Target, target) || string.IsNullOrEmpty(result) ||
                result != confirmed.Result || result != candidate.Result)
            { Status = "대상이 바뀌었습니다. 다시 선택하세요."; Refresh(action, Time.unscaledTime, true); return false; }
            EventEligibilitySnapshot eligibility = EventEligibility.Evaluate(target);
            if (action.Force ? !eligibility.CanUseResult : !eligibility.IsEligible)
            { Status = "현재 발동 상태가 바뀌었습니다. 다시 선택하세요."; Refresh(action, Time.unscaledTime, true); return false; }
            // Strict eligibility is evaluated again in the running Lua
            // continuation immediately before returning the direct result.
            // Calling the original GetRate here could partly consume counters
            // or RNG and then throw; retrying the original after that would
            // repeat effects. Random/unsupported graphs cannot be normal
            // confirmations, so deterministic audited reads are sufficient.
            action.DirectTarget = target;
            action.DirectResult = result;
            return true;
        }

        public static bool Select(string id, bool force)
        {
            if (!waiting || current == null || current.SelectedId != null || current.RandomRequested) return false;
            foreach (EventCandidate item in candidates)
                if (item.Id == id && (force ? item.CanForce : item.CanSelect))
                {
                    current.SelectedId = id; current.SelectedCandidate = item; current.Force = force;
                    Status = "현재 판정을 확인하고 있습니다.";
                    return true;
                }
            return false;
        }

        private static void ReleaseRandom(string reason)
        {
            if (current == null || current.Consumed || current.Released) return;
            current.SelectedId = null; current.SelectedCandidate = null; current.DirectResult = null; current.DirectTarget = null;
            current.SwitchDirectTarget = null; current.SwitchDirectResult = 0;
            current.Released = true;
            Log("release token=" + current.Token + " mode=random reason=" + reason);
            SetWaiting(false);
        }

        public static void ContinueRandom()
        {
            if (!waiting || current == null || current.SelectedId != null || current.RandomRequested) return;
            current.RandomRequested = true;
            Status = "원래 판정으로 진행합니다.";
        }

        public static void CancelConfirmation()
        {
            if (current == null || current.Executing || current.Released) return;
            bool cancelled = current.SelectedId != null || current.RandomRequested;
            current.SelectedId = null;
            current.SelectedCandidate = null;
            current.RandomRequested = false;
            if (cancelled && waiting) Status = "진행 요청이 취소되었습니다. 다시 확인하세요.";
        }

        public static void Tick(bool visible, float now)
        {
            displayVisible = visible;
            if (current == null) return;
            try
            {
                if (!Valid(current)) { Invalidate("후보 또는 게임 문맥 변경"); return; }
                if (!IsEnabled(current.SwitchPool != null)) { ReleaseRandom("setting-disabled"); return; }
                if (!waiting || !displayVisible || now < current.NextRefresh) return;
                current.NextRefresh = now + .25f;
                Refresh(current, now, previewBudget: 2);
                if ((current.SwitchPool == null && current.Pool.List.Count == 0) || candidates.Count == 0) ReleaseRandom("empty-candidates");
                Recovered("display");
            }
            catch (Exception error)
            {
                Failure("display", error);
                // A broken candidate pool cannot maintain a hidden wait.
                ReleaseRandom("candidate-read-failure");
            }
        }

        internal static ActionContextHandle BeginPosition(CheckPointManager manager, string name)
        {
            ActionContext action = current;
            if (action == null || action.SwitchPool != null || !action.Permit || action.Executing || !ReferenceEquals(manager, action.Manager) || action.Name != name)
                return null;
            action.Permit = false;
            action.Executing = true;
            return new ActionContextHandle(action.Token);
        }

        public sealed class ActionContextHandle
        {
            internal readonly long Token;
            internal ActionContextHandle(long token) { Token = token; }
        }

        internal static bool TryDirectResult(PositionResultData pool, out string result)
        {
            result = null;
            ActionContext action = current;
            if (action == null || !action.Executing || action.Consumed || !ReferenceEquals(pool, action.Pool)) return false;
            action.Consumed = true;
            if (action.Released || string.IsNullOrEmpty(action.DirectResult)) return false;
            // No effects have occurred yet. This final identity check does not
            // evaluate conditions or consume any additional randomness.
            if (!Valid(action) || !Contains(action, action.DirectTarget) || Result(action, action.DirectTarget) != action.DirectResult)
                throw new InvalidOperationException("Selected event changed during the execution boundary.");
            result = action.DirectResult;
            Log("consumed token=" + action.Token + " mode=" + (action.Force ? "force" : "normal") +
                " target=" + action.DirectTarget.name + " result=" + result);
            return true;
        }

        internal static void CompletePosition(ActionContextHandle handle, Exception error)
        {
            if (handle == null || current == null || current.Token != handle.Token) return;
            Log("completed token=" + current.Token + " mode=" + (current.Released ? "random" : current.Force ? "force" : "normal") +
                " result-consumed=" + current.Consumed + " failed=" + (error != null));
            current = null; candidates = Empty; Status = ""; SetWaiting(false);
        }
    }
}
