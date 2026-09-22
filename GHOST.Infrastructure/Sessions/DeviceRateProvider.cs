using GHOST.Application.Sessions;

namespace GHOST.Infrastructure.Sessions;

public sealed class DeviceRateProvider : ISessionRateProvider
{
    public decimal ResolveRatePerHour(decimal deviceHourlyRate)
    {
        if (deviceHourlyRate < 0) throw new InvalidOperationException("A device rate cannot be negative.");
        return deviceHourlyRate;
    }
}
