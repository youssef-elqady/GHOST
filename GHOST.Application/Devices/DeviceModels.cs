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
    decimal? CurrentAmount)
{
    public decimal SingleRate { get; init; }

    public decimal MultiRate { get; init; }

    public bool HasAirConditioning { get; init; }
}

public sealed record RoomSummary(
    Guid Id,
    string Name,
    int DeviceCount);

public sealed record CreateDeviceRequest(
    string Name,
    string DeviceType,
    Guid? RoomId,
    decimal SingleRate = 100m,
    decimal MultiRate = 100m,
    bool HasAirConditioning = false);

public sealed record CreateRoomRequest(
    string Name);
