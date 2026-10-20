using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using DiceMaster.Config;
using DiceMaster.EmbeddedTranslations;
using DiceMaster.EventSelection;
using DiceMaster.Gamepad;
using DiceMaster.LiveLog;
using DiceMaster.LogUI;
using DiceMaster.Information;
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
	private readonly EventPopupRenderer eventPopup = new EventPopupRenderer();
	private readonly LogCursorScope logCursor = new LogCursorScope();
	private LogInputMode logInputMode;
	private LogInputMode eventInputMode;
	private bool eventInputModeKnown;
	private long lastLogContext = -1;

	internal float nextLiveLogRefresh;

	public static DicePlugin Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
		_logger = base.Logger;
		UntranslatedLogger.Initialize();
		ConfigManager.Init();
		EventSelectionRuntime.Enabled = () => ConfigManager.GetConfigValue(ConfigManager.EnableOptionInfo)
			&& ConfigManager.GetConfigValue(ConfigManager.EnableEventSelectionPopup);
		EventSelectionRuntime.SwitchEnabled = () => ConfigManager.GetConfigValue(ConfigManager.EnableOptionInfo)
			&& ConfigManager.GetConfigValue(ConfigManager.EnableSwitchSelectionPopup);
		EventSelectionRuntime.ReservationsEnabled = () => ConfigManager.GetConfigValue(ConfigManager.EnableOptionInfo);
		EventSelectionRuntime.WaitingChanged = waiting =>
		{
			ResetEventInput(ModalInputResetReason.PopupWaitingChanged);
			GamepadBridge.SetModalCapture(showLogWindow || waiting, Application.isFocused, Paths.CachePath);
			GameActionGate.SetLogState(showLogWindow || waiting, Application.isFocused);
		};
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
				EventSelectionRuntime.Install(_harmony);
				InformationRuntime.Install(_harmony);
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
		logRenderer = new LogRenderer(LogBridge.Model, RefreshSelectedData, () => SetLogOpen(false));
		logRenderer.TabChanged = tab => { currentTab = tab; RefreshLiveLogDisplay(); };
		logRenderer.ReselectController = GamepadBridge.ReselectLogController;
		logRenderer.InformationSettingLabel = InformationRuntime.SettingLabel;
		logRenderer.ToggleInformationSetting = InformationRuntime.Toggle;
		logRenderer.OneShotEventTag = InformationRuntime.EventTag;
		logRenderer.OneShotOptionTag = InformationRuntime.OptionTag;
		logRenderer.OneShotEventDetail = InformationRuntime.EventDetail;
		logRenderer.OneShotOptionDetail = InformationRuntime.OptionDetail;
		eventPopup.OneShotEventTag = InformationRuntime.EventTag;
		eventPopup.OneShotEventDetail = InformationRuntime.EventDetail;
	}

	private void SetLogOpen(bool open, int padOwner = -1)
	{
		ResetEventInput(ModalInputResetReason.LogVisibilityChanged);
		showLogWindow = open;
		EventSelectionRuntime.SetLogVisible(open);
		logInputMode = GameInputModeResolver.Resolve();
		logCursor.SetActive((open || EventSelectionRuntime.Waiting) && Application.isFocused, logInputMode);
		GameActionGate.SetLogState(open || EventSelectionRuntime.Waiting, Application.isFocused);
		EnsureLogRenderer();
		logRenderer.ResetInputFocus();
		GamepadBridge.CancelLogInput();
		if (open && padOwner >= 0) GamepadBridge.BeginPadLog(padOwner);
	}

	private void Update()
	{
		EnsureLogRenderer();
		logInputMode = GameInputModeResolver.Resolve();
		ObserveEventInputMode(logInputMode);
		EventSelectionRuntime.SetLogVisible(showLogWindow);
		SynchronizeLogContext();
		if (Input.GetKeyDown(ConfigManager.LogMenuToggleKey.Value))
		{
			SetLogOpen(!showLogWindow);
		}
		else
		{
			int padOwner = GamepadBridge.PollLogOpener(ConfigManager.EnableGamepadLogOpen.Value,
				Application.isFocused, showLogWindow, Paths.CachePath, EventSelectionRuntime.Waiting);
			if (!showLogWindow && padOwner >= 0) SetLogOpen(true, padOwner);
		}
		// Establish the foreground owner before refreshing a waiting popup.
		// Gameplay stays captured throughout the popup/log round trip.
		EventSelectionRuntime.Tick(!showLogWindow && Application.isFocused, Time.unscaledTime);
		bool modalOpen = showLogWindow || EventSelectionRuntime.Waiting;
		logCursor.SetActive(modalOpen && Application.isFocused, logInputMode);
		GameActionGate.SetLogState(modalOpen, Application.isFocused);
		currentTab = logRenderer.CurrentTab;
		if (!showLogWindow || logRenderer.InformationPage == null) RefreshLiveLogDisplay();
		SynchronizeLogContext();
		// This runs even with dice adjustment disabled or the log closed: the
		// shared owner must finish consuming held input before returning it.
		LogPadFrame frame = GamepadBridge.TickLog(showLogWindow || EventSelectionRuntime.Waiting, Application.isFocused,
			logInputMode, Time.unscaledTime, Paths.CachePath);
		if (frame.ResetFocus) logRenderer.ResetInputFocus(frame.PreservePointerTransaction, frame.PreserveNavigationArea);
		ResetEventPadInput(frame);
		if (!showLogWindow) eventPopup.UpdateInput();
		if (showLogWindow && Application.isFocused)
		{
			logRenderer.Handle(frame.Command);
			logRenderer.Scroll(frame.Scroll);
			logRenderer.InputHint = GamepadBridge.LogHint();
		}
		else if (EventSelectionRuntime.Waiting && Application.isFocused)
		{
			eventPopup.InputHint = GamepadBridge.LogHint().Replace("로그", EventSelectionRuntime.IsSwitch ? "조건 분기 선택" : "이벤트 선택");
			eventPopup.Handle(frame.Command);
			eventPopup.Scroll(frame.Scroll);
		}
		InformationRuntime.Tick(showLogWindow ? logRenderer.InformationPage : null, Application.isFocused, Time.unscaledTime, false, !showLogWindow && !EventSelectionRuntime.Waiting, !showLogWindow && EventSelectionRuntime.Waiting);
		DicePatch.UpdateGamepad();
		string gateWarning = GameActionGate.ConsumeWarning();
		if (!string.IsNullOrEmpty(gateWarning)) LogWarning(gateWarning);
	}

	private void ObserveEventInputMode(LogInputMode mode)
	{
		// A touch can update the game's control scheme after its release has
		// already queued a result. That observation is not a new action owner:
		// preserve the request until Lua resumes and revalidates its target.
		// Focus, log/context and physical pad-owner changes still cancel it.
		if (eventInputModeKnown && eventInputMode != mode && EventSelectionRuntime.Waiting)
		{
			ResetEventInput(ModalInputResetReason.AutomaticInputModeObserved);
		}
		eventInputMode = mode;
		eventInputModeKnown = true;
	}

	private void ResetEventPadInput(LogPadFrame frame)
	{
		if (frame.ResetFocus) ResetEventInput(ModalInputResetPolicy.FromPadFrame(frame));
	}

	private void ResetEventInput(ModalInputResetReason reason)
	{
		ModalInputReset reset = ModalInputResetPolicy.For(reason);
		if (reset.CancelConfirmation) EventSelectionRuntime.CancelConfirmation();
		eventPopup.ResetInput(reset.PreservePointerTransaction);
	}

	private void SynchronizeLogContext()
	{
		if (lastLogContext != LogBridge.Model.ContextId)
		{
			lastLogContext = LogBridge.Model.ContextId;
			logRenderer.ResetInputFocus();
			GamepadBridge.CancelLogInput();
		}
	}

	private void OnDestroy()
	{
		EventSelectionRuntime.SetLogVisible(false);
		EventSelectionRuntime.Invalidate("모드 종료");
		EventSelectionRuntime.WaitingChanged = null;
		InformationRuntime.Clear();
		logCursor.Release();
		GameActionGate.SetLogState(false, false);
		_harmony?.UnpatchSelf();
		GamepadBridge.Shutdown();
	}

	private void OnApplicationFocus(bool focused)
	{
		ResetEventInput(ModalInputResetReason.ApplicationFocusChanged);
		logCursor.SetActive((showLogWindow || EventSelectionRuntime.Waiting) && focused, logInputMode);
		GameActionGate.SetLogState(showLogWindow || EventSelectionRuntime.Waiting, focused);
		if (!focused) GamepadBridge.SuspendLogOpener();
		GamepadBridge.CancelPadEntry();
		logRenderer?.ResetInputFocus();
		GamepadBridge.CancelLogInput();
	}

	private void OnDisable()
	{
		EventSelectionRuntime.SetLogVisible(false);
		EventSelectionRuntime.Invalidate("모드 비활성화");
		logCursor.Release();
		GameActionGate.SetLogState(false, Application.isFocused);
		logRenderer?.ResetInputFocus();
		GamepadBridge.SuspendLogInput();
	}

	private void OnGUI()
	{
		ObserveEventInputMode(GameInputModeResolver.Resolve());
		// The game can relock the cursor after Update when its scheme changes.
		// Keep the visible log reachable without changing that game scheme.
		if (showLogWindow)
		{
			logInputMode = GameInputModeResolver.Resolve();
			logCursor.SetActive(Application.isFocused, logInputMode);
			EnsureLogRenderer();
			logRenderer.Draw();
		}
		else if (EventSelectionRuntime.Waiting)
		{
			logCursor.SetActive(Application.isFocused, logInputMode);
			eventPopup.Draw();
		}
		else logCursor.Release();
		InformationRuntime.Draw(showLogWindow, EventSelectionRuntime.Waiting, logRenderer.WindowRect, Time.unscaledTime);
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

	private void RefreshSelectedData()
	{
		if (showLogWindow && logRenderer.InformationPage != null)
			InformationRuntime.Tick(logRenderer.InformationPage, Application.isFocused, Time.unscaledTime, true, false, false);
		else RefreshGameData();
	}

	internal void RefreshLiveLogDisplay()
	{
		LogBridge.Tick(this, manual: false);
	}
}
