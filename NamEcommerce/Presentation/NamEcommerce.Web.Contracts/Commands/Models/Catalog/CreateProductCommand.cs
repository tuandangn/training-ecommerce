using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Commands.Models.Catalog;

[Serializable]
public sealed class CreateProductCommand : ICommand<CreateProductResultModel>
{
    public required string Name { get; init; }
    public string? ShortDesc { get; set; }
    public Guid? CategoryId { get; set; }
    public IList<Guid> VendorIds { get; set; } = [];
    public Guid? UnitMeasurementId { get; set; }
    public int DisplayOrder { get; set; }
    public Guid? PictureId { get; set; }
    public decimal? UnitPrice { get; set; }

    public IEnumerable<ProductStockModel>? ProductStocks { get; set; }

    public sealed record ProductStockModel(Guid WarehouseId, decimal Quantity, decimal UnitCost);
}
