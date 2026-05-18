using MediatR;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class AllocatePoItemToOrderHandler(IPurchaseOrderAppService purchaseOrderAppService)
    : IRequestHandler<AllocatePoItemToOrderCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(AllocatePoItemToOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await purchaseOrderAppService.AllocatePoItemToOrderAsync(new AllocatePoItemToOrderAppDto
        {
            PurchaseOrderItemId = request.PurchaseOrderItemId,
            OrderItemId = request.OrderItemId,
            Quantity = request.Quantity,
            DirectShipAddress = request.DirectShipAddress,
            DirectShipContactName = request.DirectShipContactName,
            DirectShipContactPhone = request.DirectShipContactPhone
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
