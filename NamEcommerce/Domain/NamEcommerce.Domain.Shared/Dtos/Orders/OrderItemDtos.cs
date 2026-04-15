using NamEcommerce.Domain.Shared.Exceptions.Orders;

namespace NamEcommerce.Domain.Shared.Dtos.Orders;

[Serializable]
public record BaseOrderItemDto
{
    public required Guid ProductId { get; init; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public void Verify()
    {
        if (Quantity <= 0)
            throw new OrderItemDataIsInvalidException("Quantity must be greater than 0");
        if (UnitPrice < 0)
            throw new OrderItemDataIsInvalidException("Unit price must be >= 0");
    }
}

[Serializable]
public sealed record OrderItemDto(Guid Id) : BaseOrderItemDto
{
    public required Guid OrderId { get; init; }
    public string? ProductName { get; set; }
    public decimal SubTotal { get; set; }
    public bool IsDelivered { get; set; }
    public DateTime? DeliveredOnUtc { get; set; }
}

[Serializable]
public sealed record AddOrderItemDto : BaseOrderItemDto;

[Serializable]
public sealed record UpdateOrderItemDto
{
    public required Guid OrderId { get; init; }
    public required Guid OrderItemId { get; init; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public void Verify()
    {
        if (Quantity <= 0)
            throw new OrderItemDataIsInvalidException("Quantity must be greater than 0");
        if (UnitPrice < 0)
            throw new OrderItemDataIsInvalidException("Unit price must be >= 0");
    }
}

[Serializable]
public sealed record DeleteOrderItemDto(Guid OrderId, Guid OrderItemId);

[Serializable]
public sealed record MarkOrderItemDeliveredDto
{
    public required Guid OrderId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required Guid PictureId { get; init; }
}
