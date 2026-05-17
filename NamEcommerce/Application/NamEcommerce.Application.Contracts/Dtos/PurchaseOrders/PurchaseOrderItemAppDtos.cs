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
            return (false, "Error.PurchaseOrderUnitCostCannotBeNegative");

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
public sealed record AddPurchaseOrderItemAppDto() : BasePurchaseOrderItemAppDto;

[Serializable]
public sealed record ReceivedGoodsForItemAppDto(Guid PurchaseOrderId, Guid PurchaseOrderItemId)
{
    public required decimal ReceivedQuantity { get; init; }
    public required Guid? WarehouseId { get; init; }
    public Guid? ReceivedByUserId { get; set; }

    /// <summary>
    /// Giá bán mới cho sản phẩm (tùy chọn). Nếu null thì giữ nguyên UnitPrice hiện tại của Product.
    /// </summary>
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

/// <summary>DTO cho một dòng item trong batch nhận hàng.</summary>
[Serializable]
public sealed record BulkReceiveItemAppDto
{
    public required Guid ItemId { get; init; }
    public required decimal Quantity { get; set; }
    public required Guid? WarehouseId { get; set; }
    public decimal? ActualUnitCost { get; set; }
}

/// <summary>DTO cho thao tác nhận nhiều hàng hóa cùng lúc (bulk receive).</summary>
[Serializable]
public sealed record BulkReceiveGoodsAppDto(Guid PurchaseOrderId)
{
    public IList<BulkReceiveItemAppDto> Items { get; init; } = [];

    /// <summary>Phí vận chuyển cộng thêm vào đơn (tùy chọn).</summary>
    public decimal AdditionalShipping { get; set; }

    /// <summary>Tiền thuế cộng thêm vào đơn, đã được tính từ % rate (tùy chọn).</summary>
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
public sealed record AllocatePoItemToOrderAppDto
{
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required decimal Quantity { get; init; }
    public string? DirectShipAddress { get; init; }
    public string? DirectShipContactName { get; init; }
    public string? DirectShipContactPhone { get; init; }
}

/// <summary>Kết quả bulk receive — kèm danh sách GoodsReceipt IDs đã tạo để UI có thể link/show.</summary>
[Serializable]
public sealed record BulkReceiveGoodsResultAppDto : CommonActionResultDto
{
    public IReadOnlyList<Guid> CreatedGoodsReceiptIds { get; init; } = [];

    public static BulkReceiveGoodsResultAppDto CreateSuccess(IReadOnlyList<Guid> createdIds)
        => new() { Success = true, CreatedGoodsReceiptIds = createdIds };

    public static new BulkReceiveGoodsResultAppDto CreateError(string? errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}
