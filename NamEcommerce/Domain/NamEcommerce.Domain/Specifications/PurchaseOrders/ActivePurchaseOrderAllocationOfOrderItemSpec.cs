using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.PurchaseOrders;

[Serializable]
public sealed class ActivePurchaseOrderAllocationOfOrderItemSpec(Guid orderId, IList<Guid> orderItemIds)
    : BaseSpecification<PurchaseOrderItemAllocation>(
        new NotHaveStatusPurchaseOrderAllocationSpec([AllocationStatus.Cancelled]).Criteria
        .And(new PurchaseOrderAllocationOfOrderItemsSpec(orderId, orderItemIds).Criteria)
    );
