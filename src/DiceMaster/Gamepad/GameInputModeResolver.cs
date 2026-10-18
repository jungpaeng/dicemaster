using System;
using System.Diagnostics;
using System.Reflection;

namespace DiceMaster.Gamepad
{
    // Reflection keeps the optional InputSystem boundary out of compile/runtime
    // dependencies. Never call OnControlsChanged or modify cursor/action maps.
    public static class GameInputModeResolver
    {
        private static object controller;
        private static Type controllerType, unityObjectType;
        private static FieldInfo playerInputField;
        private static PropertyInfo activeProperty, schemeProperty, inputActiveProperty;
        private static MethodInfo findObjects;
        private static long nextSearch;
        private static string cause, warning;
        private static Type FindType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(name, false);
                if (type != null) return type;
            }
            return null;
        }
        public static LogInputMode FromScheme(string scheme, bool active)
        {
            if (!active) return LogInputMode.Unknown;
            return scheme == "Gamepad" ? LogInputMode.Gamepad
                : scheme == "Keyboard&Mouse" ? LogInputMode.KeyboardMouse : LogInputMode.Unknown;
        }
        private static LogInputMode Unavailable(string reason)
        {
            if (cause != reason)
            {
                cause = reason;
                warning = "로그 입력 모드 조회 불가 (" + reason + "): 마우스·키보드와 안전한 XInput 패드를 함께 지원합니다.";
            }
            return LogInputMode.Unknown;
        }
        public static LogInputMode Resolve()
        {
            try
            {
                if (controllerType == null)
                {
                    controllerType = FindType("Mortal.Core.PlayerInputController");
                    unityObjectType = FindType("UnityEngine.Object");
                    if (controllerType == null || unityObjectType == null) return Unavailable("controller-type");
                    playerInputField = controllerType.GetField("_playerInput", BindingFlags.Instance | BindingFlags.NonPublic);
                    activeProperty = controllerType.GetProperty("isActiveAndEnabled");
                    findObjects = unityObjectType.GetMethod("FindObjectsOfType", new[] { typeof(Type) });
                }
                if (playerInputField == null || activeProperty == null || findObjects == null) return Unavailable("controller-boundary");
                if (controller == null || !(bool)activeProperty.GetValue(controller, null))
                {
                    controller = null;
                    long now = Stopwatch.GetTimestamp();
                    if (now < nextSearch) return LogInputMode.Unknown;
                    nextSearch = now + Stopwatch.Frequency / 2;
                    Array candidates = (Array)findObjects.Invoke(null, new object[] { controllerType });
                    foreach (object candidate in candidates)
                    {
                        if (!(bool)activeProperty.GetValue(candidate, null)) continue;
                        if (controller != null) { controller = null; return Unavailable("multiple-active-controllers"); }
                        controller = candidate;
                    }
                    if (controller == null) return Unavailable("no-active-controller");
                }
                object input = playerInputField.GetValue(controller);
                if (input == null) return Unavailable("player-input-unprepared");
                if (schemeProperty == null || schemeProperty.DeclaringType != input.GetType())
                {
                    schemeProperty = input.GetType().GetProperty("currentControlScheme");
                    inputActiveProperty = input.GetType().GetProperty("isActiveAndEnabled");
                }
                if (schemeProperty == null || inputActiveProperty == null) return Unavailable("scheme-boundary");
                PropertyInfo missing = input.GetType().GetProperty("hasMissingRequiredDevices");
                if (missing != null && (bool)missing.GetValue(input, null)) return Unavailable("missing-required-device");
                PropertyInfo devices = input.GetType().GetProperty("devices");
                if (devices != null)
                {
                    object connectedDevices = devices.GetValue(input, null);
                    PropertyInfo count = connectedDevices == null ? null : connectedDevices.GetType().GetProperty("Count");
                    if (count != null && (int)count.GetValue(connectedDevices, null) == 0) return Unavailable("no-paired-device");
                }
                string scheme = schemeProperty.GetValue(input, null) as string;
                LogInputMode mode = FromScheme(scheme, (bool)inputActiveProperty.GetValue(input, null));
                if (mode == LogInputMode.Unknown) return Unavailable("unknown-or-inactive-scheme");
                // ControlSchemeType returns zero even for null/unknown strings;
                // only the validated string can establish keyboard/mouse mode.
                if (cause != null)
                {
                    cause = null;
                    warning = "로그 입력 모드 조회가 복구되었습니다. 게임의 현재 제어 체계를 다시 사용합니다.";
                }
                return mode;
            }
            catch (Exception)
            {
                controller = null;
                return Unavailable("lookup-failed");
            }
        }
        public static string ConsumeWarning() { string result = warning; warning = null; return result; }
    }
}
