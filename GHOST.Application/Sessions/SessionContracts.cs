using GHOST.Domain.Enums;

namespace GHOST.Application.Sessions;

public sealed record BillingPolicy(BillingRounding Rounding, int MinimumBillableMinutes, bool DeductPauses = true)
{
    public static BillingPolicy Default { get; } = new(BillingRounding.PerMinute, 0);
}

public sealed record BillingResult(TimeSpan Elapsed, TimeSpan PauseDuration, TimeSpan BillableDuration, decimal Amount);
public interface IBillingCalculator { BillingResult Calculate(DateTimeOffset startedAt, DateTimeOffset endedAt, int pausedSeconds, decimal ratePerHour, BillingPolicy policy); }
public interface IClock { DateTimeOffset UtcNow { get; } }
public interface ISessionRateProvider { decimal ResolveRatePerHour(decimal deviceHourlyRate); }
public sealed record StartSessionRequest(Guid DeviceId, Guid? CustomerId = null);
public sealed record CashPaymentRequest(Guid SessionId, decimal AmountReceived);
public interface ISessionService
{
    Task<Guid> StartAsync(Guid actorId, StartSessionRequest request, CancellationToken cancellationToken = default);
    Task PauseAsync(Guid actorId, Guid sessionId, CancellationToken cancellationToken = default);
    Task ResumeAsync(Guid actorId, Guid sessionId, CancellationToken cancellationToken = default);
    Task EndAsync(Guid actorId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<Guid> TakeCashPaymentAsync(Guid actorId, CashPaymentRequest request, CancellationToken cancellationToken = default);
}
