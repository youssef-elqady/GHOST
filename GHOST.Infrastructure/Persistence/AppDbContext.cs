using GHOST.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionPause> SessionPauses => Set<SessionPause>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Discount> Discounts => Set<Discount>(); public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>(); public DbSet<Gift> Gifts => Set<Gift>(); public DbSet<GiftCard> GiftCards => Set<GiftCard>(); public DbSet<Offer> Offers => Set<Offer>(); public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>(); public DbSet<Product> Products => Set<Product>(); public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>(); public DbSet<Order> Orders => Set<Order>(); public DbSet<OrderItem> OrderItems => Set<OrderItem>(); public DbSet<Shift> Shifts => Set<Shift>(); public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DeviceType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.HourlyRate).HasPrecision(18, 2);
            entity.Property(x => x.MultiHourlyRate).HasPrecision(18, 2);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasOne(x => x.Room).WithMany(x => x.Devices).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<Room>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(100).IsRequired(); entity.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(150).IsRequired(); entity.Property(x => x.Phone).HasMaxLength(30);
            entity.HasIndex(x => x.Phone); entity.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(x => x.Id); entity.Property(x => x.RatePerHour).HasPrecision(18, 2); entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.HasOne(x => x.Device).WithMany(x => x.Sessions).HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Customer).WithMany(x => x.Sessions).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.StartedBy).WithMany().HasForeignKey(x => x.StartedById).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.EndedBy).WithMany().HasForeignKey(x => x.EndedById).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.DeviceId, x.Status });
            entity.HasIndex(x => x.DeviceId).IsUnique().HasFilter("\"IsActive\" = 1");
        });
        modelBuilder.Entity<SessionPause>(entity => { entity.HasKey(x => x.Id); entity.HasOne(x => x.Session).WithMany(x => x.Pauses).HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(x => x.Id); entity.Property(x => x.AmountDue).HasPrecision(18, 2); entity.Property(x => x.AmountReceived).HasPrecision(18, 2); entity.Property(x => x.Change).HasPrecision(18, 2);
            entity.HasOne(x => x.Session).WithMany(x => x.Payments).HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Cashier).WithMany().HasForeignKey(x => x.CashierId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Shift).WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.SessionId).IsUnique();
            entity.HasIndex(x => x.ShiftId);
        });
        modelBuilder.Entity<User>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Username).HasMaxLength(100).IsRequired(); entity.Property(x => x.PasswordHash).IsRequired(); entity.HasIndex(x => x.Username).IsUnique(); entity.HasMany(x => x.Roles).WithMany(x => x.Users).UsingEntity("UserRoles"); });
        modelBuilder.Entity<Role>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(50).IsRequired(); entity.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<Discount>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Value).HasPrecision(18, 2); entity.Property(x => x.AppliedAmount).HasPrecision(18, 2); entity.Property(x => x.Reason).HasMaxLength(500).IsRequired(); entity.HasIndex(x => x.SessionId).IsUnique().HasFilter("\"SessionId\" IS NOT NULL"); entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.RequestedBy).WithMany().HasForeignKey(x => x.RequestedById).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.ApprovedBy).WithMany().HasForeignKey(x => x.ApprovedById).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<LoyaltyTransaction>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Reason).HasMaxLength(500).IsRequired(); entity.HasIndex(x => new { x.CustomerId, x.CreatedAt }); entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Gift>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Description).HasMaxLength(500).IsRequired(); entity.HasIndex(x => new { x.CustomerId, x.Period, x.IssuedAt }); entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.IssuedBy).WithMany().HasForeignKey(x => x.IssuedById).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.RedeemedBy).WithMany().HasForeignKey(x => x.RedeemedById).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<GiftCard>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Code).HasMaxLength(40).IsRequired(); entity.Property(x => x.CashValue).HasPrecision(18, 2); entity.HasIndex(x => x.Code).IsUnique(); entity.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.UsedBy).WithMany().HasForeignKey(x => x.UsedById).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Offer>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(150).IsRequired(); entity.Property(x => x.DiscountValue).HasPrecision(18, 2); entity.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<AuditLog>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Action).HasMaxLength(100).IsRequired(); entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired(); entity.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt }); entity.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.SetNull); });
        modelBuilder.Entity<ProductCategory>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(100).IsRequired(); e.HasIndex(x => x.Name).IsUnique(); });
        modelBuilder.Entity<Product>(e => { e.HasKey(x => x.Id); e.Property(x => x.Name).HasMaxLength(150).IsRequired(); e.Property(x => x.SellingPrice).HasPrecision(18, 2); e.Property(x => x.CostPrice).HasPrecision(18, 2); e.HasIndex(x => x.Name).IsUnique(); e.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<InventoryTransaction>(e => { e.HasKey(x => x.Id); e.Property(x => x.Reason).HasMaxLength(500).IsRequired(); e.HasIndex(x => new { x.ProductId, x.CreatedAt }); e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Order>(e => { e.HasKey(x => x.Id); e.Property(x => x.TotalAmount).HasPrecision(18, 2); e.Property(x => x.DiscountAmount).HasPrecision(18, 2); e.Property(x => x.FinalAmount).HasPrecision(18, 2); e.Property(x => x.AmountReceived).HasPrecision(18, 2); e.Property(x => x.Change).HasPrecision(18, 2); e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull); e.HasOne(x => x.Session).WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.SetNull); e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<OrderItem>(e => { e.HasKey(x => x.Id); e.Property(x => x.UnitPrice).HasPrecision(18, 2); e.Property(x => x.Discount).HasPrecision(18, 2); e.Property(x => x.Total).HasPrecision(18, 2); e.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Shift>(e => { e.HasKey(x => x.Id); e.Property(x => x.OpeningCash).HasPrecision(18, 2); e.Property(x => x.ExpectedCash).HasPrecision(18, 2); e.Property(x => x.ActualCash).HasPrecision(18, 2); e.Property(x => x.Difference).HasPrecision(18, 2); e.HasIndex(x => x.IsOpen).HasFilter("\"IsOpen\" = 1").IsUnique(); e.HasOne(x => x.OpenedBy).WithMany().HasForeignKey(x => x.OpenedById).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.ClosedBy).WithMany().HasForeignKey(x => x.ClosedById).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<CashTransaction>(e => { e.HasKey(x => x.Id); e.Property(x => x.Amount).HasPrecision(18, 2); e.Property(x => x.Reason).HasMaxLength(500).IsRequired(); e.HasIndex(x => new { x.ShiftId, x.Timestamp }); e.HasOne(x => x.Shift).WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict); });
    }
}
