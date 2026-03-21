namespace NamEcommerce.Web.Contracts.Models.UnitMeasurements;

[Serializable]
public sealed record DeleteUnitMeasurementResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }
}
