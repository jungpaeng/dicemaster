using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
		luaOptions.Clear();
		luaSwitchCheck.Clear();
		luaPositionWork.Clear();
		luaModification.Clear();
		luaOptionStoryText.Clear();
		luaSwitchStoryText.Clear();
		luaSwitchBranchStoryText.Clear();
		DicePlugin.LogInfo("LuaParser 내부 캐시(luaOptions, luaSwitchCheck 등)가 모두 청소되었습니다!");
	}

	public Dictionary<string, List<Action>> ExtractOptionActionsFromLua(string friendlyName, string luaScript = "")
	{
		Dictionary<string, List<Action>> dictionary = new Dictionary<string, List<Action>>();
		if (!luaOptions.ContainsKey(friendlyName))
		{
			luaOptions[friendlyName] = new List<string>();
			Dictionary<string, List<string>> dictionary2 = new Dictionary<string, List<string>>();
			luaOptionStoryText[friendlyName] = dictionary2;
			MatchCollection matchCollection = indexMarkerRegex.Matches(luaScript);
			for (int i = 0; i < matchCollection.Count; i++)
			{
				string value = matchCollection[i].Groups[1].Value;
				luaOptions[friendlyName].Add(value);
				int num = matchCollection[i].Index + matchCollection[i].Length;
				int num2 = luaScript.Length;
				if (i + 1 < matchCollection.Count)
				{
					num2 = Math.Min(num2, matchCollection[i + 1].Index);
				}
				num2 = Math.Min(num2, FindOptionBranchEnd(luaScript, num, num2));
				string blockContent = luaScript.Substring(num, num2 - num);
				string text = ExtractFirstStoryTextKey(blockContent);
				dictionary2[value] = (string.IsNullOrEmpty(text) ? new List<string>() : new List<string> { text });
				if (OptionResult.ContainsKey(value))
				{
					dictionary[value] = OptionResult[value];
					continue;
				}
				List<Action> list = new List<Action>();
				AddActionsToList(blockContent, modifyRegex, ActionType.ModifyAction, list);
				AddActionsToList(blockContent, battleScenesRegex, ActionType.BattleScene, list);
				dictionary[value] = list;
				OptionResult[value] = list;
			}
			ExtractOptionActionsFromChooseBlocks(luaScript, luaOptions[friendlyName], dictionary2, dictionary);
		}
		else
		{
			List<string> list2 = luaOptions[friendlyName];
			if (!luaOptionStoryText.ContainsKey(friendlyName))
			{
				luaOptionStoryText[friendlyName] = new Dictionary<string, List<string>>();
			}
			Dictionary<string, List<string>> dictionary3 = luaOptionStoryText[friendlyName];
			foreach (string item in list2)
			{
				if (OptionResult.ContainsKey(item))
				{
					dictionary.Add(item, OptionResult[item]);
				}
				else
				{
					dictionary.Add(item, new List<Action>());
					DicePlugin.LogError("CRITICAL ERROR: luaOptions에 있는 키 '" + item + "'가 OptionResult에 없습니다! (friendlyName: " + friendlyName + ") 이 문제는 게임을 불안정하게 만들 수 있습니다!");
				}
				if (!dictionary3.ContainsKey(item))
				{
					dictionary3[item] = new List<string>();
				}
			}
		}
		return dictionary;
	}

	private void ExtractOptionActionsFromChooseBlocks(string luaScript, List<string> optionKeys, Dictionary<string, List<string>> optionStoryMap, Dictionary<string, List<Action>> optionActionMap)
	{
		if (string.IsNullOrWhiteSpace(luaScript) || optionKeys == null || optionStoryMap == null || optionActionMap == null)
		{
			return;
		}
		foreach (Match item in chooseAssignmentRegex.Matches(luaScript))
		{
			string text = item.Groups[1].Value.Trim();
			string text2 = ParseFirstLuaArgument(item.Groups[2].Value);
			if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
			{
				continue;
			}
			int startIndex = item.Index + item.Length;
			int num = FindOptionTableDeclaration(luaScript, text2, item.Index);
			if (num < 0)
			{
				continue;
			}
			Dictionary<string, string> dictionary = ParseOptionTableItems(luaScript, text2, num, item.Index);
			if (dictionary.Count == 0)
			{
				continue;
			}
			int num2 = FindIfLineForSwitchVar(luaScript, text, startIndex, luaScript.Length);
			if (num2 < 0)
			{
				continue;
			}
			int num3 = FindLineEnd(luaScript, num2, luaScript.Length);
			if (num3 >= luaScript.Length)
			{
				continue;
			}
			Dictionary<string, string> dictionary2 = ExtractFirstStoryTextByBranchFromIfElseChain(luaScript, text, num2, num3, luaScript.Length);
			Dictionary<string, string> dictionary3 = ExtractBranchBlockByLabelFromIfElseChain(luaScript, text, num2, num3, luaScript.Length);
			foreach (KeyValuePair<string, string> item2 in dictionary)
			{
				string key = item2.Key;
				string value = item2.Value;
				if (!string.IsNullOrWhiteSpace(value))
				{
					if (!optionKeys.Contains(value))
					{
						optionKeys.Add(value);
					}
					if (!optionStoryMap.TryGetValue(value, out var value2))
					{
						value2 = (optionStoryMap[value] = new List<string>());
					}
					if (dictionary2.TryGetValue(key, out var value3) && !string.IsNullOrWhiteSpace(value3) && !value2.Contains(value3))
					{
						value2.Add(value3);
					}
					List<Action> list2 = null;
					if (dictionary3.TryGetValue(key, out var value4))
					{
						list2 = BuildActionListFromBlock(value4);
					}
					if (!optionActionMap.TryGetValue(value, out var value5) || value5 == null || value5.Count == 0)
					{
						value5 = (optionActionMap[value] = list2 ?? new List<Action>());
						OptionResult[value] = value5;
					}
					else if (!OptionResult.ContainsKey(value))
					{
						OptionResult[value] = value5;
					}
				}
			}
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
		List<Action> list = new List<Action>();
		if (string.IsNullOrWhiteSpace(blockContent))
		{
			return list;
		}
		AddActionsToList(blockContent, modifyRegex, ActionType.ModifyAction, list);
		AddActionsToList(blockContent, battleScenesRegex, ActionType.BattleScene, list);
		return list;
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
		if (luaSwitchBranchStoryText.ContainsKey(friendlyName))
		{
			return luaSwitchBranchStoryText[friendlyName];
		}
		Dictionary<string, Dictionary<string, string>> dictionary = new Dictionary<string, Dictionary<string, string>>();
		List<Match> list = switchAssignmentRegex.Matches(luaScript).Cast<Match>().ToList();
		for (int i = 0; i < list.Count; i++)
		{
			Match match = list[i];
			string value = match.Groups[1].Value;
			string text = ParseFirstLuaArgument(match.Groups[2].Value);
			if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			int startIndex = match.Index + match.Length;
			int length = luaScript.Length;
			int num = FindIfLineForSwitchVar(luaScript, value, startIndex, length);
			if (num < 0)
			{
				continue;
			}
			int num2 = FindLineEnd(luaScript, num, length);
			if (num2 >= length)
			{
				continue;
			}
			Dictionary<string, string> dictionary2 = ExtractFirstStoryTextByBranchFromIfElseChain(luaScript, value, num, num2, length);
			if (!dictionary.TryGetValue(text, out var value2))
			{
				value2 = (dictionary[text] = new Dictionary<string, string>());
			}
			foreach (KeyValuePair<string, string> item in dictionary2)
			{
				if (!value2.ContainsKey(item.Key))
				{
					value2[item.Key] = item.Value;
				}
				else if (string.IsNullOrWhiteSpace(value2[item.Key]) && !string.IsNullOrWhiteSpace(item.Value))
				{
					value2[item.Key] = item.Value;
				}
			}
		}
		luaSwitchBranchStoryText[friendlyName] = dictionary;
		return dictionary;
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
		if (string.IsNullOrWhiteSpace(blockContent))
		{
			return string.Empty;
		}
		Match match = storyTextRegex.Match(blockContent);
		if (!match.Success)
		{
			return string.Empty;
		}
		return ParseFirstLuaArgument(match.Groups[1].Value);
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
		if (luaSwitchCheck.ContainsKey(friendlyName))
		{
			return luaSwitchCheck[friendlyName];
		}
		List<string> list = ExtractMatches(luaScript, switchRegex);
		luaSwitchCheck[friendlyName] = list;
		return list;
	}

	public List<string> ExtractPositionWorkFromLua(string friendlyName, string luaScript = "")
	{
		if (luaPositionWork.ContainsKey(friendlyName))
		{
			return luaPositionWork[friendlyName];
		}
		List<string> list = ExtractMatches(luaScript, positionRegex);
		luaPositionWork[friendlyName] = list;
		return list;
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
		if (string.IsNullOrWhiteSpace(rawArgs))
		{
			return string.Empty;
		}
		Match match = Regex.Match(rawArgs, "^\\s*['\\\"]([^'\\\"]+)['\\\"]");
		if (match.Success)
		{
			return match.Groups[1].Value.Trim();
		}
		int num = rawArgs.IndexOf(',');
		return ((num >= 0) ? rawArgs.Substring(0, num) : rawArgs).Trim().Trim('\'', '"');
	}

	public List<Action> ExtractActionsFromLua(string luaScriptName)
	{
		if (luaModification.ContainsKey(luaScriptName))
		{
			return luaModification[luaScriptName];
		}
		string[] array = DataHelper.LoadLuaScript(luaScriptName).Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
		List<Action> list = new List<Action>();
		bool isConditional = false;
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string text = array2[i].Trim();
			if (text.StartsWith("if") || text.StartsWith("elseif"))
			{
				isConditional = true;
			}
			else if (text.StartsWith("end"))
			{
				isConditional = false;
			}
			foreach (Match item in modifyRegex.Matches(text))
			{
				list.Add(new Action(ActionType.ModifyAction, item.Value, isConditional));
			}
			foreach (Match item2 in battleScenesRegex.Matches(text))
			{
				list.Add(new Action(ActionType.BattleScene, item2.Value, isConditional));
			}
		}
		luaModification[luaScriptName] = list;
		return list;
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
