using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using Quotio.Core.Constants;

namespace Quotio.App.ViewModels.Screens;

public partial class AboutViewModel : ObservableObject
{
    public string Version => $"{AppConstants.AppVersion}-windows-preview";

    [RelayCommand]
    private void OpenGitHub()
    {
        Process.Start(new ProcessStartInfo("https://github.com/nguyenphutrong/quotio") { UseShellExecute = true });
    }
}
