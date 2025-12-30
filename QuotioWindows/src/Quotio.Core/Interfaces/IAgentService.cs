using Quotio.Core.Enums;
using Quotio.Core.Models;

namespace Quotio.Core.Interfaces;

public interface IAgentDetectionService
{
    Task<IReadOnlyList<AgentInfo>> DetectInstalledAgentsAsync(CancellationToken ct = default);
}

public interface IAgentConfigurationService
{
    Task<bool> ConfigureAgentAsync(AgentType agent, AgentConfig config, CancellationToken ct = default);
    Task<AgentConfig?> GetCurrentConfigAsync(AgentType agent, CancellationToken ct = default);
}

public record AgentConfig(
    string? ApiKey = null,
    string? OpusModel = null,
    string? SonnetModel = null,
    string? HaikuModel = null,
    bool UseEnvironmentVariables = false
);
