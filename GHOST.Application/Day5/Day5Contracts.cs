using GHOST.Domain.Entities;
using GHOST.Domain.Enums;

namespace GHOST.Application.Day5;

public sealed record ProductRequest(
    string Name,
    Guid CategoryId,
    decimal SellingPrice,
    decimal CostPrice,
    int MinimumStockLevel,
    string? Barcode = null);

public sealed record UpdateProductRequest(
    Guid ProductId,
    string Name,
    Guid CategoryId,
    decimal SellingPrice,
    decimal CostPrice,
    int MinimumStockLevel,
    string? Barcode = null);

public enum ProductCatalogStatus
{
    All,
    Active,
    Inactive,
    LowStock
}

public sealed record ProductCatalogFilterRequest(
    string? SearchText = null,
    Guid? CategoryId = null,
    ProductCatalogStatus Status = ProductCatalogStatus.Active,
    int Page = 1,
    int PageSize = 50);

public sealed record ProductCatalogResult(
    IReadOnlyList<ProductSummary> Items,
    int TotalCount,
    int ActiveCount,
    int LowStockCount,
    decimal RetailStockValue,
    decimal CostStockValue,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record ProductSummary(
    Guid Id,
    string Name,
    string? Barcode,
    Guid CategoryId,
    string CategoryName,
    decimal SellingPrice,
    decimal CostPrice,
    int StockQuantity,
    int MinimumStockLevel,
    bool IsActive);

public sealed record CategorySummary(
    Guid Id,
    string Name,
    bool IsActive);

public sealed record InventoryRequest(
    Guid ProductId,
    InventoryTransactionType Type,
    int Quantity,
    string Reason);

public sealed record InventoryHistoryFilter(
    Guid? ProductId = null,
    InventoryTransactionType? Type = null,
    int Page = 1,
    int PageSize = 50);

public sealed record InventoryHistoryItem(
    Guid Id,
    Guid ProductId,
    string ProductName,
    InventoryTransactionType Type,
    int Quantity,
    int BeforeQuantity,
    int AfterQuantity,
    string Reason,
    Guid CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt);

public sealed record PosItem(
    Guid ProductId,
    int Quantity,
    decimal Discount = 0m);

public sealed record CompleteOrderRequest(
    IReadOnlyList<PosItem> Items,
    decimal AmountReceived,
    Guid? CustomerId = null,
    Guid? SessionId = null,
    decimal OrderDiscount = 0m);


public sealed record AddProductToPlayRequest(
    Guid SessionId,
    Guid ProductId,
    int Quantity);

public sealed record PlayOrderItemSummary(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Total);

public sealed record PlayOrderSummary(
    Guid SessionId,
    string DeviceName,
    string? RoomName,
    SessionMode Mode,
    SessionStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int TotalPausedSeconds,
    decimal RatePerHour,
    decimal PlayAmount,
    decimal ProductsAmount,
    decimal TotalAmount,
    IReadOnlyList<PlayOrderItemSummary> Items);

public sealed record CloseShiftRequest(
    decimal ActualCash,
    string? DifferenceReason);

public sealed record ShiftSummary(
    Guid Id,
    decimal OpeningCash,
    decimal ExpectedCash,
    bool IsOpen,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    decimal? ActualCash,
    decimal? Difference);

public interface IDay5Service
{
    Task<ProductCatalogResult> GetProductCatalogAsync(
        ProductCatalogFilterRequest request,
        CancellationToken ct = default);

    Task<IReadOnlyList<ProductSummary>> GetProductsAsync(
        bool includeInactive = true,
        CancellationToken ct = default);

    Task<IReadOnlyList<CategorySummary>> GetCategoriesAsync(
        bool includeInactive = true,
        CancellationToken ct = default);

    Task<ProductSummary?> GetProductByBarcodeAsync(
        string barcode,
        CancellationToken ct = default);

    Task<ProductCategory> CreateCategoryAsync(
        Guid actor,
        string name,
        CancellationToken ct = default);

    Task SetCategoryActiveAsync(
        Guid actor,
        Guid categoryId,
        bool active,
        CancellationToken ct = default);

    Task<Product> CreateProductAsync(
        Guid actor,
        ProductRequest request,
        CancellationToken ct = default);

    Task UpdateProductAsync(
        Guid actor,
        UpdateProductRequest request,
        CancellationToken ct = default);

    Task SetProductActiveAsync(
        Guid actor,
        Guid productId,
        bool active,
        CancellationToken ct = default);

    Task<InventoryTransaction> ChangeStockAsync(
        Guid actor,
        InventoryRequest request,
        CancellationToken ct = default);

    Task<IReadOnlyList<Product>> GetLowStockAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<InventoryHistoryItem>> GetInventoryHistoryAsync(
        InventoryHistoryFilter filter,
        CancellationToken ct = default);

    Task<Order> CompleteOrderAsync(
        Guid actor,
        CompleteOrderRequest request,
        CancellationToken ct = default);

    Task<PlayOrderSummary> GetPlayDetailsAsync(
        Guid sessionId,
        CancellationToken ct = default);

    Task<PlayOrderSummary> AddProductToPlayAsync(
        Guid actor,
        AddProductToPlayRequest request,
        CancellationToken ct = default);

    Task<ShiftSummary?> GetOpenShiftAsync(
        CancellationToken ct = default);

    Task<Shift> OpenShiftAsync(
        Guid actor,
        decimal openingCash,
        CancellationToken ct = default);

    Task<Shift> CloseShiftAsync(
        Guid actor,
        Guid shiftId,
        CloseShiftRequest request,
        CancellationToken ct = default);
}
