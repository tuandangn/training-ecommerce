using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public abstract record BasePurchaseOrderDto
{
    public required string Code { get; init; }
    public required Guid? VendorId { get; init; }
    public required Guid? WarehouseId { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public PurchaseOrderStatus Status { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? Note { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }

    public IList<PurchaseOrderItemDto> Items { get; } = [];

    public virtual void Verify()
    {
        if (string.IsNullOrEmpty(Code))
            throw new PurchaseOrderDataIsInvalidException("Code is not empty");
        if (ExpectedDeliveryDate.HasValue && ExpectedDeliveryDate.Value < DateTime.UtcNow)
            throw new PurchaseOrderDataIsInvalidException("Expected delivery date must be in the future");
        if (TaxAmount < 0)
            throw new PurchaseOrderDataIsInvalidException("Tax amount must be greater than or equal to 0");
        if (ShippingAmount < 0)
            throw new PurchaseOrderDataIsInvalidException("Shipping amount must be greater than or equal to 0");

        foreach (var item in Items)
            item.Verify();
    }
}

[Serializable]
public sealed record PurchaseOrderDto(Guid Id) : BasePurchaseOrderDto;

[Serializable]
public sealed record CreatePurchaseOrderDto : BasePurchaseOrderDto;
[Serializable]
public sealed record CreatePurchaseOrderResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdatePurchaseOrderDto(Guid Id) : BasePurchaseOrderDto;
[Serializable]
public sealed record UpdatePurchaseOrderResultDto(Guid Id) : BasePurchaseOrderDto;
