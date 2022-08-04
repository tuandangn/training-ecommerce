using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Orders;

[Serializable]
public sealed record Order : AppAggregateEntity
{
    public Order(Guid id, Guid userId, decimal orderTotal, PaymentStatus paymentStatus, OrderStatus orderStatus)
        : this(id, userId, orderTotal, paymentStatus, orderStatus, Array.Empty<OrderItem>())
    { }

    public Order(Guid id, Guid userId, decimal orderTotal, PaymentStatus paymentStatus, OrderStatus orderStatus, IList<OrderItem> orderItems) 
        : base(id)
        => (UserId, OrderTotal, PaymentStatus, OrderStatus, _orderItems) 
        = (userId, orderTotal, paymentStatus, orderStatus, orderItems);

    public Guid UserId { get; init; }
    public decimal OrderTotal { get; init; }
    public decimal OrderDiscount { get; init; }

    public PaymentStatus PaymentStatus { get; init; }
    public ShippingStatus ShippingStatus { get; init; }
    public OrderStatus OrderStatus { get; init; }

    private IList<OrderItem> _orderItems;
    public IEnumerable<OrderItem> OrderItems => _orderItems.AsEnumerable();

    public DateTime CreatedOnUtc { get; init; }
        = DateTime.UtcNow;
    public DateTime? UpdatedOnUtc { get; init; }
}
