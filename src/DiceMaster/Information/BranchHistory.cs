using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using DiceMaster.EventSelection;
using DiceMaster.LiveLog;
using Mortal.Story;

namespace DiceMaster.Information
{
    // Actual executions own identities. Display reads cannot create entries and
    // neither titles nor result values are used to deduplicate different calls.
    public static class BranchHistory
    {
        public const int Capacity = 20;
        public const float NoticeSeconds = 5f;
        internal const string Confirmed = "실제 선택 확인";
        internal const string Unmatched = "확인 불가 · 원본 매칭 실패";
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal sealed class Observation
        {
            internal long Token, Generation;
            internal string Kind, Name, Source, Friendly, Result;
            internal object Pool, Target;
            internal EventSwitchSite Site;
            internal bool Completed, Matched, CountMode, BodyResolved, NoticeExpired;
            internal int Branch, Count;
            internal float Time;
            internal string Body, EffectText;
            internal EventResultPreview Preview;
            internal PreviewAnalysis Analysis;
            internal InformationRow SummaryRow;
            internal string Failure;
        }

        private static readonly List<Observation> entries = new List<Observation>();
        private static readonly Dictionary<string, string> boundaryFailures = new Dictionary<string, string>();
        private static bool enabled;
        private static long generation, sequence;
        private static string contextKey = "0";
        public static string ContextKey { get { return contextKey; } }
        public static Action Cleared;

        public static bool Enabled
        {
            get { return enabled; }
            set
            {
                if (enabled == value) return;
                enabled = value;
                // A callback from an execution begun before OFF cannot replay
                // after ON. Clear also invalidates outstanding detail queries.
                if (!value) Clear();
            }
        }

        public static void Clear()
        {
            unchecked { generation++; }
            contextKey = generation.ToString(CultureInfo.InvariantCulture);
            entries.Clear();
            boundaryFailures.Clear();
            // Published display rows are discarded synchronously as well; a
            // notification failure must never interrupt save/load/gameplay.
            try { Cleared?.Invoke(); }
            catch (Exception error) { BoundaryFailure("clear-notification", null, error); }
        }

        internal static Observation Begin(string kind, object pool, string name, string source, string friendly, EventSwitchSite site)
        {
            if (!enabled) return null;
            return new Observation { Token = unchecked(++sequence), Generation = generation,
                Kind = kind, Pool = pool, Name = name ?? "", Source = source, Friendly = friendly ?? "", Site = site };
        }

        internal static void Complete(Observation observation, string result, int branch, float now, Exception error)
        {
            if (observation == null || observation.Completed) return;
            observation.Completed = true;
            if (error != null || !enabled || observation.Generation != generation) return;
            observation.Result = observation.Kind == "이벤트" ? result : branch.ToString(CultureInfo.InvariantCulture);
            observation.Branch = branch;
            observation.Time = now;
            try { observation.Matched = Match(observation, false); RecoverBoundary(observation.Kind); }
            catch (Exception failure) { BoundaryFailure(observation.Kind, observation.Name, failure); }
            entries.Insert(0, observation);
            if (entries.Count > Capacity) entries.RemoveAt(Capacity);
        }

        // Name reads are deferred to automatic/detail presentation. The actual
        // result callback stores only IDs, object identities and the return.
        private static InformationRow Summary(Observation entry)
        {
            if (entry.SummaryRow != null) return entry.SummaryRow;
            string name = entry.Name;
            string title = entry.Kind == "이벤트" ? ReturnText(entry) :
                (string.IsNullOrEmpty(name) ? "조건" : name) + ": 분기 " + entry.Result;
            return entry.SummaryRow = new InformationRow(Id(entry), title, entry.Kind, entry.Matched ? Confirmed : Unmatched,
                "실제 반환 결과: " + ReturnText(entry), available: entry.Matched);
        }

        private static string ReturnText(Observation entry)
        { return entry.Result == null ? "(null)" : entry.Result.Length == 0 ? "(빈 문자열)" : entry.Result; }

        private static string Id(Observation entry)
        { return "branch/" + entry.Generation.ToString(CultureInfo.InvariantCulture) + "/" + entry.Token.ToString(CultureInfo.InvariantCulture); }

        public static InformationRow Latest(float now)
        {
            if (!enabled || entries.Count == 0 || float.IsNaN(now) || float.IsInfinity(now)) return null;
            Observation entry = entries[0];
            float elapsed = now - entry.Time;
            // Invalid/reset clocks do not resurrect an expired notification.
            if (entry.NoticeExpired) return null;
            if (float.IsNaN(entry.Time) || float.IsInfinity(entry.Time) || elapsed < 0 || elapsed >= NoticeSeconds)
            { entry.NoticeExpired = true; return null; }
            return Summary(entry);
        }

        public static List<DisplayQuery> Discover()
        {
            var result = new List<DisplayQuery>();
            if (!enabled) return result;
            foreach (Observation observed in entries)
            {
                Observation entry = observed;
                result.Add(new DisplayQuery(Id(entry), () => ReadDetail(entry)));
            }
            return result;
        }

        private static InformationRow ReadDetail(Observation entry)
        {
            if (!enabled || entry.Generation != generation || !entries.Contains(entry)) throw new DataNotReadyException();
            InformationRow summary = Summary(entry);
            string detail = "실제 반환 결과: " + ReturnText(entry) + "\n원래 체크포인트: " +
                (string.IsNullOrEmpty(entry.Name) ? "확인 불가" : entry.Name);
            if (!entry.Matched || !Match(entry, true))
                return new InformationRow(summary.Id, summary.Title, entry.Kind, Unmatched,
                    detail + "\n원본 결과를 유일하게 연결하지 못해 조건·효과는 확인 불가입니다.");
            try
            {
                string title = summary.Title;
                string conditions;
                if (entry.Kind == "이벤트")
                {
                    string translated = DataHelper.GetEventName((PositionEventData)entry.Target);
                    if (!string.IsNullOrWhiteSpace(translated)) title = translated;
                    string unused;
                    conditions = PopupConditionText.Event(entry.Target, null, out unused);
                }
                else conditions = SwitchConditions(entry);
                string effects = Effects(entry);
                detail += "\n조건은 조회 시점의 읽기 전용 설명입니다. 실행 당시 상태·난수를 재현하지 않습니다.\n\n관련 조건\n" +
                    conditions + "\n\n관련 효과 (정적 분석)\n" + effects;
                if (entry.Failure != null)
                { Log("recovered target=" + Safe(entry.Name)); entry.Failure = null; }
                return new InformationRow(summary.Id, title, entry.Kind, Confirmed, detail, available: true);
            }
            catch (Exception failure)
            {
                string cause = failure.GetType().FullName;
                if (entry.Failure != cause) { entry.Failure = cause; Log("failure target=" + Safe(entry.Name) + " type=" + cause); }
                throw;
            }
        }

        private static object Read(object value, string name) { return LogBridge.ReadFieldStrict(value, name); }

        private static bool Match(Observation entry, bool recheck)
        {
            object target = null;
            if (entry.Kind == "이벤트")
            {
                var pool = entry.Pool as PositionResultData;
                if (ReferenceEquals(pool, null) || pool.List == null || string.IsNullOrEmpty(entry.Result)) return false;
                string prefix = Read(pool, "_scriptPrefix") as string;
                int count = 0;
                foreach (PositionEventData candidate in pool.List)
                    if (!ReferenceEquals(candidate, null) && prefix + "_" + candidate.ResultType == entry.Result)
                    { target = candidate; count++; }
                if (count != 1) return false;
            }
            else
            {
                bool countMode = (bool)Read(entry.Pool, "_useCount");
                IList items = Read(entry.Pool, countMode ? "_countItems" : "_items") as IList;
                if (items == null || entry.Branch < 1 || entry.Branch > items.Count + 1) return false;
                target = entry.Branch == items.Count + 1 ? entry.Pool : items[entry.Branch - 1];
                if (target == null || recheck && countMode != entry.CountMode) return false;
                if (!recheck) { entry.CountMode = countMode; entry.Count = (int)Read(entry.Pool, "_count"); }
            }
            if (recheck) return ReferenceEquals(entry.Target, target);
            entry.Target = target;
            return true;
        }

        private static string SwitchConditions(Observation entry)
        {
            IList items = Read(entry.Pool, entry.CountMode ? "_countItems" : "_items") as IList;
            if (items == null) throw new DataNotReadyException();
            var lines = new List<string>();
            // Preserve the ordered conditions preceding the actual branch;
            // explanation does not rerun a comparison or increment _count.
            int last = Math.Min(entry.Branch, items.Count);
            for (int index = 0; index < last; index++)
            {
                string condition;
                if (entry.CountMode)
                {
                    string[] operators = { "=", "≠", "<", "≤", ">", "≥" };
                    int op = Convert.ToInt32(Read(items[index], "_compareOp"), CultureInfo.InvariantCulture);
                    condition = op < 0 || op >= operators.Length ? "확인 불가 · 미지원 비교" :
                        "실제 실행 횟수 " + entry.Count + " " + operators[op] + " " +
                        Convert.ToInt32(Read(items[index], "_value"), CultureInfo.InvariantCulture);
                }
                else condition = PopupConditionText.Condition(items[index]);
                lines.Add("분기 " + (index + 1) + (index + 1 == entry.Branch ? " (실제 선택)" : " (앞 분기)") + ": " + condition);
            }
            if (entry.Branch == items.Count + 1) lines.Add("그 외 (실제 선택): 앞 분기 조건이 모두 미충족일 때 원래 선택되는 기본 분기입니다. 실제 반환에는 직접 지정도 포함됩니다.");
            return string.Join("\n", lines);
        }

        private static PreviewParseCache SharedParseCache()
        { return (PreviewParseCache)Read(LogBridge.GetEventParser(), "previewParseCache"); }

        private static string SwitchBody(Observation entry)
        {
            if (entry.BodyResolved) return entry.Body;
            if (entry.Site != null) { entry.BodyResolved = true; return entry.Body = entry.Site.Body(entry.Branch); }
            if (string.IsNullOrEmpty(entry.Source) || string.IsNullOrEmpty(entry.Name) ||
                entry.Source.Length > PreviewParseCache.DefaultMaxCharacters) { entry.BodyResolved = true; return null; }
            var tokens = SharedParseCache().Tokens(entry.Source);
            EventSwitchSite matched = null;
            int matches = 0;
            for (int index = 0; index < tokens.Count; index++)
            {
                string owner, name; int open, close;
                if (!PreviewSyntax.TryCall(tokens, index, out owner, out name, out open, out close) ||
                    owner != "checkpointmanager" || name != "Switch" || close < 0) continue;
                var args = PreviewSyntax.Arguments(entry.Source, tokens, open, close);
                string key;
                if (args.Count != 1 || !PreviewSyntax.TryString(args[0], out key) || key != entry.Name) continue;
                matches++;
                matched = EventSwitchSite.Read(entry.Source, tokens, index, close);
            }
            // A checkpoint name appearing twice cannot identify which Lua arm
            // actually handled the return. Keep that effect explicitly unknown.
            entry.BodyResolved = true;
            return entry.Body = matches == 1 && matched != null ? matched.Body(entry.Branch) : null;
        }

        private static string Effects(Observation entry)
        {
            if (entry.EffectText != null) return entry.EffectText;
            if (entry.Kind == "이벤트")
            {
                entry.Preview = EventResultPreview.Read(entry.Result);
                if (entry.Preview.Retry) throw new InvalidOperationException("Branch effect details could not be read.");
                return entry.EffectText = entry.Preview.Detail;
            }
            string body = SwitchBody(entry);
            if (body == null) return entry.EffectText = "확인 불가 · 실제 호출의 후속 Lua 분기를 유일하게 연결하지 못했습니다.";
            if (entry.Analysis == null)
                entry.Analysis = PreviewAnalyzer.AnalyzeWithCache(entry.Friendly + "/" + entry.Name + "/" + entry.Branch,
                    body, DataHelper.ReadPreviewScript, null, SharedParseCache());
            else if (entry.Analysis.HasReadFailures) PreviewAnalyzer.RetryReads(entry.Analysis);
            entry.Analysis.SnapshotAllowed = false;
            if (entry.Analysis.HasReadFailures) throw new InvalidOperationException("Branch script details could not be read.");
            return entry.EffectText = PreviewDisplay.Describe(entry.Analysis);
        }

        internal static void BoundaryFailure(string boundary, string name, Exception error)
        {
            string cause = error.GetType().FullName;
            string previous;
            if (boundaryFailures.TryGetValue(boundary, out previous) && previous == cause) return;
            boundaryFailures[boundary] = cause;
            Log("failure boundary=" + boundary + " target=" + Safe(name) + " type=" + cause);
        }
        private static void RecoverBoundary(string boundary)
        { if (boundaryFailures.Remove(boundary)) Log("recovered boundary=" + boundary); }
        private static string Safe(string value)
        {
            value = value ?? "";
            if (value.Length > 96) value = value.Substring(0, 96);
            return value.Replace('\n', '_').Replace('\r', '_');
        }
        private static void Log(string message)
        { try { DicePlugin.LogInfo("Information feature=branch " + message); } catch (Exception) { } }
    }
}
