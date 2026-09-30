using System.Windows;
using System.Windows.Controls;
using GHOST.Presentation.ViewModels;

namespace GHOST.Presentation;

public partial class AdminDashboardView : UserControl
{
    public AdminDashboardView() => InitializeComponent();

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminDashboardViewModel viewModel)
            await viewModel.RefreshAsync();
    }
}