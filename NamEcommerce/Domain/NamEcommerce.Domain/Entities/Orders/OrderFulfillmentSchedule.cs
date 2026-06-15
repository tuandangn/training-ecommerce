using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions.Orders;

namespace NamEcommerce.Domain.Entities.Orders;

[Serializable]
public sealed record OrderFulfillmentSchedule : AppAggregateEntity
{
    public OrderFulfillmentSchedule(Guid id) : base(id)
    {
        OrderCode = string.Empty;
        _items = [];
    }

    internal OrderFulfillmentSchedule(
        Guid orderId,
        string orderCode,
        OrderFulfillmentScheduleMode mode,
        DateTime? scheduledFromUtc,
        DateTime? scheduledToUtc,
        string? note,
        Guid? createdByUserId) : base(Guid.NewGuid())
    {
        if (orderId == Guid.Empty)
            throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderIsNotFound");
        if (!Enum.IsDefined(mode))
            throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleModeInvalid");

        OrderId = orderId;
        OrderCode = string.IsNullOrWhiteSpace(orderCode) ? string.Empty : orderCode.Trim();
        Mode = mode;
        CreatedByUserId = createdByUserId;
        CreatedOnUtc = DateTime.UtcNow;
        IsActive = true;
        _items = [];

        SetWindow(scheduledFromUtc, scheduledToUtc);
        SetNote(note);
    }

    public Guid OrderId { get; private set; }
    public string OrderCode { get; private set; }
    public DateTime? ScheduledFromUtc { get; private set; }
    public DateTime? ScheduledToUtc { get; private set; }
    public OrderFulfillmentScheduleMode Mode { get; private set; }
    public string? Note { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    public DateTime? InactivatedOnUtc { get; private set; }

    private readonly List<OrderFulfillmentScheduleItem> _items;
    public IReadOnlyCollection<OrderFulfillmentScheduleItem> Items => _items.AsReadOnly();

    internal void SetWindow(DateTime? scheduledFromUtc, DateTime? scheduledToUtc)
    {
        if (scheduledFromUtc.HasValue && scheduledToUtc.HasValue && scheduledToUtc.Value < scheduledFromUtc.Value)
            throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleWindowInvalid");

        ScheduledFromUtc = scheduledFromUtc;
        ScheduledToUtc = scheduledToUtc;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void SetMode(OrderFulfillmentScheduleMode mode)
    {
        if (!Enum.IsDefined(mode))
            throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleModeInvalid");

        Mode = mode;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void SetNote(string? note)
    {
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void ReplaceItems(IEnumerable<CreateOrderFulfillmentScheduleItemDto> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var newItems = items.ToList();
        if (newItems.Count == 0)
            throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleItemRequired");

        foreach (var item in newItems)
        {
            if (item.OrderItemId == Guid.Empty)
                throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderItemIsNotFound");
            if (item.ProductId == Guid.Empty)
                throw new OrderFulfillmentScheduleDataIsInvalidException("Error.ProductIsNotFound");
            if (item.Quantity <= 0)
                throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleQuantityMustBePositive");
        }

        _items.Clear();
        foreach (var item in newItems)
        {
            _items.Add(new OrderFulfillmentScheduleItem(
                Id,
                item.OrderItemId,
                item.ProductId,
                item.ProductName ?? string.Empty,
                item.Quantity));
        }

        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Activate()
    {
        IsActive = true;
        InactivatedOnUtc = null;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Inactivate()
    {
        IsActive = false;
        InactivatedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
