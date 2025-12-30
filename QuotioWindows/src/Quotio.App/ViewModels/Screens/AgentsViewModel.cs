using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quotio.Core.Interfaces;
using Quotio.Core.Models;
using System.Collections.ObjectModel;

namespace Quotio.App.ViewModels.Screens;

public partial class AgentsViewModel : ObservableObject
{
    private readonly IAgentDetectionService _detectionService;
    private readonly IAgentConfigurationService _configService;

    [ObservableProperty]
    private ObservableCollection<AgentInfo> _agents = new();

    [ObservableProperty]
    private bool _isLoading;

    public AgentsViewModel(IAgentDetectionService detectionService, IAgentConfigurationService configService)
    {
        _detectionService = detectionService;
        _configService = configService;
        LoadAgentsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadAgents()
    {
        IsLoading = true;
        try
        {
            var agents = await _detectionService.DetectInstalledAgentsAsync();
            Agents = new ObservableCollection<AgentInfo>(agents);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ConfigureAgent(AgentInfo agent)
    {
        // TODO: Implement configuration logic (backup config, write new config)
        // For now, this is a placeholder
        await Task.CompletedTask;
    }
}
