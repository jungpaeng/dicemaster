using Mortal.Core;

namespace DiceMaster;

internal static class PlayerinfoTranslations
{
	public static string TranslatePlayerstats(string mystats_id_or_original_name)
	{
		if (string.IsNullOrEmpty(mystats_id_or_original_name))
		{
			return GetLocalizedSystemTextForTranslations("System/Stat/Unknown", "(알수없음)", "시스템_스탯");
		}
		string text = "PlayerStat/";
		string text2 = mystats_id_or_original_name;
		switch (mystats_id_or_original_name)
		{
		case "體力":
			text2 = "life";
			break;
		case "內力":
			text2 = "stamina";
			break;
		case "輕功":
			text2 = "dexterity";
			break;
		case "刀劍":
			text2 = "m-sword";
			break;
		case "暗器":
			text2 = "m-projectile";
			break;
		case "拳掌":
			text2 = "m-fist";
			break;
		case "學問":
			text2 = "literacy";
			break;
		case "嘴力":
			text2 = "talking";
			break;
		case "抗毒":
			text2 = "poison-resistance";
			break;
		case "抗麻":
			text2 = "paralysis-resistance";
			break;
		case "性情":
			text2 = "disposition";
			break;
		case "處世":
			text2 = "behaviour";
			break;
		case "道德":
			text2 = "karma";
			break;
		case "修養":
			text2 = "training";
			break;
		case "銀兩":
			text2 = "money";
			break;
		case "心理衛生":
			text2 = "mental";
			break;
		case "門派名聲":
			text2 = "fame";
			break;
		case "向心力":
			text2 = "team";
			break;
		case "命運":
			text2 = "fate";
			break;
		case "陰陽內功":
			text2 = "internal";
			break;
		case "心上人":
			text2 = "lover";
			break;
		case "廚藝":
			text2 = "cook";
			break;
		case "棋力":
			text2 = "chess";
			break;
		case "行動次數":
			text2 = "action";
			break;
		case "個人貢獻度":
			text2 = "contribution";
			break;
		case "門派規模":
			text2 = "faction";
			break;
		case "門派資產":
			text2 = "assets";
			break;
		}
		string text3 = text + text2;
		string text4 = null;
		LocalizationManager instance = LocalizationManager.Instance;
		if (((instance != null) ? instance.LocaleResolver : null) != null)
		{
			text4 = LocalizationManager.Instance.LocaleResolver.GetString(text3);
		}
		if (string.IsNullOrEmpty(text4) || text4 == text3)
		{
			UntranslatedLogger.LogUntranslated("스탯명", text3, mystats_id_or_original_name);
			GetLocalizedSystemTextForTranslations("System/Stat/UntranslatedSuffix", "(스탯 미번역)", "시스템_스탯");
			return mystats_id_or_original_name;
		}
		return text4;
	}

	private static string GetLocalizedSystemTextForTranslations(string key, string defaultValue, string categoryForLog = "시스템_플레이어정보")
	{
		LocalizationManager instance = LocalizationManager.Instance;
		if (((instance != null) ? instance.LocaleResolver : null) != null)
		{
			string text = LocalizationManager.Instance.LocaleResolver.GetString(key);
			if (!string.IsNullOrEmpty(text) && text != key)
			{
				return text;
			}
			if (text == key && !string.IsNullOrEmpty(key))
			{
				UntranslatedLogger.LogUntranslated(categoryForLog, key);
			}
		}
		return defaultValue;
	}
}
