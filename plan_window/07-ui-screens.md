# Phase 7: UI Screens

> **Duration**: 7-10 days  
> **Goal**: Implement all 8 application screens

---

## Tasks

- [ ] DashboardView
- [ ] QuotaView
- [ ] ProvidersView
- [ ] AgentsView
- [ ] ApiKeysView
- [ ] LogsView
- [ ] SettingsView
- [ ] AboutView

---

## 1. Dashboard Screen

### DashboardView.xaml
```xml
<UserControl x:Class="Quotio.App.Views.Screens.DashboardView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="24">
            <!-- Header -->
            <TextBlock Text="Dashboard" 
                       Style="{StaticResource MaterialDesignHeadline4TextBlock}"
                       Margin="0,0,0,24"/>

            <!-- Stats Cards Row -->
            <UniformGrid Columns="4" Margin="0,0,0,24">
                <!-- Total Requests -->
                <materialDesign:Card Margin="8" Padding="16">
                    <StackPanel>
                        <TextBlock Text="Total Requests" 
                                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                                   Opacity="0.7"/>
                        <TextBlock Text="{Binding Stats.TotalRequests, StringFormat='{}{0:N0}'}" 
                                   Style="{StaticResource MaterialDesignHeadline4TextBlock}"
                                   Margin="0,8,0,0"/>
                    </StackPanel>
                </materialDesign:Card>

                <!-- Success Rate -->
                <materialDesign:Card Margin="8" Padding="16">
                    <StackPanel>
                        <TextBlock Text="Success Rate" 
                                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                                   Opacity="0.7"/>
                        <TextBlock Text="{Binding Stats.SuccessRate, StringFormat='{}{0:F1}%'}" 
                                   Style="{StaticResource MaterialDesignHeadline4TextBlock}"
                                   Foreground="{StaticResource QuotaHighBrush}"
                                   Margin="0,8,0,0"/>
                    </StackPanel>
                </materialDesign:Card>

                <!-- Active Accounts -->
                <materialDesign:Card Margin="8" Padding="16">
                    <StackPanel>
                        <TextBlock Text="Active Accounts" 
                                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                                   Opacity="0.7"/>
                        <TextBlock Text="{Binding ActiveAccountsCount}" 
                                   Style="{StaticResource MaterialDesignHeadline4TextBlock}"
                                   Margin="0,8,0,0"/>
                    </StackPanel>
                </materialDesign:Card>

                <!-- Uptime -->
                <materialDesign:Card Margin="8" Padding="16">
                    <StackPanel>
                        <TextBlock Text="Uptime" 
                                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                                   Opacity="0.7"/>
                        <TextBlock Text="{Binding FormattedUptime}" 
                                   Style="{StaticResource MaterialDesignHeadline4TextBlock}"
                                   Margin="0,8,0,0"/>
                    </StackPanel>
                </materialDesign:Card>
            </UniformGrid>

            <!-- Quota Overview -->
            <TextBlock Text="Quota Overview" 
                       Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                       Margin="0,0,0,16"/>
            
            <ItemsControl ItemsSource="{Binding ProviderQuotas}">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <UniformGrid Columns="3"/>
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <controls:QuotaCardControl Margin="8"/>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

### DashboardViewModel.cs
```csharp
namespace Quotio.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ManagementApiClient _apiClient;
    private readonly QuotaService _quotaService;
    private readonly CLIProxyManager _proxyManager;

    [ObservableProperty]
    private ProxyStats? _stats;

    [ObservableProperty]
    private int _activeAccountsCount;

    public ObservableCollection<ProviderQuotaSummary> ProviderQuotas { get; } = new();

    public string FormattedUptime => _proxyManager.Status.Uptime?.ToString(@"d\.hh\:mm\:ss") ?? "--:--:--";

    public DashboardViewModel(
        ManagementApiClient apiClient,
        QuotaService quotaService,
        CLIProxyManager proxyManager)
    {
        _apiClient = apiClient;
        _quotaService = quotaService;
        _proxyManager = proxyManager;

        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Stats = await _apiClient.GetStatsAsync();
        
        var accounts = await _apiClient.GetAccountsAsync();
        ActiveAccountsCount = accounts.Count(a => a.IsActive);

        await RefreshQuotasAsync();
    }

    private async Task RefreshQuotasAsync()
    {
        ProviderQuotas.Clear();
        
        foreach (AIProvider provider in Enum.GetValues<AIProvider>())
        {
            if (!provider.SupportsQuotaTracking()) continue;
            
            var quotas = await _quotaService.GetQuotasForProviderAsync(provider);
            if (quotas.Any())
            {
                ProviderQuotas.Add(new ProviderQuotaSummary(
                    provider,
                    quotas.Min(q => q.LowestPercentage),
                    quotas.Count));
            }
        }
    }
}

public record ProviderQuotaSummary(AIProvider Provider, double LowestPercentage, int AccountCount);
```

---

## 2. Quota Screen

### QuotaView.xaml
```xml
<UserControl x:Class="Quotio.App.Views.Screens.QuotaView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    
    <DockPanel Margin="24">
        <!-- Header -->
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,24">
            <TextBlock Text="Quota" 
                       Style="{StaticResource MaterialDesignHeadline4TextBlock}"/>
            <Button Content="Refresh" 
                    Command="{Binding RefreshCommand}"
                    Margin="24,0,0,0"
                    Style="{StaticResource MaterialDesignOutlinedButton}"/>
        </StackPanel>

        <!-- Provider Tabs -->
        <TabControl ItemsSource="{Binding Providers}"
                    SelectedItem="{Binding SelectedProvider}">
            <TabControl.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <controls:ProviderIconControl Provider="{Binding}" Width="20" Height="20"/>
                        <TextBlock Text="{Binding, Converter={StaticResource ProviderToNameConverter}}"
                                   Margin="8,0,0,0"/>
                    </StackPanel>
                </DataTemplate>
            </TabControl.ItemTemplate>
            <TabControl.ContentTemplate>
                <DataTemplate>
                    <ScrollViewer VerticalScrollBarVisibility="Auto">
                        <ItemsControl ItemsSource="{Binding DataContext.AccountQuotas, 
                                                    RelativeSource={RelativeSource AncestorType=TabControl}}">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <controls:QuotaCardControl Margin="0,0,0,16"/>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </ScrollViewer>
                </DataTemplate>
            </TabControl.ContentTemplate>
        </TabControl>
    </DockPanel>
</UserControl>
```

---

## 3. Providers Screen

### ProvidersView.xaml
```xml
<UserControl x:Class="Quotio.App.Views.Screens.ProvidersView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    
    <DockPanel Margin="24">
        <!-- Header -->
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,24">
            <TextBlock Text="Providers" 
                       Style="{StaticResource MaterialDesignHeadline4TextBlock}"/>
            <Button Content="Add Account" 
                    Command="{Binding AddAccountCommand}"
                    Margin="24,0,0,0"
                    Style="{StaticResource MaterialDesignRaisedButton}">
                <Button.ContentTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal">
                            <materialDesign:PackIcon Kind="Plus"/>
                            <TextBlock Text="Add Account" Margin="8,0,0,0"/>
                        </StackPanel>
                    </DataTemplate>
                </Button.ContentTemplate>
            </Button>
        </StackPanel>

        <!-- Provider Grid -->
        <ScrollViewer VerticalScrollBarVisibility="Auto">
            <ItemsControl ItemsSource="{Binding ProviderGroups}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Expander IsExpanded="True" Margin="0,0,0,16">
                            <Expander.Header>
                                <StackPanel Orientation="Horizontal">
                                    <controls:ProviderIconControl Provider="{Binding Provider}" 
                                                                   Width="24" Height="24"/>
                                    <TextBlock Text="{Binding Provider, Converter={StaticResource ProviderToNameConverter}}"
                                               Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                                               Margin="12,0,0,0"/>
                                    <TextBlock Text="{Binding Accounts.Count, StringFormat='({0})'}"
                                               Opacity="0.5"
                                               Margin="8,0,0,0"
                                               VerticalAlignment="Center"/>
                                </StackPanel>
                            </Expander.Header>
                            
                            <ItemsControl ItemsSource="{Binding Accounts}" Margin="36,8,0,0">
                                <ItemsControl.ItemTemplate>
                                    <DataTemplate>
                                        <Border Background="{DynamicResource MaterialDesignCardBackground}"
                                                CornerRadius="8"
                                                Padding="16"
                                                Margin="0,0,0,8">
                                            <Grid>
                                                <Grid.ColumnDefinitions>
                                                    <ColumnDefinition Width="*"/>
                                                    <ColumnDefinition Width="Auto"/>
                                                </Grid.ColumnDefinitions>
                                                
                                                <StackPanel>
                                                    <TextBlock Text="{Binding Email}"
                                                               Style="{StaticResource MaterialDesignBody1TextBlock}"/>
                                                    <TextBlock Text="{Binding PlanType}"
                                                               Style="{StaticResource MaterialDesignCaptionTextBlock}"
                                                               Opacity="0.7"/>
                                                </StackPanel>
                                                
                                                <StackPanel Grid.Column="1" Orientation="Horizontal">
                                                    <Button Content="Remove"
                                                            Command="{Binding DataContext.RemoveAccountCommand, 
                                                                      RelativeSource={RelativeSource AncestorType=UserControl}}"
                                                            CommandParameter="{Binding}"
                                                            Style="{StaticResource MaterialDesignFlatButton}"
                                                            Foreground="{StaticResource QuotaLowBrush}"/>
                                                </StackPanel>
                                            </Grid>
                                        </Border>
                                    </DataTemplate>
                                </ItemsControl.ItemTemplate>
                            </ItemsControl>
                        </Expander>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>
    </DockPanel>
</UserControl>
```

---

## 4. Agents Screen

### AgentsView.xaml
```xml
<UserControl x:Class="Quotio.App.Views.Screens.AgentsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    
    <DockPanel Margin="24">
        <TextBlock DockPanel.Dock="Top" 
                   Text="Agents" 
                   Style="{StaticResource MaterialDesignHeadline4TextBlock}"
                   Margin="0,0,0,24"/>

        <ScrollViewer VerticalScrollBarVisibility="Auto">
            <ItemsControl ItemsSource="{Binding Agents}">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <UniformGrid Columns="2"/>
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <controls:AgentCardControl Margin="8"/>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>
    </DockPanel>
</UserControl>
```

---

## 5. Settings Screen

### SettingsView.xaml
```xml
<UserControl x:Class="Quotio.App.Views.Screens.SettingsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="24" MaxWidth="600">
            <TextBlock Text="Settings" 
                       Style="{StaticResource MaterialDesignHeadline4TextBlock}"
                       Margin="0,0,0,24"/>

            <!-- General Section -->
            <TextBlock Text="General" 
                       Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                       Margin="0,0,0,16"/>
            
            <materialDesign:Card Padding="16" Margin="0,0,0,24">
                <StackPanel>
                    <!-- App Mode -->
                    <StackPanel Margin="0,0,0,16">
                        <TextBlock Text="App Mode" 
                                   Style="{StaticResource MaterialDesignBody1TextBlock}"/>
                        <ComboBox ItemsSource="{Binding AppModes}"
                                  SelectedItem="{Binding Settings.AppMode}"
                                  Margin="0,8,0,0"/>
                    </StackPanel>
                    
                    <!-- Language -->
                    <StackPanel Margin="0,0,0,16">
                        <TextBlock Text="Language" 
                                   Style="{StaticResource MaterialDesignBody1TextBlock}"/>
                        <ComboBox ItemsSource="{Binding Languages}"
                                  SelectedItem="{Binding Settings.Language}"
                                  Margin="0,8,0,0"/>
                    </StackPanel>
                    
                    <!-- Theme -->
                    <StackPanel>
                        <TextBlock Text="Theme" 
                                   Style="{StaticResource MaterialDesignBody1TextBlock}"/>
                        <ComboBox ItemsSource="{Binding Themes}"
                                  SelectedItem="{Binding Settings.Theme}"
                                  Margin="0,8,0,0"/>
                    </StackPanel>
                </StackPanel>
            </materialDesign:Card>

            <!-- Proxy Section -->
            <TextBlock Text="Proxy" 
                       Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                       Margin="0,0,0,16"/>
            
            <materialDesign:Card Padding="16" Margin="0,0,0,24">
                <StackPanel>
                    <!-- Port -->
                    <StackPanel Margin="0,0,0,16">
                        <TextBlock Text="Port" 
                                   Style="{StaticResource MaterialDesignBody1TextBlock}"/>
                        <TextBox Text="{Binding Settings.ProxyPort}"
                                 Margin="0,8,0,0"/>
                    </StackPanel>
                    
                    <!-- Routing Strategy -->
                    <StackPanel Margin="0,0,0,16">
                        <TextBlock Text="Routing Strategy" 
                                   Style="{StaticResource MaterialDesignBody1TextBlock}"/>
                        <ComboBox ItemsSource="{Binding RoutingStrategies}"
                                  SelectedItem="{Binding Settings.RoutingStrategy}"
                                  Margin="0,8,0,0"/>
                    </StackPanel>
                    
                    <!-- Auto Start -->
                    <CheckBox Content="Auto-start proxy on launch"
                              IsChecked="{Binding Settings.AutoStartProxy}"/>
                </StackPanel>
            </materialDesign:Card>

            <!-- Notifications Section -->
            <TextBlock Text="Notifications" 
                       Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                       Margin="0,0,0,16"/>
            
            <materialDesign:Card Padding="16" Margin="0,0,0,24">
                <StackPanel>
                    <CheckBox Content="Low quota warnings"
                              IsChecked="{Binding Settings.EnableLowQuotaWarnings}"/>
                    
                    <StackPanel Orientation="Horizontal" Margin="0,16,0,0">
                        <TextBlock Text="Warning threshold:" VerticalAlignment="Center"/>
                        <TextBox Text="{Binding Settings.LowQuotaThreshold}"
                                 Width="60"
                                 Margin="8,0,0,0"/>
                        <TextBlock Text="%" VerticalAlignment="Center" Margin="4,0,0,0"/>
                    </StackPanel>
                </StackPanel>
            </materialDesign:Card>

            <!-- Save Button -->
            <Button Content="Save Settings"
                    Command="{Binding SaveCommand}"
                    HorizontalAlignment="Left"
                    Style="{StaticResource MaterialDesignRaisedButton}"/>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

---

## Screen Summary

| Screen | ViewModel | Key Features |
|--------|-----------|--------------|
| Dashboard | DashboardViewModel | Stats cards, quota overview |
| Quota | QuotaViewModel | Provider tabs, model quotas |
| Providers | ProvidersViewModel | Account management, OAuth |
| Agents | AgentsViewModel | Agent detection, configuration |
| API Keys | ApiKeysViewModel | Key management, copy to clipboard |
| Logs | LogsViewModel | Request/response logs |
| Settings | SettingsViewModel | App configuration |
| About | AboutViewModel | App info, donation links |

---

## Verification

```powershell
cd c:\Users\Admin\Documents\quotio\QuotioWindows
dotnet build src/Quotio.App
dotnet run --project src/Quotio.App
```

- Navigate through all screens
- Verify data binding works
- Check theme switching

---

## Next Phase

→ [08-system-integration.md](08-system-integration.md)
