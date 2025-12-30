using Microsoft.Extensions.Logging;
using Quotio.Core.Enums;
using Quotio.Core.Models;
using Quotio.Services.System;

namespace Quotio.Services.QuotaFetchers;

public class OpenAIQuotaFetcher : BaseQuotaFetcher
{
    private readonly SettingsService _settingsService;

    public override AIProvider Provider => AIProvider.Codex; // Maps to OpenAI generic

    public OpenAIQuotaFetcher(HttpClient httpClient, ILogger<OpenAIQuotaFetcher> logger, SettingsService settingsService)
        : base(httpClient, logger) 
    {
        _settingsService = settingsService;
    }

    public override async Task<IEnumerable<ProviderQuotaData>> FetchQuotasAsync(CancellationToken ct = default)
    {
        // Retrieve API Key from settings or environment
        var apiKey = _settingsService.Current.OpenAiApiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
             return new[] { new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true) { ProviderName = "OpenAI", AccountName = "Not Configured" } };
        }

        try 
        {
            var headers = new Dictionary<string, string> 
            { 
                ["Authorization"] = $"Bearer {apiKey}" 
            };

            // This endpoint is often protected/internal, but we'll try standard patterns to check connectivity
            var modelsUrl = "https://api.openai.com/v1/models";
            await GetJsonAsync<dynamic>(modelsUrl, headers, ct);

            // If effective, we assume 100% available or similar, or just "Connected"
            // Since we can't get real quota easily without deprecated billing API.
            return new[] 
            { 
                new ProviderQuotaData(
                    new List<ModelQuota> { new ModelQuota("openai-access", 100, "N/A") }, 
                    DateTime.UtcNow) 
                { 
                    ProviderName = "OpenAI",
                    AccountName = "Connected"
                } 
            };
        }
        catch
        {
            return new[] { new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true) { ProviderName = "OpenAI", AccountName = "Error" } };
        }
    }
}
