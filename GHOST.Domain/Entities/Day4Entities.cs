using GHOST.Domain.Enums;

namespace GHOST.Domain.Entities;

public sealed class Discount
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid? CustomerId { get; set; } public Customer? Customer { get; set; } public Guid? SessionId { get; set; } public Session? Session { get; set; }
    public DiscountType Type { get; set; } public decimal Value { get; set; } public required string Reason { get; set; } public DiscountStatus Status { get; set; } = DiscountStatus.Requested;
    public Guid RequestedById { get; set; } public User RequestedBy { get; set; } = null!; public Guid? ApprovedById { get; set; } public User? ApprovedBy { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow; public DateTimeOffset? ApprovedAt { get; set; } public decimal? AppliedAmount { get; set; }
}
public sealed class LoyaltyTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid CustomerId { get; set; } public Customer Customer { get; set; } = null!; public LoyaltyTransactionType Type { get; set; } public int Points { get; set; } public required string Reason { get; set; } public Guid CreatedById { get; set; } public User CreatedBy { get; set; } = null!; public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public sealed class Gift
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid CustomerId { get; set; } public Customer Customer { get; set; } = null!; public GiftPeriod Period { get; set; } public required string Description { get; set; } public GiftStatus Status { get; set; } = GiftStatus.Issued; public Guid IssuedById { get; set; } public User IssuedBy { get; set; } = null!; public Guid? RedeemedById { get; set; } public User? RedeemedBy { get; set; } public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow; public DateTimeOffset? RedeemedAt { get; set; }
}
public sealed class GiftCard
{
    public Guid Id { get; set; } = Guid.NewGuid(); public required string Code { get; set; } public GiftCardRewardType RewardType { get; set; } public decimal? CashValue { get; set; } public string? Description { get; set; } public GiftCardStatus Status { get; set; } = GiftCardStatus.Active; public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; public DateTimeOffset? ExpiresAt { get; set; } public Guid CreatedById { get; set; } public User CreatedBy { get; set; } = null!; public DateTimeOffset? UsedAt { get; set; } public Guid? UsedById { get; set; } public User? UsedBy { get; set; }
}
public sealed class Offer
{
    public Guid Id { get; set; } = Guid.NewGuid(); public required string Name { get; set; } public DateOnly StartDate { get; set; } public DateOnly EndDate { get; set; } public string DaysOfWeek { get; set; } = string.Empty; public TimeOnly StartTime { get; set; } public TimeOnly EndTime { get; set; } public DiscountType DiscountType { get; set; } public decimal DiscountValue { get; set; } public string ApplicableDeviceTypes { get; set; } = string.Empty; public int MinimumDurationMinutes { get; set; } public int? MaximumUsage { get; set; } public int UsageCount { get; set; } public bool Active { get; set; } = true; public Guid CreatedById { get; set; } public User CreatedBy { get; set; } = null!;
}
public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid? ActorId { get; set; } public User? Actor { get; set; } public required string Action { get; set; } public required string EntityType { get; set; } public Guid? EntityId { get; set; } public string? Reason { get; set; } public string? OldValue { get; set; } public string? NewValue { get; set; } public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
