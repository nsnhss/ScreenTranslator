using Microsoft.Win32;

namespace ScreenTranslator.Services;

/// <summary>
/// 开机自启：通过 HKCU\...\Run 注册表项实现（不需要管理员权限）。
/// </summary>
public static class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ScreenTranslator";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        var value = key?.GetValue(ValueName) as string;
        return value != null && value.Trim('"') == ExePath();
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
            key.SetValue(ValueName, $"\"{ExePath()}\"");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private static string ExePath() =>
        Environment.ProcessPath ??
        System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!;
}
