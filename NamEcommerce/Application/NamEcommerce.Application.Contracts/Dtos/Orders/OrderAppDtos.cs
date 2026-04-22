using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Dtos.Orders;

[Serializable]
public abstract record BaseOrderAppDto
{
    public DateTime? ExpectedShippingDateUtc { get; set; }
    public string? Note { get; init; }
    public decimal? OrderDiscount { get; init; }

    public virtual (bool valid, string? errorMessage) Validate()
    {
        if (OrderDiscount.HasValue && OrderDiscount < 0)
            return (false, "Error.OrderDiscountCannotBeNegative");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record OrderAppDto(Guid Id) : BaseOrderAppDto
{
    public required string Code { get; init; }
    public required Guid CustomerId { get; init; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }

    public required Guid? CreatedByUserId { get; init; }
    public string? CreatedByUsername { get; set; }

    public required decimal OrderSubTotal { get; init; }
    public required decimal TotalAmount { get; init; }
    public required int Status { get; init; }
    public bool IsFinished { get; set; }
    public string? LockOrderReason { get; set; }

    public string? ShippingAddress { get; set; }

    public bool CanUpdateInfo { get; init; }
    public bool CanCancelOrder { get; init; }
    public bool CanUpdateOrderItems { get; init; }

    public DateTime CreatedOnUtc { get; set; }

    public IList<OrderItemAppDto> Items { get; } = [];
}

[Serializable]
public sealed record CreateOrderAppDto : BaseOrderAppDto
{
    public required Guid CustomerId { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public string? ShippingAddress { get; set; }
    public IList<OrderItemAppDto> Items { get; } = [];

    public override (bool valid, string? errorMessage) Validate()
    {
        if (ExpectedShippingDateUtc < DateTime.UtcNow.Date)
            return (false, "Error.ExpectedShippingDateInvalid");

        foreach (var item in Items)
        {
            var validateResult = item.Validate();
            if (!validateResult.valid)
                return validateResult;
        }

        if (OrderDiscount > Items.Sum(item => item.Quantity * item.UnitPrice))
            return (false, "Error.OrderDiscountExceedsTotal");

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
                return (false, "Error.OrderItemQuantityMustBePositive");
            if (UnitPrice < 0)
                return (false, "Error.OrderItemUnitPriceCannotBeNegative");

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
public sealed record DeleteOrderAppDto(Guid OrderId);
[Serializable]
public sealed record DeleteOrderResultAppDto : CommonActionResultDto;

[Serializable]
public sealed record UpdateOrderShippingAppDto
{
    public required Guid OrderId { get; init; }
    public DateTime? ExpectedShippingDateUtc { get; set; }
    public string? Address { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (ExpectedShippingDateUtc < DateTime.UtcNow.Date)
            return (false, "Error.ExpectedShippingDateInvalid");

        return (true, string.Empty);
    }
}
[Serializable]
public sealed record UpdateOrderShippingResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record LockOrderAppDto
{
    public required Guid OrderId { get; init; }
    public required string? Reason { get; init; }
}
[Serializable]
public sealed record LockOrderResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record MarkOrderItemDeliveredAppDto
{
    public required Guid OrderId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required Guid PictureId { get; init; }
}
[Serializable]
public sealed record MarkOrderItemDeliveredResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
    public bool OrderAutoLocked { get; set; }
}
