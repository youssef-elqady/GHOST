using GHOST.Application.Day6;
using GHOST.Domain.Enums;
using GHOST.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Infrastructure.Day6;

public sealed class AdminDashboardService(
AppDbContext db,
IReportingService reporting) : IAdminDashboardService
{
    public async Task<AdminDashboardSnapshot> GetAsync(
    DateRange range,
    CancellationToken ct = default)
    {
        if (range.To <= range.From)
            throw new ArgumentException(
            "نطاق التاريخ غير صالح.",
            nameof(range));

    var duration = range.To - range.From;

        var previousRange = new DateRange(
            range.From - duration,
            range.From);

        // =========================================================
        // CURRENT PERIOD
        // =========================================================

        var currentReport =
            await reporting.GetRevenueAsync(
                range,
                ct);

        var previousReport =
            await reporting.GetRevenueAsync(
                previousRange,
                ct);

        // =========================================================
        // ORDERS
        // SQLite + DateTimeOffset:
        // materialize first, then filter in memory.
        // =========================================================

        var allOrders =
            await db.Orders
                .AsNoTracking()
                .Select(x => new
                {
                    x.CreatedAt,
                    x.FinalAmount
                })
                .ToListAsync(ct);

        var currentOrders =
            allOrders
                .Where(x =>
                    x.CreatedAt >= range.From &&
                    x.CreatedAt < range.To)
                .ToList();

        // =========================================================
        // SESSIONS / CUSTOMERS
        // =========================================================

        var allSessions =
            await db.Sessions
                .AsNoTracking()
                .Select(x => new
                {
                    x.StartedAt,
                    x.EndedAt,
                    x.Status,
                    x.CustomerId
                })
                .ToListAsync(ct);

        var currentSessions =
            allSessions
                .Where(x =>
                    x.StartedAt >= range.From &&
                    x.StartedAt < range.To)
                .ToList();

        var customers =
            currentSessions
                .Where(x => x.CustomerId.HasValue)
                .Select(x => x.CustomerId!.Value)
                .Distinct()
                .Count();

        // =========================================================
        // CURRENT LIVE STATE
        // This is intentionally independent from the selected
        // reporting period.
        // =========================================================

        var activeSessions =
            await db.Sessions
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.Status == SessionStatus.Running ||
                        x.Status == SessionStatus.Paused,
                    ct);

        var pausedSessions =
            await db.Sessions
                .AsNoTracking()
                .CountAsync(
                    x => x.Status == SessionStatus.Paused,
                    ct);

        // =========================================================
        // INVENTORY
        // =========================================================

        var lowStockProducts =
            await db.Products
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.IsActive &&
                        x.StockQuantity <= x.MinimumStockLevel,
                    ct);

        // =========================================================
        // CURRENT OPEN SHIFT
        // =========================================================

        var openShift =
            await db.Shifts
                .AsNoTracking()
                .Include(x => x.OpenedBy)
                .SingleOrDefaultAsync(
                    x => x.IsOpen,
                    ct);

        // =========================================================
        // CASH DIFFERENCE
        // =========================================================

        var allShifts =
            await db.Shifts
                .AsNoTracking()
                .Select(x => new
                {
                    x.ClosedAt,
                    x.Difference,
                    x.IsOpen
                })
                .ToListAsync(ct);

        var cashDifference =
            allShifts
                .Where(x =>
                    !x.IsOpen &&
                    x.ClosedAt.HasValue &&
                    x.ClosedAt.Value >= range.From &&
                    x.ClosedAt.Value < range.To)
                .Sum(x => x.Difference ?? 0m);

        // =========================================================
        // AUDIT ACTIVITY
        // =========================================================

        var auditRows =
            await db.AuditLogs
                .AsNoTracking()
                .Include(x => x.Actor)
                .Select(x => new
                {
                    x.Action,
                    x.EntityType,
                    Actor =
                        x.Actor == null
                            ? "النظام"
                            : x.Actor.Username,
                    x.OccurredAt,
                    x.Reason,
                    x.NewValue
                })
                .ToListAsync(ct);

        var activity =
            auditRows
                .Where(x =>
                    x.OccurredAt >= range.From &&
                    x.OccurredAt < range.To)
                .OrderByDescending(x => x.OccurredAt)
                .Take(8)
                .Select(x =>
                    new DashboardActivity(
                        TranslateAction(x.Action),
                        TranslateEntity(x.EntityType),
                        x.Actor,
                        x.OccurredAt,
                        FirstNonEmpty(
                            x.Reason,
                            x.NewValue,
                            "تم تنفيذ العملية.")))
                .ToList();

        // =========================================================
        // ADMIN ALERTS
        // No device clutter here.
        // =========================================================

        var alerts = new List<string>();

        if (lowStockProducts > 0)
        {
            alerts.Add(
                $"يوجد {lowStockProducts} منتج يحتاج مراجعة المخزون.");
        }

        if (openShift is null)
        {
            alerts.Add(
                "لا توجد وردية مفتوحة حاليًا.");
        }

        if (cashDifference != 0)
        {
            alerts.Add(
                $"يوجد فرق نقدية خلال الفترة بقيمة {cashDifference:N2} ج.م.");
        }

        if (pausedSessions > 0)
        {
            alerts.Add(
                $"يوجد {pausedSessions} جلسة متوقفة مؤقتًا حاليًا.");
        }

        return new AdminDashboardSnapshot(
            Revenue:
                currentReport.Revenue,

            ProductSales:
                currentOrders.Sum(x => x.FinalAmount),

            Sessions:
                currentReport.SessionCount,

            Orders:
                currentOrders.Count,

            Customers:
                customers,

            ActiveSessions:
                activeSessions,

            PausedSessions:
                pausedSessions,

            LowStockProducts:
                lowStockProducts,

            IsShiftOpen:
                openShift is not null,

            ShiftOpenedBy:
                openShift?.OpenedBy.Username,

            ShiftOpenedAt:
                openShift?.OpenedAt,

            CashDifference:
                cashDifference,

            PreviousRevenue:
                previousReport.Revenue,

            PreviousProductSales:
                previousReport
                    .ByProduct
                    .Sum(x => x.Amount),

            PreviousSessions:
                previousReport.SessionCount,

            RecentActivity:
                activity,

            Alerts:
                alerts);
    }

    private static string FirstNonEmpty(
        string? first,
        string? second,
        string fallback)
    {
        if (!string.IsNullOrWhiteSpace(first))
            return first;

        if (!string.IsNullOrWhiteSpace(second))
            return second;

        return fallback;
    }

    private static string TranslateAction(
        string action) =>
        action switch
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

    private static string TranslateEntity(
        string entity) =>
        entity switch
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
