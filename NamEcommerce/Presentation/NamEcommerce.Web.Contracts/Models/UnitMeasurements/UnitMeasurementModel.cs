namespace NamEcommerce.Web.Contracts.Models.UnitMeasurements;

[Serializable]
public sealed record UnitMeasurementModel
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }
}
