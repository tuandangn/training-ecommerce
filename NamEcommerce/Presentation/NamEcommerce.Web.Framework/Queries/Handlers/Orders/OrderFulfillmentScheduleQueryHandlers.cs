using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Orders;

public sealed class GetOrderFulfillmentBoardHandler(IOrderFulfillmentScheduleAppService appService)
    : IRequestHandler<GetOrderFulfillmentBoardQuery, OrderFulfillmentBoardModel>
{
    public async Task<OrderFulfillmentBoardModel> Handle(GetOrderFulfillmentBoardQuery request, CancellationToken cancellationToken)
    {
        var board = await appService.GetBoardAsync(new OrderFulfillmentBoardFilterAppDto
        {
            DateUtc = request.Date?.ToUniversalTime(),
            Keywords = request.Keywords,
            Risk = request.Risk,
            IncludeInactive = request.IncludeInactive
        }).ConfigureAwait(false);

        return ToModel(board);
    }

    private static OrderFulfillmentBoardModel ToModel(OrderFulfillmentBoardAppDto board)
        => new()
        {
            DateUtc = board.DateUtc,
            Today = board.Today.Select(ToModel).ToList(),
            Next3Days = board.Next3Days.Select(ToModel).ToList(),
            Next7Days = board.Next7Days.Select(ToModel).ToList(),
            Next30Days = board.Next30Days.Select(ToModel).ToList(),
            UnscheduledGroups = board.UnscheduledGroups.Select(ToModel).ToList(),
            TotalEntries = board.TotalEntries,
            DangerCount = board.DangerCount,
            WarningCount = board.WarningCount
        };

    private static OrderFulfillmentBoardDayModel ToModel(OrderFulfillmentBoardDayAppDto day)
        => new()
        {
            DateUtc = day.DateUtc,
            Label = day.Label,
            Entries = day.Entries.Select(ToModel).ToList()
        };

    private static OrderFulfillmentUnscheduledGroupModel ToModel(OrderFulfillmentUnscheduledGroupAppDto group)
        => new()
        {
            Key = group.Key,
            Title = group.Title,
            Tone = group.Tone,
            Entries = group.Entries.Select(ToModel).ToList()
        };

    private static OrderFulfillmentBoardEntryModel ToModel(OrderFulfillmentBoardEntryAppDto entry)
        => new()
        {
            Id = entry.Id,
            SourceType = entry.SourceType,
            SourceId = entry.SourceId,
            SourceCode = entry.SourceCode,
            OrderId = entry.OrderId,
            OrderCode = entry.OrderCode,
            CustomerName = entry.CustomerName,
            CustomerPhone = entry.CustomerPhone,
            ShippingAddress = entry.ShippingAddress,
            ScheduledFromUtc = entry.ScheduledFromUtc,
            ScheduledToUtc = entry.ScheduledToUtc,
            Mode = entry.Mode,
            Tone = entry.Tone,
            StatusText = entry.StatusText,
            IsActive = entry.IsActive,
            Note = entry.Note,
            Items = entry.Items.Select(ToModel).ToList(),
            Dependencies = entry.Dependencies.Select(ToModel).ToList()
        };

    private static OrderFulfillmentBoardItemModel ToModel(OrderFulfillmentBoardItemAppDto item)
        => new()
        {
            OrderItemId = item.OrderItemId,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            ScheduledQuantity = item.ScheduledQuantity,
            ShippedQuantity = item.ShippedQuantity,
            AvailableQuantity = item.AvailableQuantity,
            MissingSourceQuantity = item.MissingSourceQuantity
        };

    private static OrderFulfillmentBoardDependencyModel ToModel(OrderFulfillmentBoardDependencyAppDto dependency)
        => new()
        {
            PurchaseOrderId = dependency.PurchaseOrderId,
            PurchaseOrderCode = dependency.PurchaseOrderCode,
            AllocatedQuantity = dependency.AllocatedQuantity,
            ReceivedQuantity = dependency.ReceivedQuantity,
            ExpectedReceiveDateUtc = dependency.ExpectedReceiveDateUtc,
            IsDirectShip = dependency.IsDirectShip
        };
}

public sealed class GetOrderFulfillmentSchedulesByOrderHandler(IOrderFulfillmentScheduleAppService appService)
    : IRequestHandler<GetOrderFulfillmentSchedulesByOrderQuery, IList<OrderFulfillmentScheduleModel>>
{
    public async Task<IList<OrderFulfillmentScheduleModel>> Handle(GetOrderFulfillmentSchedulesByOrderQuery request, CancellationToken cancellationToken)
        => (await appService.GetByOrderIdAsync(request.OrderId).ConfigureAwait(false))
            .Select(ToModel)
            .ToList();

    private static OrderFulfillmentScheduleModel ToModel(OrderFulfillmentScheduleAppDto schedule)
        => new(schedule.Id)
        {
            OrderId = schedule.OrderId,
            OrderCode = schedule.OrderCode,
            ScheduledFromUtc = schedule.ScheduledFromUtc,
            ScheduledToUtc = schedule.ScheduledToUtc,
            Mode = schedule.Mode,
            Note = schedule.Note,
            IsActive = schedule.IsActive,
            CreatedByUserId = schedule.CreatedByUserId,
            CreatedOnUtc = schedule.CreatedOnUtc,
            UpdatedOnUtc = schedule.UpdatedOnUtc,
            InactivatedOnUtc = schedule.InactivatedOnUtc,
            Items = schedule.Items.Select(item => new OrderFulfillmentScheduleItemModel(item.Id)
            {
                OrderFulfillmentScheduleId = item.OrderFulfillmentScheduleId,
                OrderItemId = item.OrderItemId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                CreatedOnUtc = item.CreatedOnUtc
            }).ToList()
        };
}
