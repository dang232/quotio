using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quotio.Core.Models;
using Quotio.Services.System;

namespace Quotio.App.ViewModels.Screens;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly StartupManager _startupManager;

    [ObservableProperty]
    private int _proxyPort;

    [ObservableProperty]
    private bool _autoStartProxy;

    [ObservableProperty]
    private bool _minimizeToTray;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _enableLowQuotaWarnings;

    [ObservableProperty]
    private int _lowQuotaThreshold;

    public SettingsViewModel(SettingsService settingsService, StartupManager startupManager)
    {
        _settingsService = settingsService;
        _startupManager = startupManager;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var current = _settingsService.Current;
        ProxyPort = current.ProxyPort;
        AutoStartProxy = current.AutoStartProxy;
        MinimizeToTray = current.MinimizeToTray;
        StartMinimized = current.StartMinimized;
        EnableLowQuotaWarnings = current.EnableLowQuotaWarnings;
        LowQuotaThreshold = current.LowQuotaThreshold;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _settingsService.Update(s =>
        {
            s.ProxyPort = ProxyPort;
            s.AutoStartProxy = AutoStartProxy;
            s.MinimizeToTray = MinimizeToTray;
            s.StartMinimized = StartMinimized;
            s.EnableLowQuotaWarnings = EnableLowQuotaWarnings;
            s.LowQuotaThreshold = LowQuotaThreshold;
        });

        _startupManager.SetAutoStart(AutoStartProxy);
    }
}
