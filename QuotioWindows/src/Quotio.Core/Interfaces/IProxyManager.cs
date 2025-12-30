using Quotio.Core.Models;

namespace Quotio.Core.Interfaces;

public interface IProxyManager : IDisposable
{
    ProxyStatus Status { get; }
    event EventHandler<ProxyStatus>? StatusChanged;
    
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task RestartAsync(CancellationToken ct = default);
}
