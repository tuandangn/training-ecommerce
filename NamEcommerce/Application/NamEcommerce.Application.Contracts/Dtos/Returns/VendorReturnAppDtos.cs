namespace NamEcommerce.Application.Contracts.Dtos.Returns;

[Serializable]
public sealed record VendorReturnAppDto(Guid Id)
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

    /// <summary>Chi phí phát sinh khi trả hàng NCC (vận chuyển, đền bù...).</summary>
    public required decimal AdditionalCost { get; init; }

    public Guid? GeneratedDeliveryNoteId { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public required IEnumerable<VendorReturnItemAppDto> Items { get; init; }

    /// <summary>Số tiền thu lại từ NCC = Σ(AcceptedQty × ReturnUnitCost) − AdditionalCost (floor 0).</summary>
    public decimal NetRecoveryAmount => Math.Max(0, Items.Sum(i => i.AcceptedTotal) - AdditionalCost);
}

[Serializable]
public sealed record VendorReturnItemAppDto(Guid Id)
{
    public required Guid VendorReturnId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public Guid? GoodsReceiptItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }

    /// <summary>Giá vốn gốc tại thời điểm nhập (tham chiếu) — null nếu tạo tự do.</summary>
    public decimal? OriginalUnitCost { get; init; }

    /// <summary>Giá NCC hoàn trả thực tế.</summary>
    public required decimal ReturnUnitCost { get; init; }

    public decimal AcceptedTotal => AcceptedQuantity * ReturnUnitCost;
}

[Serializable]
public sealed record CreateVendorReturnAppDto
{
    public required Guid VendorId { get; init; }
    public Guid? GoodsReceiptId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? Note { get; init; }
    public decimal AdditionalCost { get; init; } = 0;
    public required IEnumerable<CreateVendorReturnItemAppDto> Items { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (VendorId == Guid.Empty)
            return (false, "Error.VendorReturn.VendorRequired");
        if (WarehouseId == Guid.Empty)
            return (false, "Error.VendorReturn.WarehouseRequired");
        if (AdditionalCost < 0)
            return (false, "Error.VendorReturn.AdditionalCostCannotBeNegative");
        if (!Items.Any())
            return (false, "Error.VendorReturn.NoItems");
        return (true, null);
    }
}

[Serializable]
public sealed record CreateVendorReturnItemAppDto
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
public sealed record UpdateVendorReturnAppDto(Guid Id)
{
    public string? Note { get; init; }
    public DateTime? ReturnDate { get; init; }
}

[Serializable]
public sealed record CreateVendorReturnResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateVendorReturnResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record ConfirmVendorReturnResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
