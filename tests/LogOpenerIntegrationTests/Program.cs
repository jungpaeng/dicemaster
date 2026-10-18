using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using DiceMaster.Gamepad;
using DiceMaster.LogUI;

internal static class Program
{
    private const ushort LeftThumb = 0x0040, RightThumb = 0x0080, Chord = LeftThumb | RightThumb;
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
    private static int assertions;
    private static void Check(bool value, string message)
    { assertions++; if (!value) throw new InvalidOperationException(message); }

    // Every case gets fresh production statics. Test setup substitutes only the
    // original XInput export and successful Win32 initialization, never owner,
    // waiting, pad override, capture, readiness, command or UI selection state.
    private sealed class Boundary
    {
        internal readonly PadState[] Physical = new PadState[4];
        internal readonly bool[] Connected = new bool[4];
        internal readonly NativeXInput Native;
        internal readonly LogPadController Controller;
        internal readonly XInputHook Hook;
        internal bool Open, Focused = true, Enabled = true;
        internal float Time;
        internal int OpenRequests, GameActivations;
        internal Boundary(int owner = 0)
        {
            Connected[owner] = true;
            UnityEngine.Application.isFocused = true;
            Native = (NativeXInput)typeof(GamepadBridge).GetField("native", PrivateStatic).GetValue(null);
            Controller = (LogPadController)typeof(GamepadBridge).GetField("log", PrivateStatic).GetValue(null);
            Hook = new XInputHook { Owner = Native, Original = ReadOriginal };
            ((XInputHook[])typeof(NativeXInput).GetField("hooks", PrivateInstance).GetValue(Native))[0] = Hook;
            typeof(NativeXInput).GetField("count", PrivateInstance).SetValue(Native, 1);
            typeof(GamepadBridge).GetField("initialized", PrivateStatic).SetValue(null, true);
            GameActionGate.Initialize(new HarmonyLib.Harmony());
            Check(HarmonyLib.Harmony.Prefix != null, "actual GameActionGate initialization must connect its prefix through the declared InputActionState signature");
        }
        private uint ReadOriginal(uint user, out PadState state)
        {
            state = user < 4 ? Physical[user] : default;
            return user < 4 && Connected[user] ? 0u : 1167u;
        }
        internal void State(int slot, ushort buttons = 0, short lx = 0, short ly = 0, short rx = 0, short ry = 0, byte lt = 0, byte rt = 0)
        {
            uint packet = Physical[slot].Packet + 1;
            Physical[slot] = new PadState { Packet = packet, Buttons = buttons, LeftX = lx, LeftY = ly, RightX = rx, RightY = ry, LeftTrigger = lt, RightTrigger = rt };
        }
        internal PadState GamePoll(int slot)
        {
            uint result = Hook.Invoke((uint)slot, out PadState state);
            Check(result == (Connected[slot] ? 0u : 1167u), "real hook preserves connected/disconnected export result");
            return state;
        }
        internal void GamePollAll()
        { for (int slot = 0; slot < 4; slot++) if (Connected[slot]) GamePoll(slot); }
        internal void RawPollAll()
        {
            for (uint slot = 0; slot < 4; slot++)
            {
                bool connected = Native.ReadRaw(slot, out PadState raw);
                Check(connected == Connected[slot] && (!connected || raw.Buttons == Physical[slot].Buttons),
                    "actual UI raw path reads the physical export without filtering its state or creating a gesture");
            }
        }
        internal int Poll()
        {
            UnityEngine.Application.isFocused = Focused;
            int owner = GamepadBridge.PollLogOpener(Enabled, Focused, Open, "unused-no-Win32-initialization");
            if (owner >= 0) OpenRequests++;
            return owner;
        }
        internal void Arm()
        {
            Check(Poll() == -1, "configuration alone cannot fabricate an opening request");
            GamePollAll();
            Check(Poll() == -1 && Native.PollingFresh, "actual game neutral poll arms safely without opening");
        }
        internal bool Action(int phase)
        {
            bool allowed = (bool)HarmonyLib.Harmony.Prefix.Invoke(null, new object[] { (UnityEngine.InputSystem.InputActionPhase)phase });
            if (allowed && phase == 3) GameActivations++;
            return allowed;
        }
        internal void Captured(string reason)
        {
            Check(!Action(2) && !Action(3) && Action(4), reason + ": actual pending/active action gate suppresses Started/Performed and permits Canceled cleanup");
            for (int slot = 0; slot < 4; slot++)
                if (Connected[slot]) Check(PadInputFilter.IsNeutral(GamePoll(slot)), reason + ": game-facing native hook captures all channels and slots");
        }
        internal void Accept(int owner, string scheme)
        {
            Check(owner >= 0, "opening must come from an actual consumed native request");
            // Match DicePlugin's public opening boundary: keyboard/pointer focus
            // cancellation, gate activation, then explicit pad-origin ownership.
            Open = true;
            GameActionGate.SetLogState(true, Focused);
            GamepadBridge.CancelLogInput();
            Captured("SetLogOpen cancellation-to-BeginPadLog");
            GamepadBridge.BeginPadLog(owner);
            Mortal.Core.FakePlayerInput.Current.SetScheme(scheme);
            Check(Tick().Command == LogPadCommand.None, "opening gesture cannot also navigate or activate");
            Check(Controller.Owner == owner, "the opening device must be the actual navigation owner");
            Captured("open handoff");
        }
        internal LogPadFrame Tick()
        {
            UnityEngine.Application.isFocused = Focused;
            Time += .1f;
            return GamepadBridge.TickLog(Open, Focused, GameInputModeResolver.Resolve(), Time, "unused-no-Win32-initialization");
        }
        internal void Neutral(int owner)
        { State(owner); GamePollAll(); Check(Tick().Command == LogPadCommand.None, "neutral only rearms and never activates"); }
        internal void OpenGesture(int slot, bool bridgeFirst = false)
        {
            State(slot, Chord);
            if (bridgeFirst) { RawPollAll(); Check(Poll() == -1 && !GamepadBridge.LogOpening, "raw UI polling cannot detect a game-facing opening gesture"); }
            Check((GamePoll(slot).Buttons & Chord) == 0, "opening chord never reaches the game's original button state");
            Check(GamepadBridge.LogOpening, "real game callback must synchronously expose the pending opening barrier");
            Captured("before plugin Update");
        }
    }

    private static void Main(string[] args)
    {
        if (args.Length == 1)
        {
            Run(args[0]);
            Check(UnityEngine.Random.Calls == 0, "opening/navigation must not call the game RNG");
            Console.WriteLine("CASE: " + args[0] + ": " + assertions + " assertions");
            return;
        }
        var cases = new List<string>();
        foreach (string schedule in new[] { "game", "bridge" })
            foreach (string mode in new[] { "Gamepad", "KeyboardMouse", "Unknown" })
                foreach (int slot in new[] { 0, 2 }) cases.Add("open/" + schedule + "/" + mode + "/" + slot);
        cases.AddRange(new[] { "raw-only", "no-polling", "pending-stale", "cancel", "disable", "focus", "disconnect", "cross-slot", "held-reopen", "entry-mode-hover", "hard-entry-boundaries", "full-neutral", "dice-roundtrip", "entry-readiness", "entry-disconnect", "pure-gesture" });
        int total = 0;
        foreach (string scenario in cases)
        {
            string host = Environment.ProcessPath;
            var start = new ProcessStartInfo(host) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            if (Path.GetFileNameWithoutExtension(host).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            start.ArgumentList.Add(scenario);
            using Process child = Process.Start(start);
            string output = child.StandardOutput.ReadToEnd(), error = child.StandardError.ReadToEnd();
            child.WaitForExit();
            if (child.ExitCode != 0) throw new InvalidOperationException(scenario + "\n" + output + error);
            Console.Write(output);
            string marker = ": " + scenario + ": ";
            int index = output.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0) throw new InvalidOperationException("missing scenario assertion receipt: " + scenario);
            string count = output.Substring(index + marker.Length).Split(' ')[0]; total += int.Parse(count);
        }
        Console.WriteLine("PASS: LogOpenerIntegrationTests: " + total + " assertions across " + cases.Count
            + " isolated scenarios; linked actual native hook/filter/bridge/controller/action-gate sources with only Win32/original-export/Unity boundaries simulated (not Windows/game runtime validation).");
    }

    private static string Scheme(string mode) => mode == "KeyboardMouse" ? "Keyboard&Mouse" : mode == "Unknown" ? "unrecognized" : "Gamepad";
    private static void FirstNavigation(Boundary b, int owner, string mode)
    {
        b.Neutral(owner);
        Check(!b.Controller.Waiting, "full release/neutral must finish the opening barrier");
        b.State(owner, PadButtons.A); b.GamePollAll();
        LogPadFrame first = b.Tick();
        Check(first.Command == LogPadCommand.Activate, "first fresh A after a pad opening must work without a second transition press in " + mode);
        Check(b.Tick().Command == LogPadCommand.None, "held first A cannot repeat");
        Check(Mortal.Core.FakePlayerInput.Current.currentControlScheme == Scheme(mode), "pad entry must not force the game's control scheme");
        Check(b.GameActivations == 0, "no pending/open callback was delivered as a gameplay activation");
    }

    private static void Run(string scenario)
    {
        if (scenario.StartsWith("open/", StringComparison.Ordinal))
        {
            string[] pieces = scenario.Split('/'); string mode = pieces[2]; int owner = int.Parse(pieces[3]);
            var b = new Boundary(owner); b.Connected[0] = true;
            Mortal.Core.FakePlayerInput.Current.SetScheme(Scheme(mode)); b.Arm();
            b.OpenGesture(owner, pieces[1] == "bridge");
            int requested = b.Poll(); Check(requested == owner && b.OpenRequests == 1, "opening request retains the gesture's physical slot");
            b.Captured("consume-to-open handoff");
            b.Accept(requested, Scheme(mode));
            for (int i = 0; i < 3; i++) Check(b.Poll() == -1 && b.Tick().Command == LogPadCommand.None, "held opening buttons cannot open twice or navigate");
            if (owner != 0)
            {
                b.State(0, PadButtons.A); b.GamePollAll();
                Check(b.Tick().Command == LogPadCommand.None && b.Controller.Owner == owner, "another connected pad cannot steal the opening owner");
                b.State(0);
            }
            FirstNavigation(b, owner, mode);
            return;
        }
        if (scenario == "pure-gesture") { PureGestures(); return; }
        var boundary = new Boundary();
        Mortal.Core.FakePlayerInput.Current.SetScheme("Keyboard&Mouse");
        if (scenario == "raw-only")
        {
            boundary.Arm(); boundary.State(0, Chord);
            for (int i = 0; i < 5; i++) { boundary.RawPollAll(); Check(boundary.Poll() == -1 && !GamepadBridge.LogOpening, "ReadRaw must never invent an opener before a real game poll"); }
            boundary.OpenGesture(0); Check(boundary.Poll() == 0, "real callback still sees a fresh gesture after duplicate UI raw polls");
            return;
        }
        if (scenario == "no-polling")
        {
            boundary.State(0, Chord); boundary.RawPollAll();
            Check(boundary.Poll() == -1 && !boundary.Native.PollingFresh, "raw export availability is not proof of a captured game input path");
            boundary.GamePoll(0); Check(boundary.Poll() == -1, "first observed held chord cannot open without observed neutral");
            boundary.State(0); boundary.GamePoll(0); boundary.Poll();
            boundary.OpenGesture(0); Check(boundary.Poll() == 0, "a later real neutral and fresh chord recover safely");
            return;
        }
        if (scenario == "cross-slot")
        {
            boundary.Connected[2] = true; boundary.Arm();
            boundary.State(0, LeftThumb); boundary.State(2, RightThumb); boundary.GamePollAll();
            Check(boundary.Poll() == -1 && !GamepadBridge.LogOpening, "thumb presses from different devices must never form a chord");
            boundary.State(0); boundary.State(2); boundary.GamePollAll(); boundary.Poll();
            boundary.OpenGesture(2); Check(boundary.Poll() == 2, "one device's complete chord preserves its actual slot");
            return;
        }
        boundary.Arm(); boundary.OpenGesture(0);
        if (scenario == "pending-stale")
        {
            typeof(NativeXInput).GetField("lastPoll", PrivateInstance).SetValue(boundary.Native, Stopwatch.GetTimestamp() - Stopwatch.Frequency * 2);
            Check(boundary.Poll() == -1 && !GamepadBridge.LogOpening, "a stale capture path must discard pending requests");
            boundary.GamePoll(0); Check(boundary.Poll() == -1, "polling recovery cannot revive the held old gesture");
            boundary.State(0); boundary.GamePoll(0); boundary.Poll(); boundary.OpenGesture(0);
            Check(boundary.Poll() == 0, "stale capture recovery requires real neutral and a fresh gesture"); return;
        }
        if (scenario == "cancel" || scenario == "disable" || scenario == "focus" || scenario == "disconnect")
        {
            if (scenario == "cancel") GamepadBridge.CancelLogOpener();
            if (scenario == "disable") boundary.Enabled = false;
            if (scenario == "focus") { boundary.Focused = UnityEngine.Application.isFocused = false; GamepadBridge.SuspendLogOpener(); }
            if (scenario == "disconnect") { boundary.Connected[0] = false; boundary.GamePoll(0); }
            Check(boundary.Poll() == -1 && !GamepadBridge.LogOpening, "context/config/focus/device loss invalidates the pending request");
            boundary.Enabled = boundary.Focused = UnityEngine.Application.isFocused = true; boundary.Connected[0] = true;
            boundary.GamePoll(0); Check(boundary.Poll() == -1, "recovery while held cannot reopen from an old gesture");
            boundary.State(0); boundary.GamePoll(0); boundary.Poll(); boundary.OpenGesture(0);
            Check(boundary.Poll() == 0, "recovery accepts a new gesture after full neutral"); return;
        }
        int request = boundary.Poll(); boundary.Accept(request, "Keyboard&Mouse");
        if (scenario == "held-reopen")
        {
            boundary.Open = false; GameActionGate.SetLogState(false, true); GamepadBridge.CancelLogInput();
            boundary.Poll(); boundary.Tick();
            for (int i = 0; i < 3; i++)
            {
                Check((boundary.GamePoll(0).Buttons & Chord) == 0 && boundary.Poll() == -1, "closing while opener held neither leaks it nor reopens");
                Check(boundary.Tick().Command == LogPadCommand.None, "held close cannot issue navigation to a hidden log");
            }
            boundary.State(0); boundary.GamePoll(0); boundary.Poll(); boundary.OpenGesture(0);
            Check(boundary.Poll() == 0, "release followed by a new chord can open again"); return;
        }
        if (scenario == "entry-mode-hover")
        {
            boundary.Neutral(0);
            foreach (string scheme in new[] { "Gamepad", "unrecognized", "Keyboard&Mouse" })
            {
                Mortal.Core.FakePlayerInput.Current.SetScheme(scheme); GamepadBridge.NotifyPointerMotion();
                Check(boundary.Tick().Command == LogPadCommand.None && !boundary.Controller.Waiting, "automatic scheme changes and cursor hover must not restart pad entry's completed neutral boundary");
            }
            FirstNavigation(boundary, 0, "KeyboardMouse"); return;
        }
        if (scenario == "full-neutral")
        {
            foreach (PadState held in new[] {
                new PadState { Buttons = PadButtons.A }, new PadState { Buttons = PadButtons.B },
                new PadState { LeftX = 8001 }, new PadState { RightY = -8001 },
                new PadState { LeftTrigger = 31 }, new PadState { RightTrigger = 31 }
            })
            {
                boundary.Physical[0] = held; boundary.GamePollAll();
                Check(boundary.Tick().Command == LogPadCommand.None && boundary.Controller.Waiting,
                    "opening stays blocked until every button, both sticks and both triggers are neutral");
                boundary.Captured("partial release");
            }
            FirstNavigation(boundary, 0, "KeyboardMouse"); return;
        }
        if (scenario == "dice-roundtrip")
        {
            boundary.State(0, (ushort)(Chord | PadButtons.LB | PadButtons.Right)); boundary.GamePollAll();
            Check(boundary.Tick().Command == LogPadCommand.None, "held opening gesture plus dice controls cannot navigate the new log");
            Check(GamepadBridge.Tick(true, true, 17, boundary.Time, "unused-no-Win32-initialization") == 17,
                "actual dice bridge cannot edit the override while the log owns input");
            boundary.Open = false; GameActionGate.SetLogState(false, true); GamepadBridge.CancelLogInput(); boundary.Poll(); boundary.Tick();
            Check(GamepadBridge.Tick(true, true, 17, boundary.Time, "unused-no-Win32-initialization") == 17
                && PadInputFilter.IsNeutral(boundary.GamePoll(0)),
                "closing with LB/direction/opener held cannot change dice or leak game input");
            boundary.State(0); boundary.GamePollAll(); boundary.Tick();
            Check(GamepadBridge.Tick(true, true, 17, boundary.Time, "unused-no-Win32-initialization") == 17,
                "full release rearms dice without an adjustment");
            boundary.State(0, (ushort)(PadButtons.LB | PadButtons.Right)); boundary.GamePollAll();
            Check(GamepadBridge.Tick(true, true, 17, boundary.Time + .1f, "unused-no-Win32-initialization") == 18,
                "a separate fresh dice gesture works after the log release barrier"); return;
        }
        if (scenario == "entry-readiness")
        {
            boundary.Neutral(0); boundary.State(0, PadButtons.A);
            typeof(NativeXInput).GetField("lastPoll", PrivateInstance).SetValue(boundary.Native, Stopwatch.GetTimestamp() - Stopwatch.Frequency * 2);
            Check(boundary.Tick().Command == LogPadCommand.None && boundary.Controller.Waiting,
                "capture freshness loss cancels pad entry before held A can activate");
            boundary.GamePollAll(); Check(boundary.Tick().Command == LogPadCommand.None,
                "capture recovery consumes the old held action");
            Mortal.Core.FakePlayerInput.Current.SetScheme("Gamepad"); boundary.Neutral(0);
            boundary.State(0, PadButtons.A); boundary.GamePollAll();
            Check(boundary.Tick().Command == LogPadCommand.Activate, "fresh input returns after capture recovery and full neutral"); return;
        }
        if (scenario == "entry-disconnect")
        {
            boundary.Neutral(0); boundary.State(0, PadButtons.A); boundary.Connected[0] = false; boundary.Connected[2] = true;
            boundary.GamePoll(0); boundary.GamePoll(2);
            Check(boundary.Tick().Command == LogPadCommand.None && boundary.Controller.Owner == 0,
                "owner disconnect cancels the entry without assigning another connected pad");
            boundary.Connected[0] = true; boundary.GamePollAll();
            Check(boundary.Tick().Command == LogPadCommand.None && boundary.Controller.Owner == 0,
                "reconnecting owner with held A cannot activate or transfer ownership");
            Mortal.Core.FakePlayerInput.Current.SetScheme("Gamepad"); boundary.Neutral(0);
            boundary.State(0, PadButtons.A); boundary.GamePollAll();
            Check(boundary.Tick().Command == LogPadCommand.Activate, "reconnected owner needs full neutral and a new action"); return;
        }
        if (scenario == "hard-entry-boundaries")
        {
            boundary.Neutral(0); boundary.State(0, PadButtons.A);
            boundary.Focused = UnityEngine.Application.isFocused = false;
            Check(boundary.Tick().Command == LogPadCommand.None, "focus loss cancels an entry rather than accepting held A");
            boundary.Focused = UnityEngine.Application.isFocused = true;
            Check(boundary.Tick().Command == LogPadCommand.None, "focus return cannot revive held A");
            boundary.Neutral(0); Mortal.Core.FakePlayerInput.Current.SetScheme("Gamepad"); boundary.Tick();
            boundary.State(0, PadButtons.A); Check(boundary.Tick().Command == LogPadCommand.Activate, "fresh input works after focus recovery and neutral");
            GamepadBridge.NotifyPointerActivity(); Check(boundary.Tick().Command == LogPadCommand.None, "explicit pointer takeover consumes simultaneous held A");
            boundary.Neutral(0); boundary.State(0, PadButtons.A); GamepadBridge.NotifyKeyboardActivity();
            Check(boundary.Tick().Command == LogPadCommand.None, "explicit keyboard takeover also cancels a simultaneous pad action");
            boundary.Neutral(0); boundary.State(0, PadButtons.A); GamepadBridge.CancelLogInput();
            Check(boundary.Tick().Command == LogPadCommand.None, "explicit context cancellation cannot be softened by an old pad opening");
            return;
        }
        throw new InvalidOperationException("unknown scenario " + scenario);
    }

    private static void PureGestures()
    {
        void Configure(PadInputFilter filter) => filter.ConfigureLogOpener(true, true, false);
        PadState State(ushort buttons = 0, short lx = 0, short ry = 0, byte lt = 0) => new PadState { Buttons = buttons, LeftX = lx, RightY = ry, LeftTrigger = lt };
        foreach (double separation in new[] { .0, .299, .301 })
        {
            var filter = new PadInputFilter(); Configure(filter);
            filter.FilterGame(0, State(), false, 1);
            Check((filter.FilterGame(0, State(LeftThumb), false, 2).Buttons & LeftThumb) == 0, "reserved single thumb is hidden from game polling");
            filter.FilterGame(0, State(Chord), false, 2 + separation);
            Check(filter.TryConsumeLogOpen(out int owner) == (separation < .3) && (owner == 0 || owner == -1), "chord overlap window uses elapsed time rather than poll count");
        }
        foreach (PadState invalid in new[] { State((ushort)(Chord | PadButtons.A)), State(Chord, lx:8001), State(Chord, ry:-8001), State(Chord, lt:31) })
        {
            var filter = new PadInputFilter(); Configure(filter); filter.FilterGame(0, State(), false, 0);
            filter.FilterGame(0, invalid, false, 1);
            Check(!filter.TryConsumeLogOpen(out _), "an extra button, off-center stick or trigger cancels the opener instead of sharing gameplay input");
            filter.FilterGame(0, State(Chord), false, 1.1);
            Check(!filter.TryConsumeLogOpen(out _), "removing the extra channel without full neutral cannot revive a cancelled chord");
            filter.FilterGame(0, State(), false, 1.2); filter.FilterGame(0, State(Chord), false, 1.3);
            Check(filter.TryConsumeLogOpen(out int owner) && owner == 0, "full neutral and a new chord recover after an invalid gesture");
        }
        var duplicate = new PadInputFilter(); Configure(duplicate); duplicate.FilterGame(0, State(), false, 0);
        for (int i = 0; i < 10; i++) duplicate.FilterGame(0, State(Chord), false, 1 + i * .001);
        Check(duplicate.TryConsumeLogOpen(out int duplicateOwner) && duplicateOwner == 0 && !duplicate.TryConsumeLogOpen(out _), "repeated native callbacks and unchanged held packets produce exactly one request");
    }
}

namespace DiceMaster.Config
{
    public sealed class Setting<T> { public T Value; public Setting(T value) { Value = value; } }
    public static class ConfigManager
    {
        public static Setting<UnityEngine.KeyCode> LogMenuToggleKey = new Setting<UnityEngine.KeyCode>(UnityEngine.KeyCode.Tab);
    }
}
namespace UnityEngine
{
    public enum KeyCode { Tab }
    public static class Application { public static bool isFocused = true; }
    public static class Input
    {
        public static bool anyKey; public static int touchCount; public static bool Toggle;
        public static bool GetKeyDown(KeyCode key) => Toggle;
    }
    public static class Object { public static Array FindObjectsOfType(Type type) => new[] { Mortal.Core.PlayerInputController.Current }; }
    public static class Random { public static int Calls; public static int Range(int min, int max) { Calls++; throw new InvalidOperationException("game RNG used"); } }
}
namespace Mortal.Core
{
    public sealed class FakePlayerInput
    {
        public static readonly FakePlayerInput Current = new FakePlayerInput();
        private string scheme = "Gamepad";
        public string currentControlScheme => scheme;
        public bool isActiveAndEnabled => true;
        public void SetScheme(string value) { scheme = value; }
    }
    public sealed class PlayerInputController
    {
        private readonly FakePlayerInput _playerInput = FakePlayerInput.Current;
        public bool isActiveAndEnabled => true;
        public static readonly PlayerInputController Current = new PlayerInputController();
    }
}
namespace HarmonyLib
{
    public sealed class HarmonyMethod
    {
        public readonly MethodInfo Method;
        public HarmonyMethod(Type type, string name) { Method = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic); }
    }
    public sealed class Harmony
    {
        public static MethodInfo Prefix;
        public void Patch(MethodBase original, HarmonyMethod prefix) { Prefix = prefix.Method; }
    }
    public static class AccessTools
    {
        public static Type TypeByName(string name) => typeof(AccessTools).Assembly.GetType(name);
        public static MethodInfo Method(Type type, string name) => type.GetMethod(name);
    }
}
namespace UnityEngine.InputSystem
{
    public enum InputActionPhase { Disabled, Waiting, Started, Performed, Canceled }
    public sealed class InputActionMap { }
    public struct CallbackContext { }
    public sealed class InputActionState
    {
        public void CallActionListeners(int index, InputActionMap map, InputActionPhase phase, ref CallbackContext context, string name) { }
    }
}
