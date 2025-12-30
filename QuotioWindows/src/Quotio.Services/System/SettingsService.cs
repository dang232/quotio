using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quotio.Core.Constants;
using Quotio.Core.Models;

namespace Quotio.Services.System;

public class SettingsService
{
    private readonly string _settingsPath;
    private readonly ILogger<SettingsService> _logger;
    
    public AppSettings Current { get; private set; } = new();
    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        _settingsPath = AppConstants.SettingsPath;
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                Current = JsonConvert.DeserializeObject<AppSettings>(json) ?? new();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
                
            var json = JsonConvert.SerializeObject(Current, Formatting.Indented);
            File.WriteAllText(_settingsPath, json);
            
            SettingsChanged?.Invoke(this, Current);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
    }

    public void Update(Action<AppSettings> updateAction)
    {
        updateAction(Current);
        Save();
    }
}
