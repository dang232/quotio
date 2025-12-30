# Phase 4: Core Services

> **Duration**: 3-4 days  
> **Goal**: Implement proxy management, API client, and settings services

---

## Tasks

- [ ] CLIProxyManager (proxy lifecycle)
- [ ] ManagementApiClient (HTTP to proxy)
- [ ] SettingsService (app configuration)
- [ ] Language service (i18n)

---

## CLIProxyManager

### CLIProxyManager.cs
```csharp
namespace Quotio.Services.Proxy;

public class CLIProxyManager : IProxyManager
{
    private Process? _proxyProcess;
    private readonly ILogger<CLIProxyManager> _logger;
    private readonly SettingsService _settings;
    private CancellationTokenSource? _monitorCts;

    public ProxyStatus Status { get; private set; } = new(false, 8317);
    public event EventHandler<ProxyStatus>? StatusChanged;

    public CLIProxyManager(ILogger<CLIProxyManager> logger, SettingsService settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (Status.IsRunning)
        {
            _logger.LogWarning("Proxy is already running");
            return;
        }

        var binaryPath = GetProxyBinaryPath();
        if (!File.Exists(binaryPath))
        {
            await DownloadProxyBinaryAsync(ct);
        }

        var psi = new ProcessStartInfo
        {
            FileName = binaryPath,
            Arguments = $"--port {_settings.Current.ProxyPort}",
            WorkingDirectory = Path.GetDirectoryName(binaryPath),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _proxyProcess = Process.Start(psi);
        
        if (_proxyProcess == null)
            throw new InvalidOperationException("Failed to start proxy process");

        // Wait for proxy to be ready
        await WaitForProxyReadyAsync(ct);

        Status = Status with 
        { 
            IsRunning = true, 
            ProcessId = _proxyProcess.Id,
            StartedAt = DateTime.UtcNow
        };
        
        OnStatusChanged();
        StartMonitoring();
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (!Status.IsRunning || _proxyProcess == null)
            return;

        _monitorCts?.Cancel();

        try
        {
            // Try graceful shutdown first
            _proxyProcess.CloseMainWindow();
            
            if (!_proxyProcess.WaitForExit(5000))
            {
                _logger.LogWarning("Proxy didn't stop gracefully, forcing termination");
                _proxyProcess.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping proxy");
        }
        finally
        {
            _proxyProcess.Dispose();
            _proxyProcess = null;
            
            // Kill any orphan processes on the port
            await KillProcessOnPortAsync(_settings.Current.ProxyPort);
        }

        Status = Status with { IsRunning = false, ProcessId = null, StartedAt = null };
        OnStatusChanged();
    }

    public async Task RestartAsync(CancellationToken ct = default)
    {
        await StopAsync(ct);
        await Task.Delay(500, ct);
        await StartAsync(ct);
    }

    private async Task WaitForProxyReadyAsync(CancellationToken ct)
    {
        using var client = new HttpClient();
        var healthUrl = $"http://localhost:{_settings.Current.ProxyPort}/health";
        
        for (int i = 0; i < 30; i++)
        {
            try
            {
                var response = await client.GetAsync(healthUrl, ct);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch { }
            
            await Task.Delay(100, ct);
        }
        
        throw new TimeoutException("Proxy failed to start within timeout");
    }

    private void StartMonitoring()
    {
        _monitorCts = new CancellationTokenSource();
        
        Task.Run(async () =>
        {
            while (!_monitorCts.Token.IsCancellationRequested)
            {
                await Task.Delay(5000, _monitorCts.Token);
                
                if (_proxyProcess == null || _proxyProcess.HasExited)
                {
                    _logger.LogWarning("Proxy process has exited unexpectedly");
                    Status = Status with { IsRunning = false };
                    OnStatusChanged();
                    break;
                }
            }
        }, _monitorCts.Token);
    }

    private async Task KillProcessOnPortAsync(int port)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c netstat -ano | findstr :{port}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var match = Regex.Match(output, @"\s+LISTENING\s+(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var pid))
            {
                Process.GetProcessById(pid).Kill();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to kill process on port {Port}", port);
        }
    }

    private static string GetProxyBinaryPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Quotio", "bin", "CLIProxyAPI.exe");
    }

    private async Task DownloadProxyBinaryAsync(CancellationToken ct)
    {
        // Download from GitHub releases
        var url = "https://github.com/router-for-me/CLIProxyAPIPlus/releases/latest/download/CLIProxyAPI-windows-x64.exe";
        var targetPath = GetProxyBinaryPath();
        
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        
        using var client = new HttpClient();
        var bytes = await client.GetByteArrayAsync(url, ct);
        await File.WriteAllBytesAsync(targetPath, bytes, ct);
    }

    private void OnStatusChanged() => StatusChanged?.Invoke(this, Status);

    public void Dispose()
    {
        _monitorCts?.Cancel();
        _proxyProcess?.Dispose();
    }
}
```

---

## ManagementApiClient

### ManagementApiClient.cs
```csharp
namespace Quotio.Services.Proxy;

public class ManagementApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settings;
    private readonly ILogger<ManagementApiClient> _logger;

    private string BaseUrl => $"http://localhost:{_settings.Current.ProxyPort}";

    public ManagementApiClient(SettingsService settings, ILogger<ManagementApiClient> logger)
    {
        _settings = settings;
        _logger = logger;
        
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        };
        
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<ProxyStats?> GetStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/stats", ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonConvert.DeserializeObject<ProxyStats>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get proxy stats");
            return null;
        }
    }

    public async Task<List<ProviderAccount>> GetAccountsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/accounts", ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonConvert.DeserializeObject<List<ProviderAccount>>(json) 
                ?? new List<ProviderAccount>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get accounts");
            return new List<ProviderAccount>();
        }
    }

    public async Task<bool> AddAccountAsync(ProviderAccount account, CancellationToken ct = default)
    {
        try
        {
            var content = new StringContent(
                JsonConvert.SerializeObject(account),
                Encoding.UTF8,
                "application/json");
                
            var response = await _httpClient.PostAsync($"{BaseUrl}/accounts", content, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add account");
            return false;
        }
    }

    public async Task<bool> RemoveAccountAsync(string accountId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/accounts/{accountId}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove account {AccountId}", accountId);
            return false;
        }
    }

    public async Task<List<ApiKeyInfo>> GetApiKeysAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api-keys", ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonConvert.DeserializeObject<List<ApiKeyInfo>>(json) 
                ?? new List<ApiKeyInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get API keys");
            return new List<ApiKeyInfo>();
        }
    }

    public async Task<string?> CreateApiKeyAsync(string name, CancellationToken ct = default)
    {
        try
        {
            var content = new StringContent(
                JsonConvert.SerializeObject(new { name }),
                Encoding.UTF8,
                "application/json");
                
            var response = await _httpClient.PostAsync($"{BaseUrl}/api-keys", content, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonConvert.DeserializeObject<CreateApiKeyResponse>(json);
            return result?.Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create API key");
            return null;
        }
    }

    public void Dispose() => _httpClient.Dispose();
}

public record ProxyStats(
    long TotalRequests,
    long SuccessfulRequests,
    long FailedRequests,
    Dictionary<string, int> RequestsByProvider);

public record ApiKeyInfo(string Id, string Name, DateTime CreatedAt);
internal record CreateApiKeyResponse(string Key);
```

---

## SettingsService

### SettingsService.cs
```csharp
namespace Quotio.Services.System;

public class SettingsService
{
    private readonly string _settingsPath;
    private readonly ILogger<SettingsService> _logger;
    
    public AppSettings Current { get; private set; } = new();
    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        _settingsPath = AppConstants.SettingsPath;
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                Current = JsonConvert.DeserializeObject<AppSettings>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
                
            var json = JsonConvert.SerializeObject(Current, Formatting.Indented);
            File.WriteAllText(_settingsPath, json);
            
            SettingsChanged?.Invoke(this, Current);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
    }

    public void Update(Action<AppSettings> updateAction)
    {
        updateAction(Current);
        Save();
    }
}
```

---

## Verification

```powershell
cd c:\Users\Admin\Documents\quotio\QuotioWindows
dotnet build src/Quotio.Services
dotnet test tests/Quotio.Tests --filter "Category=Services"
```

---

## Next Phase

→ [05-agent-services.md](05-agent-services.md)
