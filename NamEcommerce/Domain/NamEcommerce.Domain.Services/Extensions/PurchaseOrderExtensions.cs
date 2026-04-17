using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

namespace NamEcommerce.Domain.Services.Extensions;

public static class PurchaseOrderExtensions
{
    public static PurchaseOrderDto ToDto(this PurchaseOrder purchaseOrder)
    {
        var dto = new PurchaseOrderDto(purchaseOrder.Id)
        {
            Code = purchaseOrder.Code,
            CreatedByUserId = purchaseOrder.CreatedByUserId,
            VendorId = purchaseOrder.VendorId,
            WarehouseId = purchaseOrder.WarehouseId,
            ExpectedDeliveryDateUtc = purchaseOrder.ExpectedDeliveryDateUtc,
            Note = purchaseOrder.Note,
            ShippingAmount = purchaseOrder.ShippingAmount,
            TaxAmount = purchaseOrder.TaxAmount,
            Status = purchaseOrder.Status,
            CanAddItems = purchaseOrder.CanAddPurchaseOrderItems(),
            CanReceiveGoods = purchaseOrder.CanReceiveGoods(),
            CreatedOnUtc = purchaseOrder.CreatedOnUtc,
            TotalAmount = purchaseOrder.TotalAmount
        };

        foreach (var purchaseOrderItem in purchaseOrder.Items)
        {
            var itemDto = new PurchaseOrderItemDto(purchaseOrderItem.Id)
            {
                PurchaseOrderId = purchaseOrder.Id,
                Note = purchaseOrderItem.Note,
                QuantityOrdered = purchaseOrderItem.QuantityOrdered,
                QuantityReceived = purchaseOrderItem.QuantityReceived,
                RemainingQuantity = purchaseOrderItem.QuantityOrdered - purchaseOrderItem.QuantityReceived,
                ProductId = purchaseOrderItem.ProductId,
                UnitCost = purchaseOrderItem.UnitCost,
                TotalCost = purchaseOrderItem.TotalCost
            };
            dto.Items.Add(itemDto);
        }

        return dto;
    }
}
