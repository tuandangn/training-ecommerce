using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class ReceivePurchaseOrderItemHandler : IRequestHandler<ReceivePurchaseOrderItemCommand, ReceivePurchaseOrderItemResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public ReceivePurchaseOrderItemHandler(IPurchaseOrderAppService appService, ICurrentUserService currentUserService, ISender sender)
    {
        _purchaseOrderAppService = appService;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<ReceivePurchaseOrderItemResultModel> Handle(ReceivePurchaseOrderItemCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);

        // DS allocation must be created BEFORE receiving so SyncReceivedForPurchaseOrderItem
        // finds it and transfers stock to DirectShipTransit in the same receive transaction.
        if (request.DirectShipOrderItemId.HasValue
            && request.DirectShipOrderItemId.Value != Guid.Empty
            && !string.IsNullOrWhiteSpace(request.DirectShipAddress))
        {
            var allocationResult = await _sender.Send(new AllocatePoItemToOrderCommand
            {
                PurchaseOrderItemId = request.PurchaseOrderItemId,
                OrderItemId = request.DirectShipOrderItemId.Value,
                Quantity = request.ReceivedQuantity,
                DirectShipAddress = request.DirectShipAddress,
                DirectShipContactName = request.DirectShipContactName,
                DirectShipContactPhone = request.DirectShipContactPhone
            }, cancellationToken).ConfigureAwait(false);

            if (!allocationResult.Success)
                return new ReceivePurchaseOrderItemResultModel { Success = false, ErrorMessage = allocationResult.ErrorMessage };
        }

        var result = await _purchaseOrderAppService.ReceiveItemAsync(new ReceivedGoodsForItemAppDto(request.PurchaseOrderId, request.PurchaseOrderItemId)
        {
            ReceivedQuantity = request.ReceivedQuantity,
            WarehouseId = request.WarehouseId,
            ReceivedByUserId = currentUser?.Id,
            SellingPrice = request.SellingPrice,
            ActualUnitCost = request.ActualUnitCost,
            OversupplyAction = request.OversupplyAction
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
