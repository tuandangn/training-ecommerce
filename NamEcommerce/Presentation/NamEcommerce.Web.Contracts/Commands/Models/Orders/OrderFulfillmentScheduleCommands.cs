using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed record OrderFulfillmentScheduleItemCommand
{
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public string? ProductName { get; init; }
    public required decimal Quantity { get; init; }
}

[Serializable]
public sealed record CreateOrderFulfillmentScheduleCommand : ICommand<CommonActionResultModel>
{
    public required Guid OrderId { get; init; }
    public DateTime? ScheduledFromUtc { get; init; }
    public DateTime? ScheduledToUtc { get; init; }
    public required int Mode { get; init; }
    public string? Note { get; init; }
    public IList<OrderFulfillmentScheduleItemCommand> Items { get; init; } = [];
}

[Serializable]
public sealed record UpdateOrderFulfillmentScheduleCommand : ICommand<CommonActionResultModel>
{
    public required Guid Id { get; init; }
    public required Guid OrderId { get; init; }
    public DateTime? ScheduledFromUtc { get; init; }
    public DateTime? ScheduledToUtc { get; init; }
    public required int Mode { get; init; }
    public string? Note { get; init; }
    public IList<OrderFulfillmentScheduleItemCommand> Items { get; init; } = [];
}

[Serializable]
public sealed record SetOrderFulfillmentScheduleActiveCommand(Guid Id, bool IsActive) : ICommand<CommonActionResultModel>;

[Serializable]
public sealed record DeleteOrderFulfillmentScheduleCommand(Guid Id) : ICommand<CommonActionResultModel>;
