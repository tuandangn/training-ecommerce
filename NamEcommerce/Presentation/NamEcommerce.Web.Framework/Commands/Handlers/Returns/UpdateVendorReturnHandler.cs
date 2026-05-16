using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Commands.Models.Returns;
using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Returns;

public sealed class UpdateVendorReturnHandler : IRequestHandler<UpdateVendorReturnCommand, UpdateVendorReturnResultModel>
{
    private readonly IVendorReturnAppService _vendorReturnAppService;

    public UpdateVendorReturnHandler(IVendorReturnAppService vendorReturnAppService)
    {
        _vendorReturnAppService = vendorReturnAppService;
    }

    public async Task<UpdateVendorReturnResultModel> Handle(UpdateVendorReturnCommand request, CancellationToken cancellationToken)
    {
        var result = await _vendorReturnAppService.UpdateAsync(new UpdateVendorReturnAppDto(request.Id)
        {
            Note = request.Note,
            ReturnDate = DateTimeHelper.ToUniversalTime(request.ReturnDate)
        }).ConfigureAwait(false);

        return new UpdateVendorReturnResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
