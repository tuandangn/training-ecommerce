using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Events.Finance;
using NamEcommerce.Domain.Shared.Exceptions.Finance;

namespace NamEcommerce.Domain.Entities.Finance;

[Serializable]
public sealed record FixedAsset : AppAggregateEntity
{
    private FixedAsset() : base(Guid.Empty) { }

    internal FixedAsset(
        string code,
        string name,
        string? description,
        FixedAssetCategory category,
        FixedAssetCostCenter costCenter,
        DateTime acquisitionDate,
        decimal acquisitionCost,
        decimal residualValue,
        int usefulLifeMonths,
        Guid? vendorId,
        string? vendorInvoiceNumber,
        string? note) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (acquisitionCost <= 0)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.AcquisitionCostMustBePositive");
        if (residualValue < 0 || residualValue >= acquisitionCost)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.ResidualValueInvalid");
        if (usefulLifeMonths <= 0)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.UsefulLifeMustBePositive");

        Code = code;
        Name = name;
        Description = description;
        Category = category;
        CostCenter = costCenter;
        AcquisitionDate = acquisitionDate;
        AcquisitionCost = acquisitionCost;
        ResidualValue = residualValue;
        UsefulLifeMonths = usefulLifeMonths;
        VendorId = vendorId;
        VendorInvoiceNumber = vendorInvoiceNumber;
        Note = note;
        Status = FixedAssetStatus.Active;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public FixedAssetCategory Category { get; private set; }
    public FixedAssetCostCenter CostCenter { get; private set; }
    public DateTime AcquisitionDate { get; private set; }
    public decimal AcquisitionCost { get; private set; }
    public decimal ResidualValue { get; private set; }
    public int UsefulLifeMonths { get; private set; }
    public Guid? VendorId { get; private set; }
    public string? VendorInvoiceNumber { get; private set; }
    public string? Note { get; private set; }
    public FixedAssetStatus Status { get; private set; }
    public DateTime? DisposedOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    // Ngày bắt đầu tính KH: mua ngày 1 → từ tháng đó; ngày 2+ → tháng kế tiếp (TT200 điều 10)
    public DateTime DepreciationStartDate =>
        AcquisitionDate.Day == 1
            ? new DateTime(AcquisitionDate.Year, AcquisitionDate.Month, 1)
            : new DateTime(AcquisitionDate.Year, AcquisitionDate.Month, 1).AddMonths(1);

    public decimal MonthlyDepreciation =>
        (AcquisitionCost - ResidualValue) / UsefulLifeMonths;

    public int ElapsedDepreciationMonths(DateTime asOf)
    {
        var effectiveAsOf = Status == FixedAssetStatus.Disposed && DisposedOnUtc.HasValue
            ? DisposedOnUtc.Value
            : asOf;

        var start = DepreciationStartDate;
        if (effectiveAsOf < start) return 0;

        var months = (effectiveAsOf.Year - start.Year) * 12
                     + (effectiveAsOf.Month - start.Month) + 1;
        return Math.Min(months, UsefulLifeMonths);
    }

    public decimal GetAccumulatedDepreciation(DateTime asOf)
        => Math.Round(MonthlyDepreciation * ElapsedDepreciationMonths(asOf), 0);

    public decimal GetBookValue(DateTime asOf)
        => AcquisitionCost - GetAccumulatedDepreciation(asOf);

    public decimal GetDepreciationForMonth(int year, int month)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var start = DepreciationStartDate;

        if (monthEnd < start) return 0;
        if (Status == FixedAssetStatus.Disposed && DisposedOnUtc.HasValue && DisposedOnUtc.Value < monthStart) return 0;

        var elapsed = ElapsedDepreciationMonths(monthEnd);
        if (elapsed <= 0 || elapsed > UsefulLifeMonths) return 0;
        return Math.Round(MonthlyDepreciation, 0);
    }

    internal void UpdateInfo(string name, string? description, string? note, FixedAssetCostCenter costCenter)
    {
        if (Status == FixedAssetStatus.Disposed)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.CannotEditDisposed");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Description = description;
        Note = note;
        CostCenter = costCenter;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Dispose(DateTime disposedOnUtc)
    {
        if (Status == FixedAssetStatus.Disposed)
            throw new FixedAssetDataInvalidException("Error.FixedAsset.AlreadyDisposed");
        Status = FixedAssetStatus.Disposed;
        DisposedOnUtc = disposedOnUtc;
        UpdatedOnUtc = DateTime.UtcNow;
        RaiseDomainEvent(new FixedAssetDisposed(Id, GetBookValue(disposedOnUtc)));
    }

    internal void CheckAndMarkFullyDepreciated(DateTime asOf)
    {
        if (Status == FixedAssetStatus.Active && ElapsedDepreciationMonths(asOf) >= UsefulLifeMonths)
        {
            Status = FixedAssetStatus.FullyDepreciated;
            UpdatedOnUtc = DateTime.UtcNow;
        }
    }

    internal void MarkCreated() => RaiseDomainEvent(new FixedAssetCreated(Id, Name));
}
