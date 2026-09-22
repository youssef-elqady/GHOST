namespace GHOST.Application.Sessions;

public sealed class BillingCalculator : IBillingCalculator
{
    public BillingResult Calculate(DateTimeOffset startedAt, DateTimeOffset endedAt, int pausedSeconds, decimal ratePerHour, BillingPolicy policy)
    {
        if (endedAt < startedAt) throw new ArgumentException("End time cannot precede start time.");
        if (pausedSeconds < 0 || ratePerHour < 0 || policy.MinimumBillableMinutes < 0) throw new ArgumentOutOfRangeException(nameof(pausedSeconds));
        var elapsed = endedAt - startedAt;
        var pause = policy.DeductPauses ? TimeSpan.FromSeconds(pausedSeconds) : TimeSpan.Zero;
        var rawBillable = elapsed - pause;
        if (rawBillable < TimeSpan.Zero) rawBillable = TimeSpan.Zero;
        var minimum = TimeSpan.FromMinutes(policy.MinimumBillableMinutes);
        if (rawBillable < minimum) rawBillable = minimum;
        var intervalMinutes = (int)policy.Rounding;
        var roundedSeconds = Math.Ceiling(rawBillable.TotalSeconds / (intervalMinutes * 60d)) * intervalMinutes * 60d;
        var billable = TimeSpan.FromSeconds(roundedSeconds);
        var amount = decimal.Round(ratePerHour * (decimal)billable.TotalHours, 2, MidpointRounding.AwayFromZero);
        return new BillingResult(elapsed, pause, billable, amount);
    }
}
