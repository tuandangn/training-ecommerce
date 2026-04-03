using MediatR;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Dtos.Orders;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, CreateOrderResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public CreateOrderHandler(IOrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    public async Task<CreateOrderResultModel> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var dto = new CreateOrderAppDto
        {
            CustomerId = request.CustomerId,
            WarehouseId = request.WarehouseId,
            OrderDiscount = request.OrderDiscount,
            Note = request.Note,
            Items = request.Items.Select(i => new OrderItemAppDto(Guid.Empty, i.ProductId, i.Quantity, i.UnitPrice)).ToList()
        };

        var result = await _orderAppService.CreateOrderAsync(dto).ConfigureAwait(false);

        return new CreateOrderResultModel
        {
            Success = result.Success,
            CreatedId = result.CreatedId
        };
    }
}
