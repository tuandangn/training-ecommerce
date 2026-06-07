using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Exceptions.Finance;

namespace NamEcommerce.Domain.Shared.Dtos.Finance;

public sealed record FixedAssetDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public FixedAssetCategory Category { get; init; }
    public FixedAssetCostCenter CostCenter { get; init; }
    public DateTime AcquisitionDate { get; init; }
    public decimal AcquisitionCost { get; init; }
    public decimal ResidualValue { get; init; }
    public int UsefulLifeMonths { get; init; }
    public decimal MonthlyDepreciation { get; init; }
    public string? VendorInvoiceNumber { get; init; }
    public string? Note { get; init; }
    public FixedAssetStatus Status { get; init; }
    public DateTime? DisposedOnUtc { get; init; }
}

public sealed record CreateFixedAssetDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public FixedAssetCategory Category { get; init; }
    public FixedAssetCostCenter CostCenter { get; init; }
    public DateTime AcquisitionDate { get; init; }
    public decimal AcquisitionCost { get; init; }
    public decimal ResidualValue { get; init; }
    public int UsefulLifeMonths { get; init; }
    public Guid? VendorId { get; init; }
    public string? VendorInvoiceNumber { get; init; }
    public string? Note { get; init; }

    public void Verify()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new FixedAssetDataInvalidException("Error.FixedAsset.NameRequired");
        if (AcquisitionCost <= 0)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.AcquisitionCostMustBePositive");
        if (ResidualValue < 0 || ResidualValue >= AcquisitionCost)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.ResidualValueInvalid");
        if (UsefulLifeMonths <= 0)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.UsefulLifeMustBePositive");
    }
}
