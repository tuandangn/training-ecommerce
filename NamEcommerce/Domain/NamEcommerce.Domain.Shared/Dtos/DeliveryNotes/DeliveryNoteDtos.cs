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
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal SubTotal { get; init; }
}

[Serializable]
public sealed record CreateDeliveryNoteDto
{
    public required Guid OrderId { get; init; }
    public required Guid WarehouseId { get; init; }
    public string? WarehouseName { get; init; }
    public required string ShippingAddress { get; init; }
    public bool ShowPrice { get; init; }
    public string? Note { get; init; }
    public decimal Surcharge { get; init; }
    public string? SurchargeReason { get; init; }
    public decimal AmountToCollect { get; init; }
    public Guid? CreatedByUserId { get; init; }
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
    public required decimal Quantity { get; init; }
}

[Serializable]
public sealed record MarkDeliveryNoteDeliveredDto
{
    public required Guid DeliveryNoteId { get; init; }
    public required Guid PictureId { get; init; }
    public string? ReceiverName { get; init; }
}

[Serializable]
public sealed record DeliveryNoteLinkDto(Guid Id, string Code, DeliveryNoteStatus Status, DateTime CreatedOnUtc);
