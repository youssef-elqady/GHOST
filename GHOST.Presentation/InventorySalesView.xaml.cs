using System.Windows;
using System.Windows.Controls;
using GHOST.Application.Authentication;
using GHOST.Application.Day5;
using GHOST.Application.Devices;

namespace GHOST.Presentation;

public partial class InventorySalesView : UserControl
{
    private readonly IDay5Service day5Service;
    private readonly ICurrentUserContext currentUser;
    private readonly IDeviceDashboardService dashboardService;

    public InventorySalesView(
        IDay5Service day5Service,
        ICurrentUserContext currentUser,
        IDeviceDashboardService dashboardService)
    {
        this.day5Service = day5Service;
        this.currentUser = currentUser;
        this.dashboardService = dashboardService;

        InitializeComponent();

        SalesHost.Content = new SalesView(day5Service, dashboardService);
        InventoryHost.Content = new InventoryView(day5Service, currentUser);

        if (currentUser.IsInRole("Cashier"))
        {
            InventoryTab.Visibility = Visibility.Collapsed;
            SalesInventoryTabs.SelectedItem = SalesTab;
        }
        else
        {
            SalesTab.Visibility = Visibility.Collapsed;
            SalesInventoryTabs.SelectedItem = InventoryTab;
        }
    }

    private void SalesInventoryTabs_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (e.Source != SalesInventoryTabs)
            return;
    }
}