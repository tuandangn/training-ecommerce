namespace NamEcommerce.Application.Contracts.Dtos.Orders;

[Serializable]
public record BaseOrderItemAppDto
{
    public required Guid ProductId { get; init; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (Quantity <= 0)
            return (false, "Error.OrderItemQuantityMustBePositive");
        if (UnitPrice < 0)
            return (false, "Error.OrderItemUnitPriceCannotBeNegative");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record OrderItemAppDto(Guid Id) : BaseOrderItemAppDto
{
    public required Guid OrderId { get; init; }

    public string? ProductName { get; set; }
    public bool IsDelivered { get; set; }
    public DateTime? DeliveredOnUtc { get; set; }
}

[Serializable]
public sealed record AddOrderItemAppDto : BaseOrderItemAppDto
{
    public required Guid OrderId { get; init; }
}
[Serializable]
public sealed record AddOrderItemResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
    public Guid OrderId { get; init; }
}

[Serializable]
public sealed record UpdateOrderItemAppDto
{
    public required Guid OrderId { get; init; }
    public required Guid OrderItemId { get; init; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (Quantity <= 0)
            return (false, "Error.OrderItemQuantityMustBePositive");
        if (UnitPrice < 0)
            return (false, "Error.OrderItemUnitPriceCannotBeNegative");

        return (true, string.Empty);
    }
}
[Serializable]
public sealed record UpdateOrderItemResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
    public Guid OrderId { get; init; }
    public Guid? UpdatedItemId { get; init; }
}

[Serializable]
public sealed record DeleteOrderItemAppDto(Guid OrderId, Guid OrderItemId);
[Serializable]
public sealed record DeleteOrderItemResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
}
