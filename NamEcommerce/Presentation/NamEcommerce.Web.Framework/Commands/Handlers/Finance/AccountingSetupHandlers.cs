using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Finance;

public sealed class SaveAccountingSetupHandler(IAccountingSetupAppService appService)
    : IRequestHandler<SaveAccountingSetupCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(SaveAccountingSetupCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.SaveSetupAsync(new SaveAccountingSetupAppDto
        {
            FiscalYearStartMonth = request.FiscalYearStartMonth,
            FiscalYearStartDay = request.FiscalYearStartDay,
            AccountingStartDate = request.AccountingStartDate,
            OpeningCash = request.OpeningCash,
            OpeningEquity = request.OpeningEquity,
            DefaultTaxRate = request.DefaultTaxRate
        }).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class FinalizeAccountingSetupHandler(IAccountingSetupAppService appService)
    : IRequestHandler<FinalizeAccountingSetupCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(FinalizeAccountingSetupCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.FinalizeSetupAsync().ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class UpdateCorporateTaxProvisionHandler(IAccountingSetupAppService appService)
    : IRequestHandler<UpdateCorporateTaxProvisionCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(UpdateCorporateTaxProvisionCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.UpdateCorporateTaxProvisionAsync(request.Amount).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
