using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quotio.Core.Enums;
using Quotio.Core.Interfaces;
using Quotio.Services.System;

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

        try 
        {
            // Placeholder: Parse based on agent type and return config
            // For now just return empty if file exists
            return await Task.FromResult(new AgentConfig());   
        }
        catch
        {
            return null;
        }
    }
}
