namespace NamEcommerce.Application.Contracts.Dtos.Orders;

[Serializable]
public sealed record CreateOrderAppDto
{
    public required Guid CustomerId { get; init; }
    public Guid? WarehouseId { get; init; }
    public string? Note { get; init; }
    public decimal? OrderDiscount { get; init; }
    public IList<OrderItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CreateOrderResultAppDto
{
    public required bool Success { get; init; }
    public Guid? CreatedId { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record UpdateOrderAppDto(Guid Id)
{
    public string? Note { get; init; }
    public decimal? OrderDiscount { get; init; }
}

[Serializable]
public sealed record UpdateOrderResultAppDto
{
    public required bool Success { get; init; }
    public Guid? UpdatedId { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record AddOrderItemAppDto(Guid OrderId, Guid ProductId, decimal Quantity, decimal UnitPrice);

[Serializable]
public sealed record AddOrderItemResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record OrderAppDto(Guid Id) : BaseOrderAppDto
{
    public required decimal TotalAmount { get; init; }
    public required int Status { get; init; }
    public string? CustomerName { get; set; }
    public string? WarehouseName { get; set; }
    public IList<OrderItemAppDto> Items { get; } = [];
}

[Serializable]
public abstract record BaseOrderAppDto
{
    public Guid CustomerId { get; init; }
    public Guid? WarehouseId { get; init; }
    public string? Note { get; init; }
    public decimal OrderDiscount { get; init; }
}

[Serializable]
public sealed record UpdateOrderItemAppDto(Guid OrderId, Guid ItemId, decimal Quantity, decimal UnitPrice);

[Serializable]
public sealed record UpdateOrderItemResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record DeleteOrderItemAppDto(Guid OrderId, Guid ItemId);

[Serializable]
public sealed record DeleteOrderItemResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record OrderItemAppDto(Guid ItemId, Guid ProductId, decimal Quantity, decimal UnitPrice);
