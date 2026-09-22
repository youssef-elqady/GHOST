using GHOST.Domain.Entities; using GHOST.Domain.Enums;
namespace GHOST.Application.Day5;
public sealed record ProductRequest(string Name, Guid CategoryId, decimal SellingPrice, decimal CostPrice, int MinimumStockLevel);
public sealed record InventoryRequest(Guid ProductId, InventoryTransactionType Type, int Quantity, string Reason);
public sealed record PosItem(Guid ProductId, int Quantity, decimal Discount = 0m);
public sealed record CompleteOrderRequest(IReadOnlyList<PosItem> Items, decimal AmountReceived, Guid? CustomerId = null, Guid? SessionId = null, decimal OrderDiscount = 0m);
public sealed record CloseShiftRequest(decimal ActualCash, string? DifferenceReason);
public interface IDay5Service { Task<ProductCategory> CreateCategoryAsync(Guid actor, string name, CancellationToken ct = default); Task<Product> CreateProductAsync(Guid actor, ProductRequest request, CancellationToken ct = default); Task SetProductActiveAsync(Guid actor, Guid productId, bool active, CancellationToken ct = default); Task<InventoryTransaction> ChangeStockAsync(Guid actor, InventoryRequest request, CancellationToken ct = default); Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken ct = default); Task<Order> CompleteOrderAsync(Guid actor, CompleteOrderRequest request, CancellationToken ct = default); Task<Shift> OpenShiftAsync(Guid actor, decimal openingCash, CancellationToken ct = default); Task<Shift> CloseShiftAsync(Guid actor, Guid shiftId, CloseShiftRequest request, CancellationToken ct = default); }
