using GHOST.Application.Devices;
using GHOST.Application.Sessions;
using GHOST.Domain.Entities;
using GHOST.Domain.Enums;
using GHOST.Infrastructure.Devices;
using GHOST.Infrastructure.Persistence;
using GHOST.Infrastructure.Sessions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Tests;

public sealed class DeviceManagementTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private AppDbContext db = null!;
    private DeviceAdministrationService administration = null!;
    public async Task InitializeAsync()
    {
        await connection.OpenAsync(); db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await new DatabaseInitializer(db).InitializeAsync();
        administration = new DeviceAdministrationService(db, new CreateDeviceRequestValidator(), new CreateRoomRequestValidator());
    }
    public async Task DisposeAsync() { await db.DisposeAsync(); await connection.DisposeAsync(); }

    [Fact] public void Device_action_guard_rejects_invalid_future_session_actions()
    {
        Assert.Throws<InvalidOperationException>(() => DeviceActionGuard.EnsureCanStartSession(DeviceStatus.Running));
        Assert.Throws<InvalidOperationException>(() => DeviceActionGuard.EnsureCanStartSession(DeviceStatus.Maintenance));
        Assert.Throws<InvalidOperationException>(() => DeviceActionGuard.EnsureCanEndSession(DeviceStatus.Available));
    }

    [Fact] public async Task Unauthorized_actor_cannot_administer_devices()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => administration.CreateRoomAsync(Guid.NewGuid(), new CreateRoomRequest("VIP Room")));
    }

    [Fact] public async Task Administrator_can_create_room_device_assign_room_and_update_status()
    {
        var adminRole = await db.Roles.SingleAsync(x => x.Name == "Admin");
        var admin = new User { Username = "device-admin", PasswordHash = "not-used-in-test", Roles = [adminRole] };
        db.Users.Add(admin); await db.SaveChangesAsync();
        var room = await administration.CreateRoomAsync(admin.Id, new CreateRoomRequest("VIP Room"));
        var device = await administration.CreateDeviceAsync(admin.Id, new CreateDeviceRequest("VIP Test", "VIP", room.Id));
        await administration.UpdateStatusAsync(admin.Id, device.Id, DeviceStatus.Maintenance);
        var stored = await db.Devices.SingleAsync(x => x.Id == device.Id);
        Assert.Equal(room.Id, stored.RoomId); Assert.Equal(DeviceStatus.Maintenance, stored.Status);
    }

    [Fact] public async Task Dashboard_exposes_all_seeded_devices_with_truthful_unavailable_values()
    {
        var dashboard = new DeviceDashboardService(db, new SystemClock(), new BillingCalculator());
        var devices = await dashboard.GetDevicesAsync();
        Assert.Equal(10, devices.Count); Assert.All(devices, x => { Assert.Null(x.CurrentPrice); Assert.Null(x.ActiveSessionId); Assert.Null(x.Runtime); Assert.Null(x.CurrentAmount); });
    }
}
