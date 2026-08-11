namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public abstract record BasePurchaseOrderAppDto
{
    public required DateTime PlacedOnUtc { get; init; }
    public required Guid VendorId { get; init; }
    public required Guid? WarehouseId { get; init; }

    public DateTime? ExpectedDeliveryDateUtc { get; set; }
    public string? Note { get; set; }

    public virtual (bool valid, string? errorMessage) Validate()
    {
        if (PlacedOnUtc > DateTime.UtcNow)
            return (false, "Error.PlacedOrderDateCannotBeInFuture");

        if (ExpectedDeliveryDateUtc.HasValue && ExpectedDeliveryDateUtc.Value < PlacedOnUtc)
            return (false, "Error.ExpectedDeliveryDateCannotBeLessThanPlaceOrderDate");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record PurchaseOrderAppDto(Guid Id) : BasePurchaseOrderAppDto
{
    public required string Code { get; init; }
    public required int Status { get; init; }

    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal AccumulatedTaxAmount { get; set; }
    public decimal AccumulatedShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public IList<PurchaseOrderItemAppDto> Items { get; } = [];

    public DateTime CreatedOnUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }

    public bool CanAddItems { get; init; }
    public bool CanReceiveGoods { get; init; }
    public bool CanModifyInfo { get; init; }
    public bool CanChangeDate { get; init; }
    public bool CanChangeFees { get; init; }
    public bool CanChangeVendor { get; init; }
    public bool CanAllocation { get; init; }
}

[Serializable]
public sealed record CreatePurchaseOrderAppDto : BasePurchaseOrderAppDto
{
    public IList<CreatePurchaseOrderItemAppDto> Items { get; init; } = [];

    public decimal TaxAmount { get; init; }
    public decimal ShippingAmount { get; init; }

    public override (bool valid, string? errorMessage) Validate()
    {
        if (Items.Count == 0)
            return (false, "Error.PurchaseOrderItemRequired");
        foreach (var item in Items)
        {
            var itemValidate = item.Validate();
            if (!itemValidate.valid)
                return itemValidate;
        }

        if (TaxAmount < 0)
            return (false, "Error.TaxAmountCannotBeNegative");
        if (ShippingAmount < 0)
            return (false, "Error.ShippingAmountCannotBeNegative");

        return base.Validate();
    }
}
[Serializable]
public sealed class SplitPurchaseOrderAppDto
{
    public Guid PurchaseOrderId { get; init; }
    public IList<SplitItemAppDto> Items { get; init; } = [];


    public (bool valid, string? errorMessage) Validate()
    {
        if (Items.Count == 0)
            return (false, "Error.PurchaseOrderItemRequired");

        foreach (var item in Items)
            return item.Validate();

        return (true, string.Empty);
    }

    [Serializable]
    public sealed class SplitItemAppDto
    {
        public Guid ItemId { get; init; }
        public decimal Quantity { get; init; }

        public (bool valid, string? errorMessage) Validate()
        {
            if (Quantity <= 0)
                return (false, "Error.PurchaseOrderItemQuantityMustBePositive");

            return (true, string.Empty);
        }
    }
}

[Serializable]
public sealed record CreatePurchaseOrderResultAppDto
{
    public required bool Success { get; init; }
    public Guid? CreatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record UpdatePurchaseOrderAppDto(Guid Id) : BasePurchaseOrderAppDto
{
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
}
[Serializable]
public sealed record UpdatePurchaseOrderResultAppDto
{
    public required bool Success { get; init; }
    public Guid? UpdatedId { get; set; }
    public string? ErrorMessage { get; set; }
}