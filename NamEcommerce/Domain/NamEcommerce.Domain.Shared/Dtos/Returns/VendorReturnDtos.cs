using NamEcommerce.Domain.Shared.Exceptions.Returns;

namespace NamEcommerce.Domain.Shared.Dtos.Returns;

[Serializable]
public sealed record VendorReturnDto(Guid Id)
{
    public required string Code { get; init; }
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public Guid? GoodsReceiptId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public string? Note { get; init; }
    public required int Status { get; init; }
    public required DateTime ReturnDate { get; init; }
    public DateTime? ConfirmedOnUtc { get; init; }
    public DateTime? ReversedOnUtc { get; init; }
    public string? ReversedReason { get; init; }
    public Guid? GeneratedDeliveryNoteId { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }

    public required decimal AdditionalCost { get; init; }
    public required IEnumerable<VendorReturnItemDto> Items { get; init; }

    /// <summary>Số tiền thu lại từ NCC = Σ(AcceptedQty × ReturnUnitCost) − AdditionalCost (floor 0).</summary>
    public decimal NetRecoveryAmount => Math.Max(0, Items.Sum(i => i.AcceptedTotal) - AdditionalCost);
}

[Serializable]
public sealed record VendorReturnItemDto(Guid Id)
{
    public required Guid VendorReturnId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public Guid? GoodsReceiptItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }

    /// <summary>Giá vốn gốc tại thời điểm nhập (tham chiếu) — null nếu tạo tự do.</summary>
    public decimal? OriginalUnitCost { get; init; }

    /// <summary>Giá NCC hoàn trả — có thể thấp hơn giá gốc (hàng hư) hoặc cao hơn (thêm chi phí vận chuyển).</summary>
    public required decimal ReturnUnitCost { get; init; }

    public decimal AcceptedTotal => AcceptedQuantity * ReturnUnitCost;
}

[Serializable]
public sealed record CreateVendorReturnDto
{
    public required Guid VendorId { get; init; }
    public Guid? GoodsReceiptId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? Note { get; init; }
    public decimal AdditionalCost { get; init; } = 0;
    public required IEnumerable<CreateVendorReturnItemDto> Items { get; init; }

    public void Verify()
    {
        if (VendorId == Guid.Empty)
            throw new ReturnDataIsInvalidException("Error.VendorReturn.VendorRequired");
        if (WarehouseId == Guid.Empty)
            throw new ReturnDataIsInvalidException("Error.VendorReturn.WarehouseRequired");
        if (AdditionalCost < 0)
            throw new ReturnDataIsInvalidException("Error.VendorReturn.AdditionalCostCannotBeNegative");
        if (!Items.Any())
            throw new ReturnDataIsInvalidException("Error.VendorReturn.NoItems");
        foreach (var item in Items)
        {
            if (item.RequestedQuantity <= 0)
                throw new ReturnDataIsInvalidException("Error.VendorReturn.RequestedQuantityMustBePositive");
            if (item.AcceptedQuantity < 0)
                throw new ReturnDataIsInvalidException("Error.VendorReturn.AcceptedQuantityCannotBeNegative");
            if (item.ReturnUnitCost < 0)
                throw new ReturnDataIsInvalidException("Error.VendorReturn.ReturnUnitCostCannotBeNegative");
        }
    }
}

[Serializable]
public sealed record CreateVendorReturnItemDto
{
    public required Guid ProductId { get; init; }
    public Guid? GoodsReceiptItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }

    /// <summary>Giá vốn gốc (tham chiếu) — null nếu không lấy từ phiếu nhập.</summary>
    public decimal? OriginalUnitCost { get; init; }

    /// <summary>Giá NCC hoàn trả thực tế.</summary>
    public required decimal ReturnUnitCost { get; init; }
}

[Serializable]
public sealed record UpdateVendorReturnDto(Guid Id)
{
    public string? Note { get; init; }
    public DateTime? ReturnDate { get; init; }
    public IEnumerable<CreateVendorReturnItemDto> Items { get; init; } = [];
}
