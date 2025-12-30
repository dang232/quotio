using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quotio.Core.Interfaces;
using Quotio.Services.Agents;
using Quotio.Services.Proxy;
using Quotio.Services.QuotaFetchers;
using Quotio.Services.System;
using Quotio.App.ViewModels;
using Quotio.App.ViewModels.Screens;

namespace Quotio.App;

public partial class App : Application
{
    private IHost? _host;

    public static new App Current => (App)Application.Current;
    public IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "quotio_error.log");
        
        // Handle background thread exceptions
        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            File.WriteAllText(logPath, $"AppDomain Exception: {(ex.ExceptionObject as Exception)?.Message}\n\n{(ex.ExceptionObject as Exception)?.StackTrace}");
        };

        TaskScheduler.UnobservedTaskException += (s, ex) =>
        {
            File.WriteAllText(logPath, $"Task Exception: {ex.Exception.Message}\n\n{ex.Exception.StackTrace}");
            ex.SetObserved();
        };
        
        this.DispatcherUnhandledException += (s, ex) =>
        {
            File.WriteAllText(logPath, $"Dispatcher Exception: {ex.Exception.Message}\n\n{ex.Exception.StackTrace}");
            ex.Handled = true;
            Shutdown(1);
        };

        try
        {
            var builder = Host.CreateApplicationBuilder();

            // Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddDebug();

            // HTTP Client
            builder.Services.AddHttpClient();

            // Core Services
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddSingleton<CLIProxyManager>();
            builder.Services.AddSingleton<ManagementApiClient>();
            builder.Services.AddSingleton<ShellProfileManager>();
            
            // Agent Services
            builder.Services.AddSingleton<IAgentDetectionService, AgentDetectionService>();
            builder.Services.AddSingleton<IAgentConfigurationService, AgentConfigurationService>();
            builder.Services.AddSingleton<INotificationService, WindowsNotificationService>();
            builder.Services.AddSingleton<StartupManager>();
            builder.Services.AddSingleton<UpdateCheckerService>();

            // Quota Fetchers
            RegisterFetcher<AntigravityQuotaFetcher>(builder.Services);
            RegisterFetcher<ClaudeCodeQuotaFetcher>(builder.Services);
            RegisterFetcher<CopilotQuotaFetcher>(builder.Services);
            RegisterFetcher<CodexCLIQuotaFetcher>(builder.Services);
            RegisterFetcher<GeminiCLIQuotaFetcher>(builder.Services);
            RegisterFetcher<CursorQuotaFetcher>(builder.Services);
            RegisterFetcher<OpenAIQuotaFetcher>(builder.Services);

            // ViewModels
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<QuotaViewModel>();
            builder.Services.AddTransient<ProvidersViewModel>();
            builder.Services.AddTransient<AgentsViewModel>();
            builder.Services.AddTransient<ApiKeysViewModel>();
            builder.Services.AddTransient<LogsViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<AboutViewModel>();
            
            // Windows
            builder.Services.AddSingleton<MainWindow>();

            _host = builder.Build();
            Services = _host.Services;

            await _host.StartAsync();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            File.WriteAllText(logPath, $"Startup Error: {ex.Message}\n\nInner: {ex.InnerException?.Message}\n\n{ex.StackTrace}");
            Shutdown(1);
        }
    }

    private static void RegisterFetcher<T>(IServiceCollection services) where T : class, IQuotaFetcher
    {
        services.AddHttpClient<T>();
        services.AddTransient<IQuotaFetcher>(sp => sp.GetRequiredService<T>());
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
