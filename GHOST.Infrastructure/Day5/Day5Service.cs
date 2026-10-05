using GHOST.Application.Day5;
using GHOST.Application.Sessions;
using GHOST.Domain.Entities;
using GHOST.Domain.Enums;
using GHOST.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GHOST.Infrastructure.Day5;

public sealed class Day5Service(AppDbContext db, IClock clock, IBillingCalculator billingCalculator) : IDay5Service
{
    public async Task<IReadOnlyList<ProductSummary>> GetProductsAsync(
        bool includeInactive = true,
        CancellationToken ct = default)
    {
        var query = db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(x => x.IsActive && x.Category.IsActive);

        return await query
            .OrderBy(x => x.Category.Name)
            .ThenBy(x => x.Name)
            .Select(x => new ProductSummary(
                x.Id,
                x.Name,
                x.Barcode,
                x.CategoryId,
                x.Category.Name,
                x.SellingPrice,
                x.CostPrice,
                x.StockQuantity,
                x.MinimumStockLevel,
                x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CategorySummary>> GetCategoriesAsync(
        bool includeInactive = true,
        CancellationToken ct = default)
    {
        var query = db.ProductCategories.AsNoTracking().AsQueryable();

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new CategorySummary(x.Id, x.Name, x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<ProductSummary?> GetProductByBarcodeAsync(
        string barcode,
        CancellationToken ct = default)
    {
        var normalized = barcode.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        return await db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.Barcode == normalized && x.IsActive && x.Category.IsActive)
            .Select(x => new ProductSummary(
                x.Id,
                x.Name,
                x.Barcode,
                x.CategoryId,
                x.Category.Name,
                x.SellingPrice,
                x.CostPrice,
                x.StockQuantity,
                x.MinimumStockLevel,
                x.IsActive))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<ProductCategory> CreateCategoryAsync(
        Guid actor,
        string name,
        CancellationToken ct = default)
    {
        await Role(actor, "Admin", ct);

        var normalized = name.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("اسم التصنيف مطلوب.");

        if (await db.ProductCategories.AnyAsync(x => x.Name == normalized, ct))
            throw new ArgumentException("هذا التصنيف موجود بالفعل.");

        var category = new ProductCategory
        {
            Name = normalized
        };

        db.ProductCategories.Add(category);

        await SaveAudit(
            actor,
            "CategoryCreated",
            "ProductCategory",
            category.Id,
            null,
            category.Name,
            ct);

        return category;
    }

    public async Task SetCategoryActiveAsync(
        Guid actor,
        Guid categoryId,
        bool active,
        CancellationToken ct = default)
    {
        await Role(actor, "Admin", ct);

        var category = await db.ProductCategories
            .SingleOrDefaultAsync(x => x.Id == categoryId, ct)
            ?? throw new KeyNotFoundException("التصنيف غير موجود.");

        if (!active && await db.Products.AnyAsync(x => x.CategoryId == categoryId && x.IsActive, ct))
            throw new InvalidOperationException("لا يمكن تعطيل تصنيف يحتوي على منتجات نشطة. عطّل المنتجات أولًا.");

        category.IsActive = active;

        await SaveAudit(
            actor,
            active ? "CategoryEnabled" : "CategoryDeactivated",
            "ProductCategory",
            category.Id,
            null,
            active.ToString(),
            ct);
    }

    public async Task<Product> CreateProductAsync(
        Guid actor,
        ProductRequest request,
        CancellationToken ct = default)
    {
        await Role(actor, "Admin", ct);

        ValidateProductInput(
            request.Name,
            request.SellingPrice,
            request.CostPrice,
            request.MinimumStockLevel);

        await EnsureCategoryIsActive(request.CategoryId, ct);
        await EnsureBarcodeIsAvailable(request.Barcode, null, ct);

        var product = new Product
        {
            Name = request.Name.Trim(),
            Barcode = NormalizeBarcode(request.Barcode),
            CategoryId = request.CategoryId,
            SellingPrice = request.SellingPrice,
            CostPrice = request.CostPrice,
            MinimumStockLevel = request.MinimumStockLevel
        };

        db.Products.Add(product);

        await SaveAudit(
            actor,
            "ProductCreated",
            "Product",
            product.Id,
            null,
            product.Name,
            ct);

        return product;
    }

    public async Task UpdateProductAsync(
        Guid actor,
        UpdateProductRequest request,
        CancellationToken ct = default)
    {
        await Role(actor, "Admin", ct);

        ValidateProductInput(
            request.Name,
            request.SellingPrice,
            request.CostPrice,
            request.MinimumStockLevel);

        var product = await Product(request.ProductId, ct);

        await EnsureCategoryIsActive(request.CategoryId, ct);
        await EnsureBarcodeIsAvailable(request.Barcode, product.Id, ct);

        var oldValue =
            $"{product.Name} | {product.Barcode ?? "بدون باركود"} | {product.SellingPrice:N2}";

        product.Name = request.Name.Trim();
        product.Barcode = NormalizeBarcode(request.Barcode);
        product.CategoryId = request.CategoryId;
        product.SellingPrice = request.SellingPrice;
        product.CostPrice = request.CostPrice;
        product.MinimumStockLevel = request.MinimumStockLevel;

        await SaveAudit(
            actor,
            "ProductUpdated",
            "Product",
            product.Id,
            oldValue,
            $"{product.Name} | {product.Barcode ?? "بدون باركود"} | {product.SellingPrice:N2}",
            ct);
    }

    public async Task SetProductActiveAsync(
        Guid actor,
        Guid id,
        bool active,
        CancellationToken ct = default)
    {
        await Role(actor, "Admin", ct);

        var product = await Product(id, ct);
        product.IsActive = active;

        await SaveAudit(
            actor,
            active ? "ProductEnabled" : "ProductDeactivated",
            "Product",
            id,
            null,
            active.ToString(),
            ct);
    }

    public async Task<InventoryTransaction> ChangeStockAsync(
        Guid actor,
        InventoryRequest request,
        CancellationToken ct = default)
    {
        await Role(
            actor,
            request.Type is InventoryTransactionType.Adjustment or InventoryTransactionType.Waste
                ? "Manager"
                : "Cashier",
            ct);

        if (request.Quantity <= 0 || string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("الكمية وسبب الحركة مطلوبان.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var product = await Product(request.ProductId, ct);

        var delta =
            request.Type is InventoryTransactionType.Purchase or InventoryTransactionType.Return
                ? request.Quantity
                : -request.Quantity;

        if (product.StockQuantity + delta < 0)
            throw new InvalidOperationException("الكمية المطلوبة أكبر من المخزون الحالي.");

        var transaction = new InventoryTransaction
        {
            ProductId = product.Id,
            Type = request.Type,
            Quantity = delta,
            BeforeQuantity = product.StockQuantity,
            AfterQuantity = product.StockQuantity + delta,
            Reason = request.Reason.Trim(),
            CreatedById = actor
        };

        product.StockQuantity += delta;
        db.InventoryTransactions.Add(transaction);

        await SaveAudit(
            actor,
            "Inventory" + request.Type,
            "Product",
            product.Id,
            transaction.BeforeQuantity.ToString(),
            transaction.AfterQuantity.ToString(),
            ct,
            request.Reason);

        await tx.CommitAsync(ct);

        return transaction;
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(
        CancellationToken ct = default)
    {
        return await db.Products
            .AsNoTracking()
            .Where(x => x.StockQuantity <= x.MinimumStockLevel)
            .OrderBy(x => x.StockQuantity)
            .ToListAsync(ct);
    }

    public async Task<PlayOrderSummary> GetPlayDetailsAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        var session = await db.Sessions
            .AsNoTracking()
            .Include(x => x.Device)
                .ThenInclude(x => x.Room)
            .Include(x => x.Pauses)
            .SingleOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new KeyNotFoundException("اللعب غير موجود.");

        if (session.Status is not (SessionStatus.Running or SessionStatus.Paused or SessionStatus.Completed))
            throw new InvalidOperationException("اللعب غير متاح.");

        return await BuildPlayOrderSummary(session, ct);
    }

    public async Task<PlayOrderSummary> AddProductToPlayAsync(
        Guid actor,
        AddProductToPlayRequest request,
        CancellationToken ct = default)
    {
        await Role(actor, "Cashier", ct);

        if (request.Quantity <= 0)
            throw new ArgumentException("الكمية يجب أن تكون أكبر من صفر.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var shiftOpen = await db.Shifts.AnyAsync(x => x.IsOpen, ct);
        if (!shiftOpen)
            throw new InvalidOperationException("يجب فتح شيفت قبل إضافة منتجات على اللعب.");

        var session = await db.Sessions
            .Include(x => x.Device)
                .ThenInclude(x => x.Room)
            .Include(x => x.Pauses)
            .SingleOrDefaultAsync(x => x.Id == request.SessionId, ct)
            ?? throw new KeyNotFoundException("اللعب غير موجود.");

        if (session.Status is not (SessionStatus.Running or SessionStatus.Paused))
            throw new InvalidOperationException("لا يمكن إضافة منتجات إلا أثناء اللعب.");

        var product = await db.Products
            .SingleOrDefaultAsync(x => x.Id == request.ProductId, ct)
            ?? throw new KeyNotFoundException("المنتج غير موجود.");

        if (!product.IsActive)
            throw new InvalidOperationException("المنتج غير نشط.");

        if (product.StockQuantity < request.Quantity)
            throw new InvalidOperationException(
                $"الكمية المطلوبة أكبر من المخزون الحالي: {product.Name}");

        var order = await db.Orders
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.SessionId == request.SessionId, ct);

        if (order is null)
        {
            order = new Order
            {
                SessionId = session.Id,
                CustomerId = session.CustomerId,
                CreatedById = actor,
                TotalAmount = 0m,
                FinalAmount = 0m
            };

            db.Orders.Add(order);
        }

        var item = order.Items
            .SingleOrDefault(x => x.ProductId == product.Id);

        if (item is null)
        {
            item = new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = request.Quantity,
                UnitPrice = product.SellingPrice,
                Discount = 0m,
                Total = product.SellingPrice * request.Quantity
            };

            order.Items.Add(item);
        }
        else
        {
            item.Quantity += request.Quantity;
            item.Total = item.Quantity * item.UnitPrice - item.Discount;
        }

        var before = product.StockQuantity;
        product.StockQuantity -= request.Quantity;

        db.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = product.Id,
            Type = InventoryTransactionType.Sale,
            Quantity = -request.Quantity,
            BeforeQuantity = before,
            AfterQuantity = product.StockQuantity,
            Reason = $"إضافة {request.Quantity} من المنتج إلى لعب {session.Device.Name}",
            CreatedById = actor
        });

        order.TotalAmount = order.Items.Sum(x => x.Total) + order.DiscountAmount;
        order.FinalAmount = order.Items.Sum(x => x.Total) - order.DiscountAmount;

        await SaveAudit(
            actor,
            "ProductAddedToPlay",
            "Order",
            order.Id,
            null,
            $"{product.Name} × {request.Quantity}",
            ct,
            $"Device={session.Device.Name}; Session={session.Id}");

        await tx.CommitAsync(ct);

        return await BuildPlayOrderSummary(session, ct);
    }

    public async Task<Order> CompleteOrderAsync(
        Guid actor,
        CompleteOrderRequest request,
        CancellationToken ct = default)
    {
        await Role(actor, "Cashier", ct);

        if (request.Items.Count == 0 ||
            request.AmountReceived < 0 ||
            request.OrderDiscount < 0)
        {
            throw new ArgumentException("بيانات الطلب غير صحيحة.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var shift = await db.Shifts
            .SingleOrDefaultAsync(x => x.IsOpen, ct)
            ?? throw new InvalidOperationException("يجب فتح شيفت قبل تسجيل البيع.");

        var order = new Order
        {
            CustomerId = request.CustomerId,
            SessionId = request.SessionId,
            CreatedById = actor,
            DiscountAmount = request.OrderDiscount
        };

        foreach (var line in request.Items)
        {
            if (line.Quantity <= 0 || line.Discount < 0)
                throw new ArgumentException("بيانات أحد أصناف الطلب غير صحيحة.");

            var product = await Product(line.ProductId, ct);

            if (!product.IsActive ||
                product.StockQuantity < line.Quantity)
            {
                throw new InvalidOperationException(
                    $"المنتج غير متاح أو الكمية غير كافية: {product.Name}");
            }

            var total =
                product.SellingPrice * line.Quantity -
                line.Discount;

            if (total < 0)
                throw new InvalidOperationException(
                    $"خصم المنتج أكبر من قيمته: {product.Name}");

            product.StockQuantity -= line.Quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitPrice = product.SellingPrice,
                Discount = line.Discount,
                Total = total
            });

            db.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = product.Id,
                Type = InventoryTransactionType.Sale,
                Quantity = -line.Quantity,
                BeforeQuantity = product.StockQuantity + line.Quantity,
                AfterQuantity = product.StockQuantity,
                Reason = "بيع عبر نقطة البيع",
                CreatedById = actor
            });
        }

        order.TotalAmount =
            order.Items.Sum(x => x.Total) +
            request.Items.Sum(x => x.Discount);

        order.FinalAmount =
            order.Items.Sum(x => x.Total) -
            request.OrderDiscount;

        if (order.FinalAmount < 0 ||
            request.AmountReceived < order.FinalAmount)
        {
            throw new InvalidOperationException("المبلغ المستلم غير كافٍ.");
        }

        order.AmountReceived = request.AmountReceived;
        order.Change = request.AmountReceived - order.FinalAmount;

        db.Orders.Add(order);

        db.CashTransactions.Add(new CashTransaction
        {
            ShiftId = shift.Id,
            Amount = order.FinalAmount,
            Type = CashTransactionType.Sale,
            Reference = order.Id.ToString(),
            Reason = "بيع عبر نقطة البيع",
            UserId = actor
        });

        await SaveAudit(
            actor,
            "OrderCompleted",
            "Order",
            order.Id,
            null,
            order.FinalAmount.ToString(),
            ct);

        await tx.CommitAsync(ct);

        return order;
    }

    public async Task<ShiftSummary?> GetOpenShiftAsync(
        CancellationToken ct = default)
    {
        return await db.Shifts
            .AsNoTracking()
            .Where(x => x.IsOpen)
            .OrderByDescending(x => x.OpenedAt)
            .Select(x => new ShiftSummary(
                x.Id,
                x.OpeningCash,
                x.ExpectedCash,
                x.IsOpen,
                x.OpenedAt,
                x.ClosedAt,
                x.ActualCash,
                x.Difference))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Shift> OpenShiftAsync(
        Guid actor,
        decimal cash,
        CancellationToken ct = default)
    {
        await Role(actor, "Cashier", ct);

        if (cash < 0 ||
            await db.Shifts.AnyAsync(x => x.IsOpen, ct))
        {
            throw new InvalidOperationException("بيانات فتح الشيفت غير صحيحة.");
        }

        var shift = new Shift
        {
            OpenedById = actor,
            OpeningCash = cash
        };

        db.Shifts.Add(shift);

        db.CashTransactions.Add(new CashTransaction
        {
            ShiftId = shift.Id,
            Amount = cash,
            Type = CashTransactionType.Opening,
            Reason = "رصيد افتتاحي",
            UserId = actor
        });

        await SaveAudit(
            actor,
            "ShiftOpened",
            "Shift",
            shift.Id,
            null,
            cash.ToString(),
            ct);

        return shift;
    }

    public async Task<Shift> CloseShiftAsync(
        Guid actor,
        Guid id,
        CloseShiftRequest request,
        CancellationToken ct = default)
    {
        await Role(actor, "Cashier", ct);

        if (request.ActualCash < 0)
            throw new ArgumentException("النقدية الفعلية غير صحيحة.");

        var shift = await db.Shifts
            .SingleOrDefaultAsync(x => x.Id == id && x.IsOpen, ct)
            ?? throw new InvalidOperationException("الشيفت غير مفتوح.");

        var expected =
            shift.OpeningCash +
            await db.CashTransactions
                .Where(x =>
                    x.ShiftId == id &&
                    x.Type != CashTransactionType.Opening &&
                    x.Type != CashTransactionType.Closing)
                .SumAsync(x => (decimal?)x.Amount, ct)
            ?? shift.OpeningCash;

        var difference = request.ActualCash - expected;

        if (Math.Abs(difference) > 0.01m &&
            string.IsNullOrWhiteSpace(request.DifferenceReason))
        {
            throw new InvalidOperationException("سبب فرق الخزينة مطلوب.");
        }

        shift.IsOpen = false;
        shift.ClosedById = actor;
        shift.ClosedAt = DateTimeOffset.UtcNow;
        shift.ExpectedCash = expected;
        shift.ActualCash = request.ActualCash;
        shift.Difference = difference;
        shift.DifferenceReason = request.DifferenceReason;

        db.CashTransactions.Add(new CashTransaction
        {
            ShiftId = id,
            Amount = request.ActualCash,
            Type = CashTransactionType.Closing,
            Reason = request.DifferenceReason ?? "إغلاق الخزينة",
            UserId = actor
        });

        await SaveAudit(
            actor,
            "ShiftClosed",
            "Shift",
            id,
            expected.ToString(),
            request.ActualCash.ToString(),
            ct,
            request.DifferenceReason);

        return shift;
    }

    private async Task<PlayOrderSummary> BuildPlayOrderSummary(
        Session session,
        CancellationToken ct)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
                .ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.SessionId == session.Id, ct);

        var items = order?.Items
            .OrderBy(x => x.Product.Name)
            .Select(x => new PlayOrderItemSummary(
                x.ProductId,
                x.Product.Name,
                x.Quantity,
                x.UnitPrice,
                x.Total))
            .ToList()
            ?? [];

        var productsAmount = items.Sum(x => x.Total);

        var playAmount = session.TotalAmount ?? 0m;

        if (session.Status is SessionStatus.Running or SessionStatus.Paused)
        {
            var pausedSeconds = session.Pauses.Sum(x => x.DurationSeconds ?? 0);
            var bill = billingCalculator.Calculate(
                session.StartedAt,
                clock.UtcNow,
                pausedSeconds,
                session.RatePerHour,
                BillingPolicy.Default);

            playAmount = bill.Amount;
        }

        return new PlayOrderSummary(
            session.Id,
            session.Device.Name,
            session.Device.Room?.Name,
            session.Mode,
            session.Status,
            session.StartedAt,
            session.EndedAt,
            session.Pauses.Sum(x => x.DurationSeconds ?? 0),
            session.RatePerHour,
            playAmount,
            productsAmount,
            playAmount + productsAmount,
            items);
    }

    private static void ValidateProductInput(
        string name,
        decimal sellingPrice,
        decimal costPrice,
        int minimumStockLevel)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            sellingPrice < 0 ||
            costPrice < 0 ||
            minimumStockLevel < 0)
        {
            throw new ArgumentException("بيانات المنتج غير صحيحة.");
        }
    }

    private async Task EnsureCategoryIsActive(
        Guid categoryId,
        CancellationToken ct)
    {
        if (!await db.ProductCategories.AnyAsync(
                x => x.Id == categoryId && x.IsActive,
                ct))
        {
            throw new ArgumentException("التصنيف غير موجود أو غير نشط.");
        }
    }

    private async Task EnsureBarcodeIsAvailable(
        string? barcode,
        Guid? exceptProductId,
        CancellationToken ct)
    {
        var normalized = NormalizeBarcode(barcode);

        if (normalized is null)
            return;

        var exists = await db.Products.AnyAsync(
            x => x.Barcode == normalized &&
                 (!exceptProductId.HasValue || x.Id != exceptProductId.Value),
            ct);

        if (exists)
            throw new ArgumentException("هذا الباركود مستخدم بالفعل لمنتج آخر.");
    }

    private static string? NormalizeBarcode(string? barcode)
    {
        var value = barcode?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private async Task<Product> Product(
        Guid id,
        CancellationToken ct)
    {
        return await db.Products
            .SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("المنتج غير موجود.");
    }

    private async Task Role(
        Guid actor,
        string need,
        CancellationToken ct)
    {
        var roles = await db.Users
            .AsNoTracking()
            .Where(x => x.Id == actor && x.IsActive)
            .SelectMany(x => x.Roles)
            .Select(x => x.Name)
            .ToListAsync(ct);

        var allowed =
            need == "Cashier"
                ? roles.Any(x => x is "Cashier" or "Manager" or "Admin")
                : need == "Manager"
                    ? roles.Any(x => x is "Manager" or "Admin")
                    : roles.Contains("Admin");

        if (!allowed)
            throw new UnauthorizedAccessException("ليس لديك صلاحية تنفيذ هذا الإجراء.");
    }

    private async Task SaveAudit(
        Guid actor,
        string action,
        string entityType,
        Guid id,
        string? oldValue,
        string? newValue,
        CancellationToken ct,
        string? reason = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = actor,
            Action = action,
            EntityType = entityType,
            EntityId = id,
            OldValue = oldValue,
            NewValue = newValue,
            Reason = reason
        });

        await db.SaveChangesAsync(ct);
    }
}
