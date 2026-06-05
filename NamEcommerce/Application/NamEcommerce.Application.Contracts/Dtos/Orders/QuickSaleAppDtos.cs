using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Dtos.Orders;

public enum QuickSaleFulfillmentMode
{
    DeliverNow = 10,
    NotDelivered = 20
}

public enum QuickSalePaymentTiming
{
    PayNow = 10,
    Unpaid = 20
}

[Serializable]
public sealed record QuickSaleItemAppDto
{
    public required Guid ProductId { get; init; }
    public Guid WarehouseId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (ProductId == Guid.Empty)
            return (false, "Error.ProductRequired");
        if (Quantity <= 0)
            return (false, "Error.OrderItemQuantityMustBePositive");
        if (UnitPrice < 0)
            return (false, "Error.OrderItemUnitPriceCannotBeNegative");

        return (true, null);
    }
}

[Serializable]
public sealed record CreateQuickSaleAppDto
{
    public required Guid CustomerId { get; init; }
    public required Guid WarehouseId { get; init; }
    public IList<QuickSaleItemAppDto> Items { get; init; } = [];
    public decimal? OrderDiscount { get; init; }
    public string? Note { get; init; }
    public int FulfillmentMode { get; init; } = (int)QuickSaleFulfillmentMode.DeliverNow;
    public int PaymentTiming { get; init; } = (int)QuickSalePaymentTiming.PayNow;
    public int PaymentMethod { get; init; }
    public decimal PaidAmount { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (CustomerId == Guid.Empty)
            return (false, "Error.CustomerRequired");
        if (!Enum.IsDefined(typeof(QuickSaleFulfillmentMode), FulfillmentMode))
            return (false, "Error.FastSaleFulfillmentModeInvalid");
        if (!Enum.IsDefined(typeof(QuickSalePaymentTiming), PaymentTiming))
            return (false, "Error.FastSalePaymentTimingInvalid");

        var fulfillmentMode = (QuickSaleFulfillmentMode)FulfillmentMode;
        var paymentTiming = (QuickSalePaymentTiming)PaymentTiming;

        if (fulfillmentMode == QuickSaleFulfillmentMode.DeliverNow
            && WarehouseId == Guid.Empty
            && Items.Any(item => item.WarehouseId == Guid.Empty))
        {
            return (false, "Error.WarehouseRequired");
        }
        if (Items.Count == 0)
            return (false, "Error.OrderItemRequired");
        if (OrderDiscount is < 0)
            return (false, "Error.OrderDiscountCannotBeNegative");
        if (paymentTiming == QuickSalePaymentTiming.PayNow && PaidAmount <= 0)
            return (false, "Error.PaymentAmountMustBePositive");
        if (paymentTiming == QuickSalePaymentTiming.Unpaid && PaidAmount != 0)
            return (false, "Error.PaymentAmountMustBeZeroWhenUnpaid");

        foreach (var item in Items)
        {
            var result = item.Validate();
            if (!result.valid)
                return result;
        }

        var subtotal = Items.Sum(item => item.Quantity * item.UnitPrice);
        if ((OrderDiscount ?? 0) > subtotal)
            return (false, "Error.OrderDiscountExceedsTotal");

        return (true, null);
    }
}

[Serializable]
public sealed record QuickSaleResultAppDto : CommonActionResultDto
{
    public Guid? OrderId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public Guid? CustomerDebtId { get; init; }
    public Guid? CustomerPaymentId { get; init; }
    public Guid? PaymentIntentId { get; init; }

    public static new QuickSaleResultAppDto CreateError(string? errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}
