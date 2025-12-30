# Phase 5: Agent Services

> **Duration**: 2-3 days  
> **Goal**: Implement agent detection and configuration for Windows

---

## Tasks

- [ ] AgentDetectionService
- [ ] AgentConfigurationService
- [ ] ShellProfileManager (PowerShell)

---

## AgentDetectionService

### AgentDetectionService.cs
```csharp
namespace Quotio.Services.Agents;

public class AgentDetectionService : IAgentDetectionService
{
    private readonly ILogger<AgentDetectionService> _logger;

    public AgentDetectionService(ILogger<AgentDetectionService> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<AgentInfo>> DetectInstalledAgentsAsync(
        CancellationToken ct = default)
    {
        var agents = new List<AgentInfo>();
        
        foreach (AgentType type in Enum.GetValues<AgentType>())
        {
            var info = await DetectAgentAsync(type, ct);
            agents.Add(info);
        }

        return agents.AsReadOnly();
    }

    private async Task<AgentInfo> DetectAgentAsync(AgentType type, CancellationToken ct)
    {
        var binaryNames = GetBinaryNames(type);
        string? foundPath = null;
        string? version = null;

        foreach (var name in binaryNames)
        {
            foundPath = await FindBinaryInPathAsync(name, ct);
            if (foundPath != null)
            {
                version = await GetVersionAsync(foundPath, ct);
                break;
            }
        }

        var isConfigured = foundPath != null && CheckIfConfigured(type);

        return new AgentInfo(type, foundPath, version, foundPath != null, isConfigured);
    }

    private static string[] GetBinaryNames(AgentType type) => type switch
    {
        AgentType.ClaudeCode => new[] { "claude.exe", "claude" },
        AgentType.CodexCLI => new[] { "codex.exe", "codex" },
        AgentType.GeminiCLI => new[] { "gemini.exe", "gemini" },
        AgentType.AmpCLI => new[] { "amp.exe", "amp" },
        AgentType.OpenCode => new[] { "opencode.exe", "opencode", "oc.exe", "oc" },
        AgentType.FactoryDroid => new[] { "droid.exe", "droid", "factory-droid.exe", "fd.exe" },
        _ => new[] { type.ToString().ToLower() + ".exe" }
    };

    private static async Task<string?> FindBinaryInPathAsync(string binaryName, CancellationToken ct)
    {
        // Check PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var paths = pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path, binaryName);
            if (File.Exists(fullPath))
                return fullPath;
        }

        // Check common installation locations
        var commonPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                ".local", "bin"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                "AppData", "Local", "Microsoft", "WinGet", "Packages")
        };

        foreach (var basePath in commonPaths)
        {
            if (!Directory.Exists(basePath)) continue;
            
            var found = Directory.GetFiles(basePath, binaryName, 
                SearchOption.AllDirectories).FirstOrDefault();
            if (found != null)
                return found;
        }

        // Use where.exe
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = binaryName,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync(ct);
                await process.WaitForExitAsync(ct);
                
                if (process.ExitCode == 0)
                {
                    var firstLine = output.Split('\n').FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(firstLine) && File.Exists(firstLine))
                        return firstLine;
                }
            }
        }
        catch { }

        return null;
    }

    private static async Task<string?> GetVersionAsync(string binaryPath, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = binaryPath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            var match = Regex.Match(output, @"(\d+\.\d+(?:\.\d+)?)");
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool CheckIfConfigured(AgentType type)
    {
        var configPath = type.GetConfigPath();
        return !string.IsNullOrEmpty(configPath) && File.Exists(configPath);
    }
}
```

---

## AgentConfigurationService

### AgentConfigurationService.cs
```csharp
namespace Quotio.Services.Agents;

public class AgentConfigurationService : IAgentConfigurationService
{
    private readonly SettingsService _settings;
    private readonly ILogger<AgentConfigurationService> _logger;

    public AgentConfigurationService(
        SettingsService settings, 
        ILogger<AgentConfigurationService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> ConfigureAgentAsync(
        AgentType agent, 
        AgentConfig config, 
        CancellationToken ct = default)
    {
        try
        {
            return agent switch
            {
                AgentType.ClaudeCode => await ConfigureClaudeCodeAsync(config, ct),
                AgentType.CodexCLI => await ConfigureCodexCliAsync(config, ct),
                AgentType.OpenCode => await ConfigureOpenCodeAsync(config, ct),
                AgentType.FactoryDroid => await ConfigureFactoryDroidAsync(config, ct),
                AgentType.GeminiCLI => await ConfigureGeminiCliAsync(config, ct),
                AgentType.AmpCLI => await ConfigureAmpCliAsync(config, ct),
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure agent {Agent}", agent);
            return false;
        }
    }

    private async Task<bool> ConfigureClaudeCodeAsync(AgentConfig config, CancellationToken ct)
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude", "settings.json");

        var settings = new Dictionary<string, object>();
        
        if (File.Exists(settingsPath))
        {
            var existing = await File.ReadAllTextAsync(settingsPath, ct);
            settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(existing) 
                ?? new Dictionary<string, object>();
        }

        settings["apiUrl"] = $"http://localhost:{_settings.Current.ProxyPort}";
        settings["apiKey"] = config.ApiKey ?? "quotio";

        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        await File.WriteAllTextAsync(settingsPath, 
            JsonConvert.SerializeObject(settings, Formatting.Indented), ct);

        return true;
    }

    private async Task<bool> ConfigureCodexCliAsync(AgentConfig config, CancellationToken ct)
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
        
        Directory.CreateDirectory(configDir);

        // config.toml
        var tomlPath = Path.Combine(configDir, "config.toml");
        var toml = $"""
            [api]
            base_url = "http://localhost:{_settings.Current.ProxyPort}"
            
            [models]
            opus = "{config.OpusModel ?? "gemini-claude-opus-4-5-thinking"}"
            sonnet = "{config.SonnetModel ?? "gemini-claude-sonnet-4-5"}"
            haiku = "{config.HaikuModel ?? "gemini-3-flash-preview"}"
            """;
        
        await File.WriteAllTextAsync(tomlPath, toml, ct);

        // auth.json
        var authPath = Path.Combine(configDir, "auth.json");
        var auth = new { api_key = config.ApiKey ?? "quotio" };
        await File.WriteAllTextAsync(authPath, 
            JsonConvert.SerializeObject(auth, Formatting.Indented), ct);

        return true;
    }

    private async Task<bool> ConfigureOpenCodeAsync(AgentConfig config, CancellationToken ct)
    {
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "opencode", "opencode.json");

        var configuration = new
        {
            providers = new
            {
                openai = new
                {
                    apiKey = config.ApiKey ?? "quotio",
                    baseUrl = $"http://localhost:{_settings.Current.ProxyPort}"
                }
            }
        };

        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        await File.WriteAllTextAsync(configPath,
            JsonConvert.SerializeObject(configuration, Formatting.Indented), ct);

        return true;
    }

    private async Task<bool> ConfigureFactoryDroidAsync(AgentConfig config, CancellationToken ct)
    {
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".factory", "config.json");

        var configuration = new
        {
            api_key = config.ApiKey ?? "quotio",
            api_url = $"http://localhost:{_settings.Current.ProxyPort}"
        };

        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        await File.WriteAllTextAsync(configPath,
            JsonConvert.SerializeObject(configuration, Formatting.Indented), ct);

        return true;
    }

    private Task<bool> ConfigureGeminiCliAsync(AgentConfig config, CancellationToken ct)
    {
        // Gemini CLI uses environment variables only
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", config.ApiKey ?? "quotio", 
            EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable("GEMINI_BASE_URL", 
            $"http://localhost:{_settings.Current.ProxyPort}", 
            EnvironmentVariableTarget.User);
        
        return Task.FromResult(true);
    }

    private async Task<bool> ConfigureAmpCliAsync(AgentConfig config, CancellationToken ct)
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "amp");
        var secretsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "amp");

        Directory.CreateDirectory(configDir);
        Directory.CreateDirectory(secretsDir);

        // settings.json
        var settingsPath = Path.Combine(configDir, "settings.json");
        var settings = new { base_url = $"http://localhost:{_settings.Current.ProxyPort}" };
        await File.WriteAllTextAsync(settingsPath,
            JsonConvert.SerializeObject(settings, Formatting.Indented), ct);

        // secrets.json
        var secretsPath = Path.Combine(secretsDir, "secrets.json");
        var secrets = new { api_key = config.ApiKey ?? "quotio" };
        await File.WriteAllTextAsync(secretsPath,
            JsonConvert.SerializeObject(secrets, Formatting.Indented), ct);

        return true;
    }

    public async Task<AgentConfig?> GetCurrentConfigAsync(AgentType agent, CancellationToken ct)
    {
        var configPath = agent.GetConfigPath();
        if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
            return null;

        var content = await File.ReadAllTextAsync(configPath, ct);
        // Parse based on agent type and return config
        return new AgentConfig();
    }
}

public record AgentConfig(
    string? ApiKey = null,
    string? OpusModel = null,
    string? SonnetModel = null,
    string? HaikuModel = null,
    bool UseEnvironmentVariables = false
);
```

---

## ShellProfileManager

### ShellProfileManager.cs
```csharp
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

        var quotioBlock = $"""
            # Quotio Environment Variables
            $env:QUOTIO_PROXY_URL = "http://localhost:{port}"
            $env:QUOTIO_API_KEY = "quotio"
            $env:OPENAI_BASE_URL = $env:QUOTIO_PROXY_URL
            $env:ANTHROPIC_BASE_URL = $env:QUOTIO_PROXY_URL
            # End Quotio
            """;

        Directory.CreateDirectory(Path.GetDirectoryName(profilePath)!);

        if (File.Exists(profilePath))
        {
            var content = await File.ReadAllTextAsync(profilePath, ct);
            
            // Remove existing Quotio block
            content = Regex.Replace(content, 
                @"# Quotio Environment Variables.*?# End Quotio\r?\n?", 
                "", RegexOptions.Singleline);
            
            content = content.Trim() + "\n\n" + quotioBlock;
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
```

---

## Verification

```powershell
cd c:\Users\Admin\Documents\quotio\QuotioWindows
dotnet build src/Quotio.Services
dotnet test tests/Quotio.Tests --filter "Category=Agents"
```

---

## Next Phase

→ [06-ui-foundation.md](06-ui-foundation.md)
