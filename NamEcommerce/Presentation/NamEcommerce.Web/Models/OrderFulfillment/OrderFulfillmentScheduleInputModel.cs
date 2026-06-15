using NamEcommerce.Web.Contracts.Models.Orders;

namespace NamEcommerce.Web.Models.OrderFulfillment;

[Serializable]
public sealed record OrderFulfillmentScheduleInputModel
{
    public Guid? Id { get; set; }
    public required Guid OrderId { get; set; }
    public DateTime? ScheduledFromUtc { get; set; }
    public DateTime? ScheduledToUtc { get; set; }
    public required int Mode { get; set; }
    public string? Note { get; set; }
    public IList<OrderFulfillmentScheduleItemInputModel> Items { get; set; } = [];
}

[Serializable]
public sealed record OrderFulfillmentScheduleItemInputModel
{
    public required Guid OrderItemId { get; set; }
    public required Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public required decimal Quantity { get; set; }
}

[Serializable]
public sealed record OrderFulfillmentSchedulePanelModel
{
    public required Guid OrderId { get; init; }
    public bool CanUpdateSchedules { get; init; }
    public IList<OrderFulfillmentScheduleAvailableItemModel> AvailableItems { get; init; } = [];
    public IList<OrderFulfillmentScheduleModel> Schedules { get; init; } = [];
}

[Serializable]
public sealed record OrderFulfillmentScheduleAvailableItemModel
{
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public int QuantityDecimalPlaces { get; init; }
}
