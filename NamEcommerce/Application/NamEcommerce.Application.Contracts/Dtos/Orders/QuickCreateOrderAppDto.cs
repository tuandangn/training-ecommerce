namespace NamEcommerce.Application.Contracts.Dtos.Orders;

[Serializable]
public sealed record QuickCreateOrderAppDto
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

