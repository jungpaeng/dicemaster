using System;
using System.Collections.Generic;
using DiceMaster.Config;
using DiceMaster.EventSelection;
using DiceMaster.LiveLog;
using DiceMaster.LogUI;
using HarmonyLib;
using Mortal.Story;
using Mortal.Core;
using Fungus;
using UnityEngine;

namespace DiceMaster.Information
{
    // Owns display scheduling only. Providers never receive input/action authority.
    public static partial class InformationRuntime
    {
        private sealed class Page
        {
            internal readonly RefreshEngine Engine = new RefreshEngine();
            internal string Context;
            internal List<DisplayRow> Rows = new List<DisplayRow>();
            internal bool Visible;
        }
        private static readonly Page map = new Page(), shop = new Page(), branch = new Page(), hints = new Page(), choices = new Page(), markers = new Page();
        private static readonly bool[] enabled = new bool[4], ready = new bool[4];
        private static readonly Dictionary<string,string> failures = new Dictionary<string,string>();
                private static bool parent;
        private static long luaContext = -1;
        private static readonly string[] names = { "지도 안내", "1회성 표시", "분기 알림·기록", "상점 효과" };
        public static bool Enabled(int index) => index >= 0 && index < 4 && enabled[index];
        private static ConfigElement<bool> Setting(int index) => index == 0 ? ConfigManager.EnableMapGuide :
            index == 1 ? ConfigManager.EnableOneShotHints : index == 2 ? ConfigManager.EnableBranchNotices : ConfigManager.EnableShopEffectInfo;
        public static void Install(Harmony harmony)
        {
            // EnableOptionInfo is deliberately latched at startup (restart contract).
            parent = ConfigManager.EnableOptionInfo != null && ConfigManager.EnableOptionInfo.Value;
            if (!parent) { Array.Clear(enabled, 0, enabled.Length); BranchHistory.Enabled = false; Clear(); return; }
            BranchHistory.Cleared = () => { Reset(branch); Publish(branch, "분기 기록"); };
            OptionHintSource.Enabled = () => Enabled(1);
            MapGuideSource.Enabled = () => Enabled(0) || Enabled(1);
            MapGuideSource.MarkerVisible = InformationAnchors.IsVisible;
            ChoiceStorySource.Enabled = () => Enabled(0) || Enabled(1);
            ChoiceStorySource.GuideEnabled = () => Enabled(0);
            ChoiceStorySource.OneShotEnabled = () => Enabled(1);
            ChoiceStorySource.Interactive = InformationAnchors.IsInteractive;
            ChoiceStorySource.CanReadEvent = MapGuideSource.SmallEventGraph;
            ChoiceStorySource.PoolReader = MapGuideSource.ReadChoicePool;
            ShopEffectSource.Enabled = () => Enabled(3);
            try { MapInformationPatches.Install(harmony); ready[0] = ready[1] = true; }
            catch (Exception error) { Failure("지도 후킹", error); }
            try { ChoiceStoryPatches.Install(harmony); }
            catch (Exception error) { ready[0] = false; Failure("선택 경로 후킹", error); }
            try { ShopInformationPatches.Install(harmony); ready[3] = true; }
            catch (Exception error) { Failure("상점 후킹", error); }
            try { BranchObservationPatches.Install(harmony); ready[2] = true; }
            catch (Exception error) { Failure("분기 후킹", error); }
            try { OptionInformationPatches.Install(harmony); InformationLifecyclePatches.Install(harmony); }
            catch (Exception error) { ready[1] = false; Failure("선택지 후킹", error); }
            RefreshSettings();
        }
        public static string SettingLabel(int index)
        {
            if (index < 0 || index > 3) return "";
            return names[index] + ": " + (Enabled(index) ? "ON" : "OFF") +
                (!parent ? " · 상위 정보 설정 꺼짐 (재시작 필요)" : !ready[index] ? " · 연결 실패" :
                failures.ContainsKey("설정 " + names[index]) ? " · 설정 읽기 실패" : "");
        }
        public static void Toggle(int index)
        {
            if (index < 0 || index > 3) return;
            try
            {
                var setting = Setting(index);
                if (setting == null) throw new InvalidOperationException("Missing setting");
                setting.Value = !ConfigManager.GetConfigValue(setting);
            }
            catch (Exception error) { Failure("설정 " + names[index], error); }
            RefreshSettings();
        }
        public static void RefreshSettings()
        {
            for (int index = 0; index < 4; index++)
            {
                bool value = false;
                try
                {
                    var setting = Setting(index);
                    if (setting == null) throw new InvalidOperationException("Missing setting");
                    value = parent && ready[index] && ConfigManager.GetConfigValue(setting);
                    Recover("설정 " + names[index]);
                }
                catch (Exception error) { Failure("설정 " + names[index], error); }
                if (enabled[index] && !value)
                {
                    if (index < 2) Reset(markers);
                    if (index == 0) { Reset(map); Reset(choices); ChoiceStorySource.ClearSnapshots(); }
                    if (index == 1) { Reset(hints); Reset(choices); ChoiceStorySource.ClearSnapshots(); OptionHintSource.Clear(); OneShotProof.Clear(); }
                    if (index == 2) { BranchHistory.Clear(); Reset(branch); }
                    if (index == 3) { ShopEffectSource.Clear(); Reset(shop); }
                }
                enabled[index] = value;
            }
            BranchHistory.Enabled = enabled[2];
            if (!enabled[0] && !enabled[1]) MapGuideSource.ClearSnapshots();
        }
        private static void Reset(Page page)
        { page.Engine.ResetContext(); page.Rows.Clear(); page.Context = null; page.Visible = false; }
        public static void Clear()
        {
            Reset(map); Reset(shop); Reset(branch); Reset(hints); Reset(choices); Reset(markers); OptionHintSource.Clear(); ChoiceStorySource.Clear();
            MapGuideSource.Clear(); ShopEffectSource.Clear(); BranchHistory.Clear(); OneShotProof.Clear();
        }
        public static void Tick(string detailPage, bool focused, float now, bool manual = false, bool automaticVisible = true, bool popupVisible = false)
        {
            try { TickCore(detailPage, focused, now, manual, automaticVisible, popupVisible); }
            catch (Exception error) { Failure("정보 갱신 경계", error); }
        }
        private static void TickCore(string detailPage, bool focused, float now, bool manual, bool automaticVisible, bool popupVisible)
        {
            RefreshSettings();
            if (luaContext != LogBridge.Model.ContextId)
            {
                luaContext = LogBridge.Model.ContextId;
                OptionHintSource.Clear(); OneShotProof.Clear(); Reset(hints);
                ChoiceStorySource.ContextChanged(); Reset(choices);
                MapGuideSource.ClearSnapshotsPreservingSyntax(); Reset(map); Reset(markers);
                // Scene/load patches clear history separately; ordinary Lua keeps it.
            }
            bool choiceVisible = (enabled[0] || enabled[1]) && ChoiceStorySource.Visible;
            bool menuVisible = ChoiceStorySource.MenuVisible;
            bool mapVisible = focused && automaticVisible && !menuVisible && MapGuideSource.Visible;
            bool mapDetails = detailPage == "지도 안내";
            Run(map, "지도 안내", MapKey(mapDetails),
                !menuVisible && (enabled[0] || enabled[1]) && focused && (mapVisible || detailPage == "지도 안내"), now,
                () => MapGuideSource.Discover(enabled[0], enabled[1], mapDetails), manual, () => MapKey(mapDetails));
            Run(choices, "선택 경로", ChoiceKey(mapDetails),
                choiceVisible && focused && (automaticVisible || mapDetails), now,
                () => ChoiceStorySource.Discover(mapDetails), manual,
                () => ChoiceKey(mapDetails));
            Run(markers, "지도 표식", MarkerKey(),
                (enabled[0] || enabled[1]) && focused && automaticVisible && !popupVisible && !menuVisible && MapGuideSource.MarkerSessionVisible, now,
                () => MapGuideSource.DiscoverMarkers(enabled[0], enabled[1]), manual, MarkerKey);
            Run(shop, "상점 효과", ShopEffectSource.ContextKey,
                enabled[3] && focused && (automaticVisible && ShopEffectSource.Visible || detailPage == "상점 효과"), now,
                ShopEffectSource.Discover, manual);
            if (enabled[3])
            {
                string summary = "";
                if (ShopEffectSource.Visible && shop.Rows.Count > 0)
                {
                    DisplayRow row = shop.Rows[0];
                    summary = (row.Value as InformationRow)?.AutoText ?? "상점 효과 · " + row.Status;
                    if (!string.IsNullOrEmpty(row.Status)) summary = row.Status + "\n" + summary;
                }
                ShopEffectSource.RenderSummary(summary);
            }
            Run(branch, "분기 기록", BranchHistory.ContextKey, enabled[2] && focused && detailPage == "분기 기록", now,
                BranchHistory.Discover, manual);
            // One-shot proofs are evaluated only for visible option requests and popup rows.
            bool popup = focused && popupVisible && EventSelectionRuntime.Waiting;
            Run(hints, "1회성", luaContext + ":" + EventSelectionRuntime.Token + ":" + OptionHintSource.ContextKey,
                enabled[1] && focused && (popup || automaticVisible && OptionHintSource.Visible), now, () =>
                {
                    var queries = new List<DisplayQuery>();
                    if (popup && !EventSelectionRuntime.IsSwitch)
                        foreach (var candidate in EventSelectionRuntime.Candidates)
                        {
                            var item = candidate;
                            queries.Add(new DisplayQuery("event:" + item.Id, () => ReadProof(OneShotProof.ForEvent(item.Target, item.Result))));
                        }
                    if (automaticVisible && OptionHintSource.Visible) queries.AddRange(OptionHintSource.Discover());
                    return queries;
                }, manual);
            if (enabled[1] && automaticVisible) OptionHintSource.RenderHints();
        }
        private static object ReadProof(OneShotResult proof)
        {
            if (proof.Failed) throw new InvalidOperationException("One-shot proof read failed");
            return proof;
        }
        private static string MapKey(bool details) => MapGuideSource.ContextKey + ":" + enabled[0] + ":" + enabled[1] + ":" + details + ":" + ChoiceStorySource.MenuVisible + ":" + ChoiceStorySource.ContextKey;
        private static string MarkerKey() => MapGuideSource.MarkerContextKey + ":" + enabled[0] + ":" + enabled[1] + ":" + luaContext;
        private static string ChoiceKey(bool details) => ChoiceStorySource.ContextKey + ":" + enabled[0] + ":" + enabled[1] + ":" + details;
        private static string ModelTitle(string title) => title == "선택 경로" ? "지도 안내" : title;
        private static void Run(Page page, string title, string key, bool visible, float now,
            Func<List<DisplayQuery>> discover, bool manual, Func<string> currentKey = null)
        {
            if (page.Context != key)
            { Reset(page); page.Context = key; Publish(page, title); }
            page.Visible = visible;
            page.Engine.Tick(title, visible, true, now, false, manual, discover, rows =>
            {
                string current = currentKey != null ? currentKey() :
                    title == "상점 효과" ? ShopEffectSource.ContextKey : title == "분기 기록" ? BranchHistory.ContextKey : key;
                if (current != key) { Reset(page); page.Context = current; Publish(page, title); return; }
                page.Rows = rows; Publish(page, title);
            }, message => DicePlugin.LogInfo("Information " + message));
            // The shared log model can receive a new Lua context between healthy ticks.
            if (title != "지도 표식" && !LogBridge.Model.Tab(ModelTitle(title)).HasPublished) Publish(page, title);
        }
        private static void Publish(Page page, string title)
        {
            if (title == "1회성" || title == "지도 표식") return;
            // Both contexts use the existing map tool, with only the currently
            // visible owner publishing. A hidden map must not erase menu rows.
            if (title == "지도 안내" && (Enabled(0) || Enabled(1)) && ChoiceStorySource.Visible) return;
            if (title == "선택 경로" && (!(Enabled(0) || Enabled(1)) || !ChoiceStorySource.Visible)) return;
            var rows = new List<LogItem>();
            foreach (DisplayRow source in page.Rows)
            {
                var choice = source.Value as ChoiceStorySnapshot;
                if (title == "선택 경로" && choice != null)
                {
                    foreach (InformationRow entry in choice.Entries) AddPublishedRow(rows, source, entry);
                    continue;
                }
                var snapshot = source.Value as MapGuideSnapshot;
                if (title == "지도 안내" && snapshot != null)
                {
                    foreach (InformationRow entry in snapshot.Entries) AddPublishedRow(rows, source, entry);
                    if (snapshot.Entries.Count == 0 && snapshot.Summary != null) AddPublishedRow(rows, source, snapshot.Summary);
                    continue;
                }
                var value = source.Value as InformationRow;
                AddPublishedRow(rows, source, value);
            }
            if (rows.Count == 0)
            {
                bool on = title == "선택 경로" ? Enabled(0) || Enabled(1) : title == "지도 안내" ? Enabled(0) || Enabled(1) : title == "상점 효과" ? Enabled(3) : Enabled(2);
                bool waiting = on && (title == "선택 경로" ? ChoiceStorySource.Visible : title == "지도 안내" ? MapGuideSource.Visible || Enabled(0) && MapGuideSource.SessionVisible : title == "상점 효과" && ShopEffectSource.Visible);
                rows.Add(new LogItem { Id = "__empty__", Title = !on ? "기능 OFF" : waiting ? RefreshEngine.WaitingStatus :
                    title == "지도 안내" ? "현재 지도 장소 없음" : title == "상점 효과" ? "현재 선택한 상점 물품 없음" : "실제 분기 기록 없음",
                    Description = "현재 문맥의 자료만 조회합니다. 정보 조회는 게임 진행·예약·구매·사용을 실행하지 않습니다.",
                    ReservationAllowed = false, RefreshState = waiting ? LogRefreshState.Waiting : LogRefreshState.Healthy });
            }
            LogBridge.Model.Publish(new LogSnapshot(LogBridge.Model.ContextId, ModelTitle(title), rows));
        }
        private static void AddPublishedRow(List<LogItem> rows, DisplayRow source, InformationRow value)
        {
            if (value == null && source.Value is MapGuideSnapshot) return;
            rows.Add(new LogItem { Id = value?.Id ?? source.Id, RawId = value?.Id ?? source.Id,
                    Title = value?.Title ?? (source.Status ?? "조회 대기"),
                    Text = value == null ? "" : value.Kind + " · " + value.Status,
                    Description = value?.Detail ?? "", Target = null, ReservationAllowed = false,
                    RefreshStatus = source.Status ?? "", HasLastGood = value != null,
                    RefreshState = string.IsNullOrEmpty(source.Status) ? LogRefreshState.Healthy :
                        source.Status == RefreshEngine.WaitingStatus ? LogRefreshState.Waiting :
                        value == null ? LogRefreshState.ReadFailure : LogRefreshState.RetainedFailure });
        }
        internal static void Failure(string key, Exception error)
        {
            string cause = error.GetType().FullName;
            string previous;
            if (failures.TryGetValue(key, out previous) && previous == cause) return;
            failures[key] = cause; DicePlugin.LogWarning("Information " + key + " · " + cause);
        }
        private static void Recover(string key)
        { if (failures.Remove(key)) DicePlugin.LogInfo("Information 복구 · " + key); }
        public static string EventTag(object item) => Enabled(1) ? OneShotProof.CachedEventTag(item) : "";
        public static string OptionTag(string key) => Enabled(1) ? OneShotProof.CachedOptionTag(key) : "";
        public static string EventDetail(object item) => Enabled(1) ? OneShotProof.CachedEventDetail(item) : "";
        public static string OptionDetail(string key) => Enabled(1) ? OneShotProof.CachedOptionDetail(key) : "";
        internal static void ResetDisplays()
        {
            Reset(map); Reset(shop); Reset(hints); Reset(choices); Reset(markers);
            MapGuideSource.ClearSnapshots(); ShopEffectSource.Clear(); OptionHintSource.Clear(); OneShotProof.Clear(); ChoiceStorySource.Clear();
            Publish(map, "지도 안내"); Publish(shop, "상점 효과");
        }
    }
}
