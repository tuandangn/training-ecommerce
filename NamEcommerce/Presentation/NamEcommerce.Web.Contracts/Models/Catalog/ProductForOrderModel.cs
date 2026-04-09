namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed record ProductForOrderModel(Guid Id)
{
    public required string Name { get; init; }
    public string? PictureUrl { get; set; }

    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityAvailable { get; set; }

    public IEnumerable<Guid> AvailableWarehouseIds { get; set; } = [];
}
