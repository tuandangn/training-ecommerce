namespace NamEcommerce.Web.Contracts.Models.UnitMeasurements;

[Serializable]
public sealed record CreateUnitMeasurementResultModel
{
    public required bool Success { get; init; }
    public required string? ErrorMessage { get; init; }

    public Guid CreatedId { get; set; }
}
