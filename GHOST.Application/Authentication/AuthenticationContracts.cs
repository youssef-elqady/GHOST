namespace GHOST.Application.Authentication;

public sealed record AuthenticatedUser(Guid Id, string Username, IReadOnlySet<string> Roles)
{
    public bool IsInRole(string role) => Roles.Contains(role);
}

public interface ICurrentUserContext
{
    AuthenticatedUser? Current { get; }
    AuthenticatedUser RequireAuthenticated();
    bool IsInRole(string role);
}

public interface IAuthenticationService
{
    Task<AuthenticatedUser> SignInAsync(string username, string password, CancellationToken cancellationToken = default);
    void SignOut();
}
