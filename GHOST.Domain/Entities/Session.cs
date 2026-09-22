using GHOST.Domain.Enums;

namespace GHOST.Domain.Entities;

public sealed class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public Device Device { get; set; } = null!;
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public decimal RatePerHour { get; set; }
    public int TotalPausedSeconds { get; set; }
    public decimal? TotalAmount { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Running;
    public Guid? StartedById { get; set; }
    public User? StartedBy { get; set; }
    public Guid? EndedById { get; set; }
    public User? EndedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<SessionPause> Pauses { get; set; } = new List<SessionPause>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
