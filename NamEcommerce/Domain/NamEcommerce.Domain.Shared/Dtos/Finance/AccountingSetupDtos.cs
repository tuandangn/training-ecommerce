using NamEcommerce.Domain.Shared.Exceptions.Finance;

namespace NamEcommerce.Domain.Shared.Dtos.Finance;

public sealed class AccountingSetupDto
{
    public Guid Id { get; set; }
    public int FiscalYearStartMonth { get; set; }
    public int FiscalYearStartDay { get; set; }
    public DateTime AccountingStartDate { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal OpeningEquity { get; set; }
    public decimal DefaultTaxRate { get; set; }
    public decimal? CorporateTaxProvision { get; set; }
    public bool IsFinalized { get; set; }
    public DateTime? FinalizedOnUtc { get; set; }
}

public sealed class SaveAccountingSetupDto
{
    public int FiscalYearStartMonth { get; set; }
    public int FiscalYearStartDay { get; set; }
    public DateTime AccountingStartDate { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal OpeningEquity { get; set; }
    public decimal DefaultTaxRate { get; set; }

    public void Verify()
    {
        if (FiscalYearStartMonth is < 1 or > 12)
            throw new AccountingSetupDataInvalidException("Error.Accounting.InvalidFiscalYearMonth");
        if (FiscalYearStartDay is < 1 or > 28)
            throw new AccountingSetupDataInvalidException("Error.Accounting.InvalidFiscalYearDay");
        if (OpeningCash < 0)
            throw new AccountingSetupDataInvalidException("Error.Accounting.OpeningCashCannotBeNegative");
        if (OpeningEquity < 0)
            throw new AccountingSetupDataInvalidException("Error.Accounting.OpeningEquityCannotBeNegative");
        if (DefaultTaxRate is not (0 or 0.05m or 0.08m or 0.10m))
            throw new AccountingSetupDataInvalidException("Error.Accounting.InvalidTaxRate");
    }
}
