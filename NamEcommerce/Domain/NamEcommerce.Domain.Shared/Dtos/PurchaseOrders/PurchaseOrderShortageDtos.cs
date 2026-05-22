using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record ExistingDraftPurchaseOrderDto(Guid Id)
{
    public required Guid VendorId { get; init; }
    public required string Code { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? ExpectedDeliveryDateUtc { get; init; }
}

[Serializable]
public sealed record RelatedPurchaseOrderItemDto
{
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal QuantityOrdered { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public required decimal AvailableForAllocation { get; init; }
    public required decimal UnitCost { get; init; }
}

[Serializable]
public sealed record RelatedPurchaseOrderDto(Guid Id)
{
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public required string Code { get; init; }
    public required PurchaseOrderStatus Status { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? ExpectedDeliveryDateUtc { get; init; }
    public required decimal TotalAmount { get; init; }
    public required int ItemCount { get; init; }
    public required bool CanAllocate { get; init; }
    public required bool CanMerge { get; init; }
    public IList<RelatedPurchaseOrderItemDto> Items { get; init; } = [];
}

[Serializable]
public sealed record DirectShipInfoDto
{
    public required string Address { get; init; }
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public int Priority { get; init; }
}

[Serializable]
public sealed record CreatePoFromShortageItemDto
{
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? AllocationQuantity { get; init; }
    public decimal UnitCost { get; init; }
    public string? Note { get; init; }
    public DirectShipInfoDto? DirectShipInfo { get; init; }

    public void Verify()
    {
        if (OrderItemId == Guid.Empty)
            throw new PurchaseOrderItemDataIsInvalidException("Error.OrderItemIsNotFound");
        if (ProductId == Guid.Empty)
            throw new PurchaseOrderItemDataIsInvalidException("Error.ProductIsNotFound");
        if (Quantity <= 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemQuantityMustBePositive");
        if (AllocationQuantity < 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.AllocatedQuantityMustBePositive");
        if (AllocationQuantity > Quantity)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemAllocationQuantityExceedsAvailable");
        if (UnitCost < 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemUnitCostCannotBeNegative");
    }
}

[Serializable]
public sealed record CreatePoFromShortageDto
{
    public required Guid VendorId { get; init; }
    public Guid? WarehouseId { get; init; }
    public DateTime? ExpectedDeliveryDateUtc { get; init; }
    public string? Note { get; init; }
    public IList<CreatePoFromShortageItemDto> Items { get; init; } = [];

    public void Verify()
    {
        if (VendorId == Guid.Empty)
            throw new PurchaseOrderItemDataIsInvalidException("Error.VendorIsNotFound");
        if (Items.Count == 0)
            throw new PurchaseOrderItemDataIsInvalidException("Error.PurchaseOrderItemRequired");
        foreach (var item in Items)
            item.Verify();
    }
}

[Serializable]
public sealed record CreatePoFromShortageResultDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }
    public required bool CreatedNew { get; init; }
}
