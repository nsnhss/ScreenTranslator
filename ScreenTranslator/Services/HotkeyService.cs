using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ScreenTranslator.Services;

/// <summary>
/// 全局快捷键（Win32 RegisterHotKey），程序最小化时也能响应。
/// </summary>
public class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 0x9001;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _source;

    /// <summary>快捷键被按下时触发（在 UI 线程）</summary>
    public event Action? HotkeyPressed;

    public void Register(Window window, string modifiers, Key key)
    {
        Unregister();

        var helper = new WindowInteropHelper(window);
        _source = HwndSource.FromHwnd(helper.EnsureHandle());
        _source.AddHook(WndProc);

        uint mod = MOD_NOREPEAT;
        foreach (var m in modifiers.Split('+', StringSplitOptions.RemoveEmptyEntries))
        {
            mod |= m.Trim().ToLowerInvariant() switch
            {
                "alt" => MOD_ALT,
                "ctrl" or "control" => MOD_CONTROL,
                "shift" => MOD_SHIFT,
                "win" => MOD_WIN,
                _ => 0u
            };
        }

        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (!RegisterHotKey(helper.Handle, HOTKEY_ID, mod, vk))
            throw new InvalidOperationException(
                $"快捷键注册失败（可能被其他程序占用）：{modifiers}+{key}");
    }

    public void Unregister()
    {
        if (_source != null)
        {
            UnregisterHotKey(_source.Handle, HOTKEY_ID);
            _source.RemoveHook(WndProc);
            _source = null;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            handled = true;
            HotkeyPressed?.Invoke();
        }
        return IntPtr.Zero;
    }

    public void Dispose() => Unregister();
}
