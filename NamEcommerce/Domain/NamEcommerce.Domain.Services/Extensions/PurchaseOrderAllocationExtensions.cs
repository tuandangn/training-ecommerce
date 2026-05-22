using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

namespace NamEcommerce.Domain.Services.Extensions;

public static class PurchaseOrderAllocationExtensions
{
    public static PurchaseOrderItemAllocationDto ToDto(this PurchaseOrderItemAllocation allocation)
        => new(allocation.Id)
        {
            PurchaseOrderItemId = allocation.PurchaseOrderItemId,
            OrderItemId = allocation.OrderItemId,
            AllocatedQuantity = allocation.AllocatedQuantity,
            ReceivedQuantity = allocation.ReceivedQuantity,
            Status = allocation.Status,
            IsDirectShip = allocation.IsDirectShip,
            DirectShipAddress = allocation.DirectShipAddress,
            DirectShipContactName = allocation.DirectShipContactName,
            DirectShipContactPhone = allocation.DirectShipContactPhone,
            DirectShipPriority = allocation.DirectShipPriority,
            CreatedOnUtc = allocation.CreatedOnUtc
        };
}
