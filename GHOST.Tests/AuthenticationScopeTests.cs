using GHOST.Application.Authentication;
using GHOST.Infrastructure;
using GHOST.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GHOST.Tests;

public sealed class AuthenticationScopeTests
{
    [Fact]
    public async Task Sign_in_context_is_shared_by_all_scopes_and_sign_out_revokes_it()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ghost-auth-{Guid.NewGuid():N}.db");
        try
        {
            await using var provider = new ServiceCollection().AddGhostInfrastructure(path).BuildServiceProvider();
            using (var scope = provider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
                await scope.ServiceProvider.GetRequiredService<IAdminSetupService>().CompleteSetupAsync("scope-admin", "ScopePassword1");
                await scope.ServiceProvider.GetRequiredService<IAuthenticationService>().SignInAsync("scope-admin", "ScopePassword1");
            }
            using (var scope = provider.CreateScope()) Assert.Equal("scope-admin", scope.ServiceProvider.GetRequiredService<ICurrentUserContext>().RequireAuthenticated().Username);
            using (var scope = provider.CreateScope()) scope.ServiceProvider.GetRequiredService<IAuthenticationService>().SignOut();
            Assert.Throws<UnauthorizedAccessException>(() => provider.GetRequiredService<ICurrentUserContext>().RequireAuthenticated());
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
    }
}
