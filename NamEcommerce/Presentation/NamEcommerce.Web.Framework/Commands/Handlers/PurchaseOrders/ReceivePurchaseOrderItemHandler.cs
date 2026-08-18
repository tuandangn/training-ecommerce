using MediatR;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class ReceivePurchaseOrderItemHandler(IPurchaseOrderAppService appService, ICurrentUserService currentUserService, ISender sender,
    IWarehouseAppService warehouseAppService, IProductAppService productAppService, IUnitMeasurementAppService unitMeasurementAppService) 
    : IRequestHandler<ReceivePurchaseOrderItemCommand, ReceivePurchaseOrderItemResultModel>
{
    public async Task<ReceivePurchaseOrderItemResultModel> Handle(ReceivePurchaseOrderItemCommand request, CancellationToken cancellationToken)
    {
        var purchaseOrder = await appService.GetPurchaseOrderByIdAsync(request.PurchaseOrderId);
        if (purchaseOrder is null)
        {
            return new ReceivePurchaseOrderItemResultModel
            {
                Success = false,
                ErrorMessage = "Error.ProductIsNotFound"
            };
        }

        if (!purchaseOrder.CanReceiveGoods)
        {
            return new ReceivePurchaseOrderItemResultModel
            {
                Success = false,
                ErrorMessage = "Error.PurchaseOrderCannotReceiveGoods"
            };
        }

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == request.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
        {
            return new ReceivePurchaseOrderItemResultModel
            {
                Success = false,
                ErrorMessage = "Error.PurchaseOrderItemIsNotFound"
            };
        }

        var product = await productAppService.GetProductByIdAsync(purchaseOrderItem.ProductId).ConfigureAwait(false);
        if (product is null)
        {
            return new ReceivePurchaseOrderItemResultModel
            {
                Success = false,
                ErrorMessage = "Error.GoodsReceipt.ProductIsNotFound"
            };
        }

        if (product.UnitMeasurementId.HasValue)
        {
            var unitMeasurement = await unitMeasurementAppService.GetUnitMeasurementByIdAsync(product.UnitMeasurementId.Value).ConfigureAwait(false);
            if (unitMeasurement is not null)
            {
                if (!NumberHelper.IsValidDecimalPlace(request.ReceivedQuantity, unitMeasurement.DecimalPlaces))
                {
                    return new ReceivePurchaseOrderItemResultModel
                    {
                        Success = false,
                        ErrorMessage = "Error.QuantityMustBeInteger"
                    };
                }
            }
        }

        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);

        var upgradeExisting = request.DirectShipExistingAllocationId.HasValue
            && request.DirectShipExistingAllocationId.Value != Guid.Empty;
        var directShipRequested = !upgradeExisting
            && (request.DirectShipOrderId.HasValue || request.DirectShipOrderItemId.HasValue);

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

        if (upgradeExisting)
        {
            if (string.IsNullOrWhiteSpace(request.DirectShipContactPhone))
                return new ReceivePurchaseOrderItemResultModel { Success = false, ErrorMessage = "Error.DirectShipContactPhoneRequired" };
            if (string.IsNullOrWhiteSpace(request.DirectShipAddress))
                return new ReceivePurchaseOrderItemResultModel { Success = false, ErrorMessage = "Error.DirectShipAddressRequired" };
        }

        var hasDirectShip = directShipRequested || upgradeExisting;

        Guid? warehouseId = request.WarehouseId;
        var maxAllocationQuantity = 0m;

        if (hasDirectShip)
        {
            maxAllocationQuantity = upgradeExisting
                ? await appService.GetAllocationRemainingQuantityAsync(request.DirectShipExistingAllocationId!.Value).ConfigureAwait(false)
                : await appService.GetMaxAllocationQuantityForOrderItemAsync(request.DirectShipOrderId!.Value, request.DirectShipOrderItemId!.Value).ConfigureAwait(false);

            if (maxAllocationQuantity <= 0)
                return new ReceivePurchaseOrderItemResultModel { Success = false, ErrorMessage = "Error.PurchaseOrderItemAllocationQuantityExceedsAvailable" };

            var physicalWarehouseRequired = request.ReceivedQuantity > maxAllocationQuantity;

            warehouseId ??= purchaseOrder?.WarehouseId;
            if (!warehouseId.HasValue && physicalWarehouseRequired)
                return new ReceivePurchaseOrderItemResultModel { Success = false, ErrorMessage = "Error.WarehouseRequired" };

            if (warehouseId.HasValue)
            {
                var warehouse = await warehouseAppService.GetWarehouseByIdAsync(warehouseId.Value).ConfigureAwait(false);
                if (warehouse is null)
                    return new ReceivePurchaseOrderItemResultModel { Success = false, ErrorMessage = "Error.WarehouseIsNotFound" };
                if (physicalWarehouseRequired && !warehouse.IsPhysical)
                    return new ReceivePurchaseOrderItemResultModel { Success = false, ErrorMessage = "Error.PhysicalWarehouseRequired" };
            }
        }

        if (upgradeExisting)
        {
            var markResult = await sender.Send(new MarkAllocationAsDirectShipCommand
            {
                AllocationId = request.DirectShipExistingAllocationId!.Value,
                Address = request.DirectShipAddress!,
                ContactName = request.DirectShipContactName,
                ContactPhone = request.DirectShipContactPhone
            }, cancellationToken).ConfigureAwait(false);

            if (!markResult.Success)
                return new ReceivePurchaseOrderItemResultModel { Success = false, ErrorMessage = markResult.ErrorMessage };
        }
        else if (directShipRequested)
        {
            var allocationResult = await sender.Send(new AllocatePoItemForOrderItemCommand
            {
                PurchaseOrderId = request.PurchaseOrderId,
                PurchaseOrderItemId = request.PurchaseOrderItemId,
                OrderId = request.DirectShipOrderId!.Value,
                OrderItemId = request.DirectShipOrderItemId!.Value,
                Quantity = Math.Min(request.ReceivedQuantity, maxAllocationQuantity),
                DirectShipAddress = request.DirectShipAddress,
                DirectShipContactName = request.DirectShipContactName,
                DirectShipContactPhone = request.DirectShipContactPhone
            }, cancellationToken).ConfigureAwait(false);

            if (!allocationResult.Success)
                return new ReceivePurchaseOrderItemResultModel { Success = false, ErrorMessage = allocationResult.ErrorMessage };
        }

        var result = await appService.ReceiveItemAsync(new ReceiveGoodsAppDto(request.PurchaseOrderId, request.PurchaseOrderItemId)
        {
            ReceivedQuantity = request.ReceivedQuantity,
            WarehouseId = request.WarehouseId,
            ReceivedByUserId = currentUser?.Id,
            SellingPrice = request.SellingPrice,
            ActualUnitCost = request.ActualUnitCost,
            OversupplyAction = request.OversupplyAction,
            TaxRate = request.TaxRate,
            QuantityDecimalPlaces = request.QuantityDecimalPlaces
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
