using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public abstract record BasePurchaseOrderItemDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required decimal QuantityOrdered { get; init; }

    public decimal UnitCost { get; set; }
    public string? Note { get; set; }

    public void Verify()
    {
        if (QuantityOrdered <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemQuantityMustBePositive");
        if (UnitCost < 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemUnitCostCannotBeNegative");
    }
}

[Serializable]
public sealed record PurchaseOrderItemDto(Guid Id) : BasePurchaseOrderItemDto
{
    public decimal QuantityReceived { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal TotalCost { get; set; }
}

