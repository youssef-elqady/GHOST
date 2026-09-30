using GHOST.Application.Authentication;
using GHOST.Application.Day5;
using GHOST.Application.Sessions;
using GHOST.Infrastructure;
using GHOST.Infrastructure.Persistence;
using GHOST.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace GHOST.Tests;

public sealed class FreshInstallWorkflowTests
{
    [Fact]
    public async Task Isolated_database_initializes_authenticates_runs_session_payment_and_requires_login_after_logout()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ghost-fresh-" + Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(directory, "data", "ghost.db");
        ServiceProvider? services = null;
        try
        {
            services = new ServiceCollection().AddGhostInfrastructure(databasePath).BuildServiceProvider();
            using (var scope = services.CreateScope())
            {
                var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>(); await initializer.InitializeAsync();
                Assert.True(File.Exists(databasePath));
                Assert.Equal(10, await scope.ServiceProvider.GetRequiredService<AppDbContext>().Devices.CountAsync());
                var setup = scope.ServiceProvider.GetRequiredService<IAdminSetupService>(); await setup.CompleteSetupAsync("fresh-admin", "FreshInstallPass1");
                var auth = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.SignInAsync("fresh-admin", "wrong"));
                await auth.SignInAsync("fresh-admin", "FreshInstallPass1");
                var day5 = scope.ServiceProvider.GetRequiredService<IDay5Service>(); await day5.OpenShiftAsync(services.GetRequiredService<ICurrentUserContext>().RequireAuthenticated().Id, 0m);
            }
            Guid sessionId;
            using (var scope = services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var device = await db.Devices.FirstAsync();
                var sessions = scope.ServiceProvider.GetRequiredService<ISessionService>(); sessionId = await sessions.StartAsync(new(device.Id)); await sessions.PauseAsync(sessionId); await sessions.ResumeAsync(sessionId); await sessions.EndAsync(sessionId); await sessions.TakeCashPaymentAsync(new(sessionId, 100m));
                Assert.Single(await db.Payments.ToListAsync()); Assert.Single(await db.CashTransactions.Where(x => x.Type == CashTransactionType.Sale && x.Reason == "Session payment").ToListAsync());
            }
            using (var scope = services.CreateScope())
            {
                var auth = scope.ServiceProvider.GetRequiredService<IAuthenticationService>(); auth.SignOut();
                Assert.Throws<UnauthorizedAccessException>(() => services.GetRequiredService<ICurrentUserContext>().RequireAuthenticated());
            }
        }
        finally { if (services is not null) await services.DisposeAsync(); SqliteConnection.ClearAllPools(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
