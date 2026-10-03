namespace GHOST.Application.Day6;

public sealed record DateRange(
DateTimeOffset From,
DateTimeOffset To);

public sealed record RevenueReport(
decimal Revenue,
int SessionCount,
TimeSpan Runtime,
IReadOnlyList<RevenueLine> ByDevice,
IReadOnlyList<RevenueLine> ByProduct);

public sealed record RevenueLine(
string Name,
decimal Amount,
int Count);

public sealed record UtilizationLine(
string Device,
TimeSpan Used,
decimal Percent);

public sealed record AdminDashboard(
decimal Revenue,
int Sessions,
int LowStock,
decimal CashDifference,
IReadOnlyList<UtilizationLine> Utilization,
IReadOnlyList<string> Suggestions);

public sealed record DashboardActivity(
string Action,
string EntityType,
string Actor,
DateTimeOffset OccurredAt,
string Details);

public sealed record AdminDashboardSnapshot(
decimal Revenue,
decimal ProductSales,
int Sessions,
int Orders,
int Customers,
int ActiveSessions,
int PausedSessions,
int LowStockProducts,
bool IsShiftOpen,
string? ShiftOpenedBy,
DateTimeOffset? ShiftOpenedAt,
decimal CashDifference,

decimal PreviousRevenue,
decimal PreviousProductSales,
int PreviousSessions,

IReadOnlyList<DashboardActivity> RecentActivity,
IReadOnlyList<string> Alerts);

public sealed record BackupOptions(
string DirectoryPath,
int RetentionCount = 14);

public interface IReportingService
{
    Task<RevenueReport> GetRevenueAsync(
    DateRange range,
    CancellationToken ct = default);

Task<AdminDashboard> GetDashboardAsync(
    DateRange range,
    CancellationToken ct = default);

}

public interface IAdminDashboardService
{
    Task<AdminDashboardSnapshot> GetAsync(
    DateRange range,
    CancellationToken ct = default);
}

public interface IBackupService
{
    Task<string> CreateBackupAsync(
    string databasePath,
    BackupOptions options,
    CancellationToken ct = default);

Task<bool> ValidateAsync(
    string backupPath,
    CancellationToken ct = default);

    Task RestoreAsync(
        string databasePath,
        string backupPath,
        BackupOptions options,
        CancellationToken ct = default);

}
