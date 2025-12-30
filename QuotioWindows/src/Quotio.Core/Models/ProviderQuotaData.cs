namespace Quotio.Core.Models;

public record ProviderQuotaData(
    List<ModelQuota> Models,
    DateTime LastUpdated,
    bool IsForbidden = false,
    string? PlanType = null
)
{
    public string ProviderName { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
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
