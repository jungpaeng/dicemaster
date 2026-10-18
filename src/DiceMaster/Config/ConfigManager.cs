using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace DiceMaster.Config;

public static class ConfigManager
{
	internal static readonly Dictionary<string, IConfigElement> ConfigElements = new Dictionary<string, IConfigElement>();

	private const string SEC_NAME = "DiceMaster";

	public static ConfigElement<bool> EnableDiceController;

	public static ConfigElement<bool> EnableOptionInfo;

	public static ConfigElement<KeyCode> LogMenuToggleKey;

	public static ConfigElement<KeyCode> SpeedUpKey;

	public static ConfigElement<bool> EnableGamepadAdjustment;

	public static ConfigElement<float> LogWindowX;
	public static ConfigElement<float> LogWindowY;
	public static ConfigElement<float> LogWindowWidth;
	public static ConfigElement<float> LogWindowHeight;
	public static ConfigElement<int> LogFontSize;

	private static ConfigFile Config => DicePlugin.Instance.Config;

	public static void Init()
	{
		CreateConfigElements();
		LoadConfig();
	}

	internal static void RegisterConfigElement<T>(ConfigElement<T> configElement)
	{
		ConfigEntry<T> entry = Config.Bind("DiceMaster", configElement.Name, configElement.Value, configElement.Description);
		entry.SettingChanged += delegate
		{
			configElement.Value = entry.Value;
		};
		ConfigElements.Add(configElement.Name, configElement);
	}

	public static T GetConfigValue<T>(ConfigElement<T> element)
	{
		ConfigEntry<T> entry = null;
		if (!Config.TryGetEntry("DiceMaster", element.Name, out entry))
		{
			throw new Exception("Could not get config entry '" + element.Name + "'");
		}
		return entry.Value;
	}

	public static void SetConfigValue<T>(ConfigElement<T> element, T value)
	{
		ConfigEntry<T> entry = null;
		if (Config.TryGetEntry("DiceMaster", element.Name, out entry))
		{
			entry.Value = value;
		}
		else
		{
			DicePlugin.LogInfo("Could not get config entry '" + element.Name + "' to set value.");
		}
	}

	public static void LoadConfig()
	{
		foreach (KeyValuePair<string, IConfigElement> configElement in ConfigElements)
		{
			ConfigDefinition key = new ConfigDefinition("DiceMaster", configElement.Key);
			if (Config.ContainsKey(key))
			{
				ConfigEntryBase configEntryBase = Config[key];
				if (configEntryBase != null)
				{
					configElement.Value.BoxedValue = configEntryBase.BoxedValue;
				}
			}
		}
	}

	public static void SaveConfig()
	{
		Config.Save();
	}

	private static void CreateConfigElements()
	{
		EnableDiceController = new ConfigElement<bool>("EnableDiceController", "Enable dice controller", defaultValue: true);
		EnableOptionInfo = new ConfigElement<bool>("EnableOptionInfo", "Enable option info", defaultValue: true);
		LogMenuToggleKey = new ConfigElement<KeyCode>("LogMenuToggleKey", "Log menu toggle key", KeyCode.Tab);
		SpeedUpKey = new ConfigElement<KeyCode>("SpeedUpKey", "Speed up key", KeyCode.LeftControl);
		EnableGamepadAdjustment = new ConfigElement<bool>("EnableGamepadAdjustment", "Xbox 패드로 주사위를 조정합니다. LB+십자키/왼쪽 스틱: 수치 조정, LB+B: 자동.", defaultValue: true);
		LogWindowX = new ConfigElement<float>("LogWindowX", "로그 창 왼쪽 위치(화면 픽셀). 화면 밖/비정상 값은 화면 안으로 복구합니다.", 20f);
		LogWindowY = new ConfigElement<float>("LogWindowY", "로그 창 위쪽 위치(화면 픽셀). 화면 밖/비정상 값은 화면 안으로 복구합니다.", 20f);
		LogWindowWidth = new ConfigElement<float>("LogWindowWidth", "로그 창 너비(화면 픽셀). 540 이상, 현재 화면 너비 이하로 복구합니다(작은 화면에서는 화면 너비).", 750f);
		LogWindowHeight = new ConfigElement<float>("LogWindowHeight", "로그 창 높이(화면 픽셀). 440 이상, 현재 화면 높이 이하로 복구합니다(작은 화면에서는 화면 높이).", 750f);
		LogFontSize = new ConfigElement<int>("LogFontSize", "로그 창 글자 크기. 12~28, 기본 14. 작은 화면·창에서는 조작 영역 확보를 위해 표시 크기를 줄입니다.", 14);
	}
}
