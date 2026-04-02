namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public abstract record BasePurchaseOrderAppDto
{
    public Guid? WarehouseId { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }

    public DateTime? ExpectedDeliveryDateUtc { get; set; }
    public string? Note { get; set; }

    public virtual (bool valid, string? errorMessage) Validate()
    {
        if (ExpectedDeliveryDateUtc.HasValue && ExpectedDeliveryDateUtc.Value < DateTime.UtcNow)
            return (false, "Expected delivery date must be in the future");
        if (TaxAmount < 0)
            return (false, "Tax amount must be greater than or equal to 0");
        if (ShippingAmount < 0)
            return (false, "Shipping amount must be greater than or equal to 0");


        return (true, string.Empty);
    }
}

[Serializable]
public sealed record PurchaseOrderAppDto(Guid Id) : BasePurchaseOrderAppDto
{
    public required string Code { get; set; }
    public decimal TotalAmount { get; set; }
    public int Status { get; set; }
    public IList<PurchaseOrderItemAppDto> Items { get; } = [];
    public DateTime CreatedOnUtc { get; set; }

    public bool CanAddItems { get; set; }
    public bool CanReceiveGoods { get; set; }

    public override (bool valid, string? errorMessage) Validate()
    {
        if (string.IsNullOrWhiteSpace(Code))
            return (false, "Code is required");
        if (TotalAmount < 0)
            return (false, "Total amount must be greater than or equal to 0");
        foreach (var item in Items)
        {
            var itemValidationResult = item.Validate();
            if (!itemValidationResult.valid)
                return itemValidationResult;
        }
        return base.Validate();
    }
}

[Serializable]
public sealed record CreatePurchaseOrderAppDto : BasePurchaseOrderAppDto;
[Serializable]
public sealed record CreatePurchaseOrderResultAppDto
{
    public required bool Success { get; init; }
    public Guid? CreatedId { get; set; }
    public string? ErrorMessage { get; set; }
}
