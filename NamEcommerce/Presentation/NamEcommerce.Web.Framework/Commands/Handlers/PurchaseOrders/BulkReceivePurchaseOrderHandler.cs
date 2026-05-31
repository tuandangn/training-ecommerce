using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class BulkReceivePurchaseOrderHandler : IRequestHandler<BulkReceivePurchaseOrderCommand, BulkReceivePurchaseOrderResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;
    private readonly ISender _sender;

    public BulkReceivePurchaseOrderHandler(IPurchaseOrderAppService appService, ISender sender)
    {
        _purchaseOrderAppService = appService;
        _sender = sender;
    }

    public async Task<BulkReceivePurchaseOrderResultModel> Handle(BulkReceivePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var createdGoodsReceiptIds = new List<Guid>();
        foreach (var item in request.Items)
        {
            var receiveResult = await _sender.Send(new ReceivePurchaseOrderItemCommand
            {
                PurchaseOrderId = request.PurchaseOrderId,
                PurchaseOrderItemId = item.ItemId,
                ReceivedQuantity = item.Quantity,
                WarehouseId = item.WarehouseId,
                ActualUnitCost = item.ActualUnitCost,
                DirectShipOrderId = item.DirectShipOrderId,
                DirectShipOrderItemId = item.DirectShipOrderItemId,
                DirectShipAddress = item.DirectShipAddress,
                DirectShipContactName = item.DirectShipContactName,
                DirectShipContactPhone = item.DirectShipContactPhone,
                DirectShipExistingAllocationId = item.DirectShipExistingAllocationId
            }, cancellationToken).ConfigureAwait(false);

            if (!receiveResult.Success)
            {
                return new BulkReceivePurchaseOrderResultModel
                {
                    Success = false,
                    ErrorMessage = receiveResult.ErrorMessage,
                    CreatedGoodsReceiptIds = createdGoodsReceiptIds
                };
            }

            if (receiveResult.CreatedGoodsReceiptId.HasValue)
                createdGoodsReceiptIds.Add(receiveResult.CreatedGoodsReceiptId.Value);
        }

        if (request.AdditionalShipping > 0 || request.AdditionalTax > 0)
        {
            var feeResult = await _purchaseOrderAppService.AddReceiptFeesAsync(
                request.PurchaseOrderId,
                request.AdditionalShipping,
                request.AdditionalTax).ConfigureAwait(false);

            if (!feeResult.Success)
            {
                return new BulkReceivePurchaseOrderResultModel
                {
                    Success = false,
                    ErrorMessage = feeResult.ErrorMessage,
                    CreatedGoodsReceiptIds = createdGoodsReceiptIds
                };
            }
        }

        return new BulkReceivePurchaseOrderResultModel
        {
            Success = true,
            CreatedGoodsReceiptIds = createdGoodsReceiptIds
        };
    }
}
