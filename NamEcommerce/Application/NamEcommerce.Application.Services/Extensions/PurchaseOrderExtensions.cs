using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Entities.PurchaseOrders;

namespace NamEcommerce.Application.Services.Extensions;

public static class PurchaseOrderExtensions
{
    public static PurchaseOrderAppDto ToDto(this PurchaseOrder purchaseOrder)
    {
        var dto = new PurchaseOrderAppDto(purchaseOrder.Id)
        {
            Code = purchaseOrder.Code,
            VendorId = purchaseOrder.VendorId,
            WarehouseId = purchaseOrder.WarehouseId,
            CreatedByUserId = purchaseOrder.CreatedByUserId,
            TaxAmount = purchaseOrder.TaxAmount,
            ShippingAmount = purchaseOrder.ShippingAmount,
            Status = (int)purchaseOrder.Status,
            ExpectedDeliveryDateUtc = purchaseOrder.ExpectedDeliveryDateUtc,
            Note = purchaseOrder.Note,
            CreatedOnUtc = purchaseOrder.CreatedOnUtc,
            TotalAmount = purchaseOrder.TotalAmount
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
                UnitCost = item.UnitCost
            });
        }

        return dto;
    }
}
