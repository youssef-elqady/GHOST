using GHOST.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Infrastructure.Persistence;

public sealed class DatabaseInitializer(AppDbContext dbContext)
{
    private static readonly (string Name, string Type)[] Devices =
    [ ("PS1", "PlayStation"), ("PS2", "PlayStation"), ("PS3", "PlayStation"), ("PS4", "PlayStation"), ("PS5", "PlayStation"), ("VIP 1", "VIP"), ("VIP 2", "VIP"), ("VIP 3", "VIP"), ("Ultra VIP 1", "Ultra VIP"), ("Ultra VIP 2", "Ultra VIP") ];
    private static readonly string[] Roles = ["Admin", "Manager", "Cashier"];

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
        if (!await dbContext.Devices.AnyAsync(cancellationToken)) dbContext.Devices.AddRange(Devices.Select(x => new Device { Name = x.Name, DeviceType = x.Type, HourlyRate = 100m }));
        var existing = await dbContext.Roles.Select(x => x.Name).ToListAsync(cancellationToken);
        dbContext.Roles.AddRange(Roles.Where(x => !existing.Contains(x)).Select(x => new Role { Name = x }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
