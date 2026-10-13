using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.PurchaseOrders;

[Serializable]
public sealed class PurchaseOrdersOfPurchaseOrderItemsSpec : BaseSpecification<PurchaseOrder>
{
    public PurchaseOrdersOfPurchaseOrderItemsSpec(IList<Guid> purchaseOrderItemIds) 
        : base(purchaseOrder => purchaseOrder.Items.Any(item => purchaseOrderItemIds.Contains(item.Id)))
    {
    }
}