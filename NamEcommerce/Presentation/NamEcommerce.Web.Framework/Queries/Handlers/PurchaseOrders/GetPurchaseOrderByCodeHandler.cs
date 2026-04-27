using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Queries.Handlers.PurchaseOrders;

public sealed class GetPurchaseOrderByCodeHandler : IRequestHandler<GetPurchaseOrderByCodeQuery, Guid?>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public GetPurchaseOrderByCodeHandler(IPurchaseOrderAppService purchaseOrderAppService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    public async Task<Guid?> Handle(GetPurchaseOrderByCodeQuery request, CancellationToken cancellationToken)
    {
        var purchaseOrder = await _purchaseOrderAppService.GetPurchaseOrderByCodeAsync(request.Code).ConfigureAwait(false);

        return purchaseOrder?.Id;
    }
}
