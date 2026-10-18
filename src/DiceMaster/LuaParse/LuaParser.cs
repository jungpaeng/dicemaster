using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DiceMaster.LiveLog;

namespace DiceMaster.LuaParse;

public class LuaParser
{
	private static readonly Lazy<LuaParser> instance = new Lazy<LuaParser>(() => new LuaParser());

	private static readonly Regex indexMarkerRegex = new Regex("^\\s*--index:(O(_\\w+)+)", RegexOptions.Multiline);

	private static readonly Regex switchRegex = new Regex("checkpointmanager\\.Switch\\(([^)]*)\\)");

	private static readonly Regex positionRegex = new Regex("checkpointmanager\\.Position\\(([^)]*)\\)");

	private static readonly Regex modifyRegex = new Regex("\\s*statmodifymanager\\.(?!GetDisplayTime)\\w+\\(.*?\\)");

	private static readonly Regex battleScenesRegex = new Regex("luamanager\\.ChangeScene\\(\"(Combat|Battle)\"");

	private static readonly Regex scriptActionsRegex = new Regex("luamanager\\.SetNextScript");

	private static readonly Regex storyTextRegex = new Regex("GetStoryText\\s*\\(([^)]*)\\)");

	private static readonly Regex switchAssignmentRegex = new Regex("^[ \\t]*(?:local\\s+)?([A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*checkpointmanager\\.Switch\\(([^)]*)\\)", RegexOptions.Multiline);

	private static readonly Regex chooseAssignmentRegex = new Regex("^[ \\t]*(?:local\\s+)?([A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*choose\\(([^)]*)\\)", RegexOptions.Multiline);

	public Dictionary<string, List<Action>> OptionResult;

	private Dictionary<string, List<Action>> luaModification;

	private Dictionary<string, List<string>> luaOptions;

	private Dictionary<string, List<string>> luaSwitchCheck;

	private Dictionary<string, List<string>> luaPositionWork;

	private Dictionary<string, Dictionary<string, List<string>>> luaOptionStoryText;

	private Dictionary<string, Dictionary<string, List<string>>> luaSwitchStoryText;

	private Dictionary<string, Dictionary<string, Dictionary<string, string>>> luaSwitchBranchStoryText;

	private readonly Dictionary<string, Dictionary<string, List<Action>>> previewOptionCache = new Dictionary<string, Dictionary<string, List<Action>>>();
	private readonly Dictionary<string, string> previewSources = new Dictionary<string, string>();
	private string previewSourceName = "";
	private readonly PreviewParseCache previewParseCache = new PreviewParseCache();
	private readonly Dictionary<string, bool> previewIncomplete = new Dictionary<string, bool>();
	public bool IncompleteOptionDiscovery;
	private static readonly HashSet<PreviewAnalysis> retryArmed = new HashSet<PreviewAnalysis>();

	public static LuaParser Instance => instance.Value;

	private LuaParser()
	{
		OptionResult = new Dictionary<string, List<Action>>();
		luaModification = new Dictionary<string, List<Action>>();
		luaOptions = new Dictionary<string, List<string>>();
		luaSwitchCheck = new Dictionary<string, List<string>>();
		luaPositionWork = new Dictionary<string, List<string>>();
		luaOptionStoryText = new Dictionary<string, Dictionary<string, List<string>>>();
		luaSwitchStoryText = new Dictionary<string, Dictionary<string, List<string>>>();
		luaSwitchBranchStoryText = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
	}

	public void ClearCaches()
	{
		luaOptions.Clear(); luaSwitchCheck.Clear(); luaPositionWork.Clear(); luaModification.Clear();
		luaOptionStoryText.Clear(); luaSwitchStoryText.Clear(); luaSwitchBranchStoryText.Clear();
		OptionResult.Clear(); previewOptionCache.Clear(); previewSources.Clear(); retryArmed.Clear();
		previewParseCache.Clear(); previewSourceName = ""; IncompleteOptionDiscovery = false; previewIncomplete.Clear();
	}

	public Dictionary<string, List<Action>> ExtractOptionActionsFromLua(string friendlyName, string luaScript = "")
	{
		friendlyName = friendlyName ?? ""; luaScript = luaScript ?? "";
		if (previewOptionCache.TryGetValue(friendlyName, out var cached) &&
			(luaScript.Length == 0 || previewSources[friendlyName] == luaScript))
		{
			IncompleteOptionDiscovery = previewIncomplete[friendlyName];
			foreach (var pair in cached) OptionResult[pair.Key] = pair.Value;
			return cached;
		}
		previewSourceName = friendlyName;
		var result = new Dictionary<string, List<Action>>();
		var keys = new List<string>(); var stories = new Dictionary<string, List<string>>();
		var blocks = PreviewSyntax.OptionBlocks(luaScript, out bool incomplete);
		IncompleteOptionDiscovery = incomplete; previewIncomplete[friendlyName] = incomplete;
		if (incomplete)
		{
			var discovery = PreviewAnalyzer.Analyze(friendlyName, "");
			discovery.AddIssue(friendlyName, "options", "option-lexical-limit");
			PreviewDiagnostics.Report(discovery, "*");
		}
		foreach (var block in blocks)
		{
			if (result.ContainsKey(block.Key)) continue;
			var analysis = PreviewAnalyzer.AnalyzeWithCache(friendlyName, block.Body, DataHelper.ReadPreviewScript, null, previewParseCache);
			if (block.Conditional) analysis.MarkConditionalRoot();
			if (block.LimitReached) analysis.AddIssue(friendlyName, "options", "option-lexical-limit");
			analysis.OptionKey = block.Key;
			PreviewDiagnostics.Report(analysis, block.Key);
			result[block.Key] = new List<Action> { Action.FromAnalysis(analysis) };
			keys.Add(block.Key); stories[block.Key] = string.IsNullOrEmpty(block.Story) ? new List<string>() : new List<string> { block.Story };
		}
		if (previewOptionCache.TryGetValue(friendlyName, out var previous)) foreach (var pair in previous) OptionResult.Remove(pair.Key);
		previewOptionCache[friendlyName] = result; previewSources[friendlyName] = luaScript;
		luaOptions[friendlyName] = keys; luaOptionStoryText[friendlyName] = stories;
		luaSwitchStoryText.Remove(friendlyName); luaSwitchBranchStoryText.Remove(friendlyName);
		foreach (var pair in result) OptionResult[pair.Key] = pair.Value;
		return result;
	}

	private void ExtractOptionActionsFromChooseBlocks(string luaScript, List<string> optionKeys, Dictionary<string, List<string>> optionStoryMap, Dictionary<string, List<Action>> optionActionMap)
	{
		foreach (var block in PreviewSyntax.OptionBlocks(luaScript))
		{
			if (!optionKeys.Contains(block.Key)) optionKeys.Add(block.Key);
			if (!optionStoryMap.ContainsKey(block.Key)) optionStoryMap[block.Key] = new List<string>();
			if (block.Story.Length != 0 && !optionStoryMap[block.Key].Contains(block.Story)) optionStoryMap[block.Key].Add(block.Story);
			if (!optionActionMap.ContainsKey(block.Key))
			{
				var analysis = PreviewAnalyzer.AnalyzeWithCache(previewSourceName, block.Body, DataHelper.ReadPreviewScript, null, previewParseCache);
				if (block.Conditional) analysis.MarkConditionalRoot();
				optionActionMap[block.Key] = new List<Action> { Action.FromAnalysis(analysis) };
			}
			OptionResult[block.Key] = optionActionMap[block.Key];
		}
	}

	private static int FindOptionTableDeclaration(string luaScript, string optionTableVar, int beforeIndex)
	{
		if (string.IsNullOrWhiteSpace(luaScript) || string.IsNullOrWhiteSpace(optionTableVar))
		{
			return -1;
		}
		Regex regex = new Regex("^[ \\t]*(?:local\\s+)?" + Regex.Escape(optionTableVar) + "\\s*=\\s*\\{\\s*\\}", RegexOptions.Multiline);
		int result = -1;
		foreach (Match item in regex.Matches(luaScript))
		{
			if (item.Index < beforeIndex)
			{
				result = item.Index;
			}
		}
		return result;
	}

	private static Dictionary<string, string> ParseOptionTableItems(string luaScript, string optionTableVar, int tableDeclIndex, int beforeIndex)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (string.IsNullOrWhiteSpace(luaScript) || string.IsNullOrWhiteSpace(optionTableVar) || tableDeclIndex < 0 || beforeIndex <= tableDeclIndex)
		{
			return dictionary;
		}
		string input = luaScript.Substring(tableDeclIndex, beforeIndex - tableDeclIndex);
		foreach (Match item in new Regex("^\\s*" + Regex.Escape(optionTableVar) + "\\[(\\d+)\\]\\s*=\\s*['\\\"]([^'\\\"]+)['\\\"]", RegexOptions.Multiline).Matches(input))
		{
			string value = item.Groups[1].Value;
			string value2 = item.Groups[2].Value;
			if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(value2) && !dictionary.ContainsKey(value))
			{
				dictionary[value] = value2.Trim();
			}
		}
		return dictionary;
	}

	private List<Action> BuildActionListFromBlock(string blockContent)
	{
		return new List<Action> { Action.FromAnalysis(PreviewAnalyzer.AnalyzeWithCache(previewSourceName, blockContent, DataHelper.ReadPreviewScript, null, previewParseCache)) };
	}

	public Dictionary<string, List<string>> ExtractOptionFirstStoryTextsFromLua(string friendlyName, string luaScript = "")
	{
		if (!luaOptionStoryText.ContainsKey(friendlyName))
		{
			ExtractOptionActionsFromLua(friendlyName, luaScript);
		}
		if (luaOptionStoryText.TryGetValue(friendlyName, out var value))
		{
			return value;
		}
		return new Dictionary<string, List<string>>();
	}

	public Dictionary<string, List<string>> ExtractSwitchFirstStoryTextsFromLua(string friendlyName, string luaScript = "")
	{
		if (luaSwitchStoryText.ContainsKey(friendlyName))
		{
			return luaSwitchStoryText[friendlyName];
		}
		Dictionary<string, Dictionary<string, string>> dictionary = ExtractSwitchBranchFirstStoryTextsFromLua(friendlyName, luaScript);
		Dictionary<string, List<string>> dictionary2 = new Dictionary<string, List<string>>();
		foreach (KeyValuePair<string, Dictionary<string, string>> item in dictionary)
		{
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, string> item2 in item.Value)
			{
				if (!string.IsNullOrWhiteSpace(item2.Value) && !list.Contains(item2.Value))
				{
					list.Add(item2.Value);
				}
			}
			if (list.Count > 0)
			{
				dictionary2[item.Key] = list;
			}
		}
		luaSwitchStoryText[friendlyName] = dictionary2;
		return dictionary2;
	}

	public Dictionary<string, Dictionary<string, string>> ExtractSwitchBranchFirstStoryTextsFromLua(string friendlyName, string luaScript = "")
	{
		if (luaSwitchBranchStoryText.TryGetValue(friendlyName, out var cached)) return cached;
		var result = new Dictionary<string, Dictionary<string, string>>();
		var tokens = PreviewSyntax.Tokenize(luaScript);
		for (int i = 2; i < tokens.Count; i++)
		{
			if (tokens[i - 1].Text != "=" || !tokens[i - 2].IsIdentifier || !PreviewSyntax.TryCall(tokens, i, out var owner, out var name, out var open, out var close) || owner != "checkpointmanager" || name != "Switch" || close < 0) continue;
			var args = PreviewSyntax.Arguments(luaScript, tokens, open, close);
			if (args.Count == 0 || !PreviewSyntax.TryString(args[0], out var key)) continue;
			int branch = PreviewSyntax.FindAssignedBranch(tokens, close + 1, tokens[i - 2].Text); if (branch < 0) continue;
			var entries = new Dictionary<string, string>();
			foreach (var b in PreviewSyntax.Branches(tokens, branch, out var ending)) entries[b.Label] = PreviewSyntax.FirstStory(PreviewSyntax.Slice(luaScript, tokens, b.Start, b.End));
			result[key] = entries;
		}
		luaSwitchBranchStoryText[friendlyName] = result;
		return result;
	}

	private static int FindOptionBranchEnd(string luaScript, int startIndex, int hardLimit)
	{
		if (string.IsNullOrEmpty(luaScript))
		{
			return startIndex;
		}
		int num = Math.Max(0, startIndex);
		int num2 = Math.Min(luaScript.Length, hardLimit);
		int num3 = 0;
		while (num < num2)
		{
			int num4 = luaScript.IndexOf('\n', num, num2 - num);
			int num5 = ((num4 >= 0) ? num4 : num2);
			string text = luaScript.Substring(num, num5 - num).Trim();
			if (text.Length != 0 && !text.StartsWith("--"))
			{
				if (num3 == 0 && (text.StartsWith("elseif ") || text.StartsWith("else") || text == "end"))
				{
					return num;
				}
				if (IsBlockOpen(text))
				{
					num3++;
				}
				else if (text == "end" && num3 > 0)
				{
					num3--;
				}
			}
			if (num4 < 0)
			{
				break;
			}
			num = num4 + 1;
		}
		return num2;
	}

	private static bool IsBlockOpen(string trimmedLine)
	{
		if (string.IsNullOrEmpty(trimmedLine))
		{
			return false;
		}
		if (trimmedLine.StartsWith("if ") && trimmedLine.Contains(" then"))
		{
			return true;
		}
		if (trimmedLine.StartsWith("if(") && trimmedLine.Contains(" then"))
		{
			return true;
		}
		if (trimmedLine.StartsWith("for ") && trimmedLine.Contains(" do"))
		{
			return true;
		}
		if (trimmedLine.StartsWith("while ") && trimmedLine.Contains(" do"))
		{
			return true;
		}
		if (trimmedLine.StartsWith("function "))
		{
			return true;
		}
		return false;
	}

	private static string ExtractFirstStoryTextKey(string blockContent)
	{
		return PreviewSyntax.FirstStory(blockContent);
	}

	private static int FindIfLineForSwitchVar(string luaScript, string switchResultVar, int startIndex, int hardLimit)
	{
		if (string.IsNullOrWhiteSpace(luaScript) || string.IsNullOrWhiteSpace(switchResultVar))
		{
			return -1;
		}
		int num = Math.Max(0, startIndex);
		int num2 = Math.Min(luaScript.Length, hardLimit);
		while (num < num2)
		{
			int num3 = luaScript.IndexOf('\n', num, num2 - num);
			int num4 = ((num3 >= 0) ? num3 : num2);
			if (IsSwitchIfConditionLine(luaScript.Substring(num, num4 - num).Trim(), switchResultVar))
			{
				return num;
			}
			if (num3 < 0)
			{
				break;
			}
			num = num3 + 1;
		}
		return -1;
	}

	private static bool IsSwitchIfConditionLine(string trimmedLine, string switchResultVar)
	{
		if (string.IsNullOrWhiteSpace(trimmedLine) || string.IsNullOrWhiteSpace(switchResultVar))
		{
			return false;
		}
		return Regex.IsMatch(trimmedLine, "^if\\s*\\(*\\s*" + Regex.Escape(switchResultVar) + "\\s*==.+\\bthen\\b");
	}

	private static int FindLineEnd(string luaScript, int lineStart, int hardLimit)
	{
		int num = Math.Min(luaScript.Length, hardLimit);
		int num2 = luaScript.IndexOf('\n', lineStart, Math.Max(0, num - lineStart));
		if (num2 < 0)
		{
			return num;
		}
		return num2 + 1;
	}

	private static List<string> ExtractFirstStoryTextKeysFromIfElseChain(string luaScript, int branchStart, int hardLimit)
	{
		List<string> list = new List<string>();
		int num = branchStart;
		int num2 = 0;
		while (num < hardLimit && num2 < 128)
		{
			num2++;
			int num3 = FindOptionBranchEnd(luaScript, num, hardLimit);
			string text = ExtractFirstStoryTextKey(luaScript.Substring(num, Math.Max(0, num3 - num)));
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(text);
			}
			if (num3 >= hardLimit)
			{
				break;
			}
			int num4 = luaScript.IndexOf('\n', num3, hardLimit - num3);
			int num5 = ((num4 >= 0) ? num4 : hardLimit);
			string text2 = luaScript.Substring(num3, num5 - num3).Trim();
			if (!text2.StartsWith("elseif ") && !text2.StartsWith("else"))
			{
				break;
			}
			num = ((num4 >= 0) ? (num4 + 1) : hardLimit);
		}
		return list;
	}

	private static Dictionary<string, string> ExtractFirstStoryTextByBranchFromIfElseChain(string luaScript, string switchVar, int branchHeaderIndex, int branchStart, int hardLimit)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		int lineStart = branchHeaderIndex;
		int num = branchStart;
		int num2 = 0;
		while (num < hardLimit && num2 < 128)
		{
			num2++;
			int num3 = FindOptionBranchEnd(luaScript, num, hardLimit);
			string blockContent = luaScript.Substring(num, Math.Max(0, num3 - num));
			if (TryParseSwitchBranchLabel(GetLineText(luaScript, lineStart, hardLimit), switchVar, out var label))
			{
				dictionary[label] = ExtractFirstStoryTextKey(blockContent);
			}
			if (num3 >= hardLimit)
			{
				break;
			}
			int num4 = luaScript.IndexOf('\n', num3, hardLimit - num3);
			int num5 = ((num4 >= 0) ? num4 : hardLimit);
			string text = luaScript.Substring(num3, num5 - num3).Trim();
			if (!text.StartsWith("elseif ") && !text.StartsWith("else"))
			{
				break;
			}
			lineStart = num3;
			num = ((num4 >= 0) ? (num4 + 1) : hardLimit);
		}
		return dictionary;
	}

	private static Dictionary<string, string> ExtractBranchBlockByLabelFromIfElseChain(string luaScript, string switchVar, int branchHeaderIndex, int branchStart, int hardLimit)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		int lineStart = branchHeaderIndex;
		int num = branchStart;
		int num2 = 0;
		while (num < hardLimit && num2 < 128)
		{
			num2++;
			int num3 = FindOptionBranchEnd(luaScript, num, hardLimit);
			string lineText = GetLineText(luaScript, lineStart, hardLimit);
			string value = luaScript.Substring(num, Math.Max(0, num3 - num));
			if (TryParseSwitchBranchLabel(lineText, switchVar, out var label))
			{
				dictionary[label] = value;
			}
			if (num3 >= hardLimit)
			{
				break;
			}
			int num4 = luaScript.IndexOf('\n', num3, hardLimit - num3);
			int num5 = ((num4 >= 0) ? num4 : hardLimit);
			string text = luaScript.Substring(num3, num5 - num3).Trim();
			if (!text.StartsWith("elseif ") && !text.StartsWith("else"))
			{
				break;
			}
			lineStart = num3;
			num = ((num4 >= 0) ? (num4 + 1) : hardLimit);
		}
		return dictionary;
	}

	private static string GetLineText(string luaScript, int lineStart, int hardLimit)
	{
		int num = Math.Min(luaScript.Length, hardLimit);
		if (lineStart < 0 || lineStart >= num)
		{
			return string.Empty;
		}
		int num2 = luaScript.IndexOf('\n', lineStart, num - lineStart);
		int num3 = ((num2 >= 0) ? num2 : num);
		return luaScript.Substring(lineStart, num3 - lineStart).Trim();
	}

	private static bool TryParseSwitchBranchLabel(string branchHeaderLine, string switchVar, out string label)
	{
		label = string.Empty;
		if (string.IsNullOrWhiteSpace(branchHeaderLine) || string.IsNullOrWhiteSpace(switchVar))
		{
			return false;
		}
		Match match = Regex.Match(branchHeaderLine, "^(?:if|elseif)\\s*\\(*\\s*" + Regex.Escape(switchVar) + "\\s*==\\s*([^\\s\\)]+)");
		if (match.Success)
		{
			string text = match.Groups[1].Value.Trim().Trim('\'', '"', '(', ')');
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			label = text;
			return true;
		}
		if (branchHeaderLine == "else" || branchHeaderLine.StartsWith("else "))
		{
			label = "else";
			return true;
		}
		return false;
	}

	private static void AddDistinctStoryKeys(List<string> target, IEnumerable<string> source)
	{
		if (target == null || source == null)
		{
			return;
		}
		foreach (string item in source)
		{
			if (!string.IsNullOrWhiteSpace(item) && !target.Contains(item))
			{
				target.Add(item);
			}
		}
	}

	private void AddActionsToList(string blockContent, Regex regex, ActionType actionType, List<Action> actionList)
	{
		actionList.AddRange(from Match m in regex.Matches(blockContent)
			select new Action(actionType, m.Value.Trim()));
	}

	public List<string> ExtractSwitchCheckFromLua(string friendlyName, string luaScript = "")
	{
		if (luaSwitchCheck.TryGetValue(friendlyName, out var cached)) return cached;
		return luaSwitchCheck[friendlyName] = ExtractPreviewCallKeys(luaScript, "checkpointmanager", "Switch");
	}

	public List<string> ExtractPositionWorkFromLua(string friendlyName, string luaScript = "")
	{
		if (luaPositionWork.TryGetValue(friendlyName, out var cached)) return cached;
		return luaPositionWork[friendlyName] = ExtractPreviewCallKeys(luaScript, "checkpointmanager", "Position");
	}

	private List<string> ExtractMatches(string luaScript, Regex regex)
	{
		List<string> list = new List<string>();
		foreach (Match item in regex.Matches(luaScript))
		{
			string text = ParseFirstLuaArgument(item.Groups[1].Value);
			if (!string.IsNullOrEmpty(text) && !list.Contains(text))
			{
				list.Add(text);
			}
		}
		return list;
	}

	private static string ParseFirstLuaArgument(string rawArgs)
	{
		var source = "f(" + (rawArgs ?? "") + ")"; var tokens = PreviewSyntax.Tokenize(source);
		if (tokens.Count < 3) return "";
		var args = PreviewSyntax.Arguments(source, tokens, 1, tokens.Count - 1);
		if (args.Count == 0) return "";
		return PreviewSyntax.TryString(args[0], out var value) ? value : args[0].Trim();
	}

	public List<Action> ExtractActionsFromLua(string luaScriptName)
	{
		if (luaModification.TryGetValue(luaScriptName, out var cached)) return cached;
		var read = DataHelper.ReadPreviewScript(luaScriptName);
		PreviewAnalysis analysis;
		if (read.State == ScriptReadState.Success) analysis = PreviewAnalyzer.AnalyzeWithCache(luaScriptName, read.Text, DataHelper.ReadPreviewScript, null, previewParseCache);
		else
		{
			analysis = PreviewAnalyzer.AnalyzeWithCache(luaScriptName, "", DataHelper.ReadPreviewScript, null, previewParseCache);
			analysis.RootNeedsRead = true;
			analysis.AddIssue(luaScriptName, "script", read.State == ScriptReadState.Missing ? "script-target-missing" : read.State == ScriptReadState.Waiting ? "script-data-waiting" : "script-read-failed", true, read.State == ScriptReadState.Waiting);
		}
		analysis.SnapshotAllowed = false;
		analysis.OptionKey = luaScriptName;
		PreviewDiagnostics.Report(analysis, luaScriptName);
		var actions = new List<Action> { Action.FromAnalysis(analysis) };
		// Keep failed definitions too. Only the owning visible row may retry
		// their resource reads; repeated fixed-description requests read cache.
		luaModification[luaScriptName] = actions;
		return actions;
	}

	private static List<string> ExtractPreviewCallKeys(string script, string wantedOwner, string wantedName)
	{
		var result = new List<string>(); var tokens = PreviewSyntax.Tokenize(script);
		for (int i = 0; i < tokens.Count; i++)
			if (PreviewSyntax.TryCall(tokens, i, out var owner, out var name, out var open, out var close) && owner == wantedOwner && name == wantedName && close >= 0)
			{
				var args = PreviewSyntax.Arguments(script, tokens, open, close);
				if (args.Count > 0 && PreviewSyntax.TryString(args[0], out var key) && !result.Contains(key)) result.Add(key);
				i = close;
			}
		return result;
	}

	public static void RefreshPreviewReads(List<Action> actions)
	{
		if (actions == null) return;
		foreach (var action in actions)
		{
			var analysis = action.Analysis;
			if (analysis == null || !analysis.HasReadFailures) continue;
			if (retryArmed.Add(analysis)) continue;
			PreviewAnalyzer.RetryReads(analysis);
			PreviewDiagnostics.Report(analysis, analysis.OptionKey ?? analysis.RootName);
			if (!analysis.HasReadFailures) retryArmed.Remove(analysis);
		}
	}

	public static string Description(List<Action> actions)
	{
		if (actions == null)
		{
			return string.Empty;
		}
		return string.Join("|", from action in actions
			select action.Description into description
			where !string.IsNullOrEmpty(description)
			select description);
	}
}
