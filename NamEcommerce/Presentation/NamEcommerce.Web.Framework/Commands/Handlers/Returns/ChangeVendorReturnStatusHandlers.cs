using MediatR;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Commands.Models.Returns;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Returns;

public sealed class MoveVendorReturnToInspectingHandler
    : IRequestHandler<MoveVendorReturnToInspectingCommand, CommonActionResultModel>
{
    private readonly IVendorReturnAppService _vendorReturnAppService;

    public MoveVendorReturnToInspectingHandler(IVendorReturnAppService vendorReturnAppService)
    {
        _vendorReturnAppService = vendorReturnAppService;
    }

    public async Task<CommonActionResultModel> Handle(MoveVendorReturnToInspectingCommand request, CancellationToken cancellationToken)
    {
        var result = await _vendorReturnAppService.MoveToInspectingAsync(request.Id).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class ConfirmVendorReturnHandler
    : IRequestHandler<ConfirmVendorReturnCommand, CommonActionResultModel>
{
    private readonly IVendorReturnAppService _vendorReturnAppService;

    public ConfirmVendorReturnHandler(IVendorReturnAppService vendorReturnAppService)
    {
        _vendorReturnAppService = vendorReturnAppService;
    }

    public async Task<CommonActionResultModel> Handle(ConfirmVendorReturnCommand request, CancellationToken cancellationToken)
    {
        var result = await _vendorReturnAppService.ConfirmAsync(request.Id).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class CancelVendorReturnHandler
    : IRequestHandler<CancelVendorReturnCommand, CommonActionResultModel>
{
    private readonly IVendorReturnAppService _vendorReturnAppService;

    public CancelVendorReturnHandler(IVendorReturnAppService vendorReturnAppService)
    {
        _vendorReturnAppService = vendorReturnAppService;
    }

    public async Task<CommonActionResultModel> Handle(CancelVendorReturnCommand request, CancellationToken cancellationToken)
    {
        var result = await _vendorReturnAppService.CancelAsync(request.Id).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
