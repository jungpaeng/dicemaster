using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using DiceMaster.LogUI;

namespace DiceMaster.Config;

public static class ConfigManager
{
	internal static readonly Dictionary<string, IConfigElement> ConfigElements = new Dictionary<string, IConfigElement>();

	private const string SEC_NAME = "DiceMaster";

	public static ConfigElement<bool> EnableDiceController;

	public static ConfigElement<bool> EnableOptionInfo;
	public static ConfigElement<bool> EnableEventSelectionPopup;

	public static ConfigElement<KeyCode> LogMenuToggleKey;
	public static ConfigElement<bool> EnableGamepadLogOpen;

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
		EnableEventSelectionPopup = new ConfigElement<bool>("EnableEventSelectionPopup", "실제 이벤트 판정 직전에 이벤트 선택 팝업을 표시합니다. 끄면 대기를 취소하고 원래 무작위 판정으로 진행합니다. EnableOptionInfo가 유효하게 켜져 있을 때만 작동하며, 이 상위 설정의 재시작 규칙은 유지합니다.", defaultValue: true);
		LogMenuToggleKey = new ConfigElement<KeyCode>("LogMenuToggleKey", "Log menu toggle key", KeyCode.Tab);
		EnableGamepadLogOpen = new ConfigElement<bool>("EnableGamepadLogOpen", "같은 패드의 양쪽 스틱 클릭(LS+RS) 누름이 겹치면 로그를 엽니다. 한쪽을 누른 채 다른 쪽을 누르면 되며 순서와 간격 제한은 없습니다. 활성화하면 게임에 초점이 있는 동안 두 스틱 클릭을 로그 전용으로 사용합니다. 끄면 버튼 해제 후 원래 게임 입력으로 돌아갑니다. Tab 열기는 유지합니다.", defaultValue: true);
		SpeedUpKey = new ConfigElement<KeyCode>("SpeedUpKey", "Speed up key", KeyCode.LeftControl);
		EnableGamepadAdjustment = new ConfigElement<bool>("EnableGamepadAdjustment", "Xbox 패드로 주사위를 조정합니다. LB+십자키/왼쪽 스틱: 수치 조정, LB+B: 자동.", defaultValue: true);
		// Bind a stable seed. Awake can run before the game's final resolution;
		// the renderer resolves default geometry only when it is first drawn.
		Rect layout = new Rect(20, 20, LogLayout.DefaultWidth, LogLayout.DefaultHeight);
		LogWindowX = new ConfigElement<float>("LogWindowX", "로그 창 왼쪽 위치(화면 픽셀). 화면 밖/비정상 값은 화면 안으로 복구합니다.", layout.x);
		LogWindowY = new ConfigElement<float>("LogWindowY", "로그 창 위쪽 위치(화면 픽셀). 화면 밖/비정상 값은 화면 안으로 복구합니다.", layout.y);
		LogWindowWidth = new ConfigElement<float>("LogWindowWidth", "로그 창 너비(화면 픽셀). 새 기본 배치는 1920×1200 화면의 872px 비율(너비 약 45.42%)로 처음 표시할 때 적용합니다. 사용자 지정값은 유지합니다.", layout.width);
		LogWindowHeight = new ConfigElement<float>("LogWindowHeight", "로그 창 높이(화면 픽셀). 새 기본 배치는 1920×1200 화면의 960px 비율(높이 80%)로 처음 표시할 때 적용합니다. 사용자 지정값은 유지합니다.", layout.height);
		LogFontSize = new ConfigElement<int>("LogFontSize", "로그 창 글자 크기. 12~64px, 기본 1920×1200의 20px(처음 표시하는 화면 높이에 맞춰 조정, 배율 0.85~4). 창 크기로 다시 축소하지 않습니다.", LogLayout.DefaultFontSize);
	}
}
