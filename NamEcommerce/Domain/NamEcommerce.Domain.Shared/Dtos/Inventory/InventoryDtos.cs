namespace NamEcommerce.Domain.Shared.Dtos.Inventory;

[Serializable]
public sealed record InventoryStockDto(Guid Id)
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public required decimal QuantityOnHand { get; init; }
    public required decimal QuantityReserved { get; init; }
    public required decimal QuantityAvailable { get; init; }
    public required DateTime UpdatedOnUtc { get; init; }
}

[Serializable]
public sealed record StockMovementLogDto(Guid Id)
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required int MovementType { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal QuantityBefore { get; init; }
    public required decimal QuantityAfter { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public string? Note { get; init; }
}
