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
