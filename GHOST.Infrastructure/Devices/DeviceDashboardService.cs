using GHOST.Application.Devices;
using GHOST.Domain.Enums;
using GHOST.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Infrastructure.Devices;

public sealed class DeviceDashboardService(AppDbContext dbContext) : IDeviceDashboardService
{
    public async Task<IReadOnlyList<DeviceSummary>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var devices = await dbContext.Devices.AsNoTracking().Include(x => x.Room).Include(x => x.Sessions.Where(s => s.Status == SessionStatus.Running || s.Status == SessionStatus.Paused)).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return devices.Select(device =>
        {
            var active = device.Sessions.OrderByDescending(x => x.StartedAt).FirstOrDefault();
            TimeSpan? runtime = active is null ? null : now - active.StartedAt - TimeSpan.FromSeconds(active.TotalPausedSeconds);
            return new DeviceSummary(device.Id, device.Name, device.DeviceType, device.Status, device.Room?.Name, active?.RatePerHour, active?.Id, runtime, null);
        }).ToArray();
    }

    public async Task<IReadOnlyList<RoomSummary>> GetRoomsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Rooms.AsNoTracking().OrderBy(x => x.Name).Select(x => new RoomSummary(x.Id, x.Name, x.Devices.Count)).ToListAsync(cancellationToken);
}
