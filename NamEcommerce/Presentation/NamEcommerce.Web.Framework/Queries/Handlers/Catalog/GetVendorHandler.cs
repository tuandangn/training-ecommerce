using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetVendorHandler : IRequestHandler<GetVendorQuery, VendorModel?>
{
    private readonly IVendorAppService _vendorAppService;

    public GetVendorHandler(IVendorAppService vendorAppService)
    {
        _vendorAppService = vendorAppService;
    }

    public async Task<VendorModel?> Handle(GetVendorQuery request, CancellationToken cancellationToken)
    {
        var vendor = await _vendorAppService.GetVendorByIdAsync(request.Id);
        if (vendor == null)
            return null;

        return new VendorModel
        {
            Id = vendor.Id,
            Name = vendor.Name,
            PhoneNumber = vendor.PhoneNumber,
            Address = vendor.Address,
            DisplayOrder = vendor.DisplayOrder
        };
    }
}
