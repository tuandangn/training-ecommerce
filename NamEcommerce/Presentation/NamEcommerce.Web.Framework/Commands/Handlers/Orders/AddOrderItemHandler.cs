using MediatR;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Dtos.Orders;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class AddOrderItemHandler : IRequestHandler<AddOrderItemCommand, AddOrderItemResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public AddOrderItemHandler(IOrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    public async Task<AddOrderItemResultModel> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        var addOrderItemResult = await _orderAppService.AddOrderItemAsync(new AddOrderItemAppDto
        {
            OrderId = request.OrderId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice
        }).ConfigureAwait(false);

        return new AddOrderItemResultModel
        {
            Success = addOrderItemResult.Success,
            ErrorMessage = addOrderItemResult.ErrorMessage
        };
    }
}
