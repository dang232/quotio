using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Quotio.Core.Enums;
using Quotio.Core.Models;

namespace Quotio.Services.QuotaFetchers;

public class ClaudeCodeQuotaFetcher : BaseQuotaFetcher
{
    public override AIProvider Provider => AIProvider.Claude;

    public ClaudeCodeQuotaFetcher(HttpClient httpClient, ILogger<ClaudeCodeQuotaFetcher> logger)
        : base(httpClient, logger) { }

    public override async Task<IEnumerable<ProviderQuotaData>> FetchQuotasAsync(CancellationToken ct = default)
    {
        // Claude Code uses CLI to fetch quota. Authentication is handled by the CLI tool itself.
        // We act as if there is one "default" account.
        
        var result = await ExecuteClaudeCliAsync("usage", ct);
        
        if (result == null)
            return new[] { new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true) { ProviderName = "Claude Code" } };

        var models = ParseClaudeUsageOutput(result);
        return new[] { new ProviderQuotaData(models, DateTime.UtcNow) { ProviderName = "Claude Code" } };
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
            // If claude is not installed or other error
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
