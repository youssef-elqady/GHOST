using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using GHOST.Application.Authentication;
using GHOST.Application.Day5;
using GHOST.Application.Devices;
using GHOST.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GHOST.Presentation;

public partial class MainWindow : Window
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ICurrentUserContext currentUser;
    private readonly IDay5Service day5Service;
    private readonly AdminDashboardViewModel adminDashboardViewModel;

    private MainViewModel ViewModel =>
        (MainViewModel)DataContext;

    public MainWindow(
        MainViewModel viewModel,
        IServiceScopeFactory scopeFactory,
        ICurrentUserContext currentUser,
        IDay5Service day5Service,
        IDeviceDashboardService dashboardService)
    {
        this.scopeFactory = scopeFactory;
        this.currentUser = currentUser;
        this.day5Service = day5Service;

        currentUser.RequireAuthenticated();

        InitializeComponent();

        DataContext = viewModel;

        adminDashboardViewModel = new AdminDashboardViewModel(scopeFactory);
        DashboardHost.Content = new AdminDashboardView
        {
            DataContext = adminDashboardViewModel
        };

        InventoryHost.Content = new InventorySalesView(day5Service, currentUser, dashboardService);
    }

    private void OpenPlayOrders_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not DeviceCardViewModel device ||
            device.ActiveSessionId is null)
        {
            return;
        }

        var window = new PlayOrderView(
            day5Service,
            currentUser,
            device.ActiveSessionId.Value)
        {
            Owner = this
        };

        window.ShowDialog();
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.Toggle();
        ThemeToggleText.Text = ThemeService.IsDark
            ? "الوضع الفاتح"
            : "الوضع الداكن";
    }

    private async void Window_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            SetActiveNavigation(0);

            await ViewModel.RefreshCommand
                .ExecuteAsync(null);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "GHOST",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Dashboard_Click(
        object sender,
        RoutedEventArgs e)
    {
        NavigateTo(
            0,
            "الرئيسية",
            "ملخص الإدارة والإيرادات وحالة التشغيل والتنبيهات");
    }

    private void Devices_Click(
        object sender,
        RoutedEventArgs e)
    {
        NavigateTo(
            1,
            "الأجهزة والجلسات",
            "إدارة ومتابعة أجهزة اللعب والجلسات الحالية");
    }

    private void Payments_Click(
        object sender,
        RoutedEventArgs e)
    {
        NavigateTo(
            2,
            "المدفوعات",
            "تحصيل الحسابات واستلام النقدية");
    }

    private void Customers_Click(
        object sender,
        RoutedEventArgs e)
    {
        NavigateTo(
            3,
            "العملاء",
            "ملفات العملاء والزيارات والإنفاق والولاء");
    }

    private void Inventory_Click(
        object sender,
        RoutedEventArgs e)
    {
        NavigateTo(
            4,
            "المبيعات والمخزون",
            "المنتجات والمخزون والطلبات");
    }

    private void Shifts_Click(
        object sender,
        RoutedEventArgs e)
    {
        NavigateTo(
            5,
            "الشيفتات والخزينة",
            "إدارة الشيفتات وحركة النقدية");
    }

    private void Reports_Click(
        object sender,
        RoutedEventArgs e)
    {
        NavigateTo(
            6,
            "التقارير",
            "تحليل أداء المكان والإيرادات والتشغيل");
    }

    private void Administration_Click(
        object sender,
        RoutedEventArgs e)
    {
        NavigateTo(
            7,
            "الإدارة",
            "إعدادات النظام والأجهزة والصلاحيات");
    }

    private void NavigateTo(
        int index,
        string title,
        string subtitle)
    {
        if (index < 0 ||
            index >= ContentTabs.Items.Count)
        {
            return;
        }

        SetActiveNavigation(index);

        ContentTabs.SelectedIndex = index;

        PageTitle.Text = title;
        PageSubtitle.Text = subtitle;
    }

    private void SetActiveNavigation(int index)
    {
        var buttons = new[]
        {
            NavDashboard,
            NavDevices,
            NavPayments,
            NavCustomers,
            NavInventory,
            NavShifts,
            NavReports,
            NavAdministration
        };

        foreach (var button in buttons)
            button.Tag = null;

        if (index >= 0 && index < buttons.Length)
            buttons[index].Tag = "Active";
    }

    private void WindowMinimize_Click(
        object sender,
        RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void WindowMaximize_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            MaximizeButton.Content = "□";
            return;
        }

        WindowState = WindowState.Maximized;
        MaximizeButton.Content = "❐";
    }

    private void WindowClose_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }

    private async void Logout_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            using var scope =
                scopeFactory.CreateScope();

            var authentication =
                scope.ServiceProvider
                    .GetRequiredService<IAuthenticationService>();

            authentication.SignOut();

            Hide();

            var loginWindow =
                new LoginWindow(authentication);

            var loginResult =
                loginWindow.ShowDialog();

            if (loginResult == true)
            {
                currentUser.RequireAuthenticated();

                Show();

                await ViewModel.RefreshCommand
                    .ExecuteAsync(null);

                return;
            }

            Close();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "GHOST",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Show();
        }
    }

    private void Window_Closing(
        object? sender,
        CancelEventArgs e)
    {
        ViewModel.Dispose();
        adminDashboardViewModel.Dispose();
    }
}