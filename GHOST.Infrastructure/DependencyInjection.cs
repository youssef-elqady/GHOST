using GHOST.Application.Authentication;
using GHOST.Infrastructure.Authentication;
using GHOST.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GHOST.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGhostInfrastructure(this IServiceCollection services, string? databasePath = null)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(GhostDatabase.GetConnectionString(databasePath)));
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<IAdminSetupService, AdminSetupService>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        return services;
    }
}
