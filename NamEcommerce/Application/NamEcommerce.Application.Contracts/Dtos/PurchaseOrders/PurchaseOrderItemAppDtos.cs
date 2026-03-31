namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public abstract record BasePurchaseOrderItemAppDto
{
    public required Guid PurchaseOrderId { get; set; }
    public required Guid ProductId { get; init; }

    public decimal QuantityOrdered { get; set; }
    public decimal UnitCost { get; set; }

    public string? Note { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (QuantityOrdered <= 0)
            return (false, "Item quantity ordered must be greater than 0");
        if (UnitCost < 0)
            return (false, "Item unit cost must be greater than or equal to 0");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record PurchaseOrderItemAppDto(Guid Id) : BasePurchaseOrderItemAppDto
{
    public decimal QuantityReceived { get; set; }
}

[Serializable]
public sealed record AddPurchaseOrderItemAppDto() : BasePurchaseOrderItemAppDto;
[Serializable]
public sealed record AddPurchaseOrderItemResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
    public Guid PurchaseOrderId { get; init; }
    public Guid? CreatedItemId { get; init; }
}

[Serializable]
public sealed record ReceivedGoodsForItemAppDto(Guid PurchaseOrderId, Guid PurchaseOrderItemId)
{
    public decimal ReceivedQuantity { get; set; }
    public Guid ReceivedByUserId { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (ReceivedQuantity < 0)
            return (false, "Received quantity must be greater than or equal to 0");

        return (true, string.Empty);
    }
}
[Serializable]
public sealed record ReceivedGoodsForItemResultAppDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal ReceivedQuantity { get; set; }
}
