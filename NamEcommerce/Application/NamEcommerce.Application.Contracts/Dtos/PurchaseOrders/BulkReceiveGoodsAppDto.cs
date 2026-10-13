using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public sealed record BulkReceiveGoodsAppDto(Guid PurchaseOrderId)
{
    public DateTime? ReceivedOnUtc { get; set; }
    public IList<BulkReceiveItemAppDto> Items { get; init; } = [];
    public decimal ShippingAmount { get; set; }
    public decimal? TaxRate { get; set; }
    public Guid? ReceivedByUserId { get; set; }
    public IList<Guid> PictureIds { get; init; } = [];

    public (bool valid, string? errorMessage) Validate()
    {
        if (ShippingAmount < 0)
            return (false, "Error.ShippingAmountCannotBeNegative");
        if (TaxRate.HasValue && TaxRate < 0)
            return (false, "Error.ExpenseTaxRateInvalid");

        if (Items.Count == 0)
            return (false, "Error.BulkReceive.NoItemsProvided");

        foreach (var item in Items)
        {
            var validateResult = item.Validate();
            if (!validateResult.valid)
                return validateResult;
        }

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record BulkReceiveItemAppDto
{
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid? WarehouseId { get; set; }
    public required decimal ReceivedQuantity { get; set; }
    public decimal? ActualUnitCost { get; set; }

    public Guid? DirectShipOrderId { get; init; }
    public Guid? DirectShipOrderItemId { get; init; }
    public string? DirectShipAddress { get; init; }
    public string? DirectShipContactName { get; init; }
    public string? DirectShipContactPhone { get; init; }
    public Guid? DirectShipExistingAllocationId { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (PurchaseOrderItemId == Guid.Empty)
            return (false, "Error.PurchaseOrderItemIsNotFound");
        if (WarehouseId == Guid.Empty)
            return (false, "Error.WarehouseRequired");
        if (ReceivedQuantity <= 0)
            return (false, "Error.PurchaseOrderReceiveQuantityMustBePositive");
        if (ActualUnitCost.HasValue && ActualUnitCost.Value < 0)
            return (false, "Error.PurchaseOrderItemUnitCostCannotBeNegative");

        return (true, string.Empty);
    }
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