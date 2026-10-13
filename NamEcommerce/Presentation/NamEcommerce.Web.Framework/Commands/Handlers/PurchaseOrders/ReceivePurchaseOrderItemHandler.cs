using MediatR;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class ReceivePurchaseOrderItemHandler(IPurchaseOrderAppService appService, ICurrentUserService currentUserService) 
    : IRequestHandler<ReceivePurchaseOrderItemCommand, ReceivePurchaseOrderItemResultModel>
{
    public async Task<ReceivePurchaseOrderItemResultModel> Handle(ReceivePurchaseOrderItemCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var result = await appService.ReceiveItemAsync(new ReceiveGoodsAppDto(request.PurchaseOrderId, request.PurchaseOrderItemId)
        {
            ReceivedQuantity = request.ReceivedQuantity,
            WarehouseId = request.WarehouseId,
            ReceivedByUserId = currentUser?.Id,
            SellingPrice = request.SellingPrice,
            ActualUnitCost = request.ActualUnitCost,
            TaxRate = request.TaxRate,
            PictureIds = request.PictureIds,
            ReceivedOnUtc = DateTimeHelper.ToUniversalTime(request.ReceivedOn),
            ShippingAmount = request.ShippingAmount,
            QuantityDecimalPlaces = request.QuantityDecimalPlaces,
            DirectShipOrderId = request.DirectShipOrderId,
            DirectShipOrderItemId = request.DirectShipOrderItemId,
            DirectShipExistingAllocationId = request.DirectShipExistingAllocationId,
            DirectShipAddress = request.DirectShipAddress,
            DirectShipContactName = request.DirectShipContactName,
            DirectShipContactPhone = request.DirectShipContactPhone,
        }).ConfigureAwait(false);

        return new ReceivePurchaseOrderItemResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            ActualReceivedQuantity = result.ActualReceivedQuantity,
            CreatedGoodsReceiptId = result.CreatedGoodsReceiptId
        };
    }
}
