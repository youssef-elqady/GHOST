namespace GHOST.Application.Authentication;

public interface IAdminSetupService
{
    Task<bool> IsSetupRequiredAsync(CancellationToken cancellationToken = default);
    Task CompleteSetupAsync(string username, string password, CancellationToken cancellationToken = default);
}
