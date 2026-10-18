using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using DiceMaster.LiveLog;
using DiceMaster.LogUI;
using Mortal.Core;
using Mortal.Story;
using OBB.Framework.Data;
using OBB.Framework.Utils;
using UnityEngine;

namespace DiceMaster;

public static class DataHelper
{
	private static readonly Dictionary<string, string> _characterNameCache = new Dictionary<string, string>(StringComparer.Ordinal);

	private static readonly object _characterNameCacheLock = new object();

	internal static object LiveMissionFilter;

	public static string ParseSwitchCheckpoint(string checkpoint)
	{
		return DescribeSwitchCheckpoint(checkpoint)?.DescriptionFallback ?? string.Empty;
	}

	public static LogConditionDetails DescribeSwitchCheckpoint(string checkpoint)
	{
		CheckPointManager instance = CheckPointManager.Instance;
		if (instance == null)
		{
			return null;
		}
		SwitchResultConfig[] fieldValue = instance.GetFieldValue<SwitchResultConfig[]>("_switchList");
		SwitchResultData switchResultData = null;
		if (fieldValue != null)
		{
			SwitchResultConfig[] array = fieldValue;
			foreach (SwitchResultConfig switchResultConfig in array)
			{
				if (!(switchResultConfig == null))
				{
					switchResultData = switchResultConfig.GetData(checkpoint);
					if (switchResultData != null)
					{
						break;
					}
				}
			}
		}
		if (switchResultData == null)
		{
			return null;
		}
		// Execute also supports a separate count-based route. Reading its
		// condition list would invent branches that this route never checks;
		// do not execute it or advance its counter to discover the result.
		if (switchResultData.GetFieldValue<bool>("_useCount"))
			return new LogConditionDetails("", null,
				"이 체크포인트는 조건식 대신 실행 횟수를 기준으로 분기합니다.");
		List<ConditionResultItem> fieldValue2 = switchResultData.GetFieldValue<List<ConditionResultItem>>("_items");
		// Keep the old successful empty-description contract for an absent
		// outer list. Malformed items still follow ExtractConditions' existing
		// failure/last-good path rather than becoming a partial branch list.
		if (fieldValue2 == null) return new LogConditionDetails("", null);
		var branches = new List<LogConditionBranch>();
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < fieldValue2.Count; i++)
		{
			// One description evaluation per real item supplies both the
			// structured UI and the compatibility plaintext projection.
			string description = ExtractConditions(fieldValue2[i], out var _);
			// The game's LogicOp returns false for an existing empty item
			// list. An empty block must not resemble an unconditional branch.
			string branchDescription = string.IsNullOrEmpty(description) ?
				"등록된 조건이 없어 이 분기는 충족되지 않습니다." : description;
			branches.Add(new LogConditionBranch("분기 " + (i + 1),
				i == 0 ? "이 조건을 만족하면" : "앞 분기가 모두 불충족일 때 검사", branchDescription));
			stringBuilder.AppendLine("    " + description);
		}
		branches.Add(new LogConditionBranch("그 외 (분기 " + (fieldValue2.Count + 1) + ")",
			"위 분기가 모두 불충족일 때", ""));
		return new LogConditionDetails("위에서부터 검사하며, 처음 만족한 분기로 진행합니다.",
			branches, stringBuilder.ToString());
	}

	public static string ExtractConditions(ConditionResultItem condition, out bool isInPeriod)
	{
		return PreviewFormatting.ExtractConditions(condition, out isInPeriod);
	}

	private static string FormatConditionValName(StatGroupVariable variable)
	{
		if (variable == null)
		{
			return GetLocalizedSystemText("System/Text/Unknown", "(알수없음)", "시스템_조건값_알수없음", "(알수없음)");
		}
		string name = variable.name;
		if (name.StartsWith("旗標_"))
		{
			List<StatValueReference> fieldValue = variable.GetFieldValue<List<StatValueReference>>("_value");
			if (fieldValue != null && fieldValue.Count > 0)
			{
				FlagData fieldValue2 = fieldValue[0].GetFieldValue<FlagData>("_flag");
				if (fieldValue2 != null)
				{
					string text = AppendFlagIdSuffix(GetFlagName(fieldValue2), fieldValue2.name);
					if (!string.IsNullOrEmpty(text))
					{
						return text;
					}
					return name;
				}
			}
		}
		string text2 = TimeTranslations.TranslateChineseTimeExpression(name);
		if (text2 != name)
		{
			return text2;
		}
		string text3 = name.Replace("數值_", "").Replace("時間_", "").Replace("常數_", "");
		string text4 = TryResolveFlagIdToName(text3);
		if (!string.IsNullOrEmpty(text4))
		{
			return text4;
		}
		string text5 = TryResolveStoryIdToName(text3);
		if (!string.IsNullOrEmpty(text5))
		{
			return text5;
		}
		string text6 = TryResolveGetFlagExpressionToName(text3);
		if (!string.IsNullOrEmpty(text6))
		{
			return text6;
		}
		string text7 = TryResolveRelationshipExpressionToName(text3);
		if (!string.IsNullOrEmpty(text7))
		{
			return text7;
		}
		string text8 = PlayerinfoTranslations.TranslatePlayerstats(text3);
		if (!string.IsNullOrEmpty(text8) && text8 != text3)
		{
			return text8;
		}
		string text9 = TryResolveCharacterIdToName(text3);
		if (!string.IsNullOrEmpty(text9))
		{
			return text9;
		}
		string text10 = TryResolveEmbeddedCharacterIdToName(text3);
		if (!string.IsNullOrEmpty(text10))
		{
			return text10;
		}
		string text11 = UiTextMapper.ApplyTranslations(text3);
		if (!string.IsNullOrEmpty(text11))
		{
			return text11;
		}
		return text3;
	}

	private static string TryResolveFlagIdToName(string candidate)
	{
		if (string.IsNullOrEmpty(candidate))
		{
			return null;
		}
		string text = TryResolveFlagValueIdToName(candidate);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		if (!LooksLikeFlagId(candidate))
		{
			return null;
		}
		string flagName = GetFlagName(candidate);
		if (!string.IsNullOrEmpty(flagName) && flagName != candidate)
		{
			return AppendFlagIdSuffix(flagName, candidate);
		}
		return null;
	}

	private static string TryResolveGetFlagExpressionToName(string candidate)
	{
		if (string.IsNullOrEmpty(candidate))
		{
			return null;
		}
		bool flag = false;
		string input = Regex.Replace(candidate, "(?:ext\\.)?GetFlag\\('([^']+)'\\)", delegate(Match m)
		{
			string value = m.Groups[1].Value;
			string flagName = GetFlagName(value);
			if (string.IsNullOrEmpty(flagName) || flagName == value)
			{
				return m.Value;
			}
			flag = true;
			return AppendFlagIdSuffix(flagName, value);
		});
		input = Regex.Replace(input, "(?:ext\\.)?GetFlag\\(\"([^\"]+)\"\\)", delegate(Match m)
		{
			string value = m.Groups[1].Value;
			string flagName = GetFlagName(value);
			if (string.IsNullOrEmpty(flagName) || flagName == value)
			{
				return m.Value;
			}
			flag = true;
			return AppendFlagIdSuffix(flagName, value);
		});
		if (!flag)
		{
			return null;
		}
		return input;
	}

	private static string AppendFlagIdSuffix(string displayName, string flagId)
	{
		if (string.IsNullOrEmpty(displayName))
		{
			return displayName;
		}
		if (string.IsNullOrEmpty(flagId))
		{
			return displayName;
		}
		string text = "(" + flagId + ")";
		if (displayName.EndsWith(text, StringComparison.Ordinal))
		{
			return displayName;
		}
		return displayName + text;
	}

	private static string TryResolveFlagValueIdToName(string candidate)
	{
		Match match = Regex.Match(candidate, "^([A-Za-z][A-Za-z0-9]+)_([0-9]+)$");
		if (!match.Success || !int.TryParse(match.Groups[2].Value, out var result))
		{
			return null;
		}
		string value = match.Groups[1].Value;
		MissionManagerData instance = MissionManagerData.Instance;
		FlagData flagData = ((instance != null) ? instance.GetFlag(value) : null);
		if (flagData == null)
		{
			return null;
		}
		string text = TryTranslateFlagDevNote(flagData.DevNote, result);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		string text2 = TryTranslateStructuredFlagText(flagData.DevNote, result);
		if (!string.IsNullOrEmpty(text2))
		{
			return text2;
		}
		string text3 = $"Flag/Desc/{value}_{result}";
		string localizedSystemText = GetLocalizedSystemText(text3, null, "플래그값설명", $"{value}_{result}");
		if (!string.IsNullOrEmpty(localizedSystemText) && localizedSystemText != text3)
		{
			return localizedSystemText;
		}
		return null;
	}

	private static string TryResolveStoryIdToName(string candidate)
	{
		if (string.IsNullOrEmpty(candidate))
		{
			return null;
		}
		if (candidate.StartsWith("Ch_", StringComparison.OrdinalIgnoreCase) || candidate.StartsWith("S", StringComparison.Ordinal) || candidate.IndexOf('_') >= 0)
		{
			string text = TryResolveLegendInfoName(candidate);
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
		}
		return null;
	}

	private static string TryResolveLegendInfoName(string id)
	{
		ILocaleResolver localeResolver = LocalizationManager.Instance?.LocaleResolver;
		if (localeResolver == null || string.IsNullOrEmpty(id))
		{
			return null;
		}
		string text = "LegendInfo/" + id;
		string text2 = localeResolver.GetString(text);
		if (!string.IsNullOrEmpty(text2) && text2 != text)
		{
			return text2;
		}
		return null;
	}

	private static string TryResolveCharacterIdToName(string candidate)
	{
		if (string.IsNullOrEmpty(candidate))
		{
			return null;
		}
		lock (_characterNameCacheLock)
		{
			if (_characterNameCache.TryGetValue(candidate, out var value))
			{
				return string.IsNullOrEmpty(value) ? null : value;
			}
		}
		string text = null;
		ILocaleResolver localeResolver = LocalizationManager.Instance?.LocaleResolver;
		if (localeResolver != null)
		{
			string text2 = "Character/" + candidate;
			string text3 = localeResolver.GetString(text2);
			if (!string.IsNullOrEmpty(text3) && text3 != text2)
			{
				text = text3;
			}
		}
		lock (_characterNameCacheLock)
		{
			_characterNameCache[candidate] = text ?? string.Empty;
			return text;
		}
	}

	private static string TryResolveEmbeddedCharacterIdToName(string candidate)
	{
		if (string.IsNullOrEmpty(candidate))
		{
			return null;
		}
		bool flag = false;
		string result = Regex.Replace(candidate, "(special\\d+|girl\\d+|brother\\d+|sister\\d+|trainee(?:_girl)?\\d+)", delegate(Match m)
		{
			string text = TryResolveCharacterIdToName(m.Value);
			if (string.IsNullOrEmpty(text))
			{
				return m.Value;
			}
			flag = true;
			return text;
		}, RegexOptions.IgnoreCase);
		if (!flag)
		{
			return null;
		}
		return result;
	}

	private static string TryResolveRelationshipExpressionToName(string candidate)
	{
		if (string.IsNullOrEmpty(candidate))
		{
			return null;
		}
		int num = candidate.IndexOf("GetRelationship('", StringComparison.Ordinal);
		if (num < 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder(candidate.Length + 32);
		int num2 = 0;
		bool flag = false;
		while (num >= 0)
		{
			int num3 = num + "GetRelationship('".Length;
			int num4 = candidate.IndexOf("')", num3, StringComparison.Ordinal);
			if (num4 < 0)
			{
				break;
			}
			string value = TryResolveCharacterIdToName(candidate.Substring(num3, num4 - num3));
			if (string.IsNullOrEmpty(value))
			{
				num = candidate.IndexOf("GetRelationship('", num4 + 2, StringComparison.Ordinal);
				continue;
			}
			stringBuilder.Append(candidate, num2, num - num2);
			stringBuilder.Append("호감도_");
			stringBuilder.Append(value);
			num2 = num4 + 2;
			num = candidate.IndexOf("GetRelationship('", num2, StringComparison.Ordinal);
			flag = true;
		}
		if (!flag)
		{
			return null;
		}
		if (num2 < candidate.Length)
		{
			stringBuilder.Append(candidate, num2, candidate.Length - num2);
		}
		return stringBuilder.ToString();
	}

	private static bool LooksLikeFlagId(string value)
	{
		if (string.IsNullOrEmpty(value) || value.Length < 2 || !char.IsLetter(value[0]))
		{
			return false;
		}
		bool result = false;
		for (int i = 1; i < value.Length; i++)
		{
			char c = value[i];
			if (char.IsDigit(c))
			{
				result = true;
			}
			else if (c != '_')
			{
				return false;
			}
		}
		return result;
	}

	public static Dictionary<MissionCheckData, (string Key, string Description)> GetMissions()
	{
		Dictionary<MissionCheckData, (string, string)> dictionary = new Dictionary<MissionCheckData, (string, string)>();
		MissionManagerData instance = MissionManagerData.Instance;
		if (instance == null)
		{
			return dictionary;
		}
		SubMissionsData fieldValue = instance.GetFieldValue<SubMissionsData>("_subMissions");
		if (fieldValue == null || ((CollectionData<MissionData>)fieldValue).List == null)
		{
			return dictionary;
		}
		foreach (MissionData item2 in ((CollectionData<MissionData>)fieldValue).List)
		{
			if ((LiveMissionFilter != null && (object)item2 != LiveMissionFilter) || item2 == null)
			{
				continue;
			}
			string keyToAttempt = "Mission/Name/" + item2.name;
			string keyToAttempt2 = "Mission/Desc/" + item2.GetFieldValue<string>("_desc");
			string localizedSystemText = GetLocalizedSystemText(keyToAttempt, item2.name, "미션명", item2.name);
			string localizedSystemText2 = GetLocalizedSystemText(keyToAttempt2, item2.GetFieldValue<string>("_desc"), "미션설명", item2.GetFieldValue<string>("_desc"));
			string text = ((localizedSystemText2.IsNullOrWhiteSpace() || localizedSystemText2 == item2.GetFieldValue<string>("_desc")) ? localizedSystemText : localizedSystemText2);
			if (string.IsNullOrEmpty(text) || text == item2.name)
			{
				text = localizedSystemText;
				if (string.IsNullOrEmpty(text) || text == item2.name)
				{
					text = item2.name;
				}
			}
			string currentKey = item2.CurrentKey;
			if (currentKey == "clear")
			{
				continue;
			}
			MissionCheckData byKey = item2.GetByKey(currentKey);
			if (!(byKey != null) || !IsValidMission(byKey))
			{
				continue;
			}
			string keyToAttempt3 = "Mission/CheckName/" + byKey.name;
			string keyToAttempt4 = "Mission/CheckDesc/" + byKey.name;
			string fieldValue2 = byKey.GetFieldValue<string>("_desc");
			string name = byKey.name;
			string localizedSystemText3 = GetLocalizedSystemText(keyToAttempt4, fieldValue2, "미션단계설명", fieldValue2);
			string localizedSystemText4 = GetLocalizedSystemText(keyToAttempt3, name, "미션단계명", name);
			string text2 = ((localizedSystemText3.IsNullOrWhiteSpace() || localizedSystemText3 == fieldValue2) ? localizedSystemText4 : localizedSystemText3);
			if (string.IsNullOrEmpty(text2) || text2 == name)
			{
				text2 = name;
			}
			string item = text + " - " + text2;
			string text3 = LocationTranslations.TranslateLocation(byKey.Position.ToString().Replace("養成_", ""));
			if (!GetMissionPeriod(byKey, out var periodText))
			{
				continue;
			}
			bool isInPeriod;
			List<string> list = ExtractConditions(byKey, out isInPeriod);
			if (!isInPeriod)
			{
				continue;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(GetLocalizedSystemText("System/Mission/LocationPrefix", "위치 :", "시스템_미션_위치접두사", "위치 :") + " " + text3);
			stringBuilder.AppendLine(GetLocalizedSystemText("System/Mission/TimePrefix", "기한 :", "시스템_미션_시간접두사", "기한 :") + " " + periodText);
			stringBuilder.AppendLine(GetLocalizedSystemText("System/Mission/ConditionPrefix", "조건 :", "시스템_미션_조건접두사", "조건 :"));
			foreach (string item3 in list)
			{
				stringBuilder.AppendLine("    " + item3);
			}
			dictionary[byKey] = (item, stringBuilder.ToString());
		}
		return dictionary;
	}

	private static bool IsValidMission(MissionCheckData checkData)
	{
		bool fieldValue = checkData.GetFieldValue<bool>("_dayCheck");
		bool flag = checkData.Position > PositionType.無;
		ConditionResultItem[] fieldValue2 = checkData.GetFieldValue<ConditionResultItem[]>("_conditions");
		bool flag2 = fieldValue2 != null && fieldValue2.Length != 0;
		if (!string.IsNullOrEmpty(LoadLuaScript(checkData.StartScript)))
		{
			return fieldValue || flag || flag2;
		}
		return false;
	}

	private static List<string> ExtractConditions(MissionCheckData checkData, out bool isInPeriod)
	{
		List<string> list = new List<string>();
		ConditionResultItem[] fieldValue = checkData.GetFieldValue<ConditionResultItem[]>("_conditions");
		isInPeriod = true;
		if (fieldValue != null)
		{
			ConditionResultItem[] array = fieldValue;
			foreach (ConditionResultItem condition in array)
			{
				list.Add(ExtractConditions(condition, out var isInPeriod2));
				if (!isInPeriod2)
				{
					isInPeriod = false;
				}
			}
		}
		return list;
	}

	public static bool GetMissionPeriod(MissionCheckData checkData, out string periodText)
	{
		string localizedSystemText = GetLocalizedSystemText("System/Time/Unlimited", "무제한", "시스템_시간_무제한", "무제한");
		if (!checkData.GetFieldValue<bool>("_dayCheck"))
		{
			periodText = localizedSystemText;
			return true;
		}
		PlayerStatManagerData instance = PlayerStatManagerData.Instance;
		GameTime gameTime = ((instance != null) ? instance.GameTime : null);
		TimeCheckType fieldValue = checkData.GetFieldValue<TimeCheckType>("_timeCheckType");
		string localizedSystemText2 = GetLocalizedSystemText("System/Time/YearSuffix", "년", "시간_단위_년", "년");
		string localizedSystemText3 = GetLocalizedSystemText("System/Time/MonthSuffix", "월", "시간_단위_월", "월");
		string text = " ";
		switch (fieldValue)
		{
		case TimeCheckType.特定:
		{
			GameTime fieldValue4 = checkData.GetFieldValue<GameTime>("_specificTime");
			string text8 = TimeTranslations.TranslateNumberOrMonth(fieldValue4.YearText);
			string text9 = TimeTranslations.TranslateNumberOrMonth(fieldValue4.MonthText);
			string text10 = TimeTranslations.TranslateStage(fieldValue4.StageText);
			periodText = text8 + localizedSystemText2 + text + text9 + localizedSystemText3 + text + text10;
			return gameTime == fieldValue4;
		}
		case TimeCheckType.區間:
		{
			GameTime fieldValue2 = checkData.GetFieldValue<GameTime>("_minTime");
			GameTime fieldValue3 = checkData.GetFieldValue<GameTime>("_maxTime");
			string text2 = TimeTranslations.TranslateNumberOrMonth(fieldValue2.YearText);
			string text3 = TimeTranslations.TranslateNumberOrMonth(fieldValue2.MonthText);
			string text4 = TimeTranslations.TranslateStage(fieldValue2.StageText);
			string text5 = TimeTranslations.TranslateNumberOrMonth(fieldValue3.YearText);
			string text6 = TimeTranslations.TranslateNumberOrMonth(fieldValue3.MonthText);
			string text7 = TimeTranslations.TranslateStage(fieldValue3.StageText);
			string localizedSystemText4 = GetLocalizedSystemText("System/Time/RangeSeparator", " - ", "시간_구분자_범위", " - ");
			periodText = text2 + localizedSystemText2 + text + text3 + localizedSystemText3 + text + text4 + localizedSystemText4 + text5 + localizedSystemText2 + text + text6 + localizedSystemText3 + text + text7;
			if (gameTime >= fieldValue2)
			{
				return gameTime <= fieldValue3;
			}
			return false;
		}
		default:
			periodText = localizedSystemText;
			return true;
		}
	}

	public static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct
	{
		return EnumUtils.TryParseByStringValue<TEnum>(value, out result);
	}

	public static string GetItemName(GameItemType itemType, string id)
	{
		ItemDatabase instance = ItemDatabase.Instance;
		IGameItem gameItem = ((instance != null) ? instance.GetItem(itemType, id) : null);
		if (gameItem == null)
		{
			UntranslatedLogger.LogUntranslated("아이템_존재안함", id, id);
			return id;
		}
		string localizedSystemText = GetLocalizedSystemText(gameItem.GetNameKey(), id, "아이템명", id);
		if (localizedSystemText == id)
		{
			return id;
		}
		return localizedSystemText;
	}

	public static string GetTalentName(string id)
	{
		PlayerStatManagerData instance = PlayerStatManagerData.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			PlayerTalentDataCollection talents = instance.Talents;
			obj = ((talents != null) ? talents.Get(id) : null);
		}
		PlayerTalentData playerTalentData = (PlayerTalentData)obj;
		if (playerTalentData == null)
		{
			UntranslatedLogger.LogUntranslated("기능_존재안함", id, id);
			return id;
		}
		string localizedSystemText = GetLocalizedSystemText(playerTalentData.GetIdKey(), id, "기능명", id);
		if (localizedSystemText == id)
		{
			return id;
		}
		return localizedSystemText;
	}

	public static string GetStoryName(string id)
	{
		string localizedSystemText = GetLocalizedSystemText("LegendInfo/" + id, id, "전기명", id);
		if (localizedSystemText == id)
		{
			return id;
		}
		return localizedSystemText;
	}

	public static string GetTeamName(string id)
	{
		PlayerStatManagerData instance = PlayerStatManagerData.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			BattleTeamData enemyTeam = instance.EnemyTeam;
			obj = ((enemyTeam != null) ? enemyTeam.Get(id) : null);
		}
		BattleTeamStat battleTeamStat = (BattleTeamStat)obj;
		if (battleTeamStat == null)
		{
			UntranslatedLogger.LogUntranslated("팀_존재안함", id, id);
			return id;
		}
		string localizedSystemText = GetLocalizedSystemText(battleTeamStat.GetNameKey(), id, "팀명", id);
		if (localizedSystemText == id)
		{
			return id;
		}
		return localizedSystemText;
	}

	public static string GetEventName(PositionEventData eventData)
	{
		if (eventData == null)
		{
			return string.Empty;
		}
		StoryMappingItem fieldValue = eventData.GetFieldValue<StoryMappingItem>("_config");
		string text = ((fieldValue != null) ? fieldValue.name : null);
		string fieldValue2 = eventData.GetFieldValue<string>("_devNote");
		if (!fieldValue2.IsNullOrWhiteSpace())
		{
			text = fieldValue2;
		}
		return RichText.Golden(UiTextMapper.ApplyTranslations(text));
	}

	public static string LoadLuaScript(string scriptName)
	{
		TextAsset textAsset = Resources.Load<TextAsset>("story/chinesetraditional/" + scriptName.ToLower());
		return ((textAsset != null) ? textAsset.text : null) ?? string.Empty;
	}

	// This boundary only reads the game's local Resources namespace. A missing
	// asset differs from a successfully loaded asset whose text is empty.
	public static DiceMaster.LiveLog.ScriptReadResult ReadPreviewScript(string scriptName)
	{
		if (string.IsNullOrWhiteSpace(scriptName) || scriptName.StartsWith("/") ||
			scriptName.Contains(":") || scriptName.Contains("\\"))
			return DiceMaster.LiveLog.ScriptReadResult.Failed();
		foreach (string part in scriptName.Split('/'))
			if (part == ".." || part == "." || part.Length == 0)
				return DiceMaster.LiveLog.ScriptReadResult.Failed();
		foreach (char character in scriptName)
			if (char.IsControl(character)) return DiceMaster.LiveLog.ScriptReadResult.Failed();
		try
		{
			TextAsset asset = Resources.Load<TextAsset>("story/chinesetraditional/" + scriptName.ToLowerInvariant());
			return asset == null ? DiceMaster.LiveLog.ScriptReadResult.Missing() :
				DiceMaster.LiveLog.ScriptReadResult.Success(asset.text ?? string.Empty);
		}
		catch (Exception) { return DiceMaster.LiveLog.ScriptReadResult.Failed(); }
	}

	public static string GetCompareOperator(StatCompareType compareType)
	{
		return (int)compareType switch
		{
			0 => "=", 
			1 => "!=", 
			2 => "<", 
			3 => "<=", 
			4 => ">", 
			5 => ">=", 
			_ => GetLocalizedSystemText("System/Operator/Unknown", "??", "시스템_연산자_알수없음", "??"), 
		};
	}

	public static string GetLogicOperator(LogicOperatorType logicOp)
	{
		return logicOp switch
		{
			LogicOperatorType.OR => GetLocalizedSystemText("System/Logic/Or", "또는", "시스템_논리_또는", "또는"), 
			LogicOperatorType.AND => GetLocalizedSystemText("System/Logic/And", " | ", "시스템_논리_그리고", "그리고"), 
			_ => GetLocalizedSystemText("System/Logic/Unknown", "알수없음", "시스템_논리_알수없음", "알수없음"), 
		};
	}

	public static string GetFlagName(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return string.Empty;
		}
		MissionManagerData instance = MissionManagerData.Instance;
		FlagData flagData = ((instance != null) ? instance.GetFlag(id) : null);
		if (flagData != null)
		{
			return GetFlagName(flagData);
		}
		return GetLocalizedSystemText("Flag/Name/" + id, id, "플래그명", id);
	}

	public static string GetFlagName(FlagData flagData)
	{
		if (flagData == null)
		{
			return string.Empty;
		}
		string name = flagData.name;
		string localizedSystemText = GetLocalizedSystemText("Flag/Name/" + name, name, "플래그명", name);
		string devNote = flagData.DevNote;
		string text = TryTranslateFlagDevNote(devNote, flagData.State);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		string text2 = TryTranslateStructuredFlagText(devNote, flagData.State);
		if (!string.IsNullOrEmpty(text2))
		{
			return text2;
		}
		if (localizedSystemText != name)
		{
			string text3 = UiTextMapper.ApplyTranslations(localizedSystemText);
			if (!string.IsNullOrEmpty(text3) && text3 != localizedSystemText)
			{
				if (!IsMixedHanHangul(text3))
				{
					return text3;
				}
				string text4 = TryTranslateStructuredFlagText(localizedSystemText, flagData.State);
				if (!string.IsNullOrEmpty(text4))
				{
					return text4;
				}
			}
			return localizedSystemText;
		}
		if (!string.IsNullOrEmpty(devNote))
		{
			string text5 = UiTextMapper.ApplyTranslations(devNote);
			if (!string.IsNullOrEmpty(text5) && text5 != devNote)
			{
				return text5;
			}
		}
		return name;
	}

	private static string TryTranslateFlagDevNote(string devNote, int state)
	{
		if (string.IsNullOrEmpty(devNote))
		{
			return null;
		}
		string text = FlagTranslations.TranslateDevNote(devNote, state);
		if (string.IsNullOrEmpty(text) || text == devNote || text.StartsWith(devNote))
		{
			return null;
		}
		string text2 = UiTextMapper.ApplyTranslations(text);
		if (string.IsNullOrEmpty(text2))
		{
			return text;
		}
		if (!IsMixedHanHangul(text2))
		{
			return text2;
		}
		return text;
	}

	private static string TryTranslateStructuredFlagText(string source, int state)
	{
		if (string.IsNullOrEmpty(source))
		{
			return null;
		}
		string text = source.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		string[] array = text.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 0)
		{
			return null;
		}
		string text2 = array[0].Trim();
		if (string.IsNullOrEmpty(text2))
		{
			return null;
		}
		string text3 = UiTextMapper.ApplyTranslations(text2);
		if (IsMixedHanHangul(text3))
		{
			text3 = text2;
		}
		Dictionary<int, string> dictionary = new Dictionary<int, string>();
		for (int i = 1; i < array.Length; i++)
		{
			string text4 = array[i].Trim();
			if (!string.IsNullOrEmpty(text4))
			{
				Match match = Regex.Match(text4, "^([0-9]+)\\s*[\\.:：、\\)\\]]?\\s*(.+)$");
				if (match.Success && int.TryParse(match.Groups[1].Value, out var result))
				{
					string value = match.Groups[2].Value.Trim();
					dictionary[result] = value;
				}
			}
		}
		if (dictionary.TryGetValue(state, out var value2))
		{
			string text5 = UiTextMapper.ApplyTranslations(value2);
			if (IsMixedHanHangul(text5))
			{
				text5 = value2;
			}
			string text6 = (string.IsNullOrEmpty(text3) ? text2 : text3);
			if (!string.IsNullOrEmpty(text5))
			{
				return text6 + "(" + text5 + ")";
			}
			return text6;
		}
		if (!string.IsNullOrEmpty(text3) && text3 != text2)
		{
			if (IsMixedHanHangul(text3))
			{
				return null;
			}
			return text3;
		}
		if (!ContainsHan(text2))
		{
			return text2;
		}
		return null;
	}

	private static bool ContainsHan(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		foreach (char c in value)
		{
			if (c >= '一' && c <= '鿿')
			{
				return true;
			}
		}
		return false;
	}

	private static bool ContainsHangul(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		foreach (char c in value)
		{
			if (c >= '가' && c <= '힣')
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsMixedHanHangul(string value)
	{
		if (ContainsHan(value))
		{
			return ContainsHangul(value);
		}
		return false;
	}

	private static string GetLocalizedSystemText(string keyToAttempt, string defaultValue, string categoryForLog = "시스템", string originalIdentifierIfKeyFailed = null)
	{
		LocalizationManager instance = LocalizationManager.Instance;
		if (((instance != null) ? instance.LocaleResolver : null) != null && !string.IsNullOrEmpty(keyToAttempt))
		{
			string text = LocalizationManager.Instance.LocaleResolver.GetString(keyToAttempt);
			if (!string.IsNullOrEmpty(text) && text != keyToAttempt)
			{
				return text;
			}
			if (text == keyToAttempt || string.IsNullOrEmpty(text))
			{
				UntranslatedLogger.LogUntranslated(categoryForLog, keyToAttempt, string.IsNullOrEmpty(originalIdentifierIfKeyFailed) ? keyToAttempt : originalIdentifierIfKeyFailed);
			}
		}
		return defaultValue;
	}

	private static string GetLocalizedSystemTextForTranslationsInFlag(string key, string defaultValue, string categoryForLog = "시스템_플래그_헬퍼")
	{
		return GetLocalizedSystemText(key, defaultValue, categoryForLog, key);
	}
}
