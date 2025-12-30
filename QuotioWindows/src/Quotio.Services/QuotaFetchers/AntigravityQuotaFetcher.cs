using System.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quotio.Core.Constants;
using Quotio.Core.Enums;
using Quotio.Core.Models;

namespace Quotio.Services.QuotaFetchers;

public class AntigravityQuotaFetcher : BaseQuotaFetcher
{
    private const string QuotaApiUrl = ApiEndpoints.AntigravityQuotaApi;
    private const string TokenUrl = ApiEndpoints.GoogleTokenUrl;
    private const string ClientId = AppConstants.AntigravityClientId;
    private const string ClientSecret = "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";

    public override AIProvider Provider => AIProvider.Antigravity;

    public AntigravityQuotaFetcher(HttpClient httpClient, ILogger<AntigravityQuotaFetcher> logger) 
        : base(httpClient, logger) { }

    public override async Task<IEnumerable<ProviderQuotaData>> FetchQuotasAsync(CancellationToken ct = default)
    {
        var results = new List<ProviderQuotaData>();
        var authDir = AppConstants.AuthDirectory;

        if (!Directory.Exists(authDir))
            return results;

        var files = Directory.GetFiles(authDir, "antigravity-*.json");
        
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var authFile = JsonConvert.DeserializeObject<AntigravityAuthFile>(json);
                
                if (authFile == null) continue;

                var accessToken = authFile.AccessToken;
                
                if (authFile.IsExpired && authFile.RefreshToken != null)
                {
                    try 
                    {
                        accessToken = await RefreshTokenAsync(authFile.RefreshToken, ct);
                        // Update file with new token
                        authFile = authFile with { AccessToken = accessToken };
                        await File.WriteAllTextAsync(file, 
                            JsonConvert.SerializeObject(authFile), ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to refresh token for {File}", file);
                        continue; // Skip this file if refresh fails
                    }
                }

                var quota = await FetchSingleQuotaAsync(accessToken, ExtractEmailFromFilename(file), ct);
                results.Add(quota);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch quota for {File}", file);
            }
        }

        return results;
    }

    private async Task<ProviderQuotaData> FetchSingleQuotaAsync(string accessToken, string accountName, CancellationToken ct)
    {
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = $"Bearer {accessToken}",
            ["User-Agent"] = AppConstants.AntigravityUserAgent
        };

        var payload = new { };
        
        try
        {
            var response = await PostJsonAsync<AntigravityQuotaResponse>(
                QuotaApiUrl, payload, headers, ct);

            if (response?.Models == null)
                return new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true) { AccountName = accountName, ProviderName = "Antigravity" };

            var models = response.Models
                .Where(kvp => kvp.Key.Contains("gemini") || kvp.Key.Contains("claude"))
                .Select(kvp => new ModelQuota(
                    kvp.Key,
                    (kvp.Value.QuotaInfo?.RemainingFraction ?? 0) * 100,
                    kvp.Value.QuotaInfo?.ResetTime ?? ""
                ))
                .ToList();

            return new ProviderQuotaData(models, DateTime.UtcNow) { AccountName = accountName, ProviderName = "Antigravity" };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true) { AccountName = accountName, ProviderName = "Antigravity" };
        }
    }

    private async Task<string> RefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        });

        var response = await _httpClient.PostAsync(TokenUrl, content, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var tokenResponse = JsonConvert.DeserializeObject<TokenRefreshResponse>(json);
        
        return tokenResponse?.AccessToken 
            ?? throw new InvalidOperationException("Token refresh failed");
    }

    private static string ExtractEmailFromFilename(string path)
    {
        var filename = Path.GetFileNameWithoutExtension(path);
        return filename
            .Replace("antigravity-", "")
            .Replace("_", ".")
            .Replace(".gmail.com", "@gmail.com");
    }
}

// Response models
internal record AntigravityQuotaResponse(
    [property: JsonProperty("models")] Dictionary<string, ModelInfo>? Models);

internal record ModelInfo(
    [property: JsonProperty("quotaInfo")] QuotaInfo? QuotaInfo);

internal record QuotaInfo(
    [property: JsonProperty("remainingFraction")] double? RemainingFraction,
    [property: JsonProperty("resetTime")] string? ResetTime);

internal record TokenRefreshResponse(
    [property: JsonProperty("access_token")] string AccessToken,
    [property: JsonProperty("expires_in")] int ExpiresIn);

internal record AntigravityAuthFile(
    [property: JsonProperty("access_token")] string AccessToken,
    [property: JsonProperty("email")] string Email,
    [property: JsonProperty("expired")] string? Expired,
    [property: JsonProperty("expires_in")] int? ExpiresIn,
    [property: JsonProperty("refresh_token")] string? RefreshToken)
{
    public bool IsExpired => DateTime.TryParse(Expired, out var expiry) 
        && DateTime.UtcNow > expiry;
}
