using GHOST.Application.Authentication;
using GHOST.Domain.Entities;
using GHOST.Infrastructure.Authentication;
using GHOST.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Tests;

public sealed class AuthenticationServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private AppDbContext db = null!;
    private CurrentUserContext current = null!;
    private Pbkdf2PasswordHasher hasher = null!;
    public async Task InitializeAsync()
    {
        await connection.OpenAsync(); db = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options); await new DatabaseInitializer(db).InitializeAsync();
        hasher = new(); current = new(); var admin = await db.Roles.SingleAsync(x => x.Name == "Admin");
        db.Users.Add(new User { Username = "admin", PasswordHash = hasher.Hash("CorrectHorseBattery1"), Roles = [admin] }); await db.SaveChangesAsync();
    }
    public async Task DisposeAsync() { await db.DisposeAsync(); await connection.DisposeAsync(); }
    [Fact] public async Task Sign_in_verifies_hash_sets_context_and_rejects_invalid_password()
    {
        var service = new AuthenticationService(db, hasher, current);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SignInAsync("admin", "wrong"));
        var user = await service.SignInAsync("admin", "CorrectHorseBattery1");
        Assert.Equal(user.Id, current.RequireAuthenticated().Id); Assert.True(current.IsInRole("Admin")); service.SignOut(); Assert.Throws<UnauthorizedAccessException>(() => current.RequireAuthenticated());
    }
}
