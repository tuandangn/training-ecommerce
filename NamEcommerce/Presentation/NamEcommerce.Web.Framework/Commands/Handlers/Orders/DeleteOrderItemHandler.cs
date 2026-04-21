using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class DeleteOrderItemHandler : IRequestHandler<DeleteOrderItemCommand, CommonActionResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public DeleteOrderItemHandler(IOrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    public async Task<CommonActionResultModel> Handle(DeleteOrderItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.DeleteOrderItemAsync(new DeleteOrderItemAppDto(request.OrderId, request.ItemId)).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
