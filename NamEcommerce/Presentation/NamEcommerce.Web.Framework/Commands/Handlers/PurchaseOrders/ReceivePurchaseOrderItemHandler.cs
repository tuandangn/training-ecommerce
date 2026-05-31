using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Services;
using NamEcommerce.Application.Contracts.Inventory;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class ReceivePurchaseOrderItemHandler : IRequestHandler<ReceivePurchaseOrderItemCommand, ReceivePurchaseOrderItemResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;
    private readonly IWarehouseAppService _warehouseAppService;

    public ReceivePurchaseOrderItemHandler(IPurchaseOrderAppService appService,
        ICurrentUserService currentUserService, ISender sender, IWarehouseAppService warehouseAppService)
    {
        _purchaseOrderAppService = appService;
        _currentUserService = currentUserService;
        _sender = sender;
        _warehouseAppService = warehouseAppService;
    }

    public async Task<ReceivePurchaseOrderItemResultModel> Handle(ReceivePurchaseOrderItemCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var directShipRequested = request.DirectShipOrderId.HasValue
            || request.DirectShipOrderItemId.HasValue;
        if (directShipRequested)
        {
            if (!request.DirectShipOrderId.HasValue || request.DirectShipOrderId.Value == Guid.Empty)
                return new ReceivePurchaseOrderItemResultModel { Success = false, ErrorMessage = "Error.OrderRequired" };
            if (!request.DirectShipOrderItemId.HasValue || request.DirectShipOrderItemId.Value == Guid.Empty)
                return new ReceivePurchaseOrderItemResultModel { Success = false, ErrorMessage = "Error.OrderItemIsNotFound" };
            if (string.IsNullOrWhiteSpace(request.DirectShipContactPhone))
                return new ReceivePurchaseOrderItemResultModel { Success = false, ErrorMessage = "Error.DirectShipContactPhoneRequired" };
            if (string.IsNullOrWhiteSpace(request.DirectShipAddress))
                return new ReceivePurchaseOrderItemResultModel { Success = false, ErrorMessage = "Error.DirectShipAddressRequired" };
        }

        var hasDirectShip = directShipRequested;

        Guid? warehouseId = request.WarehouseId;
        var maxAllocationQuantityForOrderItem = 0m;
        if (hasDirectShip)
        {
            maxAllocationQuantityForOrderItem = await _purchaseOrderAppService.GetMaxAllocationQuantityForOrderItemAsync(
                request.DirectShipOrderId!.Value, request.DirectShipOrderItemId!.Value
            ).ConfigureAwait(false);
            if (maxAllocationQuantityForOrderItem <= 0)
            {
                return new ReceivePurchaseOrderItemResultModel
                {
                    Success = false,
                    ErrorMessage = "Error.PurchaseOrderItemAllocationQuantityExceedsAvailable"
                };
            }

            var physicalWarehouseRequired = request.ReceivedQuantity > maxAllocationQuantityForOrderItem;

            var purchaseOrder = await _purchaseOrderAppService.GetPurchaseOrderByIdAsync(request.PurchaseOrderId).ConfigureAwait(false);
            warehouseId ??= purchaseOrder?.WarehouseId;
            if (!warehouseId.HasValue && physicalWarehouseRequired)
            {
                return new ReceivePurchaseOrderItemResultModel
                {
                    Success = false,
                    ErrorMessage = "Error.WarehouseRequired"
                };
            }
            if (warehouseId.HasValue)
            {
                var warehouse = await _warehouseAppService.GetWarehouseByIdAsync(warehouseId.Value).ConfigureAwait(false);
                if (warehouse is null)
                {
                    return new ReceivePurchaseOrderItemResultModel
                    {
                        Success = false,
                        ErrorMessage = "Error.WarehouseIsNotFound"
                    };
                }
                else if (physicalWarehouseRequired && !warehouse.IsPhysical)
                {
                    return new ReceivePurchaseOrderItemResultModel
                    {
                        Success = false,
                        ErrorMessage = "Error.PhysicalWarehouseRequired"
                    };
                }
            }
        }

        if (hasDirectShip)
        {
            var allocationResult = await _sender.Send(new AllocatePoItemForOrderItemCommand
            {
                PurchaseOrderId = request.PurchaseOrderId,
                PurchaseOrderItemId = request.PurchaseOrderItemId,
                OrderId = request.DirectShipOrderId!.Value,
                OrderItemId = request.DirectShipOrderItemId!.Value,
                Quantity = Math.Min(request.ReceivedQuantity, maxAllocationQuantityForOrderItem),
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
