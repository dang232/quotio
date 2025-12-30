using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quotio.Services.System;

namespace Quotio.App.ViewModels.Screens;

public partial class ApiKeysViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private string? _openAiApiKey;

    [ObservableProperty]
    private string? _geminiApiKey;

    [ObservableProperty]
    private string? _claudeApiKey;

    public ApiKeysViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadKeys();
    }

    private void LoadKeys()
    {
        OpenAiApiKey = _settingsService.Current.OpenAiApiKey;
        // Assume we add others to AppSettings later, for now just OpenAI
    }

    [RelayCommand]
    private void SaveKeys()
    {
        _settingsService.Update(s =>
        {
            s.OpenAiApiKey = OpenAiApiKey;
        });
        
        // Notify or Toast
    }
}
