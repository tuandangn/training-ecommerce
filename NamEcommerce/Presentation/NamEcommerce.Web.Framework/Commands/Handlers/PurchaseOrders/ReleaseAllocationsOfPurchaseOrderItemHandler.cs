using MediatR;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class ReleaseAllocationsOfPurchaseOrderItemHandler(IPurchaseOrderAppService purchaseOrderAppService)
    : IRequestHandler<ReleaseAllocationsOfPurchaseOrderItemCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(ReleaseAllocationsOfPurchaseOrderItemCommand request, CancellationToken cancellationToken)
    {
        var result = await purchaseOrderAppService.ReleasePoItemAllocationForOrderItemAsync(new ReleaseAllocationsOfPurchaseOrderItemAppDto
        {
            PurchaseOrderId = request.PurchaseOrderId,
            PurchaseOrderItemId = request.PurchaseOrderItemId
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
