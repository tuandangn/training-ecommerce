namespace NamEcommerce.Domain.Entities.Orders;

[Serializable]
public sealed record OrderItem : AppEntity
{
    public OrderItem(int id, int orderId, int productId, decimal unitPrice, decimal quantity) : base(id)
        => (OrderId, ProductId, UnitPrice, Quantity) = (orderId, productId, unitPrice, quantity);

    public int OrderId { get; init; }
    public int ProductId { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Discount { get; init; }
    public decimal Quantity { get; init; }
    public decimal Price => (UnitPrice - Discount) * Quantity;
}
