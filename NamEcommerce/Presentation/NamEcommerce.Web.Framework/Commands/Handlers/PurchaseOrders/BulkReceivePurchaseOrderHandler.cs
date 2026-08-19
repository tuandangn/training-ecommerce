using MediatR;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class BulkReceivePurchaseOrderHandler(IPurchaseOrderAppService purchaseOrderAppService, ICurrentUserService currentUserService)
    : IRequestHandler<BulkReceivePurchaseOrderCommand, BulkReceivePurchaseOrderResultModel>
{
    public async Task<BulkReceivePurchaseOrderResultModel> Handle(BulkReceivePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var bulkReceivedResult = await purchaseOrderAppService.BulkReceiveItemsAsync(new BulkReceiveGoodsAppDto(request.PurchaseOrderId)
        {
            Items = request.Items.Select(item => new BulkReceiveItemAppDto
            {
                PurchaseOrderItemId = item.ItemId,
                ActualUnitCost = item.ActualUnitCost,
                ReceivedQuantity = item.Quantity,
                WarehouseId = item.WarehouseId,
                DirectShipOrderId = item.DirectShipOrderId,
                DirectShipOrderItemId = item.DirectShipOrderItemId,
                DirectShipAddress = item.DirectShipAddress,
                DirectShipContactName = item.DirectShipContactName,
                DirectShipContactPhone = item.DirectShipContactPhone,
                DirectShipExistingAllocationId = item.DirectShipExistingAllocationId
            }).ToList(),
            PictureIds = request.PictureIds,
            ReceivedByUserId = currentUser?.Id,
            ReceivedOnUtc = DateTimeHelper.ToUniversalTime(request.ReceivedOn),
            ShippingAmount = request.ShippingAmount,
            TaxRate = request.TaxRate
        });

        return new BulkReceivePurchaseOrderResultModel
        {
            Success = true,
            CreatedGoodsReceiptIds = bulkReceivedResult.CreatedGoodsReceiptIds
        };
    }
}
