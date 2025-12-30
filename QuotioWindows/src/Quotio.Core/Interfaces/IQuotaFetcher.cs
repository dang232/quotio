using Quotio.Core.Enums;
using Quotio.Core.Models;

namespace Quotio.Core.Interfaces;

public interface IQuotaFetcher
{
    AIProvider Provider { get; }
    Task<IEnumerable<ProviderQuotaData>> FetchQuotasAsync(CancellationToken ct = default);
}
