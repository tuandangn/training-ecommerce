using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class UpdateOrderItemHandler : IRequestHandler<UpdateOrderItemCommand, CommonResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public UpdateOrderItemHandler(IOrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    public async Task<CommonResultModel> Handle(UpdateOrderItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.UpdateOrderItemAsync(new UpdateOrderItemAppDto(request.OrderId, request.ItemId, request.Quantity, request.UnitPrice)).ConfigureAwait(false);
        return new CommonResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class DeleteOrderItemHandler : IRequestHandler<DeleteOrderItemCommand, CommonResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public DeleteOrderItemHandler(IOrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    public async Task<CommonResultModel> Handle(DeleteOrderItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.DeleteOrderItemAsync(new DeleteOrderItemAppDto(request.OrderId, request.ItemId)).ConfigureAwait(false);
        return new CommonResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
