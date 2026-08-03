using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.PurchaseOrders;

[Serializable]
public sealed class HaveStatusPurchaseOrderSpec(IList<PurchaseOrderStatus> status)
    : BaseSpecification<PurchaseOrder>(purchaseOrder => status.Contains(purchaseOrder.Status));
