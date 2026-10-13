using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record UpdatePurchaseOrderItemDto : BasePurchaseOrderItemDto
{
    public required Guid PurchaseOrderItemId { get; init; }

    public new void Verify()
    {
        if (PurchaseOrderItemId == Guid.Empty)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemIsNotFound");

        base.Verify();
    }
}

