using MediatR;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Returns;

public sealed class GetValidWarehousesForReturnHandler
    : IRequestHandler<GetValidWarehousesForReturnQuery, List<Guid>>
{
    private readonly IVendorReturnAppService _vendorReturnAppService;

    public GetValidWarehousesForReturnHandler(IVendorReturnAppService vendorReturnAppService)
    {
        _vendorReturnAppService = vendorReturnAppService;
    }

    public Task<List<Guid>> Handle(GetValidWarehousesForReturnQuery request, CancellationToken cancellationToken)
    {
        var items = request.Items
            .Select(i => (i.ProductId, i.RequiredQty))
            .ToList();

        return _vendorReturnAppService.GetWarehousesWithSufficientStockAsync(items);
    }
}
