using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Dtos.Orders;

[Serializable]
public sealed record QuickCreateOrderAppDto2
{
    public Guid CustomerId { get; init; }
    public IList<QuickCreateOrderItemAppDto2> Items { get; init; } = [];
    public decimal? OrderDiscount { get; init; }
    public string? Note { get; init; }

    public bool DeliveryNow { get; init; }

    public string? ShippingAddress { get; set; }
    public string? ShippingPhoneNumber { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (CustomerId == Guid.Empty)
            return (false, "Error.CustomerRequired");

        if (DeliveryNow && Items.Any(item => !item.WarehouseId.HasValue))
            return (false, "Error.WarehouseRequired");

        if (Items.Count == 0)
            return (false, "Error.OrderItemRequired");

        if (OrderDiscount < 0)
            return (false, "Error.OrderDiscountCannotBeNegative");

        if (!DeliveryNow)
        {
            if (string.IsNullOrWhiteSpace(ShippingAddress))
                return (false, "Error.ShippingAddressRequired");
            if (string.IsNullOrWhiteSpace(ShippingPhoneNumber))
                return (false, "Error.PhoneNumberRequired");
        }

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

    [Serializable]
    public sealed record QuickCreateOrderItemAppDto2
    {
        public required Guid ProductId { get; init; }
        public Guid? WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public (bool valid, string? errorMessage) Validate()
        {
            if (Quantity <= 0)
                return (false, "Error.OrderItemQuantityMustBePositive");
            if (UnitPrice < 0)
                return (false, "Error.OrderItemUnitPriceCannotBeNegative");

            return (true, null);
        }
    }
}

[Serializable]
public sealed record CompleteQuickCreateOrderPaymentAppDto
{
    public required Guid OrderId { get; init; }
    public required decimal PaidAmount { get; init; }
    public required Guid? PaymentIntentId { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (PaidAmount < 0)
            return (false, "Error.PaidAmountCannotBeNegative");

        return (true, null);
    }
}


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
public sealed record QuickCreateOrderItemAppDto
{
    public required Guid ProductId { get; init; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

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
public sealed record QuickCreateOrderAppDto
{
    public Guid CustomerId { get; init; }
    public IList<QuickCreateOrderItemAppDto> Items { get; init; } = [];
    public decimal? OrderDiscount { get; init; }
    public string? Note { get; init; }
    public int FulfillmentMode { get; init; } = (int)QuickSaleFulfillmentMode.DeliverNow;
    public int PaymentTiming { get; init; } = (int)QuickSalePaymentTiming.PayNow;
    public int PaymentMethod { get; init; }
    public decimal PaidAmount { get; init; }
    public string? ShippingAddress { get; set; }
    public string? ShippingPhoneNumber { get; set; }

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
        if (fulfillmentMode == QuickSaleFulfillmentMode.DeliverNow && Items.Any(item => item.WarehouseId == Guid.Empty))
            return (false, "Error.WarehouseRequired");

        if (Items.Count == 0)
            return (false, "Error.OrderItemRequired");

        if (OrderDiscount is < 0)
            return (false, "Error.OrderDiscountCannotBeNegative");

        if (paymentTiming == QuickSalePaymentTiming.PayNow && PaidAmount <= 0)
            return (false, "Error.PaymentAmountMustBePositive");
        if (paymentTiming == QuickSalePaymentTiming.Unpaid && PaidAmount != 0)
            return (false, "Error.PaymentAmountMustBeZeroWhenUnpaid");

        if (fulfillmentMode == QuickSaleFulfillmentMode.NotDelivered)
        {
            if (string.IsNullOrWhiteSpace(ShippingAddress))
                return (false, "Error.ShippingAddressRequired");
            if (string.IsNullOrWhiteSpace(ShippingPhoneNumber))
                return (false, "Error.PhoneNumberRequired");
        }

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
public sealed record QuickSaleOrderItemResultAppDto
{
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
}

[Serializable]
public sealed record QuickCreateOrderResultAppDto : CommonActionResultDto
{
    public Guid? OrderId { get; init; }

    public static new QuickCreateOrderResultAppDto CreateError(string? errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}

[Serializable]
public sealed record QuickSaleResultAppDto : CommonActionResultDto
{
    public Guid? OrderId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public Guid? CustomerDebtId { get; init; }
    public Guid? CustomerPaymentId { get; init; }
    public Guid? PaymentIntentId { get; init; }
    public IList<QuickSaleOrderItemResultAppDto> OrderItems { get; init; } = [];

    public static new QuickSaleResultAppDto CreateError(string? errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}
