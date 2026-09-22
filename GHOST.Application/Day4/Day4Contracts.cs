using GHOST.Domain.Entities;
using GHOST.Domain.Enums;

namespace GHOST.Application.Day4;

public sealed record CustomerProfile(Customer Customer, IReadOnlyList<Session> Sessions, bool IsFrequent);
public sealed record CreateCustomerRequest(string Name, string? Phone, string? Notes);
public sealed record UpdateCustomerRequest(string Name, string? Phone, string? Notes);
public sealed record DiscountRequest(Guid? CustomerId, Guid? SessionId, DiscountType Type, decimal Value, string Reason);
public sealed record LoyaltyRule(decimal PointsPerEgp, int PointsPerVisit, int FrequentVisitThreshold = 10, int FrequentWindowDays = 30);
public sealed record CreateGiftCardRequest(GiftCardRewardType RewardType, decimal? CashValue, string? Description, DateTimeOffset? ExpiresAt);
public sealed record CreateOfferRequest(string Name, DateOnly StartDate, DateOnly EndDate, string DaysOfWeek, TimeOnly StartTime, TimeOnly EndTime, DiscountType DiscountType, decimal Value, string DeviceTypes, int MinimumDurationMinutes, int? MaximumUsage);
public interface IDay4Service
{
    Task<Customer> CreateCustomerAsync(Guid actorId, CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<Customer> UpdateCustomerAsync(Guid actorId, Guid customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> SearchCustomersAsync(string term, CancellationToken cancellationToken = default);
    Task<CustomerProfile> GetCustomerProfileAsync(Guid customerId, LoyaltyRule rule, CancellationToken cancellationToken = default);
    Task SetCustomerBlockedAsync(Guid actorId, Guid customerId, bool blocked, string reason, CancellationToken cancellationToken = default);
    Task<Discount> RequestDiscountAsync(Guid actorId, DiscountRequest request, CancellationToken cancellationToken = default);
    Task ApproveDiscountAsync(Guid actorId, Guid discountId, CancellationToken cancellationToken = default);
    Task RejectDiscountAsync(Guid actorId, Guid discountId, string reason, CancellationToken cancellationToken = default);
    Task<LoyaltyTransaction> AddLoyaltyAsync(Guid actorId, Guid customerId, LoyaltyTransactionType type, int points, string reason, CancellationToken cancellationToken = default);
    Task<Gift> IssueGiftAsync(Guid actorId, Guid customerId, GiftPeriod period, string description, CancellationToken cancellationToken = default);
    Task RedeemGiftAsync(Guid actorId, Guid giftId, CancellationToken cancellationToken = default);
    Task<GiftCard> CreateGiftCardAsync(Guid actorId, CreateGiftCardRequest request, CancellationToken cancellationToken = default);
    Task RedeemGiftCardAsync(Guid actorId, string code, CancellationToken cancellationToken = default);
    Task<Offer> CreateOfferAsync(Guid actorId, CreateOfferRequest request, CancellationToken cancellationToken = default);
    Task SetOfferActiveAsync(Guid actorId, Guid offerId, bool active, CancellationToken cancellationToken = default);
}
