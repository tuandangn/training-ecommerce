namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public abstract record BasePurchaseOrderAppDto
{    public Guid? VendorId { get; set; }
    public Guid? WarehouseId { get; set; }

    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }

    public DateTime? ExpectedDeliveryDateUtc { get; set; }
    public string? Note { get; set; }

    public virtual (bool valid, string? errorMessage) Validate()
    {
        if (ExpectedDeliveryDateUtc.HasValue && ExpectedDeliveryDateUtc.Value < DateTime.UtcNow.Date)
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
    public Guid? WarehouseId { get; set; }

    public required string Code { get; set; }
    public decimal TotalAmount { get; set; }
    public int Status { get; set; }
    public IList<PurchaseOrderItemAppDto> Items { get; } = [];
    public DateTime CreatedOnUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public bool CanAddItems { get; set; }
    public bool CanReceiveGoods { get; set; }
}

[Serializable]
public sealed record CreatePurchaseOrderAppDto : BasePurchaseOrderAppDto
{
    public Guid? CreatedByUserId { get; set; }
    public IList<CreatePurchaseOrderItemAppDto> Items { get; init; } = [];
}
[Serializable]
public sealed record CreatePurchaseOrderResultAppDto
{
    public required bool Success { get; init; }
    public Guid? CreatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record UpdatePurchaseOrderAppDto(Guid Id) : BasePurchaseOrderAppDto;
[Serializable]
public sealed record UpdatePurchaseOrderResultAppDto
{
    public required bool Success { get; init; }
    public Guid? UpdatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record CreatePurchaseOrderItemAppDto
{
    public Guid? ProductId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
}
