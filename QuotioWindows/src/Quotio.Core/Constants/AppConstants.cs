namespace Quotio.Core.Constants;

public static class AppConstants
{
    public const string AppName = "Quotio";
    public const string AppVersion = "1.0.0";
    public const int DefaultProxyPort = 8317;
    public const int QuotaRefreshIntervalSeconds = 15;
    public const int TokenRefreshBufferMinutes = 5;
    
    public const string AntigravityClientId = "1071006060591-tmhssin2h21lcre235vtolojh4g403ep.apps.googleusercontent.com";
    public const string AntigravityUserAgent = "antigravity/1.11.3 Windows/x64";
    
    public static string AuthDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".cli-proxy-api"
    );
    
    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Quotio", "settings.json"
    );
    
    public static string LogsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Quotio", "logs"
    );
    
    public static string BinaryDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Quotio", "bin"
    );
}
