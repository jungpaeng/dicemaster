using System;
using System.Globalization;

namespace DiceMaster.Gamepad
{
    public static class GamepadBridge
    {
        private static readonly PadAdjustment adjustment = new PadAdjustment();
        private static readonly NativeXInput native = new NativeXInput();
        private static readonly LogPadController log = new LogPadController();
        private static readonly PadState[] states = new PadState[4];
        private static readonly bool[] pads = new bool[4];
        private static bool initialized, failed, visible, connected, ready, adjusting, logOpen, logFocused;
        private static bool logCapture, lastReady, reportedWaiting;
        private static LogInputMode lastMode;
        private static int controller = -1, value;
        private static string warning;
        private static void Initialize(string cacheRoot)
        {
            if (initialized) return;
            initialized = true;
            try { native.Initialize(cacheRoot); }
            catch (Exception e) { failed = true; warning = "패드 입력 차단을 시작하지 못했습니다: " + e.Message; }
        }
        private static void Poll()
        {
            for (uint i = 0; i < 4; i++) pads[i] = native.ReadRaw(i, out states[i]);
            ready = native.PollingFresh;
        }
        public static LogPadFrame TickLog(bool open, bool focused, LogInputMode mode, float time, string cacheRoot)
        {
            logOpen = open; logFocused = focused;
            if (open) Initialize(cacheRoot);
            bool capture = open && focused && !failed;
            if (capture != logCapture || (open && mode != lastMode))
            {
                native.RequireNeutral(); adjustment.Reset();
            }
            logCapture = capture; lastMode = mode;
            native.LogActive = capture;
            if (capture) native.WindowActive = false;
            if (initialized && !failed) Poll();
            if (open && !failed && ready != lastReady)
            {
                if (!ready) { warning = "로그 패드 입력 중단: 게임의 XInput 차단 경로 응답을 기다립니다. 마우스·키보드로 복구할 수 있습니다."; reportedWaiting = true; }
                else if (reportedWaiting) { warning = "로그 패드 입력 차단 경로가 복구되었습니다. 버튼 해제·스틱 중립 후 새 입력을 기다립니다."; reportedWaiting = false; }
                native.RequireNeutral(); log.Cancel();
            }
            if (open && !failed && !ready && !reportedWaiting)
            {
                warning = "로그 패드 입력 대기: 게임의 XInput 차단 경로를 아직 확인하지 못했습니다. 마우스·키보드를 사용해 주세요.";
                reportedWaiting = true;
            }
            lastReady = ready;
            return log.Tick(open, focused, !failed && ready, mode, time, states, pads);
        }
        public static void CancelLogInput() { log.Cancel(); native.RequireNeutral(); adjustment.Reset(); }
        public static void SuspendLogInput()
        {
            // Disabling the component stops Update without destroying hooks.
            // Return ownership through the same release/neutral barrier while
            // keeping the initialized native bridge usable after re-enable.
            logOpen = logFocused = logCapture = false;
            native.LogActive = native.WindowActive = false;
            CancelLogInput();
        }
        public static void NotifyPointerActivity() { log.PointerActivity(); }
        public static void NotifyKeyboardActivity() { log.KeyboardActivity(); }
        public static void ReselectLogController() { log.Reselect(pads); native.RequireNeutral(); }
        public static string LogHint()
        {
            if (failed) return "패드 입력 차단 실패: 패드 조작을 중단했습니다. 마우스·키보드와 BepInEx 로그를 확인해 주세요.";
            if (!ready) return "게임의 XInput 차단 경로 확인 대기 · 마우스·키보드 사용 가능";
            if (log.Owner < 0) return "연결된 XInput 패드 없음 · 마우스·키보드 사용 가능";
            if (!pads[log.Owner]) return "선택 패드 연결 해제 · 재연결 또는 도구의 패드 다시 선택";
            if (log.Waiting) return "패드 " + (log.Owner + 1) + " · 버튼 해제·양쪽 스틱 중립·트리거 해제 후 새 입력";
            if (!log.PadMode) return "게임의 키보드·마우스 모드 · 패드 전환은 입력 후 중립을 거쳐 다시 조작";
            // The renderer shows context-specific controls in its footer.
            // Keep device ownership/status to one line above the content.
            return "패드 " + (log.Owner + 1) + " · 로그 탐색 중";
        }
        public static int Tick(bool enabled, bool windowVisible, int current, float time, string cacheRoot)
        {
            visible = enabled && windowVisible && !logOpen;
            value = current;
            if (!enabled || logOpen) { native.WindowActive = false; ResetAdjustment(); return current; }
            Initialize(cacheRoot);
            native.WindowActive = visible && !failed;
            if (failed) { ResetAdjustment(); return current; }
            Poll();
            PadState state = default(PadState);
            bool found = false; int selected = -1;
            for (int i = 0; i < 4; i++)
            {
                if (!pads[i]) continue;
                if (!found || (states[i].Buttons & PadButtons.LB) != 0)
                {
                    found = true; state = states[i]; selected = i;
                    if ((state.Buttons & PadButtons.LB) != 0) break;
                }
            }
            connected = found;
            if (selected != controller) { adjustment.Reset(); controller = selected; }
            // The log release latch also gates raw dice edits. Filtering only the
            // game path would still permit a held log input to change the dice.
            adjusting = visible && ready && connected && native.CanUseRaw((uint)selected)
                && (state.Buttons & PadButtons.LB) != 0;
            value = adjustment.Tick(adjusting, state.Buttons, time, current, state.LeftX, state.LeftY);
            return value;
        }
        public static bool ShowHint { get { return visible; } }
        public static string Hint()
        {
            if (failed) return "패드 조정을 사용할 수 없습니다. 로그를 확인해 주세요.";
            if (!connected) return "패드를 연결하면 LB를 누른 채 주사위 수치를 조정할 수 있습니다.";
            if (!ready) return "게임의 패드 입력을 기다리는 중입니다.";
            string current = value == 0 ? "자동" : value.ToString(CultureInfo.InvariantCulture);
            return "주사위: " + current + (adjusting ? "  [LB 조정 중]" : "")
                + "\nLB + 십자키/왼쪽 스틱: ←/→ ±1   ↑/↓ ±10\nLB+B: 자동 (0)";
        }
        public static string ConsumeWarning()
        {
            string result = warning; warning = null;
            return result ?? GameInputModeResolver.ConsumeWarning();
        }
        public static void ResetAdjustment() { adjustment.Reset(); adjusting = false; controller = -1; }
        public static void Shutdown()
        {
            native.LogActive = false; native.WindowActive = false; native.Dispose();
            visible = ready = connected = adjusting = false; logOpen = logFocused = false;
            ResetAdjustment(); log.Cancel();
        }
    }
}
