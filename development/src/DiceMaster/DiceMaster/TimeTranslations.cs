using System;
using Mortal.Core;

namespace DiceMaster;

internal static class TimeTranslations
{
	public static string TranslateNumberOrMonth(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return key;
		}
		if (key.StartsWith("System/Number_"))
		{
			return key.Substring("System/Number_".Length);
		}
		if (key.StartsWith("System/Month_"))
		{
			return key.Substring("System/Month_".Length);
		}
		return key;
	}

	public static string TranslateStage(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return key;
		}
		string text = "";
		switch (key)
		{
		case "System/Stage_1":
			return GetLocalizedSystemTextForTime("System/StageName/Early", "상순", "시간_순");
		case "System/Stage_2":
			return GetLocalizedSystemTextForTime("System/StageName/Mid", "중순", "시간_순");
		case "System/Stage_3":
			return GetLocalizedSystemTextForTime("System/StageName/Late", "하순", "시간_순");
		default:
			UntranslatedLogger.LogUntranslated("시간_순_알수없음", key, key);
			return key;
		}
	}

	public static string TranslateChineseTimeExpression(string chineseTime)
	{
		if (string.IsNullOrEmpty(chineseTime))
		{
			return chineseTime;
		}
		string text = null;
		string result = chineseTime;
		if (chineseTime == "目前時間")
		{
			text = "System/Time/CurrentTime";
		}
		else
		{
			switch (chineseTime)
			{
			case "第一年五月下旬":
				text = "System/Time/Year1_May_Late";
				break;
			case "第二年二月下旬":
				text = "System/Time/Year2_Feb_Late";
				break;
			case "第二年八月中旬":
				text = "System/Time/Year2_Aug_Mid";
				break;
			case "第二年八月下旬":
				text = "System/Time/Year2_Aug_Late";
				break;
			case "第三年三月上旬":
				text = "System/Time/Year3_Mar_Early";
				break;
			case "第三年十月上旬":
				text = "System/Time/Year3_Oct_Early";
				break;
			case "第三年十二月上旬":
				text = "System/Time/Year3_Dec_Early";
				break;
			}
		}
		if (text != null)
		{
			result = GetLocalizedSystemTextForTime(text, chineseTime, "시간표현_중국어", chineseTime);
		}
		else if (IsPotentiallyChinese(chineseTime))
		{
			UntranslatedLogger.LogUntranslated("시간표현_중국어_키없음", chineseTime, chineseTime);
		}
		return result;
	}

	private static string GetLocalizedSystemTextForTime(string keyToAttempt, string defaultValue, string categoryForLog = "시스템_시간번역", string originalIdentifierIfKeyFailed = null)
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

	private static bool IsPotentiallyChinese(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		char[] array = new char[8] { '年', '月', '上', '中', '下', '旬', '時', '間' };
		foreach (char c in text)
		{
			if (Array.Exists(array, (char element) => element == c))
			{
				return true;
			}
		}
		return false;
	}
}
