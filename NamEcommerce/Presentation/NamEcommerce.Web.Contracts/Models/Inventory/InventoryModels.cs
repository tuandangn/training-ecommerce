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
        public required decimal TotalReservedByOrder { get; init; }
        public required decimal QuantityAvailable { get; init; }
        public required DateTime UpdatedOn { get; init; }
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
        public required int Reason { get; init; }
        public Guid? ReferenceId { get; init; }
        public required DateTime CreatedOn { get; init; }
    }
}
