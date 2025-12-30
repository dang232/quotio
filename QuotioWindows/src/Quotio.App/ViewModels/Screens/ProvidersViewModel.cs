using CommunityToolkit.Mvvm.ComponentModel;
using Quotio.Core.Enums;
using System.Collections.ObjectModel;

namespace Quotio.App.ViewModels.Screens;

public partial class ProvidersViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ProviderItem> _providers = new();

    public ProvidersViewModel()
    {
        LoadProviders();
    }

    private void LoadProviders()
    {
        // Mock data matching known providers
        Providers = new ObservableCollection<ProviderItem>
        {
            new("Antigravity", "Antigravity Cloud", AIProvider.Antigravity, true, "Managed via local auth files"),
            new("OpenAI", "Direct API", AIProvider.Codex, false, "Requires API Key in settings"),
            new("GitHub Copilot", "VS Code Extension", AIProvider.Copilot, false, "Auto-detected from VS Code"),
            new("Claude Code", "Anthropic CLI", AIProvider.Claude, false, "Uses local 'claude' CLI auth"),
            new("Cursor", "IDE Integration", AIProvider.Cursor, false, "Reads local Cursor database"),
            new("Gemini CLI", "Google AI", AIProvider.Gemini, false, "Uses environment variables")
        };
    }
}

public class ProviderItem
{
    public string Name { get; }
    public string Description { get; }
    public AIProvider Type { get; }
    public bool IsConfigured { get; set; }
    public string StatusMessage { get; }

    public ProviderItem(string name, string description, AIProvider type, bool isConfigured, string statusMessage)
    {
        Name = name;
        Description = description;
        Type = type;
        IsConfigured = isConfigured;
        StatusMessage = statusMessage;
    }
}
