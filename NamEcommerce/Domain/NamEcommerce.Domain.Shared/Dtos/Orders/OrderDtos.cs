using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions.Orders;

namespace NamEcommerce.Domain.Shared.Dtos.Orders;

[Serializable]
public abstract record BaseOrderDto
{
    public DateTime ExpectedShippingDateUtc { get; set; }
    public string? Note { get; init; }
    public decimal? OrderDiscount { get; init; }

    public virtual void Verify()
    {
        if (OrderDiscount.HasValue && OrderDiscount < 0)
            throw new OrderDataIsInvalidException("Order discount cannot less than 0");
        if (ExpectedShippingDateUtc < DateTime.UtcNow.Date)
            throw new OrderDataIsInvalidException("Expected shipping date is invalid");
    }
}

[Serializable]
public sealed record OrderDto(Guid Id) : BaseOrderDto
{
    public required string Code { get; init; }
    public required Guid CustomerId { get; init; }
    public required Guid? CreatedByUserId { get; init; }

    public required decimal TotalAmount { get; init; }
    public required OrderStatus Status { get; init; }

    // Payment
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public DateTime? PaidOnUtc { get; set; }
    public string? PaymentNote { get; set; }

    // Shipping
    public ShippingStatus ShippingStatus { get; set; }
    public string? ShippingAddress { get; set; }
    public DateTime? ShippedOnUtc { get; set; }
    public string? ShippingNote { get; set; }

    // Cancellation
    public string? CancellationReason { get; set; }

    public IList<OrderItemDto> Items { get; } = [];
}

[Serializable]
public sealed record CreateOrderDto : BaseOrderDto
{
    public required string Code { get; init; }
    public required Guid CustomerId { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public IList<AddOrderItemDto> Items { get; } = [];

    public override void Verify()
    {
        foreach (var item in Items)
            item.Verify();

        base.Verify();
    }
}

[Serializable]
public sealed record CreateOrderResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateOrderDto(Guid Id) : BaseOrderDto;

[Serializable]
public sealed record UpdateOrderResultDto
{
    public required Guid UpdatedId { get; init; }
}

[Serializable]
public sealed record MarkAsPaidDto
{
    public required Guid OrderId { get; init; }
    public required PaymentMethod PaymentMethod { get; init; }
    public string? Note { get; set; }
}

[Serializable]
public sealed record UpdateShippingDto
{
    public required Guid OrderId { get; init; }
    public required ShippingStatus ShippingStatus { get; init; }
    public string? Address { get; set; }
    public string? Note { get; set; }

}

[Serializable]
public sealed record CancelOrderDto
{
    public required Guid OrderId { get; init; }
    public required string? Reason { get; init; }
}
