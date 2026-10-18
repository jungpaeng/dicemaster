using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;

namespace DiceMaster;

public static class UntranslatedLogger
{
	private static readonly string FilePath = Path.Combine(Paths.PluginPath, "DiceMaster_UntranslatedLog.txt");

	private static readonly HashSet<string> LoggedSignatures = new HashSet<string>();

	public static void Initialize()
	{
		try
		{
			if (File.Exists(FilePath))
			{
				File.Delete(FilePath);
			}
			LoggedSignatures.Clear();
			File.AppendAllText(FilePath, $"# DiceMaster 미번역 항목 로그 ({DateTime.Now:yyyy-MM-dd HH:mm:ss})\n", Encoding.UTF8);
			File.AppendAllText(FilePath, "# 형식: 원본텍스트=([카테고리] KEY 미번역)\n\n", Encoding.UTF8);
		}
		catch (Exception ex)
		{
			DicePlugin.LogError("UntranslatedLogger 초기화 실패: " + ex.Message);
		}
	}

	public static void LogUntranslated(string category, string localizationKey, string originalValue = null)
	{
		if (string.IsNullOrEmpty(localizationKey))
		{
			DicePlugin.LogWarning("LogUntranslated 호출 시 localizationKey가 누락되었습니다. (Category: " + (category ?? "null") + ")");
			return;
		}
		string text = (string.IsNullOrEmpty(originalValue) ? localizationKey : originalValue).Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n")
			.Trim();
		string text2 = localizationKey.Trim();
		string text3 = (string.IsNullOrEmpty(category) ? "미분류" : category.Trim());
		string item = text + "|" + text3 + "|" + text2;
		if (LoggedSignatures.Contains(item))
		{
			return;
		}
		string text4 = text + "=([" + text3 + "] " + text2 + " 미번역)";
		try
		{
			File.AppendAllText(FilePath, text4 + "\n", Encoding.UTF8);
			LoggedSignatures.Add(item);
		}
		catch (Exception ex)
		{
			DicePlugin.LogError("미번역 기록 실패 (로그라인: " + text4 + "): " + ex.Message);
		}
	}
}
