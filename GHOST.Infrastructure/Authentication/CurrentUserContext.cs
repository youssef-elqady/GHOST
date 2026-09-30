using GHOST.Application.Authentication;

namespace GHOST.Infrastructure.Authentication;

public sealed class CurrentUserContext : ICurrentUserContext
{
    private AuthenticatedUser? current;
    public AuthenticatedUser? Current => current;
    public AuthenticatedUser RequireAuthenticated() => current ?? throw new UnauthorizedAccessException("Authentication is required.");
    public bool IsInRole(string role) => current?.IsInRole(role) == true;
    internal void Set(AuthenticatedUser user) => current = user;
    internal void Clear() => current = null;
}
