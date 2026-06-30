using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MindedOS.Engine;

/// <summary>
/// Executes computer actions against the real system via Win32. Gated by
/// <see cref="SafeMode"/> (ON by default) — when safe, actions are logged
/// instead of executed so nothing fires accidentally during testing.
/// </summary>
public sealed class ActionExecutor
{
    private readonly ActionRegistry _registry;

    public ActionExecutor(ActionRegistry registry) => _registry = registry;

    /// <summary>When true (default), actions are logged but not executed.</summary>
    public bool SafeMode { get; set; } = true;

    /// <summary>When set, actions are suppressed unless this returns true (skin
    /// contact present). Null = always allowed (tests, headless).</summary>
    public Func<bool>? ContactGate { get; set; }

    /// <summary>Raised for every action attempt: (message, executed?).</summary>
    public event Action<string, bool>? Logged;

    public void Run(string actionId)
    {
        var action = _registry.Find(actionId);
        if (action is null)
        {
            Logged?.Invoke($"Unknown action '{actionId}'", false);
            return;
        }
        Run(action);
    }

    public void Run(ComputerAction action)
    {
        if (SafeMode)
        {
            Logged?.Invoke($"[SAFE] would run {action.Id} — {action.Name} ({action.Kind}:{action.Payload})", false);
            return;
        }

        if (ContactGate is not null && !ContactGate())
        {
            Logged?.Invoke($"[NO CONTACT] suppressed {action.Id} — headset off skin", false);
            return;
        }

        try
        {
            Dispatch(action);
            Logged?.Invoke($"[RUN] {action.Id} — {action.Name}", true);
        }
        catch (Exception ex)
        {
            Logged?.Invoke($"[FAIL] {action.Id} — {ex.Message}", false);
        }
    }

    private void Dispatch(ComputerAction a)
    {
        switch (a.Kind.ToLowerInvariant())
        {
            case "combo": SendCombo(a.Payload); break;
            case "key": SendKeySpec(a.Payload); break;
            case "text": TypeText(Unescape(a.Payload)); break;
            case "run": RunProcess(a.Payload); break;
            case "url": OpenShell(a.Payload); break;
            case "system": RunSystem(a.Payload); break;
            case "mouse": RunMouse(a.Payload); break;
            default: throw new NotSupportedException($"kind '{a.Kind}'");
        }
    }

    // ---- keyboard ---------------------------------------------------------

    private static void SendKeySpec(string payload)
    {
        // payload may be "KEY" or "KEY*N" to repeat.
        int repeat = 1;
        var key = payload;
        int star = payload.IndexOf('*');
        if (star > 0 && int.TryParse(payload.AsSpan(star + 1), out int n))
        {
            key = payload[..star];
            repeat = Math.Clamp(n, 1, 50);
        }
        if (!VirtualKeys.TryResolve(key, out byte vk)) return;
        for (int i = 0; i < repeat; i++) { TapKey(vk); Thread.Sleep(15); }
    }

    private static void SendCombo(string combo)
    {
        var parts = combo.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var vks = new List<byte>();
        foreach (var p in parts)
            if (VirtualKeys.TryResolve(p, out byte vk)) vks.Add(vk);
        if (vks.Count == 0) return;

        foreach (var vk in vks) KeyDown(vk);
        for (int i = vks.Count - 1; i >= 0; i--) KeyUp(vks[i]);
    }

    private static void TapKey(byte vk) { KeyDown(vk); KeyUp(vk); }
    private static void KeyDown(byte vk) => keybd_event(vk, 0, 0, UIntPtr.Zero);
    private static void KeyUp(byte vk) => keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

    private static void TypeText(string text)
    {
        foreach (char ch in text)
        {
            if (ch == '\n') { TapKey(0x0D); continue; }
            if (ch == '\t') { TapKey(0x09); continue; }
            SendUnicode(ch);
        }
    }

    private static void SendUnicode(char ch)
    {
        var inputs = new INPUT[2];
        inputs[0] = UnicodeInput(ch, down: true);
        inputs[1] = UnicodeInput(ch, down: false);
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT UnicodeInput(char ch, bool down) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = 0,
                wScan = ch,
                dwFlags = KEYEVENTF_UNICODE | (down ? 0u : KEYEVENTF_KEYUP),
                time = 0,
                dwExtraInfo = UIntPtr.Zero,
            }
        }
    };

    // ---- processes / shell ------------------------------------------------

    private static void RunProcess(string payload)
    {
        // payload "exe" or "exe|args"
        var bar = payload.IndexOf('|');
        string file = bar >= 0 ? payload[..bar] : payload;
        string args = bar >= 0 ? payload[(bar + 1)..] : "";
        Process.Start(new ProcessStartInfo
        {
            FileName = file.Trim(),
            Arguments = args,
            UseShellExecute = true,
        });
    }

    private static void OpenShell(string target) =>
        Process.Start(new ProcessStartInfo { FileName = target.Trim(), UseShellExecute = true });

    // ---- system ops -------------------------------------------------------

    private static void RunSystem(string op)
    {
        switch (op.Trim().ToLowerInvariant())
        {
            case "lock": LockWorkStation(); break;
            case "sleep": SetSuspendState(false, false, false); break;
            case "hibernate": SetSuspendState(true, false, false); break;
            case "displayoff":
                SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, 2); break;
            case "screenshot": TapKey(0x2C); break; // PrintScreen
            case "minimizeall": SendCombo("WIN+M"); break;
            case "showdesktop": SendCombo("WIN+D"); break;
            case "emptyrecycle": SHEmptyRecycleBin(IntPtr.Zero, null, 0); break;
            case "signout": ExitWindowsEx(EWX_LOGOFF, 0); break;
            case "restart": Process.Start("shutdown", "/r /t 0"); break;
            case "shutdown": Process.Start("shutdown", "/s /t 0"); break;
            default: throw new NotSupportedException($"system op '{op}'");
        }
    }

    private static void RunMouse(string op)
    {
        int repeat = 1;
        var name = op;
        int star = op.IndexOf('*');
        if (star > 0 && int.TryParse(op.AsSpan(star + 1), out int n)) { name = op[..star]; repeat = Math.Clamp(n, 1, 30); }

        for (int i = 0; i < repeat; i++)
        {
            switch (name.Trim().ToLowerInvariant())
            {
                case "lclick": mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                               mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero); break;
                case "rclick": mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                               mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero); break;
                case "mclick": mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
                               mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero); break;
                case "dblclick":
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero); mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero); mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero); break;
                case "scrollup": mouse_event(MOUSEEVENTF_WHEEL, 0, 0, 120, UIntPtr.Zero); break;
                case "scrolldown": mouse_event(MOUSEEVENTF_WHEEL, 0, 0, unchecked((uint)-120), UIntPtr.Zero); break;
                case "scrollleft": mouse_event(MOUSEEVENTF_HWHEEL, 0, 0, unchecked((uint)-120), UIntPtr.Zero); break;
                case "scrollright": mouse_event(MOUSEEVENTF_HWHEEL, 0, 0, 120, UIntPtr.Zero); break;
                default: throw new NotSupportedException($"mouse op '{op}'");
            }
            Thread.Sleep(20);
        }
    }

    private static string Unescape(string s) => s.Replace("\\n", "\n").Replace("\\t", "\t");

    // ---- P/Invoke ---------------------------------------------------------

    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint INPUT_KEYBOARD = 1;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002, MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008, MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020, MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_WHEEL = 0x0800, MOUSEEVENTF_HWHEEL = 0x01000;
    private const int EWX_LOGOFF = 0;
    private static readonly IntPtr HWND_BROADCAST = new(0xFFFF);
    private const uint WM_SYSCOMMAND = 0x0112;
    private static readonly IntPtr SC_MONITORPOWER = new(0xF170);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")] private static extern bool LockWorkStation();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);
    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
    [DllImport("shell32.dll")] private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? rootPath, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion U; }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo;
    }
}
