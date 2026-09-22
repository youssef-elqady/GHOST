using GHOST.Application.Authentication;
using GHOST.Domain.Entities;
using GHOST.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Infrastructure.Authentication;

public sealed class AdminSetupService(AppDbContext dbContext, IPasswordHasher passwordHasher) : IAdminSetupService
{
    public async Task<bool> IsSetupRequiredAsync(CancellationToken cancellationToken = default) => !await dbContext.Users.AnyAsync(cancellationToken);
    public async Task CompleteSetupAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || password.Length < 12) throw new ArgumentException("A username and a password of at least 12 characters are required.");
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (await dbContext.Users.AnyAsync(cancellationToken)) throw new InvalidOperationException("Initial setup has already been completed.");
        var admin = await dbContext.Roles.SingleAsync(x => x.Name == "Admin", cancellationToken);
        dbContext.Users.Add(new User { Username = username.Trim(), PasswordHash = passwordHasher.Hash(password), Roles = [admin] });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
