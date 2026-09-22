using GHOST.Domain.Enums;

namespace GHOST.Application.Devices;

public sealed record DeviceSummary(
    Guid Id,
    string Name,
    string DeviceType,
    DeviceStatus Status,
    string? RoomName,
    decimal? CurrentPrice,
    Guid? ActiveSessionId,
    TimeSpan? Runtime,
    decimal? CurrentAmount);

public sealed record RoomSummary(Guid Id, string Name, int DeviceCount);
public sealed record CreateDeviceRequest(string Name, string DeviceType, Guid? RoomId);
public sealed record CreateRoomRequest(string Name);
