namespace GHOST.Domain.Entities;

public sealed class SessionPause
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public int? DurationSeconds { get; set; }
}
