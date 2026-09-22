using System.Windows;
using GHOST.Presentation.ViewModels;

namespace GHOST.Presentation;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel) { InitializeComponent(); DataContext = viewModel; }
    private async void Window_Loaded(object sender, RoutedEventArgs e) => await ((MainViewModel)DataContext).RefreshCommand.ExecuteAsync(null);
}
