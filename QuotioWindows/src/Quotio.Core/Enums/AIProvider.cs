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
