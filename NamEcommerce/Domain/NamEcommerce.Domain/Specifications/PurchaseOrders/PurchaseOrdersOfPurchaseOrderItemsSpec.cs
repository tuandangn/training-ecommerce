using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrdersOfPurchaseOrderItemsSpec(IList<Guid> purchaseOrderItemIds)
    : BaseSpecification<PurchaseOrder>(purchaseOrder => purchaseOrder.Items.Any(item => purchaseOrderItemIds.Contains(item.Id)));
