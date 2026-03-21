using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class DeleteVendorHandler : IRequestHandler<DeleteVendorCommand, DeleteVendorResultModel>
{
    private readonly IVendorAppService _vendorAppService;

    public DeleteVendorHandler(IVendorAppService vendorAppService)
    {
        _vendorAppService = vendorAppService;
    }

    public async Task<DeleteVendorResultModel> Handle(DeleteVendorCommand request, CancellationToken cancellationToken)
    {
        var deleteResultDto = await _vendorAppService.DeleteVendorAsync(
            new DeleteVendorAppDto(request.Id)).ConfigureAwait(false);

        return new DeleteVendorResultModel
        {
            Success = deleteResultDto.Success,
            ErrorMessage = deleteResultDto.ErrorMessage
        };
    }
}
