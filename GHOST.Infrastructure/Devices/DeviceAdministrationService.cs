using FluentValidation;
using GHOST.Application.Devices;
using GHOST.Domain.Entities;
using GHOST.Domain.Enums;
using GHOST.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Infrastructure.Devices;

public sealed class DeviceAdministrationService(
    AppDbContext dbContext,
    CreateDeviceRequestValidator deviceValidator,
    CreateRoomRequestValidator roomValidator) : IDeviceAdministrationService
{
    public async Task<bool> CanManageDevicesAsync(Guid actorId, CancellationToken cancellationToken = default) =>
        await dbContext.Users.AsNoTracking().Where(x => x.Id == actorId && x.IsActive).SelectMany(x => x.Roles).AnyAsync(x => x.Name == "Admin" || x.Name == "Manager", cancellationToken);

    public async Task<DeviceSummary> CreateDeviceAsync(Guid actorId, CreateDeviceRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureAuthorizedAsync(actorId, cancellationToken);
        await deviceValidator.ValidateAndThrowAsync(request, cancellationToken);
        if (await dbContext.Devices.AnyAsync(x => x.Name == request.Name.Trim(), cancellationToken)) throw new InvalidOperationException("A device with this name already exists.");
        if (request.RoomId is not null && !await dbContext.Rooms.AnyAsync(x => x.Id == request.RoomId, cancellationToken)) throw new KeyNotFoundException("The selected room does not exist.");
        var device = new Device
        {
            Name = request.Name.Trim(),
            DeviceType = request.DeviceType.Trim(),
            RoomId = request.RoomId,
            HourlyRate = request.SingleRate,
            MultiHourlyRate = request.MultiRate,
            HasAirConditioning = request.HasAirConditioning
        };
        dbContext.Devices.Add(device);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeviceSummary(device.Id, device.Name, device.DeviceType, device.Status, null, null, null, null, null);
    }

    public async Task<RoomSummary> CreateRoomAsync(Guid actorId, CreateRoomRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureAuthorizedAsync(actorId, cancellationToken);
        await roomValidator.ValidateAndThrowAsync(request, cancellationToken);
        if (await dbContext.Rooms.AnyAsync(x => x.Name == request.Name.Trim(), cancellationToken)) throw new InvalidOperationException("A room with this name already exists.");
        var room = new Room { Name = request.Name.Trim() };
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new RoomSummary(room.Id, room.Name, 0);
    }

    public async Task AssignRoomAsync(Guid actorId, Guid deviceId, Guid? roomId, CancellationToken cancellationToken = default)
    {
        await EnsureAuthorizedAsync(actorId, cancellationToken);
        var device = await dbContext.Devices.SingleOrDefaultAsync(x => x.Id == deviceId, cancellationToken) ?? throw new KeyNotFoundException("Device not found.");
        if (roomId is not null && !await dbContext.Rooms.AnyAsync(x => x.Id == roomId, cancellationToken)) throw new KeyNotFoundException("Room not found.");
        device.RoomId = roomId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(Guid actorId, Guid deviceId, DeviceStatus status, CancellationToken cancellationToken = default)
    {
        await EnsureAuthorizedAsync(actorId, cancellationToken);
        var device = await dbContext.Devices.SingleOrDefaultAsync(x => x.Id == deviceId, cancellationToken) ?? throw new KeyNotFoundException("Device not found.");
        device.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAuthorizedAsync(Guid actorId, CancellationToken cancellationToken)
    {
        if (!await CanManageDevicesAsync(actorId, cancellationToken)) throw new UnauthorizedAccessException("Device administration requires an active Admin or Manager account.");
    }
}
