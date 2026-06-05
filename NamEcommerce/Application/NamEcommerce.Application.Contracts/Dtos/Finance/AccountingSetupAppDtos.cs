namespace NamEcommerce.Application.Contracts.Dtos.Finance;

public sealed class AccountingSetupAppDto
{
    public Guid? Id { get; set; }
    public bool IsConfigured { get; set; }
    public bool IsFinalized { get; set; }
    public int FiscalYearStartMonth { get; set; }
    public int FiscalYearStartDay { get; set; }
    public DateTime AccountingStartDate { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal OpeningEquity { get; set; }
    public decimal DefaultTaxRate { get; set; }
    public decimal? CorporateTaxProvision { get; set; }
    public DateTime? FinalizedOnUtc { get; set; }
}

public sealed class SaveAccountingSetupAppDto
{
    public int FiscalYearStartMonth { get; set; }
    public int FiscalYearStartDay { get; set; }
    public DateTime AccountingStartDate { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal OpeningEquity { get; set; }
    public decimal DefaultTaxRate { get; set; }

    public (bool valid, string? error) Validate()
    {
        if (FiscalYearStartMonth is < 1 or > 12) return (false, "Error.Accounting.InvalidFiscalYearMonth");
        if (FiscalYearStartDay is < 1 or > 28) return (false, "Error.Accounting.InvalidFiscalYearDay");
        if (OpeningCash < 0) return (false, "Error.Accounting.OpeningCashCannotBeNegative");
        if (OpeningEquity < 0) return (false, "Error.Accounting.OpeningEquityCannotBeNegative");
        if (DefaultTaxRate is not (0 or 0.05m or 0.08m or 0.10m)) return (false, "Error.Accounting.InvalidTaxRate");
        return (true, null);
    }
}

public sealed class AccountingSetupOperationResultAppDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
