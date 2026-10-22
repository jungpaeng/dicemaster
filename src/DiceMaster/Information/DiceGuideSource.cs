using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DiceMaster.LiveLog;
using DiceMaster.EventSelection;
using Fungus;
using HarmonyLib;
using Mortal.Core;
using Mortal.Story;
using UnityEngine;
using UnityEngine.UI;

namespace DiceMaster.Information
{
    public sealed class DiceGuideSnapshot
    {
        public readonly List<InformationRow> Entries = new List<InformationRow>();
        public readonly List<object> Preparing = new List<object>();
        public string ContextKey;
    }
    // Passive dice-row guidance. No coroutine replacement, native selection,
    // checkpoint execution, input listener or accepted-result recording.
    public static class DiceGuideSource
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly FieldInfo nativeButtons = typeof(MenuDialog).GetField("cachedButtons", Fields);
        private static readonly FieldInfo rawText = typeof(DiceOption).GetField("_sourceText", Fields);
        private static readonly FieldInfo normalText = typeof(DiceOption).GetField("_normalText", Fields);
        private static readonly FieldInfo highText = typeof(DiceOption).GetField("_highText", Fields);
        private static readonly FieldInfo conditionText = typeof(DiceOption).GetField("_conditionText", Fields);
        private static string script, source, key;
        private static DiceRouteIndex index;
        private static DiceRouteIndex.Site site;
        private static DiceMenuDialog owner;
        private sealed class PendingRoll
        {
            internal DiceMenuDialog Owner;
            internal string[] Raw;
            internal string Key;
            internal long Context;
            internal int InitializationDepth;
        }
        private static PendingRoll pending;
        private static string[] raw;
        private static int generation, cursor;
        private static long context;
        private static bool guide, once;
        private static bool located;
        private static readonly Dictionary<int, MapRouteDefinitions> arms = new Dictionary<int, MapRouteDefinitions>();
        private static readonly Dictionary<object, DiceGuideLabel> labels = new Dictionary<object, DiceGuideLabel>();
        private static readonly Dictionary<object, float> proofRetries = new Dictionary<object, float>();
        private static readonly Dictionary<object, string> eventNames = new Dictionary<object, string>();
        private static string TextOf(object text)
        { return text == null ? "" : text.GetType().GetProperty("text", Fields)?.GetValue(text, null) as string ?? ""; }
        private static bool Live(DiceMenuDialog dice) => dice != null && dice.isActiveAndEnabled && dice.gameObject.activeInHierarchy && ReferenceEquals(MenuDialog.ActiveMenuDialog, dice);
        public static bool Active => (guide || once) &&
            (context == LogBridge.Model.ContextId && Live(owner) || pending != null && pending.Context == LogBridge.Model.ContextId && Live(pending.Owner));
        public static string ContextKey => generation + "/" + LogBridge.Model.ContextId + "/" + guide + "/" + once;
        public static void Configure(bool showGuide, bool showOnce)
        {
            if (guide == showGuide && once == showOnce) return;
            guide = showGuide; once = showOnce; Restore(); generation++;
            if (!guide && !once) { arms.Clear(); index = null; site = null; located = false; }
        }
        public static void ObserveScript(string text, string friendly)
        {
            const string suffix = ".LuaScript";
            if (string.IsNullOrEmpty(friendly) || !friendly.EndsWith(suffix, StringComparison.Ordinal)) return;
            string name = friendly.Substring(0, friendly.Length - suffix.Length);
            if (name.Length == 0 || name.Length > 256) return;
            foreach (char c in name) if (!(char.IsLetterOrDigit(c) || c == '_')) return;
            Clear(); script = name; source = text ?? "";
        }
        public static void Clear()
        { ResetView(); pending = null; script = source = null; index = null; proofRetries.Clear(); eventNames.Clear(); }
        private static void ResetView()
        { Restore(); owner = null; key = null; raw = null; site = null; located = false; arms.Clear(); labels.Clear(); generation++; cursor = 0; }
        // ExecuteRoll returns an iterator before its body activates the view.
        // Capture this invocation, but bind only after native UpdateOptions.
        private static void Capture(DiceMenuDialog dice, string[] options, string checkName)
        {
            ResetView(); pending = null;
            if (!guide && !once || dice == null || options == null || options.Length == 0 || options.Length > 16) return;
            var copy = (string[])options.Clone();
            foreach (string value in copy) if (value == null || value.Length > 2048) return;
            pending = new PendingRoll { Owner = dice, Raw = copy, Key = checkName, Context = LogBridge.Model.ContextId };
        }
        private static void PreparedView(DiceMenuDialog dice)
        {
            var next = pending;
            if (next == null || !ReferenceEquals(next.Owner, dice) || next.InitializationDepth != 0 ||
                next.Context != LogBridge.Model.ContextId || !Live(dice)) return;
            // A stale/partial native refresh cannot activate captured evidence.
            for (int at = 0; at < next.Raw.Length; at++) if (Option(dice, next.Raw, at) == null) return;
            owner = dice; key = next.Key; raw = next.Raw; context = next.Context; pending = null; generation++;
        }
        private static void Closed(MenuDialog menu)
        {
            bool captured = pending != null && ReferenceEquals(pending.Owner, menu);
            // Native MenuDialog.Awake calls Clear on the first activation.
            // Preserve only the exact pending invocation inside that scope.
            if (captured && pending.InitializationDepth > 0) return;
            if (captured) pending = null;
            if (captured || ReferenceEquals(owner, menu)) ResetView();
        }
        private static void Hidden(DiceMenuDialog dice)
        {
            bool captured = pending != null && ReferenceEquals(pending.Owner, dice);
            if (captured) pending = null;
            if (captured || ReferenceEquals(owner, dice)) ResetView();
        }
        private static bool Valid => (guide || once) && Live(owner) && context == LogBridge.Model.ContextId && raw != null;
        public static List<DisplayQuery> Discover()
        {
            var result = new List<DisplayQuery>(); if (!Active) return result;
            result.Add(new DisplayQuery("dice/" + ContextKey, ReadSnapshot)); return result;
        }
        private static DiceOption Option(int at) => Option(owner, raw, at);
        private static DiceOption Option(DiceMenuDialog dice, string[] options, int at)
        {
            var buttons = nativeButtons == null || dice == null ? null : nativeButtons.GetValue(dice) as Button[];
            if (options == null || buttons == null || at >= buttons.Length || buttons[at] == null || !buttons[at].gameObject.activeInHierarchy) return null;
            var option = buttons[at].GetComponent<DiceOption>();
            return option != null && rawText != null && TextOf(rawText.GetValue(option)) == options[at] ? option : null;
        }
        private static ScriptReadResult Script(string name) => string.Equals(name, script, StringComparison.OrdinalIgnoreCase) ? ScriptReadResult.Success(source) : DataHelper.ReadPreviewScript(name);
        private static object ReadSnapshot()
        {
            if (!Active) throw new DataNotReadyException();
            var snapshot = new DiceGuideSnapshot { ContextKey = ContextKey };
            if (!Valid || source == null)
            {
                snapshot.Entries.Add(new InformationRow("dice/status", "주사위 연결 자료", "자료 상태",
                    !Valid ? "주사위 결과 화면 준비 대기" : "주사위 원문 미확인",
                    "스크립트: " + (script ?? "미확인") + "\n판정 키: " + (pending == null ? key : pending.Key) +
                    "\n현재 주사위 화면을 관찰했습니다. 원본 결과 행의 준비와 원문 연결을 확인한 뒤 안내합니다. 이전 지도 결과로 대신하지 않습니다.", ""));
                return snapshot;
            }
            var slice = new MapRouteDefinitions.PreparationSlice();
            if (index == null) index = new DiceRouteIndex(script, source);
            index.Advance(262144, slice.ShouldYield);
            if (index.Ready && !located) { site = index.Find(key, raw); located = true; }
            if (site != null)
            {
                // One screen-wide preparation budget; cursor survives ticks.
                int work = 8192;
                for (int n = 0; n < raw.Length && work > 0 && !slice.ShouldYield(); n++)
                {
                    int at = cursor++ % raw.Length + 1;
                    if (!arms.TryGetValue(at, out var graph)) { graph = index.Arm(site, at, Script, MapGuideSource.ReadChoicePool); arms.Add(at, graph); }
                    if (graph == null) continue;
                    int before = graph.WorkCount; graph.AdvanceBudgeted(Time.unscaledTime, Math.Min(2048, work), slice.ShouldYield); work -= graph.WorkCount - before;
                }
            }
            var current = new MapRouteRead(); int paths = 128, gates = 128, kinds = 128, proofs = 1;
            Dictionary<string, ChoiceStorySource.PendingChoice> waiting = null;
            for (int at = 0; at < raw.Length; at++)
            {
                var option = Option(at); if (option == null) continue;
                InformationRow row = null;
                if (site != null && arms.TryGetValue(at + 1, out var graph) && graph != null)
                {
                    if (guide) foreach (var witness in graph.StoryPaths)
                    {
                        if (paths-- <= 0 || slice.ShouldYield()) break;
                        var path = witness.Path;
                        if (!path.Gate.IsKnownSyntax || path.Steps.Count == 0 || path.Steps[0].Index != at + 1 ||
                            path.ReadGuidance(current, 0, true, true, ref gates, ref kinds, out var state) != MapGuidanceRejection.None) continue;
                        string name = ChoiceStoryLabels.DialogueTarget(path.DialogueCharacter);
                        string route = ChoiceStoryLabels.Path(path.Steps);
                        row = new InformationRow("dice/" + generation + "/" + at, "결과 " + (at + 1), "이야기 연결", "해당 결과 시 연결 후보",
                            "실제 열린 주사위: " + key + "\n표시 근거: " + witness.Story.Script + " · " + witness.Story.StoryKey +
                            "\n연결 경로: " + route + "\n현재 내부 조건: " + state +
                            "\n해당 결과가 나왔을 때의 경로 후보입니다. 후속 상태 변경·추가 판정·사용자 분기 선택 이후의 발생은 보장하지 않습니다.",
                            (path.Steps.Count > 1 ? ChoiceStoryLabels.Resolve(path.TopicStep.RawLabel) + " → " : "") + name + " · 연결 후보", option, false, false, true); break;
                    }
                    // Opportunity markers retain the existing stricter meaning.
                    // No future effect/unknown predicate can prove current reach.
                    if (!graph.IsPreparing && !graph.HasIncompleteOpportunityPaths && !graph.HasReadFailure &&
                        CheckpointReservationStore.Current(false) == null && CheckpointReservationStore.Current(true) == null)
                    {
                        if (once) foreach (var entry in graph.Entries)
                        {
                            if (paths-- <= 0 || slice.ShouldYield()) break;
                            if (!Met(entry.Paths, current, ref gates) || !MapGuideSource.SmallEventGraph(entry.Event) || !(entry.Event is PositionEventData item)) continue;
                            if (proofRetries.TryGetValue(entry.Event, out float retry) && Time.unscaledTime < retry) continue;
                            if (!OneShotProof.TryGetEvent(entry.Event, entry.ResultScript, out var proof))
                            { if (proofs-- <= 0 || !graph.CanBuildBoundedProof(entry.ResultScript)) break; proof = OneShotProof.ForEvent(entry.Event, entry.ResultScript); }
                            if (proof.Failed) { proofRetries[entry.Event] = Time.unscaledTime + 5; continue; }
                            if (!proof.Proven) continue;
                            if (!eventNames.TryGetValue(item, out string name)) eventNames[item] = name = DataHelper.GetEventName(item);
                            row = Opportunity(at, option, name, "1회성 이야기", proof.DetailText); break;
                        }
                        if (guide) foreach (var entry in graph.ScriptEntries)
                        {
                            if (paths-- <= 0 || slice.ShouldYield()) break;
                            if (waiting == null) waiting = ChoiceStorySource.WaitingScripts();
                            if (!waiting.TryGetValue(entry.Script, out var pending) || !Met(entry.Paths, current, ref gates)) continue;
                            row = Opportunity(at, option, pending.Name, "등록된 대기 임무", "실제 등록된 임무의 시작 스크립트와 현재 경로를 확인했습니다."); break;
                        }
                    }
                    if (row == null && graph.IsPreparing && !graph.HasReadFailure) snapshot.Preparing.Add(option);
                }
                else if (!index.Ready) snapshot.Preparing.Add(option);
                if (row != null) snapshot.Entries.Add(row);
            }
            string status = !index.Ready ? "정의 준비 중" : site == null ? "현재 주사위 위치·결과 연결 미확인" : "결과별 경로 조회";
            snapshot.Entries.Add(new InformationRow("dice/status", "주사위 연결 자료", "자료 상태", status,
                "스크립트: " + script + "\n판정 키: " + key + "\n정의 작업: " + index.Work + "\n" +
                "동일한 키·결과가 여러 위치에 있거나 미지원 구문이면 자동 표시하지 않습니다. 숨김/설정 OFF에서는 분석하지 않습니다. 판정·예약·RNG를 실행하지 않습니다.", ""));
            return snapshot;
        }
        private static bool Met(IList<MapRoutePath> paths, MapRouteRead read, ref int budget)
        {
            foreach (var path in paths)
                if (!path.Uncertain && read.BudgetedCurrent(path.Gate, ref budget, out var state) && state == PreviewConditionState.Met && path.ReadChoiceAvailability(read, ref budget, 0) == MapRouteState.Met) return true;
            return false;
        }
        private static InformationRow Opportunity(int at, object anchor, string name, string kind, string detail)
        { return new InformationRow("dice/" + generation + "/" + at, "결과 " + (at + 1), kind, "해당 결과 시 진행 가능", detail + "\n주사위 결과 자체를 보장하거나 선택하지 않습니다.", ChoiceStoryLabels.ShortName(name) + " · 해당 결과 시 진행 가능", anchor, true, true); }
        private static void Apply(DiceOption option, string note)
        {
            foreach (var field in new[] { normalText, highText })
            {
                object text = field?.GetValue(option); if (text == null) continue;
                if (!labels.TryGetValue(text, out var label)) labels.Add(text, label = new DiceGuideLabel(text, conditionText?.GetValue(option)));
                label.Apply(note);
            }
        }
        public static void Render(DiceGuideSnapshot snapshot)
        {
            if (snapshot == null || !Valid || snapshot.ContextKey != ContextKey) { Restore(); return; }
            for (int at = 0; at < raw.Length; at++)
            {
                var option = Option(at); if (option == null) continue;
                string note = ""; foreach (var row in snapshot.Entries) if (ReferenceEquals(row.Anchor, option)) { note = row.AutoText; break; }
                Observe(() => Apply(option, note));
            }
        }
        public static void Restore() { foreach (var label in labels.Values) Observe(() => label.Apply("")); }
        private static void Observe(Action action)
        { try { action(); } catch (Exception error) { InformationRuntime.Failure("주사위 경로", error); } }
        public static void Install(Harmony harmony)
        { harmony.PatchAll(typeof(Roll)); harmony.PatchAll(typeof(Initialize)); harmony.PatchAll(typeof(Prepared)); harmony.PatchAll(typeof(Close)); harmony.PatchAll(typeof(Hide)); harmony.PatchAll(typeof(Content)); }
        [HarmonyPatch(typeof(DiceMenuDialog), "ExecuteRoll")]
        private static class Roll
        {
            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void Postfix(DiceMenuDialog __instance, string[] options, string checkName)
            { if (OptionInfoPatch.DisplayRefreshDepth == 0) Observe(() => Capture(__instance, options, checkName)); }
        }
        [HarmonyPatch(typeof(MenuDialog), "Awake")]
        private static class Initialize
        {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void Prefix(MenuDialog __instance, out PendingRoll __state)
            {
                __state = pending != null && ReferenceEquals(pending.Owner, __instance) ? pending : null;
                if (__state != null) __state.InitializationDepth++;
            }
            [HarmonyFinalizer, HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(Exception __exception, PendingRoll __state)
            {
                if (__state != null)
                {
                    __state.InitializationDepth--;
                    if (__exception != null && ReferenceEquals(pending, __state)) Observe(() => { pending = null; ResetView(); });
                }
                return __exception;
            }
        }
        [HarmonyPatch(typeof(CustomMenuDialog), "UpdateOptions")]
        private static class Prepared
        {
            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void Postfix(CustomMenuDialog __instance)
            { if (__instance is DiceMenuDialog dice && OptionInfoPatch.DisplayRefreshDepth == 0) Observe(() => PreparedView(dice)); }
        }
        [HarmonyPatch(typeof(MenuDialog), nameof(MenuDialog.Clear))]
        private static class Close
        {
            [HarmonyPrefix]
            private static void Prefix(MenuDialog __instance)
            { Observe(() => Closed(__instance)); }
        }
        [HarmonyPatch(typeof(DiceMenuDialog), "OnDisable")]
        private static class Hide
        {
            [HarmonyPrefix]
            private static void Prefix(DiceMenuDialog __instance)
            { Observe(() => Hidden(__instance)); }
        }
        [HarmonyPatch(typeof(DiceOption), nameof(DiceOption.UpdateContent))]
        private static class Content
        {
            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void Postfix(DiceOption __instance)
            { if (OptionInfoPatch.DisplayRefreshDepth == 0 && Valid) Observe(() => Apply(__instance, "")); }
        }
    }
    // Adds only a bounded suffix to rendered TMP labels, leaving native raw
    // keys, thresholds, existing effects and layout unchanged. No new focus.
    internal sealed class DiceGuideLabel
    {
        private readonly object label;
        private readonly PropertyInfo text;
        private readonly MethodInfo measure;
        private readonly Component blocker;
        private readonly Vector3[] corners = new Vector3[4];
        private string original, rendered, note;
        private float width, font;
        internal DiceGuideLabel(object target, object other = null)
        { label = target; blocker = other as Component; text = target.GetType().GetProperty("text"); measure = target.GetType().GetMethod("GetPreferredValues", new[] { typeof(string) }); }
        internal void Apply(string value)
        {
            if (text == null) return;
            string current = text.GetValue(label, null) as string ?? "";
            bool changed = current != rendered;
            if (changed) original = current;
            var rt = (label as Component)?.transform as RectTransform; float nextWidth = rt == null ? 0 : rt.rect.width;
            if (rt != null && blocker != null && blocker.gameObject.activeInHierarchy && blocker.transform is RectTransform other)
            {
                other.GetWorldCorners(corners);
                Vector3 min = rt.InverseTransformPoint(corners[0]), max = rt.InverseTransformPoint(corners[2]);
                if (max.y >= rt.rect.yMin && min.y <= rt.rect.yMax && min.x > rt.rect.xMin)
                    nextWidth = Math.Min(nextWidth, min.x - rt.rect.xMin);
            }
            float nextFont = label.GetType().GetProperty("fontSize")?.GetValue(label, null) is float size ? size : 0;
            if (!changed && note == value && width == nextWidth && font == nextFont) return;
            note = value; width = nextWidth; font = nextFont; rendered = original ?? current;
            if (value.Length != 0 && measure != null && width > 0)
                foreach (int max in new[] { 48, 32, 20, 12, 8 })
                {
                    string shortText = value.Length > max ? value.Substring(0, max - 1) + "…" : value;
                    string candidate = rendered + " <size=70%>〔" + shortText.Replace("<", "＜").Replace(">", "＞") + "〕</size>";
                    if (((Vector2)measure.Invoke(label, new object[] { candidate })).x > width - 12) continue;
                    rendered = candidate; break;
                }
            if (rendered != current) text.SetValue(label, rendered, null);
        }
    }
}
