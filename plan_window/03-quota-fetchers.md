# Phase 3: Quota Fetchers

> **Duration**: 5-7 days  
> **Goal**: Port all 7 provider quota fetchers from Swift to C#

---

## Tasks

- [ ] Create base fetcher class
- [ ] AntigravityQuotaFetcher (priority)
- [ ] ClaudeCodeQuotaFetcher
- [ ] CodexCLIQuotaFetcher
- [ ] GeminiCLIQuotaFetcher
- [ ] CopilotQuotaFetcher
- [ ] CursorQuotaFetcher
- [ ] OpenAIQuotaFetcher

---

## Base Fetcher Class

### BaseQuotaFetcher.cs
```csharp
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

    public abstract Task<ProviderQuotaData> FetchQuotaAsync(
        string accessToken, CancellationToken ct = default);

    public abstract Task<string> RefreshTokenAsync(
        string refreshToken, CancellationToken ct = default);

    protected async Task<T?> PostJsonAsync<T>(
        string url, 
        object payload, 
        Dictionary<string, string>? headers = null,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(
            JsonConvert.SerializeObject(payload),
            Encoding.UTF8,
            "application/json");

        if (headers != null)
        {
            foreach (var (key, value) in headers)
                request.Headers.TryAddWithoutValidation(key, value);
        }

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        return JsonConvert.DeserializeObject<T>(content);
    }
}
```

---

## Antigravity Quota Fetcher

### AntigravityQuotaFetcher.cs
```csharp
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

    public override async Task<ProviderQuotaData> FetchQuotaAsync(
        string accessToken, CancellationToken ct = default)
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
                return new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true);

            var models = response.Models
                .Where(kvp => kvp.Key.Contains("gemini") || kvp.Key.Contains("claude"))
                .Select(kvp => new ModelQuota(
                    kvp.Key,
                    (kvp.Value.QuotaInfo?.RemainingFraction ?? 0) * 100,
                    kvp.Value.QuotaInfo?.ResetTime ?? ""
                ))
                .ToList();

            return new ProviderQuotaData(models, DateTime.UtcNow);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true);
        }
    }

    public override async Task<string> RefreshTokenAsync(
        string refreshToken, CancellationToken ct = default)
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

    public async Task<Dictionary<string, ProviderQuotaData>> FetchAllAccountsAsync(
        CancellationToken ct = default)
    {
        var results = new Dictionary<string, ProviderQuotaData>();
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
                    accessToken = await RefreshTokenAsync(authFile.RefreshToken, ct);
                    // Update file with new token
                    authFile = authFile with { AccessToken = accessToken };
                    await File.WriteAllTextAsync(file, 
                        JsonConvert.SerializeObject(authFile), ct);
                }

                var quota = await FetchQuotaAsync(accessToken, ct);
                var email = ExtractEmailFromFilename(file);
                results[email] = quota;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch quota for {File}", file);
            }
        }

        return results;
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
    Dictionary<string, ModelInfo> Models);

internal record ModelInfo(QuotaInfo? QuotaInfo);

internal record QuotaInfo(
    double? RemainingFraction,
    string? ResetTime);

internal record TokenRefreshResponse(
    [property: JsonProperty("access_token")] string AccessToken,
    [property: JsonProperty("expires_in")] int ExpiresIn);

internal record AntigravityAuthFile(
    [property: JsonProperty("access_token")] string AccessToken,
    string Email,
    string? Expired,
    [property: JsonProperty("expires_in")] int? ExpiresIn,
    [property: JsonProperty("refresh_token")] string? RefreshToken)
{
    public bool IsExpired => DateTime.TryParse(Expired, out var expiry) 
        && DateTime.UtcNow > expiry;
}
```

---

## Claude Code Quota Fetcher

### ClaudeCodeQuotaFetcher.cs
```csharp
namespace Quotio.Services.QuotaFetchers;

public class ClaudeCodeQuotaFetcher : BaseQuotaFetcher
{
    public override AIProvider Provider => AIProvider.Claude;

    public ClaudeCodeQuotaFetcher(HttpClient httpClient, ILogger<ClaudeCodeQuotaFetcher> logger)
        : base(httpClient, logger) { }

    public override async Task<ProviderQuotaData> FetchQuotaAsync(
        string accessToken, CancellationToken ct = default)
    {
        // Claude Code uses CLI to fetch quota
        var result = await ExecuteClaudeCliAsync("usage", ct);
        
        if (result == null)
            return new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true);

        var models = ParseClaudeUsageOutput(result);
        return new ProviderQuotaData(models, DateTime.UtcNow);
    }

    public override Task<string> RefreshTokenAsync(
        string refreshToken, CancellationToken ct = default)
    {
        // Claude Code handles token refresh internally
        throw new NotSupportedException("Claude Code handles token refresh internally");
    }

    private async Task<string?> ExecuteClaudeCliAsync(string args, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }

    private static List<ModelQuota> ParseClaudeUsageOutput(string output)
    {
        var models = new List<ModelQuota>();
        
        // Parse output like:
        // Weekly Usage: 45% remaining (resets in 3d 2h)
        // Sonnet Only: 80% remaining
        
        var lines = output.Split('\n');
        foreach (var line in lines)
        {
            var match = Regex.Match(line, @"(.+?):\s*(\d+(?:\.\d+)?)%\s*remaining");
            if (match.Success)
            {
                var name = match.Groups[1].Value.Trim().ToLower().Replace(" ", "-");
                var percentage = double.Parse(match.Groups[2].Value);
                models.Add(new ModelQuota(name, percentage, ""));
            }
        }

        return models;
    }
}
```

---

## GitHub Copilot Quota Fetcher

### CopilotQuotaFetcher.cs
```csharp
namespace Quotio.Services.QuotaFetchers;

public class CopilotQuotaFetcher : BaseQuotaFetcher
{
    private const string CopilotApiUrl = "https://api.github.com/copilot_internal/v2/token";
    
    public override AIProvider Provider => AIProvider.Copilot;

    public CopilotQuotaFetcher(HttpClient httpClient, ILogger<CopilotQuotaFetcher> logger)
        : base(httpClient, logger) { }

    public override async Task<ProviderQuotaData> FetchQuotaAsync(
        string accessToken, CancellationToken ct = default)
    {
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = $"token {accessToken}",
            ["Accept"] = "application/json",
            ["User-Agent"] = "Quotio/1.0"
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, CopilotApiUrl);
            foreach (var (key, value) in headers)
                request.Headers.TryAddWithoutValidation(key, value);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var data = JsonConvert.DeserializeObject<CopilotTokenResponse>(json);

            var models = new List<ModelQuota>();
            
            if (data?.ChatQuota != null)
            {
                var chatPct = CalculatePercentage(data.ChatQuota.Used, data.ChatQuota.Limit);
                models.Add(new ModelQuota("copilot-chat", chatPct, "", 
                    data.ChatQuota.Used, data.ChatQuota.Limit));
            }

            if (data?.CompletionsQuota != null)
            {
                var compPct = CalculatePercentage(
                    data.CompletionsQuota.Used, data.CompletionsQuota.Limit);
                models.Add(new ModelQuota("copilot-completions", compPct, "",
                    data.CompletionsQuota.Used, data.CompletionsQuota.Limit));
            }

            return new ProviderQuotaData(models, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Copilot quota");
            return new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true);
        }
    }

    public override async Task<string> RefreshTokenAsync(
        string refreshToken, CancellationToken ct = default)
    {
        // GitHub uses different refresh mechanism via device flow
        throw new NotSupportedException("Use device code flow for GitHub authentication");
    }

    private static double CalculatePercentage(int used, int limit)
    {
        if (limit <= 0) return 0;
        return Math.Max(0, 100.0 - (used * 100.0 / limit));
    }
}

internal record CopilotTokenResponse(
    CopilotQuota? ChatQuota,
    CopilotQuota? CompletionsQuota);

internal record CopilotQuota(int Used, int Limit);
```

---

## Other Fetchers Summary

| Fetcher | Source | Key Implementation Details |
|---------|--------|---------------------------|
| `CodexCLIQuotaFetcher` | `CodexCLIQuotaFetcher.swift` | Reads from `~/.codex/auth.json` |
| `GeminiCLIQuotaFetcher` | `GeminiCLIQuotaFetcher.swift` | Uses Google OAuth, simpler API |
| `CursorQuotaFetcher` | `CursorQuotaFetcher.swift` | Reads Cursor app's SQLite DB |
| `OpenAIQuotaFetcher` | `OpenAIQuotaFetcher.swift` | Standard OpenAI API |

---

## Verification

```powershell
cd c:\Users\Admin\Documents\quotio\QuotioWindows
dotnet build src/Quotio.Services
dotnet test tests/Quotio.Tests --filter "Category=QuotaFetchers"
```

---

## Unit Test Example

### AntigravityQuotaFetcherTests.cs
```csharp
public class AntigravityQuotaFetcherTests
{
    [Fact]
    public async Task FetchQuotaAsync_ValidToken_ReturnsQuotaData()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(ApiEndpoints.AntigravityQuotaApi)
            .Respond("application/json", """
            {
                "models": {
                    "gemini-3-pro-high": { "quotaInfo": { "remainingFraction": 0.75 } },
                    "claude-sonnet-4-5": { "quotaInfo": { "remainingFraction": 0.50 } }
                }
            }
            """);

        var httpClient = new HttpClient(mockHttp);
        var logger = Mock.Of<ILogger<AntigravityQuotaFetcher>>();
        var fetcher = new AntigravityQuotaFetcher(httpClient, logger);

        // Act
        var result = await fetcher.FetchQuotaAsync("test-token");

        // Assert
        result.Models.Should().HaveCount(2);
        result.Models.Should().Contain(m => m.Name == "gemini-3-pro-high" && m.Percentage == 75);
    }
}
```

---

## Next Phase

→ [04-core-services.md](04-core-services.md)
