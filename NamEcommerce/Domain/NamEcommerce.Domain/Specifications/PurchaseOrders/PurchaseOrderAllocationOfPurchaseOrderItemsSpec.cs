using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderAllocationOfPurchaseOrderItemsSpec : BaseSpecification<PurchaseOrderItemAllocation>
{
    public PurchaseOrderAllocationOfPurchaseOrderItemsSpec(Guid purchaseOrderId, IList<Guid> purchaseOrderItemIds)
        : base(allocation => allocation.PurchaseOrderItemId.PrimaryId == purchaseOrderId && purchaseOrderItemIds.Contains(allocation.PurchaseOrderItemId.SecondaryId))
    {
    }
    internal PurchaseOrderAllocationOfPurchaseOrderItemsSpec(IList<Guid> purchaseOrderItemIds)
        : base(allocation => purchaseOrderItemIds.Contains(allocation.PurchaseOrderItemId.SecondaryId))
    {
    }
}
