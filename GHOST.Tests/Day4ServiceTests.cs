using GHOST.Application.Day4;
using GHOST.Domain.Entities;
using GHOST.Domain.Enums;
using GHOST.Infrastructure.Day4;
using GHOST.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Tests;

public sealed class Day4ServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:"); private AppDbContext db = null!; private Day4Service service = null!; private Guid cashier; private Guid manager; private Guid admin;
    public async Task InitializeAsync()
    {
        await connection.OpenAsync(); db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options); await new DatabaseInitializer(db).InitializeAsync();
        cashier = await UserAsync("Cashier"); manager = await UserAsync("Manager"); admin = await UserAsync("Admin"); service = new Day4Service(db);
    }
    public async Task DisposeAsync() { await db.DisposeAsync(); await connection.DisposeAsync(); }
    private async Task<Guid> UserAsync(string role) { var r = await db.Roles.SingleAsync(x => x.Name == role); var u = new User { Username = Guid.NewGuid().ToString("N"), PasswordHash = "test", Roles = [r] }; db.Users.Add(u); await db.SaveChangesAsync(); return u.Id; }
    [Fact] public async Task Customer_create_search_update_and_block_are_audited()
    { var c = await service.CreateCustomerAsync(cashier, new CreateCustomerRequest("Ahmed", "01000000000", "note")); Assert.Single(await service.SearchCustomersAsync("01000000000")); await service.UpdateCustomerAsync(cashier, c.Id, new UpdateCustomerRequest("Ahmed Ali", "01000000000", null)); await service.SetCustomerBlockedAsync(manager, c.Id, true, "abuse"); Assert.True((await db.Customers.SingleAsync(x => x.Id == c.Id)).IsBlocked); Assert.Contains(await db.AuditLogs.ToListAsync(), x => x.Action == "CustomerBlocked"); }
    [Fact] public async Task Duplicate_phone_and_unauthorized_block_are_rejected()
    { await service.CreateCustomerAsync(cashier, new CreateCustomerRequest("A", "011", null)); await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateCustomerAsync(cashier, new CreateCustomerRequest("B", "011", null))); var c = await service.CreateCustomerAsync(cashier, new CreateCustomerRequest("C", null, null)); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SetCustomerBlockedAsync(cashier, c.Id, true, "x")); }
    [Fact] public async Task Discounts_validate_and_require_manager_approval()
    { var d = await service.RequestDiscountAsync(cashier, new DiscountRequest(null, null, DiscountType.Percentage, 10, "service")); Assert.Equal(DiscountStatus.Requested, d.Status); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ApproveDiscountAsync(cashier, d.Id)); await service.ApproveDiscountAsync(manager, d.Id); Assert.Equal(DiscountStatus.Approved, (await db.Discounts.SingleAsync()).Status); await Assert.ThrowsAsync<ArgumentException>(() => service.RequestDiscountAsync(cashier, new DiscountRequest(null, null, DiscountType.Percentage, 101, "x"))); }
    [Fact] public async Task Loyalty_is_transactional_and_cannot_go_negative()
    { var c = await service.CreateCustomerAsync(cashier, new CreateCustomerRequest("Loyal", null, null)); await service.AddLoyaltyAsync(cashier, c.Id, LoyaltyTransactionType.Earn, 10, "visit"); await service.AddLoyaltyAsync(cashier, c.Id, LoyaltyTransactionType.Redeem, 5, "reward"); Assert.Equal(5, (await db.Customers.SingleAsync(x => x.Id == c.Id)).LoyaltyPoints); await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddLoyaltyAsync(cashier, c.Id, LoyaltyTransactionType.Redeem, 6, "reward")); }
    [Fact] public async Task Gifts_enforce_period_and_cards_enforce_single_redemption()
    { var c = await service.CreateCustomerAsync(cashier, new CreateCustomerRequest("Gift", null, null)); var gift = await service.IssueGiftAsync(manager, c.Id, GiftPeriod.Weekly, "drink"); await Assert.ThrowsAsync<InvalidOperationException>(() => service.IssueGiftAsync(manager, c.Id, GiftPeriod.Weekly, "drink")); await service.RedeemGiftAsync(cashier, gift.Id); var card = await service.CreateGiftCardAsync(admin, new CreateGiftCardRequest(GiftCardRewardType.CashValue, 100m, null, null)); await service.RedeemGiftCardAsync(cashier, card.Code); await Assert.ThrowsAsync<InvalidOperationException>(() => service.RedeemGiftCardAsync(cashier, card.Code)); }
    [Fact] public async Task Expired_cards_and_non_admin_offers_are_rejected()
    { var card = await service.CreateGiftCardAsync(admin, new CreateGiftCardRequest(GiftCardRewardType.FreeDrink, null, null, DateTimeOffset.UtcNow.AddDays(-1))); await Assert.ThrowsAsync<InvalidOperationException>(() => service.RedeemGiftCardAsync(cashier, card.Code)); var req = new CreateOfferRequest("Monday", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), "Monday", new TimeOnly(14, 0), new TimeOnly(17, 0), DiscountType.Percentage, 20m, "PlayStation", 60, 5); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateOfferAsync(manager, req)); var offer = await service.CreateOfferAsync(admin, req); Assert.True(offer.Active); }
}
