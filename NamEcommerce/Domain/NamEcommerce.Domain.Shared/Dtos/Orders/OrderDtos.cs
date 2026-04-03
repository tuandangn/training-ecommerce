namespace NamEcommerce.Domain.Shared.Dtos.Orders;

[Serializable]
public abstract record BaseOrderDto
{
    public Guid? CustomerId { get; init; }
    public Guid? WarehouseId { get; init; }
    public string? Note { get; init; }
    public decimal? OrderDiscount { get; init; }
}

[Serializable]
public sealed record OrderDto(Guid Id) : BaseOrderDto
{
    public required decimal TotalAmount { get; init; }
    public string? WarehouseName { get; set; }
    public required int Status { get; init; }
    public IList<OrderItemDto> Items { get; } = [];
}

[Serializable]
public sealed record CreateOrderDto : BaseOrderDto
{
    public IList<OrderItemDto> Items { get; } = [];
}

[Serializable]
public sealed record CreateOrderResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateOrderDto(Guid Id) : BaseOrderDto { }
[Serializable]
public sealed record UpdateOrderResultDto { public required Guid UpdatedId { get; init; } }

[Serializable]
public sealed record AddOrderItemDto(Guid OrderId, Guid ProductId, decimal Quantity, decimal UnitPrice);

[Serializable]
public sealed record UpdateOrderItemDto(Guid OrderId, Guid ItemId, decimal Quantity, decimal UnitPrice);

[Serializable]
public sealed record DeleteOrderItemDto(Guid OrderId, Guid ItemId);

[Serializable]
public sealed record OrderItemDto(Guid ItemId, Guid ProductId, decimal Quantity, decimal UnitPrice)
{
    public (bool valid, string? message) Validate()
    {
        if (Quantity <= 0) return (false, "Quantity must be greater than 0");
        if (UnitPrice < 0) return (false, "Unit price must be >= 0");
        return (true, null);
    }
}
