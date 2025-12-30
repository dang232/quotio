using Quotio.Core.Enums;

namespace Quotio.Core.Models;

public record ProviderAccount(
    AIProvider Provider,
    string Email,
    string? AccessToken = null,
    string? RefreshToken = null,
    DateTime? ExpiresAt = null,
    bool IsActive = true
)
{
    public string UniqueId => $"{Provider}_{Email}";
    
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
}
