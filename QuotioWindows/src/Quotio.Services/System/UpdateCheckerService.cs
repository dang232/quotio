using System.Net.Http.Json;

namespace Quotio.Services.System;

public class UpdateCheckerService
{
    private readonly HttpClient _httpClient;
    private const string GitHubRepo = "nguyenphutrong/quotio";

    public UpdateCheckerService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Quotio-App");
    }

    public async Task<string?> CheckForUpdateAsync(string currentVersion)
    {
        try
        {
            // GitHub API to get latest release
            var url = $"https://api.github.com/repos/{GitHubRepo}/releases/latest";
            var release = await _httpClient.GetFromJsonAsync<GitHubRelease>(url);

            if (release != null && IsNewer(release.TagName, currentVersion))
            {
                return release.TagName; // Return new version string
            }
        }
        catch
        {
            // Ignore errors for update check
        }

        return null;
    }

    private bool IsNewer(string? tagName, string currentVersion)
    {
        if (string.IsNullOrEmpty(tagName)) return false;
        // Simple string comparison or SemVer parsing
        // Removing 'v' prefix if present
        var vTag = tagName.TrimStart('v');
        var vCurrent = currentVersion.TrimStart('v');
        return string.Compare(vTag, vCurrent, StringComparison.Ordinal) > 0;
    }

    private record GitHubRelease(string TagName, string HtmlUrl); // Simplified
}
