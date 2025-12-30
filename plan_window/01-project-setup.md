# Phase 1: Project Setup

> **Duration**: 2-3 days  
> **Goal**: Create .NET 8 WPF solution with proper structure

---

## Tasks

- [ ] Create solution and projects
- [ ] Configure project references
- [ ] Add NuGet packages
- [ ] Setup folder structure
- [ ] Create initial App.xaml configuration

---

## Step 1: Create Solution

```powershell
# Navigate to quotio folder
cd c:\Users\Admin\Documents\quotio

# Create solution folder
mkdir QuotioWindows
cd QuotioWindows

# Create solution
dotnet new sln -n Quotio

# Create projects
dotnet new wpf -n Quotio.App -o src/Quotio.App -f net8.0-windows
dotnet new classlib -n Quotio.Core -o src/Quotio.Core -f net8.0
dotnet new classlib -n Quotio.Services -o src/Quotio.Services -f net8.0-windows
dotnet new xunit -n Quotio.Tests -o tests/Quotio.Tests -f net8.0

# Add projects to solution
dotnet sln add src/Quotio.App/Quotio.App.csproj
dotnet sln add src/Quotio.Core/Quotio.Core.csproj
dotnet sln add src/Quotio.Services/Quotio.Services.csproj
dotnet sln add tests/Quotio.Tests/Quotio.Tests.csproj

# Add project references
dotnet add src/Quotio.App reference src/Quotio.Core src/Quotio.Services
dotnet add src/Quotio.Services reference src/Quotio.Core
dotnet add tests/Quotio.Tests reference src/Quotio.Core src/Quotio.Services
```

---

## Step 2: Add NuGet Packages

```powershell
cd c:\Users\Admin\Documents\quotio\QuotioWindows

# Quotio.App packages
dotnet add src/Quotio.App package CommunityToolkit.Mvvm --version 8.2.2
dotnet add src/Quotio.App package Hardcodet.NotifyIcon.Wpf --version 1.1.0
dotnet add src/Quotio.App package MaterialDesignThemes --version 5.0.0
dotnet add src/Quotio.App package ModernWpfUI --version 0.9.6

# Quotio.Services packages
dotnet add src/Quotio.Services package Newtonsoft.Json --version 13.0.3
dotnet add src/Quotio.Services package Microsoft.Toolkit.Uwp.Notifications --version 7.1.3

# Quotio.Core packages
dotnet add src/Quotio.Core package Newtonsoft.Json --version 13.0.3

# Test packages
dotnet add tests/Quotio.Tests package Moq --version 4.20.70
dotnet add tests/Quotio.Tests package FluentAssertions --version 6.12.0
```

---

## Step 3: Create Folder Structure

### Quotio.App Structure
```
src/Quotio.App/
├── Views/
│   ├── MainWindow.xaml
│   ├── Screens/
│   │   ├── DashboardView.xaml
│   │   ├── QuotaView.xaml
│   │   ├── ProvidersView.xaml
│   │   ├── AgentsView.xaml
│   │   ├── ApiKeysView.xaml
│   │   ├── LogsView.xaml
│   │   ├── SettingsView.xaml
│   │   └── AboutView.xaml
│   └── Dialogs/
│       ├── AgentConfigDialog.xaml
│       └── ProviderAuthDialog.xaml
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── QuotaViewModel.cs
│   ├── ProvidersViewModel.cs
│   ├── AgentsViewModel.cs
│   ├── ApiKeysViewModel.cs
│   ├── LogsViewModel.cs
│   └── SettingsViewModel.cs
├── Controls/
│   ├── SidebarControl.xaml
│   ├── QuotaCardControl.xaml
│   ├── QuotaProgressBar.xaml
│   ├── AgentCardControl.xaml
│   └── ProviderIconControl.xaml
├── Resources/
│   ├── Styles/
│   │   ├── Colors.xaml
│   │   ├── Buttons.xaml
│   │   └── Cards.xaml
│   ├── Themes/
│   │   ├── Light.xaml
│   │   └── Dark.xaml
│   └── Icons/
│       └── (provider icons)
├── Converters/
│   ├── BoolToVisibilityConverter.cs
│   ├── PercentageToColorConverter.cs
│   └── ProviderToIconConverter.cs
├── App.xaml
└── App.xaml.cs
```

### Quotio.Core Structure
```
src/Quotio.Core/
├── Models/
│   ├── ModelQuota.cs
│   ├── ProviderQuotaData.cs
│   ├── ProviderAccount.cs
│   ├── AgentInfo.cs
│   ├── AgentConfig.cs
│   └── ProxyStatus.cs
├── Enums/
│   ├── AIProvider.cs
│   ├── AgentType.cs
│   ├── RoutingStrategy.cs
│   └── AppMode.cs
├── Interfaces/
│   ├── IQuotaFetcher.cs
│   ├── IProxyManager.cs
│   ├── IAgentService.cs
│   └── INotificationService.cs
└── Constants/
    ├── ApiEndpoints.cs
    └── AppConstants.cs
```

### Quotio.Services Structure
```
src/Quotio.Services/
├── QuotaFetchers/
│   ├── AntigravityQuotaFetcher.cs
│   ├── ClaudeCodeQuotaFetcher.cs
│   ├── CodexCLIQuotaFetcher.cs
│   ├── GeminiCLIQuotaFetcher.cs
│   ├── CopilotQuotaFetcher.cs
│   ├── CursorQuotaFetcher.cs
│   └── OpenAIQuotaFetcher.cs
├── Proxy/
│   ├── CLIProxyManager.cs
│   └── ManagementApiClient.cs
├── Agents/
│   ├── AgentDetectionService.cs
│   ├── AgentConfigurationService.cs
│   └── ShellProfileManager.cs
├── System/
│   ├── NotificationManager.cs
│   ├── SystemTrayManager.cs
│   ├── SettingsService.cs
│   └── UpdateService.cs
└── Auth/
    ├── OAuthService.cs
    └── DirectAuthFileService.cs
```

---

## Step 4: Create Initial Files

### App.xaml
```xml
<Application x:Class="Quotio.App.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             StartupUri="Views/MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <materialDesign:BundledTheme BaseTheme="Dark" 
                                              PrimaryColor="DeepPurple" 
                                              SecondaryColor="Lime" />
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml" />
                <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### App.xaml.cs
```csharp
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Quotio.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<QuotaViewModel>();
        services.AddTransient<ProvidersViewModel>();
        
        // Services
        services.AddSingleton<CLIProxyManager>();
        services.AddSingleton<NotificationManager>();
        services.AddSingleton<SettingsService>();
        
        // Quota Fetchers
        services.AddSingleton<AntigravityQuotaFetcher>();
        services.AddSingleton<ClaudeCodeQuotaFetcher>();
        services.AddSingleton<CodexCLIQuotaFetcher>();
    }
}
```

---

## Verification

```powershell
# Build solution
cd c:\Users\Admin\Documents\quotio\QuotioWindows
dotnet build

# Run app (should show empty window)
dotnet run --project src/Quotio.App

# Run tests
dotnet test
```

---

## Deliverables

- [x] Solution file `Quotio.sln`
- [x] 4 projects with references
- [x] NuGet packages installed
- [x] Folder structure created
- [x] App.xaml with Material Design theme
- [x] Dependency injection setup

---

## Next Phase

→ [02-core-models.md](02-core-models.md)
