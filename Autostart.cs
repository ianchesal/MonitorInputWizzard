using Microsoft.Win32;

namespace MonitorInputWizzard;

/// <summary>"Start with Windows" via the per-user Run key. Launches with --tray so it starts hidden.</summary>
public static class Autostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MonitorInputWizzard";

    private static string Command => $"\"{Environment.ProcessPath}\" --tray";

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string;
        }
    }

    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled) key.SetValue(ValueName, Command);
        else key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    /// <summary>If autostart is on, re-point it at this exe in case the app was moved.</summary>
    public static void RefreshPath()
    {
        if (IsEnabled) Set(true);
    }
}
