using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.UnitMeasurements;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class UpdateVendorHandler : IRequestHandler<UpdateVendorCommand, UpdateVendorResultModel>
{
    private readonly IVendorAppService _vendorAppService;

    public UpdateVendorHandler(IVendorAppService vendorAppService)
    {
        _vendorAppService = vendorAppService;
    }

    public async Task<UpdateVendorResultModel> Handle(UpdateVendorCommand request, CancellationToken cancellationToken)
    {
        var updateResult = await _vendorAppService.UpdateVendorAsync(new UpdateVendorAppDto(request.Id) {
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Name = request.Name,
            DisplayOrder = request.DisplayOrder
        });

        return new UpdateVendorResultModel
        {
            Success = updateResult.Success,
            ErrorMessage = updateResult.ErrorMessage,
            UpdatedId = updateResult.UpdatedId
        };
    }
}
