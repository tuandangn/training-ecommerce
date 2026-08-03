using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Dtos.Orders;

[Serializable]
public sealed record OrderFulfillmentScheduleItemAppDto(Guid Id)
{
    public required Guid OrderFulfillmentScheduleId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record OrderFulfillmentScheduleAppDto(Guid Id)
{
    public required Guid OrderId { get; init; }
    public required string OrderCode { get; init; }
    public DateTime? ScheduledFromUtc { get; init; }
    public DateTime? ScheduledToUtc { get; init; }
    public required int Mode { get; init; }
    public string? Note { get; init; }
    public required bool IsActive { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public DateTime? InactivatedOnUtc { get; init; }
    public Guid? InactivatedByUserId { get; init; }
    public IList<OrderFulfillmentScheduleItemAppDto> Items { get; init; } = [];
}

[Serializable]
public sealed record OrderFulfillmentScheduleItemInputAppDto
{
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public required decimal Quantity { get; init; }
}

[Serializable]
public sealed record CreateOrderFulfillmentScheduleAppDto
{
    public required Guid OrderId { get; init; }
    public DateTime? ScheduledFromUtc { get; init; }
    public DateTime? ScheduledToUtc { get; init; }
    public required int Mode { get; init; }
    public string? Note { get; init; }
    public IList<OrderFulfillmentScheduleItemInputAppDto> Items { get; init; } = [];

    public (bool valid, string? errorMessage) Validate()
        => OrderFulfillmentScheduleValidation.Validate(OrderId, Mode, ScheduledFromUtc, ScheduledToUtc, Items);
}

[Serializable]
public sealed record CreateOrderFulfillmentScheduleResultAppDto : CommonActionResultDto
{
    public Guid? CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateOrderFulfillmentScheduleAppDto(Guid Id)
{
    public required Guid OrderId { get; init; }
    public DateTime? ScheduledFromUtc { get; init; }
    public DateTime? ScheduledToUtc { get; init; }
    public required int Mode { get; init; }
    public required IList<OrderFulfillmentScheduleItemInputAppDto> Items { get; init; }
    public string? Note { get; init; }
    public bool? IsActive { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (Id == Guid.Empty)
            return (false, "Error.OrderFulfillmentScheduleIsNotFound");

        return OrderFulfillmentScheduleValidation.Validate(OrderId, Mode, ScheduledFromUtc, ScheduledToUtc, Items);
    }
}

[Serializable]
public sealed record UpdateOrderFulfillmentScheduleResultAppDto : CommonActionResultDto
{
    public Guid? UpdatedId { get; init; }
}

[Serializable]
public sealed record SetOrderFulfillmentScheduleActiveAppDto(Guid Id, bool IsActive);

[Serializable]
public sealed record OrderFulfillmentBoardFilterAppDto
{
    public DateTime? DateUtc { get; init; }
    public string? Keywords { get; init; }
    public string? Risk { get; init; }
    public bool IncludeInactive { get; init; }
}

[Serializable]
public sealed record OrderFulfillmentBoardAppDto
{
    public required DateTime DateUtc { get; init; }
    public IList<OrderFulfillmentBoardDayAppDto> Overdue { get; init; } = [];
    public IList<OrderFulfillmentBoardDayAppDto> Today { get; init; } = [];
    public IList<OrderFulfillmentBoardDayAppDto> Next3Days { get; init; } = [];
    public IList<OrderFulfillmentBoardDayAppDto> Next7Days { get; init; } = [];
    public IList<OrderFulfillmentBoardDayAppDto> Next30Days { get; init; } = [];
    public IList<OrderFulfillmentUnscheduledGroupAppDto> UnscheduledGroups { get; init; } = [];
    public int TotalEntries { get; init; }
    public int OverdueCount { get; init; }
    public int DangerCount { get; init; }
    public int WarningCount { get; init; }
}

[Serializable]
public sealed record OrderFulfillmentBoardDayAppDto
{
    public required DateTime DateUtc { get; init; }
    public required string Label { get; init; }
    public IList<OrderFulfillmentBoardEntryAppDto> Entries { get; init; } = [];
}

[Serializable]
public sealed record OrderFulfillmentUnscheduledGroupAppDto
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string Tone { get; init; }
    public IList<OrderFulfillmentBoardEntryAppDto> Entries { get; init; } = [];
}

[Serializable]
public sealed record OrderFulfillmentBoardEntryAppDto
{
    public required Guid Id { get; init; }
    public required string SourceType { get; init; }
    public required Guid SourceId { get; init; }
    public required string SourceCode { get; init; }
    public required Guid OrderId { get; init; }
    public required string OrderCode { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? ShippingAddress { get; init; }
    public DateTime? ScheduledFromUtc { get; init; }
    public DateTime? ScheduledToUtc { get; init; }
    public required int Mode { get; init; }
    public required string Tone { get; init; }
    public required string StatusText { get; init; }
    public required bool IsActive { get; init; }
    public string? Note { get; init; }
    public IList<OrderFulfillmentBoardItemAppDto> Items { get; init; } = [];
    public IList<OrderFulfillmentBoardDependencyAppDto> Dependencies { get; init; } = [];
}

[Serializable]
public sealed record OrderFulfillmentBoardItemAppDto
{
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal ScheduledQuantity { get; init; }
    public required decimal ShippedQuantity { get; init; }
    public required decimal AvailableQuantity { get; init; }
    public required decimal MissingSourceQuantity { get; init; }
}

[Serializable]
public sealed record OrderFulfillmentBoardDependencyAppDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public DateTime? ExpectedReceiveDateUtc { get; init; }
    public bool IsDirectShip { get; init; }
}

internal static class OrderFulfillmentScheduleValidation
{
    private const int AsSoonAsPossible = 10;
    private const int NotBeforeDate = 20;
    private const int WhenStockAvailable = 30;

    public static (bool valid, string? errorMessage) Validate(
        Guid orderId,
        int mode,
        DateTime? scheduledFromUtc,
        DateTime? scheduledToUtc,
        IList<OrderFulfillmentScheduleItemInputAppDto> items)
    {
        if (orderId == Guid.Empty)
            return (false, "Error.OrderIsNotFound");
        if (mode is not AsSoonAsPossible and not NotBeforeDate and not WhenStockAvailable)
            return (false, "Error.OrderFulfillmentScheduleModeInvalid");
        if (mode == NotBeforeDate && !scheduledFromUtc.HasValue)
            return (false, "Error.OrderFulfillmentScheduleDateRequired");
        if (scheduledFromUtc.HasValue && scheduledToUtc.HasValue && scheduledToUtc.Value < scheduledFromUtc.Value)
            return (false, "Error.OrderFulfillmentScheduleWindowInvalid");
        if (items.Count == 0)
            return (false, "Error.OrderFulfillmentScheduleItemRequired");
        if (items.Any(item => item.OrderItemId == Guid.Empty))
            return (false, "Error.OrderItemIsNotFound");
        if (items.Any(item => item.ProductId == Guid.Empty))
            return (false, "Error.ProductIsNotFound");
        if (items.Any(item => item.Quantity <= 0))
            return (false, "Error.OrderFulfillmentScheduleQuantityMustBePositive");

        return (true, null);
    }
}
