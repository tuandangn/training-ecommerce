using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

namespace NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;

[Serializable]
public sealed record DeliveryNoteDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? CustomerAddress { get; init; }
    
    public required string ShippingAddress { get; init; }
    
    public bool ShowPrice { get; init; }
    public string? Note { get; init; }
    
    public DeliveryNoteStatus Status { get; init; }
    
    public DateTime? DeliveredOnUtc { get; init; }
    public Guid? DeliveryProofPictureId { get; init; }
    public string? DeliveryReceiverName { get; init; }
    
    public Guid? CreatedByUserId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }

    public decimal TotalAmount { get; init; }

    public IList<DeliveryNoteItemDto> Items { get; init; } = [];
}

[Serializable]
public sealed record DeliveryNoteItemDto
{
    public required Guid Id { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal SubTotal { get; init; }
}

[Serializable]
public sealed record CreateDeliveryNoteDto
{
    public required Guid OrderId { get; init; }
    public required string ShippingAddress { get; init; }
    public bool ShowPrice { get; init; }
    public string? Note { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public required IList<CreateDeliveryNoteItemDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CreateDeliveryNoteItemDto
{
    public required Guid OrderItemId { get; init; }
    public required decimal Quantity { get; init; }
}

[Serializable]
public sealed record MarkDeliveryNoteDeliveredDto
{
    public required Guid DeliveryNoteId { get; init; }
    public required Guid PictureId { get; init; }
    public string? ReceiverName { get; init; }
}
