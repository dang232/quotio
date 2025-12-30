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
