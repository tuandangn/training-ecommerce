using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Finance;

public sealed class SaveAccountingSetupCommand : ICommand<CommonActionResultModel>
{
    public int FiscalYearStartMonth { get; init; }
    public int FiscalYearStartDay { get; init; }
    public DateTime AccountingStartDate { get; init; }
    public decimal OpeningCash { get; init; }
    public decimal OpeningEquity { get; init; }
    public decimal DefaultTaxRate { get; init; }
}

public sealed class FinalizeAccountingSetupCommand : ICommand<CommonActionResultModel>;

public sealed class UpdateCorporateTaxProvisionCommand : ICommand<CommonActionResultModel>
{
    public decimal? Amount { get; init; }
}
