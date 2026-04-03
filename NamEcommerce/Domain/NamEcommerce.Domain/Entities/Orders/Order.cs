using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Orders;

[Serializable]
public sealed record Order : AppAggregateEntity
{
    public Order(Guid id, Guid customerId, Guid? warehouseId, decimal orderTotal, PaymentStatus paymentStatus, OrderStatus orderStatus)
        : this(id, customerId, warehouseId, orderTotal, paymentStatus, orderStatus, Array.Empty<OrderItem>())
    { }

    public Order(Guid id, Guid customerId, Guid? warehouseId, decimal orderTotal, PaymentStatus paymentStatus, OrderStatus orderStatus, IList<OrderItem> orderItems) 
        : base(id)
    {
        CustomerId = customerId;
        WarehouseId = warehouseId;
        OrderTotal = orderTotal;
        PaymentStatus = paymentStatus;
        OrderStatus = orderStatus;
        _orderItems = orderItems.ToList();
    }

    public Guid CustomerId { get; init; }
    public Guid? WarehouseId { get; internal set; }
    public decimal OrderTotal { get; internal set; }
    public decimal OrderDiscount { get; internal set; }

    public PaymentStatus PaymentStatus { get; internal set; }
    public ShippingStatus ShippingStatus { get; internal set; }
    public OrderStatus OrderStatus { get; internal set; }

    private readonly List<OrderItem> _orderItems = new();
    public IEnumerable<OrderItem> OrderItems => _orderItems.AsReadOnly();

    public DateTime CreatedOnUtc { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedOnUtc { get; internal set; }

    public string? Note { get; internal set; }

    internal void AddOrderItem(OrderItem item)
    {
        if (item.OrderId != Id)
            throw new InvalidOperationException("Item does not belong to this order");

        _orderItems.Add(item);
        RecalculateTotal();
    }

    internal void UpdateOrderItem(Guid itemId, decimal quantity, decimal unitPrice)
    {
        var item = _orderItems.FirstOrDefault(i => i.Id == itemId);
        if (item == null) throw new ArgumentException("Order item not found");

        item.Update(quantity, unitPrice);
        RecalculateTotal();
    }

    internal void RemoveOrderItem(Guid itemId)
    {
        var item = _orderItems.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return;

        _orderItems.Remove(item);
        RecalculateTotal();
    }

    private void RecalculateTotal()
    {
        OrderTotal = _orderItems.Sum(i => i.Price) - OrderDiscount;
    }

    public bool CanChangeStatusTo(OrderStatus toStatus)
    {
        if (OrderStatus == OrderStatus.Completed || OrderStatus == OrderStatus.Cancelled)
            return false;

        // simple flow: Pending -> Processing -> Completed
        var subtract = (int)toStatus - (int)OrderStatus;
        return new[] { 0, 1, 2 }.Contains(subtract);
    }

    internal void ChangeStatus(OrderStatus status)
    {
        if (!CanChangeStatusTo(status))
            throw new InvalidOperationException("Cannot change order status to the specified status");

        OrderStatus = status;
    }
}
