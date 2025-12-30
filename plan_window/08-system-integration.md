# Phase 8: System Integration

> **Duration**: 3-4 days  
> **Goal**: System tray, notifications, auto-start

---

## Tasks

- [ ] System tray with popup
- [ ] Windows toast notifications
- [ ] Auto-start on login
- [ ] Auto-update

---

## System Tray

### SystemTrayManager.cs
```csharp
namespace Quotio.Services.System;

public class SystemTrayManager : IDisposable
{
    private readonly TaskbarIcon _trayIcon;
    private readonly MainViewModel _mainViewModel;

    public SystemTrayManager(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        _trayIcon = new TaskbarIcon
        {
            Icon = LoadIcon(),
            ToolTipText = "Quotio",
            ContextMenu = CreateContextMenu()
        };
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();
    }

    private ContextMenu CreateContextMenu() => new()
    {
        Items =
        {
            new MenuItem { Header = "Open", Command = new RelayCommand(ShowMainWindow) },
            new Separator(),
            new MenuItem { Header = "Start/Stop", Command = _mainViewModel.ToggleProxyCommand },
            new Separator(),
            new MenuItem { Header = "Exit", Command = new RelayCommand(Exit) }
        }
    };

    private void ShowMainWindow()
    {
        Application.Current.MainWindow?.Show();
        Application.Current.MainWindow?.Activate();
    }

    private void Exit()
    {
        _trayIcon.Dispose();
        Application.Current.Shutdown();
    }

    public void Dispose() => _trayIcon.Dispose();
}
```

---

## Notifications

### NotificationManager.cs
```csharp
public class NotificationManager : INotificationService
{
    public void ShowQuotaWarning(AIProvider provider, string account, double pct)
    {
        new ToastContentBuilder()
            .AddText($"{provider.GetDisplayName()} Low Quota")
            .AddText($"{account}: {pct:F0}% remaining")
            .Show();
    }

    public void ShowInfo(string title, string msg) => 
        new ToastContentBuilder().AddText(title).AddText(msg).Show();
}
```

---

## Auto-Start

### AutoStartManager.cs
```csharp
public class AutoStartManager
{
    private const string Key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public void Enable()
    {
        var exe = Process.GetCurrentProcess().MainModule?.FileName;
        using var key = Registry.CurrentUser.OpenSubKey(Key, true);
        key?.SetValue("Quotio", $"\"{exe}\" --minimized");
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(Key, true);
        key?.DeleteValue("Quotio", false);
    }
}
```

---

## Next Phase

→ [09-testing-release.md](09-testing-release.md)
