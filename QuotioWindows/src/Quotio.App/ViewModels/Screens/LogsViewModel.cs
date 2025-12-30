using CommunityToolkit.Mvvm.ComponentModel;

namespace Quotio.App.ViewModels.Screens;

public partial class LogsViewModel : ObservableObject
{
    // Placeholder for logs
    public string GlobalLogs { get; } = "Application logs will appear here...\n[INFO] Application Started\n[INFO] Proxy Service Initialized";

    public LogsViewModel()
    {
    }
}
