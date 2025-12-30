using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;

namespace Quotio.App.ViewModels.Screens;

public partial class AboutViewModel : ObservableObject
{
    public string Version => "1.0.0-windows-preview";

    [RelayCommand]
    private void OpenGitHub()
    {
        Process.Start(new ProcessStartInfo("https://github.com/nguyenphutrong/quotio") { UseShellExecute = true });
    }
}
