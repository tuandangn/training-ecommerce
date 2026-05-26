using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Dtos.Inventory;

[Serializable]
public sealed record InventoryStockAppDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public required decimal QuantityOnHand { get; init; }
    public required decimal QuantityReserved { get; init; }
    public required decimal TotalReservedByOrder { get; init; }
    public required decimal QuantityAvailable { get; init; }
    public required decimal CurrentUnitCost { get; init; }
    public required DateTime UpdatedOnUtc { get; init; }
    public decimal ReorderLevel { get; init; }
    public decimal MaxStockLevel { get; init; }
    public IReadOnlyList<InventoryCostHistoryAppDto> CostHistory { get; init; } = [];
}

[Serializable]
public sealed record InventoryStockByProductAppDto
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal QuantityOnHand { get; init; }
    public required decimal QuantityReserved { get; init; }
    public required decimal TotalReservedByOrder { get; init; }
    public required decimal QuantityAvailable { get; init; }
    public required decimal CurrentUnitCost { get; init; }
    public required DateTime UpdatedOnUtc { get; init; }
    public required IReadOnlyList<InventoryStockAppDto> Warehouses { get; init; }
    public IReadOnlyList<InventoryCostHistoryAppDto> CostHistory { get; init; } = [];
}

[Serializable]
public sealed record InventoryCostHistoryAppDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public required DateTime OccurredAtUtc { get; init; }
    public required long SequenceNumber { get; init; }
    public required int MovementType { get; init; }
    public required decimal QuantityDelta { get; init; }
    public decimal? UnitCost { get; init; }
    public decimal? TotalCost { get; init; }
    public required decimal QuantityBalanceAfter { get; init; }
    public required decimal ValueBalanceAfter { get; init; }
    public required decimal AverageCostAfter { get; init; }
    public required int CostingStatus { get; init; }
    public required int ReferenceType { get; init; }
    public required Guid ReferenceId { get; init; }
    public required Guid ReferenceItemId { get; init; }
}

[Serializable]
public sealed record ProductInventoryStockInfoAppDto
{
    public required Guid ProductId { get; init; }
    public string? ProductName { get; set; }
    public Guid? WarehouseId { get; init; }
    public string? WarehouseName { get; set; }
    public required decimal QuantityOnHand { get; init; }
    public required decimal QuantityReserved { get; init; }
    public required decimal QuantityAvailable { get; init; }
    public DateTime UpdatedOnUtc { get; init; }
}


[Serializable]
public sealed record StockMovementLogAppDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required int MovementType { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal QuantityBefore { get; init; }
    public required decimal QuantityAfter { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed record ProductReservationLedgerAppDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required Guid OrderId { get; init; }
    public string? OrderCode { get; init; }
    public required decimal QuantityDelta { get; init; }
    public decimal? UnitPrice { get; init; }
    public required int Reason { get; init; }
    public Guid? ReferenceId { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed class ResultAppDto
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }
    public Guid? CreatedId { get; set; }
}

[Serializable]
public sealed record SetStockLevelsAppDto(Guid Id)
{
    public required decimal ReorderLevel { get; init; }
    public required decimal MaxStockLevel { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (Id == Guid.Empty)
            return (false, "Error.StockIdRequired");
        if (ReorderLevel < 0)
            return (false, "Error.ReorderLevelMustBeNonNegative");
        if (MaxStockLevel < 0)
            return (false, "Error.MaxStockLevelMustBeNonNegative");
        if (MaxStockLevel > 0 && MaxStockLevel < ReorderLevel)
            return (false, "Error.MaxStockLevelMustBeGreaterOrEqualReorderLevel");
        return (true, null);
    }
}

[Serializable]
public sealed record SetStockLevelsResultAppDto
{
    public required bool Success { get; init; }
    public Guid UpdatedId { get; set; }
    public string? ErrorMessage { get; set; }
}
