using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.PurchaseOrders;

[Serializable]
public sealed class ActivePurchaseOrderAllocationOfPurchaseOrderItemSpec : BaseSpecification<PurchaseOrderItemAllocation>
{
    public ActivePurchaseOrderAllocationOfPurchaseOrderItemSpec(Guid purchaseOrderId, IList<Guid> purchaseOrderItemIds)
        : base(new NotHaveStatusPurchaseOrderAllocationSpec([AllocationStatus.Cancelled]).Criteria.And(new PurchaseOrderAllocationOfPurchaseOrderItemsSpec(purchaseOrderId, purchaseOrderItemIds).Criteria))
    {
    }
    public ActivePurchaseOrderAllocationOfPurchaseOrderItemSpec(IList<Guid> purchaseOrderItemIds)
        : base(new NotHaveStatusPurchaseOrderAllocationSpec([AllocationStatus.Cancelled]).Criteria.And(new PurchaseOrderAllocationOfPurchaseOrderItemsSpec(purchaseOrderItemIds).Criteria))
    {
    }
}