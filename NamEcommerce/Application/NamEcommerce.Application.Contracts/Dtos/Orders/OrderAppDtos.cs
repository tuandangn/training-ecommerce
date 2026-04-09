namespace NamEcommerce.Application.Contracts.Dtos.Orders;

[Serializable]
public abstract record BaseOrderAppDto
{
    public DateTime ExpectedShippingDateUtc { get; set; }
    public string? Note { get; init; }
    public decimal? OrderDiscount { get; init; }

    public virtual (bool valid, string? errorMessage) Validate()
    {
        if (OrderDiscount.HasValue && OrderDiscount < 0)
            return (false, "Order discount cannot less than 0");
        if (ExpectedShippingDateUtc < DateTime.UtcNow.Date)
            return (false, "Expected shipping date is invalid");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record OrderAppDto(Guid Id) : BaseOrderAppDto
{
    public required string Code { get; init; }
    public required Guid CustomerId { get; init; }
    public required Guid? CreatedByUserId { get; init; }

    public required decimal TotalAmount { get; init; }
    public required int Status { get; init; }

    // Payment
    public int PaymentStatus { get; set; }
    public int? PaymentMethod { get; set; }
    public DateTime? PaidOnUtc { get; set; }
    public string? PaymentNote { get; set; }

    // Shipping
    public int ShippingStatus { get; set; }
    public string? ShippingAddress { get; set; }
    public DateTime? ShippedOnUtc { get; set; }
    public string? ShippingNote { get; set; }

    // Cancellation
    public string? CancellationReason { get; set; }

    public IList<OrderItemAppDto> Items { get; } = [];
}

[Serializable]
public sealed record CreateOrderAppDto : BaseOrderAppDto
{
    public required Guid CustomerId { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public IList<OrderItemAppDto> Items { get; } = [];

    public override (bool valid, string? errorMessage) Validate()
    {
        foreach (var item in Items)
        {
            var validateResult = item.Validate();
            if (!validateResult.valid)
                return validateResult;
        }

        return base.Validate();
    }

    [Serializable]
    public record OrderItemAppDto
    {
        public required Guid ProductId { get; init; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public (bool valid, string? errorMessage) Validate()
        {
            if (Quantity <= 0)
                return (false, "Quantity must be greater than 0");
            if (UnitPrice < 0)
                return (false, "Unit price must be >= 0");

            return (true, string.Empty);
        }
    }
}

[Serializable]
public sealed record CreateOrderResultAppDto
{
    public required bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateOrderAppDto(Guid Id) : BaseOrderAppDto;

[Serializable]
public sealed record UpdateOrderResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
    public Guid? UpdatedId { get; init; }
}

[Serializable]
public sealed record MarkOrderAsPaidAppDto
{
    public required Guid OrderId { get; init; }
    public required int PaymentMethod { get; init; }
    public string? Note { get; set; }
}
[Serializable]
public sealed record MarkOrderAsPaidResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record UpdateOrderShippingAppDto
{
    public required Guid OrderId { get; init; }
    public required int ShippingStatus { get; init; }
    public string? Address { get; set; }
    public string? Note { get; set; }
}
[Serializable]
public sealed record UpdateOrderShippingResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record CancelOrderAppDto
{
    public required Guid OrderId { get; init; }
    public required string? Reason { get; init; }
}
[Serializable]
public sealed record CancelOrderResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
}
