using GHOST.Application.Sessions;
using GHOST.Application.Authentication;
using GHOST.Domain.Entities;
using GHOST.Domain.Enums;
using GHOST.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Infrastructure.Sessions;

public sealed class SessionService(AppDbContext dbContext, IClock clock, ISessionRateProvider rateProvider, IBillingCalculator billingCalculator, ICurrentUserContext currentUser) : ISessionService
{
    public async Task<Guid> StartAsync(StartSessionRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUser.RequireAuthenticated().Id;
        await EnsureOperatorAsync(actorId, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var device = await dbContext.Devices.SingleOrDefaultAsync(x => x.Id == request.DeviceId, cancellationToken) ?? throw new KeyNotFoundException("Device not found.");
        if (device.Status != DeviceStatus.Available) throw new InvalidOperationException($"Cannot start a session when the device is {device.Status}.");
        if (await dbContext.Sessions.AnyAsync(x => x.DeviceId == request.DeviceId && (x.Status == SessionStatus.Running || x.Status == SessionStatus.Paused), cancellationToken)) throw new InvalidOperationException("The device already has an active session.");
        if (request.CustomerId is not null && !await dbContext.Customers.AnyAsync(x => x.Id == request.CustomerId, cancellationToken)) throw new KeyNotFoundException("Customer not found.");
        var now = clock.UtcNow;
        var session = new Session { DeviceId = device.Id, CustomerId = request.CustomerId, StartedAt = now, RatePerHour = rateProvider.ResolveRatePerHour(device.HourlyRate), Status = SessionStatus.Running, IsActive = true, StartedById = actorId, CreatedAt = now, UpdatedAt = now };
        device.Status = DeviceStatus.Running;
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return session.Id;
    }

    public async Task PauseAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var actorId = currentUser.RequireAuthenticated().Id;
        await EnsureOperatorAsync(actorId, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var session = await LoadActiveSessionAsync(sessionId, cancellationToken);
        if (session.Status != SessionStatus.Running) throw new InvalidOperationException("Only a running session can be paused.");
        if (session.Pauses.Any(x => x.EndedAt is null)) throw new InvalidOperationException("Session already has an open pause.");
        var now = clock.UtcNow;
        dbContext.SessionPauses.Add(new SessionPause { SessionId = session.Id, StartedAt = now });
        session.Status = SessionStatus.Paused; session.Device.Status = DeviceStatus.Paused; session.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }

    public async Task ResumeAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var actorId = currentUser.RequireAuthenticated().Id;
        await EnsureOperatorAsync(actorId, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var session = await LoadActiveSessionAsync(sessionId, cancellationToken);
        if (session.Status != SessionStatus.Paused) throw new InvalidOperationException("Only a paused session can be resumed.");
        var openPauses = session.Pauses.Where(x => x.EndedAt is null).ToArray();
        if (openPauses.Length != 1) throw new InvalidOperationException("A paused session must have exactly one open pause.");
        var now = clock.UtcNow;
        ClosePause(openPauses[0], now);
        session.Status = SessionStatus.Running; session.Device.Status = DeviceStatus.Running; session.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }

    public async Task EndAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var actorId = currentUser.RequireAuthenticated().Id;
        await EnsureOperatorAsync(actorId, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var session = await LoadActiveSessionAsync(sessionId, cancellationToken);
        if (session.Status is not (SessionStatus.Running or SessionStatus.Paused)) throw new InvalidOperationException("Only an active session can be ended.");
        var now = clock.UtcNow;
        if (session.Status == SessionStatus.Paused)
        {
            var openPauses = session.Pauses.Where(x => x.EndedAt is null).ToArray();
            if (openPauses.Length != 1) throw new InvalidOperationException("A paused session must have exactly one open pause.");
            ClosePause(openPauses[0], now);
        }
        var pausedSeconds = session.Pauses.Sum(x => x.DurationSeconds ?? 0);
        var bill = billingCalculator.Calculate(session.StartedAt, now, pausedSeconds, session.RatePerHour, BillingPolicy.Default);
        session.EndedAt = now; session.TotalPausedSeconds = pausedSeconds; session.TotalAmount = bill.Amount; session.Status = SessionStatus.Completed; session.IsActive = false; session.EndedById = actorId; session.UpdatedAt = now; session.Device.Status = DeviceStatus.Available;
        if (session.CustomerId is not null)
        {
            var customer = await dbContext.Customers.SingleAsync(x => x.Id == session.CustomerId, cancellationToken);
            customer.LastVisitAt = now; customer.TotalVisits++; customer.TotalSpent += bill.Amount; customer.TotalMinutes += (int)bill.BillableDuration.TotalMinutes;
        }
        await dbContext.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }

    public async Task<Guid> TakeCashPaymentAsync(CashPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var actorId = currentUser.RequireAuthenticated().Id;
        await EnsureOperatorAsync(actorId, cancellationToken);
        if (request.AmountReceived < 0) throw new ArgumentOutOfRangeException(nameof(request.AmountReceived));
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var session = await dbContext.Sessions.SingleOrDefaultAsync(x => x.Id == request.SessionId, cancellationToken) ?? throw new KeyNotFoundException("Session not found.");
        if (session.Status != SessionStatus.Completed || session.TotalAmount is null) throw new InvalidOperationException("Only completed sessions can be paid.");
        if (await dbContext.Payments.AnyAsync(x => x.SessionId == request.SessionId, cancellationToken)) throw new InvalidOperationException("A final payment already exists for this session.");
        if (request.AmountReceived < session.TotalAmount.Value) throw new InvalidOperationException("Insufficient cash received.");
        var shift = await dbContext.Shifts.SingleOrDefaultAsync(x => x.IsOpen, cancellationToken) ?? throw new InvalidOperationException("An open shift is required before accepting session payments.");
        var payment = new Payment { SessionId = session.Id, AmountDue = session.TotalAmount.Value, AmountReceived = request.AmountReceived, Change = request.AmountReceived - session.TotalAmount.Value, PaymentDate = clock.UtcNow, CashierId = actorId, ShiftId = shift.Id };
        dbContext.Payments.Add(payment);
        dbContext.CashTransactions.Add(new CashTransaction { ShiftId = shift.Id, Amount = payment.AmountDue, Type = CashTransactionType.Sale, Reference = payment.Id.ToString(), Reason = "Session payment", UserId = actorId, Timestamp = payment.PaymentDate });
        await dbContext.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return payment.Id;
    }
    public async Task<SessionPaymentSummary> GetPaymentSummaryAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        currentUser.RequireAuthenticated();
        var session = await dbContext.Sessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken) ?? throw new KeyNotFoundException("Session not found.");
        if (session.Status != SessionStatus.Completed || session.TotalAmount is null) throw new InvalidOperationException("Only completed sessions can be paid.");
        return new SessionPaymentSummary(session.Id, session.TotalAmount.Value, await dbContext.Payments.AnyAsync(x => x.SessionId == sessionId, cancellationToken));
    }

    private async Task<Session> LoadActiveSessionAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Sessions.Include(x => x.Device).Include(x => x.Pauses).SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new KeyNotFoundException("Session not found.");
    private static void ClosePause(SessionPause pause, DateTimeOffset now) { if (now < pause.StartedAt) throw new InvalidOperationException("Pause end time cannot precede its start."); pause.EndedAt = now; pause.DurationSeconds = checked((int)(now - pause.StartedAt).TotalSeconds); }
    private async Task EnsureOperatorAsync(Guid actorId, CancellationToken cancellationToken)
    {
        var authorized = await dbContext.Users.AsNoTracking().Where(x => x.Id == actorId && x.IsActive).SelectMany(x => x.Roles).AnyAsync(x => x.Name == "Cashier" || x.Name == "Manager" || x.Name == "Admin", cancellationToken);
        if (!authorized) throw new UnauthorizedAccessException("Session operations require an active Cashier, Manager, or Admin account.");
    }
}
