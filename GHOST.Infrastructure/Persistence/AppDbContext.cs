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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DeviceType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.HourlyRate).HasPrecision(18, 2);
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
            entity.HasIndex(x => x.SessionId).IsUnique();
        });
        modelBuilder.Entity<User>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Username).HasMaxLength(100).IsRequired(); entity.Property(x => x.PasswordHash).IsRequired(); entity.HasIndex(x => x.Username).IsUnique(); entity.HasMany(x => x.Roles).WithMany(x => x.Users).UsingEntity("UserRoles"); });
        modelBuilder.Entity<Role>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(50).IsRequired(); entity.HasIndex(x => x.Name).IsUnique(); });
    }
}
