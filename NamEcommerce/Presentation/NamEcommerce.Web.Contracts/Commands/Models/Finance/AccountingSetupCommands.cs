using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Finance;

public sealed class SaveAccountingSetupCommand : IRequest<CommonActionResultModel>
{
    public int FiscalYearStartMonth { get; init; }
    public int FiscalYearStartDay { get; init; }
    public DateTime AccountingStartDate { get; init; }
    public decimal OpeningCash { get; init; }
    public decimal OpeningEquity { get; init; }
    public decimal DefaultTaxRate { get; init; }
}

public sealed class FinalizeAccountingSetupCommand : IRequest<CommonActionResultModel>;

public sealed class UpdateCorporateTaxProvisionCommand : IRequest<CommonActionResultModel>
{
    public decimal? Amount { get; init; }
}
