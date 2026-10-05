using GHOST.Domain.Enums;

namespace GHOST.Application.Devices;

public interface IDeviceAdministrationService
{
    Task<DeviceSummary> CreateDeviceAsync(Guid actorId, CreateDeviceRequest request, CancellationToken cancellationToken = default);
    Task<RoomSummary> CreateRoomAsync(Guid actorId, CreateRoomRequest request, CancellationToken cancellationToken = default);
    Task AssignRoomAsync(Guid actorId, Guid deviceId, Guid? roomId, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid actorId, Guid deviceId, DeviceStatus status, CancellationToken cancellationToken = default);
    Task<bool> CanManageDevicesAsync(Guid actorId, CancellationToken cancellationToken = default);
    Task<DeviceSummary> UpdateRatesAsync(
    Guid actorId,
    Guid deviceId,
    UpdateDeviceRatesRequest request,
    CancellationToken cancellationToken = default);
}
