namespace NamEcommerce.Web.Contracts.Models.Orders;

[Serializable]
public sealed record OrderFulfillmentScheduleItemModel(Guid Id)
{
    public required Guid OrderFulfillmentScheduleId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record OrderFulfillmentScheduleModel(Guid Id)
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
    public IList<OrderFulfillmentScheduleItemModel> Items { get; init; } = [];
}

[Serializable]
public sealed record OrderFulfillmentBoardModel
{
    public required DateTime DateUtc { get; init; }
    public IList<OrderFulfillmentBoardDayModel> Overdue { get; init; } = [];
    public IList<OrderFulfillmentBoardDayModel> Today { get; init; } = [];
    public IList<OrderFulfillmentBoardDayModel> Next3Days { get; init; } = [];
    public IList<OrderFulfillmentBoardDayModel> Next7Days { get; init; } = [];
    public IList<OrderFulfillmentBoardDayModel> Next30Days { get; init; } = [];
    public IList<OrderFulfillmentUnscheduledGroupModel> UnscheduledGroups { get; init; } = [];
    public int TotalEntries { get; init; }
    public int OverdueCount { get; init; }
    public int DangerCount { get; init; }
    public int WarningCount { get; init; }
}

[Serializable]
public sealed record OrderFulfillmentBoardDayModel
{
    public required DateTime DateUtc { get; init; }
    public required string Label { get; init; }
    public IList<OrderFulfillmentBoardEntryModel> Entries { get; init; } = [];
}

[Serializable]
public sealed record OrderFulfillmentUnscheduledGroupModel
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string Tone { get; init; }
    public IList<OrderFulfillmentBoardEntryModel> Entries { get; init; } = [];
}

[Serializable]
public sealed record OrderFulfillmentBoardEntryModel
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
    public IList<OrderFulfillmentBoardItemModel> Items { get; init; } = [];
    public IList<OrderFulfillmentBoardDependencyModel> Dependencies { get; init; } = [];
}

[Serializable]
public sealed record OrderFulfillmentBoardItemModel
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
public sealed record OrderFulfillmentBoardDependencyModel
{
    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }
    public required decimal AllocatedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public DateTime? ExpectedReceiveDateUtc { get; init; }
    public bool IsDirectShip { get; init; }
}
