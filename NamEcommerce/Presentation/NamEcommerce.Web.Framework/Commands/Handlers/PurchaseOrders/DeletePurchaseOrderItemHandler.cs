using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class DeletePurchaseOrderItemHandler : IRequestHandler<DeletePurchaseOrderItemCommand, CommonResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public DeletePurchaseOrderItemHandler(IPurchaseOrderAppService purchaseOrderAppService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    public async Task<CommonResultModel> Handle(DeletePurchaseOrderItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _purchaseOrderAppService.DeletePurchaseOrderItemAsync(
            new DeletePurchaseOrderItemAppDto(request.PurchaseOrderId, request.ItemId)
        ).ConfigureAwait(false);

        return new CommonResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
