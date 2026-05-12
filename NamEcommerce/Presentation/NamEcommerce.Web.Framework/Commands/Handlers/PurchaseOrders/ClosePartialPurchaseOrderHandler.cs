using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class ClosePartialPurchaseOrderHandler : IRequestHandler<ClosePartialPurchaseOrderCommand, CommonActionResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public ClosePartialPurchaseOrderHandler(IPurchaseOrderAppService purchaseOrderAppService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    public async Task<CommonActionResultModel> Handle(ClosePartialPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var (success, errorMessage) = await _purchaseOrderAppService
            .ClosePartialPurchaseOrderAsync(request.PurchaseOrderId, request.Reason)
            .ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = success,
            ErrorMessage = errorMessage
        };
    }
}
