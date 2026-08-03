using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions.Orders;

namespace NamEcommerce.Domain.Shared.Dtos.Orders;

[Serializable]
public sealed record OrderFulfillmentScheduleDto(Guid Id)
{
    public required Guid OrderId { get; init; }
    public required string OrderCode { get; init; }
    public DateTime? ScheduledFromUtc { get; init; }
    public DateTime? ScheduledToUtc { get; init; }
    public required OrderFulfillmentScheduleMode Mode { get; init; }
    public string? Note { get; init; }
    public required bool IsActive { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public DateTime? InactivatedOnUtc { get; init; }
    public Guid? InactivatedByUserId { get; init; }
    public IList<OrderFulfillmentScheduleItemDto> Items { get; init; } = [];

    [Serializable]
    public sealed record OrderFulfillmentScheduleItemDto(Guid Id)
    {
        public required Guid OrderFulfillmentScheduleId { get; init; }
        public required Guid OrderItemId { get; init; }
        public required Guid ProductId { get; init; }
        public required string ProductName { get; init; }
        public required decimal Quantity { get; init; }
        public required DateTime CreatedOnUtc { get; init; }
    }
}

[Serializable]
public sealed record CreateOrderFulfillmentScheduleItemDto
{
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public required decimal Quantity { get; init; }
}

[Serializable]
public sealed record CreateOrderFulfillmentScheduleDto
{
    public required Guid OrderId { get; init; }
    public DateTime? ScheduledFromUtc { get; init; }
    public DateTime? ScheduledToUtc { get; init; }
    public required OrderFulfillmentScheduleMode Mode { get; init; }
    public string? Note { get; init; }
    public IList<CreateOrderFulfillmentScheduleItemDto> Items { get; init; } = [];

    public void Verify()
    {
        if (OrderId == Guid.Empty)
            throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderIsNotFound");
        VerifyScheduleData(Mode, ScheduledFromUtc, ScheduledToUtc, Items);
    }

    internal static void VerifyScheduleData(
        OrderFulfillmentScheduleMode mode,
        DateTime? scheduledFromUtc,
        DateTime? scheduledToUtc,
        IList<CreateOrderFulfillmentScheduleItemDto> items)
    {
        if (!Enum.IsDefined(mode))
            throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleModeInvalid");
        if (mode == OrderFulfillmentScheduleMode.NotBeforeDate && !scheduledFromUtc.HasValue)
            throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleDateRequired");
        if (scheduledFromUtc.HasValue && scheduledToUtc.HasValue && scheduledToUtc.Value < scheduledFromUtc.Value)
            throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleWindowInvalid");
        if (items.Count == 0)
            throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleItemRequired");

        foreach (var item in items)
        {
            if (item.OrderItemId == Guid.Empty)
                throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderItemIsNotFound");
            if (item.ProductId == Guid.Empty)
                throw new OrderFulfillmentScheduleDataIsInvalidException("Error.ProductIsNotFound");
            if (item.Quantity <= 0)
                throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderFulfillmentScheduleQuantityMustBePositive");
        }
    }
}

[Serializable]
public sealed record CreateOrderFulfillmentScheduleResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateOrderFulfillmentScheduleDto(Guid Id)
{
    public required Guid OrderId { get; init; }
    public DateTime? ScheduledFromUtc { get; init; }
    public DateTime? ScheduledToUtc { get; init; }
    public required OrderFulfillmentScheduleMode Mode { get; init; }
    public required IList<CreateOrderFulfillmentScheduleItemDto> Items { get; init; }
    public string? Note { get; init; }
    public bool? IsActive { get; set; }

    public void Verify()
    {
        if (Id == Guid.Empty)
            throw new OrderFulfillmentScheduleIsNotFoundException(Id);
        if (OrderId == Guid.Empty)
            throw new OrderFulfillmentScheduleDataIsInvalidException("Error.OrderIsNotFound");
        CreateOrderFulfillmentScheduleDto.VerifyScheduleData(Mode, ScheduledFromUtc, ScheduledToUtc, Items);
    }
}

[Serializable]
public sealed record UpdateOrderFulfillmentScheduleResultDto
{
    public required Guid UpdatedId { get; init; }
}

[Serializable]
public sealed record SetOrderFulfillmentScheduleActiveDto(Guid Id, bool IsActive);
