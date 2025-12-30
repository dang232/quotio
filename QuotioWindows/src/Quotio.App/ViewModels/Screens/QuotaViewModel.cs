using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Quotio.Core.Interfaces;
using Quotio.Core.Models;
using System.Collections.ObjectModel;

namespace Quotio.App.ViewModels.Screens;

public partial class QuotaViewModel : ObservableObject
{
    private readonly IEnumerable<IQuotaFetcher> _fetchers;

    [ObservableProperty]
    private ObservableCollection<ProviderQuotaData> _quotas = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ProviderQuotaData? _selectedQuota;

    public QuotaViewModel(IEnumerable<IQuotaFetcher> fetchers)
    {
        _fetchers = fetchers;
        LoadQuotasCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadQuotas()
    {
        IsLoading = true;
        Quotas.Clear();

        try
        {
            foreach (var fetcher in _fetchers)
            {
                try
                {
                    var accountQuotas = await fetcher.FetchQuotasAsync();
                    if (accountQuotas != null)
                    {
                        foreach (var data in accountQuotas)
                        {
                            Quotas.Add(data);
                        }
                    }
                }
                catch
                {
                    // Log error
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
