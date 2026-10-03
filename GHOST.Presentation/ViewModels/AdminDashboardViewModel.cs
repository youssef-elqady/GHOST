using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GHOST.Application.Day6;
using GHOST.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace GHOST.Presentation.ViewModels;

public enum DashboardPeriod
{
    Today,
    Yesterday,
    ThisWeek,
    ThisMonth,
    PreviousMonth,
    Custom
}

public sealed partial class AdminDashboardViewModel
: ObservableObject, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DispatcherTimer _refreshTimer;

private DashboardPeriod _period =
    DashboardPeriod.Today;

    private DateTimeOffset? _customFrom;
    private DateTimeOffset? _customTo;

    public AdminDashboardViewModel(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        _refreshTimer =
            new DispatcherTimer
            {
                Interval =
                    TimeSpan.FromSeconds(30)
            };

        _refreshTimer.Tick +=
            async (_, _) =>
                await RefreshAsync();

        _refreshTimer.Start();
    }

    // =============================================================
    // STATE
    // =============================================================

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string lastUpdatedText =
        "لم يتم التحديث بعد";

    [ObservableProperty]
    private string selectedPeriodText =
        "اليوم";

    [ObservableProperty]
    private decimal revenue;

    [ObservableProperty]
    private decimal productSales;

    [ObservableProperty]
    private int sessions;

    [ObservableProperty]
    private int orders;

    [ObservableProperty]
    private int customers;

    [ObservableProperty]
    private int activeSessions;

    [ObservableProperty]
    private int pausedSessions;

    [ObservableProperty]
    private int lowStockProducts;

    [ObservableProperty]
    private bool isShiftOpen;

    [ObservableProperty]
    private string shiftOpenedBy =
        "لا توجد";

    [ObservableProperty]
    private string shiftOpenedAt =
        "—";

    [ObservableProperty]
    private decimal cashDifference;

    [ObservableProperty]
    private decimal previousRevenue;

    [ObservableProperty]
    private decimal previousProductSales;

    [ObservableProperty]
    private int previousSessions;

    public ObservableCollection<DashboardActivityItem>
        RecentActivity
    { get; } = [];

    public ObservableCollection<string>
        Alerts
    { get; } = [];

    // =============================================================
    // DISPLAY
    // =============================================================

    public string RevenueText =>
        $"{Revenue:N2} ج.م";

    public string ProductSalesText =>
        $"{ProductSales:N2} ج.م";

    public string CashDifferenceText =>
        $"{CashDifference:N2} ج.م";

    public string ActiveSessionsText =>
        $"{ActiveSessions} جلسة";

    public string SessionsText =>
        Sessions.ToString("N0");

    public string OrdersText =>
        Orders.ToString("N0");

    public string CustomersText =>
        Customers.ToString("N0");

    public string LowStockText =>
        LowStockProducts.ToString("N0");

    public string ShiftStatusText =>
        IsShiftOpen
            ? "الوردية مفتوحة"
            : "لا توجد وردية مفتوحة";

    public string ShiftStatusDetail =>
        IsShiftOpen
            ? $"{ShiftOpenedBy} • {ShiftOpenedAt}"
            : "يجب فتح وردية قبل التحصيل.";

    public int AlertCount =>
        Alerts.Count;

    public decimal TotalSales =>
        Revenue;

    public string TotalSalesText =>
        $"{TotalSales:N2} ج.م";

    // =============================================================
    // TREND
    // =============================================================

    public string RevenueTrendText =>
        BuildTrendText(
            Revenue,
            PreviousRevenue);

    public string ProductTrendText =>
        BuildTrendText(
            ProductSales,
            PreviousProductSales);

    public string SessionsTrendText =>
        BuildCountTrendText(
            Sessions,
            PreviousSessions);

    private static string BuildTrendText(
        decimal current,
        decimal previous)
    {
        if (previous == 0)
        {
            return current == 0
                ? "لا يوجد نشاط"
                : "جديد في الفترة";
        }

        var percent =
            ((current - previous) / previous) * 100m;

        return percent switch
        {
            > 0 =>
                $"↑ {percent:N1}% عن الفترة السابقة",

            < 0 =>
                $"↓ {Math.Abs(percent):N1}% عن الفترة السابقة",

            _ =>
                "مستقر عن الفترة السابقة"
        };
    }

    private static string BuildCountTrendText(
        int current,
        int previous)
    {
        if (previous == 0)
        {
            return current == 0
                ? "لا يوجد نشاط"
                : "جديد في الفترة";
        }

        var percent =
            ((decimal)(current - previous) /
             previous) * 100m;

        return percent switch
        {
            > 0 =>
                $"↑ {percent:N1}% عن الفترة السابقة",

            < 0 =>
                $"↓ {Math.Abs(percent):N1}% عن الفترة السابقة",

            _ =>
                "مستقر عن الفترة السابقة"
        };
    }

    // =============================================================
    // REFRESH
    // =============================================================

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var range = BuildRange(
                _period,
                _customFrom,
                _customTo);

            using var scope =
                _scopeFactory.CreateScope();

            var service =
                scope.ServiceProvider
                    .GetRequiredService<
                        IAdminDashboardService>();

            var snapshot =
                await service.GetAsync(range);

            Revenue =
                snapshot.Revenue;

            ProductSales =
                snapshot.ProductSales;

            Sessions =
                snapshot.Sessions;

            Orders =
                snapshot.Orders;

            Customers =
                snapshot.Customers;

            ActiveSessions =
                snapshot.ActiveSessions;

            PausedSessions =
                snapshot.PausedSessions;

            LowStockProducts =
                snapshot.LowStockProducts;

            IsShiftOpen =
                snapshot.IsShiftOpen;

            ShiftOpenedBy =
                snapshot.ShiftOpenedBy ??
                "لا توجد";

            ShiftOpenedAt =
                snapshot.ShiftOpenedAt?
                    .ToLocalTime()
                    .ToString("hh:mm tt")
                ?? "—";

            CashDifference =
                snapshot.CashDifference;

            PreviousRevenue =
                snapshot.PreviousRevenue;

            PreviousProductSales =
                snapshot.PreviousProductSales;

            PreviousSessions =
                snapshot.PreviousSessions;

            RecentActivity.Clear();

            foreach (var item
                     in snapshot.RecentActivity)
            {
                RecentActivity.Add(
                    new DashboardActivityItem(
                        item.Action,
                        item.EntityType,
                        item.Actor,
                        item.OccurredAt
                            .ToLocalTime()
                            .ToString("hh:mm"),
                        item.Details));
            }

            Alerts.Clear();

            foreach (var alert
                     in snapshot.Alerts)
            {
                Alerts.Add(alert);
            }

            RaiseCalculatedProperties();

            LastUpdatedText =
                $"آخر تحديث {DateTime.Now:hh:mm:ss tt}";
        }
        catch (Exception exception)
        {
            ErrorMessage =
                exception.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // =============================================================
    // FILTER
    // =============================================================

    public async Task ApplyFilterAsync(
        DashboardPeriod period,
        DateTime? customFrom,
        DateTime? customTo)
    {
        if (period == DashboardPeriod.Custom)
        {
            if (!customFrom.HasValue ||
                !customTo.HasValue)
            {
                ErrorMessage =
                    "اختر تاريخ البداية والنهاية.";

                return;
            }

            if (customFrom.Value.Date >
                customTo.Value.Date)
            {
                ErrorMessage =
                    "تاريخ البداية يجب أن يسبق تاريخ النهاية.";

                return;
            }

            _customFrom =
                new DateTimeOffset(
                    customFrom.Value.Date,
                    DateTimeOffset.Now.Offset);

            _customTo =
                new DateTimeOffset(
                    customTo.Value.Date.AddDays(1),
                    DateTimeOffset.Now.Offset);
        }

        _period = period;

        SelectedPeriodText =
            GetPeriodText(period);

        await RefreshAsync();
    }

    private static string GetPeriodText(
        DashboardPeriod period) =>
        period switch
        {
            DashboardPeriod.Today =>
                "اليوم",

            DashboardPeriod.Yesterday =>
                "أمس",

            DashboardPeriod.ThisWeek =>
                "هذا الأسبوع",

            DashboardPeriod.ThisMonth =>
                "هذا الشهر",

            DashboardPeriod.PreviousMonth =>
                "الشهر السابق",

            DashboardPeriod.Custom =>
                "فترة مخصصة",

            _ =>
                "اليوم"
        };

    private static DateRange BuildRange(
        DashboardPeriod period,
        DateTimeOffset? customFrom,
        DateTimeOffset? customTo)
    {
        var now =
            DateTimeOffset.Now;

        var today =
            new DateTimeOffset(
                now.Year,
                now.Month,
                now.Day,
                0,
                0,
                0,
                now.Offset);

        return period switch
        {
            DashboardPeriod.Today =>
                new DateRange(
                    today,
                    today.AddDays(1)),

            DashboardPeriod.Yesterday =>
                new DateRange(
                    today.AddDays(-1),
                    today),

            DashboardPeriod.ThisWeek =>
                BuildWeekRange(today),

            DashboardPeriod.ThisMonth =>
                new DateRange(
                    new DateTimeOffset(
                        today.Year,
                        today.Month,
                        1,
                        0,
                        0,
                        0,
                        today.Offset),
                    new DateTimeOffset(
                        today.Year,
                        today.Month,
                        1,
                        0,
                        0,
                        0,
                        today.Offset).AddMonths(1)),

            DashboardPeriod.PreviousMonth =>
                BuildPreviousMonthRange(today),

            DashboardPeriod.Custom
                when customFrom.HasValue &&
                     customTo.HasValue =>
                new DateRange(
                    customFrom.Value,
                    customTo.Value),

            _ =>
                new DateRange(
                    today,
                    today.AddDays(1))
        };
    }

    private static DateRange BuildWeekRange(
        DateTimeOffset today)
    {
        var difference =
            ((int)today.DayOfWeek + 6) % 7;

        var start =
            today.AddDays(-difference);

        return new DateRange(
            start,
            start.AddDays(7));
    }

    private static DateRange BuildPreviousMonthRange(
        DateTimeOffset today)
    {
        var start =
            new DateTimeOffset(
                today.Year,
                today.Month,
                1,
                0,
                0,
                0,
                today.Offset)
            .AddMonths(-1);

        return new DateRange(
            start,
            start.AddMonths(1));
    }

    // =============================================================
    // HELPERS
    // =============================================================

    private void RaiseCalculatedProperties()
    {
        OnPropertyChanged(
            nameof(RevenueText));

        OnPropertyChanged(
            nameof(ProductSalesText));

        OnPropertyChanged(
            nameof(CashDifferenceText));

        OnPropertyChanged(
            nameof(ActiveSessionsText));

        OnPropertyChanged(
            nameof(SessionsText));

        OnPropertyChanged(
            nameof(OrdersText));

        OnPropertyChanged(
            nameof(CustomersText));

        OnPropertyChanged(
            nameof(LowStockText));

        OnPropertyChanged(
            nameof(ShiftStatusText));

        OnPropertyChanged(
            nameof(ShiftStatusDetail));

        OnPropertyChanged(
            nameof(AlertCount));

        OnPropertyChanged(
            nameof(TotalSales));

        OnPropertyChanged(
            nameof(TotalSalesText));

        OnPropertyChanged(
            nameof(RevenueTrendText));

        OnPropertyChanged(
            nameof(ProductTrendText));

        OnPropertyChanged(
            nameof(SessionsTrendText));
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
    }

}

public sealed record DashboardActivityItem(
string Action,
string EntityType,
string Actor,
string Time,
string Details);
