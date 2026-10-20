using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DiceMaster.EmbeddedTranslations;
using DiceMaster.LiveLog;
using DiceMaster.LuaParse;
using Mortal.Core;
using Mortal.Story;

namespace DiceMaster;

public class PositionEventCache
{
	private static readonly Lazy<PositionEventCache> instance = new Lazy<PositionEventCache>(() => new PositionEventCache());

	private readonly Dictionary<string, List<PositionEventData>> positionEventDataCache;

	private readonly Dictionary<PositionEventData, string> eventKeyCache;

	private readonly Dictionary<PositionEventData, string> eventDescCache;

	private readonly Dictionary<PositionEventData, string> scriptPrefixCache;

	public static PositionEventCache Instance => instance.Value;

	private PositionEventCache()
	{
		positionEventDataCache = new Dictionary<string, List<PositionEventData>>();
		eventKeyCache = new Dictionary<PositionEventData, string>();
		eventDescCache = new Dictionary<PositionEventData, string>();
		scriptPrefixCache = new Dictionary<PositionEventData, string>();
	}

	public List<PositionEventData> GetPositionEvents(string position)
	{
		if (!positionEventDataCache.TryGetValue(position, out var value) || value == null || value.Count == 0)
		{
			value = LoadPositionEvents(position);
			if (value.Count != 0)
			{
				positionEventDataCache[position] = value;
			}
		}
		return value;
	}

	public string GetPositionEventName(PositionEventData eventData)
	{
		if (eventData == null)
		{
			return Display.TranslateLabel(GetLocalizedSystemTextForCache("System/Event/UnknownEventName", "(알수없는 이벤트명)", "시스템_이벤트명"));
		}
		if (eventKeyCache.TryGetValue(eventData, out var value))
		{
			return Display.TranslateLabel(value);
		}
		string key = positionEventDataCache.FirstOrDefault((KeyValuePair<string, List<PositionEventData>> p) => p.Value.Contains(eventData)).Key;
		string eventName = DataHelper.GetEventName(eventData);
		value = key + "이벤트: " + eventName;
		eventKeyCache[eventData] = value;
		return Display.TranslateLabel(value);
	}

	public string GetPositionEventDesc(PositionEventData eventData)
	{
		if (eventData == null)
		{
			return "";
		}
		try
		{
			if (!eventDescCache.TryGetValue(eventData, out var value))
			{
				value = GenerateStaticDesc(eventData);
				eventDescCache[eventData] = value;
			}
			string description = GeneratePreviewDynamicDesc(eventData) + "\n" + value;
			if (OptionInfoPatch.DisplayRefreshDepth == 0) LogBridge.RecordDescriptionResult(eventData, null);
			return description;
		}
		catch (PreviewReadException failure)
		{
			if (OptionInfoPatch.DisplayRefreshDepth != 0) throw;
			LogBridge.RecordDescriptionResult(eventData, failure);
			return (failure.IsWaiting ? RefreshEngine.WaitingStatus : RefreshEngine.ReadFailureStatus) +
				" | " + failure.PartialDescription;
		}
	}

	// The live log already owns a strict eligibility/condition snapshot. Read
	// only result text here so sampled dynamic conditions cannot contradict it.
	public string GetPositionEventResultDesc(PositionEventData eventData)
	{
		if (eventData == null) return "";
		if (!eventDescCache.TryGetValue(eventData, out var value))
		{
			value = GenerateStaticDesc(eventData);
			eventDescCache[eventData] = value;
		}
		return value;
	}

	public int GetPositionEventRate(PositionEventData eventData)
	{
		return OptionInfoPatch.PreviewReadEventRate(eventData);
	}

	public List<PositionEventData> GetPositionEventsSortedByRate(string position)
	{
		return GetPositionEvents(position).OrderByDescending(GetPositionEventRate).ToList();
	}

	private List<PositionEventData> LoadPositionEvents(string position)
	{
		List<PositionEventData> list = new List<PositionEventData>();
		CheckPointManager checkPointManager = CheckPointManager.Instance;
		if (checkPointManager == null)
		{
			return list;
		}
		PositionResultConfig fieldValue = checkPointManager.GetFieldValue<PositionResultConfig>("_position");
		if (fieldValue == null)
		{
			return list;
		}
		List<PositionResultData> list2 = ((CollectionData<PositionResultData>)fieldValue).List;
		if (list2 == null)
		{
			return list;
		}
		PositionResultData positionResultData = list2.FirstOrDefault((PositionResultData p) => p.name == position);
		if (positionResultData == null)
		{
			return list;
		}
		string fieldValue2 = positionResultData.GetFieldValue<string>("_scriptPrefix");
		List<PositionEventData> list3 = ((CollectionData<PositionEventData>)positionResultData).List;
		if (list3 == null)
		{
			return list;
		}
		foreach (PositionEventData item in list3)
		{
			if (!(item == null))
			{
				scriptPrefixCache[item] = fieldValue2;
				list.Add(item);
			}
		}
		return list;
	}

	private string GenerateStaticDesc(PositionEventData eventData)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(GetLocalizedSystemTextForCache("System/Event/PossibleResults", "가능한 결과：", "시스템_이벤트"));
		if (!scriptPrefixCache.TryGetValue(eventData, out var value))
		{
			UntranslatedLogger.LogUntranslated("이벤트_스크립트접두사", eventData.name, eventData.name);
			value = eventData.name;
		}
		List<DiceMaster.LuaParse.Action> source = GetDefinitionParser().ExtractActionsFromLua(value + "_" + eventData.ResultType);
		if (source.Count > 0 && source[0].Analysis != null)
		{
			// The structured description owns branch/followup labels. Wrapping
			// the whole analysis in "immediate" would mislabel conditional effects.
			stringBuilder.AppendLine(LuaParser.Description(source));
			return stringBuilder.ToString();
		}
		List<DiceMaster.LuaParse.Action> actions = source.Where((DiceMaster.LuaParse.Action a) => !a.IsConditional).ToList();
		List<DiceMaster.LuaParse.Action> actions2 = source.Where((DiceMaster.LuaParse.Action a) => a.IsConditional).ToList();
		string text = LuaParser.Description(actions);
		string text2 = LuaParser.Description(actions2);
		string localizedSystemTextForCache = GetLocalizedSystemTextForCache("System/Event/ImmediateTrigger", "    즉시 발동：", "시스템_이벤트");
		string localizedSystemTextForCache2 = GetLocalizedSystemTextForCache("System/Event/ConditionalTrigger", "특수 조건 만족시：", "시스템_이벤트");
		string localizedSystemTextForCache3 = GetLocalizedSystemTextForCache("System/Event/None", "없음", "시스템_이벤트");
		stringBuilder.AppendLine(localizedSystemTextForCache + (string.IsNullOrEmpty(text) ? localizedSystemTextForCache3 : text));
		stringBuilder.AppendLine(localizedSystemTextForCache2 + (string.IsNullOrEmpty(text2) ? localizedSystemTextForCache3 : text2));
		return stringBuilder.ToString();
	}

	internal void RetryDefinitionReads(PositionEventData eventData)
	{
		if (eventData == null) return;
		if (!scriptPrefixCache.TryGetValue(eventData, out string prefix)) prefix = eventData.name;
		List<DiceMaster.LuaParse.Action> actions = GetDefinitionParser().ExtractActionsFromLua(prefix + "_" + eventData.ResultType);
		LuaParser.RefreshPreviewReads(actions);
	}

	private string GenerateDynamicDesc(PositionEventData eventData)
	{
		return PreviewFormatting.EventDescription(eventData);
	}

	private string GetLocalizedSystemTextForCache(string key, string defaultValue, string categoryForLog = "시스템_이벤트캐시", string originalIdentifierIfKeyFailed = null)
	{
		LocalizationManager localizationManager = LocalizationManager.Instance;
		if (((localizationManager != null) ? localizationManager.LocaleResolver : null) != null && !string.IsNullOrEmpty(key))
		{
			string text = LocalizationManager.Instance.LocaleResolver.GetString(key);
			if (!string.IsNullOrEmpty(text) && text != key)
			{
				return text;
			}
			if (text == key || string.IsNullOrEmpty(text))
			{
				UntranslatedLogger.LogUntranslated(categoryForLog, key, string.IsNullOrEmpty(originalIdentifierIfKeyFailed) ? key : originalIdentifierIfKeyFailed);
			}
		}
		return defaultValue;
	}

	internal string GeneratePreviewDynamicDesc(PositionEventData P_0)
	{
		return LogBridge.DescribeEvent(this, P_0);
	}

	internal void ClearDisplayCaches()
	{
		positionEventDataCache.Clear();
		eventKeyCache.Clear();
		eventDescCache.Clear();
		scriptPrefixCache.Clear();
	}

	internal static LuaParser GetDefinitionParser()
	{
		return (LuaParser)LogBridge.GetEventParser();
	}
}
