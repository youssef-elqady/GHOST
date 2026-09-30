namespace GHOST.Domain.Entities;

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;
    public decimal AmountDue { get; set; }
    public decimal AmountReceived { get; set; }
    public decimal Change { get; set; }
    public DateTimeOffset PaymentDate { get; set; } = DateTimeOffset.UtcNow;
    public Guid CashierId { get; set; }
    public User Cashier { get; set; } = null!;
    public Guid ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
}
