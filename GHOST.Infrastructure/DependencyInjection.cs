using GHOST.Application.Authentication;
using GHOST.Application.Devices;
using GHOST.Application.Day4;
using GHOST.Application.Sessions;
using GHOST.Infrastructure.Authentication;
using GHOST.Infrastructure.Devices;
using GHOST.Infrastructure.Day4;
using GHOST.Infrastructure.Persistence;
using GHOST.Infrastructure.Sessions;
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
        services.AddScoped<IDeviceDashboardService, DeviceDashboardService>();
        services.AddScoped<IDeviceAdministrationService, DeviceAdministrationService>();
        services.AddScoped<CreateDeviceRequestValidator>();
        services.AddScoped<CreateRoomRequestValidator>();
        services.AddSingleton<IBillingCalculator, BillingCalculator>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ISessionRateProvider, DeviceRateProvider>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IDay4Service, Day4Service>();
        return services;
    }
}
