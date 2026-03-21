namespace NamEcommerce.Web.Models.Catalog;

[Serializable]
public sealed record EditUnitMeasurementModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public int DisplayOrder { get; set; }
}
