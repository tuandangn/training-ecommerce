using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;

[Serializable]
public sealed record CreateGoodsReceiptFromPurchaseOrderDto
{
    public DateTime? ReceivedOnUtc { get; set; }
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }

    public Guid? VendorId { get; init; }

    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }

    public decimal? UnitCost { get; init; }

    public void Verify()
    {
        if (PurchaseOrderId == Guid.Empty)
            throw new ArgumentException("PurchaseOrderId is required", nameof(PurchaseOrderId));
        if (string.IsNullOrEmpty(PurchaseOrderCode))
            throw new ArgumentException("PurchaseOrderCode is required", nameof(PurchaseOrderCode));
        if (ProductId == Guid.Empty)
            throw new ArgumentException("ProductId is required", nameof(ProductId));
        if (WarehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId is required", nameof(WarehouseId));
        if (Quantity <= 0)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.Item.QuantityMustBePositive");
        if (ReceivedOnUtc.HasValue && ReceivedOnUtc > DateTime.UtcNow)
            throw new PurchaseOrderItemDataIsInvalidException("Error.ReceivedDateCannotBeInFuture");

    }
}

[Serializable]
public sealed record CreateGoodsReceiptFromVendorOversupplyDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }
    public required Guid VendorId { get; init; }
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitCost { get; init; }

    public void Verify()
    {
        if (PurchaseOrderId == Guid.Empty)
            throw new ArgumentException("PurchaseOrderId is required", nameof(PurchaseOrderId));
        if (string.IsNullOrEmpty(PurchaseOrderCode))
            throw new ArgumentException("PurchaseOrderCode is required", nameof(PurchaseOrderCode));
        if (VendorId == Guid.Empty)
            throw new ArgumentException("VendorId is required", nameof(VendorId));
        if (ProductId == Guid.Empty)
            throw new ArgumentException("ProductId is required", nameof(ProductId));
        if (WarehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId is required", nameof(WarehouseId));
        if (Quantity <= 0)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.Item.QuantityMustBePositive");
        if (UnitCost < 0)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.Item.UnitCostCannotBeNegative");
    }
}

[Serializable]
public sealed record CreateGoodsReceiptFromPurchaseOrderBulkDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }

    public Guid? VendorId { get; init; }

    public required Guid WarehouseId { get; init; }

    public Guid? BulkReceiveBatchId { get; init; }

    public required IList<CreateGoodsReceiptFromPurchaseOrderBulkItemDto> Items { get; init; }

    public void Verify()
    {
        if (PurchaseOrderId == Guid.Empty)
            throw new ArgumentException("PurchaseOrderId is required", nameof(PurchaseOrderId));
        if (string.IsNullOrEmpty(PurchaseOrderCode))
            throw new ArgumentException("PurchaseOrderCode is required", nameof(PurchaseOrderCode));
        if (WarehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId is required", nameof(WarehouseId));
        if (Items is null || Items.Count == 0)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.Item.QuantityMustBePositive");
        foreach (var item in Items)
            item.Verify();
    }
}

[Serializable]
public sealed record CreateGoodsReceiptFromPurchaseOrderBulkItemDto
{
    public required Guid ProductId { get; init; }
    public required decimal Quantity { get; init; }

    public decimal? UnitCost { get; init; }

    public void Verify()
    {
        if (ProductId == Guid.Empty)
            throw new ArgumentException("ProductId is required", nameof(ProductId));
        if (Quantity <= 0)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.Item.QuantityMustBePositive");
    }
}

[Serializable]
public abstract record BaseGoodsReceiptDto
{
    public string? TruckDriverName { get; set; }
    public string? TruckNumberSerial { get; set; }

    public required IEnumerable<Guid> PictureIds { get; init; }

    public string? Note { get; set; }

    public Guid? VendorId { get; set; }

    public virtual void Verify()
    {
    }
}

[Serializable]
public sealed record GoodsReceiptDto(Guid Id) : BaseGoodsReceiptDto
{
    public DateTime ReceivedOnUtc { get; set; }
    public string Code { get; init; } = string.Empty;
    public required IEnumerable<GoodsReceiptItemDto> Items { get; init; }
    public bool IsPendingCosting { get; init; }

    public string? VendorName { get; init; }
    public string? VendorPhone { get; init; }
    public string? VendorAddress { get; init; }

    public Guid? PurchaseOrderId { get; init; }
    public string? PurchaseOrderCode { get; init; }

    public Guid? BulkReceiveBatchId { get; init; }
}

[Serializable]
public sealed record CreateGoodsReceiptDto : BaseGoodsReceiptDto
{
    public DateTime? ReceivedOnUtc { get; set; }
    public required IList<AddGoodsReceiptItemDto> Items { get; init; }

    public Guid? PurchaseOrderId { get; set; }

    public override void Verify()
    {
        if (ReceivedOnUtc.HasValue && ReceivedOnUtc > DateTime.UtcNow)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.ReceivedDateGreaterThanNow");

        foreach (var item in Items)
            item.Verify();

        base.Verify();
    }
}

[Serializable]
public sealed record CreateGoodsReceiptResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateGoodsReceiptDto(Guid Id) : BaseGoodsReceiptDto
{
    public DateTime ReceivedOnUtc { get; set; }

    public override void Verify()
    {
        if (ReceivedOnUtc > DateTime.UtcNow)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.ReceivedDateGreaterThanNow");

        base.Verify();
    }
}

[Serializable]
public sealed record UpdateGoodsReceiptResultDto
{
    public required Guid UpdatedId { get; init; }
}

[Serializable]
public sealed record DeleteGoodsReceiptDto(Guid GoodsReceiptId);

[Serializable]
public sealed record SetGoodsReceiptVendorDto(Guid GoodsReceiptId)
{
    public Guid? VendorId { get; init; }

    public void Verify()
    {
        if (GoodsReceiptId == Guid.Empty)
            throw new GoodsReceiptIsNotFoundException(GoodsReceiptId);
    }
}

[Serializable]
public sealed record SetGoodsReceiptVendorResultDto
{
    public required Guid UpdatedId { get; init; }
}

[Serializable]
public sealed record CreateOpeningInventoryReceiptDto
{
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitCost { get; init; }
    public Guid? CreatedByUserId { get; init; }

    public void Verify()
    {
        if (ProductId == Guid.Empty)
            throw new ArgumentException("ProductId is required", nameof(ProductId));
        if (WarehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId is required", nameof(WarehouseId));
        if (Quantity <= 0)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.Item.QuantityMustBePositive");
        if (UnitCost <= 0)
            throw new GoodsReceiptItemDataIsInvalidException("Error.OpeningInventory.UnitCostMustBePositive");
    }
}

[Serializable]
public sealed record CreateGoodsReceiptFromCustomerReturnDto
{
    public required Guid CustomerReturnId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required IEnumerable<CreateGoodsReceiptFromCustomerReturnItemDto> Items { get; init; }
}

[Serializable]
public sealed record CreateGoodsReceiptFromCustomerReturnItemDto
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
}
