using System.Windows;
using System.Windows.Controls;
using GHOST.Presentation.ViewModels;

namespace GHOST.Presentation;

public partial class AdminDashboardView : UserControl
{
    public AdminDashboardView()
    {
        InitializeComponent();
    }

private async void UserControl_Loaded(
    object sender,
    RoutedEventArgs e)
    {
        if (DataContext is AdminDashboardViewModel vm)
            await vm.RefreshAsync();
    }

    private void OpenFilters_Click(
        object sender,
        RoutedEventArgs e)
    {
        FilterOverlay.Visibility =
            Visibility.Visible;
    }

    private void CloseFilters_Click(
        object sender,
        RoutedEventArgs e)
    {
        FilterOverlay.Visibility =
            Visibility.Collapsed;
    }

    private async void ApplyFilters_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is not AdminDashboardViewModel vm)
            return;

        var period =
            PeriodComboBox.SelectedIndex switch
            {
                0 => DashboardPeriod.Today,
                1 => DashboardPeriod.Yesterday,
                2 => DashboardPeriod.ThisWeek,
                3 => DashboardPeriod.ThisMonth,
                4 => DashboardPeriod.PreviousMonth,
                5 => DashboardPeriod.Custom,
                _ => DashboardPeriod.Today
            };

        await vm.ApplyFilterAsync(
            period,
            FromDatePicker.SelectedDate,
            ToDatePicker.SelectedDate);

        if (string.IsNullOrWhiteSpace(
                vm.ErrorMessage))
        {
            FilterOverlay.Visibility =
                Visibility.Collapsed;
        }
    }

    private void ResetFilters_Click(
        object sender,
        RoutedEventArgs e)
    {
        PeriodComboBox.SelectedIndex = 0;

        FromDatePicker.SelectedDate = null;
        ToDatePicker.SelectedDate = null;

        if (DataContext is AdminDashboardViewModel vm)
        {
            vm.ErrorMessage = string.Empty;
        }
    }

    private async void Refresh_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is AdminDashboardViewModel vm)
            await vm.RefreshAsync();
    }

}
