using GHOST.Domain.Enums;

namespace GHOST.Application.Devices;

/// <summary>Protects future session commands until the Day 3 session engine owns state transitions.</summary>
public static class DeviceActionGuard
{
    public static void EnsureCanStartSession(DeviceStatus status)
    {
        if (status == DeviceStatus.Running) throw new InvalidOperationException("A running device cannot start another session.");
        if (status == DeviceStatus.Maintenance) throw new InvalidOperationException("A device under maintenance cannot start a session.");
    }

    public static void EnsureCanEndSession(DeviceStatus status)
    {
        if (status == DeviceStatus.Available) throw new InvalidOperationException("An available device has no active session to end.");
    }
}
