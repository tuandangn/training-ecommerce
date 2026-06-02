using MediatR;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class UpdatePurchaseOrderItemHandler : IRequestHandler<UpdatePurchaseOrderItemCommand, CommonActionResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public UpdatePurchaseOrderItemHandler(IPurchaseOrderAppService purchaseOrderAppService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    public async Task<CommonActionResultModel> Handle(UpdatePurchaseOrderItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _purchaseOrderAppService.UpdatePurchaseOrderItemAsync(new UpdatePurchaseOrderItemAppDto
        {
            PurchaseOrderId = request.PurchaseOrderId,
            PurchaseOrderItemId = request.PurchaseOrderItemId,
            ProductId = Guid.Empty,
            QuantityOrdered = request.Quantity,
            UnitCost = request.UnitCost,
            Note = request.Note
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
