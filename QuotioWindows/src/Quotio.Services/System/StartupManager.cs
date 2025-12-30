using Microsoft.Win32;
using System.Reflection;

namespace Quotio.Services.System;

public class StartupManager
{
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Quotio";

    public void SetAutoStart(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
        if (key == null) return;

        if (enable)
        {
            var exePath = Environment.ProcessPath;
            // Add --minimized argument if needed, managed by Settings usually
            key.SetValue(AppName, $"\"{exePath}\" --minimized"); 
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }

    public bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
        return key?.GetValue(AppName) != null;
    }
}
