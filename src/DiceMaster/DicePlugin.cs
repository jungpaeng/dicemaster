using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using DiceMaster.Config;
using DiceMaster.EmbeddedTranslations;
using DiceMaster.Gamepad;
using DiceMaster.LiveLog;
using HarmonyLib;
using Mortal.Core;
using Mortal.Story;
using UnityEngine;

namespace DiceMaster;

[BepInPlugin("DiceMaster", "DiceMaster", "1.0.0")]
public class DicePlugin : BaseUnityPlugin
{
	public static class Constants
	{
		public const string BundleRes = "Assets.mortaldice";

		public const string AssetName = "DiceInputField";
	}

	public GameObject DictInputPrefab;

	internal static ManualLogSource _logger;

	private static Harmony _harmony = new Harmony("DiceMaster");

	private List<(string Key, string OptionText, string Description)> optionEntries = new List<(string, string, string)>();

	private List<(string Key, string Description)> switchEntries = new List<(string, string)>();

	private List<(PositionEventData EventData, string Key, string Description)> eventEntries = new List<(PositionEventData, string, string)>();

	private List<(MissionCheckData MissionData, string Key, string Description)> missionEntries = new List<(MissionCheckData, string, string)>();

	private Vector2 logScrollPosition;

	private Rect logWindowRect = new Rect(20f, 20f, 750f, 750f);

	private bool showLogWindow = false;

	private float baseFontSize = 14f;

	private float scaleFactor = 1f;

	private GUIStyle headerStyle;

	private GUIStyle logEntryStyle;

	private GUIStyle descriptionStyle;

	private GUIStyle buttonStyle;

	private GUIStyle darkBackgroundStyle;

	private GUIStyle lightBackgroundStyle;

	private bool isInitialized;

	private string currentTab = "선택지";

	private string[] tabs = new string[4] { "선택지", "조건", "이벤트", "미션" };

	private Vector2Int lastScreenSize;

	internal float nextLiveLogRefresh;

	public static DicePlugin Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
		_logger = base.Logger;
		UntranslatedLogger.Initialize();
		ConfigManager.Init();
		try
		{
			if (ConfigManager.EnableDiceController.Value)
			{
				_harmony.PatchAll(typeof(DicePatch));
			}
			_harmony.PatchAll(typeof(DebugPatch));
			if (ConfigManager.EnableOptionInfo.Value)
			{
				_harmony.PatchAll(typeof(OptionInfoPatch));
			}
		}
		catch (Exception ex)
		{
			LogError("Failed to patch: " + ex.Message);
		}
		if (AssetBundleHelper.LoadAsset<GameObject>("DiceInputField", "Assets.mortaldice", out var asset))
		{
			DictInputPrefab = asset;
		}
		else
		{
			LogError("Failed to load asset: DiceInputField");
		}
		lastScreenSize = new Vector2Int(Screen.width, Screen.height);
		OnScreenSizeChanged(Screen.width, Screen.height);
	}

	private void SetGUI()
	{
		headerStyle = new GUIStyle(GUI.skin.label)
		{
			fontSize = (int)(baseFontSize * scaleFactor),
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleCenter,
			richText = true
		};
		logEntryStyle = new GUIStyle(GUI.skin.label)
		{
			fontSize = (int)(16f * scaleFactor),
			richText = true,
			wordWrap = true
		};
		descriptionStyle = new GUIStyle(GUI.skin.label)
		{
			fontSize = (int)(baseFontSize * scaleFactor),
			richText = true,
			wordWrap = true
		};
		buttonStyle = new GUIStyle(GUI.skin.button)
		{
			fontSize = (int)(baseFontSize * scaleFactor)
		};
		GUIStyle gUIStyle = new GUIStyle(GUI.skin.box);
		gUIStyle.normal.background = MakeTex(1, 1, new Color(0.12f, 0.12f, 0.15f, 0.9f));
		darkBackgroundStyle = gUIStyle;
		GUIStyle gUIStyle2 = new GUIStyle(GUI.skin.box);
		gUIStyle2.normal.background = MakeTex(1, 1, new Color(0.22f, 0.22f, 0.25f, 0.8f));
		lightBackgroundStyle = gUIStyle2;
	}

	private void Update()
	{
		if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
		{
			OnScreenSizeChanged(Screen.width, Screen.height);
		}
		if (Input.GetKeyDown(ConfigManager.LogMenuToggleKey.Value))
		{
			showLogWindow = !showLogWindow;
		}
		RefreshLiveLogDisplay();
		DicePatch.UpdateGamepad();
	}

	private void OnDestroy()
	{
		_harmony?.UnpatchSelf();
		GamepadBridge.Shutdown();
	}

	private void OnGUI()
	{
		if (!isInitialized || darkBackgroundStyle == null || darkBackgroundStyle.normal.background == null)
		{
			LogInfo("GUI 스타일을 다시 초기화합니다. isInitialized: " + isInitialized);
			SetGUI();
			isInitialized = true;
		}
		GUISkin skin = GUI.skin;
		if (((skin != null) ? skin.label : null) != null)
		{
			GUI.skin.label.fontSize = (int)(baseFontSize * scaleFactor);
		}
		if (showLogWindow)
		{
			logWindowRect = GUILayout.Window(1, logWindowRect, DrawLogWindow, "다이스마스터 로그창(한글화: 당문졸개)");
		}
		DicePatch.DrawGamepadHint();
	}

	public void AddOptionEntry(string key, string description = "", StoryReadState storyReadState = StoryReadState.NoStory, string optionText = "")
	{
		string item = ColorizeKeyByStoryReadState(key, storyReadState);
		optionEntries.Add((item, optionText ?? string.Empty, description ?? string.Empty));
	}

	public void AddSwitchEntry(string key, string description, StoryReadState storyReadState = StoryReadState.NoStory, string branchStateText = "")
	{
		string text = NormalizeCheckpointKey(key);
		if (string.IsNullOrEmpty(text))
		{
			text = key;
		}
		string item = (string.IsNullOrWhiteSpace(branchStateText) ? ColorizeKeyByStoryReadState(text, storyReadState) : (RichText.Golden(text) + " - " + branchStateText));
		switchEntries.Add((item, description ?? string.Empty));
	}

	public void AddEventEntry(PositionEventData eventData, string key, string description)
	{
		eventEntries.Add((eventData, key, description));
	}

	public void AddMissionEntry(MissionCheckData missionData, string key, string description)
	{
		missionEntries.Add((missionData, key, description));
	}

	private void DrawLogWindow(int windowID)
	{
		GUILayout.BeginHorizontal();
		string[] array = tabs;
		foreach (string text in array)
		{
			if (GUILayout.Toggle(currentTab == text, text, buttonStyle ?? GUI.skin.button, GUILayout.ExpandWidth(expand: true)))
			{
				currentTab = text;
			}
		}
		GUILayout.EndHorizontal();
		RefreshLiveLogDisplay();
		if (GUILayout.Button("게임 데이터 새로고침", buttonStyle ?? GUI.skin.button, GUILayout.ExpandWidth(expand: true)))
		{
			RefreshGameData();
		}
		logScrollPosition = GUILayout.BeginScrollView(logScrollPosition);
		switch (currentTab)
		{
		case "미션":
			DrawMissionEntries(missionEntries);
			break;
		case "이벤트":
			DrawEventEntries(eventEntries);
			break;
		case "조건":
			DrawEntries(switchEntries, applyUiMapping: true);
			break;
		case "선택지":
			DrawOptionEntries(optionEntries);
			break;
		}
		GUILayout.EndScrollView();
		GUI.DragWindow();
	}

	private void DrawEntries(IEnumerable<(string, string)> entries, bool applyUiMapping = false)
	{
		bool flag = true;
		foreach (var entry in entries)
		{
			string item = entry.Item1;
			string item2 = entry.Item2;
			string text = (applyUiMapping ? UiTextMapper.ApplyTranslations(item2) : item2);
			GUIStyle style = (flag ? darkBackgroundStyle : lightBackgroundStyle);
			flag = !flag;
			GUILayout.BeginVertical(style);
			DiceMaster.EmbeddedTranslations.Display.Label(item, logEntryStyle, Array.Empty<GUILayoutOption>());
			DiceMaster.EmbeddedTranslations.Display.Label(text, descriptionStyle, Array.Empty<GUILayoutOption>());
			GUILayout.EndVertical();
		}
	}

	private void DrawOptionEntries(IEnumerable<(string Key, string OptionText, string Description)> entries)
	{
		bool flag = true;
		foreach (var entry in entries)
		{
			string item = entry.Key;
			string item2 = entry.OptionText;
			string text = UiTextMapper.ApplyTranslations(entry.Description);
			GUIStyle style = (flag ? darkBackgroundStyle : lightBackgroundStyle);
			flag = !flag;
			GUILayout.BeginVertical(style);
			DiceMaster.EmbeddedTranslations.Display.Label(item, logEntryStyle, Array.Empty<GUILayoutOption>());
			if (!string.IsNullOrWhiteSpace(item2))
			{
				DiceMaster.EmbeddedTranslations.Display.Label("<color=#9A9A9A>" + item2 + "</color>", descriptionStyle, Array.Empty<GUILayoutOption>());
			}
			DiceMaster.EmbeddedTranslations.Display.Label(text, descriptionStyle, Array.Empty<GUILayoutOption>());
			GUILayout.EndVertical();
		}
	}

	private void DrawEventEntries(IEnumerable<(PositionEventData EventData, string Key, string Description)> entries)
	{
		bool flag = true;
		foreach (var entry in entries)
		{
			PositionEventData item = entry.EventData;
			string item2 = entry.Key;
			string text = UiTextMapper.ApplyTranslations(entry.Description);
			GUIStyle style = (flag ? darkBackgroundStyle : lightBackgroundStyle);
			flag = !flag;
			GUILayout.BeginVertical(style);
			DiceMaster.EmbeddedTranslations.Display.Label(item2, logEntryStyle, Array.Empty<GUILayoutOption>());
			DiceMaster.EmbeddedTranslations.Display.Label(text, descriptionStyle, Array.Empty<GUILayoutOption>());
			if ((object)item != null && GUILayout.Button("이벤트 실행", buttonStyle ?? GUI.skin.button))
			{
				OptionInfoPatch.TargetEvent = item;
			}
			GUILayout.EndVertical();
		}
	}

	private void DrawMissionEntries(IEnumerable<(MissionCheckData, string, string)> entries)
	{
		bool flag = true;
		foreach (var entry in entries)
		{
			MissionCheckData item = entry.Item1;
			string item2 = entry.Item2;
			string text = UiTextMapper.ApplyTranslations(entry.Item3);
			GUIStyle style = (flag ? darkBackgroundStyle : lightBackgroundStyle);
			flag = !flag;
			GUILayout.BeginVertical(style);
			DiceMaster.EmbeddedTranslations.Display.Label(item2, logEntryStyle, Array.Empty<GUILayoutOption>());
			DiceMaster.EmbeddedTranslations.Display.Label(text, descriptionStyle, Array.Empty<GUILayoutOption>());
			if ((object)item != null && GUILayout.Button("미션 실행", buttonStyle ?? GUI.skin.button))
			{
				OptionInfoPatch.TargetMission = item;
				MissionManagerData instance = MissionManagerData.Instance;
				if (((instance != null) ? instance.ActiveSubMissions : null) != null)
				{
					if (!MissionManagerData.Instance.ActiveSubMissions.ContainsKey(item2))
					{
						MissionManagerData.Instance.ActiveSubMissions.Add(item2, item);
					}
					else
					{
						MissionManagerData.Instance.ActiveSubMissions[item2] = item;
					}
				}
			}
			GUILayout.EndVertical();
		}
	}

	private Texture2D MakeTex(int width, int height, Color col)
	{
		Color[] array = new Color[width * height];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = col;
		}
		Texture2D texture2D = new Texture2D(width, height);
		texture2D.SetPixels(array);
		texture2D.Apply();
		return texture2D;
	}

	private static string NormalizeCheckpointKey(string rawKey)
	{
		if (string.IsNullOrWhiteSpace(rawKey))
		{
			return string.Empty;
		}
		string text = rawKey.Trim();
		int num = text.IndexOf(',');
		if (num >= 0)
		{
			text = text.Substring(0, num);
		}
		text = text.Trim().Trim('\'', '"');
		if (text.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(0, text.Length - 4);
		}
		return text;
	}

	private static string ColorizeKeyByStoryReadState(string key, StoryReadState storyReadState)
	{
		string text = key ?? string.Empty;
		return storyReadState switch
		{
			StoryReadState.Read => "<color=#E8A53A>" + text + "</color>", 
			StoryReadState.Unread => "<color=#FFFFFF>" + text + "</color>", 
			_ => "<color=#B8B8B8>" + text + "</color>", 
		};
	}

	public void ClearLogEntries()
	{
		optionEntries.Clear();
		switchEntries.Clear();
		eventEntries.Clear();
		missionEntries.Clear();
	}

	public static void LogInfo(object o)
	{
		ManualLogSource logger = _logger;
		if (logger != null)
		{
			bool isEnabled = false;
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(3, 2, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(DateTime.Now.ToString(DateTimeFormatInfo.InvariantInfo));
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("] ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(o);
			}
			logger.LogInfo(bepInExInfoLogInterpolatedStringHandler);
		}
	}

	public static void LogError(object o)
	{
		ManualLogSource logger = _logger;
		if (logger != null)
		{
			bool isEnabled = false;
			BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(3, 2, out isEnabled);
			if (isEnabled)
			{
				bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[");
				bepInExErrorLogInterpolatedStringHandler.AppendFormatted(DateTime.Now.ToString(DateTimeFormatInfo.InvariantInfo));
				bepInExErrorLogInterpolatedStringHandler.AppendLiteral("] ");
				bepInExErrorLogInterpolatedStringHandler.AppendFormatted(o);
			}
			logger.LogError(bepInExErrorLogInterpolatedStringHandler);
		}
	}

	public static void LogWarning(object data)
	{
		if (_logger != null)
		{
			_logger.LogWarning(data);
		}
	}

	private void OnScreenSizeChanged(float width, float height)
	{
		lastScreenSize.x = (int)width;
		lastScreenSize.y = (int)height;
		scaleFactor = Mathf.Min(width / 1920f, height / 1080f);
		logWindowRect = new Rect(20f * scaleFactor, 20f * scaleFactor, 800f * scaleFactor, 1000f * scaleFactor);
		isInitialized = false;
	}

	private void RefreshGameData()
	{
		LogBridge.Tick(this, manual: true);
	}

	internal void RefreshLiveLogDisplay()
	{
		LogBridge.Tick(this, manual: false);
	}
}
