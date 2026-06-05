using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Exceptions.Finance;

namespace NamEcommerce.Domain.Entities.Finance;

[Serializable]
public sealed record AccountingSetup : AppAggregateEntity
{
    private AccountingSetup() : base(Guid.Empty) { }

    internal AccountingSetup(
        int fiscalYearStartMonth,
        int fiscalYearStartDay,
        DateTime accountingStartDate,
        decimal openingCash,
        decimal openingEquity,
        decimal defaultTaxRate) : base(Guid.NewGuid())
    {
        FiscalYearStartMonth = fiscalYearStartMonth;
        FiscalYearStartDay = fiscalYearStartDay;
        AccountingStartDate = accountingStartDate;
        OpeningCash = openingCash;
        OpeningEquity = openingEquity;
        DefaultTaxRate = defaultTaxRate;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public int FiscalYearStartMonth { get; private set; }
    public int FiscalYearStartDay { get; private set; }
    public DateTime AccountingStartDate { get; private set; }
    public decimal OpeningCash { get; private set; }
    public decimal OpeningEquity { get; private set; }
    public decimal DefaultTaxRate { get; private set; }
    public decimal? CorporateTaxProvision { get; private set; }
    public bool IsFinalized { get; private set; }
    public DateTime? FinalizedOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    internal void Update(
        int fiscalYearStartMonth, int fiscalYearStartDay,
        DateTime accountingStartDate,
        decimal openingCash, decimal openingEquity,
        decimal defaultTaxRate)
    {
        if (IsFinalized)
            throw new AccountingSetupAlreadyFinalizedException();

        FiscalYearStartMonth = fiscalYearStartMonth;
        FiscalYearStartDay = fiscalYearStartDay;
        AccountingStartDate = accountingStartDate;
        OpeningCash = openingCash;
        OpeningEquity = openingEquity;
        DefaultTaxRate = defaultTaxRate;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void ConfirmAndLock()
    {
        if (IsFinalized)
            throw new AccountingSetupAlreadyFinalizedException();

        IsFinalized = true;
        FinalizedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void UpdateCorporateTaxProvision(decimal? amount)
    {
        if (amount.HasValue && amount.Value < 0)
            throw new AccountingSetupDataInvalidException("Error.Accounting.CorporateTaxProvisionCannotBeNegative");

        CorporateTaxProvision = amount;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
