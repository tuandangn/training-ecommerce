using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Exceptions.Orders;

namespace NamEcommerce.Domain.Entities.Orders;

[Serializable]
public sealed record OrderItem : AppEntity
{
    internal OrderItem(Guid orderId, Guid productId, decimal unitPrice, decimal quantity) : base(Guid.Empty)
    {
        (OrderId, ProductId) = (orderId, productId);

        Update(quantity, unitPrice);
    }

    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    internal string? ProductName { get; set; }

    public decimal UnitPrice { get; internal set; }
    public decimal CostPrice { get; internal set; }
    public decimal Discount { get; internal set; }
    public decimal Quantity { get; internal set; }

    public bool IsDelivered { get; private set; }
    public DateTime? DeliveredOnUtc { get; private set; }
    public Guid? DeliveryProofPictureId { get; private set; }

    public decimal SubTotal => (UnitPrice - Discount) * Quantity;

    internal void Update(decimal quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new OrderItemDataIsInvalidException("Quantity must be greather than 0.");

        if (unitPrice < 0)
            throw new OrderItemDataIsInvalidException("Unit price must be greather than or equal to 0.");

        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    internal void MarkDelivered(Guid pictureId)
    {
        if (IsDelivered)
            throw new OrderItemDataIsInvalidException("Order item is already delivered.");
            
        if (pictureId == Guid.Empty)
            throw new OrderItemDataIsInvalidException("Delivery proof picture is required.");

        IsDelivered = true;
        DeliveredOnUtc = DateTime.UtcNow;
        DeliveryProofPictureId = pictureId;
    }
}
