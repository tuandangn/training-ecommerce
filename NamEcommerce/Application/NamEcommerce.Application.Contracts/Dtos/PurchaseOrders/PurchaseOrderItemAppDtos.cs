using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public abstract record BasePurchaseOrderItemAppDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required decimal QuantityOrdered { get; init; }
    public decimal UnitCost { get; set; }
    public string? Note { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (QuantityOrdered <= 0)
            return (false, "Error.PurchaseOrderQuantityMustBePositive");
        if (UnitCost < 0)
            return (false, "Error.PurchaseOrderItemUnitCostCannotBeNegative");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record PurchaseOrderItemAppDto(Guid Id) : BasePurchaseOrderItemAppDto
{
    public decimal QuantityReceived { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal TotalCost { get; set; }
}

[Serializable]
public sealed record CreatePurchaseOrderItemAppDto
{
    public required Guid? ProductId { get; init; }
    public required decimal Quantity { get; init; }
    public decimal UnitCost { get; set; }
    public string? Note { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (Quantity <= 0)
            return (false, "Error.QuantityMustBePositive");

        if (UnitCost < 0)
            return (false, "Error.");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record AddPurchaseOrderItemAppDto() : BasePurchaseOrderItemAppDto;

[Serializable]
public sealed record ReceivedGoodsForItemAppDto(Guid PurchaseOrderId, Guid PurchaseOrderItemId)
{
    public required decimal ReceivedQuantity { get; init; }
    public required Guid? WarehouseId { get; init; }
    public Guid? ReceivedByUserId { get; set; }

    public decimal? SellingPrice { get; set; }
    public decimal? ActualUnitCost { get; set; }
    public string? OversupplyAction { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (ReceivedQuantity <= 0)
            return (false, "Error.PurchaseOrderQuantityMustBePositive");
        if (SellingPrice.HasValue && SellingPrice.Value < 0)
            return (false, "Error.ProductUnitPriceCannotBeNegative");
        if (ActualUnitCost.HasValue && ActualUnitCost.Value < 0)
            return (false, "Error.PurchaseOrderItemUnitCostCannotBeNegative");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record DeletePurchaseOrderItemAppDto(Guid PurchaseOrderId, Guid ItemId);

[Serializable]
public sealed record BulkReceiveItemAppDto
{
    public required Guid ItemId { get; init; }
    public required decimal Quantity { get; set; }
    public required Guid? WarehouseId { get; set; }
    public decimal? ActualUnitCost { get; set; }
}

[Serializable]
public sealed record BulkReceiveGoodsAppDto(Guid PurchaseOrderId)
{
    public IList<BulkReceiveItemAppDto> Items { get; init; } = [];
    public decimal AdditionalShipping { get; set; }
    public decimal AdditionalTax { get; set; }
    public Guid? ReceivedByUserId { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (Items.Count == 0)
            return (false, "Error.BulkReceive.NoItemsProvided");

        foreach (var item in Items)
        {
            if (item.Quantity <= 0)
                return (false, "Error.PurchaseOrderQuantityMustBePositive");
        }

        if (AdditionalShipping < 0)
            return (false, "Error.ShippingAmountCannotBeNegative");
        if (AdditionalTax < 0)
            return (false, "Error.TaxAmountCannotBeNegative");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record EligibleOrderItemForAllocationAppDto
{
    public required Guid OrderItemId { get; init; }
    public required Guid OrderId { get; init; }
    public required string OrderCode { get; init; }
    public required string CustomerName { get; init; }
    public required string ProductName { get; init; }
    public required decimal TotalQuantity { get; init; }
    public required decimal AllocatedOutstanding { get; init; }
    public required decimal AvailableToAllocate { get; init; }
    public string? ShippingAddress { get; init; }
    public string? CustomerPhone { get; init; }
}

[Serializable]
public sealed record PurchaseOrderItemAllocationForPoItemAppDto
{
    public required Guid AllocationId { get; init; }
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required string OrderCode { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public required int Status { get; init; }
    public required bool IsDirectShip { get; init; }
}

[Serializable]
public sealed record AllocatePoItemToOrderAppDto
{
    public required Guid PurchaseOrderId { get; set; }
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid OrderId { get; set; }
    public required Guid OrderItemId { get; init; }
    public required decimal Quantity { get; init; }
    public string? DirectShipAddress { get; init; }
    public string? DirectShipContactName { get; init; }
    public string? DirectShipContactPhone { get; init; }
}

[Serializable]
public sealed record ReceiveItemResultAppDto : CommonActionResultDto
{
    public decimal ActualReceivedQuantity { get; init; }
    public Guid? CreatedGoodsReceiptId { get; init; }

    public static ReceiveItemResultAppDto CreateSuccess(decimal actualQty, Guid? createdGoodsReceiptId)
        => new() { Success = true, ActualReceivedQuantity = actualQty, CreatedGoodsReceiptId = createdGoodsReceiptId };

    public static new ReceiveItemResultAppDto CreateError(string? errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}

[Serializable]
public sealed record BulkReceiveGoodsResultAppDto : CommonActionResultDto
{
    public IReadOnlyList<Guid> CreatedGoodsReceiptIds { get; init; } = [];

    public static BulkReceiveGoodsResultAppDto CreateSuccess(IReadOnlyList<Guid> createdIds)
        => new() { Success = true, CreatedGoodsReceiptIds = createdIds };

    public static new BulkReceiveGoodsResultAppDto CreateError(string? errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}
