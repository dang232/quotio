# Phase 2: Core Models

> **Duration**: 1-2 days  
> **Goal**: Create all data models, enums, and interfaces

---

## Tasks

- [ ] Create enums for providers, agents, modes
- [ ] Create data models (records)
- [ ] Create service interfaces
- [ ] Create constants

---

## Step 1: Enums

### AIProvider.cs
```csharp
namespace Quotio.Core.Enums;

public enum AIProvider
{
    Gemini,
    Claude,
    Codex,
    Qwen,
    VertexAI,
    IFlow,
    Antigravity,
    Kiro,
    Copilot,
    Cursor
}

public static class AIProviderExtensions
{
    public static string GetDisplayName(this AIProvider provider) => provider switch
    {
        AIProvider.Gemini => "Google Gemini",
        AIProvider.Claude => "Anthropic Claude",
        AIProvider.Codex => "OpenAI Codex",
        AIProvider.Qwen => "Qwen Code",
        AIProvider.VertexAI => "Vertex AI",
        AIProvider.IFlow => "iFlow",
        AIProvider.Antigravity => "Antigravity",
        AIProvider.Kiro => "Kiro",
        AIProvider.Copilot => "GitHub Copilot",
        AIProvider.Cursor => "Cursor",
        _ => provider.ToString()
    };

    public static bool SupportsOAuth(this AIProvider provider) => provider switch
    {
        AIProvider.Gemini or AIProvider.Claude or AIProvider.Codex 
            or AIProvider.Qwen or AIProvider.IFlow or AIProvider.Antigravity => true,
        _ => false
    };

    public static bool SupportsQuotaTracking(this AIProvider provider) => provider switch
    {
        AIProvider.Gemini or AIProvider.Claude or AIProvider.Codex 
            or AIProvider.Antigravity or AIProvider.Copilot or AIProvider.Cursor => true,
        _ => false
    };
}
```

### AgentType.cs
```csharp
namespace Quotio.Core.Enums;

public enum AgentType
{
    ClaudeCode,
    CodexCLI,
    GeminiCLI,
    AmpCLI,
    OpenCode,
    FactoryDroid
}

public static class AgentTypeExtensions
{
    public static string GetBinaryName(this AgentType agent) => agent switch
    {
        AgentType.ClaudeCode => "claude",
        AgentType.CodexCLI => "codex",
        AgentType.GeminiCLI => "gemini",
        AgentType.AmpCLI => "amp",
        AgentType.OpenCode => "opencode",
        AgentType.FactoryDroid => "droid",
        _ => agent.ToString().ToLower()
    };

    public static string GetConfigPath(this AgentType agent) => agent switch
    {
        AgentType.ClaudeCode => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
            ".claude", "settings.json"),
        AgentType.CodexCLI => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
            ".codex", "config.toml"),
        AgentType.OpenCode => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "opencode", "opencode.json"),
        AgentType.FactoryDroid => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
            ".factory", "config.json"),
        _ => ""
    };
}
```

### RoutingStrategy.cs
```csharp
namespace Quotio.Core.Enums;

public enum RoutingStrategy
{
    RoundRobin,
    FillFirst
}
```

### AppMode.cs
```csharp
namespace Quotio.Core.Enums;

public enum AppMode
{
    Full,       // Full proxy mode
    QuotaOnly   // Just quota tracking
}
```

---

## Step 2: Data Models

### ModelQuota.cs
```csharp
namespace Quotio.Core.Models;

public record ModelQuota(
    string Name,
    double Percentage,
    string ResetTime,
    int? Used = null,
    int? Limit = null,
    int? Remaining = null
)
{
    public double UsedPercentage => 100 - Percentage;
    
    public string FormattedPercentage => Percentage switch
    {
        < 0 => "—",
        var p when p == Math.Floor(p) => $"{p:F0}%",
        _ => $"{Percentage:F2}%"
    };

    public string? FormattedUsage => Used switch
    {
        null => null,
        var u when Limit is > 0 => $"{u}/{Limit}",
        var u => $"{u} used"
    };

    public string DisplayName => Name switch
    {
        "gemini-3-pro-high" => "Gemini Pro",
        "gemini-3-flash" => "Gemini Flash",
        "claude-sonnet-4-5-thinking" => "Claude 4.5",
        "codex-session" => "Session",
        "codex-weekly" => "Weekly",
        "copilot-chat" => "Chat",
        "copilot-completions" => "Completions",
        "copilot-premium" => "Premium",
        _ => Name
    };
}
```

### ProviderQuotaData.cs
```csharp
namespace Quotio.Core.Models;

public record ProviderQuotaData(
    List<ModelQuota> Models,
    DateTime LastUpdated,
    bool IsForbidden = false,
    string? PlanType = null
)
{
    public string? PlanDisplayName => PlanType?.ToLower() switch
    {
        "plus" => "Plus",
        "pro" => "Pro",
        "team" => "Team",
        "enterprise" => "Enterprise",
        "free" => "Free",
        _ => PlanType
    };

    public double LowestPercentage => Models
        .Where(m => m.Percentage >= 0)
        .Select(m => m.Percentage)
        .DefaultIfEmpty(0)
        .Min();
}
```

### ProviderAccount.cs
```csharp
namespace Quotio.Core.Models;

public record ProviderAccount(
    AIProvider Provider,
    string Email,
    string? AccessToken = null,
    string? RefreshToken = null,
    DateTime? ExpiresAt = null,
    bool IsActive = true
)
{
    public string UniqueId => $"{Provider}_{Email}";
    
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
}
```

### AgentInfo.cs
```csharp
namespace Quotio.Core.Models;

public record AgentInfo(
    AgentType Type,
    string? BinaryPath,
    string? Version,
    bool IsInstalled,
    bool IsConfigured
)
{
    public string DisplayName => Type switch
    {
        AgentType.ClaudeCode => "Claude Code",
        AgentType.CodexCLI => "Codex CLI",
        AgentType.GeminiCLI => "Gemini CLI",
        AgentType.AmpCLI => "Amp CLI",
        AgentType.OpenCode => "OpenCode",
        AgentType.FactoryDroid => "Factory Droid",
        _ => Type.ToString()
    };
}
```

### ProxyStatus.cs
```csharp
namespace Quotio.Core.Models;

public record ProxyStatus(
    bool IsRunning,
    int Port,
    int? ProcessId = null,
    DateTime? StartedAt = null,
    long TotalRequests = 0,
    long SuccessfulRequests = 0
)
{
    public TimeSpan? Uptime => StartedAt.HasValue 
        ? DateTime.UtcNow - StartedAt.Value 
        : null;

    public double SuccessRate => TotalRequests > 0 
        ? (double)SuccessfulRequests / TotalRequests * 100 
        : 0;
}
```

### AppSettings.cs
```csharp
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
    public string Language { get; set; } = "en";
    public string Theme { get; set; } = "Dark";
}
```

---

## Step 3: Interfaces

### IQuotaFetcher.cs
```csharp
namespace Quotio.Core.Interfaces;

public interface IQuotaFetcher
{
    AIProvider Provider { get; }
    Task<ProviderQuotaData> FetchQuotaAsync(string accessToken, CancellationToken ct = default);
    Task<string> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}
```

### IProxyManager.cs
```csharp
namespace Quotio.Core.Interfaces;

public interface IProxyManager : IDisposable
{
    ProxyStatus Status { get; }
    event EventHandler<ProxyStatus>? StatusChanged;
    
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task RestartAsync(CancellationToken ct = default);
}
```

### IAgentService.cs
```csharp
namespace Quotio.Core.Interfaces;

public interface IAgentDetectionService
{
    Task<IReadOnlyList<AgentInfo>> DetectInstalledAgentsAsync(CancellationToken ct = default);
}

public interface IAgentConfigurationService
{
    Task<bool> ConfigureAgentAsync(AgentType agent, AgentConfig config, CancellationToken ct = default);
    Task<AgentConfig?> GetCurrentConfigAsync(AgentType agent, CancellationToken ct = default);
}
```

### INotificationService.cs
```csharp
namespace Quotio.Core.Interfaces;

public interface INotificationService
{
    void ShowInfo(string title, string message);
    void ShowWarning(string title, string message);
    void ShowError(string title, string message);
    void ShowQuotaWarning(AIProvider provider, string account, double percentage);
}
```

---

## Step 4: Constants

### ApiEndpoints.cs
```csharp
namespace Quotio.Core.Constants;

public static class ApiEndpoints
{
    // Antigravity
    public const string AntigravityQuotaApi = "https://cloudcode-pa.googleapis.com/v1internal:fetchAvailableModels";
    public const string AntigravityLoadProject = "https://cloudcode-pa.googleapis.com/v1internal:loadCodeAssist";
    
    // OAuth
    public const string GoogleTokenUrl = "https://oauth2.googleapis.com/token";
    public const string ClaudeTokenUrl = "https://console.anthropic.com/api/oauth/token";
    
    // GitHub Copilot
    public const string CopilotDeviceCodeUrl = "https://github.com/login/device/code";
    public const string CopilotTokenUrl = "https://github.com/login/oauth/access_token";
}
```

### AppConstants.cs
```csharp
namespace Quotio.Core.Constants;

public static class AppConstants
{
    public const string AppName = "Quotio";
    public const string AppVersion = "1.0.0";
    public const int DefaultProxyPort = 8317;
    public const int QuotaRefreshIntervalSeconds = 15;
    public const int TokenRefreshBufferMinutes = 5;
    
    public const string AntigravityClientId = "1071006060591-tmhssin2h21lcre235vtolojh4g403ep.apps.googleusercontent.com";
    public const string AntigravityUserAgent = "antigravity/1.11.3 Windows/x64";
    
    public static string AuthDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".cli-proxy-api"
    );
    
    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Quotio", "settings.json"
    );
}
```

---

## Verification

```powershell
cd c:\Users\Admin\Documents\quotio\QuotioWindows
dotnet build src/Quotio.Core
```

---

## Deliverables

- [x] 4 enums with extension methods
- [x] 6 data models (records)
- [x] 4 service interfaces
- [x] 2 constants classes

---

## Next Phase

→ [03-quota-fetchers.md](03-quota-fetchers.md)
