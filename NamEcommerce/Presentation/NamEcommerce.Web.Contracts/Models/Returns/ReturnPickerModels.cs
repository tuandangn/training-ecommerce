namespace NamEcommerce.Web.Contracts.Models.Returns;

/// <summary>Phiếu xuất kho để chọn khi tạo phiếu Khách trả hàng.</summary>
[Serializable]
public sealed class DeliveryNotePickerModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required DateTime DeliveredOn { get; init; }
}

/// <summary>Phiếu nhập kho để chọn khi tạo phiếu Trả hàng NCC.</summary>
[Serializable]
public sealed class GoodsReceiptPickerModel
{
    public required Guid Id { get; init; }

    /// <summary>Nhãn hiển thị = mã PO (nếu có) hoặc "Nhập {ReceivedOn:dd/MM/yyyy}".</summary>
    public required string Label { get; init; }

    public required DateTime ReceivedOn { get; init; }
    public string? PurchaseOrderCode { get; init; }
}

/// <summary>Dòng hàng có thể trả — dùng cho cả CustomerReturn và VendorReturn.</summary>
[Serializable]
public sealed class ReturnableItemModel
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required string Unit { get; init; }

    /// <summary>Số lượng đã giao/nhận từ phiếu nguồn.</summary>
    public required decimal OriginalQty { get; init; }

    /// <summary>Số lượng đã trả trong các phiếu Confirmed khác.</summary>
    public required decimal AlreadyReturnedQty { get; init; }

    /// <summary>Số lượng còn được phép trả = OriginalQty - AlreadyReturnedQty.</summary>
    public decimal AvailableQty => Math.Max(0, OriginalQty - AlreadyReturnedQty);

    /// <summary>Đơn giá tham chiếu (bán giá hoặc giá vốn tùy context).</summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>ID item trong phiếu nguồn — null nếu không tham chiếu.</summary>
    public Guid? SourceItemId { get; init; }
}
