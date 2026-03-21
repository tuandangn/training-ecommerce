using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Catalog;

public sealed class CreateVendorHandler : IRequestHandler<CreateVendorCommand, CreateVendorResultModel>
{
    private readonly IVendorAppService _vendorAppService;

    public CreateVendorHandler(IVendorAppService vendorAppService)
    {
        _vendorAppService = vendorAppService;
    }

    public async Task<CreateVendorResultModel> Handle(CreateVendorCommand request, CancellationToken cancellationToken)
    {
        var result = await _vendorAppService.CreateVendorAsync(new CreateVendorAppDto
        {
            Name = request.Name,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            DisplayOrder = request.DisplayOrder
        }).ConfigureAwait(false);

        return new CreateVendorResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId ?? default
        };
    }
}
