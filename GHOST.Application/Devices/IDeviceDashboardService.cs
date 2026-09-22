namespace GHOST.Application.Devices;

public interface IDeviceDashboardService
{
    Task<IReadOnlyList<DeviceSummary>> GetDevicesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoomSummary>> GetRoomsAsync(CancellationToken cancellationToken = default);
}
