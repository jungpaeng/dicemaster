using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DiceMaster.EmbeddedTranslations;

internal static class Display
{
	private static readonly Dictionary<string, string> exact;

	private static readonly Dictionary<char, List<KeyValuePair<string, string>>> starts;

	private static readonly Dictionary<string, string> cache;

	private static readonly HashSet<string> unknown;

	private static readonly Dictionary<GUIStyle, GUIStyle> noteStyles;

	private static readonly object gate;

	private static readonly Regex identifiers;

	private static readonly Regex masked;

	private static readonly Regex eventResultLabel;

	private static Comparison<KeyValuePair<string, string>> CS_0024_003C_003E9__CachedAnonymousMethodDelegate1;

	private static MatchEvaluator CS_0024_003C_003E9__CachedAnonymousMethodDelegate4;

	private static MatchEvaluator CS_0024_003C_003E9__CachedAnonymousMethodDelegate6;

	private static MatchEvaluator CS_0024_003C_003E9__CachedAnonymousMethodDelegate9;

	internal static Dictionary<string, int> _0024_0024method0x600000e_002D1;

	static Display()
	{
		exact = new Dictionary<string, string>(StringComparer.Ordinal);
		starts = new Dictionary<char, List<KeyValuePair<string, string>>>();
		cache = new Dictionary<string, string>(StringComparer.Ordinal);
		unknown = new HashSet<string>(StringComparer.Ordinal);
		noteStyles = new Dictionary<GUIStyle, GUIStyle>();
		gate = new object();
		identifiers = new Regex("(?<![A-Za-z0-9_\\u3400-\\u9fff\\uac00-\\ud7af])[A-Za-z][A-Za-z0-9]*_[A-Za-z0-9_\\u3400-\\u9fff\\uac00-\\ud7af]+", RegexOptions.CultureInvariant);
		masked = new Regex("\ue000([A-Za-z0-9+/=]+)\ue001", RegexOptions.CultureInvariant);
		eventResultLabel = new Regex("(?<![A-Za-z0-9_])P_(大成功|大失敗|成功|失敗|正常|特殊|其他)_([0-9]+)(?![A-Za-z0-9_])", RegexOptions.CultureInvariant);
		using (Stream stream = typeof(Display).Assembly.GetManifestResourceStream("DiceMaster.Translations.Pairs"))
		{
			using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
			string text;
			while ((text = streamReader.ReadLine()) != null)
			{
				int num = text.IndexOf('\t');
				if (num >= 1)
				{
					string key = Encoding.UTF8.GetString(Convert.FromBase64String(text.Substring(0, num)));
					string value = Encoding.UTF8.GetString(Convert.FromBase64String(text.Substring(num + 1)));
					exact[key] = value;
				}
			}
		}
		foreach (KeyValuePair<string, string> item in exact)
		{
			if (!starts.TryGetValue(item.Key[0], out var value2))
			{
				value2 = new List<KeyValuePair<string, string>>();
				starts.Add(item.Key[0], value2);
			}
			value2.Add(item);
		}
		foreach (List<KeyValuePair<string, string>> value3 in starts.Values)
		{
			List<KeyValuePair<string, string>> list = value3;
			if (CS_0024_003C_003E9__CachedAnonymousMethodDelegate1 == null)
			{
				CS_0024_003C_003E9__CachedAnonymousMethodDelegate1 = (KeyValuePair<string, string> a, KeyValuePair<string, string> b) => b.Key.Length.CompareTo(a.Key.Length);
			}
			list.Sort(CS_0024_003C_003E9__CachedAnonymousMethodDelegate1);
		}
	}

	public static string Translate(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		bool flag = text.IndexOf("Note:", StringComparison.Ordinal) >= 0;
		if (!flag)
		{
			for (int i = 0; i < text.Length; i++)
			{
				if (IsHan(text[i]))
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			return text;
		}
		lock (gate)
		{
			if (exact.TryGetValue(text, out var value))
			{
				return value;
			}
			if (cache.TryGetValue(text, out value))
			{
				return value;
			}
			StringBuilder stringBuilder = new StringBuilder(text.Length);
			int i = 0;
			while (i < text.Length)
			{
				if ((text[i] >= 'A' && text[i] <= 'Z') || (text[i] >= 'a' && text[i] <= 'z'))
				{
					Match match = identifiers.Match(text, i);
					if (match.Success && match.Index == i)
					{
						stringBuilder.Append(match.Value);
						i += match.Length;
						continue;
					}
				}
				if (text[i] == '<')
				{
					int num = text.IndexOf('>', i + 1);
					if (num > i && IsRichTag(text.Substring(i, num - i + 1)))
					{
						stringBuilder.Append(text, i, num - i + 1);
						i = num + 1;
						continue;
					}
				}
				bool flag2 = false;
				if (starts.TryGetValue(text[i], out var value2))
				{
					foreach (KeyValuePair<string, string> item in value2)
					{
						if (i + item.Key.Length <= text.Length && string.CompareOrdinal(text, i, item.Key, 0, item.Key.Length) == 0)
						{
							stringBuilder.Append(item.Value);
							i += item.Key.Length;
							flag2 = true;
							break;
						}
					}
				}
				if (!flag2)
				{
					stringBuilder.Append(text[i++]);
				}
			}
			value = stringBuilder.ToString();
			if (cache.Count >= 4096)
			{
				cache.Clear();
			}
			cache[text] = value;
			return value;
		}
	}

	private static bool IsRichTag(string value)
	{
		if (value.StartsWith("<color", StringComparison.OrdinalIgnoreCase) || value.StartsWith("</color", StringComparison.OrdinalIgnoreCase) || value.StartsWith("<size", StringComparison.OrdinalIgnoreCase) || value.StartsWith("</size", StringComparison.OrdinalIgnoreCase))
		{
			goto IL_007a;
		}
		switch (value)
		{
		case "<b>":
		case "</b>":
		case "<i>":
		case "</i>":
			goto IL_007a;
		}
		int result = ((value == "<br>") ? 1 : 0);
		goto IL_007b;
		IL_007b:
		return (byte)result != 0;
		IL_007a:
		result = 1;
		goto IL_007b;
	}

	public static bool IsHan(char c)
	{
		return c >= '㐀' && c <= '鿿';
	}

	public static string PreTranslate(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		Regex regex = identifiers;
		if (CS_0024_003C_003E9__CachedAnonymousMethodDelegate4 == null)
		{
			CS_0024_003C_003E9__CachedAnonymousMethodDelegate4 = delegate(Match m)
			{
				string value = m.Value;
				foreach (char c in value)
				{
					if (IsHan(c))
					{
						return "\ue000" + Convert.ToBase64String(Encoding.UTF8.GetBytes(m.Value)) + "\ue001";
					}
				}
				return m.Value;
			};
		}
		return Translate(regex.Replace(text, CS_0024_003C_003E9__CachedAnonymousMethodDelegate4));
	}

	public static string PostTranslate(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		Regex regex = masked;
		string input = Translate(text);
		if (CS_0024_003C_003E9__CachedAnonymousMethodDelegate6 == null)
		{
			CS_0024_003C_003E9__CachedAnonymousMethodDelegate6 = (Match m) => Encoding.UTF8.GetString(Convert.FromBase64String(m.Groups[1].Value));
		}
		return regex.Replace(input, CS_0024_003C_003E9__CachedAnonymousMethodDelegate6);
	}

	private static void RecordUnknown(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		bool flag = false;
		string text2 = identifiers.Replace(text, "");
		foreach (char c in text2)
		{
			if (IsHan(c))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		lock (gate)
		{
			if (unknown.Count >= 1000 || !unknown.Add(text))
			{
				return;
			}
			try
			{
				string path = Path.Combine(Path.GetDirectoryName(typeof(Display).Assembly.Location), "DiceMaster_Untranslated.log");
				File.AppendAllText(path, text.Replace("\r", "\\r").Replace("\n", "\\n") + Environment.NewLine, Encoding.UTF8);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}

	public static void Label(string text, GUIStyle style, GUILayoutOption[] options)
	{
		string text2 = TranslateLabel(text);
		GUILayout.Label(text2, style, options);
	}

	public static string TranslateLabel(string text)
	{
		string text2 = Translate(text);
		if (string.IsNullOrEmpty(text2))
		{
			return text2;
		}
		Regex regex = eventResultLabel;
		if (CS_0024_003C_003E9__CachedAnonymousMethodDelegate9 == null)
		{
			CS_0024_003C_003E9__CachedAnonymousMethodDelegate9 = (Match m) => "P_" + m.Groups[1].Value switch
			{
				"大成功" => "대성공", 
				"大失敗" => "대실패", 
				"成功" => "성공", 
				"失敗" => "실패", 
				"正常" => "일반", 
				"特殊" => "특수", 
				_ => "기타", 
			} + "_" + m.Groups[2].Value;
		}
		string result = regex.Replace(text2, CS_0024_003C_003E9__CachedAnonymousMethodDelegate9);
		// Buttons, cached event names and the game's narrative/summary text
		// share this display boundary as well as ordinary IMGUI labels.
		RecordUnknown(result);
		return result;
	}

	public static void DebugLabel(string text, GUIStyle style, GUILayoutOption[] options)
	{
		string text2 = Translate(text);
		RecordUnknown(text2);
		if (style != null && text2 != null && text2.StartsWith("참고:", StringComparison.Ordinal))
		{
			if (!noteStyles.TryGetValue(style, out var value))
			{
				value = new GUIStyle(style);
				value.wordWrap = true;
				value.fixedHeight = 0f;
				noteStyles.Add(style, value);
			}
			style = value;
		}
		GUILayout.Label(text2, style, options);
	}

	internal static string[] ReadEmbeddedLines(string resourceName)
	{
		List<string> list;
		using (StreamReader streamReader = new StreamReader(typeof(Display).Assembly.GetManifestResourceStream(resourceName), Encoding.UTF8))
		{
			list = new List<string>();
			string item;
			while ((item = streamReader.ReadLine()) != null)
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}
}
