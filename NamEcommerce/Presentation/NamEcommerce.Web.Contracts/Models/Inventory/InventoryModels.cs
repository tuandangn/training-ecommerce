using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Inventory;

[Serializable]
public sealed class InventoryStockListModel
{
    public string? Keywords { get; init; }
    public Guid? WarehouseId { get; init; }
    public bool IncludeDirectTransit { get; set; }
    public bool GroupByProduct { get; init; }
    public required IPagedDataModel<ItemModel> Data { get; init; }
    public required IPagedDataModel<GroupedItemModel> GroupedData { get; init; }

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required Guid ProductId { get; init; }
        public required string ProductName { get; init; }
        public required Guid WarehouseId { get; init; }
        public required string WarehouseName { get; init; }
        public required decimal QuantityOnHand { get; init; }
        public required decimal QuantityReserved { get; init; }
        public required decimal TotalReservedByOrder { get; init; }
        public required decimal QuantityAvailable { get; init; }
        public required decimal CurrentUnitCost { get; init; }
        public required DateTime UpdatedOn { get; init; }
        public decimal ReorderLevel { get; init; }
        public decimal MaxStockLevel { get; init; }
        public IReadOnlyList<CostHistoryItemModel> CostHistory { get; init; } = [];
    }

    [Serializable]
    public sealed record GroupedItemModel(Guid ProductId)
    {
        public required string ProductName { get; init; }
        public required decimal QuantityOnHand { get; init; }
        public required decimal QuantityReserved { get; init; }
        public required decimal TotalReservedByOrder { get; init; }
        public required decimal QuantityAvailable { get; init; }
        public required decimal CurrentUnitCost { get; init; }
        public required DateTime UpdatedOn { get; init; }
        public required IReadOnlyList<ItemModel> Warehouses { get; init; }
        public IReadOnlyList<CostHistoryItemModel> CostHistory { get; init; } = [];
    }

    [Serializable]
    public sealed record CostHistoryItemModel(Guid Id)
    {
        public required Guid ProductId { get; init; }
        public required string ProductName { get; init; }
        public required Guid WarehouseId { get; init; }
        public required string WarehouseName { get; init; }
        public required DateTime OccurredAt { get; init; }
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
}

[Serializable]
public sealed class StockMovementLogListModel
{
    public Guid? ProductId { get; init; }
    public Guid? WarehouseId { get; init; }
    public required IPagedDataModel<ItemModel> Data { get; init; }

    [Serializable]
    public sealed record ItemModel(Guid Id)
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
}

[Serializable]
public sealed class ProductReservationLedgerListModel
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required IPagedDataModel<ItemModel> Data { get; init; }

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required Guid ProductId { get; init; }
        public required Guid OrderId { get; init; }
        public string? OrderCode { get; init; }
        public required decimal QuantityDelta { get; init; }
        public decimal? UnitPrice { get; init; }
        public required int Reason { get; init; }
        public Guid? ReferenceId { get; init; }
        public required DateTime CreatedOn { get; init; }
    }
}

[Serializable]
public sealed record InventoryCostingPolicySettingsModel
{
    public required Guid Id { get; init; }
    public required int CostingMethod { get; init; }
    public required int ValuationScope { get; init; }
    public required DateTime EffectiveFrom { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed record UpdateInventoryCostingPolicyResultModel : ICommandResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? UpdatedId { get; init; }
}

[Serializable]
public sealed record RebuildInventoryCostingResultModel : ICommandResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? RebuildRunId { get; init; }
}
