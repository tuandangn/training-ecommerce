namespace NamEcommerce.Application.Contracts.Dtos.Returns;

[Serializable]
public sealed record CustomerReturnAppDto(Guid Id)
{
    public required string Code { get; init; }

    public Guid? DeliveryNoteId { get; init; }
    public string? DeliveryNoteCode { get; init; }

    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public string? Note { get; init; }
    public required int Status { get; init; }
    public required DateTime ReturnDate { get; init; }
    public DateTime? ConfirmedOnUtc { get; init; }

    public required decimal AdditionalCost { get; init; }

    public Guid? GeneratedGoodsReceiptId { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public required IEnumerable<CustomerReturnItemAppDto> Items { get; init; }

    /// <summary>Số tiền hoàn khách = Σ(AcceptedQty × ReturnUnitPrice) − AdditionalCost (floor 0).</summary>
    public decimal NetRefundAmount => Math.Max(0, Items.Sum(i => i.AcceptedTotal) - AdditionalCost);
}

[Serializable]
public sealed record CustomerReturnItemAppDto(Guid Id)
{
    public required Guid CustomerReturnId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public Guid? DeliveryNoteItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }

    /// <summary>Giá bán gốc (tham chiếu) — null nếu tạo tự do.</summary>
    public decimal? OriginalUnitPrice { get; init; }

    /// <summary>Giá hoàn trả thực tế — có thể thấp hơn giá bán gốc (hàng hư).</summary>
    public required decimal ReturnUnitPrice { get; init; }

    public decimal AcceptedTotal => AcceptedQuantity * ReturnUnitPrice;
}

[Serializable]
public sealed record CreateCustomerReturnAppDto
{
    /// <summary>Chọn từ phiếu xuất kho — null = tạo tự do (cần cung cấp CustomerId).</summary>
    public Guid? DeliveryNoteId { get; init; }

    /// <summary>Bắt buộc khi DeliveryNoteId = null (tạo tự do).</summary>
    public Guid? CustomerId { get; init; }

    public required Guid WarehouseId { get; init; }
    public string? Note { get; init; }

    /// <summary>Chi phí phát sinh (vận chuyển, bồi thường...) — giảm vào khoản hoàn khách.</summary>
    public decimal AdditionalCost { get; init; } = 0;

    public required IEnumerable<CreateCustomerReturnItemAppDto> Items { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (DeliveryNoteId == null && (CustomerId == null || CustomerId == Guid.Empty))
            return (false, "Error.CustomerReturn.DeliveryNoteOrCustomerRequired");
        if (WarehouseId == Guid.Empty)
            return (false, "Error.CustomerReturn.WarehouseRequired");
        if (AdditionalCost < 0)
            return (false, "Error.CustomerReturn.AdditionalCostCannotBeNegative");
        if (!Items.Any())
            return (false, "Error.CustomerReturn.NoItems");
        return (true, null);
    }
}

[Serializable]
public sealed record CreateCustomerReturnItemAppDto
{
    public required Guid ProductId { get; init; }
    public Guid? DeliveryNoteItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }

    /// <summary>Giá bán gốc (tham chiếu) — null nếu không lấy từ phiếu xuất.</summary>
    public decimal? OriginalUnitPrice { get; init; }

    /// <summary>Giá hoàn trả thực tế.</summary>
    public required decimal ReturnUnitPrice { get; init; }
}

[Serializable]
public sealed record UpdateCustomerReturnAppDto(Guid Id)
{
    public string? Note { get; init; }
    public DateTime? ReturnDate { get; init; }
}

[Serializable]
public sealed record CreateCustomerReturnResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateCustomerReturnResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record ConfirmCustomerReturnResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
