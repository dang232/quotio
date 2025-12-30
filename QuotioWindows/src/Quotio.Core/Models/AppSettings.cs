using Quotio.Core.Enums;

namespace Quotio.Core.Models;

public class AppSettings
{
    public int ProxyPort { get; set; } = 8317;
    public RoutingStrategy RoutingStrategy { get; set; } = RoutingStrategy.RoundRobin;
    public AppMode AppMode { get; set; } = AppMode.Full;
    public bool AutoStartProxy { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
    public bool EnableLogging { get; set; } = true;
    public int LowQuotaThreshold { get; set; } = 20;
    public bool EnableLowQuotaWarnings { get; set; } = true;
    public string Language { get; set; } = "en";
    public string Theme { get; set; } = "Dark";
    public string? OpenAiApiKey { get; set; }
}
