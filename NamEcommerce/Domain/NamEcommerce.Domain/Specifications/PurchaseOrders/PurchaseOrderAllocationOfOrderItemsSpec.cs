using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrderAllocationOfOrderItemsSpec(Guid orderId, IList<Guid> orderItemIds)
    : BaseSpecification<PurchaseOrderItemAllocation>(allocation 
        => allocation.OrderItemId.PrimaryId == orderId && orderItemIds.Contains(allocation.OrderItemId.SecondaryId));
