using GHOST.Application.Sessions;
using GHOST.Domain.Enums;

namespace GHOST.Tests;

public sealed class BillingCalculatorTests
{
    private readonly BillingCalculator calculator = new();
    private static readonly DateTimeOffset Start = new(2026, 9, 22, 18, 0, 0, TimeSpan.Zero);
    [Fact] public void Calculates_150_minutes_at_100_as_250() => Assert.Equal(250m, calculator.Calculate(Start, Start.AddMinutes(150), 0, 100m, BillingPolicy.Default).Amount);
    [Fact] public void Deducts_pause_time() { var bill = calculator.Calculate(Start, Start.AddMinutes(150), 30 * 60, 100m, BillingPolicy.Default); Assert.Equal(TimeSpan.FromMinutes(120), bill.BillableDuration); Assert.Equal(200m, bill.Amount); }
    [Fact] public void Applies_rounding_and_minimum_duration() { var rounded = calculator.Calculate(Start, Start.AddMinutes(6), 0, 100m, new BillingPolicy(BillingRounding.NearestFiveMinutes, 0)); var minimum = calculator.Calculate(Start, Start.AddMinutes(2), 0, 100m, new BillingPolicy(BillingRounding.PerMinute, 15)); Assert.Equal(16.67m, rounded.Amount); Assert.Equal(25m, minimum.Amount); }
}
