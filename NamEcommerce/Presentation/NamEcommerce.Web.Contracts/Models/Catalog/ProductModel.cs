using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed record ProductModel
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? ShortDesc { get; init; }
    public Guid? CategoryId { get; set; }
    public IList<Guid> VendorIds { get; set; } = [];
    public Guid? UnitMeasurementId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int DisplayOrder { get; set; }
    public Base64ImageModel? ImageFile { get; set; }
} 