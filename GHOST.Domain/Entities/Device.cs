using GHOST.Domain.Enums;

namespace GHOST.Domain.Entities;

public sealed class Device
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string DeviceType { get; set; }

    /// <summary>Hourly rate for single-player mode.</summary>
    public decimal HourlyRate { get; set; }

    /// <summary>Hourly rate for multiplayer mode.</summary>
    public decimal MultiHourlyRate { get; set; }

    public bool HasAirConditioning { get; set; }
    public DeviceStatus Status { get; set; } = DeviceStatus.Available;
    public Guid? RoomId { get; set; }
    public Room? Room { get; set; }
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}