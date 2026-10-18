using Mortal.Core;

namespace DiceMaster;

internal static class CharacterTranslations
{
	public static string TranslateCharacter(string 캐릭터_ID_조각)
	{
		if (string.IsNullOrEmpty(캐릭터_ID_조각))
		{
			return GetLocalizedSystemTextForTranslations("System/Text/Unknown", "알 수 없음", "시스템_캐릭터");
		}
		string text = "Character/" + 캐릭터_ID_조각;
		string text2 = null;
		LocalizationManager instance = LocalizationManager.Instance;
		if (((instance != null) ? instance.LocaleResolver : null) != null)
		{
			text2 = LocalizationManager.Instance.LocaleResolver.GetString(text);
		}
		if (string.IsNullOrEmpty(text2) || text2 == text)
		{
			UntranslatedLogger.LogUntranslated("캐릭터명", text, 캐릭터_ID_조각);
			GetLocalizedSystemTextForTranslations("System/Character/NameNotTranslatedSuffix", "(이름 번역 없음)", "시스템_캐릭터");
			return 캐릭터_ID_조각;
		}
		return text2;
	}

	private static string GetLocalizedSystemTextForTranslations(string key, string defaultValue, string categoryForLog = "시스템_캐릭터번역")
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
