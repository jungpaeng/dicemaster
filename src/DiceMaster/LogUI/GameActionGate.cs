using System;
using System.Reflection;
using DiceMaster.Config;
using DiceMaster.Gamepad;
using HarmonyLib;
using UnityEngine;

namespace DiceMaster.LogUI
{
    // Supplement the native XInput filter for keyboard/mouse action callbacks.
    // Action state and maps remain intact; canceled callbacks still clean up
    // game state. Direct InputAction value polling is outside this boundary.
    internal static class GameActionGate
    {
        private static volatile bool active, releasing;
        private static bool initialized;
        private static string warning;
        internal static void Initialize(Harmony harmony)
        {
            if (initialized) return;
            initialized = true;
            try
            {
                Type type = AccessTools.TypeByName("UnityEngine.InputSystem.InputActionState");
                MethodInfo target = type == null ? null : AccessTools.Method(type, "CallActionListeners");
                ParameterInfo[] args = target == null ? null : target.GetParameters();
                if (target == null || target.IsStatic || target.IsGenericMethod || target.ReturnType != typeof(void)
                    || args.Length != 5 || args[0].ParameterType != typeof(int)
                    || args[1].ParameterType.FullName != "UnityEngine.InputSystem.InputActionMap"
                    || args[2].ParameterType.FullName != "UnityEngine.InputSystem.InputActionPhase"
                    || !args[3].ParameterType.IsByRef || args[4].ParameterType != typeof(string))
                    throw new MissingMethodException("InputActionState.CallActionListeners contract differs");
                Type phase = args[2].ParameterType;
                if (Convert.ToInt32(Enum.Parse(phase, "Started")) != 2
                    || Convert.ToInt32(Enum.Parse(phase, "Performed")) != 3
                    || Convert.ToInt32(Enum.Parse(phase, "Canceled")) != 4)
                    throw new MissingMemberException("InputActionPhase contract differs");
                harmony.Patch(target, prefix: new HarmonyMethod(typeof(GameActionGate), nameof(BeforeActionListeners)));
            }
            catch (Exception e)
            {
                warning = "로그 게임 액션 차단 경계 연결 실패: " + e.GetType().Name
                    + ". XInput 차단은 유지되며 키보드·마우스의 게임 중복 입력은 Windows 확인이 필요합니다.";
            }
        }
        internal static void SetLogState(bool open, bool focused)
        {
            bool next = open && focused;
            if (active != next) releasing = true;
            active = next;
            ReleaseIfNeutral();
        }
        private static void ReleaseIfNeutral()
        {
            if (releasing && !active && Application.isFocused && !Input.anyKey && Input.touchCount == 0) releasing = false;
        }
        private static bool BeforeActionListeners(object __2)
        {
            ReleaseIfNeutral();
            // The InputSystem update can precede the plugin's Update. Consume
            // the configured opening/closing key at this callback boundary too.
            bool toggle = Application.isFocused && ConfigManager.LogMenuToggleKey != null
                && Input.GetKeyDown(ConfigManager.LogMenuToggleKey.Value);
            if (toggle) releasing = true;
            return LogActionPhases.Allow(active, releasing || toggle, Convert.ToInt32(__2));
        }
        internal static string ConsumeWarning() { string result = warning; warning = null; return result; }
        internal static void Shutdown() { active = releasing = false; }
    }
}
