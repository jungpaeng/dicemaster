using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using DiceMaster.Config;
using DiceMaster.EmbeddedTranslations;
using DiceMaster.LiveLog;
using DiceMaster.LuaParse;
using Fungus;
using HarmonyLib;
using Mortal.Core;
using Mortal.Free;
using Mortal.Story;

namespace DiceMaster;

[HarmonyPatch]
internal static class OptionInfoPatch
{
	private static PositionController _currentPositionControllerInstance;

	public static PositionEventData TargetEvent;

	public static MissionCheckData TargetMission;

	private static string _lastLuaString;

	private static string _lastFriendlyName;

	private static bool _suppressStoryTextPostfix;

	internal static int DisplayRefreshDepth;

	[HarmonyPrefix]
	[HarmonyPatch(typeof(LuaEnvironment), "LoadLuaFunction")]
	public static void LuaEnvironment_LoadLuaFunction_Prefix(string luaString, string friendlyName)
	{
		if (DisplayRefreshDepth == 0)
		{
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(LeanLocalizationResolver), "GetStoryText")]
	public static void LeanLocalizationResolver_GetStoryText_Postfix(string key, ref string __result)
	{
		if (_suppressStoryTextPostfix || ConfigManager.EnableOptionInfo == null || !ConfigManager.EnableOptionInfo.Value || string.IsNullOrEmpty(key) || LuaParser.Instance.OptionResult == null)
		{
			return;
		}
		string text;
		if (!LuaParser.Instance.OptionResult.TryGetValue(key, out List<DiceMaster.LuaParse.Action> list) || list == null)
		{
			if (!LuaParser.Instance.IncompleteOptionDiscovery || !key.StartsWith("O_", StringComparison.Ordinal)) return;
			text = PreviewDisplay.UnsupportedStatus;
		}
		else
		{
			try
			{
				text = list.Count == 0 ? "확인된 효과 없음" : LuaParser.Description(list);
				LogBridge.RecordDescriptionResult(list, null);
			}
			catch (PreviewReadException failure)
			{
				LogBridge.RecordDescriptionResult(list, failure);
				text = (failure.IsWaiting ? RefreshEngine.WaitingStatus : RefreshEngine.ReadFailureStatus) +
					" | " + failure.PartialDescription;
			}
		}
		if (!text.IsNullOrWhiteSpace())
		{
			string text2 = Regex.Replace(Display.TranslateLabel(UiTextMapper.ApplyTranslations(text)), "<.*?>", string.Empty);
			text2 = Regex.Replace(text2, @"\s*[\r\n]+\s*", " / ");
			if (text2.Length > 50)
			{
				text2 = text2.Substring(0, 50) + "...";
			}
			string localizedSystemText = GetLocalizedSystemText("System/UI/OpenParenthesis", "(");
			string localizedSystemText2 = GetLocalizedSystemText("System/UI/CloseParenthesis", ")");
			__result = __result + " <size=18>" + localizedSystemText + text2 + localizedSystemText2 + "</size>";
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(PositionController), "OnPositionEnter")]
	public static void PositionController_OnPositionEnter_Prefix(PositionController __instance)
	{
		_currentPositionControllerInstance = __instance;
		if (ConfigManager.EnableOptionInfo != null && ConfigManager.EnableOptionInfo.Value)
		{
			FreePositionData fieldValue = __instance.GetFieldValue<FreePositionData>("_positionData");
			if (string.IsNullOrEmpty((fieldValue != null) ? fieldValue.name : null))
			{
				DicePlugin.LogWarning("OnPositionEnter_Prefix: PositionController '" + __instance.gameObject.name + "'의 currentPositionKey를 가져올 수 없습니다.");
			}
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(PositionController), "OnPositionClick")]
	public static void PositionController_OnPositionClick_Prefix(PositionController __instance)
	{
		if (ConfigManager.EnableOptionInfo == null || !ConfigManager.EnableOptionInfo.Value)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		FreePositionData fieldValue = __instance.GetFieldValue<FreePositionData>("_positionData");
		string text = ((fieldValue != null) ? fieldValue.name : null);
		if (string.IsNullOrEmpty(text))
		{
			DicePlugin.LogWarning("OnPositionClick_Prefix: PositionController '" + __instance.gameObject.name + "'의 currentPositionKey를 가져올 수 없습니다.");
			return;
		}
		string locationKeyWithoutYcPrefix = (text.StartsWith("養成_") ? text.Substring("養成_".Length) : text);
		stringBuilder.AppendLine(GetLocalizedSystemText("System/Log/CurrentLocationLabel", "현재 위치:") + LocationTranslations.TranslateLocation(locationKeyWithoutYcPrefix));
		if (PositionEventCache.Instance != null)
		{
			List<PositionEventData> positionEvents = PositionEventCache.Instance.GetPositionEvents(text);
			if (positionEvents != null && positionEvents.Count > 0)
			{
				stringBuilder.AppendLine(RichText.SeparateLine(GetLocalizedSystemText("System/Log/EventsLabel", "발생 가능 이벤트")));
				foreach (PositionEventData item in positionEvents)
				{
					if (!(item == null))
					{
						stringBuilder.AppendLine(PositionEventCache.Instance.GetPositionEventName(item));
						stringBuilder.AppendLine(PositionEventCache.Instance.GetPositionEventDesc(item));
					}
				}
			}
		}
		PrintToNarrativeLog(stringBuilder.ToString());
		MissionCheckData missionForDescription = TargetMission;
		if (missionForDescription == null && TryGetMissionKeyForPosition(text, out var missionKey) && MissionManagerData.Instance != null)
		{
			SubMissionsData fieldValue2 = MissionManagerData.Instance.GetFieldValue<SubMissionsData>("_subMissions");
			MissionData missionData = ((fieldValue2 != null) ? fieldValue2.Get(missionKey) : null);
			if (missionData != null)
			{
				missionForDescription = missionData.GetByKey(missionData.CurrentKey);
				if (LogBridge.ShouldAutoReserveMission(missionForDescription))
				{
					TargetMission = missionForDescription;
					LogBridge.NotifyReservationChanged("미션");
				}
			}
		}
		if (missionForDescription != null && missionForDescription.name != null)
		{
			string key = "Mission/Name/" + missionForDescription.name;
			LocalizationManager instance = LocalizationManager.Instance;
			object obj = ((!(instance == null)) ? instance.LocaleResolver?.GetString(key) : null);
			if (obj == null)
			{
				obj = missionForDescription.name;
			}
			string text2 = (string)obj;
			LogCheckpoint(GetLocalizedSystemText("System/Log/MissionConditionsLabel", "미션 조건: ") + text2, missionForDescription.GetFieldValue<ConditionResultItem[]>("_conditions"));
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(ConditionResultData), "Execute")]
	public static void PrefixExecuteConditionResult(ConditionResultData __instance)
	{
		if (DisplayRefreshDepth == 0)
		{
			LogCheckpoint(GetLocalizedSystemText("System/Log/TriggerStoryCheckpoint", "스토리 체크포인트 발동:"), __instance.GetFieldValue<List<ConditionResultItem>>("_items"));
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(MissionCheckData), "Check")]
	public static void PrefixCheckMission(MissionCheckData __instance)
	{
		if (DisplayRefreshDepth != 0)
		{
			return;
		}
		ConditionResultItem[] fieldValue = __instance.GetFieldValue<ConditionResultItem[]>("_conditions");
		StringBuilder stringBuilder = new StringBuilder(GetLocalizedSystemText("System/Log/TriggerMissionCheckpoint", "미션 체크포인트 발동:"));
		AppendConditions(fieldValue, stringBuilder);
		if (__instance.GetFieldValue<bool>("_dayCheck"))
		{
			DataHelper.GetMissionPeriod(__instance, out var periodText);
			if (!periodText.IsNullOrWhiteSpace())
			{
				stringBuilder.AppendLine("    " + GetLocalizedSystemText("System/Log/TimeLimitLabel", "시간 제한: ") + periodText);
			}
		}
		PrintToNarrativeLog(stringBuilder.ToString());
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(SwitchResultData), "Execute")]
	public static void PrefixExecuteSwitchResult(SwitchResultData __instance)
	{
		if (DisplayRefreshDepth == 0)
		{
			LogCheckpoint(GetLocalizedSystemText("System/Log/TriggerSwitchCheckpoint", "분기 체크포인트 발동:"), __instance.GetFieldValue<List<ConditionResultItem>>("_items"));
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(PositionEventData), "GetRate")]
	public static void PositionEventData_GetRate_Postfix(PositionEventData __instance, ref int __result)
	{
		if (DisplayRefreshDepth == 0 && !(TargetEvent != __instance))
		{
			__result = 99999;
			TargetEvent = null;
			LogBridge.NotifyReservationChanged("이벤트");
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MissionCheckData), "Check")]
	public static void MissionCheckData_Postfix(MissionCheckData __instance, ref bool __result)
	{
		if (DisplayRefreshDepth == 0 && !(TargetMission != __instance))
		{
			__result = true;
			TargetMission = null;
			LogBridge.NotifyReservationChanged("미션");
		}
	}

	private static bool TryGetMissionKeyForPosition(string currentPositionKey, out string missionKey)
	{
		missionKey = string.Empty;
		SubMissionsData subMissionsData = MissionManagerData.Instance?.GetFieldValue<SubMissionsData>("_subMissions");
		if (((CollectionData<MissionData>)subMissionsData)?.List == null)
		{
			return false;
		}
		foreach (MissionData item in ((CollectionData<MissionData>)subMissionsData).List)
		{
			if (!(item == null))
			{
				MissionCheckData byKey = item.GetByKey(item.CurrentKey);
				if (byKey != null && byKey.Position.ToString() == currentPositionKey)
				{
					missionKey = item.name;
					return true;
				}
			}
		}
		return false;
	}

	private static void LogCheckpoint(string header, IEnumerable<ConditionResultItem> items)
	{
		StringBuilder stringBuilder = new StringBuilder(header);
		AppendConditions(items, stringBuilder);
		PrintToNarrativeLog(stringBuilder.ToString());
	}

	private static void AppendConditions(IEnumerable<ConditionResultItem> items, StringBuilder logBuilder)
	{
		if (items == null)
		{
			return;
		}
		foreach (ConditionResultItem item in items)
		{
			if (item != null)
			{
				bool isInPeriod;
				string text = DataHelper.ExtractConditions(item, out isInPeriod);
				if (!text.IsNullOrWhiteSpace())
				{
					logBuilder.AppendLine("    " + text);
				}
			}
		}
	}

	private static void PrintToNarrativeLog(string message)
	{
		string text = Regex.Replace(Display.TranslateLabel(UiTextMapper.ApplyTranslations(message)), "^\\s*$\\n|\\r", string.Empty, RegexOptions.Multiline).Trim();
		if (!string.IsNullOrEmpty(text))
		{
			FungusManager instance = FungusManager.Instance;
			NarrativeLog narrativeLog = ((instance != null) ? instance.NarrativeLog : null);
			if (!(narrativeLog == null))
			{
				NarrativeLogEntry entry = new NarrativeLogEntry
				{
					name = GetLocalizedSystemText("System/ModName", "DiceMaster"),
					text = "<size=18>" + text + "</size>"
				};
				narrativeLog.AddLine(entry);
			}
		}
	}

	private static string GetLocalizedSystemText(string key, string defaultValue)
	{
		LocalizationManager instance = LocalizationManager.Instance;
		if (((instance != null) ? instance.LocaleResolver : null) != null)
		{
			string text = LocalizationManager.Instance.LocaleResolver.GetString(key);
			if (!string.IsNullOrEmpty(text) && text != key)
			{
				return text;
			}
		}
		return defaultValue;
	}

	public static void ReprocessLastLuaFunction()
	{
		LogBridge.Tick(DicePlugin.Instance, manual: true);
	}

	private static StoryReadState EvaluateStoryReadState(Dictionary<string, List<string>> source, string scopeKey)
	{
		if (source == null || string.IsNullOrWhiteSpace(scopeKey))
		{
			return StoryReadState.NoStory;
		}
		if (!source.TryGetValue(scopeKey, out var value) || value == null || value.Count == 0)
		{
			return StoryReadState.NoStory;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		bool flag = false;
		bool flag2 = false;
		foreach (string item in value)
		{
			if (!string.IsNullOrWhiteSpace(item) && hashSet.Add(item))
			{
				if (IsStoryRead(item))
				{
					flag = true;
				}
				else
				{
					flag2 = true;
				}
			}
		}
		if (flag2)
		{
			return StoryReadState.Unread;
		}
		if (flag)
		{
			return StoryReadState.Read;
		}
		return StoryReadState.NoStory;
	}

	private static bool IsStoryRead(string storyKey)
	{
		if (string.IsNullOrWhiteSpace(storyKey))
		{
			return false;
		}
		ReadStorySystem instance = ReadStorySystem.Instance;
		if (instance == null)
		{
			return false;
		}
		Dictionary<string, bool> fieldValue = instance.GetFieldValue<Dictionary<string, bool>>("_dicStoryIndex");
		if (fieldValue != null)
		{
			return fieldValue.ContainsKey(storyKey);
		}
		try
		{
			return instance.ExistKey(storyKey);
		}
		catch
		{
			if (DisplayRefreshDepth != 0)
			{
				throw;
			}
			return false;
		}
	}

	private static StoryReadState EvaluateSwitchStoryReadState(Dictionary<string, Dictionary<string, string>> source, string checkpointKey)
	{
		if (source == null || string.IsNullOrWhiteSpace(checkpointKey))
		{
			return StoryReadState.NoStory;
		}
		if (!source.TryGetValue(checkpointKey, out var value) || value == null || value.Count == 0)
		{
			return StoryReadState.NoStory;
		}
		bool flag = false;
		bool flag2 = false;
		foreach (KeyValuePair<string, string> item in value)
		{
			if (!string.IsNullOrWhiteSpace(item.Value))
			{
				if (IsStoryRead(item.Value))
				{
					flag = true;
				}
				else
				{
					flag2 = true;
				}
			}
		}
		if (flag2)
		{
			return StoryReadState.Unread;
		}
		if (flag)
		{
			return StoryReadState.Read;
		}
		return StoryReadState.NoStory;
	}

	private static string BuildSwitchBranchStateText(Dictionary<string, Dictionary<string, string>> source, string checkpointKey)
	{
		if (source == null || string.IsNullOrWhiteSpace(checkpointKey))
		{
			return string.Empty;
		}
		if (!source.TryGetValue(checkpointKey, out var value) || value == null || value.Count == 0)
		{
			return string.Empty;
		}
		List<string> list = new List<string>(value.Keys);
		list.Sort(CompareBranchLabels);
		List<string> list2 = new List<string>();
		foreach (string item in list)
		{
			if (value.TryGetValue(item, out var value2))
			{
				StoryReadState state = ((!string.IsNullOrWhiteSpace(value2)) ? ((!IsStoryRead(value2)) ? StoryReadState.Unread : StoryReadState.Read) : StoryReadState.NoStory);
				list2.Add(ColorizeBranchLabel(item, state));
			}
		}
		return string.Join(" ", list2);
	}

	private static int CompareBranchLabels(string left, string right)
	{
		if (int.TryParse(left, out var result) && int.TryParse(right, out var result2))
		{
			return result.CompareTo(result2);
		}
		if (int.TryParse(left, out var result3))
		{
			return -1;
		}
		if (int.TryParse(right, out result3))
		{
			return 1;
		}
		return string.Compare(left, right, StringComparison.Ordinal);
	}

	private static string ColorizeBranchLabel(string label, StoryReadState state)
	{
		string text = label ?? string.Empty;
		return state switch
		{
			StoryReadState.Read => "<color=#E8A53A>" + text + "</color>", 
			StoryReadState.Unread => "<color=#FFFFFF>" + text + "</color>", 
			_ => "<color=#333333>" + text + "</color>", 
		};
	}

	private static string ResolveOptionStoryText(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return string.Empty;
		}
		ILocaleResolver localeResolver = LocalizationManager.Instance?.LocaleResolver;
		if (localeResolver == null)
		{
			return string.Empty;
		}
		string text = string.Empty;
		try
		{
			_suppressStoryTextPostfix = true;
			text = localeResolver.GetStoryText(key);
		}
		catch (Exception ex)
		{
			if (DisplayRefreshDepth != 0)
			{
				throw;
			}
			DicePlugin.LogWarning("선택지 텍스트 조회 실패(" + key + "): " + ex.Message);
			return string.Empty;
		}
		finally
		{
			_suppressStoryTextPostfix = false;
		}
		if (string.IsNullOrWhiteSpace(text) || text == key)
		{
			return string.Empty;
		}
		string input = Regex.Replace(text, "<.*?>", string.Empty).Replace("\r", " ").Replace("\n", " ");
		input = Regex.Replace(input, "\\s+", " ").Trim();
		return input;
	}

	internal static void RebuildLiveLogEntries()
	{
		LogBridge.Tick(DicePlugin.Instance, manual: false);
	}

	internal static int PreviewReadGroup(StatGroupVariable P_0)
	{
		return SafeDisplayEvaluation.ReadGroup(P_0);
	}

	internal static bool PreviewCheckComparison(StatCompareItem P_0)
	{
		return SafeDisplayEvaluation.CheckComparison(P_0);
	}

	internal static int PreviewReadEventRate(PositionEventData P_0)
	{
		return SafeDisplayEvaluation.ReadEventRate(P_0);
	}

	[HarmonyPatch(typeof(SaveSystem), "NewGameData")]
	[HarmonyPrefix]
	internal static void LiveLog_SaveSystem_NewGameData_Prefix(out long __state)
	{
		__state = LogBridge.EnterTransition();
	}

	[HarmonyPatch(typeof(SaveSystem), "NewGameData")]
	[HarmonyFinalizer]
	internal static Exception LiveLog_SaveSystem_NewGameData_Finalizer(Exception __exception, long __state)
	{
		LogBridge.ExitTransition(__state);
		return __exception;
	}

	[HarmonyPatch(typeof(SaveSystem), "NewActGameData")]
	[HarmonyPrefix]
	internal static void LiveLog_SaveSystem_NewActGameData_Prefix(out long __state)
	{
		__state = LogBridge.EnterTransition();
	}

	[HarmonyPatch(typeof(SaveSystem), "NewActGameData")]
	[HarmonyFinalizer]
	internal static Exception LiveLog_SaveSystem_NewActGameData_Finalizer(Exception __exception, long __state)
	{
		LogBridge.ExitTransition(__state);
		return __exception;
	}

	[HarmonyPatch(typeof(SaveSystem), "LoadGameData")]
	[HarmonyPrefix]
	internal static void LiveLog_SaveSystem_LoadGameData_Prefix(out long __state)
	{
		__state = LogBridge.EnterTransition();
	}

	[HarmonyPatch(typeof(SaveSystem), "LoadGameData")]
	[HarmonyFinalizer]
	internal static Exception LiveLog_SaveSystem_LoadGameData_Finalizer(Exception __exception, long __state)
	{
		LogBridge.ExitTransition(__state);
		return __exception;
	}

	[HarmonyPatch(typeof(SaveSystem), "AutoLoadGameData")]
	[HarmonyPrefix]
	internal static void LiveLog_SaveSystem_AutoLoadGameData_Prefix(out long __state)
	{
		__state = LogBridge.EnterTransition();
	}

	[HarmonyPatch(typeof(SaveSystem), "AutoLoadGameData")]
	[HarmonyFinalizer]
	internal static Exception LiveLog_SaveSystem_AutoLoadGameData_Finalizer(Exception __exception, long __state)
	{
		LogBridge.ExitTransition(__state);
		return __exception;
	}

	[HarmonyPatch(typeof(SaveSystem), "LoadRestoreSaveData")]
	[HarmonyPrefix]
	internal static void LiveLog_SaveSystem_LoadRestoreSaveData_Prefix(out long __state)
	{
		__state = LogBridge.EnterTransition();
	}

	[HarmonyPatch(typeof(SaveSystem), "LoadRestoreSaveData")]
	[HarmonyFinalizer]
	internal static Exception LiveLog_SaveSystem_LoadRestoreSaveData_Finalizer(Exception __exception, long __state)
	{
		LogBridge.ExitTransition(__state);
		return __exception;
	}

	[HarmonyPatch(typeof(SaveSystem), "ExecuteLoadGameData")]
	[HarmonyPrefix]
	internal static void LiveLog_SaveSystem_ExecuteLoadGameData_Prefix(out long __state)
	{
		__state = LogBridge.EnterTransition();
	}

	[HarmonyPatch(typeof(SaveSystem), "ExecuteLoadGameData")]
	[HarmonyFinalizer]
	internal static Exception LiveLog_SaveSystem_ExecuteLoadGameData_Finalizer(Exception __exception, long __state)
	{
		LogBridge.ExitTransition(__state);
		return __exception;
	}

	[HarmonyPatch(typeof(SceneController), "StartLoadScene")]
	[HarmonyPrefix]
	internal static void LiveLog_SceneController_StartLoadScene_Prefix(out long __state)
	{
		__state = LogBridge.EnterTransition();
	}

	[HarmonyPatch(typeof(SceneController), "StartLoadScene")]
	[HarmonyFinalizer]
	internal static Exception LiveLog_SceneController_StartLoadScene_Finalizer(Exception __exception, long __state)
	{
		LogBridge.ExitTransition(__state);
		return __exception;
	}

	[HarmonyPatch(typeof(LuaEnvironment), "LoadLuaFunction")]
	[HarmonyPrefix]
	internal static void LiveLog_LuaStage_Prefix(string luaString, string friendlyName, out long __state)
	{
		__state = LogBridge.StageLua(luaString, friendlyName);
	}

	[HarmonyPatch(typeof(LuaEnvironment), "LoadLuaFunction")]
	[HarmonyPostfix]
	internal static void LiveLog_LuaComplete_Postfix(object __result, long __state)
	{
		LogBridge.CompleteLua(__state, __result);
	}

	[HarmonyPatch(typeof(LuaEnvironment), "LoadLuaFunction")]
	[HarmonyFinalizer]
	internal static Exception LiveLog_LuaAbort_Finalizer(Exception __exception, long __state)
	{
		if (__exception != null)
		{
			LogBridge.AbortLua(__state);
		}
		return __exception;
	}
}
