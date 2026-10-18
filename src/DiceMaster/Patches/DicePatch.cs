using BepInEx;
using DiceMaster.Config;
using DiceMaster.Gamepad;
using HarmonyLib;
using Mortal.Story;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DiceMaster;

internal static class DicePatch
{
	private static int _diceValue;

	private static InputField _curInputField;

	private static GUIStyle _gamepadHintStyle;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(DiceMenuDialog), "Awake")]
	public static void DiceMenuDialog_Awake_Postfix(DiceMenuDialog __instance)
	{
		if (!(DicePlugin.Instance.DictInputPrefab != null))
		{
			return;
		}
		GameObject gameObject = Object.Instantiate(DicePlugin.Instance.DictInputPrefab, __instance.transform, worldPositionStays: false);
		gameObject.transform.localPosition = new Vector3(850f, -230f, 0f);
		_curInputField = gameObject.GetComponentInChildren<InputField>();
		((UnityEvent<string>)_curInputField.GetComponentInChildren<InputField>().onValueChanged).AddListener(delegate(string value)
		{
			if (int.TryParse(value, out var result))
			{
				_diceValue = result;
			}
		});
		_curInputField.text = "0";
		_diceValue = 0;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(DiceMenuDialog), "OnEnable")]
	public static void DiceMenuDialog_OnEnable_Postfix(DiceMenuDialog __instance)
	{
		if (_curInputField != null)
		{
			_curInputField.text = "0";
			_diceValue = 0;
		}
		GamepadBridge.ResetAdjustment();
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CheckPointManager), "Dice")]
	public static void CheckPointManager_Dice_Prefix(ref int random)
	{
		if (_diceValue != 0 && !(DicePlugin.Instance.DictInputPrefab == null))
		{
			DiceMenuDialog diceMenuDialog = Object.FindObjectOfType<DiceMenuDialog>();
			if (diceMenuDialog != null)
			{
				diceMenuDialog.SetPrivateField("_currentRandomValue", _diceValue);
			}
			random = _diceValue;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(DiceMenuDialog), "CheckRevolution")]
	public static bool DiceMenuDialog_CheckRevolution_Prefix(ref bool __result)
	{
		__result = true;
		return false;
	}

	internal static void UpdateGamepad()
	{
		int num = GamepadBridge.Tick(windowVisible: _curInputField != null && (_curInputField.gameObject.activeInHierarchy & Application.isFocused), enabled: ConfigManager.EnableDiceController.Value & ConfigManager.EnableGamepadAdjustment.Value, current: _diceValue, time: Time.unscaledTime, cacheRoot: Paths.CachePath);
		if (num != _diceValue)
		{
			_diceValue = num;
			if (_curInputField != null)
			{
				_curInputField.text = num.ToString();
			}
		}
		string text = GamepadBridge.ConsumeWarning();
		if (!string.IsNullOrEmpty(text))
		{
			DicePlugin.LogWarning(text);
		}
	}

	internal static void DrawGamepadHint()
	{
		if (GamepadBridge.ShowHint)
		{
			if (_gamepadHintStyle == null)
			{
				_gamepadHintStyle = new GUIStyle(GUI.skin.label)
				{
					fontSize = 18,
					wordWrap = true
				};
			}
			GUI.Label(new Rect((float)Screen.width - 600f, (float)Screen.height - 100f, 580f, 90f), GamepadBridge.Hint(), _gamepadHintStyle);
		}
	}
}
