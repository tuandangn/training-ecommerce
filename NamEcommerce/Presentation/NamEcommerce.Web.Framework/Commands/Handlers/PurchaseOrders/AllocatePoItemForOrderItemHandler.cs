using MediatR;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class AllocatePoItemForOrderItemHandler(IPurchaseOrderAppService purchaseOrderAppService)
    : IRequestHandler<AllocatePoItemForOrderItemCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(AllocatePoItemForOrderItemCommand request, CancellationToken cancellationToken)
    {
        var result = await purchaseOrderAppService.AllocatePoItemForOrderItemAsync(new AllocatePoItemForOrderItemAppDto
        {
            PurchaseOrderId = request.PurchaseOrderId,
            PurchaseOrderItemId = request.PurchaseOrderItemId,
            OrderId = request.OrderId,
            OrderItemId = request.OrderItemId,
            Quantity = request.Quantity,
            DirectShipAddress = request.DirectShipAddress,
            DirectShipContactName = request.DirectShipContactName,
            DirectShipContactPhone = request.DirectShipContactPhone
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
