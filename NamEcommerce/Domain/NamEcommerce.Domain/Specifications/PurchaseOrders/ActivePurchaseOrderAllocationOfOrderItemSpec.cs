using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.PurchaseOrders;

[Serializable]
public sealed class ActivePurchaseOrderAllocationOfOrderItemSpec : BaseSpecification<PurchaseOrderItemAllocation>
{
    public ActivePurchaseOrderAllocationOfOrderItemSpec(Guid orderId, IList<Guid> orderItemIds)
        : base(new NotHaveStatusPurchaseOrderAllocationSpec([AllocationStatus.Cancelled]).Criteria.And(new PurchaseOrderAllocationOfOrderItemsSpec(orderId, orderItemIds).Criteria))
    {
    }
    public ActivePurchaseOrderAllocationOfOrderItemSpec(IList<Guid> orderItemIds)
        : base(new NotHaveStatusPurchaseOrderAllocationSpec([AllocationStatus.Cancelled]).Criteria.And(new PurchaseOrderAllocationOfOrderItemsSpec(orderItemIds).Criteria))
    {
    }
}