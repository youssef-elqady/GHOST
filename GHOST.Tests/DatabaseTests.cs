using GHOST.Domain.Entities;
using GHOST.Domain.Enums;
using GHOST.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Tests;

public sealed class DatabaseTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private AppDbContext db = null!;
    public async Task InitializeAsync() { await connection.OpenAsync(); db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options); await new DatabaseInitializer(db).InitializeAsync(); }
    public async Task DisposeAsync() { await db.DisposeAsync(); await connection.DisposeAsync(); }

    [Fact] public async Task Database_schema_can_be_created() => Assert.True(await db.Database.CanConnectAsync());
    [Fact] public async Task Initializer_seeds_required_devices_and_roles()
    {
        await new DatabaseInitializer(db).InitializeAsync();
        Assert.Equal(10, await db.Devices.CountAsync());
        Assert.Equal(["PS1", "PS2", "PS3", "PS4", "PS5", "Ultra VIP 1", "Ultra VIP 2", "VIP 1", "VIP 2", "VIP 3"], await db.Devices.OrderBy(x => x.Name).Select(x => x.Name).ToArrayAsync());
        Assert.Equal(["Admin", "Cashier", "Manager"], await db.Roles.OrderBy(x => x.Name).Select(x => x.Name).ToArrayAsync());
    }
    [Fact] public async Task Device_name_is_unique()
    {
        db.Devices.AddRange(new Device { Name = "Unique", DeviceType = "Test" }, new Device { Name = "Unique", DeviceType = "Test" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
    [Fact] public async Task Session_and_pause_relationships_work()
    {
        var device = new Device { Name = "Test device", DeviceType = "Test", Status = DeviceStatus.Available }; db.Devices.Add(device); await db.SaveChangesAsync();
        var session = new Session { DeviceId = device.Id, StartedAt = DateTimeOffset.UtcNow, RatePerHour = 100m }; db.Sessions.Add(session); await db.SaveChangesAsync();
        db.SessionPauses.Add(new SessionPause { SessionId = session.Id, StartedAt = DateTimeOffset.UtcNow }); await db.SaveChangesAsync();
        Assert.Equal(session.Id, (await db.SessionPauses.Include(x => x.Session).SingleAsync()).Session.Id);
    }
    [Fact] public async Task Payment_requires_existing_session_and_cashier()
    {
        db.Payments.Add(new Payment { SessionId = Guid.NewGuid(), CashierId = Guid.NewGuid(), AmountDue = 1m, AmountReceived = 1m, Change = 0m });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
