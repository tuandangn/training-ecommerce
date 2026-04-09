using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class CancelPurchaseOrderHandler : IRequestHandler<CancelPurchaseOrderCommand, CommonResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public CancelPurchaseOrderHandler(IPurchaseOrderAppService purchaseOrderAppService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    public async Task<CommonResultModel> Handle(CancelPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var (success, errorMessage) = await _purchaseOrderAppService.CancelPurchaseOrderAsync(request.PurchaseOrderId).ConfigureAwait(false);

        return new CommonResultModel
        {
            Success = success,
            ErrorMessage = errorMessage
        };
    }
}
