using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public abstract record BasePurchaseOrderDto
{
    public required DateTime PlacedOnUtc { get; init; }
    public required Guid VendorId { get; init; }
    public required Guid? WarehouseId { get; init; }

    public DateTime? ExpectedDeliveryDateUtc { get; set; }
    public string? Note { get; set; }

    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }

    public virtual void Verify()
    {
        if (ExpectedDeliveryDateUtc.HasValue && ExpectedDeliveryDateUtc.Value < DateTime.UtcNow.Date)
            throw new PurchaseOrderDataIsInvalidException("Error.ExpectedDeliveryDateInPast");
        if (TaxAmount < 0)
            throw new PurchaseOrderDataIsInvalidException("Error.TaxAmountCannotBeNegative");
        if (ShippingAmount < 0)
            throw new PurchaseOrderDataIsInvalidException("Error.ShippingAmountCannotBeNegative");
    }
}

[Serializable]
public sealed record PurchaseOrderDto(Guid Id) : BasePurchaseOrderDto
{
    public required string Code { get; init; }
    public required Guid? CreatedByUserId { get; init; }
    public required PurchaseOrderStatus Status { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public decimal TotalAmount { get; set; }
    public bool CanAddItems { get; set; }
    public bool CanReceiveGoods { get; set; }

    public IList<PurchaseOrderItemDto> Items { get; } = [];
}

[Serializable]
public sealed record CreatePurchaseOrderDto : BasePurchaseOrderDto
{
    public required string Code { get; init; }
    public required Guid? CreatedByUserId { get; init; }

    public IList<PurchaseOrderItemDto> Items { get; } = [];

    public override void Verify()
    {
        if (string.IsNullOrEmpty(Code))
            throw new PurchaseOrderDataIsInvalidException("Error.PurchaseOrderCodeRequired");

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
