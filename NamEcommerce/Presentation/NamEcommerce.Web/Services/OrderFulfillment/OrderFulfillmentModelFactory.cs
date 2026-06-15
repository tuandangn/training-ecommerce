using MediatR;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;
using NamEcommerce.Web.Models.OrderFulfillment;

namespace NamEcommerce.Web.Services.OrderFulfillment;

public sealed class OrderFulfillmentModelFactory : IOrderFulfillmentModelFactory
{
    private readonly IMediator _mediator;

    public OrderFulfillmentModelFactory(IMediator mediator, AppConfig appConfig)
    {
        _mediator = mediator;
        _ = appConfig;
    }

    public async Task<OrderFulfillmentBoardSearchModel> PrepareBoardModelAsync(OrderFulfillmentBoardSearchModel searchModel)
    {
        var model = searchModel ?? new OrderFulfillmentBoardSearchModel();
        model.Board = await _mediator.Send(new GetOrderFulfillmentBoardQuery
        {
            Range = string.IsNullOrWhiteSpace(model.Range) ? "today" : model.Range,
            Date = model.Date ?? DateTime.Today,
            Keywords = model.Keywords,
            Risk = model.Risk,
            IncludeInactive = model.IncludeInactive
        }).ConfigureAwait(false);

        return model;
    }

    public async Task<OrderFulfillmentSchedulePanelModel> PrepareSchedulePanelModelAsync(Guid orderId)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery { Id = orderId }).ConfigureAwait(false);
        var schedules = await _mediator.Send(new GetOrderFulfillmentSchedulesByOrderQuery(orderId)).ConfigureAwait(false);

        return new OrderFulfillmentSchedulePanelModel
        {
            OrderId = orderId,
            CanUpdateSchedules = order?.CanUpdateInfo == true,
            AvailableItems = order?.Items.Select(item => new OrderFulfillmentScheduleAvailableItemModel
            {
                OrderItemId = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName ?? string.Empty,
                Quantity = item.Quantity,
                QuantityDecimalPlaces = 0
            }).ToList() ?? [],
            Schedules = schedules
        };
    }
}
