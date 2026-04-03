using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Inventory;

[Serializable]
public sealed class InventoryStockListModel
{
    public string? Keywords { get; init; }
    public Guid? WarehouseId { get; init; }
    public required IPagedDataModel<ItemModel> Data { get; init; }

    [Serializable]
    public sealed record ItemModel(Guid Id)
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
}

[Serializable]
public sealed record AdjustStockResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record ReserveStockResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record ReleaseReservedStockResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record DispatchStockResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record ReceiveStockResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }
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
