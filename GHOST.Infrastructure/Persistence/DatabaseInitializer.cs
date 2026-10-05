using GHOST.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Infrastructure.Persistence;

public sealed class DatabaseInitializer(AppDbContext dbContext)
{
    private sealed record DeviceSeed(string Name, string Type, decimal SingleRate, decimal MultiRate, bool AirConditioning);

    private static readonly DeviceSeed[] Devices =
    [
        new("PS1", "PlayStation", 25m, 40m, false),
        new("PS2", "PlayStation", 25m, 40m, false),
        new("PS3", "PlayStation", 25m, 40m, false),
        new("PS4", "PlayStation", 25m, 40m, false),
        new("PS5", "PlayStation", 25m, 40m, false),
        new("VIP 1", "VIP", 30m, 45m, false),
        new("VIP 2", "VIP", 30m, 45m, false),
        new("VIP 3", "VIP", 30m, 45m, false),
        new("Ultra VIP 1", "Ultra VIP", 60m, 60m, true),
        new("Ultra VIP 2", "Ultra VIP", 60m, 60m, true)
    ];

    private static readonly string[] Roles = ["Admin", "Manager", "Cashier"];

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (!await dbContext.Devices.AnyAsync(cancellationToken))
        {
            dbContext.Devices.AddRange(Devices.Select(x => new Device
            {
                Name = x.Name,
                DeviceType = x.Type,
                HourlyRate = x.SingleRate,
                MultiHourlyRate = x.MultiRate,
                HasAirConditioning = x.AirConditioning
            }));
        }

        var existing = await dbContext.Roles.Select(x => x.Name).ToListAsync(cancellationToken);
        dbContext.Roles.AddRange(Roles.Where(x => !existing.Contains(x)).Select(x => new Role { Name = x }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}