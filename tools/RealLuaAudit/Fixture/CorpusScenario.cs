using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DiceMaster
{
    // This file is an isolated offline audit harness, not shipped product code.
    // All Lua parsing, constant validation, tracing and formatting below use
    // IL copied from the pinned released DLL. No game assembly is loaded.
    public static class CorpusScenario
    {
        public sealed class Input { public string key { get; set; } public string path { get; set; } public string sha256 { get; set; }
            public string resource_key { get; set; } public bool resource_key_verified { get; set; } public long path_id { get; set; } }
        const string StoryPrefix = "story/chinesetraditional/";
        static readonly Assembly Self = typeof(CorpusScenario).Assembly;
        static Type T(string name) => Self.GetType("DiceMaster." + name, true);
        static object Member(object value, string name) => value.GetType().GetField(name)?.GetValue(value) ?? value.GetType().GetProperty(name)?.GetValue(value);
        static IList Items(object value, string name) => (IList)Member(value, name);
        static object Call(Type type, object owner, string name, params object[] args)
        {
            try { return type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Invoke(owner, args); }
            catch (TargetInvocationException error) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException ?? error).Throw(); throw; }
        }
        static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
        static string HashText(string value) => Hash(Encoding.UTF8.GetBytes(value));
        static string SafeOption(string value) => Regex.IsMatch(value, "\\A[A-Za-z0-9_]+\\z") ? value : "sha256:" + HashText(value);
        static void Increment(Dictionary<string, int> counts, string name) { counts.TryGetValue(name, out int before); counts[name] = before + 1; }
        static object Read(string kind, string text = null) => Call(T("LiveLog.ScriptReadResult"), null, kind, kind == "Success" ? new object[] { text } : Array.Empty<object>());

        public static object Run(string indexPath)
        {
            var inputs = JsonSerializer.Deserialize<List<Input>>(File.ReadAllText(indexPath));
            bool mapped = Environment.GetEnvironmentVariable("DICEMASTER_CORPUS_RESOURCE_MAP") == "1";
            string root = Path.GetDirectoryName(indexPath);
            var scripts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Input input in inputs)
            {
                if (input.key != input.key.ToLowerInvariant() || string.IsNullOrEmpty(input.key) || scripts.ContainsKey(input.key))
                    throw new InvalidDataException("Invalid or duplicate normalized resource key");
                if (mapped && (!input.resource_key_verified || input.resource_key != input.key || !input.key.StartsWith(StoryPrefix, StringComparison.Ordinal) ||
                    !Regex.IsMatch(input.key.Substring(StoryPrefix.Length), "\\A[A-Za-z0-9_-]+\\z")))
                    throw new InvalidDataException("Mapped audit requires unique verified exact Traditional Chinese resource keys");
                string path = Path.GetFullPath(Path.Combine(root, input.path));
                if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    throw new InvalidDataException("Corpus path escaped analysis root");
                byte[] bytes = File.ReadAllBytes(path);
                if (Hash(bytes) != input.sha256) throw new InvalidDataException("Corpus byte hash mismatch");
                scripts[input.key] = new UTF8Encoding(false, true).GetString(bytes);
                hashes[input.key] = input.sha256;
            }
            var missing = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var resolved = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var invalid = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int readCalls = 0;
            DataHelper.PreviewScriptReader = name =>
            {
                readCalls++;
                if (mapped)
                {
                    if (string.IsNullOrWhiteSpace(name) || name.StartsWith("/", StringComparison.Ordinal) || name.Contains(":") || name.Contains("\\") ||
                        name.Split('/').Any(part => part == "." || part == ".." || part.Length == 0) || name.Any(char.IsControl))
                    { Increment(invalid, SafeOption(name ?? "")); return Read("Failed"); }
                    string resource = StoryPrefix + name.ToLowerInvariant();
                    if (scripts.TryGetValue(resource, out string body))
                    { Increment(resolved, resource); return Read("Success", body); }
                    Increment(missing, SafeOption(name));
                    // Verified absence in this supplied ResourceManager map only.
                    // This is not a claim about a running Unity installation.
                    return Read("Missing");
                }
                Increment(missing, name);
                // No ResourceManager paths were supplied. A matching leaf
                // name is not proof of the Story resource's identity.
                return Read("Waiting");
            };
            // Disable dummy state too. No estimates or name lookups may depend
            // on a real/current player. All remaining names use ID fallbacks.
            Mortal.Core.PlayerStatManagerData.Ready(false);
            Mortal.Core.MissionManagerData.Instance = null;
            var parserType = T("LuaParse.LuaParser");
            object parser = parserType.GetProperty("Instance").GetValue(null);
            var files = new List<object>();
            var rows = new List<object>();
            var stateCounts = new Dictionary<string, int>();
            var issueCounts = new Dictionary<string, int>();
            var functionCounts = new Dictionary<string, int>();
            var costs = new List<double>();
            var allocated = new List<long>();
            var characters = new List<int>();
            var tokens = new List<int>();
            var work = new List<int>();
            var scriptCounts = new List<int>();
            var branchDiagnostics = new List<object>();
            int scriptsWithOptions = 0, incompleteDiscovery = 0, formatFailures = 0, expectedReadWaits = 0, expectedReadFailures = 0, totalEffects = 0;
            var wall = Stopwatch.StartNew();
            foreach (Input input in inputs.OrderBy(i => i.key, StringComparer.Ordinal))
            {
                Call(parserType, parser, "ClearCaches");
                Call(T("LiveLog.PreviewDiagnostics"), null, "ResetContext");
                DicePlugin.Warnings.Clear();
                long allocationStart = GC.GetAllocatedBytesForCurrentThread();
                var watch = Stopwatch.StartNew();
                IDictionary options;
                string rootFriendlyName = mapped ? input.key.Substring(StoryPrefix.Length) : input.key;
                try { options = (IDictionary)Call(parserType, parser, "ExtractOptionActionsFromLua", rootFriendlyName, scripts[input.key]); }
                catch (Exception error)
                {
                    files.Add(new { source = input.key, source_sha256 = hashes[input.key], characters = scripts[input.key].Length, exception_type = error.GetType().FullName });
                    continue;
                }
                if (options.Count > 0) scriptsWithOptions++;
                bool incomplete = (bool)parserType.GetField("IncompleteOptionDiscovery").GetValue(parser);
                if (incomplete) incompleteDiscovery++;
                var optionKeys = new List<string>();
                foreach (DictionaryEntry entry in options)
                {
                    optionKeys.Add(SafeOption((string)entry.Key));
                    foreach (object action in (IList)entry.Value)
                    {
                        object analysis = Member(action, "Analysis");
                        if (analysis == null) throw new InvalidDataException("Actual released Action lost analysis");
                        analysis.GetType().GetField("SnapshotAllowed").SetValue(analysis, false);
                        string state = Member(analysis, "State").ToString();
                        Increment(stateCounts, state);
                        var effects = new List<object>();
                        var issues = new List<object>();
                        foreach (object issue in Items(analysis, "Issues"))
                        {
                            string function = (string)Member(issue, "Function"), reason = (string)Member(issue, "Reason");
                            Increment(issueCounts, function + "|" + reason);
                            string issueSource = (string)Member(issue, "Source");
                            issues.Add(new { source = mapped ? SafeOption(issueSource) : issueSource, function, reason,
                                is_read_failure = (bool)Member(issue, "IsReadFailure"), is_waiting = (bool)Member(issue, "IsWaiting") });
                        }
                        foreach (object effect in Items(analysis, "Effects"))
                        {
                            string owner = (string)Member(effect, "Owner"), name = (string)Member(effect, "Name");
                            Increment(functionCounts, owner + "." + name);
                            totalEffects++;
                            var arguments = ((IEnumerable)Member(effect, "Arguments")).Cast<string>().ToArray();
                            var branches = new List<object>();
                            foreach (object branch in Items(effect, "Branches"))
                                branches.Add(new { group = (int)Member(branch, "Group"), alternative = (int)Member(branch, "Alternative"), is_else = (bool)Member(branch, "IsElse") });
                            string effectSource = (string)Member(effect, "Source");
                            string effectResource = mapped ? StoryPrefix + effectSource.ToLowerInvariant() : effectSource;
                            if (mapped && !scripts.ContainsKey(effectResource)) throw new InvalidDataException("Effect source has no verified exact resource key");
                            effects.Add(new { source = effectResource, owner, name,
                                argument_count = arguments.Length, argument_sha256 = arguments.Select(HashText).ToArray(),
                                conditional = (bool)Member(effect, "Conditional"), followup = (bool)Member(effect, "Followup"),
                                execution_uncertain = (bool)Member(effect, "ExecutionUncertain"), branches,
                                validation_error = (string)Member(effect, "ValidationError") });
                        }
                        int knownEffects = Items(analysis, "Effects").Cast<object>().Count(effect =>
                            Member(effect, "ValidationError") == null &&
                            !(bool)Call(T("LiveLog.PreviewDisplay"), null, "IsDisplayOnly", effect) &&
                            !(bool)Call(T("LiveLog.PreviewDisplay"), null, "IsRoutingOnly", effect));
                        bool nonReadIssue = Items(analysis, "Issues").Cast<object>().Any(issue => !(bool)Member(issue, "IsReadFailure"));
                        if (Environment.GetEnvironmentVariable("DICEMASTER_CORPUS_BRANCH_DIAGNOSTICS") == "1" && Items(analysis, "Issues").Cast<object>().Any(issue => (string)Member(issue, "Function") == "if" && (string)Member(issue, "Reason") == "unbalanced-branch"))
                            branchDiagnostics.Add(DiagnoseBranch(input.key, (string)entry.Key, scripts[input.key]));
                        string rootStaticSupport = nonReadIssue ? knownEffects > 0 ? "Partial" : "Unsupported" : knownEffects > 0 ? "Supported" : "NoEffects";
                        Increment(stateCounts, (mapped ? "mapped-static/" : "root-static/") + rootStaticSupport);
                        string descriptionHash = null, descriptionFailure = null;
                        int descriptionChars = 0;
                        try
                        {
                            string description = (string)Call(T("LiveLog.PreviewDisplay"), null, "Describe", analysis);
                            descriptionHash = HashText(description); descriptionChars = description.Length;
                        }
                        catch (Exception error)
                        {
                            descriptionFailure = error.GetType().FullName;
                            if (descriptionFailure == "DiceMaster.LiveLog.PreviewReadException")
                            {
                                string partial = (string)Member(error, "PartialDescription");
                                descriptionHash = HashText(partial); descriptionChars = partial.Length;
                                if ((bool)Member(error, "IsWaiting")) expectedReadWaits++; else expectedReadFailures++;
                            }
                            else formatFailures++;
                        }
                        int charCount = (int)Member(analysis, "CharacterCount"), tokenCount = (int)Member(analysis, "TokenCount"),
                            workCount = (int)Member(analysis, "WorkCount"), scriptCount = (int)Member(analysis, "ScriptCount");
                        characters.Add(charCount); tokens.Add(tokenCount); work.Add(workCount); scriptCounts.Add(scriptCount);
                        rows.Add(new { source = input.key, option = SafeOption((string)entry.Key), state,
                            static_support = rootStaticSupport, support_scope = mapped ? "root-and-followups" : "root-only",
                            root_static_support = mapped ? null : rootStaticSupport, effects, issues,
                            stopped_at_choice = (bool)Member(analysis, "StoppedAtChoice"), character_count = charCount,
                            token_count = tokenCount, work_count = workCount, script_count = scriptCount,
                            description_sha256 = descriptionHash, description_characters = descriptionChars, description_failure_type = descriptionFailure });
                    }
                }
                watch.Stop(); costs.Add(watch.Elapsed.TotalMilliseconds);
                allocated.Add(GC.GetAllocatedBytesForCurrentThread() - allocationStart);
                files.Add(new { source = input.key, source_sha256 = hashes[input.key], characters = scripts[input.key].Length,
                    option_keys = optionKeys, incomplete_option_discovery = incomplete,
                    elapsed_ms = watch.Elapsed.TotalMilliseconds, thread_allocated_bytes = allocated.Last() });
            }
            wall.Stop();
            int forbidden = UnityEngine.Random.Calls + Mortal.Core.PlayerStatManagerData.InstanceReads + Mortal.Core.ItemDatabase.InstanceReads +
                Mortal.Core.MissionManagerData.InstanceReads + Mortal.Core.StatGroupVariable.OriginalReads + Mortal.Core.StatCompareItem.OriginalReads +
                DataHelper.ConditionReads + DataHelper.MissionReads;
            if (forbidden != 0) throw new InvalidDataException("Forbidden RNG, singleton, condition or current-state fixture boundary was called");
            return new {
                contract = "offline-pinned-actual-IL-static-corpus-audit; no Lua, game, RNG, condition chain or save execution",
                exact_resource_map_enabled = mapped,
                source_index_sha256 = Hash(File.ReadAllBytes(indexPath)),
                snapshot_allowed = false, forbidden_boundary_calls = forbidden, corpus_scripts = scripts.Count,
                corpus_characters = scripts.Values.Sum(s => (long)s.Length), scripts_with_options = scriptsWithOptions,
                options = rows.Count, incomplete_discovery_scripts = incompleteDiscovery, states = stateCounts,
                collected_effects_including_routing_and_display = totalEffects, functions = functionCounts,
                issues = issueCounts, unresolved_literal_target_reads = missing, resolved_literal_target_reads = resolved,
                invalid_literal_target_reads = invalid, script_read_calls = readCalls,
                followup_resolution = mapped ? "verified-exact-ResourceManager-TraditionalChinese-map; no other-language or leaf-name fallback; Missing means absent in supplied map only" : "unavailable-resource-path-map; all target reads return Waiting, never inferred Missing",
                formatter_unexpected_failures = formatFailures, expected_unresolved_followup_read_waits = expectedReadWaits,
                expected_mapped_read_failures = expectedReadFailures, files, option_results = rows,
                unbalanced_branch_diagnostics = branchDiagnostics,
                metrics = new { total_wall_ms = wall.Elapsed.TotalMilliseconds,
                    per_script_ms = Quantiles(costs), per_script_thread_allocated_bytes = Quantiles(allocated.Select(v => (double)v).ToList()),
                    analysis_cumulative_characters = Quantiles(characters.Select(v => (double)v).ToList()),
                    analysis_cumulative_tokens = Quantiles(tokens.Select(v => (double)v).ToList()),
                    analysis_work_steps = Quantiles(work.Select(v => (double)v).ToList()),
                    analysis_scripts = Quantiles(scriptCounts.Select(v => (double)v).ToList()) },
                historical_alpha2_limits = new { depth = 8, scripts = 32, cumulative_utf16_characters = 262144, tokens = 65536, work = 131072 },
                actual_trace_limits = LimitValues("LiveLog.PreviewTraceLimits"),
                actual_discovery_limits = LimitValues("LiveLog.PreviewDiscoveryLimits"),
                final_production_limits_certified = false, current_player_estimates_checked = false, windows_or_unity_runtime_checked = false
            };
        }

        static object LimitValues(string name)
        {
            Type type = Self.GetType("DiceMaster." + name, false);
            if (type == null) return null;
            object limits = Activator.CreateInstance(type);
            return type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.FieldType == typeof(int))
                .ToDictionary(f => f.Name, f => (int)f.GetValue(limits));
        }

        static object DiagnoseBranch(string sourceID, string option, string source)
        {
            Type syntax = T("LiveLog.PreviewSyntax");
            IList blocks = (IList)syntax.GetMethod("OptionBlocks", new[] { typeof(string) }).Invoke(null, new object[] { source });
            object block = blocks.Cast<object>().FirstOrDefault(b => (string)Member(b, "Key") == option);
            if (block == null) return new { source = sourceID, option = SafeOption(option), block_not_found = true };
            string body = (string)Member(block, "Body");
            int start = source.IndexOf(body, StringComparison.Ordinal), end = start + body.Length;
            bool unique = start >= 0 && source.IndexOf(body, start + 1, StringComparison.Ordinal) < 0;
            IList tokens = (IList)syntax.GetMethod("Tokenize").Invoke(null, new object[] { body });
            IList fullTokens = (IList)syntax.GetMethod("Tokenize").Invoke(null, new object[] { source });
            IList markers = (IList)syntax.GetMethod("IndexMarkers", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { source });
            bool markerAtCut = markers.Cast<object>().Any(m => (int)Member(m, "Start") == end);
            var openings = new List<object>();
            for (int i = 0; i < tokens.Count; i++)
            {
                if ((bool)Member(tokens[i], "IsString") || (string)Member(tokens[i], "Text") != "if") continue;
                object[] args = { tokens, i, 0 };
                syntax.GetMethod("Branches").Invoke(null, args);
                if ((int)args[2] < tokens.Count) continue;
                int absolute = start + (int)Member(tokens[i], "Start");
                int fullIndex = Enumerable.Range(0, fullTokens.Count).FirstOrDefault(j => (int)Member(fullTokens[j], "Start") == absolute, -1);
                int closing = -1;
                if (fullIndex >= 0)
                {
                    object[] fullArgs = { fullTokens, fullIndex, 0 };
                    syntax.GetMethod("Branches").Invoke(null, fullArgs);
                    int closingIndex = (int)fullArgs[2];
                    if (closingIndex < fullTokens.Count) closing = (int)Member(fullTokens[closingIndex], "Start");
                }
                bool chooseBeforeCut = Enumerable.Range(i, tokens.Count-i).Any(j =>
                    !(bool)Member(tokens[j], "IsString") && (string)Member(tokens[j], "Text") == "choose");
                openings.Add(new { absolute_if_start = absolute, matching_end_in_full_source = closing,
                    matching_end_after_cut = closing >= end, choose_before_cut = chooseBeforeCut });
            }
            return new { source = sourceID, option = SafeOption(option), body_sha256 = HashText(body),
                body_start = start, body_end = end, body_characters = body.Length, unique_body_match = unique,
                discovery_limit_reached = (bool)Member(block, "LimitReached"), next_index_marker_at_cut = markerAtCut,
                unclosed_body_ifs = openings };
        }

        static object Quantiles(List<double> values)
        {
            values.Sort();
            if (values.Count == 0) return new { count = 0, sum = 0.0, p50 = 0.0, p95 = 0.0, max = 0.0 };
            return new { count = values.Count, sum = values.Sum(), p50 = values[(int)Math.Floor((values.Count - 1) * .50)],
                p95 = values[(int)Math.Floor((values.Count - 1) * .95)], max = values[values.Count - 1] };
        }
    }
}
