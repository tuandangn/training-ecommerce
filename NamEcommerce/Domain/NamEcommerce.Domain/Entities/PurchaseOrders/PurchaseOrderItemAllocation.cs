using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Entities.PurchaseOrders;

[Serializable]
public sealed record PurchaseOrderItemAllocation : AppAggregateEntity
{
    internal PurchaseOrderItemAllocation(Guid purchaseOrderItemId, Guid orderItemId, decimal allocatedQuantity)
        : base(Guid.NewGuid())
    {
        if (purchaseOrderItemId == Guid.Empty)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemIsNotFound");
        if (orderItemId == Guid.Empty)
            throw new PurchaseOrderItemDataIsInvalidException("Error.OrderItemIsNotFound");
        if (allocatedQuantity <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.AllocatedQuantityMustBePositive");

        PurchaseOrderItemId = purchaseOrderItemId;
        OrderItemId = orderItemId;
        AllocatedQuantity = allocatedQuantity;
        ReceivedQuantity = 0;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid PurchaseOrderItemId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public decimal AllocatedQuantity { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    internal void IncreaseReceived(decimal receivedQuantity)
    {
        if (receivedQuantity <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderReceiveQuantityMustBePositive");
        if (ReceivedQuantity + receivedQuantity > AllocatedQuantity)
            throw new PurchaseOrderItemDataIsInvalidException("Error.ReceivedQuantityCannotExceedAllocatedQuantity");

        ReceivedQuantity += receivedQuantity;
    }

    internal void ReduceAllocationToReceived()
    {
        AllocatedQuantity = ReceivedQuantity;
    }
}
