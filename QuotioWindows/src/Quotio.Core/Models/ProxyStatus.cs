namespace Quotio.Core.Models;

public record ProxyStatus(
    bool IsRunning,
    int Port,
    int? ProcessId = null,
    DateTime? StartedAt = null,
    long TotalRequests = 0,
    long SuccessfulRequests = 0
)
{
    public TimeSpan? Uptime => StartedAt.HasValue 
        ? DateTime.UtcNow - StartedAt.Value 
        : null;

    public double SuccessRate => TotalRequests > 0 
        ? (double)SuccessfulRequests / TotalRequests * 100 
        : 0;
}
