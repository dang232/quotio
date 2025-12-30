using Microsoft.Extensions.Logging;
using Quotio.Core.Enums;
using Quotio.Core.Models;

namespace Quotio.Services.QuotaFetchers;

public class CopilotQuotaFetcher : BaseQuotaFetcher
{
    public override AIProvider Provider => AIProvider.Copilot;

    public CopilotQuotaFetcher(HttpClient httpClient, ILogger<CopilotQuotaFetcher> logger) 
        : base(httpClient, logger) { }

    public override async Task<IEnumerable<ProviderQuotaData>> FetchQuotasAsync(CancellationToken ct = default)
    {
        // For now, assume we can get token from environment or config file if needed.
        // But Copilot is tricky without direct access to VS Code internal state or similar.
        // We will just return a placeholder or look for a specific env var users might set.
        
        var accessToken = Environment.GetEnvironmentVariable("GITHUB_COPILOT_TOKEN");

        if (string.IsNullOrEmpty(accessToken))
        {
            return new[] { new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true) { ProviderName = "GitHub Copilot", AccountName = "Not Configured" } };
        }
        
        try
        {
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"token {accessToken}",
                ["User-Agent"] = "GithubCopilot/1.155.0",
                ["Accept"] = "application/json"
            };

            var url = "https://api.github.com/copilot_internal/v2/token"; // Example internal endpoint, subject to change
            // Or just check user status
            var userUrl = "https://api.github.com/user";
            
            await GetJsonAsync<dynamic>(userUrl, headers, ct);

             return new[] 
            { 
                new ProviderQuotaData(
                    new List<ModelQuota> { new ModelQuota("copilot-access", 100, "Active") }, 
                    DateTime.UtcNow) 
                { 
                    ProviderName = "GitHub Copilot",
                    AccountName = "Connected"
                } 
            };
        }
        catch
        {
             return new[] { new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true) { ProviderName = "GitHub Copilot", AccountName = "Error" } };
        }
    }
}
