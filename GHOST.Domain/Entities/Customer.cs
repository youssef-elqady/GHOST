namespace GHOST.Domain.Entities;

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastVisitAt { get; set; }
    public int TotalVisits { get; set; }
    public decimal TotalSpent { get; set; }
    public int TotalMinutes { get; set; }
    public int LoyaltyPoints { get; set; }
    public bool IsBlocked { get; set; }
    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
