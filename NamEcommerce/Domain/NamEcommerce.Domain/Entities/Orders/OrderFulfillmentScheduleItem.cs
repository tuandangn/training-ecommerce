using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Orders;

[Serializable]
public sealed record OrderFulfillmentScheduleItem : AppEntity
{
    private OrderFulfillmentScheduleItem(Guid id) : base(id)
    {
        ProductName = string.Empty;
    }

    internal OrderFulfillmentScheduleItem(Guid scheduleId, Guid orderItemId, Guid productId, string productName, decimal quantity)
        : base(Guid.Empty)
    {
        OrderFulfillmentScheduleId = scheduleId;
        OrderItemId = orderItemId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid OrderFulfillmentScheduleId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
}
