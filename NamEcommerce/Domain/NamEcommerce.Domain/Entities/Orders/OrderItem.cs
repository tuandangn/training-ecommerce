using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Orders;

[Serializable]
public sealed record OrderItem : AppEntity
{
    public OrderItem(Guid id, Guid orderId, Guid productId, decimal unitPrice, decimal quantity) : base(id)
        => (OrderId, ProductId, UnitPrice, Quantity) = (orderId, productId, unitPrice, quantity);

    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public decimal UnitPrice { get; internal set; }
    public decimal Discount { get; internal set; }
    public decimal Quantity { get; internal set; }
    public decimal Price => (UnitPrice - Discount) * Quantity;

    internal void Update(decimal quantity, decimal unitPrice)
    {
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
