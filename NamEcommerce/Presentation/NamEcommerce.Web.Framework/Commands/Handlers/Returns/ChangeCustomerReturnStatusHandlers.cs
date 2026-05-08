using MediatR;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Commands.Models.Returns;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Returns;

public sealed class MoveCustomerReturnToInspectingHandler
    : IRequestHandler<MoveCustomerReturnToInspectingCommand, CommonActionResultModel>
{
    private readonly ICustomerReturnAppService _customerReturnAppService;

    public MoveCustomerReturnToInspectingHandler(ICustomerReturnAppService customerReturnAppService)
    {
        _customerReturnAppService = customerReturnAppService;
    }

    public async Task<CommonActionResultModel> Handle(MoveCustomerReturnToInspectingCommand request, CancellationToken cancellationToken)
    {
        var result = await _customerReturnAppService.MoveToInspectingAsync(request.Id).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class ConfirmCustomerReturnHandler
    : IRequestHandler<ConfirmCustomerReturnCommand, CommonActionResultModel>
{
    private readonly ICustomerReturnAppService _customerReturnAppService;

    public ConfirmCustomerReturnHandler(ICustomerReturnAppService customerReturnAppService)
    {
        _customerReturnAppService = customerReturnAppService;
    }

    public async Task<CommonActionResultModel> Handle(ConfirmCustomerReturnCommand request, CancellationToken cancellationToken)
    {
        var result = await _customerReturnAppService.ConfirmAsync(request.Id).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class CancelCustomerReturnHandler
    : IRequestHandler<CancelCustomerReturnCommand, CommonActionResultModel>
{
    private readonly ICustomerReturnAppService _customerReturnAppService;

    public CancelCustomerReturnHandler(ICustomerReturnAppService customerReturnAppService)
    {
        _customerReturnAppService = customerReturnAppService;
    }

    public async Task<CommonActionResultModel> Handle(CancelCustomerReturnCommand request, CancellationToken cancellationToken)
    {
        var result = await _customerReturnAppService.CancelAsync(request.Id).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
