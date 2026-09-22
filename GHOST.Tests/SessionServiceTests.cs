using GHOST.Application.Sessions;
using GHOST.Domain.Entities;
using GHOST.Domain.Enums;
using GHOST.Infrastructure.Persistence;
using GHOST.Infrastructure.Sessions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Tests;

public sealed class SessionServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private AppDbContext db = null!;
    private TestClock clock = null!;
    private SessionService service = null!;
    private Guid cashierId;
    public async Task InitializeAsync()
    {
        await connection.OpenAsync(); db = NewContext(); await new DatabaseInitializer(db).InitializeAsync();
        clock = new TestClock(new DateTimeOffset(2026, 9, 22, 18, 0, 0, TimeSpan.Zero)); service = CreateService(db);
        var role = await db.Roles.SingleAsync(x => x.Name == "Cashier"); var cashier = new User { Username = Guid.NewGuid().ToString("N"), PasswordHash = "test", Roles = [role] };
        db.Users.Add(cashier); await db.SaveChangesAsync(); cashierId = cashier.Id;
    }
    public async Task DisposeAsync() { await db.DisposeAsync(); await connection.DisposeAsync(); }
    private AppDbContext NewContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
    private SessionService CreateService(AppDbContext context) => new(context, clock, new DeviceRateProvider(), new BillingCalculator());
    private async Task<Device> DeviceAsync(string name = "PS1") => await db.Devices.SingleAsync(x => x.Name == name);
    private async Task<Guid> StartAsync(string name = "PS1") => await service.StartAsync(cashierId, new StartSessionRequest((await DeviceAsync(name)).Id));

    [Fact] public async Task Start_available_device_succeeds_and_freezes_rate()
    {
        var device = await DeviceAsync(); device.HourlyRate = 100m; await db.SaveChangesAsync();
        var id = await StartAsync(); device.HourlyRate = 120m; await db.SaveChangesAsync();
        var session = await db.Sessions.SingleAsync(x => x.Id == id);
        Assert.Equal(SessionStatus.Running, session.Status); Assert.Equal(DeviceStatus.Running, (await DeviceAsync()).Status); Assert.Equal(100m, session.RatePerHour);
    }
    [Fact] public async Task Start_rejects_running_maintenance_offline_and_duplicate_active_sessions()
    {
        var device = await DeviceAsync(); device.Status = DeviceStatus.Maintenance; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => StartAsync());
        device.Status = DeviceStatus.Offline; await db.SaveChangesAsync(); await Assert.ThrowsAsync<InvalidOperationException>(() => StartAsync());
        device.Status = DeviceStatus.Available; await db.SaveChangesAsync(); await StartAsync(); await Assert.ThrowsAsync<InvalidOperationException>(() => StartAsync());
    }
    [Fact] public async Task Failed_start_leaves_device_and_sessions_consistent()
    {
        var device = await DeviceAsync(); device.Status = DeviceStatus.Maintenance; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => StartAsync());
        Assert.Equal(DeviceStatus.Maintenance, (await DeviceAsync()).Status); Assert.Empty(await db.Sessions.ToListAsync());
    }
    [Fact] public async Task Pause_resume_persists_duration_and_state()
    {
        var id = await StartAsync(); clock.Advance(TimeSpan.FromMinutes(10)); await service.PauseAsync(cashierId, id); clock.Advance(TimeSpan.FromMinutes(5)); await service.ResumeAsync(cashierId, id);
        var session = await db.Sessions.Include(x => x.Pauses).SingleAsync(x => x.Id == id);
        Assert.Equal(SessionStatus.Running, session.Status); Assert.Equal(300, session.Pauses.Single().DurationSeconds); Assert.Equal(DeviceStatus.Running, (await DeviceAsync()).Status);
    }
    [Fact] public async Task Pause_and_resume_reject_invalid_states()
    {
        var id = await StartAsync(); await service.PauseAsync(cashierId, id); await Assert.ThrowsAsync<InvalidOperationException>(() => service.PauseAsync(cashierId, id));
        await service.ResumeAsync(cashierId, id); await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResumeAsync(cashierId, id));
        clock.Advance(TimeSpan.FromMinutes(1)); await service.EndAsync(cashierId, id); await Assert.ThrowsAsync<InvalidOperationException>(() => service.PauseAsync(cashierId, id));
    }
    [Fact] public async Task Ending_running_session_persists_amount_end_actor_and_available_device()
    {
        var id = await StartAsync(); clock.Advance(TimeSpan.FromMinutes(150)); await service.EndAsync(cashierId, id);
        var session = await db.Sessions.SingleAsync(x => x.Id == id);
        Assert.Equal(SessionStatus.Completed, session.Status); Assert.Equal(250m, session.TotalAmount); Assert.Equal(cashierId, session.EndedById); Assert.Equal(DeviceStatus.Available, (await DeviceAsync()).Status);
    }
    [Fact] public async Task Ending_paused_session_closes_pause_and_deducts_it()
    {
        var id = await StartAsync(); clock.Advance(TimeSpan.FromMinutes(60)); await service.PauseAsync(cashierId, id); clock.Advance(TimeSpan.FromMinutes(30)); await service.EndAsync(cashierId, id);
        var session = await db.Sessions.Include(x => x.Pauses).SingleAsync(x => x.Id == id);
        Assert.Equal(1800, session.TotalPausedSeconds); Assert.Equal(100m, session.TotalAmount); Assert.NotNull(session.Pauses.Single().EndedAt);
    }
    [Fact] public async Task Payments_enforce_completion_amount_and_single_final_payment()
    {
        var id = await StartAsync(); clock.Advance(TimeSpan.FromMinutes(60)); await Assert.ThrowsAsync<InvalidOperationException>(() => service.TakeCashPaymentAsync(cashierId, new CashPaymentRequest(id, 100m)));
        await service.EndAsync(cashierId, id); await Assert.ThrowsAsync<InvalidOperationException>(() => service.TakeCashPaymentAsync(cashierId, new CashPaymentRequest(id, 99m))); await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.TakeCashPaymentAsync(cashierId, new CashPaymentRequest(id, -1m)));
        var paymentId = await service.TakeCashPaymentAsync(cashierId, new CashPaymentRequest(id, 120m)); var payment = await db.Payments.SingleAsync(x => x.Id == paymentId);
        Assert.Equal(100m, payment.AmountDue); Assert.Equal(20m, payment.Change); await Assert.ThrowsAsync<InvalidOperationException>(() => service.TakeCashPaymentAsync(cashierId, new CashPaymentRequest(id, 100m)));
    }
    [Fact] public async Task Active_session_survives_context_restart_and_reconstructs_elapsed_time()
    {
        var id = await StartAsync(); clock.Advance(TimeSpan.FromMinutes(42)); await db.DisposeAsync(); db = NewContext(); service = CreateService(db);
        var session = await db.Sessions.SingleAsync(x => x.Id == id); Assert.Equal(SessionStatus.Running, session.Status); Assert.Equal(clock.UtcNow - session.StartedAt, TimeSpan.FromMinutes(42));
    }
    private sealed class TestClock(DateTimeOffset now) : IClock { public DateTimeOffset UtcNow { get; private set; } = now; public void Advance(TimeSpan duration) => UtcNow += duration; }
}
