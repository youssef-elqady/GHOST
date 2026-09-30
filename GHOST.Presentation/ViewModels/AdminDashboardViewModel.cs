using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GHOST.Application.Day6;
using Microsoft.Extensions.DependencyInjection;

namespace GHOST.Presentation.ViewModels;

public sealed partial class AdminDashboardViewModel : ObservableObject, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DispatcherTimer _refreshTimer;

    public AdminDashboardViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _refreshTimer.Tick += async (_, _) => await RefreshAsync();
        _refreshTimer.Start();
    }

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string lastUpdatedText = "لم يتم التحديث بعد";
    [ObservableProperty] private decimal revenue;
    [ObservableProperty] private decimal productSales;
    [ObservableProperty] private int sessionsToday;
    [ObservableProperty] private int ordersToday;
    [ObservableProperty] private int customersToday;
    [ObservableProperty] private int activeSessions;
    [ObservableProperty] private int runningSessions;
    [ObservableProperty] private int pausedSessions;
    [ObservableProperty] private int availableDevices;
    [ObservableProperty] private int maintenanceDevices;
    [ObservableProperty] private int offlineDevices;
    [ObservableProperty] private int reservedDevices;
    [ObservableProperty] private int lowStockProducts;
    [ObservableProperty] private bool isShiftOpen;
    [ObservableProperty] private string shiftOpenedBy = "لا توجد";
    [ObservableProperty] private string shiftOpenedAt = "—";
    [ObservableProperty] private decimal cashDifferenceToday;

    public ObservableCollection<DashboardActivityItem> RecentActivity { get; } = [];
    public ObservableCollection<DashboardWorkloadItem> TopDevices { get; } = [];
    public ObservableCollection<string> Alerts { get; } = [];

    public string RevenueText => $"{Revenue:N2} ج.م";
    public string ProductSalesText => $"{ProductSales:N2} ج.م";
    public string CashDifferenceText => $"{CashDifferenceToday:N2} ج.م";
    public string ShiftStatusText => IsShiftOpen ? "الوردية مفتوحة" : "لا توجد وردية مفتوحة";
    public string ShiftStatusDetail => IsShiftOpen ? $"{ShiftOpenedBy} • {ShiftOpenedAt}" : "يجب فتح وردية قبل التشغيل والتحصيل.";
    public int TotalAttention => PausedSessions + MaintenanceDevices + OfflineDevices + LowStockProducts;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IAdminDashboardService>();
            var snapshot = await service.GetTodayAsync();

            Revenue = snapshot.Revenue;
            ProductSales = snapshot.ProductSales;
            SessionsToday = snapshot.SessionsToday;
            OrdersToday = snapshot.OrdersToday;
            CustomersToday = snapshot.CustomersToday;
            ActiveSessions = snapshot.ActiveSessions;
            RunningSessions = snapshot.RunningSessions;
            PausedSessions = snapshot.PausedSessions;
            AvailableDevices = snapshot.AvailableDevices;
            MaintenanceDevices = snapshot.MaintenanceDevices;
            OfflineDevices = snapshot.OfflineDevices;
            ReservedDevices = snapshot.ReservedDevices;
            LowStockProducts = snapshot.LowStockProducts;
            IsShiftOpen = snapshot.IsShiftOpen;
            ShiftOpenedBy = snapshot.ShiftOpenedBy ?? "لا توجد";
            ShiftOpenedAt = snapshot.ShiftOpenedAt?.ToLocalTime().ToString("hh:mm tt") ?? "—";
            CashDifferenceToday = snapshot.CashDifferenceToday;

            RecentActivity.Clear();
            foreach (var item in snapshot.RecentActivity)
                RecentActivity.Add(new DashboardActivityItem(item.Action, item.EntityType, item.Actor, item.OccurredAt.ToLocalTime().ToString("hh:mm"), item.Details));

            TopDevices.Clear();
            foreach (var item in snapshot.TopDevices)
                TopDevices.Add(new DashboardWorkloadItem(item.Device, $"{item.Percent:N1}%", item.Percent));

            Alerts.Clear();
            foreach (var alert in snapshot.Alerts)
                Alerts.Add(alert);

            OnPropertyChanged(nameof(RevenueText));
            OnPropertyChanged(nameof(ProductSalesText));
            OnPropertyChanged(nameof(CashDifferenceText));
            OnPropertyChanged(nameof(ShiftStatusText));
            OnPropertyChanged(nameof(ShiftStatusDetail));
            OnPropertyChanged(nameof(TotalAttention));
            LastUpdatedText = $"آخر تحديث اليوم {DateTime.Now:hh:mm:ss tt}";
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally { IsLoading = false; }
    }

    public void Dispose() => _refreshTimer.Stop();
}

public sealed record DashboardActivityItem(string Action, string EntityType, string Actor, string Time, string Details);
public sealed record DashboardWorkloadItem(string Device, string PercentText, decimal Percent);