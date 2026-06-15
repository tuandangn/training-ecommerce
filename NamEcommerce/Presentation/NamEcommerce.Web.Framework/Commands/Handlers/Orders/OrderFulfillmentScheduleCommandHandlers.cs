using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class CreateOrderFulfillmentScheduleHandler(IOrderFulfillmentScheduleAppService appService)
    : IRequestHandler<CreateOrderFulfillmentScheduleCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CreateOrderFulfillmentScheduleCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.CreateAsync(new CreateOrderFulfillmentScheduleAppDto
        {
            OrderId = request.OrderId,
            ScheduledFromUtc = request.ScheduledFromUtc?.ToUniversalTime(),
            ScheduledToUtc = request.ScheduledToUtc?.ToUniversalTime(),
            Mode = request.Mode,
            Note = request.Note,
            Items = request.Items.Select(ToInput).ToList()
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }

    private static OrderFulfillmentScheduleItemInputAppDto ToInput(OrderFulfillmentScheduleItemCommand item)
        => new()
        {
            OrderItemId = item.OrderItemId,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Quantity = item.Quantity
        };
}

public sealed class UpdateOrderFulfillmentScheduleHandler(IOrderFulfillmentScheduleAppService appService)
    : IRequestHandler<UpdateOrderFulfillmentScheduleCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(UpdateOrderFulfillmentScheduleCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.UpdateAsync(new UpdateOrderFulfillmentScheduleAppDto(request.Id)
        {
            OrderId = request.OrderId,
            ScheduledFromUtc = request.ScheduledFromUtc?.ToUniversalTime(),
            ScheduledToUtc = request.ScheduledToUtc?.ToUniversalTime(),
            Mode = request.Mode,
            Note = request.Note,
            Items = request.Items.Select(ToInput).ToList()
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }

    private static OrderFulfillmentScheduleItemInputAppDto ToInput(OrderFulfillmentScheduleItemCommand item)
        => new()
        {
            OrderItemId = item.OrderItemId,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Quantity = item.Quantity
        };
}

public sealed class SetOrderFulfillmentScheduleActiveHandler(IOrderFulfillmentScheduleAppService appService)
    : IRequestHandler<SetOrderFulfillmentScheduleActiveCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(SetOrderFulfillmentScheduleActiveCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.SetActiveAsync(new SetOrderFulfillmentScheduleActiveAppDto(request.Id, request.IsActive)).ConfigureAwait(false);
        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}

public sealed class DeleteOrderFulfillmentScheduleHandler(IOrderFulfillmentScheduleAppService appService)
    : IRequestHandler<DeleteOrderFulfillmentScheduleCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(DeleteOrderFulfillmentScheduleCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.DeleteAsync(request.Id).ConfigureAwait(false);
        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
