namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public abstract record BasePurchaseOrderAppDto
{
    public required DateTime PlacedOnUtc { get; init; }
    public required Guid VendorId { get; init; }
    public required Guid? WarehouseId { get; init; }

    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }

    public DateTime? ExpectedDeliveryDateUtc { get; set; }
    public string? Note { get; set; }

    public virtual (bool valid, string? errorMessage) Validate()
    {
        if (ExpectedDeliveryDateUtc.HasValue && ExpectedDeliveryDateUtc.Value < DateTime.UtcNow.Date)
            return (false, "Error.ExpectedDeliveryDateCannotBeInPast");
        if (TaxAmount < 0)
            return (false, "Error.TaxAmountCannotBeNegative");
        if (ShippingAmount < 0)
            return (false, "Error.ShippingAmountCannotBeNegative");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record PurchaseOrderAppDto(Guid Id) : BasePurchaseOrderAppDto
{
    public required string Code { get; set; }
    public required int Status { get; init; }

    public decimal TotalAmount { get; set; }

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
    public required Guid? ProductId { get; init; }
    public required decimal Quantity { get; init; }
    public decimal UnitCost { get; set; }
}
