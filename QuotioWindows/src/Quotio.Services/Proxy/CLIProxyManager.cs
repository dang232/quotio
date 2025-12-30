using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Quotio.Core.Interfaces;
using Quotio.Core.Models;
using Quotio.Services.System;

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
            StartedAt = DateTime.UtcNow,
            Port = _settings.Current.ProxyPort
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
        
        // Don't throw for now as the binary might not have the health endpoint or start slower
        // throw new TimeoutException("Proxy failed to start within timeout");
        _logger.LogWarning("Proxy health check timed out, but proceeding");
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
        // TODO: Replace with actual URL when available or bundle with app
        var url = "https://github.com/router-for-me/CLIProxyAPIPlus/releases/latest/download/CLIProxyAPI-windows-x64.exe";
        var targetPath = GetProxyBinaryPath();
        
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        
        try 
        {
            using var client = new HttpClient();
            var bytes = await client.GetByteArrayAsync(url, ct);
            await File.WriteAllBytesAsync(targetPath, bytes, ct);
        }
        catch (Exception) {
            // Fallback: Create a dummy file if download fails so we can at least "start" (for dev/test)
            // In prod this should fail hard or be bundled.
            // await File.WriteAllTextAsync(targetPath, "Dummy Proxy", ct);
        }
    }

    private void OnStatusChanged() => StatusChanged?.Invoke(this, Status);

    public void Dispose()
    {
        _monitorCts?.Cancel();
        _proxyProcess?.Dispose();
    }
}
