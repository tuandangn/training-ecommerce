using MediatR;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Application.Contracts.Orders;

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
        if (request.Quantity <= 0 || request.UnitPrice < 0)
            return new AddOrderItemResultModel { Success = false, ErrorMessage = "Invalid quantity or price" };

        try
        {
            await _orderAppService.AddOrderItemAsync(new NamEcommerce.Application.Contracts.Dtos.Orders.AddOrderItemAppDto(request.OrderId, request.ProductId, request.Quantity, request.UnitPrice)).ConfigureAwait(false);
            return new AddOrderItemResultModel { Success = true };
        }
        catch (Exception ex)
        {
            return new AddOrderItemResultModel { Success = false, ErrorMessage = ex.Message };
        }
    }
}
