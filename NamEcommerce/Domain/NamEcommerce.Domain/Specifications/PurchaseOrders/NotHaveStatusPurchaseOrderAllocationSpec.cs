using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.PurchaseOrders;

[Serializable]
public sealed class NotHaveStatusPurchaseOrderAllocationSpec(IList<AllocationStatus> status)
    : BaseSpecification<PurchaseOrderItemAllocation>(allocation => !status.Contains(allocation.Status));
