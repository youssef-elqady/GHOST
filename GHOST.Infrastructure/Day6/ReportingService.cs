using GHOST.Application.Day6;
using GHOST.Domain.Enums;
using GHOST.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Infrastructure.Day6;

public sealed class ReportingService(AppDbContext db) : IReportingService
{
    public async Task<RevenueReport> GetRevenueAsync(DateRange range, CancellationToken ct = default)
    {
        var sessions = (await db.Sessions.AsNoTracking().Where(x => x.Status == SessionStatus.Completed).Select(x => new { Device = x.Device.Name, x.TotalAmount, x.StartedAt, x.EndedAt }).ToListAsync(ct)).Where(x => x.EndedAt >= range.From && x.EndedAt < range.To).ToList();
        var items = (await db.OrderItems.AsNoTracking().Select(x => new { Product = x.Product.Name, x.Total, x.Quantity, x.Order.CreatedAt }).ToListAsync(ct)).Where(x => x.CreatedAt >= range.From && x.CreatedAt < range.To).ToList();
        var runtime = sessions.Aggregate(TimeSpan.Zero, (total, item) => total + (item.EndedAt!.Value - item.StartedAt));
        return new RevenueReport(sessions.Sum(x => x.TotalAmount ?? 0) + items.Sum(x => x.Total), sessions.Count, runtime, sessions.GroupBy(x => x.Device).Select(x => new RevenueLine(x.Key, x.Sum(y => y.TotalAmount ?? 0), x.Count())).OrderByDescending(x => x.Amount).ToList(), items.GroupBy(x => x.Product).Select(x => new RevenueLine(x.Key, x.Sum(y => y.Total), x.Sum(y => y.Quantity))).OrderByDescending(x => x.Amount).ToList());
    }

    public async Task<AdminDashboard> GetDashboardAsync(DateRange range, CancellationToken ct = default)
    {
        var report = await GetRevenueAsync(range, ct); var devices = await db.Devices.AsNoTracking().Select(x => x.Name).ToListAsync(ct);
        var sessions = (await db.Sessions.AsNoTracking().Select(x => new { Device = x.Device.Name, x.StartedAt, x.EndedAt, x.TotalPausedSeconds }).ToListAsync(ct)).Where(x => x.StartedAt < range.To && (x.EndedAt ?? range.To) >= range.From).ToList();
        var available = range.To - range.From;
        var utilization = devices.Select(name => { var used = sessions.Where(x => x.Device == name).Aggregate(TimeSpan.Zero, (total, session) => { var start = session.StartedAt > range.From ? session.StartedAt : range.From; var end = (session.EndedAt ?? range.To) < range.To ? session.EndedAt!.Value : range.To; var duration = end - start - TimeSpan.FromSeconds(session.TotalPausedSeconds); return total + (duration > TimeSpan.Zero ? duration : TimeSpan.Zero); }); return new UtilizationLine(name, used, available <= TimeSpan.Zero ? 0 : decimal.Round((decimal)used.TotalMinutes / (decimal)available.TotalMinutes * 100m, 2)); }).ToList();
        var lowStock = await db.Products.AsNoTracking().CountAsync(x => x.StockQuantity <= x.MinimumStockLevel, ct); var cashDifference = (await db.Shifts.AsNoTracking().Select(x => new { x.ClosedAt, x.Difference }).ToListAsync(ct)).Where(x => x.ClosedAt >= range.From && x.ClosedAt < range.To).Sum(x => x.Difference ?? 0);
        var suggestions = new List<string>(); if (lowStock > 0) suggestions.Add("Low stock detected: review replenishment."); if (utilization.Any(x => x.Percent < 20)) suggestions.Add("Underutilized devices detected: consider a manager-reviewed offer.");
        return new AdminDashboard(report.Revenue, report.SessionCount, lowStock, cashDifference, utilization, suggestions);
    }
}
