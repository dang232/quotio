using Quotio.Core.Enums;

namespace Quotio.Core.Models;

public record AgentInfo(
    AgentType Type,
    string? BinaryPath,
    string? Version,
    bool IsInstalled,
    bool IsConfigured
)
{
    public string DisplayName => Type.GetDisplayName();
}
