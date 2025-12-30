using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Quotio.Services.System;

namespace Quotio.Services.Agents;

public class ShellProfileManager
{
    private readonly SettingsService _settings;
    private readonly ILogger<ShellProfileManager> _logger;

    public ShellProfileManager(SettingsService settings, ILogger<ShellProfileManager> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task AddEnvironmentVariablesAsync(CancellationToken ct = default)
    {
        var port = _settings.Current.ProxyPort;
        
        // Set for current user
        Environment.SetEnvironmentVariable("QUOTIO_PROXY_URL", 
            $"http://localhost:{port}", EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable("QUOTIO_API_KEY", 
            "quotio", EnvironmentVariableTarget.User);

        // Update PowerShell profile
        await UpdatePowerShellProfileAsync(port, ct);
    }

    private async Task UpdatePowerShellProfileAsync(int port, CancellationToken ct)
    {
        var profilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "WindowsPowerShell", "Microsoft.PowerShell_profile.ps1");

        // Ensure modern PowerShell Core path also checked or use $PROFILE logic if we could process it
        // For standard "MyDocuments" path:
        
        var quotioBlock = $"""
            # Quotio Environment Variables
            $env:QUOTIO_PROXY_URL = "http://localhost:{port}"
            $env:QUOTIO_API_KEY = "quotio"
            $env:OPENAI_BASE_URL = $env:QUOTIO_PROXY_URL
            $env:ANTHROPIC_BASE_URL = $env:QUOTIO_PROXY_URL
            # End Quotio
            """;

        var dir = Path.GetDirectoryName(profilePath);
        if (!string.IsNullOrEmpty(dir))
             Directory.CreateDirectory(dir);

        if (File.Exists(profilePath))
        {
            var content = await File.ReadAllTextAsync(profilePath, ct);
            
            // Remove existing Quotio block
            content = Regex.Replace(content, 
                @"# Quotio Environment Variables.*?# End Quotio\r?\n?", 
                "", RegexOptions.Singleline);
            
            content = content.Trim() + "\r\n\r\n" + quotioBlock;
            await File.WriteAllTextAsync(profilePath, content, ct);
        }
        else
        {
            await File.WriteAllTextAsync(profilePath, quotioBlock, ct);
        }
    }

    public async Task RemoveEnvironmentVariablesAsync(CancellationToken ct = default)
    {
        Environment.SetEnvironmentVariable("QUOTIO_PROXY_URL", null, 
            EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable("QUOTIO_API_KEY", null, 
            EnvironmentVariableTarget.User);

        // Clean up PowerShell profile
        var profilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "WindowsPowerShell", "Microsoft.PowerShell_profile.ps1");

        if (File.Exists(profilePath))
        {
            var content = await File.ReadAllTextAsync(profilePath, ct);
            content = Regex.Replace(content, 
                @"# Quotio Environment Variables.*?# End Quotio\r?\n?", 
                "", RegexOptions.Singleline);
            await File.WriteAllTextAsync(profilePath, content.Trim(), ct);
        }
    }
}
