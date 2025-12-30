using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Quotio.Core.Enums;
using Quotio.Core.Models;

namespace Quotio.Services.QuotaFetchers;

public class CursorQuotaFetcher : BaseQuotaFetcher
{
    public override AIProvider Provider => AIProvider.Cursor;

    public CursorQuotaFetcher(HttpClient httpClient, ILogger<CursorQuotaFetcher> logger)
        : base(httpClient, logger) { }

    public override async Task<IEnumerable<ProviderQuotaData>> FetchQuotasAsync(CancellationToken ct = default)
    {
        // Try to find Cursor state.vscdb
        var dbPath = GetCursorDatabasePath();
        
        if (dbPath == null || !File.Exists(dbPath))
        {
             return new[] { new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true) { ProviderName = "Cursor", AccountName = "Not Found" } };
        }
        
        // This is a simplified example. In reality, Cursor stores auth token in specific keys in the SQLite DB
        // and usage is fetched via API using the token found in 'cursorAuth/accessToken'
        
        try
        {
            var accessToken = await ExtractAccessTokenFromDbAsync(dbPath, ct);
            
            if (string.IsNullOrEmpty(accessToken))
                 return new[] { new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true) { ProviderName = "Cursor", AccountName = "No Token" } };

             // Fetch actual quota from Cursor API using token (hypothetical endpoint)
             // var quota = await FetchFromApi(accessToken);
             
             // For now, return connected state
             return new[] 
            { 
                new ProviderQuotaData(
                    new List<ModelQuota> { new ModelQuota("cursor-cpp", 100, "Unknown") }, 
                    DateTime.UtcNow) 
                { 
                    ProviderName = "Cursor",
                    AccountName = "Connected"
                } 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read Cursor database");
             return new[] { new ProviderQuotaData(new List<ModelQuota>(), DateTime.UtcNow, true) { ProviderName = "Cursor", AccountName = "Error" } };
        }
    }

    private string? GetCursorDatabasePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        // Typical path: %APPDATA%\Cursor\User\globalStorage\state.vscdb
        var path = Path.Combine(appData, "Cursor", "User", "globalStorage", "state.vscdb");
        return File.Exists(path) ? path : null;
    }

    private async Task<string?> ExtractAccessTokenFromDbAsync(string dbPath, CancellationToken ct)
    {
        // Copy DB to temp file to avoid locks
        var tempPath = Path.GetTempFileName();
        File.Copy(dbPath, tempPath, true);
        
        try
        {
            using var connection = new SqliteConnection($"Data Source={tempPath}");
            await connection.OpenAsync(ct);
            
            var command = connection.CreateCommand();
            command.CommandText = "SELECT value FROM ItemTable WHERE key = 'cursorAuth/accessToken'";
            
            var result = await command.ExecuteScalarAsync(ct);
            return result?.ToString();
        }
        finally
        {
            try { File.Delete(tempPath); } catch { }
        }
    }
}
