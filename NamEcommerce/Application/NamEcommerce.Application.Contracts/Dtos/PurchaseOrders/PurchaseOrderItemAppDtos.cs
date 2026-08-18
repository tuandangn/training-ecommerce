namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public abstract record BasePurchaseOrderItemAppDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required decimal QuantityOrdered { get; init; }
    public decimal UnitCost { get; set; }
    public string? Note { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (QuantityOrdered <= 0)
            return (false, "Error.PurchaseOrderQuantityMustBePositive");
        if (UnitCost < 0)
            return (false, "Error.PurchaseOrderItemUnitCostCannotBeNegative");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record PurchaseOrderItemAppDto(Guid Id) : BasePurchaseOrderItemAppDto
{
    public decimal QuantityReceived { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal TotalCost { get; set; }
}

[Serializable]
public sealed record CreatePurchaseOrderItemAppDto
{
    public required Guid? ProductId { get; init; }
    public required decimal Quantity { get; init; }
    public decimal UnitCost { get; set; }
    public string? Note { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (Quantity <= 0)
            return (false, "Error.QuantityMustBePositive");

        if (UnitCost < 0)
            return (false, "Error.");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record AddPurchaseOrderItemAppDto() : BasePurchaseOrderItemAppDto;

[Serializable]
public sealed record UpdatePurchaseOrderItemAppDto()
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid PurchaseOrderItemId { get; init; }
    public required decimal Quantity { get; init; }
    public decimal UnitCost { get; set; }
    public string? Note { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (Quantity <= 0)
            return (false, "Error.PurchaseOrderQuantityMustBePositive");
        if (UnitCost < 0)
            return (false, "Error.PurchaseOrderItemUnitCostCannotBeNegative");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record DeletePurchaseOrderItemAppDto(Guid PurchaseOrderId, Guid ItemId);

