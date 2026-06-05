namespace NamEcommerce.Web.Contracts.Models.Inventory;

[Serializable]
public sealed record ProductStockInfoModel
{
    public Guid ProductId { get; set; }
    public Guid? WarehouseId { get; set; }

    public required decimal QuantityOnHand { get; set; }
    public required decimal QuantityReserved { get; set; }
    public required decimal QuantityAvailable { get; set; }
    public IEnumerable<Guid> AvailableWarehouseIds { get; set; } = [];
    public IList<ProductStockWarehouseInfoModel> Warehouses { get; init; } = [];
}

[Serializable]
public sealed record ProductStockWarehouseInfoModel
{
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public required decimal QuantityOnHand { get; init; }
    public required decimal QuantityReserved { get; init; }
    public required decimal QuantityAvailable { get; init; }
}
