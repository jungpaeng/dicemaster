using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using DiceMaster.LiveLog;
using Fungus;
using Mortal.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceMaster.Information
{
    public sealed class ChoiceStorySnapshot
    {
        public InformationRow Selected;
        public readonly List<InformationRow> Entries = new List<InformationRow>();
        public readonly List<InformationRow> Hints = new List<InformationRow>();
        public string ContextKey;
        internal MenuDialog Menu;
    }

    // Observe buttons produced by the game's own menu. The caller schedules
    // this one query; all per-frame APIs below only select already-built rows.
    public static partial class ChoiceStorySource
    {
        private sealed class Option
        {
            internal WeakReference Control;
            internal MenuDialog Menu;
            internal string Raw, Title, Kind;
            internal long Context;
        }
        private sealed class Binding
        {
            internal readonly Dictionary<string, List<MapChoiceStep>> Nodes = new Dictionary<string, List<MapChoiceStep>>(StringComparer.Ordinal);
            internal readonly List<MapStoryEntry> Stories = new List<MapStoryEntry>();
            internal readonly List<MapRoutePath> Paths = new List<MapRoutePath>();
            internal readonly Dictionary<MapRoutePath, MapStoryEntry> PathStories = new Dictionary<MapRoutePath, MapStoryEntry>();
            internal PreviewCondition CurrentPrerequisite;
            internal readonly List<MapRouteEntry> Events = new List<MapRouteEntry>();
            internal readonly List<MapScriptEntry> Scripts = new List<MapScriptEntry>();
        }
        private static readonly List<Option> options = new List<Option>();
        private static readonly Dictionary<string, Binding> bindings = new Dictionary<string, Binding>(StringComparer.Ordinal);
        private static readonly Dictionary<MapStoryEntry, int> indexed = new Dictionary<MapStoryEntry, int>();
        private static MapRouteDefinitions routes;
        private static bool routeHadFailure;
        private static int indexedChoices;
        private static readonly FieldInfo menuCount = typeof(MenuDialog).GetField("nextOptionIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static string script, source;
        private static WeakReference observedSource;
        private static long sourceContext;
        private static int generation;
        private static bool hasClock;
        private static float lastClock;
        private static double clock;
        public static Func<bool> Enabled, GuideEnabled, OneShotEnabled;
        public static Func<object, bool> Interactive, CanReadEvent;
        public static Func<long> ContextReader;
        public static Func<string, IList<MapRouteEvent>> PoolReader;
        private static bool IsEnabled { get { try { return Enabled == null || Enabled(); } catch (Exception) { return false; } } }
        private static long Context { get { return ContextReader == null ? 0 : ContextReader(); } }
        private static MenuDialog Active
        { get { var menu = MenuDialog.ActiveMenuDialog; return menu != null && menu.gameObject.activeInHierarchy ? menu : null; } }
        public static bool MenuVisible
        { get { MenuDialog menu = Active; return menu != null && menuCount != null && (int)menuCount.GetValue(menu) > 0; } }
        public static string ContextKey
        {
            get
            {
                MenuDialog menu = Active;
                return generation.ToString(CultureInfo.InvariantCulture) + "/" + Context.ToString(CultureInfo.InvariantCulture) + "/" +
                    (menu == null ? "" : menu.GetInstanceID().ToString(CultureInfo.InvariantCulture)) + "/" + script;
            }
        }
        public static bool Visible
        {
            get
            {
                MenuDialog menu = Active;
                if (!IsEnabled || menu == null || !MenuVisible) return false;
                foreach (Option option in options) if (Live(option.Control.Target, menu)) return true;
                return false;
            }
        }
        private static bool Live(object control, MenuDialog menu)
        {
            if (menu == null) return false;
            var normal = control as NormalOptionButton;
            if (normal != null) return normal.isActiveAndEnabled && normal.gameObject.activeInHierarchy && normal.transform.IsChildOf(menu.transform);
            var position = control as PositionOptionButton;
            if (position != null) return position.isActiveAndEnabled && position.gameObject.activeInHierarchy && position.transform.IsChildOf(menu.transform);
            var rest = control as BreakOptionButton;
            return rest != null && rest.isActiveAndEnabled && rest.gameObject.activeInHierarchy && rest.transform.IsChildOf(menu.transform);
        }
        private static GameObject GameObjectOf(object control)
        {
            if (control is NormalOptionButton normal) return normal.gameObject;
            if (control is PositionOptionButton position) return position.gameObject;
            if (control is BreakOptionButton rest) return rest.gameObject;
            return null;
        }
        private static object Field(object target, string field) { return LogBridge.ReadFieldStrict(target, field); }
        public static void Observe(NormalOptionButton control) { Observe(control, "일반 선택지"); }
        public static void Observe(PositionOptionButton control) { Observe(control, "장소 선택지"); }
        public static void Observe(BreakOptionButton control) { Observe(control, "대화·활동 선택지"); }
        private static void Observe(object control, string kind)
        {
            if (control == null) return;
            Option found = null;
            foreach (Option option in options) if (ReferenceEquals(option.Control.Target, control)) { found = option; break; }
            if (found == null)
            {
                for (int at = options.Count - 1; at >= 0; at--) if (options[at].Control.Target as UnityEngine.Object == null) options.RemoveAt(at);
                if (options.Count >= 256) return;
                options.Add(found = new Option { Control = new WeakReference(control), Kind = kind });
            }
            found.Raw = found.Title = null; found.Menu = null; found.Context = Context;
            generation++;
            if (IsEnabled) Capture(found, Active);
        }
        private static void Capture(Option option, MenuDialog menu)
        {
            object control = option.Control.Target;
            if (!Live(control, menu)) return;
            Text raw = Field(control, "_source") as Text;
            Text[] labels = Field(control, control is NormalOptionButton ? "_text" : "_titleText") as Text[];
            if (raw == null || labels == null) throw new DataNotReadyException();
            string value = raw.text;
            if (string.IsNullOrEmpty(value) || value.Length > 2048) return;
            string rawValue = ChoiceStoryLabels.Raw(value), title = null;
            foreach (Text label in labels)
                if (label != null && !string.IsNullOrWhiteSpace(label.text)) { title = ChoiceStoryLabels.Rendered(label.text); break; }
            if (string.IsNullOrWhiteSpace(title)) title = ChoiceStoryLabels.Resolve(rawValue);
            option.Raw = rawValue; option.Title = title; option.Menu = menu; option.Context = Context;
        }
        public static void ObserveScript(string text, string friendlyName)
        {
            const string suffix = ".LuaScript";
            if (string.IsNullOrEmpty(friendlyName) || !friendlyName.EndsWith(suffix, StringComparison.Ordinal)) return;
            string name = friendlyName.Substring(0, friendlyName.Length - suffix.Length);
            if (!LocalScript(name)) return;
            ClearGraph(); options.Clear(); script = name;
            observedSource = new WeakReference(text ?? ""); source = IsEnabled ? text ?? "" : null;
            sourceContext = Context; generation++;
        }
        // A successful Lua observation happens before its menu is built. The
        // next runtime Tick must preserve those fresh same-context captures.
        public static void ContextChanged()
        {
            long current = Context; bool changed = false;
            for (int at = options.Count - 1; at >= 0; at--)
                if (options[at].Context != current) { options.RemoveAt(at); changed = true; }
            if (sourceContext != current) { ClearGraph(); script = source = null; observedSource = null; changed = true; }
            if (changed) generation++;
        }
        private static bool LocalScript(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > 256) return false;
            foreach (char value in name)
                if (!(value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z' || value >= '0' && value <= '9' || value == '_')) return false;
            return true;
        }
        public static void MenuCleared(MenuDialog menu)
        {
            bool changed = false;
            for (int at = options.Count - 1; at >= 0; at--)
                if (ReferenceEquals(options[at].Menu, menu) || Live(options[at].Control.Target, menu)) { options.RemoveAt(at); changed = true; }
            if (changed) generation++;
        }
        private static void ClearGraph()
        { if (routes != null) routes.Clear(); routes = null; routeHadFailure = false; indexedChoices = 0; bindings.Clear(); indexed.Clear(); opportunityWork = -1; eventNames.Clear(); }
        public static void ClearSnapshots()
        { ClearGraph(); source = null; foreach (Option option in options) { option.Raw = option.Title = null; option.Menu = null; } generation++; }
        public static void Clear()
        { ClearGraph(); options.Clear(); script = source = null; observedSource = null; sourceContext = 0; hasClock = false; clock = 0; generation++; }
        public static List<DisplayQuery> Discover(bool details = false)
        {
            var queries = new List<DisplayQuery>(); MenuDialog menu = Active;
            if (!IsEnabled || menu == null || !Visible) return queries;
            foreach (Option option in options)
                if (Live(option.Control.Target, menu) && (option.Raw == null || !ReferenceEquals(option.Menu, menu))) Capture(option, menu);
            string key = ContextKey;
            queries.Add(new DisplayQuery("choice/" + key, () => {
                if (!IsEnabled || ContextKey != key || !ReferenceEquals(Active, menu)) throw new DataNotReadyException();
                return Read(menu, key, details);
            }));
            return queries;
        }
        private static float Now
        {
            get
            {
                float raw = Time.unscaledTime;
                if (!float.IsNaN(raw) && !float.IsInfinity(raw))
                { if (hasClock && raw > lastClock) clock += raw - lastClock; hasClock = true; lastClock = raw; }
                return (float)clock;
            }
        }
        private static ChoiceStorySnapshot Read(MenuDialog menu, string key, bool details)
        {
            var snapshot = new ChoiceStorySnapshot { ContextKey = key, Menu = menu };
            if (string.IsNullOrEmpty(script) || sourceContext != Context)
            { snapshot.Entries.Add(Status("현재 선택 화면의 이야기 자료 대기 중")); return snapshot; }
            if (source == null)
            {
                source = observedSource == null ? null : observedSource.Target as string;
                if (source == null)
                {
                    ScriptReadResult read = DataHelper.ReadPreviewScript(script);
                    if (read == null || read.State != ScriptReadState.Success) throw new DataNotReadyException();
                    source = read.Text ?? "";
                }
            }
            if (routes == null)
            {
                string root = script, body = source;
                routes = new MapRouteDefinitions(root, name => string.Equals(name, root, StringComparison.OrdinalIgnoreCase) ? ScriptReadResult.Success(body) : DataHelper.ReadPreviewScript(name),
                    name => PoolReader == null ? null : PoolReader(name)) { UseSharedSyntax = true };
            }
            var preparation = new MapRouteDefinitions.PreparationSlice();
            routes.AdvanceBudgeted(Now, 8192, preparation.ShouldYield);
            if (routes.HasReadFailure != routeHadFailure)
            {
                routeHadFailure = routes.HasReadFailure;
                DicePlugin.LogWarning("Information choice-story target=" + script + (routeHadFailure ? " failure · retry=5s" : " recovered"));
            }
            bool indexing = Index();
            var current = new MapRouteRead(); int classificationBudget = 32;
            foreach (Option option in options)
            {
                if (!Feature(GuideEnabled)) break;
                if (option.Raw == null || option.Context != Context || !ReferenceEquals(option.Menu, menu) || !Live(option.Control.Target, menu)) continue;
                Binding binding;
                if (!bindings.TryGetValue(option.Raw, out binding) || binding.Stories.Count == 0) continue;
                // Keep oversized/unresolved bindings conservative. A current
                // lookup never scans an unbounded collection to prove absence.
                if (binding.Paths.Count <= 32 && !routes.IsPreparing && !routes.HasReadFailure && !indexing)
                {
                    Binding storyBinding = new Binding { CurrentPrerequisite = binding.CurrentPrerequisite };
                    foreach (MapRoutePath path in binding.Paths)
                        if (path.ReadStoryKind(current, ref classificationBudget) != MapStoryKind.ActivityResponse)
                        {
                            MapStoryEntry story;
                            binding.PathStories.TryGetValue(path, out story);
                            storyBinding.Paths.Add(path); storyBinding.PathStories.Add(path, story);
                            if (story != null && !storyBinding.Stories.Contains(story)) storyBinding.Stories.Add(story);
                        }
                    if (storyBinding.Paths.Count == 0)
                    {
                        MapChoiceStep first = binding.Paths.Count == 0 || binding.Paths[0].Steps.Count == 0 ? null : binding.Paths[0].Steps[0];
                        var observedTopics = new List<MapChoiceStep>();
                        foreach (MapRoutePath path in binding.Paths)
                            foreach (MapChoiceStep step in path.Steps) if (!observedTopics.Contains(step)) observedTopics.Add(step);
                        if (first != null && routes.CurrentOmissionsBlocked(first, observedTopics, current, ref classificationBudget)) continue;
                        // An unread or unsupported followup can still contain
                        // a distinct story beyond the observed fixed response.
                    }
                    else
                    {
                        foreach (var node in binding.Nodes) storyBinding.Nodes.Add(node.Key, node.Value);
                        binding = storyBinding;
                    }
                }
                bool ambiguous = binding.Nodes.Count != 1;
                bool incomplete = routes.IsPreparing || routes.HasReadFailure || routes.HasIncompleteStoryPaths || indexing;
                List<MapChoiceStep> alternatives = null;
                if (!ambiguous) foreach (var node in binding.Nodes.Values) { alternatives = node; break; }
                // Only a current, uniquely matched required option guard can
                // reject a path across unrelated downstream uncertainty. A
                // true guard never upgrades the whole story to available.
                bool blocked = !ambiguous && !routes.IsPreparing && !indexing && CurrentOptionBlocked(alternatives, current);
                bool currentRoot = !ambiguous && !routes.IsPreparing && !indexing && alternatives.Count == 1 && alternatives[0].HasCurrentRootSnapshot;
                bool storyBlocked = currentRoot && binding.CurrentPrerequisite != null && current.Current(binding.CurrentPrerequisite) == PreviewConditionState.NotMet;
                StorySelection selection = storyBlocked ? new StorySelection { State = MapRouteState.NotMet } :
                    SelectStory(binding.Paths, binding.PathStories, current, currentRoot, incomplete, ambiguous || routes.IsPreparing || indexing, option.Raw);
                MapRouteState state = blocked ? MapRouteState.NotMet : selection.State;
                if (state == MapRouteState.NotMet && !details) continue;
                string status = state == MapRouteState.Met ? "진행 가능" : state == MapRouteState.NotMet ? "지금 진행 불가" : "진행 가능성 있음";
                string detail = (ambiguous || incomplete ? "현재 메뉴 원문과 일치하는 연결 후보입니다. 현재 메뉴의 스크립트 위치는 아직 확정하지 않았습니다." : "실제 현재 메뉴의 원본 선택지와 정확히 연결한 자료입니다.") +
                    " 표시만 수행하며 선택·조건 판정·게임 진행을 실행하지 않습니다.\n원본 선택지: " + option.Raw;
                if (binding.Nodes.Count != 1) detail += "\n같은 스크립트에 동일한 원문 선택지가 여러 번 있어 현재 메뉴의 위치를 안전하게 구분하지 못했습니다.";
                else if (incomplete) detail += "\n경로를 전부 확인하지 못해 같은 이름의 다른 선택 화면이 없는지 아직 확정하지 않았습니다.";
                detail += "\n연결 이야기의 내부 분기 조건: " + (state == MapRouteState.Met ? "현재 값으로 충족" : state == MapRouteState.NotMet ? "현재 값으로 미충족" : "안전하게 확정하지 못함");
                if (blocked) detail += "\n현재 선택지의 필수 조건이 미충족입니다. 이후 경로의 불확실성과 구분한 현재 조건 확인 결과입니다.";
                if (storyBlocked) detail += "\n관찰한 이야기 경로의 공통 필수 조건이 미충족입니다. 후속 경로의 불확실성과 구분한 현재 조건 확인 결과입니다.";
                detail += "\n진행 가능성 있음은 확률을 뜻하지 않습니다. 선행 상태 변경·지원하지 않는 조건·경로 누락은 충족으로 간주하지 않으며 실제 이야기 발생을 보장하지 않습니다.";
                if (details)
                {
                    int shown = 0;
                    foreach (MapRoutePath path in binding.Paths)
                    { if (shown++ >= 32) { detail += "\n이후 경로는 일부 확인하지 않았습니다."; break; } detail += "\n연결 경로: " + ChoiceStoryLabels.Path(path.Steps); }
                }
                var anchor = option.Control.Target as UnityEngine.Object;
                string preview = ambiguous || selection.Path == null ? ChoiceStoryLabels.ShortName(option.Title) + " · 이야기 연결 확인 필요" : (incomplete ? "경로 후보: " : "") + PreviewPath(option, selection.Path);
                var row = new InformationRow("choice/" + key + "/" + (anchor == null ? "" : anchor.GetInstanceID().ToString(CultureInfo.InvariantCulture)),
                    option.Title, "인물 대화", status, detail,
                    state == MapRouteState.NotMet ? "" : preview + (!ambiguous && !incomplete && binding.Paths.Count <= 32 && selection.Count > 1 ? " · 외 " + (selection.Count - 1) + "개" : "") + " · " + status,
                    anchor, !ambiguous && !incomplete && state == MapRouteState.Met);
                snapshot.Entries.Add(row);
                if (row.AutoText.Length != 0 && snapshot.Hints.Count < 3) snapshot.Hints.Add(row);
            }
            AppendOpportunities(snapshot, current, indexing, details, preparation);
            if (routes.IsPreparing || indexing || routes.IsPartial || routes.HasReadFailure)
                snapshot.Entries.Add(Status(routes.HasReadFailure ? "이야기 연결 자료 읽기 대기/실패 · 5초 뒤 재시도" : routes.IsPreparing || indexing ? "이야기 안내 준비 중" : "이야기 연결 일부 확인 필요"));
            snapshot.Selected = CurrentRow(snapshot);
            return snapshot;
        }
        private static InformationRow Status(string text)
        { return new InformationRow("choice/status/" + ContextKey, "선택 화면 안내", "자료 상태", text, text, text); }
        private static bool Index()
        {
            int budget = 64; bool pending = false;
            while (indexedChoices < routes.ChoiceSteps.Count)
            {
                if (budget-- <= 0) { pending = true; break; }
                Bind(routes.ChoiceSteps[indexedChoices++]);
            }
            foreach (MapStoryEntry story in routes.StoryEntries)
            {
                int at; indexed.TryGetValue(story, out at);
                while (at < story.Paths.Count)
                {
                    if (budget-- <= 0) { pending = true; break; }
                    MapRoutePath path = story.Paths[at++];
                    if (string.IsNullOrEmpty(path.DialogueCharacter)) continue;
                    foreach (MapChoiceStep step in path.Steps)
                    {
                        if (!string.Equals(step.Script, script, StringComparison.OrdinalIgnoreCase)) continue;
                        Binding binding = Bind(step);
                        if (!binding.Paths.Contains(path)) AddStoryPath(binding, path, story);
                    }
                }
                indexed[story] = at;
            }
            return pending;
        }
        private static void AddStoryPath(Binding binding, MapRoutePath path, MapStoryEntry story)
        {
            // Definition-only intersection, prepared under Index's existing
            // budget. A shared false prerequisite rejects even a large set
            // without raising the 32-path current-read limit.
            binding.CurrentPrerequisite = binding.Paths.Count == 0 ? path.CurrentPrerequisite :
                MapRoutePath.CommonPrerequisite(binding.CurrentPrerequisite, path.CurrentPrerequisite);
            binding.Paths.Add(path); binding.PathStories.Add(path, story);
            if (story != null && !binding.Stories.Contains(story)) binding.Stories.Add(story);
        }
        private static Binding Bind(MapChoiceStep step)
        {
            if (!string.Equals(step.Script, script, StringComparison.OrdinalIgnoreCase)) return null;
            string raw = ChoiceStoryLabels.Raw(step.RawLabel);
            Binding binding;
            if (!bindings.TryGetValue(raw, out binding)) bindings.Add(raw, binding = new Binding());
            string identity = step.Script + "/" + step.SourceOffset + "/" + step.Index;
            List<MapChoiceStep> alternatives;
            if (!binding.Nodes.TryGetValue(identity, out alternatives)) binding.Nodes.Add(identity, alternatives = new List<MapChoiceStep>());
            if (!alternatives.Contains(step)) alternatives.Add(step);
            return binding;
        }
        internal static MapRouteState CurrentStoryConditions(IList<MapRoutePath> paths, MapRouteRead current)
        { return SelectStory(paths, null, current, false, false, false).State; }
        private struct StorySelection
        {
            internal MapRouteState State;
            internal MapRoutePath Path;
            internal int Count;
        }
        private static StorySelection SelectStory(IList<MapRoutePath> paths, IDictionary<MapRoutePath, MapStoryEntry> pathStories,
            MapRouteRead current, bool currentRoot, bool incomplete, bool unresolved, string raw = null)
        {
            int count = paths == null ? 0 : paths.Count;
            var selection = new StorySelection { State = count == 0 || count > 32 ? MapRouteState.Unknown : MapRouteState.NotMet };
            MapStoryEntry first = null; HashSet<MapStoryEntry> counted = null; bool followup = false;
            for (int at = 0; at < Math.Min(32, count); at++)
            {
                MapRoutePath path = paths[at];
                // Only a uniquely matched, closed root menu may reuse the
                // map's negative projection. Inputs affected by future writes
                // are already masked in that projection; earlier captured
                // menu guards must never be reinterpreted after a write.
                MapRouteState state = currentRoot && path.ReadCurrentBlocker(current) ? MapRouteState.NotMet :
                    unresolved || path.StoryConditionsMayHaveChanged ? MapRouteState.Unknown : path.ReadStoryConditions(current);
                if (state == MapRouteState.NotMet) continue;
                if (incomplete) state = MapRouteState.Unknown;
                MapStoryEntry story = null; if (pathStories != null) pathStories.TryGetValue(path, out story);
                if (selection.Path == null || state == MapRouteState.Met && selection.State != MapRouteState.Met)
                {
                    selection.Path = path; selection.State = state; selection.Count = 1; first = story; counted = null;
                    followup = HasFollowingChoice(path, raw);
                }
                else if (state == selection.State)
                {
                    // Preserve the useful next-topic preview, but only among
                    // witnesses with this same current condition state.
                    if (!followup && HasFollowingChoice(path, raw)) { selection.Path = path; followup = true; }
                    if (story != null && !ReferenceEquals(first, story))
                    {
                        if (counted == null) counted = new HashSet<MapStoryEntry> { first };
                        if (counted.Add(story)) selection.Count++;
                    }
                }
            }
            return selection;
        }
        private static bool HasFollowingChoice(MapRoutePath path, string raw)
        {
            if (raw == null) return false;
            bool found = false;
            foreach (MapChoiceStep step in path.Steps)
            {
                if (found) return true;
                found = string.Equals(step.Script, script, StringComparison.OrdinalIgnoreCase) && ChoiceStoryLabels.Raw(step.RawLabel) == raw;
            }
            return false;
        }
        private static bool CurrentOptionBlocked(IList<MapChoiceStep> alternatives, MapRouteRead current)
        {
            if (alternatives == null || alternatives.Count == 0 || alternatives.Count > 32) return false;
            foreach (MapChoiceStep step in alternatives) if (!step.ReadCurrentBlocker(current)) return false;
            return true;
        }
        internal static MapRouteState CurrentConditions(IList<MapChoiceStep> alternatives, MapRouteRead current)
        {
            bool unknown = alternatives == null || alternatives.Count == 0 || alternatives.Count > 32;
            if (alternatives != null) for (int at = 0; at < Math.Min(32, alternatives.Count); at++)
            {
                // The same menu slot may have different definitions in two
                // branches. Its own guard alone cannot select that branch.
                MapRouteState state = alternatives.Count == 1 ? alternatives[at].ReadConditions(current) : MapRoutePath.State(current.Current(alternatives[at].Gate));
                if (state == MapRouteState.Met) return state;
                unknown |= state == MapRouteState.Unknown;
            }
            return unknown ? MapRouteState.Unknown : MapRouteState.NotMet;
        }
        private static string PreviewPath(Option option, MapRoutePath path)
        {
            // Use one supported future path and complete labels. Dialogue
            // sentences are never invented as story titles.
            string fallback = ChoiceStoryLabels.ShortName(option.Title) + " · 인물 대화";
            var labels = new List<string>(); bool started = false;
            foreach (MapChoiceStep step in path.Steps)
            {
                if (!started)
                {
                    if (!string.Equals(step.Script, script, StringComparison.OrdinalIgnoreCase) || ChoiceStoryLabels.Raw(step.RawLabel) != option.Raw) continue;
                    started = true; labels.Add(ChoiceStoryLabels.ShortName(option.Title)); continue;
                }
                if (labels.Count == 3) { labels.Add("이후 선택은 상세 확인"); break; }
                labels.Add(ChoiceStoryLabels.ShortName(ChoiceStoryLabels.Resolve(step.RawLabel)));
            }
            return started ? string.Join(" → ", labels) + " · " + ChoiceStoryLabels.DialogueTarget(path.DialogueCharacter) : fallback;
        }
        public static bool IsCurrent(ChoiceStorySnapshot snapshot)
        { return snapshot != null && Visible && ReferenceEquals(snapshot.Menu, Active) && snapshot.ContextKey == ContextKey; }
        public static InformationRow CurrentRow(ChoiceStorySnapshot snapshot)
        {
            if (!IsCurrent(snapshot)) return null;
            EventSystem system = EventSystem.current;
            GameObject selected = system == null ? null : system.currentSelectedGameObject;
            if (selected == null) return null;
            foreach (InformationRow row in snapshot.Entries)
                if (!string.IsNullOrEmpty(row.AutoText) && row.Anchor != null && Live(row.Anchor, snapshot.Menu) && ReferenceEquals(GameObjectOf(row.Anchor), selected)) return row;
            InformationRow nested = null;
            foreach (InformationRow row in snapshot.Entries)
            {
                if (string.IsNullOrEmpty(row.AutoText)) continue;
                GameObject anchor = GameObjectOf(row.Anchor);
                if (anchor == null || !Live(row.Anchor, snapshot.Menu) || !selected.transform.IsChildOf(anchor.transform)) continue;
                if (nested != null) return null;
                nested = row;
            }
            return nested;
        }
    }
}
