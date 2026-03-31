using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Catalog;

public sealed class GetVendorOptionListHandler : IRequestHandler<GetVendorOptionListQuery, EntityOptionListModel>
{
    private readonly IVendorAppService _vendorAppService;

    public GetVendorOptionListHandler(IVendorAppService vendorAppService)
    {
        _vendorAppService = vendorAppService;
    }

    public async Task<EntityOptionListModel> Handle(GetVendorOptionListQuery request, CancellationToken cancellationToken)
    {
        var vendors = await _vendorAppService.GetVendorsAsync(null, 0, int.MaxValue);

        return new EntityOptionListModel
        {
            Options = vendors.Select(x => new EntityOptionListModel.EntityOptionModel
            {
                Id = x.Id,
                Name = x.Name
            })
        };
    }
}

public sealed class GetProductOptionListHandler : IRequestHandler<GetProductOptionListQuery, EntityOptionListModel>
{
    private readonly IProductAppService _productAppService;

    public GetProductOptionListHandler(IProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    public async Task<EntityOptionListModel> Handle(GetProductOptionListQuery request, CancellationToken cancellationToken)
    {
        var products = await _productAppService.GetProductsAsync(null, 0, int.MaxValue);

        return new EntityOptionListModel
        {
            Options = products.Select(x => new EntityOptionListModel.EntityOptionModel
            {
                Id = x.Id,
                Name = x.Name
            })
        };
    }
}
