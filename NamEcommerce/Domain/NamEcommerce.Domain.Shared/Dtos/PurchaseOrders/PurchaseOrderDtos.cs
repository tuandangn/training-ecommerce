using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public abstract record BasePurchaseOrderDto
{
    public Guid? VendorId { get; init; }

    public DateTime? ExpectedDeliveryDateUtc { get; set; }
    public string? Note { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }

    public virtual void Verify()
    {
        if (!VendorId.HasValue)
            throw new PurchaseOrderDataIsInvalidException("Vendor is not empty");
        if (ExpectedDeliveryDateUtc.HasValue && ExpectedDeliveryDateUtc.Value < DateTime.UtcNow.Date)
            throw new PurchaseOrderDataIsInvalidException("Expected delivery date must be in the future");
        if (TaxAmount < 0)
            throw new PurchaseOrderDataIsInvalidException("Tax amount must be greater than or equal to 0");
        if (ShippingAmount < 0)
            throw new PurchaseOrderDataIsInvalidException("Shipping amount must be greater than or equal to 0");
    }
}

[Serializable]
public sealed record PurchaseOrderDto(Guid Id) : BasePurchaseOrderDto
{
    public required string Code { get; init; }
    public required Guid? WarehouseId { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public PurchaseOrderStatus Status { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public bool CanAddItems { get; set; }
    public bool CanReceiveGoods { get; set; }

    public IList<PurchaseOrderItemDto> Items { get; } = [];
}

[Serializable]
public sealed record CreatePurchaseOrderDto : BasePurchaseOrderDto
{
    public required string Code { get; init; }
    public required Guid? WarehouseId { get; init; }
    public required Guid? CreatedByUserId { get; init; }

    public IList<PurchaseOrderItemDto> Items { get; } = [];

    public override void Verify()
    {
        if (string.IsNullOrEmpty(Code))
            throw new PurchaseOrderDataIsInvalidException("Code is not empty");

        foreach (var item in Items)
            item.Verify();

        base.Verify();
    }
}
[Serializable]
public sealed record CreatePurchaseOrderResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdatePurchaseOrderDto(Guid Id) : BasePurchaseOrderDto;
[Serializable]
public sealed record UpdatePurchaseOrderResultDto(Guid Id) : BasePurchaseOrderDto;
