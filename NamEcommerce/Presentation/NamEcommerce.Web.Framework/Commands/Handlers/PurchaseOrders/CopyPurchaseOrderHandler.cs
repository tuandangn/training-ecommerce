using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class CopyPurchaseOrderHandler(IPurchaseOrderAppService purchaseOrderAppService)
    : IRequestHandler<CopyPurchaseOrderCommand, CreatePurchaseOrderResultModel>
{
    public async Task<CreatePurchaseOrderResultModel> Handle(CopyPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await purchaseOrderAppService.CopyPurchaseOrderAsync(request.Id).ConfigureAwait(false);
        return new CreatePurchaseOrderResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}
