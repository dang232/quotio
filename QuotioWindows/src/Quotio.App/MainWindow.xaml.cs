using System.Windows;
using Quotio.App.ViewModels;

namespace Quotio.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}