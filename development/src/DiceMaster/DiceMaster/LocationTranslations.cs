using Mortal.Core;

namespace DiceMaster;

internal static class LocationTranslations
{
	public static string TranslateLocation(string locationKeyWithoutYcPrefix)
	{
		if (string.IsNullOrEmpty(locationKeyWithoutYcPrefix))
		{
			return GetLocalizedSystemTextForTranslations("System/Location/Unknown", "(알수없음)", "시스템_장소");
		}
		string text = locationKeyWithoutYcPrefix;
		bool flag = false;
		string text2 = null;
		bool flag2 = false;
		if (text.StartsWith("關閉_"))
		{
			flag = true;
			text = text.Substring("關閉_".Length);
		}
		switch (text)
		{
		case "伙房":
			text2 = "부엌";
			flag2 = true;
			break;
		case "大門":
			text2 = "대문";
			flag2 = true;
			break;
		case "女弟子房":
			text2 = "여제자방";
			flag2 = true;
			break;
		case "弟子房":
			text2 = "남제자방";
			flag2 = true;
			break;
		case "後山":
			text2 = "뒷산";
			flag2 = true;
			break;
		case "正心堂":
			text2 = "정심당";
			flag2 = true;
			break;
		case "煉丹房":
			text2 = "연단방";
			flag2 = true;
			break;
		case "秘密":
			text2 = "비밀 장소";
			flag2 = true;
			break;
		case "校場":
		case "練武場":
			text2 = "연무장";
			flag2 = true;
			break;
		case "校場_夜晚":
			text2 = "연무장(밤)";
			flag2 = true;
			break;
		case "校場_早上":
			text2 = "연무장(아침)";
			flag2 = true;
			break;
		case "校場_白天":
			text2 = "연무장(낮)";
			flag2 = true;
			break;
		case "校場_黄昏":
			text2 = "연무장(저녁)";
			flag2 = true;
			break;
		case "講經堂":
			text2 = "강경당";
			flag2 = true;
			break;
		case "鍛冶場":
			text2 = "대장간";
			flag2 = true;
			break;
		case "無":
			text2 = "없음";
			flag2 = true;
			break;
		}
		if (flag2 && text2 != null)
		{
			if (flag)
			{
				string localizedSystemTextForTranslations = GetLocalizedSystemTextForTranslations("System/Location/ClosedSuffix", " (닫힘)", "시스템_장소");
				return text2 + localizedSystemTextForTranslations;
			}
			return text2;
		}
		string text3 = null;
		string text4 = "Position/Name/" + text;
		string text5 = "MapPosition/Name/" + text;
		string localizationKey = text4;
		LocalizationManager instance = LocalizationManager.Instance;
		if (((instance != null) ? instance.LocaleResolver : null) != null)
		{
			text3 = LocalizationManager.Instance.LocaleResolver.GetString(text4);
			if (string.IsNullOrEmpty(text3) || text3 == text4)
			{
				localizationKey = text5;
				text3 = LocalizationManager.Instance.LocaleResolver.GetString(text5);
				if (string.IsNullOrEmpty(text3) || text3 == text5)
				{
					text3 = null;
				}
			}
		}
		if (!string.IsNullOrEmpty(text3))
		{
			if (flag)
			{
				string localizedSystemTextForTranslations2 = GetLocalizedSystemTextForTranslations("System/Location/ClosedSuffix", " (닫힘)", "시스템_장소");
				return text3 + localizedSystemTextForTranslations2;
			}
			return text3;
		}
		UntranslatedLogger.LogUntranslated("위치명_최종실패", localizationKey, text);
		string text6 = text;
		if (flag)
		{
			string localizedSystemTextForTranslations3 = GetLocalizedSystemTextForTranslations("System/Location/ClosedSuffix", " (닫힘)", "시스템_장소");
			text6 += localizedSystemTextForTranslations3;
		}
		return text6;
	}

	private static string GetLocalizedSystemTextForTranslations(string keyToAttempt, string defaultValue, string categoryForLog, string originalIdentifierIfKeyFailed = null)
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
}
