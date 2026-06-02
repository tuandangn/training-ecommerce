using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;

[Serializable]
public sealed record DeliveryNoteDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    
    public required Guid OrderId { get; init; }
    public required string? OrderCode { get; set; }
    public required Guid WarehouseId { get; init; }

    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? CustomerAddress { get; init; }
    
    public required string ShippingAddress { get; init; }
    
    public bool ShowPrice { get; init; }
    public string? Note { get; init; }
    
    public DeliveryNoteStatus Status { get; init; }
    public DeliveryNoteSourceType SourceType { get; init; }
    public bool IsDirectShip { get; init; }
    public DeliveryConfirmationStatus DeliveryConfirmationStatus { get; init; }
    public DateTime? ConfirmedAtUtc { get; init; }
    public string? ConfirmedNote { get; init; }

    public DateTime? DeliveredOnUtc { get; init; }
    public Guid? DeliveryProofPictureId { get; init; }
    public string? DeliveryReceiverName { get; init; }
    
    public Guid? CreatedByUserId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }

    public decimal TotalAmount { get; init; }
    public decimal Surcharge { get; init; }
    public string? SurchargeReason { get; init; }
    public decimal AmountToCollect { get; init; }

    public IList<DeliveryNoteItemDto> Items { get; init; } = [];
}

[Serializable]
public sealed record DeliveryNoteItemDto
{
    public required Guid Id { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal SubTotal { get; init; }

    /// <summary>
    /// Snapshot hiển thị chuyển tiếp; COGS authoritative nằm trong InventoryCostAllocation.
    /// </summary>
    public decimal? CostAtDispatch { get; init; }
}

[Serializable]
public sealed record CreateDeliveryNoteDto
{
    public required Guid OrderId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public required string ShippingAddress { get; init; }
    public bool ShowPrice { get; init; }
    public bool CompensateReturnedQuantityInNextDelivery { get; init; }
    public string? Note { get; init; }
    public decimal Surcharge { get; init; }
    public string? SurchargeReason { get; init; }
    public decimal AmountToCollect { get; init; }
    public required IList<CreateDeliveryNoteItemDto> Items { get; init; } = [];

    public void Verify()
    {
        if (OrderId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.OrderRequired");
        if (WarehouseId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.WarehouseRequired");
        if (string.IsNullOrEmpty(ShippingAddress))
            throw new NamEcommerceDomainException("Error.ShippingAddressRequired");
        if (Items == null || !Items.Any())
            throw new NamEcommerceDomainException("Error.DeliveryNoteItemsRequired");
        if (Items.Any(i => i.WarehouseId == Guid.Empty))
            throw new NamEcommerceDomainException("Error.WarehouseRequired");
        if (Items.Any(i => i.Quantity <= 0))
            throw new NamEcommerceDomainException("Error.QuantityMustBePositive");
        if (Surcharge < 0)
            throw new NamEcommerceDomainException("Error.SurchargeCannotBeNegative");
        if (AmountToCollect < 0)
            throw new NamEcommerceDomainException("Error.AmountToCollectCannotBeNegative");
    }
}

[Serializable]
public sealed record CreateDeliveryNoteItemDto
{
    public required Guid OrderItemId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }
}

[Serializable]
public sealed record DeliveryAcceptanceItemDto
{
    public required Guid DeliveryNoteItemId { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public required decimal RejectedQuantity { get; init; }
    public string? RejectReason { get; init; }
}

[Serializable]
public sealed record DeliveryAcceptanceDto
{
    public decimal AgreedCustomerCharge { get; init; }
    public string? AgreedCustomerChargeReason { get; init; }
    public bool CompensateInNextDelivery { get; init; }
    public IList<DeliveryAcceptanceItemDto> Items { get; init; } = [];
}

[Serializable]
public sealed record MarkDeliveryNoteDeliveredDto
{
    public required Guid DeliveryNoteId { get; init; }
    public required IReadOnlyList<Guid> PictureIds { get; init; }
    public string? ReceiverName { get; init; }
    public DeliveryAcceptanceDto? Acceptance { get; init; }
}

[Serializable]
public sealed record DeliveryNoteLinkDto(Guid Id, string Code, DeliveryNoteStatus Status, DateTime CreatedOnUtc);

/// <summary>
/// DTO để tạo DeliveryNote tự động (Status=Delivered ngay) khi VendorReturn được Confirm.
/// UnitCost ở đây là số tiền thu hồi NCC trên VendorReturnItem, không phải inventory COGS.
/// </summary>
[Serializable]
public sealed record CreateDeliveryNoteFromVendorReturnDto
{
    public required Guid VendorReturnId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required IEnumerable<CreateDeliveryNoteFromVendorReturnItemDto> Items { get; init; }
}

[Serializable]
public sealed record CreateDeliveryNoteFromVendorReturnItemDto
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitCost { get; init; }
}

/// <summary>
/// DTO để tạo DeliveryNote direct-ship tự động ở trạng thái Confirmed khi GoodsReceipt
/// phân bổ hàng cho một allocation direct-ship. DeliveryNoteManager tự tìm Order/Customer từ OrderItemId.
/// </summary>
[Serializable]
public sealed record CreateDeliveryNoteForDirectShipDto
{
    public required Guid GoodsReceiptId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required decimal Quantity { get; init; }
    public required Guid DirectShipWarehouseId { get; init; }
    public required string ShippingAddress { get; init; }
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
}
