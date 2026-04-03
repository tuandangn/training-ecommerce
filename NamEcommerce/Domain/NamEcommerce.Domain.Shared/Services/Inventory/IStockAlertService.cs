namespace NamEcommerce.Domain.Shared.Services.Inventory;

/// <summary>
/// Service for managing inventory alerts and notifications (low stock, reorder, etc.)
/// </summary>
public interface IStockAlertService
{
    /// <summary>
    /// Alert that stock has fallen below reorder level
    /// </summary>
    Task AlertLowStockAsync(Guid productId, Guid warehouseId, decimal currentQuantity, decimal reorderLevel);

    /// <summary>
    /// Alert that stock has exceeded maximum level
    /// </summary>
    Task AlertOverstockAsync(Guid productId, Guid warehouseId, decimal currentQuantity, decimal maxLevel);

    /// <summary>
    /// Get all active low stock alerts
    /// </summary>
    Task<List<StockAlertDto>> GetActiveLowStockAlertsAsync(Guid? warehouseId = null);
}

/// <summary>
/// DTO for stock alerts
/// </summary>
[Serializable]
public sealed record StockAlertDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public required decimal CurrentQuantity { get; init; }
    public required decimal ReorderLevel { get; init; }
    public required string AlertType { get; init; } // "LowStock", "Overstock"
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? ResolvedOnUtc { get; init; }
}
