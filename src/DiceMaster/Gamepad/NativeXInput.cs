using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace DiceMaster.Gamepad
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate uint GetState(uint user, out PadState state);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int HookInitialize();
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int HookCreate(IntPtr target, IntPtr callback, out IntPtr original);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate int HookTarget(IntPtr target);

    internal sealed class XInputHook
    {
        internal NativeXInput Owner;
        internal IntPtr Target;
        internal GetState Original, Callback;
        internal uint Invoke(uint user, out PadState state)
        {
            NativeXInput.CallbackDepth++;
            try
            {
                uint error = Original(user, out state);
                if (error == 0 && NativeXInput.RawDepth == 0 && NativeXInput.CallbackDepth == 1)
                {
                    Owner.ObserveGamePoll();
                    state = Owner.Filter.FilterGame(user, state, Owner.WindowActive);
                }
                else if (error != 0 && NativeXInput.RawDepth == 0 && NativeXInput.CallbackDepth == 1)
                    Owner.Filter.Disconnected(user);
                return error;
            }
            catch
            {
                // Exceptions must never cross a reverse P/Invoke boundary.
                state = default(PadState);
                return 1167; // ERROR_DEVICE_NOT_CONNECTED
            }
            finally { NativeXInput.CallbackDepth--; }
        }
    }

    public sealed class NativeXInput : IDisposable
    {
        [ThreadStatic] internal static int RawDepth;
        [ThreadStatic] internal static int CallbackDepth;
        public volatile bool WindowActive;
        private long lastPoll;
        // A path observed in a previous scene is insufficient. Navigation stops
        // after one second without a real game caller reaching the hook.
        public bool GamePollingObserved { get { return PollingFresh; } }
        public bool PollingFresh
        {
            get
            {
                long last = Interlocked.Read(ref lastPoll);
                return last != 0 && Stopwatch.GetTimestamp() - last < Stopwatch.Frequency;
            }
        }
        internal void ObserveGamePoll()
        {
            long now = Stopwatch.GetTimestamp();
            long previous = Interlocked.Exchange(ref lastPoll, now);
            // A held gesture may span a scene/device/polling outage. Its first
            // resumed callback must establish neutral before a fresh chord.
            if (previous == 0 || now - previous >= Stopwatch.Frequency) Filter.CancelLogOpener();
        }
        public bool LogActive { set { Filter.SetLogCapture(value); } }
        public void RequireNeutral() { Filter.RequireNeutral(); }
        public bool CanUseRaw(uint user) { return Filter.CanUseRaw(user); }
        public void ConfigureLogOpener(bool enabled, bool focused, bool open)
        {
            Filter.ConfigureLogOpener(enabled && !disposed, focused, open);
            if (!PollingFresh) Filter.CancelLogOpener();
        }
        public bool TryConsumeLogOpen(out int owner)
        {
            if (disposed || !PollingFresh)
            {
                Filter.CancelLogOpener(); owner = -1; return false;
            }
            return Filter.TryConsumeLogOpen(out owner);
        }
        public bool LogOpening
        {
            get
            {
                if (disposed || !PollingFresh) { Filter.CancelLogOpener(); return false; }
                return Filter.LogOpening;
            }
        }
        public void CancelLogOpener() { Filter.CancelLogOpener(); }
        internal readonly PadInputFilter Filter = new PadInputFilter();
        private readonly XInputHook[] hooks = new XInputHook[12];
        private int count;
        private HookInitialize initialize, applyQueued;
        private HookCreate createHook;
        private HookTarget queueEnable, disableHook;
        private bool disposed;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr LoadLibraryW(string name);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string name);
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", ExactSpelling = true)]
        private static extern IntPtr GetProcAddressOrdinal(IntPtr module, IntPtr ordinal);

        private static Delegate Function(IntPtr module, string name, Type type)
        {
            IntPtr address = GetProcAddress(module, name);
            if (address == IntPtr.Zero) throw new InvalidOperationException("Missing native export: " + name);
            return Marshal.GetDelegateForFunctionPointer(address, type);
        }
        private static byte[] Resource(string name)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (stream == null) throw new InvalidOperationException("Missing embedded resource: " + name);
                using (MemoryStream memory = new MemoryStream()) { stream.CopyTo(memory); return memory.ToArray(); }
            }
        }
        private static string Hash(byte[] bytes)
        {
            using (SHA256 hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        private static string Extract(string root)
        {
            string arch = IntPtr.Size == 8 ? "x64" : "x86";
            byte[] bytes = Resource("DiceMaster.Gamepad.Native.MinHook." + arch + ".dll");
            string digest = Hash(bytes);
            string directory = Path.Combine(root, "DiceMaster", "Gamepad", digest.Substring(0, 16));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "DiceMaster.MinHook.1.3.4." + arch + ".dll");
            if (!File.Exists(path) || Hash(File.ReadAllBytes(path)) != digest)
            {
                string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllBytes(temporary, bytes);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temporary, path);
            }
            if (Hash(File.ReadAllBytes(path)) != digest) throw new InvalidOperationException("Native resource hash mismatch.");
            return path;
        }
        public void Initialize(string cacheRoot)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                throw new PlatformNotSupportedException("Windows XInput is required.");
            if (Marshal.SizeOf(typeof(PadState)) != 16) throw new InvalidOperationException("Unexpected XInput structure layout.");
            IntPtr library = LoadLibraryW(Extract(cacheRoot));
            if (library == IntPtr.Zero) throw new InvalidOperationException("Cannot load embedded MinHook: " + Marshal.GetLastWin32Error());
            initialize = (HookInitialize)Function(library, "MH_Initialize", typeof(HookInitialize));
            createHook = (HookCreate)Function(library, "MH_CreateHook", typeof(HookCreate));
            queueEnable = (HookTarget)Function(library, "MH_QueueEnableHook", typeof(HookTarget));
            applyQueued = (HookInitialize)Function(library, "MH_ApplyQueued", typeof(HookInitialize));
            disableHook = (HookTarget)Function(library, "MH_DisableHook", typeof(HookTarget));
            int status = initialize();
            if (status != 0 && status != 1) throw new InvalidOperationException("MH_Initialize: " + status);
            try
            {
                foreach (string name in new[] { "xinput1_4.dll", "xinput1_3.dll", "xinput9_1_0.dll", "xinputuap.dll" })
                {
                    IntPtr module = LoadLibraryW(name);
                    if (module == IntPtr.Zero) continue;
                    Add(GetProcAddress(module, "XInputGetState"));
                    // Some games use XInputGetStateEx (ordinal 100), with the
                    // same state ABI. Deduplicate forwarded exports by address.
                    if (name == "xinput1_4.dll" || name == "xinput1_3.dll")
                        Add(GetProcAddressOrdinal(module, new IntPtr(100)));
                }
                if (count == 0) throw new InvalidOperationException("No filterable XInput state entry point was found.");
                for (int i = 0; i < count; i++)
                {
                    status = queueEnable(hooks[i].Target);
                    if (status != 0) throw new InvalidOperationException("MH_QueueEnableHook: " + status);
                }
                status = applyQueued();
                if (status != 0) throw new InvalidOperationException("MH_ApplyQueued: " + status);
            }
            catch { Dispose(); throw; }
        }
        private void Add(IntPtr target)
        {
            if (target == IntPtr.Zero) return;
            for (int i = 0; i < count; i++) if (hooks[i].Target == target) return;
            if (count == hooks.Length) throw new InvalidOperationException("Too many XInput entry points.");
            XInputHook hook = new XInputHook(); hook.Owner = this; hook.Target = target; hook.Callback = hook.Invoke;
            IntPtr original;
            int status = createHook(target, Marshal.GetFunctionPointerForDelegate(hook.Callback), out original);
            if (status != 0) return; // Another usable entry point may exist.
            hook.Original = (GetState)Marshal.GetDelegateForFunctionPointer(original, typeof(GetState));
            hooks[count++] = hook; // Root both delegates before hooks are enabled.
        }
        public bool ReadRaw(uint user, out PadState state)
        {
            state = default(PadState);
            if (disposed) return false;
            RawDepth++;
            try
            {
                // The XInput versions address the same four physical slots.
                // Do not repeatedly poll disconnected slots through every DLL.
                bool connected = count != 0 && hooks[0].Original(user, out state) == 0;
                if (connected) Filter.ObserveRaw(user, state);
                else Filter.Disconnected(user);
                return connected;
            }
            finally { RawDepth--; }
        }
        public void Dispose()
        {
            WindowActive = false;
            if (disposed) return;
            disposed = true;
            for (int i = 0; i < count; i++)
            {
                if (disableHook != null) disableHook(hooks[i].Target);
            }
            Filter.Reset();
            // Keep trampolines, modules and delegates rooted until process exit.
            // An already-entered callback may still call its original trampoline
            // after DisableHook returns; freeing it here would race that caller.
        }
    }
}
