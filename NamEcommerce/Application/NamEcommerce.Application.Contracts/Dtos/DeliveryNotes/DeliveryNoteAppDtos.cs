

namespace NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;

[Serializable]
public sealed record DeliveryNoteAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    
    public required Guid OrderId { get; init; }
    public required string? OrderCode { get; set; }

    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public Guid? AssignedDeliveryUserId { get; init; }
    public string? AssignedDeliveryUsername { get; init; }
    public string? AssignedDeliveryFullName { get; init; }
    public DateTime? AssignedDeliveryOnUtc { get; init; }

    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? CustomerAddress { get; init; }
    
    public required string ShippingAddress { get; init; }
    
    public bool ShowPrice { get; init; }
    public string? Note { get; init; }
    
    public int Status { get; init; }
    public int SourceType { get; init; }
    public bool IsDirectShip { get; init; }
    public int DeliveryConfirmationStatus { get; init; }
    public DateTime? ConfirmedAtUtc { get; init; }
    public string? ConfirmedNote { get; init; }

    public DateTime? DeliveredOnUtc { get; init; }
    public Guid? DeliveryProofPictureId { get; init; }
    public string? DeliveryReceiverName { get; init; }
    public double? DeliveryLatitude { get; init; }
    public double? DeliveryLongitude { get; init; }
    public string? DeliveryLocationAddress { get; init; }
    public string? DeliveryCompletionNote { get; init; }
    public string? DeliveryCompletionSource { get; init; }
    public string? DeliveryCompletionIdempotencyKey { get; init; }
    
    public Guid? CreatedByUserId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }

    public decimal TotalAmount { get; init; }
    public decimal Surcharge { get; init; }
    public string? SurchargeReason { get; init; }
    public decimal AmountToCollect { get; init; }

    public IList<DeliveryNoteItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record DeliveryNoteItemAppDto
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
    public decimal? CostAtDispatch { get; init; }
}

[Serializable]
public sealed record CreateDeliveryNoteAppDto
{
    public required Guid OrderId { get; init; }
    public required Guid WarehouseId { get; set; }
    public required string ShippingAddress { get; init; }
    public bool ShowPrice { get; init; }
    public bool CompensateReturnedQuantityInNextDelivery { get; init; }
    public string? Note { get; init; }
    public decimal Surcharge { get; init; }
    public string? SurchargeReason { get; init; }
    public decimal AmountToCollect { get; init; }
    public required IList<CreateDeliveryNoteItemAppDto> Items { get; init; } = [];

    public (bool valid, string? errorMessage) Validate()
    {
        if (OrderId == Guid.Empty)
            return (false, "Error.OrderRequired");
        if (WarehouseId == Guid.Empty)
            return (false, "Error.WarehouseRequired");
        if (string.IsNullOrEmpty(ShippingAddress))
            return (false, "Error.ShippingAddressRequired");
        if (Items == null || !Items.Any())
            return (false, "Error.DeliveryNoteItemsRequired");
        if (Items.Any(i => i.WarehouseId == Guid.Empty))
            return (false, "Error.WarehouseRequired");
        if (Items.Any(i => i.Quantity <= 0))
            return (false, "Error.DeliveryNoteQuantityMustBePositive");
        if (Surcharge < 0)
            return (false, "Error.SurchargeCannotBeNegative");
        if (AmountToCollect < 0)
            return (false, "Error.AmountToCollectCannotBeNegative");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record CreateDeliveryNoteItemAppDto
{
    public required Guid OrderItemId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required decimal Quantity { get; init; }
}

[Serializable]
public sealed record DeliveryAcceptanceItemAppDto
{
    public required Guid DeliveryNoteItemId { get; init; }
    public required decimal AcceptedQuantity { get; init; }
    public required decimal RejectedQuantity { get; init; }
    public string? RejectReason { get; init; }
}

[Serializable]
public sealed record DeliveryAcceptanceAppDto
{
    public decimal AgreedCustomerCharge { get; init; }
    public string? AgreedCustomerChargeReason { get; init; }
    public bool CompensateInNextDelivery { get; init; }
    public IList<DeliveryAcceptanceItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record DeliveryCompletionMetadataAppDto
{
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? LocationAddress { get; init; }
    public string? Note { get; init; }
    public string? Source { get; init; }
    public string? IdempotencyKey { get; init; }
}

[Serializable]
public sealed record MarkDeliveryNoteDeliveredAppDto
{
    public required Guid DeliveryNoteId { get; init; }
    public required IReadOnlyList<Guid> PictureIds { get; init; }
    public string? ReceiverName { get; init; }
    public DeliveryAcceptanceAppDto? Acceptance { get; init; }
    public DeliveryCompletionMetadataAppDto? CompletionMetadata { get; init; }
}

[Serializable]
public sealed record MarkDeliveryNoteDeliveredResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record AssignDeliveryUserAppDto
{
    public required Guid DeliveryNoteId { get; init; }
    public required Guid AssignedDeliveryUserId { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (DeliveryNoteId == Guid.Empty)
            return (false, "Error.DeliveryNoteRequired");
        if (AssignedDeliveryUserId == Guid.Empty)
            return (false, "Error.DeliveryUserRequired");

        return (true, null);
    }
}

[Serializable]
public sealed record AssignDeliveryUserResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record CreateDeliveryNoteFromVendorReturnAppDto
{
    public required Guid VendorReturnId { get; init; }
    public required Guid WarehouseId { get; init; }
    public required IEnumerable<CreateDeliveryNoteFromVendorReturnItemAppDto> Items { get; init; }
}

[Serializable]
public sealed record CreateDeliveryNoteFromVendorReturnItemAppDto
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitCost { get; init; }
}
