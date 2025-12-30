using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Quotio.Core.Models;
using Quotio.Services.Proxy;
using Quotio.Services.System;
using Quotio.Core.Interfaces;
using Quotio.App.ViewModels.Screens;
using System.Collections.ObjectModel;
using System.Windows;

namespace Quotio.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly CLIProxyManager _proxyManager;
    private readonly ManagementApiClient _apiClient;

    private readonly IServiceProvider _services;
    private readonly UpdateCheckerService _updateChecker;
    private readonly INotificationService _notifications;

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private string _proxyStatusText = "Proxy: Stopped";

    [ObservableProperty]
    private bool _isProxyRunning;

    [ObservableProperty]
    private ObservableCollection<NavigationItem> _navigationItems;

    public MainViewModel(
        CLIProxyManager proxyManager, 
        ManagementApiClient apiClient,
        IServiceProvider services,
        UpdateCheckerService updateChecker,
        INotificationService notifications)
    {
        _proxyManager = proxyManager;
        _apiClient = apiClient;
        _services = services;
        _updateChecker = updateChecker;
        _notifications = notifications;
        
        _navigationItems = new ObservableCollection<NavigationItem>
        {
            new("Dashboard", "ViewDashboard", "DashboardView"),
            new("Quota", "ChartArc", "QuotaView"),
            new("Providers", "AccountMultiple", "ProvidersView"),
            new("Agents", "Robot", "AgentsView"),
            new("API Keys", "Key", "ApiKeysView"),
            new("Logs", "ScriptText", "LogsView"),
            new("Settings", "Cog", "SettingsView"),
            new("About", "Information", "AboutView")
        };

        _proxyManager.StatusChanged += OnProxyStatusChanged;
        UpdateStatus(_proxyManager.Status);

        // Default navigation
        Navigate("DashboardView");

        // Check for updates
        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        // Get current version (mocked or assembly)
        var version = "v1.0.0"; 
        var newVersion = await _updateChecker.CheckForUpdateAsync(version);
        if (newVersion != null)
        {
            _notifications.ShowInfo("Update Available", $"Quotio version {newVersion} is available!");
        }
    }

    [RelayCommand]
    private async Task ToggleProxy()
    {
        try
        {
            if (IsProxyRunning)
            {
                await _proxyManager.StopAsync();
            }
            else
            {
                await _proxyManager.StartAsync();
            }
        }
        catch (Exception ex)
        {
            ProxyStatusText = $"Error: {ex.Message}";
            _notifications.ShowInfo("Proxy Error", $"Failed to toggle proxy: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Navigate(string viewName)
    {
        CurrentPage = viewName switch
        {
            "Dashboard" or "DashboardView" => _services.GetRequiredService<DashboardViewModel>(),
            "Quota" or "QuotaView" => _services.GetRequiredService<QuotaViewModel>(),
            "Providers" or "ProvidersView" => _services.GetRequiredService<ProvidersViewModel>(),
            "Agents" or "AgentsView" => _services.GetRequiredService<AgentsViewModel>(),
            "ApiKeys" or "ApiKeysView" => _services.GetRequiredService<ApiKeysViewModel>(),
            "Logs" or "LogsView" => _services.GetRequiredService<LogsViewModel>(),
            "Settings" or "SettingsView" => _services.GetRequiredService<SettingsViewModel>(),
            "About" or "AboutView" => _services.GetRequiredService<AboutViewModel>(),
            _ => null
        };
    }

    private void OnProxyStatusChanged(object? sender, ProxyStatus status)
    {
        UpdateStatus(status);
    }

    private void UpdateStatus(ProxyStatus status)
    {
        IsProxyRunning = status.IsRunning;
        ProxyStatusText = status.IsRunning 
            ? $"Proxy: Running (Port {status.Port})" 
            : "Proxy: Stopped";
    }

    [RelayCommand]
    private void ShowWindow()
    {
        Application.Current.MainWindow.Show();
        Application.Current.MainWindow.WindowState = System.Windows.WindowState.Normal;
        Application.Current.MainWindow.Activate();
    }

    [RelayCommand]
    private void ExitApplication()
    {
        Application.Current.Shutdown();
    }
}

public class NavigationItem
{
    public string Title { get; }
    public string Icon { get; }
    public string ViewName { get; }

    public NavigationItem(string title, string icon, string viewName)
    {
        Title = title;
        Icon = icon;
        ViewName = viewName;
    }
}
