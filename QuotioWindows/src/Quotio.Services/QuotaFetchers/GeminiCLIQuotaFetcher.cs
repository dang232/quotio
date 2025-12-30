using Microsoft.Extensions.Logging;
using Quotio.Core.Enums;
using Quotio.Core.Models;

namespace Quotio.Services.QuotaFetchers;

public class GeminiCLIQuotaFetcher : BaseQuotaFetcher
{
    public override AIProvider Provider => AIProvider.Gemini;

    public GeminiCLIQuotaFetcher(HttpClient httpClient, ILogger<GeminiCLIQuotaFetcher> logger)
        : base(httpClient, logger) { }

    public override async Task<IEnumerable<ProviderQuotaData>> FetchQuotasAsync(CancellationToken ct = default)
    {
        // Similar to other CLIs, check for env var or assume not configured for MVP
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            return new[] { new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true) { ProviderName = "Gemini CLI", AccountName = "Not Configured" } };
        }

        return await Task.FromResult(new[] 
        { 
            new ProviderQuotaData(
                new List<ModelQuota> { new ModelQuota("gemini-pro", 100, "Unknown") }, 
                DateTime.UtcNow) 
            { 
                ProviderName = "Gemini CLI",
                AccountName = "Connected"
            } 
        });
    }
}
