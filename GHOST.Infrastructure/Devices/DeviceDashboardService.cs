using GHOST.Application.Devices;
using GHOST.Application.Sessions;
using GHOST.Domain.Enums;
using GHOST.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Infrastructure.Devices;

public sealed class DeviceDashboardService(
    AppDbContext dbContext,
    IClock clock,
    IBillingCalculator billingCalculator) : IDeviceDashboardService
{
    public async Task<IReadOnlyList<DeviceSummary>> GetDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        var devices =
            await dbContext.Devices
                .AsNoTracking()
                .Include(x => x.Room)
                .Include(x =>
                    x.Sessions.Where(
                        s =>
                            s.Status == SessionStatus.Running ||
                            s.Status == SessionStatus.Paused))
                .ThenInclude(x => x.Pauses)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

        return devices
            .Select(device =>
            {
                var active =
                    device.Sessions
                        .OrderByDescending(x => x.StartedAt)
                        .FirstOrDefault();

                var pauseSeconds =
                    active?.Pauses.Sum(
                        x =>
                            x.DurationSeconds
                            ?? (
                                x.EndedAt is null
                                    ? Math.Max(
                                        0,
                                        (int)(now - x.StartedAt).TotalSeconds)
                                    : 0
                            )
                    ) ?? 0;

                TimeSpan? runtime =
                    active is null
                        ? null
                        : now
                          - active.StartedAt
                          - TimeSpan.FromSeconds(pauseSeconds);

                decimal? amount =
                    active is null
                        ? null
                        : billingCalculator
                            .Calculate(
                                active.StartedAt,
                                now,
                                pauseSeconds,
                                active.RatePerHour,
                                BillingPolicy.Default)
                            .Amount;

                var summary =
                    new DeviceSummary(
                        device.Id,
                        device.Name,
                        device.DeviceType,
                        device.Status,
                        device.Room?.Name,
                        active?.RatePerHour,
                        active?.Id,
                        runtime,
                        amount)
                    {
                        SingleRate = device.HourlyRate,
                        MultiRate = device.MultiHourlyRate,
                        HasAirConditioning = device.HasAirConditioning
                    };

                return summary;
            })
            .ToArray();
    }

    public async Task<IReadOnlyList<RoomSummary>> GetRoomsAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Rooms
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(
                x =>
                    new RoomSummary(
                        x.Id,
                        x.Name,
                        x.Devices.Count))
            .ToListAsync(cancellationToken);
}