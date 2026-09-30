using GHOST.Application.Day6;
using GHOST.Domain.Enums;
using GHOST.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Infrastructure.Day6;

public sealed class AdminDashboardService(
    AppDbContext db,
    IReportingService reporting) : IAdminDashboardService
{
    public async Task<AdminDashboardSnapshot> GetTodayAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.Now;
        var from = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
        var to = from.AddDays(1);

        var report = await reporting.GetRevenueAsync(new DateRange(from, to), ct);

        var productSales = await db.Orders.AsNoTracking()
            .Where(x => x.CreatedAt >= from && x.CreatedAt < to)
            .SumAsync(x => (decimal?)x.FinalAmount, ct) ?? 0m;

        var ordersToday = await db.Orders.AsNoTracking()
            .CountAsync(x => x.CreatedAt >= from && x.CreatedAt < to, ct);

        var customersToday = await db.Sessions.AsNoTracking()
            .Where(x => x.StartedAt >= from && x.StartedAt < to && x.CustomerId != null)
            .Select(x => x.CustomerId!.Value)
            .Distinct()
            .CountAsync(ct);

        var activeSessions = await db.Sessions.AsNoTracking()
            .CountAsync(x => x.Status == SessionStatus.Running || x.Status == SessionStatus.Paused, ct);

        var runningSessions = await db.Sessions.AsNoTracking()
            .CountAsync(x => x.Status == SessionStatus.Running, ct);

        var pausedSessions = await db.Sessions.AsNoTracking()
            .CountAsync(x => x.Status == SessionStatus.Paused, ct);

        var availableDevices = await db.Devices.AsNoTracking()
            .CountAsync(x => x.Status == DeviceStatus.Available, ct);

        var maintenanceDevices = await db.Devices.AsNoTracking()
            .CountAsync(x => x.Status == DeviceStatus.Maintenance, ct);

        var offlineDevices = await db.Devices.AsNoTracking()
            .CountAsync(x => x.Status == DeviceStatus.Offline, ct);

        var reservedDevices = await db.Devices.AsNoTracking()
            .CountAsync(x => x.Status == DeviceStatus.Reserved, ct);

        var lowStockProducts = await db.Products.AsNoTracking()
            .CountAsync(x => x.IsActive && x.StockQuantity <= x.MinimumStockLevel, ct);

        var openShift = await db.Shifts.AsNoTracking()
            .Include(x => x.OpenedBy)
            .SingleOrDefaultAsync(x => x.IsOpen, ct);

        var cashDifferenceToday = await db.Shifts.AsNoTracking()
            .Where(x => !x.IsOpen && x.ClosedAt >= from && x.ClosedAt < to)
            .SumAsync(x => (decimal?)x.Difference, ct) ?? 0m;

        var auditRows = await db.AuditLogs.AsNoTracking()
            .Include(x => x.Actor)
            .OrderByDescending(x => x.OccurredAt)
            .Take(10)
            .Select(x => new
            {
                x.Action,
                x.EntityType,
                Actor = x.Actor == null ? "النظام" : x.Actor.Username,
                x.OccurredAt,
                x.Reason,
                x.NewValue
            })
            .ToListAsync(ct);

        var activity = auditRows.Select(x => new DashboardActivity(
            TranslateAction(x.Action),
            TranslateEntity(x.EntityType),
            x.Actor,
            x.OccurredAt,
            FirstNonEmpty(x.Reason, x.NewValue, "تم تنفيذ العملية."))).ToList();

        var alerts = new List<string>();

        if (lowStockProducts > 0)
            alerts.Add($"يوجد {lowStockProducts} منتجًا عند أو تحت الحد الأدنى للمخزون.");
        if (pausedSessions > 0)
            alerts.Add($"يوجد {pausedSessions} جلسة متوقفة مؤقتًا وتحتاج متابعة.");
        if (maintenanceDevices > 0)
            alerts.Add($"يوجد {maintenanceDevices} جهازًا في حالة صيانة.");
        if (offlineDevices > 0)
            alerts.Add($"يوجد {offlineDevices} جهازًا غير متصل.");
        if (openShift is null)
            alerts.Add("لا توجد وردية مفتوحة حاليًا.");
        if (cashDifferenceToday != 0)
            alerts.Add($"يوجد فرق نقدية اليوم بقيمة {cashDifferenceToday:N2} ج.م.");

        var topDevices = report.ByDevice
            .Select(line => new UtilizationLine(
                line.Name,
                TimeSpan.Zero,
                report.SessionCount == 0
                    ? 0m
                    : Math.Min(100m, decimal.Round(
                        line.Count / (decimal)Math.Max(1, report.SessionCount) * 100m, 1))))
            .OrderByDescending(x => x.Percent)
            .Take(5)
            .ToList();

        return new AdminDashboardSnapshot(
            report.Revenue,
            productSales,
            report.SessionCount,
            ordersToday,
            customersToday,
            activeSessions,
            runningSessions,
            pausedSessions,
            availableDevices,
            maintenanceDevices,
            offlineDevices,
            reservedDevices,
            lowStockProducts,
            openShift is not null,
            openShift?.OpenedBy.Username,
            openShift?.OpenedAt,
            cashDifferenceToday,
            topDevices,
            activity,
            alerts);
    }

    private static string FirstNonEmpty(string? first, string? second, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(first)) return first;
        if (!string.IsNullOrWhiteSpace(second)) return second;
        return fallback;
    }

    private static string TranslateAction(string action) => action switch
    {
        "ProductCreated" => "إضافة منتج",
        "ProductUpdated" => "تعديل منتج",
        "ProductEnabled" => "تفعيل منتج",
        "ProductDisabled" => "إيقاف منتج",
        "StockChanged" => "تعديل المخزون",
        "OrderCompleted" => "إتمام بيع",
        "ShiftOpened" => "فتح وردية",
        "ShiftClosed" => "إغلاق وردية",
        "CustomerCreated" => "إضافة عميل",
        "CustomerUpdated" => "تعديل عميل",
        "CustomerBlocked" => "حظر عميل",
        "CustomerUnblocked" => "إلغاء حظر عميل",
        "SessionStarted" => "بدء جلسة",
        "SessionPaused" => "إيقاف جلسة مؤقتًا",
        "SessionResumed" => "استكمال جلسة",
        "SessionEnded" => "إنهاء جلسة",
        _ => action
    };

    private static string TranslateEntity(string entity) => entity switch
    {
        "Product" => "منتج",
        "Order" => "فاتورة",
        "Shift" => "وردية",
        "Customer" => "عميل",
        "Session" => "جلسة",
        "Device" => "جهاز",
        _ => entity
    };
}