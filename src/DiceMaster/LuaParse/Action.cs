using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mortal.Core;

namespace DiceMaster.LuaParse;

public class Action
{
	public ActionType Type { get; set; }

	public bool IsConditional { get; set; }

	public string Content { get; set; }

	public string Description { get; private set; }

	public Action(ActionType type, string content, bool isConditional = false)
	{
		Type = type;
		Content = content;
		IsConditional = isConditional;
		Description = ParseAction();
	}

	private string ParseAction()
	{
		switch (Type)
		{
		case ActionType.ModifyAction:
			return ParseModifyAction();
		case ActionType.BattleScene:
		{
			LocalizationManager instance = LocalizationManager.Instance;
			if (((instance != null) ? instance.LocaleResolver : null) != null)
			{
				string text = (Content.Contains("Combat") ? "System/Combat/Duel" : "System/Combat/Skirmish");
				string text2 = LocalizationManager.Instance.LocaleResolver.GetString(text);
				if (string.IsNullOrEmpty(text2) || text2 == text)
				{
					if (!Content.Contains("Combat"))
					{
						return "난전";
					}
					return "결투";
				}
				return text2;
			}
			if (!Content.Contains("Combat"))
			{
				return "난전";
			}
			return "결투";
		}
		default:
			return string.Empty;
		}
	}

	public static string ToLiteral(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}
		StringBuilder stringBuilder = new StringBuilder(input.Length + 2);
		foreach (char c in input)
		{
			switch (c)
			{
			case '\n':
				stringBuilder.Append("\\n");
				break;
			case '\r':
				stringBuilder.Append("\\r");
				break;
			case '\t':
				stringBuilder.Append("\\t");
				break;
			case '\\':
				stringBuilder.Append("\\\\");
				break;
			case '"':
				stringBuilder.Append("\\\"");
				break;
			default:
				stringBuilder.Append(c);
				break;
			}
		}
		return stringBuilder.ToString();
	}

	private string ParseModifyAction()
	{
		Match match = Regex.Match(Content, "statmodifymanager\\.(\\w+)\\(([^)]*)\\)");
		if (!match.Success)
		{
			return string.Empty;
		}
		string value = match.Groups[1].Value;
		string value2 = match.Groups[2].Value;
		return GetActionDescription(value, value2);
	}

	private string GetActionDescription(string actionFunctionName, string parametersString)
	{
		string[] array = (from p in parametersString.Split(',')
			select p.Trim().Trim('"')).ToArray();
		if (array.Length < 1)
		{
			return string.Empty;
		}
		string text = array[0];
		string text2;
		if (actionFunctionName != null)
		{
			switch (actionFunctionName.Length)
			{
			case 6:
				break;
			case 10:
				goto IL_00a3;
			default:
				goto IL_00e9;
			case 7:
				goto IL_00f0;
			case 9:
				goto IL_015a;
			case 15:
				goto IL_01a4;
			case 19:
				goto IL_024f;
			}
			if (actionFunctionName == "Player")
			{
				text2 = PlayerinfoTranslations.TranslatePlayerstats(text);
				goto IL_01bb;
			}
		}
		goto IL_00e9;
		IL_00e9:
		text2 = text;
		goto IL_01bb;
		IL_00a3:
		char c = actionFunctionName[0];
		if (c != 'A')
		{
			if (c != 'R' || !(actionFunctionName == "RemoveMisc"))
			{
				goto IL_00e9;
			}
			text2 = DataHelper.GetItemName(GameItemType.雜物, text);
		}
		else
		{
			if (!(actionFunctionName == "AddSpecial"))
			{
				goto IL_00e9;
			}
			text2 = DataHelper.GetItemName(GameItemType.貴重品, text);
		}
		goto IL_01bb;
		IL_015a:
		char c2 = actionFunctionName[0];
		if (c2 != 'A')
		{
			if (c2 != 'C' || !(actionFunctionName == "Character"))
			{
				goto IL_00e9;
			}
			text2 = CharacterTranslations.TranslateCharacter(text);
		}
		else
		{
			if (!(actionFunctionName == "AddTalent"))
			{
				goto IL_00e9;
			}
			text2 = DataHelper.GetTalentName(text);
		}
		goto IL_01bb;
		IL_01a4:
		if (!(actionFunctionName == "ModifyEnemyTeam"))
		{
			goto IL_00e9;
		}
		text2 = DataHelper.GetTeamName(text);
		goto IL_01bb;
		IL_01bb:
		if (array.Length == 1)
		{
			if (actionFunctionName == "RemoveMisc")
			{
				string text3 = "버림:";
				LocalizationManager instance = LocalizationManager.Instance;
				if (((instance != null) ? instance.LocaleResolver : null) != null)
				{
					string text4 = LocalizationManager.Instance.LocaleResolver.GetString("System/Action/DiscardPrefix");
					if (!string.IsNullOrEmpty(text4) && text4 != "System/Action/DiscardPrefix")
					{
						text3 = text4;
					}
				}
				return text3 + text2;
			}
			return string.Empty;
		}
		if (!int.TryParse(array[1], out var result))
		{
			return string.Empty;
		}
		return GetDescription(actionFunctionName, text2, result);
		IL_024f:
		if (!(actionFunctionName == "UpdateSetPlayerStat"))
		{
			goto IL_00e9;
		}
		text2 = PlayerinfoTranslations.TranslatePlayerstats(text);
		goto IL_01bb;
		IL_00f0:
		char c3 = actionFunctionName[3];
		if (c3 != 'B')
		{
			if (c3 != 'F')
			{
				if (c3 != 'M' || !(actionFunctionName == "AddMisc"))
				{
					goto IL_00e9;
				}
				text2 = DataHelper.GetItemName(GameItemType.雜物, text);
			}
			else
			{
				if (!(actionFunctionName == "AddFlag") && !(actionFunctionName == "SetFlag"))
				{
					goto IL_00e9;
				}
				text2 = text;
			}
		}
		else
		{
			if (!(actionFunctionName == "AddBook"))
			{
				goto IL_00e9;
			}
			text2 = DataHelper.GetItemName(GameItemType.書籍, text);
		}
		goto IL_01bb;
	}

	private string GetDescription(string actionFunctionName, string translatedId, int value)
	{
		string text = ((value > 0) ? $"+{value}" : $"{value}");
		if (actionFunctionName != null)
		{
			switch (actionFunctionName.Length)
			{
			case 6:
				if (actionFunctionName == "Player")
				{
					return RichText.ColorText(translatedId + " " + text);
				}
				break;
			case 7:
				switch (actionFunctionName[3])
				{
				case 'B':
					if (actionFunctionName == "AddBook")
					{
						return $"{translatedId} x{value}";
					}
					break;
				case 'F':
				{
					string text9;
					if (actionFunctionName == "AddFlag")
					{
						text9 = FlagTranslations.TranslateDevNote(translatedId, value);
						if (string.IsNullOrEmpty(text9))
						{
							text9 = translatedId;
						}
						text9 = AppendFlagIdSuffix(text9, translatedId);
						return $"{text9}({value})";
					}
					if (!(actionFunctionName == "SetFlag"))
					{
						break;
					}
					text9 = FlagTranslations.TranslateDevNote(translatedId, value);
					if (string.IsNullOrEmpty(text9))
					{
						text9 = translatedId;
					}
					text9 = AppendFlagIdSuffix(text9, translatedId);
					string arg2 = "";
					LocalizationManager instance5 = LocalizationManager.Instance;
					if (((instance5 != null) ? instance5.LocaleResolver : null) != null)
					{
						string text10 = LocalizationManager.Instance.LocaleResolver.GetString("System/Flag/ActivatedPrefix");
						if (!string.IsNullOrEmpty(text10) && text10 != "System/Flag/ActivatedPrefix")
						{
							arg2 = text10;
						}
					}
					return $"{arg2}{text9}({value})";
				}
				case 'M':
					if (actionFunctionName == "AddMisc")
					{
						return $"{translatedId} x{value}";
					}
					break;
				}
				break;
			case 9:
				switch (actionFunctionName[0])
				{
				case 'A':
				{
					if (!(actionFunctionName == "AddTalent"))
					{
						break;
					}
					string text7 = "PlayerTalent/Format/LevelUp";
					string format = "기능 {0} Lv{1}";
					LocalizationManager instance4 = LocalizationManager.Instance;
					if (((instance4 != null) ? instance4.LocaleResolver : null) != null)
					{
						string text8 = LocalizationManager.Instance.LocaleResolver.GetString(text7);
						if (!string.IsNullOrEmpty(text8) && text8 != text7)
						{
							format = text8;
						}
					}
					return string.Format(format, translatedId, value);
				}
				case 'C':
				{
					if (!(actionFunctionName == "Character"))
					{
						break;
					}
					string text5 = " 호감도 ";
					LocalizationManager instance3 = LocalizationManager.Instance;
					if (((instance3 != null) ? instance3.LocaleResolver : null) != null)
					{
						string text6 = LocalizationManager.Instance.LocaleResolver.GetString("System/Relation/FavorSuffix");
						if (!string.IsNullOrEmpty(text6) && text6 != "System/Relation/FavorSuffix")
						{
							text5 = text6;
						}
					}
					return RichText.ColorText(translatedId + text5 + text);
				}
				}
				break;
			case 10:
				if (actionFunctionName == "AddSpecial")
				{
					return $"{translatedId} x{value}";
				}
				break;
			case 15:
			{
				if (!(actionFunctionName == "ModifyEnemyTeam"))
				{
					break;
				}
				string text3 = "단결력 ";
				LocalizationManager instance2 = LocalizationManager.Instance;
				if (((instance2 != null) ? instance2.LocaleResolver : null) != null)
				{
					string text4 = LocalizationManager.Instance.LocaleResolver.GetString("System/Team/CohesionSuffix");
					if (!string.IsNullOrEmpty(text4) && text4 != "System/Team/CohesionSuffix")
					{
						text3 = text4;
					}
				}
				return RichText.ColorText(translatedId + text3 + text);
			}
			case 19:
			{
				if (!(actionFunctionName == "UpdateSetPlayerStat"))
				{
					break;
				}
				string arg = "";
				LocalizationManager instance = LocalizationManager.Instance;
				if (((instance != null) ? instance.LocaleResolver : null) != null)
				{
					string text2 = LocalizationManager.Instance.LocaleResolver.GetString("System/Flag/ActivatedPrefix");
					if (!string.IsNullOrEmpty(text2) && text2 != "System/Flag/ActivatedPrefix")
					{
						arg = text2;
					}
				}
				return $"{arg}{translatedId} {value}";
			}
			}
		}
		return string.Empty;
	}

	private static string AppendFlagIdSuffix(string displayName, string flagId)
	{
		if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(flagId))
		{
			return displayName;
		}
		string text = "(" + flagId + ")";
		if (displayName.EndsWith(text))
		{
			return displayName;
		}
		return displayName + text;
	}
}
