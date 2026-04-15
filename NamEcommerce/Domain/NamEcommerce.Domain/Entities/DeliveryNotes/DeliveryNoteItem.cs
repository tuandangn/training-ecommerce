using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.DeliveryNotes;

public sealed record DeliveryNoteItem : AppEntity
{
    public Guid DeliveryNoteId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    
    public decimal SubTotal => Quantity * UnitPrice;

    public DeliveryNoteItem(Guid id) : base(id)
    {
        ProductName = string.Empty;
    }

    internal DeliveryNoteItem(Guid deliveryNoteId, Guid orderItemId, Guid productId, string productName, decimal quantity, decimal unitPrice) : base(Guid.NewGuid())
    {
        DeliveryNoteId = deliveryNoteId;
        OrderItemId = orderItemId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
