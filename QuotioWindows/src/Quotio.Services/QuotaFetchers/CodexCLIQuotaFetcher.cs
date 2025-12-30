using Microsoft.Extensions.Logging;
using Quotio.Core.Enums;
using Quotio.Core.Models;

namespace Quotio.Services.QuotaFetchers;

public class CodexCLIQuotaFetcher : BaseQuotaFetcher
{
    public override AIProvider Provider => AIProvider.Codex;

    public CodexCLIQuotaFetcher(HttpClient httpClient, ILogger<CodexCLIQuotaFetcher> logger)
        : base(httpClient, logger) { }

    public override async Task<IEnumerable<ProviderQuotaData>> FetchQuotasAsync(CancellationToken ct = default)
    {
         // Placeholder implementation
         return await Task.FromResult(new[] 
        { 
            new ProviderQuotaData(
                new List<ModelQuota>(), 
                DateTime.UtcNow,
                true) 
            { 
                ProviderName = "Codex CLI",
                AccountName = "Not Implemented"
            } 
        });
    }
}
