namespace NamEcommerce.Web.Contracts.Models.Inventory;

[Serializable]
public sealed record ProductStockInfoModel
{
    public Guid ProductId { get; set; }
    public Guid? WarehouseId { get; set; }

    public required decimal QuantityOnHand { get; set; }
    public required decimal QuantityReserved { get; set; }
    public required decimal QuantityAvailable { get; set; }
}
