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
using DiceMaster.LogUI;
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

	// Tuple fields above remain the compatibility publication surface. The
	// renderer consumes LogBridge.Model, including stable identity and status.
	private bool showLogWindow;
	private string currentTab = "선택지";
	private LogRenderer logRenderer;
	private long lastLogContext = -1;

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
			GameActionGate.Initialize(_harmony);
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
		EnsureLogRenderer();
	}

	private void EnsureLogRenderer()
	{
		if (logRenderer != null) return;
		logRenderer = new LogRenderer(LogBridge.Model, RefreshGameData, () => SetLogOpen(false));
		logRenderer.TabChanged = tab => { currentTab = tab; RefreshLiveLogDisplay(); };
		logRenderer.ReselectController = GamepadBridge.ReselectLogController;
	}

	private void SetLogOpen(bool open)
	{
		showLogWindow = open;
		GameActionGate.SetLogState(open, Application.isFocused);
		EnsureLogRenderer();
		logRenderer.ResetInputFocus();
		GamepadBridge.CancelLogInput();
	}

	private void Update()
	{
		EnsureLogRenderer();
		GameActionGate.SetLogState(showLogWindow, Application.isFocused);
		if (Input.GetKeyDown(ConfigManager.LogMenuToggleKey.Value))
		{
			SetLogOpen(!showLogWindow);
		}
		currentTab = logRenderer.CurrentTab;
		RefreshLiveLogDisplay();
		if (lastLogContext != LogBridge.Model.ContextId)
		{
			lastLogContext = LogBridge.Model.ContextId;
			logRenderer.ResetInputFocus();
			GamepadBridge.CancelLogInput();
		}
		// This runs even with dice adjustment disabled or the log closed: the
		// shared owner must finish consuming held input before returning it.
		LogPadFrame frame = GamepadBridge.TickLog(showLogWindow, Application.isFocused,
			GameInputModeResolver.Resolve(), Time.unscaledTime, Paths.CachePath);
		if (frame.ResetFocus) logRenderer.ResetInputFocus();
		if (showLogWindow && Application.isFocused)
		{
			logRenderer.Handle(frame.Command);
			logRenderer.Scroll(frame.Scroll);
			logRenderer.InputHint = GamepadBridge.LogHint();
		}
		DicePatch.UpdateGamepad();
		string gateWarning = GameActionGate.ConsumeWarning();
		if (!string.IsNullOrEmpty(gateWarning)) LogWarning(gateWarning);
	}

	private void OnDestroy()
	{
		GameActionGate.SetLogState(false, false);
		_harmony?.UnpatchSelf();
		GamepadBridge.Shutdown();
	}

	private void OnGUI()
	{
		if (showLogWindow)
		{
			EnsureLogRenderer();
			logRenderer.Draw();
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

	private void RefreshGameData()
	{
		LogBridge.Tick(this, manual: true);
	}

	internal void RefreshLiveLogDisplay()
	{
		LogBridge.Tick(this, manual: false);
	}
}
