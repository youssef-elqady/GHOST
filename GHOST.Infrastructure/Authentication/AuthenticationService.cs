using GHOST.Application.Authentication;
using GHOST.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Infrastructure.Authentication;

public sealed class AuthenticationService(AppDbContext dbContext, IPasswordHasher passwordHasher, CurrentUserContext currentUser) : IAuthenticationService
{
    public async Task<AuthenticatedUser> SignInAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password)) throw new UnauthorizedAccessException("Invalid username or password.");
        var user = await dbContext.Users.Include(x => x.Roles).SingleOrDefaultAsync(x => x.Username == username.Trim(), cancellationToken);
        if (user is null || !user.IsActive || !passwordHasher.Verify(password, user.PasswordHash)) throw new UnauthorizedAccessException("Invalid username or password.");
        var authenticated = new AuthenticatedUser(user.Id, user.Username, user.Roles.Select(x => x.Name).ToHashSet(StringComparer.Ordinal));
        currentUser.Set(authenticated);
        return authenticated;
    }

    public void SignOut() => currentUser.Clear();
}
