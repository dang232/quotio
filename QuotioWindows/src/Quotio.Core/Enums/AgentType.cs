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

    public static string GetDisplayName(this AgentType agent) => agent switch
    {
        AgentType.ClaudeCode => "Claude Code",
        AgentType.CodexCLI => "Codex CLI",
        AgentType.GeminiCLI => "Gemini CLI",
        AgentType.AmpCLI => "Amp CLI",
        AgentType.OpenCode => "OpenCode",
        AgentType.FactoryDroid => "Factory Droid",
        _ => agent.ToString()
    };
}
