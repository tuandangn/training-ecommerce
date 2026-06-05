namespace NamEcommerce.Application.Contracts.Dtos.Finance;

public sealed record FixedAssetAppDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Category { get; init; }
    public string CategoryDisplay { get; init; } = string.Empty;
    public int CostCenter { get; init; }
    public DateTime AcquisitionDate { get; init; }
    public decimal AcquisitionCost { get; init; }
    public decimal ResidualValue { get; init; }
    public int UsefulLifeMonths { get; init; }
    public decimal MonthlyDepreciation { get; init; }
    public decimal AccumulatedDepreciation { get; init; }
    public decimal BookValue { get; init; }
    public int RemainingMonths { get; init; }
    public int Status { get; init; }
    public DateTime? DisposedOnUtc { get; init; }
}

public sealed record CreateFixedAssetAppDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int Category { get; init; }
    public int CostCenter { get; init; }
    public DateTime AcquisitionDate { get; init; }
    public decimal AcquisitionCost { get; init; }
    public decimal ResidualValue { get; init; }
    public int UsefulLifeMonths { get; init; }
    public string? VendorInvoiceNumber { get; init; }
    public string? Note { get; init; }

    public (bool valid, string? error) Validate()
    {
        if (string.IsNullOrWhiteSpace(Name)) return (false, "Error.FixedAsset.NameRequired");
        if (AcquisitionCost <= 0) return (false, "Error.FixedAsset.AcquisitionCostMustBePositive");
        if (ResidualValue < 0 || ResidualValue >= AcquisitionCost)
            return (false, "Error.FixedAsset.ResidualValueInvalid");
        if (UsefulLifeMonths <= 0) return (false, "Error.FixedAsset.UsefulLifeMustBePositive");
        return (true, null);
    }
}

public sealed record FixedAssetOperationResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? AssetId { get; init; }
}

public sealed record DepreciationScheduleItemAppDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal DepreciationAmount { get; init; }
    public decimal CumulativeDepreciation { get; init; }
    public decimal BookValue { get; init; }
}
