using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quotio.Core.Models;
using Quotio.Services.System;

namespace Quotio.Services.Proxy;

public class ManagementApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settings;
    private readonly ILogger<ManagementApiClient> _logger;

    private string BaseUrl => $"http://localhost:{_settings.Current.ProxyPort}";

    public ManagementApiClient(SettingsService settings, ILogger<ManagementApiClient> logger)
    {
        _settings = settings;
        _logger = logger;
        
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        };
        
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<ProxyStats?> GetStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/stats", ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonConvert.DeserializeObject<ProxyStats>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get proxy stats");
            return null;
        }
    }

    public async Task<List<ProviderAccount>> GetAccountsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/accounts", ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonConvert.DeserializeObject<List<ProviderAccount>>(json) 
                ?? new List<ProviderAccount>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get accounts");
            return new List<ProviderAccount>();
        }
    }

    public async Task<bool> AddAccountAsync(ProviderAccount account, CancellationToken ct = default)
    {
        try
        {
            var content = new StringContent(
                JsonConvert.SerializeObject(account),
                Encoding.UTF8,
                "application/json");
                
            var response = await _httpClient.PostAsync($"{BaseUrl}/accounts", content, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add account");
            return false;
        }
    }

    public async Task<bool> RemoveAccountAsync(string accountId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/accounts/{accountId}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove account {AccountId}", accountId);
            return false;
        }
    }

    public async Task<List<ApiKeyInfo>> GetApiKeysAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/api-keys", ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonConvert.DeserializeObject<List<ApiKeyInfo>>(json) 
                ?? new List<ApiKeyInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get API keys");
            return new List<ApiKeyInfo>();
        }
    }

    public async Task<string?> CreateApiKeyAsync(string name, CancellationToken ct = default)
    {
        try
        {
            var content = new StringContent(
                JsonConvert.SerializeObject(new { name }),
                Encoding.UTF8,
                "application/json");
                
            var response = await _httpClient.PostAsync($"{BaseUrl}/api-keys", content, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonConvert.DeserializeObject<CreateApiKeyResponse>(json);
            return result?.Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create API key");
            return null;
        }
    }

    public void Dispose() => _httpClient.Dispose();
}

public record ProxyStats(
    long TotalRequests,
    long SuccessfulRequests,
    long FailedRequests,
    Dictionary<string, int> RequestsByProvider);

public record ApiKeyInfo(string Id, string Name, DateTime CreatedAt);
internal record CreateApiKeyResponse(string Key);
