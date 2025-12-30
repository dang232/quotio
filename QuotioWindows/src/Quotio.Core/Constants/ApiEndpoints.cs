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
