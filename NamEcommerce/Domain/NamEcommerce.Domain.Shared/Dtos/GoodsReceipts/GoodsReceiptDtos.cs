using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

namespace NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;

/// <summary>
/// DTO nội bộ — tự động tạo GoodsReceipt khi nhận hàng từ PurchaseOrder.
/// Không đi qua flow thông thường (không cần ảnh chứng từ, không cần người dùng nhập thủ công).
/// Mỗi lần gọi tương ứng với 1 item nhận hàng của 1 PO.
/// </summary>
[Serializable]
public sealed record CreateGoodsReceiptFromPurchaseOrderDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }

    /// <summary>VendorId từ PO — nullable vì PO có thể chưa có vendor (hiếm).</summary>
    public Guid? VendorId { get; init; }

    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }

    /// <summary>UnitCost từ PO item — nullable nếu PO item chưa có giá.</summary>
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
    }
}

/// <summary>
/// DTO nội bộ — tạo 1 GoodsReceipt cho nhiều items cùng nhập vào 1 kho từ 1 PurchaseOrder.
/// Caller (PurchaseOrderManager.BulkReceiveItemsAsync) đã group lines theo WarehouseId trước khi gọi.
/// </summary>
[Serializable]
public sealed record CreateGoodsReceiptFromPurchaseOrderBulkDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }

    /// <summary>VendorId từ PO — nullable vì PO có thể chưa có vendor (hiếm).</summary>
    public Guid? VendorId { get; init; }

    /// <summary>Tất cả items trong DTO này cùng nhập vào kho này.</summary>
    public required Guid WarehouseId { get; init; }

    /// <summary>BatchId chia sẻ giữa các GoodsReceipt được tạo cùng một lần bulk-receive (nhiều kho).</summary>
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

    /// <summary>UnitCost từ PO item — nullable nếu PO item chưa có giá.</summary>
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
    public required DateTime ReceivedOnUtc { get; init; }

    public string? TruckDriverName { get; set; }
    public string? TruckNumberSerial { get; set; }

    public required IEnumerable<Guid> PictureIds { get; init; }

    public string? Note { get; set; }

    /// <summary>Nhà cung cấp — nullable.</summary>
    public Guid? VendorId { get; set; }

    public virtual void Verify()
    {
        if (PictureIds is null || !PictureIds.Any())
            throw new GoodsReceiptProofPictureRequired();

        if (ReceivedOnUtc > DateTime.UtcNow)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.ReceivedDateGreaterThanNow");
    }
}

[Serializable]
public sealed record GoodsReceiptDto(Guid Id) : BaseGoodsReceiptDto
{
    public string Code { get; init; } = string.Empty;
    public required IEnumerable<GoodsReceiptItemDto> Items { get; init; }
    public bool IsPendingCosting { get; init; }

    // Vendor snapshot — chỉ có trong DTO đọc, không phải DTO ghi
    public string? VendorName { get; init; }
    public string? VendorPhone { get; init; }
    public string? VendorAddress { get; init; }

    // PurchaseOrder linkage
    public Guid? PurchaseOrderId { get; init; }
    public string? PurchaseOrderCode { get; init; }

    /// <summary>BatchId của lần bulk-receive (nếu phiếu sinh từ bulk-receive nhiều kho).</summary>
    public Guid? BulkReceiveBatchId { get; init; }
}

[Serializable]
public sealed record CreateGoodsReceiptDto : BaseGoodsReceiptDto
{
    public required IList<AddGoodsReceiptItemDto> Items { get; init; }

    public override void Verify()
    {
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
public sealed record UpdateGoodsReceiptDto(Guid Id) : BaseGoodsReceiptDto;

[Serializable]
public sealed record UpdateGoodsReceiptResultDto
{
    public required Guid UpdatedId { get; init; }
}

[Serializable]
public sealed record DeleteGoodsReceiptDto(Guid GoodsReceiptId) : BaseGoodsReceiptDto;

[Serializable]
public sealed record SetGoodsReceiptVendorDto(Guid GoodsReceiptId)
{
    /// <summary>null = xoá vendor khỏi phiếu.</summary>
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

/// <summary>
/// DTO để tạo GoodsReceipt tự động khi CustomerReturn được Confirm.
/// UnitCost = ReturnUnitPrice của item (giá trả về thực tế); nếu = 0 thì fallback sang AverageCost.
/// </summary>
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

    /// <summary>
    /// Giá trả về từ CustomerReturnItem.ReturnUnitPrice — dùng làm UnitCost khi nhập kho.
    /// Nếu = 0, GoodsReceiptManager sẽ fallback sang AverageCost.
    /// </summary>
    public decimal ReturnUnitPrice { get; init; } = 0;
}
