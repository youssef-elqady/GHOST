using GHOST.Application.Day5;
using GHOST.Application.Sessions;
using GHOST.Domain.Entities;
using GHOST.Domain.Enums;
using GHOST.Infrastructure.Day5;
using GHOST.Infrastructure.Persistence;
using GHOST.Infrastructure.Sessions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Tests;

public sealed class Day5ServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection c =
        new("Data Source=:memory:");

    private AppDbContext db = null!;
    private Day5Service s = null!;

    private Guid cashier;
    private Guid manager;
    private Guid admin;

    private TestClock clock = null!;
    private IBillingCalculator billingCalculator = null!;

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } =
            DateTimeOffset.UtcNow;
    }

    public async Task InitializeAsync()
    {
        await c.OpenAsync();

        db = new(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(c)
                .Options);

        await new DatabaseInitializer(db).InitializeAsync();

        cashier = await U("Cashier");
        manager = await U("Manager");
        admin = await U("Admin");

        clock = new TestClock();
        billingCalculator = new BillingCalculator();

        s = new(db, clock, billingCalculator);
    }

    public async Task DisposeAsync()
    {
        await db.DisposeAsync();
        await c.DisposeAsync();
    }

    private async Task<Guid> U(string n)
    {
        var r = await db.Roles.SingleAsync(x => x.Name == n);

        var u = new User
        {
            Username = Guid.NewGuid().ToString(),
            PasswordHash = "x",
            Roles = [r]
        };

        db.Users.Add(u);

        await db.SaveChangesAsync();

        return u.Id;
    }

    private async Task<Product> P()
    {
        var cat =
            await s.CreateCategoryAsync(
                admin,
                "Drinks" + Guid.NewGuid());

        var p =
            await s.CreateProductAsync(
                admin,
                new("Cola", cat.Id, 20, 10, 2));

        await s.ChangeStockAsync(
            cashier,
            new(
                p.Id,
                InventoryTransactionType.Purchase,
                10,
                "delivery"));

        return p;
    }

    [Fact]
    public async Task Product_catalog_filters_and_calculates_stock_metrics_in_database()
    {
        var drinks = await s.CreateCategoryAsync(admin, "Drinks" + Guid.NewGuid());
        var snacks = await s.CreateCategoryAsync(admin, "Snacks" + Guid.NewGuid());

        var cola = await s.CreateProductAsync(
            admin,
            new("Cola", drinks.Id, 20, 10, 5, "1001"));
        var chips = await s.CreateProductAsync(
            admin,
            new("Chips", snacks.Id, 15, 8, 3, "1002"));

        await s.ChangeStockAsync(
            cashier,
            new(cola.Id, InventoryTransactionType.Purchase, 10, "delivery"));
        await s.ChangeStockAsync(
            cashier,
            new(chips.Id, InventoryTransactionType.Purchase, 2, "delivery"));

        var result = await s.GetProductCatalogAsync(
            new(
                SearchText: "1002",
                CategoryId: snacks.Id,
                Status: ProductCatalogStatus.LowStock,
                Page: 1,
                PageSize: 50));

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(chips.Id, result.Items[0].Id);
        Assert.Equal(1, result.LowStockCount);
        Assert.Equal(30m, result.RetailStockValue);
        Assert.Equal(16m, result.CostStockValue);
    }

    [Fact]
    public async Task Inventory_tracks_purchase_sale_and_prevents_negative()
    {
        var p = await P();

        await s.ChangeStockAsync(
            cashier,
            new(
                p.Id,
                InventoryTransactionType.Sale,
                3,
                "sale"));

        Assert.Equal(
            7,
            (await db.Products.SingleAsync()).StockQuantity);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                s.ChangeStockAsync(
                    manager,
                    new(
                        p.Id,
                        InventoryTransactionType.Waste,
                        8,
                        "waste")));

        Assert.Equal(
            2,
            await db.InventoryTransactions.CountAsync());
    }

    [Fact]
    public async Task Pos_is_atomic_snapshots_price_and_writes_cash()
    {
        var p = await P();

        await s.OpenShiftAsync(
            cashier,
            100);

        var order =
            await s.CompleteOrderAsync(
                cashier,
                new(
                    [new PosItem(p.Id, 2)],
                    50));

        p.SellingPrice = 30;

        await db.SaveChangesAsync();

        var item =
            await db.OrderItems.SingleAsync();

        Assert.Equal(
            20,
            item.UnitPrice);

        Assert.Equal(
            40,
            order.FinalAmount);

        Assert.Equal(
            10,
            order.Change);

        Assert.Equal(
            8,
            (await db.Products.SingleAsync()).StockQuantity);

        Assert.Contains(
            await db.CashTransactions.ToListAsync(),
            x =>
                x.Type == CashTransactionType.Sale &&
                x.Amount == 40);
    }

    [Fact]
    public async Task Pos_failure_rolls_back_stock_and_order()
    {
        var p = await P();

        await s.OpenShiftAsync(
            cashier,
            0);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                s.CompleteOrderAsync(
                    cashier,
                    new(
                        [new PosItem(p.Id, 11)],
                        500)));

        Assert.Equal(
            10,
            (await db.Products.SingleAsync()).StockQuantity);

        Assert.Empty(
            await db.Orders.ToListAsync());
    }

    [Fact]
    public async Task Inactive_products_and_duplicate_shift_are_rejected()
    {
        var p = await P();

        await s.SetProductActiveAsync(
            admin,
            p.Id,
            false);

        await s.OpenShiftAsync(
            cashier,
            0);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                s.CompleteOrderAsync(
                    cashier,
                    new(
                        [new PosItem(p.Id, 1)],
                        20)));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                s.OpenShiftAsync(
                    cashier,
                    0));
    }

    [Fact]
    public async Task Closing_shift_requires_discrepancy_reason_and_calculates_difference()
    {
        var sh =
            await s.OpenShiftAsync(
                cashier,
                100);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                s.CloseShiftAsync(
                    cashier,
                    sh.Id,
                    new(90, null)));

        var closed =
            await s.CloseShiftAsync(
                cashier,
                sh.Id,
                new(90, "count error"));

        Assert.Equal(
            100,
            closed.ExpectedCash);

        Assert.Equal(
            -10,
            closed.Difference);

        Assert.False(
            closed.IsOpen);
    }
}