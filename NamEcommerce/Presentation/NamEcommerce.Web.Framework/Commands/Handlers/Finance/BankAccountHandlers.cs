using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Finance;

public sealed class CreateBankAccountHandler(IBankAccountAppService appService)
    : IRequestHandler<CreateBankAccountCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CreateBankAccountCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.CreateBankAccountAsync(new CreateBankAccountAppDto
        {
            DisplayName = request.DisplayName,
            BankCode = request.BankCode,
            BankName = request.BankName,
            AccountNumber = request.AccountNumber,
            AccountHolderName = request.AccountHolderName,
            OpeningBalance = request.OpeningBalance,
            SetAsDefault = request.SetAsDefault
        }).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class UpdateBankAccountHandler(IBankAccountAppService appService)
    : IRequestHandler<UpdateBankAccountCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(UpdateBankAccountCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.UpdateBankAccountAsync(new UpdateBankAccountAppDto
        {
            Id = request.Id,
            DisplayName = request.DisplayName,
            BankCode = request.BankCode,
            BankName = request.BankName,
            AccountNumber = request.AccountNumber,
            AccountHolderName = request.AccountHolderName
        }).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class SetDefaultBankAccountHandler(IBankAccountAppService appService)
    : IRequestHandler<SetDefaultBankAccountCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(SetDefaultBankAccountCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.SetDefaultBankAccountAsync(request.Id).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class DeactivateBankAccountHandler(IBankAccountAppService appService)
    : IRequestHandler<DeactivateBankAccountCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(DeactivateBankAccountCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.DeactivateBankAccountAsync(request.Id).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class ActivateBankAccountHandler(IBankAccountAppService appService)
    : IRequestHandler<ActivateBankAccountCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(ActivateBankAccountCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.ActivateBankAccountAsync(request.Id).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
