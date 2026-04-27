using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

namespace NamEcommerce.Application.Services.Extensions;

public static class PurchaseOrderExtensions
{
    public static PurchaseOrderAppDto ToDto(this PurchaseOrder purchaseOrder)
    {
        var dto = new PurchaseOrderAppDto(purchaseOrder.Id)
        {
            Code = purchaseOrder.Code,
            PlacedOnUtc = purchaseOrder.PlacedOnUtc,
            VendorId = purchaseOrder.VendorId,
            WarehouseId = purchaseOrder.WarehouseId,
            CreatedByUserId = purchaseOrder.CreatedByUserId,
            TaxAmount = purchaseOrder.TaxAmount,
            ShippingAmount = purchaseOrder.ShippingAmount,
            Status = (int)purchaseOrder.Status,
            ExpectedDeliveryDateUtc = purchaseOrder.ExpectedDeliveryDateUtc,
            Note = purchaseOrder.Note,
            CreatedOnUtc = purchaseOrder.CreatedOnUtc,
            TotalAmount = purchaseOrder.TotalAmount,
            CanAddItems = purchaseOrder.CanUpdatePurchaseOrderItems(),
            CanReceiveGoods = purchaseOrder.CanReceiveGoods()
        };

        foreach(var item in purchaseOrder.Items)
        {
            dto.Items.Add(new PurchaseOrderItemAppDto(item.Id)
            {
                PurchaseOrderId = item.PurchaseOrderId,
                ProductId = item.ProductId,
                Note = item.Note,
                QuantityOrdered = item.QuantityOrdered,
                QuantityReceived = item.QuantityReceived,
                UnitCost = item.UnitCost,
                TotalCost = item.TotalCost
            });
        }

        return dto;
    }

    public static PurchaseOrderAppDto ToDto(this PurchaseOrderDto purchaseOrder)
    {
        var dto = new PurchaseOrderAppDto(purchaseOrder.Id)
        {
            Code = purchaseOrder.Code,
            PlacedOnUtc = purchaseOrder.PlacedOnUtc,
            VendorId = purchaseOrder.VendorId,
            WarehouseId = purchaseOrder.WarehouseId,
            CreatedByUserId = purchaseOrder.CreatedByUserId,
            TaxAmount = purchaseOrder.TaxAmount,
            ShippingAmount = purchaseOrder.ShippingAmount,
            Status = (int)purchaseOrder.Status,
            ExpectedDeliveryDateUtc = purchaseOrder.ExpectedDeliveryDateUtc,
            Note = purchaseOrder.Note,
            CreatedOnUtc = purchaseOrder.CreatedOnUtc,
            TotalAmount = purchaseOrder.TotalAmount,
            CanAddItems = purchaseOrder.CanAddItems,
            CanReceiveGoods = purchaseOrder.CanReceiveGoods
        };

        foreach (var item in purchaseOrder.Items)
        {
            dto.Items.Add(new PurchaseOrderItemAppDto(item.Id)
            {
                PurchaseOrderId = item.PurchaseOrderId,
                ProductId = item.ProductId,
                Note = item.Note,
                QuantityOrdered = item.QuantityOrdered,
                QuantityReceived = item.QuantityReceived,
                RemainingQuantity = item.RemainingQuantity,
                UnitCost = item.UnitCost,
                TotalCost = item.TotalCost
            });
        }

        return dto;
    }
}
