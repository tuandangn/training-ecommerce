namespace NamEcommerce.Application.Contracts.Dtos.Returns;

/// <summary>Phiếu xuất kho để chọn khi tạo phiếu Khách trả hàng.</summary>
[Serializable]
public sealed record DeliveryNotePickerAppDto(Guid Id)
{
    public required string Code { get; init; }
    public required DateTime DeliveredOnUtc { get; init; }
}

/// <summary>Phiếu nhập kho để chọn khi tạo phiếu Trả hàng NCC.</summary>
[Serializable]
public sealed record GoodsReceiptPickerAppDto(Guid Id)
{
    public required string Label { get; init; }
    public required DateTime ReceivedOnUtc { get; init; }
    public string? PurchaseOrderCode { get; init; }
}

/// <summary>Dòng hàng có thể trả từ phiếu nguồn (DeliveryNote hoặc GoodsReceipt).</summary>
[Serializable]
public sealed record ReturnableItemAppDto
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required string Unit { get; init; }

    /// <summary>Số lượng đã giao/nhập từ phiếu nguồn.</summary>
    public required decimal OriginalQty { get; init; }

    /// <summary>Số lượng đã trả trong các phiếu Confirmed khác.</summary>
    public required decimal AlreadyReturnedQty { get; init; }

    /// <summary>Đơn giá tham chiếu (bán giá hoặc giá vốn tùy context).</summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>ID item trong phiếu nguồn — null nếu không tham chiếu.</summary>
    public Guid? SourceItemId { get; init; }
}
