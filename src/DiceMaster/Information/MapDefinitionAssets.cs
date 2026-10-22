using System;
using System.Collections.Generic;
using DiceMaster.LiveLog;
using Mortal.Core;
using Mortal.Story;
using UnityEngine;

namespace DiceMaster.Information
{
    // Prefer already loaded definitions. Missing route metadata may use the
    // fingerprint-checked installation reader; no Resources.Load, prefab
    // instantiation, scene/position discovery, initializer or game check runs.
    internal static class MapDefinitionAssets
    {
        private sealed class Cache
        {
            internal object[] Values;
            internal float RetryAt;
            internal bool Attempted;
            internal bool Blocked;
        }
        private static readonly Cache[] caches = { new Cache(), new Cache(), new Cache(), new Cache() };
        private static int searches;
        internal static bool IsPreparing { get { return MapFileDefinitions.IsPreparing; } }
        internal static void Install()
        { PreviewConditions.LoadedDefinitionReader = Read; PreviewConditions.ExistingOwnerAlive = Alive; }
        internal static bool Alive(object value)
        { return value is UnityEngine.Object native && native != null; }
        internal static void Clear()
        { foreach (var cache in caches) { cache.Values = null; cache.Attempted = cache.Blocked = false; cache.RetryAt = 0; } MapFileDefinitions.Clear(); }
        internal static void Observe(object missions, object player) { MapFileDefinitions.Observe(missions, player); }
        internal static void SetVisible(bool visible) { if (!visible) MapFileDefinitions.Suspend(); }
        internal static string Describe()
        {
            string Status(int at)
            {
                Cache cache = caches[at];
                return cache.Values != null && cache.Values.Length != 0 ? cache.Values.Length + "개" : cache.Attempted ? "미확인 · 재시도 대기" : "미조회";
            }
            return "로드된 설정 자산 · 조건: " + Status(0) + " / 분기: " + Status(1) + " / 이벤트 풀: " + Status(2) + " / 대화 초기화: " + Status(3) +
                "\n누적 자료 검색 " + searches + "회 · 정상 주기 전체 검색 없음\n파일 정의: " + MapFileDefinitions.Describe();
        }
        private static object[] Objects<T>() where T : UnityEngine.Object
        {
            T[] assets = Resources.FindObjectsOfTypeAll<T>();
            if (assets == null || assets.Length > 64) throw new InvalidOperationException("Definition asset limit.");
            var result = new List<object>(assets.Length);
            foreach (var asset in assets)
            {
                if (asset == null) continue;
                if (asset.GetType() != typeof(T)) throw new InvalidOperationException("Unsupported definition asset subclass.");
                result.Add(asset);
            }
            return result.ToArray();
        }
        private static object[] Read(PreviewDefinitionKind kind)
        {
            int at = (int)kind;
            if (at < 0 || at >= caches.Length) return null;
            Cache cache = caches[at];
            if (cache.Values != null && cache.Values.Length != 0)
            {
                bool valid = true; foreach (object value in cache.Values) valid &= Alive(value);
                if (valid) return cache.Values;
                cache.Values = null; cache.Attempted = false;
            }
            float now = Time.unscaledTime;
            if (cache.Attempted && now < cache.RetryAt) return cache.Blocked ? null : MapFileDefinitions.Read(kind);
            cache.Attempted = true; cache.Blocked = false; cache.RetryAt = now + 5;
            try
            {
                searches++;
                if (kind == PreviewDefinitionKind.Conditions) cache.Values = Objects<ConditionResultConfig>();
                else if (kind == PreviewDefinitionKind.Switches) cache.Values = Objects<SwitchResultConfig>();
                else if (kind == PreviewDefinitionKind.Positions) cache.Values = Objects<PositionResultConfig>();
                else
                {
                    // Read the game's actual serialized reset footprint, never
                    // infer it from NPC names, flag prefixes or all flag assets.
                    object collection = null;
                    foreach (object owner in Objects<StatModifyManager>())
                    {
                        object candidate = LogBridge.ReadFieldStrict(owner, "_talkOptionFlag");
                        if (!(candidate is FlagCollectionData) || !Alive(candidate)) { cache.Blocked = true; return null; }
                        if (collection != null && !ReferenceEquals(collection, candidate)) { cache.Blocked = true; return null; }
                        collection = candidate;
                    }
                    cache.Values = collection == null ? null : new[] { collection };
                }
                return cache.Values != null && cache.Values.Length != 0 ? cache.Values : MapFileDefinitions.Read(kind);
            }
            catch (Exception) { cache.Values = null; cache.Blocked = true; return null; }
        }
    }
}
