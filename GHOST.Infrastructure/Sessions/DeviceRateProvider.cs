using GHOST.Application.Sessions;
using GHOST.Domain.Enums;

namespace GHOST.Infrastructure.Sessions;

public sealed class DeviceRateProvider : ISessionRateProvider
{
    public decimal ResolveRatePerHour(
        decimal singleRate,
        decimal multiRate,
        SessionMode mode)
    {
        var selectedRate = mode switch
        {
            SessionMode.Single => singleRate,
            SessionMode.Multi => multiRate,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported session mode.")
        };

        if (selectedRate < 0)
            throw new InvalidOperationException("A device rate cannot be negative.");

        return selectedRate;
    }
}
