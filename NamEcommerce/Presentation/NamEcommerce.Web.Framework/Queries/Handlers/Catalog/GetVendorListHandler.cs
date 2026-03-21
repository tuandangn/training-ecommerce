using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Framework.Common;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetVendorListHandler : IRequestHandler<GetVendorListQuery, VendorListModel>
{
    private readonly IVendorAppService _vendorAppService;

    public GetVendorListHandler(IVendorAppService vendorAppService)
    {
        _vendorAppService = vendorAppService;
    }

    public async Task<VendorListModel> Handle(GetVendorListQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await _vendorAppService.GetVendorsAsync(request.Keywords, request.PageIndex, request.PageSize);

        var model = new VendorListModel
        {
            Keywords = request.Keywords,
            Data = pagedData.MapToModel(item => new VendorListModel.ItemModel(item.Id)
            {
                Name = item.Name,
                PhoneNumber = item.PhoneNumber,
                Address = item.Address,
                DisplayOrder = item.DisplayOrder
            })
        };

        return model;
    }
}
