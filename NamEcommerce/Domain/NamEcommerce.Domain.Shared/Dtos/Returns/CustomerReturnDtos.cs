using NamEcommerce.Domain.Shared.Exceptions.Returns;

namespace NamEcommerce.Domain.Shared.Dtos.Returns;

[Serializable]
public sealed record CustomerReturnDto(Guid Id)
{
    public required string Code { get; init; }

    public required Guid DeliveryNoteId { get; init; }
    public required string DeliveryNoteCode { get; init; }

    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public string? Note { get; init; }
    public required int Status { get; init; }
    public required DateTime ReturnDate { get; init; }
    public DateTime? ConfirmedOnUtc { get; init; }
    public Guid? GeneratedGoodsReceiptId { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }

    public required decimal AdditionalCost { get; init; }

    public required IEnumerable<CustomerReturnItemDto> Items { get; init; }

    /// <summary>Số tiền hoàn thực tế = Σ(AcceptedQty × ReturnUnitPrice) − AdditionalCost (floor 0).</summary>
    public decimal NetRefundAmount => Math.Max(0, Items.Sum(i => i.AcceptedTotal) - AdditionalCost);
}

[Serializable]
public sealed record CustomerReturnItemDto(Guid Id)
{
    public required Guid CustomerReturnId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public Guid? DeliveryNoteItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }

    /// <summary>Giá bán gốc (tham chiếu từ phiếu xuất) — null nếu tạo tự do.</summary>
    public decimal? OriginalUnitPrice { get; init; }

    /// <summary>Giá trả về thực tế (có thể thấp hơn giá gốc do khấu hao, hư hỏng...).</summary>
    public required decimal ReturnUnitPrice { get; init; }

    public decimal AcceptedTotal => AcceptedQuantity * ReturnUnitPrice;
}

[Serializable]
public sealed record CreateCustomerReturnDto
{
    public Guid? DeliveryNoteId { get; init; }
    public required Guid CustomerId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? Note { get; init; }
    public decimal AdditionalCost { get; init; } = 0;
    public Guid? ExcludeCustomerReturnRequestId { get; init; }
    public required IEnumerable<CreateCustomerReturnItemDto> Items { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.CustomerRequired");
        if (DeliveryNoteId == Guid.Empty)
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.DeliveryNoteRequired");
        if (WarehouseId == Guid.Empty)
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.WarehouseRequired");
        if (AdditionalCost < 0)
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.AdditionalCostCannotBeNegative");
        if (Items is null || !Items.Any())
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.NoItems");
        foreach (var item in Items)
        {
            if (item.ProductId == Guid.Empty)
                throw new ReturnDataIsInvalidException("Error.CustomerReturn.ProductRequired");
            if (item.RequestedQuantity <= 0)
                throw new ReturnDataIsInvalidException("Error.CustomerReturn.RequestedQuantityMustBePositive");
            if (item.AcceptedQuantity < 0)
                throw new ReturnDataIsInvalidException("Error.CustomerReturn.AcceptedQuantityCannotBeNegative");
            if (item.AcceptedQuantity > item.RequestedQuantity)
                throw new ReturnDataIsInvalidException("Error.CustomerReturn.AcceptedQuantityExceedsRequested");
            if (item.ReturnUnitPrice < 0)
                throw new ReturnDataIsInvalidException("Error.CustomerReturn.ReturnUnitPriceCannotBeNegative");
        }
    }
}

[Serializable]
public sealed record CreateCustomerReturnItemDto
{
    public required Guid ProductId { get; init; }
    public Guid? DeliveryNoteItemId { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public decimal? OriginalUnitPrice { get; init; }
    public required decimal ReturnUnitPrice { get; init; }
}

[Serializable]
public sealed record UpdateCustomerReturnDto(Guid Id)
{
    public string? Note { get; init; }
    public DateTime? ReturnDate { get; init; }
    public IEnumerable<CreateCustomerReturnItemDto> Items { get; init; } = [];
}
