using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DiceMaster.LiveLog;
using Mortal.Story;
using UnityEngine;

namespace DiceMaster.Information
{
    internal static class MapFileDefinitions
    {
        private sealed class Result
        { internal object[][] Roots; internal string Failure; internal bool Permanent; internal long Bytes; internal double Milliseconds; }
        private static object missions, player;
        private static Task<Result> pending;
        private static CancellationTokenSource cancellation;
        private static Result ready;
        private static float retryAt;
        // Test seam; normal code resolves only the actual loaded assembly's
        // installation folder. No working-directory guesses or config changes.
        internal static Func<string> DirectoryReader = InstallationDirectory;
        private static string InstallationDirectory()
        {
            string managed = Path.GetDirectoryName(typeof(CheckPointManager).Assembly.Location);
            return string.Equals(Path.GetFileName(managed), "Managed", StringComparison.OrdinalIgnoreCase) ? Path.GetDirectoryName(managed) : null;
        }
        internal static void Observe(object currentMissions, object currentPlayer)
        {
            if (ReferenceEquals(missions, currentMissions) && ReferenceEquals(player, currentPlayer)) return;
            Clear(); missions = currentMissions; player = currentPlayer;
        }
        internal static void Clear()
        {
            cancellation?.Cancel(); cancellation = null; pending = null; ready = null; missions = player = null; retryAt = 0;
        }
        internal static void Suspend()
        {
            // In-flight synchronous reads stop at the next chunk/object boundary.
            // A late completion has no route back to the active publication.
            if (pending == null) return;
            cancellation?.Cancel(); cancellation = null; pending = null; ready = null; retryAt = 0;
        }
        internal static string Describe()
        {
            if (pending != null && !pending.IsCompleted) return "설치 파일 확인 중 · 별도 스레드 · 게임 실행 없음";
            if (ready == null) return "미조회";
            if (ready.Roots == null) return (ready.Permanent ? "지원 자료 불일치/미확인 · " : "읽기 대기 · 5초 후 재시도 · ") + ready.Failure;
            return "확인 · 조건/분기/초기화·고정 날짜 정의 + 현재 게임 참조 · " + ready.Milliseconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " ms (파일 검증/해석, 프레임 시간 아님)";
        }
        internal static object[] Read(PreviewDefinitionKind kind)
        {
            if (kind == PreviewDefinitionKind.Positions) return null; // native pools retain their existing authority
            if (pending != null && pending.IsCompleted)
            { ready = pending.GetAwaiter().GetResult(); pending = null; if (ready.Roots == null) retryAt = Time.unscaledTime + 5; }
            if (ready?.Roots != null) return ready.Roots[(int)kind];
            if (pending != null || ready != null && (ready.Permanent || Time.unscaledTime < retryAt) || missions == null || player == null) return null;
            try
            {
                string directory = DirectoryReader();
                if (string.IsNullOrEmpty(directory)) { ready = new Result { Permanent = true, Failure = "게임 설치 경로 미확인" }; return null; }
                // Main thread: existing identities only, never State/FinalValue,
                // a lazy Instance getter or a global flag/object search.
                Dictionary<string, object> bindings = Bindings(missions, player);
                var tokenSource = cancellation = new CancellationTokenSource();
                pending = Task.Run(() => Load(directory, bindings, tokenSource.Token));
            }
            catch (Exception error) { ready = new Result { Failure = error.GetType().Name }; retryAt = Time.unscaledTime + 5; }
            return null;
        }
        private static Result Load(string directory, Dictionary<string, object> bindings, CancellationToken token)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                using (Stream index = typeof(MapFileDefinitions).Assembly.GetManifestResourceStream("DiceMaster.MapDefinitions.index"))
                {
                    var roots = MapDefinitionFileReader.Read(directory, index, bindings, token, out long bytes);
                    return new Result { Roots = roots, Bytes = bytes, Milliseconds = watch.Elapsed.TotalMilliseconds };
                }
            }
            catch (OperationCanceledException) { return new Result { Permanent = true, Failure = "이전 문맥 취소" }; }
            catch (InvalidDataException error) { return new Result { Permanent = true, Failure = error.Message }; }
            catch (Exception error) { return new Result { Failure = error.GetType().Name }; }
        }
        private static object Field(object value, string name)
        {
            if (value == null) return null;
            for (Type at = value as Type ?? value.GetType(); at != null; at = at.BaseType)
            {
                FieldInfo field = at.GetField(name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(value is Type ? null : value);
            }
            return null;
        }
        private static bool InputType(string type)
        {
            switch (type)
            {
                case "Mortal.Core.FlagData": case "Mortal.Core.GameStat": case "Mortal.Core.RelationshipStat": case "Mortal.Core.GameTimeData":
                case "Mortal.Core.Book": case "Mortal.Core.Misc": case "Mortal.Core.SpecialItem": case "Mortal.Core.PlayerTalentData":
                case "Mortal.Core.UpgradeItemData": case "Mortal.Core.BookDataCollection": case "Mortal.Core.LibraryItemData": return true;
                default: return false;
            }
        }
        internal static Dictionary<string, object> Bindings(object currentMissions, object currentPlayer)
        {
            if (currentMissions.GetType().FullName != "Mortal.Core.MissionManagerData" || currentPlayer.GetType().FullName != "Mortal.Core.PlayerStatManagerData") throw new InvalidOperationException("Unsupported current owners.");
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            var visited = new HashSet<object>(); int remaining = 16384;
            Action<object, int> add = null;
            add = (value, depth) => {
                if (value == null || depth > 4 || !visited.Add(value)) return;
                if (--remaining < 0) throw new InvalidOperationException("Current definition binding limit.");
                string type = value.GetType().FullName;
                if (InputType(type) && value is UnityEngine.Object native && native != null)
                {
                    string key = type + "/" + native.name;
                    if (result.TryGetValue(key, out object old) && !ReferenceEquals(old, value)) result[key] = null;
                    else if (!result.ContainsKey(key)) result.Add(key, value);
                }
                // The fixed current catalogues include zero-count/locked items.
                // No current availability filter or gameplay getter runs here.
                if (value is IList list)
                { if (list.Count > 8192) throw new InvalidOperationException("Current catalogue limit."); foreach (object item in list) add(item, depth + 1); }
                else if (Field(value, "_list") is IList children) add(children, depth + 1);
            };
            object flags = Field(currentMissions, "_flagsCollection");
            if (!(flags is Array)) throw new InvalidOperationException("Current flag collections unavailable.");
            add(flags, 0);
            foreach (string field in new[] { "_stats", "_relationships", "_gameTimeData", "_timeCollection", "_talents", "_poisonUpgradeItems", "_weaponUpgradeItems" }) add(Field(currentPlayer, field), 0);
            // Optional catalogues: inspect only already existing static owners.
            // Missing owners leave exact input references unknown.
            foreach (string type in new[] { "Mortal.Core.ItemDatabase", "Mortal.Core.LibrarySystem" })
            {
                Type definition = currentPlayer.GetType().Assembly.GetType(type, false);
                // Reflection must not trigger a new/unsupported type initializer.
                // The reviewed owners have none; changed types stay unknown.
                if (definition == null || definition.TypeInitializer != null) continue;
                object owner = Field(definition, "_instance");
                if (!(owner is UnityEngine.Object native) || native == null) continue;
                foreach (string field in new[] { "_books", "_miscs", "_special", "_achievement" }) add(Field(owner, field), 0);
            }
            return result;
        }
    }
}
