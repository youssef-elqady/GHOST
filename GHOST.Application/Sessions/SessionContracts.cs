using GHOST.Domain.Enums;

namespace GHOST.Application.Sessions;

public sealed record BillingPolicy(BillingRounding Rounding, int MinimumBillableMinutes, bool DeductPauses = true)
{
    public static BillingPolicy Default { get; } = new(BillingRounding.PerMinute, 0);
}

public sealed record BillingResult(TimeSpan Elapsed, TimeSpan PauseDuration, TimeSpan BillableDuration, decimal Amount);
public interface IBillingCalculator { BillingResult Calculate(DateTimeOffset startedAt, DateTimeOffset endedAt, int pausedSeconds, decimal ratePerHour, BillingPolicy policy); }
public interface IClock { DateTimeOffset UtcNow { get; } }
public interface ISessionRateProvider
{
    decimal ResolveRatePerHour(decimal singleRate, decimal multiRate, SessionMode mode);
}
public sealed record StartSessionRequest(Guid DeviceId, Guid? CustomerId = null, SessionMode Mode = SessionMode.Single);
public sealed record CashPaymentRequest(Guid SessionId, decimal AmountReceived);
public sealed record SessionPaymentSummary(Guid SessionId, decimal PlayAmount, decimal ProductsAmount, decimal AmountDue, bool IsPaid);
public interface ISessionService
{
    Task<Guid> StartAsync(StartSessionRequest request, CancellationToken cancellationToken = default);
    Task PauseAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task ResumeAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task EndAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<Guid> TakeCashPaymentAsync(CashPaymentRequest request, CancellationToken cancellationToken = default);
    Task<SessionPaymentSummary> GetPaymentSummaryAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
