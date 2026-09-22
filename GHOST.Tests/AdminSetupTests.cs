using GHOST.Infrastructure.Authentication;
using GHOST.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Tests;

public sealed class AdminSetupTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private AppDbContext db = null!;
    public async Task InitializeAsync() { await connection.OpenAsync(); db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options); await new DatabaseInitializer(db).InitializeAsync(); }
    public async Task DisposeAsync() { await db.DisposeAsync(); await connection.DisposeAsync(); }

    [Fact] public void Password_hasher_never_returns_plaintext_and_verifies_correct_password()
    {
        var hasher = new Pbkdf2PasswordHasher(); var hash = hasher.Hash("a secure test password");
        Assert.NotEqual("a secure test password", hash); Assert.True(hasher.Verify("a secure test password", hash)); Assert.False(hasher.Verify("wrong password", hash));
    }
    [Fact] public async Task Setup_creates_only_one_admin_account()
    {
        var service = new AdminSetupService(db, new Pbkdf2PasswordHasher());
        Assert.True(await service.IsSetupRequiredAsync()); await service.CompleteSetupAsync("admin", "a secure test password");
        Assert.False(await service.IsSetupRequiredAsync()); await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteSetupAsync("other", "another secure password"));
    }
}
