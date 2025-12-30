using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Quotio.Core.Enums;
using Quotio.Core.Interfaces;
using Quotio.Core.Models;

namespace Quotio.Services.QuotaFetchers;

public abstract class BaseQuotaFetcher : IQuotaFetcher
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;

    public abstract AIProvider Provider { get; }

    protected BaseQuotaFetcher(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public abstract Task<IEnumerable<ProviderQuotaData>> FetchQuotasAsync(CancellationToken ct = default);

    protected async Task<T?> GetJsonAsync<T>(string url, Dictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch JSON from {Url}", url);
            return default;
        }
    }

    protected async Task<T?> PostJsonAsync<T>(string url, object payload, Dictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = JsonContent.Create(payload);

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post JSON to {Url}", url);
            return default;
        }
    }
}
