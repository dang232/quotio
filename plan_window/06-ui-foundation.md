# Phase 6: UI Foundation

> **Duration**: 3-4 days  
> **Goal**: Create MainWindow, navigation, themes, and base layout

---

## Tasks

- [ ] MainWindow with sidebar navigation
- [ ] Theme system (Light/Dark)
- [ ] Color resources
- [ ] Base ViewModels

---

## MainWindow

### MainWindow.xaml
```xml
<Window x:Class="Quotio.App.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
        xmlns:screens="clr-namespace:Quotio.App.Views.Screens"
        xmlns:controls="clr-namespace:Quotio.App.Controls"
        mc:Ignorable="d"
        Title="Quotio" 
        Height="700" Width="1000"
        MinHeight="500" MinWidth="800"
        WindowStartupLocation="CenterScreen"
        Style="{StaticResource MaterialDesignWindow}">
    
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="220"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <!-- Sidebar -->
        <controls:SidebarControl x:Name="Sidebar"
                                  Grid.Column="0"
                                  SelectedPage="{Binding CurrentPage, Mode=TwoWay}"
                                  ProxyStatus="{Binding ProxyStatus}"/>

        <!-- Content Area -->
        <Border Grid.Column="1" 
                Background="{DynamicResource MaterialDesignPaper}">
            <ContentControl Content="{Binding CurrentPage}">
                <ContentControl.Resources>
                    <DataTemplate DataType="{x:Type screens:DashboardViewModel}">
                        <screens:DashboardView/>
                    </DataTemplate>
                    <DataTemplate DataType="{x:Type screens:QuotaViewModel}">
                        <screens:QuotaView/>
                    </DataTemplate>
                    <DataTemplate DataType="{x:Type screens:ProvidersViewModel}">
                        <screens:ProvidersView/>
                    </DataTemplate>
                    <DataTemplate DataType="{x:Type screens:AgentsViewModel}">
                        <screens:AgentsView/>
                    </DataTemplate>
                    <DataTemplate DataType="{x:Type screens:ApiKeysViewModel}">
                        <screens:ApiKeysView/>
                    </DataTemplate>
                    <DataTemplate DataType="{x:Type screens:LogsViewModel}">
                        <screens:LogsView/>
                    </DataTemplate>
                    <DataTemplate DataType="{x:Type screens:SettingsViewModel}">
                        <screens:SettingsView/>
                    </DataTemplate>
                    <DataTemplate DataType="{x:Type screens:AboutViewModel}">
                        <screens:AboutView/>
                    </DataTemplate>
                </ContentControl.Resources>
            </ContentControl>
        </Border>
    </Grid>
</Window>
```

### MainWindow.xaml.cs
```csharp
namespace Quotio.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var settings = App.Services.GetRequiredService<SettingsService>();
        
        if (settings.Current.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        
        base.OnClosing(e);
    }
}
```

---

## Sidebar Control

### SidebarControl.xaml
```xml
<UserControl x:Class="Quotio.App.Controls.SidebarControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    
    <Border Background="{DynamicResource MaterialDesignCardBackground}">
        <DockPanel>
            <!-- Header -->
            <StackPanel DockPanel.Dock="Top" Margin="16">
                <TextBlock Text="Quotio" 
                           Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                           FontWeight="Bold"/>
                
                <!-- Status Panel -->
                <Border Margin="0,16,0,0" 
                        Padding="12" 
                        CornerRadius="8"
                        Background="{DynamicResource MaterialDesignToolBarBackground}">
                    <StackPanel>
                        <StackPanel Orientation="Horizontal">
                            <Ellipse Width="10" Height="10" 
                                     Fill="{Binding ProxyStatus.IsRunning, Converter={StaticResource StatusToColorConverter}}"/>
                            <TextBlock Text="{Binding ProxyStatus.IsRunning, Converter={StaticResource StatusToTextConverter}}"
                                       Margin="8,0,0,0"
                                       Style="{StaticResource MaterialDesignBody2TextBlock}"/>
                        </StackPanel>
                        <TextBlock Text="{Binding ProxyStatus.Port, StringFormat='Port: {0}'}"
                                   Margin="0,4,0,0"
                                   Style="{StaticResource MaterialDesignCaptionTextBlock}"
                                   Opacity="0.7"/>
                    </StackPanel>
                </Border>
            </StackPanel>

            <!-- Navigation -->
            <ListBox DockPanel.Dock="Top"
                     ItemsSource="{Binding NavigationItems}"
                     SelectedValue="{Binding SelectedPage, Mode=TwoWay}"
                     SelectedValuePath="Page"
                     Style="{StaticResource MaterialDesignNavigationListBox}">
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal" Margin="8,4">
                            <materialDesign:PackIcon Kind="{Binding Icon}" 
                                                     Width="24" Height="24"
                                                     VerticalAlignment="Center"/>
                            <TextBlock Text="{Binding Title}" 
                                       Margin="16,0,0,0"
                                       VerticalAlignment="Center"/>
                        </StackPanel>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>

            <!-- Spacer -->
            <Border/>

            <!-- Start/Stop Button -->
            <Button DockPanel.Dock="Bottom"
                    Margin="16"
                    Command="{Binding ToggleProxyCommand}"
                    Style="{StaticResource MaterialDesignRaisedButton}">
                <StackPanel Orientation="Horizontal">
                    <materialDesign:PackIcon Kind="{Binding ProxyStatus.IsRunning, Converter={StaticResource RunningToIconConverter}}"/>
                    <TextBlock Text="{Binding ProxyStatus.IsRunning, Converter={StaticResource RunningToButtonTextConverter}}"
                               Margin="8,0,0,0"/>
                </StackPanel>
            </Button>
        </DockPanel>
    </Border>
</UserControl>
```

---

## MainViewModel

### MainViewModel.cs
```csharp
namespace Quotio.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly CLIProxyManager _proxyManager;
    private readonly SettingsService _settings;

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private ProxyStatus _proxyStatus = new(false, 8317);

    public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

    public MainViewModel(
        CLIProxyManager proxyManager,
        SettingsService settings,
        DashboardViewModel dashboardVm)
    {
        _proxyManager = proxyManager;
        _settings = settings;

        InitializeNavigation();
        
        _proxyManager.StatusChanged += (_, status) => ProxyStatus = status;
        CurrentPage = dashboardVm;
    }

    private void InitializeNavigation()
    {
        var mode = _settings.Current.AppMode;
        
        NavigationItems.Add(new("Dashboard", PackIconKind.ViewDashboard, typeof(DashboardViewModel)));
        NavigationItems.Add(new("Quota", PackIconKind.ChartBar, typeof(QuotaViewModel)));
        
        if (mode == AppMode.Full)
        {
            NavigationItems.Add(new("Providers", PackIconKind.CloudOutline, typeof(ProvidersViewModel)));
            NavigationItems.Add(new("Agents", PackIconKind.Robot, typeof(AgentsViewModel)));
            NavigationItems.Add(new("API Keys", PackIconKind.Key, typeof(ApiKeysViewModel)));
            NavigationItems.Add(new("Logs", PackIconKind.FileDocumentOutline, typeof(LogsViewModel)));
        }
        else
        {
            NavigationItems.Add(new("Accounts", PackIconKind.AccountMultiple, typeof(ProvidersViewModel)));
        }
        
        NavigationItems.Add(new("Settings", PackIconKind.Cog, typeof(SettingsViewModel)));
        NavigationItems.Add(new("About", PackIconKind.InformationOutline, typeof(AboutViewModel)));
    }

    [RelayCommand]
    private async Task ToggleProxyAsync()
    {
        if (ProxyStatus.IsRunning)
            await _proxyManager.StopAsync();
        else
            await _proxyManager.StartAsync();
    }

    public void NavigateTo(Type viewModelType)
    {
        CurrentPage = App.Services.GetRequiredService(viewModelType);
    }
}

public record NavigationItem(string Title, PackIconKind Icon, Type ViewModelType);
```

---

## Theme Resources

### Colors.xaml
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- Status Colors -->
    <SolidColorBrush x:Key="StatusRunningBrush" Color="#4CAF50"/>
    <SolidColorBrush x:Key="StatusStoppedBrush" Color="#F44336"/>
    <SolidColorBrush x:Key="StatusWarningBrush" Color="#FF9800"/>
    
    <!-- Quota Colors -->
    <SolidColorBrush x:Key="QuotaHighBrush" Color="#4CAF50"/>
    <SolidColorBrush x:Key="QuotaMediumBrush" Color="#FF9800"/>
    <SolidColorBrush x:Key="QuotaLowBrush" Color="#F44336"/>
    <SolidColorBrush x:Key="QuotaCriticalBrush" Color="#D32F2F"/>
    
    <!-- Provider Colors -->
    <SolidColorBrush x:Key="GeminiBrush" Color="#4285F4"/>
    <SolidColorBrush x:Key="ClaudeBrush" Color="#D97757"/>
    <SolidColorBrush x:Key="OpenAIBrush" Color="#10A37F"/>
    <SolidColorBrush x:Key="CopilotBrush" Color="#000000"/>
    <SolidColorBrush x:Key="AntigravityBrush" Color="#7C3AED"/>
    
</ResourceDictionary>
```

### App.xaml (Updated)
```xml
<Application x:Class="Quotio.App.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             StartupUri="Views/MainWindow.xaml"
             Startup="Application_Startup"
             Exit="Application_Exit">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- Material Design -->
                <materialDesign:BundledTheme BaseTheme="Dark" 
                                              PrimaryColor="DeepPurple" 
                                              SecondaryColor="Lime"/>
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml"/>
                
                <!-- App Resources -->
                <ResourceDictionary Source="Resources/Styles/Colors.xaml"/>
                <ResourceDictionary Source="Resources/Styles/Buttons.xaml"/>
                <ResourceDictionary Source="Resources/Styles/Cards.xaml"/>
                
                <!-- Converters -->
                <ResourceDictionary Source="Resources/Converters.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

---

## Converters

### BoolToVisibilityConverter.cs
```csharp
namespace Quotio.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = parameter?.ToString() == "Invert";
        var visible = value is bool b && b;
        
        if (invert) visible = !visible;
        
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

### PercentageToColorConverter.cs
```csharp
namespace Quotio.App.Converters;

public class PercentageToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double percentage)
            return Brushes.Gray;

        return percentage switch
        {
            >= 50 => Application.Current.FindResource("QuotaHighBrush"),
            >= 20 => Application.Current.FindResource("QuotaMediumBrush"),
            >= 5 => Application.Current.FindResource("QuotaLowBrush"),
            _ => Application.Current.FindResource("QuotaCriticalBrush")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

---

## Verification

```powershell
cd c:\Users\Admin\Documents\quotio\QuotioWindows
dotnet build src/Quotio.App
dotnet run --project src/Quotio.App
```

Should display:
- Window with sidebar navigation
- Status panel showing "Stopped"
- Navigation items based on mode
- Start button

---

## Next Phase

→ [07-ui-screens.md](07-ui-screens.md)
