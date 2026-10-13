using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderAllocationOfOrderItemsSpec : BaseSpecification<PurchaseOrderItemAllocation>
{
    public PurchaseOrderAllocationOfOrderItemsSpec(Guid orderId, IList<Guid> orderItemIds) 
        : base(allocation => allocation.OrderItemId.PrimaryId == orderId && orderItemIds.Contains(allocation.OrderItemId.SecondaryId))
    {
    }
    internal PurchaseOrderAllocationOfOrderItemsSpec(IList<Guid> orderItemIds) 
        : base(allocation => orderItemIds.Contains(allocation.OrderItemId.SecondaryId))
    {
    }
}