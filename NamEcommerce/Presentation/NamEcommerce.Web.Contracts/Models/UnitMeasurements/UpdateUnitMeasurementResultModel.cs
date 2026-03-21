namespace NamEcommerce.Web.Contracts.Models.UnitMeasurements;

[Serializable]
public sealed record UpdateUnitMeasurementResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }

    public Guid UpdatedId { get; init; }
}
